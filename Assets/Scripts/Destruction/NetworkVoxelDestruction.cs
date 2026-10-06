using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using VoxelDestructionPro.Data;
using VoxelDestructionPro.VoxelObjects;

[Serializable]
public struct NetworkVoxelDestructionCommand
{
    public uint Sequence;
    public Vector3 HitPoint;
    public Vector3 HitNormal;
    public float VoxelRadius;
    public byte DestructionType;
    public uint FragmentSeed;
    public int[] RemovedVoxelRanges;
}

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
public sealed class NetworkVoxelDestruction : NetworkBehaviour
{
    [Header("Validation")]
    [FormerlySerializedAs("maximumWorldRadius")]
    [Min(1f)] [SerializeField] private float maximumVoxelRadius = 50f;
    [Min(0f)] [SerializeField] private float hitValidationPadding = 2f;
    [Min(1)] [SerializeField] private int maximumPendingRequests = 128;
    [Min(0f)] [SerializeField] private float duplicateRequestWindow = 0.05f;
    [Min(0f)] [SerializeField] private float duplicatePositionTolerance = 0.05f;

    [Header("Target")]
    [SerializeField] private DynamicVoxelObj voxel;

    private readonly SyncList<NetworkVoxelDestructionCommand> destructionHistory = new();
    private readonly Queue<NetworkVoxelDestructionCommand> serverQueue = new();
    private readonly Queue<NetworkVoxelDestructionCommand> clientQueue = new();
    private readonly HashSet<uint> clientSequences = new();

    private uint nextSequence;
    private bool subscribed;
    private bool subscribedToVoxel;
    private bool waitingForServerResult;
    private NetworkVoxelDestructionCommand activeServerCommand;
    private uint expandedClientSequence;
    private int[] expandedClientIndices;
    private float lastServerRequestTime = float.NegativeInfinity;
    private NetworkVoxelDestructionCommand lastServerRequest;

    private void Awake() => EnsureTarget();

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        EnsureTarget();
        SubscribeHistory();
        SubscribeVoxel();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        for (int i = 0; i < destructionHistory.Count; i++)
            nextSequence = Math.Max(nextSequence, destructionHistory[i].Sequence);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (IsServerInitialized) return;

