using System;
using System.Collections.Generic;
using UnityEngine;

public class WeaponCustomizationManager : MonoBehaviour
{
    private InfantryLoadoutCustomization infantryLoadoutCustomization;
    private enum CustomizationPart
    {
        None,
        Sight,
        Barrel,
        Mag,
        Grip,
        SideGrip,
        Ergonomics
    }

    private CustomizationPart _currentCustomizationPart = CustomizationPart.None;
    private readonly List<GameObject> _buttonsList = new List<GameObject>();

    public void Initialize(InfantryLoadoutCustomization parent)
    {
        infantryLoadoutCustomization = parent;
        SetupButtonListeners();
    }

    private void SetupButtonListeners()
    {
        SetupCustomizeButtonListener(infantryLoadoutCustomization.customizeWeaponButton, OnCustomizeWeaponButtonClicked);
        SetupCustomizeButtonListener(infantryLoadoutCustomization.customizeWeaponButtonSight, OnCustomizeSightButtonClicked);
        SetupCustomizeButtonListener(infantryLoadoutCustomization.customizeWeaponButtonBarrel, OnCustomizeBarrelButtonClicked);
        SetupCustomizeButtonListener(infantryLoadoutCustomization.customizeWeaponButtonMag, OnCustomizeMagButtonClicked);
        SetupCustomizeButtonListener(infantryLoadoutCustomization.customizeWeaponButtonGrip, OnCustomizeGripButtonClicked);
        SetupCustomizeButtonListener(infantryLoadoutCustomization.customizeWeaponButtonSideGrip, OnCustomizeSideGripButtonClicked);
        SetupCustomizeButtonListener(infantryLoadoutCustomization.customizeWeaponButtonErgonomics, OnCustomizeErgonomicsButtonClicked);
    }

    private void SetupCustomizeButtonListener(GameObject button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
        {
            var btn = button.GetComponent<UnityEngine.UI.Button>();
            if (btn != null) btn.onClick.AddListener(action);
        }
    }

    public void OnCustomizeWeaponButtonClicked()
    {
        if (infantryLoadoutCustomization._currentItemSelected == null) return;

        infantryLoadoutCustomization.SetCurrentStage(InfantryLoadoutCustomization.SelectionStage.WeaponCustomization);
        infantryLoadoutCustomization._weaponBeingCustomized = infantryLoadoutCustomization._currentItemSelected;
        _currentCustomizationPart = CustomizationPart.None;

        infantryLoadoutCustomization.itemSelectionManager.ClearWeaponsGadgetsChildren();

        if (infantryLoadoutCustomization.weaponsGadgetsSlider != null)
            infantryLoadoutCustomization.weaponsGadgetsSlider.gameObject.SetActive(false);

        infantryLoadoutCustomization.weaponsGadgetsParent.gameObject.SetActive(false);
        infantryLoadoutCustomization.customizeWeaponButton.SetActive(false);
        infantryLoadoutCustomization.customization_buttons_parent.SetActive(true);

        WeaponProperties wp = infantryLoadoutCustomization._weaponBeingCustomized.GetComponent<WeaponProperties>();
        string weaponName = wp != null ? wp.weapon_name : infantryLoadoutCustomization._weaponBeingCustomized.name;
        UpdateSelectionText($"Customizando: {weaponName}");
    }

    public void OnCustomizeSightButtonClicked()
    {
        _currentCustomizationPart = CustomizationPart.Sight;
        CreateCustomizationButtons<Sight>("Mira", sight => sight.icon_hud, sight => sight.attachmentName);
        UpdateSelectionText("Selecione uma mira");
    }

    public void OnCustomizeBarrelButtonClicked()
    {
        _currentCustomizationPart = CustomizationPart.Barrel;
        CreateCustomizationButtons<Barrel>("Cano", barrel => barrel.icon_hud, barrel => barrel.attachmentName);
        UpdateSelectionText("Selecione um cano");
    }

    public void OnCustomizeMagButtonClicked()
    {
        _currentCustomizationPart = CustomizationPart.Mag;
        CreateCustomizationButtons<Mag>("Carregador", mag => mag.icon_hud, mag => mag.attachmentName);
        UpdateSelectionText("Selecione um carregador");
    }

