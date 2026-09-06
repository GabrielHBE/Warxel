using UnityEngine;
using UnityEngine.Serialization;

public abstract class Attatchment : MonoBehaviour
{   
    [SerializeField] protected string attatchmentDescription;

    [Header("Attatchment Settings")]
    public string attachmentName;
    [FormerlySerializedAs("attatchment_points")]
    [Tooltip("Cost of this attachment. The total equipped on the weapon cannot exceed 100 points.")]
    [Min(0f)]
    public float attatchmentPoints;
    public float weaponLevelToUnlock;
    public Sprite iconHud;
    [SerializeField] protected WeaponProperties weaponProperties;
    public bool isStandardAttatchment;

    public bool IsAttatchmentUnlocked()
    {
        InitializeWeaponProperties();
        return weaponProperties != null && weaponProperties.weaponKills >= weaponLevelToUnlock;
    }

    public virtual void Initialize() => InitializeWeaponProperties();
    protected void InitializeWeaponProperties() =>  weaponProperties = GetComponentInParent<WeaponProperties>();
    public string GetAttatchmentDescription() => attatchmentDescription;

}
