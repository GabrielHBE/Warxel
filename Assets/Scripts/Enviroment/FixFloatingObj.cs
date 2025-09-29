using Unity.VisualScripting;
using UnityEngine;
using VoxelDestructionPro.Tools;

public class FixFloatingObj : MonoBehaviour
{

    [SerializeField] private VoxCollider voxCollider;

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Voxel"))
        {
            voxCollider.Collide(collision);
        }
    }


}
