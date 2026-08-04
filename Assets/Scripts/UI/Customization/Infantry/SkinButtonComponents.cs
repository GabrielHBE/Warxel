// SkinButtonComponents.cs
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkinButtonComponents : MonoBehaviour
{
    private Skin _skin;
    private InfantryLoadoutCustomization infantryLoadoutCustomization;
    private Outline _outline;
    private GameObject _buyButton;
    private bool _isUnlocked;
    private Image _skinImage;

    public void Initialize(Skin skin, InfantryLoadoutCustomization parent)
    {
        _skin = skin;
        infantryLoadoutCustomization = parent;
        
        _isUnlocked = skin.battleCoinsToUnlock == 0 ||
                      PlayerPrefs.GetInt($"Skin_Unlocked_{skin.skingName}_{skin.skinClass}", 0) == 1;

        SetupImage(skin.HudIcon);
        SetupText();
        SetupOutline();
        SetupBuyButton();
        SetupEvents();
        UpdateOutlineState();
    }

    private void SetupImage(Sprite image)
    {
        Image[] allImages = GetComponentsInChildren<Image>(true);
        if (allImages != null && allImages.Length > 0)
        {
            _skinImage = allImages[allImages.Length - 1];
            
            if (_skinImage != null) _skinImage.sprite = image;

        }
    }

    private void SetupText()
    {
        TextMeshProUGUI buttonText = GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null) buttonText.text = _skin.skingName;
        
    }

    private void SetupOutline()
    {
        _outline = gameObject.AddComponent<Outline>();
        _outline.effectColor = infantryLoadoutCustomization.selectedOutlineColor;
        _outline.effectDistance = new Vector2(infantryLoadoutCustomization.outlineWidth, 
                                              infantryLoadoutCustomization.outlineWidth);
        _outline.enabled = false;
    }

    private void SetupBuyButton()
    {
        if (_isUnlocked || _skin.battleCoinsToUnlock == 0) return;

        _buyButton = Instantiate(InfantryLoadoutCustomization.BuyWeaponButton.gameObject, transform);
        _buyButton.transform.SetParent(transform, false);

        RectTransform rectTransform = _buyButton.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1, 0.5f);
        rectTransform.anchorMax = new Vector2(1, 0.5f);
        rectTransform.pivot = new Vector2(0, 0.5f);
        rectTransform.anchoredPosition = new Vector2(10, 0);
        rectTransform.sizeDelta = new Vector2(150, 40);

        Image bgImage = _buyButton.GetComponent<Image>();
        bgImage.color = new Color(0.2f, 0.5f, 0.2f);

        TextMeshProUGUI buttonText = _buyButton.GetComponentInChildren<TextMeshProUGUI>();
        buttonText.text = $"Buy: {_skin.battleCoinsToUnlock}";
        buttonText.fontSize = 18;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.white;

        Button button = _buyButton.GetComponent<Button>();
        button.onClick.AddListener(OnBuyButtonClicked);

        buttonText.outlineWidth = 0.2f;
        buttonText.outlineColor = Color.black;
    }

    private void OnBuyButtonClicked()
    {
        if (AccountManager.Instance.battle_coins < _skin.battleCoinsToUnlock)
        {
            SoundManager.Play2dSoundLocal(
                InfantryLoadoutCustomization.reference_purchase_denial_item_sfx.clip,
                InfantryLoadoutCustomization.reference_purchase_denial_item_sfx.properties);
            return;
        }

        _isUnlocked = true;

        if (_skinImage != null)
            _skinImage.color = Color.white;

        if (_buyButton != null)
            Destroy(_buyButton);

        SetupEvents();

        SoundManager.Play2dSoundLocal(
            InfantryLoadoutCustomization.reference_purchase_item_sfx.clip,
            InfantryLoadoutCustomization.reference_purchase_item_sfx.properties);
        
        AccountManager.Instance.RemoveBattleCoin(_skin.battleCoinsToUnlock);
        PlayerPrefs.SetInt($"Skin_Unlocked_{_skin.skingName}_{_skin.skinClass}", 1);
        PlayerPrefs.Save();
    }

    private void SetupEvents()
    {
        var eventTrigger = GetComponent<EventTrigger>();
        if (eventTrigger != null) Destroy(eventTrigger);

        if (_isUnlocked)
        {
            eventTrigger = gameObject.AddComponent<EventTrigger>();
            AddEventTrigger(eventTrigger, EventTriggerType.PointerClick, OnSkinClicked);
        }
    }

    private void OnSkinClicked()
    {
        if (infantryLoadoutCustomization.skinSelectionManager != null) infantryLoadoutCustomization.skinSelectionManager.OnSkinSelected(_skin);
        
    }

    private void AddEventTrigger(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction action)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(_ => action());
        trigger.triggers.Add(entry);
    }

    public void UpdateOutlineState()
    {
        if (infantryLoadoutCustomization == null || !_isUnlocked) return;

        string currentSkinName = PlayerPrefs.GetString(
            $"Skin_Selected_{infantryLoadoutCustomization._selectedClass}", "");
        
        bool isSelected = !string.IsNullOrEmpty(currentSkinName) && 
                          currentSkinName == _skin.skingName;

        if (_outline != null)
            _outline.enabled = isSelected;
    }

    private void OnDestroy()
    {
        if (_buyButton != null) Destroy(_buyButton);
    }
}