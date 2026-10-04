using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using VoxelDestructionPro;
using VoxelDestructionPro.Settings;
using VoxelDestructionPro.VoxelObjects;

/// <summary>
/// Centralized lifecycle and physics budget for runtime voxel debris.
/// One manager updates every fragment, avoiding one lifetime script per object.
/// </summary>
[DefaultExecutionOrder(1000)]
public sealed class VoxelFragmentBudget : MonoBehaviour
{
    private const int MaximumMaintenanceChecksPerFrame = 64;
    private const int MaximumGlobalSpawnsPerFrame = 2;

    private sealed class FragmentEntry
    {
        public GameObject GameObject;
        public Rigidbody Rigidbody;
        public DynamicVoxelObj VoxelObject;
        public float SpawnTime;
        public float SleepStartTime = -1f;
        public float Lifetime;
        public float MinimumPhysicsTime;
        public float SleepTimeBeforeFreeze;
        public bool DisableCollisionAfterSleep;
        public bool DisableShadows;
        public bool ReleaseVoxelData;
        public bool RuntimeDataReleased;
    }

    private static VoxelFragmentBudget instance;
    private static bool applicationIsQuitting;
    private static int spawnBudgetFrame = -1;
    private static int spawnCountThisFrame;

    private readonly List<FragmentEntry> fragments = new List<FragmentEntry>();
    private int maintenanceCursor;

    public static int ActiveFragmentCount => instance == null ? 0 : instance.fragments.Count;

    public static bool TryAcquireSpawnSlot()
    {
        int frame = Time.frameCount;
        if (spawnBudgetFrame != frame)
        {
            spawnBudgetFrame = frame;
            spawnCountThisFrame = 0;
        }

        if (spawnCountThisFrame >= MaximumGlobalSpawnsPerFrame)
            return false;

        spawnCountThisFrame++;
        return true;
    }

    public static void Register(GameObject fragment, DynSettings settings)
    {
        if (fragment == null || settings == null || applicationIsQuitting)
            return;

        PrepareFragmentPhysics(fragment);

        VoxelFragmentBudget manager = GetOrCreateInstance();
        if (manager == null)
            return;

        manager.EnforceLimitBeforeAdding(settings.maxActiveFragments);
        manager.fragments.Add(new FragmentEntry
        {
            GameObject = fragment,
            Rigidbody = fragment.GetComponent<Rigidbody>(),
            VoxelObject = fragment.GetComponent<DynamicVoxelObj>(),
            SpawnTime = Time.unscaledTime,
            Lifetime = settings.fragmentLifetime,
            MinimumPhysicsTime = settings.minimumFragmentPhysicsTime,
            SleepTimeBeforeFreeze = settings.fragmentSleepTimeBeforeFreeze,
            DisableCollisionAfterSleep = settings.disableFragmentCollisionAfterSleep,
            DisableShadows = settings.disableFragmentShadows,
            ReleaseVoxelData = settings.releaseFragmentVoxelData
        });
    }

    private static void PrepareFragmentPhysics(GameObject fragment)
    {
        // Runtime fragments must not collide with one another. Dense decorative
        // models (window frames and ornaments in particular) can create several
        // overlapping convex bodies at once; PhysX then keeps depenetrating them,
        // which appears as rapid flickering/jittering. The project's VoxelDebris
        // layer still collides with the building and ground, but not with itself.
        int debrisLayer = LayerMask.NameToLayer("VoxelDebris");
        if (debrisLayer >= 0)
            SetLayerRecursively(fragment.transform, debrisLayer);

        Rigidbody body = fragment.GetComponent<Rigidbody>();
        if (body == null)
            return;

        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        body.linearDamping = Mathf.Max(body.linearDamping, 0.15f);
        body.angularDamping = Mathf.Max(body.angularDamping, 0.75f);
        body.maxAngularVelocity = Mathf.Min(body.maxAngularVelocity, 12f);
        body.maxDepenetrationVelocity = Mathf.Min(body.maxDepenetrationVelocity, 3f);
        body.sleepThreshold = Mathf.Max(body.sleepThreshold, 0.02f);
    }

