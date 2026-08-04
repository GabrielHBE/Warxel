using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(InfantryLoadoutCustomization))]
public class InfantryLoadoutCustomizationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Desenha as variáveis padrão no Inspector
        DrawDefaultInspector();

        // Pega a referência do script
        InfantryLoadoutCustomization customization = (InfantryLoadoutCustomization)target;

        // Adiciona um espaço visual
        GUILayout.Space(10);

        // Cria o botão no Inspector
        if (GUILayout.Button("Refresh Loadout Lists", GUILayout.Height(30)))
        {
            // Executa a função de atualizar as listas de armas e gadgets
            customization.UpdateLoadoutLists();
            
            // Força a atualização da interface do Inspector
            Repaint();
        }
    }
}
#endif