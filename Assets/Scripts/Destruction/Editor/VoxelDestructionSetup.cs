using System;
using System.Collections.Generic;
using FishNet.Component.Transforming;
using FishNet.Object;
using UnityEditor;
using UnityEngine;

public class VoxelDestructionSetup : EditorWindow
{
    private enum DestructionMode { WholePiece, Fragments, Event }
    private static readonly string[] ModeNames = { "Peça inteira", "Fragmentação", "Por evento" };
    [SerializeField] private DestructionMode mode;
    [SerializeField] private float damageToDestroy = 150f;
    [SerializeField] private List<GameObject> eventTargets = new List<GameObject>();
    private Vector2 scroll;
    private string result;

    [MenuItem("Tools/Warxel/Destruction/Prepare selected hierarchy")]
    private static void OpenWindow()
    {
        var window = GetWindow<VoxelDestructionSetup>("Configurar destruição");
        window.minSize = new Vector2(410f, 330f);
        window.Show();
    }

    private void OnSelectionChange()
    {
        result = null; 
        Repaint();
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.LabelField("Configurar objetos selecionados", EditorStyles.boldLabel);
        var selected = Selection.gameObjects;
        EditorGUILayout.LabelField($"Selecionados: {selected.Length}");
        using (new EditorGUI.DisabledScope(true))
            foreach (var go in selected) EditorGUILayout.ObjectField(go, typeof(GameObject), true);

        mode = (DestructionMode)EditorGUILayout.Popup("Tipo de destruição", (int)mode, ModeNames);
        damageToDestroy = EditorGUILayout.FloatField("Dano para destruir", damageToDestroy);
        if (mode == DestructionMode.WholePiece)
            EditorGUILayout.HelpBox("O objeto selecionado permanece inteiro e passa a cair com física ao ser destruído.", MessageType.Info);
        else if (mode == DestructionMode.Fragments)
            EditorGUILayout.HelpBox("O selecionado é a peça intacta. Seus filhos com MeshFilter serão os fragmentos. Prepare essas meshes previamente; este comando não corta a mesh.", MessageType.Info);
        else
        {
            EditorGUILayout.HelpBox("O selecionado será um gatilho: ao ser destruído, cairá e também destruirá os alvos abaixo. Alvos sem destruição serão preparados como peças inteiras.", MessageType.Info);
            for (int i = 0; i < eventTargets.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                eventTargets[i] = (GameObject)EditorGUILayout.ObjectField($"Alvo {i + 1}", eventTargets[i], typeof(GameObject), true);
                if (GUILayout.Button("Remover", GUILayout.Width(70))) { eventTargets.RemoveAt(i); i--; }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("Adicionar alvo")) eventTargets.Add(null);
        }

        string error = ValidateSelection(selected);
        if (error != null) EditorGUILayout.HelpBox(error, MessageType.Warning);
        bool replacesMode = false;
        foreach (var go in selected)
            foreach (var current in go.GetComponents<VoxelDestruction>())
                if (current.GetType() != SelectedType) replacesMode = true;
        if (replacesMode)
            EditorGUILayout.HelpBox("A troca de modo substitui o componente atual. Revise referências externas ao componente substituído. A operação pode ser desfeita com Ctrl+Z.", MessageType.Info);
        using (new EditorGUI.DisabledScope(error != null))
            if (GUILayout.Button("Aplicar aos selecionados", GUILayout.Height(32))) Apply(selected);
        if (!string.IsNullOrEmpty(result)) EditorGUILayout.HelpBox(result, MessageType.Info);
        EditorGUILayout.EndScrollView();
    }

    private Type SelectedType => mode == DestructionMode.Fragments ? typeof(VoxelFragmentDestruction) :
        mode == DestructionMode.Event ? typeof(VoxelFullCollapseTrigger) : typeof(VoxelPartialCollapse);

