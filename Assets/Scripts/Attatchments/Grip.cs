using UnityEngine;

public class Grip : Attatchment
{
    [Header("Changes")]
    [HideInInspector] public GameObject left_hand_holder;
    public GameObject grip_holder;
    [Range(Recoil.MIN_RECOIL_VALUE, Recoil.MAX_RECOIL_VALUE)] public float vertical_recoil_change;
    [Range(Recoil.MIN_RECOIL_VALUE, Recoil.MAX_RECOIL_VALUE)] public float horizontal_recoil_change;
    [Range(Recoil.MIN_FIRTSHOTINCREASER_VALUE, Recoil.MIN_FIRTSHOTINCREASER_VALUE)] public float first_shoot_change;
    public float reload_speed_change;
    public float ads_speed_change;
    public float pick_up_weapon_speed_change;
    public float store_weapon_speed_change;

    void Update()
    {
        if (left_hand_holder != null)
        {
            left_hand_holder.transform.position = grip_holder.transform.position;
            left_hand_holder.transform.rotation = grip_holder.transform.rotation;
        }

    }

    public override string GetAttatchmentDescription()
    {
        throw new System.NotImplementedException();
    }

}
