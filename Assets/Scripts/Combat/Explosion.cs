using System.Collections.Generic;
using UnityEngine;

public static class Explosion
{
    public static void SphereExplosion(
        Vector3 contactPoint,
        float infantryDmg,
        float vehicleDmg,
        float destructionRadius,
        float damageFalloff,
        GameObject parentVehicle,
        GameObject shootRoot,
        GameObject ignoreHitGameobject = null)
    {
        Collider[] colliders = Physics.OverlapSphere(contactPoint, destructionRadius);

        var processedVehicles = new HashSet<ProcessVehicleDamage>();
        var processedPlayers = new HashSet<PlayerController>();

        foreach (Collider collider in colliders)
        {
            if (ShouldIgnoreCollider(collider, ignoreHitGameobject))
                continue;

            ProcessVehicleCollision(collider, contactPoint, shootRoot, destructionRadius,
                                    infantryDmg, vehicleDmg, damageFalloff,
                                    parentVehicle, processedVehicles);

            ProcessPlayerCollision(collider, contactPoint, shootRoot, destructionRadius,
                                   infantryDmg, damageFalloff, processedPlayers);

            ProcessVoxelCollision(collider, infantryDmg);
        }
    }

    public static void NoDamageSphereExplosion(
        Vector3 contactPoint,
        float infantryDmg,
        float destructionRadius,
        GameObject ignoreHitGameobject = null)
    {
        Collider[] colliders = Physics.OverlapSphere(contactPoint, destructionRadius);

        foreach (Collider collider in colliders)
        {
            if (ShouldIgnoreCollider(collider, ignoreHitGameobject))
                continue;

            ProcessVoxelCollision(collider, infantryDmg);
        }
    }

    private static void ProcessVehicleCollision(
        Collider collider,
        Vector3 contactPoint,
        GameObject shootRoot,
        float destructionRadius,
        float infantryDmg,
        float vehicleDmg,
        float damageFalloff,
        GameObject parentVehicle,
        HashSet<ProcessVehicleDamage> processedVehicles)
    {
        if (collider.gameObject.layer != LayerMask.NameToLayer("Vehicle"))
            return;

        ProcessVehicleDamage vehicle = GetVehicleComponent(collider);
        if (vehicle == null || processedVehicles.Contains(vehicle) || vehicle.IsVehicleDestroyed())
            return;

        processedVehicles.Add(vehicle);

        float dmg = ShouldUseVehicleDamage(parentVehicle, collider) ? vehicleDmg : infantryDmg;

        // Cálculo de Falloff transferido do ProcessHit
        Vector3 closestPoint = collider.ClosestPoint(contactPoint);
        float distance = Vector3.Distance(contactPoint, closestPoint);
        float distanceRatio = Mathf.Clamp01(1 - (distance / destructionRadius));
        float damageMultiplier = Mathf.Pow(distanceRatio, damageFalloff);
        float baseDamage = dmg * damageMultiplier;

        float target_resistance = vehicle.GetResistance();
        float dano_real = baseDamage * ((100f - target_resistance) / 100f);

        vehicle.Damage(dano_real);
        DamageMarker.Instance.UpdateDamage(dano_real);

        string[] occupantNames = vehicle.GetOccupantNames();
        if (vehicle.IsVehicleDestroyed()) ProcessKill.ProcessVehicleKill(shootRoot, occupantNames);
    }

    private static void ProcessPlayerCollision(
        Collider collider,
        Vector3 contactPoint,
        GameObject shootRoot,
        float destructionRadius,
        float infantryDmg,
        float damageFalloff,
        HashSet<PlayerController> processedPlayers)
    {
        if (collider.gameObject.layer != LayerMask.NameToLayer("PlayerHitBox"))
            return;

        PlayerController player = collider.GetComponent<PlayerController>();
        if (player == null || processedPlayers.Contains(player))
            return;

        PlayerProperties playerProperties = player.GetComponent<PlayerProperties>();
        if (playerProperties != null && playerProperties.isDead.Value) return;

        ProcessInfantryDamage processInfantryDamage = player.GetComponent<ProcessInfantryDamage>();
        if (processInfantryDamage == null || processInfantryDamage.IsPlayerDead()) return;

        processedPlayers.Add(player);

        // Cálculo de Falloff transferido do ProcessHit
        Vector3 closestPoint = collider.ClosestPoint(contactPoint);
        float distance = Vector3.Distance(contactPoint, closestPoint);
        float distanceRatio = Mathf.Clamp01(1 - (distance / destructionRadius));
        float damageMultiplier = Mathf.Pow(distanceRatio, damageFalloff);
        float baseDamage = infantryDmg * damageMultiplier;

        ProcessInfantryDamage.LimbMultiplier limb = processInfantryDamage.GetLimbMultiplier();
        float limbMultiplier = ProcessHit.limbMultiplierValues.TryGetValue(limb, out float multiplier) ? multiplier : 1f;
        bool isHeadShot = limb == ProcessInfantryDamage.LimbMultiplier.Head;

        float preResistanceDamage = baseDamage * limbMultiplier;
        float target_resistance = processInfantryDamage.GetResistance();
        float dano_real = preResistanceDamage * ((100f - target_resistance) / 100f);

        processInfantryDamage.Damage(dano_real);
        DamageMarker.Instance.UpdateDamage(dano_real);

        CameraShake cameraShake = player.GetComponentInChildren<CameraShake>();
        if (cameraShake != null) cameraShake.RequestShake(preResistanceDamage / 10f, 1f);

        if (playerProperties != null && playerProperties.isDead.Value) 
            ProcessKill.ProcessInfantryKill(shootRoot, isHeadShot, processInfantryDamage.GetPlayerName());
    }

    private static void ProcessVoxelCollision(Collider collider, float dmg)
    {
        if (!VoxelObj.IsVoxelLayer(collider.gameObject.layer)) return;
        TryApplyVoxelPartialCollapseDamage(collider, dmg);
    }

    private static void TryApplyVoxelPartialCollapseDamage(Collider collider, float dmg) => collider.GetComponent<VoxelDestruction>()?.TakeDamage(dmg / 2);
    private static bool ShouldUseVehicleDamage(GameObject parentVehicle, Collider collider) => parentVehicle != null && collider.gameObject != parentVehicle.gameObject;
    private static ProcessVehicleDamage GetVehicleComponent(Collider collider) => collider.gameObject.GetComponent<ProcessVehicleDamage>();
    private static bool ShouldIgnoreCollider(Collider collider, GameObject ignoreHitGameobject) => ignoreHitGameobject != null && collider.gameObject == ignoreHitGameobject;
}
