
using UnityEngine;

public class WeaponHolder : MonoBehaviour
{
    public GameObject rightHand_destiny;
    public GameObject leftHand_destiny;
    public Transform weapon_mag;
    public GameObject weapon_extractor;

    private CameraShake cameraShake;

    private FirstPersonArms firstPersonArms;

    void Awake()
    {
        cameraShake = GetComponentInParent<CameraShake>();
        weapon_mag = GetChildMag(transform, "MagHandPosition");


    }

    Transform GetChildMag(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
            {
                return child;
            }

            Transform found = GetChildMag(child, name);
            if (found != null)
            {
                return found;
            }

        }

        return null;
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