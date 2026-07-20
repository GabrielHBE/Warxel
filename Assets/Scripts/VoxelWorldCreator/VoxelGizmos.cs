using UnityEngine;

[ExecuteInEditMode]
public class VoxelGizmos : MonoBehaviour
{
    public bool showGizmos = true;
    public Color gizmoColor = Color.green;
    public float gizmoSize = 0.5f;
    
    VoxelTerrain terrain;
    
    void OnEnable()
    {
        terrain = GetComponent<VoxelTerrain>();
    }
    
    void OnDrawGizmos()
    {
        if (!showGizmos || terrain == null)
            return;
            
        Gizmos.color = gizmoColor;
        
        foreach (var voxel in terrain.voxels)
        {
            if (!voxel.isActive)
                continue;
                
            Vector3 pos = new Vector3(voxel.position.x, voxel.position.y, voxel.position.z) * terrain.voxelSize;
            Gizmos.DrawWireCube(pos, Vector3.one * terrain.voxelSize);
        }
    }
}