
using System.Collections;
using System.IO.Compression;
using UnityEngine;
using UnityEngine.Rendering;

/*
    This script provides jumping and movement in Unity 3D - Gatsby
*/

public class PlayerController : MonoBehaviour
{
    // Camera Rotation
    [Header("Camera")]
    public float mouseSensitivity = 2f;
    public float mouse_aim_sensitivity = 1f;
    public GameObject player_camera;
    private float current_mouse_sensitivity;
    private float verticalRotation = 0f;
    private Transform cameraTransform;

    // Ground Movement
    [Header("Movement")]
    private Rigidbody rb;
    public float MoveSpeed = 6f;
    public float sprint_multiplier = 1.7f;
    private float sprint_speed;
    public float crouch_speed = 2f;
    public float walk_speed = 6f;
    public float moveHorizontal;
    public float moveForward;
    public bool is_cruch_on_hold;
    public bool is_sprint_on_hold;
    // Jumping
    public float jumpForce = 2f;
    public float fallMultiplier = 2.5f; // Multiplies gravity when falling down
    public float ascendMultiplier = 2f; // Multiplies gravity for ascending to peak of jump
    private bool wasGroundedLastFrame = true;

    public LayerMask groundLayer;
    private float groundCheckTimer = 0f;
    private float groundCheckDelay = 0.3f;
    private float playerHeight;
    private float raycastDistance;

    [Header("Keycodes")]
    public KeyCode sprint_key;
    public KeyCode prone_key;
    public KeyCode jump_key;
    public KeyCode crouch_key;


    //Instances
    private PlayerProperties playerProperties;
    private CameraShake cameraShake;

    //Variables
    private float original_moveSpeed;
    private float recoilVerticalTarget;
    private float recoilVerticalCurrent;
    private float recoilSmoothTime = 0.05f;
    private float recoilVelocity;


