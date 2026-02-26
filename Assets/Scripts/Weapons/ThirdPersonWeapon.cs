using UnityEngine;
using UnityEngine.Rendering;

public class ThirdPersonWeapon : MonoBehaviour
{
    [SerializeField] private PlayerProperties playerProperties;
    private Transform shoot_pos;
    private GameObject muzzle;

    public void Reestart()
    {
        HideWeapon();

        foreach (Transform t in transform.GetComponentsInChildren<Transform>(true))
        {
            if (t.gameObject.name == "ShootPos")
            {
                shoot_pos = t;
            }
        }
    }

    public void HideWeapon()
    {
        foreach (MeshRenderer t in transform.GetComponentsInChildren<MeshRenderer>(true))
        {
            t.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }
    }

    public void ShowWeapon()
    {
        foreach (MeshRenderer t in transform.GetComponentsInChildren<MeshRenderer>(true))
        {
            t.shadowCastingMode = ShadowCastingMode.On;
        }
    }

    public void CreateMuzzle(GameObject muzzle)
    {

        this.muzzle = Instantiate(muzzle, shoot_pos);
    }

    public void DeleteMuzzle()
    {
        Destroy(muzzle);
    }



}
