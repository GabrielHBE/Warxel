using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Serialized voxel source with disposable, chunked rendering caches.</summary>
[ExecuteAlways, DisallowMultipleComponent]
public sealed class VoxelTerrain : MonoBehaviour
{
    public const int ChunkSize = 16;
    public const int CoordinateLimit = 32767;
    [Min(0.01f)] public float voxelSize = 1f;
    public Material voxelMaterial;
    public bool generateColliders = true;
    // Keep the original field names and script GUID so existing scenes retain their data.
    [SerializeField, HideInInspector] private List<WorldVoxelData> voxels = new List<WorldVoxelData>();
    private readonly Dictionary<Vector3Int, int> index = new Dictionary<Vector3Int, int>();
    private readonly Dictionary<Vector3Int, int> chunkCounts = new Dictionary<Vector3Int, int>();
    private readonly Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();
    private readonly HashSet<Vector3Int> dirty = new HashSet<Vector3Int>();
    private readonly HashSet<Vector3Int> colliderDirty = new HashSet<Vector3Int>();
    private bool ready, rebuildRequested;
    private Material fallbackMaterial;

    private sealed class Chunk
    {
        public GameObject gameObject;
        public Mesh mesh;
        public MeshRenderer renderer;
        public MeshCollider collider;
    }

    public int VoxelCount { get { EnsureIndex(); return index.Count; } }
    public int ChunkCount => chunks.Count;
    public IReadOnlyList<WorldVoxelData> Voxels { get { EnsureIndex(); return voxels; } }

    private void OnEnable()
    {
        if (Application.isPlaying) RebuildAll();
        else rebuildRequested = true;
    }
    private void OnValidate() { voxelSize = Mathf.Max(0.01f, voxelSize); rebuildRequested = true; }
    private void Update()
    {
        if (rebuildRequested) RebuildAll();
        else if (dirty.Count > 0) Flush();
    }
    private void OnDisable() { ReleaseChunks(); ready = false; }
    private void OnDestroy() { ReleaseChunks(); DestroyOwned(fallbackMaterial); }

    public static bool IsValidPosition(Vector3Int p) =>
        Math.Abs((long)p.x) <= CoordinateLimit && Math.Abs((long)p.y) <= CoordinateLimit && Math.Abs((long)p.z) <= CoordinateLimit;

    public static Vector3Int ChunkOf(Vector3Int p) => new Vector3Int(p.x >> 4, p.y >> 4, p.z >> 4);

    private void EnsureIndex()
    {
        if (ready) return;
        index.Clear();
        chunkCounts.Clear();
        // Compact inactive entries and duplicates left by the old editor.
        int write = 0;
        for (int read = 0; read < voxels.Count; read++)
        {
            var v = voxels[read];
            if (v == null || !v.isActive || !IsValidPosition(v.position)) continue;
            if (index.TryGetValue(v.position, out int previous)) { voxels[previous] = v; continue; }
            voxels[write] = v;
            index.Add(v.position, write++);
            var key = ChunkOf(v.position);
            chunkCounts.TryGetValue(key, out int count);
            chunkCounts[key] = count + 1;
        }
        if (write < voxels.Count) voxels.RemoveRange(write, voxels.Count - write);
        ready = true;
    }

    public bool TryGetVoxel(Vector3Int position, out Color32 color)
    {
        EnsureIndex();
        if (index.TryGetValue(position, out int i)) { color = voxels[i].color; return true; }
        color = default;
        return false;
    }

    public bool SetVoxel(Vector3Int position, Color32 color, bool onlyExisting = false)
    {
        if (!IsValidPosition(position)) return false;
        EnsureIndex();
        color.a = 255;
        if (index.TryGetValue(position, out int i))
        {
            if (((Color32)voxels[i].color).Equals(color)) return false;
            voxels[i].color = color;
        }
        else
        {
            if (onlyExisting) return false;
            index.Add(position, voxels.Count);
            voxels.Add(new WorldVoxelData(position, color));
            var key = ChunkOf(position);
            chunkCounts.TryGetValue(key, out int count);
            chunkCounts[key] = count + 1;
        }
        MarkDirty(position);
        return true;
    }

