using System.Runtime.Remoting.Messaging;
using UnityEngine;

public abstract class Attatchment : MonoBehaviour
{   
    protected string attatchmentDescription;

    [Header("Attatchment Settings")]
    public string attachmentName;
    public float attatchment_points;
    public float weapon_level_to_unlock;
    public Sprite icon_hud;
    protected WeaponProperties weaponProperties;

    public bool IsAttatchmentUnlocked()
    {
        InitializeWeaponProperties();
        return weaponProperties != null && weaponProperties.weapon_kills >= weapon_level_to_unlock;
    }

    public virtual void Initialize()
    {
        InitializeWeaponProperties();
    }

    protected void InitializeWeaponProperties()
    {
        weaponProperties = GetComponentInParent<WeaponProperties>();
    }

    public abstract string GetAttatchmentDescription();

}
