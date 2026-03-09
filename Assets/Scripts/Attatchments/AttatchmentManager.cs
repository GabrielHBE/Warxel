using UnityEngine;
using System;

public class AttatchmentManager : MonoBehaviour
{
    [SerializeField] private GameObject left_hand_target;
    [SerializeField] private GameObject right_hand_target;

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
        public float weapon_stability_change;
        public float reload_speed_change;
        public float ads_speed_change;
        public float pick_up_weapon_speed_change;

        // Barrel
        public float muzzle_lightning_change;
        public float muzzle_velocity_change;
        public float shoot_pith_change;
        public float spread_change;

        // Sight
        public float zoom_change;
        public float sway_change;

        // Mag
        public int bullet_counter_change;
        public float stability_change;
        public float reload_speed_changer;
        public bool is_tape_mag;
    }

    // Trackers dos attachments atuais (apenas para saber quais estão ativos)
    private AttachmentData currentGrip;
    private AttachmentData currentBarrel;
    private AttachmentData currentSight;
    private AttachmentData currentMag;
    private AttachmentData currentSideGrip;

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
            grip.left_hand_holder = left_hand_target;
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

        // Carrega attachments salvos
        LoadAttachmentsFromPlayerPrefs();

        /*
        // Se ainda não tem Mag equipado, equipa o primeiro disponível
        if (currentMag == null && weaponProperties != null)
        {
            Mag firstMag = GetFirstAvailableMag();
            if (firstMag != null)
            {
                UpdateMag(firstMag, weaponProperties, true);
            }
        }
        */
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
            weapon_stability_change = g.weapon_stability_change,
            reload_speed_change = g.reload_speed_change,
            ads_speed_change = g.ads_speed_change,
            pick_up_weapon_speed_change = g.pick_up_weapon_speed_change
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
            stability_change = m.stability_change,
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
        wp.weapon_stability += grip.weapon_stability_change;
        wp.reload_time += grip.reload_speed_change;
        wp.ads_speed += grip.ads_speed_change;
    }

    private void AddBarrelStats(WeaponProperties wp, AttachmentData barrel)
    {
        for (int i = 0; i < wp.vertical_recoil.Length; i++)
        {
            wp.vertical_recoil[i] = Math.Clamp(wp.vertical_recoil[i] + barrel.vertical_recoil_change, MIN_RECOIL_VALUE, MAX_RECOIL_VALUE);
        }

        for (int i = 0; i < wp.horizontal_recoil.Length; i++)
        {
            wp.horizontal_recoil[i] += barrel.horizontal_recoil_change;
        }

        wp.current_attachment_points += barrel.points;
        wp.first_shoot_increaser += barrel.first_shoot_change;
        wp.muzzle_velocity += barrel.muzzle_velocity_change;
        wp.weapon_sound.shoot_sound.pitch += barrel.shoot_pith_change;
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
        wp.weapon_stability += mag.stability_change;
        wp.ads_speed += mag.ads_speed_change;
        wp.reload_time += mag.reload_speed_changer;
    }

    private void AddSideGripStats(WeaponProperties wp, AttachmentData sideGrip)
    {
        wp.current_attachment_points += sideGrip.points;
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
        attachment.weapon_stability_change = 0;
        attachment.reload_speed_change = 0;
        attachment.ads_speed_change = 0;
        attachment.pick_up_weapon_speed_change = 0;
        attachment.muzzle_lightning_change = 0;
        attachment.muzzle_velocity_change = 0;
        attachment.shoot_pith_change = 0;
        attachment.spread_change = 0;
        attachment.zoom_change = 0;
        attachment.sway_change = 0;
        attachment.bullet_counter_change = 0;
        attachment.stability_change = 0;
        attachment.reload_speed_changer = 0;
        attachment.is_tape_mag = false;
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
        wp.weapon_stability -= grip.weapon_stability_change;
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
            wp.horizontal_recoil[i] -= barrel.horizontal_recoil_change;
        }

        wp.current_attachment_points -= barrel.points;
        wp.first_shoot_increaser -= barrel.first_shoot_change;
        wp.muzzle_velocity -= barrel.muzzle_velocity_change;
        wp.weapon_sound.shoot_sound.pitch -= barrel.shoot_pith_change;
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
        wp.weapon_stability -= mag.stability_change;
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

    #endregion

    #region Update Methods - APENAS SETACTIVE

    public void UpdateGrip(Grip g, WeaponProperties weaponProperties, bool shouldSave = true)
    {
        if (g == null || weaponProperties == null) return;

        // Primeiro, desativa o grip atual
        DisableAllOfType<Grip>();

        // Remove stats do grip atual se existir
        if (currentGrip != null)
        {
            RemoveGripStats(weaponProperties, currentGrip);
        }

        // Ativa o novo grip e adiciona stats
        currentGrip = CreateGripData(g);
        AddGripStats(weaponProperties, currentGrip);
        g.gameObject.SetActive(true);

        if (shouldSave)
            SaveAttachmentsToPlayerPrefs();

        //Debug.Log($"Grip atualizado para: {currentGrip.attachmentName}");
    }

    public void UpdateBarrel(Barrel b, WeaponProperties weaponProperties, bool shouldSave = true)
    {
        if (b == null || weaponProperties == null) return;

        // Primeiro, desativa o barrel atual
        DisableAllOfType<Barrel>();

        // Remove stats do barrel atual se existir
        if (currentBarrel != null)
        {
            RemoveBarrelStats(weaponProperties, currentBarrel);
        }

        // Ativa o novo barrel e adiciona stats
        currentBarrel = CreateBarrelData(b);
        AddBarrelStats(weaponProperties, currentBarrel);
        b.gameObject.SetActive(true);

        if (shouldSave)
            SaveAttachmentsToPlayerPrefs();

        //Debug.Log($"Barrel atualizado para: {currentBarrel.attachmentName}");
    }

    public void UpdateSight(Sight s, WeaponProperties weaponProperties, bool shouldSave = true)
    {
        if (s == null || weaponProperties == null) return;

        // Primeiro, desativa a sight atual
        DisableAllOfType<Sight>();

        // Remove stats da sight atual se existir
        if (currentSight != null)
        {
            RemoveSightStats(weaponProperties, currentSight);
        }

        // Ativa a nova sight e adiciona stats
        currentSight = CreateSightData(s);
        AddSightStats(weaponProperties, currentSight);
        s.gameObject.SetActive(true);

        if (shouldSave)
            SaveAttachmentsToPlayerPrefs();

        //Debug.Log($"Sight atualizada para: {currentSight.attachmentName}");
    }

    public void UpdateMag(Mag m, WeaponProperties weaponProperties, bool shouldSave = true)
    {
        if (weaponProperties == null) return;

        // Se o Mag for nulo, tenta pegar o primeiro disponível
        /*
        if (m == null)
        {
            m = GetFirstAvailableMag();
            if (m == null)
            {
                Debug.LogWarning("Nenhum Mag disponível encontrado!");
                return;
            }
        }
        */

        // Primeiro, desativa o mag atual
        DisableAllOfType<Mag>();

        // Remove stats do mag atual se existir
        if (currentMag != null)
        {
            RemoveMagStats(weaponProperties, currentMag);
        }

        // Ativa o novo mag e adiciona stats
        currentMag = CreateMagData(m);
        AddMagStats(weaponProperties, currentMag);
        m.gameObject.SetActive(true);

        if (shouldSave)
            SaveAttachmentsToPlayerPrefs();

    }

    public void UpdateSideGrip(SideGrip sg, WeaponProperties weaponProperties, bool shouldSave = true)
    {
        if (sg == null || weaponProperties == null) return;

        // Primeiro, desativa o side grip atual
        DisableAllOfType<SideGrip>();

        // Remove stats do side grip atual se existir
        if (currentSideGrip != null)
        {
            RemoveSideGripStats(weaponProperties, currentSideGrip);
        }

        // Ativa o novo side grip e adiciona stats
        currentSideGrip = CreateSideGripData(sg);
        AddSideGripStats(weaponProperties, currentSideGrip);
        sg.gameObject.SetActive(true);

        if (shouldSave)
            SaveAttachmentsToPlayerPrefs();

        //Debug.Log($"SideGrip atualizado para: {currentSideGrip.attachmentName}");
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
    }

    public void RemoveGrip(bool shouldSave = true)
    {
        if (currentGrip != null && weaponProperties != null)
        {
            Debug.Log($"Removendo grip: {currentGrip.attachmentName}");
            RemoveGripStats(weaponProperties, currentGrip);
            DisableAllOfType<Grip>();
            currentGrip = null;

            if (shouldSave)
                SaveAttachmentsToPlayerPrefs();

            //Debug.Log("Grip removido com sucesso");
        }
        else
        {
            Debug.LogWarning("Tentou remover grip mas nenhum grip está equipado");
        }
    }

    public void RemoveBarrel(bool shouldSave = true)
    {
        if (currentBarrel != null && weaponProperties != null)
        {
            Debug.Log($"Removendo barrel: {currentBarrel.attachmentName}");
            RemoveBarrelStats(weaponProperties, currentBarrel);
            DisableAllOfType<Barrel>();
            currentBarrel = null;

            if (shouldSave)
                SaveAttachmentsToPlayerPrefs();

            //Debug.Log("Barrel removido com sucesso");
        }
        else
        {
            Debug.LogWarning("Tentou remover barrel mas nenhum barrel está equipado");
        }
    }

    public void RemoveSight(bool shouldSave = true)
    {
        if (currentSight != null && weaponProperties != null)
        {
            Debug.Log($"Removendo sight: {currentSight.attachmentName}");
            RemoveSightStats(weaponProperties, currentSight);
            DisableAllOfType<Sight>();
            currentSight = null;

            if (shouldSave)
                SaveAttachmentsToPlayerPrefs();

            //Debug.Log("Sight removida com sucesso");
        }
        else
        {
            Debug.LogWarning("Tentou remover sight mas nenhuma sight está equipada");
        }
    }

    public void RemoveMag_(bool shouldSave = true)
    {
        if (currentMag != null && weaponProperties != null)
        {
            Debug.Log($"Removendo mag: {currentMag.attachmentName}");
            RemoveMagStats(weaponProperties, currentMag);
            DisableAllOfType<Mag>();
            currentMag = null;

            if (shouldSave)
                SaveAttachmentsToPlayerPrefs();

            //Debug.Log("Mag removido com sucesso");
        }
        else
        {
            Debug.LogWarning("Tentou remover mag mas nenhum mag está equipado");
        }
    }

    public void RemoveSideGrip(bool shouldSave = true)
    {
        if (currentSideGrip != null && weaponProperties != null)
        {
            Debug.Log($"Removendo side grip: {currentSideGrip.attachmentName}");
            RemoveSideGripStats(weaponProperties, currentSideGrip);
            DisableAllOfType<SideGrip>();
            currentSideGrip = null;

            if (shouldSave)
                SaveAttachmentsToPlayerPrefs();

            //Debug.Log("SideGrip removido com sucesso");
        }
        else
        {
            Debug.LogWarning("Tentou remover side grip mas nenhum side grip está equipado");
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

    // Adicione este método na região Helper Methods
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

        string json = JsonUtility.ToJson(saveData);
        //Debug.Log($"[{weaponName}] Salvando attachments: {json}");

        PlayerPrefs.SetString(saveKey, json);
        PlayerPrefs.Save();
    }

    public void LoadAttachmentsFromPlayerPrefs()
    {
        if (string.IsNullOrEmpty(weaponName) || weaponProperties == null) return;

        string saveKey = $"WeaponAttachments_{weaponName}";
        //Debug.Log($"[{weaponName}] Tentando carregar attachments de: {saveKey}");

        if (!PlayerPrefs.HasKey(saveKey))
        {
            Debug.Log($"[{weaponName}] Nenhum save encontrado - Criando save padrão com magazine inicial");

            // Cria um save com todos os campos nulos exceto o activeMag
            var defaultSaveData = new WeaponAttachmentSaveData(weaponName);

            // Encontra o primeiro magazine disponível
            Mag firstMag = GetFirstAvailableMag();
            if (firstMag != null)
            {
                defaultSaveData.activeMag = firstMag.gameObject.name;
                Debug.Log($"[{weaponName}] Magazine padrão definido: {defaultSaveData.activeMag}");
            }

            // Salva o JSON padrão
            string defaultJson = JsonUtility.ToJson(defaultSaveData);
            PlayerPrefs.SetString(saveKey, defaultJson);
            PlayerPrefs.Save();

            Debug.Log($"[{weaponName}] Save padrão criado: {defaultJson}");

            // Carrega o save que acabamos de criar (isso vai equipar o magazine)
            LoadAttachmentsFromPlayerPrefs();
            return;
        }

        string json = PlayerPrefs.GetString(saveKey);
        //Debug.Log($"[{weaponName}] JSON carregado: {json}");

        var saveData = JsonUtility.FromJson<WeaponAttachmentSaveData>(json);

        if (saveData == null)
        {
            Debug.LogError($"[{weaponName}] Falha ao desserializar save");
            return;
        }

        //Debug.Log($"[{weaponName}] Dados carregados: Sight={saveData.activeSight}, Barrel={saveData.activeBarrel}, Mag={saveData.activeMag}");

        // Carrega Sight
        if (!string.IsNullOrEmpty(saveData.activeSight))
        {
            Sight sight = FindAttachmentByName<Sight>(saveData.activeSight);
            if (sight != null)
            {
                //Debug.Log($"[{weaponName}] Carregando sight: {saveData.activeSight}");
                UpdateSight(sight, weaponProperties, false);
            }
        }

        // Carrega Barrel
        if (!string.IsNullOrEmpty(saveData.activeBarrel))
        {
            Barrel barrel = FindAttachmentByName<Barrel>(saveData.activeBarrel);
            if (barrel != null)
            {
                //Debug.Log($"[{weaponName}] Carregando barrel: {saveData.activeBarrel}");
                UpdateBarrel(barrel, weaponProperties, false);
            }
        }

        // Carrega Mag
        if (!string.IsNullOrEmpty(saveData.activeMag))
        {
            Mag mag = FindAttachmentByName<Mag>(saveData.activeMag);
            if (mag != null)
            {
                //Debug.Log($"[{weaponName}] Carregando mag: {saveData.activeMag}");
                UpdateMag(mag, weaponProperties, false);
            }
        }

        // Carrega Grip
        if (!string.IsNullOrEmpty(saveData.activeGrip))
        {
            Grip grip = FindAttachmentByName<Grip>(saveData.activeGrip);
            if (grip != null)
            {
                //Debug.Log($"[{weaponName}] Carregando grip: {saveData.activeGrip}");
                UpdateGrip(grip, weaponProperties, false);
            }
        }

        // Carrega SideGrip
        if (!string.IsNullOrEmpty(saveData.activeSideGrip))
        {
            SideGrip sideGrip = FindAttachmentByName<SideGrip>(saveData.activeSideGrip);
            if (sideGrip != null)
            {
                //Debug.Log($"[{weaponName}] Carregando side grip: {saveData.activeSideGrip}");
                UpdateSideGrip(sideGrip, weaponProperties, false);
            }
        }
    }

    // Método para compatibilidade com o sistema antigo
    public void LoadSavedAttachments(PlayerAttachmentsSaveData saveData)
    {
        if (saveData == null || weaponProperties == null) return;

        var weaponData = saveData.GetWeaponData(weaponName);
        if (weaponData == null) return;

        // Carrega Sight
        if (!string.IsNullOrEmpty(weaponData.activeSight))
        {
            Sight sight = FindAttachmentByName<Sight>(weaponData.activeSight);
            if (sight != null) UpdateSight(sight, weaponProperties, false);
        }

        // Carrega Barrel
        if (!string.IsNullOrEmpty(weaponData.activeBarrel))
        {
            Barrel barrel = FindAttachmentByName<Barrel>(weaponData.activeBarrel);
            if (barrel != null) UpdateBarrel(barrel, weaponProperties, false);
        }

        // Carrega Mag
        if (!string.IsNullOrEmpty(weaponData.activeMag))
        {
            Mag mag = FindAttachmentByName<Mag>(weaponData.activeMag);
            if (mag != null) UpdateMag(mag, weaponProperties, false);
        }

        // Carrega Grip
        if (!string.IsNullOrEmpty(weaponData.activeGrip))
        {
            Grip grip = FindAttachmentByName<Grip>(weaponData.activeGrip);
            if (grip != null) UpdateGrip(grip, weaponProperties, false);
        }

        // Carrega SideGrip
        if (!string.IsNullOrEmpty(weaponData.activeSideGrip))
        {
            SideGrip sideGrip = FindAttachmentByName<SideGrip>(weaponData.activeSideGrip);
            if (sideGrip != null) UpdateSideGrip(sideGrip, weaponProperties, false);
        }
    }

    #endregion

    #region Public Getters

    public AttachmentData GetCurrentGrip() => currentGrip;
    public AttachmentData GetCurrentBarrel() => currentBarrel;
    public AttachmentData GetCurrentSight() => currentSight;
    public AttachmentData GetCurrentMag() => currentMag;
    public AttachmentData GetCurrentSideGrip() => currentSideGrip;

    public bool HasGrip() => currentGrip != null;
    public bool HasBarrel() => currentBarrel != null;
    public bool HasSight() => currentSight != null;
    public bool HasMag() => currentMag != null;
    public bool HasSideGrip() => currentSideGrip != null;

    #endregion
}