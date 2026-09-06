using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CameraZoomController))]
public class AdsBehaviour : InMatchClientSingleton<AdsBehaviour>
{

    [Header("Instances")]
    [SerializeField] private PlayerProperties playerProperties;
    [SerializeField] private SwitchWeapon switchWeapon;

    [Header("Smooth Settings")]
    [Tooltip("Curve used to customize sight acceleration/deceleration directly in the Inspector.")]
    [SerializeField] private AnimationCurve aimCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Time, in seconds, to switch between the primary and canted sights.")]
    [SerializeField, Min(0f)] private float sightSwitchDuration = 0.2f;
    [Tooltip("Time to reset the canted sight roll during reloading.")]
    [SerializeField, Min(0f)] private float reloadSightReturnDuration = 0.2f;

    public bool dot_position { get; private set; }

    private float adsTimer;
    private Transform adsReference;
    private EquippableItemAudio equippableItemAudio;
    private Sight primarySight;
    private Sight currentSight;
    private CantedSight cantedSight;
    private Transform weaponTransform;
    private float cantedSightRollAngle;
    private float currentSightRollAngle;
    private float appliedSightRollAngle;
    private Quaternion lastSightRollRotation;
    private bool hasAppliedSightRoll;
    private bool isUsingCantedSight;

    private bool aiming;
    private Vector3 original_ads_position;
    [SerializeField] private float zOffset;
    private Coroutine aimCoroutine;
    private Coroutine sightSwitchCoroutine;
    private Coroutine reloadSightRollCoroutine;
    private bool isAimTransitionActive;
    private bool canReloadAiming;
    private bool canAim;
    private bool wasReloading;
    private float reloadSightRollBlend = 1f;
    private CameraZoomController cameraZoomController;

    public void Setup(Transform adsReference, float adsTimer, float zoom, bool canReloadAiming, EquippableItemAudio equippableItemAudio)
    {
        ResetSightSwitchState();
        ClearSightReferences();
        ConfigureAim(adsReference, adsTimer, zoom, canReloadAiming, equippableItemAudio);
    }

    public void Setup(
        Sight primarySight,
        CantedSight cantedSight,
        WeaponProperties weaponProperties,
        float adsTimer,
        bool canReloadAiming,
        EquippableItemAudio equippableItemAudio)
    {
        ResetSightSwitchState();

        this.primarySight = primarySight;
        currentSight = primarySight;
        this.cantedSight = cantedSight;
        weaponTransform = weaponProperties != null ? weaponProperties.transform : null;
        isUsingCantedSight = false;

        if (weaponTransform != null)
        {
            cantedSightRollAngle = CalculateCantedRollAngle();
        }

        ConfigureAim(
            primarySight.adsPosition,
            adsTimer,
            GetZoomForSight(primarySight),
            canReloadAiming,
            equippableItemAudio);
    }

    private void ConfigureAim(Transform adsReference, float adsTimer, float zoom, bool canReloadAiming, EquippableItemAudio equippableItemAudio)
    {
        canAim = true;
        this.adsReference = adsReference;
        this.adsTimer = adsTimer;
        this.canReloadAiming = canReloadAiming;
        this.equippableItemAudio = equippableItemAudio;
        wasReloading = playerProperties != null && playerProperties.reloading;
        reloadSightRollBlend = wasReloading && canReloadAiming ? 0f : 1f;
        cameraZoomController.SetZoomMultiplier(zoom);
    }

    protected override void Awake()
    {
        base.Awake();
        cameraZoomController = GetComponent<CameraZoomController>();
        if (cameraZoomController == null) cameraZoomController = gameObject.AddComponent<CameraZoomController>();
        original_ads_position = transform.localPosition;

        StartCoroutine(DelayToSetCameraZoomControllerbaseFov());
    }

    private void OnDisable() => ResetSightSwitchState();

    private IEnumerator DelayToSetCameraZoomControllerbaseFov()
    {
        while (Settings.Instance == null)
        {
            yield return null;
        }

        cameraZoomController.SetBaseFOV(Settings.Instance._video.infantary_fov);

    }

    void Update()
    {
        RemoveAppliedSightRoll();
        HandleReloadSightRoll();

        if (adsReference == null || !canAim) return;

        if (SettingsHUD.Instance.is_menu_settings_active)
        {
            StopAiming();
            return;
        }

        HandleSightSwitchInput();
        HandleAimInput();
    }

    private void LateUpdate()
    {
        ApplySightRoll();
    }

