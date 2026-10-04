using UnityEngine;
using UnityEngine.SceneManagement;
using VoxelDestructionPro.Settings;
using VoxelDestructionPro.VoxelObjects;

/// <summary>
/// Routes fragments created by the isolation pass through the same physics
/// budget used by fragments created directly by a destruction hit.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(DynamicVoxelObj))]
public sealed class VoxelIsolationFragmentPhysics : MonoBehaviour
{
    private DynamicVoxelObj voxel;

    private void Awake()
    {
        voxel = GetComponent<DynamicVoxelObj>();
        voxel.isolationOrigin = IsoSettings.IsolationOrigin.ComponentZNeg;
    }

    private void OnEnable()
    {
        if (voxel == null)
            voxel = GetComponent<DynamicVoxelObj>();

        voxel.onIsolationFragmentCreated -= OnIsolationFragmentCreated;
        voxel.onIsolationFragmentCreated += OnIsolationFragmentCreated;
    }

    private void OnDisable()
    {
        if (voxel != null)
            voxel.onIsolationFragmentCreated -= OnIsolationFragmentCreated;
    }

    private void OnIsolationFragmentCreated(GameObject fragment)
    {
        VoxelFragmentBudget.Register(fragment, voxel.dynamicSettings);
    }

}

internal static class NewYorkVoxelFragmentPhysicsBootstrap
{
    private const string BuildingName = "Predio Nova Iorque";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplyToInitiallyLoadedScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
            ApplyToScene(SceneManager.GetSceneAt(i));
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyToScene(scene);
    }

    private static void ApplyToScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform building = FindBuilding(roots[i].transform);
            if (building == null)
                continue;

            DynamicVoxelObj[] voxelObjects =
                building.GetComponentsInChildren<DynamicVoxelObj>(true);
            for (int j = 0; j < voxelObjects.Length; j++)
            {
                GameObject target = voxelObjects[j].gameObject;
                if (target.GetComponent<VoxelIsolationFragmentPhysics>() == null)
                    target.AddComponent<VoxelIsolationFragmentPhysics>();
            }
        }
    }

    private static Transform FindBuilding(Transform current)
    {
        if (current.name == BuildingName)
            return current;

        for (int i = 0; i < current.childCount; i++)
        {
            Transform result = FindBuilding(current.GetChild(i));
            if (result != null)
                return result;
        }

        return null;
    }
}
