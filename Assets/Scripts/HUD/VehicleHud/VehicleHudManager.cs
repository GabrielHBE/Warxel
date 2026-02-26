using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VehicleHudManager : MonoBehaviour
{
    [Header("Instances")]
    protected Settings settings;
    [SerializeField] protected Vehicle vehicle;
    [SerializeField] private RectTransform hp_bar;
    [SerializeField] protected TextMeshProUGUI countermeasures_status;
    [SerializeField] protected Image countermeasures_image;
    [SerializeField] protected Outline countermeasures_outline;
    [SerializeField] protected Image primary_image;
    [SerializeField] protected Image secondary_image;
    [SerializeField] protected Outline primary_image_outline;
    [SerializeField] protected Outline secondary_image_outline;


    [Header("Huds")]
    [SerializeField] private List<Image> images_change_color_itens = new List<Image>();
    [SerializeField] private List<TextMeshProUGUI> text_change_color_itens = new List<TextMeshProUGUI>();

    [Header("Emission Settings")]
    [SerializeField] private Color emissionColor = Color.green;
    [SerializeField] private float emissionIntensity = 2f;


    private float originalWidth;

    protected virtual void Start()
    {
        if (hp_bar != null) originalWidth = hp_bar.sizeDelta.x;

        settings = GameObject.FindGameObjectWithTag("GeneralHUD").GetComponent<Settings>();

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

    // Restante do código...
    protected virtual void Update()
    {   
        if (hp_bar != null) UpdateDamage();
    }

    public void UpdateDamage()
    {

        float hpPercent = vehicle.hp / vehicle.original_hp;
        hpPercent = Mathf.Clamp01(hpPercent);

        hp_bar.sizeDelta = new Vector2(
            originalWidth * hpPercent,
            hp_bar.sizeDelta.y
        );
    }

    public void UpdateCountermeasuresStatus(string text)
    {
        if (countermeasures_status == null) return;

        if (text == "Ready")
        {
            countermeasures_outline.enabled = true;
        }
        else
        {
            countermeasures_outline.enabled = false;
        }

        countermeasures_status.text = "Countermeasures: " + text;


    }

    public void SetImages(Sprite primary_image, Sprite secondary_image, Sprite countermeasures_image)
    {
        if (primary_image != null) this.primary_image.sprite = primary_image;
        if (secondary_image != null) this.secondary_image.sprite = secondary_image;
        if (countermeasures_image != null) this.countermeasures_image.sprite = countermeasures_image;
    }

}
