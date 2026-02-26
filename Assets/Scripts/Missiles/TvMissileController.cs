using UnityEngine;

public class TvMissileController : MissileController
{
    public Camera original_camera;
    protected override void Start()
    {
        base.Start();
        InitializeMissiles<TvMissile>();
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
                ReloadMissiles<TvMissile>();
                spawnInterval = original_spawn_interval;
            }
        }

        if (!is_active) return;

        UpdateRocketsHUD();

        if (current_missile != null)
        {
            //MoveToNextMissile();
        }
    }

    public override void Shoot(KeyCode keyCode)
    {
        if (CanShoot() && Input.GetKeyDown(keyCode))
        {
            if (only_show_missiles_when_shoot) current_missile.GetComponent<MeshRenderer>().enabled = true;
            original_camera.enabled = false;
            shoot_delay = original_shoot_delay;
            current_missile.GetComponent<TvMissile>().Shoot(original_camera);
            MoveToNextMissile();
        }
    }
}
