using UnityEngine;
using System;
using System.Collections.Generic;

public class AttatchmentManager : MonoBehaviour
{
    // Constante para o valor mínimo de recoil
    private const float MIN_RECOIL_VALUE = 0.1f;
    private const float MAX_RECOIL_VALUE = 10f;

    // Estruturas para guardar os valores dos attachments
    [Serializable]
    public class AttachmentData
    {
        public string attachmentName;
        public float points;

        // Grip
        public float vertical_recoil_change;
        public float horizontal_recoil_change;
        public float first_shoot_change;
        public float reload_speed_change;
        public float ads_speed_change;
        public float pick_up_weapon_speed_change;
        public float store_weapon_speed_change;

        // Barrel
        public float muzzle_lightning_change;
        public float muzzle_velocity_change;
        public float shoot_pith_change;
        public float shoot_volume_change;
        public float spread_change;

        // Sight
        public float zoom_change;
        public float sway_change;

        // Mag
        public int bullet_counter_change;
        public float reload_speed_changer;
        public bool is_tape_mag;

        // Ergonomics (NOVO)
        public Vector3 visual_recoil_change;
        public List<WeaponProperties.FireMode> fire_modes_change = new List<WeaponProperties.FireMode>();
        public float rate_of_fire_change;
        public int burst_bullets_per_tap_change;
        public float burst_time_between_bursts_change;
    }

    // Trackers dos attachments atuais (apenas para saber quais estão ativos)
    private AttachmentData currentGrip;
    private AttachmentData currentBarrel;
    private AttachmentData currentSight;
    private AttachmentData currentMag;
    private AttachmentData currentSideGrip;
    private AttachmentData currentErgonomics;

    // Referência para o WeaponProperties
    private WeaponProperties weaponProperties;

    // Nome da arma para salvar/carregar
    private string weaponName;

