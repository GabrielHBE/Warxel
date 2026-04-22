using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    [Header("Button")]
    [SerializeField] private UnityEngine.UI.Button settings_button;
    [SerializeField] private UnityEngine.UI.Button account_button;
    [SerializeField] private UnityEngine.UI.Button weapon_armory_button;
    [SerializeField] private UnityEngine.UI.Button vehicle_armory_button;
    [SerializeField] private UnityEngine.UI.Button start_button_button;

    [Header("Parents")]
    [SerializeField] private GameObject account_parent;
    [SerializeField] private GameObject weapon_armory_parent;
    [SerializeField] private GameObject vehicle_armory_parent;
    [SerializeField] private GameObject start_button_parent;
    
}