        for (int i = 0; i < destructionHistory.Count; i++)
            EnqueueClientCommand(destructionHistory[i]);
    }

    public override void OnStopNetwork()
    {
        UnsubscribeHistory();
        UnsubscribeVoxel();
        serverQueue.Clear();
        clientQueue.Clear();
        clientSequences.Clear();
        waitingForServerResult = false;
        expandedClientSequence = 0u;
        expandedClientIndices = null;
        base.OnStopNetwork();
    }

    private void Update()
    {
        if (IsServerInitialized) ProcessServerQueue();
        else if (IsClientInitialized) ProcessClientQueue();
    }

    public bool RequestDestruction(
        Vector3 hitPoint,
        Vector3 hitNormal,
        float voxelRadius,
        DestructionData.DestructionType destructionType)
    {
        if (!ValidateRequest(hitPoint, hitNormal, voxelRadius, destructionType)) return false;

        if (voxelRadius >= 5f)
            VoxelDestructionImpactEffect.Show(
                hitPoint,
                voxelRadius * Mathf.Abs(voxel.GetSingleVoxelSize()));

        if (IsServerInitialized)
            return EnqueueServerCommand(hitPoint, hitNormal, voxelRadius, destructionType);

        if (IsClientInitialized && IsSpawned)
        {
            RequestDestructionServerRpc(hitPoint, hitNormal, voxelRadius, (byte)destructionType);
            return true;
        }

        if (InstanceFinder.IsClientStarted || InstanceFinder.IsServerStarted)
            return false;

        // Supports an offline scene without silently requiring FishNet to be running.
        return ProcessHit.VoxelHitLocal(voxel, hitPoint, hitNormal, voxelRadius, destructionType, 0u);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestDestructionServerRpc(
        Vector3 hitPoint,
        Vector3 hitNormal,
        float voxelRadius,
        byte destructionType,
        NetworkConnection caller = null)
    {
        DestructionData.DestructionType type = (DestructionData.DestructionType)destructionType;
        if (!ValidateRequest(hitPoint, hitNormal, voxelRadius, type)) return;

        EnqueueServerCommand(hitPoint, hitNormal, voxelRadius, type);
    }

    private bool EnqueueServerCommand(
        Vector3 hitPoint,
        Vector3 hitNormal,
        float voxelRadius,
        DestructionData.DestructionType destructionType)
    {
        if (serverQueue.Count >= maximumPendingRequests) return false;

        float now = Time.unscaledTime;
        float toleranceSquared = duplicatePositionTolerance * duplicatePositionTolerance;
        bool duplicate = now - lastServerRequestTime <= duplicateRequestWindow &&
                         (lastServerRequest.HitPoint - hitPoint).sqrMagnitude <= toleranceSquared &&
                         Mathf.Abs(lastServerRequest.VoxelRadius - voxelRadius) <= 0.01f &&
                         lastServerRequest.DestructionType == (byte)destructionType;
        if (duplicate) return false;

        uint sequence = ++nextSequence;
        if (sequence == 0u) sequence = ++nextSequence;

        uint objectId = NetworkObject != null ? unchecked((uint)NetworkObject.ObjectId) : 0u;
        uint seed = unchecked((sequence * 747796405u) ^ (objectId * 2891336453u));
        if (seed == 0u) seed = 1u;

        var command = new NetworkVoxelDestructionCommand
        {
            Sequence = sequence,
            HitPoint = hitPoint,
            HitNormal = hitNormal,
            VoxelRadius = voxelRadius,
            DestructionType = (byte)destructionType,
            FragmentSeed = seed
        };

        serverQueue.Enqueue(command);
        lastServerRequest = command;
        lastServerRequestTime = now;

        // Start authoritative calculation in the same frame. Previously every
        // request waited for this component's next Update before it was scheduled.
        ProcessServerQueue();

        return true;
    }

    private void ProcessServerQueue()
    {
        if (waitingForServerResult || serverQueue.Count == 0 || voxel == null) return;

        NetworkVoxelDestructionCommand command = serverQueue.Peek();
        activeServerCommand = command;
        waitingForServerResult = true;

        if (!ProcessHit.VoxelHitLocal(
                voxel,
                command.HitPoint,
                command.HitNormal,
                command.VoxelRadius,
                (DestructionData.DestructionType)command.DestructionType,
                command.FragmentSeed))
        {
            waitingForServerResult = false;
        }
    }

    private void ProcessClientQueue()
    {
        if (clientQueue.Count == 0 || voxel == null) return;

        NetworkVoxelDestructionCommand command = clientQueue.Peek();
        if (expandedClientSequence != command.Sequence)
        {
            expandedClientSequence = command.Sequence;
            expandedClientIndices = ExpandRanges(command.RemovedVoxelRanges);
        }

        if (!voxel.ApplyAuthoritativeDestruction(
                expandedClientIndices,
                command.HitPoint,
                command.FragmentSeed,
                command.VoxelRadius))
            return;

        clientQueue.Dequeue();
        expandedClientSequence = 0u;
        expandedClientIndices = null;
    }

    private void OnServerDestructionCalculated(NativeList<int> removedVoxelIndices)
    {
        if (!IsServerInitialized || !waitingForServerResult) return;

        NetworkVoxelDestructionCommand result = activeServerCommand;
        result.RemovedVoxelRanges = CompressRanges(removedVoxelIndices);

        if (serverQueue.Count > 0 && serverQueue.Peek().Sequence == result.Sequence)
            serverQueue.Dequeue();

        if (result.RemovedVoxelRanges.Length == 0)
        {
            waitingForServerResult = false;
            return;
        }

        // The SyncList remains the persistent state for late joiners. A reliable
        // RPC bypasses its sync interval for observers that are already connected.
        ReceiveDestructionResultObserversRpc(result);
        destructionHistory.Add(result);
        waitingForServerResult = false;
    }

    [ObserversRpc(ExcludeServer = true)]
    private void ReceiveDestructionResultObserversRpc(NetworkVoxelDestructionCommand result)
    {
        if (!IsClientInitialized || IsServerInitialized) return;
        if (result.VoxelRadius >= 5f)
            VoxelDestructionImpactEffect.Show(
                result.HitPoint,
                result.VoxelRadius * Mathf.Abs(voxel.GetSingleVoxelSize()));
        EnqueueClientCommand(result);
    }

    private void OnDestructionHistoryChanged(
        SyncListOperation operation,
        int index,
        NetworkVoxelDestructionCommand previous,
        NetworkVoxelDestructionCommand next,
        bool asServer)
    {
        if (asServer || IsServerInitialized) return;

        if (operation == SyncListOperation.Add ||
            operation == SyncListOperation.Insert ||
            operation == SyncListOperation.Set)
        {
            EnqueueClientCommand(next);
        }
        else if (operation == SyncListOperation.Clear)
        {
            clientQueue.Clear();
            clientSequences.Clear();
        }
    }

    private void EnqueueClientCommand(NetworkVoxelDestructionCommand command)
    {
        if (command.Sequence == 0u || !clientSequences.Add(command.Sequence)) return;
        clientQueue.Enqueue(command);

        // Apply immediately when the voxel object is available instead of adding
        // another frame of latency after the network message is received.
        if (IsClientInitialized && !IsServerInitialized)
            ProcessClientQueue();
    }

    private bool ValidateRequest(
        Vector3 hitPoint,
        Vector3 hitNormal,
        float voxelRadius,
        DestructionData.DestructionType destructionType)
    {
        EnsureTarget();
        if (voxel == null || !IsFinite(hitPoint) || !IsFinite(hitNormal) ||
            !IsFinitePositive(voxelRadius)) return false;
        if (voxelRadius > maximumVoxelRadius) return false;
        if (destructionType < DestructionData.DestructionType.Sphere ||
            destructionType > DestructionData.DestructionType.Cube) return false;

        Collider targetCollider = voxel.targetCollider;
        if (targetCollider == null) return true;

        float voxelSize = Mathf.Abs(voxel.GetSingleVoxelSize());
        if (!IsFinitePositive(voxelSize)) return false;

        float allowedDistance = voxelRadius * voxelSize + hitValidationPadding;
        return targetCollider.bounds.SqrDistance(hitPoint) <= allowedDistance * allowedDistance;
    }

    private void EnsureTarget()
    {
        if (voxel == null) voxel = GetComponent<DynamicVoxelObj>();
    }

    private void SubscribeHistory()
    {
        if (subscribed) return;
        destructionHistory.OnChange += OnDestructionHistoryChanged;
        subscribed = true;
    }

    private void SubscribeVoxel()
    {
        if (subscribedToVoxel || voxel == null) return;
        voxel.onDestructionCalculated += OnServerDestructionCalculated;
        voxel.shouldDeferDestructionRebuild += HasPendingDestructionForVoxel;
        subscribedToVoxel = true;
    }

    private void UnsubscribeVoxel()
    {
        if (!subscribedToVoxel || voxel == null) return;
        voxel.onDestructionCalculated -= OnServerDestructionCalculated;
        voxel.shouldDeferDestructionRebuild -= HasPendingDestructionForVoxel;
        subscribedToVoxel = false;
    }

    private bool HasPendingDestructionForVoxel()
    {
        if (IsServerInitialized)
            return serverQueue.Count > 0;

        return IsClientInitialized && clientQueue.Count > 0;
    }

    private void UnsubscribeHistory()
    {
        if (!subscribed) return;
        destructionHistory.OnChange -= OnDestructionHistoryChanged;
        subscribed = false;
    }

    private static bool IsFinite(Vector3 value) =>
        IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);

    private static bool IsFinite(float value) =>
        !float.IsNaN(value) && !float.IsInfinity(value);

    private static bool IsFinitePositive(float value) => value > 0f && IsFinite(value);

    private static int[] CompressRanges(NativeList<int> indices)
    {
        if (!indices.IsCreated || indices.Length == 0) return Array.Empty<int>();

        var ranges = new List<int>();
        int start = indices[0];
        int previous = start;

        // VoxelDestructor sorts its parallel output once before publishing it.
        // Compress directly from that NativeList instead of allocating and sorting
        // another potentially very large managed array on the server.
        for (int i = 1; i < indices.Length; i++)
        {
            int current = indices[i];
            if (current == previous) continue;

            if (current == previous + 1)
            {
                previous = current;
                continue;
            }

            ranges.Add(start);
            ranges.Add(previous - start + 1);
            start = previous = current;
        }

        ranges.Add(start);
        ranges.Add(previous - start + 1);
        return ranges.ToArray();
    }

    private static int[] ExpandRanges(int[] ranges)
    {
        if (ranges == null || ranges.Length < 2) return Array.Empty<int>();

        int total = 0;
        for (int i = 1; i < ranges.Length; i += 2)
            total += Mathf.Max(0, ranges[i]);

        var result = new int[total];
        int output = 0;
        for (int i = 0; i + 1 < ranges.Length; i += 2)
        {
            int start = ranges[i];
            int count = Mathf.Max(0, ranges[i + 1]);
            for (int offset = 0; offset < count; offset++)
                result[output++] = start + offset;
        }

        return result;
    }
}

