using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class PlayerAnimation : NetworkBehaviour
{
    public ThirdPersonArms thirdPersonArms;
    [SerializeField] private Animator anim;
    [SerializeField] private PlayerController playercontroller;
    [SerializeField] private PlayerProperties playerProperties;
    private readonly SyncVar<bool> aiming = new SyncVar<bool>();
    private readonly SyncVar<bool> isSprinting = new SyncVar<bool>();
    private readonly SyncVar<bool> isProne = new SyncVar<bool>();

    #region Unity 
    private void Update()
    {
        if (IsOwner)
        {
            ShouldRequestUpdateSyncVar();
            UpdateAnimatorParameters();
        }

        UpdaateThirdPersonArms();
    }
    #endregion

    #region Animation Parameters
    private void UpdateAnimatorParameters()
    {
        if (anim == null || playercontroller == null || playerProperties == null) return;

        anim.SetFloat("Horizontal", playercontroller.moveHorizontal, 0.1f, Time.deltaTime);
        anim.SetFloat("Vertical", playercontroller.moveForward, 0.1f, Time.deltaTime);
        anim.SetBool("Crouched", playerProperties.crouched);
        anim.SetBool("Proned", playerProperties.proned);
        anim.SetBool("Walking",
            !playerProperties.sprinting &&
            !playerProperties.proned &&
            (playercontroller.moveHorizontal != 0 || playercontroller.moveForward != 0));
        anim.SetBool("Sprinting",
            !playerProperties.crouched &&
            playerProperties.sprinting &&
            !playerProperties.proned &&
            (playercontroller.moveHorizontal != 0 || playercontroller.moveForward != 0));
        anim.SetBool("Reloading", playerProperties.reloading);
        anim.SetBool("ProneTransition", playerProperties.isProneTransition);
        anim.SetBool("Roll", playerProperties.roll);
        anim.SetBool("HasLeftHandHolder", thirdPersonArms.HasLeftHandTarget());
        anim.SetBool("IsGrounded", playerProperties.grounded);
        anim.SetBool("InVehicle", playerProperties.isInVehicle);
        anim.SetBool("Aiming", playerProperties.aiming);
    }
    #endregion

    #region  Update SyncVars
    private void ShouldRequestUpdateSyncVar()
    {
        if (playerProperties.aiming != aiming.Value) RequestUpdateIsAimingSyncVar(playerProperties.aiming);
        if (playerProperties.sprinting != isSprinting.Value) RequestUpdateisSprintingSyncVar(playerProperties.sprinting);
        if ((playerProperties.isProneTransition || playerProperties.proned) != isProne.Value) RequestUpdateisProneSyncVar(playerProperties.isProneTransition || playerProperties.proned);
    }

    [ServerRpc]
    private void RequestUpdateIsAimingSyncVar(bool state) => aiming.Value = state;
    [ServerRpc]
    private void RequestUpdateisSprintingSyncVar(bool state) => isSprinting.Value = state;
    [ServerRpc]
    private void RequestUpdateisProneSyncVar(bool state) => isProne.Value = state;
    #endregion

    #region Third Person Arms
    private void UpdaateThirdPersonArms()
    {
        if (thirdPersonArms == null) return;


        bool hasLeftHandTarget = thirdPersonArms.HasLeftHandTarget();

        bool shouldIncreaseRightIK;
        if (!hasLeftHandTarget) shouldIncreaseRightIK = aiming.Value && !playerProperties.isDead.Value;
        else shouldIncreaseRightIK = (!isSprinting.Value && !playerProperties.isDead.Value) && (!isProne.Value || aiming.Value);
    
        thirdPersonArms.UpdateRightRandRigValue(shouldIncreaseRightIK);

        bool shouldIncreaseLeftIK = !playerProperties.isDead.Value && (hasLeftHandTarget || aiming.Value);
        thirdPersonArms.UpdateLeftRandRigValue(shouldIncreaseLeftIK);

        thirdPersonArms.UpdateRigWeight();
        thirdPersonArms.SetLeftHandFollowerPosition();
        thirdPersonArms.SetRightHandFollowerPosition(aiming.Value, false);
    }
    #endregion

}