using UnityEngine;

public class Mag : MonoBehaviour
{

    public int bullet_counter_change;
    public float stability_change;
    public float ads_speed_change;
    public float reload_speed_changer;
    public bool is_tape_mag;
    bool did_reload=false;
    private WeaponProperties weaponProperties;
    private Weapon weapon;

    float original_reload_time;

    void Start()
    {

        weaponProperties = GetComponentInParent<WeaponProperties>();
        weapon = GetComponentInParent<Weapon>();

        original_reload_time = weaponProperties.reload_time;

    }

    /*
    void Update()
    {

        if (!is_tape_mag)
        {
            return;
        }

        if (Input.GetKeyDown(weapon.reload_key))
        {
            did_reload = !did_reload;

            if (!did_reload)
            {
                weaponProperties.reload_time = original_reload_time;
            }
            else
            {
                weaponProperties.reload_time -= reload_speed_changer;
            }
        }

    }
    */

}
