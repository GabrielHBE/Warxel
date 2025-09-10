using System.Collections.Generic;
using UnityEngine;

public class MagCounter : MonoBehaviour
{
    public Transform weaponParent;
    public GameObject mag_count;

    private List<GameObject> mags = new List<GameObject>();

    float add_space;

    public void Restart()
    {

        /*
        if (mags != null)
        {
            Delete();
        }

        add_space = 100;

        WeaponProperties weaponProperties = GetActiveWeapon();



        for (int i = 0; i < weaponProperties.mag_count - 1; i++)
        {
            GameObject mag = Instantiate(
            mag_count,
            mag_count.transform.position,
            mag_count.transform.rotation,
            transform.parent
            );
            mags.Add(mag);

            mag.transform.localPosition = new Vector3(mag.transform.localPosition.x - add_space, mag.transform.localPosition.y, mag.transform.localPosition.z);
            add_space += 100;
        }
        */

    }

    WeaponProperties GetActiveWeapon()
    {
        foreach (Transform weapon in weaponParent) // Itera pelos filhos
        {
            if (weapon.gameObject.activeSelf) // Verifica se está ativo
            {
                return weapon.GetComponent<WeaponProperties>(); // Pega o script
            }
        }
        return null; // Nenhuma arma ativa
    }

    void Delete()
    {
        for (int i = 0; i < mags.Count; i++)
        {
            Destroy(mags[i]);
        }
    }

}
