using FishNet.Object;
using FishNet.Object.Synchronizing;

public abstract class VoxelDestruction : VoxelObj
{
    public float damageToDestroy = 150;
    protected readonly SyncVar<float> damageTaken = new SyncVar<float>();
    protected readonly SyncVar<bool> isDestroyed = new SyncVar<bool>(false);

    [ServerRpc(RequireOwnership = false)]
    public virtual void Damage(float damage)
    {
        if (isDestroyed.Value) return;
        damageTaken.Value += damage;

        if (damageTaken.Value >= damageToDestroy) Destroy();
    }
    public abstract void Destroy();
}