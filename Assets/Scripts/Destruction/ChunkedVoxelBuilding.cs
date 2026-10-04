using System;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

/// <summary>
/// Network authority and persistent state for a baked building. Chunks are
/// static local children; only this root is a FishNet object.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
public sealed class ChunkedVoxelBuilding : NetworkBehaviour
{
    [Header("Identity and chunks")]
    [SerializeField] private string buildingId;
    [SerializeField] private VoxelDestructionChunk[] chunks = Array.Empty<VoxelDestructionChunk>();
    [Min(1f)] [SerializeField] private float damageToDestroyChunk = 150f;

    [Header("Fragment materialization")]
    [Min(1)] [SerializeField] private int maxMaterializedFragmentsPerEvent = 24;
    [Min(0)] [SerializeField] private int maxPhysicalFragmentsPerEvent = 8;
    [Min(0.1f)] [SerializeField] private float minimumFragmentRadius = 1.25f;
    [Min(0f)] [SerializeField] private float minimumPhysicalVolume = 0.0001f;
    [Min(0f)] [SerializeField] private float debrisImpulse = 4f;
    [Min(0.25f)] [SerializeField] private float debrisLifetime = 12f;

    [Header("Global debris budget")]
    [Min(0)] [SerializeField] private int maxDynamicDebrisGlobally = 96;
    [Min(1)] [SerializeField] private int maxVisibleDebrisGlobally = 384;
    [Min(0f)] [SerializeField] private float settleSpeed = 0.15f;
    [Min(0f)] [SerializeField] private float settleDelay = 1.5f;
    [SerializeField] private string debrisLayer = "VoxelDebris";

    private readonly SyncList<int> destroyedChunkIds = new();
    private readonly List<FragmentCandidate> fragmentCandidates = new(64);
    private float[] accumulatedDamage = Array.Empty<float>();
    private bool[] locallyDestroyed = Array.Empty<bool>();
    private bool[] debrisPlayed = Array.Empty<bool>();
    private int eventRevision;
    private bool initialized;

    public string BuildingId => buildingId;
    public IReadOnlyList<VoxelDestructionChunk> Chunks => chunks;

    private readonly struct FragmentCandidate
    {
        public readonly VoxelFragmentDefinition Definition;
        public readonly float DistanceSquared;

        public FragmentCandidate(VoxelFragmentDefinition definition, float distanceSquared)
        {
            Definition = definition;
            DistanceSquared = distanceSquared;
        }
    }

    private void Awake()
    {
        EnsureInitialized();
        destroyedChunkIds.OnChange += OnDestroyedChunksChanged;
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        EnsureInitialized();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        EnsureInitialized();
        ApplyPersistentState();
    }

    private void OnDestroy()
    {
        destroyedChunkIds.OnChange -= OnDestroyedChunksChanged;
        VoxelDebrisPool.ReleaseBuilding(buildingId);
    }

    public void Configure(
        string id,
        VoxelDestructionChunk[] bakedChunks,
        float chunkHealth,
        int materializedPerEvent,
        int physicalPerEvent,
        int globalDynamicBudget,
        int globalVisibleBudget)
    {
        buildingId = id;
        chunks = bakedChunks ?? Array.Empty<VoxelDestructionChunk>();
        damageToDestroyChunk = Mathf.Max(1f, chunkHealth);
        maxMaterializedFragmentsPerEvent = Mathf.Max(1, materializedPerEvent);
        maxPhysicalFragmentsPerEvent = Mathf.Max(0, physicalPerEvent);
        maxDynamicDebrisGlobally = Mathf.Max(0, globalDynamicBudget);
        maxVisibleDebrisGlobally = Mathf.Max(1, globalVisibleBudget);
        initialized = false;
        EnsureInitialized();
    }

