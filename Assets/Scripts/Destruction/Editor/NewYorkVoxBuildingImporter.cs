using System;
using System.IO;
using System.Linq;
using FishNet.Object;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using VoxelDestructionPro.Data;
using VoxelDestructionPro.Settings;
using VoxelDestructionPro.VoxelObjects;
using VoxReader;
using VoxReader.Interfaces;

[InitializeOnLoad]
internal static class NewYorkVoxBuildingImporter
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string VoxRelativePath = "NYBuilds/Predio_Nova_Iorque";
    private const string RootName = "Predio Nova Iorque";
    private const float VoxelScale = 0.1f;
    private const float PlacementGap = 10f;

    private const string MaterialPath =
        "Assets/Atan Games/Voxel Destruction Pro/Materials/URP/VoxelModel.mat";
    private const string MeshSettingsPath =
        "Assets/Atan Games/Voxel Destruction Pro/Data/MeshSettings.asset";
    private const string IsolationSettingsPath =
        "Assets/Scripts/Destruction/NewYorkComponentIsolation.asset";
    private const string DynamicSettingsPath =
        "Assets/Atan Games/Voxel Destruction Pro/Data/Dynamic Settings/URP/SphereFragments.asset";

    private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
    private static string RequestPath => Path.Combine(ProjectRoot, "Temp", "NewYorkVoxBuilding.request");
    private static string ResultPath => Path.Combine(ProjectRoot, "Temp", "NewYorkVoxBuilding.result");
    private static string FixRequestPath => Path.Combine(ProjectRoot, "Temp", "NewYorkVoxBuildingFix.request");
    private static string FixResultPath => Path.Combine(ProjectRoot, "Temp", "NewYorkVoxBuildingFix.result");
    private static string PhysicsRequestPath => Path.Combine(ProjectRoot, "Temp", "NewYorkVoxPhysicsFix.request");
    private static string PhysicsResultPath => Path.Combine(ProjectRoot, "Temp", "NewYorkVoxPhysicsFix.result");

    static NewYorkVoxBuildingImporter() => EditorApplication.delayCall += TryRunRequestedAction;

    [MenuItem("Tools/Warxel/Destruction/Import Predio Nova Iorque")]
    public static void ImportFromMenu() => RunImport(showDialog: true);

    [MenuItem("Tools/Warxel/Destruction/Fix Predio Nova Iorque positions")]
    public static void FixPositionsFromMenu() => RunPositionFix(showDialog: true);

    [MenuItem("Tools/Warxel/Destruction/Fix Predio Nova Iorque fragment physics")]
    public static void FixFragmentPhysicsFromMenu() => RunFragmentPhysicsFix(showDialog: true);

    private static void TryRunRequestedAction()
    {
        if (!File.Exists(RequestPath) && !File.Exists(FixRequestPath) &&
            !File.Exists(PhysicsRequestPath)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += TryRunRequestedAction;
            return;
        }

        if (File.Exists(PhysicsRequestPath)) RunFragmentPhysicsFix(showDialog: false);
        else if (File.Exists(FixRequestPath)) RunPositionFix(showDialog: false);
        else RunImport(showDialog: false);
    }

    private static void RunImport(bool showDialog)
    {
        Scene scene = default;
        bool openedForImport = false;
        GameObject createdRoot = null;

        try
        {
            scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                openedForImport = true;
            }

            GameObject existing = FindInScene(scene, RootName);
            Bounds existingBounds = default;
            bool hadExistingBounds = existing != null && TryGetBounds(existing, out existingBounds);

            createdRoot = BuildBuilding(scene);

            // A rebuild is required when the voxel scale changes because the
            // generated meshes also contain that scale. Preserve the previous
            // visible placement while replacing all parts at the new scale.
            if (hadExistingBounds && TryGetBounds(createdRoot, out Bounds rebuiltBounds))
            {
                createdRoot.transform.position += new Vector3(
                    existingBounds.center.x - rebuiltBounds.center.x,
                    existingBounds.min.y - rebuiltBounds.min.y,
                    existingBounds.center.z - rebuiltBounds.center.z);
            }

            // Keep the old hierarchy intact until the replacement has been
            // generated successfully, so a failed import cannot remove it.
            if (existing != null)
                UnityEngine.Object.DestroyImmediate(existing);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"Could not save {ScenePath}.");

            Selection.activeGameObject = createdRoot;
            WriteResult("success", createdRoot);
            if (showDialog)
                EditorUtility.DisplayDialog(
                    "Predio Nova Iorque",
                    $"Importacao concluida em escala {VoxelScale}. O core estrutural foi mantido indestrutivel.",
                    "OK");
        }
        catch (Exception exception)
        {
            if (createdRoot != null) UnityEngine.Object.DestroyImmediate(createdRoot);
            Debug.LogException(exception);
            File.WriteAllText(ResultPath, "failed\n" + exception);
            if (showDialog)
                EditorUtility.DisplayDialog("Falha na importacao", exception.Message, "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            if (File.Exists(RequestPath)) File.Delete(RequestPath);
            if (openedForImport && scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static GameObject BuildBuilding(Scene scene)
    {
        string voxPath = Path.Combine(Application.streamingAssetsPath, VoxRelativePath + ".vox");
        if (!File.Exists(voxPath))
            throw new FileNotFoundException("New York VOX file was not found.", voxPath);

        IVoxFile file = VoxReader.VoxReader.Read(voxPath, false);
        IModel[] models = file.Models;
        if (models == null || models.Length == 0)
            throw new InvalidOperationException("The New York VOX file contains no models.");

        Material material = LoadRequiredAsset<Material>(MaterialPath);
        MeshSettingsObj meshSettings = LoadRequiredAsset<MeshSettingsObj>(MeshSettingsPath);
        IsoSettings isolationSettings = LoadRequiredAsset<IsoSettings>(IsolationSettingsPath);
        DynSettings dynamicSettings = LoadRequiredAsset<DynSettings>(DynamicSettingsPath);

        GameObject root = new GameObject(RootName);
        SceneManager.MoveGameObjectToScene(root, scene);

        int voxelLayer = LayerMask.NameToLayer("Voxel");
        if (voxelLayer < 0) voxelLayer = 0;

        for (int modelIndex = 0; modelIndex < models.Length; modelIndex++)
        {
            IModel model = models[modelIndex];
            bool isCore = IsCore(model.Name);
            EditorUtility.DisplayProgressBar(
                "Importando Predio Nova Iorque",
                $"{modelIndex + 1}/{models.Length} - {model.Name}",
                (modelIndex + 1f) / models.Length);

            GameObject part = new GameObject(string.IsNullOrWhiteSpace(model.Name)
                ? $"Model {modelIndex:000}"
                : model.Name);
            part.layer = voxelLayer;
            part.transform.SetParent(root.transform, false);
            part.transform.localPosition = VoxToUnity(model.GlobalPosition) * VoxelScale;
            part.transform.localRotation = ConvertRotation(model.GlobalRotation);

            GameObject meshObject = new GameObject("Voxel Mesh");
            meshObject.layer = voxelLayer;
            meshObject.transform.SetParent(part.transform, false);
            meshObject.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            MeshFilter filter = meshObject.AddComponent<MeshFilter>();
            MeshRenderer renderer = meshObject.AddComponent<MeshRenderer>();
            MeshCollider collider = meshObject.AddComponent<MeshCollider>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.TwoSided;
            collider.cookingOptions = MeshColliderCookingOptions.None;

            if (isCore)
            {
                // The core intentionally has no DynamicVoxelObj or damage receiver.
                // It remains visible and collidable, but no destruction path can target it.
                VoxelObjBase temporaryVoxel = part.AddComponent<VoxelObjBase>();
                ConfigureBase(temporaryVoxel, filter, collider, meshSettings);
                temporaryVoxel.AssignVoxelData(new VoxelData(model), true);
                AlignMeshToVoxPivot(meshObject.transform, model.LocalSize);
                UnityEngine.Object.DestroyImmediate(temporaryVoxel);
                continue;
            }

            part.AddComponent<NetworkObject>();
            DynamicVoxelObj voxel = part.AddComponent<DynamicVoxelObj>();
            part.AddComponent<VoxelIsolationFragmentPhysics>();
            ConfigureBase(voxel, filter, collider, meshSettings);
            voxel.isoSettings = isolationSettings;
            voxel.isolationOrigin = IsoSettings.IsolationOrigin.ComponentZNeg;
            voxel.dynamicSettings = dynamicSettings;

            NetworkVoxelDestruction networkDestruction = part.AddComponent<NetworkVoxelDestruction>();
            SerializedObject serializedNetwork = new SerializedObject(networkDestruction);
            serializedNetwork.FindProperty("voxel").objectReferenceValue = voxel;
            serializedNetwork.ApplyModifiedPropertiesWithoutUndo();

            SharedVoxFileDataProvider provider = part.AddComponent<SharedVoxFileDataProvider>();
            provider.modelPath = VoxRelativePath;
            provider.modelIndex = modelIndex;

            voxel.AssignVoxelData(new VoxelData(model), true);
            AlignMeshToVoxPivot(meshObject.transform, model.LocalSize);
        }

        PlaceBesideReference(root, scene);
        return root;
    }

    private static void RunFragmentPhysicsFix(bool showDialog)
    {
        Scene scene = default;
        bool openedForFix = false;

        try
        {
            scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                openedForFix = true;
            }

            GameObject root = FindInScene(scene, RootName);
            if (root == null)
                throw new InvalidOperationException($"'{RootName}' was not found in {ScenePath}.");

            DynamicVoxelObj[] voxelObjects = root.GetComponentsInChildren<DynamicVoxelObj>(true);
            int added = 0;
            int isolationUpdated = 0;
            IsoSettings componentIsolationSettings = LoadRequiredAsset<IsoSettings>(IsolationSettingsPath);
            for (int i = 0; i < voxelObjects.Length; i++)
            {
                DynamicVoxelObj voxel = voxelObjects[i];
                if (voxel.isoSettings != componentIsolationSettings ||
                    voxel.isolationOrigin != IsoSettings.IsolationOrigin.ComponentZNeg)
                {
                    Undo.RecordObject(voxel, "Configure New York component isolation");
                    voxel.isoSettings = componentIsolationSettings;
                    voxel.isolationOrigin = IsoSettings.IsolationOrigin.ComponentZNeg;
                    EditorUtility.SetDirty(voxel);
                    isolationUpdated++;
                }

                GameObject target = voxel.gameObject;
                if (target.GetComponent<VoxelIsolationFragmentPhysics>() != null)
                    continue;

                Undo.AddComponent<VoxelIsolationFragmentPhysics>(target);
                added++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"Could not save {ScenePath}.");

            Selection.activeGameObject = root;
            File.WriteAllText(PhysicsResultPath,
                $"success\nscene={ScenePath}\nroot={root.name}\n" +
                $"voxelObjects={voxelObjects.Length}\nhandlersAdded={added}\n" +
                $"isolationUpdated={isolationUpdated}\n" +
                $"handlersTotal={root.GetComponentsInChildren<VoxelIsolationFragmentPhysics>(true).Length}\n");

            if (showDialog)
                EditorUtility.DisplayDialog(
                    "Predio Nova Iorque",
                    $"Isolamento por componente configurado em {voxelObjects.Length} objetos voxel.",
                    "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            File.WriteAllText(PhysicsResultPath, "failed\n" + exception);
            if (showDialog)
                EditorUtility.DisplayDialog("Falha na correcao de fisica", exception.Message, "OK");
        }
        finally
        {
            if (File.Exists(PhysicsRequestPath)) File.Delete(PhysicsRequestPath);
            if (openedForFix && scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void ConfigureBase(
        VoxelObjBase voxel,
        MeshFilter filter,
        Collider collider,
        MeshSettingsObj meshSettings)
    {
        // Each model's transform in the VOX scene is relative to the center of
        // its declared SIZE chunk. VoxelObjBase's automatic pivot uses only the
        // occupied mesh bounds and writes that value before the -90 degree axis
        // conversion, which separates sparse building pieces from one another.
        voxel.setPivot = false;
        voxel.pivotPlacement = new Vector3(0.5f, 0.5f, 0.5f);
        voxel.scaleType = VoxelObjBase.ScaleType.Voxel;
        voxel.objectScale = VoxelScale;
        voxel.targetFilter = filter;
        voxel.targetCollider = collider;
        voxel.meshSettings = meshSettings;
        voxel.calculateVoxelCount = false;
    }

    private static void AlignMeshToVoxPivot(Transform meshTransform, Vector3 localSize)
    {
        // Voxel centers run from zero through size - 1. Center that interval,
        // then convert MagicaVoxel (X,Y,Z-up) to Unity (X,Y-up,Z).
        Vector3 voxelCenter = (localSize - Vector3.one) * (VoxelScale * 0.5f);
        meshTransform.localPosition = -VoxToUnity(voxelCenter);
        meshTransform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
    }

    private static void RunPositionFix(bool showDialog)
    {
        Scene scene = default;
        bool openedForFix = false;

        try
        {
            scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                openedForFix = true;
            }

            GameObject root = FindInScene(scene, RootName);
            if (root == null)
                throw new InvalidOperationException($"'{RootName}' was not found in {ScenePath}.");

            string voxPath = Path.Combine(Application.streamingAssetsPath, VoxRelativePath + ".vox");
            IVoxFile file = VoxReader.VoxReader.Read(voxPath, false);
            IModel[] models = file.Models;
            var modelByName = models
                .Where(model => !string.IsNullOrWhiteSpace(model.Name))
                .GroupBy(model => model.Name)
                .ToDictionary(group => group.Key, group => group.First());

            bool hadOldBounds = TryGetBounds(root, out Bounds oldBounds);
            int fixedParts = 0;
            int fixedCores = 0;

            for (int childIndex = 0; childIndex < root.transform.childCount; childIndex++)
            {
                Transform part = root.transform.GetChild(childIndex);
                SharedVoxFileDataProvider provider = part.GetComponent<SharedVoxFileDataProvider>();
                IModel model = null;

                if (provider != null && provider.modelIndex >= 0 && provider.modelIndex < models.Length)
                    model = models[provider.modelIndex];
                else
                    modelByName.TryGetValue(part.name, out model);

                if (model == null)
                {
                    Debug.LogWarning($"No VOX model found for '{part.name}'.", part);
                    continue;
                }

                EditorUtility.DisplayProgressBar(
                    "Corrigindo Predio Nova Iorque",
                    $"{childIndex + 1}/{root.transform.childCount} - {part.name}",
                    (childIndex + 1f) / root.transform.childCount);

                Undo.RecordObject(part, "Fix New York building positions");
                part.localPosition = VoxToUnity(model.GlobalPosition) * VoxelScale;
                part.localRotation = ConvertRotation(model.GlobalRotation);

                Transform meshTransform = part.Find("Voxel Mesh");
                if (meshTransform == null)
                {
                    Debug.LogWarning($"Voxel Mesh child not found for '{part.name}'.", part);
                    continue;
                }

                Undo.RecordObject(meshTransform, "Fix New York building pivot");
                AlignMeshToVoxPivot(meshTransform, model.LocalSize);

                DynamicVoxelObj voxel = part.GetComponent<DynamicVoxelObj>();
                if (voxel != null)
                {
                    Undo.RecordObject(voxel, "Disable occupied-bounds pivot");
                    voxel.setPivot = false;
                    EditorUtility.SetDirty(voxel);
                }
                else if (IsCore(part.name))
                {
                    fixedCores++;
                }

                EditorUtility.SetDirty(part);
                EditorUtility.SetDirty(meshTransform);
                fixedParts++;
            }

            // Keep the building at the same visible location and ground height
            // while correcting the relative positions of all of its pieces.
            if (hadOldBounds && TryGetBounds(root, out Bounds newBounds))
            {
                Undo.RecordObject(root.transform, "Preserve New York building placement");
                root.transform.position += new Vector3(
                    oldBounds.center.x - newBounds.center.x,
                    oldBounds.min.y - newBounds.min.y,
                    oldBounds.center.z - newBounds.center.z);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"Could not save {ScenePath}.");

            Selection.activeGameObject = root;
            File.WriteAllText(FixResultPath,
                $"success\nscene={ScenePath}\nroot={root.name}\nfixedParts={fixedParts}\n" +
                $"fixedCores={fixedCores}\nposition={root.transform.position}\n");

            if (showDialog)
                EditorUtility.DisplayDialog(
                    "Predio Nova Iorque",
                    $"Posicoes corrigidas para {fixedParts} partes do arquivo VOX.",
                    "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            File.WriteAllText(FixResultPath, "failed\n" + exception);
            if (showDialog)
                EditorUtility.DisplayDialog("Falha na correcao", exception.Message, "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            if (File.Exists(FixRequestPath)) File.Delete(FixRequestPath);
            if (openedForFix && scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void PlaceBesideReference(GameObject building, Scene scene)
    {
        GameObject reference = FindInScene(scene, "Predio Teste");
        if (reference == null) return;

        building.transform.position = reference.transform.position;
        if (!TryGetBounds(reference, out Bounds referenceBounds) ||
            !TryGetBounds(building, out Bounds buildingBounds))
            return;

        Vector3 offset = new Vector3(
            referenceBounds.max.x + PlacementGap - buildingBounds.min.x,
            referenceBounds.min.y - buildingBounds.min.y,
            referenceBounds.center.z - buildingBounds.center.z);
        building.transform.position += offset;
    }

    private static bool TryGetBounds(GameObject root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            bounds = default;
            return false;
        }

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return true;
    }

    private static bool IsCore(string modelName) =>
        !string.IsNullOrEmpty(modelName) &&
        modelName.StartsWith("CORE_Estrutura_", StringComparison.OrdinalIgnoreCase);

    private static Vector3 VoxToUnity(Vector3 value) =>
        new Vector3(value.x, value.z, -value.y);

    private static Quaternion ConvertRotation(Matrix3 rotation)
    {
        Vector3 up = VoxToUnity(rotation * new Vector3(0f, 0f, 1f));
        Vector3 forward = VoxToUnity(rotation * new Vector3(0f, -1f, 0f));
        return Quaternion.LookRotation(forward, up);
    }

    private static T LoadRequiredAsset<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) throw new InvalidOperationException($"Required asset not found: {path}");
        return asset;
    }

    private static GameObject FindInScene(Scene scene, string objectName)
    {
        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(candidate => candidate.name == objectName)
            ?.gameObject;
    }

    private static void WriteResult(string status, GameObject root)
    {
        int coreCount = root.GetComponentsInChildren<Transform>(true)
            .Count(item => IsCore(item.name));
        int destructibleCount = root.GetComponentsInChildren<DynamicVoxelObj>(true).Length;
        int networkCount = root.GetComponentsInChildren<NetworkObject>(true).Length;
        int meshCount = root.GetComponentsInChildren<MeshFilter>(true).Length;

        File.WriteAllText(ResultPath,
            $"{status}\nscene={ScenePath}\nroot={root.name}\nmeshes={meshCount}\n" +
            $"cores={coreCount}\ndestructibles={destructibleCount}\nnetworkObjects={networkCount}\n" +
            $"objectScale={VoxelScale}\nposition={root.transform.position}\n");
    }
}
