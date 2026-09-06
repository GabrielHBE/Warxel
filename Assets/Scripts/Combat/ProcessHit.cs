using System.Collections.Generic;
using UnityEngine;

public static class ProcessHit
{
    // Tornou-se público para ser acessado pela classe Explosion
    public static Dictionary<ProcessInfantryDamage.LimbMultiplier, float> limbMultiplierValues =
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

        ProcessInfantryDamage.LimbMultiplier limb = processInfantryDamage.GetLimbMultiplier();
        float limbMultiplier = limbMultiplierValues.TryGetValue(limb, out float multiplier) ? multiplier : 1f;
        bool isHeadShot = limb == ProcessInfantryDamage.LimbMultiplier.Head;

        float base_damage = damage * limbMultiplier;
        if (isHeadShot) base_damage *= hs_multiplier;

        float target_resistance = processInfantryDamage.GetResistance();
        float dano_real = base_damage * ((100f - target_resistance) / 100f);

        processInfantryDamage.Damage(dano_real);
        DamageMarker.Instance.UpdateDamage(dano_real);

        bool is_lethal_shot = (start_hp - dano_real) <= 0;
        if (is_lethal_shot) ProcessKill.ProcessInfantryKill(shoot_root, isHeadShot, processInfantryDamage.GetPlayerName());
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
    #endregion
}