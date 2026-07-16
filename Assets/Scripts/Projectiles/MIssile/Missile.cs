using UnityEngine;

public class Missile : Projectile
{
    private bool hasExploded = false;

    public override void CreateProjectile(ProjectileProperties prop, ProjectileValues values)
    {
        hasExploded = false;
        SetVisualsActive(true);
        Activate();

        SetProjectileValues(values);
        SetProjectileProperties(prop);
        SetDirection(prop.direction, values.muzzleVelocity);

        StopAllCoroutines();
        StartCoroutine(DespawnTimer());

    }

    public override void LocalUpdate()
    {
        if (isDespawning || hasExploded) return;
        ProcessDamageDropoff();
    }

}