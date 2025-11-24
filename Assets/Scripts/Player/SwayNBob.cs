using System;
using System.Collections;
using System.IO.Compression;
using UnityEngine;

public class SwayNBobScript : MonoBehaviour
{

    [HideInInspector]
    public PlayerController mover;
    private PlayerProperties playerProperties;

    [Header("Instances")]
    [SerializeField] private Transform reticleTransform;

    [Header("Weapon")]

    private SwitchWeapon switchWeapon;

    [Header("Sway")]
    public float step = 0.01f;
    public float maxStepDistance = 0.06f;
    [HideInInspector] public Vector3 swayPos;



    [Header("Sway Rotation")]
    public float rotationStep = 4f;
    public float maxRotationStep = 5f;
    [HideInInspector] public Vector3 swayEulerRot;

    public float smooth = 10f;
    float smoothRot = 12f;

    [Header("Bobbing")]
    public float speedCurve;
    float curveSin { get => Mathf.Sin(speedCurve); }
    float curveCos { get => Mathf.Cos(speedCurve); }

    public Vector3 travelLimit = Vector3.one * 0.025f;
    public Vector3 bobLimit = Vector3.one * 0.01f;
    Vector3 bobPosition;

    public float bobExaggeration;

    [Header("Bob Rotation")]
    private Vector3 current_multiplier;
    Vector3 bobEulerRotation;

    private Quaternion sprintTargetWeaponRotation;
    private Vector3 sprintTargetWeaponPosition;

    [Header("Bob Rotation/Position Values")]
    Vector3 inicialVector3;
    PlayerController playerController;
    Quaternion initial_rotation;
    Vector3 initial_position;
    bool do_once;
    float max_position_sprinting = 1;
    float current_position_sprinting = 0;
    int sprintDirection = 1;
    bool restarted;
    private Transform myTransform;

    private bool isAiming;
    private bool isSprinting;
    private bool isGrounded;
    private bool isCrouched;
    private bool isReloading;
    private bool isFiring;


    //Itens that change with the item
    Transform item;
    float bob_walk_exageration;
    float bob_sprint_exageration;
    float bob_crouch_exageration;
    float bob_aim_exageration;
    Vector3 walk_multiplier;
    Vector3 sprint_multiplier;
    Vector3 aim_multiplier;
    Vector3 crouch_multiplier;
    [HideInInspector] public float[] vector3Values = new float[3];
    [HideInInspector] public float[] quaternionValues = new float[3];



    // Start is called before the first frame update
    void Start()
    {
        restarted = false;
        initial_rotation = transform.localRotation;
        initial_position = transform.localPosition;

        myTransform = transform;
    }

    public void Restart(Transform item,
                        float bob_walk_exageration,
                        float bob_sprint_exageration,
                        float bob_crouch_exageration,
                        float bob_aim_exageration,
                        Vector3 walk_multiplier,
                        Vector3 sprint_multiplier,
                        Vector3 aim_multiplier,
                        Vector3 crouch_multiplier,
                        float[] vector3Values,
                        float[] quaternionValues)
    {
        
        this.item = item;
        this.bob_walk_exageration = bob_walk_exageration;
        this.bob_sprint_exageration = bob_sprint_exageration;
        this.bob_crouch_exageration = bob_crouch_exageration;
        this.bob_aim_exageration = bob_aim_exageration;
        this.walk_multiplier = walk_multiplier;
        this.sprint_multiplier = sprint_multiplier;
        this.aim_multiplier = aim_multiplier;
        this.crouch_multiplier = crouch_multiplier;
        this.vector3Values = vector3Values;
        this.quaternionValues = quaternionValues;

        switchWeapon = GetComponent<SwitchWeapon>();
        playerController = GetComponentInParent<PlayerController>();
        playerProperties = GetComponentInParent<PlayerProperties>();

        current_multiplier = this.walk_multiplier;

        sprintTargetWeaponRotation = initial_rotation;

        inicialVector3 = current_multiplier;


        CachePlayerProperties();

        restarted = true;

    }

    private void CachePlayerProperties()
    {
        isAiming = playerProperties.is_aiming;
        isSprinting = playerProperties.sprinting;
        isGrounded = playerProperties.isGrounded;
        isCrouched = playerProperties.crouched;
        isReloading = playerProperties.is_reloading;
        isFiring = playerProperties.is_firing;
    }


