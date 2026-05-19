using UnityEngine;

public class AltitudeLevelUI : MonoBehaviour
{
    [SerializeField] private bool onlyText;

    [SerializeField] private TMPro.TextMeshProUGUI hud_altidude_controller;
    [SerializeField] private GameObject hud_altidude_controller_max_pos;
    [SerializeField] private GameObject hud_altidude_controller_min_pos;

    private IAltitudeLevelUIValues altitudeLevelUIValues;

    void Start()
    {
        altitudeLevelUIValues = GetComponentInParent<IAltitudeLevelUIValues>();
        if (altitudeLevelUIValues == null)
        {
            Debug.LogError("AltitudeLevelUI: Não foi possível encontrar um componente que implemente IAltitudeLevelUIValues no objeto pai.");
            return;
        }
    }

    void Update()
    {
        UpdateAltitude(altitudeLevelUIValues.GetCurrentAltitude());
    }

    public void UpdateAltitude(float altitude)
    {
        if (hud_altidude_controller == null) return;
        hud_altidude_controller.text = altitude.ToString("F0");

        if(onlyText) return;
        
        Vector3 altitude_currentPosition = Vector3.Lerp(
            hud_altidude_controller_min_pos.transform.localPosition,
            hud_altidude_controller_max_pos.transform.localPosition,
            Mathf.Clamp01(altitude / MapSettings.Instance.max_altitude)
        );

        hud_altidude_controller.transform.localPosition = altitude_currentPosition;
    }
}

public interface IAltitudeLevelUIValues
{
    public float GetCurrentAltitude();
}
