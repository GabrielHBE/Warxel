using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Merges coplanar exposed faces of the same color, including neighbor-chunk culling.</summary>
public static class GreedyMeshing
{
    private struct Face
    {
        public bool exists;
        public Color32 color;
        public bool Matches(Face other) => exists && other.exists && color.Equals(other.color);
    }

    public static void Build(VoxelTerrain terrain, Vector3Int chunk, Mesh mesh)
    {
        const int n = VoxelTerrain.ChunkSize;
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var colors = new List<Color32>();
        var triangles = new List<int>();
        var mask = new Face[n * n];
        Vector3Int origin = chunk * n;
        // One-cell halo: hash lookups happen once per sample, not once per face.
        const int padded = n + 2;
        var samples = new Face[padded * padded * padded];
        for (int z = 0; z < padded; z++)
        for (int y = 0; y < padded; y++)
        for (int x = 0; x < padded; x++)
        {
            bool occupied = terrain.TryGetVoxel(origin + new Vector3Int(x - 1, y - 1, z - 1), out var color);
            samples[x + padded * (y + padded * z)] = new Face { exists = occupied, color = color };
        }
        for (int axis = 0; axis < 3; axis++)
        for (int sign = -1; sign <= 1; sign += 2)
        {
            int u = (axis + 1) % 3, v = (axis + 2) % 3;
            var normal = Vector3.zero; normal[axis] = sign;
            int neighborOffset = sign * (axis == 0 ? 1 : axis == 1 ? padded : padded * padded);
            for (int slice = 0; slice < n; slice++)
            {
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    var p = Vector3Int.one; p[axis] += slice; p[u] += x; p[v] += y;
                    int sample = p.x + padded * (p.y + padded * p.z);
                    var face = samples[sample];
                    mask[x + y * n] = new Face { exists = face.exists && !samples[sample + neighborOffset].exists, color = face.color };
                }
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n;)
                {
                    var face = mask[x + y * n];
                    if (!face.exists) { x++; continue; }
                    int width = 1, height = 1;
                    while (x + width < n && face.Matches(mask[x + width + y * n])) width++;
                    bool done = false;
                    while (y + height < n && !done)
                    {
                        for (int k = 0; k < width; k++)
                            if (!face.Matches(mask[x + k + (y + height) * n])) { done = true; break; }
                        if (!done) height++;
                    }
                    var a = Vector3.zero; a[axis] = slice + sign * 0.5f; a[u] = x - 0.5f; a[v] = y - 0.5f;
                    var du = Vector3.zero; du[u] = width;
                    var dv = Vector3.zero; dv[v] = height;
                    int start = vertices.Count;
                    vertices.Add(a * terrain.voxelSize);
                    vertices.Add((a + du) * terrain.voxelSize);
                    vertices.Add((a + du + dv) * terrain.voxelSize);
                    vertices.Add((a + dv) * terrain.voxelSize);
                    for (int k = 0; k < 4; k++) { normals.Add(normal); colors.Add(face.color); }
                    triangles.Add(start); triangles.Add(start + (sign > 0 ? 1 : 2)); triangles.Add(start + (sign > 0 ? 2 : 1));
                    triangles.Add(start); triangles.Add(start + (sign > 0 ? 2 : 3)); triangles.Add(start + (sign > 0 ? 3 : 2));
                    for (int h = 0; h < height; h++)
                    for (int w = 0; w < width; w++) mask[x + w + (y + h) * n] = default;
                    x += width;
                }
            }
        }
        mesh.Clear();
        mesh.indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
    }
}
