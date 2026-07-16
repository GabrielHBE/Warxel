using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class PlayerAnimation : NetworkBehaviour
{
    public ThirdPersonArms thirdPersonArms;
    [SerializeField] private Animator anim;
    [SerializeField] private PlayerController playercontroller;
    [SerializeField] private PlayerProperties playerProperties;
    private readonly SyncVar<bool> isAiming = new SyncVar<bool>();
    private readonly SyncVar<bool> isSprinting = new SyncVar<bool>();
    private readonly SyncVar<bool> isProne = new SyncVar<bool>();

    #region Unity 
    void Awake()
    {
        playerProperties.is_dead.OnChange += OnIsDeadChanded;
    }

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
        anim.SetBool("Proned", playerProperties.is_proned);
        anim.SetBool("Walking",
            !playerProperties.sprinting &&
            !playerProperties.is_proned &&
            (playercontroller.moveHorizontal != 0 || playercontroller.moveForward != 0));

        anim.SetBool("Sprinting",
            !playerProperties.crouched &&
            playerProperties.sprinting &&
            !playerProperties.is_proned &&
            (playercontroller.moveHorizontal != 0 || playercontroller.moveForward != 0));

        anim.SetBool("Reloading", playerProperties.is_reloading);
        anim.SetBool("ProneTransition", playerProperties.isProneTransition);
        anim.SetBool("Roll", playerProperties.roll);

        //anim.SetFloat("Rotation", InputManager.GetAxis("Mouse X"));
        anim.SetBool("HasLeftHandHolder", thirdPersonArms.HasLeftHandTarget());
        anim.SetBool("IsGrounded", playerProperties.isGrounded);
        anim.SetBool("InVehicle", playerProperties.is_in_vehicle);
        anim.SetBool("Aiming", playerProperties.is_aiming);
    }

    private void OnIsDeadChanded(bool prev, bool next, bool asServer)
    {
        anim.SetBool("Dead", next);
        if (next)
        {
            SetDeathAnimationIndex();
        }
    }

    public void SetDeathAnimationIndex()
    {
        anim.SetInteger("RandomDeadAnimation", Random.Range(1, 8));
    }
    #endregion

    #region  Update SyncVars
    private void ShouldRequestUpdateSyncVar()
    {
        if (playerProperties.is_aiming != isAiming.Value)
        {
            RequestUpdateIsAimingSyncVar(playerProperties.is_aiming);
        }

        if (playerProperties.sprinting != isSprinting.Value)
        {
            RequestUpdateisSprintingSyncVar(playerProperties.sprinting);
        }

        if ((playerProperties.isProneTransition || playerProperties.is_proned) != isProne.Value)
        {
            RequestUpdateisProneSyncVar(playerProperties.isProneTransition || playerProperties.is_proned);
        }

    }

    [ServerRpc]
    private void RequestUpdateIsAimingSyncVar(bool state) => isAiming.Value = state;
    [ServerRpc]
    private void RequestUpdateisSprintingSyncVar(bool state) => isSprinting.Value = state;
    [ServerRpc]
    private void RequestUpdateisProneSyncVar(bool state) => isProne.Value = state;
    #endregion

    #region Third Person Arms
    private void UpdaateThirdPersonArms()
    {
        if (thirdPersonArms == null) return;

        // Verifica se tem left hand target
        bool hasLeftHandTarget = thirdPersonArms.HasLeftHandTarget();

        // Right hand IK:
        // - Se NÃO tem leftHandTarget: só ativa se estiver mirando
        // - Se tem leftHandTarget: segue a lógica normal (sprint, dead, prone)
        bool shouldIncreaseRightIK;
        if (!hasLeftHandTarget)
        {
            // Sem suporte para mão esquerda: só ativa se estiver mirando
            shouldIncreaseRightIK = isAiming.Value && !playerProperties.is_dead.Value;
        }
        else
        {
            // Com suporte para mão esquerda: lógica normal
            shouldIncreaseRightIK = (!isSprinting.Value && !playerProperties.is_dead.Value) && (!isProne.Value || isAiming.Value);
        }
        thirdPersonArms.UpdateRightRandRigValue(shouldIncreaseRightIK);

        // Left hand IK: ativa se não está morto E (tem leftHandTarget OU está mirando)
        bool shouldIncreaseLeftIK = !playerProperties.is_dead.Value && (hasLeftHandTarget || isAiming.Value);
        thirdPersonArms.UpdateLeftRandRigValue(shouldIncreaseLeftIK);

        thirdPersonArms.UpdateRigWeight();
        thirdPersonArms.SetLeftHandFollowerPosition();
        thirdPersonArms.SetRightHandFollowerPosition(isAiming.Value, false);
    }
    #endregion

}