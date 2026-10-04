using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

/// <summary>Dependency-free regression suite, runnable from the menu or Unity -executeMethod.</summary>
public static class VoxelStudioValidation
{
    private static int assertions;

    [MenuItem("Tools/Warxel/Validate Voxel Studio")]
    public static void Run()
    {
        assertions = 0;
        var previous = SceneManager.GetActiveScene();
        string bootstrapPath = null;
        if (string.IsNullOrEmpty(previous.path))
        {
            if (!Application.isBatchMode)
            {
                Debug.LogWarning("Salve a cena atual antes de executar a validação do Voxel Studio.");
                return;
            }
            bootstrapPath = "Assets/VoxelValidationBootstrap_" + Guid.NewGuid().ToString("N") + ".unity";
            EditorSceneManager.SaveScene(previous, bootstrapPath);
        }
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        string scenePath = "Assets/VoxelValidation_" + Guid.NewGuid().ToString("N") + ".unity";
        try
        {
            var terrain = new GameObject("Validation map").AddComponent<VoxelTerrain>();
            terrain.RebuildAll();
            TestDataAndChunks(terrain);
            TestMeshing(terrain);
            TestRaycast(terrain);
            TestBrushes();
            TestFill(terrain);
            TestUndo(terrain);
            TestSelectionTransforms(terrain);
            TestSelectionWindow(terrain);
            var watch = Stopwatch.StartNew();
            terrain.ReplaceVoxels(Array.Empty<WorldVoxelData>(), 1);
            for (int z = 0; z < 32; z++)
            for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++) terrain.SetVoxel(new Vector3Int(x, y, z), Color.white);
            terrain.Flush(); watch.Stop();
            Check(terrain.VoxelCount == 32768 && terrain.ChunkCount == 8, "32 cubed map uses eight chunks");
            Check(terrain.GetComponentsInChildren<MeshFilter>().Sum(f => f.sharedMesh.vertexCount) == 96, "solid 32 cubed map has only 24 chunk boundary quads");
            Debug.Log($"Voxel Studio: 32,768 voxels + greedy meshes + colliders in {watch.ElapsedMilliseconds} ms (local validation, not a gameplay benchmark).");
            Check(EditorSceneManager.SaveScene(scene, scenePath), "save scene");
            EditorSceneManager.CloseScene(scene, true);
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            terrain = scene.GetRootGameObjects().Select(g => g.GetComponent<VoxelTerrain>()).First(t => t);
            terrain.RebuildAll();
            Check(terrain.VoxelCount == 32768 && terrain.ChunkCount == 8, "scene reload restores voxels and generated chunks");
            Check(scene.GetRootGameObjects().Length == 1, "generated objects are not serialized as scene roots");
            Debug.Log($"VOXEL_STUDIO_VALIDATION_PASS: {assertions} assertions.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            throw;
        }
        finally
        {
            if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            AssetDatabase.DeleteAsset(scenePath);
            if (bootstrapPath != null) AssetDatabase.DeleteAsset(bootstrapPath);
        }
    }