    public void TakeChunkDamage(int chunkId, float damage, Vector3 hitPoint, float radius)
    {
        if (!IsFinitePositive(damage)) return;
        EnsureInitialized();
        if (!IsValidChunk(chunkId) || locallyDestroyed[chunkId]) return;

        radius = IsFinite(radius) ? Mathf.Max(0f, radius) : 0f;
        if (IsServerInitialized)
            ApplyDamageOnAuthority(chunkId, damage, hitPoint, radius);
        else if (IsClientInitialized)
            RequestChunkDamageServerRpc(buildingId, chunkId, damage, hitPoint, radius);
        else
            ApplyDamageOffline(chunkId, damage, hitPoint, radius);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestChunkDamageServerRpc(
        string requestedBuildingId,
        int chunkId,
        float damage,
        Vector3 hitPoint,
        float radius)
    {
        if (requestedBuildingId != buildingId || !IsFinitePositive(damage) || !IsFinite(radius)) return;
        ApplyDamageOnAuthority(chunkId, damage, hitPoint, Mathf.Max(0f, radius));
    }

    [Server]
    private void ApplyDamageOnAuthority(int chunkId, float damage, Vector3 hitPoint, float radius)
    {
        if (!IsValidChunk(chunkId) || locallyDestroyed[chunkId]) return;
        accumulatedDamage[chunkId] += damage;
        if (accumulatedDamage[chunkId] < damageToDestroyChunk) return;

        int seed = CreateEventSeed(chunkId);
        MarkChunkDestroyed(chunkId);
        destroyedChunkIds.Add(chunkId);
        ApplyDestructionEvent(chunkId, hitPoint, radius, seed, true, simulatePhysics: true);
        ObserversChunkStateRpc(buildingId, chunkId, hitPoint, radius, seed, true);
    }

    private void ApplyDamageOffline(int chunkId, float damage, Vector3 hitPoint, float radius)
    {
        accumulatedDamage[chunkId] += damage;
        if (accumulatedDamage[chunkId] < damageToDestroyChunk) return;
        int seed = CreateEventSeed(chunkId);
        MarkChunkDestroyed(chunkId);
        ApplyDestructionEvent(chunkId, hitPoint, radius, seed, true, simulatePhysics: true);
    }

    [ObserversRpc(ExcludeServer = true)]
    private void ObserversChunkStateRpc(
        string eventBuildingId,
        int chunkId,
        Vector3 hitPoint,
        float radius,
        int seed,
        bool destroyed)
    {
        if (eventBuildingId != buildingId || !IsValidChunk(chunkId)) return;
        if (!destroyed)
        {
            chunks[chunkId].SetDestroyed(false);
            locallyDestroyed[chunkId] = false;
            debrisPlayed[chunkId] = false;
            return;
        }

        MarkChunkDestroyed(chunkId);
        ApplyDestructionEvent(chunkId, hitPoint, radius, seed, true, simulatePhysics: false);
    }

    private void ApplyDestructionEvent(
        int chunkId,
        Vector3 hitPoint,
        float radius,
        int seed,
        bool destroyed,
        bool simulatePhysics)
    {
        if (!destroyed || debrisPlayed[chunkId]) return;
        debrisPlayed[chunkId] = true;
        VoxelDestructionChunk chunk = chunks[chunkId];
        VoxelDestructionChunkAsset data = chunk.Data;
        if (data == null || data.Fragments == null || data.Fragments.Length == 0) return;

        float selectionRadius = Mathf.Max(minimumFragmentRadius, radius);
        float radiusSquared = selectionRadius * selectionRadius;
        Matrix4x4 buildingToWorld = transform.localToWorldMatrix;
        fragmentCandidates.Clear();

        VoxelFragmentDefinition closest = null;
        float closestDistance = float.MaxValue;
        foreach (VoxelFragmentDefinition definition in data.Fragments)
        {
            if (definition == null || definition.Mesh == null) continue;
            Vector3 worldCenter = buildingToWorld.MultiplyPoint3x4(definition.LocalCenter);
            float distance = (worldCenter - hitPoint).sqrMagnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = definition;
            }
            if (distance <= radiusSquared)
                fragmentCandidates.Add(new FragmentCandidate(definition, distance));
        }

        if (fragmentCandidates.Count == 0 && closest != null)
            fragmentCandidates.Add(new FragmentCandidate(closest, closestDistance));
        fragmentCandidates.Sort((left, right) => left.DistanceSquared.CompareTo(right.DistanceSquared));

        int count = Mathf.Min(maxMaterializedFragmentsPerEvent, fragmentCandidates.Count);
        int physicalCount = 0;
        uint randomState = unchecked((uint)seed);
        bool visible = !IsServerInitialized || IsClientInitialized;
        int layer = ResolveDebrisLayer(chunk.gameObject.layer);
        for (int i = 0; i < count; i++)
        {
            VoxelFragmentDefinition definition = fragmentCandidates[i].Definition;
            Matrix4x4 worldMatrix = buildingToWorld * definition.LocalMatrix;
            Vector3 position = worldMatrix.GetColumn(3);
            Quaternion rotation = worldMatrix.rotation;
            Vector3 scale = worldMatrix.lossyScale;

            Vector3 direction = position - hitPoint;
            if (direction.sqrMagnitude < 0.0001f) direction = RandomUnitVector(ref randomState);
            else direction.Normalize();
            Vector3 scatter = RandomUnitVector(ref randomState) * 0.35f;
            float impulseScale = 0.65f + NextFloat(ref randomState) * 0.7f;
            Vector3 velocity = (direction + Vector3.up * 0.45f + scatter).normalized * debrisImpulse * impulseScale;
            Vector3 angularVelocity = RandomUnitVector(ref randomState) * (2f + NextFloat(ref randomState) * 6f);

            bool physical = simulatePhysics &&
                            physicalCount < maxPhysicalFragmentsPerEvent &&
                            definition.Volume >= minimumPhysicalVolume;
            if (physical) physicalCount++;
            if (!visible && !physical) continue;

            VoxelDebrisPool.Spawn(
                buildingId, chunkId, definition, position, rotation, scale,
                velocity, angularVelocity, physical, visible, layer,
                debrisLifetime, maxDynamicDebrisGlobally, maxVisibleDebrisGlobally,
                settleSpeed, settleDelay);
        }
    }

