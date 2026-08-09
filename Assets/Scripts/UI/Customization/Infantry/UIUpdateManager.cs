using System.Linq;
using TMPro;
using UnityEngine;

public class UIUpdateManager : MonoBehaviour
{
    private InfantryLoadoutCustomization infantryLoadoutCustomization;

    [Header("Camera")]
    public Camera switchLoadoutCamera;

    [Header(" Stats Display")]
    [SerializeField] public TextMeshProUGUI itemStatusText;

    public void Initialize(InfantryLoadoutCustomization parent)
    {
        if (switchLoadoutCamera != null) switchLoadoutCamera.enabled = false;
        infantryLoadoutCustomization = parent;
    }

    public void UpdateUI()
    {
        UpdateBattleCoins();
        UpdateWeaponStatusDisplay();
        UpdateCameraAndButtonVisibility();
        UpdateClassParentVisibility();
    }

    private void UpdateBattleCoins()
    {
        if (infantryLoadoutCustomization.current_battle_coins_indicator != null) infantryLoadoutCustomization.current_battle_coins_indicator.text = "Current Battle coins: " + AccountManager.Instance.battle_coins;
    }

    private void UpdateWeaponStatusDisplay()
    {
        bool isWeaponSelected = infantryLoadoutCustomization._currentItemSelected != null;

        if (isWeaponSelected)
        {
            infantryLoadoutCustomization.weaponStatusParent.SetActive(true);
            bool showCustomizeButton = infantryLoadoutCustomization.GetCurrentStage() == InfantryLoadoutCustomization.SelectionStage.ItemSelection &&
                                      IsPrimaryOrSecondaryWeapon();
            infantryLoadoutCustomization.customizeWeaponButton.SetActive(showCustomizeButton);
        }
        else
        {
            infantryLoadoutCustomization.weaponStatusParent.SetActive(false);
            infantryLoadoutCustomization.customizeWeaponButton.SetActive(false);
            infantryLoadoutCustomization.customization_buttons_parent.SetActive(false);
        }
    }

    private bool IsPrimaryOrSecondaryWeapon()
    {
        var currentOption = infantryLoadoutCustomization.loadoutOptionManager.GetCurrentOption();

        return currentOption == LoadoutOptionManager.LoadoutOption.PrimaryWeapon || currentOption == LoadoutOptionManager.LoadoutOption.SecondaryWeapon;
    }

    private void UpdateCameraAndButtonVisibility()
    {
        if (infantryLoadoutCustomization.GetCurrentStage() == InfantryLoadoutCustomization.SelectionStage.ClassSelection)
        {
            if (switchLoadoutCamera != null)
                switchLoadoutCamera.enabled = false;
            infantryLoadoutCustomization.updateLoadoutButton.SetActive(true);
        }
        else
        {
            if (switchLoadoutCamera != null)
                switchLoadoutCamera.enabled = true;
            infantryLoadoutCustomization.updateLoadoutButton.SetActive(false);
        }
    }

    private void UpdateClassParentVisibility() => infantryLoadoutCustomization.classesParent.gameObject.SetActive(infantryLoadoutCustomization.GetCurrentStage() == InfantryLoadoutCustomization.SelectionStage.ClassSelection);
    public void UpdateItemStatusText(string text) => itemStatusText.text = text;

}