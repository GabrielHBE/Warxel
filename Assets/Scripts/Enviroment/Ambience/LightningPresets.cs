using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "LightningPreset", menuName = "Scriptables/Lightning Preset", order = 1)]
public class LightningPreset : ScriptableObject
{
    public Gradient ambient_color;
    public Gradient directional_color;
    public Gradient fog_color;
    

}
