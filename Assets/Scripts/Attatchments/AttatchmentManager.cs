using UnityEngine;
using System;
using System.Collections.Generic;

public class AttatchmentManager : MonoBehaviour
{
    public const float MaxAttachmentPoints = 100f;
    private const float AttachmentPointsTolerance = 0.001f;

    [Serializable]
    public class AttachmentData
    {
        public string attachmentName;
        public float points;

        // Grip
        public float verticalRecoilChange;
        public float horizontalRecoilChange;
        public float firstShootChange;
        public float reloadSpeedChange;
        public float adsSpeedChange;
        public float drawWeaponSpeed;
        public float storeWeaponSpeedChange;

        // Nozzle
        public float muzzleLightningChange;
        public int muzzleVelocityChange;
        public float shootPithChange;
        public float shootVolumeChange;
        public float spreadChange;

        // Sight
        public float zoomChange;

        // Mag
        public int bulletPerMagChange;
        public int magCountChange;
        public float reloadSpeedChanger;
        public float timeToTransferAmmoChange;
        public int bulletsPerShotChange;

        // Ergonomics
        public Vector3 visualRecoilPositionChange;
        public Vector3 visualRecoilRotationChange;
        public List<Firing.FireMode> fireModesChange = new List<Firing.FireMode>();
        public int rateOfFireChange;
        public int burstBulletsPerTapChange;
        public bool canReloadAiming;
    }

    private AttachmentData currentGrip;
    private AttachmentData currentNozzle;
    private AttachmentData currentBarrel;
    private AttachmentData currentSight;
    private AttachmentData currentCantedSight;
    private AttachmentData currentMag;
    private AttachmentData currentSideGrip;
    private AttachmentData currentErgonomics;
    private WeaponProperties weaponProperties;

    private string weaponName;

    public bool CanEquipAttachment(Attatchment attachment)
    {
        if (attachment == null) return false;

        if (weaponProperties == null) weaponProperties = GetComponent<WeaponProperties>();
        if (weaponProperties == null) return false;

        SynchronizeAttachmentPoints(weaponProperties);
        return GetProjectedAttachmentPoints(attachment) <= MaxAttachmentPoints + AttachmentPointsTolerance;
    }

    public float GetProjectedAttachmentPoints(Attatchment attachment)
    {
        if (attachment == null) return CurrentAttachmentPoints;

        AttachmentData currentSlotAttachment = GetCurrentAttachmentForSlot(attachment);
        float currentSlotPoints = currentSlotAttachment != null ? currentSlotAttachment.points : 0f;
        float newAttachmentPoints = Mathf.Max(0f, attachment.attatchmentPoints);

        return Mathf.Max(0f, CurrentAttachmentPoints - currentSlotPoints + newAttachmentPoints);
    }

    public float CurrentAttachmentPoints
    {
        get
        {
            if (weaponProperties == null) weaponProperties = GetComponent<WeaponProperties>();
            return SynchronizeAttachmentPoints(weaponProperties);
        }
    }

    private AttachmentData GetCurrentAttachmentForSlot(Attatchment attachment)
    {
        if (attachment is CantedSight) return currentCantedSight;
        if (attachment is Sight) return currentSight;
        if (attachment is Nozzle) return currentNozzle;
        if (attachment is Barrel) return currentBarrel;
        if (attachment is Mag) return currentMag;
        if (attachment is Grip) return currentGrip;
        if (attachment is SideGrip) return currentSideGrip;
        if (attachment is Ergonomics) return currentErgonomics;
        return null;
    }

    private float SynchronizeAttachmentPoints(WeaponProperties targetWeaponProperties)
    {
        float total = GetAttachmentPoints(currentGrip) +
                      GetAttachmentPoints(currentNozzle) +
                      GetAttachmentPoints(currentBarrel) +
                      GetAttachmentPoints(currentSight) +
                      GetAttachmentPoints(currentCantedSight) +
                      GetAttachmentPoints(currentMag) +
                      GetAttachmentPoints(currentSideGrip) +
                      GetAttachmentPoints(currentErgonomics);

        if (targetWeaponProperties != null) targetWeaponProperties.currentAttachmentPoints = total;
        return total;
    }

    private static float GetAttachmentPoints(AttachmentData attachment)
    {
        return attachment != null ? Mathf.Max(0f, attachment.points) : 0f;
    }

    public void InitializeAttachments()
    {
        weaponProperties = GetComponent<WeaponProperties>();
        if (weaponProperties != null)
        {
            weaponName = weaponProperties.weaponName;
            RemoveAllAttachmentsWithoutSaving();
            SynchronizeAttachmentPoints(weaponProperties);
        }

        Grip[] grips = GetComponentsInChildren<Grip>(true);
        foreach (Grip grip in grips)
        {
            grip.gameObject.SetActive(false);
        }

        Nozzle[] nozzles = GetComponentsInChildren<Nozzle>(true);
        foreach (Nozzle nozzle in nozzles)
        {
            nozzle.gameObject.SetActive(false);
        }

        Barrel[] barrels = GetComponentsInChildren<Barrel>(true);
        foreach (Barrel barrel in barrels)
        {
            barrel.gameObject.SetActive(false);
        }

        Sight[] sights = GetComponentsInChildren<Sight>(true);
        foreach (Sight sight in sights)
        {
            sight.gameObject.SetActive(false);
        }

        Mag[] mags = GetComponentsInChildren<Mag>(true);
        foreach (Mag mag in mags)
        {
            mag.gameObject.SetActive(false);
        }

        SideGrip[] sideGrips = GetComponentsInChildren<SideGrip>(true);
        foreach (SideGrip sideGrip in sideGrips)
        {
            sideGrip.gameObject.SetActive(false);
        }

        Ergonomics[] ergonomics = GetComponentsInChildren<Ergonomics>(true);
        foreach (Ergonomics ergo in ergonomics)
        {
            ergo.gameObject.SetActive(false);
        }

        LoadAttachmentsFromPlayerPrefs();
    }

