using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HelicopterPilotHUD : VehicleHudManager
{
    [Header("Hud indicators")]
    [SerializeField] private TextMeshProUGUI speed_text;
    [SerializeField] private TextMeshProUGUI throttle_text;
    [SerializeField] private TextMeshProUGUI altitude_text;
    [SerializeField] private TextMeshProUGUI rotation_x_text;
    [SerializeField] private TextMeshProUGUI rotation_y_text;
    [SerializeField] private Transform rotation_z_indicator;
    [SerializeField] private Transform max_rotation_x_position;
    [SerializeField] private Transform min_rotation_x_position;
    [SerializeField] private Transform max_rotation_y_position;
    [SerializeField] private Transform min_rotation_y_position;

    [Header("Angle Display Settings")]
    [SerializeField] private bool useSignedAngles = false; // Se true, mostra -180 a 180

    private Quaternion hudOriginalRotation;
    private Transform hudTransform;
    private float originalZRotation; // Armazena apenas a rotação Z original

    protected override void Start()
    {
        hudTransform = transform;
        hudOriginalRotation = hudTransform.localRotation;

        // Salva apenas a rotação Z original (global)
        if (rotation_z_indicator != null)
        {
            originalZRotation = rotation_z_indicator.eulerAngles.z;
        }
    }

    void LateUpdate()
    {
        // Mantém a rotação local fixa, ignorando a rotação do pai
        hudTransform.localRotation = hudOriginalRotation;

        // Atualiza outros elementos se necessário
        UpdateRotationZ();
    }


    public void SetPrimaryActive()
    {
        if (primary_image == null) return;
        primary_image_outline.enabled = true;
        secondary_image_outline.enabled = false;
    }

    public void SetSecondaryActive()
    {
        if (secondary_image == null) return;
        primary_image_outline.enabled = false;
        secondary_image_outline.enabled = true;
    }

    public void UpdateRotationX(float rotate_value)
    {
        float angle = rotate_value;
        if (angle > 180f)
            angle -= 360f;

        float absrollAngle = Mathf.Abs(angle);

        rotation_x_text.text = absrollAngle.ToString("F0") + " -> ";

        Vector3 speed_currentPosition = Vector3.Lerp(min_rotation_x_position.transform.localPosition, max_rotation_x_position.transform.localPosition, Mathf.Clamp01(absrollAngle / 90));
        rotation_x_text.transform.localPosition = speed_currentPosition;

    }

    public void UpdateRotationY(float rotate_value)
    {
        float displayValue = useSignedAngles ?
            NormalizeAngleSigned(rotate_value) :
            NormalizeAngleUnsigned(rotate_value);

        rotation_y_text.text = displayValue.ToString("F0") + "°";

        Vector3 speed_currentPosition = Vector3.Lerp(min_rotation_y_position.transform.localPosition, max_rotation_y_position.transform.localPosition, Mathf.Clamp01(displayValue / 360));
        rotation_y_text.transform.localPosition = speed_currentPosition;
    }

    public void UpdateRotationZ()
    {
        // Mantém apenas a rotação Z fixa (global), permitindo X e Y seguirem o pai
        if (rotation_z_indicator != null)
        {
            Vector3 currentRotation = rotation_z_indicator.eulerAngles;
            currentRotation.z = originalZRotation;
            rotation_z_indicator.eulerAngles = currentRotation;
        }
    }

    public void UpdateAltitude(float altitude)
    {
        altitude_text.text = altitude.ToString("F0");
    }

    public void UpdateSpeed(float speed)
    {
        speed_text.text = "Speed: " + speed.ToString("F1");
    }

    public void UpdateThrottle(float throttle)
    {
        throttle_text.text = "Throttle: " + throttle.ToString("F1");
    }

    // Normaliza para 0-360
    private float NormalizeAngleUnsigned(float angle)
    {
        angle %= 360f;
        if (angle < 0) angle += 360f;
        return angle;
    }

    // Normaliza para -180 a 180
    private float NormalizeAngleSigned(float angle)
    {
        angle %= 360f;

        if (angle > 180f)
        {
            angle -= 360f;
        }
        else if (angle < -180f)
        {
            angle += 360f;
        }

        return angle;
    }
}