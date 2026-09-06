using TMPro;
using UnityEngine;

public class CurrentAmmoUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI mag_ammo_hud;

    private ICurrentAmmoUIValues currentAmmoUIValues;

    void Start()
    {
        currentAmmoUIValues = GetComponentInParent<ICurrentAmmoUIValues>();
        if (currentAmmoUIValues == null)
        {
            Debug.LogError("CurrentAmmoUI: Could not find a component implementing ICurrentAmmoUIValues on the parent object.");
            return;
        }
    }


    void Update()
    {
        UpdateMagCount(currentAmmoUIValues.GetCurrentAmmo());
    }

    private void UpdateMagCount(string ammo)
    {
        if (mag_ammo_hud == null) return;

        mag_ammo_hud.text = ammo;
    }

}

public interface ICurrentAmmoUIValues
{
    public string GetCurrentAmmo();
}
