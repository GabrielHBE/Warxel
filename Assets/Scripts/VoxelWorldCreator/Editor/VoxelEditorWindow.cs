using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class VoxelEditorWindow : EditorWindow
{
    private enum Mode { Add, Erase, Paint, Pick, Fill, Select }
    private enum SelectionTool { Region, Move, Rotate }
    private static readonly string[] ModeLabels = { "Adicionar [B]", "Apagar [R]", "Pintar [P]", "Conta-gotas [I]", "Preencher [G]", "Selecionar [M]" };
    private static readonly string[] ShapeLabels = { "Caixa", "Esfera", "Cilindro", "Plano XZ" };
    [SerializeField] private VoxelTerrain terrain;
    [SerializeField] private Mode mode;
    [SerializeField] private VoxelShape shape;
    [SerializeField] private Color color = new Color32(80, 180, 225, 255);
    [SerializeField] private Vector3Int size = Vector3Int.one;
    [SerializeField] private bool hollow, planeOnly, showGrid = true, clipSelection;
    [SerializeField] private int layer;
    [SerializeField] private bool hasSelection;
    [SerializeField] private Vector3Int selectionMin, selectionMax, moveOffset = Vector3Int.right;
    [SerializeField] private SelectionTool selectionTool;
    [SerializeField] private List<Vector3Int> selectedCells = new List<Vector3Int>();
    [SerializeField] private bool selectionCaptured, overwriteSelection;
    [SerializeField] private Vector3Int selectionPivot;
    private List<WorldVoxelData> transformSource, transformPreview;
    private Vector3Int previewOffset, previewTurns;
    private Vector3Int previewMin, previewMax;
    private Vector3 moveHandlePosition;
    private Quaternion rotationHandle = Quaternion.identity;
    private int rotationAxis = -1, moveAxis = -1, transformControl;
    private readonly int[] moveControlIds = new int[3];
    private bool previewValid;
    private string previewError;
    [SerializeField] private List<Color> palette = new List<Color>();
    [SerializeField] private VoxelMapAsset mapAsset;
    private bool editing, stroke, changed, hasCursor;
    private int undoGroup = -1, control;
    private Tool previousTool;
    private Vector3Int cursor, lastCell, selectionStart;
    private Plane strokePlane;
    private Vector2 scroll;
    private string status = "Crie um mapa ou escolha um VoxelTerrain na cena.";

    [MenuItem("Tools/Warxel/Voxel Studio")]
    public static void Open() => GetWindow<VoxelEditorWindow>("Voxel Studio");

    public static void Open(VoxelTerrain target)
    {
        var window = GetWindow<VoxelEditorWindow>("Voxel Studio");
        window.SetTarget(target); window.SetEditing(true); window.Show();
    }

    private void OnEnable()
    {
        minSize = new Vector2(340, 520);
        SceneView.duringSceneGui += OnSceneGUI;
        Undo.undoRedoPerformed += OnUndo;
        EditorApplication.playModeStateChanged += OnPlayMode;
        if (palette.Count == 0)
            foreach (string hex in new[] { "F2F2F2", "9CA6B0", "505C69", "202A35", "EE6464", "F2A65A", "F5D76E", "A4C969", "4FAA83", "50B4E1", "5A7BD8", "9E78CC", "D979AE", "8D6748", "C9AF86", "647D57" })
                if (ColorUtility.TryParseHtmlString("#" + hex, out var c)) palette.Add(c);
        if (!terrain && Selection.activeGameObject) terrain = Selection.activeGameObject.GetComponentInParent<VoxelTerrain>();
    }

    private void OnDisable()
    {
        SetEditing(false);
        SceneView.duringSceneGui -= OnSceneGUI;
        Undo.undoRedoPerformed -= OnUndo;
        EditorApplication.playModeStateChanged -= OnPlayMode;
    }
    private void OnPlayMode(PlayModeStateChange state) { SetEditing(false); Repaint(); }
    private void OnUndo()
    {
        CancelTransform();
        stroke = false; undoGroup = -1;
        if (GUIUtility.hotControl == control) GUIUtility.hotControl = 0;
        if (terrain) terrain.RebuildAll();
        SceneView.RepaintAll(); Repaint();
    }
    private void SetTarget(VoxelTerrain target)
    {
        CancelTransform(); selectedCells.Clear(); selectionCaptured = false;
        FinishStroke(); terrain = target; hasSelection = false; hasCursor = false;
        if (terrain && EditorUtility.IsPersistent(terrain))
        {
            terrain = null;
            status = "Abra o prefab no Prefab Mode ou instancie-o na cena antes de editar.";
        }
        if (terrain) { terrain.RebuildAll(); Selection.activeGameObject = terrain.gameObject; }
    }
    private void SetEditing(bool value)
    {
        value &= terrain && !EditorApplication.isPlayingOrWillChangePlaymode && terrain.isActiveAndEnabled;
        if (editing == value) return;
        if (value) { previousTool = Tools.current; Tools.current = Tool.None; }
        else { CancelTransform(); FinishStroke(); if (Tools.current == Tool.None) Tools.current = previousTool; }
        editing = value; SceneView.RepaintAll(); Repaint();
    }

    private void OnGUI()
    {
        if (editing && (!terrain || !terrain.isActiveAndEnabled)) SetEditing(false);
        EditorGUILayout.LabelField("VOXEL STUDIO", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Construa e pinte diretamente na Scene View.", EditorStyles.wordWrappedMiniLabel);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            var target = (VoxelTerrain)EditorGUILayout.ObjectField("Mapa da cena", terrain, typeof(VoxelTerrain), true);
            if (target != terrain) { SetEditing(false); SetTarget(target); }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Novo mapa vazio", GUILayout.Height(26))) CreateMap();
                using (new EditorGUI.DisabledScope(!terrain))
                    if (GUILayout.Button("Enquadrar", GUILayout.Height(26))) FrameMap();
            }
            if (!terrain)
            {
                EditorGUILayout.HelpBox("Use Novo mapa vazio para começar. A janela pode ficar encaixada ao lado da Scene View.", MessageType.Info);
                EditorGUILayout.LabelField(status, EditorStyles.wordWrappedMiniLabel);
                DrawAssetControls(); EditorGUILayout.EndScrollView(); return;
            }
            bool enabled = GUILayout.Toggle(editing, editing ? "EDIÇÃO ATIVA — Esc para sair" : "Ativar edição na Scene View", "Button", GUILayout.Height(32));
            if (enabled != editing) SetEditing(enabled);
            EditorGUILayout.LabelField($"{terrain.VoxelCount:N0} voxels  •  {terrain.ChunkCount:N0} chunks", EditorStyles.miniLabel);
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Ferramentas", EditorStyles.boldLabel);
            var nextMode = (Mode)GUILayout.SelectionGrid((int)mode, ModeLabels, 2, GUILayout.Height(84));
            if (nextMode != mode) { CancelTransform(); FinishStroke(); mode = nextMode; }
            color = EditorGUILayout.ColorField(new GUIContent("Cor do voxel", "Cor opaca aplicada ao adicionar, pintar ou preencher."), color, true, false, false);
            DrawPalette();
            if (mode == Mode.Add || mode == Mode.Erase || mode == Mode.Paint)
            {
                EditorGUILayout.Space(5);
                shape = (VoxelShape)GUILayout.SelectionGrid((int)shape, ShapeLabels, 2);
                size = VoxelBrush.ClampSize(EditorGUILayout.Vector3IntField("Dimensões (voxels)", size));
                hollow = EditorGUILayout.Toggle(new GUIContent("Forma oca", "Aplica somente a casca da forma; no plano, somente o contorno."), hollow);
                EditorGUILayout.HelpBox("Clique para carimbar. Arraste para traçar no plano da face inicial. [ e ] alteram o tamanho.", MessageType.None);
            }
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Plano de construção", EditorStyles.boldLabel);
            planeOnly = EditorGUILayout.Toggle(new GUIContent("Fixar no plano XZ", "Ignora superfícies e usa a camada Y indicada."), planeOnly);
            layer = Mathf.Clamp(EditorGUILayout.IntField("Camada Y", layer), -VoxelTerrain.CoordinateLimit, VoxelTerrain.CoordinateLimit);
            showGrid = EditorGUILayout.Toggle("Mostrar grade local", showGrid);
            DrawSelection();
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Configuração do mapa", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            float scale = Mathf.Max(0.01f, EditorGUILayout.FloatField("Tamanho do voxel", terrain.voxelSize));
            bool colliders = EditorGUILayout.Toggle("Colisores", terrain.generateColliders);
            var material = (Material)EditorGUILayout.ObjectField(new GUIContent("Material (opcional)", "Vazio usa o shader URP de cores de vértice incluso. Materiais comuns podem ignorar as cores."), terrain.voxelMaterial, typeof(Material), false);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RegisterCompleteObjectUndo(terrain, "Configurar mapa voxel");
                terrain.voxelSize = scale; terrain.generateColliders = colliders; terrain.voxelMaterial = material;
                terrain.RebuildAll(); MarkScene();
            }
            DrawAssetControls();
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(status, MessageType.None);
            EditorGUILayout.LabelField("Alt + mouse: navegar  •  Ctrl/Cmd+Z: desfazer\nB adicionar  •  R apagar  •  P pintar  •  I cor\nG preencher cor conectada  •  M selecionar", EditorStyles.wordWrappedMiniLabel);
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawPalette()
    {
        int columns = Mathf.Max(4, Mathf.FloorToInt((position.width - 30) / 34));
        for (int row = 0; row * columns < palette.Count; row++)
        using (new EditorGUILayout.HorizontalScope())
        {
            for (int col = 0; col < columns && row * columns + col < palette.Count; col++)
            {
                int i = row * columns + col;
                Rect rect = GUILayoutUtility.GetRect(30, 27, GUILayout.Width(30));
                EditorGUI.DrawRect(rect, palette[i]);
                Color contrast = palette[i].grayscale > 0.55f ? Color.black : Color.white;
                var style = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
                style.normal.textColor = contrast;
                if (GUI.Button(rect, new GUIContent(((Color32)color).Equals((Color32)palette[i]) ? "✓" : (i + 1).ToString(), "#" + ColorUtility.ToHtmlStringRGB(palette[i])), style)) color = palette[i];
            }
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(palette.Count >= 64))
                if (GUILayout.Button("Guardar cor na paleta")) palette.Add(color);
            using (new EditorGUI.DisabledScope(palette.Count == 0))
                if (GUILayout.Button("Remover cor")) { int i = palette.FindIndex(c => ((Color32)c).Equals((Color32)color)); if (i >= 0) palette.RemoveAt(i); }
        }
    }

    private void DrawSelection()
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Seleção de região", EditorStyles.boldLabel);
        if (mode == Mode.Select)
        {
            var next = (SelectionTool)GUILayout.Toolbar((int)selectionTool, new[] { "Região [Q]", "Mover [W]", "Girar [E]" });
            if (next != selectionTool) { CancelTransform(); FinishStroke(); selectionTool = next; SceneView.RepaintAll(); }
            EditorGUILayout.HelpBox("Ao terminar a seleção, as setas aparecem automaticamente. Arraste X (vermelho), Y (verde) ou Z (azul) para mover somente naquele eixo. E: anéis de rotação. Q: nova seleção. Esc cancela o arrasto. Rotação em passos de 90°.", MessageType.None);
        }
        if (!hasSelection) return;
        if (!selectionCaptured) CaptureSelection();
        EditorGUI.BeginChangeCheck();
        selectionMin = ClampPosition(EditorGUILayout.Vector3IntField("Mínimo", selectionMin));
        selectionMax = Vector3Int.Max(selectionMin, ClampPosition(EditorGUILayout.Vector3IntField("Máximo", selectionMax)));
        if (EditorGUI.EndChangeCheck()) { CancelTransform(); CaptureSelection(); SceneView.RepaintAll(); }
        EditorGUILayout.LabelField($"{selectedCells.Count:N0} voxels selecionados • pivô {selectionPivot}", EditorStyles.wordWrappedMiniLabel);
        clipSelection = EditorGUILayout.Toggle("Limitar pincel à seleção", clipSelection);
        overwriteSelection = EditorGUILayout.Toggle(new GUIContent("Substituir no destino", "Permite substituir voxels que não pertencem à seleção. Desativado bloqueia a transformação inteira se houver colisão."), overwriteSelection);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Pintar região")) EditSelection(false);
            if (GUILayout.Button("Apagar região")) EditSelection(true);
            if (GUILayout.Button("Desmarcar")) { CancelTransform(); hasSelection = false; selectedCells.Clear(); SceneView.RepaintAll(); }
        }
        moveOffset = ClampPosition(EditorGUILayout.Vector3IntField("Deslocamento", moveOffset));
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Mover seleção")) MoveSelection(false);
            if (GUILayout.Button("Duplicar seleção")) MoveSelection(true);
        }
        for (int axis = 0; axis < 3; axis++)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                string label = axis == 0 ? "X" : axis == 1 ? "Y" : "Z";
                EditorGUILayout.LabelField("Girar em " + label, GUILayout.Width(80));
                if (GUILayout.Button("−90°")) RotateSelection(axis, -1);
                if (GUILayout.Button("+90°")) RotateSelection(axis, 1);
            }
        }
    }

    private static Vector3Int ClampPosition(Vector3Int p) => new Vector3Int(Mathf.Clamp(p.x, -32767, 32767), Mathf.Clamp(p.y, -32767, 32767), Mathf.Clamp(p.z, -32767, 32767));
    private bool InSelection(Vector3Int p) => p.x >= selectionMin.x && p.y >= selectionMin.y && p.z >= selectionMin.z && p.x <= selectionMax.x && p.y <= selectionMax.y && p.z <= selectionMax.z;
    private List<WorldVoxelData> Selected()
    {
        if (!selectionCaptured) CaptureSelection();
        var result = new List<WorldVoxelData>();
        foreach (var p in selectedCells)
            if (terrain.TryGetVoxel(p, out var c)) result.Add(new WorldVoxelData(p, c));
        return result;
    }
    private void CaptureSelection()
    {
        selectedCells.Clear();
        foreach (var v in terrain.Voxels) if (InSelection(v.position)) selectedCells.Add(v.position);
        // A cell-centered pivot keeps every rotation exact, including even-sized selections.
        selectionPivot = selectionMin + (selectionMax - selectionMin) / 2;
        selectionCaptured = true;
    }
    private void EditSelection(bool erase)
    {
        CancelTransform();
        var selected = Selected();
        if (selected.Count == 0) return;
        BeginChange("Editar seleção voxel");
        Undo.RegisterCompleteObjectUndo(this, "Editar seleção voxel");
        foreach (var v in selected) changed |= erase ? terrain.EraseVoxel(v.position) : terrain.SetVoxel(v.position, color, true);
        if (erase) { selectedCells.Clear(); hasSelection = false; }
        FinishStroke();
    }
    private void MoveSelection(bool duplicate)
    {
        CancelTransform();
        if (moveOffset == Vector3Int.zero) return;
        ApplySelectionTransform(Selected(), moveOffset, Vector3Int.zero, duplicate);
    }

    private void RotateSelection(int axis, int turns)
    {
        CancelTransform();
        var rotation = Vector3Int.zero; rotation[axis] = turns;
        ApplySelectionTransform(Selected(), Vector3Int.zero, rotation, false);
    }

    private void ApplySelectionTransform(List<WorldVoxelData> source, Vector3Int offset, Vector3Int turns, bool duplicate)
    {
        if (!VoxelSelectionTransform.TryPlan(terrain, source, selectionPivot, offset, turns, overwriteSelection, out var destination, out var error))
        { status = error; Repaint(); return; }
        if (offset == Vector3Int.zero && turns.x % 4 == 0 && turns.y % 4 == 0 && turns.z % 4 == 0) return;
        string name = duplicate ? "Duplicar voxels" : turns != Vector3Int.zero ? "Rotacionar voxels" : "Mover voxels";
        BeginChange(name);
        Undo.RegisterCompleteObjectUndo(this, name);
        VoxelSelectionTransform.Apply(terrain, source, destination, duplicate);
        changed = true;
        selectedCells.Clear();
        selectionMin = selectionMax = destination[0].position;
        foreach (var v in destination)
        {
            selectedCells.Add(v.position);
            selectionMin = Vector3Int.Min(selectionMin, v.position);
            selectionMax = Vector3Int.Max(selectionMax, v.position);
        }
        selectionPivot += offset;
        hasSelection = selectionCaptured = true;
        FinishStroke(); status = name + ": " + destination.Count + " voxels.";
        Repaint();
    }

    private void CancelTransform()
    {
        transformSource = transformPreview = null;
        previewOffset = previewTurns = Vector3Int.zero;
        rotationHandle = Quaternion.identity; rotationAxis = moveAxis = -1;
        if (transformControl != 0 && GUIUtility.hotControl == transformControl) GUIUtility.hotControl = 0;
        transformControl = 0;
        SceneView.RepaintAll();
    }

    private void UpdateTransformPreview()
    {
        previewValid = VoxelSelectionTransform.TryPlan(terrain, transformSource, selectionPivot,
            previewOffset, previewTurns, overwriteSelection, out transformPreview, out previewError);
        if (transformPreview.Count > 0)
        {
            previewMin = previewMax = transformPreview[0].position;
            foreach (var v in transformPreview)
            {
                previewMin = Vector3Int.Min(previewMin, v.position);
                previewMax = Vector3Int.Max(previewMax, v.position);
            }
        }
        status = previewValid ? "Prévia: solte o mouse para aplicar; Esc cancela." : previewError;
        Repaint(); SceneView.RepaintAll();
    }

    private void DrawSelectionHandles()
    {
        if (mode != Mode.Select || selectionTool == SelectionTool.Region || !hasSelection || stroke) return;
        if (!selectionCaptured) CaptureSelection();
        if (selectedCells.Count == 0) return;
        // Selection gizmos stay visible even when their pivot is inside solid voxels.
        var previousDepthTest = Handles.zTest;
        using (new Handles.DrawingScope(Matrix4x4.identity))
        {
            try
            {
                Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
                DrawTransformGizmo();
            }
            finally { Handles.zTest = previousDepthTest; }
        }
    }

    private void DrawTransformGizmo()
    {
        Vector3 pivot = terrain.GridToWorld(selectionPivot);
        bool mouseUp = Event.current.rawType == EventType.MouseUp && Event.current.button == 0;
        if (selectionTool == SelectionTool.Move)
        {
            if (transformSource == null) moveHandlePosition = pivot;
            float handleSize = HandleUtility.GetHandleSize(moveHandlePosition);
            for (int axis = 0; axis < 3; axis++)
            {
                Vector3 localAxis = axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward;
                Vector3 direction = terrain.transform.TransformVector(localAxis).normalized;
                moveControlIds[axis] = GUIUtility.GetControlID("VoxelSelectionMoveAxis".GetHashCode() + axis, FocusType.Passive);
                using (new Handles.DrawingScope(axis == 0 ? Handles.xAxisColor : axis == 1 ? Handles.yAxisColor : Handles.zAxisColor))
                {
                    EditorGUI.BeginChangeCheck();
                    Vector3 next = Handles.Slider(moveControlIds[axis], moveHandlePosition, direction, handleSize, Handles.ArrowHandleCap, 0f);
                    if (EditorGUI.EndChangeCheck() && (moveAxis < 0 || moveAxis == axis))
                    {
                        if (transformSource == null)
                        {
                            transformSource = Selected(); transformControl = GUIUtility.hotControl; moveAxis = axis;
                        }
                        moveHandlePosition = next;
                        var offset = AxisMoveOffset(terrain.transform.InverseTransformVector(next - pivot) / terrain.voxelSize, axis);
                        if (transformPreview == null || previewOffset != offset)
                        { previewOffset = offset; UpdateTransformPreview(); }
                    }
                    if (Event.current.type == EventType.Repaint)
                        Handles.Label(moveHandlePosition + direction * handleSize * 1.12f, axis == 0 ? "X" : axis == 1 ? "Y" : "Z", EditorStyles.boldLabel);
                }
            }
        }
        else
        {
            float radius = HandleUtility.GetHandleSize(pivot) * 0.9f;
            for (int axis = 0; axis < 3; axis++)
            {
                Vector3 localAxis = axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward;
                Vector3 worldAxis = terrain.transform.rotation * localAxis;
                using (new Handles.DrawingScope(axis == 0 ? Handles.xAxisColor : axis == 1 ? Handles.yAxisColor : Handles.zAxisColor))
                {
                    EditorGUI.BeginChangeCheck();
                    var next = Handles.Disc(rotationAxis == axis ? rotationHandle : Quaternion.identity, pivot, worldAxis, radius, false, 90f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (transformSource == null) { transformSource = Selected(); transformControl = GUIUtility.hotControl; }
                        rotationAxis = axis; rotationHandle = next;
                        next.ToAngleAxis(out float angle, out var direction);
                        var turns = Vector3Int.zero;
                        turns[axis] = Mathf.RoundToInt(angle / 90f) * (Vector3.Dot(direction, worldAxis) < 0 ? -1 : 1);
                        if (transformPreview == null || previewTurns != turns)
                        { previewTurns = turns; UpdateTransformPreview(); }
                    }
                }
            }
        }
        if (GUIUtility.hotControl != 0 && GUIUtility.hotControl != control) transformControl = GUIUtility.hotControl;
        if (mouseUp && transformSource != null)
        {
            var source = transformSource; var offset = previewOffset; var turns = previewTurns;
            CancelTransform();
            ApplySelectionTransform(source, offset, turns, false);
        }
        else if (mouseUp) transformControl = 0;
    }

    internal static Vector3Int AxisMoveOffset(Vector3 localDelta, int axis)
    {
        // Quantize only the dragged axis. Transform round-off cannot move another coordinate.
        var offset = Vector3Int.zero;
        offset[axis] = Mathf.RoundToInt(Mathf.Clamp(localDelta[axis], -65534, 65534));
        return offset;
    }

    private void CompleteRegionSelection()
    {
        CaptureSelection();
        if (selectedCells.Count == 0) { status = "A região não contém voxels. Selecione uma região ocupada."; return; }
        selectionTool = SelectionTool.Move;
        Tools.current = Tool.None;
        status = "Arraste uma seta X/Y/Z para mover a seleção. E gira; Q seleciona outra região.";
    }

    private void DrawAssetControls()
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Biblioteca de mapas", EditorStyles.boldLabel);
        mapAsset = (VoxelMapAsset)EditorGUILayout.ObjectField("Asset do mapa", mapAsset, typeof(VoxelMapAsset), false);
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(!terrain))
                if (GUILayout.Button("Salvar cópia .asset")) SaveAsset();
            using (new EditorGUI.DisabledScope(!mapAsset))
                if (GUILayout.Button("Abrir como novo mapa"))
                {
                    CreateMap();
                    terrain.voxelMaterial = mapAsset.material;
                    terrain.ReplaceVoxels(mapAsset.voxels, mapAsset.voxelSize);
                    MarkScene(); FrameMap();
                }
        }
    }
    private void SaveAsset()
    {
        FinishStroke();
        string path = EditorUtility.SaveFilePanelInProject("Salvar mapa voxel", "VoxelMap", "asset", "Escolha o destino da cópia do mapa.");
        if (string.IsNullOrEmpty(path)) return;
        path = AssetDatabase.GenerateUniqueAssetPath(path);
        var asset = CreateInstance<VoxelMapAsset>(); asset.voxelSize = terrain.voxelSize; asset.material = terrain.voxelMaterial;
        foreach (var v in terrain.Voxels) asset.voxels.Add(new WorldVoxelData(v.position, v.color));
        AssetDatabase.CreateAsset(asset, path); AssetDatabase.SaveAssets(); mapAsset = asset;
        status = "Mapa salvo em " + path; EditorGUIUtility.PingObject(asset);
    }
    private void CreateMap()
    {
        SetEditing(false);
        var go = new GameObject("Voxel Map");
        Undo.RegisterCreatedObjectUndo(go, "Criar mapa voxel");
        SetTarget(Undo.AddComponent<VoxelTerrain>(go));
        SetEditing(true); MarkScene(); FrameMap();
        status = "Mapa criado. Clique na grade da Scene View para adicionar voxels.";
    }
    private void FrameMap()
    {
        if (!terrain) return;
        var bounds = new Bounds(terrain.transform.position, Vector3.one * terrain.voxelSize * 12);
        foreach (var v in terrain.Voxels) bounds.Encapsulate(terrain.GridToWorld(v.position));
        var view = SceneView.lastActiveSceneView;
        if (!view) view = GetWindow<SceneView>();
        view.Frame(bounds, false);
    }

    private void BeginChange(string name)
    {
        FinishStroke();
        Undo.IncrementCurrentGroup(); undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(name); Undo.RegisterCompleteObjectUndo(terrain, name);
        changed = false;
    }
    private void FinishStroke()
    {
        if (terrain && (stroke || undoGroup >= 0))
        {
            terrain.Flush();
            if (changed) MarkScene();
        }
        if (undoGroup >= 0) Undo.CollapseUndoOperations(undoGroup);
        undoGroup = -1; stroke = false; changed = false;
        if (control != 0 && GUIUtility.hotControl == control) GUIUtility.hotControl = 0;
        SceneView.RepaintAll();
    }
    private void MarkScene()
    {
        EditorUtility.SetDirty(terrain);
        PrefabUtility.RecordPrefabInstancePropertyModifications(terrain);
        if (terrain.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(terrain.gameObject.scene);
    }

    private void OnSceneGUI(SceneView view)
    {
        if (editing && !terrain) SetEditing(false);
        if (!editing || !terrain || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!terrain.isActiveAndEnabled) { SetEditing(false); return; }
        var e = Event.current;
        control = GUIUtility.GetControlID("WarxelVoxelStudio".GetHashCode(), FocusType.Passive);
        if (e.type == EventType.Layout && !e.alt) HandleUtility.AddDefaultControl(control);
        if (e.rawType == EventType.MouseUp && e.button == 0 && stroke)
        {
            if (mode == Mode.Select) CompleteRegionSelection();
            FinishStroke(); e.Use(); Repaint();
        }
        if (e.type == EventType.KeyDown && !e.alt && !e.control && !e.command && !EditorGUIUtility.editingTextField)
        {
            bool used = true;
            switch (e.keyCode)
            {
                case KeyCode.B: CancelTransform(); FinishStroke(); mode = Mode.Add; break;
                case KeyCode.R: CancelTransform(); FinishStroke(); mode = Mode.Erase; break;
                case KeyCode.P: CancelTransform(); FinishStroke(); mode = Mode.Paint; break;
                case KeyCode.I: CancelTransform(); FinishStroke(); mode = Mode.Pick; break;
                case KeyCode.G: CancelTransform(); FinishStroke(); mode = Mode.Fill; break;
                case KeyCode.M:
                case KeyCode.Q: CancelTransform(); FinishStroke(); mode = Mode.Select; selectionTool = SelectionTool.Region; Tools.current = Tool.None; break;
                case KeyCode.W: CancelTransform(); FinishStroke(); mode = Mode.Select; selectionTool = SelectionTool.Move; Tools.current = Tool.None; break;
                case KeyCode.E: CancelTransform(); FinishStroke(); mode = Mode.Select; selectionTool = SelectionTool.Rotate; Tools.current = Tool.None; break;
                case KeyCode.LeftBracket: size = VoxelBrush.ClampSize(size - Vector3Int.one); break;
                case KeyCode.RightBracket: size = VoxelBrush.ClampSize(size + Vector3Int.one); break;
                case KeyCode.Escape:
                    if (transformSource != null) { CancelTransform(); status = "Transformação cancelada."; }
                    else SetEditing(false);
                    break;
                default: used = false; break;
            }
            if (used) { e.Use(); Repaint(); view.Repaint(); }
        }
        if (!editing) return;
        // Interactive handles must receive Layout and mouse events, not only Repaint.
        DrawSelectionHandles();
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        hasCursor = TryCursor(ray, out cursor, out var normal);
        if (e.type == EventType.Repaint) DrawScene(view);
        if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag) view.Repaint();
        if (mode == Mode.Select && selectionTool != SelectionTool.Region) return;
        if (e.alt || e.button != 0 || !hasCursor) return;
        if (e.type == EventType.MouseDown && HandleUtility.nearestControl == control)
        {
            if (mode == Mode.Pick)
            {
                if (terrain.TryGetVoxel(cursor, out var sampled)) { color = sampled; status = "Cor selecionada: #" + ColorUtility.ToHtmlStringRGB(color); }
            }
            else if (mode == Mode.Fill) Fill(cursor);
            else
            {
                if (mode != Mode.Select) BeginChange("Traço voxel: " + ModeLabels[(int)mode]);
                stroke = true; GUIUtility.hotControl = control; lastCell = cursor;
                var worldNormal = terrain.transform.worldToLocalMatrix.transpose.MultiplyVector(normal).normalized;
                strokePlane = new Plane(worldNormal, terrain.GridToWorld(cursor));
                if (mode == Mode.Select)
                {
                    selectionStart = cursor; selectionMin = selectionMax = cursor; hasSelection = true;
                    selectedCells.Clear(); selectionCaptured = false;
                }
                else { Stamp(cursor); terrain.Flush(false); }
            }
            e.Use(); Repaint(); view.Repaint();
        }
        else if (e.type == EventType.MouseDrag && stroke && GUIUtility.hotControl == control)
        {
            if (mode == Mode.Select) { selectionMin = Vector3Int.Min(selectionStart, cursor); selectionMax = Vector3Int.Max(selectionStart, cursor); }
            else if (lastCell != cursor)
            {
                long distance = Math.Max(Math.Abs(cursor.x - lastCell.x), Math.Max(Math.Abs(cursor.y - lastCell.y), Math.Abs(cursor.z - lastCell.z))) + 1L;
                long volume = (long)size.x * (shape == VoxelShape.Plane ? 1 : size.y) * size.z;
                if (distance * volume <= VoxelBrush.MaxOperationVoxels)
                    foreach (var p in VoxelBrush.Line(lastCell, cursor)) Stamp(p);
                else { Stamp(cursor); status = "Salto grande: aplicado somente o carimbo final para manter a edição responsiva."; }
                terrain.Flush(false);
            }
            lastCell = cursor; e.Use(); Repaint();
        }
    }

    private bool TryCursor(Ray ray, out Vector3Int cell, out Vector3Int normal)
    {
        normal = Vector3Int.up; cell = default;
        if (stroke)
        {
            if (!strokePlane.Raycast(ray, out float t)) return false;
            cell = terrain.WorldToGrid(ray.GetPoint(t)); return VoxelTerrain.IsValidPosition(cell);
        }
        if (!planeOnly && terrain.Raycast(ray, out cell, out normal))
        {
            if (mode == Mode.Add) cell += normal;
            return VoxelTerrain.IsValidPosition(cell);
        }
        normal = Vector3Int.up;
        var worldNormal = terrain.transform.worldToLocalMatrix.transpose.MultiplyVector(Vector3.up).normalized;
        var plane = new Plane(worldNormal, terrain.GridToWorld(new Vector3Int(0, layer, 0)));
        if (!plane.Raycast(ray, out float distance)) return false;
        cell = terrain.WorldToGrid(ray.GetPoint(distance)); cell.y = layer;
        return VoxelTerrain.IsValidPosition(cell);
    }
    private void Stamp(Vector3Int center)
    {
        foreach (var p in VoxelBrush.Cells(center, size, shape, hollow))
        {
            if (clipSelection && hasSelection && !InSelection(p)) continue;
            if (mode == Mode.Erase) changed |= terrain.EraseVoxel(p);
            else changed |= terrain.SetVoxel(p, color, mode == Mode.Paint);
        }
    }
    private void Fill(Vector3Int seed)
    {
        if (!terrain.TryGetVoxel(seed, out var original) || original.Equals((Color32)color)) return;
        var cells = new List<Vector3Int>();
        if (!VoxelBrush.CollectConnected(terrain, seed, cells, clipSelection && hasSelection ? InSelection : (Predicate<Vector3Int>)null))
        { status = "Preenchimento excedeu 262.144 voxels; ative Limitar pincel à seleção em uma região menor."; return; }
        BeginChange("Preencher cor conectada");
        foreach (var p in cells)
            if (!clipSelection || !hasSelection || InSelection(p)) changed |= terrain.SetVoxel(p, color, true);
        FinishStroke();
    }

    private void DrawScene(SceneView view)
    {
        using (new Handles.DrawingScope(terrain.transform.localToWorldMatrix))
        {
            float unit = terrain.voxelSize;
            if (showGrid)
            {
                Handles.color = new Color(0.5f, 0.65f, 0.8f, 0.3f);
                int cx = hasCursor ? cursor.x : 0, cz = hasCursor ? cursor.z : 0;
                float y = (layer - 0.5f) * unit;
                for (int i = -20; i <= 20; i++)
                {
                    Handles.DrawLine(new Vector3(cx + i - 0.5f, 0, cz - 20.5f) * unit + Vector3.up * y, new Vector3(cx + i - 0.5f, 0, cz + 19.5f) * unit + Vector3.up * y);
                    Handles.DrawLine(new Vector3(cx - 20.5f, 0, cz + i - 0.5f) * unit + Vector3.up * y, new Vector3(cx + 19.5f, 0, cz + i - 0.5f) * unit + Vector3.up * y);
                }
            }
            if (hasSelection)
            {
                Handles.color = new Color(1, 0.8f, 0.15f);
                Handles.DrawWireCube(((Vector3)selectionMin + selectionMax) * (unit * 0.5f), (Vector3)(selectionMax - selectionMin + Vector3Int.one) * unit);
                if (!stroke && selectedCells.Count <= 512)
                    foreach (var p in selectedCells)
                        if (terrain.TryGetVoxel(p, out _)) Handles.DrawWireCube((Vector3)p * unit, Vector3.one * unit * 1.01f);
                if (!stroke && mode == Mode.Select && selectionTool != SelectionTool.Region)
                    Handles.DrawWireCube((Vector3)selectionPivot * unit, Vector3.one * unit * 0.25f);
            }
            if (transformPreview != null && transformPreview.Count > 0)
            {
                Handles.color = previewValid ? new Color(0.2f, 1, 0.75f) : new Color(1, 0.25f, 0.25f);
                if (transformPreview.Count <= 512)
                    foreach (var v in transformPreview) Handles.DrawWireCube((Vector3)v.position * unit, Vector3.one * unit);
                Handles.DrawWireCube(((Vector3)previewMin + previewMax) * (unit * 0.5f), (Vector3)(previewMax - previewMin + Vector3Int.one) * unit);
            }
            if (hasCursor && !(mode == Mode.Select && selectionTool != SelectionTool.Region))
            {
                Handles.color = mode == Mode.Erase ? new Color(1, 0.3f, 0.3f) : new Color(0.25f, 0.9f, 1);
                Vector3Int previewSize = mode == Mode.Add || mode == Mode.Paint || mode == Mode.Erase ? size : Vector3Int.one;
                if (shape == VoxelShape.Plane) previewSize.y = 1;
                if ((long)previewSize.x * previewSize.y * previewSize.z <= 512)
                {
                    foreach (var p in VoxelBrush.Cells(cursor, previewSize, shape, hollow))
                        Handles.DrawWireCube((Vector3)p * unit, Vector3.one * unit);
                }
                else
                {
                    var min = cursor - new Vector3Int((previewSize.x - 1) / 2, (previewSize.y - 1) / 2, (previewSize.z - 1) / 2);
                    Handles.DrawWireCube(((Vector3)min + (Vector3)(previewSize - Vector3Int.one) * 0.5f) * unit, (Vector3)previewSize * unit);
                }
            }
        }
        Handles.BeginGUI();
        try
        {
            // DrawScene runs only on Repaint: fixed rectangles do not require a GUILayout Layout pass.
            GUI.Box(new Rect(12, 12, 320, 48), GUIContent.none, EditorStyles.helpBox);
            GUI.Label(new Rect(18, 16, 308, 20), "Voxel Studio • " + ModeLabels[(int)mode], EditorStyles.boldLabel);
            GUI.Label(new Rect(18, 36, 308, 18), hasCursor ? $"X {cursor.x}   Y {cursor.y}   Z {cursor.z}  •  Esc: sair" : "Aponte para o mapa ou para a grade.", EditorStyles.miniLabel);
        }
        finally
        {
            Handles.EndGUI();
        }
    }
}
