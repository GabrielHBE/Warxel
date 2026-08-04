using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClassSelectionManager : MonoBehaviour
{
    private InfantryLoadoutCustomization infantryLoadoutCustomization;
    private Dictionary<ClassManager.Class, GameObject> _classButtons = new Dictionary<ClassManager.Class, GameObject>();
    [SerializeField] private Color normalButtonColor = Color.white;
    [SerializeField] private Color selectedButtonColor = Color.darkRed;
    private readonly List<GameObject> _buttonsList = new List<GameObject>();

    public void Initialize(InfantryLoadoutCustomization infantryLoadoutCustomization) => this.infantryLoadoutCustomization = infantryLoadoutCustomization;

    public void InitializeUI()
    {
        infantryLoadoutCustomization.backButton.SetActive(false);
        infantryLoadoutCustomization.loadoutOptionsParent.gameObject.SetActive(false);
        infantryLoadoutCustomization.weaponsGadgetsParent.gameObject.SetActive(false);
        infantryLoadoutCustomization.weaponsGadgetsSlider.gameObject.SetActive(false);

        ShowClassSelection();
        UpdateClassButtonColors();
    }

    public void ShowClassSelection()
    {
        infantryLoadoutCustomization.SetCurrentStage(InfantryLoadoutCustomization.SelectionStage.ClassSelection);
        ClearAllButtons();
        UpdateSelectionText("Selecione sua Classe");

        int classIndex = 0;
        foreach (ClassManager.Class classType in Enum.GetValues(typeof(ClassManager.Class)))
        {
            CreateClassButton(classType, classIndex);
            classIndex++;
        }
        
        SelectClass(infantryLoadoutCustomization._selectedClass);
    }

    private void CreateClassButton(ClassManager.Class classType, int index)
    {
        GameObject classButton = Instantiate(infantryLoadoutCustomization.classButtonPrefab, infantryLoadoutCustomization.classesParent);

        float xPosition = infantryLoadoutCustomization.classButtonStartX + (index * infantryLoadoutCustomization.classButtonSpacingX);
        RectTransform rectTransform = classButton.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = new Vector2(xPosition, infantryLoadoutCustomization.classButtonY);
        }

        TextMeshProUGUI buttonText = classButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null) buttonText.text = infantryLoadoutCustomization.classManager.GetClassName(classType);

        Button button = classButton.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => SelectClass(classType));
        }

        _classButtons[classType] = classButton;

        Image buttonImage = classButton.GetComponent<Image>();
        if (buttonImage != null)
        {
            if (_classButtons.Count == 1)
                normalButtonColor = buttonImage.color;
        }

        _buttonsList.Add(classButton);
    }

    public void SelectClass(ClassManager.Class @class)
    {
        infantryLoadoutCustomization.selected_primary = null;
        infantryLoadoutCustomization.selected_secondary = null;
        infantryLoadoutCustomization.selected_gadget1 = null;
        infantryLoadoutCustomization.selected_gadget2 = null;
        infantryLoadoutCustomization._selectedClass = @class;
        AccountManager.Instance.SetClass(@class);

        Gadget gadget2_class = infantryLoadoutCustomization.classManager.GetClassGadget(@class);
        if (gadget2_class != null) infantryLoadoutCustomization.selected_gadget2 = gadget2_class.gameObject;

        if (infantryLoadoutCustomization.loadoutSaverManager != null)
        {
            infantryLoadoutCustomization.loadoutSaverManager.LoadLoadoutForClass(@class);
        }

        infantryLoadoutCustomization.class_description_text.text = "Class attributes:\n" + infantryLoadoutCustomization.classManager.GetClassDescription(@class);
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

            buttonImage.color = buttonClass == infantryLoadoutCustomization._selectedClass ? selectedButtonColor : normalButtonColor;
        }
    }

    public void OnBackToClassSelection()
    {
        if (PlayerSpawnController.Instance != null)
            PlayerSpawnController.Instance.SwitchPerspectiveButtons(true);

        infantryLoadoutCustomization.SetCurrentStage(InfantryLoadoutCustomization.SelectionStage.ClassSelection);
        infantryLoadoutCustomization.loadoutOptionsParent.gameObject.SetActive(false);
        infantryLoadoutCustomization.backButton.SetActive(false);

        ShowClassSelection();
        UpdateClassButtonColors();
    }

    private void ClearAllButtons()
    {
        foreach (GameObject button in _buttonsList)
        {
            if (button != null) Destroy(button);
        }
        _buttonsList.Clear();
        _classButtons.Clear();
    }

    private void UpdateSelectionText(string text)
    {
        if (infantryLoadoutCustomization.currentSelectionText != null)
            infantryLoadoutCustomization.currentSelectionText.text = text;
    }
}