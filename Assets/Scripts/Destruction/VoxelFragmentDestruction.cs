using FishNet.Object;

public class VoxelFragmentDestruction : VoxelDestruction
{
    private VoxelFragmentedObj[] voxelFragmentedObjs;
    private bool initialized;

    protected override void Start() => EnsureInitialized();

    private void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;
        base.Start();
        voxelFragmentedObjs = GetComponentsInChildren<VoxelFragmentedObj>(true);
        isDestroyed.OnChange += OnDestroyedChanged;
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        EnsureInitialized();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        ApplyVisualState();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        ApplyVisualState();
    }

    private void OnDestroy() => isDestroyed.OnChange -= OnDestroyedChanged;

    private void OnDestroyedChanged(bool previous, bool next, bool asServer)
    {
        if (!asServer && IsServerInitialized) return;
        ApplyVisualState();
    }

    private void ApplyVisualState()
    {
        EnsureInitialized();
        meshCollider.enabled = !IsDestroyed;
        meshRenderer.enabled = !IsDestroyed;
    }

    [Server]
    public override void Destroy()
    {
        if (!IsSpawned || IsDestroyed) return;
        EnsureInitialized();
        damageTaken.Value = damageToDestroy;
        isDestroyed.Value = true;
        ApplyVisualState();
        foreach (VoxelFragmentedObj fragment in voxelFragmentedObjs)
        {
            // Nested destructible roots own their own fragments.
            if (fragment != null && fragment.GetComponentInParent<VoxelFragmentDestruction>() == this)
                VoxelDestructionScheduler.Enqueue(fragment);
        }
        NotifyDestroyedOnServer();
    }

    [Server]
    public void ResetFragments()
    {
        EnsureInitialized();
        InvalidatePendingDestruction();
        foreach (VoxelFragmentedObj fragment in voxelFragmentedObjs)
        {
            if (fragment != null && fragment.GetComponentInParent<VoxelFragmentDestruction>() == this)
                fragment.ResetCollapse();
        }
        damageTaken.Value = 0;
        isDestroyed.Value = false;
        ApplyVisualState();
    }
}