    public void InitializeAttachments()
    {
        weaponProperties = GetComponent<WeaponProperties>();
        if (weaponProperties != null)
            weaponName = weaponProperties.weapon_name;

        // Desativa todos os attachments no início
        Grip[] grips = GetComponentsInChildren<Grip>(true);
        foreach (Grip grip in grips)
        {
            grip.gameObject.SetActive(false);
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

        // NOVO
        Ergonomics[] ergonomics = GetComponentsInChildren<Ergonomics>(true);
        foreach (Ergonomics ergo in ergonomics)
        {
            ergo.gameObject.SetActive(false);
        }

        // Carrega attachments salvos
        LoadAttachmentsFromPlayerPrefs();
    }

    #region Data Creation Methods

    private AttachmentData CreateGripData(Grip g)
    {
        return new AttachmentData
        {
            attachmentName = g.gameObject.name,
            points = g.attatchment_points,
            vertical_recoil_change = g.vertical_recoil_change,
            horizontal_recoil_change = g.horizontal_recoil_change,
            first_shoot_change = g.first_shoot_change,
            reload_speed_change = g.reload_speed_change,
            ads_speed_change = g.ads_speed_change,
            pick_up_weapon_speed_change = g.pick_up_weapon_speed_change,
            store_weapon_speed_change = g.store_weapon_speed_change
        };
    }

    private AttachmentData CreateBarrelData(Barrel b)
    {
        return new AttachmentData
        {
            attachmentName = b.gameObject.name,
            points = b.attatchment_points,
            horizontal_recoil_change = b.horizontal_recoil_change,
            vertical_recoil_change = b.vertical_recoil_change,
            first_shoot_change = b.first_shoot_recoil_change,
            muzzle_lightning_change = b.muzzle_lightning_change,
            muzzle_velocity_change = b.muzzle_velocity_change,
            shoot_pith_change = b.shoot_pith_change,
            spread_change = b.spread_change
        };
    }

    private AttachmentData CreateSightData(Sight s)
    {
        return new AttachmentData
        {
            attachmentName = s.gameObject.name,
            points = s.attatchment_points,
            zoom_change = s.zoom_change,
            ads_speed_change = s.ads_speed_change,
            sway_change = s.sway_change,
        };
    }

    private AttachmentData CreateMagData(Mag m)
    {
        return new AttachmentData
        {
            attachmentName = m.gameObject.name,
            points = m.attatchment_points,
            bullet_counter_change = m.bullet_counter_change,
            ads_speed_change = m.ads_speed_change,
            reload_speed_changer = m.reload_speed_changer,
        };
    }

    private AttachmentData CreateSideGripData(SideGrip sg)
    {
        return new AttachmentData
        {
            attachmentName = sg.gameObject.name,
            points = sg.attatchment_points
        };
    }

    // NOVO
    private AttachmentData CreateErgonomicsData(Ergonomics e)
    {
        return new AttachmentData
        {
            attachmentName = e.gameObject.name,
            points = e.attatchment_points,
            visual_recoil_change = e.visualRecoilChange,
            reload_speed_change = e.reloadSpeedChange,
            ads_speed_change = e.adsSpeedChange,
            pick_up_weapon_speed_change = e.pickupWeaponSpeedChange,
            store_weapon_speed_change = e.storeWeaponSpeedChange,
            fire_modes_change = new List<WeaponProperties.FireMode>(e.fireModesChange),
            rate_of_fire_change = e.rafeOfFireChange, // Mantido do seu script original
            burst_bullets_per_tap_change = e.burstBulletsPerTapChange,
            burst_time_between_bursts_change = e.burstTimeBetweenBurstsChange
        };
    }

    #endregion

    #region Stats Addition Methods

    private void AddGripStats(WeaponProperties wp, AttachmentData grip)
    {
        for (int i = 0; i < wp.vertical_recoil.Length; i++)
        {
            wp.vertical_recoil[i] = Math.Clamp(wp.vertical_recoil[i] + grip.vertical_recoil_change, MIN_RECOIL_VALUE, MAX_RECOIL_VALUE);
        }

        for (int i = 0; i < wp.horizontal_recoil.Length; i++)
        {
            wp.horizontal_recoil[i] += grip.horizontal_recoil_change;
        }

        wp.current_attachment_points += grip.points;
        wp.first_shoot_increaser += grip.first_shoot_change;
        wp.pick_up_weapon_speed += grip.pick_up_weapon_speed_change;
        wp.store_weapon_speed += grip.store_weapon_speed_change;
        wp.reload_time += grip.reload_speed_change;
        wp.ads_speed += grip.ads_speed_change;
    }

    private void AddBarrelStats(WeaponProperties wp, AttachmentData barrel)
    {
        if (wp == null || barrel == null) return;

        for (int i = 0; i < wp.vertical_recoil.Length; i++)
        {
            wp.vertical_recoil[i] = Math.Clamp(wp.vertical_recoil[i] + barrel.vertical_recoil_change, MIN_RECOIL_VALUE, MAX_RECOIL_VALUE);
        }

        for (int i = 0; i < wp.horizontal_recoil.Length; i++)
        {
            if (wp.horizontal_recoil[i] < 0)
                wp.horizontal_recoil[i] -= barrel.horizontal_recoil_change;
            else
                wp.horizontal_recoil[i] += barrel.horizontal_recoil_change;
        }

        wp.current_attachment_points += barrel.points;
        wp.first_shoot_increaser += barrel.first_shoot_change;
        wp.muzzle_velocity += barrel.muzzle_velocity_change;
        wp.weapon_sound.shootSoundProperties.pitch += barrel.shoot_pith_change;
        wp.weapon_sound.shootSoundProperties.volume += barrel.shoot_volume_change;
        wp.spread_increaser += barrel.spread_change;
    }

    private void AddSightStats(WeaponProperties wp, AttachmentData sight)
    {
        wp.current_attachment_points += sight.points;
        wp.zoom += sight.zoom_change;
        wp.ads_speed += sight.ads_speed_change;
    }

    private void AddMagStats(WeaponProperties wp, AttachmentData mag)
    {
        wp.current_attachment_points += mag.points;
        wp.bullets_per_mag = mag.bullet_counter_change;
        wp.ads_speed += mag.ads_speed_change;
        wp.reload_time += mag.reload_speed_changer;
    }

    private void AddSideGripStats(WeaponProperties wp, AttachmentData sideGrip)
    {
        wp.current_attachment_points += sideGrip.points;
    }

    // NOVO
    private void AddErgonomicsStats(WeaponProperties wp, AttachmentData ergo)
    {
        if (wp == null || ergo == null) return;

        for (int i = 0; i < wp.vertical_recoil.Length; i++)
        {
            wp.vertical_recoil[i] = Math.Clamp(wp.vertical_recoil[i] + ergo.vertical_recoil_change, MIN_RECOIL_VALUE, MAX_RECOIL_VALUE);
        }

        for (int i = 0; i < wp.horizontal_recoil.Length; i++)
        {
            if (wp.horizontal_recoil[i] < 0)
                wp.horizontal_recoil[i] -= ergo.horizontal_recoil_change;
            else
                wp.horizontal_recoil[i] += ergo.horizontal_recoil_change;
        }

        wp.current_attachment_points += ergo.points;
        wp.first_shoot_increaser += ergo.first_shoot_change;
        wp.reload_time += ergo.reload_speed_change;
        wp.ads_speed += ergo.ads_speed_change;
        wp.pick_up_weapon_speed += ergo.pick_up_weapon_speed_change;
        wp.store_weapon_speed += ergo.store_weapon_speed_change;

        // Assumindo que essas variáveis existam em WeaponProperties. Ajuste se o nome for diferente!
        wp.visual_recoil += ergo.visual_recoil_change;
        wp.rate_of_fire += ergo.rate_of_fire_change;
        wp.bullets_per_tap += ergo.burst_bullets_per_tap_change;
        wp.time_between_bursts += ergo.burst_time_between_bursts_change;

        if (ergo.fire_modes_change != null && ergo.fire_modes_change.Count > 0)
        {
            foreach (var fm in ergo.fire_modes_change)
            {
                if (!wp.fire_modes.Contains(fm)) // Assumindo que a lista se chama 'fire_modes'
                    wp.fire_modes.Add(fm);
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
        attachment.vertical_recoil_change = 0;
        attachment.horizontal_recoil_change = 0;
        attachment.first_shoot_change = 0;
        attachment.reload_speed_change = 0;
        attachment.ads_speed_change = 0;
        attachment.pick_up_weapon_speed_change = 0;
        attachment.store_weapon_speed_change = 0;
        attachment.muzzle_lightning_change = 0;
        attachment.muzzle_velocity_change = 0;
        attachment.shoot_pith_change = 0;
        attachment.spread_change = 0;
        attachment.zoom_change = 0;
        attachment.sway_change = 0;
        attachment.bullet_counter_change = 0;
        attachment.reload_speed_changer = 0;
        attachment.is_tape_mag = false;
        attachment.visual_recoil_change = Vector3.zero;
        if (attachment.fire_modes_change != null) attachment.fire_modes_change.Clear();
        attachment.rate_of_fire_change = 0;
        attachment.burst_bullets_per_tap_change = 0;
        attachment.burst_time_between_bursts_change = 0;
    }

    private void RemoveGripStats(WeaponProperties wp, AttachmentData grip)
    {
        if (wp == null || grip == null) return;

        for (int i = 0; i < wp.vertical_recoil.Length; i++)
        {
            wp.vertical_recoil[i] = Math.Clamp(wp.vertical_recoil[i] - grip.vertical_recoil_change, MIN_RECOIL_VALUE, MAX_RECOIL_VALUE);
        }

        for (int i = 0; i < wp.horizontal_recoil.Length; i++)
        {
            wp.horizontal_recoil[i] -= grip.horizontal_recoil_change;
        }

        wp.current_attachment_points -= grip.points;
        wp.first_shoot_increaser -= grip.first_shoot_change;
        wp.pick_up_weapon_speed -= grip.pick_up_weapon_speed_change;
        wp.store_weapon_speed -= grip.store_weapon_speed_change;
        wp.reload_time -= grip.reload_speed_change;
        wp.ads_speed -= grip.ads_speed_change;

        ResetAttachmentDataToZero(grip);
    }

    private void RemoveBarrelStats(WeaponProperties wp, AttachmentData barrel)
    {
        if (wp == null || barrel == null) return;

        for (int i = 0; i < wp.vertical_recoil.Length; i++)
        {
            wp.vertical_recoil[i] = Math.Clamp(wp.vertical_recoil[i] - barrel.vertical_recoil_change, MIN_RECOIL_VALUE, MAX_RECOIL_VALUE);
        }

        for (int i = 0; i < wp.horizontal_recoil.Length; i++)
        {
            if (wp.horizontal_recoil[i] < 0)
                wp.horizontal_recoil[i] += barrel.horizontal_recoil_change;
            else
                wp.horizontal_recoil[i] -= barrel.horizontal_recoil_change;
        }

        wp.current_attachment_points -= barrel.points;
        wp.first_shoot_increaser -= barrel.first_shoot_change;
        wp.muzzle_velocity -= barrel.muzzle_velocity_change;
        wp.weapon_sound.shootSoundProperties.pitch -= barrel.shoot_pith_change;
        wp.weapon_sound.shootSoundProperties.volume -= barrel.shoot_volume_change;
        wp.spread_increaser -= barrel.spread_change;

        ResetAttachmentDataToZero(barrel);
    }

    private void RemoveSightStats(WeaponProperties wp, AttachmentData sight)
    {
        if (wp == null || sight == null) return;

        wp.current_attachment_points -= sight.points;
        wp.zoom -= sight.zoom_change;
        wp.ads_speed -= sight.ads_speed_change;

        ResetAttachmentDataToZero(sight);
    }

    private void RemoveMagStats(WeaponProperties wp, AttachmentData mag)
    {
        if (wp == null || mag == null) return;

        wp.current_attachment_points -= mag.points;
        wp.bullets_per_mag -= mag.bullet_counter_change;
        wp.ads_speed -= mag.ads_speed_change;
        wp.reload_time -= mag.reload_speed_changer;

        ResetAttachmentDataToZero(mag);
    }

    private void RemoveSideGripStats(WeaponProperties wp, AttachmentData sideGrip)
    {
        if (wp == null || sideGrip == null) return;

        wp.current_attachment_points -= sideGrip.points;
        ResetAttachmentDataToZero(sideGrip);
    }

    // NOVO
    private void RemoveErgonomicsStats(WeaponProperties wp, AttachmentData ergo)
    {
        if (wp == null || ergo == null) return;

        wp.current_attachment_points -= ergo.points;
        wp.reload_time -= ergo.reload_speed_change;
        wp.ads_speed -= ergo.ads_speed_change;
        wp.pick_up_weapon_speed -= ergo.pick_up_weapon_speed_change;
        wp.store_weapon_speed -= ergo.store_weapon_speed_change;

        wp.visual_recoil -= ergo.visual_recoil_change;
        wp.rate_of_fire -= ergo.rate_of_fire_change;
        wp.bullets_per_tap -= ergo.burst_bullets_per_tap_change;
        wp.time_between_bursts -= ergo.burst_time_between_bursts_change;

        if (ergo.fire_modes_change != null && ergo.fire_modes_change.Count > 0)
        {
            foreach (var fm in ergo.fire_modes_change)
            {
                wp.fire_modes.Remove(fm);
            }
        }

        ResetAttachmentDataToZero(ergo);
    }

    #endregion

    #region Update Methods - APENAS SETACTIVE

    public void UpdateGrip(Grip g, WeaponProperties weaponProperties, bool shouldSave = true)
    {
        if (g == null || weaponProperties == null) return;

        DisableAllOfType<Grip>();

        if (currentGrip != null)
        {
            RemoveGripStats(weaponProperties, currentGrip);
        }

        currentGrip = CreateGripData(g);
        AddGripStats(weaponProperties, currentGrip);
        g.gameObject.SetActive(true);

        if (shouldSave)
            SaveAttachmentsToPlayerPrefs();
    }

    public void UpdateBarrel(Barrel b, WeaponProperties weaponProperties, bool shouldSave = true)
    {
        if (b == null || weaponProperties == null) return;

        DisableAllOfType<Barrel>();

        if (currentBarrel != null)
        {
            RemoveBarrelStats(weaponProperties, currentBarrel);
        }

        currentBarrel = CreateBarrelData(b);
        AddBarrelStats(weaponProperties, currentBarrel);
        b.gameObject.SetActive(true);

        if (shouldSave)
            SaveAttachmentsToPlayerPrefs();
    }

    public void UpdateSight(Sight s, WeaponProperties weaponProperties, bool shouldSave = true)
    {
        if (s == null || weaponProperties == null) return;

        DisableAllOfType<Sight>();

        if (currentSight != null)
        {
            RemoveSightStats(weaponProperties, currentSight);
        }

        currentSight = CreateSightData(s);
        AddSightStats(weaponProperties, currentSight);
        s.gameObject.SetActive(true);

        if (shouldSave)
            SaveAttachmentsToPlayerPrefs();
    }

    public void UpdateMag(Mag m, WeaponProperties weaponProperties, bool shouldSave = true)
    {
        if (weaponProperties == null) return;

        DisableAllOfType<Mag>();

        if (currentMag != null)
        {
            RemoveMagStats(weaponProperties, currentMag);
        }

        currentMag = CreateMagData(m);
        AddMagStats(weaponProperties, currentMag);
        m.gameObject.SetActive(true);

        if (shouldSave)
            SaveAttachmentsToPlayerPrefs();
    }

    public void UpdateSideGrip(SideGrip sg, WeaponProperties weaponProperties, bool shouldSave = true)
    {
        if (sg == null || weaponProperties == null) return;

        DisableAllOfType<SideGrip>();

        if (currentSideGrip != null)
        {
            RemoveSideGripStats(weaponProperties, currentSideGrip);
        }

        currentSideGrip = CreateSideGripData(sg);
        AddSideGripStats(weaponProperties, currentSideGrip);
        sg.gameObject.SetActive(true);

        if (shouldSave)
            SaveAttachmentsToPlayerPrefs();
    }

    // NOVO
    public void UpdateErgonomics(Ergonomics e, WeaponProperties weaponProperties, bool shouldSave = true)
    {
        if (e == null || weaponProperties == null) return;

        DisableAllOfType<Ergonomics>();

        if (currentErgonomics != null)
        {
            RemoveErgonomicsStats(weaponProperties, currentErgonomics);
        }

        currentErgonomics = CreateErgonomicsData(e);
        AddErgonomicsStats(weaponProperties, currentErgonomics);
        e.gameObject.SetActive(true);

        if (shouldSave)
            SaveAttachmentsToPlayerPrefs();
    }

    // Método auxiliar para desativar todos os attachments de um tipo específico
    private void DisableAllOfType<T>() where T : Component
    {
        T[] components = GetComponentsInChildren<T>(true);
        foreach (T comp in components)
        {
            comp.gameObject.SetActive(false);
        }
    }

    #endregion

    #region Remove Methods (Public)

    public void RemoveAllAttatchments()
    {
        RemoveGrip(true);
        RemoveBarrel(true);
        RemoveMag_(true);
        RemoveSight(true);
        RemoveSideGrip(true);
        RemoveErgonomics(true); // NOVO
    }

    public void RemoveGrip(bool shouldSave = true)
    {
        if (currentGrip != null && weaponProperties != null)
        {
            RemoveGripStats(weaponProperties, currentGrip);
            DisableAllOfType<Grip>();
            currentGrip = null;

            if (shouldSave)
                SaveAttachmentsToPlayerPrefs();
        }
    }

    public void RemoveBarrel(bool shouldSave = true)
    {
        if (currentBarrel != null && weaponProperties != null)
        {
            RemoveBarrelStats(weaponProperties, currentBarrel);
            DisableAllOfType<Barrel>();
            currentBarrel = null;

            if (shouldSave)
                SaveAttachmentsToPlayerPrefs();
        }
    }

    public void RemoveSight(bool shouldSave = true)
    {
        if (currentSight != null && weaponProperties != null)
        {
            RemoveSightStats(weaponProperties, currentSight);
            DisableAllOfType<Sight>();
            currentSight = null;

            if (shouldSave)
                SaveAttachmentsToPlayerPrefs();
        }
    }

    public void RemoveMag_(bool shouldSave = true)
    {
        if (currentMag != null && weaponProperties != null)
        {
            RemoveMagStats(weaponProperties, currentMag);
            DisableAllOfType<Mag>();
            currentMag = null;

            if (shouldSave)
                SaveAttachmentsToPlayerPrefs();
        }
    }

    public void RemoveSideGrip(bool shouldSave = true)
    {
        if (currentSideGrip != null && weaponProperties != null)
        {
            RemoveSideGripStats(weaponProperties, currentSideGrip);
            DisableAllOfType<SideGrip>();
            currentSideGrip = null;

            if (shouldSave)
                SaveAttachmentsToPlayerPrefs();
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

            if (shouldSave)
                SaveAttachmentsToPlayerPrefs();
        }
    }

    #endregion

    #region Helper Methods

    private T FindAttachmentByName<T>(string name) where T : Component
    {
        if (string.IsNullOrEmpty(name)) return null;

        T[] components = GetComponentsInChildren<T>(true);
        foreach (T comp in components)
        {
            if (comp.gameObject.name == name)
                return comp;
        }
        return null;
    }

    private Mag GetFirstAvailableMag()
    {
        Mag[] mags = GetComponentsInChildren<Mag>(true);
        if (mags != null && mags.Length > 0)
        {
            return mags[0];
        }
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
            var defaultSaveData = new WeaponAttachmentSaveData(weaponName);

            Mag firstMag = GetFirstAvailableMag();
            if (firstMag != null)
            {
                defaultSaveData.activeMag = firstMag.gameObject.name;
            }

            string defaultJson = JsonUtility.ToJson(defaultSaveData);
            PlayerPrefs.SetString(saveKey, defaultJson);
            PlayerPrefs.Save();

            LoadAttachmentsFromPlayerPrefs();
            return;
        }

        string json = PlayerPrefs.GetString(saveKey);
        var saveData = JsonUtility.FromJson<WeaponAttachmentSaveData>(json);

        if (saveData == null)
        {
            Debug.LogError($"[{weaponName}] Falha ao desserializar save");
            return;
        }

        if (!string.IsNullOrEmpty(saveData.activeSight))
        {
            Sight sight = FindAttachmentByName<Sight>(saveData.activeSight);
            if (sight != null) UpdateSight(sight, weaponProperties, false);
        }

        if (!string.IsNullOrEmpty(saveData.activeBarrel))
        {
            Barrel barrel = FindAttachmentByName<Barrel>(saveData.activeBarrel);
            if (barrel != null) UpdateBarrel(barrel, weaponProperties, false);
        }

        if (!string.IsNullOrEmpty(saveData.activeMag))
        {
            Mag mag = FindAttachmentByName<Mag>(saveData.activeMag);
            if (mag != null) UpdateMag(mag, weaponProperties, false);
        }

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

        if (!string.IsNullOrEmpty(weaponData.activeSight))
        {
            Sight sight = FindAttachmentByName<Sight>(weaponData.activeSight);
            if (sight != null) UpdateSight(sight, weaponProperties, false);
        }

        if (!string.IsNullOrEmpty(weaponData.activeBarrel))
        {
            Barrel barrel = FindAttachmentByName<Barrel>(weaponData.activeBarrel);
            if (barrel != null) UpdateBarrel(barrel, weaponProperties, false);
        }

        if (!string.IsNullOrEmpty(weaponData.activeMag))
        {
            Mag mag = FindAttachmentByName<Mag>(weaponData.activeMag);
            if (mag != null) UpdateMag(mag, weaponProperties, false);
        }

        if (!string.IsNullOrEmpty(weaponData.activeGrip))
        {
            Grip grip = FindAttachmentByName<Grip>(weaponData.activeGrip);
            if (grip != null) UpdateGrip(grip, weaponProperties, false);
        }

        if (!string.IsNullOrEmpty(weaponData.activeSideGrip))
        {
            SideGrip sideGrip = FindAttachmentByName<SideGrip>(weaponData.activeSideGrip);
            if (sideGrip != null) UpdateSideGrip(sideGrip, weaponProperties, false);
        }

        // NOVO
        if (!string.IsNullOrEmpty(weaponData.activeErgonomics))
        {
            Ergonomics ergonomics = FindAttachmentByName<Ergonomics>(weaponData.activeErgonomics);
            if (ergonomics != null) UpdateErgonomics(ergonomics, weaponProperties, false);
        }
    }

    #endregion

    #region Public Getters

    public AttachmentData GetCurrentGrip() => currentGrip;
    public AttachmentData GetCurrentBarrel() => currentBarrel;
    public AttachmentData GetCurrentSight() => currentSight;
    public AttachmentData GetCurrentMag() => currentMag;
    public AttachmentData GetCurrentSideGrip() => currentSideGrip;
    public AttachmentData GetCurrentErgonomics() => currentErgonomics; // NOVO

    public bool HasGrip() => currentGrip != null;
    public bool HasBarrel() => currentBarrel != null;
    public bool HasSight() => currentSight != null;
    public bool HasMag() => currentMag != null;
    public bool HasSideGrip() => currentSideGrip != null;
    public bool HasErgonomics() => currentErgonomics != null; // NOVO

    #endregion
}