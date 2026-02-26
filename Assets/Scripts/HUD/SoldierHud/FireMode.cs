using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class FireMode : MonoBehaviour
{
    [SerializeField] private GameObject auto;
    [SerializeField] private GameObject burst;
    [SerializeField] private GameObject single;

    [SerializeField] private TextMeshProUGUI key;

    private Settings settings;

    void Awake()
    {
        settings = GameObject.FindGameObjectWithTag("GeneralHUD").GetComponent<Settings>();
    }

    void Update()
    {
        key.text = settings.WEAPON_switchFireModeButton.text;
    }

    public void SetFireMode(string fire_mode)
    {
        switch (fire_mode)
        {
            case "Auto":
                auto.SetActive(true);
                burst.SetActive(false);
                single.SetActive(false);
                break;

            case "Burst":
                auto.SetActive(false);
                burst.SetActive(true);
                single.SetActive(false);
                break;

            case "Single":
                auto.SetActive(false);
                burst.SetActive(false);
                single.SetActive(true);
                break;

            default:
                break;
        }
    }
}
