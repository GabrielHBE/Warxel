using System;
using System.Collections;
using System.IO.Compression;
using UnityEngine;

public class SwayNBobScript : MonoBehaviour
{

    [HideInInspector]
    public PlayerController mover;
    private PlayerProperties playerProperties;
    private WeaponProperties weaponProperties;
    
    [Header("Instances")]
    public GameObject reticle;

    [Header("Weapon")]
    private GameObject weapon;
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

    [HideInInspector] public Vector3 initialWeaponPosition;
    [HideInInspector] public Quaternion initialWeaponRotation;


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
    int sprintDirection = 1; // 1 = subindo, -1 = descendo

    bool restarted;


    // Start is called before the first frame update
    void Start()
    {
        restarted = false;
        initial_rotation = transform.localRotation;
        initial_position = transform.localPosition;
    }

    public void Restart()
    {
        switchWeapon = GetComponent<SwitchWeapon>();
        playerController = GetComponentInParent<PlayerController>();
        playerProperties = GetComponentInParent<PlayerProperties>();
        weaponProperties = GetComponentInChildren<WeaponProperties>();

        weapon = weaponProperties.weapon;

        current_multiplier = weaponProperties.walk_multiplier;



        initialWeaponPosition = weaponProperties.initial_potiion;
        initialWeaponRotation = weapon.transform.localRotation;
        initialWeaponRotation = new Quaternion(weaponProperties.inicial_rotation.x, weaponProperties.inicial_rotation.y, weaponProperties.inicial_rotation.z, initialWeaponRotation.w);

        //initialWeaponPosition = transform.localPosition;
        //initialWeaponRotation = transform.localRotation;
        //sprintTargetWeaponRotation = initialWeaponRotation;

        sprintTargetWeaponRotation = initial_rotation;

        inicialVector3 = current_multiplier;

        restarted = true;

    }


    void Update()
    {

        if (!restarted)
        {
            return;
        }


        if (playerProperties.sprinting == true && !playerProperties.is_reloading && switchWeapon._switch == false && !playerProperties.is_aiming)
        {
            bobExaggeration = weaponProperties.bob_sprint_exageration;
            current_multiplier = weaponProperties.sprint_multiplier;

            Sprinting();
        }
        else
        {
            current_position_sprinting = 0f;
            bobExaggeration = weaponProperties.bob_walk_exageration;

            //sprintTargetWeaponRotation = initialWeaponRotation;
            //sprintTargetWeaponPosition = initialWeaponPosition;

            sprintTargetWeaponPosition = initial_position;
            sprintTargetWeaponRotation = initial_rotation;

            current_multiplier.x = inicialVector3.x;
            current_multiplier.y = inicialVector3.y;
            current_multiplier.z = inicialVector3.z;
        }

        if ((playerProperties.crouched == true && playerProperties.sprinting == false) || playerProperties.is_aiming)
        {

            bobExaggeration = weaponProperties.bob_crouch_exageration;
            current_multiplier = weaponProperties.crouch_multiplier;


        }

        if (playerController.moveForward == 0f && playerController.moveHorizontal == 0f)
        {
            bobExaggeration = 0f;
            //sprintTargetWeaponRotation = initialWeaponRotation;
            //sprintTargetWeaponPosition = initialWeaponPosition;
            sprintTargetWeaponPosition = initial_position;
            sprintTargetWeaponRotation = initial_rotation;

            current_multiplier.x = 0.1f;
            current_multiplier.y = 0.1f;
            current_multiplier.z = 0.1f;

        }

        if (playerProperties.is_aiming && (playerController.moveForward != 0f || playerController.moveHorizontal != 0f))
        {

            bobExaggeration = weaponProperties.bob_aim_exageration;
            current_multiplier.x = weaponProperties.aim_multiplier.x;
            current_multiplier.y = weaponProperties.aim_multiplier.y;
            current_multiplier.z = weaponProperties.aim_multiplier.z;
        }

        SwayRotation();
        Sway();

        BobOffset();
        BobRotation();

        GetInput();
        CompositePositionRotation();
        //JumpOffset();

        transform.localPosition = Vector3.Lerp(transform.localPosition,
        new Vector3(transform.localPosition.x + current_position_sprinting, transform.localPosition.y, transform.localPosition.z),
        Time.deltaTime
        );

    }


    Vector2 walkInput;
    Vector2 lookInput;

