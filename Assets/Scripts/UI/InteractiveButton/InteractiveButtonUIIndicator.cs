using TMPro;
using UnityEngine;

public class InteractiveButtonUIIndicator : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI interactDescription;
    [SerializeField] private TextMeshProUGUI interactKey;

    void Start()
    {
        interactKey.text = Settings.Instance._keybinds.PLAYER_interactKey.ToString();
    }

    public void SetInteractiDesciption(string text)
    {
        interactDescription.text = text;
    }
    
}
