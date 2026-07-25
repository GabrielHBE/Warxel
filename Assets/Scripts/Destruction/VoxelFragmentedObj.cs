using System.Linq;
using FishNet.Component.Transforming;
using FishNet.Object;
using UnityEngine;

[RequireComponent(typeof(NetworkTransform))]
public class VoxelFragmentedObj : VoxelObj
{
    private MeshCollider physicsMeshCllider;

    protected override void Start()
    {
        base.Start();
        physicsMeshCllider = GetComponents<MeshCollider>().FirstOrDefault(collider => collider.convex);
        if(physicsMeshCllider == null)
        {
            physicsMeshCllider = gameObject.AddComponent<MeshCollider>();
            physicsMeshCllider.convex = true;
        }
        physicsMeshCllider.enabled = false;
        gameObject.SetActive(false);
    }

    public void Activate()
    {
        SetGameobjectActive();
        meshCollider.enabled = false;
        physicsMeshCllider.enabled = true;
        rb.isKinematic = false;
        ApplyRandomTorque();
    }

    [ObserversRpc]
    private void SetGameobjectActive() => gameObject.SetActive(true);    
}