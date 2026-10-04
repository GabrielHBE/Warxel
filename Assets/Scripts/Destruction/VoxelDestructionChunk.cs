using UnityEngine;

/// <summary>
/// Lightweight, static hit surface for one baked section of a building.
/// It intentionally has no Rigidbody, NetworkObject, or NetworkTransform.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider))]
public sealed class VoxelDestructionChunk : MonoBehaviour, IVoxelDamageReceiver
{
    [SerializeField] private int chunkId;
    [SerializeField] private VoxelDestructionChunkAsset data;
    [SerializeField] private VoxelObj.VoxelMaterialType materialType = VoxelObj.VoxelMaterialType.Concrete;

    private ChunkedVoxelBuilding building;
    private MeshRenderer meshRenderer;
    private BoxCollider hitCollider;

    public int ChunkId => chunkId;
    public VoxelDestructionChunkAsset Data => data;
    public VoxelObj.VoxelMaterialType VoxelMaterial => materialType;

    private void Awake()
    {
        CacheComponents();
        if (building == null) building = GetComponentInParent<ChunkedVoxelBuilding>();
    }

    public void Configure(
        ChunkedVoxelBuilding owner,
        int id,
        VoxelDestructionChunkAsset chunkData,
        VoxelObj.VoxelMaterialType voxelMaterial)
    {
        building = owner;
        chunkId = id;
        data = chunkData;
        materialType = voxelMaterial;
        CacheComponents();
    }

    public void Bind(ChunkedVoxelBuilding owner)
    {
        building = owner;
        CacheComponents();
    }

    public void TakeVoxelDamage(float damage, Vector3 hitPoint, float radius)
    {
        if (building == null) building = GetComponentInParent<ChunkedVoxelBuilding>();
        building?.TakeChunkDamage(chunkId, damage, hitPoint, radius);
    }

    public void SetDestroyed(bool destroyed)
    {
        CacheComponents();
        if (meshRenderer != null) meshRenderer.enabled = !destroyed;
        if (hitCollider != null) hitCollider.enabled = !destroyed;
    }

    private void CacheComponents()
    {
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        if (hitCollider == null) hitCollider = GetComponent<BoxCollider>();
    }
}