    private static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new InvalidOperationException("Voxel Studio validation failed: " + message);
    }
    private static void TestDataAndChunks(VoxelTerrain t)
    {
        var red = new Color32(255, 0, 0, 255);
        Check(t.SetVoxel(new Vector3Int(-1, 0, 0), red), "insert negative cell");
        Check(!t.SetVoxel(new Vector3Int(-1, 0, 0), red), "same color is no-op");
        t.SetVoxel(Vector3Int.zero, red); t.Flush();
        Check(t.ChunkCount == 2, "negative chunk coordinates floor correctly");
        Check(!t.SetVoxel(new Vector3Int(3, 0, 0), red, true), "paint does not insert");
        t.EraseVoxel(new Vector3Int(-1, 0, 0)); t.Flush();
        Check(t.VoxelCount == 1 && t.ChunkCount == 1 && t.TryGetVoxel(Vector3Int.zero, out _), "swap-remove preserves index and removes empty chunk");
        Check(!t.SetVoxel(new Vector3Int(32768, 0, 0), red), "coordinate bound enforced");
        t.ReplaceVoxels(new[] { new WorldVoxelData(Vector3Int.zero, Color.red), new WorldVoxelData(Vector3Int.zero, Color.blue), new WorldVoxelData(Vector3Int.one, Color.white) { isActive = false } }, 1);
        Check(t.VoxelCount == 1 && t.TryGetVoxel(Vector3Int.zero, out var c) && c.b == 255, "legacy duplicates and inactive cells compacted");
    }
    private static void TestMeshing(VoxelTerrain t)
    {
        t.ReplaceVoxels(Array.Empty<WorldVoxelData>(), 1);
        t.SetVoxel(Vector3Int.zero, Color.white); t.SetVoxel(Vector3Int.right, Color.white); t.Flush();
        var mesh = t.GetComponentInChildren<MeshFilter>().sharedMesh;
        Check(mesh.vertexCount == 24 && mesh.triangles.Length == 36, "same-color prism merges to six quads");
        var vertices = mesh.vertices; var normals = mesh.normals; var indices = mesh.triangles;
        for (int i = 0; i < indices.Length; i += 3)
            Check(Vector3.Dot(Vector3.Cross(vertices[indices[i + 1]] - vertices[indices[i]], vertices[indices[i + 2]] - vertices[indices[i]]), normals[indices[i]]) > 0, "triangle winding matches outward normal");
        Physics.SyncTransforms();
        Check(t.GetComponentInChildren<MeshCollider>().Raycast(new Ray(new Vector3(0, 5, 0), Vector3.down), out _, 10), "generated collider is usable by physics");
        Check(Physics.RaycastAll(new Ray(new Vector3(0, 5, 0), Vector3.down), 10).Any(hit => hit.collider == t.GetComponentInChildren<MeshCollider>()), "hidden generated collider participates in the physics world");
        t.SetVoxel(Vector3Int.right, Color.red); t.Flush();
        Check(mesh.vertexCount == 40, "different colors do not merge");
        t.ReplaceVoxels(new[] { new WorldVoxelData(new Vector3Int(15, 0, 0), Color.white), new WorldVoxelData(new Vector3Int(16, 0, 0), Color.white) }, 1);
        Check(t.GetComponentsInChildren<MeshFilter>().Sum(f => f.sharedMesh.vertexCount) == 40, "shared chunk boundary is culled");
        t.EraseVoxel(new Vector3Int(16, 0, 0)); t.Flush();
        Check(t.GetComponentInChildren<MeshFilter>().sharedMesh.vertexCount == 24, "removal exposes neighbor chunk face");
        t.SetVoxel(new Vector3Int(15, 1, 0), Color.white); t.Flush(false);
        Check(t.GetComponentInChildren<MeshCollider>().sharedMesh == null, "collider cooking deferred during stroke");
        t.FlushColliders(); Check(t.GetComponentInChildren<MeshCollider>().sharedMesh != null, "collider restored after stroke");
        t.generateColliders = false; t.RebuildAll();
        Check(!t.GetComponentInChildren<MeshCollider>().enabled, "collider toggle applied");
        t.generateColliders = true;
    }
    private static void TestRaycast(VoxelTerrain t)
    {
        t.ReplaceVoxels(new[] { new WorldVoxelData(Vector3Int.zero, Color.white) }, 0.5f);
        t.transform.SetPositionAndRotation(new Vector3(11, -5, 8), Quaternion.Euler(20, 40, 10));
        t.transform.localScale = new Vector3(2, 3, 0.75f);
        var start = t.transform.TransformPoint(new Vector3(0, 5, 0));
        var target = t.GridToWorld(Vector3Int.zero);
        Check(t.Raycast(new Ray(start, target - start), out var cell, out var normal) && cell == Vector3Int.zero && normal == Vector3Int.up, "DDA respects translation, rotation, scale, voxel size");
        Check(!t.Raycast(new Ray(start, target - start), out _, out _, 0.1f), "ray distance measured in world space");
        t.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); t.transform.localScale = Vector3.one;
        t.ReplaceVoxels(new[] { new WorldVoxelData(new Vector3Int(-17, -2, 0), Color.white) }, 1);
        Check(t.Raycast(new Ray(new Vector3(-17, 10, 0), Vector3.down), out cell, out normal) && cell == new Vector3Int(-17, -2, 0), "DDA negative cells and axis parallel ray");
    }
    private static void TestBrushes()
    {
        Check(VoxelBrush.Cells(Vector3Int.zero, new Vector3Int(4, 2, 6), VoxelShape.Box, false).Count() == 48, "even dimensions exact");
        Check(VoxelBrush.Cells(Vector3Int.zero, new Vector3Int(3, 3, 3), VoxelShape.Box, true).Count() == 26, "hollow box excludes center");
        Check(VoxelBrush.Cells(Vector3Int.zero, new Vector3Int(3, 8, 3), VoxelShape.Plane, true).Count() == 8, "hollow plane is a perimeter");
        Check(VoxelBrush.Cells(Vector3Int.zero, Vector3Int.one, VoxelShape.Sphere, false).Count() == 1, "single-cell sphere");
        var cylinder = VoxelBrush.Cells(Vector3Int.zero, new Vector3Int(5, 4, 5), VoxelShape.Cylinder, false).ToList();
        Check(cylinder.Max(p => p.y) - cylinder.Min(p => p.y) + 1 == 4, "cylinder height exact");
        var line = VoxelBrush.Line(new Vector3Int(-3, 4, 1), new Vector3Int(5, 8, -1)).ToList();
        Check(line.Count == 9 && line.First() == new Vector3Int(-3, 4, 1) && line.Last() == new Vector3Int(5, 8, -1), "stroke interpolation includes endpoints");
    }
    private static void TestFill(VoxelTerrain t)
    {
        t.ReplaceVoxels(new[] { new WorldVoxelData(Vector3Int.zero, Color.red), new WorldVoxelData(Vector3Int.right, Color.red), new WorldVoxelData(Vector3Int.up, Color.blue), new WorldVoxelData(Vector3Int.right * 4, Color.red) }, 1);
        var cells = new List<Vector3Int>();
        Check(VoxelBrush.CollectConnected(t, Vector3Int.zero, cells) && cells.Count == 2, "fill stops at different colors and disconnected islands");
        Check(VoxelBrush.CollectConnected(t, Vector3Int.zero, cells, p => p.x == 0) && cells.Count == 1, "fill traversal honors selection bounds");
        var asset = ScriptableObject.CreateInstance<VoxelMapAsset>();
        foreach (var v in t.Voxels) asset.voxels.Add(new WorldVoxelData(v.position, v.color));
        t.ReplaceVoxels(asset.voxels, asset.voxelSize); t.SetVoxel(Vector3Int.zero, Color.green);
        Check(((Color32)asset.voxels[0].color).r == 255, "asset import uses independent data");
        Object.DestroyImmediate(asset);
    }
    private static void TestUndo(VoxelTerrain t)
    {
        int before = t.VoxelCount;
        Undo.IncrementCurrentGroup();
        Undo.RegisterCompleteObjectUndo(t, "Voxel validation undo");
        t.SetVoxel(new Vector3Int(9, 9, 9), Color.white); t.Flush();
        Undo.FlushUndoRecordObjects(); Undo.PerformUndo(); t.RebuildAll();
        Check(t.VoxelCount == before && !t.TryGetVoxel(new Vector3Int(9, 9, 9), out _), "undo restores serialized data and caches");
        Undo.PerformRedo(); t.RebuildAll();
        Check(t.VoxelCount == before + 1, "redo restores edit");
        Undo.ClearUndo(t);
    }

    private static void TestSelectionTransforms(VoxelTerrain t)
    {
        var source = new List<WorldVoxelData>
        {
            new WorldVoxelData(new Vector3Int(-1, 2, 0), Color.red),
            new WorldVoxelData(new Vector3Int(0, 2, 0), Color.blue),
            new WorldVoxelData(new Vector3Int(-1, 3, 0), Color.green)
        };
        var pivot = new Vector3Int(-1, 2, 0);
        t.ReplaceVoxels(source, 0.5f);
        Check(VoxelSelectionTransform.TryPlan(t, source, pivot, Vector3Int.right, Vector3Int.zero, false, out var destination, out _), "overlapping movement within the original selection is allowed");
        VoxelSelectionTransform.Apply(t, source, destination, false); t.Flush();
        Check(t.VoxelCount == 3 && !t.TryGetVoxel(new Vector3Int(-1, 2, 0), out _), "movement removes old positions without dropping overlapping voxels");
        Check(t.TryGetVoxel(new Vector3Int(0, 2, 0), out var red) && red.r == 255 && red.b == 0, "overlapping movement preserves source colors");
        for (int axis = 0; axis < 3; axis++)
        {
            var turns = Vector3Int.zero; turns[axis] = 1;
            foreach (var v in source)
            {
                var p = v.position;
                for (int i = 0; i < 4; i++) p = VoxelSelectionTransform.TransformPosition(p, pivot, Vector3Int.zero, turns);
                Check(p == v.position, "four quarter turns restore even-sized selection without drift");
                p = VoxelSelectionTransform.TransformPosition(v.position, pivot, Vector3Int.zero, turns);
                p = VoxelSelectionTransform.TransformPosition(p, pivot, Vector3Int.zero, -turns);
                Check(p == v.position, "inverse quarter turn restores the original position");
            }
        }
        t.ReplaceVoxels(source, 1);
        Check(VoxelSelectionTransform.TryPlan(t, source, pivot, Vector3Int.zero, Vector3Int.up, false, out destination, out _), "quarter rotation can be planned");
        Check(destination.Select(v => v.position).Distinct().Count() == source.Count, "rotation does not merge source cells");
        VoxelSelectionTransform.Apply(t, source, destination, false); t.Flush();
        Check(t.VoxelCount == 3 && t.TryGetVoxel(new Vector3Int(-1, 2, -1), out var blue) && blue.b == 255, "Y rotation has correct positions and colors");
        t.ReplaceVoxels(source, 1);
        var offset = new Vector3Int(16, 0, -16);
        var blocker = source[0].position + offset;
        t.SetVoxel(blocker, Color.yellow); t.Flush();
        Check(!VoxelSelectionTransform.TryPlan(t, source, pivot, offset, Vector3Int.zero, false, out _, out _), "outside occupied destinations block the complete operation");
        Check(t.VoxelCount == 4 && t.TryGetVoxel(source[0].position, out _), "invalid plan leaves the map untouched");
        Check(VoxelSelectionTransform.TryPlan(t, source, pivot, offset, Vector3Int.zero, true, out destination, out _), "overwrite is explicit");
        VoxelSelectionTransform.Apply(t, source, destination, true); t.Flush();
        Check(t.VoxelCount == 6 && t.TryGetVoxel(source[0].position, out _) && t.TryGetVoxel(blocker, out red) && red.g == 0, "duplicate retains originals and replaces destination only when enabled");
        Check(!VoxelSelectionTransform.TryPlan(t, source, pivot, new Vector3Int(32767, 32767, 0), Vector3Int.zero, true, out _, out _), "out-of-range transformations are rejected");
    }

    private static void TestSelectionWindow(VoxelTerrain t)
    {
        for (int axis = 0; axis < 3; axis++)
        {
            var expected = Vector3Int.zero; expected[axis] = 3;
            Check(VoxelEditorWindow.AxisMoveOffset(new Vector3(3.2f, 3.2f, 3.2f), axis) == expected, "arrow motion only changes the dragged axis");
        }
        // Exercise the actual editor commands and serialized selection Undo, without opening a user window.
        var window = ScriptableObject.CreateInstance<VoxelEditorWindow>();
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var type = typeof(VoxelEditorWindow);
        Action<string, object> set = (name, value) => type.GetField(name, flags).SetValue(window, value);
        Func<string, object> get = name => type.GetField(name, flags).GetValue(window);
        Action<string, object[]> call = (name, args) => type.GetMethod(name, flags).Invoke(window, args);
        try
        {
            t.ReplaceVoxels(new[] { new WorldVoxelData(Vector3Int.zero, Color.red), new WorldVoxelData(new Vector3Int(2, 0, 0), Color.blue), new WorldVoxelData(new Vector3Int(1, 0, -1), Color.green) }, 1);
            set("terrain", t); set("hasSelection", true);
            set("selectionMin", Vector3Int.zero); set("selectionMax", new Vector3Int(2, 0, 0));
            call("CompleteRegionSelection", Array.Empty<object>());
            Check(Convert.ToInt32(get("selectionTool")) == 1, "completing a nonempty region automatically shows movement arrows");
            set("moveOffset", new Vector3Int(0, 0, -1));
            call("MoveSelection", new object[] { false });
            Check(((List<Vector3Int>)get("selectedCells")).Count == 2, "moving selection does not capture an unrelated voxel in its new bounds");
            var movedPivot = (Vector3Int)get("selectionPivot");
            Check(movedPivot == new Vector3Int(1, 0, -1), "selection pivot follows translation");
            Undo.PerformUndo(); t.RebuildAll();
            Check(t.TryGetVoxel(Vector3Int.zero, out _) && (Vector3Int)get("selectionPivot") == Vector3Int.right, "undo restores both voxel positions and selection pivot");
            Undo.PerformRedo(); t.RebuildAll();
            Check(!t.TryGetVoxel(Vector3Int.zero, out _) && (Vector3Int)get("selectionPivot") == movedPivot, "redo restores moved selection");
            call("RotateSelection", new object[] { 1, 1 });
            Check(t.TryGetVoxel(new Vector3Int(1, 0, 0), out var red) && red.r == 255, "editor rotation command applies the selected transform");
            Check(t.TryGetVoxel(new Vector3Int(1, 0, -1), out var green) && green.g == 255 && ((List<Vector3Int>)get("selectedCells")).Count == 2, "rotation preserves unselected voxel inside selection bounds");
            Check((Vector3Int)get("selectionPivot") == movedPivot, "rotation keeps its pivot stable");
            Undo.PerformUndo(); t.RebuildAll();
            Check(t.TryGetVoxel(new Vector3Int(0, 0, -1), out _) && ((List<Vector3Int>)get("selectedCells")).Contains(new Vector3Int(0, 0, -1)), "rotation undo restores the exact selected coordinates");
        }
        finally
        {
            Undo.ClearUndo(window); Undo.ClearUndo(t);
            Object.DestroyImmediate(window);
        }
    }
}