    void Update()
    {

        if (!restarted)
        {
            return;
        }

        CachePlayerProperties();

        if (playerProperties.sprinting == true && !playerProperties.is_reloading && switchWeapon._switch == false && !playerProperties.is_aiming)
        {
            bobExaggeration = bob_sprint_exageration;
            current_multiplier = sprint_multiplier;

            Sprinting();
        }
        else
        {
            current_position_sprinting = 0f;
            bobExaggeration = bob_walk_exageration;

            sprintTargetWeaponPosition = initial_position;
            sprintTargetWeaponRotation = initial_rotation;

            current_multiplier.x = inicialVector3.x;
            current_multiplier.y = inicialVector3.y;
            current_multiplier.z = inicialVector3.z;
        }

        if ((playerProperties.crouched == true && playerProperties.sprinting == false) || playerProperties.is_aiming)
        {

            bobExaggeration = bob_crouch_exageration;
            current_multiplier = crouch_multiplier;


        }

        if (playerController.moveForward == 0f && playerController.moveHorizontal == 0f)
        {
            bobExaggeration = 0f;
            sprintTargetWeaponPosition = initial_position;
            sprintTargetWeaponRotation = initial_rotation;

            current_multiplier.x = 0.1f;
            current_multiplier.y = 0.1f;
            current_multiplier.z = 0.1f;

        }

        if (playerProperties.is_aiming && (playerController.moveForward != 0f || playerController.moveHorizontal != 0f))
        {

            bobExaggeration = bob_aim_exageration;
            current_multiplier.x = aim_multiplier.x;
            current_multiplier.y = aim_multiplier.y;
            current_multiplier.z = aim_multiplier.z;
        }

        SwayRotation();
        Sway();
        BobOffset();
        BobRotation();
        GetInput();
        CompositePositionRotation();

        transform.localPosition = Vector3.Lerp(transform.localPosition,
        new Vector3(transform.localPosition.x + current_position_sprinting, transform.localPosition.y, transform.localPosition.z),
        Time.deltaTime
        );

    }


    Vector2 walkInput;
    Vector2 lookInput;

    void Sprinting()
    {
        sprintTargetWeaponPosition = initial_position + new Vector3(vector3Values[0], vector3Values[1], vector3Values[2]);
        sprintTargetWeaponRotation = initial_rotation * Quaternion.Euler(quaternionValues[0], quaternionValues[1], quaternionValues[2]);

        current_position_sprinting += sprintDirection * Time.deltaTime * 8;

        if (isGrounded)
        {
            if (current_position_sprinting >= max_position_sprinting)
            {
                current_position_sprinting = max_position_sprinting;
                sprintDirection = -1;
            }
            else if (current_position_sprinting <= -max_position_sprinting)
            {
                current_position_sprinting = -max_position_sprinting;
                sprintDirection = 1;
            }
        }
        else
        {
            current_position_sprinting = 0;
        }
    }


    void GetInput()
    {
        walkInput.x = playerController.moveHorizontal;
        walkInput.y = playerController.moveForward;
        walkInput = walkInput.normalized;

        lookInput.x = Input.GetAxis("Mouse X");
        lookInput.y = Input.GetAxis("Mouse Y");

    }


    void Sway()
    {
        Vector3 invertLook = (lookInput * -step).normalized;
        invertLook.x = Mathf.Clamp(invertLook.x, -maxStepDistance, maxStepDistance);
        invertLook.y = Mathf.Clamp(invertLook.y, -maxStepDistance, maxStepDistance);

        swayPos = invertLook;
    }

    void SwayRotation()
    {

        Vector2 invertLook = (lookInput * -rotationStep).normalized;
        invertLook.x = Mathf.Clamp(invertLook.x, -maxRotationStep, maxRotationStep);
        invertLook.y = Mathf.Clamp(invertLook.y, -maxRotationStep, maxRotationStep);
        swayEulerRot = new Vector3(invertLook.y, invertLook.x, invertLook.x).normalized;

    }

