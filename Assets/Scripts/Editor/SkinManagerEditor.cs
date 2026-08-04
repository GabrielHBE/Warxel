using UnityEngine;
using UnityEditor;


#if UNITY_EDITOR
[CustomEditor(typeof(SkinsManager))]
public class SkinsControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Desenha as variáveis padrão do Inspector (rootFolder e a lista)
        DrawDefaultInspector();

        // Pega a referência do script
        SkinsManager controller = (SkinsManager)target;

        // Adiciona um espaço visual
        GUILayout.Space(10);

        // Cria o botão no Inspector
        if (GUILayout.Button("Update Skins Button", GUILayout.Height(30)))
        {
            // Quando clicado, executa a função de busca
            controller.UpdateSkinsButton();
            
            // Força o Inspector a atualizar visualmente para mostrar a lista preenchida
            Repaint();
        }
    }
}
#endif