    private static void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        for (int i = 0; i < root.childCount; i++)
            SetLayerRecursively(root.GetChild(i), layer);
    }

    private static VoxelFragmentBudget GetOrCreateInstance()
    {
        if (instance != null)
            return instance;

        GameObject managerObject = new GameObject("[Voxel Fragment Budget]");
        DontDestroyOnLoad(managerObject);
        instance = managerObject.AddComponent<VoxelFragmentBudget>();
        return instance;
    }

    private void Update()
    {
        float now = Time.unscaledTime;
        int checksRemaining = Mathf.Min(MaximumMaintenanceChecksPerFrame, fragments.Count);
        while (checksRemaining-- > 0 && fragments.Count > 0)
        {
            if (maintenanceCursor >= fragments.Count)
                maintenanceCursor = 0;

            FragmentEntry entry = fragments[maintenanceCursor];
            if (entry.GameObject == null)
            {
                RemoveEntryAt(maintenanceCursor);
                continue;
            }

            if (!entry.GameObject.activeInHierarchy)
            {
                GameObject inactiveFragment = entry.GameObject;
                RemoveEntryAt(maintenanceCursor);
                ReleaseFragmentObject(inactiveFragment);
                continue;
            }

            float age = now - entry.SpawnTime;
            if (entry.Lifetime > 0f && age >= entry.Lifetime)
            {
                RemoveFragmentAt(maintenanceCursor);
                continue;
            }

            TryReleaseRuntimeData(entry);
            TryFreezeSleepingPhysics(entry, age, now);
            maintenanceCursor++;
        }
    }

    private static void TryReleaseRuntimeData(FragmentEntry entry)
    {
        if (entry.RuntimeDataReleased || entry.VoxelObject == null)
            return;

        if (entry.ReleaseVoxelData && !entry.VoxelObject.TryReleaseRuntimeVoxelData())
            return;

        if (entry.DisableShadows)
        {
            Renderer[] renderers = entry.GameObject.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].shadowCastingMode = ShadowCastingMode.Off;
        }

        entry.RuntimeDataReleased = true;
    }

    private static void TryFreezeSleepingPhysics(FragmentEntry entry, float age, float now)
    {
        Rigidbody body = entry.Rigidbody;
        if (body == null || body.isKinematic || age < entry.MinimumPhysicsTime)
            return;

        if (!body.IsSleeping())
        {
            entry.SleepStartTime = -1f;
            return;
        }

        if (entry.SleepStartTime < 0f)
        {
            entry.SleepStartTime = now;
            return;
        }

        if (now - entry.SleepStartTime < entry.SleepTimeBeforeFreeze)
            return;

        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.isKinematic = true;
        if (entry.DisableCollisionAfterSleep)
            body.detectCollisions = false;
    }

    private void EnforceLimitBeforeAdding(int maximumActiveFragments)
    {
        if (maximumActiveFragments <= 0)
            return;

        while (fragments.Count >= maximumActiveFragments)
            RemoveFragmentAt(0);
    }

    private void RemoveFragmentAt(int index)
    {
        FragmentEntry entry = fragments[index];
        RemoveEntryAt(index);
        ReleaseFragmentObject(entry.GameObject);
    }

    private static void ReleaseFragmentObject(GameObject fragment)
    {
        if (fragment != null && !VoxelManager.TryReleasePooled(fragment))
            Destroy(fragment);
    }

    private void RemoveEntryAt(int index)
    {
        fragments.RemoveAt(index);
        if (index < maintenanceCursor)
            maintenanceCursor--;
        if (maintenanceCursor < 0 || maintenanceCursor >= fragments.Count)
            maintenanceCursor = 0;
    }

    private void OnApplicationQuit()
    {
        applicationIsQuitting = true;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