    #region Data Creation Methods
    private AttachmentData CreateGripData(Grip g)
    {
        return new AttachmentData
        {
            attachmentName = g.gameObject.name,
            points = g.attatchmentPoints,
            verticalRecoilChange = g.verticalRecoilChange,
            horizontalRecoilChange = g.horizontalRecoilChange,
            firstShootChange = g.firstShootChange,
            reloadSpeedChange = g.reloadSpeedChange,
            adsSpeedChange = g.adsSpeedChange,
            drawWeaponSpeed = g.drawWeaponSpeedChange,
            storeWeaponSpeedChange = g.storeWeaponSpeedChange
        };
    }

    private AttachmentData CreateNozzleData(Nozzle nozzle)
    {
        return new AttachmentData
        {
            attachmentName = nozzle.gameObject.name,
            points = nozzle.attatchmentPoints,
            horizontalRecoilChange = nozzle.horizontalRecoilChange,
            verticalRecoilChange = nozzle.verticalRecoilChange,
            firstShootChange = nozzle.firstShootRecoilChange,
            muzzleLightningChange = nozzle.muzzleLightningChange,
            muzzleVelocityChange = nozzle.muzzleVelocityChange,
            shootPithChange = nozzle.shootPithChange,
            shootVolumeChange = nozzle.shootVolumeChange,
            spreadChange = nozzle.spreadChange
        };
    }

    private AttachmentData CreateBarrelData(Barrel barrel)
    {
        return new AttachmentData
        {
            attachmentName = barrel.gameObject.name,
            points = barrel.attatchmentPoints,
            horizontalRecoilChange = barrel.horizontalRecoilChange,
            verticalRecoilChange = barrel.verticalRecoilChange,
            firstShootChange = barrel.firstShootRecoilChange,
            muzzleVelocityChange = barrel.muzzleVelocityChange,
            adsSpeedChange = barrel.adsSpeedChange
        };
    }

    private AttachmentData CreateSightData(Sight s)
    {
        return new AttachmentData
        {
            attachmentName = s.gameObject.name,
            points = s.attatchmentPoints,
            zoomChange = s.zoomChange,
        };
    }

    private AttachmentData CreateMagData(Mag m)
    {
        return new AttachmentData
        {
            bulletsPerShotChange = m.bulletsPerShotChange,
            attachmentName = m.gameObject.name,
            points = m.attatchmentPoints,
            bulletPerMagChange = m.reloadValues.bulletsPerMag,
            adsSpeedChange = m.adsSpeedChange,
            reloadSpeedChanger = m.reloadValues.reloadTime,
            magCountChange = m.reloadValues.magCount,
            timeToTransferAmmoChange = m.reloadValues.timeToTransferAmmo
        };
    }

    private AttachmentData CreateSideGripData(SideGrip sg)
    {
        return new AttachmentData
        {
            attachmentName = sg.gameObject.name,
            points = sg.attatchmentPoints
        };
    }

    private AttachmentData CreateErgonomicsData(Ergonomics e)
    {
        return new AttachmentData
        {
            attachmentName = e.gameObject.name,
            points = e.attatchmentPoints,
            visualRecoilPositionChange = e.visualRecoilPositionChange,
            visualRecoilRotationChange = e.visualRecoilRotationChange,
            reloadSpeedChange = e.reloadSpeedChange,
            adsSpeedChange = e.adsSpeedChange,
            drawWeaponSpeed = e.pickupWeaponSpeedChange,
            storeWeaponSpeedChange = e.storeWeaponSpeedChange,
            fireModesChange = new List<Firing.FireMode>(e.fireModesChange),
            rateOfFireChange = e.rafeOfFireChange,
            burstBulletsPerTapChange = e.burstBulletsPerTapChange,
            canReloadAiming = e.canReloadAiming
        };
    }
    #endregion

    #region Stats Addition Methods
    private void AddGripStats(WeaponProperties wp, AttachmentData grip)
    {
        for (int i = 0; i < wp.recoilValues.recoilPattern.Length; i++)
        {
            //Vertical Recoil

            wp.recoilValues.recoilPattern[i].verticalRecoil.value += grip.verticalRecoilChange;
            wp.recoilValues.recoilPattern[i].verticalRecoil.value = Math.Clamp(wp.recoilValues.recoilPattern[i].verticalRecoil.value, Recoil.MIN_RECOIL_VALUE, Recoil.MAX_RECOIL_VALUE);

            //Horizontal Recoil
            wp.recoilValues.recoilPattern[i].horizontalRecoil.value += grip.horizontalRecoilChange;
            wp.recoilValues.recoilPattern[i].horizontalRecoil.value = Math.Clamp(wp.recoilValues.recoilPattern[i].horizontalRecoil.value, Recoil.MIN_RECOIL_VALUE, Recoil.MAX_RECOIL_VALUE);
        }

        wp.currentAttachmentPoints += grip.points;
        wp.recoilValues.firstShootRecoilMultiplier += grip.firstShootChange;
        wp.drawWeaponSpeed += grip.drawWeaponSpeed;
        wp.storeWeaponSpeed += grip.storeWeaponSpeedChange;
        wp.reloadValues.reloadTime += grip.reloadSpeedChange;
        wp.adsSpeed += grip.adsSpeedChange;
    }

