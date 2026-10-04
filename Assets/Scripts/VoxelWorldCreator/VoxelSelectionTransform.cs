using System.Collections.Generic;
using UnityEngine;

/// <summary>Exact grid transforms. Build and validate the entire destination before editing the map.</summary>
public static class VoxelSelectionTransform
{
    public static Vector3Int TransformPosition(Vector3Int position, Vector3Int pivot, Vector3Int offset, Vector3Int turns)
    {
        var p = position - pivot;
        for (int i = 0; i < ((turns.x % 4) + 4) % 4; i++) p = new Vector3Int(p.x, -p.z, p.y);
        for (int i = 0; i < ((turns.y % 4) + 4) % 4; i++) p = new Vector3Int(p.z, p.y, -p.x);
        for (int i = 0; i < ((turns.z % 4) + 4) % 4; i++) p = new Vector3Int(-p.y, p.x, p.z);
        return pivot + p + offset;
    }

    public static bool TryPlan(VoxelTerrain terrain, IReadOnlyList<WorldVoxelData> source,
        Vector3Int pivot, Vector3Int offset, Vector3Int turns, bool overwrite,
        out List<WorldVoxelData> destination, out string error)
    {
        destination = new List<WorldVoxelData>(source.Count);
        error = null;
        if (source.Count == 0) { error = "A seleção não contém voxels."; return false; }
        var original = new HashSet<Vector3Int>();
        foreach (var v in source) original.Add(v.position);
        foreach (var v in source)
        {
            var p = TransformPosition(v.position, pivot, offset, turns);
            destination.Add(new WorldVoxelData(p, v.color));
            if (!VoxelTerrain.IsValidPosition(p)) error = "O destino excede os limites do mapa.";
            else if (!overwrite && !original.Contains(p) && terrain.TryGetVoxel(p, out _))
                error = "Destino ocupado por voxels fora da seleção. Mova para um espaço livre ou ative Substituir no destino.";
        }
        return error == null;
    }

    // Call only with a successfully validated plan, in the same synchronous operation.
    public static void Apply(VoxelTerrain terrain, IReadOnlyList<WorldVoxelData> source,
        IReadOnlyList<WorldVoxelData> destination, bool duplicate)
    {
        if (!duplicate) foreach (var v in source) terrain.EraseVoxel(v.position);
        foreach (var v in destination) terrain.SetVoxel(v.position, v.color);
    }
}
