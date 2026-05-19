using UnityEngine;

public class CurrentThrottledUI : MonoBehaviour
{
    [SerializeField] private bool onlyText;
    [SerializeField] private TMPro.TextMeshProUGUI hud_speed_controller;
    [SerializeField] private GameObject hud_speed_controller_max_pos;
    [SerializeField] private GameObject hud_speed_controller_min_pos;
    
    private ICurrentThrottleUIValues currentThrottleUIValues;
    
    void Start()
    {
        currentThrottleUIValues = GetComponentInParent<ICurrentThrottleUIValues>();
        if (currentThrottleUIValues == null)
        {
            Debug.LogError("CurrentSpeedUI: Não foi possível encontrar um componente que implemente IcurrentThrottleUIValues no objeto pai.");
            return;
        }
    }

    void Update()
    {
        UpdateSpeed(currentThrottleUIValues.GetCurrentThrottle());
    }

    public void UpdateSpeed(float speed)
    {
        if (hud_speed_controller == null) return;
        hud_speed_controller.text = "Throttle: " + speed.ToString("F0");

        if(onlyText) return;

        Vector3 speed_currentPosition = Vector3.Lerp(hud_speed_controller_min_pos.transform.localPosition, hud_speed_controller_max_pos.transform.localPosition, Mathf.Clamp01(speed / currentThrottleUIValues.GetMaxThrottle()));
        hud_speed_controller.transform.localPosition = speed_currentPosition;
    }
}

public interface ICurrentThrottleUIValues
{
    public float GetCurrentThrottle();
    public float GetMaxThrottle();
}