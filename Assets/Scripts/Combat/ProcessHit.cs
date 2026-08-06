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

        if (processInfantryDamage.IsPlayerDead()) return;

        float start_hp = processInfantryDamage.GetHP();

        // Obtém o multiplicador do membro usando o dicionário
        ProcessInfantryDamage.LimbMultiplier limb = processInfantryDamage.GetLimbMultiplier();
        float limbMultiplier = limbMultiplierValues.TryGetValue(limb, out float multiplier) ? multiplier : 1;

        // Verifica se é headshot (opcional, se quiser diferenciar)
        bool isHeadShot = limb == ProcessInfantryDamage.LimbMultiplier.Head;

        // Aplica o multiplicador do membro E o multiplicador de headshot (se for cabeça)
        float base_damage = damage * limbMultiplier;
        if (isHeadShot) base_damage *= hs_multiplier; // Aplica o multiplicador extra de headshot


        processInfantryDamage.Damage(base_damage);

        float target_resistance = processInfantryDamage.GetResistance();
        float dano_real_esperado = base_damage * ((100f - target_resistance) / 100f);
        float post_hp = start_hp - dano_real_esperado;
        bool is_lethal_shot = post_hp <= 0;

        if (is_lethal_shot)
            ProcessKill.ProcessInfantryKill(shoot_root, isHeadShot, processInfantryDamage.GetPlayerName());

        DamageMarker.Instance.UpdateDamage(dano_real_esperado);
    }

    /// <summary>
    /// Indirect hit from explosion
    /// </summary>
    public static void PlayerHit(PlayerController player, Collider collider, Vector3 contact_point, GameObject itemUsedToKill, float dmg, float damageFalloff, float destructionRadius)
    {
        PlayerProperties playerProperties = player.GetComponent<PlayerProperties>();
        
        if (playerProperties.is_dead.Value) return;

        ProcessInfantryDamage processInfantryDamage = player.GetComponent<ProcessInfantryDamage>();
        
        if (processInfantryDamage.IsPlayerDead()) return;

        Vector3 closestPoint = collider.ClosestPoint(contact_point);

        float distance = Vector3.Distance(contact_point, closestPoint);

        // Calcula a porcentagem da distância (1 = colado na explosão, 0 = no limite do destructionRadius)
        float distanceRatio = Mathf.Clamp01(1 - (distance / destructionRadius));

        // Aplica a variável de controle usando potência (Pow)
        float damageMultiplier = Mathf.Pow(distanceRatio, damageFalloff);

        float baseDamage = dmg * damageMultiplier;

        // Obtém o multiplicador do membro usando o dicionário
        ProcessInfantryDamage.LimbMultiplier limb = processInfantryDamage.GetLimbMultiplier();
        float limbMultiplier = limbMultiplierValues.TryGetValue(limb, out float multiplier) ? multiplier : 1;

        // Verifica se é headshot (explosões geralmente não fazem headshot, mas mantemos para consistência)
        bool isHeadShot = limb == ProcessInfantryDamage.LimbMultiplier.Head;

        // Aplica o multiplicador do membro (explosões não têm multiplicador de headshot extra)
        float finalDamage = baseDamage * limbMultiplier;
        
        // Aplica o dano ao jogador
        processInfantryDamage.Damage(finalDamage);

        float target_resistance = processInfantryDamage.GetResistance();
        float final_actual_damage = finalDamage * ((100f - target_resistance) / 100f);

        DamageMarker.Instance.UpdateDamage(final_actual_damage);

        CameraShake cameraShake = player.GetComponentInChildren<CameraShake>();

        if (cameraShake != null) cameraShake.RequestShake(finalDamage / 10, 1f);

        // Verifica se o jogador morreu após o dano
        if (playerProperties.is_dead.Value) 
        {
            ProcessKill.ProcessInfantryKill(itemUsedToKill, isHeadShot, playerProperties.player_name.Value);
        }
    }
    #endregion

    #region Vehicle
    /// <summary>
    /// Direct hit
    /// </summary>
    public static void VehicleHit(GameObject collisionGo, float damage, GameObject shoot_root)
    {
        ProcessVehicleDamage hit_vehicle = collisionGo.gameObject.GetComponent<ProcessVehicleDamage>();

        if (hit_vehicle != null)
        {
            string[] occupantNames = hit_vehicle.GetOccupantNames();

            if (!hit_vehicle.IsVehicleDestroyed())
            {
                hit_vehicle.Damage(damage);

                float target_resistance = hit_vehicle.GetResistance();
                float final_actual_damage = damage * ((100f - target_resistance) / 100f);

                DamageMarker.Instance.UpdateDamage(final_actual_damage);
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

        float damage = dmg * damageMultiplier;

        float target_resistance = vehicle.GetResistance();
        float final_actual_damage = damage * ((100f - target_resistance) / 100f);

        DamageMarker.Instance.UpdateDamage(final_actual_damage);

        vehicle.Damage(damage);

        string[] occupantNames = vehicle.GetOccupantNames();

        if (vehicle.IsVehicleDestroyed()) ProcessKill.ProcessVehicleKill(itemUsedToKill, occupantNames);
        
    }
    #endregion
}