using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(VoxelTerrain))]
public class VoxelEditor : Editor
{
    VoxelTerrain terrain;
    Color currentColor = Color.red;
    bool paintMode = false;
    bool removeMode = false;
    bool selectMode = false;
    bool placeMode = false;


    enum ShapeType { Box, FilledBox, Circle, Sphere, Cylinder }
    ShapeType currentShape = ShapeType.Box;
    Vector3Int shapeSize = new Vector3Int(3, 3, 3);
    int shapeRadius = 3;
    int shapeHeight = 5;

    void OnEnable()
    {
        terrain = (VoxelTerrain)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Voxel Tools", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        currentColor = EditorGUILayout.ColorField("Current Color", currentColor);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();

        // Botão Place (novo)
        GUI.color = placeMode ? Color.cyan : Color.white;
        if (GUILayout.Button("Place Mode", GUILayout.Height(30)))
        {
            placeMode = true;
            paintMode = false;
            removeMode = false;
            selectMode = false;
            terrain.isSelecting = false;
        }

        // Botão Paint (modificado)
        GUI.color = paintMode ? Color.green : Color.white;
        if (GUILayout.Button("Paint Mode", GUILayout.Height(30)))
        {
            paintMode = true;
            placeMode = false;
            removeMode = false;
            selectMode = false;
            terrain.isSelecting = false;
        }

        GUI.color = removeMode ? Color.red : Color.white;
        if (GUILayout.Button("Remove Mode", GUILayout.Height(30)))
        {
            removeMode = true;
            paintMode = false;
            placeMode = false;
            selectMode = false;
            terrain.isSelecting = false;
        }

        GUI.color = selectMode ? Color.blue : Color.white;
        if (GUILayout.Button("Select Mode", GUILayout.Height(30)))
        {
            selectMode = true;
            paintMode = false;
            removeMode = false;
            placeMode = false;
            terrain.isSelecting = true;
        }

        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        if (terrain.selectedVoxels.Count > 0)
        {
            EditorGUILayout.LabelField($"Selected: {terrain.selectedVoxels.Count} voxels");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Paint Selected"))
            {
                terrain.PaintSelectedVoxels(currentColor);
            }
            if (GUILayout.Button("Remove Selected"))
            {
                terrain.RemoveSelectedVoxels();
            }
            if (GUILayout.Button("Clear Selection"))
            {
                terrain.ClearSelection();
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Geometric Shapes", EditorStyles.boldLabel);

        currentShape = (ShapeType)EditorGUILayout.EnumPopup("Shape Type", currentShape);

        switch (currentShape)
        {
            case ShapeType.Box:
            case ShapeType.FilledBox:
                shapeSize = EditorGUILayout.Vector3IntField("Size", shapeSize);
                break;
            case ShapeType.Circle:
            case ShapeType.Sphere:
                shapeRadius = EditorGUILayout.IntField("Radius", shapeRadius);
                break;
            case ShapeType.Cylinder:
                shapeRadius = EditorGUILayout.IntField("Radius", shapeRadius);
                shapeHeight = EditorGUILayout.IntField("Height", shapeHeight);
                break;
        }

        if (GUILayout.Button("Create Shape", GUILayout.Height(30)))
        {
            CreateShape();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Clear All Voxels", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Clear All Voxels",
                "Are you sure you want to clear all voxels?", "Yes", "No"))
            {
                terrain.voxels.Clear();
                terrain.selectedVoxels.Clear();
                terrain.UpdateMesh();
            }
        }

        if (GUILayout.Button("Refresh Mesh", GUILayout.Height(30)))
        {
            terrain.UpdateMesh();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Total Voxels: {terrain.voxels.Count}", EditorStyles.boldLabel);

        SceneView.RepaintAll();
    }

    void CreateShape()
    {
        Vector3Int center = GetPlacementPosition();

        switch (currentShape)
        {
            case ShapeType.Box:
                terrain.CreateBox(center, shapeSize, currentColor);
                break;
            case ShapeType.FilledBox:
                terrain.CreateFilledBox(center, shapeSize, currentColor);
                break;
            case ShapeType.Circle:
                terrain.CreateCircle2D(center, shapeRadius, currentColor);
                break;
            case ShapeType.Sphere:
                terrain.CreateSphere(center, shapeRadius, currentColor);
                break;
            case ShapeType.Cylinder:
                terrain.CreateCylinder(center, shapeRadius, shapeHeight, currentColor);
                break;
        }
    }

    Vector3Int GetPlacementPosition()
    {
        if (Event.current != null)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 1000f))
            {
                if (hit.collider.gameObject == terrain.gameObject)
                {
                    return GetVoxelPositionFromHit(hit);
                }
            }

