using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ItemSelectionManager : MonoBehaviour
{
    private InfantryLoadoutCustomization infantryLoadoutCustomization;
    private readonly List<GameObject> _buttonsList = new List<GameObject>();
    private Vector3 _originalWeaponsGadgetsPosition;
    private float _maxSliderY;
    private float _minScrollY;


    public void Initialize(InfantryLoadoutCustomization parent)
    {
        infantryLoadoutCustomization = parent;
        _originalWeaponsGadgetsPosition = infantryLoadoutCustomization.weaponsGadgetsParent.localPosition;
        _minScrollY = infantryLoadoutCustomization.minScrollY;

        InitializeSlider();
    }

    private void InitializeSlider()
    {
        if (infantryLoadoutCustomization.weaponsGadgetsSlider == null) return;

        infantryLoadoutCustomization.weaponsGadgetsSlider.minValue = infantryLoadoutCustomization.sliderMinValue;
        infantryLoadoutCustomization.weaponsGadgetsSlider.maxValue = infantryLoadoutCustomization.sliderMaxValue;
        infantryLoadoutCustomization.weaponsGadgetsSlider.value = 0;
        infantryLoadoutCustomization.weaponsGadgetsSlider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    public void ShowAvailableItems(LoadoutOptionManager.LoadoutOption option)
    {
        // LIMPA COMPLETAMENTE todos os botões e filhos do weaponsGadgetsParent
        ClearAllWeaponsGadgetsChildren();
        
        infantryLoadoutCustomization.weaponsGadgetsParent.gameObject.SetActive(true);
        _maxSliderY = _minScrollY;

        switch (option)
        {
            case LoadoutOptionManager.LoadoutOption.PrimaryWeapon:
                ShowPrimaryWeaponsForClass();
                break;
            case LoadoutOptionManager.LoadoutOption.SecondaryWeapon:
                ShowSecondaryWeaponsForClass();
                break;
            case LoadoutOptionManager.LoadoutOption.Gadget1:
                ShowGadgets();
                break;
        }

        UpdateAllButtonOutlines();
    }

    private void ShowPrimaryWeaponsForClass()
    {
        int weaponIndex = 0;
        foreach (GameObject weapon in infantryLoadoutCustomization.primaryWeapons)
        {
            WeaponProperties wp = weapon.GetComponent<WeaponProperties>();
            if (HasClassAccessToWeapon(wp) && HasFactionAccessToWeapon(wp))
            {
                CreateWeaponButton(weapon, wp, weaponIndex);
                weaponIndex++;
            }
        }
    }

    private void ShowSecondaryWeaponsForClass()
    {
        int weaponIndex = 0;
        foreach (GameObject weapon in infantryLoadoutCustomization.secondaryWeapons)
        {
            WeaponProperties wp = weapon.GetComponent<WeaponProperties>();
            if (HasClassAccessToWeapon(wp) && HasFactionAccessToWeapon(wp))
            {
                CreateWeaponButton(weapon, wp, weaponIndex);
                weaponIndex++;
            }
        }
    }

    private void ShowGadgets()
    {
        int gadgetIndex = 0;
        foreach (GameObject gadget in infantryLoadoutCustomization.gadgets)
        {
            Gadget gd = gadget.GetComponent<Gadget>();
            if (gd == null) continue;

            if (!HasClassAccessToGadget(gd)) continue;

            CreateGadgetButton(gadget, gd, gadgetIndex);
            gadgetIndex++;
        }
    }

    private bool HasClassAccessToWeapon(WeaponProperties weaponProperties)
    {
        return weaponProperties.class_weapon.Any(c => c == infantryLoadoutCustomization._selectedClass);
    }

    private bool HasFactionAccessToWeapon(WeaponProperties weaponProperties)
    {
        if (AccountManager.Instance == null) return true;
        return weaponProperties.faction.Any(c => c == AccountManager.Instance.faction);
    }

    private bool HasClassAccessToGadget(Gadget gadget)
    {
        return gadget.class_gadget.Any(c => c == infantryLoadoutCustomization._selectedClass);
    }

    private void CreateWeaponButton(GameObject weapon, WeaponProperties wp, int index)
    {
        _maxSliderY += infantryLoadoutCustomization.maxScrollYIncreaser;

        GameObject weaponButton = Instantiate(infantryLoadoutCustomization.buttonPrefab, infantryLoadoutCustomization.weaponsGadgetsParent);
        ApplyVerticalSpacing(weaponButton, index);

        var component = weaponButton.AddComponent<WeaponButtonComponents>();
        component.Initialize(wp.icon_hud, weapon, wp, infantryLoadoutCustomization);

        _buttonsList.Add(weaponButton);
    }

    private void CreateGadgetButton(GameObject gadget, Gadget gd, int index)
    {
        GameObject gadgetButton = Instantiate(infantryLoadoutCustomization.buttonPrefab, infantryLoadoutCustomization.weaponsGadgetsParent);
        ApplyVerticalSpacing(gadgetButton, index);

        var component = gadgetButton.AddComponent<GadgetButtonComponents>();
        component.Initialize(gd.icon_hud, gadget, gd, infantryLoadoutCustomization);

        _buttonsList.Add(gadgetButton);
    }

    private void ApplyVerticalSpacing(GameObject button, int index)
    {
        RectTransform rectTransform = button.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            float yPosition = infantryLoadoutCustomization.itemButtonStartY + (index * infantryLoadoutCustomization.itemButtonSpacingY);
            rectTransform.anchoredPosition = new Vector2(infantryLoadoutCustomization.itemButtonX, yPosition);
        }
    }

    public void OnButtonMouseEnter(GameObject item)
    {
        if (infantryLoadoutCustomization._currentItemSelected != null) Destroy(infantryLoadoutCustomization._currentItemSelected);

        infantryLoadoutCustomization._currentItemSelected = Instantiate(item, infantryLoadoutCustomization.currentItemParent);
        SetupItemForCustomization(infantryLoadoutCustomization._currentItemSelected);

        infantryLoadoutCustomization._currentItemSelected.GetComponent<AttatchmentManager>().InitializeAttachments();

        WeaponProperties wp = infantryLoadoutCustomization._currentItemSelected.GetComponent<WeaponProperties>();
        if (wp != null)
        {
            infantryLoadoutCustomization.UpdateWeaponStats(wp);
        }
    }

    public void OnButtonClicked(GameObject item)
    {
        if (infantryLoadoutCustomization._currentItemSelected != null) Destroy(infantryLoadoutCustomization._currentItemSelected);

        infantryLoadoutCustomization._currentItemSelected = Instantiate(item, infantryLoadoutCustomization.currentItemParent);
        SetupItemForCustomization(infantryLoadoutCustomization._currentItemSelected);

        infantryLoadoutCustomization._currentItemSelected.GetComponent<AttatchmentManager>().InitializeAttachments();

        WeaponProperties wp = infantryLoadoutCustomization._currentItemSelected.GetComponent<WeaponProperties>();
        if (wp != null) infantryLoadoutCustomization.UpdateWeaponStats(wp);
        

        EquipItem(item);
        UpdateAllButtonOutlines();
    }

    private void SetupItemForCustomization(GameObject item)
    {
        item.layer = LayerMask.NameToLayer("LoadoutCustomization");

        foreach (MeshRenderer renderer in item.GetComponentsInChildren<MeshRenderer>(true))
        {
            renderer.gameObject.layer = LayerMask.NameToLayer("LoadoutCustomization");
        }
    }

    private void EquipItem(GameObject item)
    {
        if (infantryLoadoutCustomization.selectItemSfx != null) SoundManager.Play2dSoundLocal(infantryLoadoutCustomization.selectItemSfx.clip, infantryLoadoutCustomization.selectItemSfx.properties);

        LoadoutOptionManager.LoadoutOption currentOption = infantryLoadoutCustomization.loadoutOptionManager.GetCurrentOption();

        switch (currentOption)
        {
            case LoadoutOptionManager.LoadoutOption.PrimaryWeapon:
                infantryLoadoutCustomization.selected_primary = item;
                break;
            case LoadoutOptionManager.LoadoutOption.SecondaryWeapon:
                infantryLoadoutCustomization.selected_secondary = item;
                break;
            case LoadoutOptionManager.LoadoutOption.Gadget1:
                infantryLoadoutCustomization.selected_gadget1 = item;
                break;
        }

        infantryLoadoutCustomization.SaveCurrentLoadout();
    }

    public void UpdateAllButtonOutlines()
    {
        foreach (GameObject button in _buttonsList)
        {
            if (button == null) continue;

            var weaponComponent = button.GetComponent<WeaponButtonComponents>();
            if (weaponComponent != null)
            {
                weaponComponent.UpdateOutlineState();
                continue;
            }

            var gadgetComponent = button.GetComponent<GadgetButtonComponents>();
            if (gadgetComponent != null)
            {
                gadgetComponent.UpdateOutlineState();
            }
        }
    }

    private void OnSliderValueChanged(float value)
    {
        if (infantryLoadoutCustomization.GetCurrentStage() != InfantryLoadoutCustomization.SelectionStage.ItemSelection &&
            infantryLoadoutCustomization.GetCurrentStage() != InfantryLoadoutCustomization.SelectionStage.WeaponCustomization) return;

        float scrollY = Mathf.Lerp(infantryLoadoutCustomization.minScrollY, _maxSliderY, value);
        Vector3 newPosition = _originalWeaponsGadgetsPosition;
        newPosition.y = scrollY;
        infantryLoadoutCustomization.weaponsGadgetsParent.localPosition = newPosition;
    }

    public void ResetSlider()
    {
        if (infantryLoadoutCustomization.weaponsGadgetsSlider != null)
        {
            infantryLoadoutCustomization.weaponsGadgetsSlider.gameObject.SetActive(true);
            infantryLoadoutCustomization.weaponsGadgetsSlider.value = 0f;
        }
        infantryLoadoutCustomization.weaponsGadgetsParent.localPosition = _originalWeaponsGadgetsPosition;
    }

    public void ClearItemButtons()
    {
        foreach (GameObject button in _buttonsList.ToArray())
        {
            if (button != null && (button.GetComponent<WeaponButtonComponents>() != null ||
                                   button.GetComponent<GadgetButtonComponents>() != null))
            {
                Destroy(button);
                _buttonsList.Remove(button);
            }
        }
    }

    public void ClearWeaponsGadgetsChildren()
    {
        foreach (Transform child in infantryLoadoutCustomization.weaponsGadgetsParent)
        {
            Destroy(child.gameObject);
        }
        _buttonsList.Clear();
    }

    // NOVO MÉTODO: Limpa COMPLETAMENTE todos os filhos do weaponsGadgetsParent
    public void ClearAllWeaponsGadgetsChildren()
    {
        // Destroi todos os filhos
        foreach (Transform child in infantryLoadoutCustomization.weaponsGadgetsParent)
        {
            Destroy(child.gameObject);
        }
        
        // Limpa a lista de botões
        _buttonsList.Clear();
        
        // Limpa também os botões de skin no SkinSelectionManager
        if (infantryLoadoutCustomization.skinSelectionManager != null)
        {
            infantryLoadoutCustomization.skinSelectionManager.ClearSkinButtons();
        }
    }
}