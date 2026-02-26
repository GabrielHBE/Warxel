using Unity.VisualScripting;
using UnityEngine;

public class SkinManager : MonoBehaviour
{
    [Header("SKINS")]


    [Header("Feet")]
    [SerializeField] private GameObject left_foot;
    [SerializeField] private GameObject right_foot;

    [Header("Leg Shin")]
    [SerializeField] private GameObject left_leg_shin;
    [SerializeField] private GameObject right_leg_shin;

    [Header("Leg Thigh")]
    [SerializeField] private GameObject left_thigh_shin;
    [SerializeField] private GameObject right_thigh_shin;

    [Header("Torso")]
    [SerializeField] private GameObject torso;

    [Header("Hand")]
    [SerializeField] private GameObject left_hand;
    [SerializeField] private GameObject right_hand;

    [Header("Forearm")]
    [SerializeField] private GameObject left_forearm;
    [SerializeField] private GameObject right_forearm;

    [Header("Arm")]
    [SerializeField] private GameObject left_arm;
    [SerializeField] private GameObject right_arm;

    [Header("Head")]
    [SerializeField] private GameObject head;


    [Header("Parents")]


    [Header("Feet")]
    [SerializeField] private Transform left_foot_parent_transform;
    [SerializeField] private Transform right_foot_parent_transform;

    [Header("Leg Shin")]
    [SerializeField] private Transform left_leg_shin_parent_transform;
    [SerializeField] private Transform right_leg_shin_parent_transform;

    [Header("Leg Thigh")]
    [SerializeField] private Transform left_thigh_shin_parent_transform;
    [SerializeField] private Transform right_thigh_shin_parent_transform;

    [Header("Torso")]
    [SerializeField] private Transform torso_parent_transform;

    [Header("Hand")]
    [SerializeField] private Transform left_hand_parent_transform;
    [SerializeField] private Transform right_hand_parent_transform;

    [Header("Forearm")]
    [SerializeField] private Transform left_forearm_parent_transform;
    [SerializeField] private Transform right_forearm_parent_transform;

    [Header("Arm")]
    [SerializeField] private Transform left_arm_parent_transform;
    [SerializeField] private Transform right_arm_parent_transform;

    [Header("Head")]
    [SerializeField] private Transform head_parent_transform;


    void Start()
    {
        // Feet
        SetSkin(left_foot, left_foot_parent_transform);
        SetSkin(right_foot, right_foot_parent_transform);

        // Leg Shin
        SetSkin(left_leg_shin, left_leg_shin_parent_transform);
        SetSkin(right_leg_shin, right_leg_shin_parent_transform);

        // Leg Thigh
        SetSkin(left_thigh_shin, left_thigh_shin_parent_transform);
        SetSkin(right_thigh_shin, right_thigh_shin_parent_transform);

        // Torso
        SetSkin(torso, torso_parent_transform);

        // Hand
        SetSkin(left_hand, left_hand_parent_transform);
        SetSkin(right_hand, right_hand_parent_transform);

        // Forearm
        SetSkin(left_forearm, left_forearm_parent_transform);
        SetSkin(right_forearm, right_forearm_parent_transform);

        // Arm
        SetSkin(left_arm, left_arm_parent_transform);
        SetSkin(right_arm, right_arm_parent_transform);

        // Head
        SetSkin(head, head_parent_transform);
    }


    private void SetSkin(GameObject skin, Transform parent)
    {
        if (skin == null || parent == null) return;

        skin.transform.SetParent(parent);
        skin.transform.localPosition = Vector3.zero;
        skin.transform.localRotation = Quaternion.identity;
        skin.transform.localScale = Vector3.one;
    }


}
