using System.Collections;
using UnityEngine;

public class SideGrip : Attatchment
{

    [Header("Configurations")]
    [SerializeField] private GameObject Object;
    [SerializeField] private bool show_in_third_person;
    
    bool state = true;
    private Weapon weapon;

    void Awake()
    {
        weapon = GetComponentInParent<Weapon>();
        weaponProperties = GetComponentInParent<WeaponProperties>();
        if(Object!=null) Object.SetActive(state);
    }

    void Update()
    {
        if (!gameObject.activeSelf) return;

        if (InputManager.GetKeyDown(Settings.Instance._keybinds.WEAPON_activateSideGrip))
        {
            if (weaponProperties != null) StartCoroutine(Shake(weaponProperties.transform));
            state = !state;

            Object.SetActive(state);
        }

        if (weapon != null) weapon.is_side_grip_activated = state;
    }

    public IEnumerator Shake(Transform weapon)
    {
        Quaternion originalRot = weapon.localRotation;

        Quaternion upRot = originalRot * Quaternion.Euler(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f));

        float duration = 0.05f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            weapon.localRotation = Quaternion.Lerp(originalRot, upRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        weapon.localRotation = upRot;

        elapsed = 0f;
        while (elapsed < duration)
        {
            weapon.localRotation = Quaternion.Lerp(upRot, originalRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        weapon.localRotation = originalRot;
    }

}
