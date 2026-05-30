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
    private float originalZRotation;

    private ICurrentRotationUIValues currentRotationUIValues;

    void Start()
    {
        currentRotationUIValues = GetComponentInParent<ICurrentRotationUIValues>();
        if (currentRotationUIValues == null)
        {
            Debug.LogError("CurrentRotationUI: Não foi possível encontrar um componente que implemente ICurrentRotationUIValues no objeto pai.");
            return;
        }

        hudTransform = transform;
        hudOriginalRotation = hudTransform.localRotation;

        // Salva a rotação local inicial do indicador Z
        if (rotation_z_indicator != null)
        {
            originalZRotation = rotation_z_indicator.localEulerAngles.z;
        }
    }

    void LateUpdate()
    {
        // Mantém o HUD fixo em relação ao pai, se essa for a intenção do seu projeto
        hudTransform.localRotation = hudOriginalRotation;
    }

    void Update()
    {
        if (currentRotationUIValues == null) return;

        if (updateRotationX && rotation_x_text != null)
            UpdateRotationX(currentRotationUIValues.GetXRotation());

        if (updateRotationY && rotation_y_text != null)
            UpdateRotationY(currentRotationUIValues.GetYRotation());

        // Passando o valor real do Z para a função
        if (updateRotationZ && rotation_z_indicator != null)
            UpdateRotationZ(currentRotationUIValues.GetZRotation());
    }

    public void UpdateRotationX(float rotate_value)
    {
        float angle = rotate_value;
        if (angle > 180f)
            angle -= 360f;

        float absrollAngle = Mathf.Abs(angle);

        rotation_x_text.text = absrollAngle.ToString("F0") + " -> ";

        if (min_rotation_x_position != null && max_rotation_x_position != null)
        {
            Vector3 speed_currentPosition = Vector3.Lerp(min_rotation_x_position.localPosition, max_rotation_x_position.localPosition, Mathf.Clamp01(absrollAngle / 90));
            rotation_x_text.transform.localPosition = speed_currentPosition;
        }
    }

    public void UpdateRotationY(float rotate_value)
    {
        float displayValue = useSignedAngles ?
            NormalizeAngleSigned(rotate_value) :
            NormalizeAngleUnsigned(rotate_value);

        rotation_y_text.text = displayValue.ToString("F0") + "°";

        if (min_rotation_y_position != null && max_rotation_y_position != null)
        {
            Vector3 speed_currentPosition = Vector3.Lerp(min_rotation_y_position.localPosition, max_rotation_y_position.localPosition, Mathf.Clamp01(displayValue / 360));
            rotation_y_text.transform.localPosition = speed_currentPosition;
        }
    }

    // Modificado para aceitar o valor da rotação atual
    public void UpdateRotationZ(float rotate_value)
    {
        if (rotation_z_indicator != null)
        {
            // 1. Usamos eulerAngles (Global) para anular o balanço do pai, 
            // mas injetamos o valor exato que queremos no Z.
            Vector3 currentGlobalRotation = rotation_z_indicator.eulerAngles;

            // 2. Definimos o Z global baseado no valor da interface + o offset inicial
            currentGlobalRotation.z = originalZRotation - rotate_value;

            // Se o ponteiro girar no sentido inverso ao que você quer, 
            // mude o sinal acima para: originalZRotation + rotate_value;

            rotation_z_indicator.eulerAngles = currentGlobalRotation;
        }
    }
    private float NormalizeAngleUnsigned(float angle)
    {
        angle %= 360f;
        if (angle < 0) angle += 360f;
        return angle;
    }

    private float NormalizeAngleSigned(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        else if (angle < -180f) angle += 360f;
        return angle;
    }
}

public interface ICurrentRotationUIValues
{
    public float GetXRotation();
    public float GetYRotation();
    public float GetZRotation();
}