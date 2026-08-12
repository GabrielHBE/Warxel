using UnityEngine;

public class Sight : Attatchment
{
    [Header("Settings")]
    public Transform adsPosition;
    public SightType sightType;
    [SerializeField] private Texture2D sight;
    [SerializeField] private MeshRenderer glassRenderer;

    private Material sightMaterial;

    [Header("Changes")]
    public float zoom_change;
    public float ads_speed_change;
    public float sway_change;

    private Color newColor => Settings.Instance._gameplay.sight_reticle_collor;
    private float scale => Settings.Instance._gameplay.sight_reticle_size;

    public enum SightType
    {
        CloseRange,
        MediumRange,
        LongRange,
        ExtremeLongRange
    }

    void Update()
    {
        if (sightMaterial != null && sight != null)
        {
            float calculatedScale = 2 - scale;
            sightMaterial.SetFloat("_Reticle_Layer_1_Scale", calculatedScale);

            Color currentColor = sightMaterial.GetColor("_Reticle_Layer_1_Color");
            if (currentColor != newColor) sightMaterial.SetColor("_Reticle_Layer_1_Color", newColor);
        }
    }


    void Awake()
    {
        if (glassRenderer != null) sightMaterial = glassRenderer.material;
    }

    void OnEnable()
    {
        if (sightMaterial != null && sight != null) sightMaterial.SetTexture("_Reticle_Layer_1", sight);
    }

}