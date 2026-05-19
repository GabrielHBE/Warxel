using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum VehicleCategory
{
    MBT,
    IFV,
    ScoutHelicopter,
    AttackHelicopter,
    TransportHelicopter,
    AttackJet,
    StealthJet,
    Gunship
}

public class VehicleLoadoutCustomization : MonoBehaviour
{
    public static VehicleLoadoutCustomization Instance { get; private set; }

    [Header("Camera")]
    [SerializeField] private Camera switchLoadoutCamera;

    [Header("Prefabs")]
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private GameObject vehicleCategoryButtonPrefab; // Usado para categorias
    [SerializeField] private GameObject loadoutOptionButtonPrefab; // Usado para slots (ex: TankShell)

    [Header("UI Parents")]
    [SerializeField] private Transform categoriesParent;
    [SerializeField] private Transform vehiclesParent;
    [SerializeField] private Transform optionsParent;
    [SerializeField] private Transform partsParent;
    [SerializeField] private Transform currentItemParent;

    [Header("Vehicle Lists")]
    public GameObject[] mbtVehicles;
    public GameObject[] ifvVehicles;
    public GameObject[] scoutHeliVehicles;
    public GameObject[] attackHeliVehicles;
    public GameObject[] transportHeliVehicles;
    public GameObject[] attackJetVehicles;
    public GameObject[] stealthJetVehicles;
    public GameObject[] gunshipVehicles;

    [Space]

    [Header("UI Elements")]
    [SerializeField] private GameObject backButton;
    [SerializeField] private TextMeshProUGUI currentSelectionText;
    [SerializeField] private Slider itemsSlider;

    // NOVO: Botão para entrar na customização do veículo selecionado
    [Header("Vehicle Customization Button")]
    [SerializeField] private GameObject customizeVehicleButton;

    [Header("Selection Outline")]
    [SerializeField] private Color selectedOutlineColor = Color.white;
    [SerializeField] private float outlineWidth = 5f;

    [Header("Layout Settings")]
    [SerializeField] private float buttonSpacingX = 150f;
    [SerializeField] private float buttonStartX = -300f;
    [SerializeField] private float buttonY = 0f;
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

    [Header("Sound Effects")]
    [SerializeField] private AudioSource audio_source;
    [SerializeField] private AudioClip select_item_sfx;

    // Enums e Estados
    public enum SelectionStage
    {
        CategorySelection,
        VehicleSelection,
        OptionSelection,
        PartSelection
    }

    private SelectionStage _currentStage = SelectionStage.CategorySelection;
    private VehicleCategory _selectedCategory;
    private GameObject _selectedVehiclePrefab;
    private GameObject _currentVehicleInstance;
    private VehicleCustomizableParts _selectedPartType;

    private readonly List<GameObject> _buttonsList = new List<GameObject>();
    private Vector3 _originalPartsPosition;
    private float _maxSliderY;

    private VehicleLoadoutSaver loadoutSaver;

    public Vehicle selectedMbt;
    public Vehicle selectedIfv;
    public Vehicle selectedScountHeli;
    public Vehicle selectedAttackHeli;
    public Vehicle selectedTransportHeli;
    public Vehicle selectedAttackJet;
    public Vehicle selectedStealthJet;
    public Vehicle selectedGunship;

    #region Unity Lifecycle

    private void Start()
    {
        Instance = this;
        InitializeComponents();
        InitializeUI();
        ShowCategories();
    }

    private void Update()
    {
        UpdateCameraVisibility();
    }

    #endregion

    #region Initialization

    private void InitializeComponents()
    {
        loadoutSaver = gameObject.AddComponent<VehicleLoadoutSaver>();
        if (switchLoadoutCamera != null) switchLoadoutCamera.enabled = false;
        _originalPartsPosition = partsParent.localPosition;
    }

    private void InitializeUI()
    {
        InitializeSlider();

        // Inicializa o botão de customização
        if (customizeVehicleButton != null)
        {
            customizeVehicleButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(OnCustomizeVehicleButtonClicked);
            customizeVehicleButton.SetActive(false);
        }

        backButton.SetActive(false);
        categoriesParent.gameObject.SetActive(false);
        vehiclesParent.gameObject.SetActive(false);
        optionsParent.gameObject.SetActive(false);
        partsParent.gameObject.SetActive(false);
        itemsSlider.gameObject.SetActive(false);
    }

