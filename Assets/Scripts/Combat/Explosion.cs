using System.Collections.Generic;
using UnityEngine;
using VoxelDestructionPro.Data;
using VoxelDestructionPro.VoxelObjects;

public static class Explosion
{
    private const int MaxOverlapResults = 16384;
    private static Collider[] overlapResults = new Collider[256];

    private readonly struct VoxelImpactCandidate
    {
        public readonly DynamicVoxelObj Voxel;
        public readonly float DistanceSquared;

        public VoxelImpactCandidate(DynamicVoxelObj voxel, float distanceSquared)
        {
            Voxel = voxel;
            DistanceSquared = distanceSquared;
        }
    }

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
        int colliderCount = OverlapSphere(contactPoint, destructionRadius, Physics.AllLayers);

        var processedVehicles = new HashSet<ProcessVehicleDamage>();
        var processedPlayers = new HashSet<PlayerController>();
        var processedVoxels = new HashSet<DynamicVoxelObj>();
        var voxelCandidates = new List<VoxelImpactCandidate>();

        for (int i = 0; i < colliderCount; i++)
        {
            Collider collider = overlapResults[i];
            if (ShouldIgnoreCollider(collider, ignoreHitGameobject))
                continue;

            ProcessVehicleCollision(collider, contactPoint, shootRoot, destructionRadius,
                                    infantryDmg, vehicleDmg, damageFalloff,
                                    parentVehicle, processedVehicles);

            ProcessPlayerCollision(collider, contactPoint, shootRoot, destructionRadius,
                                   infantryDmg, damageFalloff, processedPlayers);

            CollectVoxelCollision(
                collider, contactPoint, destructionRadius, processedVoxels, voxelCandidates);
        }

        ProcessVoxelCandidates(voxelCandidates, contactPoint, destructionRadius);
    }

    public static void NoDamageSphereExplosion(
        Vector3 contactPoint,
        float infantryDmg,
        float destructionRadius,
        GameObject ignoreHitGameobject = null)
    {
        int voxelMask = LayerMask.GetMask("Voxel");
        int colliderCount = OverlapSphere(
            contactPoint,
            destructionRadius,
            voxelMask != 0 ? voxelMask : Physics.AllLayers);
        var processedVoxels = new HashSet<DynamicVoxelObj>();
        var voxelCandidates = new List<VoxelImpactCandidate>();

        for (int i = 0; i < colliderCount; i++)
        {
            Collider collider = overlapResults[i];
            if (ShouldIgnoreCollider(collider, ignoreHitGameobject))
                continue;

            CollectVoxelCollision(
                collider, contactPoint, destructionRadius, processedVoxels, voxelCandidates);
        }

        ProcessVoxelCandidates(voxelCandidates, contactPoint, destructionRadius);
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

    private static void CollectVoxelCollision(
        Collider collider,
        Vector3 hitPoint,
        float radius,
        HashSet<DynamicVoxelObj> processed,
        List<VoxelImpactCandidate> candidates)
    {
        if (!ProcessHit.TryGetVoxel(collider.gameObject, out DynamicVoxelObj voxel)) return;
        if (!processed.Add(voxel)) return;

        // Explosion radius is expressed in voxel cells by the destruction
        // system. Avoid submitting every building collider found by the wider
        // gameplay overlap when its voxel volume cannot intersect the damage.
        float worldVoxelRadius = radius * Mathf.Abs(voxel.GetSingleVoxelSize());
        Collider voxelCollider = voxel.targetCollider;
        float distanceSquared = voxelCollider != null
            ? voxelCollider.bounds.SqrDistance(hitPoint)
            : (voxel.transform.position - hitPoint).sqrMagnitude;
        if (distanceSquared > worldVoxelRadius * worldVoxelRadius)
            return;

        candidates.Add(new VoxelImpactCandidate(voxel, distanceSquared));
    }

    private static void ProcessVoxelCandidates(
        List<VoxelImpactCandidate> candidates,
        Vector3 hitPoint,
        float radius)
    {
        candidates.Sort((left, right) => left.DistanceSquared.CompareTo(right.DistanceSquared));
        for (int i = 0; i < candidates.Count; i++)
        {
            DynamicVoxelObj voxel = candidates[i].Voxel;
            if (voxel == null)
                continue;

            ProcessHit.VoxelHit(
                voxel,
                hitPoint,
                Vector3.up,
                radius,
                DestructionData.DestructionType.Sphere);
        }
    }

    private static int OverlapSphere(Vector3 center, float radius, int layerMask)
    {
        while (true)
        {
            int count = Physics.OverlapSphereNonAlloc(
                center, radius, overlapResults, layerMask, QueryTriggerInteraction.Collide);
            if (count < overlapResults.Length || overlapResults.Length >= MaxOverlapResults) return count;
            System.Array.Resize(ref overlapResults, Mathf.Min(overlapResults.Length * 2, MaxOverlapResults));
        }
    }

    private static bool ShouldUseVehicleDamage(GameObject parentVehicle, Collider collider) => parentVehicle != null && collider.gameObject != parentVehicle.gameObject;
    private static ProcessVehicleDamage GetVehicleComponent(Collider collider) => collider.gameObject.GetComponent<ProcessVehicleDamage>();
    private static bool ShouldIgnoreCollider(Collider collider, GameObject ignoreHitGameobject) => ignoreHitGameobject != null && collider.gameObject == ignoreHitGameobject;
}
