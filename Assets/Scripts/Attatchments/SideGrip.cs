using System.Collections;
using UnityEngine;

public class SideGrip : MonoBehaviour
{
    [Header("KeyCode")]
    public KeyCode activate;

    [Header("Object")]
    public GameObject Object;

    bool state = true;

    private CameraShake cameraShake;

    private WeaponProperties weaponProperties;

    void Start()
    {
        cameraShake = GetComponentInParent<CameraShake>();
        Object.SetActive(state);
    }

    void Update()
    {
        if (Input.GetKeyDown(activate))
        {
            weaponProperties = GetComponentInParent<WeaponProperties>();
            StartCoroutine(Shake(weaponProperties.weapon.transform));
            StartCoroutine(cameraShake.SideGripActivateShake());
            state = !state;
            Object.SetActive(state);
        }
    }
    
    public IEnumerator Shake(Transform weapon)
    {
        Quaternion originalRot = weapon.transform.localRotation;

        Quaternion upRot = originalRot * Quaternion.Euler(Random.Range(-0.5f,0.5f), Random.Range(-0.5f,0.5f), Random.Range(-0.5f,0.5f)); 

        float duration = 0.05f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            weapon.transform.localRotation = Quaternion.Lerp(originalRot, upRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        weapon.transform.localRotation = upRot;

        elapsed = 0f;
        while (elapsed < duration)
        {
            weapon.transform.localRotation = Quaternion.Lerp(upRot, originalRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        weapon.transform.localRotation = originalRot;
    }

}
