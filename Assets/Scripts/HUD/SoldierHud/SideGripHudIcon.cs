using UnityEngine.UI;
using TMPro;
using UnityEngine;

public class SideGripIcon : MonoBehaviour
{
    [SerializeField] private Weapon weapon;
    [SerializeField] private TextMeshProUGUI keybind_hud;
    [SerializeField] private GameObject activated_image;
    [SerializeField] private GameObject deactivated_image;


    void Update()
    {
        keybind_hud.text = SettingsHUD.Instance.WEAPON_activateSideGripButton.text;
        if (weapon.is_side_grip_activated)
        {
            activated_image.SetActive(true);
            deactivated_image.SetActive(false);
        }
        else
        {
            activated_image.SetActive(false);
            deactivated_image.SetActive(true);
        }
    }




}
