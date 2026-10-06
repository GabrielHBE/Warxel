using System.Collections.Generic;
using UnityEngine;

public class SkinSelectionManager : MonoBehaviour
{
    private InfantryLoadoutCustomization infantryLoadoutCustomization;
    private readonly List<GameObject> _buttonsList = new List<GameObject>();
    private GameObject _currentSkinPreview;
    private Skin _selectedSkin;
    private int skinSelectionRequest;

    public void Initialize(InfantryLoadoutCustomization parent) => infantryLoadoutCustomization = parent;

    public async void ShowSkinsForClass()
    {
        ClearSkinButtons();
        int request = skinSelectionRequest;
        ClassManager.Class selectedClass = infantryLoadoutCustomization._selectedClass;
        if (SkinsManager.Instance == null || !await SkinsManager.Instance.WaitUntilReadyAsync()) return;
        if (this == null || !isActiveAndEnabled || request != skinSelectionRequest ||
            selectedClass != infantryLoadoutCustomization._selectedClass ||
            infantryLoadoutCustomization.GetCurrentStage() != InfantryLoadoutCustomization.SelectionStage.SkinSelection) return;

        infantryLoadoutCustomization.weaponsGadgetsParent.gameObject.SetActive(true);

        UpdateSelectionText($"Selecting Skin - {infantryLoadoutCustomization._selectedClass}");

        // Obtém todas as skins disponíveis para a classe atual
        List<Skin> availableSkins = GetSkinsForClass(infantryLoadoutCustomization._selectedClass);

        // Carrega a skin atualmente selecionada
        _selectedSkin = ResolveSkinForClass(selectedClass);

        int skinIndex = 0;
        foreach (Skin skin in availableSkins)
        {
            CreateSkinButton(skin, skinIndex);
            skinIndex++;
        }

        if (_selectedSkin != null) ShowSkinPreview(_selectedSkin);
        

        UpdateAllButtonOutlines();
        infantryLoadoutCustomization.itemSelectionManager.ConfigureScrollForItemCount(availableSkins.Count);
    }

    private List<Skin> GetSkinsForClass(ClassManager.Class classType)
    {
        List<Skin> skinsForClass = new List<Skin>();

        var allSkins = SkinsManager.Instance.GetAllSkins();

        foreach (Skin skin in allSkins)
        {
            if (skin.skinClass == classType)
            {
                bool isUnlocked = IsSkinUnlocked(skin);
                if (isUnlocked) skinsForClass.Add(skin);
            }
        }

        return skinsForClass;
    }

    private void CreateSkinButton(Skin skin, int index)
    {
        float yPosition = infantryLoadoutCustomization.itemButtonStartY +
                         (index * infantryLoadoutCustomization.itemButtonSpacingY);

        GameObject skinButton = Instantiate(infantryLoadoutCustomization.buttonPrefab,
                                           infantryLoadoutCustomization.weaponsGadgetsParent);

        RectTransform rectTransform = skinButton.GetComponent<RectTransform>();
        if (rectTransform != null) rectTransform.anchoredPosition = new Vector2(infantryLoadoutCustomization.itemButtonX, yPosition);
        

        var component = skinButton.AddComponent<SkinButtonComponents>();
        component.Initialize(skin, infantryLoadoutCustomization);

        _buttonsList.Add(skinButton);
    }

    public void OnSkinSelected(Skin skin)
    {
        _selectedSkin = skin;

        // Salva a skin selecionada para a classe atual
        SaveSkinForClass(infantryLoadoutCustomization._selectedClass, SkinsManager.GetSkinName(skin));

        // Mostra a pré-visualização da skin
        ShowSkinPreview(skin);

        // Atualiza os outlines
        UpdateAllButtonOutlines();

        // Toca o som de seleção
        if (infantryLoadoutCustomization.selectItemSfx != null)SoundManager.Play2dSoundLocal(infantryLoadoutCustomization.selectItemSfx.clip, infantryLoadoutCustomization.selectItemSfx.properties);
        

        UpdateSelectionText($"Selected Skin: {skin.skingName}");

        // Salva o loadout para persistir a skin
        infantryLoadoutCustomization.SaveCurrentLoadout();
    }

    private void ShowSkinPreview(Skin skin)
    {
        // Remove a pré-visualização anterior
        ClearSkinPreview();

        // Cria a pré-visualização da skin
        if (skin != null && infantryLoadoutCustomization.currentItemParent != null)
        {
            // Instancia o GameObject da skin diretamente
            _currentSkinPreview = Instantiate(skin.anim.gameObject, infantryLoadoutCustomization.currentItemParent);

            // Ajusta a posição, rotação e escala para melhor visualização
            _currentSkinPreview.transform.localPosition = new Vector3(0.7f, -1.76f, 0);
            _currentSkinPreview.transform.localRotation = Quaternion.Euler(0, 90, 0);
            _currentSkinPreview.transform.localScale = new Vector3(70,70, 70);
        }
    }

    private static void SaveSkinForClass(ClassManager.Class classType, string skinName)
    {
        PlayerPrefs.SetString($"Skin_Selected_{classType}", skinName);
        PlayerPrefs.Save();
    }

    public static string LoadCurrentSkinForClass(ClassManager.Class classType) => PlayerPrefs.GetString($"Skin_Selected_{classType}", "");

    public static bool IsSkinUnlocked(Skin skin) => skin.battleCoinsToUnlock == 0 ||
        PlayerPrefs.GetInt($"Skin_Unlocked_{SkinsManager.GetSkinName(skin)}_{skin.skinClass}", 0) == 1;

    public static Skin ResolveSkinForClass(ClassManager.Class classType)
    {
        string savedName = LoadCurrentSkinForClass(classType);
        if (SkinsManager.TryGetSkin(savedName, classType, out Skin selected) && IsSkinUnlocked(selected))
            return selected;

        if (SkinsManager.Instance == null) return null;
        selected = null;

        // Save the preview fallback so spawning uses the same skin.
        foreach (Skin skin in SkinsManager.Instance.GetAllSkins())
        {
            if (skin == null || skin.skinClass != classType || !IsSkinUnlocked(skin)) continue;
            if (selected == null || string.CompareOrdinal(SkinsManager.GetSkinName(skin),
                SkinsManager.GetSkinName(selected)) < 0)
                selected = skin;
        }

        if (selected != null) SaveSkinForClass(classType, SkinsManager.GetSkinName(selected));
        return selected;
    }

    public void UpdateAllButtonOutlines()
    {
        foreach (GameObject button in _buttonsList)
        {
            if (button == null) continue;

            var skinComponent = button.GetComponent<SkinButtonComponents>();
            if (skinComponent != null) skinComponent.UpdateOutlineState();
        }
    }

    public void ClearSkinButtons()
    {
        skinSelectionRequest++;
        foreach (GameObject button in _buttonsList.ToArray())
        {
            if (button != null) Destroy(button);
        }
        _buttonsList.Clear();
    }

    private void UpdateSelectionText(string text)
    {
        if (infantryLoadoutCustomization.currentSelectionText != null)
            infantryLoadoutCustomization.currentSelectionText.text = text;
    }

    public void ClearSkinPreview()
    {
        if (_currentSkinPreview != null)
        {
            Destroy(_currentSkinPreview);
            _currentSkinPreview = null;
        }
    }

    public Skin GetCurrentSkinForClass(ClassManager.Class classType)
    {
        return ResolveSkinForClass(classType);
    }


}
