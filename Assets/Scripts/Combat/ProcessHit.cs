using System.Collections.Generic;
using FishNet;
using UnityEngine;
using VoxelDestructionPro.Data;
using VoxelDestructionPro.VoxelModifications;
using VoxelDestructionPro.VoxelObjects;

public static class ProcessHit
{
    private static readonly HashSet<int> missingNetworkVoxelWarnings = new();

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

    #region Voxel
    public static bool TryGetVoxel(GameObject collisionGo, out DynamicVoxelObj voxel)
    {
        voxel = collisionGo != null
            ? collisionGo.GetComponentInParent<DynamicVoxelObj>()
            : null;

        return voxel != null;
    }

    public static bool VoxelHit(
        GameObject collisionGo,
        Vector3 hitPoint,
        Vector3 hitNormal,
        float destructionRadius,
        DestructionData.DestructionType destructionType = DestructionData.DestructionType.Sphere)
    {
        return TryGetVoxel(collisionGo, out DynamicVoxelObj voxel) &&
               VoxelHit(voxel, hitPoint, hitNormal, destructionRadius, destructionType);
    }

    public static bool VoxelHit(
        DynamicVoxelObj voxel,
        Vector3 hitPoint,
        Vector3 hitNormal,
        float destructionRadius,
        DestructionData.DestructionType destructionType = DestructionData.DestructionType.Sphere)
    {
        if (voxel == null) return false;

        NetworkVoxelDestruction networkDestruction = voxel.GetComponent<NetworkVoxelDestruction>();
        if (networkDestruction != null)
            return networkDestruction.RequestDestruction(
                hitPoint,
                hitNormal,
                destructionRadius,
                destructionType);

        // Runtime fragments are recreated locally from the server-provided voxel
        // indices and deterministic seed. They deliberately have no NetworkObject,
        // so they cannot issue another authoritative voxel-destruction request.
        // Mod_SmallFragment is part of both fragment prefabs and distinguishes
        // those transient debris objects from an incorrectly configured building.
        if (voxel.GetComponent<Mod_SmallFragment>() != null)
            return false;

        if (InstanceFinder.IsClientStarted || InstanceFinder.IsServerStarted)
        {
            int instanceId = voxel.GetInstanceID();
            if (missingNetworkVoxelWarnings.Add(instanceId))
            {
                Debug.LogError(
                    $"{voxel.name} has a DynamicVoxelObj but no NetworkVoxelDestruction. " +
                    "Networked destruction was blocked to preserve server authority.",
                    voxel);
            }

            return false;
        }

        return VoxelHitLocal(voxel, hitPoint, hitNormal, destructionRadius, destructionType, 0u);
    }

    internal static bool VoxelHitLocal(
        DynamicVoxelObj voxel,
        Vector3 hitPoint,
        Vector3 hitNormal,
        float destructionRadius,
        DestructionData.DestructionType destructionType,
        uint fragmentSeed)
    {
        if (voxel == null || !IsFinitePositive(destructionRadius))
            return false;

        float voxelSize = Mathf.Abs(voxel.GetSingleVoxelSize());
        if (!IsFinitePositive(voxelSize))
            return false;

        // Weapon destructionRadius is expressed directly in voxel units.
        float voxelRange = destructionRadius;
        float worldRange = voxelRange * voxelSize;

        Vector3 normal = hitNormal.sqrMagnitude > 0f
            ? hitNormal.normalized
            : Vector3.up;

        Vector3 endPoint = destructionType == DestructionData.DestructionType.Line
            ? hitPoint - normal * worldRange
            : Vector3.zero;

        var destruction = new DestructionData(
            destructionType,
            hitPoint,
            endPoint,
            voxelRange);

        return fragmentSeed == 0u
            ? voxel.AddDestruction(destruction)
            : voxel.AddAuthoritativeDestruction(destruction, fragmentSeed);
    }

    private static bool IsFinitePositive(float value) =>
        value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
    #endregion
}
