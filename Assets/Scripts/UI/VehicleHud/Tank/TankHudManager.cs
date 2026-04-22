using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TankHudManager : VehicleHudManager
{
    [SerializeField] private TankProperties tankProperties;
[SerializeField] private Tank tank;
    [SerializeField] private TextMeshProUGUI speed;
    [SerializeField] private TextMeshProUGUI throttle;

    [Header("Gun properties")]

    [SerializeField] private RectTransform heat_bar;
    [SerializeField] private TextMeshProUGUI ammo_count;
    [SerializeField] private Image heat_bar_image;
    [SerializeField] private TextMeshProUGUI main_gun_status;
    [SerializeField] private TextMeshProUGUI cannon_rotation;
    [SerializeField] private Transform cannon_rotation_min_pos;
    [SerializeField] private Transform cannon_rotation_max_pos;

    // Cores para interpolação
    [SerializeField] private Color coolColor = Color.white;
    [SerializeField] private Color hotColor = Color.red;

    public void UpdateSpeed(float speed)
    {
        this.speed.text = "Speed: " + speed.ToString("F0");
    }
    public void ChangeHeatIndicatorActive(bool status)
    {
        heat_bar.gameObject.SetActive(status);
        ammo_count.gameObject.SetActive(!status);

        primary_image_outline.enabled = !status;
        secondary_image_outline.enabled = status;
    }

    public void UpdateMainCannonStatus(string status)
    {
        ammo_count.text = status;
    }

    public void UpdateCannonRotation(float rotate_value)
    {
        cannon_rotation.text = rotate_value.ToString("F0") + " -> ";

        Vector3 speed_currentPosition = Vector3.Lerp(cannon_rotation_min_pos.transform.localPosition, cannon_rotation_max_pos.transform.localPosition, Mathf.Clamp01(rotate_value / tank.maxRotationUp));
        cannon_rotation.transform.localPosition = speed_currentPosition;
        
    }
    

    public void UpdateHeat(float overheat)
    {
        float heatPercent = Mathf.Clamp01(overheat / tankProperties.overheat_time);

        // Atualiza o tamanho da barra
        heat_bar.localScale = new Vector3(heatPercent, 1f, 1f);

        // Atualiza a cor da barra baseada no heatPercent
        if (heat_bar_image != null)
        {
            heat_bar_image.color = Color.Lerp(coolColor, hotColor, heatPercent);
        }

        if (heatPercent >= 1)
        {
            main_gun_status.text = "Gun Status: Overheated";
        }
        else if (heatPercent <= 0)
        {
            main_gun_status.text = "Gun Status: Ok";
        }
    }

}
