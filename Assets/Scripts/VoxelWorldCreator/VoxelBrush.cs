using System.Collections.Generic;
using UnityEngine;

public enum VoxelShape { Box, Sphere, Cylinder, Plane }

public static class VoxelBrush
{
    public const int MaxOperationVoxels = 262144;
    public static readonly Vector3Int[] Neighbors = { Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down, Vector3Int.forward, Vector3Int.back };

    public static Vector3Int ClampSize(Vector3Int size) => new Vector3Int(Mathf.Clamp(size.x, 1, 64), Mathf.Clamp(size.y, 1, 64), Mathf.Clamp(size.z, 1, 64));

    public static IEnumerable<Vector3Int> Cells(Vector3Int center, Vector3Int size, VoxelShape shape, bool hollow)
    {
        size = ClampSize(size);
        if (shape == VoxelShape.Plane) size.y = 1;
        var min = center - new Vector3Int((size.x - 1) / 2, (size.y - 1) / 2, (size.z - 1) / 2);
        for (int z = 0; z < size.z; z++)
        for (int y = 0; y < size.y; y++)
        for (int x = 0; x < size.x; x++)
        {
            var p = new Vector3Int(x, y, z);
            if (!Inside(p, size, shape)) continue;
            if (hollow)
            {
                bool edge = false;
                foreach (var n in Neighbors)
                {
                    if (shape == VoxelShape.Plane && n.y != 0) continue;
                    if (!Inside(p + n, size, shape)) { edge = true; break; }
                }
                if (!edge) continue;
            }
            yield return min + p;
        }
    }

    private static bool Inside(Vector3Int p, Vector3Int size, VoxelShape shape)
    {
        if (p.x < 0 || p.y < 0 || p.z < 0 || p.x >= size.x || p.y >= size.y || p.z >= size.z) return false;
        if (shape == VoxelShape.Box || shape == VoxelShape.Plane) return true;
        Vector3 q = new Vector3((2f * p.x + 1 - size.x) / size.x, (2f * p.y + 1 - size.y) / size.y, (2f * p.z + 1 - size.z) / size.z);
        return q.x * q.x + q.z * q.z + (shape == VoxelShape.Sphere ? q.y * q.y : 0) <= 1f;
    }

    public static IEnumerable<Vector3Int> Line(Vector3Int from, Vector3Int to)
    {
        var delta = to - from;
        int steps = Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y), Mathf.Abs(delta.z));
        for (int i = 0; i <= steps; i++)
            yield return steps == 0 ? from : Vector3Int.RoundToInt(Vector3.Lerp(from, to, (float)i / steps));
    }

    /// <summary>Collect before changing anything, so the size limit never leaves a partial fill.</summary>
    public static bool CollectConnected(VoxelTerrain terrain, Vector3Int seed, List<Vector3Int> result, System.Predicate<Vector3Int> allowed = null)
    {
        result.Clear();
        if ((allowed != null && !allowed(seed)) || !terrain.TryGetVoxel(seed, out var original)) return true;
        var visited = new HashSet<Vector3Int> { seed };
        var queue = new Queue<Vector3Int>(); queue.Enqueue(seed);
        while (queue.Count > 0)
        {
            var p = queue.Dequeue(); result.Add(p);
            if (result.Count > MaxOperationVoxels) { result.Clear(); return false; }
            foreach (var n in Neighbors)
            {
                var next = p + n;
                if (!visited.Add(next)) continue;
                if (allowed != null && !allowed(next)) continue;
                if (terrain.TryGetVoxel(next, out var color) && color.Equals(original)) queue.Enqueue(next);
            }
        }
        return true;
    }
}
