public class Missile : Projectile
{
    public override void CreateProjectile(ProjectileProperties prop, ProjectileValues values)
    {

        Activate();

        SetProjectileValues(values);
        SetProjectileProperties(prop);
        SetDirection(prop.direction, values.muzzleVelocity);

        SetVisualsActive(true);

        StopAllCoroutines();
        StartCoroutine(DespawnTimer());

        isSetup = true;
    }
}
