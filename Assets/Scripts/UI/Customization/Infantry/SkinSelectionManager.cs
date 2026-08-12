using System.Collections.Generic;
using UnityEngine;

public class SkinSelectionManager : MonoBehaviour
{
    private InfantryLoadoutCustomization infantryLoadoutCustomization;
    private readonly List<GameObject> _buttonsList = new List<GameObject>();
    private GameObject _currentSkinPreview;
    private Skin _selectedSkin;

    public void Initialize(InfantryLoadoutCustomization parent) => infantryLoadoutCustomization = parent;

    public void ShowSkinsForClass()
    {
        ClearSkinButtons();
        infantryLoadoutCustomization.weaponsGadgetsParent.gameObject.SetActive(true);

        UpdateSelectionText($"Selecionando Skin - {infantryLoadoutCustomization._selectedClass}");

        // Obtém todas as skins disponíveis para a classe atual
        List<Skin> availableSkins = GetSkinsForClass(infantryLoadoutCustomization._selectedClass);

        // Carrega a skin atualmente selecionada
        string currentSkinName = LoadCurrentSkinForClass(infantryLoadoutCustomization._selectedClass);
        _selectedSkin = SkinsManager.GetSkin(currentSkinName, infantryLoadoutCustomization._selectedClass);

        int skinIndex = 0;
        foreach (Skin skin in availableSkins)
        {
            CreateSkinButton(skin, skinIndex);
            skinIndex++;
        }

        if (_selectedSkin != null) ShowSkinPreview(_selectedSkin);
        else if (availableSkins.Count > 0) ShowSkinPreview(availableSkins[0]);
        

        UpdateAllButtonOutlines();
        ResetSlider();
    }

    private List<Skin> GetSkinsForClass(ClassManager.Class classType)
    {
        List<Skin> skinsForClass = new List<Skin>();

        var allSkins = SkinsManager.Instance.GetAllSkins();

        foreach (Skin skin in allSkins)
        {
            if (skin.skinClass == classType)
            {
                bool isUnlocked = skin.battleCoinsToUnlock == 0 || PlayerPrefs.GetInt($"Skin_Unlocked_{skin.skingName}_{classType}", 0) == 1;
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
        SaveSkinForClass(infantryLoadoutCustomization._selectedClass, skin.skingName);

        // Mostra a pré-visualização da skin
        ShowSkinPreview(skin);

        // Atualiza os outlines
        UpdateAllButtonOutlines();

        // Toca o som de seleção
        if (infantryLoadoutCustomization.selectItemSfx != null)SoundManager.Play2dSoundLocal(infantryLoadoutCustomization.selectItemSfx.clip, infantryLoadoutCustomization.selectItemSfx.properties);
        

        UpdateSelectionText($"Skin selecionada: {skin.skingName}");

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
            _currentSkinPreview.transform.localPosition = new Vector3(0.7f, -1, 0);
            _currentSkinPreview.transform.localRotation = Quaternion.Euler(0, 90, 0);
            _currentSkinPreview.transform.localScale = new Vector3(70,70, 70);
        }
    }

    private void SaveSkinForClass(ClassManager.Class classType, string skinName)
    {
        PlayerPrefs.SetString($"Skin_Selected_{classType}", skinName);
        PlayerPrefs.Save();
    }

    public static string LoadCurrentSkinForClass(ClassManager.Class classType) => PlayerPrefs.GetString($"Skin_Selected_{classType}", "");

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
        foreach (GameObject button in _buttonsList.ToArray())
        {
            if (button != null) Destroy(button);
        }
        _buttonsList.Clear();
    }

    private void ResetSlider()
    {
        if (infantryLoadoutCustomization.weaponsGadgetsSlider != null)
        {
            infantryLoadoutCustomization.weaponsGadgetsSlider.gameObject.SetActive(true);
            infantryLoadoutCustomization.weaponsGadgetsSlider.value = 0f;
        }
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
        string skinName = LoadCurrentSkinForClass(classType);
        if (!string.IsNullOrEmpty(skinName)) return SkinsManager.GetSkin(skinName, classType);
        
        return null;
    }


}