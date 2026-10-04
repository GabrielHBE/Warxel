using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VehicleLoadoutCustomization : InMatchClientSingleton<VehicleLoadoutCustomization>
{
    [Header("Camera")]
    [SerializeField] private Camera switchLoadoutCamera;

    [Header("Prefabs")]
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private GameObject vehicleCategoryButtonPrefab;

    [Header("UI Parents")]
    [SerializeField] private Transform categoriesParent;
    [SerializeField] private Transform vehiclesParent;
    [SerializeField] private Transform optionsParent;
    [SerializeField] private Transform partsParent;

    [Header("Vehicle Lists")]
    public GameObject[] tankVehicles;
    public GameObject[] jetVehicles;
    public GameObject[] boatVehicles;
    public GameObject[] helicopterVehicles;

    [Header("UI Elements")]
    [SerializeField] private GameObject backButton;
    [SerializeField] private TextMeshProUGUI currentSelectionText;
    [SerializeField] private TextMeshProUGUI previewVehicleText;
    [SerializeField] private Slider itemsSlider;
    [SerializeField] private GameObject customizeVehicleButton;

    [Header("Layout Settings")]
    [SerializeField] private float categoryButtonSpacingY = -66f;
    [SerializeField] private float itemButtonSpacingY = -130f;
    [SerializeField] private float itemButtonStartY;
    [SerializeField] private float itemButtonX;
    [SerializeField] private Color selectedButtonColor = Color.darkRed;
    private Color normalButtonColor = Color.white;

    [Header("Slider Settings")]
    [SerializeField] private float sliderMinValue;
    [SerializeField] private float sliderMaxValue = 1f;
    [SerializeField] private float maxScrollYIncreaser = 100f;

    [Header("Selected Vehicles")]
    public Vehicle selectedBoat;
    public Vehicle selectedJet;
    public Vehicle selectedTank;
    public Vehicle selectedHelicopter;
    public Vehicle selectedPlane => selectedJet;

    private const string PreferencePrefix = "VehicleLoadout_Selected_";
    private readonly List<GameObject> createdButtons = new List<GameObject>();
    private readonly Dictionary<Vehicle, Button> vehicleButtons = new Dictionary<Vehicle, Button>();
    private Vector3 originalVehiclesPosition;
    private Vehicle.VehicleCategory currentCategory;
    private bool showingVehicles;

    private void Start()
    {
        if (switchLoadoutCamera != null) switchLoadoutCamera.enabled = false;
        if (customizeVehicleButton != null) customizeVehicleButton.SetActive(false);
        if (optionsParent != null) optionsParent.gameObject.SetActive(false);
        if (partsParent != null) partsParent.gameObject.SetActive(false);
        if (vehiclesParent != null) originalVehiclesPosition = vehiclesParent.localPosition;
        if (itemsSlider != null)
        {
            itemsSlider.minValue = sliderMinValue;
            itemsSlider.maxValue = sliderMaxValue;
            itemsSlider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        foreach (Vehicle.VehicleCategory category in Enum.GetValues(typeof(Vehicle.VehicleCategory)))
            RestoreSelection(category);
        ShowCategories();
    }

    private void OnDestroy()
    {
        if (itemsSlider != null) itemsSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
    }

    public Vehicle GetSelectedVehicle(Vehicle.VehicleCategory category)
    {
        switch (category)
        {
            case Vehicle.VehicleCategory.Plane: return selectedJet;
            case Vehicle.VehicleCategory.Boat: return selectedBoat;
            case Vehicle.VehicleCategory.Helicopter: return selectedHelicopter;
            case Vehicle.VehicleCategory.Tank: return selectedTank;
            default: return null;
        }
    }

    public void ShowCategories()
    {
        showingVehicles = false;
        ClearButtons();
        if (categoriesParent == null || vehicleCategoryButtonPrefab == null) return;

        categoriesParent.gameObject.SetActive(true);
        if (vehiclesParent != null) vehiclesParent.gameObject.SetActive(false);
        if (backButton != null) backButton.SetActive(false);
        if (itemsSlider != null) itemsSlider.gameObject.SetActive(false);
        SetSelectionText("Select a Vehicle Category");
        if (previewVehicleText != null) previewVehicleText.text = "SELECT A VEHICLE";

        int index = 0;
        foreach (Vehicle.VehicleCategory category in Enum.GetValues(typeof(Vehicle.VehicleCategory)))
        {
            GameObject buttonObject = Instantiate(vehicleCategoryButtonPrefab, categoriesParent);
            PositionCategoryButton(buttonObject, index * categoryButtonSpacingY);
            SetButtonText(buttonObject, category.ToString());
            Button button = buttonObject.GetComponent<Button>();
            if (button != null) button.onClick.AddListener(() => ShowVehicles(category));
            createdButtons.Add(buttonObject);
            index++;
        }
    }

    public void ShowVehicles(Vehicle.VehicleCategory category)
    {
        if (vehiclesParent == null || buttonPrefab == null) return;

        currentCategory = category;
        showingVehicles = true;
        ClearButtons();
        if (categoriesParent != null) categoriesParent.gameObject.SetActive(false);
        vehiclesParent.gameObject.SetActive(true);
        vehiclesParent.localPosition = originalVehiclesPosition;
        if (backButton != null) backButton.SetActive(true);

        int index = 0;
        foreach (Vehicle vehicle in GetAvailableVehicles(category))
        {
            GameObject buttonObject = Instantiate(buttonPrefab, vehiclesParent);
            PositionVehicleButton(buttonObject, itemButtonStartY + index * itemButtonSpacingY);
            SetButtonText(buttonObject, vehicle.gameObject.name);
            Button button = buttonObject.GetComponent<Button>();
            if (button != null)
            {
                if (index == 0 && button.image != null) normalButtonColor = button.image.color;
                button.onClick.AddListener(() => SelectVehicle(vehicle));
                vehicleButtons[vehicle] = button;
            }
            createdButtons.Add(buttonObject);
            index++;
        }

        if (index == 0)
        {
            GameObject emptyButton = Instantiate(buttonPrefab, vehiclesParent);
            PositionVehicleButton(emptyButton, itemButtonStartY);
            SetButtonText(emptyButton, "No vehicles available");
            Button button = emptyButton.GetComponent<Button>();
            if (button != null) button.interactable = false;
            createdButtons.Add(emptyButton);
        }

        if (itemsSlider != null)
        {
            itemsSlider.gameObject.SetActive(index > 4);
            itemsSlider.SetValueWithoutNotify(sliderMinValue);
        }
        UpdateVehicleButtonColors();
        UpdateVehicleSelectionText();
    }

    public void SelectVehicle(Vehicle vehicle)
    {
        if (vehicle == null || !showingVehicles || !GetAvailableVehicles(currentCategory).Contains(vehicle)) return;

        switch (currentCategory)
        {
            case Vehicle.VehicleCategory.Plane: selectedJet = vehicle; break;
            case Vehicle.VehicleCategory.Boat: selectedBoat = vehicle; break;
            case Vehicle.VehicleCategory.Helicopter: selectedHelicopter = vehicle; break;
            case Vehicle.VehicleCategory.Tank: selectedTank = vehicle; break;
        }

        PlayerPrefs.SetString(PreferencePrefix + currentCategory, vehicle.gameObject.name);
        PlayerPrefs.Save();
        UpdateVehicleButtonColors();
        UpdateVehicleSelectionText();
    }

    public void OnBackButtonClicked()
    {
        if (showingVehicles) ShowCategories();
    }

    private void RestoreSelection(Vehicle.VehicleCategory category)
    {
        string savedName = PlayerPrefs.GetString(PreferencePrefix + category, string.Empty);
        List<Vehicle> available = GetAvailableVehicles(category);
        Vehicle selected = available.Find(vehicle => vehicle.gameObject.name == savedName);
        if (selected == null)
        {
            Vehicle configured = GetSelectedVehicle(category);
            selected = available.Contains(configured) ? configured : available.Count > 0 ? available[0] : null;
        }

        switch (category)
        {
            case Vehicle.VehicleCategory.Plane: selectedJet = selected; break;
            case Vehicle.VehicleCategory.Boat: selectedBoat = selected; break;
            case Vehicle.VehicleCategory.Helicopter: selectedHelicopter = selected; break;
            case Vehicle.VehicleCategory.Tank: selectedTank = selected; break;
        }
    }

    private List<Vehicle> GetAvailableVehicles(Vehicle.VehicleCategory category)
    {
        var available = new List<Vehicle>();
        GameObject[] prefabs = GetVehicleList(category);
        if (prefabs != null)
        {
            foreach (GameObject prefab in prefabs)
            {
                if (prefab == null) continue;
                Vehicle vehicle = prefab.GetComponent<Vehicle>();
                if (vehicle != null && vehicle.vehicleCategory == category && !available.Contains(vehicle))
                    available.Add(vehicle);
            }
        }

        Vehicle configured = GetSelectedVehicle(category);
        if (configured != null && configured.vehicleCategory == category && !available.Contains(configured))
            available.Add(configured);
        return available;
    }

    private GameObject[] GetVehicleList(Vehicle.VehicleCategory category)
    {
        switch (category)
        {
            case Vehicle.VehicleCategory.Plane: return jetVehicles;
            case Vehicle.VehicleCategory.Boat: return boatVehicles;
            case Vehicle.VehicleCategory.Helicopter: return helicopterVehicles;
            case Vehicle.VehicleCategory.Tank: return tankVehicles;
            default: return null;
        }
    }

    private void UpdateVehicleButtonColors()
    {
        foreach (KeyValuePair<Vehicle, Button> pair in vehicleButtons)
        {
            Image image = pair.Value != null ? pair.Value.GetComponent<Image>() : null;
            if (image != null)
                image.color = pair.Key == GetSelectedVehicle(currentCategory) ? selectedButtonColor : normalButtonColor;
        }
    }

    private void UpdateVehicleSelectionText()
    {
        Vehicle selected = GetSelectedVehicle(currentCategory);
        SetSelectionText(vehicleButtons.Count == 0
            ? $"No {currentCategory} vehicles available"
            : selected != null ? $"{currentCategory}: {selected.gameObject.name}" : $"Select a {currentCategory}");
        if (previewVehicleText != null)
            previewVehicleText.text = selected != null ? selected.gameObject.name.ToUpperInvariant() : "NO VEHICLE AVAILABLE";
    }

    private void OnSliderValueChanged(float value)
    {
        if (!showingVehicles || vehiclesParent == null) return;
        Vector3 position = originalVehiclesPosition;
        position.y += Mathf.Max(0, vehicleButtons.Count - 4) * maxScrollYIncreaser *
            Mathf.InverseLerp(sliderMinValue, sliderMaxValue, value);
        vehiclesParent.localPosition = position;
    }

    private void ClearButtons()
    {
        foreach (GameObject button in createdButtons)
            if (button != null) Destroy(button);
        createdButtons.Clear();
        vehicleButtons.Clear();
    }

    private void SetSelectionText(string value)
    {
        if (currentSelectionText != null) currentSelectionText.text = value;
    }

    private static void SetButtonText(GameObject button, string value)
    {
        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null) text.text = value;
    }

    private static void PositionCategoryButton(GameObject button, float y)
    {
        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect == null) return;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(294f, 48f);
        rect.anchoredPosition = new Vector2(0f, y);
    }

    private void PositionVehicleButton(GameObject button, float y)
    {
        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect == null) return;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(294f, 116f);
        rect.anchoredPosition = new Vector2(itemButtonX, y);
    }
}