    public bool EraseVoxel(Vector3Int position)
    {
        EnsureIndex();
        if (!index.TryGetValue(position, out int i)) return false;
        int last = voxels.Count - 1;
        if (i != last) { voxels[i] = voxels[last]; index[voxels[i].position] = i; }
        voxels.RemoveAt(last);
        index.Remove(position);
        var key = ChunkOf(position);
        if (--chunkCounts[key] == 0) chunkCounts.Remove(key);
        MarkDirty(position);
        return true;
    }

    private void MarkDirty(Vector3Int p)
    {
        var key = ChunkOf(p);
        dirty.Add(key);
        for (int axis = 0; axis < 3; axis++)
        {
            int local = p[axis] & 15;
            if (local != 0 && local != 15) continue;
            var neighbor = key;
            neighbor[axis] += local == 0 ? -1 : 1;
            dirty.Add(neighbor);
        }
    }

    public void ReplaceVoxels(IEnumerable<WorldVoxelData> source, float size)
    {
        var copy = new List<WorldVoxelData>();
        foreach (var v in source)
            if (v != null && v.isActive) copy.Add(new WorldVoxelData(v.position, v.color));
        voxels = copy;
        voxelSize = Mathf.Max(0.01f, size);
        RebuildAll();
    }

    public void RebuildAll()
    {
        rebuildRequested = false;
        ready = false;
        EnsureIndex();
        ReleaseChunks();
        // Disable the old monolithic render/collision components without deleting user assets.
        var oldRenderer = GetComponent<MeshRenderer>();
        if (oldRenderer) oldRenderer.enabled = false;
        var oldCollider = GetComponent<MeshCollider>();
        if (oldCollider) oldCollider.enabled = false;
        foreach (var key in chunkCounts.Keys) dirty.Add(key);
        Flush();
    }

    public void Flush(bool updateColliders = true)
    {
        EnsureIndex();
        Material material = GetMaterial();
        foreach (var key in dirty)
        {
            chunks.TryGetValue(key, out var chunk);
            if (!chunkCounts.ContainsKey(key))
            {
                if (chunk != null) { DestroyOwned(chunk.gameObject); DestroyOwned(chunk.mesh); chunks.Remove(key); }
                colliderDirty.Remove(key);
                continue;
            }
            if (chunk == null)
            {
                var go = new GameObject($"Voxel chunk {key.x}, {key.y}, {key.z}");
                go.hideFlags = HideFlags.HideAndDontSave;
                go.layer = gameObject.layer;
                go.transform.SetParent(transform, false);
                chunk = new Chunk { gameObject = go, mesh = new Mesh { name = "Voxel chunk mesh", hideFlags = HideFlags.HideAndDontSave } };
                go.AddComponent<MeshFilter>().sharedMesh = chunk.mesh;
                chunk.renderer = go.AddComponent<MeshRenderer>();
                chunk.collider = go.AddComponent<MeshCollider>();
                chunks.Add(key, chunk);
            }
            // Never modify a mesh still assigned to the physics engine.
            chunk.collider.sharedMesh = null;
            chunk.gameObject.transform.localPosition = (Vector3)(key * ChunkSize) * voxelSize;
            chunk.renderer.sharedMaterial = material;
            GreedyMeshing.Build(this, key, chunk.mesh);
            colliderDirty.Add(key);
        }
        dirty.Clear();
        if (updateColliders) FlushColliders();
    }

    public void FlushColliders()
    {
        foreach (var key in colliderDirty)
            if (chunks.TryGetValue(key, out var chunk))
            {
                chunk.collider.enabled = generateColliders;
                chunk.collider.sharedMesh = generateColliders && chunk.mesh.vertexCount > 0 ? chunk.mesh : null;
            }
        colliderDirty.Clear();
    }

