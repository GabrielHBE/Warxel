using UnityEngine;

public static class ProcessHit
{
    #region Player
    /// <summary>
    /// Direct hit
    /// </summary>
    public static void PlayerHit(GameObject collisionGo, float damage, float hs_multiplier, GameObject shoot_root)
    {
        bool hs_hit = false;
        PlayerController player = collisionGo.GetComponentInParent<PlayerController>();
        PlayerProperties playerProperties = player.GetComponent<PlayerProperties>();

        if (playerProperties.is_dead.Value) return;

        float start_hp = playerProperties.hp.Value;
        float base_damage;

        if (collisionGo.gameObject.CompareTag("PlayerHead"))
        {
            base_damage = damage * hs_multiplier;
            hs_hit = true;
        }
        else if (collisionGo.gameObject.CompareTag("Arms and Legs"))
        {
            base_damage = damage * 0.8f;
        }
        else if (collisionGo.gameObject.CompareTag("Feet and Hands"))
        {
            base_damage = damage * 0.7f;
        }
        else
        {
            base_damage = damage;
        }

        player.RequestDamage(base_damage);

        float target_resistance = player.GetResistance();
        float dano_real_esperado = base_damage * ((100f - target_resistance) / 100f);
        float post_hp = start_hp - dano_real_esperado;
        bool is_lethal_shot = post_hp <= 0;

        if (is_lethal_shot)
        {
            ProcessKill.ProcessInfantryKill(shoot_root, hs_hit, playerProperties.player_name.Value);
        }

        DamageMarker.Instance.UpdateDamage(dano_real_esperado);
    }

    /// <summary>
    /// Indirect hit from explosion
    /// </summary>
    public static void PlayerHit(PlayerController player, Collider collider, Vector3 contact_point, GameObject itemUsedToKill, float dmg, float damageFalloff, float destructionRadius)
    {
        PlayerProperties playerProperties = player.GetComponent<PlayerProperties>();

        Vector3 closestPoint = collider.gameObject.layer == LayerMask.NameToLayer("PlayerHitBox")
            ? collider.ClosestPoint(contact_point)
            : collider.transform.position;

        float distance = Vector3.Distance(contact_point, closestPoint);

        // Calcula a porcentagem da distância (1 = colado na explosão, 0 = no limite do destructionRadius)
        float distanceRatio = Mathf.Clamp01(1 - (distance / destructionRadius));

        // Aplica a variável de controle usando potência (Pow)
        float damageMultiplier = Mathf.Pow(distanceRatio, damageFalloff);

        float damage = dmg * damageMultiplier;
        player.RequestDamage(damage);

        float target_resistance = player.GetResistance();
        float final_actual_damage = damage * ((100f - target_resistance) / 100f);

        DamageMarker.Instance.UpdateDamage(final_actual_damage);

        CameraShake cameraShake = player.GetComponentInChildren<CameraShake>();

        if (cameraShake != null)
        {
            cameraShake.RequestShake(damage / 10, 1f);
        }

        if (playerProperties.is_dead.Value)
        {
            ProcessKill.ProcessInfantryKill(itemUsedToKill, false, playerProperties.player_name.Value);
        }
    }
    #endregion

    #region Vehicle
    /// <summary>
    /// Direct hit
    /// </summary>
    public static void VehicleHit(GameObject collisionGo, float damage, GameObject shoot_root)
    {
        Vehicle hit_vehicle = collisionGo.gameObject.GetComponent<Vehicle>() ?? collisionGo.gameObject.GetComponentInParent<Vehicle>();

        if (hit_vehicle != null)
        {
            string[] occupantNames = hit_vehicle.GetOccupantNames();

            if (!hit_vehicle.vehicle_destroyed.Value)
            {
                hit_vehicle.RequestDamage(damage);

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
    public static void VehicleHit(Vehicle vehicle, Collider collider, Vector3 contact_point, GameObject itemUsedToKill, float dmg, float destructionRadius, float damageFalloff)
    {
        Vector3 closestPoint = collider.gameObject.layer == LayerMask.NameToLayer("Vehicle")
            ? collider.ClosestPoint(contact_point)
            : collider.transform.position;

        float distance = Vector3.Distance(contact_point, closestPoint);

        float distanceRatio = Mathf.Clamp01(1 - (distance / destructionRadius));

        float damageMultiplier = Mathf.Pow(distanceRatio, damageFalloff);

        float damage = dmg * damageMultiplier;

        float target_resistance = vehicle.GetResistance();
        float final_actual_damage = damage * ((100f - target_resistance) / 100f);

        DamageMarker.Instance.UpdateDamage(final_actual_damage);

        vehicle.RequestDamage(damage);

        string[] occupantNames = vehicle.GetOccupantNames();

        if (vehicle.vehicle_destroyed.Value)
        {
            ProcessKill.ProcessVehicleKill(itemUsedToKill, occupantNames);
        }
    }
    #endregion
}