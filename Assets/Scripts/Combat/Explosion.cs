using System.Collections.Generic;
using UnityEngine;
using VoxelDestructionPro.VoxelObjects;

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

        HashSet<Vehicle> processedVehicles = new HashSet<Vehicle>();
        HashSet<PlayerController> processedPlayers = new HashSet<PlayerController>();

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];

            if (ignoreHitGameobject != null && collider.gameObject == ignoreHitGameobject) continue;

            if (collider.gameObject.layer == LayerMask.NameToLayer("Vehicle"))
            {
                if (parentVehicle != null)
                {
                    if (collider.gameObject != parentVehicle.gameObject)
                    {
                        // Processar Vehicle
                        Vehicle vehicle = collider.gameObject.GetComponent<Vehicle>() ?? collider.gameObject.GetComponentInParent<Vehicle>();
                        if (vehicle != null && !processedVehicles.Contains(vehicle))
                        {
                            if (!vehicle.vehicle_destroyed.Value)
                            {
                                processedVehicles.Add(vehicle);
                                ProcessHit.VehicleHit(vehicle, collider, contactPoint, shootRoot, infantryDmg, damageFalloff, destructionRadius);
                            }

                        }
                    }
                }
                else
                {
                    // Processar Vehicle
                    Vehicle vehicle = collider.gameObject.GetComponent<Vehicle>() ?? collider.gameObject.GetComponentInParent<Vehicle>();
                    if (vehicle != null && !processedVehicles.Contains(vehicle))
                    {
                        if (!vehicle.vehicle_destroyed.Value)
                        {
                            processedVehicles.Add(vehicle);
                            //ProcessVehicleDamage(vehicle, collider, contact_point, vehicle_dmg);
                            ProcessHit.VehicleHit(vehicle, collider, contactPoint, shootRoot, vehicleDmg, damageFalloff, destructionRadius);
                        }
                    }
                }
            }

            // Processar Player
            if (collider.gameObject.layer == LayerMask.NameToLayer("PlayerHitBox"))
            {
                PlayerController player = collider.GetComponent<PlayerController>();
                if (player != null && !processedPlayers.Contains(player))
                {
                    processedPlayers.Add(player);
                    ProcessHit.PlayerHit(player, collider, contactPoint, shootRoot, infantryDmg, damageFalloff, destructionRadius);
                }
            }

            if (collider.gameObject.layer == LayerMask.NameToLayer("Voxel"))
            {
                // Processar Voxels
                DynamicVoxelObj vox = collider.GetComponentInParent<DynamicVoxelObj>();
                if (vox != null)
                {
                    vox.AddDestruction_Sphere(contactPoint, destructionRadius);
                }
            }
        }
    }
}