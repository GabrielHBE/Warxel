using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[ExecuteInEditMode]
public class VoxelTerrain : MonoBehaviour
{
    [Header("Voxel Settings")]
    public float voxelSize = 1f;
    public Material voxelMaterial;

    [Header("Voxel Data")]
    public List<WorldVoxelData> voxels = new List<WorldVoxelData>();
    public List<Vector3Int> selectedVoxels = new List<Vector3Int>();
    public bool isSelecting = false;

    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private MeshRenderer meshRenderer;
    private Dictionary<Vector3Int, WorldVoxelData> voxelDictionary = new Dictionary<Vector3Int, WorldVoxelData>();

    void Awake()
    {
        InitializeComponents();
    }

    void InitializeComponents()
    {
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();

        meshCollider = GetComponent<MeshCollider>();
        if (meshCollider == null)
            meshCollider = gameObject.AddComponent<MeshCollider>();

        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();

        if (voxelMaterial != null)
            meshRenderer.sharedMaterial = voxelMaterial;

        meshCollider.convex = false;
    }

    void OnValidate()
    {
        if (meshRenderer != null && voxelMaterial != null)
            meshRenderer.sharedMaterial = voxelMaterial;
    }

    // ============ MÉTODOS PRINCIPAIS ============

    public void AddVoxel(Vector3Int position, Color color)
    {
        var existing = voxels.FirstOrDefault(v => v.position == position);
        if (existing != null)
        {
            existing.color = color;
            existing.isActive = true;
        }
        else
        {
            voxels.Add(new WorldVoxelData(position, color));
        }

        UpdateMesh();
    }

    public void RemoveVoxel(Vector3Int position)
    {
        var existing = voxels.FirstOrDefault(v => v.position == position);
        if (existing != null)
        {
            existing.isActive = false;
            selectedVoxels.Remove(position);
            UpdateMesh();
        }
    }

    public void PaintVoxel(Vector3Int position, Color color)
    {
        var existing = voxels.FirstOrDefault(v => v.position == position);
        if (existing != null && existing.isActive)
        {
            existing.color = color;
            UpdateMesh();
        }
    }

    public void PaintSelectedVoxels(Color color)
    {
        foreach (var pos in selectedVoxels)
        {
            PaintVoxel(pos, color);
        }
    }

    public void RemoveSelectedVoxels()
    {
        var toRemove = new List<Vector3Int>(selectedVoxels);
        foreach (var pos in toRemove)
        {
            RemoveVoxel(pos);
        }
        selectedVoxels.Clear();
    }

    public void ClearSelection()
    {
        selectedVoxels.Clear();
        isSelecting = false;
    }

    public void UpdateMesh()
    {
        if (meshFilter == null)
            InitializeComponents();

        var meshData = GreedyMeshing.GenerateMesh(voxels, voxelSize);
        meshFilter.sharedMesh = meshData.mesh;
        meshCollider.sharedMesh = meshData.mesh;

        if (voxelMaterial != null && meshRenderer != null)
            meshRenderer.sharedMaterial = voxelMaterial;
    }

    // ============ FORMAS GEOMÉTRICAS ============

    public void CreateBox(Vector3Int center, Vector3Int size, Color color)
    {
        int halfX = size.x / 2;
        int halfY = size.y / 2;
        int halfZ = size.z / 2;

        for (int x = -halfX; x <= halfX; x++)
        {
            for (int y = -halfY; y <= halfY; y++)
            {
                for (int z = -halfZ; z <= halfZ; z++)
                {
                    // Apenas a superfície
                    if (x == -halfX || x == halfX ||
                        y == -halfY || y == halfY ||
                        z == -halfZ || z == halfZ)
                    {
                        Vector3Int pos = center + new Vector3Int(x, y, z);
                        AddVoxel(pos, color);
                    }
                }
            }
        }
    }

    public void CreateFilledBox(Vector3Int center, Vector3Int size, Color color)
    {
        int halfX = size.x / 2;
        int halfY = size.y / 2;
        int halfZ = size.z / 2;

        for (int x = -halfX; x <= halfX; x++)
        {
            for (int y = -halfY; y <= halfY; y++)
            {
                for (int z = -halfZ; z <= halfZ; z++)
                {
                    Vector3Int pos = center + new Vector3Int(x, y, z);
                    AddVoxel(pos, color);
                }
            }
        }
    }

