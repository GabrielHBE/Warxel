using UnityEngine;
using ProcessReload;

public class ReloadController : MonoBehaviour
{
    private IReloadContext context;

    private float reload_cooldown;
    private int reserve_ammo;

    public void Setup(IReloadContext context)
    {
        this.context = context;
        if (this.context != null) this.context.IsReloading = false;
    }

    void Update()
    {
        if (context == null || !context.IsActive) return;
        ProcessOngoingReload();
    }

    public void TryStartReload()
    {
        if (context == null || !context.IsActive) return;

        int reserveAmmo = context.ReloadValues.GetTotalReserveAmmo();

        if (!Reload.ReloadLogic.CanStartReload(
            context.ReloadValues,
            context.IsFiring,
            context.IsReloading,
            context.IsRolling,
            reserveAmmo))
        {
            if (reserveAmmo == 0) context.OnReloadFailedNoAmmo();
            return;
        }

        context.StartReloadAnimation();

        bool isEmpty = context.ReloadValues.IsMagazineEmpty();
        float totalReloadTime = Reload.ReloadLogic.CalculateReloadTime(context.ReloadValues, isEmpty);

        if (context.HasFireClip && !context.IsInFireAnimation) StartCooldown(totalReloadTime);
        else if (!context.HasFireClip) StartCooldown(totalReloadTime);
    }

    private void StartCooldown(float time)
    {
        reload_cooldown = time;
        context.IsReloading = true;
    }

    private void ProcessOngoingReload()
    {
        reserve_ammo = context.ReloadValues.GetTotalReserveAmmo();

        if (!context.IsReloading) return;

        if (!context.ReloadValues.isSingleReload)
        {
            if (reserve_ammo > 0) HandleStandardReload();
        }
        else HandleSingleReload();

    }

    private void HandleStandardReload()
    {
        var result = Reload.ReloadLogic.ProcessStandardReload(
            context.ReloadValues, reload_cooldown, Time.deltaTime, context.ReloadValues.IsMagazineEmpty()
        );

        reload_cooldown = result.remainingCooldown;
        context.SetCanShoot(result.canShoot);
        context.IsReloading = result.isReloading;

        if (result.shouldFinishReload)
        {
            context.FinishReloadAnimation();
            context.ResetFiringState();
        }
    }

    private void HandleSingleReload()
    {
        if (context.ReloadValues.IsMagazineFull() || reserve_ammo <= 0) StopSingleReload();
    }

    public void StopSingleReload()
    {
        context.FinishReloadAnimation(); 
        context.IsReloading = false;
    }

    // Este método continuará sendo chamado pelo seu Animation Event durante o estágio de "Reloading"
    public void SigleReloadAnimationEvent() => Reload.ReloadLogic.TransferMagazineAmmoSingleReload(context.ReloadValues);
}