    public void OnCustomizeGripButtonClicked()
    {
        _currentCustomizationPart = CustomizationPart.Grip;
        CreateCustomizationButtons<Grip>("Empunhadura", grip => grip.icon_hud, grip => grip.attachmentName);
        UpdateSelectionText("Selecione uma empunhadura");
    }

    public void OnCustomizeSideGripButtonClicked()
    {
        _currentCustomizationPart = CustomizationPart.SideGrip;
        CreateCustomizationButtons<SideGrip>("Empunhadura Lateral", sidegrip => sidegrip.icon_hud, sidegrip => sidegrip.attachmentName);
        UpdateSelectionText("Selecione uma empunhadura lateral");
    }

    public void OnCustomizeErgonomicsButtonClicked()
    {
        _currentCustomizationPart = CustomizationPart.Ergonomics;
        CreateCustomizationButtons<Ergonomics>("Ergonomia", ergonomics => ergonomics.icon_hud, ergonomics => ergonomics.attachmentName);
        UpdateSelectionText("Selecione uma Ergonomia");
    }

    private void CreateCustomizationButtons<T>(string partName, Func<T, Sprite> getIcon, Func<T, string> getName) where T : MonoBehaviour
    {
        infantryLoadoutCustomization.itemSelectionManager.ClearWeaponsGadgetsChildren();
        _buttonsList.Clear(); // --- CORREÇÃO: Limpa a lista interna de botões para evitar clones zumbis

        infantryLoadoutCustomization.weaponsGadgetsParent.gameObject.SetActive(true);
        float maxSliderY = infantryLoadoutCustomization.minScrollY;

        if (infantryLoadoutCustomization._weaponBeingCustomized == null) return;

        T[] components = infantryLoadoutCustomization._weaponBeingCustomized.GetComponentsInChildren<T>(true);

        if (typeof(T) != typeof(Mag))
        {
            CreateRemoveButton<T>(partName);
        }

        int componentIndex = typeof(T) != typeof(Mag) ? 1 : 0;
        foreach (T component in components)
        {
            maxSliderY += infantryLoadoutCustomization.maxScrollYIncreaser;
            CreateCustomizationPartButton(component, getIcon(component), getName(component), partName, componentIndex);
            componentIndex++;
        }

        ShowSliderIfNeeded(components.Length + (typeof(T) != typeof(Mag) ? 1 : 0));
    }

    private void CreateRemoveButton<T>(string partType) where T : MonoBehaviour
    {
        GameObject removeButton = Instantiate(infantryLoadoutCustomization.removeItemButtonPrefab, infantryLoadoutCustomization.weaponsGadgetsParent);
        ApplyVerticalSpacing(removeButton, 0);

        var removeComponent = removeButton.AddComponent<RemoveAttachmentButtonComponents>();
        removeComponent.Initialize(partType, infantryLoadoutCustomization._weaponBeingCustomized, this);

        _buttonsList.Add(removeButton);
    }

