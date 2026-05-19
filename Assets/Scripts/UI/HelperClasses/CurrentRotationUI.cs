using UnityEngine;

public class CurrentRotationUI : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private bool updateRotationX = true;
    [SerializeField] private bool updateRotationY = true;
    [SerializeField] private bool updateRotationZ = true;

    [SerializeField] private TMPro.TextMeshProUGUI rotation_x_text;
    [SerializeField] private TMPro.TextMeshProUGUI rotation_y_text;
    [SerializeField] private Transform rotation_z_indicator;
    [SerializeField] private Transform max_rotation_x_position;
    [SerializeField] private Transform min_rotation_x_position;
    [SerializeField] private Transform max_rotation_y_position;
    [SerializeField] private Transform min_rotation_y_position;

    [SerializeField] private bool useSignedAngles = false; // Se true, mostra -180 a 180

    private Quaternion hudOriginalRotation;
    private Transform hudTransform;
    private float originalZRotation; // Armazena apenas a rotação Z original

    void Start()
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
        hudTransform.localRotation = hudOriginalRotation;
    }

    void Update()
    {
        
        if(updateRotationX)
            UpdateRotationX(hudTransform.eulerAngles.x);
        
        if(updateRotationY)
            UpdateRotationY(hudTransform.eulerAngles.y);
        
        if(updateRotationZ)
            UpdateRotationZ();
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
