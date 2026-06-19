
using System.Collections;
using UnityEngine;

public class WeaponHolder : MonoBehaviour
{
    [SerializeField] private GameObject rightHand_destiny;
    [SerializeField] private GameObject leftHand_destiny;
    [SerializeField] private GameObject weapon_extractor;
    private Transform weapon_mag;

    private CameraShake cameraShake;

    private FirstPersonArms firstPersonArms;

    public void Initialize()
    {
        cameraShake = GetComponentInParent<CameraShake>();

    }

    public void SetWeaponMag(Transform weapon_mag)
    {
        this.weapon_mag = weapon_mag;
    }

    public void ResetWeaponState()
    {
        firstPersonArms = GetComponentInParent<FirstPersonArms>();
        LeftHandToWeapon(0);
        RightHandToWeapon(0);
    }

    #region Left Hand
    public void LeftHandToMag(float moveTime = 0.3f)
    {
        firstPersonArms.MoveLeftHand(weapon_mag, moveTime);
        cameraShake.RequestShake(0.5f, 0.5f);
    }

    public void LeftHandToWeapon(float moveTime = 0.3f)
    {
        firstPersonArms.MoveLeftHand(leftHand_destiny.transform, moveTime);
        cameraShake.RequestShake(0.5f, 0.5f);
    }

    public void LeftHandToExtractor(float moveTime = 0.3f)
    {
        firstPersonArms.MoveLeftHand(weapon_extractor.transform, moveTime);
        cameraShake.RequestShake(0.5f, 0.5f);
    }
    #endregion

    #region Right Hand
    public void RightHandToExtractor(float moveTime = 0.3f)
    {
        firstPersonArms.MoveRightHand(weapon_extractor.transform, moveTime);
        cameraShake.RequestShake(0.5f, 0.5f);
    }

    public void RightHandToWeapon(float moveTime = 0.3f)
    {
        firstPersonArms.MoveRightHand(rightHand_destiny.transform, moveTime);
        cameraShake.RequestShake(0.5f, 0.5f);
    }

    public void RightHandToMag(float moveTime = 0.3f)
    {
        firstPersonArms.MoveRightHand(weapon_mag, moveTime);
        cameraShake.RequestShake(0.5f, 0.5f);
    }
    #endregion

}