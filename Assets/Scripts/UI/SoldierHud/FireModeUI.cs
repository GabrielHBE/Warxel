using TMPro;
using UnityEngine;

public class FireModeUI : MonoBehaviour
{
    [SerializeField] private GameObject auto;
    [SerializeField] private GameObject burst;
    [SerializeField] private GameObject single;

    [SerializeField] private TextMeshProUGUI key;

    void Update()
    {
        key.text = SettingsHUD.Instance.WEAPON_switchFireModeButton.text;
    }

    public void SetFireMode(Firing.FireMode fire_mode)
    {
        switch (fire_mode)
        {
            case Firing.FireMode.Auto:
                auto.SetActive(true);
                burst.SetActive(false);
                single.SetActive(false);
                break;

            case Firing.FireMode.Burst:
                auto.SetActive(false);
                burst.SetActive(true);
                single.SetActive(false);
                break;

            case Firing.FireMode.Single:
                auto.SetActive(false);
                burst.SetActive(false);
                single.SetActive(true);
                break;

            default:
                break;
        }
    }
}
