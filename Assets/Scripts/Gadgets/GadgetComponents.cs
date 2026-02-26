using UnityEngine;

public class GadgetComponents : MonoBehaviour
{
    [Header("Hands")]
    public GameObject left_hand;
    public GameObject right_hand;

    [Header("Settings")]
    public KeyBinds keyBinds;
    public Camera player_camera;

    [Header("HUD")]
    public SoldierHudManager soldierHudManager;

    void Start()
    {
        keyBinds = GameObject.FindGameObjectWithTag("Settings").GetComponent<KeyBinds>();
    }
}
