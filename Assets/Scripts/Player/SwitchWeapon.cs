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
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerProperties playerProperties;
    [SerializeField] private WeaponAnimation weaponAnimation;
    [SerializeField] private SwayNBobScript sway;
    [SerializeField] private Weapon weapon;

    [HideInInspector] public int currentWeapon = 1;
    [HideInInspector] public bool _switch = false;

    private bool isReturning = false;
    private float switchTimer = 0f;
    private float returnTimer = 0f; // Novo timer para a animação de sacar
    private bool setupOnce = true;

    // Controlo de tempos dinâmicos em segundos
    private float currentStoreDuration = 0.5f;
    private float currentPickUpDuration = 0.5f;
    private int targetWeapon = 1;

    private WeaponProperties weaponProperties;

    private Vector3 originalPosition;
    private Quaternion originalQuaternionRotation;
    private readonly Quaternion saveQuaternionRotation = new(4, 0, 0, 1);
    private Vector3 actualSavePosition;
    private enum WeaponSlot
    {
        Primary = 1,
        Secondary = 2,
        Gadget1 = 3,
        Gadget2 = 4
    }

    public void Initialize()
    {
        InstantiatePrimaryWeapon();
        InstantiateSecodnaryWeapon();
        InstantiateGadget1();
        InstantiateGadget2();

        currentWeapon = 1;
        targetWeapon = 1;

        // Configura os tempos iniciais caso a inicialização dispare a animação
        currentStoreDuration = GetStoreSpeed(currentWeapon);
        currentPickUpDuration = GetPickUpSpeed(targetWeapon);

        _switch = true;
        playerProperties.is_reloading = false;
        playerProperties.is_firing = false;
    }

    public void InstantiatePrimaryWeapon()
    {
        if (primary != null)
        {
            GameObject g = Instantiate(primary, weapons_parent);
            AttatchmentManager attManager = g.GetComponent<AttatchmentManager>();
            if (attManager != null)
            {
                attManager.InitializeAttachments();
                attManager.LoadAttachmentsFromPlayerPrefs();
            }
            primary = g;
        }
    }

    public void InstantiateSecodnaryWeapon()
    {
        if (secondary != null)
        {
            GameObject g = Instantiate(secondary, weapons_parent);
            AttatchmentManager attManager = g.GetComponent<AttatchmentManager>();
            if (attManager != null)
            {
                attManager.InitializeAttachments();
                attManager.LoadAttachmentsFromPlayerPrefs();
            }
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
        HandleWeaponSwitchInputManager();

        if (_switch)
        {
            ProcessWeaponSwitch();
        }

        if (isReturning)
        {
            ReturnWeaponToPosition();
        }
    }

    private void HandleWeaponSwitchInputManager()
    {
        // Bloqueia inputs se já estiver a trocar ou a sacar a arma
        if (_switch || isReturning || playerProperties.is_reloading || playerProperties.is_firing || playerProperties.is_dead.Value)
            return;

        float scrollY = InputManager.GetMouseScroll();

        if (scrollY != 0f)
        {
            SwitchWeaponByScroll(scrollY);
            return;
        }

        HandleNumberKeyInputManager();
    }

    private void SwitchWeaponByScroll(float scrollDirection)
    {
        int nextWeapon = currentWeapon;

        if (scrollDirection > 0f)
        {
            nextWeapon = currentWeapon == 4 ? 1 : currentWeapon + 1;
        }
        else if (scrollDirection < 0f)
        {
            nextWeapon = currentWeapon == 1 ? 4 : currentWeapon - 1;
        }

        if (nextWeapon != currentWeapon)
        {
            StartWeaponSwitch(nextWeapon);
        }
    }

    private void HandleNumberKeyInputManager()
    {
        if (InputManager.GetKeyDown(weapon1) && primary != null && currentWeapon != 1)
        {
            StartWeaponSwitch((int)WeaponSlot.Primary);
        }
        else if (InputManager.GetKeyDown(weapon2) && secondary != null && currentWeapon != 2)
        {
            StartWeaponSwitch((int)WeaponSlot.Secondary);
        }
        else if (InputManager.GetKeyDown(weapon3) && gadget1 != null && currentWeapon != 3)
        {
            StartWeaponSwitch((int)WeaponSlot.Gadget1);
        }
        else if (InputManager.GetKeyDown(weapon4) && gadget2 != null && currentWeapon != 4)
        {
            StartWeaponSwitch((int)WeaponSlot.Gadget2);
        }
    }

    // Centraliza o início da troca calculando os tempos em segundos de cada objeto
    private void StartWeaponSwitch(int nextWeaponSlot)
    {
        targetWeapon = nextWeaponSlot;

        // Obtém a velocidade de guardar do item ATUAL e de sacar do PRÓXIMO item
        currentStoreDuration = GetStoreSpeed(currentWeapon);
        currentPickUpDuration = GetPickUpSpeed(targetWeapon);

        switchTimer = 0f;
        _switch = true;
    }

    private void ProcessWeaponSwitch()
    {
        playerProperties.is_aiming = false;
        if (weapon != null) weapon.can_shoot = false;

        StoreOriginalTransform();
        PlaySwitchEffects();

        switchTimer += Time.deltaTime;

        float t = Mathf.Clamp01(switchTimer / currentStoreDuration);

        // Deixa o movimento suave (acelera no início e desacelera no fim)
        float smoothT = Mathf.SmoothStep(0f, 1f, t);

        // Interpola usando o destino corrigido (actualSavePosition)
        transform.localPosition = Vector3.Lerp(originalPosition, actualSavePosition, smoothT);
        transform.localRotation = Quaternion.Lerp(originalQuaternionRotation, saveQuaternionRotation, smoothT);

        if (switchTimer >= currentStoreDuration)
        {
            currentWeapon = targetWeapon;
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

            // CORREÇÃO: Em vez de ir para -30 (que joga a arma no limbo instantaneamente),
            // fazemos ela descer apenas 2 unidades abaixo da sua posição original atual.
            // Você pode ajustar este -2f para mais ou para menos se a arma ainda aparecer na tela.
            actualSavePosition = new Vector3(originalPosition.x, originalPosition.y - 0.1f, originalPosition.z);
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
        if (weaponProperties != null)
        {
            weaponProperties.Restart();
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
            gadget.Reestart();
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
            WeaponHolder wh = weaponProperties.GetComponent<WeaponHolder>();
            wh.ResetWeaponState();
        }
    }

    private void ResetSwitchState()
    {
        switchTimer = 0f;
        returnTimer = 0f; // Inicializa o cronómetro de retorno
        isReturning = true;
        _switch = false;
        setupOnce = true;
    }

    private void ReturnWeaponToPosition()
    {
        returnTimer += Time.deltaTime;

        float t = Mathf.Clamp01(returnTimer / currentPickUpDuration);

        // Deixa a subida da nova arma suave também
        float smoothT = Mathf.SmoothStep(0f, 1f, t);

        // Saca a arma a partir do destino corrigido de volta para a posição original
        transform.localPosition = Vector3.Lerp(actualSavePosition, originalPosition, smoothT);
        transform.localRotation = Quaternion.Lerp(saveQuaternionRotation, originalQuaternionRotation, smoothT);

        if (returnTimer >= currentPickUpDuration)
        {
            CompleteWeaponSwitch();
        }
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
    }

    private void SetWeaponActive(GameObject weaponObject, bool active)
    {
        if (weaponObject != null)
        {
            weaponObject.SetActive(active);
        }
    }

    #region Métodos Auxiliares de Captura de Tempo

    private float GetStoreSpeed(int slot)
    {
        GameObject obj = GetWeaponObjectBySlot(slot);
        if (obj == null) return 0.3f; // Fallback caso o slot esteja vazio

        var wp = obj.GetComponentInChildren<WeaponProperties>();
        if (wp != null) return wp.store_weapon_speed;

        var gd = obj.GetComponentInChildren<Gadget>();
        if (gd != null) return gd.store_gadget_speed;

        return 0.3f;
    }

    private float GetPickUpSpeed(int slot)
    {
        GameObject obj = GetWeaponObjectBySlot(slot);
        if (obj == null) return 0.3f;

        var wp = obj.GetComponentInChildren<WeaponProperties>();
        if (wp != null) return wp.pick_up_weapon_speed;

        var gd = obj.GetComponentInChildren<Gadget>();
        if (gd != null) return gd.pick_up_gadget_speed;

        return 0.3f;
    }

    private GameObject GetWeaponObjectBySlot(int slot)
    {
        return slot switch
        {
            1 => primary,
            2 => secondary,
            3 => gadget1,
            4 => gadget2,
            _ => null
        };
    }

    #endregion
}