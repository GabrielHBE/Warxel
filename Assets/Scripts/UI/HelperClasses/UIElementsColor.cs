using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIElementsColor : MonoBehaviour
{
    [Header("Huds")]
    [SerializeField] private List<Image> images_change_color_itens = new List<Image>();
    [SerializeField] private List<TextMeshProUGUI> text_change_color_itens = new List<TextMeshProUGUI>();

    public void SetColor(Color emissionColor, float emissionIntensity)
    {
        foreach (Image image in images_change_color_itens)
        {
            image.color = Color.green;

            // Cria uma cópia do material para não afetar outros elementos
            Material mat = new Material(image.material);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emissionColor * emissionIntensity);
            image.material = mat;

            // Habilita emissão global (necessário para Unity URP/HDRP)
            image.material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        foreach (TextMeshProUGUI text in text_change_color_itens)
        {
            text.color = Color.green;

            // Para TextMeshPro
            Material mat = new Material(text.fontMaterial);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emissionColor * emissionIntensity);
            text.fontMaterial = mat;
        }
    }
}
