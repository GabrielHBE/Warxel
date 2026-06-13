using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SoundManager))]
public class SoundManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Desenha o inspector padrão do SoundManager
        DrawDefaultInspector();

        // Adiciona um espaço e o botão personalizado
        GUILayout.Space(10);
        
        if (GUILayout.Button("Update Sounds Button", GUILayout.Height(30)))
        {
            SoundManager soundManager = (SoundManager)target;
            soundManager.UpdateSoundsButton();
        }
    }
}