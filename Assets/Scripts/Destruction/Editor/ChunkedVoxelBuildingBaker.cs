using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FishNet.Object;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class ChunkedVoxelBuildingBaker : EditorWindow
{
    [SerializeField] private int piecesPerChunk = 64;
    [SerializeField] private float chunkHealth = 150f;
    [SerializeField] private int materializedFragmentsPerEvent = 24;
    [SerializeField] private int physicalFragmentsPerEvent = 8;
    [SerializeField] private int globalDynamicDebris = 96;
    [SerializeField] private int globalVisibleDebris = 384;
    [SerializeField] private string outputFolder = "Assets/Generated/Destruction";
    [SerializeField] private bool replaceSourceHierarchy = true;
    private Vector2 scroll;

    private sealed class SourcePiece
    {
        public Mesh Mesh;
        public Material[] Materials;
        public Matrix4x4 LocalMatrix;
        public Vector3 LocalCenter;
        public float Volume;
        public VoxelObj.VoxelMaterialType MaterialType;
    }

    private sealed class MaterialGroup
    {
        public Material Material;
        public readonly List<CombineInstance> Instances = new();
    }

    [MenuItem("Tools/Warxel/Destruction/Bake selected building into chunks")]
    private static void Open()
    {
        ChunkedVoxelBuildingBaker window = GetWindow<ChunkedVoxelBuildingBaker>("Bake destruction chunks");
        window.minSize = new Vector2(470f, 390f);
        window.Show();
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.LabelField("Hybrid chunk destruction", EditorStyles.boldLabel);
        GameObject selected = Selection.activeGameObject;
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.ObjectField("Building root", selected, typeof(GameObject), true);

        int sourceCount = selected == null ? 0 : CollectMeshFilters(selected).Count;
        int estimatedChunks = sourceCount == 0 ? 0 : Mathf.CeilToInt(sourceCount / (float)Mathf.Max(1, piecesPerChunk));
        EditorGUILayout.HelpBox(
            $"Source pieces: {sourceCount:N0}\nEstimated chunks: {estimatedChunks:N0}\n" +
            "The baked building uses one NetworkObject and one static BoxCollider per chunk.",
            MessageType.Info);

        piecesPerChunk = EditorGUILayout.IntSlider("Pieces per chunk", piecesPerChunk, 16, 256);
        chunkHealth = EditorGUILayout.FloatField("Chunk health", chunkHealth);
        materializedFragmentsPerEvent = EditorGUILayout.IntSlider("Visible fragments/event", materializedFragmentsPerEvent, 1, 128);
        physicalFragmentsPerEvent = EditorGUILayout.IntSlider("Physical fragments/event", physicalFragmentsPerEvent, 0, 32);
        globalDynamicDebris = EditorGUILayout.IntField("Global dynamic budget", globalDynamicDebris);
        globalVisibleDebris = EditorGUILayout.IntField("Global visible budget", globalVisibleDebris);
        outputFolder = EditorGUILayout.TextField("Generated asset folder", outputFolder);
        replaceSourceHierarchy = EditorGUILayout.Toggle("Replace source hierarchy", replaceSourceHierarchy);

        if (replaceSourceHierarchy)
            EditorGUILayout.HelpBox(
                "The source root is removed from the scene after a successful bake. Save or duplicate the scene first if you want a backup. The operation supports scene Undo; generated assets remain in the output folder.",
                MessageType.Warning);
        else
            EditorGUILayout.HelpBox(
                "The source hierarchy will be disabled instead of removed. This is safer for comparison, but retains its serialized memory cost.",
                MessageType.Info);

        string error = Validate(selected);
        if (error != null) EditorGUILayout.HelpBox(error, MessageType.Error);
        using (new EditorGUI.DisabledScope(error != null))
        {
            if (GUILayout.Button("Bake selected building", GUILayout.Height(34)) && ConfirmBake(selected, sourceCount, estimatedChunks))
                BakeSelected(selected);
        }
        EditorGUILayout.EndScrollView();
    }

    private string Validate(GameObject root)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return "Exit Play Mode before baking.";
        if (root == null) return "Select the root of one building in the Hierarchy.";
        if (EditorUtility.IsPersistent(root) || !root.scene.IsValid())
            return "Bake a scene instance, not an asset in the Project window.";
        if (root.GetComponentInParent<ChunkedVoxelBuilding>() != null || root.GetComponent<ChunkedVoxelBuilding>() != null)
            return "This hierarchy is already a chunked building.";
        if (CollectMeshFilters(root).Count == 0) return "No usable MeshFilter/MeshRenderer pairs were found.";
        if (piecesPerChunk < 1) return "Pieces per chunk must be greater than zero.";
        if (chunkHealth <= 0f || float.IsNaN(chunkHealth) || float.IsInfinity(chunkHealth))
            return "Chunk health must be a finite value greater than zero.";
        if (!outputFolder.StartsWith("Assets", StringComparison.Ordinal) || outputFolder.Contains(".."))
            return "The generated folder must be inside Assets.";
        return null;
    }

    private bool ConfirmBake(GameObject root, int sourceCount, int chunkCount)
    {
        string action = replaceSourceHierarchy ? "replace" : "disable";
        return EditorUtility.DisplayDialog(
            "Bake hybrid destruction",
            $"Bake {sourceCount:N0} mesh pieces into approximately {chunkCount:N0} chunks and {action} '{root.name}'?",
            "Bake", "Cancel");
    }

    private void BakeSelected(GameObject sourceRoot)
    {
        BakeBuilding(
            sourceRoot, piecesPerChunk, chunkHealth,
            materializedFragmentsPerEvent, physicalFragmentsPerEvent,
            globalDynamicDebris, globalVisibleDebris,
            outputFolder, replaceSourceHierarchy);
    }

    public static GameObject BakeBuilding(
        GameObject sourceRoot,
        int piecesPerChunk,
        float chunkHealth,
        int materializedFragmentsPerEvent,
        int physicalFragmentsPerEvent,
        int globalDynamicDebris,
        int globalVisibleDebris,
        string outputFolder,
        bool replaceSourceHierarchy,
        bool showFailureDialog = true)
    {
        if (sourceRoot == null) throw new ArgumentNullException(nameof(sourceRoot));
        piecesPerChunk = Mathf.Max(1, piecesPerChunk);
        chunkHealth = Mathf.Max(1f, chunkHealth);

        EnsureAssetFolder(outputFolder);
        var temporarilyReadableImporters = new List<string>();
        GameObject bakedRoot = null;
        var createdAssets = new List<string>();
        int undoGroup = -1;

        try
        {
            temporarilyReadableImporters = MakeSourceMeshesReadable(sourceRoot);
            List<SourcePiece> pieces = CollectSourcePieces(sourceRoot);
            pieces.Sort(CompareSpatially);
            int chunkCount = Mathf.CeilToInt(pieces.Count / (float)piecesPerChunk);
            Undo.IncrementCurrentGroup();
            undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Bake hybrid voxel building");

            bakedRoot = CreateBakedRoot(sourceRoot);
            Undo.AddComponent<NetworkObject>(bakedRoot);
            ChunkedVoxelBuilding building = Undo.AddComponent<ChunkedVoxelBuilding>(bakedRoot);
            var chunks = new VoxelDestructionChunk[chunkCount];
            string safeRootName = SanitizeFileName(sourceRoot.name);
            string buildingId = Guid.NewGuid().ToString("N");

            for (int chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
            {
                int first = chunkIndex * piecesPerChunk;
                int count = Mathf.Min(piecesPerChunk, pieces.Count - first);
                List<SourcePiece> group = pieces.GetRange(first, count);
                Mesh combinedMesh = Combine(group, $"{safeRootName}_Chunk_{chunkIndex:000}");
                Material[] combinedMaterials = GetCombinedMaterials(group);
                VoxelFragmentDefinition[] definitions = group.Select(ToDefinition).ToArray();
                VoxelObj.VoxelMaterialType materialType = DominantMaterialType(group);

                VoxelDestructionChunkAsset data = CreateInstance<VoxelDestructionChunkAsset>();
                string assetPath = AssetDatabase.GenerateUniqueAssetPath(
                    $"{outputFolder}/{safeRootName}_Chunk_{chunkIndex:000}.asset");
                AssetDatabase.CreateAsset(data, assetPath);
                createdAssets.Add(assetPath);
                AssetDatabase.AddObjectToAsset(combinedMesh, data);
                data.Initialize(combinedMesh, combinedMaterials, definitions);
                EditorUtility.SetDirty(data);

                GameObject chunkObject = new GameObject($"Chunk {chunkIndex:000}");
                Undo.RegisterCreatedObjectUndo(chunkObject, "Create destruction chunk");
                chunkObject.layer = ResolveVoxelLayer(sourceRoot.layer);
                chunkObject.transform.SetParent(bakedRoot.transform, false);
                MeshFilter filter = Undo.AddComponent<MeshFilter>(chunkObject);
                MeshRenderer renderer = Undo.AddComponent<MeshRenderer>(chunkObject);
                BoxCollider collider = Undo.AddComponent<BoxCollider>(chunkObject);
                VoxelDestructionChunk chunk = Undo.AddComponent<VoxelDestructionChunk>(chunkObject);
                filter.sharedMesh = combinedMesh;
                renderer.sharedMaterials = combinedMaterials;
                collider.center = combinedMesh.bounds.center;
                collider.size = MaxSize(combinedMesh.bounds.size, 0.01f);
                chunk.Configure(building, chunkIndex, data, materialType);
                chunks[chunkIndex] = chunk;
            }

            building.Configure(
                buildingId, chunks, chunkHealth, materializedFragmentsPerEvent,
                physicalFragmentsPerEvent, globalDynamicDebris, globalVisibleDebris);
            EditorUtility.SetDirty(building);
            AssetDatabase.SaveAssets();

            if (replaceSourceHierarchy)
                Undo.DestroyObjectImmediate(sourceRoot);
            else
            {
                Undo.RecordObject(sourceRoot, "Disable source building");
                sourceRoot.SetActive(false);
            }

            Selection.activeGameObject = bakedRoot;
            EditorSceneManager.MarkSceneDirty(bakedRoot.scene);
            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log($"Baked '{safeRootName}': {pieces.Count:N0} source pieces -> {chunkCount:N0} chunks, one NetworkObject.", bakedRoot);
            return bakedRoot;
        }
        catch (Exception exception)
        {
            if (undoGroup >= 0) Undo.RevertAllDownToGroup(undoGroup);
            else if (bakedRoot != null) DestroyImmediate(bakedRoot);
            foreach (string asset in createdAssets) AssetDatabase.DeleteAsset(asset);
            Debug.LogException(exception);
            if (showFailureDialog)
                EditorUtility.DisplayDialog("Bake failed", "No source hierarchy was removed. See Console for details.", "OK");
            return null;
        }
        finally
        {
            RestoreSourceMeshReadability(temporarilyReadableImporters);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    private static GameObject CreateBakedRoot(GameObject source)
    {
        GameObject result = new GameObject(source.name + "_Chunked");
        Undo.RegisterCreatedObjectUndo(result, "Create chunked building");
        result.layer = source.layer;
        result.tag = source.tag;
        Transform sourceTransform = source.transform;
        Transform resultTransform = result.transform;
        resultTransform.SetParent(sourceTransform.parent, false);
        resultTransform.SetSiblingIndex(sourceTransform.GetSiblingIndex());
        resultTransform.localPosition = sourceTransform.localPosition;
        resultTransform.localRotation = sourceTransform.localRotation;
        resultTransform.localScale = sourceTransform.localScale;
        return result;
    }

    private static List<MeshFilter> CollectMeshFilters(GameObject root)
    {
        var result = new List<MeshFilter>();
        foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null || filter.GetComponent<MeshRenderer>() == null) continue;
            if (filter.GetComponent<VoxelFragmentedObj>() != null) continue;
            result.Add(filter);
        }
        return result;
    }

    private static List<SourcePiece> CollectSourcePieces(GameObject root)
    {
        Matrix4x4 worldToRoot = root.transform.worldToLocalMatrix;
        var result = new List<SourcePiece>();
        foreach (MeshFilter filter in CollectMeshFilters(root))
        {
            Mesh mesh = filter.sharedMesh;
            MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
            Matrix4x4 localMatrix = worldToRoot * filter.transform.localToWorldMatrix;
            Vector3 scale = localMatrix.lossyScale;
            Vector3 scaledSize = Vector3.Scale(mesh.bounds.size, Abs(scale));
            VoxelObj voxel = filter.GetComponent<VoxelObj>();
            result.Add(new SourcePiece
            {
                Mesh = mesh,
                Materials = renderer.sharedMaterials,
                LocalMatrix = localMatrix,
                LocalCenter = localMatrix.MultiplyPoint3x4(mesh.bounds.center),
                Volume = Mathf.Abs(scaledSize.x * scaledSize.y * scaledSize.z),
                MaterialType = voxel == null ? VoxelObj.VoxelMaterialType.Concrete : voxel.voxelMaterialType
            });
        }
        return result;
    }

    private static int CompareSpatially(SourcePiece left, SourcePiece right)
    {
        int y = left.LocalCenter.y.CompareTo(right.LocalCenter.y);
        if (y != 0) return y;
        int z = left.LocalCenter.z.CompareTo(right.LocalCenter.z);
        return z != 0 ? z : left.LocalCenter.x.CompareTo(right.LocalCenter.x);
    }

    private static Mesh Combine(List<SourcePiece> pieces, string name)
    {
        List<MaterialGroup> materialGroups = CreateMaterialGroups(pieces);
        if (materialGroups.Count == 0)
            throw new InvalidOperationException($"Chunk '{name}' has no renderable submeshes.");
        var intermediateMeshes = new List<Mesh>();
        var finalInstances = new CombineInstance[materialGroups.Count];
        try
        {
            for (int i = 0; i < materialGroups.Count; i++)
            {
                Mesh materialMesh = new Mesh
                {
                    name = name + "_Material_" + i,
                    indexFormat = IndexFormat.UInt32
                };
                materialMesh.CombineMeshes(materialGroups[i].Instances.ToArray(), true, true, false);
                intermediateMeshes.Add(materialMesh);
                finalInstances[i] = new CombineInstance
                {
                    mesh = materialMesh,
                    subMeshIndex = 0,
                    transform = Matrix4x4.identity
                };
            }

            Mesh combined = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            combined.CombineMeshes(finalInstances, false, false, false);
            combined.RecalculateBounds();
            return combined;
        }
        finally
        {
            foreach (Mesh mesh in intermediateMeshes) DestroyImmediate(mesh);
        }
    }

    private static List<MaterialGroup> CreateMaterialGroups(List<SourcePiece> pieces)
    {
        var groups = new List<MaterialGroup>();
        var groupByMaterial = new Dictionary<int, MaterialGroup>();
        foreach (SourcePiece piece in pieces)
        {
            int subMeshCount = piece.Mesh.subMeshCount;
            if (subMeshCount <= 0) continue;
            for (int subMesh = 0; subMesh < subMeshCount; subMesh++)
            {
                Material material = GetMaterial(piece.Materials, subMesh);
                int key = material == null ? 0 : material.GetInstanceID();
                if (!groupByMaterial.TryGetValue(key, out MaterialGroup group))
                {
                    group = new MaterialGroup { Material = material };
                    groupByMaterial.Add(key, group);
                    groups.Add(group);
                }
                group.Instances.Add(new CombineInstance
                {
                    mesh = piece.Mesh,
                    subMeshIndex = subMesh,
                    transform = piece.LocalMatrix
                });
            }
        }
        return groups;
    }

    private static Material[] GetCombinedMaterials(List<SourcePiece> pieces) =>
        CreateMaterialGroups(pieces).Select(group => group.Material).ToArray();

    private static Material GetMaterial(Material[] materials, int subMesh)
    {
        if (materials == null || materials.Length == 0) return null;
        return materials[Mathf.Min(subMesh, materials.Length - 1)];
    }

    private static VoxelFragmentDefinition ToDefinition(SourcePiece piece) =>
        new VoxelFragmentDefinition(
            piece.Mesh, piece.Materials, piece.LocalMatrix, piece.LocalCenter,
            piece.Volume, piece.MaterialType);

    private static VoxelObj.VoxelMaterialType DominantMaterialType(List<SourcePiece> pieces)
    {
        return pieces.GroupBy(piece => piece.MaterialType)
            .OrderByDescending(group => group.Count()).First().Key;
    }

    private static List<string> MakeSourceMeshesReadable(GameObject root)
    {
        var paths = new HashSet<string>();
        foreach (MeshFilter filter in CollectMeshFilters(root))
        {
            Mesh mesh = filter.sharedMesh;
            if (mesh == null || mesh.isReadable) continue;
            string path = AssetDatabase.GetAssetPath(mesh);
            if (!string.IsNullOrEmpty(path)) paths.Add(path);
        }

        var changed = new List<string>();
        foreach (string path in paths)
        {
            if (AssetImporter.GetAtPath(path) is not ModelImporter importer || importer.isReadable) continue;
            changed.Add(path);
            importer.isReadable = true;
            importer.SaveAndReimport();
        }
        return changed;
    }

    private static void RestoreSourceMeshReadability(List<string> paths)
    {
        foreach (string path in paths)
        {
            if (AssetImporter.GetAtPath(path) is not ModelImporter importer) continue;
            importer.isReadable = false;
            importer.SaveAndReimport();
        }
    }

    private static void EnsureAssetFolder(string folder)
    {
        string normalized = folder.Replace('\\', '/').TrimEnd('/');
        string[] parts = normalized.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static string SanitizeFileName(string value)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '_');
        return value.Replace(' ', '_');
    }

    private static int ResolveVoxelLayer(int fallback)
    {
        int voxel = LayerMask.NameToLayer("Voxel");
        return voxel < 0 ? fallback : voxel;
    }

    private static Vector3 Abs(Vector3 value) =>
        new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));

    private static Vector3 MaxSize(Vector3 value, float minimum) =>
        new Vector3(Mathf.Max(minimum, value.x), Mathf.Max(minimum, value.y), Mathf.Max(minimum, value.z));
}
