using System.Collections;
using UnityEngine;

public class AdsBehaviour : MonoBehaviour
{
    public static AdsBehaviour Instance { get; private set; }
    
    [Header("Instances")]
    [SerializeField] private Camera player_camera;
    [SerializeField] private PlayerProperties playerProperties;
    [SerializeField] private SwitchWeapon switchWeapon;

    [Header("Smooth Settings")]
    [Tooltip("Curva para customizar a aceleração/desaceleração da mira diretamente pelo Inspector.")]
    [SerializeField] private AnimationCurve aimCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float fovLerpSpeed = 12f; // Velocidade do amortecimento do FOV

    public bool dot_position { get; private set; }

    private float adsTimer;
    private Transform adsReference;

    private float minFov;
    private bool is_aiming;
    private Vector3 original_ads_position;
    private float zoom;
    [SerializeField] private float zOffset;
    private Coroutine aimCoroutine;
    private bool isAimTransitionActive;
    private float targetCameraFov;

    private bool canUpdate;

    public void Setup(Transform adsReference, float adsTimer, float zoom)
    {
        canUpdate = true;
        this.adsReference = adsReference;
        this.adsTimer = adsTimer;
        this.zoom = zoom;
    }

    void Awake()
    {
        Instance = this;
        minFov = Settings.Instance._video.infantary_fov;
        targetCameraFov = minFov; 
        original_ads_position = transform.localPosition;
    }

    void Update()
    {
        if (adsReference == null || !canUpdate) return;
        
        if (SettingsHUD.Instance.is_menu_settings_active)
        {
            StopAiming();
            return;
        }

        HandleAimInput();
        ApplyCameraFovLerp();
    }

    private void HandleAimInput()
    {
        if (Settings.Instance._controls.is_aim_on_hold)
        {
            AimWithHoldLogic();
        }
        else
        {
            AimWithToggleLogic();
        }
    }

    private void AimWithHoldLogic()
    {
        bool canAim = CanAim();

        if (canAim && InputManager.GetKey(Settings.Instance._keybinds.WEAPON_aimKey))
        {
            StartAiming();
        }
        else
        {
            StopAiming();
        }
    }

    private void AimWithToggleLogic()
    {
        bool canAim = CanAim();

        if (InputManager.GetKeyDown(Settings.Instance._keybinds.WEAPON_aimKey))
        {
            is_aiming = !is_aiming;
        }

        if (!canAim)
            is_aiming = false;

        if (is_aiming)
        {
            StartAiming();
        }
        else
        {
            StopAiming();
        }
    }

    public bool CanAim()
    {
        if (switchWeapon == null || playerProperties == null) return false;

        return !switchWeapon._switch &&
               !playerProperties.isProneTransition &&
               !playerProperties.roll &&
               !playerProperties.is_dead.Value &&
               !playerProperties.is_reloading;
    }

    private void StartAiming()
    {
        if (adsReference == null) return;

        playerProperties.sprinting = false;
        playerProperties.is_aiming = true;

        if (!isAimTransitionActive)
        {
            if (aimCoroutine != null) StopCoroutine(aimCoroutine);
            aimCoroutine = StartCoroutine(AnimateAim(true));
        }
    }

    private void StopAiming()
    {
        if (playerProperties != null)
        {
            playerProperties.is_aiming = false;
        }
        is_aiming = false;
        dot_position = false;

        targetCameraFov = minFov;

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

        while (elapsed < adsTimer)
        {
            if (adsReference == null) yield break;

            elapsed += Time.deltaTime;
            float linearT = elapsed / adsTimer;
            
            // --- MELHORIA DE SUAVIDADE 1 ---
            // Avalia o tempo linear na curva cinematográfica (Ease In Out)
            float smoothT = aimCurve.Evaluate(linearT);

            if (aiming)
            {
                Vector3 centerGlobalTarget = transform.parent != null 
                    ? transform.parent.TransformPoint(original_ads_position) 
                    : original_ads_position;

                Vector3 forwardDirection = transform.parent != null ? transform.parent.forward : transform.forward;
                centerGlobalTarget += forwardDirection * zOffset;

                Vector3 offsetGlobal = adsReference.position - transform.position;
                Vector3 targetGlobalPosition = centerGlobalTarget - offsetGlobal;

                Vector3 targetLocalPosition = transform.parent != null 
                    ? transform.parent.InverseTransformPoint(targetGlobalPosition) 
                    : targetGlobalPosition;

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
        if (aiming && adsReference != null)
        {
            Vector3 centerGlobalTarget = transform.parent != null ? transform.parent.TransformPoint(original_ads_position) : original_ads_position;
            Vector3 forwardDirection = transform.parent != null ? transform.parent.forward : transform.forward;
            centerGlobalTarget += forwardDirection * zOffset;

            Vector3 offsetGlobal = adsReference.position - transform.position;
            Vector3 targetGlobalPosition = centerGlobalTarget - offsetGlobal;
            
            transform.localPosition = transform.parent != null 
                ? transform.parent.InverseTransformPoint(targetGlobalPosition) 
                : targetGlobalPosition;

            dot_position = true;
            targetCameraFov = minFov / zoom;
        }
        else if (!aiming)
        {
            transform.localPosition = original_ads_position;
            targetCameraFov = minFov;
        }

        aimCoroutine = null;
    }

    private void ApplyCameraFovLerp()
    {
        if (player_camera == null) return;

        // --- MELHORIA DE SUAVIDADE 2 ---
        // Alterado de interpolação dependente de frame rígido para um amortecimento progressivo exponencial estável
        float targetFov = (playerProperties != null && playerProperties.is_reloading) ? minFov : targetCameraFov;
        
        player_camera.fieldOfView = Mathf.Lerp(
            player_camera.fieldOfView, 
            targetFov, 
            1f - Mathf.Exp(-fovLerpSpeed * Time.deltaTime))
        ;
    }

    public void EnableUpdate() => canUpdate = true;
    public void DisableUpdate() => canUpdate = false;
}