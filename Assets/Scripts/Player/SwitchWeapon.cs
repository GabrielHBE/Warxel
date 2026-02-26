using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SwitchWeapon : MonoBehaviour
{
    public Transform weapons_parent;
    public Transform gadgets_parent;
    [Header("Keycodes")]
    public KeyCode weapon1 = KeyCode.Alpha1;
    public KeyCode weapon2 = KeyCode.Alpha2;
    public KeyCode weapon3 = KeyCode.Alpha3;
    public KeyCode weapon4 = KeyCode.Alpha4;

    [Header("Weapons")]
    public GameObject primary;
    public GameObject secondary;
    public GameObject gadget1;
    public GameObject gadget2;


    [Header("Sounds")]
    public AudioSource zipper;

    [Header("Instances")]
    public Reticle reticle;
    public Transform save;

    [Header("Timing")]
    [SerializeField] private float switchDuration = 0.6f;
    [SerializeField] private float pickUpWeaponSpeed = 60f; // pick_up_weapon_timer * 100

    [HideInInspector] public int currentWeapon = 1;
    [HideInInspector] public bool _switch = false;

    private bool isReturning = false;
    private float switchTimer = 0f;
    private bool setupOnce = true;

    private Weapon weapon;
    private SwayNBobScript sway;
    private WeaponAnimation weaponAnimation;
    private WeaponProperties weaponProperties;
    private PlayerController playerController;
    private PlayerProperties playerProperties;
    private WeaponHolder weaponHolder;
    private Shell shell;

    private Vector3 originalPosition;
    private Quaternion originalQuaternionRotation;

    private readonly Vector3 savePosition = new(0, -30, 1); // Inicializado diretamente
    private readonly Quaternion saveQuaternionRotation = new(4, 0, 0, 1);

    private Settings settings;

    private enum WeaponSlot
    {
        Primary = 1,
        Secondary = 2,
        Gadget1 = 3,
        Gadget2 = 4
    }

    public void Awake()
    {

        settings = GameObject.FindGameObjectWithTag("GeneralHUD").GetComponent<Settings>();

        playerController = GetComponentInParent<PlayerController>();
        playerProperties = GetComponentInParent<PlayerProperties>();

        weaponAnimation = GetComponentInChildren<WeaponAnimation>();
        weapon = GetComponentInChildren<Weapon>();
        sway = GetComponentInChildren<SwayNBobScript>();


        InstantiatePrimaryWeapon();
        InstantiateSecodnaryWeapon();
        InstantiateGadget1();
        InstantiateGadget2();

        _switch = true;
        playerProperties.is_reloading = false;
        playerProperties.is_firing = false;
        currentWeapon = 1;
    }

    public void InstantiatePrimaryWeapon()
    {
        if (primary != null)
        {
            GameObject g = Instantiate(primary, weapons_parent);
            primary = g;
        }
    }

    public void InstantiateSecodnaryWeapon()
    {
        if (secondary != null)
        {
            GameObject g = Instantiate(secondary, weapons_parent);
            secondary = g;
            g.SetActive(false);
        }
    }

    public void InstantiateGadget1()
    {
        if (gadget1 != null)
        {
            GameObject g = Instantiate(gadget1, gadgets_parent);
            gadget1 = g;
            g.SetActive(false);
        }
    }
    public void InstantiateGadget2()
    {
        if (gadget2 != null)
        {
            GameObject g = Instantiate(gadget2, gadgets_parent);
            gadget2 = g;
            g.SetActive(false);
        }
    }

    void Update()
    {

        if (settings.is_menu_settings_active) return;
        HandleWeaponSwitchInput();

        if (_switch)
        {
            ProcessWeaponSwitch();
        }

        if (isReturning)
        {
            ReturnWeaponToPosition();
        }
    }

    private void HandleWeaponSwitchInput()
    {
        if (_switch || playerProperties.is_reloading || playerProperties.is_firing || playerProperties.is_dead)
            return;

        Vector2 scrollDelta = Mouse.current.scroll.ReadValue();
        float scrollY = scrollDelta.y;

        // Handle mouse wheel scrolling
        if (scrollY != 0f)
        {
            SwitchWeaponByScroll(scrollY);
            return;
        }

        // Handle number key presses
        HandleNumberKeyInput();
    }

    private void SwitchWeaponByScroll(float scrollDirection)
    {
        if (scrollDirection > 0f)
        {
            currentWeapon = currentWeapon == 4 ? 1 : currentWeapon + 1;
        }
        else if (scrollDirection < 0f)
        {
            currentWeapon = currentWeapon == 1 ? 4 : currentWeapon - 1;
        }

        _switch = true;
    }

    private void HandleNumberKeyInput()
    {
        if (Input.GetKeyDown(weapon1) && primary != null && currentWeapon != 1)
        {
            SwitchToWeapon(WeaponSlot.Primary);
        }
        else if (Input.GetKeyDown(weapon2) && secondary != null && currentWeapon != 2)
        {
            SwitchToWeapon(WeaponSlot.Secondary);
        }
        else if (Input.GetKeyDown(weapon3) && gadget1 != null && currentWeapon != 3)
        {
            SwitchToWeapon(WeaponSlot.Gadget1);
        }
        else if (Input.GetKeyDown(weapon4) && gadget2 != null && currentWeapon != 4)
        {
            SwitchToWeapon(WeaponSlot.Gadget2);
        }
    }

    private void SwitchToWeapon(WeaponSlot weaponSlot)
    {
        currentWeapon = (int)weaponSlot;
        _switch = true;
    }

    private void ProcessWeaponSwitch()
    {
        playerProperties.is_aiming = false;
        if (weapon != null) weapon.can_shoot = false;

        StoreOriginalTransform();
        PlaySwitchEffects();

        switchTimer += Time.deltaTime;

        if (switchTimer <= switchDuration)
        {
            AnimateWeaponHide();
        }
        else
        {
            ActivateSelectedWeapon();
            ResetSwitchState();
        }
    }

    private void StoreOriginalTransform()
    {
        if (setupOnce)
        {
            originalPosition = transform.localPosition;
            originalQuaternionRotation = transform.localRotation;
        }
    }

    private void PlaySwitchEffects()
    {
        if (setupOnce)
        {
            zipper.Play();
            sway.enabled = false;
            if (weapon != null) weapon.can_aim = false;
            setupOnce = false;
        }
    }

    private void AnimateWeaponHide()
    {
        float lerpSpeed = Time.deltaTime * 0.1f;
        transform.localPosition = Vector3.Lerp(transform.localPosition, savePosition, lerpSpeed);
        transform.localRotation = Quaternion.Lerp(transform.localRotation, saveQuaternionRotation, lerpSpeed);
    }

    private void ActivateSelectedWeapon()
    {
        DeactivateAllWeapons();

        switch ((WeaponSlot)currentWeapon)
        {
            case WeaponSlot.Primary:
                SetupWeapon(primary, true);
                break;

            case WeaponSlot.Secondary:
                SetupWeapon(secondary, true);
                break;

            case WeaponSlot.Gadget1:
                SetupGadget(gadget1);
                break;

            case WeaponSlot.Gadget2:
                SetupGadget(gadget2);
                break;
        }
    }

    private void DeactivateAllWeapons()
    {
        SetWeaponActive(primary, false);
        SetWeaponActive(secondary, false);
        SetWeaponActive(gadget1, false);
        SetWeaponActive(gadget2, false);
    }

    private void SetupWeapon(GameObject weaponObject, bool isWeaponActive)
    {
        if (weaponObject == null) return;

        weaponObject.SetActive(true);
        if (weapon != null) weapon.is_active = isWeaponActive;

        InitializeWeaponComponents();
        ConfigureSwayForWeapon();
        ResetWeaponState();
    }

    private void SetupGadget(GameObject gadgetObject)
    {
        if (gadgetObject == null) return;

        gadgetObject.SetActive(true);
        weapon.is_active = false;

        InitializeGadgetComponents();
    }

    private void InitializeWeaponComponents()
    {
        weaponProperties = GetComponentInChildren<WeaponProperties>();
        shell = GetComponentInChildren<Shell>();


        if (weaponProperties != null)
        {
            weaponProperties.Restart();
        }


        if (shell != null && weaponProperties != null)
        {
            shell.Restart(weaponProperties);
        }
    }

    private void ConfigureSwayForWeapon()
    {
        if (weaponProperties == null) return;

        sway.Restart(
            weaponProperties.weapon.transform,
            weaponProperties.bob_walk_exageration,
            weaponProperties.bob_sprint_exageration,
            weaponProperties.bob_crouch_exageration,
            weaponProperties.bob_aim_exageration,
            weaponProperties.walk_multiplier,
            weaponProperties.sprint_multiplier,
            weaponProperties.aim_multiplier,
            weaponProperties.crouch_multiplier,
            weaponProperties.vector3Values,
            weaponProperties.quaternionValues
        );
    }

    private void InitializeGadgetComponents()
    {
        Gadget gadget = GetComponentInChildren<Gadget>();
        if (gadget != null)
        {
            gadget.SetActive(true);
            ConfigureSwayForGadget(gadget);
        }
    }

    private void ConfigureSwayForGadget(Gadget gadget)
    {
        sway.Restart(
            gadget.GetTransform(),
            gadget.bob_walk_exageration,
            gadget.bob_sprint_exageration,
            gadget.bob_crouch_exageration,
            gadget.bob_aim_exageration,
            gadget.walk_multiplier,
            gadget.sprint_multiplier,
            gadget.aim_multiplier,
            gadget.crouch_multiplier,
            gadget.vector3Values,
            gadget.quaternionValues
        );
    }

    private void ResetWeaponState()
    {
        if (weapon != null)
        {
            weapon.Restart();
            weapon.can_shoot = false;
        }

        weaponAnimation?.Restart();
        reticle?.Restart();

        if (weaponProperties != null)
        {
            playerController.UpdateWeaponProperties(weaponProperties.speed_change, weaponProperties.weapon_apply_recoil_speed, weaponProperties.weapon_reset_recoil_speed);
        }

        weaponHolder = GetComponentInChildren<WeaponHolder>();
        weaponHolder?.SetHandsToWeapon(0);
        if (weapon != null) weapon.ads_position.transform.localPosition = Vector3.zero;
    }

    private void ResetSwitchState()
    {
        switchTimer = 0f;
        isReturning = true;
        _switch = false;
        setupOnce = true;
    }

    private void ReturnWeaponToPosition()
    {
        float returnSpeed = pickUpWeaponSpeed * Time.deltaTime;

        transform.localPosition = Vector3.Lerp(transform.localPosition, originalPosition, returnSpeed);
        transform.localRotation = Quaternion.Lerp(transform.localRotation, originalQuaternionRotation, returnSpeed);

        if (HasReturnedToOriginalPosition())
        {
            CompleteWeaponSwitch();
        }
    }

    private bool HasReturnedToOriginalPosition()
    {
        return Vector3.Distance(transform.localPosition, originalPosition) < 0.01f &&
               Quaternion.Angle(transform.localRotation, originalQuaternionRotation) < 0.1f;
    }

    private void CompleteWeaponSwitch()
    {
        if (weapon != null)
        {
            weapon.can_shoot = true;
            weapon.can_aim = true;
        }

        isReturning = false;
        sway.enabled = true;
        // StartCoroutine(cameraShake.PickWeaponShake());
    }

    private void SetWeaponActive(GameObject weaponObject, bool active)
    {
        if (weaponObject != null)
        {
            weaponObject.SetActive(active);
        }
    }
}