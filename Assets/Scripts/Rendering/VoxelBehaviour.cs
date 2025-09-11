using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using UnityEngine;

public class VoxelBehaviour : MonoBehaviour
{
    [SerializeField] private GreedyMeshing greedyMeshing;

    private List<GameObject> objects = new List<GameObject>();
    private MeshRenderer mesh;

    void Start()
    {
        ApplyNotRender();
    }
    float distance = 5f;

    void ApplyNotRender()
    {
        greedyMeshing = GetComponent<GreedyMeshing>();

        foreach (Transform child in transform)
        {
            Vector3 origin = child.position;

            bool hitUp = Physics.Raycast(origin, Vector3.up, distance);
            bool hitDown = Physics.Raycast(origin, Vector3.down, distance);
            bool hitRight = Physics.Raycast(origin, Vector3.right, distance);
            bool hitLeft = Physics.Raycast(origin, Vector3.left, distance);
            bool hitForward = Physics.Raycast(origin, Vector3.forward, distance);
            bool hitBack = Physics.Raycast(origin, Vector3.back, distance);

            // Se estiver cercado em todas as direções → desativa
            if (hitUp && hitDown && hitRight && hitLeft && hitForward && hitBack)
            {
                mesh = child.GetComponent<MeshRenderer>();
                mesh.enabled = false;
            }

        }

        if (greedyMeshing != null)
        {
            greedyMeshing.ApplyGreedyMeshing();
        }


    }

    public void DestroyVoxel(GameObject gameObject)
    {
        Vector3 origin = gameObject.transform.position;

        Vector3[] directions = {
        Vector3.up, Vector3.down,
        Vector3.right, Vector3.left,
        Vector3.forward, Vector3.back
        };

        foreach (var dir in directions)
        {
            if (Physics.Raycast(origin, dir, out RaycastHit hit, distance))
            {
                MeshRenderer mr = hit.collider.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.enabled = true;
                }
            }
        }

        Destroy(gameObject);

        if (greedyMeshing != null)
        {
            greedyMeshing.RebuildFromBackup();
            greedyMeshing.ApplyGreedyMeshing();
        }
    }
}