    public void CreateCircle2D(Vector3Int center, int radius, Color color)
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int z = -radius; z <= radius; z++)
            {
                if (x * x + z * z <= radius * radius)
                {
                    Vector3Int pos = center + new Vector3Int(x, 0, z);
                    AddVoxel(pos, color);
                }
            }
        }
    }

    public void CreateSphere(Vector3Int center, int radius, Color color)
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    if (x * x + y * y + z * z <= radius * radius)
                    {
                        Vector3Int pos = center + new Vector3Int(x, y, z);
                        AddVoxel(pos, color);
                    }
                }
            }
        }
    }

    public void CreateCylinder(Vector3Int center, int radius, int height, Color color)
    {
        for (int y = -height / 2; y <= height / 2; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    if (x * x + z * z <= radius * radius)
                    {
                        Vector3Int pos = center + new Vector3Int(x, y, z);
                        AddVoxel(pos, color);
                    }
                }
            }
        }
    }

    // ============ MÉTODOS DE UTILIDADE ============

    public Vector3Int WorldToGrid(Vector3 worldPosition)
    {
        Vector3 localPoint = transform.InverseTransformPoint(worldPosition);
        return new Vector3Int(
            Mathf.RoundToInt(localPoint.x / voxelSize),
            Mathf.RoundToInt(localPoint.y / voxelSize),
            Mathf.RoundToInt(localPoint.z / voxelSize)
        );
    }

    // Adicione ao VoxelTerrain.cs - método auxiliar para converter Grid para World
    public Vector3 GridToWorld(Vector3Int gridPosition)
    {
        Vector3 localPos = new Vector3(
            gridPosition.x * voxelSize,
            gridPosition.y * voxelSize,
            gridPosition.z * voxelSize
        );
        return transform.TransformPoint(localPos);
    }

    // Adicione no VoxelTerrain.cs
    public void DebugVoxelPosition(Vector3 worldPos)
    {
        Vector3 localPos = transform.InverseTransformPoint(worldPos);
        float size = voxelSize;
        float halfSize = size * 0.5f;

        int x = Mathf.FloorToInt((localPos.x + halfSize) / size);
        int y = Mathf.FloorToInt((localPos.y + halfSize) / size);
        int z = Mathf.FloorToInt((localPos.z + halfSize) / size);

        // Ajuste fino
        float remX = (localPos.x + halfSize) % size;
        float remY = (localPos.y + halfSize) % size;
        float remZ = (localPos.z + halfSize) % size;

        if (remX < 0.01f) x -= 1;
        if (remY < 0.01f) y -= 1;
        if (remZ < 0.01f) z -= 1;

        Debug.Log($"World: {worldPos} | Local: {localPos} | Grid: ({x}, {y}, {z}) | Remainder: ({remX:F3}, {remY:F3}, {remZ:F3})");
    }

    // Adicione no VoxelTerrain.cs
    public bool IsVoxelActive(Vector3Int position)
    {
        return voxels.Any(v => v.position == position && v.isActive);
    }

    public Vector3Int GetNearestVoxel(Vector3 worldPos)
    {
        Vector3Int nearest = Vector3Int.zero;
        float minDist = float.MaxValue;

        foreach (var v in voxels)
        {
            if (!v.isActive) continue;
            Vector3 voxelWorldPos = GridToWorld(v.position);
            float dist = Vector3.Distance(voxelWorldPos, worldPos);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = v.position;
            }
        }

        return nearest;
    }

    // Adicione estes métodos ao VoxelTerrain.cs
    public void PlaceVoxel(Vector3Int position, Color color)
    {
        // Verifica se já existe um voxel na posição
        var existing = voxels.FirstOrDefault(v => v.position == position);
        if (existing != null)
        {
            // Se já existe, apenas atualiza a cor
            existing.color = color;
            existing.isActive = true;
        }
        else
        {
            // Se não existe, adiciona um novo
            voxels.Add(new WorldVoxelData(position, color));
        }

        UpdateMesh();
    }

    // Método para verificar se uma posição está disponível para colocar
    public bool CanPlaceVoxel(Vector3Int position)
    {
        // Verifica se já existe um voxel ativo na posição
        return !voxels.Any(v => v.position == position && v.isActive);
    }

    // Método para obter posição adjacente baseado na direção
    public Vector3Int GetAdjacentPosition(Vector3Int position, Vector3 direction)
    {
        // Converte direção world para grid
        Vector3 localDir = transform.InverseTransformDirection(direction).normalized;

        // Determina a direção dominante
        float absX = Mathf.Abs(localDir.x);
        float absY = Mathf.Abs(localDir.y);
        float absZ = Mathf.Abs(localDir.z);

        Vector3Int offset = Vector3Int.zero;

        if (absX >= absY && absX >= absZ)
            offset.x = localDir.x > 0 ? 1 : -1;
        else if (absY >= absX && absY >= absZ)
            offset.y = localDir.y > 0 ? 1 : -1;
        else
            offset.z = localDir.z > 0 ? 1 : -1;

        return position + offset;
    }

    // Adicione ao VoxelTerrain.cs
    // Substitua o método GetPlacementPosition por esta versão corrigida
    public Vector3Int GetPlacementPosition(RaycastHit hit)
    {
        float size = voxelSize;
        float halfSize = size * 0.5f;

        // Converte para coordenadas locais
        Vector3 localHit = transform.InverseTransformPoint(hit.point);
        Vector3 localNormal = transform.InverseTransformDirection(hit.normal).normalized;

        // Calcula a posição EXATA do voxel clicado baseado no ponto de impacto
        // Em vez de arredondar, usamos o ponto de impacto para determinar qual voxel foi clicado
        int clickedX = Mathf.RoundToInt((localHit.x) / size);
        int clickedY = Mathf.RoundToInt((localHit.y) / size);
        int clickedZ = Mathf.RoundToInt((localHit.z) / size);

        Vector3Int clickedPos = new Vector3Int(clickedX, clickedY, clickedZ);

        // Verifica se clicou em um voxel existente
        bool hasVoxel = voxels.Any(v => v.position == clickedPos && v.isActive);

        // Se NÃO tem voxel, retorna a posição clicada (permite colocar no chão/vazio)
        if (!hasVoxel)
        {
            return clickedPos;
        }

        // Se TEM voxel, coloca ADJACENTE baseado na normal
        Vector3Int newPos = clickedPos;

        // Determina a direção baseada na normal (com threshold mais preciso)
        float absX = Mathf.Abs(localNormal.x);
        float absY = Mathf.Abs(localNormal.y);
        float absZ = Mathf.Abs(localNormal.z);

        // Usa um threshold menor para detectar melhor as normais
        float threshold = 0.01f;

        if (absX >= absY && absX >= absZ && absX > threshold)
        {
            newPos.x += (localNormal.x > 0) ? 1 : -1;
        }
        else if (absY >= absX && absY >= absZ && absY > threshold)
        {
            newPos.y += (localNormal.y > 0) ? 1 : -1;
        }
        else if (absZ >= absX && absZ >= absY && absZ > threshold)
        {
            newPos.z += (localNormal.z > 0) ? 1 : -1;
        }
        else
        {
            // Fallback: usa a posição do hit para determinar a direção
            Vector3 offset = localHit - new Vector3(clickedPos.x * size, clickedPos.y * size, clickedPos.z * size);

            if (Mathf.Abs(offset.x) > halfSize * 0.5f)
                newPos.x += (offset.x > 0) ? 1 : -1;
            else if (Mathf.Abs(offset.y) > halfSize * 0.5f)
                newPos.y += (offset.y > 0) ? 1 : -1;
            else if (Mathf.Abs(offset.z) > halfSize * 0.5f)
                newPos.z += (offset.z > 0) ? 1 : -1;
        }

        return newPos;
    }
    // Adicione ao VoxelTerrain.cs
    public Vector3Int GetGroundPosition(Vector3 worldPosition)
    {
        // Projeta a posição no chão (Y = 0)
        Vector3 localPoint = transform.InverseTransformPoint(worldPosition);
        localPoint.y = 0;

        return new Vector3Int(
            Mathf.RoundToInt(localPoint.x / voxelSize),
            0,
            Mathf.RoundToInt(localPoint.z / voxelSize)
        );
    }

    // Adicione ao VoxelTerrain.cs
    private Vector3Int lastPlacedPosition = Vector3Int.zero;
    private bool isDragging = false;

    public void StartPlacementDrag(Vector3Int position, Color color)
    {
        isDragging = true;
        lastPlacedPosition = position;
        PlaceVoxel(position, color);
    }

    // Adicione ao VoxelTerrain.cs
    private float lastPlacementTime = 0f;
    private float placementDelay = 0.05f; // 50ms entre adições

    public void ContinuePlacementDrag(Vector3Int position, Color color)
    {
        if (!isDragging) return;

        // Limita a taxa de atualização para melhor desempenho
        if (Time.realtimeSinceStartup - lastPlacementTime < placementDelay)
            return;

        if (position != lastPlacedPosition)
        {
            if (CanPlaceVoxel(position))
            {
                PlaceVoxel(position, color);
                lastPlacedPosition = position;
                lastPlacementTime = Time.realtimeSinceStartup;
            }
        }
    }

    public void EndPlacementDrag()
    {
        isDragging = false;
        lastPlacedPosition = Vector3Int.zero;
    }

    // Adicione ao VoxelTerrain.cs
    public void LinePlacement(Vector3Int start, Vector3Int end, Color color)
    {
        // Usa o algoritmo de Bresenham para desenhar uma linha
        int dx = Mathf.Abs(end.x - start.x);
        int dy = Mathf.Abs(end.y - start.y);
        int dz = Mathf.Abs(end.z - start.z);

        int sx = start.x < end.x ? 1 : -1;
        int sy = start.y < end.y ? 1 : -1;
        int sz = start.z < end.z ? 1 : -1;

        int x = start.x;
        int y = start.y;
        int z = start.z;

        if (dx >= dy && dx >= dz)
        {
            int p1 = 2 * dy - dx;
            int p2 = 2 * dz - dx;
            while (x != end.x)
            {
                PlaceVoxel(new Vector3Int(x, y, z), color);
                if (p1 >= 0)
                {
                    y += sy;
                    p1 -= 2 * dx;
                }
                if (p2 >= 0)
                {
                    z += sz;
                    p2 -= 2 * dx;
                }
                p1 += 2 * dy;
                p2 += 2 * dz;
                x += sx;
            }
        }
        else if (dy >= dx && dy >= dz)
        {
            int p1 = 2 * dx - dy;
            int p2 = 2 * dz - dy;
            while (y != end.y)
            {
                PlaceVoxel(new Vector3Int(x, y, z), color);
                if (p1 >= 0)
                {
                    x += sx;
                    p1 -= 2 * dy;
                }
                if (p2 >= 0)
                {
                    z += sz;
                    p2 -= 2 * dy;
                }
                p1 += 2 * dx;
                p2 += 2 * dz;
                y += sy;
            }
        }
        else
        {
            int p1 = 2 * dy - dz;
            int p2 = 2 * dx - dz;
            while (z != end.z)
            {
                PlaceVoxel(new Vector3Int(x, y, z), color);
                if (p1 >= 0)
                {
                    y += sy;
                    p1 -= 2 * dz;
                }
                if (p2 >= 0)
                {
                    x += sx;
                    p2 -= 2 * dz;
                }
                p1 += 2 * dy;
                p2 += 2 * dx;
                z += sz;
            }
        }
        PlaceVoxel(end, color);
    }

    // Adicione ao VoxelTerrain.cs

    // Método para criar um retângulo (apenas bordas)
    public void CreateRectangle(Vector3Int start, Vector3Int end, Color color)
    {
        int minX = Mathf.Min(start.x, end.x);
        int maxX = Mathf.Max(start.x, end.x);
        int minY = Mathf.Min(start.y, end.y);
        int maxY = Mathf.Max(start.y, end.y);
        int minZ = Mathf.Min(start.z, end.z);
        int maxZ = Mathf.Max(start.z, end.z);

        // Apenas a superfície do retângulo
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    // Verifica se está na borda
                    if (x == minX || x == maxX ||
                        y == minY || y == maxY ||
                        z == minZ || z == maxZ)
                    {
                        Vector3Int pos = new Vector3Int(x, y, z);
                        if (CanPlaceVoxel(pos))
                            PlaceVoxel(pos, color);
                    }
                }
            }
        }
    }

    // Método para criar um retângulo preenchido
    public void CreateFilledRectangle(Vector3Int start, Vector3Int end, Color color)
    {
        int minX = Mathf.Min(start.x, end.x);
        int maxX = Mathf.Max(start.x, end.x);
        int minY = Mathf.Min(start.y, end.y);
        int maxY = Mathf.Max(start.y, end.y);
        int minZ = Mathf.Min(start.z, end.z);
        int maxZ = Mathf.Max(start.z, end.z);

        // Preenche todo o retângulo
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    Vector3Int pos = new Vector3Int(x, y, z);
                    if (CanPlaceVoxel(pos))
                        PlaceVoxel(pos, color);
                }
            }
        }
    }

    // Método para criar um retângulo 2D (apenas no plano XZ, Y constante)
    public void CreateRectangle2D(Vector3Int start, Vector3Int end, Color color)
    {
        int minX = Mathf.Min(start.x, end.x);
        int maxX = Mathf.Max(start.x, end.x);
        int minZ = Mathf.Min(start.z, end.z);
        int maxZ = Mathf.Max(start.z, end.z);
        int y = start.y; // Mantém o Y constante

        // Apenas a borda do retângulo 2D
        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                if (x == minX || x == maxX || z == minZ || z == maxZ)
                {
                    Vector3Int pos = new Vector3Int(x, y, z);
                    if (CanPlaceVoxel(pos))
                        PlaceVoxel(pos, color);
                }
            }
        }
    }

    // Método para criar um retângulo 2D preenchido
    public void CreateFilledRectangle2D(Vector3Int start, Vector3Int end, Color color)
    {
        int minX = Mathf.Min(start.x, end.x);
        int maxX = Mathf.Max(start.x, end.x);
        int minZ = Mathf.Min(start.z, end.z);
        int maxZ = Mathf.Max(start.z, end.z);
        int y = start.y;

        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                Vector3Int pos = new Vector3Int(x, y, z);
                if (CanPlaceVoxel(pos))
                    PlaceVoxel(pos, color);
            }
        }
    }

    // Variáveis para controle do retângulo
    private Vector3Int rectangleStartPos = Vector3Int.zero;
    private bool isDrawingRectangle = false;
    private bool isFilledRectangle = false; // Para alternar entre preenchido e vazio
    private List<Vector3Int> previewRectanglePositions = new List<Vector3Int>();

    // Método para iniciar o desenho do retângulo
    public void StartRectangleDrag(Vector3Int position, Color color, bool filled = false)
    {
        isDrawingRectangle = true;
        rectangleStartPos = position;
        isFilledRectangle = filled;
        // Limpa preview anterior
        ClearPreviewRectangle();
    }

    // Método para continuar o desenho do retângulo (atualiza o preview)// Adicione estas variáveis no VoxelTerrain.cs
    private Vector3Int lastPreviewPosition = Vector3Int.zero;
    private float lastPreviewUpdateTime = 0f;
    private float previewUpdateDelay = 0.1f; // Atualiza o preview 10 vezes por segundo
    private List<Vector3Int> cachedPreviewPositions = new List<Vector3Int>();

    // Adicione ao VoxelTerrain.cs
    public bool IsLargeRectangle(Vector3Int start, Vector3Int end)
    {
        int minX = Mathf.Min(start.x, end.x);
        int maxX = Mathf.Max(start.x, end.x);
        int minY = Mathf.Min(start.y, end.y);
        int maxY = Mathf.Max(start.y, end.y);
        int minZ = Mathf.Min(start.z, end.z);
        int maxZ = Mathf.Max(start.z, end.z);

        int volume = (maxX - minX + 1) * (maxY - minY + 1) * (maxZ - minZ + 1);
        return volume > 5000; // Considera grande se tiver mais de 5000 voxels
    }

    // Modifique o UpdateRectanglePreview para usar preview simplificado para retângulos grandes
    public void UpdateRectanglePreview(Vector3Int currentPos, Color color)
    {
        if (!isDrawingRectangle) return;

        if (Time.realtimeSinceStartup - lastPreviewUpdateTime < previewUpdateDelay)
            return;

        if (currentPos == lastPreviewPosition)
            return;

        lastPreviewPosition = currentPos;
        lastPreviewUpdateTime = Time.realtimeSinceStartup;

        ClearPreviewRectangle();

        int minX = Mathf.Min(rectangleStartPos.x, currentPos.x);
        int maxX = Mathf.Max(rectangleStartPos.x, currentPos.x);
        int minY = Mathf.Min(rectangleStartPos.y, currentPos.y);
        int maxY = Mathf.Max(rectangleStartPos.y, currentPos.y);
        int minZ = Mathf.Min(rectangleStartPos.z, currentPos.z);
        int maxZ = Mathf.Max(rectangleStartPos.z, currentPos.z);

        // Verifica se é grande
        bool isLarge = IsLargeRectangle(rectangleStartPos, currentPos);

        var tempPositions = new HashSet<Vector3Int>();

        if (isLarge)
        {
            // Para retângulos grandes, desenha apenas a borda externa
            // e algumas linhas internas a cada N voxels
            int step = Mathf.Max(1, Mathf.Min(maxX - minX, maxY - minY, maxZ - minZ) / 20);

            // Borda completa
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        bool isBorder = (x == minX || x == maxX ||
                                        y == minY || y == maxY ||
                                        z == minZ || z == maxZ);

                        // Para bordas, adiciona todos
                        if (isBorder)
                        {
                            Vector3Int pos = new Vector3Int(x, y, z);
                            if (CanPlaceVoxel(pos))
                                tempPositions.Add(pos);
                        }
                        // Para interior, adiciona apenas a cada N voxels
                        else if (x % step == 0 && y % step == 0 && z % step == 0)
                        {
                            Vector3Int pos = new Vector3Int(x, y, z);
                            if (CanPlaceVoxel(pos))
                                tempPositions.Add(pos);
                        }
                    }
                }
            }
        }
        else
        {
            // Retângulo normal
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        bool isBorder = (x == minX || x == maxX ||
                                        y == minY || y == maxY ||
                                        z == minZ || z == maxZ);

                        if (isFilledRectangle || isBorder)
                        {
                            Vector3Int pos = new Vector3Int(x, y, z);
                            if (CanPlaceVoxel(pos))
                                tempPositions.Add(pos);
                        }
                    }
                }
            }
        }

        cachedPreviewPositions = tempPositions.ToList();
        previewRectanglePositions = cachedPreviewPositions;
    }
    // Modifique o método GetPreviewRectanglePositions para retornar a lista cacheada
    public List<Vector3Int> GetPreviewRectanglePositions()
    {
        return previewRectanglePositions;
    }

    // Método para finalizar o retângulo
    public void FinishRectangle(Color color)
    {
        if (!isDrawingRectangle || previewRectanglePositions.Count == 0)
        {
            ClearPreviewRectangle();
            return;
        }

        // Adiciona todos os voxels do preview ao mundo
        foreach (var pos in previewRectanglePositions)
        {
            PlaceVoxel(pos, color);
        }

        ClearPreviewRectangle();
    }

    // Método para limpar o preview
    public void ClearPreviewRectangle()
    {
        previewRectanglePositions.Clear();
    }

    // Método para verificar se está desenhando um retângulo
    public bool IsDrawingRectangle()
    {
        return isDrawingRectangle;
    }

    // Método para cancelar o desenho do retângulo
    public void CancelRectangle()
    {
        isDrawingRectangle = false;
        rectangleStartPos = Vector3Int.zero;
        ClearPreviewRectangle();
    }

    // Método para alternar entre retângulo preenchido ou vazio
    public void ToggleFilledRectangle()
    {
        isFilledRectangle = !isFilledRectangle;
    }

}