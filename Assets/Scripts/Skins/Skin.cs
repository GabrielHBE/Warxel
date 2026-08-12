using UnityEngine;

public class Skin : MonoBehaviour
{
    [Header("Settings")]
    public string skingName;
    public ClassManager.Class skinClass;
    public int battleCoinsToUnlock;
    public Sprite HudIcon;
    public Animator anim;

    [Header("Core")]
    public GameObject head;
    public GameObject torso;

    [Header("Left Arm")]
    public GameObject leftUpperArm;
    public GameObject leftLowerArm;
    public GameObject leftHand;

    [Header("Right Arm")]
    public GameObject rightUpperArm;
    public GameObject rightLowerArm;
    public GameObject rightHand;

    [Header("Left Leg")]
    public GameObject leftUpperLeg;
    public GameObject leftLowerLeg;
    public GameObject leftFoot;

    [Header("Right Leg")]
    public GameObject rightUpperLeg;
    public GameObject rightLowerLeg;
    public GameObject rightFoot;
}