    private void AddRecoilStats(WeaponProperties wp, AttachmentData attachment)
    {
        if (wp == null || attachment == null) return;

        for (int i = 0; i < wp.recoilValues.recoilPattern.Length; i++)
        {
            wp.recoilValues.recoilPattern[i].verticalRecoil.value += attachment.verticalRecoilChange;
            wp.recoilValues.recoilPattern[i].verticalRecoil.value = Math.Clamp(wp.recoilValues.recoilPattern[i].verticalRecoil.value, Recoil.MIN_RECOIL_VALUE, Recoil.MAX_RECOIL_VALUE);
            wp.recoilValues.recoilPattern[i].horizontalRecoil.value += attachment.horizontalRecoilChange;
            wp.recoilValues.recoilPattern[i].horizontalRecoil.value = Math.Clamp(wp.recoilValues.recoilPattern[i].horizontalRecoil.value, Recoil.MIN_RECOIL_VALUE, Recoil.MAX_RECOIL_VALUE);
        }
    }

    private void AddNozzleStats(WeaponProperties wp, AttachmentData nozzle)
    {
        if (wp == null || nozzle == null) return;

        AddRecoilStats(wp, nozzle);
        wp.currentAttachmentPoints += nozzle.points;
        wp.recoilValues.firstShootRecoilMultiplier += nozzle.firstShootChange;
        wp.projectileValues.muzzleVelocity += nozzle.muzzleVelocityChange;
        wp.weaponSound.shootSoundProperties.pitch += nozzle.shootPithChange;
        wp.weaponSound.shootSoundProperties.volume += nozzle.shootVolumeChange;
        wp.spreadValues.spreadIncreaser += nozzle.spreadChange;
    }

    private void AddBarrelStats(WeaponProperties wp, AttachmentData barrel)
    {
        if (wp == null || barrel == null) return;

        AddRecoilStats(wp, barrel);
        wp.currentAttachmentPoints += barrel.points;
        wp.recoilValues.firstShootRecoilMultiplier += barrel.firstShootChange;
        wp.projectileValues.muzzleVelocity += barrel.muzzleVelocityChange;
        wp.adsSpeed += barrel.adsSpeedChange;
    }

    private void AddSightStats(WeaponProperties wp, AttachmentData sight)
    {
        wp.currentAttachmentPoints += sight.points;
        wp.zoom += sight.zoomChange;
        wp.adsSpeed += sight.adsSpeedChange;
    }

    private void AddCantedSightStats(WeaponProperties wp, AttachmentData sight)
    {
        wp.currentAttachmentPoints += sight.points;
        wp.adsSpeed += sight.adsSpeedChange;
    }

    private void AddMagStats(WeaponProperties wp, AttachmentData mag)
    {
        wp.currentAttachmentPoints += mag.points;
        wp.firing.bulletsPerShot += mag.bulletsPerShotChange;
        wp.reloadValues.bulletsPerMag = mag.bulletPerMagChange;
        wp.reloadValues.magCount += mag.magCountChange;
        wp.reloadValues.timeToTransferAmmo += mag.timeToTransferAmmoChange;
        wp.adsSpeed += mag.adsSpeedChange;
        wp.reloadValues.reloadTime += mag.reloadSpeedChanger;
    }

    private void AddSideGripStats(WeaponProperties wp, AttachmentData sideGrip)
    {
        wp.currentAttachmentPoints += sideGrip.points;
    }

    private void AddErgonomicsStats(WeaponProperties wp, AttachmentData ergo)
    {
        if (wp == null || ergo == null) return;

        wp.currentAttachmentPoints += ergo.points;
        wp.recoilValues.firstShootRecoilMultiplier += ergo.firstShootChange;
        wp.reloadValues.reloadTime += ergo.reloadSpeedChange;
        wp.adsSpeed += ergo.adsSpeedChange;
        wp.drawWeaponSpeed += ergo.drawWeaponSpeed;
        wp.storeWeaponSpeed += ergo.storeWeaponSpeedChange;

        wp.recoilValues.visualPositionRecoil += ergo.visualRecoilPositionChange;
        wp.recoilValues.maxRotationRecoil += ergo.visualRecoilRotationChange;
        wp.firing.rateOfFire += ergo.rateOfFireChange;
        wp.firing.BurstModeBulletsPerTap += ergo.burstBulletsPerTapChange;
        wp.canReloadAiming = ergo.canReloadAiming;

        if (ergo.fireModesChange != null && ergo.fireModesChange.Count > 0)
        {
            foreach (var fm in ergo.fireModesChange)
            {
                if (!wp.firing.fireModes.Contains(fm)) wp.firing.fireModes.Add(fm);
            }
        }
    }
    #endregion