    [Server]
    public void ResetBuilding()
    {
        EnsureInitialized();
        destroyedChunkIds.Clear();
        Array.Clear(accumulatedDamage, 0, accumulatedDamage.Length);
        for (int i = 0; i < chunks.Length; i++)
        {
            locallyDestroyed[i] = false;
            debrisPlayed[i] = false;
            chunks[i]?.SetDestroyed(false);
        }
        VoxelDebrisPool.ReleaseBuilding(buildingId);
        ObserversResetBuildingRpc(buildingId);
    }

    [ObserversRpc(ExcludeServer = true)]
    private void ObserversResetBuildingRpc(string eventBuildingId)
    {
        if (eventBuildingId != buildingId) return;
        for (int i = 0; i < chunks.Length; i++)
        {
            locallyDestroyed[i] = false;
            debrisPlayed[i] = false;
            chunks[i]?.SetDestroyed(false);
        }
        VoxelDebrisPool.ReleaseBuilding(buildingId);
    }

    private void EnsureInitialized()
    {
        if (initialized) return;
        if (string.IsNullOrWhiteSpace(buildingId)) buildingId = Guid.NewGuid().ToString("N");
        chunks ??= Array.Empty<VoxelDestructionChunk>();
        accumulatedDamage = new float[chunks.Length];
        locallyDestroyed = new bool[chunks.Length];
        debrisPlayed = new bool[chunks.Length];
        for (int i = 0; i < chunks.Length; i++)
        {
            if (chunks[i] == null) continue;
            chunks[i].Configure(this, i, chunks[i].Data, chunks[i].VoxelMaterial);
        }
        initialized = true;
    }

    private void ApplyPersistentState()
    {
        for (int i = 0; i < destroyedChunkIds.Count; i++)
            if (IsValidChunk(destroyedChunkIds[i])) MarkChunkDestroyed(destroyedChunkIds[i]);
    }

    private void OnDestroyedChunksChanged(
        SyncListOperation operation,
        int index,
        int previous,
        int next,
        bool asServer)
    {
        if (asServer) return;
        if (operation == SyncListOperation.Add || operation == SyncListOperation.Insert)
        {
            if (IsValidChunk(next)) MarkChunkDestroyed(next);
        }
        else if (operation == SyncListOperation.Clear)
        {
            for (int i = 0; i < chunks.Length; i++)
            {
                locallyDestroyed[i] = false;
                debrisPlayed[i] = false;
                chunks[i]?.SetDestroyed(false);
            }
        }
    }

    private void MarkChunkDestroyed(int chunkId)
    {
        locallyDestroyed[chunkId] = true;
        chunks[chunkId]?.SetDestroyed(true);
    }

    private bool IsValidChunk(int chunkId) =>
        chunkId >= 0 && chunkId < chunks.Length && chunks[chunkId] != null;

    private int ResolveDebrisLayer(int fallback)
    {
        if (string.IsNullOrEmpty(debrisLayer)) return fallback;
        int result = LayerMask.NameToLayer(debrisLayer);
        return result < 0 ? fallback : result;
    }

    private int CreateEventSeed(int chunkId)
    {
        eventRevision++;
        return unchecked((buildingId.GetHashCode() * 397) ^ (chunkId * 31) ^ eventRevision);
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private static bool IsFinitePositive(float value) => value > 0f && IsFinite(value);

    private static uint NextRandom(ref uint state)
    {
        if (state == 0) state = 0x9E3779B9u;
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        return state;
    }

    private static float NextFloat(ref uint state) => (NextRandom(ref state) & 0x00FFFFFFu) / 16777215f;

    private static Vector3 RandomUnitVector(ref uint state)
    {
        float z = NextFloat(ref state) * 2f - 1f;
        float angle = NextFloat(ref state) * Mathf.PI * 2f;
        float radius = Mathf.Sqrt(Mathf.Max(0f, 1f - z * z));
        return new Vector3(radius * Mathf.Cos(angle), z, radius * Mathf.Sin(angle));
    }
}
