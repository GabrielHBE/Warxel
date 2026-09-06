using UnityEngine;
using UnityEngine.Rendering;

public class WeaponProperties : MonoBehaviour, UpgradeLevel
{
    #region Variables
    [Header("Progression & Economy")]
    public string weaponName;
    public ClassManager.Class[] classWeapon;
    public FactionManager.Faction[] faction;
    public WeaponCategory category;
    public int battleCoinsToUnlock;
    public int weaponKills;
    public float currentAttachmentPoints;

    [Header("Core Settings")]
    public GameObject thirdPersonPrefab;
    public Sprite iconHud;
    public float adsSpeed;
    public float speedChange;
    [HideInInspector] public float zoom = 0;

    [Header("Handling")]
    public float drawWeaponSpeed = 0.5f;
    public float storeWeaponSpeed = 0.5f;
    public bool canReloadAiming;

    [Header("Shooting & Reloading")]
    public float delayToShootAnimation;
    public bool changeShootAnimationSpeed;
    public ProcessReload.Reload.ReloadValues reloadValues;

    [Header("Fring Settings")]
    public Firing.FiringValues firing;

    [Header("Damage & Ballistics")]
    public Projectile.ProjectileValues projectileValues;

    [Header("Spread Settings")]
    public Spread.SpreadValues spreadValues;

    [Header("Recoil Settings")]
    public Recoil.RecoilValues recoilValues;

    [Header("Sway and Bob")]
    public SwayNBobScript.SwayAndBobValues swayAndBobValues;

    [Header("References & Effects")]
    public Transform shootPos;
    public EquippableItemAudio weaponSound;
    private BulletExtractor bulletExtractor;
    [HideInInspector] public EquippableItemAnimator weaponAnimation;
    #endregion

    #region Enums
    public enum WeaponCategory
    {
        AssaultRifle,
        Dmr,
        SniperRifle,
        SubmachineGun,
        LightMachineGun,
        Shotgun,
        Pistol,
        Launcher
    }
    #endregion

    #region Initialization & Setup
    private void Awake() => DisableRendererShadows();
    private void OnTransformChildrenChanged() => DisableRendererShadows();

#if UNITY_EDITOR
    private void OnValidate() => DisableRendererShadows();
#endif

    public void Initialize()
    {
        DisableRendererShadows();
        recoilValues.CalculateRecoilSpeed(firing.interval);
        GetComponents();
        SetClassBenefits();
        weaponKills = PlayerPrefs.GetInt($"WeaponProperties_weapon_kills_{weaponName}");
        reloadValues.PopulateMags();
        weaponAnimation.Setup(delayToShootAnimation, reloadValues, changeShootAnimationSpeed, firing);
    }

    public void DisableRendererShadows()
    {
        foreach (Renderer childRenderer in GetComponentsInChildren<Renderer>())
        {
            childRenderer.shadowCastingMode = ShadowCastingMode.Off;
            childRenderer.receiveShadows = false;
        }
    }

    private void GetComponents()
    {
        bulletExtractor = GetComponentInChildren<BulletExtractor>();
        weaponAnimation = GetComponent<EquippableItemAnimator>();
    }

    private void SetClassBenefits()
    {
        if (AccountManager.Instance.selectedClass == ClassManager.Class.Assault)
        {
            reloadValues.magCount += 2;

            reloadValues.reloadTime *= 1.2f;
            recoilValues.firstShootRecoilMultiplier *= 0.9f;

            for (int i = 0; i < recoilValues.recoilPattern.Length; i++)
            {
                recoilValues.recoilPattern[i].horizontalRecoil.value *= 0.9f;
                recoilValues.recoilPattern[i].verticalRecoil.value *= 0.9f;
            }

        }
    }
    #endregion

    #region Logic & Calculations
    public void CreateBulletExtractor()
    {
        if (bulletExtractor != null) bulletExtractor.CreateBullet();
    }
    #endregion

    #region Progression Systems
    public void AddKill() => weaponKills += 1;

    public void ResetWeaponlevel()
    {
        PlayerPrefs.SetFloat($"WeaponProperties_weapon_level_progression_{weaponName}", 0);
        PlayerPrefs.SetFloat($"WeaponProperties_weapon_level_{weaponName}", 0);
        PlayerPrefs.Save();
    }
    #endregion
}