    private void InitializeSlider()
    {
        if (itemsSlider == null) return;
        itemsSlider.minValue = sliderMinValue;
        itemsSlider.maxValue = sliderMaxValue;
        itemsSlider.value = 0;
        itemsSlider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    private void UpdateCameraVisibility()
    {
        if (switchLoadoutCamera != null)
        {
            //switchLoadoutCamera.enabled = _currentStage != SelectionStage.CategorySelection && _currentStage != SelectionStage.VehicleSelection;
            switchLoadoutCamera.enabled = _currentStage != SelectionStage.CategorySelection;
        }
    }

    #endregion

    #region Slider Handling

    private void OnSliderValueChanged(float value)
    {
        if (_currentStage != SelectionStage.VehicleSelection && _currentStage != SelectionStage.PartSelection) return;

        float scrollY = Mathf.Lerp(minScrollY, _maxSliderY, value);
        Vector3 newPosition = _originalPartsPosition;
        newPosition.y = scrollY;

        // Aplica o scroll no parent que estiver ativo (vehicles ou parts)
        if (vehiclesParent.gameObject.activeSelf) vehiclesParent.localPosition = newPosition;
        if (partsParent.gameObject.activeSelf) partsParent.localPosition = newPosition;
    }

    private void ResetSlider(Transform targetParent)
    {
        if (itemsSlider != null)
        {
            itemsSlider.gameObject.SetActive(true);
            itemsSlider.value = 0f;
        }
        targetParent.localPosition = _originalPartsPosition;
        _maxSliderY = minScrollY;
    }

    #endregion

    #region Flow Methods

    public void ShowCategories()
    {
        _currentStage = SelectionStage.CategorySelection;
        ClearAllButtons();
        DisableAllParents();
        backButton.SetActive(false);
        categoriesParent.gameObject.SetActive(true);

        if (currentSelectionText != null) currentSelectionText.text = "Selecione a Categoria de Veículos";

        int index = 0;
        foreach (VehicleCategory category in Enum.GetValues(typeof(VehicleCategory)))
        {
            CreateCategoryButton(category, index);
            index++;
        }
    }

    public void ShowVehicles(VehicleCategory category)
    {
        _currentStage = SelectionStage.VehicleSelection;
        _selectedCategory = category;

        ClearAllButtons();
        DisableAllParents();
        backButton.SetActive(true);
        vehiclesParent.gameObject.SetActive(true);

        if (currentSelectionText != null) currentSelectionText.text = $"Veículos: {category}";
        ResetSlider(vehiclesParent);

        GameObject[] vehicles = GetVehiclesArrayForCategory(category);

        if (vehicles != null)
        {
            for (int i = 0; i < vehicles.Length; i++)
            {
                CreateVehicleButton(vehicles[i], i);
            }
        }

        ShowSliderIfNeeded(vehicles?.Length ?? 0, vehiclesParent);
    }

    // NOVO: Chamado ao clicar no botão de um veículo
    public void OnVehicleSelected(GameObject vehiclePrefab, VehicleCategory category)
    {
        if (audio_source != null && select_item_sfx != null)
        {
            audio_source.clip = select_item_sfx;
            audio_source.Play();
        }

        _selectedVehiclePrefab = vehiclePrefab;

        // Instancia e prepara o veículo na tela (igual ao PlayerLoadout)
        if (_currentVehicleInstance != null) Destroy(_currentVehicleInstance);
        _currentVehicleInstance = Instantiate(vehiclePrefab, currentItemParent);
        SetupInstanceForCustomization(_currentVehicleInstance);

        // Carrega loadout salvo para visualizar as peças
        loadoutSaver.LoadLoadoutForVehicle(_currentVehicleInstance, vehiclePrefab.name);

        // Atribui o script Vehicle à variável da categoria correspondente
        Vehicle vehicleComponent = vehiclePrefab.GetComponent<Vehicle>();
        if (vehicleComponent != null)
        {
            switch (category)
            {
                case VehicleCategory.MBT: selectedMbt = vehicleComponent; break;
                case VehicleCategory.IFV: selectedIfv = vehicleComponent; break;
                case VehicleCategory.ScoutHelicopter: selectedScountHeli = vehicleComponent; break;
                case VehicleCategory.AttackHelicopter: selectedAttackHeli = vehicleComponent; break;
                case VehicleCategory.TransportHelicopter: selectedTransportHeli = vehicleComponent; break;
                case VehicleCategory.AttackJet: selectedAttackJet = vehicleComponent; break;
                case VehicleCategory.StealthJet: selectedStealthJet = vehicleComponent; break;
                case VehicleCategory.Gunship: selectedGunship = vehicleComponent; break;
            }
        }

        // Ativa o botão de "Customizar"
        if (customizeVehicleButton != null)
        {
            customizeVehicleButton.SetActive(true);
        }
    }

    // NOVO: O botão secundário dispara a customização
    public void OnCustomizeVehicleButtonClicked()
    {
        if (_selectedVehiclePrefab != null)
        {
            if (customizeVehicleButton != null) customizeVehicleButton.SetActive(false);
            ShowOptions(_selectedVehiclePrefab);
        }
    }

    public void ShowOptions(GameObject vehiclePrefab)
    {
        _currentStage = SelectionStage.OptionSelection;

        ClearAllButtons();
        DisableAllParents();
        backButton.SetActive(true);
        optionsParent.gameObject.SetActive(true);

        if (currentSelectionText != null) currentSelectionText.text = $"Customizando: {vehiclePrefab.name}";

        // O veículo já foi instanciado no OnVehicleSelected, não precisamos criar novamente.

        // Procura todos os slots disponíveis neste veículo baseando-se nas partes presentes
        IsVehicleCustomizationPart[] parts = _currentVehicleInstance.GetComponentsInChildren<IsVehicleCustomizationPart>(true);
        var availableSlots = parts.Select(p => p.GetCustomizationPart()).Distinct().ToList();

        for (int i = 0; i < availableSlots.Count; i++)
        {
            CreateOptionButton(availableSlots[i], i);
        }
    }

    public void ShowParts(VehicleCustomizableParts partType)
    {
        _currentStage = SelectionStage.PartSelection;
        _selectedPartType = partType;

        ClearAllButtons();
        DisableAllParents();
        backButton.SetActive(true);
        partsParent.gameObject.SetActive(true);

        if (currentSelectionText != null) currentSelectionText.text = $"Selecionando: {partType}";
        ResetSlider(partsParent);

        IsVehicleCustomizationPart[] allParts = _currentVehicleInstance.GetComponentsInChildren<IsVehicleCustomizationPart>(true);
        var partsForSlot = allParts.Where(p => p.GetCustomizationPart() == partType).ToList();

        for (int i = 0; i < partsForSlot.Count; i++)
        {
            CreatePartButton(partsForSlot[i], i);
        }

        ShowSliderIfNeeded(partsForSlot.Count, partsParent);
        UpdateAllButtonOutlines();
    }

    #endregion

    #region Interaction Methods

    public void OnPartClicked(IsVehicleCustomizationPart selectedPart)
    {
        if (audio_source != null && select_item_sfx != null)
        {
            audio_source.clip = select_item_sfx;
            audio_source.Play();
        }

        // Desativa todos do mesmo slot
        IsVehicleCustomizationPart[] allParts = _currentVehicleInstance.GetComponentsInChildren<IsVehicleCustomizationPart>(true);
        foreach (var p in allParts)
        {
            if (p.GetCustomizationPart() == _selectedPartType)
            {
                p.Deactivate();
            }
        }

        // Ativa o selecionado
        //selectedPart.Activate(); // Você tem ele comentado no original também

        // Salva as alterações
        loadoutSaver.SaveCurrentLoadout(_selectedVehiclePrefab.name, _currentVehicleInstance);

        UpdateAllButtonOutlines();
    }

    public void OnBackButtonClicked()
    {
        if (itemsSlider != null) itemsSlider.gameObject.SetActive(false);

        switch (_currentStage)
        {
            case SelectionStage.PartSelection:
                ShowOptions(_selectedVehiclePrefab);
                break;
            case SelectionStage.OptionSelection:
                // Retorna o botão de customização ao voltar pros veículos, se houver um selecionado
                if (customizeVehicleButton != null && _selectedVehiclePrefab != null) customizeVehicleButton.SetActive(true);
                ShowVehicles(_selectedCategory);
                break;
            case SelectionStage.VehicleSelection:
                if (customizeVehicleButton != null) customizeVehicleButton.SetActive(false);
                if (_currentVehicleInstance != null) Destroy(_currentVehicleInstance);
                _selectedVehiclePrefab = null;
                ShowCategories();
                break;
        }
    }

    #endregion

    #region UI Creation Helpers

    private void CreateCategoryButton(VehicleCategory category, int index)
    {
        GameObject btnObj = Instantiate(vehicleCategoryButtonPrefab, categoriesParent);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition = new Vector2(buttonStartX + (index * buttonSpacingX), buttonY);

        btnObj.GetComponentInChildren<TextMeshProUGUI>().text = category.ToString();
        btnObj.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => ShowVehicles(category));
        _buttonsList.Add(btnObj);
    }

