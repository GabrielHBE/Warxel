using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GadgetButtonComponents : MonoBehaviour
{
    private Sprite _imageHud;
    private GameObject _gadgetGameObject;
    private Gadget _gadget;
    private InfantryLoadoutCustomization infantryLoadoutCustomization;
    private Outline _outline;

    public void Initialize(Sprite imageHud, GameObject gadgetGameObject, Gadget gadget, InfantryLoadoutCustomization parent)
    {
        _imageHud = imageHud;
        _gadgetGameObject = gadgetGameObject;
        _gadget = gadget;
        infantryLoadoutCustomization = parent;

        SetupImage();
        SetupOutline();
        SetupEvents();
        UpdateOutlineState();
    }

    private void SetupImage()
    {
        Image[] allImages = GetComponentsInChildren<Image>(true);
        if (allImages != null && allImages.Length > 0) allImages[allImages.Length - 1].sprite = _imageHud;
        
    }

    private void SetupOutline()
    {
        _outline = gameObject.AddComponent<Outline>();
        _outline.effectColor = Color.white;
        _outline.useGraphicAlpha = true;
        _outline.enabled = false;
    }

    private void SetupEvents()
    {
        var eventTrigger = gameObject.AddComponent<EventTrigger>();
        AddEventTrigger(eventTrigger, EventTriggerType.PointerEnter, OnPointerEnter);
        AddEventTrigger(eventTrigger, EventTriggerType.PointerClick, OnPointerClick);
    }

    private void OnPointerEnter() => infantryLoadoutCustomization.itemSelectionManager.OnButtonMouseEnter(_gadgetGameObject);
    private void OnPointerClick() =>  infantryLoadoutCustomization.itemSelectionManager.OnButtonClicked(_gadgetGameObject);
    
    private void AddEventTrigger(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction action)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(_ => action());
        trigger.triggers.Add(entry);
    }

    public void UpdateOutlineState()
    {
        if (infantryLoadoutCustomization == null) return;

        bool isSelected = infantryLoadoutCustomization.GetCurrentGadget1() == _gadgetGameObject;

        if (_outline != null)
            _outline.enabled = isSelected;
    }
}