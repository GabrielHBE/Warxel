using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CustomizationButtonComponents : MonoBehaviour
{
    private Sprite _imageHud;
    private GameObject _partGameObject;
    private MonoBehaviour _component;
    private string _partType;
    private string _partName;
    private InfantryLoadoutCustomization infantryLoadoutCustomization;
    private Outline _outline;
    private bool _isRemoveButton;
    private bool _isAttachmentUnlocked;

    public void Initialize(Sprite imageHud, GameObject partGameObject, MonoBehaviour component,
                          string partType, string partName, bool isRemoveButton, InfantryLoadoutCustomization parent)
    {
        _imageHud = imageHud;
        _partGameObject = partGameObject;
        _component = component;
        _partType = partType;
        _partName = partName;
        infantryLoadoutCustomization = parent;
        _isRemoveButton = isRemoveButton;
        
        Attatchment attatchment = component.GetComponent<Attatchment>();
        _isAttachmentUnlocked = attatchment != null && attatchment.IsAttatchmentUnlocked();

        SetupImage();
        SetupText();
        SetupOutline();
        SetupEvents();
        UpdateOutlineState();
    }

    private void SetupImage()
    {
        Image[] allImages = GetComponentsInChildren<Image>(true);
        if (allImages == null || allImages.Length == 0) return;

        Image attachmentImage = allImages[allImages.Length - 1];
        if (_imageHud == null)
        {
            Destroy(attachmentImage);
            return;
        }

        attachmentImage.sprite = _isAttachmentUnlocked ? _imageHud : InfantryLoadoutCustomization.locked_item_image;
    }

    private void SetupText()
    {
        TextMeshProUGUI buttonText = GetComponentInChildren<TextMeshProUGUI>();
        Attatchment attachment = _component as Attatchment;
        if (buttonText != null)
        {
            buttonText.text = attachment != null
                ? $"{_partName}  [{attachment.attatchmentPoints:0.##} pts]"
                : _partName;
        }
    }

    private void SetupOutline()
    {
        if (_isRemoveButton) return;

        _outline = gameObject.AddComponent<Outline>();
        _outline.effectColor = infantryLoadoutCustomization.selectedOutlineColor;
        _outline.effectDistance = new Vector2(infantryLoadoutCustomization.outlineWidth, infantryLoadoutCustomization.outlineWidth);
        _outline.useGraphicAlpha = true;
        _outline.enabled = false;
    }

    private void SetupEvents()
    {
        var eventTrigger = gameObject.AddComponent<EventTrigger>();
        AddEventTrigger(eventTrigger, EventTriggerType.PointerEnter, OnPointerEnter);
        AddEventTrigger(eventTrigger, EventTriggerType.PointerExit, OnPointerExit);
        
        if (_isAttachmentUnlocked) AddEventTrigger(eventTrigger, EventTriggerType.PointerClick, OnPointerClick);
        
    }

    private void OnPointerEnter()
    {
        if (!_isRemoveButton && _outline != null && !_outline.enabled)
        {
            _outline.effectColor = Color.gray;
            _outline.effectDistance = new Vector2(2f, 2f);
            _outline.enabled = true;
        }

        Attatchment attachment = _component as Attatchment;
        if (attachment != null) infantryLoadoutCustomization.PreviewAttachmentStats(attachment);
    }

    private void OnPointerExit()
    {
        infantryLoadoutCustomization.ClearAttachmentStatsPreview();
        if (_isRemoveButton || _outline == null) return;

        if (!IsSelected()) _outline.enabled = false;
    }

    private void OnPointerClick() => infantryLoadoutCustomization.weaponCustomizationManager.OnCustomizationItemClicked(_partGameObject, _component, _partType);

    private void AddEventTrigger(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction action)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(_ => action());
        trigger.triggers.Add(entry);
    }

    public void UpdateOutlineState()
    {
        if (_isRemoveButton || infantryLoadoutCustomization == null || _component == null) return;

        bool isSelected = IsSelected();

        if (_outline != null)
        {
            if (isSelected)
            {
                _outline.effectColor = infantryLoadoutCustomization.selectedOutlineColor;
                _outline.effectDistance = new Vector2(infantryLoadoutCustomization.outlineWidth, infantryLoadoutCustomization.outlineWidth);
                _outline.enabled = true;
            }
            else _outline.enabled = false;
        }
    }

    private bool IsSelected()
    {
        if (infantryLoadoutCustomization == null || infantryLoadoutCustomization._weaponBeingCustomized == null || _component == null) return false;

        Type componentType = _component.GetType();
        Component[] components = infantryLoadoutCustomization._weaponBeingCustomized.GetComponentsInChildren(componentType, true);

        foreach (Component comp in components)
        {
            if (comp.gameObject.activeInHierarchy && comp.gameObject.name == _partGameObject.name) return true;
        }

        return false;
    }
}