    private void CreateCustomizationPartButton<T>(T component, Sprite icon, string name, string partType, int index)
        where T : MonoBehaviour
    {
        GameObject button = Instantiate(infantryLoadoutCustomization.buttonPrefab, infantryLoadoutCustomization.weaponsGadgetsParent);
        ApplyVerticalSpacing(button, index);

        var buttonComponent = button.AddComponent<CustomizationButtonComponents>();
        buttonComponent.Initialize(icon, component.gameObject, component, partType, name, false, infantryLoadoutCustomization);

        _buttonsList.Add(button);
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

    private void ShowSliderIfNeeded(int itemCount)
    {
        if (infantryLoadoutCustomization.weaponsGadgetsSlider != null && itemCount > 0)
        {
            infantryLoadoutCustomization.weaponsGadgetsSlider.gameObject.SetActive(true);
            infantryLoadoutCustomization.weaponsGadgetsSlider.value = 0f;
        }
    }

    public void OnCustomizationItemClicked(GameObject partObject, MonoBehaviour component, string partType)
    {
        Type targetType = GetTargetTypeFromCustomizationPart();
        WeaponProperties weaponProps = infantryLoadoutCustomization._weaponBeingCustomized.GetComponent<WeaponProperties>();

        if (infantryLoadoutCustomization._weaponBeingCustomized == null || targetType == null) return;

        Attatchment attachment = partObject.GetComponent<Attatchment>();
        bool is_max_points_reached = weaponProps.current_attachment_points + attachment.attatchment_points > 100;
        if (is_max_points_reached) return;

        UpdateWeaponPartInInstance(partObject, targetType);
        UpdateAttachmentOutlines();
        infantryLoadoutCustomization.UpdateWeaponStats(weaponProps);
        infantryLoadoutCustomization.SaveCurrentLoadout();
    }

    private Type GetTargetTypeFromCustomizationPart()
    {
        return _currentCustomizationPart switch
        {
            CustomizationPart.Sight => typeof(Sight),
            CustomizationPart.Barrel => typeof(Barrel),
            CustomizationPart.Mag => typeof(Mag),
            CustomizationPart.Grip => typeof(Grip),
            CustomizationPart.SideGrip => typeof(SideGrip),
            CustomizationPart.Ergonomics => typeof(Ergonomics),
            _ => null
        };
    }

    private void UpdateWeaponPartInInstance(GameObject partObject, Type targetType)
    {
        if (infantryLoadoutCustomization._weaponBeingCustomized == null) return;

        WeaponProperties weaponProps = infantryLoadoutCustomization._weaponBeingCustomized.GetComponent<WeaponProperties>();

        Component[] existingParts = infantryLoadoutCustomization._weaponBeingCustomized.GetComponentsInChildren(targetType, true);
        foreach (Component part in existingParts)
        {
            bool isSelected = part.gameObject == partObject;
            part.gameObject.SetActive(isSelected);

            if (isSelected)
            {
                UpdateAttachmentController(part, weaponProps);
            }
        }
    }

    private void UpdateAttachmentController(Component part, WeaponProperties weaponProps)
    {
        var attachmentManager = weaponProps.GetComponent<AttatchmentManager>();

        if (part is Sight sight) attachmentManager.UpdateSight(sight, weaponProps);
        else if (part is Barrel barrel)attachmentManager.UpdateBarrel(barrel, weaponProps);
        else if (part is Mag mag) attachmentManager.UpdateMag(mag, weaponProps);
        else if (part is Grip grip) attachmentManager.UpdateGrip(grip, weaponProps);
        else if (part is SideGrip sidegrip) attachmentManager.UpdateSideGrip(sidegrip, weaponProps);
        else if (part is Ergonomics ergonomics) attachmentManager.UpdateErgonomics(ergonomics, weaponProps);
    }

    public void RemoveAttachment(string partType, GameObject weaponBeingCustomized)
    {
        if (weaponBeingCustomized == null) return;

        WeaponProperties weaponProps = weaponBeingCustomized.GetComponent<WeaponProperties>();
        if (weaponProps == null) return;

        Type targetType = GetTargetTypeFromPartType(partType);
        if (targetType == null) return;

        AttatchmentManager attachmentManager = weaponBeingCustomized.GetComponent<AttatchmentManager>();

        if (targetType == typeof(Grip)) attachmentManager.RemoveGrip(weaponProps);
        else if (targetType == typeof(Barrel)) attachmentManager.RemoveBarrel(weaponProps);
        else if (targetType == typeof(Sight)) attachmentManager.RemoveSight(weaponProps);
        else if (targetType == typeof(SideGrip)) attachmentManager.RemoveSideGrip(weaponProps);
        else if (targetType == typeof(Ergonomics)) attachmentManager.RemoveErgonomics();

        DisableAttachmentInInstance(targetType, weaponBeingCustomized);
        infantryLoadoutCustomization.UpdateWeaponStats(weaponProps);
        UpdateAttachmentOutlines();
        infantryLoadoutCustomization.SaveCurrentLoadout();
    }

    private Type GetTargetTypeFromPartType(string partType)
    {
        return partType switch
        {
            "Mira" => typeof(Sight),
            "Cano" => typeof(Barrel),
            "Carregador" => typeof(Mag),
            "Empunhadura" => typeof(Grip),
            "Empunhadura Lateral" => typeof(SideGrip),
            "Ergonomia" => typeof(Ergonomics),
            _ => null
        };
    }

    private void DisableAttachmentInInstance(Type targetType, GameObject weaponInstance)
    {
        Component[] components = weaponInstance.GetComponentsInChildren(targetType, true);
        foreach (Component comp in components)
        {
            comp.gameObject.SetActive(false);
        }
    }

    public void UpdateAttachmentOutlines()
    {
        foreach (GameObject button in _buttonsList)
        {
            if (button == null) continue;

            var customizationComponent = button.GetComponent<CustomizationButtonComponents>();
            if (customizationComponent != null) customizationComponent.UpdateOutlineState();
            
        }
    }

    public void OnBackFromCustomization()
    {
        // 1. Se estivermos olhando uma categoria específica (Ex: Mira, Cano), o botão de voltar 
        // deve nos retornar ao menu de categorias de customização.
        if (_currentCustomizationPart != CustomizationPart.None)
        {
            _currentCustomizationPart = CustomizationPart.None;

            infantryLoadoutCustomization.itemSelectionManager.ClearWeaponsGadgetsChildren();
            _buttonsList.Clear(); // Limpa as referências de anexos

            infantryLoadoutCustomization.weaponsGadgetsParent.gameObject.SetActive(false);

            if (infantryLoadoutCustomization.weaponsGadgetsSlider != null)
                infantryLoadoutCustomization.weaponsGadgetsSlider.gameObject.SetActive(false);

            infantryLoadoutCustomization.customization_buttons_parent.SetActive(true);

            WeaponProperties wp = infantryLoadoutCustomization._weaponBeingCustomized.GetComponent<WeaponProperties>();
            string weaponName = wp != null ? wp.weapon_name : infantryLoadoutCustomization._weaponBeingCustomized.name;
            if (infantryLoadoutCustomization.currentSelectionText != null) infantryLoadoutCustomization.currentSelectionText.text = $"Customizando: {weaponName}";

            return; // Retorna para não executar o código abaixo
        }

        // 2. Se já estivermos no menu de categorias principais, volta para a lista de Armas.
        infantryLoadoutCustomization._weaponBeingCustomized = null;

        infantryLoadoutCustomization.itemSelectionManager.ClearWeaponsGadgetsChildren();
        _buttonsList.Clear(); // Limpa referências antigas

        infantryLoadoutCustomization.customization_buttons_parent.SetActive(false);
        infantryLoadoutCustomization.weaponsGadgetsParent.gameObject.SetActive(true);

        infantryLoadoutCustomization.SetCurrentStage(InfantryLoadoutCustomization.SelectionStage.ItemSelection);

        var currentOption = infantryLoadoutCustomization.loadoutOptionManager.GetCurrentOption();
        infantryLoadoutCustomization.itemSelectionManager.ShowAvailableItems(currentOption);

        // --- CORREÇÃO VITAL: Resetar o slider para a barra rolar para o topo e exibir as armas ---
        infantryLoadoutCustomization.itemSelectionManager.ResetSlider();

        if (infantryLoadoutCustomization.currentSelectionText != null && infantryLoadoutCustomization._currentItemSelected != null)
            if (infantryLoadoutCustomization.currentSelectionText != null) infantryLoadoutCustomization.currentSelectionText.text = $"Selecionando: {GetLoadoutOptionDisplayName(currentOption)} - {infantryLoadoutCustomization._selectedClass}";

        infantryLoadoutCustomization.itemSelectionManager.UpdateAllButtonOutlines();
    }

    private string GetLoadoutOptionDisplayName(object option)
    {
        return option switch
        {
            LoadoutOptionManager.LoadoutOption.PrimaryWeapon => "Arma Primária",
            LoadoutOptionManager.LoadoutOption.SecondaryWeapon => "Arma Secundária",
            LoadoutOptionManager.LoadoutOption.Gadget1 => "Gadget",
            LoadoutOptionManager.LoadoutOption.Skin => "Skin",
            _ => option.ToString()
        };
    }

    private void UpdateSelectionText(string text)
    {
        if (infantryLoadoutCustomization.currentSelectionText != null) infantryLoadoutCustomization.currentSelectionText.text = text;
    }
}