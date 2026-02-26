using UnityEngine;

public class Attatchment : MonoBehaviour
{
    [Header("Attatchment Settings")]
    public float attatchment_points;
    public float weapon_level_to_unlock;
    public Sprite icon_hud;
    public string attachment_name;
    protected WeaponProperties weaponProperties;
    public bool is_attatchment_unlocked;

    public void Initialize(WeaponProperties weaponProperties)
    {
        this.weaponProperties = weaponProperties;
        if (weaponProperties != null) is_attatchment_unlocked = weapon_level_to_unlock >= weaponProperties.weapon_level ? true : false;
    }
}
