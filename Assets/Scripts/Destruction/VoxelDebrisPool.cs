using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared debris budget. Pooled fragments never receive networking components;
/// remote clients animate them cosmetically while the server only simulates the
/// limited subset that may affect gameplay.
/// </summary>
public sealed class VoxelDebrisPool : MonoBehaviour
{
    private static VoxelDebrisPool instance;
    private readonly List<VoxelDebrisPoolItem> active = new();
    private readonly Stack<VoxelDebrisPoolItem> inactive = new();
    private int dynamicBodyCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;

    public static void Spawn(
        string buildingId,
        int chunkId,
        VoxelFragmentDefinition definition,
        Vector3 position,
        Quaternion rotation,
        Vector3 scale,
        Vector3 velocity,
        Vector3 angularVelocity,
        bool usePhysics,
        bool visible,
        int layer,
        float lifetime,
        int maxDynamicBodies,
        int maxVisibleDebris,
        float settleSpeed,
        float settleDelay)
    {
        if (definition == null || definition.Mesh == null) return;
        EnsureInstance();
        instance.SpawnInternal(
            buildingId, chunkId, definition, position, rotation, scale,
            velocity, angularVelocity, usePhysics, visible, layer, lifetime,
            Mathf.Max(0, maxDynamicBodies), Mathf.Max(1, maxVisibleDebris),
            Mathf.Max(0f, settleSpeed), Mathf.Max(0f, settleDelay));
    }

    public static void ReleaseBuilding(string buildingId)
    {
        if (instance == null || string.IsNullOrEmpty(buildingId)) return;
        for (int i = instance.active.Count - 1; i >= 0; i--)
            if (instance.active[i].BuildingId == buildingId)
                instance.ReleaseAt(i);
    }

    private static void EnsureInstance()
    {
        if (instance != null) return;
        GameObject root = new GameObject(nameof(VoxelDebrisPool));
        instance = root.AddComponent<VoxelDebrisPool>();
        DontDestroyOnLoad(root);
    }

    private void SpawnInternal(
        string buildingId,
        int chunkId,
        VoxelFragmentDefinition definition,
        Vector3 position,
        Quaternion rotation,
        Vector3 scale,
        Vector3 velocity,
        Vector3 angularVelocity,
        bool usePhysics,
        bool visible,
        int layer,
        float lifetime,
        int maxDynamicBodies,
        int maxVisibleDebris,
        float settleSpeed,
        float settleDelay)
    {
        while (active.Count >= maxVisibleDebris) ReleaseAt(IndexOfOldest());

        if (usePhysics && maxDynamicBodies <= 0) usePhysics = false;
        if (usePhysics && dynamicBodyCount >= maxDynamicBodies)
        {
            int oldestDynamic = IndexOfOldestDynamic();
            if (oldestDynamic >= 0)
            {
                active[oldestDynamic].Settle();
                dynamicBodyCount--;
            }
        }

        VoxelDebrisPoolItem item = inactive.Count > 0 ? inactive.Pop() : CreateItem();
        item.Activate(
            buildingId, chunkId, definition, position, rotation, scale,
            velocity, angularVelocity, usePhysics, visible, layer,
            Time.time + Mathf.Max(0.25f, lifetime), settleSpeed, settleDelay);
        active.Add(item);
        if (usePhysics) dynamicBodyCount++;
    }

    private VoxelDebrisPoolItem CreateItem()
    {
        GameObject go = new GameObject("Pooled voxel debris");
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>();
        go.AddComponent<MeshRenderer>();
        go.AddComponent<BoxCollider>();
        go.AddComponent<Rigidbody>();
        go.SetActive(false);
        return go.AddComponent<VoxelDebrisPoolItem>();
    }

    private void Update()
    {
        float now = Time.time;
        float deltaTime = Time.deltaTime;
        for (int i = active.Count - 1; i >= 0; i--)
        {
            VoxelDebrisPoolItem item = active[i];
            if (now >= item.ExpiresAt)
            {
                ReleaseAt(i);
                continue;
            }

            bool wasDynamic = item.IsDynamic;
            item.Tick(deltaTime, now);
            if (wasDynamic && !item.IsDynamic) dynamicBodyCount--;
        }
    }

    private int IndexOfOldest()
    {
        int result = 0;
        float expiry = float.MaxValue;
        for (int i = 0; i < active.Count; i++)
        {
            if (active[i].ExpiresAt >= expiry) continue;
            expiry = active[i].ExpiresAt;
            result = i;
        }
        return result;
    }

    private int IndexOfOldestDynamic()
    {
        int result = -1;
        float expiry = float.MaxValue;
        for (int i = 0; i < active.Count; i++)
        {
            if (!active[i].IsDynamic || active[i].ExpiresAt >= expiry) continue;
            expiry = active[i].ExpiresAt;
            result = i;
        }
        return result;
    }

