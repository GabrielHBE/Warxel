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

    /*
    public void UpdateWeaponStats(WeaponProperties wp)
    {
        if (wp == null) return;

        infantryLoadoutCustomization.rateOfFireText.text = wp.firing.rateOfFire.ToString("F0") + " RPM";
        infantryLoadoutCustomization.adsSpeedText.text = wp.ads_speed.ToString("F2") + "s";
        infantryLoadoutCustomization.playerSpeedModifierText.text = wp.speed_change.ToString("F0");
        infantryLoadoutCustomization.zoomText.text = "x" + wp.zoom.ToString("F1");
        infantryLoadoutCustomization.fireModesText.text = string.Join(" / ", wp.firing.fireModes);
        infantryLoadoutCustomization.destructionForceText.text = wp.projectileValues.destructionRadius.ToString("F0");
        infantryLoadoutCustomization.damageText.text = wp.projectileValues.infantryDamage.ToString("F1");
        infantryLoadoutCustomization.minimumDamageText.text = wp.projectileValues.minimumDamage.ToString("F1");
        infantryLoadoutCustomization.vehicleBaseDamageText.text = wp.projectileValues.vehicleDamage.ToString("F1");
        infantryLoadoutCustomization.headshotMultiplierText.text = wp.projectileValues.headshotMultiplier.ToString("F1");
        infantryLoadoutCustomization.damageDropoffText.text = wp.projectileValues.damageDropoff.ToString("F0") + "%";
        infantryLoadoutCustomization.damageDropoffTimerText.text = wp.projectileValues.damageDropoffTimer.ToString("F2") + "s";
        infantryLoadoutCustomization.spreadIncreaserText.text = wp.spreadValues.spreadIncreaser.ToString("F2");
        infantryLoadoutCustomization.maxSpreadText.text = wp.spreadValues.maxSpread.ToString("F2");
        infantryLoadoutCustomization.horizontalRecoilText.text = wp.recoilValues.recoilPattern.Average(v => v.horizontalRecoil.value).ToString("F2");
        infantryLoadoutCustomization.verticalRecoilText.text = wp.recoilValues.recoilPattern.Average(v => v.verticalRecoil.value).ToString("F2");
        infantryLoadoutCustomization.firstShotRecoilIncreaserText.text = "x" + wp.recoilValues.firstShootRecoilMultiplier.ToString("F1");
        infantryLoadoutCustomization.magCountText.text = wp.reloadValues.magCount.ToString();
        infantryLoadoutCustomization.bulletsPerMagText.text = wp.reloadValues.bulletsPerMag.ToString();
        infantryLoadoutCustomization.reloadSpeedText.text = wp.reloadValues.reloadTime.ToString("F2") + "s";
    }
    */
}