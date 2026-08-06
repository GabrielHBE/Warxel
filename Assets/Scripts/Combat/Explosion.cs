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

    private static bool ShouldIgnoreCollider(Collider collider, GameObject ignoreHitGameobject)
    {
        return ignoreHitGameobject != null && collider.gameObject == ignoreHitGameobject;
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

        float damage = ShouldUseVehicleDamage(parentVehicle, collider) ? vehicleDmg : infantryDmg;
        ProcessHit.VehicleHit(vehicle, collider, contactPoint, shootRoot, damage, damageFalloff, destructionRadius);
    }

    private static bool ShouldUseVehicleDamage(GameObject parentVehicle, Collider collider)
    {
        return parentVehicle != null && collider.gameObject != parentVehicle.gameObject;
    }

    private static ProcessVehicleDamage GetVehicleComponent(Collider collider)
    {
        return collider.gameObject.GetComponent<ProcessVehicleDamage>();
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

        processedPlayers.Add(player);
        ProcessHit.PlayerHit(player, collider, contactPoint, shootRoot, infantryDmg, damageFalloff, destructionRadius);
    }

    private static void ProcessVoxelCollision(
        Collider collider,
        float infantryDmg)
    {
        if (collider.gameObject.layer != LayerMask.NameToLayer("Voxel"))
            return;

        TryApplyVoxelPartialCollapseDamage(collider, infantryDmg);
    }



    private static void TryApplyVoxelPartialCollapseDamage(Collider collider, float infantryDmg)
    {
        VoxelPartialCollapse collapse = collider.GetComponent<VoxelPartialCollapse>();
        if (collapse != null)
            collapse.Damage(infantryDmg / 2);
    }
}