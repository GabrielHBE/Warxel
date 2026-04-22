using FishNet.Object;
using UnityEngine;
using UnityEngine.Rendering;

public class ThirdPersonWeaponController : MonoBehaviour
{
    [SerializeField] private PlayerProperties playerProperties;
    private Transform shoot_pos;
    private GameObject muzzle;
    private bool isInitialized = false;


    void Awake()
    {
        // Inicialização segura
        InitializeShootPos();

    }


    private void InitializeShootPos()
    {
        foreach (Transform t in transform.GetComponentsInChildren<Transform>(true))
        {
            if (t.gameObject.name == "ShootPos")
            {
                shoot_pos = t;
                break;
            }
        }
        isInitialized = true;
    }

    // Agora recebe isOwner para decidir a visibilidade correta no momento do spawn da arma
    public void Reestart(bool isOwner)
    {
        if (!isInitialized)
        {
            //InitializeShootPos();
        }

        if (isOwner)
        {
            HideWeapon();
        }
        else
        {
            ShowWeapon();
        }
    }

    public void HideWeapon()
    {

        foreach (MeshRenderer t in transform.GetComponentsInChildren<MeshRenderer>(true))
        {
            t.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }

        foreach (SkinnedMeshRenderer t in transform.GetComponentsInChildren<SkinnedMeshRenderer>(true))
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

        foreach (SkinnedMeshRenderer t in transform.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            t.shadowCastingMode = ShadowCastingMode.On;
        }
    }

    public void CreateMuzzle(GameObject muzzlePrefab)
    {
        if (shoot_pos != null)
        {
            if (muzzle != null) Destroy(muzzle);
            this.muzzle = Instantiate(muzzlePrefab, shoot_pos);
        }
    }

    public void DeleteMuzzle()
    {
        if (muzzle != null)
        {
            Destroy(muzzle);
            muzzle = null;
        }
    }

    
}