using UnityEngine;
using UnityEngine.InputSystem;

public class KeyBinds : MonoBehaviour
{

    public static KeyBinds Instance; // Singleton global

    private void Awake()
    {
        // Se já existe uma instância, destrói a nova
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Define como global
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }


    
    [Header("Player")]
    public KeyCode jumpKey;
    public KeyCode interactKey;
    public KeyCode sprintKey;
    public KeyCode crouchKey;
    public KeyCode proneKey;
    public KeyCode leanLeftKey;
    public KeyCode leanRightKey;


    [Header("Weapons")]
    public KeyCode shootKey;
    public KeyCode reloadKey;
    public KeyCode aimKey;
    public KeyCode switchFireModeKey;
    public KeyCode weapon1Key;
    public KeyCode weapon2Key;


    [Header("Gadget")]
    public KeyCode gadget1Key;
    public KeyCode gadget2Key;
    public KeyCode throwGrenadeKey;
    public KeyCode throwMedkitKey;
    public KeyCode useShieldKey;
    public KeyCode throwC4Key;
    public KeyCode detonateC4Key;


    [Header("Vehicle")]
    public KeyCode enterVehicleKey;
    public KeyCode freeLookKey;


    [Header("Jet")]
    public KeyCode boostKey;
    public KeyCode landingGearsActivator;
    public KeyCode shootMissileKey;
    public KeyCode shootMachineGunKey;
    public KeyCode shootBombKey;
    public KeyCode pitchUpKey;
    public KeyCode pitchDownKey;
    public KeyCode rollLeftKey;
    public KeyCode rollRightKey;
    public KeyCode yawLeftKey;
    public KeyCode yawRightKey;
    public KeyCode speedUpKey;
    public KeyCode speedDownKey;

}
