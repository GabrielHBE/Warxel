using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerLoadoutCustomization : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera switchLoadoutCamera;

    [Header("Prefabs")]
    [SerializeField] private ClassManager classManager;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private GameObject removeItemButtonPrefab;
    [SerializeField] private GameObject classButtonPrefab;
    [SerializeField] private GameObject loadoutOptionButtonPrefab;

    [Header("UI Parents")]
    [SerializeField] private GameObject customization_buttons_parent;
    [SerializeField] private GameObject weaponStatusParent;
    [SerializeField] private Transform classesParent;
    [SerializeField] private Transform loadoutOptionsParent;
    [SerializeField] private Transform weaponsGadgetsParent;
    [SerializeField] private Transform currentItemParent;

    [Header("Loadout Lists")]
    [SerializeField] private GameObject[] primaryWeapons;
    [SerializeField] private GameObject[] secondaryWeapons;
    [SerializeField] private GameObject[] gadgets;

    [Space]

    [Header("UI Elements")]
    [SerializeField] private Image class_selection_image;
    [SerializeField] public static Sprite locked_item_image;
    [SerializeField] private Sprite lockedItemImage;
    [SerializeField] private GameObject backButton;
    [SerializeField] private GameObject updateLoadoutButton;
    [SerializeField] private TextMeshProUGUI currentSelectionText;
    [SerializeField] private Slider weaponsGadgetsSlider;

    [Header("Selection Outline")]
    [SerializeField] private Color selectedOutlineColor = Color.white;
    [SerializeField] private float outlineWidth = 5f;

    [Header("Weapon Customization Buttons")]
    [SerializeField] private GameObject customizeWeaponButton;
    [SerializeField] private GameObject customizeWeaponButtonBarrel;
    [SerializeField] private GameObject customizeWeaponButtonSight;
    [SerializeField] private GameObject customizeWeaponButtonMag;
    [SerializeField] private GameObject customizeWeaponButtonGrip;
    [SerializeField] private GameObject customizeWeaponButtonSideGrip;

    [Header("Weapon Stats Display")]
    [SerializeField] private TextMeshProUGUI rateOfFireText;
    [SerializeField] private TextMeshProUGUI adsSpeedText;
    [SerializeField] private TextMeshProUGUI playerSpeedModifierText;
    [SerializeField] private TextMeshProUGUI zoomText;
    [SerializeField] private TextMeshProUGUI fireModesText;
    [SerializeField] private TextMeshProUGUI destructionForceText;
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private TextMeshProUGUI minimumDamageText;
    [SerializeField] private TextMeshProUGUI vehicleBaseDamageText;
    [SerializeField] private TextMeshProUGUI headshotMultiplierText;
    [SerializeField] private TextMeshProUGUI damageDropoffText;
    [SerializeField] private TextMeshProUGUI damageDropoffTimerText;
    [SerializeField] private TextMeshProUGUI spreadIncreaserText;
    [SerializeField] private TextMeshProUGUI maxSpreadText;
    [SerializeField] private TextMeshProUGUI horizontalRecoilText;
    [SerializeField] private TextMeshProUGUI verticalRecoilText;
    [SerializeField] private TextMeshProUGUI firstShotRecoilIncreaserText;
    [SerializeField] private TextMeshProUGUI magCountText;
    [SerializeField] private TextMeshProUGUI bulletsPerMagText;
    [SerializeField] private TextMeshProUGUI reloadSpeedText;

    [Header("Layout Settings")]
    [SerializeField] private float classButtonSpacingX = 150f;
    [SerializeField] private float classButtonStartX = -300f;
    [SerializeField] private float classButtonY = 0f;
    [SerializeField] private float itemButtonSpacingY = -130f;
    [SerializeField] private float itemButtonStartY = 0;
    [SerializeField] private float itemButtonX = 0f;
    [SerializeField] private float loadoutOptionSpacingY = -80f;
    [SerializeField] private float loadoutOptionStartY = 100f;

    [Header("Slider Settings")]
    [SerializeField] private float sliderMinValue = 0f;
    [SerializeField] private float sliderMaxValue = 1f;
    [SerializeField] private float minScrollY = -200f;
    [SerializeField] private float maxScrollYIncreaser = 100f;

    // Enums
    public enum SelectionStage
    {
        ClassSelection,
        LoadoutOptionSelection,
        ItemSelection
    }

    private enum LoadoutOption
    {
        None,
        PrimaryWeapon,
        SecondaryWeapon,
        Gadget1
        // Gadget2 removido
    }

    private enum CustomizationPart
    {
        None,
        Sight,
        Barrel,
        Mag,
        Grip,
        SideGrip
    }

    // State variables
    public SelectionStage _currentStage = SelectionStage.ClassSelection;
    private LoadoutOption _currentLoadoutOption = LoadoutOption.None;
    private CustomizationPart _currentCustomizationPart = CustomizationPart.None;

    private ClassManager.Class _selectedClass;
    private GameObject _currentItemSelected;
    private GameObject _weaponBeingCustomized;

    private SwitchWeapon _switchWeapon;
    private PlayerProperties _playerProperties;
    private ClassLoadoutSaver loadoutSaver;


    // Adicione no início da classe, junto com as outras variáveis
    private Dictionary<ClassManager.Class, GameObject> _classButtons = new Dictionary<ClassManager.Class, GameObject>();
    private Color _normalButtonColor = Color.white;
    private Color _selectedButtonColor = Color.darkRed; // Você pode escolher a cor que preferir

    private readonly List<GameObject> _buttonsList = new List<GameObject>();
    private Vector3 _originalWeaponsGadgetsPosition;
    private float _maxSliderY;

    private bool IsCustomizingWeapon => _currentCustomizationPart != CustomizationPart.None;
    private bool IsWeaponSelected => _currentItemSelected != null;
    private bool IsPrimaryOrSecondaryWeapon =>
        _currentLoadoutOption == LoadoutOption.PrimaryWeapon ||
        _currentLoadoutOption == LoadoutOption.SecondaryWeapon;

    #region Unity Lifecycle

    private void Start()
    {
        InitializeComponents();
        InitializeUI();
        SetupButtonListeners();
    }

    private void Update()
    {
        UpdateWeaponStatusDisplay();
        UpdateCameraAndButtonVisibility();
        if (_currentStage == SelectionStage.ClassSelection)
        {
            class_selection_image.enabled = true;
        }
        else
        {
            class_selection_image.enabled = false;
        }
    }

    #endregion

    #region Initialization

    private void InitializeComponents()
    {
        locked_item_image = lockedItemImage;
        foreach (GameObject p in primaryWeapons)
        {
            WeaponProperties wp = p.GetComponent<WeaponProperties>();
            wp.Restart();
            InitializeAttatchments(wp);
            p.GetComponent<AttatchmentManager>().InitializeAttachments();
        }

        _switchWeapon = playerPrefab.GetComponentInChildren<SwitchWeapon>();
        _playerProperties = playerPrefab.GetComponent<PlayerProperties>();
        _selectedClass = _playerProperties.selected_class;
        if (loadoutSaver != null)
        {
            loadoutSaver.LoadLoadoutForClass(_selectedClass);
        }

        switchLoadoutCamera.enabled = false;
        _originalWeaponsGadgetsPosition = weaponsGadgetsParent.localPosition;

        loadoutSaver = gameObject.AddComponent<ClassLoadoutSaver>();

    }

    private void InitializeAttatchments(WeaponProperties wp)
    {
        Attatchment[] attatchments = wp.GetComponentsInChildren<Attatchment>(true);
        foreach (Attatchment a in attatchments)
        {
            a.Initialize(wp);
        }
    }

    private void InitializeUI()
    {
        InitializeSlider();

        backButton.SetActive(false);
        loadoutOptionsParent.gameObject.SetActive(false);
        weaponsGadgetsParent.gameObject.SetActive(false);
        weaponsGadgetsSlider.gameObject.SetActive(false);

        ShowClassSelection();
        UpdateClassButtonColors();

    }

    private void InitializeSlider()
    {
        if (weaponsGadgetsSlider == null)
        {
            return;
        }

        weaponsGadgetsSlider.minValue = sliderMinValue;
        weaponsGadgetsSlider.maxValue = sliderMaxValue;
        weaponsGadgetsSlider.value = 0;
        weaponsGadgetsSlider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    private void SetupButtonListeners()
    {
        SetupCustomizeButtonListener(customizeWeaponButton, OnCustomizeWeaponButtonClicked);
        SetupCustomizeButtonListener(customizeWeaponButtonSight, OnCustomizeSightButtonClicked);
        SetupCustomizeButtonListener(customizeWeaponButtonBarrel, OnCustomizeBarrelButtonClicked);
        SetupCustomizeButtonListener(customizeWeaponButtonMag, OnCustomizeMagButtonClicked);
        SetupCustomizeButtonListener(customizeWeaponButtonGrip, OnCustomizeGripButtonClicked);
        SetupCustomizeButtonListener(customizeWeaponButtonSideGrip, OnCustomizeSideGripButtonClicked);
    }

    private void SetupCustomizeButtonListener(GameObject button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
        {
            var btn = button.GetComponent<UnityEngine.UI.Button>();
            if (btn != null) btn.onClick.AddListener(action);
        }
    }

    #endregion

    #region UI Updates

    private void UpdateWeaponStatusDisplay()
    {
        if (IsWeaponSelected)
        {
            weaponStatusParent.SetActive(true);
            bool showCustomizeButtons = !IsCustomizingWeapon && IsPrimaryOrSecondaryWeapon;
            customizeWeaponButton.SetActive(showCustomizeButtons);

        }
        else
        {
            weaponStatusParent.SetActive(false);
            customizeWeaponButton.SetActive(false);
            SetCustomizeButtonsActive(false);
        }
    }

    private void UpdateCameraAndButtonVisibility()
    {
        if (_currentStage == SelectionStage.ClassSelection)
        {
            switchLoadoutCamera.enabled = false;
            updateLoadoutButton.SetActive(true);
        }
        else
        {
            switchLoadoutCamera.enabled = true;
            updateLoadoutButton.SetActive(false);
        }
    }

    private void SetCustomizeButtonsActive(bool active)
    {
        if (customization_buttons_parent != null)
        {
            customization_buttons_parent.SetActive(active);
        }
        /*
        if (customizeWeaponButtonBarrel != null) customizeWeaponButtonBarrel.SetActive(active);
        if (customizeWeaponButtonSight != null) customizeWeaponButtonSight.SetActive(active);
        if (customizeWeaponButtonMag != null) customizeWeaponButtonMag.SetActive(active);
        if (customizeWeaponButtonGrip != null) customizeWeaponButtonGrip.SetActive(active);
        if (customizeWeaponButtonSideGrip != null) customizeWeaponButtonSideGrip.SetActive(active);
        */
    }

    private void UpdateSelectionText(string text)
    {
        if (currentSelectionText != null)
            currentSelectionText.text = text;
    }

    #endregion

    #region Slider Handling

    private void OnSliderValueChanged(float value)
    {
        if (_currentStage != SelectionStage.ItemSelection) return;

        float scrollY = Mathf.Lerp(minScrollY, _maxSliderY, value);
        Vector3 newPosition = _originalWeaponsGadgetsPosition;
        newPosition.y = scrollY;
        weaponsGadgetsParent.localPosition = newPosition;
    }

    private void ResetSlider()
    {
        if (weaponsGadgetsSlider != null)
        {
            weaponsGadgetsSlider.gameObject.SetActive(true);
            weaponsGadgetsSlider.value = 0f;
        }
        weaponsGadgetsParent.localPosition = _originalWeaponsGadgetsPosition;
    }

    #endregion

    #region Class Selection

    private void ShowClassSelection()
    {
        _currentStage = SelectionStage.ClassSelection;
        ClearAllButtons();
        UpdateSelectionText("Selecione sua Classe");

        int classIndex = 0;
        foreach (ClassManager.Class classType in Enum.GetValues(typeof(ClassManager.Class)))
        {
            CreateClassButton(classType, classIndex);
            classIndex++;
        }
    }

    private void CreateClassButton(ClassManager.Class classType, int index)
    {
        GameObject classButton = Instantiate(classButtonPrefab, classesParent);

        // Position
        float xPosition = classButtonStartX + (index * classButtonSpacingX);
        RectTransform rectTransform = classButton.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = new Vector2(xPosition, classButtonY);
        }

        // Text
        TextMeshProUGUI buttonText = classButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null) buttonText.text = classManager.GetClassName(classType);

        // Click event
        UnityEngine.UI.Button button = classButton.GetComponent<UnityEngine.UI.Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => SelectClass(classType));
        }

        // Armazena o botão no dicionário
        _classButtons[classType] = classButton;

        // Configura a imagem do botão para poder mudar a cor
        Image buttonImage = classButton.GetComponent<Image>();
        if (buttonImage != null)
        {
            // Salva a cor original se for o primeiro botão
            if (_classButtons.Count == 1)
                _normalButtonColor = buttonImage.color;
        }

        _buttonsList.Add(classButton);
    }

    public void SelectClass(ClassManager.Class @class)
    {
        _selectedClass = @class;
        _playerProperties.selected_class = @class;
        Gadget gadget2_class = classManager.GetClassGadget(@class);
        if (gadget2_class != null) _switchWeapon.gadget2 = gadget2_class.gameObject;


        // Carrega o loadout da classe selecionada
        if (loadoutSaver != null)
        {
            loadoutSaver.LoadLoadoutForClass(@class);
        }

        // Atualiza as cores dos botões de classe
        UpdateClassButtonColors();

        UpdateSelectionText($"Classe: {@class}");
    }

    private void UpdateClassButtonColors()
    {
        foreach (var kvp in _classButtons)
        {
            ClassManager.Class buttonClass = kvp.Key;
            GameObject buttonObj = kvp.Value;

            if (buttonObj == null) continue;

            Image buttonImage = buttonObj.GetComponent<Image>();
            if (buttonImage == null) continue;

            // Define a cor baseado se é a classe selecionada
            if (buttonClass == _selectedClass)
            {
                buttonImage.color = _selectedButtonColor;
            }
            else
            {
                buttonImage.color = _normalButtonColor;
            }
        }
    }

    #endregion

    #region Loadout Options

    public void ShowLoadoutOptions()
    {
        _currentStage = SelectionStage.LoadoutOptionSelection;
        backButton.SetActive(true);

        ClearAllButtons();
        loadoutOptionsParent.gameObject.SetActive(true);

        UpdateSelectionText($"Selecione o que deseja alterar - {_selectedClass}");

        CreateLoadoutOptionButton("Arma Primária", LoadoutOption.PrimaryWeapon, 0);
        CreateLoadoutOptionButton("Arma Secundária", LoadoutOption.SecondaryWeapon, 1);
        CreateLoadoutOptionButton("Gadget", LoadoutOption.Gadget1, 2);
        // Gadget 2 removido
    }

    private void CreateLoadoutOptionButton(string buttonName, LoadoutOption option, int index)
    {
        GameObject optionButton = Instantiate(loadoutOptionButtonPrefab, loadoutOptionsParent);

        float yPosition = loadoutOptionStartY + (index * loadoutOptionSpacingY);
        RectTransform rectTransform = optionButton.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = new Vector2(0, yPosition);
        }

        TextMeshProUGUI buttonText = optionButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null) buttonText.text = buttonName;

        UnityEngine.UI.Button button = optionButton.GetComponent<UnityEngine.UI.Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => OnLoadoutOptionSelected(option));
        }

        _buttonsList.Add(optionButton);
    }

    public void SaveCurrentLoadout()
    {
        if (loadoutSaver != null)
        {
            loadoutSaver.SaveCurrentLoadout(_selectedClass);
        }
    }

    private void OnLoadoutOptionSelected(LoadoutOption option)
    {
        _currentLoadoutOption = option;
        _currentStage = SelectionStage.ItemSelection;

        loadoutOptionsParent.gameObject.SetActive(false);
        ShowAvailableItems(option);

        ResetSlider();

        string optionName = GetLoadoutOptionDisplayName(option);
        UpdateSelectionText($"Selecionando: {optionName} - {_selectedClass}");
    }

    private string GetLoadoutOptionDisplayName(LoadoutOption option)
    {
        return option switch
        {
            LoadoutOption.PrimaryWeapon => "Arma Primária",
            LoadoutOption.SecondaryWeapon => "Arma Secundária",
            LoadoutOption.Gadget1 => "Gadget",
            _ => option.ToString()
        };
    }

    #endregion

    #region Item Display

    private void ShowAvailableItems(LoadoutOption option)
    {
        ClearAllButtons();
        weaponsGadgetsParent.gameObject.SetActive(true);
        _maxSliderY = minScrollY;

        switch (option)
        {
            case LoadoutOption.PrimaryWeapon:
                ShowPrimaryWeaponsForClass();
                break;
            case LoadoutOption.SecondaryWeapon:
                ShowSecondaryWeaponsForClass();
                break;
            case LoadoutOption.Gadget1:
                ShowGadgets(); // Removido o parâmetro de slot
                break;
        }

        // Atualiza os outlines depois de criar todos os botões
        UpdateAllButtonOutlines();
    }

    private void ShowPrimaryWeaponsForClass()
    {
        int weaponIndex = 0;
        foreach (GameObject weapon in primaryWeapons)
        {
            WeaponProperties wp = weapon.GetComponent<WeaponProperties>();
            if (HasClassAccessToWeapon(wp))
            {
                CreateWeaponButton(weapon, wp, weaponIndex);
                weaponIndex++;
            }
        }
    }

    private void ShowSecondaryWeaponsForClass()
    {
        int weaponIndex = 0;
        foreach (GameObject weapon in secondaryWeapons)
        {
            WeaponProperties wp = weapon.GetComponent<WeaponProperties>();
            if (HasClassAccessToWeapon(wp))
            {
                CreateWeaponButton(weapon, wp, weaponIndex);
                weaponIndex++;
            }
        }
    }

    private bool HasClassAccessToWeapon(WeaponProperties weaponProperties)
    {
        return weaponProperties.class_weapon.Any(c => c == _selectedClass);
    }


    private void ShowGadgets()
    {
        int gadgetIndex = 0;
        foreach (GameObject gadget in gadgets)
        {
            Gadget gd = gadget.GetComponent<Gadget>();
            if (gd == null) continue;

            if (!HasClassAccessToGadget(gd)) continue;

            CreateGadgetButton(gadget, gd, gadgetIndex);
            gadgetIndex++;
        }
    }

    private bool HasClassAccessToGadget(Gadget gadget)
    {
        return gadget.class_gadget.Any(c => c == _selectedClass);
    }

    private void CreateWeaponButton(GameObject weapon, WeaponProperties wp, int index)
    {
        _maxSliderY += maxScrollYIncreaser;

        GameObject weaponButton = Instantiate(buttonPrefab, weaponsGadgetsParent);
        ApplyVerticalSpacing(weaponButton, index);

        var component = weaponButton.AddComponent<WeaponButtonComponents>();
        component.Initialize(wp.icon_hud, weapon, wp);

        _buttonsList.Add(weaponButton);
    }

    private void CreateGadgetButton(GameObject gadget, Gadget gd, int index)
    {
        GameObject gadgetButton = Instantiate(buttonPrefab, weaponsGadgetsParent);
        ApplyVerticalSpacing(gadgetButton, index);

        var component = gadgetButton.AddComponent<GadgetButtonComponents>();
        component.Initialize(gd.icon_hud, gadget, gd);

        _buttonsList.Add(gadgetButton);
    }

    private void ApplyVerticalSpacing(GameObject button, int index)
    {
        RectTransform rectTransform = button.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            float yPosition = itemButtonStartY + (index * itemButtonSpacingY);
            rectTransform.anchoredPosition = new Vector2(itemButtonX, yPosition);
        }
    }

    #endregion

    #region Item Selection

    public void OnButtonMouseEnter(GameObject item)
    {
        if (_currentItemSelected != null) Destroy(_currentItemSelected);

        _currentItemSelected = Instantiate(item, currentItemParent);
        SetupItemForCustomization(_currentItemSelected);

        WeaponProperties wp = item.GetComponent<WeaponProperties>();
        if (wp != null) UpdateWeaponStats(wp);
    }



    public void OnButtonClicked(GameObject item)
    {
        if (_currentItemSelected != null) Destroy(_currentItemSelected);

        _currentItemSelected = Instantiate(item, currentItemParent);
        SetupItemForCustomization(_currentItemSelected);

        WeaponProperties wp = item.GetComponent<WeaponProperties>();
        if (wp != null) UpdateWeaponStats(wp);

        EquipItem(item);

        // Atualiza os outlines após equipar
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
        switch (_currentLoadoutOption)
        {
            case LoadoutOption.PrimaryWeapon:
                EquipPrimaryWeapon(item);
                break;
            case LoadoutOption.SecondaryWeapon:
                EquipSecondaryWeapon(item);
                break;
            case LoadoutOption.Gadget1:
                EquipGadget(item);
                break;
        }

        SaveCurrentLoadout();
    }

    private void EquipPrimaryWeapon(GameObject weapon)
    {
        WeaponProperties wp = weapon.GetComponent<WeaponProperties>();
        if (wp != null)
        {
            _switchWeapon.primary = weapon;
        }
    }

    private void EquipSecondaryWeapon(GameObject weapon)
    {
        WeaponProperties wp = weapon.GetComponent<WeaponProperties>();
        if (wp != null)
        {
            _switchWeapon.secondary = weapon;
        }
    }

    private void EquipGadget(GameObject gadget)
    {
        Gadget gd = gadget.GetComponent<Gadget>();
        if (gd != null)
        {
            _switchWeapon.gadget1 = gadget;
            //_switchWeapon.gadget2 = null; // Garante que gadget2 fica vazio
        }
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

    #endregion

    #region Weapon Customization

    public void OnCustomizeWeaponButtonClicked()
    {
        if (_currentItemSelected == null) return;

        _weaponBeingCustomized = _currentItemSelected;
        _currentCustomizationPart = CustomizationPart.None;

        ClearWeaponsGadgetsChildren();

        if (weaponsGadgetsSlider != null)
            weaponsGadgetsSlider.gameObject.SetActive(false);

        customizeWeaponButton.SetActive(false);
        SetCustomizeButtonsActive(true);

        WeaponProperties wp = _weaponBeingCustomized.GetComponent<WeaponProperties>();
        string weaponName = wp != null ? wp.weapon_name : _weaponBeingCustomized.name;
        UpdateSelectionText($"Customizando: {weaponName}");
    }

    public void OnCustomizeSightButtonClicked()
    {
        _currentCustomizationPart = CustomizationPart.Sight;
        CreateCustomizationButtons<Sight>(
            "Mira",
            sight => sight.icon_hud,
            sight => sight.gameObject.name
        );
        UpdateSelectionText("Selecione uma mira");
    }

    public void OnCustomizeBarrelButtonClicked()
    {
        _currentCustomizationPart = CustomizationPart.Barrel;
        CreateCustomizationButtons<Barrel>(
            "Cano",
            barrel => barrel.icon_hud,
            barrel => barrel.gameObject.name
        );
        UpdateSelectionText("Selecione um cano");
    }

    public void OnCustomizeMagButtonClicked()
    {
        _currentCustomizationPart = CustomizationPart.Mag;
        CreateCustomizationButtons<Mag>(
            "Carregador",
            mag => mag.icon_hud,
            mag => mag.gameObject.name
        );
        UpdateSelectionText("Selecione um carregador");
    }

    public void OnCustomizeGripButtonClicked()
    {
        _currentCustomizationPart = CustomizationPart.Grip;
        CreateCustomizationButtons<Grip>(
            "Empunhadura",
            grip => grip.icon_hud,
            grip => grip.gameObject.name
        );
        UpdateSelectionText("Selecione uma empunhadura");
    }

    public void OnCustomizeSideGripButtonClicked()
    {
        _currentCustomizationPart = CustomizationPart.SideGrip;
        CreateCustomizationButtons<SideGrip>(
            "Empunhadura Lateral",
            sidegrip => sidegrip.icon_hud,
            sidegrip => sidegrip.gameObject.name
        );
        UpdateSelectionText("Selecione uma empunhadura lateral");
    }

    private void CreateCustomizationButtons<T>(string partName,
                                       Func<T, Sprite> getIcon,
                                       Func<T, string> getName) where T : MonoBehaviour
    {
        ClearWeaponsGadgetsChildren();
        weaponsGadgetsParent.gameObject.SetActive(true);
        _maxSliderY = minScrollY;

        if (_weaponBeingCustomized == null) return;

        T[] components = _weaponBeingCustomized.GetComponentsInChildren<T>(true);

        // VERIFICAÇÃO CORRETA SEM CRIAR GAMEOBJECT
        // Cria o botão de remover apenas para tipos que não são Mag
        if (typeof(T) != typeof(Mag))
        {
            CreateRemoveButton<T>(partName);
        }

        // Add part buttons (começando do índice apropriado)
        int componentIndex = typeof(T) != typeof(Mag) ? 1 : 0;
        foreach (T component in components)
        {
            _maxSliderY += maxScrollYIncreaser;
            CreateCustomizationPartButton(component, getIcon(component), getName(component), partName, componentIndex);
            componentIndex++;
        }

        ShowSliderIfNeeded(components.Length + (typeof(T) != typeof(Mag) ? 1 : 0));
    }

    private void CreateRemoveButton<T>(string partType) where T : MonoBehaviour
    {
        GameObject removeButton = Instantiate(removeItemButtonPrefab, weaponsGadgetsParent);
        ApplyVerticalSpacing(removeButton, 0); // Índice 0 = primeiro botão

        // Adiciona o componente de botão de remover
        var removeComponent = removeButton.AddComponent<RemoveAttachmentButtonComponents>();
        removeComponent.Initialize(partType, _weaponBeingCustomized, this);

        _buttonsList.Add(removeButton);
    }

    public void RemoveAttachment(string partType, GameObject weaponBeingCustomized)
    {
        if (weaponBeingCustomized == null) return;

        WeaponProperties weaponProps = weaponBeingCustomized.GetComponent<WeaponProperties>();
        if (weaponProps == null) return;

        // Determina o tipo do attachment baseado no partType
        Type targetType = null;
        switch (partType)
        {
            case "Mira":
                targetType = typeof(Sight);
                break;
            case "Cano":
                targetType = typeof(Barrel);
                break;
            case "Carregador":
                targetType = typeof(Mag);
                break;
            case "Empunhadura":
                targetType = typeof(Grip);
                break;
            case "Empunhadura Lateral":
                targetType = typeof(SideGrip);
                break;
        }

        if (targetType == null) return;

        // Primeiro, atualiza o AttachmentManager (igual no método de adicionar)
        AttatchmentManager attachmentManager = weaponBeingCustomized.GetComponent<AttatchmentManager>();

        if (targetType == typeof(Grip))
            attachmentManager.RemoveGrip(weaponProps);
        else if (targetType == typeof(Barrel))
            attachmentManager.RemoveBarrel(weaponProps);
        else if (targetType == typeof(Sight))
            attachmentManager.RemoveSight(weaponProps);
        else if (targetType == typeof(Mag))
            attachmentManager.RemoveMag(weaponProps);
        else if (targetType == typeof(SideGrip))
            attachmentManager.RemoveSideGrip(weaponProps);

        // Atualiza no prefab (seguindo o padrão do UpdateWeaponPartInPrefab)
        UpdateWeaponPartInPrefabRemove(targetType, weaponProps);

        // Atualiza na instância atual
        UpdateWeaponPartInInstanceRemove(targetType);

        // Atualiza as estatísticas da arma
        UpdateWeaponStats(weaponProps);

        // Atualiza os outlines dos attachments
        UpdateAttachmentOutlines();

        // Salva o loadout
        SaveCurrentLoadout();
    }


    private void UpdateWeaponPartInPrefabRemove(Type targetType, WeaponProperties weaponProps)
    {
        // Procura nos arrays de armas primárias e secundárias
        foreach (GameObject weapon in primaryWeapons)
        {
            WeaponProperties wp = weapon.GetComponent<WeaponProperties>();
            if (wp == null || wp.weapon_name != weaponProps.weapon_name) continue;

            Component[] existingParts = weapon.GetComponentsInChildren(targetType, true);
            foreach (Component part in existingParts)
            {
                part.gameObject.SetActive(false);

                // Atualiza o AttachmentManager no prefab também
                UpdateAttachmentControllerRemove(part, wp);
            }

            // Se for a arma primária ou secundária atual, atualiza também no SwitchWeapon
            if (_switchWeapon.primary != null &&
                _switchWeapon.primary.GetComponent<WeaponProperties>().weapon_name == weaponProps.weapon_name)
            {
                // Recarrega a arma primária com o prefab atualizado
                _switchWeapon.primary = weapon;
            }
            else if (_switchWeapon.secondary != null &&
                     _switchWeapon.secondary.GetComponent<WeaponProperties>().weapon_name == weaponProps.weapon_name)
            {
                // Recarrega a arma secundária com o prefab atualizado
                _switchWeapon.secondary = weapon;
            }

            break;
        }
    }

    private void UpdateWeaponPartInInstanceRemove(Type targetType)
    {
        if (_weaponBeingCustomized == null) return;

        Component[] existingParts = _weaponBeingCustomized.GetComponentsInChildren(targetType, true);
        foreach (Component part in existingParts)
        {
            part.gameObject.SetActive(false);

            // Atualiza o AttachmentManager na instância também
            WeaponProperties weaponProps = _weaponBeingCustomized.GetComponent<WeaponProperties>();
            UpdateAttachmentControllerRemove(part, weaponProps);
        }
    }

    private void UpdateAttachmentControllerRemove(Component part, WeaponProperties weaponProps)
    {
        // Chama os métodos apropriados do AttachmentManager para remover
        AttatchmentManager attachmentManager = weaponProps.GetComponent<AttatchmentManager>();

        print("To removendo no prefab");

        if (part is Sight)
            attachmentManager.RemoveSight(weaponProps);
        else if (part is Barrel)
            attachmentManager.RemoveBarrel(weaponProps);
        else if (part is Mag)
            attachmentManager.RemoveMag(weaponProps);
        else if (part is Grip)
            attachmentManager.RemoveGrip(weaponProps);
        else if (part is SideGrip)
            attachmentManager.RemoveSideGrip(weaponProps);
    }


    public void UpdateAttachmentOutlines()
    {
        foreach (GameObject button in _buttonsList)
        {
            if (button == null) continue;

            var customizationComponent = button.GetComponent<CustomizationButtonComponents>();
            if (customizationComponent != null)
            {
                customizationComponent.UpdateOutlineState();
            }
        }
    }

    private void CreateCustomizationPartButton<T>(T component, Sprite icon, string name, string partType, int index)
    where T : MonoBehaviour
    {
        GameObject button = Instantiate(buttonPrefab, weaponsGadgetsParent);
        ApplyVerticalSpacing(button, index);

        var buttonComponent = button.AddComponent<CustomizationButtonComponents>();
        buttonComponent.Initialize(icon, component.gameObject, component, partType, name, false);

        _buttonsList.Add(button);
    }

    private void ShowSliderIfNeeded(int itemCount)
    {
        if (weaponsGadgetsSlider != null && itemCount > 0)
        {
            weaponsGadgetsSlider.gameObject.SetActive(true);
            weaponsGadgetsSlider.value = 0f;
            weaponsGadgetsParent.localPosition = _originalWeaponsGadgetsPosition;
        }
    }

    public void OnCustomizationItemClicked(GameObject partObject, MonoBehaviour component, string partType)
    {
        Type targetType = GetTargetTypeFromCustomizationPart();
        WeaponProperties weaponProps = _weaponBeingCustomized.GetComponent<WeaponProperties>();

        bool is_max_points_reached = weaponProps.current_attachment_points + partObject.GetComponent<Attatchment>().attatchment_points == 100;


        if (_weaponBeingCustomized == null || targetType == null || is_max_points_reached) return;

        // Update in prefab
        UpdateWeaponPartInPrefab(partObject, targetType, weaponProps);

        // Update in current instance
        UpdateWeaponPartInInstance(partObject, targetType);

        // Atualiza os outlines dos attachments
        UpdateAttachmentOutlines();

        // Salva o loadout após modificar attachments
        SaveCurrentLoadout();
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
            _ => null
        };
    }

    private void UpdateWeaponPartInPrefab(GameObject partObject, Type targetType, WeaponProperties weaponProps)
    {
        foreach (GameObject primary in primaryWeapons)
        {
            WeaponProperties primaryProps = primary.GetComponent<WeaponProperties>();
            if (primaryProps.weapon_name == weaponProps.weapon_name)
            {
                Component[] existingParts = primary.GetComponentsInChildren(targetType, true);
                foreach (Component part in existingParts)
                {
                    bool isSelectedPart = part.gameObject.name == partObject.name;
                    part.gameObject.SetActive(isSelectedPart);

                    if (isSelectedPart)
                    {
                        UpdateAttachmentController(part, primaryProps);

                        // Se for a arma primária atual, atualiza também no SwitchWeapon
                        if (_switchWeapon.primary != null &&
                            _switchWeapon.primary.GetComponent<WeaponProperties>().weapon_name == weaponProps.weapon_name)
                        {
                            EquipPrimaryWeapon(primary);
                        }

                        UpdateWeaponStats(primaryProps);
                    }
                }
                break;
            }
        }
    }

    private void UpdateAttachmentController(Component part, WeaponProperties weaponProps)
    {
        if (part is Sight sight)
            weaponProps.GetComponent<AttatchmentManager>().UpdateSight(sight, weaponProps);
        else if (part is Barrel barrel)
            //_attachmentManager.UpdateBarrel(barrel, weaponProps);
            weaponProps.GetComponent<AttatchmentManager>().UpdateBarrel(barrel, weaponProps);
        else if (part is Mag mag)
            //_attachmentManager.UpdateMag(mag, weaponProps);
            weaponProps.GetComponent<AttatchmentManager>().UpdateMag(mag, weaponProps);
        else if (part is Grip grip)
            //_attachmentManager.UpdateGrip(grip, weaponProps);
            weaponProps.GetComponent<AttatchmentManager>().UpdateGrip(grip, weaponProps);
        else if (part is SideGrip sidegrip)
            //_attachmentManager.UpdateGrip(grip, weaponProps);
            weaponProps.GetComponent<AttatchmentManager>().UpdateSideGrip(sidegrip, weaponProps);
    }

    private void UpdateWeaponPartInInstance(GameObject partObject, Type targetType)
    {
        Component[] existingParts = _weaponBeingCustomized.GetComponentsInChildren(targetType, true);
        foreach (Component part in existingParts)
        {
            part.gameObject.SetActive(part.gameObject == partObject);
        }
    }

    #endregion

    #region Navigation

    public void OnBackButtonClicked()
    {
        if (IsCustomizingWeapon)
        {
            OnBackFromCustomization();
            return;
        }

        if (_currentStage != SelectionStage.ItemSelection && _currentItemSelected != null)
        {
            Destroy(_currentItemSelected);
        }

        if (weaponsGadgetsSlider != null)
            weaponsGadgetsSlider.gameObject.SetActive(false);

        switch (_currentStage)
        {
            case SelectionStage.LoadoutOptionSelection:
                _maxSliderY = minScrollY;
                _currentStage = SelectionStage.ClassSelection;
                loadoutOptionsParent.gameObject.SetActive(false);
                backButton.SetActive(false);
                ShowClassSelection();
                UpdateClassButtonColors(); // Garante que a classe selecionada mantenha a cor
                break;

            case SelectionStage.ItemSelection:
                _currentStage = SelectionStage.LoadoutOptionSelection;
                weaponsGadgetsParent.gameObject.SetActive(false);
                ClearItemButtons();
                ShowLoadoutOptions();
                break;
        }
    }

    private void OnBackFromCustomization()
    {
        _currentCustomizationPart = CustomizationPart.None;
        _weaponBeingCustomized = null;

        ClearWeaponsGadgetsChildren();
        weaponsGadgetsParent.gameObject.SetActive(false);
        SetCustomizeButtonsActive(false);

        if (weaponsGadgetsSlider != null)
            weaponsGadgetsSlider.gameObject.SetActive(false);

        _currentStage = SelectionStage.ItemSelection;

        if (currentSelectionText != null && _currentItemSelected != null)
        {
            WeaponProperties wp = _currentItemSelected.GetComponent<WeaponProperties>();
            string weaponName = wp != null ? wp.weapon_name : _currentItemSelected.name;
            UpdateSelectionText($"Selecionando: {GetLoadoutOptionDisplayName(_currentLoadoutOption)} - {_selectedClass}");
        }

        // Reativa os outlines dos itens principais
        UpdateAllButtonOutlines();
    }

    #endregion

    #region Utility Methods

    private void ClearAllButtons()
    {
        foreach (GameObject button in _buttonsList)
        {
            if (button != null) Destroy(button);
        }
        _buttonsList.Clear();
        _classButtons.Clear(); // Limpa o dicionário de botões de classe
    }

    private void ClearWeaponsGadgetsChildren()
    {
        foreach (Transform child in weaponsGadgetsParent)
        {
            Destroy(child.gameObject);
        }
        _buttonsList.Clear();
    }

    private void ClearItemButtons()
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

    public void UpdateWeaponStats(WeaponProperties wp)
    {
        if (wp == null) return;

        wp.CalculateMedia();

        rateOfFireText.text = wp.rate_of_fire.ToString("F0") + " RPM";
        adsSpeedText.text = wp.ads_speed.ToString("F2") + "s";
        playerSpeedModifierText.text = wp.speed_change.ToString("F0");
        zoomText.text = "x" + wp.zoom.ToString("F1");

        fireModesText.text = string.Join(" / ", wp.fire_modes);

        destructionForceText.text = wp.destruction_force.ToString("F0");
        damageText.text = wp.damage.ToString("F1");
        minimumDamageText.text = wp.minimum_damage.ToString("F1");
        vehicleBaseDamageText.text = wp.vehicle_damage.ToString("F1");
        headshotMultiplierText.text = wp.headshot_multiplier.ToString("F1");
        damageDropoffText.text = wp.damage_dropoff.ToString("F0") + "%";
        damageDropoffTimerText.text = wp.damage_dropoff_timer.ToString("F2") + "s";
        spreadIncreaserText.text = wp.spread_increaser.ToString("F2");
        maxSpreadText.text = wp.max_spread.ToString("F2");
        horizontalRecoilText.text = wp.horizontal_recoil_media.ToString("F2");
        verticalRecoilText.text = wp.vertical_recoil_media.ToString("F2");
        firstShotRecoilIncreaserText.text = "x" + wp.first_shoot_increaser.ToString("F1");
        magCountText.text = wp.mag_count.ToString();
        bulletsPerMagText.text = wp.bullets_per_mag.ToString();
        reloadSpeedText.text = wp.reload_time.ToString("F2") + "s";
    }

    // Public getters
    public GameObject GetCurrentPrimaryWeapon() => _switchWeapon.primary;
    public GameObject GetCurrentSecondaryWeapon() => _switchWeapon.secondary;
    public GameObject GetCurrentGadget() => _switchWeapon.gadget1; // Renomeado para refletir apenas um gadget

    public Sprite GetGadgetIcon(GameObject gadget) => gadget.GetComponent<Gadget>().icon_hud;
    public Sprite GetWeaponIcon(GameObject weapon) => weapon.GetComponent<WeaponProperties>().icon_hud;

    #endregion

    #region Classes

    private class GadgetButtonComponents : MonoBehaviour
    {
        private Sprite _imageHud;
        private GameObject _gadgetGameObject;
        private Gadget _gadget;
        private PlayerLoadoutCustomization _parent;
        private Outline _outline;

        public void Initialize(Sprite imageHud, GameObject gadgetGameObject, Gadget gadget)
        {
            _imageHud = imageHud;
            _gadgetGameObject = gadgetGameObject;
            _gadget = gadget;
            _parent = GetComponentInParent<PlayerLoadoutCustomization>();

            SetupImage();
            SetupOutline();
            SetupEvents();
            UpdateOutlineState();
        }

        private void SetupImage()
        {

            Image[] allImages = GetComponentsInChildren<Image>(true);
            if (allImages != null && allImages.Length > 0)
            {
                /*
                if (_imageHud == null)
                {
                    allImages[allImages.Length - 1].sprite = PlayerLoadoutCustomization.locked_item_image;
                }
                else
                {
                    allImages[allImages.Length - 1].sprite = _imageHud;
                }
                */

                allImages[allImages.Length - 1].sprite = _imageHud;

            }
        }

        private void SetupOutline()
        {
            _outline = gameObject.AddComponent<Outline>();
            _outline.effectColor = Color.white; // Cor da borda
            //_outline.effectDistance = new Vector2(3f, 3f); // Tamanho da borda

            // Configuração IMPORTANTE: Use apenas o outline, sem modificar a cor da imagem
            _outline.useGraphicAlpha = true; // Mantém o alpha da imagem original
            _outline.enabled = false;
        }
        private void SetupEvents()
        {


            var eventTrigger = gameObject.AddComponent<EventTrigger>();

            AddEventTrigger(eventTrigger, EventTriggerType.PointerEnter,
                () => _parent?.OnButtonMouseEnter(_gadgetGameObject));
            /*
        AddEventTrigger(eventTrigger, EventTriggerType.PointerExit,
            () => _parent?.OnButtonMouseExit());
            */
            AddEventTrigger(eventTrigger, EventTriggerType.PointerClick, () => _parent?.OnButtonClicked(_gadgetGameObject));
        }

        private void AddEventTrigger(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }

        public void UpdateOutlineState()
        {
            if (_parent == null) return;

            bool isSelected = _parent.GetCurrentGadget() == _gadgetGameObject; // Verifica apenas gadget1

            if (_outline != null)
                _outline.enabled = isSelected;
        }
    }

    private class WeaponButtonComponents : MonoBehaviour
    {
        private Sprite _imageHud;
        private GameObject _weaponGameObject;
        private WeaponProperties _weaponProperties;
        private PlayerLoadoutCustomization _parent;
        private Outline _outline;

        public void Initialize(Sprite imageHud, GameObject weaponGameObject, WeaponProperties weaponProperties)
        {
            _imageHud = imageHud;
            _weaponGameObject = weaponGameObject;
            _weaponProperties = weaponProperties;
            _parent = GetComponentInParent<PlayerLoadoutCustomization>();

            SetupImage();
            SetupText();
            SetupOutline();
            SetupEvents();
            UpdateOutlineState();
        }

        private void SetupImage()
        {

            Image[] allImages = GetComponentsInChildren<Image>(true);
            if (allImages != null && allImages.Length > 0)
            {
                /*
                if (_imageHud == null)
                {
                    allImages[allImages.Length - 1].sprite = PlayerLoadoutCustomization.locked_item_image;
                }
                else
                {
                    allImages[allImages.Length - 1].sprite = _imageHud;
                }
                */

                allImages[allImages.Length - 1].sprite = _imageHud;

            }
        }

        private void SetupText()
        {
            TextMeshProUGUI buttonText = GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null) buttonText.text = _weaponProperties.weapon_name;
        }

        private void SetupOutline()
        {
            _outline = gameObject.AddComponent<Outline>();
            _outline.effectColor = _parent.selectedOutlineColor;
            _outline.effectDistance = new Vector2(_parent.outlineWidth, _parent.outlineWidth);
            _outline.enabled = false;
        }

        private void SetupEvents()
        {
            var eventTrigger = gameObject.AddComponent<EventTrigger>();

            AddEventTrigger(eventTrigger, EventTriggerType.PointerEnter,
                () => _parent?.OnButtonMouseEnter(_weaponGameObject));
            /*
            AddEventTrigger(eventTrigger, EventTriggerType.PointerExit,
                () => _parent?.OnButtonMouseExit());
            */
            AddEventTrigger(eventTrigger, EventTriggerType.PointerClick, () => _parent?.OnButtonClicked(_weaponGameObject));
        }

        private void AddEventTrigger(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }

        public void UpdateOutlineState()
        {
            if (_parent == null) return;

            bool isSelected = false;

            switch (_parent._currentLoadoutOption)
            {
                case LoadoutOption.PrimaryWeapon:
                    isSelected = _parent.GetCurrentPrimaryWeapon() == _weaponGameObject;
                    break;
                case LoadoutOption.SecondaryWeapon:
                    isSelected = _parent.GetCurrentSecondaryWeapon() == _weaponGameObject;
                    break;
            }

            if (_outline != null)
                _outline.enabled = isSelected;
        }
    }

    private class CustomizationButtonComponents : MonoBehaviour
    {
        private Sprite _imageHud;
        private GameObject _partGameObject;
        private MonoBehaviour _component;
        private string _partType;
        private string _partName;
        private PlayerLoadoutCustomization _parent;
        private Outline _outline;
        private bool _isRemoveButton;
        private bool is_attatchment_unlocked;

        public void Initialize(Sprite imageHud, GameObject partGameObject, MonoBehaviour component,
                              string partType, string partName, bool isRemoveButton = false)
        {
            _imageHud = imageHud;
            _partGameObject = partGameObject;
            _component = component;
            _partType = partType;
            _partName = partName;
            _parent = GetComponentInParent<PlayerLoadoutCustomization>();
            _isRemoveButton = isRemoveButton;
            is_attatchment_unlocked = component.GetComponent<Attatchment>().is_attatchment_unlocked;

            SetupImage();
            SetupText();
            SetupOutline();
            SetupEvents();
            UpdateOutlineState();
        }

        private void SetupImage()
        {

            Image[] allImages = GetComponentsInChildren<Image>(true);
            if (allImages != null && allImages.Length > 0)
            {
                if (!is_attatchment_unlocked)
                {
                    allImages[allImages.Length - 1].sprite = PlayerLoadoutCustomization.locked_item_image;
                }
                else
                {
                    allImages[allImages.Length - 1].sprite = _imageHud;
                }

            }
        }

        private void SetupText()
        {
            TextMeshProUGUI buttonText = GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null) buttonText.text = _partName;
        }

        private void SetupOutline()
        {
            if (_isRemoveButton) return; // Não coloca outline no botão de remover

            _outline = gameObject.AddComponent<Outline>();
            _outline.effectColor = _parent.selectedOutlineColor;
            _outline.effectDistance = new Vector2(_parent.outlineWidth, _parent.outlineWidth);
            _outline.useGraphicAlpha = true;
            _outline.enabled = false;
        }

        private void SetupEvents()
        {

            var eventTrigger = gameObject.AddComponent<EventTrigger>();

            AddEventTrigger(eventTrigger, EventTriggerType.PointerEnter, OnPointerEnter);
            AddEventTrigger(eventTrigger, EventTriggerType.PointerExit, OnPointerExit);
            if (is_attatchment_unlocked) AddEventTrigger(eventTrigger, EventTriggerType.PointerClick, () => _parent?.OnCustomizationItemClicked(_partGameObject, _component, _partType));
        }

        private void OnPointerEnter()
        {
            if (_isRemoveButton) return;

            // Destaca temporariamente no hover
            if (_outline != null && !_outline.enabled)
            {
                _outline.effectColor = Color.gray;
                _outline.effectDistance = new Vector2(2f, 2f);
                _outline.enabled = true;
            }
        }

        private void OnPointerExit()
        {
            if (_isRemoveButton) return;

            // Remove destaque temporário
            if (_outline != null && !IsSelected())
            {
                _outline.enabled = false;
            }
        }
        private void AddEventTrigger(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }

        public void UpdateOutlineState()
        {
            if (_isRemoveButton || _parent == null || _component == null) return;

            bool isSelected = IsSelected();

            if (_outline != null)
            {
                if (isSelected)
                {
                    _outline.effectColor = _parent.selectedOutlineColor;
                    _outline.effectDistance = new Vector2(_parent.outlineWidth, _parent.outlineWidth);
                    _outline.enabled = true;
                }
                else
                {
                    _outline.enabled = false;
                }
            }
        }

        private bool IsSelected()
        {
            if (_parent == null || _parent._weaponBeingCustomized == null || _component == null)
                return false;

            // Verifica se este attachment está ativo na arma sendo customizada
            Type componentType = _component.GetType();
            Component[] components = _parent._weaponBeingCustomized.GetComponentsInChildren(componentType, true);

            foreach (Component comp in components)
            {
                if (comp.gameObject.activeInHierarchy && comp.gameObject.name == _partGameObject.name)
                {
                    return true;
                }
            }

            return false;
        }
    }

    private class RemoveAttachmentButtonComponents : MonoBehaviour
    {
        private string _partType;
        private GameObject _weaponBeingCustomized;
        private PlayerLoadoutCustomization _parent;
        private Outline _outline;

        public void Initialize(string partType, GameObject weaponBeingCustomized, PlayerLoadoutCustomization parent)
        {
            _partType = partType;
            _weaponBeingCustomized = weaponBeingCustomized;
            _parent = parent;

            SetupOutline();
            SetupEvents();
        }

        private void SetupOutline()
        {
            // Outline vermelho para o botão de remover (opcional)
            _outline = gameObject.AddComponent<Outline>();
            _outline.effectColor = Color.red;
            _outline.effectDistance = new Vector2(2f, 2f);
            _outline.useGraphicAlpha = true;
            _outline.enabled = false;
        }

        private void SetupEvents()
        {
            var eventTrigger = gameObject.AddComponent<EventTrigger>();

            AddEventTrigger(eventTrigger, EventTriggerType.PointerEnter, OnPointerEnter);
            AddEventTrigger(eventTrigger, EventTriggerType.PointerExit, OnPointerExit);
            AddEventTrigger(eventTrigger, EventTriggerType.PointerClick, OnRemoveClicked);
        }

        private void OnPointerEnter()
        {
            if (_outline != null)
            {
                _outline.enabled = true;
            }
        }

        private void OnPointerExit()
        {
            if (_outline != null)
            {
                _outline.enabled = false;
            }
        }

        private void OnRemoveClicked()
        {
            _parent?.RemoveAttachment(_partType, _weaponBeingCustomized);
        }

        private void AddEventTrigger(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }
    }

    private class ClassLoadoutSaver : MonoBehaviour
    {
        // Estrutura para armazenar o loadout de uma classe (apenas armas e gadgets)
        [Serializable]
        public class ClassLoadoutData
        {
            public ClassManager.Class className;

            // Nomes dos prefabs (mais seguro que guardar referências diretas)
            public string primaryWeaponName;
            public string secondaryWeaponName;
            public string gadget1Name;
            // gadget2Name removido
        }

        [SerializeField] private List<ClassLoadoutData> classLoadouts = new List<ClassLoadoutData>();

        private PlayerLoadoutCustomization loadoutCustomization;
        private SwitchWeapon switchWeapon;

        private void Awake()
        {
            InitializeLoadouts();
        }

        private void Start()
        {
            loadoutCustomization = GetComponent<PlayerLoadoutCustomization>();
            switchWeapon = loadoutCustomization.playerPrefab.GetComponentInChildren<SwitchWeapon>();

            // Carrega dados salvos ao iniciar
            LoadFromPlayerPrefs();
        }

        private void InitializeLoadouts()
        {
            // Inicializa com todas as classes se a lista estiver vazia
            if (classLoadouts.Count == 0)
            {
                foreach (ClassManager.Class classType in Enum.GetValues(typeof(ClassManager.Class)))
                {
                    ClassLoadoutData loadoutData = new ClassLoadoutData
                    {
                        className = classType,
                        primaryWeaponName = "",
                        secondaryWeaponName = "",
                        gadget1Name = ""
                    };

                    classLoadouts.Add(loadoutData);
                }
            }
        }

        // Salva o loadout atual para a classe selecionada
        public void SaveCurrentLoadout(ClassManager.Class targetClass)
        {
            if (loadoutCustomization == null || switchWeapon == null) return;

            ClassLoadoutData loadoutData = GetLoadoutDataForClass(targetClass);
            if (loadoutData == null)
            {
                // Se não existir, cria um novo
                loadoutData = new ClassLoadoutData { className = targetClass };
                classLoadouts.Add(loadoutData);
            }

            // Salva nomes das armas e gadgets
            if (switchWeapon.primary != null)
            {
                WeaponProperties wp = switchWeapon.primary.GetComponent<WeaponProperties>();
                loadoutData.primaryWeaponName = wp != null ? wp.weapon_name : switchWeapon.primary.name;
            }
            else
            {
                loadoutData.primaryWeaponName = "";
            }

            if (switchWeapon.secondary != null)
            {
                WeaponProperties wp = switchWeapon.secondary.GetComponent<WeaponProperties>();
                loadoutData.secondaryWeaponName = wp != null ? wp.weapon_name : switchWeapon.secondary.name;
            }
            else
            {
                loadoutData.secondaryWeaponName = "";
            }

            if (switchWeapon.gadget1 != null)
            {
                loadoutData.gadget1Name = switchWeapon.gadget1.name;
            }
            else
            {
                loadoutData.gadget1Name = "";
            }
            // gadget2 removido

            // Salva em PlayerPrefs
            SaveToPlayerPrefs();
        }

        // Carrega o loadout para a classe especificada
        public void LoadLoadoutForClass(ClassManager.Class targetClass)
        {
            ClassLoadoutData loadoutData = GetLoadoutDataForClass(targetClass);
            if (loadoutData == null)
            {
                return;
            }

            // Carrega armas
            if (!string.IsNullOrEmpty(loadoutData.primaryWeaponName))
            {
                GameObject weapon = FindWeaponByName(loadoutData.primaryWeaponName, true);
                if (weapon != null)
                {
                    switchWeapon.primary = weapon;
                }
            }

            if (!string.IsNullOrEmpty(loadoutData.secondaryWeaponName))
            {
                GameObject weapon = FindWeaponByName(loadoutData.secondaryWeaponName, false);
                if (weapon != null)
                {
                    switchWeapon.secondary = weapon;
                }
            }

            // Carrega gadgets
            if (!string.IsNullOrEmpty(loadoutData.gadget1Name))
            {
                switchWeapon.gadget1 = FindGadgetByName(loadoutData.gadget1Name);
            }

            // Garante que gadget2 fica vazio
            //switchWeapon.gadget2 = null;
        }

        // Métodos auxiliares para encontrar objetos por nome
        private GameObject FindWeaponByName(string weaponName, bool primary)
        {
            if (loadoutCustomization == null) return null;

            GameObject[] weaponArray = primary ?
                loadoutCustomization.primaryWeapons :
                loadoutCustomization.secondaryWeapons;

            foreach (GameObject weapon in weaponArray)
            {
                if (weapon == null) continue;

                WeaponProperties wp = weapon.GetComponent<WeaponProperties>();
                if (wp != null && wp.weapon_name == weaponName)
                    return weapon;

                // Fallback para nome do GameObject
                if (weapon.name == weaponName)
                    return weapon;
            }
            return null;
        }

        private GameObject FindGadgetByName(string gadgetName)
        {
            if (loadoutCustomization == null) return null;

            foreach (GameObject gadget in loadoutCustomization.gadgets)
            {
                if (gadget == null) continue;

                if (gadget.name == gadgetName)
                    return gadget;
            }
            return null;
        }

        private ClassLoadoutData GetLoadoutDataForClass(ClassManager.Class targetClass)
        {
            return classLoadouts.Find(data => data.className == targetClass);
        }

        // Persistência em PlayerPrefs
        private void SaveToPlayerPrefs()
        {
            try
            {
                SerializationWrapper wrapper = new SerializationWrapper { loadouts = classLoadouts };
                string json = JsonUtility.ToJson(wrapper);
                PlayerPrefs.SetString("ClassLoadouts", json);
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogError($"Erro ao salvar loadouts: {e.Message}");
            }
        }

        // Carrega de PlayerPrefs
        public void LoadFromPlayerPrefs()
        {
            try
            {
                if (PlayerPrefs.HasKey("ClassLoadouts"))
                {
                    string json = PlayerPrefs.GetString("ClassLoadouts");
                    SerializationWrapper wrapper = JsonUtility.FromJson<SerializationWrapper>(json);
                    if (wrapper != null && wrapper.loadouts != null)
                    {
                        classLoadouts = wrapper.loadouts;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Erro ao carregar loadouts: {e.Message}");
            }
        }

        // Método para resetar todos os loadouts (útil para testes)
        public void ResetAllLoadouts()
        {
            classLoadouts.Clear();
            InitializeLoadouts();
            SaveToPlayerPrefs();
        }

        // Wrapper para serialização
        [Serializable]
        private class SerializationWrapper
        {
            public List<ClassLoadoutData> loadouts;
        }
    }

    #endregion
}