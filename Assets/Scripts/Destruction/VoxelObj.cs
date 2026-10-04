using FishNet.Object;
using UnityEngine;

[RequireComponent(typeof(Rigidbody)), RequireComponent(typeof(MeshCollider)), RequireComponent(typeof(MeshFilter)), RequireComponent(typeof(MeshRenderer))]
public class VoxelObj : NetworkBehaviour
{
    public static bool IsVoxelLayer(int objectLayer) =>
        objectLayer == LayerMask.NameToLayer("Voxel") ||
        objectLayer == LayerMask.NameToLayer("VoxelDebris");

    public VoxelMaterialType voxelMaterialType;
    protected LayerMask layer => LayerMask.NameToLayer("Voxel");

    protected Rigidbody rb;
    protected MeshCollider meshCollider;
    protected MeshFilter meshFilter;
    protected MeshRenderer meshRenderer;
    private bool componentsInitialized;

    protected virtual void Start()
    {
        if (componentsInitialized) return;
        componentsInitialized = true;
        if (layer.value >= 0) gameObject.layer = layer;
        GetComponents();

        rb.isKinematic = true;
    }

    protected void GetComponents()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        foreach (MeshCollider candidate in GetComponents<MeshCollider>())
        {
            if (!candidate.convex) { meshCollider = candidate; break; }
        }
        if (meshCollider == null)
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.sharedMesh;
        }
    }

    protected void ApplyRandomTorque()
    {
        Vector3 randomTorque = new Vector3(
            Random.Range(-10f, 10f),
            Random.Range(-10f, 10f),
            Random.Range(-10f, 10f)
        );
        rb.AddTorque(randomTorque * rb.mass, ForceMode.Impulse);
    }

    public enum VoxelMaterialType
    {
        Glass,
        Metal,
        Wood,
        Concrete,
        Sand,
        Dirt,
        SoftBody
    }
}
