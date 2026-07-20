using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class GreedyMeshing
{
    public struct MeshData
    {
        public Mesh mesh;
        public Vector3[] vertices;
        public int[] triangles;
        public Vector2[] uv;
        public Color[] colors;
        public Vector3[] normals;
    }

    public static MeshData GenerateMesh(List<WorldVoxelData> voxels, float voxelSize)
    {
        var activeVoxels = voxels.Where(v => v.isActive).ToList();
        if (activeVoxels.Count == 0)
            return new MeshData { mesh = new Mesh() };

        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        var uv = new List<Vector2>();
        var colors = new List<Color>();
        var normals = new List<Vector3>();

        float halfSize = voxelSize / 2f;

        var voxelDict = new Dictionary<Vector3Int, WorldVoxelData>();
        foreach (var v in activeVoxels)
            voxelDict[v.position] = v;

        foreach (var voxel in activeVoxels)
        {
            Vector3 pos = new Vector3(
                voxel.position.x * voxelSize,
                voxel.position.y * voxelSize,
                voxel.position.z * voxelSize
            );

            bool right = voxelDict.ContainsKey(voxel.position + Vector3Int.right);
            bool left = voxelDict.ContainsKey(voxel.position + Vector3Int.left);
            bool up = voxelDict.ContainsKey(voxel.position + Vector3Int.up);
            bool down = voxelDict.ContainsKey(voxel.position + Vector3Int.down);
            bool forward = voxelDict.ContainsKey(voxel.position + Vector3Int.forward);
            bool back = voxelDict.ContainsKey(voxel.position + Vector3Int.back);

            // Right (+X)
            if (!right)
                AddQuad(vertices, triangles, uv, colors, normals,
                    pos + new Vector3(halfSize, -halfSize, -halfSize),
                    pos + new Vector3(halfSize, -halfSize, halfSize),
                    pos + new Vector3(halfSize, halfSize, halfSize),
                    pos + new Vector3(halfSize, halfSize, -halfSize),
                    voxel.color, Vector3.right);

            // Left (-X)
            if (!left)
                AddQuad(vertices, triangles, uv, colors, normals,
                    pos + new Vector3(-halfSize, -halfSize, halfSize),
                    pos + new Vector3(-halfSize, -halfSize, -halfSize),
                    pos + new Vector3(-halfSize, halfSize, -halfSize),
                    pos + new Vector3(-halfSize, halfSize, halfSize),
                    voxel.color, Vector3.left);

            // Up (+Y)
            if (!up)
                AddQuad(vertices, triangles, uv, colors, normals,
                    pos + new Vector3(-halfSize, halfSize, -halfSize),
                    pos + new Vector3(-halfSize, halfSize, halfSize),
                    pos + new Vector3(halfSize, halfSize, halfSize),
                    pos + new Vector3(halfSize, halfSize, -halfSize),
                    voxel.color, Vector3.up);

            // Down (-Y)
            if (!down)
                AddQuad(vertices, triangles, uv, colors, normals,
                    pos + new Vector3(-halfSize, -halfSize, halfSize),
                    pos + new Vector3(-halfSize, -halfSize, -halfSize),
                    pos + new Vector3(halfSize, -halfSize, -halfSize),
                    pos + new Vector3(halfSize, -halfSize, halfSize),
                    voxel.color, Vector3.down);

            // Forward (+Z)
            if (!forward)
                AddQuad(vertices, triangles, uv, colors, normals,
                    pos + new Vector3(-halfSize, -halfSize, halfSize),
                    pos + new Vector3(-halfSize, halfSize, halfSize),
                    pos + new Vector3(halfSize, halfSize, halfSize),
                    pos + new Vector3(halfSize, -halfSize, halfSize),
                    voxel.color, Vector3.forward);

            // Back (-Z)
            if (!back)
                AddQuad(vertices, triangles, uv, colors, normals,
                    pos + new Vector3(halfSize, -halfSize, -halfSize),
                    pos + new Vector3(halfSize, halfSize, -halfSize),
                    pos + new Vector3(-halfSize, halfSize, -halfSize),
                    pos + new Vector3(-halfSize, -halfSize, -halfSize),
                    voxel.color, Vector3.back);
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uv.ToArray();
        mesh.colors = colors.ToArray();
        mesh.normals = normals.ToArray();
        mesh.RecalculateBounds();

        return new MeshData
        {
            mesh = mesh,
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
            uv = uv.ToArray(),
            colors = colors.ToArray(),
            normals = normals.ToArray()
        };
    }

    static void AddQuad(List<Vector3> vertices, List<int> triangles, List<Vector2> uv, List<Color> colors, List<Vector3> normals,
        Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Color color, Vector3 normal)
    {
        int baseIndex = vertices.Count;

        vertices.Add(v0);
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);

        triangles.Add(baseIndex);
        triangles.Add(baseIndex + 2);
        triangles.Add(baseIndex + 1);

        triangles.Add(baseIndex);
        triangles.Add(baseIndex + 3);
        triangles.Add(baseIndex + 2);

        uv.Add(new Vector2(0, 0));
        uv.Add(new Vector2(1, 0));
        uv.Add(new Vector2(1, 1));
        uv.Add(new Vector2(0, 1));

        colors.Add(color);
        colors.Add(color);
        colors.Add(color);
        colors.Add(color);

        // Adiciona a mesma normal para todos os vértices do quad
        normals.Add(normal);
        normals.Add(normal);
        normals.Add(normal);
        normals.Add(normal);
    }
}