/// <summary>
/// Lightweight pooled dust burst used to hide the short interval between the
/// impact and the server-authoritative mesh rebuild. It is visual only and does
/// not participate in networking or physics.
/// </summary>
internal static class VoxelDestructionImpactEffect
{
    private const int PoolSize = 6;
    private const float DuplicateWindow = 0.08f;

    private static readonly ParticleSystem[] Pool = new ParticleSystem[PoolSize];
    private static Material dustMaterial;
    private static int nextEffect;
    private static float lastEffectTime = float.NegativeInfinity;
    private static Vector3 lastEffectPosition;
    private static float lastEffectRadius;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        for (int i = 0; i < Pool.Length; i++)
            Pool[i] = null;

        dustMaterial = null;
        nextEffect = 0;
        lastEffectTime = float.NegativeInfinity;
        lastEffectPosition = Vector3.zero;
        lastEffectRadius = 0f;
    }

    public static void Show(Vector3 position, float worldRadius)
    {
        if (Application.isBatchMode || !IsFinite(position) ||
            float.IsNaN(worldRadius) || float.IsInfinity(worldRadius) || worldRadius <= 0f)
            return;

        float radius = Mathf.Clamp(worldRadius, 0.5f, 6f);
        float duplicateDistance = Mathf.Max(0.35f, Mathf.Min(radius, lastEffectRadius) * 0.2f);
        if (Time.unscaledTime - lastEffectTime < DuplicateWindow &&
            (position - lastEffectPosition).sqrMagnitude <= duplicateDistance * duplicateDistance)
            return;

        lastEffectTime = Time.unscaledTime;
        lastEffectPosition = position;
        lastEffectRadius = radius;

        ParticleSystem effect = GetEffect();
        if (effect == null)
            return;

        GameObject effectObject = effect.gameObject;
        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        effectObject.transform.position = position;
        effectObject.SetActive(true);
        ConfigureBurst(effect, radius);
        effect.Play(true);
    }

    private static ParticleSystem GetEffect()
    {
        for (int offset = 0; offset < Pool.Length; offset++)
        {
            int index = (nextEffect + offset) % Pool.Length;
            ParticleSystem candidate = Pool[index];
            if (candidate == null)
            {
                candidate = CreateEffect(index);
                Pool[index] = candidate;
            }

            if (!candidate.gameObject.activeSelf || !candidate.IsAlive(true))
            {
                nextEffect = (index + 1) % Pool.Length;
                return candidate;
            }
        }

        ParticleSystem recycled = Pool[nextEffect];
        nextEffect = (nextEffect + 1) % Pool.Length;
        return recycled;
    }

    private static ParticleSystem CreateEffect(int index)
    {
        var effectObject = new GameObject($"[Voxel Impact Dust {index}]");
        UnityEngine.Object.DontDestroyOnLoad(effectObject);

        ParticleSystem particles = effectObject.AddComponent<ParticleSystem>();
        ParticleSystemRenderer renderer = effectObject.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sharedMaterial = GetDustMaterial();

        var main = particles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Disable;
        main.maxParticles = 192;

        var emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;

        var colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.38f, 0.35f, 0.31f), 0f),
                new GradientColorKey(new Color(0.22f, 0.22f, 0.21f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.72f, 0f),
                new GradientAlphaKey(0.84f, 0.05f),
                new GradientAlphaKey(0.62f, 0.72f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        var sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(
                new Keyframe(0f, 0.85f),
                new Keyframe(0.18f, 1f),
                new Keyframe(1f, 1.35f)));

        var noise = particles.noise;
        noise.enabled = true;
        noise.quality = ParticleSystemNoiseQuality.Low;
        noise.frequency = 0.35f;
        noise.scrollSpeed = 0.25f;

        effectObject.SetActive(false);
        return particles;
    }

    private static void ConfigureBurst(ParticleSystem particles, float radius)
    {
        float scale = Mathf.Clamp(radius * 0.45f, 0.45f, 2.7f);
        var main = particles.main;
        // Keep the impact concealed while large destruction requests finish
        // rebuilding their meshes. Longer-lived, slower particles remain close
        // to the damaged area without increasing the number of particles.
        main.duration = 4.25f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3.25f, 4.75f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(scale * 0.03f, scale * 0.18f);
        main.startSize = new ParticleSystem.MinMaxCurve(scale * 0.45f, scale * 1.1f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);

        var shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        // Spawn throughout the destruction volume immediately. Previously the
        // smoke started in a small central sphere and only covered the damaged
        // area after expanding, exposing the progressive mesh rebuild.
        shape.radius = Mathf.Max(0.35f, radius * 0.9f);
        shape.radiusThickness = 1f;

        var emission = particles.emission;
        short particleCount = (short)Mathf.Clamp(Mathf.RoundToInt(60f + radius * 10f), 65, 120);
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, particleCount) });

        var velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.y = new ParticleSystem.MinMaxCurve(scale * 0.08f, scale * 0.32f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var noise = particles.noise;
        noise.strength = new ParticleSystem.MinMaxCurve(scale * 0.25f, scale * 0.6f);
    }

    private static Material GetDustMaterial()
    {
        if (dustMaterial != null)
            return dustMaterial;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
            return null;

        dustMaterial = new Material(shader)
        {
            name = "Runtime Voxel Dust",
            hideFlags = HideFlags.HideAndDontSave,
            renderQueue = 3000
        };

        if (dustMaterial.HasProperty("_Surface"))
            dustMaterial.SetFloat("_Surface", 1f);
        if (dustMaterial.HasProperty("_ZWrite"))
            dustMaterial.SetFloat("_ZWrite", 0f);
        if (dustMaterial.HasProperty("_SrcBlend"))
            dustMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (dustMaterial.HasProperty("_DstBlend"))
            dustMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        dustMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        return dustMaterial;
    }

    private static bool IsFinite(Vector3 value)
    {
        return !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
               !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
               !float.IsNaN(value.z) && !float.IsInfinity(value.z);
    }
}
