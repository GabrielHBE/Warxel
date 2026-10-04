using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class EmpireStateChunkBakeOneShot
{
    private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
    private static string RequestPath => Path.Combine(ProjectRoot, "Temp", "EmpireStateChunkBake.request");
    private static string ResultPath => Path.Combine(ProjectRoot, "Temp", "EmpireStateChunkBake.result");
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string SourceName = "Empire_State_Core_Central";

    static EmpireStateChunkBakeOneShot() => EditorApplication.delayCall += TryRun;

    private static void TryRun()
    {
        if (!File.Exists(RequestPath)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += TryRun;
            return;
        }

        try
        {
            EditorSceneManager.SaveOpenScenes();
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject existing = FindInScene(scene, SourceName + "_Chunked");
            GameObject source = FindInScene(scene, SourceName);
            if (source == null)
            {
                if (existing == null) throw new InvalidOperationException($"Could not find '{SourceName}' in {ScenePath}.");
                WriteResult(existing, "already-baked", 0);
                return;
            }

            int sourceMeshes = source.GetComponentsInChildren<MeshFilter>(true)
                .Count(filter => filter.sharedMesh != null && filter.GetComponent<MeshRenderer>() != null &&
                                 filter.GetComponent<VoxelFragmentedObj>() == null);
            GameObject baked = ChunkedVoxelBuildingBaker.BakeBuilding(
                source,
                piecesPerChunk: 64,
                chunkHealth: 150f,
                materializedFragmentsPerEvent: 24,
                physicalFragmentsPerEvent: 8,
                globalDynamicDebris: 96,
                globalVisibleDebris: 384,
                outputFolder: "Assets/Generated/Destruction",
                replaceSourceHierarchy: true,
                showFailureDialog: false);
            if (baked == null) throw new InvalidOperationException("The chunk baker returned no building. See Editor.log.");

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            WriteResult(baked, "success", sourceMeshes);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            File.WriteAllText(ResultPath, "failed\n" + exception);
        }
        finally
        {
            if (File.Exists(RequestPath)) File.Delete(RequestPath);
        }
    }

    private static GameObject FindInScene(Scene scene, string objectName)
    {
        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Where(candidate => candidate.name == objectName)
            .OrderByDescending(candidate => candidate.GetComponentsInChildren<MeshFilter>(true).Length)
            .Select(candidate => candidate.gameObject)
            .FirstOrDefault();
    }

    private static void WriteResult(GameObject baked, string status, int sourceMeshes)
    {
        File.WriteAllText(ResultPath,
            $"{status}\nsourceMeshes={sourceMeshes}\nchunks={baked.GetComponentsInChildren<VoxelDestructionChunk>(true).Length}\n" +
            $"networkObjects={baked.GetComponentsInChildren<FishNet.Object.NetworkObject>(true).Length}\n" +
            $"rigidbodies={baked.GetComponentsInChildren<Rigidbody>(true).Length}\nscene={ScenePath}\n");
    }
}
