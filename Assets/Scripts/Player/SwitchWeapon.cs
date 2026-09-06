using UnityEngine;

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

    [Header("Instances")]
    [SerializeField] private ThirdPersonArms thirdPersonArms;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerProperties playerProperties;
    [SerializeField] private SwayNBobScript sway;
    [SerializeField] private Weapon weapon;

    [HideInInspector] public int currentWeapon = 1;
    [HideInInspector] public bool _switch = false;
    public bool IsSwitchingWeapon => _switch || isReturning;

    private bool isReturning = false;
    private float switchTimer = 0f;
    private float returnTimer = 0f; // Timer para a animação de sacar
    private bool setupOnce = true;

    // Controlo de tempos dinâmicos em segundos
    private float currentStoreDuration = 0.5f;
    private float currentPickUpDuration = 0.5f;
    private int targetWeapon = 1;

    private WeaponProperties weaponProperties;

    public enum WeaponSlot
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

        InstantiateThirdPersonWeapons();

        currentWeapon = 1;
        targetWeapon = 1;

        // Configura os tempos iniciais caso a inicialização dispare a animação
        currentStoreDuration = GetStoreSpeed(currentWeapon);
        currentPickUpDuration = GetDrawSpeed(targetWeapon);

        playerProperties.reloading = false;
        playerProperties.firing = false;

        StartInitialWeaponDraw();
    }

    private void StartInitialWeaponDraw()
    {
        if (primary == null)
        {
            _switch = false;
            isReturning = false;
            return;
        }

        playerProperties.aiming = false;
        if (weapon != null) weapon.can_shoot = false;
        sway?.EnableStoreWeapon(false);

        PlaySwitchEffects();
        ActivateSelectedWeapon();
        ResetSwitchState();

        EquippableItemAnimator initialAnimation = GetWeaponAnimationBySlot(currentWeapon);
        if (initialAnimation != null)
            initialAnimation.StartDrawAnimation(currentPickUpDuration);
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

            if (primary != null)
            {
                WeaponProperties wp = primary.GetComponent<WeaponProperties>();

                wp.Initialize();

                foreach (Attatchment a in wp.GetComponentsInChildren<Attatchment>(true))
                {
                    if (a.gameObject.activeSelf) a.Initialize();
                    else Destroy(a.gameObject);
                }

                primary.SetActive(false);
            }
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

            if (secondary != null)
            {
                WeaponProperties wp = secondary.GetComponent<WeaponProperties>();

                wp.Initialize();

                foreach (Attatchment a in wp.GetComponentsInChildren<Attatchment>(true))
                {
                    if (a.gameObject.activeSelf) a.Initialize();
                    else Destroy(a.gameObject);
                }

                secondary.SetActive(false);
            }
        }
    }

    public void InstantiateGadget1()
    {
        if (gadget1 != null)
        {
            GameObject g = Instantiate(gadget1, gadgets_parent);
            gadget1 = g;

            if (gadget1 != null)
            {
                Gadget gadget = gadget1.GetComponent<Gadget>();
                gadget.Initialize();

                gadget1.SetActive(false);
            }
        }
    }

    public void InstantiateGadget2()
    {
        if (gadget2 != null)
        {
            GameObject g = Instantiate(gadget2, gadgets_parent);
            gadget2 = g;

            if (gadget2 != null)
            {
                Gadget gadget = gadget2.GetComponent<Gadget>();
                gadget.Initialize();

                gadget2.SetActive(false);
            }
        }
    }

    private void InstantiateThirdPersonWeapons()
    {
        GameObject thirdPersonPrimary = null;
        GameObject thirdPersonSecondary = null;
        GameObject thirdPersonGadget1 = null;
        GameObject thirdPersonGadget2 = null;

        if (primary != null)
        {
            WeaponProperties primaryWP = primary.GetComponent<WeaponProperties>();
            if (primaryWP != null && primaryWP.thirdPersonPrefab != null) thirdPersonPrimary = primaryWP.thirdPersonPrefab;
        }

        if (secondary != null)
        {
            WeaponProperties secondaryWP = secondary.GetComponent<WeaponProperties>();
            if (secondaryWP != null && secondaryWP.thirdPersonPrefab != null) thirdPersonSecondary = secondaryWP.thirdPersonPrefab;
        }

        if (gadget1 != null)
        {
            Gadget gadget1WP = primary.GetComponent<Gadget>();
            if (gadget1WP != null && gadget1WP.thirdPersonPrefab != null) thirdPersonGadget1 = gadget1WP.thirdPersonPrefab;
        }

        if (gadget2 != null)
        {
            Gadget gadget2WP = primary.GetComponent<Gadget>();
            if (gadget2WP != null && gadget2WP.thirdPersonPrefab != null) thirdPersonGadget2 = gadget2WP.thirdPersonPrefab;
        }

        thirdPersonArms.RequestInstantiateWeapons(thirdPersonPrimary,
                                                    thirdPersonSecondary,
                                                    thirdPersonGadget1,
                                                    thirdPersonGadget2);
    }

    void Update()
    {
        HandleWeaponSwitchInputManager();

        if (_switch) ProcessWeaponSwitch();

        if (isReturning) ReturnWeaponToPosition();
    }

    private void HandleWeaponSwitchInputManager()
    {
        // Bloqueia inputs se já estiver a trocar ou a sacar a arma
        if (_switch || isReturning || playerProperties.reloading || playerProperties.firing || playerProperties.isDead.Value) return;

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

        if (scrollDirection > 0f) nextWeapon = currentWeapon == 4 ? 1 : currentWeapon + 1;
        else if (scrollDirection < 0f) nextWeapon = currentWeapon == 1 ? 4 : currentWeapon - 1;

        if (nextWeapon != currentWeapon) StartWeaponSwitch(nextWeapon);

    }

    private void HandleNumberKeyInputManager()
    {
        if (InputManager.GetKeyDown(weapon1) && primary != null && currentWeapon != 1) StartWeaponSwitch((int)WeaponSlot.Primary);
        else if (InputManager.GetKeyDown(weapon2) && secondary != null && currentWeapon != 2) StartWeaponSwitch((int)WeaponSlot.Secondary);
        else if (InputManager.GetKeyDown(weapon3) && gadget1 != null && currentWeapon != 3) StartWeaponSwitch((int)WeaponSlot.Gadget1);
        else if (InputManager.GetKeyDown(weapon4) && gadget2 != null && currentWeapon != 4) StartWeaponSwitch((int)WeaponSlot.Gadget2);
    }

    // Centraliza o início da troca e aciona a animação de guardar
    private void StartWeaponSwitch(int nextWeaponSlot)
    {
        targetWeapon = nextWeaponSlot;

        // Obtém a velocidade de guardar do item ATUAL e de sacar do PRÓXIMO item
        currentStoreDuration = GetStoreSpeed(currentWeapon);
        currentPickUpDuration = GetDrawSpeed(targetWeapon);

        switchTimer = 0f;
        _switch = true;
        sway?.EnableStoreWeapon(false);
        AdsBehaviour.Instance?.CancelAim();

        // INICIA A ANIMAÇÃO DE GUARDAR
        EquippableItemAnimator anim = GetWeaponAnimationBySlot(currentWeapon);
        if (anim != null) anim.StartStoreAnimation(currentStoreDuration);
    }

    private void ProcessWeaponSwitch()
    {
        playerProperties.aiming = false;
        if (weapon != null) weapon.can_shoot = false;

        PlaySwitchEffects();

        switchTimer += Time.deltaTime;

        // Quando o timer termina, a arma já foi "guardada" pela animação
        if (switchTimer >= currentStoreDuration)
        {
            currentWeapon = targetWeapon;
            ActivateSelectedWeapon();
            ResetSwitchState();

            // INICIA A ANIMAÇÃO DE SACAR DA NOVA ARMA ATIVADA
            EquippableItemAnimator newAnim = GetWeaponAnimationBySlot(currentWeapon);
            if (newAnim != null) newAnim.StartDrawAnimation(currentPickUpDuration);
        }
    }

    private void PlaySwitchEffects()
    {
        if (setupOnce)
        {
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
                thirdPersonArms.RequestSwitchWeapon(WeaponSlot.Primary);
                SetupWeapon(primary, true);
                break;

            case WeaponSlot.Secondary:
                thirdPersonArms.RequestSwitchWeapon(WeaponSlot.Secondary);
                SetupWeapon(secondary, true);
                break;

            case WeaponSlot.Gadget1:
                thirdPersonArms.RequestSwitchWeapon(WeaponSlot.Gadget1);
                SetupGadget(gadget1);
                break;

            case WeaponSlot.Gadget2:
                thirdPersonArms.RequestSwitchWeapon(WeaponSlot.Gadget2);
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

        InitializeWeaponComponents(weaponObject);
        ConfigureSwayForWeapon();
        ResetWeaponState(weaponProperties);
    }

    private void SetupGadget(GameObject gadgetObject)
    {
        if (gadgetObject == null) return;

        gadgetObject.SetActive(true);
        weapon.is_active = false;

        InitializeGadgetComponents();
    }

    private void InitializeWeaponComponents(GameObject weaponObject) => weaponProperties = weaponObject.GetComponent<WeaponProperties>();
    private void ConfigureSwayForWeapon() => sway.Restart(weaponProperties.swayAndBobValues);

    private void InitializeGadgetComponents()
    {
        Gadget gadget = GetComponentInChildren<Gadget>();
        if (gadget != null)
        {
            gadget.SetActive(true);
            gadget.Restart();
            ConfigureSwayForGadget(gadget);
        }
    }

    private void ConfigureSwayForGadget(Gadget gadget) => sway.Restart(gadget.swayAndBobValues);

    private void ResetWeaponState(WeaponProperties wp)
    {
        if (weapon != null)
        {
            weapon.Restart(wp);
            weapon.can_shoot = false;
        }

        if (weaponProperties != null)
        {
            playerController.UpdateWeaponProperties(weaponProperties.speedChange, weaponProperties.recoilValues.applyRecoilSpeed, weaponProperties.recoilValues.resetRecoilSpeed);
            EquippableItemHandTargets wh = weaponProperties.GetComponent<EquippableItemHandTargets>();
            wh.ResetHandTargets();
        }
    }

    private void ResetSwitchState()
    {
        switchTimer = 0f;
        returnTimer = 0f;
        isReturning = true;
        _switch = false;
        setupOnce = true;
    }


    private void ReturnWeaponToPosition()
    {
        returnTimer += Time.deltaTime;

        if (returnTimer >= currentPickUpDuration) CompleteWeaponSwitch();

    }

    private void CompleteWeaponSwitch()
    {
        EquippableItemAnimator currentAnimation = GetWeaponAnimationBySlot(currentWeapon);
        if (currentAnimation != null) currentAnimation.FinishDrawAnimation();

        if (weapon != null)
        {
            weapon.can_shoot = true;
            weapon.can_aim = true;
        }

        sway?.EnableStoreWeapon(true);

        isReturning = false;
    }

    private void SetWeaponActive(GameObject weaponObject, bool active)
    {
        if (weaponObject != null) weaponObject.SetActive(active);
    }

    #region Métodos Auxiliares de Captura
    private float GetStoreSpeed(int slot)
    {
        GameObject obj = GetWeaponObjectBySlot(slot);
        if (obj == null) return 0.3f;

        var wp = obj.GetComponentInChildren<WeaponProperties>();
        if (wp != null) return wp.storeWeaponSpeed;

        var gd = obj.GetComponentInChildren<Gadget>();
        if (gd != null) return gd.StoreGadgetSpeed;

        return 0.3f;
    }

    private float GetDrawSpeed(int slot)
    {
        GameObject obj = GetWeaponObjectBySlot(slot);
        if (obj == null) return 0.3f;

        var wp = obj.GetComponentInChildren<WeaponProperties>();
        if (wp != null) return wp.drawWeaponSpeed;

        var gd = obj.GetComponentInChildren<Gadget>();
        if (gd != null) return gd.drawGadgetSped;

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

    private EquippableItemAnimator GetWeaponAnimationBySlot(int slot)
    {
        GameObject obj = GetWeaponObjectBySlot(slot);
        if (obj == null) return null;

        return obj.GetComponent<EquippableItemAnimator>() ?? obj.GetComponentInChildren<EquippableItemAnimator>();
    }
    #endregion
}