    private void CreateVehicleButton(GameObject vehiclePrefab, int index)
    {
        _maxSliderY += maxScrollYIncreaser;
        GameObject btnObj = Instantiate(buttonPrefab, vehiclesParent);
        ApplyVerticalSpacing(btnObj, index);

        btnObj.GetComponentInChildren<TextMeshProUGUI>().text = vehiclePrefab.name;

        // MODIFICADO: Agora chama OnVehicleSelected em vez de abrir direto ShowOptions
        btnObj.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => OnVehicleSelected(vehiclePrefab, _selectedCategory));

        _buttonsList.Add(btnObj);
    }

    private void CreateOptionButton(VehicleCustomizableParts slotType, int index)
    {
        GameObject btnObj = Instantiate(loadoutOptionButtonPrefab, optionsParent);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition = new Vector2(0, loadoutOptionStartY + (index * loadoutOptionSpacingY));

        btnObj.GetComponentInChildren<TextMeshProUGUI>().text = slotType.ToString();
        btnObj.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => ShowParts(slotType));
        _buttonsList.Add(btnObj);
    }

    private void CreatePartButton(IsVehicleCustomizationPart part, int index)
    {
        _maxSliderY += maxScrollYIncreaser;
        GameObject btnObj = Instantiate(buttonPrefab, partsParent);
        ApplyVerticalSpacing(btnObj, index);

        var component = btnObj.AddComponent<PartButtonComponent>();
        component.Initialize(part, this);
        _buttonsList.Add(btnObj);
    }

    private void ApplyVerticalSpacing(GameObject button, int index)
    {
        RectTransform rectTransform = button.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = new Vector2(itemButtonX, itemButtonStartY + (index * itemButtonSpacingY));
        }
    }

    private void ShowSliderIfNeeded(int itemCount, Transform targetParent)
    {
        if (itemsSlider != null && itemCount > 4) // Ajuste o > 4 conforme sua tela
        {
            itemsSlider.gameObject.SetActive(true);
            itemsSlider.value = 0f;
            targetParent.localPosition = _originalPartsPosition;
        }
        else if (itemsSlider != null)
        {
            itemsSlider.gameObject.SetActive(false);
        }
    }

    private void DisableAllParents()
    {
        categoriesParent.gameObject.SetActive(false);
        vehiclesParent.gameObject.SetActive(false);
        optionsParent.gameObject.SetActive(false);
        partsParent.gameObject.SetActive(false);
    }

    private void ClearAllButtons()
    {
        foreach (GameObject btn in _buttonsList)
        {
            if (btn != null) Destroy(btn);
        }
        _buttonsList.Clear();
    }

    private void SetupInstanceForCustomization(GameObject item)
    {
        item.layer = LayerMask.NameToLayer("LoadoutCustomization");
        foreach (Transform child in item.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.layer = LayerMask.NameToLayer("LoadoutCustomization");
        }
    }

    public void UpdateAllButtonOutlines()
    {
        foreach (GameObject button in _buttonsList)
        {
            if (button == null) continue;
            var comp = button.GetComponent<PartButtonComponent>();
            if (comp != null) comp.UpdateOutlineState();
        }
    }

    private GameObject[] GetVehiclesArrayForCategory(VehicleCategory category)
    {
        return category switch
        {
            VehicleCategory.MBT => mbtVehicles,
            VehicleCategory.IFV => ifvVehicles,
            VehicleCategory.ScoutHelicopter => scoutHeliVehicles,
            VehicleCategory.AttackHelicopter => attackHeliVehicles,
            VehicleCategory.TransportHelicopter => transportHeliVehicles,
            VehicleCategory.AttackJet => attackJetVehicles,
            VehicleCategory.StealthJet => stealthJetVehicles,
            VehicleCategory.Gunship => gunshipVehicles,
            _ => new GameObject[0]
        };
    }

    #endregion

    #region Inner Classes

    private class PartButtonComponent : MonoBehaviour
    {
        private IsVehicleCustomizationPart _part;
        private VehicleLoadoutCustomization _parent;
        private Outline _outline;

        public void Initialize(IsVehicleCustomizationPart part, VehicleLoadoutCustomization parent)
        {
            _part = part;
            _parent = parent;

            // Define o texto baseando-se no nome da parte
            TextMeshProUGUI buttonText = GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null) buttonText.text = _part.GetCustomizationPartName();

            SetupOutline();
            GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => _parent.OnPartClicked(_part));
        }

        private void SetupOutline()
        {
            _outline = gameObject.AddComponent<Outline>();
            _outline.effectColor = _parent.selectedOutlineColor;
            _outline.effectDistance = new Vector2(_parent.outlineWidth, _parent.outlineWidth);
            _outline.useGraphicAlpha = true;
            _outline.enabled = false;
        }

        public void UpdateOutlineState()
        {
            if (_outline == null || _part == null) return;

            // Assume-se que a parte ativa estará com seu respectivo GameObject ativado ou gerenciado na lógica interna
            // Como fallback simples, validamos se o GameObject que carrega este componente está ativo na hierarquia
            MonoBehaviour mbPart = _part as MonoBehaviour;
            _outline.enabled = mbPart != null && mbPart.gameObject.activeSelf;
        }
    }

    private class VehicleLoadoutSaver : MonoBehaviour
    {
        [Serializable]
        public class VehicleLoadoutData
        {
            public string vehicleName;
            public List<SlotData> slots = new List<SlotData>();
        }

        [Serializable]
        public class SlotData
        {
            public VehicleCustomizableParts slotType;
            public string activePartName;
        }

        [Serializable]
        private class SerializationWrapper
        {
            public List<VehicleLoadoutData> loadouts = new List<VehicleLoadoutData>();
        }

        private List<VehicleLoadoutData> savedLoadouts = new List<VehicleLoadoutData>();

        private void Awake()
        {
            LoadFromPlayerPrefs();
        }

        public void SaveCurrentLoadout(string vehicleName, GameObject vehicleInstance)
        {
            VehicleLoadoutData data = savedLoadouts.Find(l => l.vehicleName == vehicleName);
            if (data == null)
            {
                data = new VehicleLoadoutData { vehicleName = vehicleName };
                savedLoadouts.Add(data);
            }

            data.slots.Clear();

            // Salva apenas as partes ativas
            IsVehicleCustomizationPart[] allParts = vehicleInstance.GetComponentsInChildren<IsVehicleCustomizationPart>(true);
            foreach (var part in allParts)
            {
                MonoBehaviour mbPart = part as MonoBehaviour;
                if (mbPart != null && mbPart.gameObject.activeSelf)
                {
                    data.slots.Add(new SlotData
                    {
                        slotType = part.GetCustomizationPart(),
                        activePartName = part.GetCustomizationPartName()
                    });
                }
            }

            SaveToPlayerPrefs();
        }

        public void LoadLoadoutForVehicle(GameObject vehicleInstance, string vehicleName)
        {
            VehicleLoadoutData data = savedLoadouts.Find(l => l.vehicleName == vehicleName);
            if (data == null) return;

            IsVehicleCustomizationPart[] allParts = vehicleInstance.GetComponentsInChildren<IsVehicleCustomizationPart>(true);

            // Primeiro desativa tudo
            foreach (var part in allParts) part.Deactivate();

            // Depois ativa os salvos
            foreach (var part in allParts)
            {
                foreach (var savedSlot in data.slots)
                {
                    if (part.GetCustomizationPart() == savedSlot.slotType &&
                        part.GetCustomizationPartName() == savedSlot.activePartName)
                    {
                        part.Activate();
                    }
                }
            }
        }

        private void SaveToPlayerPrefs()
        {
            SerializationWrapper wrapper = new SerializationWrapper { loadouts = savedLoadouts };
            PlayerPrefs.SetString("VehicleLoadouts", JsonUtility.ToJson(wrapper));
            PlayerPrefs.Save();
        }

        private void LoadFromPlayerPrefs()
        {
            if (PlayerPrefs.HasKey("VehicleLoadouts"))
            {
                string json = PlayerPrefs.GetString("VehicleLoadouts");
                SerializationWrapper wrapper = JsonUtility.FromJson<SerializationWrapper>(json);
                if (wrapper != null && wrapper.loadouts != null)
                {
                    savedLoadouts = wrapper.loadouts;
                }
            }
        }
    }

    #endregion
}