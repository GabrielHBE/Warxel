using UnityEngine;

public class TowMissileController : MissileController
{
    public Transform camera_transform;
    protected override void Start()
    {
        base.Start();
        InitializeMissiles<TowMissile>();
        current_missile = missiles[current_missile_index];
    }

    protected override void Update()
    {
        base.Update();
        

        if (missiles.Count == 0 && can_reload_missiles)
        {
            spawnInterval -= Time.deltaTime;

            if (spawnInterval <= 0)
            {
                ReloadMissiles<TowMissile>();
                spawnInterval = original_spawn_interval;
            }
        }

        if (!is_active) return;

        UpdateRocketsHUD();
    }

    public override void Shoot(KeyCode keyCode)
    {
        if (CanShoot() && Input.GetKeyDown(keyCode))
        {
            if (only_show_missiles_when_shoot) current_missile.GetComponent<MeshRenderer>().enabled = true;
            shoot_delay = original_shoot_delay;
            current_missile.GetComponent<TowMissile>().Shoot(camera_transform);
            MoveToNextMissile();
        }
    }
}