    void Start()
    {
        original_moveSpeed = MoveSpeed;
        cameraShake = GetComponentInChildren<CameraShake>();
        playerProperties = GetComponent<PlayerProperties>();

        sprint_speed = MoveSpeed * sprint_multiplier;

        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        cameraTransform = Camera.main.transform;

        // Set the raycast to be slightly beneath the player's feet
        playerHeight = GetComponent<CapsuleCollider>().height * transform.localScale.y;
        raycastDistance = (playerHeight / 2) + 0.2f;

        // Hides the mouse
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ChangeWeaponVelocitySpeed(float speed)
    {
        MoveSpeed = original_moveSpeed + speed;
        sprint_speed = MoveSpeed * sprint_multiplier;
        
    }

    void Update()
    {

        moveHorizontal = Input.GetAxisRaw("Horizontal");
        moveForward = Input.GetAxisRaw("Vertical");
        ChangeSpeed();
        RotateCamera();

        if (Input.GetKeyDown(jump_key) && playerProperties.isGrounded)
        {
            Jump();

        }

        // Checking when we're on the ground and keeping track of our ground check delay
        if (groundCheckTimer <= 0f)
        {
            Vector3 rayOrigin = transform.position;
            bool isGroundedNow = Physics.Raycast(rayOrigin, Vector3.down, raycastDistance, groundLayer);

            // Se acabou de encostar no chão (transição de ar → chão)
            if (!wasGroundedLastFrame && isGroundedNow)
            {
                StartCoroutine(cameraShake.JumpCameraShake());
            }

            playerProperties.isGrounded = isGroundedNow;
            wasGroundedLastFrame = isGroundedNow;

            Debug.DrawLine(rayOrigin, rayOrigin + Vector3.down * raycastDistance, Color.blue);
        }
        else
        {
            groundCheckTimer -= Time.deltaTime;
        }



    }

    public void ApplyCameraRecoil(float vr, float hr)
    {

        recoilVerticalTarget += vr;

        transform.Rotate(0, hr, 0);

        //cameraTransform.localRotation *= Quaternion.Lerp(cameraTransform.localRotation, new Quaternion(cameraTransform.localRotation.x, cameraTransform.localRotation.y, cameraTransform.localRotation.z + Random.Range(-5,5), cameraTransform.localRotation.w), 2*Time.deltaTime);
        StartCoroutine(RotateZAxis());
    }

    IEnumerator RotateZAxis()
    {
        WeaponProperties weaponProperties = GetComponentInChildren<WeaponProperties>();
        float elapsed = 0f;
        float rannge = (weaponProperties.horizontal_recoil_media/2 + weaponProperties.vertical_recoil_media/2)/2;
        float z = Random.Range(-rannge, rannge);

        while (elapsed < weaponProperties.apply_recoil_speed)
        {
            elapsed += Time.deltaTime;

            cameraTransform.localRotation = Quaternion.Lerp(cameraTransform.localRotation,
            new Quaternion(cameraTransform.localRotation.x, cameraTransform.localRotation.y, z, cameraTransform.localRotation.w),
            elapsed);

            yield return null;
        }

        elapsed = 0f;

    
        while (elapsed < weaponProperties.reset_recoil_speed)
        {
            elapsed += Time.deltaTime;

            cameraTransform.localRotation = Quaternion.Lerp(cameraTransform.localRotation,
            new Quaternion(cameraTransform.localRotation.x, cameraTransform.localRotation.y, 0, cameraTransform.localRotation.w),
            elapsed);
            yield return null;
        }

    }




    void ChangeSpeed()
    {

        // TOGGLE: Sprint
        if (!is_sprint_on_hold && Input.GetKeyDown(sprint_key))
        {
            playerProperties.sprinting = !playerProperties.sprinting;
            playerProperties.crouched = false;
        }

        // HOLD: Sprint
        if (is_sprint_on_hold)
        {
            playerProperties.sprinting = Input.GetKey(sprint_key);
            if (playerProperties.sprinting)
                playerProperties.crouched = false;
        }


        // TOGGLE: Crouch
        if (!is_cruch_on_hold && Input.GetKeyDown(crouch_key))
        {
            playerProperties.crouched = !playerProperties.crouched;
            playerProperties.sprinting = false;
        }

        // HOLD: Crouch
        if (is_cruch_on_hold)
        {
            playerProperties.crouched = Input.GetKey(crouch_key);
            if (playerProperties.crouched)
                playerProperties.sprinting = false;
        }

        // === Atualiza velocidade com base no estado atual ===
        if (playerProperties.crouched)
        {
            MoveSpeed = crouch_speed;
        }
        else if (playerProperties.sprinting && !playerProperties.is_aiming && !playerProperties.is_reloading)
        {
            MoveSpeed = sprint_speed;
        }
        else
        {
            MoveSpeed = walk_speed;
        }
    }

    void FixedUpdate()
    {

        MovePlayer();
        ApplyJumpPhysics();
    }

    void MovePlayer()
    {
        transform.Translate(new Vector3(moveHorizontal, 0, moveForward) * (MoveSpeed) * Time.deltaTime);

        // If we aren't moving and are on the ground, stop velocity so we don't slide
        if (playerProperties.isGrounded && moveHorizontal == 0 && moveForward == 0)
        {
            playerProperties.sprinting = false;
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }

    }


    void RotateCamera()
    {
        current_mouse_sensitivity = playerProperties.is_aiming ? mouse_aim_sensitivity : mouseSensitivity;

        // Input do mouse
        float horizontalRotation = Input.GetAxis("Mouse X") * current_mouse_sensitivity;
        float mouseVertical = Input.GetAxis("Mouse Y") * current_mouse_sensitivity;

        // Rotação do corpo (horizontal)
        transform.Rotate(0, horizontalRotation, 0);

        // Suaviza o recoil vertical acumulado
        recoilVerticalCurrent = Mathf.SmoothDamp(recoilVerticalCurrent, recoilVerticalTarget, ref recoilVelocity, recoilSmoothTime);

        // Combina input do mouse com recoil vertical
        verticalRotation -= mouseVertical;
        verticalRotation -= recoilVerticalCurrent;

        // Zera após aplicar
        recoilVerticalTarget = 0f;

        // Clamp e aplica rotação
        verticalRotation = Mathf.Clamp(verticalRotation, -80f, 80f);
        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
    }


    void Jump()
    {
        StartCoroutine(cameraShake.JumpCameraShake());
        playerProperties.isGrounded = false;
        groundCheckTimer = groundCheckDelay;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z); // Initial burst for the jump
    }

    void ApplyJumpPhysics()
    {
        if (rb.linearVelocity.y < 0)
        {
            // Falling: Apply fall multiplier to make descent faster
            rb.linearVelocity += Vector3.up * Physics.gravity.y * fallMultiplier * Time.fixedDeltaTime;
        } // Rising
        else if (rb.linearVelocity.y > 0)
        {
            // Rising: Change multiplier to make player reach peak of jump faster
            rb.linearVelocity += Vector3.up * Physics.gravity.y * ascendMultiplier * Time.fixedDeltaTime;
        }
    }


}
