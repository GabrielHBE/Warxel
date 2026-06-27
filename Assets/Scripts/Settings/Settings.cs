public class Settings : PersistentLocalSingleton<Settings>
{ 
    //public static Settings Instance {get; private set;}

    public Audio _audio;
    public Controls _controls;
    public Gameplay _gameplay;
    public KeyBinds _keybinds;
    public Video _video;

    
}
