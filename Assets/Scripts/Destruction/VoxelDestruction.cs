using FishNet.Object;
using FishNet.Object.Synchronizing;

public abstract class VoxelDestruction : VoxelObj, IDamageable
{
    public float damageToDestroy = 150;
    protected readonly SyncVar<float> damageTaken = new SyncVar<float>();
    protected readonly SyncVar<bool> isDestroyed = new SyncVar<bool>(false);

    public virtual void TakeDamage(float damage)
    {
        // Skip rubble RPCs and apply server-side chain damage directly.
        if (NetworkObject == null || !IsSpawned || isDestroyed.Value) return;
        if (IsServerInitialized) ApplyDamageOnServer(damage);
        else if (IsClientInitialized) RequestDamageServerRpc(damage);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestDamageServerRpc(float damage) => ApplyDamageOnServer(damage);

    [Server]
    public void ApplyDamageOnServer(float damage)
    {
        if (damage <= 0f || float.IsNaN(damage) || float.IsInfinity(damage)) return;
        if (isDestroyed.Value) return;
        damageTaken.Value += damage;

        if (damageTaken.Value >= damageToDestroy) Destroy();
    }
    public abstract void Destroy();
}