    #region Stats Removal Methods
    private void ResetAttachmentDataToZero(AttachmentData attachment)
    {
        if (attachment == null) return;

        attachment.attachmentName = "";
        attachment.points = 0;
        attachment.verticalRecoilChange = 0;
        attachment.horizontalRecoilChange = 0;
        attachment.firstShootChange = 0;
        attachment.reloadSpeedChange = 0;
        attachment.adsSpeedChange = 0;
        attachment.drawWeaponSpeed = 0;
        attachment.storeWeaponSpeedChange = 0;
        attachment.muzzleLightningChange = 0;
        attachment.muzzleVelocityChange = 0;
        attachment.shootPithChange = 0;
        attachment.shootVolumeChange = 0;
        attachment.spreadChange = 0;
        attachment.zoomChange = 0;
        attachment.bulletPerMagChange = 0;
        attachment.reloadSpeedChanger = 0;
        attachment.visualRecoilPositionChange = Vector3.zero;
        attachment.visualRecoilRotationChange = Vector3.zero;
        if (attachment.fireModesChange != null) attachment.fireModesChange.Clear();
        attachment.rateOfFireChange = 0;
        attachment.burstBulletsPerTapChange = 0;
        attachment.canReloadAiming = false;
    }

    private void RemoveGripStats(WeaponProperties wp, AttachmentData grip)
    {
        if (wp == null || grip == null) return;

        for (int i = 0; i < wp.recoilValues.recoilPattern.Length; i++)
        {
            //Vertical Recoil
            wp.recoilValues.recoilPattern[i].verticalRecoil.value -= grip.verticalRecoilChange;
            wp.recoilValues.recoilPattern[i].verticalRecoil.value = Math.Clamp(wp.recoilValues.recoilPattern[i].verticalRecoil.value, Recoil.MIN_RECOIL_VALUE, Recoil.MAX_RECOIL_VALUE);

            //Horizontal Recoil
            wp.recoilValues.recoilPattern[i].horizontalRecoil.value -= grip.horizontalRecoilChange;
            wp.recoilValues.recoilPattern[i].horizontalRecoil.value = Math.Clamp(wp.recoilValues.recoilPattern[i].horizontalRecoil.value, Recoil.MIN_RECOIL_VALUE, Recoil.MAX_RECOIL_VALUE);
        }

        wp.currentAttachmentPoints -= grip.points;
        wp.recoilValues.firstShootRecoilMultiplier -= grip.firstShootChange;
        wp.drawWeaponSpeed -= grip.drawWeaponSpeed;
        wp.storeWeaponSpeed -= grip.storeWeaponSpeedChange;
        wp.reloadValues.reloadTime -= grip.reloadSpeedChange;
        wp.adsSpeed -= grip.adsSpeedChange;

        ResetAttachmentDataToZero(grip);
    }

    private void RemoveRecoilStats(WeaponProperties wp, AttachmentData attachment)
    {
        if (wp == null || attachment == null) return;

        for (int i = 0; i < wp.recoilValues.recoilPattern.Length; i++)
        {
            wp.recoilValues.recoilPattern[i].verticalRecoil.value -= attachment.verticalRecoilChange;
            wp.recoilValues.recoilPattern[i].verticalRecoil.value = Math.Clamp(wp.recoilValues.recoilPattern[i].verticalRecoil.value, Recoil.MIN_RECOIL_VALUE, Recoil.MAX_RECOIL_VALUE);
            wp.recoilValues.recoilPattern[i].horizontalRecoil.value -= attachment.horizontalRecoilChange;
            wp.recoilValues.recoilPattern[i].horizontalRecoil.value = Math.Clamp(wp.recoilValues.recoilPattern[i].horizontalRecoil.value, Recoil.MIN_RECOIL_VALUE, Recoil.MAX_RECOIL_VALUE);
        }
    }

    private void RemoveNozzleStats(WeaponProperties wp, AttachmentData nozzle)
    {
        if (wp == null || nozzle == null) return;

        RemoveRecoilStats(wp, nozzle);
        wp.currentAttachmentPoints -= nozzle.points;
        wp.recoilValues.firstShootRecoilMultiplier -= nozzle.firstShootChange;
        wp.projectileValues.muzzleVelocity -= nozzle.muzzleVelocityChange;
        wp.weaponSound.shootSoundProperties.pitch -= nozzle.shootPithChange;
        wp.weaponSound.shootSoundProperties.volume -= nozzle.shootVolumeChange;
        wp.spreadValues.spreadIncreaser -= nozzle.spreadChange;

        ResetAttachmentDataToZero(nozzle);
    }

    private void RemoveBarrelStats(WeaponProperties wp, AttachmentData barrel)
    {
        if (wp == null || barrel == null) return;

        RemoveRecoilStats(wp, barrel);
        wp.currentAttachmentPoints -= barrel.points;
        wp.recoilValues.firstShootRecoilMultiplier -= barrel.firstShootChange;
        wp.projectileValues.muzzleVelocity -= barrel.muzzleVelocityChange;
        wp.adsSpeed -= barrel.adsSpeedChange;

        ResetAttachmentDataToZero(barrel);
    }

    private void RemoveSightStats(WeaponProperties wp, AttachmentData sight)
    {
        if (wp == null || sight == null) return;

        wp.currentAttachmentPoints -= sight.points;
        wp.zoom -= sight.zoomChange;
        wp.adsSpeed -= sight.adsSpeedChange;

        ResetAttachmentDataToZero(sight);
    }

    private void RemoveCantedSightStats(WeaponProperties wp, AttachmentData sight)
    {
        if (wp == null || sight == null) return;

        wp.currentAttachmentPoints -= sight.points;
        wp.adsSpeed -= sight.adsSpeedChange;

        ResetAttachmentDataToZero(sight);
    }

