using UnityEngine;

[System.Serializable]
public class WorldVoxelData
{
    public Vector3Int position;
    public Color color;
    public bool isActive;

    public WorldVoxelData(Vector3Int pos, Color col)
    {
        position = pos;
        color = col;
        isActive = true;
    }
}