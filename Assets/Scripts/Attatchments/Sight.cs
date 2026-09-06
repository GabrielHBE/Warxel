using UnityEngine;
using UnityEngine.Serialization;

public class Sight : Attatchment
{
    public const float MAX_ZOOM_LEVEL = 10;
    public const float MIN_ZOOM_LEVEL = 1;
    private const float RETICLE_COLOR_INTENSITY = 5f;
    private static readonly float ReticleColorIntensityMultiplier = Mathf.Pow(2f, RETICLE_COLOR_INTENSITY);

    [Header("Settings")]
    public Transform adsPosition;
    public SightType sightType;
    [SerializeField] private Texture2D reticle;
    [SerializeField] private MeshRenderer glassRenderer;

    [Header("Aim Shake Settings")]
    [SerializeField] private bool canAimShake;
    [SerializeField] private float tension = 1f;

    [Header("Hold Breath Settings")]
    [SerializeField] private float maxBreathDuration = 5f; // Tempo máximo segurando a respiração
    [SerializeField] private float breathCooldown = 10f;   // Tempo de penalidade caso a respiração acabe
    [SerializeField] private float breathRecoveryRate = 1f;// Velocidade que recupera o fôlego quando não está apertando

    [Header("Changes")]
    [Range(MIN_ZOOM_LEVEL, MAX_ZOOM_LEVEL)] public float[] zoomChanges = new float[1];

    public float zoomChange => GetCurrentZoomChange();

    private Material sightMaterial;
    private PlayerProperties playerProperties;
    private CameraShake cameraShake;
    private AttatchmentManager attatchmentManager;
    private int currentZoomIndex = 0;
    private bool isHoldingBreath = false;
    private float currentBreath;
    private float currentCooldown;

    public enum SightType
    {
        CloseRange,
        MediumRange,
        LongRange,
        ExtremeLongRange
    }

    #region Unity Lifeclycle
    protected virtual void Update()
    {
        UpdateReticleColor();
        ZoomChangeHandler();
        if (canAimShake) AimShakeHandler();
    }

    protected virtual void Awake()
    {
        cameraShake = GetComponentInParent<CameraShake>();
        playerProperties = GetComponentInParent<PlayerProperties>();
        weaponProperties = GetComponentInParent<WeaponProperties>();
        attatchmentManager = GetComponentInParent<AttatchmentManager>();
        currentBreath = maxBreathDuration;

        if (glassRenderer != null) sightMaterial = glassRenderer.material;
    }

    protected virtual void OnEnable()
    {
        if (sightMaterial != null && reticle != null) sightMaterial.SetTexture("_Reticle_Layer_1", reticle);
    }
    #endregion

    #region Zoom
    private float GetCurrentZoomChange()
    {
        currentZoomIndex = Mathf.Clamp(currentZoomIndex, 0, zoomChanges.Length - 1);
        return Mathf.Clamp(zoomChanges[currentZoomIndex], MIN_ZOOM_LEVEL, MAX_ZOOM_LEVEL);
    }

    private void ZoomChangeHandler()
    {
        if (playerProperties == null || !playerProperties.aiming) return;
        if (zoomChanges == null || zoomChanges.Length < 2 || !InputManager.GetKeyDown(Settings.Instance._keybinds.WEAPON_zoomChangeKey)) return;
        if (AdsBehaviour.Instance != null && !AdsBehaviour.Instance.IsCurrentSight(this)) return;

        float previousZoomChange = zoomChange;
        currentZoomIndex = (currentZoomIndex + 1) % zoomChanges.Length;
        float newZoomChange = zoomChange;

        bool updatedByManager = attatchmentManager != null && attatchmentManager.TryUpdateCurrentSightZoom(this, newZoomChange);

        if (!updatedByManager && weaponProperties != null && !(this is CantedSight)) weaponProperties.zoom += newZoomChange - previousZoomChange;

        if (AdsBehaviour.Instance != null) AdsBehaviour.Instance.RefreshSightZoom(this);
    }
    #endregion

    #region Reticle Color

    private void UpdateReticleColor()
    {
        if (sightMaterial != null && reticle != null)
        {
            float calculatedScale = 2 - Settings.Instance._gameplay.sight_reticle_size;
            sightMaterial.SetFloat("_Reticle_Layer_1_Scale", calculatedScale);

            Color selectedColor = Settings.Instance._gameplay.sight_reticle_collor;
            Color intensifiedColor = new Color(
                selectedColor.r * ReticleColorIntensityMultiplier,
                selectedColor.g * ReticleColorIntensityMultiplier,
                selectedColor.b * ReticleColorIntensityMultiplier,
                selectedColor.a);

            Color currentColor = sightMaterial.GetColor("_Reticle_Layer_1_Color");
            if (currentColor != intensifiedColor) sightMaterial.SetColor("_Reticle_Layer_1_Color", intensifiedColor);
            
        }
    }
    #endregion

    #region Aim Shake
    private void AimShakeHandler()
    {
        if (playerProperties == null || cameraShake == null || !gameObject.activeSelf) return;
        if (AdsBehaviour.Instance != null && !AdsBehaviour.Instance.IsCurrentSight(this)) return;

        // O jogador quer segurar a respiração se estiver mirando E apertando o botão
        bool wantsToHoldBreath = playerProperties.aiming && InputManager.GetKey(Settings.Instance._keybinds.PLAYER_holdBreathKey);

        // Atualiza a matemática do fôlego e cooldown
        UpdateBreathMechanic(wantsToHoldBreath);

        // Aplica a tremedeira com base nos estados atuais
        if (playerProperties.aiming)
        {
            if (isHoldingBreath) cameraShake.ResetAimShake();
            else cameraShake.CalculateScopeAimShake(tension);
        }
        else cameraShake.ResetAimShake();
    }

    private void UpdateBreathMechanic(bool wantsToHoldBreath)
    {
        // 1. CHECAGEM DE COOLDOWN
        if (currentCooldown > 0)
        {
            isHoldingBreath = false;
            currentCooldown -= Time.deltaTime;

            RecoverBreath(); // Permite que o fôlego volte mesmo enquanto em cooldown
            return;
        }

        // 2. SEGURANDO A RESPIRAÇÃO
        if (wantsToHoldBreath && currentBreath > 0)
        {
            isHoldingBreath = true;
            currentBreath -= Time.deltaTime;

            // Se o fôlego chegar a zero, ativa a punição de cooldown
            if (currentBreath <= 0)
            {
                isHoldingBreath = false;
                currentCooldown = breathCooldown;
            }
        }
        // 3. NÃO ESTÁ APERTANDO O BOTÃO
        else
        {
            isHoldingBreath = false;
            RecoverBreath();
        }
    }

    private void RecoverBreath()
    {
        if (currentBreath < maxBreathDuration)
        {
            currentBreath += Time.deltaTime * breathRecoveryRate;
            if (currentBreath > maxBreathDuration) currentBreath = maxBreathDuration;
        }
    }
    #endregion
}