    private void RemoveMagStats(WeaponProperties wp, AttachmentData mag)
    {
        if (wp == null || mag == null) return;

        wp.currentAttachmentPoints -= mag.points;
        wp.firing.bulletsPerShot -= mag.bulletsPerShotChange;
        wp.reloadValues.bulletsPerMag -= mag.bulletPerMagChange;
        wp.adsSpeed -= mag.adsSpeedChange;
        wp.reloadValues.magCount -= mag.magCountChange;
        wp.reloadValues.timeToTransferAmmo -= mag.timeToTransferAmmoChange;
        wp.reloadValues.reloadTime -= mag.reloadSpeedChanger;

        ResetAttachmentDataToZero(mag);
    }

    private void RemoveSideGripStats(WeaponProperties wp, AttachmentData sideGrip)
    {
        if (wp == null || sideGrip == null) return;

        wp.currentAttachmentPoints -= sideGrip.points;
        ResetAttachmentDataToZero(sideGrip);
    }

    private void RemoveErgonomicsStats(WeaponProperties wp, AttachmentData ergo)
    {
        if (wp == null || ergo == null) return;

        wp.currentAttachmentPoints -= ergo.points;
        wp.reloadValues.reloadTime -= ergo.reloadSpeedChange;
        wp.adsSpeed -= ergo.adsSpeedChange;
        wp.drawWeaponSpeed -= ergo.drawWeaponSpeed;
        wp.storeWeaponSpeed -= ergo.storeWeaponSpeedChange;

        wp.recoilValues.visualPositionRecoil -= ergo.visualRecoilPositionChange;
        wp.recoilValues.maxRotationRecoil -= ergo.visualRecoilRotationChange;
        wp.firing.rateOfFire -= ergo.rateOfFireChange;
        wp.firing.BurstModeBulletsPerTap -= ergo.burstBulletsPerTapChange;
        wp.canReloadAiming = ergo.canReloadAiming;

        if (ergo.fireModesChange != null && ergo.fireModesChange.Count > 0)
        {
            foreach (var fm in ergo.fireModesChange)
            {
                wp.firing.fireModes.Remove(fm);
            }
        }

        ResetAttachmentDataToZero(ergo);
    }
    #endregion

    #region Update Methods - APENAS SETACTIVE
    public void UpdateGrip(Grip g, WeaponProperties weaponProperties, bool shouldSave = true)
    {
        if (g == null || weaponProperties == null || !CanEquipAttachment(g)) return;

        DisableAllOfType<Grip>();

        if (currentGrip != null)
        {
            RemoveGripStats(weaponProperties, currentGrip);
        }

        currentGrip = CreateGripData(g);
        AddGripStats(weaponProperties, currentGrip);
        g.gameObject.SetActive(true);
        SynchronizeAttachmentPoints(weaponProperties);

        if (shouldSave) SaveAttachmentsToPlayerPrefs();
    }

    public void UpdateNozzle(Nozzle nozzle, WeaponProperties weaponProperties, bool shouldSave = true)
    {
        if (nozzle == null || weaponProperties == null || !CanEquipAttachment(nozzle)) return;

        DisableAllOfType<Nozzle>();

        if (currentNozzle != null)
        {
            RemoveNozzleStats(weaponProperties, currentNozzle);
        }

        currentNozzle = CreateNozzleData(nozzle);
        AddNozzleStats(weaponProperties, currentNozzle);
        nozzle.gameObject.SetActive(true);
        SynchronizeAttachmentPoints(weaponProperties);

        if (shouldSave) SaveAttachmentsToPlayerPrefs();
    }

    public void UpdateBarrel(Barrel barrel, WeaponProperties weaponProperties, bool shouldSave = true)
    {
        if (barrel == null || weaponProperties == null || !CanEquipAttachment(barrel)) return;

        DisableAllOfType<Barrel>();

        if (currentBarrel != null)
        {
            RemoveBarrelStats(weaponProperties, currentBarrel);
        }

        currentBarrel = CreateBarrelData(barrel);
        AddBarrelStats(weaponProperties, currentBarrel);
        barrel.gameObject.SetActive(true);
        SynchronizeAttachmentPoints(weaponProperties);

        if (shouldSave) SaveAttachmentsToPlayerPrefs();
    }

    public void UpdateSight(Sight s, WeaponProperties weaponProperties, bool shouldSave = true)
    {
        if (s == null || s is CantedSight || weaponProperties == null || !CanEquipAttachment(s)) return;

        DisableAllOfType<Sight>();

        if (currentSight != null)
        {
            RemoveSightStats(weaponProperties, currentSight);
        }

        currentSight = CreateSightData(s);
        AddSightStats(weaponProperties, currentSight);
        s.gameObject.SetActive(true);
        SynchronizeAttachmentPoints(weaponProperties);

        if (shouldSave) SaveAttachmentsToPlayerPrefs();
    }

    public void UpdateCantedSight(CantedSight sight, WeaponProperties weaponProperties, bool shouldSave = true)
    {
        if (sight == null || weaponProperties == null || !CanEquipAttachment(sight)) return;

        DisableAllOfType<CantedSight>();

        if (currentCantedSight != null)
        {
            RemoveCantedSightStats(weaponProperties, currentCantedSight);
        }

        currentCantedSight = CreateSightData(sight);
        AddCantedSightStats(weaponProperties, currentCantedSight);
        sight.gameObject.SetActive(true);
        SynchronizeAttachmentPoints(weaponProperties);

        if (shouldSave) SaveAttachmentsToPlayerPrefs();
    }

