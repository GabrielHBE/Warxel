using System;
using System.Collections;
using System.IO.Compression;
using UnityEngine;

public class SwayNBobScript : MonoBehaviour
{

    [HideInInspector]
    public PlayerController mover;
    private PlayerProperties playerProperties;

    [Header("Weapon")]
    private SwitchWeapon switchWeapon;

    [Header("Sway")]
    public float step = 0.01f;
    public float maxStepDistance = 0.06f;
    [HideInInspector] public Vector3 swayPos;

    [Header("Aiming Movement Rotation")]
    public float aimMoveRotationAmount = 5f; // Quantidade de rotação para cima/baixo
    public float aimMoveRotationSpeed = 5f; // Velocidade da rotação


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
    bool do_land_camera_chake_once;
    bool do_jump_camera_chake_once;
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
    private bool isRolling;
    private bool isProne;


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



    void Awake()
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
        isProne = playerProperties.is_proned;
        isRolling = playerProperties.roll;

    }

    Vector3 shakeOffset = Vector3.zero;
    void Update()
    {

        if (!restarted)
        {
            return;
        }

        CachePlayerProperties();


        // Verificar se o jogador está morto
        if (playerProperties.is_dead || playerProperties.isProneTransition || playerProperties.is_composing_bullets || playerProperties.is_in_vehicle)
        {
            // Apenas aplicar rotação de morte
            Quaternion deadRotation = Quaternion.Euler(40f, 0f, 0f);
            myTransform.localRotation = Quaternion.Lerp(
                myTransform.localRotation,
                deadRotation,
                Time.deltaTime * smoothRot
            );

            // Retornar para evitar outros cálculos
            return;
        }



        if (playerProperties.sprinting && !switchWeapon._switch && !playerProperties.is_aiming && !playerProperties.is_proned && !playerProperties.roll && !playerProperties.isProneTransition)
        {
            bobExaggeration = bob_sprint_exageration;
            current_multiplier = sprint_multiplier;

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

        if (((playerProperties.crouched || playerProperties.is_proned) && !playerProperties.sprinting) || playerProperties.is_aiming)
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

        //if ((!playerProperties.is_aiming && !playerProperties.is_firing && !playerProperties.is_reloading && !playerProperties.is_proned) || playerProperties.roll) Sprinting();
        if ((playerProperties.sprinting || playerProperties.roll) && !playerProperties.is_reloading) Sprinting();

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

        if (!SettingsHUD.Instance.is_menu_settings_active)
        {
            lookInput.x = Input.GetAxis("Mouse X");
            lookInput.y = Input.GetAxis("Mouse Y");
        }
        else
        {
            lookInput.x = 0;
            lookInput.y = 0;
        }


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


    /*
    void CompositePositionRotation()
    {
        // Cache de Time.deltaTime
        float deltaTime = Time.deltaTime;

        Quaternion combinedRotation;
        Vector3 combinedPosition;


        float x_input_amount = Input.GetAxis("Mouse X");

        
        if (!isAiming)
        {
            combinedRotation = sprintTargetWeaponRotation * Quaternion.Euler(swayEulerRot) * Quaternion.Euler(bobEulerRotation);
            combinedPosition = sprintTargetWeaponPosition + swayPos + bobPosition;
        }
        else
        {
            float divisor = isFiring ? 10f : 2f;
            combinedRotation = sprintTargetWeaponRotation * Quaternion.Euler(swayEulerRot / divisor) * Quaternion.Euler(bobEulerRotation / divisor);
            combinedPosition = sprintTargetWeaponPosition + swayPos / divisor + bobPosition / divisor;
        }

        // Otimizar interpolações
        if (!isGrounded)
        {
            float tiltAmount = Input.GetAxis("Horizontal") * 10f;
            Quaternion targetRotation = Quaternion.Euler(15, x_input_amount * 2, -tiltAmount);


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

            do_land_camera_chake_once = true;
        }
        else
        {

            Quaternion targetRotation = Quaternion.Euler(0, x_input_amount, 0);
            if (do_land_camera_chake_once)
            {
                StartCoroutine(JumpWeaponShake());
                do_land_camera_chake_once = false;
            }

            myTransform.localRotation = Quaternion.Lerp(
                myTransform.localRotation,
                combinedRotation * targetRotation,
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
    */


    void CompositePositionRotation()
    {
        float deltaTime = Time.deltaTime;

        Quaternion combinedRotation;
        Vector3 combinedPosition;

        // Reutilizar os valores já obtidos em GetInput()
        float x_input_amount = lookInput.x;
        float y_input_amount = lookInput.y;

        // Aplicar clamp
        x_input_amount = Mathf.Clamp(x_input_amount, -5f, 5f);
        y_input_amount = Mathf.Clamp(y_input_amount, -5f, 5f);

        // Multiplicadores (ajuste conforme necessidade)
        const int yRotationMultiplier = 4; // Ajuste para valores por SEGUNDO
        const int zRotationMultiplier = 4;
        const int xRotationMultiplier = 4;

        // Agora escalado por deltaTime
        float enhancedYRotation = x_input_amount * yRotationMultiplier;
        float enhancedZRotation = x_input_amount * zRotationMultiplier;
        float enhancedXRotation = y_input_amount * xRotationMultiplier;


        float verticalAimMoveRotation = 0f;

        if (isAiming && (playerController.moveForward != 0f || playerController.moveHorizontal != 0f) && !isSprinting && !isReloading && !isRolling)
        {
            float sinTime = Mathf.Sin(Time.time * aimMoveRotationSpeed);
            verticalAimMoveRotation = sinTime * aimMoveRotationAmount;

            if (isCrouched || isProne)
            {
                verticalAimMoveRotation /= 2;
            }
        }

        if (!isAiming)
        {
            combinedRotation = sprintTargetWeaponRotation * Quaternion.Euler(swayEulerRot) * Quaternion.Euler(bobEulerRotation);
            combinedPosition = sprintTargetWeaponPosition + swayPos + bobPosition;
        }
        else
        {
            float divisor = isFiring ? 20f : 5f;

            Vector3 aimingRotation = swayEulerRot / divisor;
            aimingRotation.x += verticalAimMoveRotation;

            combinedRotation = sprintTargetWeaponRotation *
                              Quaternion.Euler(aimingRotation) *
                              Quaternion.Euler(bobEulerRotation / divisor);
            combinedPosition = sprintTargetWeaponPosition + swayPos / divisor + bobPosition / divisor;
        }

        // ADICIONAR SHAKEOFFSET À ROTAÇÃO FINAL
        Quaternion shakeRotation = Quaternion.Euler(shakeOffset);
        combinedRotation = combinedRotation * shakeRotation;

        if (!isGrounded)
        {
            float tiltAmount = Input.GetAxis("Horizontal") * 10f;

            Quaternion targetRotation = Quaternion.Euler(15, enhancedYRotation, -tiltAmount + enhancedZRotation);

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

            if (do_jump_camera_chake_once)
            {
                //StartCoroutine(JumpWeaponShake());
                do_jump_camera_chake_once = false;
            }

            do_land_camera_chake_once = true;
        }
        else
        {
            Quaternion targetRotation;

            if (isFiring)
            {
                targetRotation = Quaternion.Euler(enhancedXRotation / 8, enhancedYRotation, -enhancedZRotation);
            }
            else
            {
                targetRotation = Quaternion.Euler(enhancedXRotation, enhancedYRotation, -enhancedZRotation);
            }

            if (do_land_camera_chake_once)
            {
                //StartCoroutine(JumpWeaponShake());
                do_land_camera_chake_once = false;
            }

            myTransform.localRotation = Quaternion.Lerp(
                myTransform.localRotation,
                combinedRotation * targetRotation,
                deltaTime * smoothRot
            );

            myTransform.localPosition = Vector3.Lerp(
                myTransform.localPosition,
                combinedPosition,
                deltaTime * smooth
            );
            do_jump_camera_chake_once = true;
        }
    }

    void BobOffset()
    {
        // Otimizar verificação de input
        float axis_bob = (Input.GetAxis("Vertical") != 0 || Input.GetAxis("Horizontal") != 0) ? 1 : 0;

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

        float intensity_local = 3;
        Quaternion originalRot = item.localRotation;

        // Usar valores pré-calculados para evitar Random.Range múltiplo
        float randomX = UnityEngine.Random.Range(-intensity_local, intensity_local);
        float randomY = UnityEngine.Random.Range(-intensity_local, intensity_local);
        float randomZ = UnityEngine.Random.Range(-intensity_local, intensity_local);

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

    float intensity = 2f;
    public IEnumerator CrouchWeaponShake()
    {
        float elapsed = 0f;
        float duration = 0.1f;

        while (elapsed < duration)
        {
            // Calcula o offset do shake
            float crouchTime = Time.time * 5f; // Frequência diferente para cada eixo

            shakeOffset = new Vector3(
                // Eixo X: mistura de Perlin noise com seno para movimento orgânico
                (Mathf.PerlinNoise(crouchTime * 1.2f, 0) * 2f - 1f) * 3f * intensity,

                // Eixo Y: usa combinação de noises para variação
                ((Mathf.PerlinNoise(0, crouchTime * 1.5f) * 2f - 1f) * 0.4f +
                 (Mathf.Sin(crouchTime * 3f) * 0.6f)) * 0.8f * intensity,

                // Eixo Z: noise com offset diferente para variar
                (Mathf.PerlinNoise(crouchTime * 0.8f, crouchTime * 0.8f) * 2f - 1f) * 0.7f * intensity
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        float returnTime = Mathf.Min(0.1f, duration * 0.5f);
        elapsed = 0f;
        Vector3 startingShake = shakeOffset;

        while (elapsed < returnTime)
        {
            float t = elapsed / returnTime;
            // Interpola suavemente de volta a zero
            shakeOffset = Vector3.Lerp(startingShake, Vector3.zero, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        shakeOffset = Vector3.zero;
    }


}