    private void HandleSightSwitchInput()
    {
        if (primarySight == null || cantedSight == null || weaponTransform == null || !playerProperties.aiming) return;
        if (primarySight.adsPosition == null || cantedSight.adsPosition == null) return;
        if (sightSwitchCoroutine != null || switchWeapon == null || switchWeapon.IsSwitchingWeapon) return;
        if (!InputManager.GetKeyDown(Settings.Instance._keybinds.WEAPON_switchSightKey)) return;

        bool useCantedSight = !isUsingCantedSight;

        if (sightSwitchDuration <= 0f)
        {
            ApplySightSelection(useCantedSight);
            return;
        }

        sightSwitchCoroutine = StartCoroutine(AnimateSightSwitch(useCantedSight));
    }

    private IEnumerator AnimateSightSwitch(bool useCantedSight)
    {
        float startRollAngle = currentSightRollAngle;
        Vector3 startAdsLocalPosition = transform.localPosition;

        currentSight = useCantedSight ? cantedSight : primarySight;
        adsReference = currentSight.adsPosition;
        dot_position = false;

        if (aimCoroutine != null)
        {
            StopCoroutine(aimCoroutine);
            aimCoroutine = null;
        }

        float elapsed = 0f;

        while (elapsed < sightSwitchDuration)
        {
            if (weaponTransform == null)
            {
                sightSwitchCoroutine = null;
                yield break;
            }

            elapsed += Time.deltaTime;
            float linearT = Mathf.Clamp01(elapsed / sightSwitchDuration);
            float smoothT = aimCurve.Evaluate(linearT);
            float targetRollAngle = GetDesiredSightRollAngle(
                useCantedSight,
                playerProperties != null && playerProperties.aiming);
            currentSightRollAngle = Mathf.LerpAngle(startRollAngle, targetRollAngle, smoothT);

            if (playerProperties != null && playerProperties.aiming)
            {
                if (aimCoroutine != null)
                {
                    StopCoroutine(aimCoroutine);
                    aimCoroutine = null;
                }

                Vector3 targetAdsLocalPosition = CalculateAimTargetLocalPosition();
                transform.localPosition = Vector3.Lerp(startAdsLocalPosition, targetAdsLocalPosition, smoothT);
            }

            yield return null;
        }

        isUsingCantedSight = useCantedSight;
        currentSightRollAngle = GetDesiredSightRollAngle(
            isUsingCantedSight,
            playerProperties != null && playerProperties.aiming);

        if (playerProperties != null && playerProperties.aiming)
        {
            transform.localPosition = CalculateAimTargetLocalPosition();
            dot_position = true;
            isAimTransitionActive = true;
            cameraZoomController.ZoomIn();
        }

        SetZoom(GetZoomForSight(currentSight));
        sightSwitchCoroutine = null;
    }

    private void ApplySightSelection(bool useCantedSight)
    {
        isUsingCantedSight = useCantedSight;
        currentSightRollAngle = GetDesiredSightRollAngle(
            isUsingCantedSight,
            playerProperties != null && playerProperties.aiming);
        currentSight = useCantedSight ? cantedSight : primarySight;
        adsReference = currentSight.adsPosition;

        if (playerProperties != null && playerProperties.aiming)
        {
            if (aimCoroutine != null)
            {
                StopCoroutine(aimCoroutine);
                aimCoroutine = null;
            }

            transform.localPosition = CalculateAimTargetLocalPosition();
            dot_position = true;
            isAimTransitionActive = true;
            cameraZoomController.ZoomIn();
        }

        SetZoom(GetZoomForSight(currentSight));
    }

    private float CalculateCantedRollAngle()
    {
        if (weaponTransform == null || cantedSight == null || primarySight == null) return 0f;

        Vector3 rotationAxis = weaponTransform.forward;
        Vector3 cantedUp = Vector3.ProjectOnPlane(cantedSight.transform.up, rotationAxis);
        Vector3 desiredUp = Vector3.ProjectOnPlane(primarySight.transform.up, rotationAxis);

        if (cantedUp.sqrMagnitude <= Mathf.Epsilon || desiredUp.sqrMagnitude <= Mathf.Epsilon)
            return 0f;

        return Vector3.SignedAngle(cantedUp, desiredUp, rotationAxis);
    }

    private void RemoveAppliedSightRoll()
    {
        if (!hasAppliedSightRoll || weaponTransform == null) return;

        // Se o Animator já sobrescreveu a pose desde o LateUpdate anterior,
        // o roll anterior não está mais presente e não deve ser removido.
        if (Quaternion.Angle(weaponTransform.localRotation, lastSightRollRotation) <= 0.001f)
        {
            Quaternion inverseRoll = Quaternion.Inverse(
                Quaternion.AngleAxis(appliedSightRollAngle, Vector3.forward));
            weaponTransform.localRotation *= inverseRoll;
        }

        hasAppliedSightRoll = false;
        appliedSightRollAngle = 0f;
    }

