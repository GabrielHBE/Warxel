using System.Collections.Generic;
using UnityEngine;

/// <summary>Bounds cascade/fragment activation work per physics step across all buildings.</summary>
public sealed class VoxelDestructionScheduler : MonoBehaviour
{
    [Min(1)] [SerializeField] private int activationsPerPhysicsStep = 16;
    private static VoxelDestructionScheduler instance;
    private readonly Queue<(VoxelDestruction target, uint revision)> pending = new();
    private readonly HashSet<(VoxelDestruction target, uint revision)> queued = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(this); return; }
        instance = this;
    }

    internal static void Enqueue(VoxelDestruction target)
    {
        if (target == null || target.NetworkObject == null || !target.IsSpawned ||
            !target.IsServerInitialized || target.IsDestroyed) return;
        if (instance == null)
        {
            var scheduler = new GameObject(nameof(VoxelDestructionScheduler));
            instance = scheduler.AddComponent<VoxelDestructionScheduler>();
            DontDestroyOnLoad(scheduler);
        }
        var request = (target, target.DestructionRevision);
        if (instance.queued.Add(request)) instance.pending.Enqueue(request);
    }

    private void FixedUpdate()
    {
        int budget = Mathf.Max(1, activationsPerPhysicsStep);
        while (budget-- > 0 && pending.Count > 0)
        {
            var request = pending.Dequeue();
            queued.Remove(request);
            var target = request.target;
            if (target != null && target.IsSpawned && target.IsServerInitialized &&
                !target.IsDestroyed && target.DestructionRevision == request.revision)
                target.Destroy();
        }
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
