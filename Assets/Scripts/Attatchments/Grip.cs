using UnityEngine;

public class Grip : MonoBehaviour
{
    [Header("Instances")]
    public string grip_name;
    public int grip_id;
    public GameObject left_hand_holder;
    public GameObject grip_holder;

    [Header("Changes")]
    public float vertical_recoil_change;
    public float horizontal_recoil_change;
    public float first_shoot_change;
    public float weapon_stability_change;
    public float reload_speed_change;
    public float ads_speed_change;
    public float pick_up_weapon_speed_change;


    void Update()
    {
        left_hand_holder.transform.position = grip_holder.transform.position;
        left_hand_holder.transform.rotation = grip_holder.transform.rotation;
    }

}
