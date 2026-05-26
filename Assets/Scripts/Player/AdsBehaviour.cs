using System.Collections;
using UnityEngine;

public class AdsBehaviour : MonoBehaviour
{
    public static AdsBehaviour Instance { get; private set; }
    
    [Header("Instances")]
    [SerializeField] private Camera player_camera;
    [SerializeField] private PlayerProperties playerProperties;
    [SerializeField] private SwitchWeapon switchWeapon;

    public bool dot_position { get; private set; }

    private float adsTimer;
    private Transform adsReference;

    private float minFov;
    private bool is_aiming;
    private Vector3 original_ads_position;
    private float zoom;
    
    // Nova variável para guardar o offset do eixo Z desejado
    [SerializeField] private float zOffset;

    private Coroutine aimCoroutine;
    private bool isAimTransitionActive;

    // Adicionado o parâmetro zOffset no método Setup
    public void Setup(Transform adsReference, float adsTimer, float zoom)
    {
        this.adsReference = adsReference;
        this.adsTimer = adsTimer;
        this.zoom = zoom;

    }

    void Awake()
    {
        Instance = this;
        minFov = Settings.Instance._video.infantary_fov;
        original_ads_position = transform.localPosition;
    }

    void Update()
    {
        if(adsReference==null) return;
        
        if (SettingsHUD.Instance.is_menu_settings_active)
        {
            StopAiming();
            return;
        }

        HandleAimInput();
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

        if (canAim && Input.GetKey(Settings.Instance._keybinds.WEAPON_aimKey))
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

        if (Input.GetKeyDown(Settings.Instance._keybinds.WEAPON_aimKey))
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
               !playerProperties.is_dead.Value;
    }

    private void StartAiming()
    {
        if (adsReference == null) return;

        playerProperties.sprinting = false;
        playerProperties.is_aiming = true;

        float targetFov = minFov / zoom;
        UpdateCameraFov(targetFov);

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

        UpdateCameraFov(minFov);

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
            float t = elapsed / adsTimer;

            if (aiming)
            {
                // 1. Obtém o centro global alvo original (posição de repouso)
                Vector3 centerGlobalTarget = transform.parent != null 
                    ? transform.parent.TransformPoint(original_ads_position) 
                    : original_ads_position;

                // 2. Aplica o offset no eixo Z (forward) baseado na orientação do pai da arma (ou da câmera)
                // Se você quiser aproximar ou afastar a arma do rosto ao mirar, adicionamos essa direção:
                Vector3 forwardDirection = transform.parent != null ? transform.parent.forward : transform.forward;
                centerGlobalTarget += forwardDirection * zOffset;

                // 3. Descobre o vetor de offset do ponto de referência para o transform atual
                Vector3 offsetGlobal = adsReference.position - transform.position;

                // 4. Subtrai o offset para alinhar o adsReference com o alvo centralizado + offset Z
                Vector3 targetGlobalPosition = centerGlobalTarget - offsetGlobal;

                // 5. Converte para o espaço local do pai para realizar o Lerp suavemente
                Vector3 targetLocalPosition = transform.parent != null 
                    ? transform.parent.InverseTransformPoint(targetGlobalPosition) 
                    : targetGlobalPosition;

                transform.localPosition = Vector3.Lerp(startLocalPosition, targetLocalPosition, t);
            }
            else
            {
                // Retorna suavemente para a posição original de Hipfire
                transform.localPosition = Vector3.Lerp(startLocalPosition, original_ads_position, t);
            }

            yield return null;
        }

        // Ajuste milimétrico final após o término do loop
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
        }
        else if (!aiming)
        {
            transform.localPosition = original_ads_position;
        }

        aimCoroutine = null;
    }

    private void UpdateCameraFov(float targetFov)
    {
        if (player_camera == null) return;

        float lerpSpeed = 10f * Time.deltaTime;
        if (playerProperties != null && !playerProperties.is_reloading)
        {
            player_camera.fieldOfView = Mathf.Lerp(player_camera.fieldOfView, targetFov, lerpSpeed);
        }
        else
        {
            player_camera.fieldOfView = Mathf.Lerp(player_camera.fieldOfView, minFov, lerpSpeed);
        }
    }
}