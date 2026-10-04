using UnityEngine;

public class Mag : Attatchment
{
    public Transform magHandPosition;
    [SerializeField] private Animator anim;
    protected Animator MagAnimator => anim;

    [Header("Changes")]
    public ProcessReload.Reload.ReloadValues reloadValues;
    public int bulletsPerShotChange;
    public float adsSpeedChange;

    public override void Initialize()
    {
        base.Initialize();
        GetWeaponHolder();
        weaponProperties.magAttatchment = this;
    }

    private void GetWeaponHolder()
    {
        EquippableItemHandTargets wh = GetComponentInParent<EquippableItemHandTargets>();
        if (wh != null) wh.SetWeaponMag(magHandPosition);
    }

    public virtual void PlayMagShootAnimation()
    {
        if (anim != null) anim.SetTrigger("Shoot_anim");
    }

    public virtual void ResetMagState(){}
}
