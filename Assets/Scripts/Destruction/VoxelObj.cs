using FishNet.Object;
using UnityEngine;
using System.Linq;

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

    protected virtual void Start()
    {
        gameObject.layer = layer;
        GetComponents();

        meshCollider.convex = false;
        rb.isKinematic = true;
    }

    protected void GetComponents()
    {
        meshCollider = GetComponents<MeshCollider>().FirstOrDefault(collider => !collider.convex);
        rb = GetComponent<Rigidbody>();
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
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