    private void ApplySightRoll()
    {
        if (weaponTransform == null || !weaponTransform.gameObject.activeInHierarchy) return;

        float visibleRollAngle = GetVisibleSightRollAngle();
        Quaternion roll = Quaternion.AngleAxis(visibleRollAngle, Vector3.forward);
        weaponTransform.localRotation *= roll;

        appliedSightRollAngle = visibleRollAngle;
        lastSightRollRotation = weaponTransform.localRotation;
        hasAppliedSightRoll = true;
    }

    private float GetDesiredSightRollAngle(bool useCantedSight, bool isAiming)
    {
        if (!useCantedSight || !isAiming) return 0f;
        return cantedSightRollAngle;
    }

    private float GetVisibleSightRollAngle() => currentSightRollAngle * reloadSightRollBlend;

    private void HandleReloadSightRoll()
    {
        if (playerProperties == null) return;

        bool isReloading = playerProperties.reloading;
        if (isReloading == wasReloading) return;

        wasReloading = isReloading;

        // Quando ADS durante recarga não é permitido, AnimateAim(false/true)
        // já faz o retorno suave completo da posição e do roll.
        float targetBlend = isReloading && canReloadAiming ? 0f : 1f;

        if (reloadSightRollCoroutine != null)
        {
            StopCoroutine(reloadSightRollCoroutine);
            reloadSightRollCoroutine = null;
        }

        if (Mathf.Approximately(reloadSightRollBlend, targetBlend))
        {
            reloadSightRollBlend = targetBlend;
            return;
        }

        if (reloadSightReturnDuration <= 0f)
        {
            reloadSightRollBlend = targetBlend;
            reloadSightRollCoroutine = null;
            return;
        }

        reloadSightRollCoroutine = StartCoroutine(AnimateReloadSightRoll(targetBlend));
    }

    private IEnumerator AnimateReloadSightRoll(float targetBlend)
    {
        float startBlend = reloadSightRollBlend;
        float elapsed = 0f;

        while (elapsed < reloadSightReturnDuration)
        {
            elapsed += Time.deltaTime;
            float linearT = Mathf.Clamp01(elapsed / reloadSightReturnDuration);
            float smoothT = aimCurve.Evaluate(linearT);
            reloadSightRollBlend = Mathf.Lerp(startBlend, targetBlend, smoothT);
            yield return null;
        }

        reloadSightRollBlend = targetBlend;
        reloadSightRollCoroutine = null;
    }

    private Vector3 GetAdsReferenceWorldPosition()
    {
        if (adsReference == null) return Vector3.zero;
        float visibleRollAngle = GetVisibleSightRollAngle();
        if (weaponTransform == null || hasAppliedSightRoll || Mathf.Approximately(visibleRollAngle, 0f))
            return adsReference.position;

        // As coroutines são processadas antes do LateUpdate. Nesse momento o roll
        // foi temporariamente removido para o Animator avaliar sua pose, portanto
        // calculamos onde o AdsPosition estará depois que o roll for reaplicado.
        Vector3 offsetFromWeapon = adsReference.position - weaponTransform.position;
        Quaternion worldRoll = Quaternion.AngleAxis(visibleRollAngle, weaponTransform.forward);
        return weaponTransform.position + worldRoll * offsetFromWeapon;
    }

    private float GetZoomForSight(Sight sight)
    {
        if (sight == null) return 1f;
        return sight.zoomChange;
    }

    private void ResetSightSwitchState()
    {
        if (sightSwitchCoroutine != null)
        {
            StopCoroutine(sightSwitchCoroutine);
            sightSwitchCoroutine = null;
        }

        if (reloadSightRollCoroutine != null)
        {
            StopCoroutine(reloadSightRollCoroutine);
            reloadSightRollCoroutine = null;
        }

        RemoveAppliedSightRoll();
        currentSightRollAngle = 0f;
        cantedSightRollAngle = 0f;
        reloadSightRollBlend = 1f;
    }

    private void ClearSightReferences()
    {
        primarySight = null;
        currentSight = null;
        cantedSight = null;
        weaponTransform = null;
        currentSightRollAngle = 0f;
        cantedSightRollAngle = 0f;
        appliedSightRollAngle = 0f;
        hasAppliedSightRoll = false;
        reloadSightRollBlend = 1f;
        isUsingCantedSight = false;
    }

    private void HandleAimInput()
    {
        if (Settings.Instance._controls.is_aim_on_hold) AimWithHoldLogic();
        else AimWithToggleLogic();
    }

    private void AimWithHoldLogic()
    {
        bool canAim = CanAim();

        if (canAim && InputManager.GetKey(Settings.Instance._keybinds.WEAPON_aimKey)) StartAiming();
        else StopAiming();
    }

