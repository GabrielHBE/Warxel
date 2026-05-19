using UnityEngine;

public class CurrentSpeedUI : MonoBehaviour
{
    [SerializeField] private bool onlyText;
    [SerializeField] private TMPro.TextMeshProUGUI hud_speed_controller;
    [SerializeField] private GameObject hud_speed_controller_max_pos;
    [SerializeField] private GameObject hud_speed_controller_min_pos;
    
    private ICurrentSpeedUIValues currentSpeedUIValues;
    
    void Start()
    {
        currentSpeedUIValues = GetComponentInParent<ICurrentSpeedUIValues>();
        if (currentSpeedUIValues == null)
        {
            Debug.LogError("CurrentSpeedUI: Não foi possível encontrar um componente que implemente ICurrentSpeedUIValues no objeto pai.");
            return;
        }
    }

    void Update()
    {
        UpdateSpeed(currentSpeedUIValues.GetCurrentSpeed());
    }

    public void UpdateSpeed(float speed)
    {
        if (hud_speed_controller == null) return;
        hud_speed_controller.text = "Speed: " + speed.ToString("F0");

        if(onlyText) return;

        Vector3 speed_currentPosition = Vector3.Lerp(hud_speed_controller_min_pos.transform.localPosition, hud_speed_controller_max_pos.transform.localPosition, Mathf.Clamp01(speed / currentSpeedUIValues.GetMaxSpeed()));
        hud_speed_controller.transform.localPosition = speed_currentPosition;
    }
}

public interface ICurrentSpeedUIValues
{
    public float GetCurrentSpeed();
    public float GetMaxSpeed();
}