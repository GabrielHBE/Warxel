using UnityEngine;

public class Settings : MonoBehaviour
{ 
    public static Settings Instance {get; private set;}

    public Audio _audio;
    public Controls _controls;
    public Gameplay _gameplay;
    public KeyBinds _keybinds;
    public Video _video;

    private void Awake()
    {
        Instance = this;
    }
    
}
