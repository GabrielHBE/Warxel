
using UnityEngine;

public class EquippableItemHandTargets : MonoBehaviour
{
    [SerializeField] private Transform rightHandPos;
    [SerializeField] private Transform leftHandPos;
    [SerializeField] private Transform actionTarget;
    [SerializeField] private Transform magazineTarget;
    [SerializeField] private Transform[] customHandPositions;

    private FirstPersonArms firstPersonArms => FirstPersonArms.Instance;
    private const float MOVE_TIME = 1;

    #region Left Hand
    public void LeftHandToMag() => firstPersonArms.MoveLeftHand(magazineTarget, MOVE_TIME);
    public void LeftHandToWeapon() => firstPersonArms.MoveLeftHand(leftHandPos, MOVE_TIME);
    public void LeftHandToExtractor() => firstPersonArms.MoveLeftHand(actionTarget, MOVE_TIME);
    public void LeftHandToCustomPosition(int index) => firstPersonArms.MoveLeftHand(customHandPositions[index], MOVE_TIME);
    #endregion

    #region Right Hand
    public void RightHandToExtractor() => firstPersonArms.MoveRightHand(actionTarget, MOVE_TIME);
    public void RightHandToWeapon() => firstPersonArms.MoveRightHand(rightHandPos, MOVE_TIME);
    public void RightHandToMag() => firstPersonArms.MoveRightHand(magazineTarget, MOVE_TIME);
    public void RightHandToCustomPosition(int index) => firstPersonArms.MoveRightHand(customHandPositions[index], MOVE_TIME);
    #endregion

    #region Helpers
    public void SetWeaponMag(Transform magazineTarget)
    {
        if (this.magazineTarget != null) return;

        this.magazineTarget = magazineTarget;
    }
    public void ResetHandTargets()
    {
        firstPersonArms.MoveLeftHand(leftHandPos, 0);
        firstPersonArms.MoveRightHand(rightHandPos, 0);
    }
    public Transform GetLeftHandPos() => leftHandPos;
    public Transform GetRightHandPos() => rightHandPos;
    #endregion

}