public class Missile : Projectile
{
    public override void CreateProjectile(ProjectileProperties prop, ProjectileValues values)
    {

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
        if (isDespawning) return;
        ProcessDamageDropoff();
    }

}