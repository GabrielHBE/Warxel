using UnityEngine;

public class SunRotationController : MonoBehaviour
{
    [Header("Sun Light Reference")]
    [SerializeField] private Light sunLight;
    
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 0.5f;
    [SerializeField] private float fixedAzimuth = 180f; // Sul (180 graus)
    
    private DayNightCycleManager dayNightCycle;
    
    public void Initialize(DayNightCycleManager cycleManager)
    {
        dayNightCycle = cycleManager;
        
        if (sunLight == null)
            sunLight = GetComponent<Light>();
    }
    
    public void UpdateRotation()
    {
        if (sunLight == null || dayNightCycle == null) return;
        
        float currentHour = dayNightCycle.GetCurrentHour();
        float sunAngle = CalculateSunAngle(currentHour);
        
        // Cria rotação apenas no eixo X, mantendo o azimute fixo
        Quaternion targetRotation = Quaternion.Euler(sunAngle, fixedAzimuth, 0f);
        
        // Interpolação suave
        sunLight.transform.rotation = Quaternion.Slerp(
            sunLight.transform.rotation,
            targetRotation,
            Time.deltaTime * rotationSpeed
        );
    }
    
    private float CalculateSunAngle(float hour)
    {
        // Normaliza a hora para 0-24
        hour = Mathf.Repeat(hour, 24f);
        
        // Mapeia as horas para ângulos:
        // 6h = 0° (nascer do sol no horizonte leste)
        // 12h = 90° (sol no zênite)
        // 18h = 180° (pôr do sol no horizonte oeste)
        // 0h = 270° (sol abaixo do horizonte - noite)
        
        if (hour >= 6f && hour < 18f)
        {
            // Dia: 6h (0°) -> 18h (180°)
            float t = (hour - 6f) / 12f;
            return Mathf.Lerp(0f, 180f, t);
        }
        else
        {
            // Noite: 18h (180°) -> 6h (360°/0°)
            float nightHour = hour < 6f ? hour + 24f : hour;
            float t = (nightHour - 18f) / 12f;
            return Mathf.Lerp(180f, 360f, t);
        }
    }
    
    public void SetSunRotationDirect(float hour)
    {
        if (sunLight == null) return;
        
        float sunAngle = CalculateSunAngle(hour);
        sunLight.transform.rotation = Quaternion.Euler(sunAngle, fixedAzimuth, 0f);
    }
    
    public float GetCurrentSunAngle()
    {
        if (sunLight == null) return 0f;
        return sunLight.transform.eulerAngles.x;
    }
}