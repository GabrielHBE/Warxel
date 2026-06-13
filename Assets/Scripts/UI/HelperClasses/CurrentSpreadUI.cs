using UnityEngine;

public class CurrentSpreadUI : MonoBehaviour
{
    [SerializeField] private bool onlyText;
    [SerializeField] private TMPro.TextMeshProUGUI hud_spread_controller;
    [SerializeField] private GameObject hud_spread_controller_max_pos;
    [SerializeField] private GameObject hud_spread_controller_min_pos;

    private ICurrentSpreadUIValues currentspreadUIValues;

    void Start()
    {
        currentspreadUIValues = transform.root.GetComponentInChildren<ICurrentSpreadUIValues>(true);
        if (currentspreadUIValues == null)
        {
            Debug.LogError("CurrentspreadUI: Não foi possível encontrar um componente que implemente ICurrentspreadUIValues no objeto pai.");
            return;
        }
    }

    void Update()
    {
        if (currentspreadUIValues != null) UpdateSpread(currentspreadUIValues.GetCurrentSpread());
    }

    public void UpdateSpread(float spread)
    {
        if (hud_spread_controller == null) return;
        hud_spread_controller.text = "spread: " + spread.ToString("F1");

        if (onlyText) return;

        Vector3 spread_currentPosition = Vector3.Lerp(hud_spread_controller_min_pos.transform.localPosition, hud_spread_controller_max_pos.transform.localPosition, Mathf.Clamp01(spread / currentspreadUIValues.GetMaxSpread()));
        hud_spread_controller.transform.localPosition = spread_currentPosition;
    }
}


public interface ICurrentSpreadUIValues
{
    public float GetCurrentSpread();
    public float GetMaxSpread();
}