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
        CantedSight,
        Nozzle,
        Barrel,
        Mag,
        Grip,
        SideGrip,
        Ergonomics
    }

    private CustomizationPart _currentCustomizationPart = CustomizationPart.None;
    private readonly List<GameObject> _buttonsList = new List<GameObject>();
    private GameObject _barrelCustomizationButton;
    private GameObject _resetAttachmentsButton;
    public void Initialize(InfantryLoadoutCustomization parent)
    {
        infantryLoadoutCustomization = parent;
        SetupButtonListeners();
    }

    private void SetupButtonListeners()
    {
        _barrelCustomizationButton = GetOrCreateBarrelCustomizationButton();
        _resetAttachmentsButton = GetOrCreateResetAttachmentsButton();

        SetupCustomizeButtonListener(infantryLoadoutCustomization.customizeWeaponButton, OnCustomizeWeaponButtonClicked);
        SetupCustomizeButtonListener(infantryLoadoutCustomization.customizeWeaponButtonSight, OnCustomizeSightButtonClicked);
        SetupCustomizeButtonListener(infantryLoadoutCustomization.customizeWeaponButtonCantedSight, OnCustomizeCantedSightButtonClicked);
        SetupCustomizeButtonListener(infantryLoadoutCustomization.customizeWeaponButtonNozzle, OnCustomizeNozzleButtonClicked);
        SetupCustomizeButtonListener(_barrelCustomizationButton, OnCustomizeBarrelButtonClicked);
        SetupCustomizeButtonListener(infantryLoadoutCustomization.customizeWeaponButtonMag, OnCustomizeMagButtonClicked);
        SetupCustomizeButtonListener(infantryLoadoutCustomization.customizeWeaponButtonGrip, OnCustomizeGripButtonClicked);
        SetupCustomizeButtonListener(infantryLoadoutCustomization.customizeWeaponButtonSideGrip, OnCustomizeSideGripButtonClicked);
        SetupCustomizeButtonListener(infantryLoadoutCustomization.customizeWeaponButtonErgonomics, OnCustomizeErgonomicsButtonClicked);
        SetupCustomizeButtonListener(_resetAttachmentsButton, OnResetAttachmentsButtonClicked);
    }

    private GameObject GetOrCreateBarrelCustomizationButton()
    {
        if (infantryLoadoutCustomization.customizeWeaponButtonBarrel != null)
            return infantryLoadoutCustomization.customizeWeaponButtonBarrel;

        GameObject nozzleButton = infantryLoadoutCustomization.customizeWeaponButtonNozzle;
        if (nozzleButton == null || nozzleButton.transform.parent == null) return null;

        GameObject barrelButton = Instantiate(nozzleButton, nozzleButton.transform.parent);
        barrelButton.name = "Barrel";

        TMPro.TextMeshProUGUI buttonText = barrelButton.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        if (buttonText != null) buttonText.text = "Barrel";

        PositionGeneratedBarrelButton(barrelButton);
        return barrelButton;
    }

    private GameObject GetOrCreateResetAttachmentsButton()
    {
        if (infantryLoadoutCustomization.resetWeaponAttachmentsButton != null)
            return infantryLoadoutCustomization.resetWeaponAttachmentsButton;

        GameObject template = infantryLoadoutCustomization.customizeWeaponButtonErgonomics != null
            ? infantryLoadoutCustomization.customizeWeaponButtonErgonomics
            : _barrelCustomizationButton;

        if (template == null || template.transform.parent == null) return null;

        GameObject resetButton = Instantiate(template, template.transform.parent);
        resetButton.name = "ResetAttachments";
        resetButton.transform.SetAsLastSibling();

        TMPro.TextMeshProUGUI buttonText = resetButton.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        if (buttonText != null) buttonText.text = "Restore Defaults";

        UnityEngine.UI.Button button = resetButton.GetComponent<UnityEngine.UI.Button>();
        if (button != null) button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();

        PositionGeneratedResetButton(resetButton, template);
        return resetButton;
    }

    private void PositionGeneratedResetButton(GameObject resetButton, GameObject template)
    {
        RectTransform resetRect = resetButton.GetComponent<RectTransform>();
        RectTransform templateRect = template.GetComponent<RectTransform>();
        if (resetRect == null || templateRect == null) return;

        float lowestY = templateRect.anchoredPosition.y;
        GameObject[] customizationButtons =
        {
            infantryLoadoutCustomization.customizeWeaponButtonSight,
            infantryLoadoutCustomization.customizeWeaponButtonCantedSight,
            infantryLoadoutCustomization.customizeWeaponButtonNozzle,
            _barrelCustomizationButton,
            infantryLoadoutCustomization.customizeWeaponButtonMag,
            infantryLoadoutCustomization.customizeWeaponButtonGrip,
            infantryLoadoutCustomization.customizeWeaponButtonSideGrip,
            infantryLoadoutCustomization.customizeWeaponButtonErgonomics
        };

        foreach (GameObject customizationButton in customizationButtons)
        {
            if (customizationButton == null) continue;
            RectTransform buttonRect = customizationButton.GetComponent<RectTransform>();
            if (buttonRect != null) lowestY = Mathf.Min(lowestY, buttonRect.anchoredPosition.y);
        }

        float spacing = Mathf.Max(Mathf.Abs(infantryLoadoutCustomization.loadoutOptionSpacingY), resetRect.rect.height);
        resetRect.anchoredPosition = new Vector2(templateRect.anchoredPosition.x, lowestY - spacing);
    }

    private void PositionGeneratedBarrelButton(GameObject barrelButton)
    {
        RectTransform barrelRect = barrelButton.GetComponent<RectTransform>();
        RectTransform nozzleRect = infantryLoadoutCustomization.customizeWeaponButtonNozzle.GetComponent<RectTransform>();
        RectTransform cantedSightRect = infantryLoadoutCustomization.customizeWeaponButtonCantedSight != null
            ? infantryLoadoutCustomization.customizeWeaponButtonCantedSight.GetComponent<RectTransform>()
            : null;
        RectTransform gripRect = infantryLoadoutCustomization.customizeWeaponButtonGrip != null
            ? infantryLoadoutCustomization.customizeWeaponButtonGrip.GetComponent<RectTransform>()
            : null;

        if (barrelRect == null || nozzleRect == null) return;

        float rowY = cantedSightRect != null
            ? cantedSightRect.anchoredPosition.y
            : nozzleRect.anchoredPosition.y + Mathf.Abs(infantryLoadoutCustomization.loadoutOptionSpacingY);
        float rightColumnX = gripRect != null
            ? gripRect.anchoredPosition.x
            : nozzleRect.anchoredPosition.x + barrelRect.sizeDelta.x;

        if (cantedSightRect != null)
            cantedSightRect.anchoredPosition = new Vector2(nozzleRect.anchoredPosition.x, rowY);

        barrelRect.anchoredPosition = new Vector2(rightColumnX, rowY);
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

        infantryLoadoutCustomization.uIUpdateManager.ClearAttachmentFocus();
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
        string weaponName = wp != null ? wp.weaponName : infantryLoadoutCustomization._weaponBeingCustomized.name;
        UpdateSelectionText($"Customizing: {weaponName}");
    }

    public void OnCustomizeSightButtonClicked()
    {
        _currentCustomizationPart = CustomizationPart.Sight;
        CreateCustomizationButtons<Sight>("Sight", sight => sight.iconHud, sight => sight.attachmentName, sight => !(sight is CantedSight));
        FocusOnAttachmentHolder<Sight>("SightHolder");
        UpdateSelectionText("Select a sight");
    }

    public void OnCustomizeCantedSightButtonClicked()
    {
        _currentCustomizationPart = CustomizationPart.CantedSight;
        CreateCustomizationButtons<CantedSight>("Canted Sight", sight => sight.iconHud, sight => sight.attachmentName);
        FocusOnAttachmentHolder<CantedSight>("CantedSightHolder", "CantedSight");
        UpdateSelectionText("Select a canted sight");
    }

    public void OnCustomizeNozzleButtonClicked()
    {
        _currentCustomizationPart = CustomizationPart.Nozzle;
        CreateCustomizationButtons<Nozzle>("Muzzle", nozzle => nozzle.iconHud, nozzle => nozzle.attachmentName);
        FocusOnAttachmentHolder<Nozzle>("NozzleHolder");
        UpdateSelectionText("Select a muzzle");
    }

    public void OnCustomizeBarrelButtonClicked()
    {
        _currentCustomizationPart = CustomizationPart.Barrel;
        CreateCustomizationButtons<Barrel>(
            "Barrel",
            barrel => barrel.iconHud,
            barrel => string.IsNullOrWhiteSpace(barrel.attachmentName) ? barrel.gameObject.name : barrel.attachmentName);
        FocusOnAttachmentHolder<Barrel>("BarrelHolder");
        UpdateSelectionText("Select a barrel");
    }

    public void OnCustomizeMagButtonClicked()
    {
        _currentCustomizationPart = CustomizationPart.Mag;
        CreateCustomizationButtons<Mag>("Magazine", mag => mag.iconHud, mag => mag.attachmentName);
        FocusOnAttachmentHolder<Mag>("MagHolder");
        UpdateSelectionText("Select a magazine");
    }

    public void OnCustomizeGripButtonClicked()
    {
        _currentCustomizationPart = CustomizationPart.Grip;
        CreateCustomizationButtons<Grip>("Grip", grip => grip.iconHud, grip => grip.attachmentName);
        FocusOnAttachmentHolder<Grip>("GripHolder");
        UpdateSelectionText("Select a grip");
    }

    public void OnCustomizeSideGripButtonClicked()
    {
        _currentCustomizationPart = CustomizationPart.SideGrip;
        CreateCustomizationButtons<SideGrip>("Side Grip", sidegrip => sidegrip.iconHud, sidegrip => sidegrip.attachmentName);
        FocusOnAttachmentHolder<SideGrip>("SideGripHolder");
        UpdateSelectionText("Select a side grip");
    }

    public void OnCustomizeErgonomicsButtonClicked()
    {
        _currentCustomizationPart = CustomizationPart.Ergonomics;
        CreateCustomizationButtons<Ergonomics>("Ergonomics", ergonomics => ergonomics.iconHud, ergonomics => ergonomics.attachmentName);
        FocusOnAttachmentHolder<Ergonomics>("ErgonomicsHolder", "Ergonomics");
        UpdateSelectionText("Select an ergonomics attachment");
    }

    private void FocusOnAttachmentHolder<T>(params string[] holderNames) where T : Attatchment
    {
        if (infantryLoadoutCustomization._weaponBeingCustomized == null ||
            infantryLoadoutCustomization.uIUpdateManager == null) return;

        Transform weaponTransform = infantryLoadoutCustomization._weaponBeingCustomized.transform;
        Transform[] descendants = weaponTransform.GetComponentsInChildren<Transform>(true);
        Transform namedFallback = null;

        foreach (string holderName in holderNames)
        {
            foreach (Transform descendant in descendants)
            {
                if (!string.Equals(descendant.name, holderName, StringComparison.OrdinalIgnoreCase)) continue;

                if (namedFallback == null) namedFallback = descendant;
                T[] attachments = descendant.GetComponentsInChildren<T>(true);
                foreach (T attachment in attachments)
                {
                    if (attachment.GetType() != typeof(T)) continue;
                    infantryLoadoutCustomization.uIUpdateManager.FocusAttachmentHolder(descendant);
                    return;
                }
            }
        }

        if (namedFallback != null)
        {
            infantryLoadoutCustomization.uIUpdateManager.FocusAttachmentHolder(namedFallback);
            return;
        }

        T[] weaponAttachments = weaponTransform.GetComponentsInChildren<T>(true);
        foreach (T attachment in weaponAttachments)
        {
            if (attachment.GetType() != typeof(T)) continue;
            Transform fallback = attachment.transform.parent != null ? attachment.transform.parent : attachment.transform;
            infantryLoadoutCustomization.uIUpdateManager.FocusAttachmentHolder(fallback);
            return;
        }

        infantryLoadoutCustomization.uIUpdateManager.ClearAttachmentFocus();
    }

    public void OnResetAttachmentsButtonClicked()
    {
        GameObject weapon = infantryLoadoutCustomization._weaponBeingCustomized;
        if (weapon == null) return;

        WeaponProperties weaponProperties = weapon.GetComponent<WeaponProperties>();
        AttatchmentManager attachmentManager = weapon.GetComponent<AttatchmentManager>();
        if (weaponProperties == null || attachmentManager == null) return;

        attachmentManager.ResetToStandardAttachments();
        UpdateAttachmentOutlines();
        infantryLoadoutCustomization.UpdateWeaponStats(weaponProperties);
        infantryLoadoutCustomization.SaveCurrentLoadout();
        UpdateSelectionText("Attachments restored to defaults");
    }

    private void CreateCustomizationButtons<T>(
        string partName,
        Func<T, Sprite> getIcon,
        Func<T, string> getName,
        Func<T, bool> includeComponent = null) where T : Attatchment
    {
        infantryLoadoutCustomization.ClearAttachmentStatsPreview();
        infantryLoadoutCustomization.itemSelectionManager.ClearWeaponsGadgetsChildren();
        _buttonsList.Clear(); // --- CORREÇÃO: Limpa a lista interna de botões para evitar clones zumbis

        infantryLoadoutCustomization.weaponsGadgetsParent.gameObject.SetActive(true);
        float maxSliderY = infantryLoadoutCustomization.minScrollY;

        if (infantryLoadoutCustomization._weaponBeingCustomized == null) return;

        T[] allComponents = infantryLoadoutCustomization._weaponBeingCustomized.GetComponentsInChildren<T>(true);
        List<T> components = new List<T>();

        foreach (T component in allComponents)
        {
            if (includeComponent == null || includeComponent(component)) components.Add(component);
        }

        List<T> orderedComponents = new List<T>(components.Count);
        foreach (T component in components)
        {
            if (component.isStandardAttatchment) orderedComponents.Add(component);
        }
        foreach (T component in components)
        {
            if (!component.isStandardAttatchment) orderedComponents.Add(component);
        }
        components = orderedComponents;

        bool canRemoveAttachment = typeof(T) != typeof(Mag) && typeof(T) != typeof(Barrel);

        if (canRemoveAttachment)
        {
            CreateRemoveButton<T>(partName);
        }

        int componentIndex = canRemoveAttachment ? 1 : 0;
        foreach (T component in components)
        {
            maxSliderY += infantryLoadoutCustomization.maxScrollYIncreaser;
            CreateCustomizationPartButton(component, getIcon(component), getName(component), partName, componentIndex);
            componentIndex++;
        }

        ShowSliderIfNeeded(components.Count + (canRemoveAttachment ? 1 : 0));
    }

    private void CreateRemoveButton<T>(string partType) where T : Attatchment
    {
        GameObject removeButton = Instantiate(infantryLoadoutCustomization.removeItemButtonPrefab, infantryLoadoutCustomization.weaponsGadgetsParent);
        ApplyVerticalSpacing(removeButton, 0);

        var removeComponent = removeButton.AddComponent<RemoveAttachmentButtonComponents>();
        removeComponent.Initialize(partType, infantryLoadoutCustomization._weaponBeingCustomized, this);

        _buttonsList.Add(removeButton);
    }

    private void CreateCustomizationPartButton<T>(T component, Sprite icon, string name, string partType, int index)
        where T : Attatchment
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
        infantryLoadoutCustomization.itemSelectionManager.ConfigureScrollForItemCount(itemCount);
    }

    public void OnCustomizationItemClicked(GameObject partObject, MonoBehaviour component, string partType)
    {
        if (infantryLoadoutCustomization._weaponBeingCustomized == null || partObject == null) return;

        Type targetType = GetTargetTypeFromCustomizationPart();
        WeaponProperties weaponProps = infantryLoadoutCustomization._weaponBeingCustomized.GetComponent<WeaponProperties>();
        AttatchmentManager attachmentManager = infantryLoadoutCustomization._weaponBeingCustomized.GetComponent<AttatchmentManager>();
        Attatchment attachment = partObject.GetComponent<Attatchment>();
        if (targetType == null || weaponProps == null || attachmentManager == null || attachment == null) return;

        if (!attachmentManager.CanEquipAttachment(attachment))
        {
            float projectedPoints = attachmentManager.GetProjectedAttachmentPoints(attachment);
            UpdateSelectionText(
                $"Attachment limit exceeded: {projectedPoints:0.##} / {AttatchmentManager.MaxAttachmentPoints:0} points");
            PlayAttachmentDenialSound();
            return;
        }

        UpdateWeaponPartInInstance(partObject, targetType);
        UpdateAttachmentOutlines();
        infantryLoadoutCustomization.UpdateWeaponStats(weaponProps);
        infantryLoadoutCustomization.SaveCurrentLoadout();
    }

    private void PlayAttachmentDenialSound()
    {
        SoundManager.SoundComponents denialSound = infantryLoadoutCustomization.purchaseDenialItemSfx;
        if (denialSound == null || denialSound.clip == null) return;

        SoundManager.Play2dSoundLocal(denialSound.clip, denialSound.properties);
    }

    private Type GetTargetTypeFromCustomizationPart()
    {
        return _currentCustomizationPart switch
        {
            CustomizationPart.Sight => typeof(Sight),
            CustomizationPart.CantedSight => typeof(CantedSight),
            CustomizationPart.Nozzle => typeof(Nozzle),
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
            if (part.GetType() != targetType) continue;

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

        if (part is CantedSight cantedSight) attachmentManager.UpdateCantedSight(cantedSight, weaponProps);
        else if (part is Sight sight) attachmentManager.UpdateSight(sight, weaponProps);
        else if (part is Nozzle nozzle) attachmentManager.UpdateNozzle(nozzle, weaponProps);
        else if (part is Barrel barrel) attachmentManager.UpdateBarrel(barrel, weaponProps);
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

        if (targetType == typeof(Grip)) attachmentManager.RemoveGrip();
        else if (targetType == typeof(Nozzle)) attachmentManager.RemoveNozzle();
        else if (targetType == typeof(Barrel)) attachmentManager.RemoveBarrel();
        else if (targetType == typeof(CantedSight)) attachmentManager.RemoveCantedSight();
        else if (targetType == typeof(Sight)) attachmentManager.RemoveSight();
        else if (targetType == typeof(SideGrip)) attachmentManager.RemoveSideGrip();
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
            "Sight" => typeof(Sight),
            "Canted Sight" => typeof(CantedSight),
            "Muzzle" => typeof(Nozzle),
            "Barrel" => typeof(Barrel),
            "Magazine" => typeof(Mag),
            "Grip" => typeof(Grip),
            "Side Grip" => typeof(SideGrip),
            "Ergonomics" => typeof(Ergonomics),
            _ => null
        };
    }

    private void DisableAttachmentInInstance(Type targetType, GameObject weaponInstance)
    {
        Component[] components = weaponInstance.GetComponentsInChildren(targetType, true);
        foreach (Component comp in components)
        {
            if (comp.GetType() != targetType) continue;
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
        infantryLoadoutCustomization.ClearAttachmentStatsPreview();

        // 1. Se estivermos olhando uma categoria específica (Ex: Mira, Cano), o botão de voltar 
        // deve nos retornar ao menu de categorias de customização.
        if (_currentCustomizationPart != CustomizationPart.None)
        {
            _currentCustomizationPart = CustomizationPart.None;
            infantryLoadoutCustomization.uIUpdateManager.ClearAttachmentFocus();

            infantryLoadoutCustomization.itemSelectionManager.ClearWeaponsGadgetsChildren();
            _buttonsList.Clear(); // Limpa as referências de anexos

            infantryLoadoutCustomization.weaponsGadgetsParent.gameObject.SetActive(false);

            if (infantryLoadoutCustomization.weaponsGadgetsSlider != null)
                infantryLoadoutCustomization.weaponsGadgetsSlider.gameObject.SetActive(false);

            infantryLoadoutCustomization.customization_buttons_parent.SetActive(true);

            WeaponProperties wp = infantryLoadoutCustomization._weaponBeingCustomized.GetComponent<WeaponProperties>();
            string weaponName = wp != null ? wp.weaponName : infantryLoadoutCustomization._weaponBeingCustomized.name;
            if (infantryLoadoutCustomization.currentSelectionText != null) infantryLoadoutCustomization.currentSelectionText.text = $"Customizing: {weaponName}";

            return; // Retorna para não executar o código abaixo
        }

        // 2. Se já estivermos no menu de categorias principais, volta para a lista de Armas.
        infantryLoadoutCustomization.uIUpdateManager.ClearAttachmentFocus();
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
            if (infantryLoadoutCustomization.currentSelectionText != null) infantryLoadoutCustomization.currentSelectionText.text = $"Selecting: {GetLoadoutOptionDisplayName(currentOption)} - {infantryLoadoutCustomization._selectedClass}";

        infantryLoadoutCustomization.itemSelectionManager.UpdateAllButtonOutlines();
    }

    private string GetLoadoutOptionDisplayName(object option)
    {
        return option switch
        {
            LoadoutOptionManager.LoadoutOption.PrimaryWeapon => "Primary Weapon",
            LoadoutOptionManager.LoadoutOption.SecondaryWeapon => "Secondary Weapon",
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
