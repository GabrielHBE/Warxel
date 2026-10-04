using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Warxel/Voxel Map", fileName = "VoxelMap")]
public sealed class VoxelMapAsset : ScriptableObject
{
    [Min(0.01f)] public float voxelSize = 1;
    public Material material;
    [HideInInspector] public List<WorldVoxelData> voxels = new List<WorldVoxelData>();
}
