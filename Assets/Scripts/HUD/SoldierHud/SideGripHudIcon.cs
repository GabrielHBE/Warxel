using UnityEngine.UI;
using TMPro;
using UnityEngine;

public class SideGripIcon : MonoBehaviour
{
    [SerializeField] private Weapon weapon;
    [SerializeField] private TextMeshProUGUI keybind_hud;
    [SerializeField] private GameObject activated_image;
    [SerializeField] private GameObject deactivated_image;
    private Settings settings;

    void Awake()
    {
        settings = GameObject.FindGameObjectWithTag("GeneralHUD").GetComponent<Settings>();
    }

    void Update()
    {
        keybind_hud.text = settings.WEAPON_activateSideGripButton.text;
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