    private string ValidateSelection(GameObject[] selected)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return "Saia do Play Mode para configurar a destruição.";
        if (selected.Length == 0) return "Selecione um GameObject na Hierarchy ou abra um prefab no Prefab Mode.";
        if (damageToDestroy <= 0 || float.IsNaN(damageToDestroy) || float.IsInfinity(damageToDestroy))
            return "Informe um dano para destruir maior que zero.";
        foreach (var go in selected)
        {
            string error = ValidatePiece(go);
            if (error != null) return error;
            if (go.GetComponent<VoxelFullCollapse>() != null)
                return $"{go.name} já é um controlador de colapso. Selecione uma peça da construção.";
            foreach (var other in selected)
                if (other != go && go.transform.IsChildOf(other.transform))
                    return "Selecione apenas pais ou filhos por vez, sem sobrepor hierarquias.";
            if (mode == DestructionMode.Fragments)
            {
                var fragments = FindFragments(go);
                if (fragments.Count == 0) return $"{go.name}: adicione filhos com as meshes dos fragmentos antes de aplicar.";
                foreach (var fragment in fragments)
                {
                    error = ValidatePiece(fragment);
                    if (error != null) return error;
                    if (fragment.GetComponent<VoxelFullCollapse>() != null)
                        return $"{fragment.name} é um controlador de colapso e não pode ser convertido em fragmento.";
                    foreach (var other in fragments)
                        if (fragment != other && fragment.transform.IsChildOf(other.transform))
                            return "Os fragmentos devem estar separados em filhos irmãos ou grupos sem mesh; um fragmento não pode conter outro.";
                }
            }
        }
        if (mode != DestructionMode.Event) return null;
        if (eventTargets.Count == 0) return "Adicione pelo menos um alvo para a destruição por evento.";
        foreach (var target in eventTargets)
        {
            string error = ValidatePiece(target);
            if (error != null) return "Alvos: " + error;
            if (Array.IndexOf(selected, target) >= 0) return "Um objeto selecionado não pode ser seu próprio alvo.";
            if (target.GetComponent<VoxelFragmentedObj>() != null || target.GetComponent<VoxelFullCollapse>() != null)
                return $"{target.name}: escolha uma peça inteira ou a raiz intacta da fragmentação como alvo.";
            foreach (var go in selected)
            {
                if (target.scene != go.scene) return "Os gatilhos e seus alvos devem estar na mesma cena ou prefab.";
                if (target.transform.IsChildOf(go.transform) || go.transform.IsChildOf(target.transform))
                    return "Mantenha o gatilho e seus alvos em ramos separados da hierarquia, para que cada peça possa cair independentemente.";
            }
        }
        return null;
    }

    private static string ValidatePiece(GameObject go)
    {
        if (go == null) return "Preencha todos os objetos da lista.";
        if (EditorUtility.IsPersistent(go) || !go.scene.IsValid()) return $"{go.name}: abra o prefab no Prefab Mode ou selecione uma instância na Hierarchy.";
        var filter = go.GetComponent<MeshFilter>();
        if (filter == null || filter.sharedMesh == null || go.GetComponent<MeshRenderer>() == null)
            return $"{go.name}: selecione o objeto que contém MeshFilter com mesh e MeshRenderer.";
        return null;
    }

    private static List<GameObject> FindFragments(GameObject root)
    {
        var fragments = new List<GameObject>();
        foreach (var mesh in root.GetComponentsInChildren<MeshFilter>(true))
            if (mesh.gameObject != root) fragments.Add(mesh.gameObject);
        return fragments;
    }

    private void Apply(GameObject[] selected)
    {
        // Validate all inputs before making any changes, including external targets.
        string error = ValidateSelection(selected);
        if (error != null) { result = error; return; }
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Configurar destruição");
        try
        {
            var targets = new List<VoxelDestruction>();
            if (mode == DestructionMode.Event)
                foreach (var go in eventTargets)
                {
                    var target = go.GetComponent<VoxelDestruction>();
                    if (target == null) target = SetMode(go, typeof(VoxelPartialCollapse));
                    PreparePiece(target);
                    if (!targets.Contains(target)) targets.Add(target);
                }
            foreach (var go in selected)
            {
                var piece = SetMode(go, SelectedType);
                Undo.RecordObject(piece, "Configurar dano");
                piece.damageToDestroy = damageToDestroy;
                PreparePiece(piece);
                if (mode == DestructionMode.Fragments)
                    foreach (var fragment in FindFragments(go))
                    {
                        PreparePiece(SetMode(fragment, typeof(VoxelFragmentedObj)));
                        // Keep grouping parents active as well, without touching ancestors of the selection.
                        for (Transform parent = fragment.transform.parent; parent != go.transform; parent = parent.parent)
                        {
                            Undo.RecordObject(parent.gameObject, "Ativar grupo de fragmentos");
                            parent.gameObject.SetActive(true);
                            PrefabUtility.RecordPrefabInstancePropertyModifications(parent.gameObject);
                        }
                    }
                else
                {
                    var settings = new SerializedObject(piece);
                    var chain = settings.FindProperty("chainCollapse");
                    chain.arraySize = mode == DestructionMode.Event ? targets.Count : 0;
                    for (int i = 0; i < chain.arraySize; i++) chain.GetArrayElementAtIndex(i).objectReferenceValue = targets[i];
                    settings.ApplyModifiedProperties();
                }
                PrefabUtility.RecordPrefabInstancePropertyModifications(piece);
            }
            Undo.CollapseUndoOperations(group);
            result = $"{selected.Length} objeto(s) configurado(s): {ModeNames[(int)mode]}. Revise os colliders e salve a cena/prefab. Ctrl+Z desfaz a configuração.";
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(group);
            result = "Não foi possível concluir. As alterações desta operação foram desfeitas.";
            Debug.LogException(exception);
        }
    }

    private static VoxelDestruction SetMode(GameObject go, Type type)
    {
        VoxelDestruction matching = null;
        var previous = go.GetComponents<VoxelDestruction>();
        foreach (var piece in previous) if (piece.GetType() == type) { matching = piece; break; }
        // Prepare required components explicitly so their initial state is also undoable.
        if (go.GetComponent<NetworkObject>() == null) Undo.AddComponent<NetworkObject>(go);
        var body = go.GetComponent<Rigidbody>();
        if (body == null) body = Undo.AddComponent<Rigidbody>(go);
        Undo.RecordObject(body, "Preparar Rigidbody");
        body.isKinematic = true;
        if (go.GetComponent<MeshCollider>() == null) Undo.AddComponent<MeshCollider>(go);
        if (typeof(VoxelPartialCollapse).IsAssignableFrom(type) && go.GetComponent<NetworkTransform>() == null)
            Undo.AddComponent<NetworkTransform>(go);
        if (matching == null)
        {
            matching = (VoxelDestruction)Undo.AddComponent(go, type);
            if (previous.Length > 0)
            {
                matching.damageToDestroy = previous[0].damageToDestroy;
                matching.voxelMaterialType = previous[0].voxelMaterialType;
                var source = new SerializedObject(previous[0]);
                var destination = new SerializedObject(matching);
                // Copy only gameplay settings, never FishNet's serialized identity/cache.
                foreach (string field in new[] { "damageModelSwap", "chainCollapse", "debrisCollider",
                    "debrisLayer", "settleWhenResting", "restingDuration", "restingLinearSpeed",
                    "restingAngularSpeed", "debrisSolverIterations" })
                {
                    var property = source.FindProperty(field);
                    if (property != null && destination.FindProperty(field) != null)
                        destination.CopyFromSerializedProperty(property);
                }
                destination.ApplyModifiedProperties();
            }
        }
        foreach (var piece in previous) if (piece != matching) Undo.DestroyObjectImmediate(piece);
        return matching;
    }

    private static void PreparePiece(VoxelDestruction piece)
    {
        GameObject go = piece.gameObject;
        if (go.GetComponent<NetworkObject>() == null) Undo.AddComponent<NetworkObject>(go);
        Undo.RecordObject(go, "Preparar objeto");
        GameObjectUtility.SetStaticEditorFlags(go, 0);
        go.SetActive(true);
        int voxelLayer = LayerMask.NameToLayer("Voxel");
        if (voxelLayer >= 0) go.layer = voxelLayer;
        var body = go.GetComponent<Rigidbody>();
        Undo.RecordObject(body, "Preparar Rigidbody");
        body.isKinematic = true;
        PrefabUtility.RecordPrefabInstancePropertyModifications(body);
        var renderer = go.GetComponent<MeshRenderer>();
        Undo.RecordObject(renderer, "Preparar visual");
        renderer.enabled = !(piece is VoxelFragmentedObj);
        PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        MeshCollider intact = null;
        foreach (var mesh in go.GetComponents<MeshCollider>()) if (!mesh.convex) { intact = mesh; break; }
        if (intact == null) intact = Undo.AddComponent<MeshCollider>(go);
        Undo.RecordObject(intact, "Preparar collider intacto");
        intact.sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
        intact.isTrigger = false;
        intact.enabled = !(piece is VoxelFragmentedObj);
        PrefabUtility.RecordPrefabInstancePropertyModifications(intact);

        Collider shape = null;
        if (piece is VoxelPartialCollapse)
        {
            var nt = go.GetComponent<NetworkTransform>();
            if (nt == null) nt = Undo.AddComponent<NetworkTransform>(go);
            var network = new SerializedObject(nt);
            network.FindProperty("_clientAuthoritative").boolValue = false;
            network.FindProperty("_sendToOwner").boolValue = true;
            network.FindProperty("_componentConfiguration").enumValueIndex = 0;
            network.FindProperty("_synchronizePosition").boolValue = true;
            network.FindProperty("_synchronizeRotation").boolValue = true;
            network.FindProperty("_synchronizeScale").boolValue = false;
            network.FindProperty("_interval").intValue = 3;
            network.ApplyModifiedProperties();
            var settings = new SerializedObject(piece);
            var colliderProperty = settings.FindProperty("debrisCollider");
            shape = colliderProperty.objectReferenceValue as Collider;
            if (shape != null && (shape.gameObject != go || (shape is MeshCollider assigned && !assigned.convex))) shape = null;
            // Reuse a previous setup's box when switching modes or reapplying.
            if (shape == null)
                foreach (var box in go.GetComponents<BoxCollider>()) if (!box.isTrigger) { shape = box; break; }
            if (shape == null)
            {
                var box = Undo.AddComponent<BoxCollider>(go);
                Bounds bounds = go.GetComponent<MeshFilter>().sharedMesh.bounds;
                box.center = bounds.center;
                box.size = bounds.size;
                shape = box;
            }
            colliderProperty.objectReferenceValue = shape;
            Undo.RecordObject(shape, "Preparar collider de destroço");
            shape.enabled = false;
            shape.isTrigger = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(shape);
            settings.FindProperty("settleWhenResting").boolValue = true;
            settings.FindProperty("debrisSolverIterations").intValue = 4;
            settings.ApplyModifiedProperties();
        }
        foreach (var other in go.GetComponents<Collider>())
        {
            if (other == intact || other == shape || other.isTrigger) continue;
            Undo.RecordObject(other, "Desativar collider duplicado");
            other.enabled = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(other);
        }
        PrefabUtility.RecordPrefabInstancePropertyModifications(go);
    }
}
