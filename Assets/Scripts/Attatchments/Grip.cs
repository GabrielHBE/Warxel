using UnityEngine;

public class Grip : Attatchment
{
    [Header("Changes")]
    [HideInInspector]public GameObject left_hand_holder;
    public GameObject grip_holder;
    [Range(-10,10)] public float vertical_recoil_change;
    [Range(-10,10)] public float horizontal_recoil_change;
    [Range(0,10)]  public float first_shoot_change;
    [Range(0,5)] public float weapon_stability_change;
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

}
