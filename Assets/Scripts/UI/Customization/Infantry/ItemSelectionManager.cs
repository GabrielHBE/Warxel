using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ItemSelectionManager : MonoBehaviour
{
    [Header("Mouse Wheel Settings")]
    [SerializeField, Min(0f)] private float mouseWheelScrollSensitivity = 0.1f;

    private InfantryLoadoutCustomization infantryLoadoutCustomization;
    private readonly List<GameObject> _buttonsList = new List<GameObject>();
    private Vector3 _originalWeaponsGadgetsPosition;
    private float _maxSliderY;
    private float _minScrollY;
    private bool _canScroll;


    private void Update()
    {
        HandleMouseWheelScroll();
    }


    public void Initialize(InfantryLoadoutCustomization parent)
    {
        infantryLoadoutCustomization = parent;
        _originalWeaponsGadgetsPosition = infantryLoadoutCustomization.weaponsGadgetsParent.localPosition;
        _minScrollY = infantryLoadoutCustomization.minScrollY;

        InitializeSlider();
    }

    public void RefreshLayoutOrigin()
    {
        _originalWeaponsGadgetsPosition = infantryLoadoutCustomization.weaponsGadgetsParent.localPosition;
        infantryLoadoutCustomization.minScrollY = _originalWeaponsGadgetsPosition.y;
        _minScrollY = infantryLoadoutCustomization.minScrollY;
    }

    private void InitializeSlider()
    {
        if (infantryLoadoutCustomization.weaponsGadgetsSlider == null) return;

        infantryLoadoutCustomization.weaponsGadgetsSlider.minValue = infantryLoadoutCustomization.sliderMinValue;
        infantryLoadoutCustomization.weaponsGadgetsSlider.maxValue = infantryLoadoutCustomization.sliderMaxValue;
        infantryLoadoutCustomization.weaponsGadgetsSlider.value = 0;
        infantryLoadoutCustomization.weaponsGadgetsSlider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    private void HandleMouseWheelScroll()
    {
        if (!_canScroll || infantryLoadoutCustomization == null) return;

        UnityEngine.UI.Slider slider = infantryLoadoutCustomization.weaponsGadgetsSlider;
        if (slider == null || !slider.isActiveAndEnabled ||
            !infantryLoadoutCustomization.weaponsGadgetsParent.gameObject.activeInHierarchy) return;

        InfantryLoadoutCustomization.SelectionStage stage = infantryLoadoutCustomization.GetCurrentStage();
        if (stage != InfantryLoadoutCustomization.SelectionStage.ItemSelection &&
            stage != InfantryLoadoutCustomization.SelectionStage.WeaponCustomization &&
            stage != InfantryLoadoutCustomization.SelectionStage.SkinSelection) return;

        float scrollDelta = Input.mouseScrollDelta.y;
        if (Mathf.Approximately(scrollDelta, 0f)) return;

        // Rolar para baixo aumenta o valor do slider e revela os itens inferiores.
        slider.normalizedValue = Mathf.Clamp01(
            slider.normalizedValue - scrollDelta * mouseWheelScrollSensitivity);
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

        ConfigureScrollForItemCount(_buttonsList.Count);
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
        if (AccountManager.Instance.selectedClass == ClassManager.Class.SquadLeader) return true;

        return weaponProperties.classWeapon.Any(c => c == infantryLoadoutCustomization._selectedClass);
    }

    private bool HasFactionAccessToWeapon(WeaponProperties weaponProperties)
    {
        if (AccountManager.Instance == null) return true;
        return weaponProperties.faction.Any(c => c == AccountManager.Instance.selectedFaction);
    }

    private bool HasClassAccessToGadget(Gadget gadget) => gadget.class_gadget.Any(c => c == infantryLoadoutCustomization._selectedClass);

    private void CreateWeaponButton(GameObject weapon, WeaponProperties wp, int index)
    {
        GameObject weaponButton = Instantiate(infantryLoadoutCustomization.buttonPrefab, infantryLoadoutCustomization.weaponsGadgetsParent);
        ApplyVerticalSpacing(weaponButton, index);

        var component = weaponButton.AddComponent<WeaponButtonComponents>();
        component.Initialize(wp.iconHud, weapon, wp, infantryLoadoutCustomization);

        _buttonsList.Add(weaponButton);
    }

    private void CreateGadgetButton(GameObject gadget, Gadget gd, int index)
    {
        GameObject gadgetButton = Instantiate(infantryLoadoutCustomization.buttonPrefab, infantryLoadoutCustomization.weaponsGadgetsParent);
        ApplyVerticalSpacing(gadgetButton, index);

        var component = gadgetButton.AddComponent<GadgetButtonComponents>();
        component.Initialize(gd.iconHud, gadget, gd, infantryLoadoutCustomization);

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
        GameObject comparisonWeapon = CreateEquippedWeaponComparison(item);
        if (infantryLoadoutCustomization._currentItemSelected != null) Destroy(infantryLoadoutCustomization._currentItemSelected);

        infantryLoadoutCustomization._currentItemSelected = InstantiateCurrentItem(item);
        SetupItemForCustomization(infantryLoadoutCustomization._currentItemSelected);

        InitializeAttachments(infantryLoadoutCustomization._currentItemSelected);

        WeaponProperties wp = infantryLoadoutCustomization._currentItemSelected.GetComponent<WeaponProperties>();
        if (wp != null)
        {
            infantryLoadoutCustomization.UpdateWeaponStats(wp);

            WeaponProperties equippedWeaponProperties = comparisonWeapon != null
                ? comparisonWeapon.GetComponent<WeaponProperties>()
                : null;
            if (equippedWeaponProperties != null)
                infantryLoadoutCustomization.uIUpdateManager.PreviewWeaponStats(equippedWeaponProperties, wp);
        }

        if (comparisonWeapon != null) Destroy(comparisonWeapon);

    }

    public void OnButtonClicked(GameObject item)
    {
        if (infantryLoadoutCustomization._currentItemSelected != null) Destroy(infantryLoadoutCustomization._currentItemSelected);

        infantryLoadoutCustomization._currentItemSelected = InstantiateCurrentItem(item);
        SetupItemForCustomization(infantryLoadoutCustomization._currentItemSelected);

        InitializeAttachments(infantryLoadoutCustomization._currentItemSelected);

        WeaponProperties wp = infantryLoadoutCustomization._currentItemSelected.GetComponent<WeaponProperties>();
        if (wp != null) infantryLoadoutCustomization.UpdateWeaponStats(wp);


        EquipItem(item);
        UpdateAllButtonOutlines();
    }

    private GameObject CreateEquippedWeaponComparison(GameObject hoveredItem)
    {
        GameObject equippedWeapon = GetEquippedWeaponForCurrentOption();
        if (equippedWeapon == null || equippedWeapon == hoveredItem) return null;

        GameObject comparisonWeapon = InstantiateCurrentItem(equippedWeapon);
        comparisonWeapon.name = $"{equippedWeapon.name} Comparison";
        comparisonWeapon.SetActive(false);
        InitializeAttachments(comparisonWeapon);
        return comparisonWeapon;
    }

    private GameObject GetEquippedWeaponForCurrentOption()
    {
        return infantryLoadoutCustomization.loadoutOptionManager.GetCurrentOption() switch
        {
            LoadoutOptionManager.LoadoutOption.PrimaryWeapon => infantryLoadoutCustomization.GetCurrentPrimaryWeapon(),
            LoadoutOptionManager.LoadoutOption.SecondaryWeapon => infantryLoadoutCustomization.GetCurrentSecondaryWeapon(),
            _ => null
        };
    }

    private static void InitializeAttachments(GameObject item)
    {
        AttatchmentManager attachmentManager = item != null ? item.GetComponent<AttatchmentManager>() : null;
        if (attachmentManager != null) attachmentManager.InitializeAttachments();
    }

    private GameObject InstantiateCurrentItem(GameObject item)
    {
        GameObject instance = Instantiate(
            item,
            infantryLoadoutCustomization.currentItemParent,
            false
        );

        // O Animator de algumas armas possui curvas no Transform raiz e restaura
        // a posicao gravada no prefab depois da instanciação. No preview ele nao
        // precisa executar, portanto deve ser desativado antes de aplicar o zero.
        foreach (Animator animator in instance.GetComponentsInChildren<Animator>(true))
            animator.enabled = false;

        instance.transform.localPosition = Vector3.zero;
        return instance;
    }

    private void SetupItemForCustomization(GameObject item)
    {
        int previewLayer = LayerMask.NameToLayer("LoadoutCustomization");
        if (previewLayer < 0) return;

        // Apply the preview layer to every child, including objects without a
        // MeshRenderer and equipment rendered by a SkinnedMeshRenderer.
        foreach (Transform child in item.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = previewLayer;
    }

    private void EquipItem(GameObject item)
    {
        if (infantryLoadoutCustomization.selectItemSfx != null && infantryLoadoutCustomization.selectItemSfx.clip != null) SoundManager.Play2dSoundLocal(infantryLoadoutCustomization.selectItemSfx.clip, infantryLoadoutCustomization.selectItemSfx.properties);

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
            if (gadgetComponent != null) gadgetComponent.UpdateOutlineState();

        }
    }

    private void OnSliderValueChanged(float value)
    {
        if (infantryLoadoutCustomization.GetCurrentStage() != InfantryLoadoutCustomization.SelectionStage.ItemSelection &&
            infantryLoadoutCustomization.GetCurrentStage() != InfantryLoadoutCustomization.SelectionStage.WeaponCustomization &&
            infantryLoadoutCustomization.GetCurrentStage() != InfantryLoadoutCustomization.SelectionStage.SkinSelection) return;

        float scrollY = Mathf.Lerp(infantryLoadoutCustomization.minScrollY, _maxSliderY, value);
        Vector3 newPosition = _originalWeaponsGadgetsPosition;
        newPosition.y = scrollY;
        infantryLoadoutCustomization.weaponsGadgetsParent.localPosition = newPosition;
    }

    public void ResetSlider()
    {
        if (infantryLoadoutCustomization.weaponsGadgetsSlider != null)
        {
            infantryLoadoutCustomization.weaponsGadgetsSlider.gameObject.SetActive(_canScroll);
            infantryLoadoutCustomization.weaponsGadgetsSlider.SetValueWithoutNotify(0f);
        }
        infantryLoadoutCustomization.weaponsGadgetsParent.localPosition = _originalWeaponsGadgetsPosition;
    }

    public void ConfigureScrollForItemCount(int itemCount)
    {
        RectTransform contentRect = infantryLoadoutCustomization.weaponsGadgetsParent as RectTransform;
        float viewportHeight = contentRect != null ? contentRect.rect.height : 0f;
        float itemSpacing = Mathf.Abs(infantryLoadoutCustomization.itemButtonSpacingY);
        float itemHeight = 104f;
        float contentHeight = itemCount > 0 ? (itemCount - 1) * itemSpacing + itemHeight : 0f;
        float scrollDistance = Mathf.Max(0f, contentHeight - viewportHeight);

        _minScrollY = _originalWeaponsGadgetsPosition.y;
        _maxSliderY = _minScrollY + scrollDistance;
        _canScroll = scrollDistance > 1f;

        ResetSlider();
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

    public void ClearAllWeaponsGadgetsChildren()
    {
        // Destroi todos os filhos
        foreach (Transform child in infantryLoadoutCustomization.weaponsGadgetsParent)
        {
            Destroy(child.gameObject);
        }

        _buttonsList.Clear();

        if (infantryLoadoutCustomization.skinSelectionManager != null) infantryLoadoutCustomization.skinSelectionManager.ClearSkinButtons();
        
    }
}
