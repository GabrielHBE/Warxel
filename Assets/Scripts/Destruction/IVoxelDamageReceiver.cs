using UnityEngine;

/// <summary>
/// Common hit entry point for legacy voxel pieces and baked destruction chunks.
/// The hit position/radius let chunked buildings select debris without one
/// damage component per source fragment.
/// </summary>
public interface IVoxelDamageReceiver
{
    VoxelObj.VoxelMaterialType VoxelMaterial { get; }
    void TakeVoxelDamage(float damage, Vector3 hitPoint, float radius);
}

public static class VoxelDamageReceiverUtility
{
    public static bool TryGet(Collider collider, out IVoxelDamageReceiver receiver)
    {
        receiver = null;
        if (collider == null) return false;

        // Both supported receivers normally live beside their collider. The
        // parent fallback also supports compound colliders without allocations.
        VoxelDestruction destruction = collider.GetComponent<VoxelDestruction>();
        if (destruction != null)
        {
            receiver = destruction;
            return true;
        }

        VoxelDestructionChunk chunk = collider.GetComponent<VoxelDestructionChunk>();
        if (chunk == null) chunk = collider.GetComponentInParent<VoxelDestructionChunk>();
        if (chunk == null) return false;

        receiver = chunk;
        return true;
    }
}