    private void ReleaseAt(int index)
    {
        if (index < 0 || index >= active.Count) return;
        VoxelDebrisPoolItem item = active[index];
        if (item.IsDynamic) dynamicBodyCount--;
        int last = active.Count - 1;
        active[index] = active[last];
        active.RemoveAt(last);
        item.Deactivate(transform);
        inactive.Push(item);
    }
}

public sealed class VoxelDebrisPoolItem : MonoBehaviour
{
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private BoxCollider boxCollider;
    private Rigidbody body;
    private Vector3 cosmeticVelocity;
    private Vector3 cosmeticAngularVelocity;
    private float settleSpeedSquared;
    private float settleAfter;
    private bool affectsGameplay;
    private bool simulateCosmetically;

    public string BuildingId { get; private set; }
    public float ExpiresAt { get; private set; }
    public bool IsDynamic { get; private set; }

    public void Activate(
        string buildingId,
        int chunkId,
        VoxelFragmentDefinition definition,
        Vector3 position,
        Quaternion rotation,
        Vector3 scale,
        Vector3 velocity,
        Vector3 angularVelocity,
        bool usePhysics,
        bool visible,
        int layer,
        float expiresAt,
        float settleSpeed,
        float settleDelay)
    {
        CacheComponents();
        BuildingId = buildingId;
        ExpiresAt = expiresAt;
        settleSpeedSquared = settleSpeed * settleSpeed;
        settleAfter = Time.time + settleDelay;
        IsDynamic = usePhysics;
        affectsGameplay = usePhysics;
        simulateCosmetically = !usePhysics && visible;
        cosmeticVelocity = velocity;
        cosmeticAngularVelocity = angularVelocity;

        gameObject.name = $"Debris {buildingId}:{chunkId}";
        gameObject.layer = layer;
        transform.SetParent(null, false);
        transform.SetPositionAndRotation(position, rotation);
        transform.localScale = scale;

        meshFilter.sharedMesh = definition.Mesh;
        meshRenderer.sharedMaterials = definition.Materials;
        meshRenderer.enabled = visible;
        boxCollider.center = definition.Mesh.bounds.center;
        boxCollider.size = definition.Mesh.bounds.size;
        boxCollider.enabled = usePhysics;

        body.isKinematic = !usePhysics;
        body.useGravity = usePhysics;
        body.mass = Mathf.Clamp(definition.Volume, 0.1f, 100f);
        body.linearDamping = 0.05f;
        body.angularDamping = 0.1f;
        body.solverIterations = 4;
        gameObject.SetActive(true);
        if (usePhysics)
        {
            body.linearVelocity = velocity;
            body.angularVelocity = angularVelocity;
        }
    }

    public void Tick(float deltaTime, float now)
    {
        if (!IsDynamic)
        {
            if (!simulateCosmetically) return;
            cosmeticVelocity += Physics.gravity * deltaTime;
            transform.position += cosmeticVelocity * deltaTime;
            transform.rotation = Quaternion.Euler(cosmeticAngularVelocity * Mathf.Rad2Deg * deltaTime) * transform.rotation;
            return;
        }

        if (now < settleAfter) return;
        if (!body.IsSleeping() &&
            (body.linearVelocity.sqrMagnitude > settleSpeedSquared ||
             body.angularVelocity.sqrMagnitude > settleSpeedSquared)) return;
        Settle();
    }

    public void Settle()
    {
        if (!IsDynamic) return;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.isKinematic = true;
        boxCollider.enabled = false;
        IsDynamic = false;
        affectsGameplay = false;
        simulateCosmetically = false;
        cosmeticVelocity = Vector3.zero;
        cosmeticAngularVelocity = Vector3.zero;
    }

    public void Deactivate(Transform poolRoot)
    {
        CacheComponents();
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.isKinematic = true;
        boxCollider.enabled = false;
        meshRenderer.enabled = false;
        meshRenderer.sharedMaterials = System.Array.Empty<Material>();
        meshFilter.sharedMesh = null;
        BuildingId = null;
        affectsGameplay = false;
        simulateCosmetically = false;
        IsDynamic = false;
        gameObject.SetActive(false);
        transform.SetParent(poolRoot, false);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!affectsGameplay) return;
        float speed = body.linearVelocity.magnitude;
        if (collision.gameObject.layer == LayerMask.NameToLayer("Vehicle"))
            ProcessHit.VehicleHit(collision.gameObject, speed, gameObject);
        else if (collision.gameObject.layer == LayerMask.NameToLayer("PlayerHitBox"))
            ProcessHit.PlayerHit(collision.gameObject, speed, 1f, gameObject);
    }

    private void CacheComponents()
    {
        if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        if (boxCollider == null) boxCollider = GetComponent<BoxCollider>();
        if (body == null) body = GetComponent<Rigidbody>();
    }
}
