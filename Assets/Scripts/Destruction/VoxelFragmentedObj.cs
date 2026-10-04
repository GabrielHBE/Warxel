using FishNet.Object;
using UnityEngine;

/// <summary>A pre-fractured piece. Keep its GameObject active so FishNet can synchronize it.</summary>
public class VoxelFragmentedObj : VoxelPartialCollapse
{
    protected override void Start()
    {
        base.Start();
        ApplyCollapseState();
    }

    protected override void ApplyCollapseState()
    {
        base.ApplyCollapseState();
        // Hide components, never the NetworkObject itself. SyncVars restore late observers.
        meshRenderer.enabled = IsDestroyed;
        meshCollider.enabled = false;
    }

    [Server]
    public void Activate() => Destroy();

    // Hidden fragments are activated by their parent, not by bullets/explosions.
    public override void TakeDamage(float damage) { }

    // Preserve the original fragment behavior: collide physically without
    // invoking the whole-piece player/vehicle damage pipeline per fragment.
    protected override void OnCollisionEnter(Collision collision) { }
}
