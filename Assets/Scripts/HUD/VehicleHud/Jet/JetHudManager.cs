using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JetHudManager : VehicleHudManager
{
    [SerializeField] private TextMeshProUGUI hud_gravity_controller;
    [SerializeField] private TextMeshProUGUI hud_Gforce_controller;
    [SerializeField] private TextMeshProUGUI hud_speed_controller;
    [SerializeField] private GameObject hud_speed_controller_min_pos;
    [SerializeField] private GameObject hud_speed_controller_max_pos;
    [SerializeField] private TextMeshProUGUI hud_altidude_controller;
    [SerializeField] private GameObject hud_altidude_controller_max_pos;
    [SerializeField] private GameObject hud_altidude_controller_min_pos;

 
    [Header("Gun properties")]
    [SerializeField] private JetProperties jetProperties;
    [SerializeField] private RectTransform heat_bar;
    [SerializeField] private TextMeshProUGUI ammo_count;
    [SerializeField] private Image heat_bar_image;
    [SerializeField] private TextMeshProUGUI main_gun_status;

    // Cores para interpolação
    [SerializeField] private Color coolColor = Color.white;
    [SerializeField] private Color hotColor = Color.red;

    public void UpdateAltitude(float altitude)
    {
        if (hud_altidude_controller == null) return;
        hud_altidude_controller.text = altitude.ToString("F0");

        Vector3 altitude_currentPosition = Vector3.Lerp(
            hud_altidude_controller_min_pos.transform.localPosition,
            hud_altidude_controller_max_pos.transform.localPosition,
            Mathf.Clamp01(altitude / MapSettings.Instante.max_altitude)
        );

        hud_altidude_controller.transform.localPosition = altitude_currentPosition;
    }

    public void UpdateGforce(float gforce)
    {
        if (hud_Gforce_controller != null) hud_Gforce_controller.text = gforce.ToString("F0");
    }

    public void UpdateSpeed(float speed)
    {
        if (hud_speed_controller == null) return;
        hud_speed_controller.text = speed.ToString("F0");

        Vector3 speed_currentPosition = Vector3.Lerp(hud_speed_controller_min_pos.transform.localPosition, hud_speed_controller_max_pos.transform.localPosition, Mathf.Clamp01(speed / 500));
        hud_speed_controller.transform.localPosition = speed_currentPosition;
    }

    public void UpdateGravity(float gravity)
    {
        if (hud_gravity_controller != null) hud_gravity_controller.text = gravity.ToString("F1") + " ↓";
    }

    public void UpdateHeat(float overheat)
    {
        float heatPercent = Mathf.Clamp01(overheat / jetProperties.overheat_time);

        // Atualiza o tamanho da barra
        heat_bar.localScale = new Vector3(heatPercent, 1f, 1f);

        // Atualiza a cor da barra baseada no heatPercent
        if (heat_bar_image != null)
        {
            heat_bar_image.color = Color.Lerp(coolColor, hotColor, heatPercent);
        }

        if (heatPercent >= 1)
        {
            main_gun_status.text = "Main Cannon Status: Overheated";
        }
        else if (heatPercent <= 0)
        {
            main_gun_status.text = "Main Cannon Status: Ok";
        }
    }


    

    public void ChangeHeatIndicatorActive(bool status)
    {
        heat_bar.gameObject.SetActive(status);
        ammo_count.gameObject.SetActive(!status);

        primary_image_outline.enabled = status;
        secondary_image_outline.enabled = !status;
    }

}