            Plane plane = new Plane(Vector3.up, terrain.transform.position);
            float distance;
            if (plane.Raycast(ray, out distance))
            {
                Vector3 point = ray.GetPoint(distance);
                return terrain.WorldToGrid(point);
            }
        }

        return Vector3Int.zero;
    }
    void OnSceneGUI()
    {
        SwitchModesByKeys();

        // Desenha seleções
        if (terrain.selectedVoxels.Count > 0)
        {
            Handles.color = Color.blue;
            foreach (var pos in terrain.selectedVoxels)
            {
                Vector3 selectedWorldPos = terrain.GridToWorld(pos);
                Handles.DrawWireCube(selectedWorldPos, Vector3.one * terrain.voxelSize);
            }
        }

        if (Event.current == null) return;

        // Obtém o ray do mouse
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        RaycastHit hit;
        bool hasHit = Physics.Raycast(ray, out hit, 1000f);
        // ========== MODO PLACE ==========
        if (placeMode)
        {
            Vector3Int targetPos = Vector3Int.zero;
            Vector3 targetWorldPos = Vector3.zero;
            bool canPlace = false;

            if (hasHit && hit.collider.gameObject == terrain.gameObject)
            {
                targetPos = GetVoxelPositionFromHit(hit);
                targetWorldPos = terrain.GridToWorld(targetPos);
                canPlace = terrain.CanPlaceVoxel(targetPos);
            }
            else
            {
                // Fallback: coloca no chão (Y=0)
                Plane plane = new Plane(Vector3.up, terrain.transform.position);
                float distance;
                if (plane.Raycast(ray, out distance))
                {
                    Vector3 point = ray.GetPoint(distance);
                    targetPos = terrain.WorldToGrid(point);
                    targetPos.y = 0;
                    targetWorldPos = terrain.GridToWorld(targetPos);
                    canPlace = terrain.CanPlaceVoxel(targetPos);
                }
            }

            // Verifica se o Shift está pressionado
            bool shiftPressed = Event.current.shift;

            // ===== DESENHO DO RETÂNGULO COM SHIFT =====
            if (shiftPressed)
            {
                if (canPlace && targetPos != Vector3Int.zero)
                {
                    // Atualiza o preview do retângulo
                    if (terrain.IsDrawingRectangle())
                    {
                        terrain.UpdateRectanglePreview(targetPos, currentColor);
                    }

                    // Desenha o preview do retângulo
                    DrawRectanglePreview();
                }

                // Inicia o desenho do retângulo
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && canPlace && targetPos != Vector3Int.zero)
                {
                    bool filled = false; // Você pode fazer um toggle com outra tecla, como Ctrl
                    terrain.StartRectangleDrag(targetPos, currentColor, filled);
                    Event.current.Use();
                    SceneView.RepaintAll();
                }
                else if (Event.current.type == EventType.MouseDrag && Event.current.button == 0 && canPlace && targetPos != Vector3Int.zero)
                {
                    // Atualiza o preview durante o arraste
                    terrain.UpdateRectanglePreview(targetPos, currentColor);
                    Event.current.Use();
                    SceneView.RepaintAll();
                }
                else if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
                {
                    // Finaliza o retângulo
                    if (terrain.IsDrawingRectangle())
                    {
                        terrain.FinishRectangle(currentColor);
                        terrain.CancelRectangle();
                        Event.current.Use();
                        SceneView.RepaintAll();
                    }
                }
            }
            else
            {
                // ===== MODO PLACE NORMAL (sem Shift) =====
                // Desenha o preview normal
                if (canPlace && targetPos != Vector3Int.zero)
                {
                    bool hasVoxel = terrain.IsVoxelActive(targetPos);

                    if (hasVoxel)
                    {
                        Handles.color = Color.red;
                        Handles.DrawWireCube(targetWorldPos, Vector3.one * terrain.voxelSize);
                    }
                    else
                    {
                        Handles.color = new Color(0, 1, 1, 0.3f);
                        Handles.CubeHandleCap(0, targetWorldPos, Quaternion.identity, terrain.voxelSize, EventType.Repaint);
                        Handles.color = Color.cyan;
                        Handles.DrawWireCube(targetWorldPos, Vector3.one * terrain.voxelSize);
                    }
                }

                // Lógica de arraste normal
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && canPlace && targetPos != Vector3Int.zero)
                {
                    terrain.StartPlacementDrag(targetPos, currentColor);
                    Event.current.Use();
                    SceneView.RepaintAll();
                }
                else if (Event.current.type == EventType.MouseDrag && Event.current.button == 0 && canPlace && targetPos != Vector3Int.zero)
                {
                    terrain.ContinuePlacementDrag(targetPos, currentColor);
                    Event.current.Use();
                    SceneView.RepaintAll();
                }
                else if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
                {
                    terrain.EndPlacementDrag();
                    Event.current.Use();
                    SceneView.RepaintAll();
                }
            }
        }
        // ========== MODO PAINT ==========
        else if (paintMode)
        {
            if (hasHit && hit.collider.gameObject == terrain.gameObject)
            {
                Vector3Int gridPos = GetVoxelPositionFromHit(hit);
                bool hasVoxel = terrain.IsVoxelActive(gridPos);
                Vector3 paintWorldPos = terrain.GridToWorld(gridPos);

                Handles.color = hasVoxel ? currentColor : Color.gray;
                Handles.DrawWireCube(paintWorldPos, Vector3.one * terrain.voxelSize);

                if (hasVoxel)
                {
                    Handles.color = new Color(currentColor.r, currentColor.g, currentColor.b, 0.3f);
                    Handles.CubeHandleCap(0, paintWorldPos, Quaternion.identity, terrain.voxelSize, EventType.Repaint);
                }

                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && hasVoxel)
                {
                    terrain.PaintVoxel(gridPos, currentColor);
                    Event.current.Use();
                    SceneView.RepaintAll();
                }
            }
        }

        // ========== MODO REMOVE ==========
        else if (removeMode)
        {
            if (hasHit && hit.collider.gameObject == terrain.gameObject)
            {
                Vector3Int gridPos = GetVoxelPositionFromHit(hit);
                bool hasVoxel = terrain.IsVoxelActive(gridPos);
                Vector3 removeWorldPos = terrain.GridToWorld(gridPos);

                Handles.color = hasVoxel ? Color.red : Color.gray;
                Handles.DrawWireCube(removeWorldPos, Vector3.one * terrain.voxelSize);

                if (hasVoxel)
                {
                    Handles.color = new Color(1, 0, 0, 0.3f);
                    Handles.CubeHandleCap(0, removeWorldPos, Quaternion.identity, terrain.voxelSize, EventType.Repaint);
                }

                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && hasVoxel)
                {
                    terrain.RemoveVoxel(gridPos);
                    Event.current.Use();
                    SceneView.RepaintAll();
                }
            }
        }

        // ========== MODO SELECT ==========
        else if (selectMode)
        {
            if (hasHit && hit.collider.gameObject == terrain.gameObject)
            {
                Vector3Int gridPos = GetVoxelPositionFromHit(hit);
                bool hasVoxel = terrain.IsVoxelActive(gridPos);
                bool isSelected = terrain.selectedVoxels.Contains(gridPos);
                Vector3 selectWorldPos = terrain.GridToWorld(gridPos);

                Handles.color = isSelected ? Color.green : Color.blue;
                Handles.DrawWireCube(selectWorldPos, Vector3.one * terrain.voxelSize);

                if (hasVoxel)
                {
                    Handles.color = new Color(0, 0, 1, 0.2f);
                    Handles.CubeHandleCap(0, selectWorldPos, Quaternion.identity, terrain.voxelSize, EventType.Repaint);
                }

                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && hasVoxel)
                {
                    if (!terrain.selectedVoxels.Contains(gridPos))
                    {
                        terrain.selectedVoxels.Add(gridPos);
                    }
                    else
                    {
                        terrain.selectedVoxels.Remove(gridPos);
                    }
                    Event.current.Use();
                    SceneView.RepaintAll();
                }
            }
        }

        // Força o repaint da cena para atualizar o preview
        if (Event.current.type == EventType.MouseMove)
        {
            SceneView.RepaintAll();
        }
    }

    // Método auxiliar para criar textura para o GUI
    private Texture2D MakeTexture(int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; ++i)
        {
            pixels[i] = color;
        }
        Texture2D texture = new Texture2D(width, height);
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
    // ========== MÉTODO DEFINITIVO - COM DETECÇÃO ROBUSTA ==========
    Vector3Int GetVoxelPositionFromHit(RaycastHit hit)
    {
        float size = terrain.voxelSize;
        float halfSize = size * 0.5f;

        // Converte para coordenadas locais
        Vector3 localHit = terrain.transform.InverseTransformPoint(hit.point);
        Vector3 localNormal = terrain.transform.InverseTransformDirection(hit.normal).normalized;

        // ===== MÉTODO 1: Tenta encontrar o voxel usando o centro =====
        Vector3 voxelCenter = localHit - localNormal * halfSize;

        int gridX = Mathf.RoundToInt(voxelCenter.x / size);
        int gridY = Mathf.RoundToInt(voxelCenter.y / size);
        int gridZ = Mathf.RoundToInt(voxelCenter.z / size);
        Vector3Int clickedPos = new Vector3Int(gridX, gridY, gridZ);

        // Verifica se o voxel existe
        bool hasVoxel = terrain.IsVoxelActive(clickedPos);

        // ===== MÉTODO 2: Se não encontrou, tenta com FloorToInt =====
        if (!hasVoxel)
        {
            int fx = Mathf.FloorToInt(localHit.x / size);
            int fy = Mathf.FloorToInt(localHit.y / size);
            int fz = Mathf.FloorToInt(localHit.z / size);
            Vector3Int floorPos = new Vector3Int(fx, fy, fz);

            if (terrain.IsVoxelActive(floorPos))
            {
                clickedPos = floorPos;
                hasVoxel = true;
            }
        }

        // ===== MÉTODO 3: Se ainda não encontrou, tenta os 8 vizinhos =====
        if (!hasVoxel)
        {
            // Tenta todos os vizinhos em um raio de 1
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        if (dx == 0 && dy == 0 && dz == 0) continue;

                        Vector3Int testPos = clickedPos + new Vector3Int(dx, dy, dz);
                        if (terrain.IsVoxelActive(testPos))
                        {
                            // Verifica se este voxel está próximo do ponto de impacto
                            Vector3 testWorldPos = terrain.GridToWorld(testPos);
                            if (Vector3.Distance(testWorldPos, hit.point) < size * 1.2f)
                            {
                                clickedPos = testPos;
                                hasVoxel = true;
                                break;
                            }
                        }
                    }
                    if (hasVoxel) break;
                }
                if (hasVoxel) break;
            }
        }

        // Se não for modo PLACE, retorna a posição calculada
        if (!placeMode)
            return clickedPos;

        // ===== MODO PLACE =====
        // Se NÃO tem voxel, tenta colocar no chão
        if (!hasVoxel)
        {
            if (terrain.CanPlaceVoxel(clickedPos))
                return clickedPos;

            Vector3Int groundPos = new Vector3Int(clickedPos.x, 0, clickedPos.z);
            if (terrain.CanPlaceVoxel(groundPos))
                return groundPos;

            return clickedPos;
        }

        // ===== TEM VOXEL - calcula a direção de colocação =====
        // Usa a posição relativa do mouse em relação ao centro do voxel
        Vector3 offset = localHit - new Vector3(clickedPos.x * size, clickedPos.y * size, clickedPos.z * size);

        // ===== PRIORIDADE ESPECIAL: Se o offset Y for positivo, coloca EM CIMA =====
        // Isso resolve o problema principal de empilhar
        if (offset.y > 0.1f)
        {
            Vector3Int upPos = clickedPos + Vector3Int.up;
            if (terrain.CanPlaceVoxel(upPos))
                return upPos;
        }

        // Se offset Y for negativo, coloca EMBAIXO
        if (offset.y < -0.1f)
        {
            Vector3Int downPos = clickedPos + Vector3Int.down;
            if (terrain.CanPlaceVoxel(downPos))
                return downPos;
        }

        // Direções laterais
        float absX = Mathf.Abs(offset.x);
        float absZ = Mathf.Abs(offset.z);

        if (absX > absZ && absX > 0.1f)
        {
            Vector3Int sidePos = clickedPos + new Vector3Int((offset.x > 0) ? 1 : -1, 0, 0);
            if (terrain.CanPlaceVoxel(sidePos))
                return sidePos;
        }
        else if (absZ > 0.1f)
        {
            Vector3Int sidePos = clickedPos + new Vector3Int(0, 0, (offset.z > 0) ? 1 : -1);
            if (terrain.CanPlaceVoxel(sidePos))
                return sidePos;
        }

        // ===== FALLBACK: Tenta todas as direções =====
        Vector3Int[] allDirs = new Vector3Int[]
        {
        Vector3Int.up, Vector3Int.down,
        Vector3Int.right, Vector3Int.left,
        Vector3Int.forward, Vector3Int.back
        };

        // Ordena: primeiro a direção do offset, depois as outras
        var sortedDirs = allDirs
            .Where(dir => terrain.CanPlaceVoxel(clickedPos + dir))
            .OrderByDescending(dir =>
            {
                // Calcula o alinhamento com o offset
                Vector3 dirVec = new Vector3(dir.x, dir.y, dir.z);
                return Vector3.Dot(offset.normalized, dirVec);
            })
            .ToList();

        if (sortedDirs.Count > 0)
        {
            return clickedPos + sortedDirs[0];
        }

        return clickedPos;
    }

    private void SwitchModesByKeys()
    {
        if (Event.current.type == EventType.KeyDown)
        {
            switch (Event.current.keyCode)
            {
                case KeyCode.Alpha1:
                    placeMode = true;
                    paintMode = false;
                    removeMode = false;
                    selectMode = false;
                    terrain.isSelecting = false;
                    Repaint();
                    Event.current.Use();
                    break;
                case KeyCode.Alpha2:
                    paintMode = true;
                    placeMode = false;
                    removeMode = false;
                    selectMode = false;
                    terrain.isSelecting = false;
                    Repaint();
                    Event.current.Use();
                    break;
                case KeyCode.Alpha3:
                    removeMode = true;
                    paintMode = false;
                    placeMode = false;
                    selectMode = false;
                    terrain.isSelecting = false;
                    Repaint();
                    Event.current.Use();
                    break;
                case KeyCode.Alpha4:
                    selectMode = true;
                    paintMode = false;
                    removeMode = false;
                    placeMode = false;
                    terrain.isSelecting = true;
                    Repaint();
                    Event.current.Use();
                    break;
                case KeyCode.F12:
                    // Atalho para debug - mostra info no console
                    Debug.Log("Modo debug ativado - mova o mouse sobre um voxel");
                    Event.current.Use();
                    break;
            }
        }
    }

    void DrawDebugInfo(RaycastHit hit)
    {
        if (!placeMode) return;

        float size = terrain.voxelSize;
        float halfSize = size * 0.5f;

        Vector3 localHit = terrain.transform.InverseTransformPoint(hit.point);
        Vector3 localNormal = terrain.transform.InverseTransformDirection(hit.normal).normalized;

        // Calcula a posição do voxel usando o método principal
        Vector3 voxelCenter = localHit - localNormal * halfSize;
        int gridX = Mathf.RoundToInt(voxelCenter.x / size);
        int gridY = Mathf.RoundToInt(voxelCenter.y / size);
        int gridZ = Mathf.RoundToInt(voxelCenter.z / size);
        Vector3Int pos = new Vector3Int(gridX, gridY, gridZ);

        // Calcula o offset
        Vector3 offset = localHit - new Vector3(pos.x * size, pos.y * size, pos.z * size);

        // Mostra o offset com uma linha colorida
        Vector3 worldPos = terrain.GridToWorld(pos);
        Handles.color = Color.magenta;
        Handles.DrawLine(worldPos, worldPos + terrain.transform.TransformDirection(offset) * 2f);

        // Mostra a posição alvo
        Vector3Int targetPos = GetVoxelPositionFromHit(hit);
        Vector3 targetWorldPos = terrain.GridToWorld(targetPos);

        // Calcula a diferença
        Vector3Int diff = targetPos - pos;
        string directionStr = diff == Vector3Int.up ? "⬆ CIMA" :
                              diff == Vector3Int.down ? "⬇ BAIXO" :
                              diff == Vector3Int.right ? "➡ DIREITA" :
                              diff == Vector3Int.left ? "⬅ ESQUERDA" :
                              diff == Vector3Int.forward ? "↗ FRENTE" :
                              diff == Vector3Int.back ? "↘ TRÁS" : "⏺ MESMO";

        // Desenha o preview
        if (terrain.CanPlaceVoxel(targetPos))
        {
            // Preenchimento verde semi-transparente
            Handles.color = new Color(0, 1, 0, 0.3f);
            Handles.CubeHandleCap(0, targetWorldPos, Quaternion.identity, size, EventType.Repaint);
            Handles.color = Color.green;
            Handles.DrawWireCube(targetWorldPos, Vector3.one * size);

            // Seta indicando a direção
            if (diff != Vector3Int.zero)
            {
                Vector3 arrowDir = terrain.transform.TransformDirection((Vector3)diff).normalized;
                Handles.color = Color.green;
                Handles.ArrowHandleCap(0, worldPos, Quaternion.LookRotation(arrowDir), size * 0.6f, EventType.Repaint);
            }
        }
        else
        {
            Handles.color = Color.red;
            Handles.DrawWireCube(targetWorldPos, Vector3.one * size);
        }

        // Mostra texto na tela
        Handles.BeginGUI();

        Rect bgRect = new Rect(10, 10, 420, 250);
        EditorGUI.DrawRect(bgRect, new Color(0, 0, 0, 0.85f));

        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 13;
        style.padding = new RectOffset(10, 10, 5, 5);

        string debugText =
            $"=== DEBUG ===\n" +
            $"Voxel: ({pos.x}, {pos.y}, {pos.z})\n" +
            $"Target: ({targetPos.x}, {targetPos.y}, {targetPos.z})\n" +
            $"Direção: {directionStr}\n" +
            $"Offset Y: {offset.y:F2} {(offset.y > 0.1f ? "⬆ CIMA!" : offset.y < -0.1f ? "⬇ BAIXO!" : "")}\n" +
            $"Offset: ({offset.x:F2}, {offset.y:F2}, {offset.z:F2})";

        GUI.Label(new Rect(15, 15, 400, 230), debugText, style);

        Handles.EndGUI();
    }
    // Substitua o método DrawRectanglePreview por esta versão otimizada
    void DrawRectanglePreview()
    {
        var previewPositions = terrain.GetPreviewRectanglePositions();
        if (previewPositions.Count == 0) return;

        // Limita o número de voxels desenhados para performance
        int maxDraw = 5000; // Limite máximo de voxels no preview
        int count = Mathf.Min(previewPositions.Count, maxDraw);

        // Usa cores mais eficientes
        Color fillColor = new Color(currentColor.r, currentColor.g, currentColor.b, 0.2f);
        Color wireColor = new Color(currentColor.r, currentColor.g, currentColor.b, 0.6f);

        // Desenha todos os voxels do preview
        for (int i = 0; i < count; i++)
        {
            Vector3 worldPos = terrain.GridToWorld(previewPositions[i]);

            // Preenchimento semi-transparente
            Handles.color = fillColor;
            Handles.CubeHandleCap(0, worldPos, Quaternion.identity, terrain.voxelSize * 0.9f, EventType.Repaint);

            // Borda - desenha apenas se não houver muitos voxels
            if (previewPositions.Count < 1000)
            {
                Handles.color = wireColor;
                Handles.DrawWireCube(worldPos, Vector3.one * terrain.voxelSize);
            }
        }
    }
}