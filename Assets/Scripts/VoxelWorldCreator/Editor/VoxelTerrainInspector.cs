using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VoxelTerrain))]
public sealed class VoxelTerrainInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var terrain = (VoxelTerrain)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"{terrain.VoxelCount:N0} voxels • {terrain.ChunkCount:N0} chunks");
        if (GUILayout.Button("Abrir Voxel Studio", GUILayout.Height(32))) VoxelEditorWindow.Open(terrain);
        EditorGUILayout.HelpBox("Os voxels são salvos na cena/prefab. As malhas e os colisores são reconstruídos a partir desses dados. Use o Voxel Studio para editar e salvar cópias .asset.", MessageType.Info);
    }
}
