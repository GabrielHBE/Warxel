using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Warxel/Destruction/Chunk Data", fileName = "VoxelChunk")]
public sealed class VoxelDestructionChunkAsset : ScriptableObject
{
    [SerializeField] private Mesh intactMesh;
    [SerializeField] private Material[] intactMaterials = Array.Empty<Material>();
    [SerializeField] private VoxelFragmentDefinition[] fragments = Array.Empty<VoxelFragmentDefinition>();

    public Mesh IntactMesh => intactMesh;
    public Material[] IntactMaterials => intactMaterials;
    public VoxelFragmentDefinition[] Fragments => fragments;

    public void Initialize(Mesh mesh, Material[] materials, VoxelFragmentDefinition[] definitions)
    {
        intactMesh = mesh;
        intactMaterials = materials ?? Array.Empty<Material>();
        fragments = definitions ?? Array.Empty<VoxelFragmentDefinition>();
    }
}

[Serializable]
public sealed class VoxelFragmentDefinition
{
    [SerializeField] private Mesh mesh;
    [SerializeField] private Material[] materials = Array.Empty<Material>();
    [SerializeField] private Matrix4x4 localMatrix = Matrix4x4.identity;
    [SerializeField] private Vector3 localCenter;
    [SerializeField] private float volume;
    [SerializeField] private VoxelObj.VoxelMaterialType materialType;

    public Mesh Mesh => mesh;
    public Material[] Materials => materials;
    public Matrix4x4 LocalMatrix => localMatrix;
    public Vector3 LocalCenter => localCenter;
    public float Volume => volume;
    public VoxelObj.VoxelMaterialType MaterialType => materialType;

    public VoxelFragmentDefinition(
        Mesh sourceMesh,
        Material[] sourceMaterials,
        Matrix4x4 matrix,
        Vector3 center,
        float sourceVolume,
        VoxelObj.VoxelMaterialType sourceMaterialType)
    {
        mesh = sourceMesh;
        materials = sourceMaterials ?? Array.Empty<Material>();
        localMatrix = matrix;
        localCenter = center;
        volume = sourceVolume;
        materialType = sourceMaterialType;
    }
}
