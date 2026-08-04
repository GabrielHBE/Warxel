using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadoutOptionManager : MonoBehaviour
{
    private InfantryLoadoutCustomization infantryLoadoutCustomization;
    public enum LoadoutOption
    {
        None,
        PrimaryWeapon,
        SecondaryWeapon,
        Gadget1,
        Skin
    }

    public LoadoutOption _currentLoadoutOption = LoadoutOption.None;

    public void Initialize(InfantryLoadoutCustomization parent) => infantryLoadoutCustomization = parent;

    public void ShowLoadoutOptions()
    {
        if (PlayerSpawnController.Instance != null) PlayerSpawnController.Instance.SwitchPerspectiveButtons(false);
        infantryLoadoutCustomization.SetCurrentStage(InfantryLoadoutCustomization.SelectionStage.LoadoutOptionSelection);
        infantryLoadoutCustomization.backButton.SetActive(true);

        ClearAllButtons();
        infantryLoadoutCustomization.loadoutOptionsParent.gameObject.SetActive(true);

        UpdateSelectionText($"Selecione o que deseja alterar - {infantryLoadoutCustomization._selectedClass}");

        CreateLoadoutOptionButton("Arma Primária", LoadoutOption.PrimaryWeapon, 0);
        CreateLoadoutOptionButton("Arma Secundária", LoadoutOption.SecondaryWeapon, 1);
        CreateLoadoutOptionButton("Gadget", LoadoutOption.Gadget1, 2);
        CreateLoadoutOptionButton("Skins", LoadoutOption.Skin, 3);
    }

    private void CreateLoadoutOptionButton(string buttonName, LoadoutOption option, int index)
    {
        GameObject optionButton = Instantiate(infantryLoadoutCustomization.loadoutOptionButtonPrefab, infantryLoadoutCustomization.loadoutOptionsParent);

        float yPosition = infantryLoadoutCustomization.loadoutOptionStartY + (index * infantryLoadoutCustomization.loadoutOptionSpacingY);
        RectTransform rectTransform = optionButton.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = new Vector2(0, yPosition);
        }

        TextMeshProUGUI buttonText = optionButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null) buttonText.text = buttonName;

        Button button = optionButton.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => OnLoadoutOptionSelected(option));
        }
    }

    private void OnLoadoutOptionSelected(LoadoutOption option)
    {
        _currentLoadoutOption = option;

        // Limpa todos os botões e pré-visualizações antes de mudar
        infantryLoadoutCustomization.itemSelectionManager.ClearAllWeaponsGadgetsChildren();

        if (infantryLoadoutCustomization.skinSelectionManager != null)
        {
            infantryLoadoutCustomization.skinSelectionManager.ClearSkinButtons();
            infantryLoadoutCustomization.skinSelectionManager.ClearSkinPreview();
        }

        if (option == LoadoutOption.Skin)
        {
            infantryLoadoutCustomization.SetCurrentStage(InfantryLoadoutCustomization.SelectionStage.SkinSelection);
            infantryLoadoutCustomization.loadoutOptionsParent.gameObject.SetActive(false);
            infantryLoadoutCustomization.skinSelectionManager.ShowSkinsForClass();

            UpdateSelectionText($"Selecionando Skin - {infantryLoadoutCustomization._selectedClass}");
            return;
        }

        infantryLoadoutCustomization.SetCurrentStage(InfantryLoadoutCustomization.SelectionStage.ItemSelection);
        infantryLoadoutCustomization.loadoutOptionsParent.gameObject.SetActive(false);
        infantryLoadoutCustomization.itemSelectionManager.ShowAvailableItems(option);
        infantryLoadoutCustomization.itemSelectionManager.ResetSlider();

        string optionName = GetLoadoutOptionDisplayName(option);
        UpdateSelectionText($"Selecionando: {optionName} - {infantryLoadoutCustomization._selectedClass}");
    }

    public void OnBackToLoadoutOptions()
    {
        if (PlayerSpawnController.Instance != null)
            PlayerSpawnController.Instance.SwitchPerspectiveButtons(false);

        // Limpa a pré-visualização da skin se estiver na seleção de skins
        if (infantryLoadoutCustomization.skinSelectionManager != null)
        {
            infantryLoadoutCustomization.skinSelectionManager.ClearSkinPreview();
        }

        infantryLoadoutCustomization.SetCurrentStage(InfantryLoadoutCustomization.SelectionStage.LoadoutOptionSelection);

        if (infantryLoadoutCustomization._currentItemSelected != null)
        {
            Destroy(infantryLoadoutCustomization._currentItemSelected);
            infantryLoadoutCustomization._currentItemSelected = null;
        }

        if (infantryLoadoutCustomization.weaponsGadgetsSlider != null)
        {
            infantryLoadoutCustomization.weaponsGadgetsSlider.gameObject.SetActive(false);
        }

        infantryLoadoutCustomization.weaponsGadgetsParent.gameObject.SetActive(false);
        infantryLoadoutCustomization.itemSelectionManager.ClearItemButtons();

        ShowLoadoutOptions();
    }

    private string GetLoadoutOptionDisplayName(LoadoutOption option)
    {
        return option switch
        {
            LoadoutOption.PrimaryWeapon => "Arma Primária",
            LoadoutOption.SecondaryWeapon => "Arma Secundária",
            LoadoutOption.Gadget1 => "Gadget",
            LoadoutOption.Skin => "Skin",
            _ => option.ToString()
        };
    }

    private void ClearAllButtons()
    {
        foreach (Transform child in infantryLoadoutCustomization.loadoutOptionsParent)
        {
            Destroy(child.gameObject);
        }
    }

    private void UpdateSelectionText(string text)
    {
        if (infantryLoadoutCustomization.currentSelectionText != null)
            infantryLoadoutCustomization.currentSelectionText.text = text;
    }
    public LoadoutOption GetCurrentOption() => _currentLoadoutOption;
}