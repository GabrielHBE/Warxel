using FishNet.Object;

public class VoxelFragmentDestruction : VoxelDestruction
{
    private VoxelFragmentedObj[] voxelFragmentedObjs;

    protected override void Start()
    {
        base.Start();
        voxelFragmentedObjs = GetComponentsInChildren<VoxelFragmentedObj>(true);
    }

    public override void Destroy()
    {
        foreach(VoxelFragmentedObj v in voxelFragmentedObjs)
        {
            v.Activate();
        }
        DestroyVisuals();

    }
    
    [ObserversRpc]
    private void DestroyVisuals()
    {
        meshCollider.enabled = false;
        meshFilter.mesh = null;
        meshRenderer.enabled = false;
    }

}