    private Material GetMaterial()
    {
        if (voxelMaterial) return voxelMaterial;
        if (!fallbackMaterial)
        {
            // Resources reference ensures the shader survives player shader stripping.
            var shader = Resources.Load<Shader>("WarxelVoxel");
            if (!shader) shader = Shader.Find("Custom/VoxelURPShader");
            if (!shader) return null;
            fallbackMaterial = new Material(shader) { name = "Voxel colors", hideFlags = HideFlags.HideAndDontSave };
        }
        return fallbackMaterial;
    }

    private void ReleaseChunks()
    {
        foreach (var chunk in chunks.Values) { DestroyOwned(chunk.gameObject); DestroyOwned(chunk.mesh); }
        chunks.Clear(); dirty.Clear(); colliderDirty.Clear();
    }

    private static void DestroyOwned(UnityEngine.Object obj)
    {
        if (!obj) return;
        if (Application.isPlaying) Destroy(obj); else DestroyImmediate(obj);
    }

    public Vector3 GridToWorld(Vector3Int p) => transform.TransformPoint((Vector3)p * voxelSize);
    public Vector3Int WorldToGrid(Vector3 p) => Vector3Int.FloorToInt(transform.InverseTransformPoint(p) / voxelSize + Vector3.one * 0.5f);

    /// <summary>DDA picking in voxel space; works without colliders and with transformed maps.</summary>
    public bool Raycast(Ray worldRay, out Vector3Int cell, out Vector3Int normal, float maxDistance = 10000f)
    {
        EnsureIndex();
        cell = normal = default;
        if (index.Count == 0) return false;
        Vector3 origin = transform.InverseTransformPoint(worldRay.origin) / voxelSize;
        Vector3 direction = transform.InverseTransformVector(worldRay.direction.normalized) / voxelSize;
        float enter = 0, exit = maxDistance;
        var min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        var max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        foreach (var key in chunkCounts.Keys)
        {
            min = Vector3.Min(min, (Vector3)(key * ChunkSize) - Vector3.one * 0.5f);
            max = Vector3.Max(max, (Vector3)((key + Vector3Int.one) * ChunkSize) - Vector3.one * 0.5f);
        }
        for (int axis = 0; axis < 3; axis++)
        {
            if (Mathf.Abs(direction[axis]) < 1e-8f)
            { if (origin[axis] < min[axis] || origin[axis] > max[axis]) return false; continue; }
            float a = (min[axis] - origin[axis]) / direction[axis];
            float b = (max[axis] - origin[axis]) / direction[axis];
            if (a > b) { float swap = a; a = b; b = swap; }
            if (a > enter) { enter = a; normal = Vector3Int.zero; normal[axis] = direction[axis] > 0 ? -1 : 1; }
            exit = Mathf.Min(exit, b);
            if (enter > exit) return false;
        }
        cell = Vector3Int.FloorToInt(origin + direction * enter + direction.normalized * 0.0001f + Vector3.one * 0.5f);
        Vector3 delta = default, next = default;
        Vector3Int step = default;
        for (int axis = 0; axis < 3; axis++)
        {
            step[axis] = direction[axis] >= 0 ? 1 : -1;
            delta[axis] = Mathf.Abs(direction[axis]) < 1e-8f ? float.PositiveInfinity : Mathf.Abs(1f / direction[axis]);
            next[axis] = float.IsInfinity(delta[axis]) ? float.PositiveInfinity :
                (cell[axis] + step[axis] * 0.5f - origin[axis]) / direction[axis];
        }
        // A bounded world permits at most 3 * 65536 cell crossings.
        for (int i = 0; i < 196608 && enter <= exit; i++)
        {
            if (index.ContainsKey(cell))
            {
                if (normal == Vector3Int.zero) { int a = Mathf.Abs(direction.x) > Mathf.Abs(direction.y) ? 0 : 1; if (Mathf.Abs(direction.z) > Mathf.Abs(direction[a])) a = 2; normal[a] = -step[a]; }
                return true;
            }
            int axis = next.x < next.y ? 0 : 1;
            if (next.z < next[axis]) axis = 2;
            enter = next[axis]; next[axis] += delta[axis]; cell[axis] += step[axis];
            normal = Vector3Int.zero; normal[axis] = -step[axis];
        }
        return false;
    }
}