using UnityEngine;

public class MainMenuManager : MonoBehaviour
{

    [Header("Parents")]
    [SerializeField] private GameObject master_parent;
    [SerializeField] private GameObject account_parent;
    [SerializeField] private GameObject weapon_armory_parent;
    [SerializeField] private GameObject vehicle_armory_parent;
    [SerializeField] private GameObject start_game_parent;

    private enum CurrentButton
    {
        StartGame,
        Account,
        WeaponArmory,
        VehicleArmory
    }

    private CurrentButton currentButton;

    void Start()
    {
        SelectStartGameButton();
    }

    void Update()
    {
        if (SettingsHUD.Instance == null) return;
        
        if (SettingsHUD.Instance.is_menu_settings_active)
        {
            if (master_parent.activeSelf) master_parent.SetActive(false);
        }
        else
        {
            if (!master_parent.activeSelf) master_parent.SetActive(true);
        }
    }

    public void SelectAccountButton()
    {
        currentButton = CurrentButton.Account;
        ActivateParent(currentButton);
    }

    public void SettingsButton()
    {
        SettingsHUD.Instance.ToggleSettingsMenu();
    }

    public void SelectStartGameButton()
    {
        currentButton = CurrentButton.StartGame;
        ActivateParent(currentButton);
    }
    public void SelectWeaponArmoryButton()
    {
        currentButton = CurrentButton.WeaponArmory;
        ActivateParent(currentButton);
    }

    public void SelectVehicleArmoryButton()
    {
        currentButton = CurrentButton.VehicleArmory;
        ActivateParent(currentButton);
    }

    private void ActivateParent(CurrentButton currentButton)
    {
        switch (currentButton)
        {
            case CurrentButton.StartGame:
                start_game_parent.SetActive(true);
                account_parent.SetActive(false);
                weapon_armory_parent.SetActive(false);
                vehicle_armory_parent.SetActive(false);
                break;

            case CurrentButton.Account:
                start_game_parent.SetActive(false);
                account_parent.SetActive(true);
                weapon_armory_parent.SetActive(false);
                vehicle_armory_parent.SetActive(false);
                account_parent.GetComponent<AccountMainMenu>().Activate();
                break;

            case CurrentButton.WeaponArmory:
                start_game_parent.SetActive(false);
                account_parent.SetActive(false);
                weapon_armory_parent.SetActive(true);
                vehicle_armory_parent.SetActive(false);
                break;

            case CurrentButton.VehicleArmory:
                start_game_parent.SetActive(false);
                account_parent.SetActive(false);
                weapon_armory_parent.SetActive(false);
                vehicle_armory_parent.SetActive(true);
                break;

            default:
                start_game_parent.SetActive(false);
                account_parent.SetActive(false);
                weapon_armory_parent.SetActive(false);
                vehicle_armory_parent.SetActive(false);
                break;
        }
    }

}
