using System.Collections.Generic;
using UnityEngine;

public static class ProcessHit
{
    private static Dictionary<ProcessInfantryDamage.LimbMultiplier, float> limbMultiplierValues =
    new Dictionary<ProcessInfantryDamage.LimbMultiplier, float>
    {
        { ProcessInfantryDamage.LimbMultiplier.Torso, 1 },
        { ProcessInfantryDamage.LimbMultiplier.Leg, 0.8f },
        { ProcessInfantryDamage.LimbMultiplier.Arm, 0.8f },
        { ProcessInfantryDamage.LimbMultiplier.Hand, 0.7f },
        { ProcessInfantryDamage.LimbMultiplier.Foot, 0.7f }
    };

    #region Player
    /// <summary>
    /// Direct hit
    /// </summary>
    public static void PlayerHit(GameObject collisionGo, float damage, float hs_multiplier, GameObject shoot_root)
    {
        ProcessInfantryDamage processInfantryDamage = collisionGo.GetComponent<ProcessInfantryDamage>();
        if (processInfantryDamage == null || processInfantryDamage.IsPlayerDead()) return;

        float start_hp = processInfantryDamage.GetHP();

        // Multiplicador de membro e headshot
        ProcessInfantryDamage.LimbMultiplier limb = processInfantryDamage.GetLimbMultiplier();
        float limbMultiplier = limbMultiplierValues.TryGetValue(limb, out float multiplier) ? multiplier : 1f;
        bool isHeadShot = limb == ProcessInfantryDamage.LimbMultiplier.Head;

        float base_damage = damage * limbMultiplier;
        if (isHeadShot) base_damage *= hs_multiplier;

        // Cálculo ÚNICO do dano real considerando a resistência
        float target_resistance = processInfantryDamage.GetResistance();
        float dano_real = base_damage * ((100f - target_resistance) / 100f);

        // Aplica o dano real no jogador e atualiza a UI com o mesmo valor
        processInfantryDamage.Damage(dano_real);
        DamageMarker.Instance.UpdateDamage(dano_real);

        bool is_lethal_shot = (start_hp - dano_real) <= 0;
        if (is_lethal_shot) ProcessKill.ProcessInfantryKill(shoot_root, isHeadShot, processInfantryDamage.GetPlayerName());
    }

    /// <summary>
    /// Indirect hit from explosion
    /// </summary>
    public static void PlayerHit(PlayerController player, Collider collider, Vector3 contact_point, GameObject itemUsedToKill, float dmg, float damageFalloff, float destructionRadius)
    {
        PlayerProperties playerProperties = player.GetComponent<PlayerProperties>();
        if (playerProperties.is_dead.Value) return;

        ProcessInfantryDamage processInfantryDamage = player.GetComponent<ProcessInfantryDamage>();
        if (processInfantryDamage == null || processInfantryDamage.IsPlayerDead()) return;

        Vector3 closestPoint = collider.ClosestPoint(contact_point);
        float distance = Vector3.Distance(contact_point, closestPoint);
        float distanceRatio = Mathf.Clamp01(1 - (distance / destructionRadius));
        float damageMultiplier = Mathf.Pow(distanceRatio, damageFalloff);

        float baseDamage = dmg * damageMultiplier;

        ProcessInfantryDamage.LimbMultiplier limb = processInfantryDamage.GetLimbMultiplier();
        float limbMultiplier = limbMultiplierValues.TryGetValue(limb, out float multiplier) ? multiplier : 1f;
        bool isHeadShot = limb == ProcessInfantryDamage.LimbMultiplier.Head;

        float preResistanceDamage = baseDamage * limbMultiplier;

        // Cálculo ÚNICO do dano real considerando a resistência
        float target_resistance = processInfantryDamage.GetResistance();
        float dano_real = preResistanceDamage * ((100f - target_resistance) / 100f);

        processInfantryDamage.Damage(dano_real);
        DamageMarker.Instance.UpdateDamage(dano_real);

        CameraShake cameraShake = player.GetComponentInChildren<CameraShake>();
        if (cameraShake != null) cameraShake.RequestShake(preResistanceDamage / 10f, 1f);

        if (playerProperties.is_dead.Value) ProcessKill.ProcessInfantryKill(itemUsedToKill, isHeadShot, processInfantryDamage.GetPlayerName());
    }
    #endregion

    #region Vehicle
    /// <summary>
    /// Direct hit
    /// </summary>
    public static void VehicleHit(GameObject collisionGo, float damage, GameObject shoot_root)
    {
        ProcessVehicleDamage hit_vehicle = collisionGo.GetComponent<ProcessVehicleDamage>();

        if (hit_vehicle != null)
        {
            string[] occupantNames = hit_vehicle.GetOccupantNames();

            if (!hit_vehicle.IsVehicleDestroyed())
            {
                // Cálculo ÚNICO do dano real (GetResistance inclui resistência base + local)
                float target_resistance = hit_vehicle.GetResistance();
                float dano_real = damage * ((100f - target_resistance) / 100f);

                hit_vehicle.Damage(dano_real);
                DamageMarker.Instance.UpdateDamage(dano_real);
            }
            else
            {
                ProcessKill.ProcessVehicleKill(shoot_root, occupantNames);
            }
        }
    }

    /// <summary>
    /// Indirect hit from explosion
    /// </summary>
    public static void VehicleHit(ProcessVehicleDamage vehicle, Collider collider, Vector3 contact_point, GameObject itemUsedToKill, float dmg, float destructionRadius, float damageFalloff)
    {
        Vector3 closestPoint = collider.ClosestPoint(contact_point);
        float distance = Vector3.Distance(contact_point, closestPoint);
        float distanceRatio = Mathf.Clamp01(1 - (distance / destructionRadius));
        float damageMultiplier = Mathf.Pow(distanceRatio, damageFalloff);

        float baseDamage = dmg * damageMultiplier;

        // Cálculo ÚNICO do dano real
        float target_resistance = vehicle.GetResistance();
        float dano_real = baseDamage * ((100f - target_resistance) / 100f);

        vehicle.Damage(dano_real);
        DamageMarker.Instance.UpdateDamage(dano_real);

        string[] occupantNames = vehicle.GetOccupantNames();
        if (vehicle.IsVehicleDestroyed()) ProcessKill.ProcessVehicleKill(itemUsedToKill, occupantNames);
    }
    #endregion
}