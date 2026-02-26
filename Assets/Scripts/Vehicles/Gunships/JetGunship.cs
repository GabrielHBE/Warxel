using UnityEngine;

public class JetGunship : Jet
{
    private bool is_pilot;
    [SerializeField] private Gunners[] gunners;

    protected override void Update()
    {

        speed = rb.linearVelocity.magnitude;
        if (is_in_vehicle)
        {
            StitchSeats();
        }

        if (is_pilot && !settings.is_menu_settings_active && is_in_vehicle)
        {
            UpdateHUD();

            if (start_engine)
            {
            }
            else
            {
                SlowDownEngine();
            }

        }
        else
        {
            SlowDownEngine();
        }

    }
    protected override void UpdateHUD()
    {
        base.UpdateHUD();

    }

    protected override void Shoot()
    {
        return;
    }
    private void StitchSeats()
    {
        
    }

    private class Gunners
    {
        public bool can_hold_trigger;
        public int gunner_id;
        public Camera camera;
        public AudioListener audioListener;
        public GameObject gunner_gameobject;
        public Transform ShootPos;
        public GameObject bulet_pref;
        public float bullet_size;
        public float spread;
        public float fire_rate;
        public float muzzle_velocity;
        public float bullet_drop;
        public float damage;
        public float min_damage;
        public float damage_dropoff;
        public float damage_dropoff_timer;
        public float destruction_radious;

    }
}