    private void AimWithToggleLogic()
    {
        bool canAim = CanAim();

        if (InputManager.GetKeyDown(Settings.Instance._keybinds.WEAPON_aimKey)) aiming = !aiming;


        if (!canAim) aiming = false;

        if (aiming) StartAiming();
        else StopAiming();
    }

    public bool CanAim()
    {
        if (switchWeapon == null || playerProperties == null) return false;

        if (canReloadAiming)
        {
            return !switchWeapon.IsSwitchingWeapon &&
               !playerProperties.isProneTransition &&
               !playerProperties.roll &&
               !playerProperties.isDead.Value;
        }

        return !switchWeapon.IsSwitchingWeapon &&
               !playerProperties.isProneTransition &&
               !playerProperties.roll &&
               !playerProperties.isDead.Value &&
               !playerProperties.reloading;

    }

    private void StartAiming()
    {
        if (adsReference == null) return;

        playerProperties.sprinting = false;
        playerProperties.aiming = true;

        if (!isAimTransitionActive && sightSwitchCoroutine == null)
        {
            if (aimCoroutine != null) StopCoroutine(aimCoroutine);
            equippableItemAudio?.AdsSound();
            aimCoroutine = StartCoroutine(AnimateAim(true));
        }
    }

    private void StopAiming()
    {
        if (playerProperties != null) playerProperties.aiming = false;

        aiming = false;
        dot_position = false;

        cameraZoomController.ZoomOut();

        if (isAimTransitionActive || (aimCoroutine == null && transform.localPosition != original_ads_position))
        {
            if (aimCoroutine != null) StopCoroutine(aimCoroutine);
            aimCoroutine = StartCoroutine(AnimateAim(false));
        }
    }

    private IEnumerator AnimateAim(bool aiming)
    {
        isAimTransitionActive = aiming;
        float elapsed = 0f;
        Vector3 startLocalPosition = transform.localPosition;
        float startRollAngle = currentSightRollAngle;

        while (elapsed < adsTimer)
        {
            if (adsReference == null) yield break;

            elapsed += Time.deltaTime;
            float linearT = elapsed / adsTimer;

            // --- MELHORIA DE SUAVIDADE 1 ---
            // Avalia o tempo linear na curva cinematográfica (Ease In Out)
            float smoothT = aimCurve.Evaluate(linearT);

            if (sightSwitchCoroutine == null)
            {
                float targetRollAngle = GetDesiredSightRollAngle(isUsingCantedSight, aiming);
                currentSightRollAngle = Mathf.LerpAngle(startRollAngle, targetRollAngle, smoothT);
            }

            if (aiming)
            {
                Vector3 targetLocalPosition = CalculateAimTargetLocalPosition();

                // Transiciona usando o t suavizado
                transform.localPosition = Vector3.Lerp(startLocalPosition, targetLocalPosition, smoothT);
            }
            else
            {
                // Retorno suave para o Hipfire
                transform.localPosition = Vector3.Lerp(startLocalPosition, original_ads_position, smoothT);
            }

            yield return null;
        }

        // Ajuste milimétrico final
        if (sightSwitchCoroutine == null)
            currentSightRollAngle = GetDesiredSightRollAngle(isUsingCantedSight, aiming);

        if (aiming && adsReference != null)
        {
            transform.localPosition = CalculateAimTargetLocalPosition();

            dot_position = true;
            cameraZoomController.ZoomIn();
        }
        else if (!aiming)
        {
            transform.localPosition = original_ads_position;
            cameraZoomController.ZoomOut();
        }

        aimCoroutine = null;
    }

    private Vector3 CalculateAimTargetLocalPosition()
    {
        Vector3 centerGlobalTarget = transform.parent != null
            ? transform.parent.TransformPoint(original_ads_position)
            : original_ads_position;

        Vector3 forwardDirection = transform.parent != null ? transform.parent.forward : transform.forward;
        centerGlobalTarget += forwardDirection * zOffset;

        Vector3 offsetGlobal = GetAdsReferenceWorldPosition() - transform.position;
        Vector3 targetGlobalPosition = centerGlobalTarget - offsetGlobal;

        return transform.parent != null
            ? transform.parent.InverseTransformPoint(targetGlobalPosition)
            : targetGlobalPosition;
    }

    public void EnableAim() => canAim = true;
    public void DisableAim() => canAim = false;
    public void CancelAim() => StopAiming();
    public void SetZoom(float zoom) => cameraZoomController.SetZoomMultiplier(zoom);
    public bool IsCurrentSight(Sight sight) => sight != null && sight == currentSight;

    public void RefreshSightZoom(Sight sight)
    {
        if (!IsCurrentSight(sight)) return;
        SetZoom(GetZoomForSight(sight));
    }
}
