using UnityEngine;

[ExecuteInEditMode]
public class CreateVoxelMaterial : MonoBehaviour
{
    void Start()
    {
        CreateMaterial();
    }

    void CreateMaterial()
    {
        // Procura pelo shader personalizado
        Shader shader = Shader.Find("Custom/VoxelURPShader");
        
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Lit");
            Debug.LogWarning("Shader Custom/VoxelURPShader não encontrado! Usando URP Lit como fallback.");
        }

        Material mat = new Material(shader);
        mat.name = "VoxelMaterial_URP";
        mat.SetColor("_Color", Color.white);
        mat.SetFloat("_Surface", 0);
        mat.SetFloat("_Blend", 0);
        mat.SetFloat("_Cull", 0);
        mat.EnableKeyword("_VERTEX_COLORS");
        mat.SetFloat("_VertexColors", 1.0f);
        
        // Configurações balanceadas
        mat.SetFloat("_Saturation", 1.0f);
        mat.SetFloat("_Brightness", 1.0f);
        mat.SetFloat("_AmbientIntensity", 0.6f);
        mat.SetFloat("_LightIntensity", 0.7f);
        mat.SetFloat("_TopLightBoost", 0.8f);      // Reduz iluminação no topo
        mat.SetFloat("_SideLightBoost", 1.0f);     // Mantém laterais
        mat.SetFloat("_BottomLightBoost", 1.3f);   // Aumenta iluminação embaixo

        #if UNITY_EDITOR
        UnityEditor.AssetDatabase.CreateAsset(mat, "Assets/VoxelMaterial_URP.mat");
        UnityEditor.AssetDatabase.SaveAssets();
        #endif
    }
}