    void CompositePositionRotation()
    {
        // Cache de Time.deltaTime
        float deltaTime = Time.deltaTime;

        Quaternion combinedRotation;
        Vector3 combinedPosition;

        if (!isAiming)
        {
            combinedRotation = sprintTargetWeaponRotation * Quaternion.Euler(swayEulerRot) * Quaternion.Euler(bobEulerRotation);
            combinedPosition = sprintTargetWeaponPosition + swayPos + bobPosition;
        }
        else
        {
            float divisor = isFiring ? 20f : 4f;
            combinedRotation = sprintTargetWeaponRotation * Quaternion.Euler(swayEulerRot / divisor) * Quaternion.Euler(bobEulerRotation / divisor);
            combinedPosition = sprintTargetWeaponPosition + swayPos / divisor + bobPosition / divisor;
        }

        // Otimizar interpolações
        if (!isGrounded)
        {
            float tiltAmount = Input.GetAxis("Horizontal") * 10f;
            Quaternion targetRotation = Quaternion.Euler(15, Input.GetAxis("Mouse X") * 2, -tiltAmount);

            myTransform.localRotation = Quaternion.Lerp(
                myTransform.localRotation,
                combinedRotation * targetRotation,
                deltaTime * smoothRot
            );

            myTransform.localPosition = Vector3.Lerp(
                myTransform.localPosition,
                new Vector3(combinedPosition.x, combinedPosition.y - 0.01f, combinedPosition.z),
                deltaTime * smooth
            );

            do_once = true;
        }
        else
        {
            if (do_once)
            {
                StartCoroutine(JumpWeaponShake());
                do_once = false;
            }

            myTransform.localRotation = Quaternion.Lerp(
                myTransform.localRotation,
                combinedRotation,
                deltaTime * smoothRot
            );

            myTransform.localPosition = Vector3.Lerp(
                myTransform.localPosition,
                combinedPosition,
                deltaTime * smooth
            );

            // Otimizar reticle
            Vector3 reticlePos = new Vector3(combinedPosition.x / 2, combinedPosition.y / 2, reticleTransform.localPosition.z);
            reticleTransform.localPosition = Vector3.Lerp(reticleTransform.localPosition, reticlePos, deltaTime * smooth);
        }
    }

    void BobOffset()
    {
        float axis_bob = 0;

        // Otimizar verificação de input
        axis_bob = (Input.GetAxis("Vertical") != 0 || Input.GetAxis("Horizontal") != 0) ? 1 : 0;

        speedCurve += Time.deltaTime * (isGrounded ? axis_bob * bobExaggeration : 1f) + 0.005f;

        // Pré-calcular curvas
        float cosCurve = curveCos;
        float sinCurve = curveSin;
        float groundedMultiplier = isGrounded ? 1 : 0;

        bobPosition.x = (cosCurve * bobLimit.x * groundedMultiplier) - (walkInput.x * travelLimit.x);
        bobPosition.y = (sinCurve * bobLimit.y) - (Input.GetAxis("Vertical") * travelLimit.y);
        bobPosition.z = -(walkInput.y * travelLimit.z);
    }

    void BobRotation()
    {
        bool isMoving = walkInput != Vector2.zero;
        float sin2x = Mathf.Sin(2 * speedCurve);

        bobEulerRotation.x = isMoving ? current_multiplier.x * sin2x : current_multiplier.x * (sin2x / 2);
        bobEulerRotation.y = isMoving ? current_multiplier.y * curveCos : 0;
        bobEulerRotation.z = isMoving ? current_multiplier.z * curveCos * walkInput.x : 0;
    }

    IEnumerator JumpWeaponShake()
    {
        Quaternion originalRot = item.localRotation;

        // Usar valores pré-calculados para evitar Random.Range múltiplo
        float randomX = UnityEngine.Random.Range(-2f, 2f);
        float randomY = UnityEngine.Random.Range(-2f, 2f);
        float randomZ = UnityEngine.Random.Range(-2f, 2f);

        Quaternion upRot = originalRot * Quaternion.Euler(randomX, randomY, randomZ);

        float duration = 0.1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            item.localRotation = Quaternion.Lerp(originalRot, upRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        item.localRotation = upRot;

        elapsed = 0f;
        while (elapsed < duration)
        {
            item.localRotation = Quaternion.Lerp(upRot, originalRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        item.localRotation = originalRot;
    }


}