using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleLockInMissileController : MissileController
{
    [SerializeField] private string lock_in_tag;
    [SerializeField] private float lock_on_timer = 2;
    [SerializeField] private float lock_on_range = 200;
    [SerializeField] private AudioSource locking_missile_sound;
    [SerializeField] private AudioSource locked_missile_sound;

    private bool is_locking;
    private bool is_locked;
    private bool can_fire;
    private bool is_shooting = false;

    private float locking_timer = 0;

    private Vehicle vehicle;


    float raycast_radius = 1f;

    protected override void Start()
    {
        base.Start();
        InitializeMissiles<VehicleLockInMissile>();
    }

    protected override void Update()
    {
        base.Update();

        if (missiles.Count < spawnPoints.Count)
        {
            spawnInterval -= Time.deltaTime;

            if (spawnInterval <= 0)
            {
                ReloadMissiles<VehicleLockInMissile>();
                spawnInterval = 10f;
            }
        }

        if (!is_active) return;

        UpdateRocketsHUD();

        RaycastHit hit;

        if (Physics.SphereCast(transform.position, raycast_radius, transform.forward, out hit, lock_on_range, LayerMask.GetMask("Vehicle")))
        {
            if (hit.collider.gameObject.tag == lock_in_tag)
            {
                locking_timer += Time.deltaTime;

                is_locking = true;

                if (locking_timer <= lock_on_timer)
                {
                    vehicle = hit.collider.GetComponent<Vehicle>();
                    is_locked = true;
                    is_locking = false;
                }
                else
                {
                    vehicle = null;
                    is_locked = false;
                    can_fire = false;
                    is_locking = false;

                }

            }
            else
            {
                vehicle = null;
                is_locked = false;
                is_locking = false;
                can_fire = false;
                locking_timer = 0;
            }


        }
        else
        {
            is_locked = false;
            is_locking = false;
            vehicle = null;
            can_fire = false;
            locking_timer = 0;
        }


    }

    protected override bool CanShoot()
    {
        if (vehicle != null && current_missile != null && is_locked && !is_shooting)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public override void Shoot(KeyCode keyCode)
    {
        if (CanShoot() && Input.GetKeyDown(keyCode))
        {
            if (only_show_missiles_when_shoot) current_missile.GetComponent<MeshRenderer>().enabled = true;
            shoot_delay = original_shoot_delay;
            current_missile.Shoot();
            MoveToNextMissile();

            StartCoroutine(ShootDelay());
        }
    }


    private IEnumerator ShootDelay()
    {
        is_shooting = true;

        yield return new WaitForSeconds(shoot_delay);

        is_shooting = false;
    }

}