    public bool TryUpdateCurrentSightZoom(Sight sight, float newZoomChange)
    {
        if (sight == null || weaponProperties == null) return false;

        if (sight is CantedSight)
        {
            if (currentCantedSight == null || currentCantedSight.attachmentName != sight.gameObject.name) return false;
            currentCantedSight.zoomChange = newZoomChange;
            return true;
        }

        if (currentSight == null) return false;
        if (currentSight.attachmentName != sight.gameObject.name) return false;

        weaponProperties.zoom += newZoomChange - currentSight.zoomChange;
        currentSight.zoomChange = newZoomChange;
        return true;
    }

    public void UpdateMag(Mag m, WeaponProperties weaponProperties, bool shouldSave = true)
    {
        if (m == null || weaponProperties == null || !CanEquipAttachment(m)) return;

        DisableAllOfType<Mag>();

        if (currentMag != null)
        {
            RemoveMagStats(weaponProperties, currentMag);
        }

        currentMag = CreateMagData(m);
        AddMagStats(weaponProperties, currentMag);
        m.gameObject.SetActive(true);
        SynchronizeAttachmentPoints(weaponProperties);

        if (shouldSave) SaveAttachmentsToPlayerPrefs();
    }

    public void UpdateSideGrip(SideGrip sg, WeaponProperties weaponProperties, bool shouldSave = true)
    {
        if (sg == null || weaponProperties == null || !CanEquipAttachment(sg)) return;

        DisableAllOfType<SideGrip>();

        if (currentSideGrip != null)
        {
            RemoveSideGripStats(weaponProperties, currentSideGrip);
        }

        currentSideGrip = CreateSideGripData(sg);
        AddSideGripStats(weaponProperties, currentSideGrip);
        sg.gameObject.SetActive(true);
        SynchronizeAttachmentPoints(weaponProperties);

        if (shouldSave) SaveAttachmentsToPlayerPrefs();
    }

    // NOVO
    public void UpdateErgonomics(Ergonomics e, WeaponProperties weaponProperties, bool shouldSave = true)
    {
        if (e == null || weaponProperties == null || !CanEquipAttachment(e)) return;

        DisableAllOfType<Ergonomics>();

        if (currentErgonomics != null)
        {
            RemoveErgonomicsStats(weaponProperties, currentErgonomics);
        }

        currentErgonomics = CreateErgonomicsData(e);
        AddErgonomicsStats(weaponProperties, currentErgonomics);
        e.gameObject.SetActive(true);
        SynchronizeAttachmentPoints(weaponProperties);

        if (shouldSave) SaveAttachmentsToPlayerPrefs();
    }

    // Método auxiliar para desativar todos os attachments de um tipo específico
    private void DisableAllOfType<T>() where T : Component
    {
        T[] components = GetComponentsInChildren<T>(true);
        foreach (T comp in components)
        {
            if (comp.GetType() != typeof(T)) continue;
            comp.gameObject.SetActive(false);
        }
    }
    #endregion

    #region Remove Methods (Public)
    public void ResetToStandardAttachments()
    {
        if (weaponProperties == null)
        {
            weaponProperties = GetComponent<WeaponProperties>();
            if (weaponProperties == null) return;
        }

        if (string.IsNullOrEmpty(weaponName)) weaponName = weaponProperties.weaponName;

        RemoveAllAttachmentsWithoutSaving();

        Grip grip = GetStandardAttachment<Grip>();
        if (grip != null) UpdateGrip(grip, weaponProperties, false);
        else
        {
            RemoveGrip(false);
            DisableAllOfType<Grip>();
        }

        Nozzle nozzle = GetStandardAttachment<Nozzle>();
        if (nozzle != null) UpdateNozzle(nozzle, weaponProperties, false);
        else
        {
            RemoveNozzle(false);
            DisableAllOfType<Nozzle>();
        }

        Barrel barrel = GetStandardAttachment<Barrel>() ?? GetFirstAvailableBarrel();
        if (barrel != null) UpdateBarrel(barrel, weaponProperties, false);

        Sight sight = GetStandardAttachment<Sight>();
        if (sight != null) UpdateSight(sight, weaponProperties, false);
        else
        {
            RemoveSight(false);
            DisableAllOfType<Sight>();
        }

        CantedSight cantedSight = GetStandardAttachment<CantedSight>();
        if (cantedSight != null) UpdateCantedSight(cantedSight, weaponProperties, false);
        else
        {
            RemoveCantedSight(false);
            DisableAllOfType<CantedSight>();
        }

        Mag mag = GetStandardAttachment<Mag>() ?? GetFirstAvailableMag();
        if (mag != null) UpdateMag(mag, weaponProperties, false);

        SideGrip sideGrip = GetStandardAttachment<SideGrip>();
        if (sideGrip != null) UpdateSideGrip(sideGrip, weaponProperties, false);
        else
        {
            RemoveSideGrip(false);
            DisableAllOfType<SideGrip>();
        }

        Ergonomics ergonomics = GetStandardAttachment<Ergonomics>();
        if (ergonomics != null) UpdateErgonomics(ergonomics, weaponProperties, false);
        else
        {
            RemoveErgonomics(false);
            DisableAllOfType<Ergonomics>();
        }

        SynchronizeAttachmentPoints(weaponProperties);
        SaveAttachmentsToPlayerPrefs();
    }

    public void RemoveAllAttatchments()
    {
        RemoveAllAttachmentsWithoutSaving();
        SaveAttachmentsToPlayerPrefs();
    }