    void Sprinting()
    {
        //sprintTargetWeaponPosition = initialWeaponPosition + new Vector3(weaponProperties.vector3Values[0], weaponProperties.vector3Values[1], weaponProperties.vector3Values[2]);
        //sprintTargetWeaponRotation = initialWeaponRotation * Quaternion.Euler(weaponProperties.quaternionValues[0], weaponProperties.quaternionValues[1], weaponProperties.quaternionValues[2]);

        sprintTargetWeaponPosition = initial_position + new Vector3(weaponProperties.vector3Values[0], weaponProperties.vector3Values[1], weaponProperties.vector3Values[2]);
        sprintTargetWeaponRotation = initial_rotation * Quaternion.Euler(weaponProperties.quaternionValues[0], weaponProperties.quaternionValues[1], weaponProperties.quaternionValues[2]);

        
        current_position_sprinting += sprintDirection * Time.deltaTime * 8;


        
        // inverter direção quando chegar nos limites
        if (playerProperties.isGrounded)
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
        Quaternion combinedRotation;
        Vector3 combinedPosition;

        if (!playerProperties.is_aiming)
        {
            combinedRotation = sprintTargetWeaponRotation * Quaternion.Euler(swayEulerRot) * Quaternion.Euler(bobEulerRotation);
            combinedPosition = sprintTargetWeaponPosition + swayPos + bobPosition;
        }
        else
        {
            //combinedRotation = initial_rotation;
            //combinedPosition = initial_position;
            if (playerProperties.is_firing)
            {
                combinedRotation = sprintTargetWeaponRotation * Quaternion.Euler(swayEulerRot / 20) * Quaternion.Euler(bobEulerRotation / 20);
                combinedPosition = sprintTargetWeaponPosition + swayPos / 20 + bobPosition / 20;
            }
            else
            {
                combinedRotation = sprintTargetWeaponRotation * Quaternion.Euler(swayEulerRot / 4) * Quaternion.Euler(bobEulerRotation / 4);
                combinedPosition = sprintTargetWeaponPosition + swayPos / 4 + bobPosition / 4;
            }


        }

        if (!playerProperties.isGrounded)
        {
            float tiltAmount = Input.GetAxis("Horizontal") * 10f;

            Quaternion targetRotation = Quaternion.Euler(
                15,
                Input.GetAxis("Mouse X") * 2,
                -tiltAmount
            );

            transform.localRotation = Quaternion.Lerp(
            transform.localRotation,
            combinedRotation * targetRotation,
            Time.deltaTime * smoothRot
            );



            transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            new Vector3(combinedPosition.x, combinedPosition.y - 0.01f, combinedPosition.z),
            Time.deltaTime * smooth
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

            transform.localRotation = Quaternion.Lerp(
            transform.localRotation,
            combinedRotation,
            Time.deltaTime * smoothRot
            );

            transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            combinedPosition,
            Time.deltaTime * smooth
            );

            //Reticle

            reticle.transform.localPosition = Vector3.Lerp(
            reticle.transform.localPosition,
            new Vector3(combinedPosition.x/2, combinedPosition.y/2, reticle.transform.localPosition.z),
            Time.deltaTime * smooth
            );

        }

    }

    void BobOffset()
    {

        float axis_bob = 0;

        //axis_bob = Math.Clamp(axis_bob, -1, 1);
        if (Input.GetAxis("Vertical") != 0)
        {
            axis_bob = 1;
        }

        if (Input.GetAxis("Horizontal") != 0)
        {
            axis_bob = 1;
        }

        speedCurve += Time.deltaTime * (playerProperties.isGrounded ? axis_bob * bobExaggeration : 1f) + 0.005f;

        bobPosition.x = (curveCos * bobLimit.x * (playerProperties.isGrounded ? 1 : 0)) - (walkInput.x * travelLimit.x);
        bobPosition.y = (curveSin * bobLimit.y) - (Input.GetAxis("Vertical") * travelLimit.y);
        bobPosition.z = -(walkInput.y * travelLimit.z);
    }


    void BobRotation()
    {

        bobEulerRotation.x = walkInput != Vector2.zero ? current_multiplier.x * (Mathf.Sin(2 * speedCurve)) : current_multiplier.x * (Mathf.Sin(2 * speedCurve) / 2);
        bobEulerRotation.y = walkInput != Vector2.zero ? current_multiplier.y * curveCos : 0;
        bobEulerRotation.z = walkInput != Vector2.zero ? current_multiplier.z * curveCos * walkInput.x : 0;

    }

    IEnumerator JumpWeaponShake()
    {
        Quaternion originalRot = weaponProperties.weapon.transform.localRotation;

        Quaternion upRot = originalRot * Quaternion.Euler(UnityEngine.Random.Range(-2f, 2f), UnityEngine.Random.Range(-2f, 2f), UnityEngine.Random.Range(-2f, 2f));

        float duration = 0.1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            weaponProperties.weapon.transform.localRotation = Quaternion.Lerp(originalRot, upRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        weaponProperties.weapon.transform.localRotation = upRot;

        elapsed = 0f;
        while (elapsed < duration)
        {
            weaponProperties.weapon.transform.localRotation = Quaternion.Lerp(upRot, originalRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        weaponProperties.weapon.transform.localRotation = originalRot;
    }


}