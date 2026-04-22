using UnityEngine;
using UnityEngine.Rendering;

public class ThirdPersonWeaponVisibility : MonoBehaviour
{
    public void Initialize(bool isOwner)
    {
        if (isOwner)
            HideWeapon();
        else
            ShowWeapon();
    }
    
    public void HideWeapon()
    {
        foreach (MeshRenderer t in transform.GetComponentsInChildren<MeshRenderer>(true))
            t.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        
        foreach (SkinnedMeshRenderer t in transform.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            t.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
    }
    
    public void ShowWeapon()
    {
        foreach (MeshRenderer t in transform.GetComponentsInChildren<MeshRenderer>(true))
            t.shadowCastingMode = ShadowCastingMode.On;
        
        foreach (SkinnedMeshRenderer t in transform.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            t.shadowCastingMode = ShadowCastingMode.On;
    }
}