    private void RemoveAllAttachmentsWithoutSaving()
    {
        RemoveGrip(false);
        RemoveNozzle(false);
        RemoveBarrel(false);
        RemoveMag_(false);
        RemoveSight(false);
        RemoveCantedSight(false);
        RemoveSideGrip(false);
        RemoveErgonomics(false);
        SynchronizeAttachmentPoints(weaponProperties);
    }

    public void RemoveGrip(bool shouldSave = true)
    {
        if (currentGrip != null && weaponProperties != null)
        {
            RemoveGripStats(weaponProperties, currentGrip);
            DisableAllOfType<Grip>();
            currentGrip = null;
            SynchronizeAttachmentPoints(weaponProperties);

            if (shouldSave)SaveAttachmentsToPlayerPrefs();
        }
    }

    public void RemoveNozzle(bool shouldSave = true)
    {
        if (currentNozzle != null && weaponProperties != null)
        {
            RemoveNozzleStats(weaponProperties, currentNozzle);
            DisableAllOfType<Nozzle>();
            currentNozzle = null;
            SynchronizeAttachmentPoints(weaponProperties);

            if (shouldSave) SaveAttachmentsToPlayerPrefs();
        }
    }

    public void RemoveBarrel(bool shouldSave = true)
    {
        if (currentBarrel != null && weaponProperties != null)
        {
            RemoveBarrelStats(weaponProperties, currentBarrel);
            DisableAllOfType<Barrel>();
            currentBarrel = null;
            SynchronizeAttachmentPoints(weaponProperties);

            if (shouldSave) SaveAttachmentsToPlayerPrefs();
        }
    }

    public void RemoveSight(bool shouldSave = true)
    {
        if (currentSight != null && weaponProperties != null)
        {
            RemoveSightStats(weaponProperties, currentSight);
            DisableAllOfType<Sight>();
            currentSight = null;
            SynchronizeAttachmentPoints(weaponProperties);

            if (shouldSave) SaveAttachmentsToPlayerPrefs();
        }
    }

    public void RemoveCantedSight(bool shouldSave = true)
    {
        if (currentCantedSight != null && weaponProperties != null)
        {
            RemoveCantedSightStats(weaponProperties, currentCantedSight);
            DisableAllOfType<CantedSight>();
            currentCantedSight = null;
            SynchronizeAttachmentPoints(weaponProperties);

            if (shouldSave) SaveAttachmentsToPlayerPrefs();
        }
    }

    public void RemoveMag_(bool shouldSave = true)
    {
        if (currentMag != null && weaponProperties != null)
        {
            RemoveMagStats(weaponProperties, currentMag);
            DisableAllOfType<Mag>();
            currentMag = null;
            SynchronizeAttachmentPoints(weaponProperties);

            if (shouldSave) SaveAttachmentsToPlayerPrefs();
        }
    }

    public void RemoveSideGrip(bool shouldSave = true)
    {
        if (currentSideGrip != null && weaponProperties != null)
        {
            RemoveSideGripStats(weaponProperties, currentSideGrip);
            DisableAllOfType<SideGrip>();
            currentSideGrip = null;
            SynchronizeAttachmentPoints(weaponProperties);

            if (shouldSave)SaveAttachmentsToPlayerPrefs();
        }
    }

    // NOVO
    public void RemoveErgonomics(bool shouldSave = true)
    {
        if (currentErgonomics != null && weaponProperties != null)
        {
            RemoveErgonomicsStats(weaponProperties, currentErgonomics);
            DisableAllOfType<Ergonomics>();
            currentErgonomics = null;
            SynchronizeAttachmentPoints(weaponProperties);

            if (shouldSave) SaveAttachmentsToPlayerPrefs();
        }
    }
    #endregion

    #region Helper Methods
    private T GetStandardAttachment<T>() where T : Attatchment
    {
        T[] attachments = GetComponentsInChildren<T>(true);
        foreach (T attachment in attachments)
        {
            if (attachment.GetType() == typeof(T) && attachment.isStandardAttatchment)
                return attachment;
        }

        return null;
    }

    private T FindAttachmentByName<T>(string name) where T : Component
    {
        if (string.IsNullOrEmpty(name)) return null;

        T[] components = GetComponentsInChildren<T>(true);
        foreach (T comp in components)
        {
            if (comp.gameObject.name == name) return comp;
        }
        return null;
    }

    private Mag GetFirstAvailableMag()
    {
        Mag standardMag = GetStandardAttachment<Mag>();
        if (standardMag != null) return standardMag;

        Mag[] mags = GetComponentsInChildren<Mag>(true);
        if (mags != null && mags.Length > 0) return mags[0];
        
        return null;
    }

    private Barrel GetFirstAvailableBarrel()
    {
        Barrel standardBarrel = GetStandardAttachment<Barrel>();
        if (standardBarrel != null) return standardBarrel;

        Barrel[] barrels = GetComponentsInChildren<Barrel>(true);
        if (barrels != null && barrels.Length > 0) return barrels[0];

        return null;
    }
    #endregion

