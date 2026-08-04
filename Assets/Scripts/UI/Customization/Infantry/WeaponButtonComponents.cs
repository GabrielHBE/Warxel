using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WeaponButtonComponents : MonoBehaviour
{
    private Sprite _imageHud;
    private GameObject _weaponGameObject;
    private WeaponProperties _weaponProperties;
    private InfantryLoadoutCustomization infantryLoadoutCustomization;
    private Outline _outline;
    private GameObject _buyButton;
    private bool _isUnlocked;
    private Image _weaponImage;

    public void Initialize(Sprite imageHud, GameObject weaponGameObject, WeaponProperties weaponProperties, InfantryLoadoutCustomization parent)
    {
        _imageHud = imageHud;
        _weaponGameObject = weaponGameObject;
        _weaponProperties = weaponProperties;
        infantryLoadoutCustomization = parent;

        _isUnlocked = weaponProperties.battle_coins_to_unlock == 0 ||
                      UnlockedWeapons.CheckWeaponStatus(weaponProperties.weapon_name);

        SetupImage();
        SetupText();
        SetupOutline();
        SetupBuyButton();
        SetupEvents();
        UpdateOutlineState();
    }

    private void SetupImage()
    {
        Image[] allImages = GetComponentsInChildren<Image>(true);
        if (allImages != null && allImages.Length > 0)
        {
            _weaponImage = allImages[allImages.Length - 1];
            _weaponImage.sprite = _isUnlocked ? _imageHud : InfantryLoadoutCustomization.locked_item_image;
            _weaponImage.color = Color.white;
        }
    }

    private void SetupText()
    {
        TextMeshProUGUI buttonText = GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = _weaponProperties.weapon_name;
        }
    }

    private void SetupOutline()
    {
        _outline = gameObject.AddComponent<Outline>();
        _outline.effectColor = infantryLoadoutCustomization.selectedOutlineColor;
        _outline.effectDistance = new Vector2(infantryLoadoutCustomization.outlineWidth, infantryLoadoutCustomization.outlineWidth);
        _outline.enabled = false;
    }

    private void SetupBuyButton()
    {
        if (_isUnlocked) return;

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
        buttonText.text = $"Buy: {_weaponProperties.battle_coins_to_unlock}";
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
        if (AccountManager.Instance.battle_coins < _weaponProperties.battle_coins_to_unlock)
        {
            SoundManager.Play2dSoundLocal(InfantryLoadoutCustomization.reference_purchase_denial_item_sfx.clip,
                                         InfantryLoadoutCustomization.reference_purchase_denial_item_sfx.properties);
            return;
        }

        _isUnlocked = true;

        if (_weaponImage != null)
            _weaponImage.sprite = _imageHud;

        if (_buyButton != null)
            Destroy(_buyButton);

        SetupEvents();

        SoundManager.Play2dSoundLocal(InfantryLoadoutCustomization.reference_purchase_item_sfx.clip,
                                     InfantryLoadoutCustomization.reference_purchase_item_sfx.properties);
        AccountManager.Instance.RemoveBattleCoin(_weaponProperties.battle_coins_to_unlock);
        UnlockedWeapons.UnlockWeapon(_weaponProperties.weapon_name);
    }

    private void SetupEvents()
    {
        var eventTrigger = GetComponent<EventTrigger>();
        if (eventTrigger != null) Destroy(eventTrigger);

        if (_isUnlocked)
        {
            eventTrigger = gameObject.AddComponent<EventTrigger>();
            AddEventTrigger(eventTrigger, EventTriggerType.PointerEnter, OnPointerEnter);
            AddEventTrigger(eventTrigger, EventTriggerType.PointerClick, OnPointerClick);
        }
    }

    private void OnPointerEnter() => infantryLoadoutCustomization.itemSelectionManager.OnButtonMouseEnter(_weaponGameObject);
    private void OnPointerClick() => infantryLoadoutCustomization.itemSelectionManager.OnButtonClicked(_weaponGameObject);

    private void AddEventTrigger(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction action)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(_ => action());
        trigger.triggers.Add(entry);
    }

    public void UpdateOutlineState()
    {
        if (infantryLoadoutCustomization == null || !_isUnlocked) return;

        bool isSelected = false;
        var currentOption = infantryLoadoutCustomization.loadoutOptionManager.GetCurrentOption();

        switch (currentOption)
        {
            case LoadoutOptionManager.LoadoutOption.PrimaryWeapon:
                isSelected = infantryLoadoutCustomization.GetCurrentPrimaryWeapon() == _weaponGameObject;
                break;
            case LoadoutOptionManager.LoadoutOption.SecondaryWeapon:
                isSelected = infantryLoadoutCustomization.GetCurrentSecondaryWeapon() == _weaponGameObject;
                break;
        }

        if (_outline != null)
            _outline.enabled = isSelected;
    }

    private void OnDestroy()
    {
        if (_buyButton != null) Destroy(_buyButton);
    }
}