using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class WeaponIcon : MonoBehaviour
{
    [SerializeField] private SwitchWeapon switchWeapon;

    [Header("Images")]
    [SerializeField] private Image primary_slot;
    [SerializeField] private Image secondary_slot;
    [SerializeField] private Image gadget1_slot;
    [SerializeField] private Image gadget2_slot;

    [Header("OutLines")]
    [SerializeField] private Outline primary_outline;
    [SerializeField] private Outline secondary_outline;
    [SerializeField] private Outline gadget1_outline;
    [SerializeField] private Outline gadget2_outline;
    void Start()
    {
        if (switchWeapon.primary != null) primary_slot.sprite = switchWeapon.primary.GetComponent<WeaponProperties>().icon_hud;
        if (switchWeapon.secondary != null) secondary_slot.sprite = switchWeapon.secondary.GetComponent<WeaponProperties>().icon_hud;
        if (switchWeapon.gadget1 != null ) gadget1_slot.sprite = switchWeapon.gadget1.GetComponent<Gadget>().icon_hud;
        if (switchWeapon.gadget2 != null ) gadget2_slot.sprite = switchWeapon.gadget2.GetComponent<Gadget>().icon_hud;

    }

    void Update()
    {
        if (switchWeapon.currentWeapon == 1)
        {
            primary_outline.enabled = true;
            secondary_outline.enabled = false;
            gadget1_outline.enabled = false;
            gadget2_outline.enabled = false;
        }
        else if (switchWeapon.currentWeapon == 2)
        {
            primary_outline.enabled = false;
            secondary_outline.enabled = true;
            gadget1_outline.enabled = false;
            gadget2_outline.enabled = false;
        }
        else if (switchWeapon.currentWeapon == 3)
        {
            primary_outline.enabled = false;
            secondary_outline.enabled = false;
            gadget1_outline.enabled = true;
            gadget2_outline.enabled = false;
        }
        else if (switchWeapon.currentWeapon == 4)
        {
            primary_outline.enabled = false;
            secondary_outline.enabled = false;
            gadget1_outline.enabled = false;
            gadget2_outline.enabled = true;
        }
    }


}