    #region Save/Load Methods
    private void SaveAttachmentsToPlayerPrefs()
    {
        if (string.IsNullOrEmpty(weaponName)) return;

        string saveKey = $"WeaponAttachments_{weaponName}";
        var saveData = new WeaponAttachmentSaveData(weaponName);

        if (currentSight != null) saveData.activeSight = currentSight.attachmentName;
        if (currentCantedSight != null) saveData.activeCantedSight = currentCantedSight.attachmentName;
        if (currentNozzle != null) saveData.activeNozzle = currentNozzle.attachmentName;
        if (currentBarrel != null) saveData.activeBarrel = currentBarrel.attachmentName;
        if (currentMag != null) saveData.activeMag = currentMag.attachmentName;
        if (currentGrip != null) saveData.activeGrip = currentGrip.attachmentName;
        if (currentSideGrip != null) saveData.activeSideGrip = currentSideGrip.attachmentName;
        if (currentErgonomics != null) saveData.activeErgonomics = currentErgonomics.attachmentName; // NOVO

        string json = JsonUtility.ToJson(saveData);

        PlayerPrefs.SetString(saveKey, json);
        PlayerPrefs.Save();
    }

    public void LoadAttachmentsFromPlayerPrefs()
    {
        if (string.IsNullOrEmpty(weaponName) || weaponProperties == null) return;

        string saveKey = $"WeaponAttachments_{weaponName}";

        if (!PlayerPrefs.HasKey(saveKey))
        {
            ResetToStandardAttachments();
            return;
        }

        string json = PlayerPrefs.GetString(saveKey);
        var saveData = JsonUtility.FromJson<WeaponAttachmentSaveData>(json);

        if (saveData == null)
        {
            Debug.LogError($"[{weaponName}] Failed to deserialize saved data");
            return;
        }

        ApplySavedAttachments(saveData);
    }

    private void ApplySavedAttachments(WeaponAttachmentSaveData saveData)
    {
        if (saveData == null || weaponProperties == null) return;

        if (!string.IsNullOrEmpty(saveData.activeSight))
        {
            Sight sight = FindAttachmentByName<Sight>(saveData.activeSight);
            if (sight is CantedSight legacyCantedSight) UpdateCantedSight(legacyCantedSight, weaponProperties, false);
            else if (sight != null) UpdateSight(sight, weaponProperties, false);
        }

        if (!string.IsNullOrEmpty(saveData.activeCantedSight))
        {
            CantedSight cantedSight = FindAttachmentByName<CantedSight>(saveData.activeCantedSight);
            if (cantedSight != null) UpdateCantedSight(cantedSight, weaponProperties, false);
        }

        string nozzleName = saveData.activeNozzle;
        string barrelName = saveData.activeBarrel;

        // Saves anteriores separavam o antigo Barrel usando apenas activeBarrel.
        // Esse attachment agora e um Nozzle; a versao evita confundi-lo com o novo Barrel.
        if (saveData.version < WeaponAttachmentSaveData.CurrentVersion)
        {
            nozzleName = saveData.activeBarrel;
            barrelName = "";
        }

        if (!string.IsNullOrEmpty(nozzleName))
        {
            Nozzle nozzle = FindAttachmentByName<Nozzle>(nozzleName);
            if (nozzle != null) UpdateNozzle(nozzle, weaponProperties, false);
        }

        Barrel barrel = FindAttachmentByName<Barrel>(barrelName);
        if (barrel == null) barrel = GetFirstAvailableBarrel();
        if (barrel != null) UpdateBarrel(barrel, weaponProperties, false);

        Mag mag = FindAttachmentByName<Mag>(saveData.activeMag);
        if (mag == null) mag = GetFirstAvailableMag();
        if (mag != null) UpdateMag(mag, weaponProperties, false);

        if (!string.IsNullOrEmpty(saveData.activeGrip))
        {
            Grip grip = FindAttachmentByName<Grip>(saveData.activeGrip);
            if (grip != null) UpdateGrip(grip, weaponProperties, false);
        }

        if (!string.IsNullOrEmpty(saveData.activeSideGrip))
        {
            SideGrip sideGrip = FindAttachmentByName<SideGrip>(saveData.activeSideGrip);
            if (sideGrip != null) UpdateSideGrip(sideGrip, weaponProperties, false);
        }

        // NOVO
        if (!string.IsNullOrEmpty(saveData.activeErgonomics))
        {
            Ergonomics ergonomics = FindAttachmentByName<Ergonomics>(saveData.activeErgonomics);
            if (ergonomics != null) UpdateErgonomics(ergonomics, weaponProperties, false);
        }
    }

    public void LoadSavedAttachments(PlayerAttachmentsSaveData saveData)
    {
        if (saveData == null || weaponProperties == null) return;

        var weaponData = saveData.GetWeaponData(weaponName);
        if (weaponData == null) return;

        ApplySavedAttachments(weaponData);
    }
    #endregion

    #region Public Getters
    public AttachmentData GetCurrentGrip() => currentGrip;
    public AttachmentData GetCurrentNozzle() => currentNozzle;
    public AttachmentData GetCurrentBarrel() => currentBarrel;
    public AttachmentData GetCurrentSight() => currentSight;
    public AttachmentData GetCurrentCantedSight() => currentCantedSight;
    public AttachmentData GetCurrentMag() => currentMag;
    public AttachmentData GetCurrentSideGrip() => currentSideGrip;
    public AttachmentData GetCurrentErgonomics() => currentErgonomics;
    public bool HasGrip() => currentGrip != null;
    public bool HasNozzle() => currentNozzle != null;
    public bool HasBarrel() => currentBarrel != null;
    public bool HasSight() => currentSight != null;
    public bool HasCantedSight() => currentCantedSight != null;
    public bool HasMag() => currentMag != null;
    public bool HasSideGrip() => currentSideGrip != null;
    public bool HasErgonomics() => currentErgonomics != null;
    #endregion
}
