using UnityEngine;
using UnityEngine.UI;

public class SoldierHudManager : MonoBehaviour, ICurrentAmmoUIValues, ICurrentHpUIValues
{

    [Header("References")]
    [SerializeField] private PlayerProperties playerProperties;
    public GameObject hud;
    [SerializeField] private Image center_screen_dot;
    public FireModeUI fire_mode_hud;
    public ScreenBlood screenBlood;
    public DeadPlayerHud deadPlayerHud;
    private string currentAmmo;

    [Header("Canvas")]
    [SerializeField] private Canvas ammoCanvas;
    [SerializeField] private Canvas armoryCanvas;
    [SerializeField] private Canvas miscelaniousCanvas;
    [SerializeField] private Canvas deadPlayerCanvas;
    [SerializeField] private Canvas hpCanvas;

    void Update()
    {
        if (playerProperties.sprinting && !playerProperties.is_in_vehicle) center_screen_dot.enabled = true;
        else center_screen_dot.enabled = false; 
    }

    public void SetCurrentAmmo(string ammo) => currentAmmo = ammo;
    public string GetCurrentAmmo() => currentAmmo;
    public float GetCurrentHp() => playerProperties.hp.Value;
    public float GetMaxHp() => playerProperties.max_hp;

    public void ActivateInVehicleHUD()
    {
        ammoCanvas.gameObject.SetActive(false);
        armoryCanvas.gameObject.SetActive(false);
        miscelaniousCanvas.gameObject.SetActive(true);
        deadPlayerCanvas.gameObject.SetActive(false);
        hpCanvas.gameObject.SetActive(true);
    }
    public void ActivateDeadHUD()
    {
        ammoCanvas.gameObject.SetActive(false);
        armoryCanvas.gameObject.SetActive(false);
        miscelaniousCanvas.gameObject.SetActive(true);
        deadPlayerCanvas.gameObject.SetActive(true);
        hpCanvas.gameObject.SetActive(false);
    }

    public void ActivateStandardHUD()
    {
        ammoCanvas.gameObject.SetActive(true);
        armoryCanvas.gameObject.SetActive(true);
        miscelaniousCanvas.gameObject.SetActive(true);
        deadPlayerCanvas.gameObject.SetActive(false);
        hpCanvas.gameObject.SetActive(true);
    }

}