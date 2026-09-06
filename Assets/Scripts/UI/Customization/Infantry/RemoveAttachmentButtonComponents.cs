using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RemoveAttachmentButtonComponents : MonoBehaviour
{
    private string _partType;
    private GameObject _weaponBeingCustomized;
    private WeaponCustomizationManager _customizationManager;
    private Outline _outline;

    public void Initialize(string partType, GameObject weaponBeingCustomized, WeaponCustomizationManager customizationManager)
    {
        _partType = partType;
        _weaponBeingCustomized = weaponBeingCustomized;
        _customizationManager = customizationManager;

        SetupText();
        SetupOutline();
        SetupEvents();
    }

    private void SetupText()
    {
        TextMeshProUGUI buttonText = GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = "Remove";
            buttonText.color = Color.red;
        }
    }

    private void SetupOutline()
    {
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
        if (_outline != null) _outline.enabled = true;
    }

    private void OnPointerExit()
    {
        if (_outline != null) _outline.enabled = false;
    }

    private void OnRemoveClicked()
    {
        _customizationManager?.RemoveAttachment(_partType, _weaponBeingCustomized);
    }

    private void AddEventTrigger(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction action)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(_ => action());
        trigger.triggers.Add(entry);
    }
}
