using UnityEngine;

public class GadgetComponents : MonoBehaviour
{
    [Header("Hands")]
    public GameObject left_hand;
    public GameObject right_hand;

    public Camera player_camera;

    [Header("HUD")]
    public SoldierHudManager soldierHudManager;

}
