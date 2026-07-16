using UnityEngine;

public class WeaponProperties : MonoBehaviour, UpgradeLevel
{
    #region Variables

    [Header("Progression & Economy")]
    public string weapon_name;
    public ClassManager.Class[] class_weapon;
    public FactionManager.Faction[] faction;
    public WeaponCategory category;
    public SwitchWeapon.WeaponSlot weaponSlot;
    public int battle_coins_to_unlock;
    public int weapon_kills;
    public float current_attachment_points;
 
    [Header("Core Settings")]
    [HideInInspector] public GameObject weapon;
    public GameObject third_person_prefab;
    public Sprite icon_hud;
    public float ads_speed;
    public float speed_change;
    public float zoom = 1;

    [Header("Handling")]
    public float pick_up_weapon_speed;
    public float store_weapon_speed;

    [Header("Shooting & Reloading")]
    public float delay_to_shoot_animation;
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
  
    [Header("Sway & Bobbing Exaggeration")]
    public float bob_walk_exageration;
    public float bob_sprint_exageration;
    public float bob_crouch_exageration;
    public float bob_aim_exageration;

    [Header("Sway Multipliers")]
    public Vector3 walk_multiplier;
    public Vector3 sprint_multiplier;
    public Vector3 aim_multiplier;
    public Vector3 crouch_multiplier;

    [Header("Sway Transforms")]
    public Vector3 initial_potiion; // Mantido o erro de digitação original para não quebrar referências externamente
    public Quaternion initial_rotation;
    public float[] vector3Values = new float[3];
    public float[] quaternionValues = new float[3];
    #endregion

    [Header("References & Effects")]
    public WeaponSounds weapon_sound;
    public GameObject barrel;
    private BulletExtractor bulletExtractor;

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

    #region Unity Callbacks
    void Awake()
    {
        weapon = gameObject;
    }
    #endregion

    #region Initialization & Setup
    public void Initialize()
    {
        weapon_kills = PlayerPrefs.GetInt($"WeaponProperties_weapon_kills_{weapon_name}");
        SetClassBenefits();
        bulletExtractor = GetComponentInChildren<BulletExtractor>();
        reloadValues.PopulateMags();
        Restart();
    }

    private void SetClassBenefits()
    {
        if (AccountManager.Instance.selected_class == ClassManager.Class.Assault)
        {
            reloadValues.reloadTime *= 1.2f;
            recoilValues.firstShootRecoilMultiplier *= 0.9f;

            for (int i = 0; i < recoilValues.recoilPattern.Length; i++)
            {
                recoilValues.recoilPattern[i].horizontalRecoil *= 0.9f;
                recoilValues.recoilPattern[i].verticalRecoil *= 0.9f;
            }

        }
    }

    public void Restart()
    {
        recoilValues.CalculateRecoilMedia();
        recoilValues.CalculateRecoilSpeed(firing.interval);
    }
    #endregion

    #region Logic & Calculations
    public void CreateBulletExtractor()
    {
        if (bulletExtractor != null)
        {
            bulletExtractor.CreateBullet();
        }
    }
    #endregion

    #region Progression Systems
    public void AddKill()
    {
        weapon_kills += 1;
    }

    public void ResetWeaponlevel()
    {
        PlayerPrefs.SetFloat($"WeaponProperties_weapon_level_progression_{weapon_name}", 0);
        PlayerPrefs.SetFloat($"WeaponProperties_weapon_level_{weapon_name}", 0);
        PlayerPrefs.Save();
    }
    #endregion
}