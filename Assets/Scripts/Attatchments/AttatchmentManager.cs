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

    // Trackers dos attachments atuais
    [SerializeField] private AttachmentData currentGrip;
    [SerializeField] private AttachmentData currentBarrel;
    [SerializeField] private AttachmentData currentSight;
    [SerializeField] private AttachmentData currentMag;
    [SerializeField] private AttachmentData currentSideGrip;

    public void InitializeAttachments()
    {


        Grip[] grips = GetComponentsInChildren<Grip>();
        foreach (Grip grip in grips)
        {
            grip.left_hand_holder = left_hand_target;
        }

        // Carrega os attachments ativos
        LoadActiveAttachments();
    }

    private void LoadActiveAttachments()
    {
        // Procura por acessórios ativos nos filhos do objeto
        Grip activeGrip = GetComponentInChildren<Grip>(false); // false = apenas ativos
        Barrel activeBarrel = GetComponentInChildren<Barrel>(false);
        Sight activeSight = GetComponentInChildren<Sight>(false);
        Mag activeMag = GetComponentInChildren<Mag>(false);
        SideGrip activeSideGrip = GetComponentInChildren<SideGrip>(false);

        // Se encontrar attachments ativos, cria os dados correspondentes
        if (activeGrip != null)
        {
            currentGrip = CreateGripData(activeGrip);
        }

        if (activeBarrel != null)
        {
            currentBarrel = CreateBarrelData(activeBarrel);
        }

        if (activeSight != null)
        {
            currentSight = CreateSightData(activeSight);
        }

        if (activeMag != null)
        {
            currentMag = CreateMagData(activeMag);
        }

        if (activeSideGrip != null)
        {
            currentSideGrip = CreateSideGripData(activeSideGrip);
        }

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

    #region Update Methods

    public void UpdateGrip(Grip g, WeaponProperties weaponProperties)
    {
        // Primeiro, remove o grip atual se existir
        if (currentGrip != null)
        {
            RemoveGripStats(weaponProperties, currentGrip);
        }

        // Depois, adiciona o novo grip
        currentGrip = CreateGripData(g);
        AddGripStats(weaponProperties, currentGrip);

        Debug.Log($"Grip atualizado para: {currentGrip.attachmentName}");
    }

    public void UpdateBarrel(Barrel b, WeaponProperties weaponProperties)
    {
        // Remove o barrel atual se existir
        if (currentBarrel != null)
        {
            RemoveBarrelStats(weaponProperties, currentBarrel);
        }

        // Adiciona o novo barrel
        currentBarrel = CreateBarrelData(b);
        AddBarrelStats(weaponProperties, currentBarrel);

        Debug.Log($"Barrel atualizado para: {currentBarrel.attachmentName}");
    }

    public void UpdateSight(Sight s, WeaponProperties weaponProperties)
    {
        // Remove a sight atual se existir
        if (currentSight != null)
        {
            RemoveSightStats(weaponProperties, currentSight);
        }

        // Adiciona a nova sight
        currentSight = CreateSightData(s);
        AddSightStats(weaponProperties, currentSight);

        Debug.Log($"Sight atualizada para: {currentSight.attachmentName}");
    }

    public void UpdateMag(Mag m, WeaponProperties weaponProperties)
    {
        // Remove o mag atual se existir
        if (currentMag != null)
        {
            RemoveMagStats(weaponProperties, currentMag);
        }

        // Adiciona o novo mag
        currentMag = CreateMagData(m);
        AddMagStats(weaponProperties, currentMag);

        Debug.Log($"Mag atualizado para: {currentMag.attachmentName}");
    }

    public void UpdateSideGrip(SideGrip sg, WeaponProperties weaponProperties)
    {
        // Remove o side grip atual se existir
        if (currentSideGrip != null)
        {
            RemoveSideGripStats(weaponProperties, currentSideGrip);
        }

        // Adiciona o novo side grip
        currentSideGrip = CreateSideGripData(sg);
        AddSideGripStats(weaponProperties, currentSideGrip);

        Debug.Log($"SideGrip atualizado para: {currentSideGrip.attachmentName}");
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
            wp.horizontal_recoil[i] = Math.Clamp(wp.horizontal_recoil[i] + grip.horizontal_recoil_change, MIN_RECOIL_VALUE, MAX_RECOIL_VALUE);
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
            wp.horizontal_recoil[i] = Math.Clamp(wp.horizontal_recoil[i] + barrel.horizontal_recoil_change, MIN_RECOIL_VALUE, MAX_RECOIL_VALUE);
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
        wp.bullets_per_mag += mag.bullet_counter_change;
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
        for (int i = 0; i < wp.vertical_recoil.Length; i++)
        {
            wp.vertical_recoil[i] = Math.Clamp(wp.vertical_recoil[i] - grip.vertical_recoil_change, MIN_RECOIL_VALUE, MAX_RECOIL_VALUE);

        }

        for (int i = 0; i < wp.horizontal_recoil.Length; i++)
        {
            wp.horizontal_recoil[i] = Math.Clamp(wp.horizontal_recoil[i] - grip.horizontal_recoil_change, MIN_RECOIL_VALUE, MAX_RECOIL_VALUE);

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
        for (int i = 0; i < wp.vertical_recoil.Length; i++)
        {
            wp.vertical_recoil[i] = Math.Clamp(wp.vertical_recoil[i] - barrel.vertical_recoil_change, MIN_RECOIL_VALUE, MAX_RECOIL_VALUE);

        }

        for (int i = 0; i < wp.horizontal_recoil.Length; i++)
        {
            wp.horizontal_recoil[i] = Math.Clamp(wp.horizontal_recoil[i] - barrel.horizontal_recoil_change, MIN_RECOIL_VALUE, MAX_RECOIL_VALUE);

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
        wp.current_attachment_points -= sight.points;
        wp.zoom -= sight.zoom_change;
        wp.ads_speed -= sight.ads_speed_change;

        ResetAttachmentDataToZero(sight);
    }

    private void RemoveMagStats(WeaponProperties wp, AttachmentData mag)
    {

        if (wp != null)
        {
            wp.current_attachment_points -= mag.points;
            wp.bullets_per_mag -= mag.bullet_counter_change;
            wp.weapon_stability -= mag.stability_change;
            wp.ads_speed -= mag.ads_speed_change;
            wp.reload_time -= mag.reload_speed_changer;
        }


        ResetAttachmentDataToZero(mag);
    }

    private void RemoveSideGripStats(WeaponProperties wp, AttachmentData sideGrip)
    {
        wp.current_attachment_points -= sideGrip.points;

        ResetAttachmentDataToZero(sideGrip);
    }

    #endregion

    #region Remove Methods (Public)

    public void RemoveGrip(WeaponProperties weaponProperties)
    {
        if (currentGrip != null)
        {
            Debug.Log($"Removendo grip: {currentGrip.attachmentName}");
            RemoveGripStats(weaponProperties, currentGrip);
            Debug.Log("Grip removido com sucesso");
        }
        else
        {
            Debug.LogWarning("Tentou remover grip mas nenhum grip está equipado");
        }
    }

    public void RemoveBarrel(WeaponProperties weaponProperties)
    {
        if (currentBarrel != null)
        {
            Debug.Log($"Removendo barrel: {currentBarrel.attachmentName}");
            RemoveBarrelStats(weaponProperties, currentBarrel);
            Debug.Log("Barrel removido com sucesso");
        }
        else
        {
            Debug.LogWarning("Tentou remover barrel mas nenhum barrel está equipado");
        }

    }

    public void RemoveSight(WeaponProperties weaponProperties)
    {
        if (currentSight != null)
        {
            Debug.Log($"Removendo sight: {currentSight.attachmentName}");
            RemoveSightStats(weaponProperties, currentSight);
            Debug.Log("Sight removida com sucesso");
        }
        else
        {
            Debug.LogWarning("Tentou remover sight mas nenhuma sight está equipada");
        }

    }

    public void RemoveMag(WeaponProperties weaponProperties)
    {
        if (currentMag != null)
        {
            Debug.Log($"Removendo mag: {currentMag.attachmentName}");
            RemoveMagStats(weaponProperties, currentMag);
            Debug.Log("Mag removido com sucesso");
        }
        else
        {
            Debug.LogWarning("Tentou remover mag mas nenhum mag está equipado");
        }

    }

    public void RemoveSideGrip(WeaponProperties weaponProperties)
    {
        if (currentSideGrip != null)
        {
            Debug.Log($"Removendo side grip: {currentSideGrip.attachmentName}");
            RemoveSideGripStats(weaponProperties, currentSideGrip);
            Debug.Log("SideGrip removido com sucesso");
        }
        else
        {
            Debug.LogWarning("Tentou remover side grip mas nenhum side grip está equipado");
        }

    }

    #endregion
}