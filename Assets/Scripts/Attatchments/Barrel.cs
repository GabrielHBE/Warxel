using UnityEngine;

public class Barrel : Attatchment
{
    [SerializeField] private Transform nozzleHolder;
    [SerializeField] private Transform nozzleHolderPos;

    [Header("Changes")]
    public float horizontalRecoilChange;
    public float verticalRecoilChange;
    public float firstShootRecoilChange;
    public int muzzleVelocityChange;
    public float adsSpeedChange;

    private void OnEnable()
    {
        InitializeWeaponProperties();

        if (nozzleHolderPos == null) nozzleHolderPos = transform.Find("NozzleHolderPos");
        if (nozzleHolder == null || nozzleHolderPos == null) return;
        
        weaponProperties.shootPos = nozzleHolderPos;

        SetParent();
    }

    private void SetParent()
    {
        nozzleHolder.SetParent(nozzleHolderPos, false);
        nozzleHolder.localPosition = Vector3.zero;
        nozzleHolder.localRotation = Quaternion.identity;
    }

}
