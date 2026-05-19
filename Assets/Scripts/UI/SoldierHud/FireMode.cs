using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class FireMode : MonoBehaviour
{
    [SerializeField] private GameObject auto;
    [SerializeField] private GameObject burst;
    [SerializeField] private GameObject single;

    [SerializeField] private TextMeshProUGUI key;

    void Update()
    {
        key.text = SettingsHUD.Instance.WEAPON_switchFireModeButton.text;
    }

    public void SetFireMode(WeaponProperties.FireMode fire_mode)
    {
        switch (fire_mode)
        {
            case WeaponProperties.FireMode.Auto:
                auto.SetActive(true);
                burst.SetActive(false);
                single.SetActive(false);
                break;

            case WeaponProperties.FireMode.Burst:
                auto.SetActive(false);
                burst.SetActive(true);
                single.SetActive(false);
                break;

            case WeaponProperties.FireMode.Single:
                auto.SetActive(false);
                burst.SetActive(false);
                single.SetActive(true);
                break;

            default:
                break;
        }
    }
}
