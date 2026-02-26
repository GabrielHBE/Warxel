using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] private ThirdPersonWeapon thirdPersonWeapon;
    [SerializeField] private Animator anim;
    [SerializeField] private PlayerController movement;
    [SerializeField] private PlayerProperties playerProperties;
    [SerializeField] private GameObject weapon_hand_holder;
    private Quaternion weapon_hand_holder_original_rotation;
    private Quaternion weapon_hand_holder_aim_rotation;

    [Header("Switch Weapon")]
    [SerializeField] private SwitchWeapon switchWeapon;
    [SerializeField] private Transform save_weapon_pos;

    private GameObject current_weapon;

    [Header("Itens")]
    [SerializeField] private Transform itens_parent;
    [SerializeField] private GameObject primary;
    [SerializeField] private GameObject secondary;
    [SerializeField] private GameObject gadget1;
    [SerializeField] private GameObject gadget2;

    [Header("Right Hand Transition")]
    [SerializeField] private float rightHandSwitchSpeed = 4f;
    [SerializeField] private float rightHandReturnSpeed = 8f;

    [Header("Aiming Settings")]
    [SerializeField] private TwoBoneIKConstraint rightHand_twoBoneIKConstraint;
    [SerializeField] private Transform rightHand_target;
    [SerializeField] private Transform rightHand;

    [SerializeField] private Transform leftHand;
    [SerializeField] private TwoBoneIKConstraint leftHand_twoBoneIKConstraint;
    [SerializeField] private Transform leftHand_target;

    private float left_twoBoneIKConstraint_weight;
    private float right_twoBoneIKConstraint_weight;

    // Death animation
    private int randomDeadAnimationValue = 1;
    private bool hasGeneratedDeadAnimation = false;
    private bool wasDead = false;

    private float right_hand_transition_speed;

    private enum WeaponSlot
    {
        Primary = 1,
        Secondary = 2,
        Gadget1 = 3,
        Gadget2 = 4
    }

    void Start()
    {
        weapon_hand_holder_original_rotation = weapon_hand_holder.transform.localRotation * Quaternion.Euler(0, 0f, 30f);
        weapon_hand_holder_aim_rotation = weapon_hand_holder.transform.localRotation;
        SwitchWeapon(switchWeapon.currentWeapon);

        randomDeadAnimationValue = UnityEngine.Random.Range(1, 8);

        left_twoBoneIKConstraint_weight = 1;
        right_twoBoneIKConstraint_weight = 0;
    }

    void Update()
    {
        UpdateAnimatorParameters();
        UpdateDeathAnimation();
        UpdateRightHandTransform();
        UpdateLeftHandIK();
        UpdateRightHandIK();
        if (playerProperties.is_in_vehicle)
        {
            if (current_weapon != null) current_weapon.SetActive(false);
        }
        else
        {
            if (current_weapon != null) current_weapon.SetActive(true);
        }
    }

    #region Animator

    private void UpdateAnimatorParameters()
    {
        anim.SetFloat("Horizontal", movement.moveHorizontal, 0.1f, Time.deltaTime);
        anim.SetFloat("Vertical", movement.moveForward, 0.1f, Time.deltaTime);

        anim.SetBool("Crouched", playerProperties.crouched);
        anim.SetBool("Proned", playerProperties.is_proned);
        anim.SetBool("Walking",
            !playerProperties.sprinting &&
            !playerProperties.is_proned &&
            (movement.moveHorizontal != 0 || movement.moveForward != 0));

        anim.SetBool("Sprinting",
            !playerProperties.crouched &&
            playerProperties.sprinting &&
            !playerProperties.is_proned &&
            (movement.moveHorizontal != 0 || movement.moveForward != 0));

        anim.SetBool("Aiming", playerProperties.is_aiming);
        anim.SetBool("Reloading", playerProperties.is_reloading);
        anim.SetBool("ProneTransition", playerProperties.isProneTransition);
        anim.SetBool("Roll", playerProperties.roll);
        anim.SetBool("Dead", playerProperties.is_dead);
        anim.SetFloat("Rotation", Input.GetAxis("Mouse X"));
        anim.SetBool("HasLeftHandHolder", leftHand_target == null ? false : true);
        anim.SetBool("IsGrounded", playerProperties.isGrounded);
        anim.SetBool("InVehicle", playerProperties.is_in_vehicle);

    }

    #endregion

    #region Death Animation

    private void UpdateDeathAnimation()
    {
        if (playerProperties.is_dead && !wasDead)
        {
            if (!hasGeneratedDeadAnimation)
            {
                randomDeadAnimationValue = UnityEngine.Random.Range(1, 8);
                hasGeneratedDeadAnimation = true;
            }
        }
        else if (!playerProperties.is_dead && wasDead)
        {
            hasGeneratedDeadAnimation = false;
        }

        wasDead = playerProperties.is_dead;
        anim.SetInteger("RandomDeadAnimation", randomDeadAnimationValue);
    }

    public void ResetDeathAnimation()
    {
        hasGeneratedDeadAnimation = false;
        wasDead = false;
        randomDeadAnimationValue = UnityEngine.Random.Range(1, 8);
    }

    #endregion

    #region Right Hand Transform (SMOOTH TRANSITION)


    private void UpdateRightHandTransform()
    {

        if (playerProperties.is_aiming || playerProperties.is_firing || leftHand_target == null)
        {
            weapon_hand_holder.transform.localRotation = Quaternion.Slerp(
                weapon_hand_holder.transform.localRotation,
                weapon_hand_holder_aim_rotation,
                Time.deltaTime * 6
            );
        }
        else
        {
            weapon_hand_holder.transform.localRotation = Quaternion.Slerp(
                weapon_hand_holder.transform.localRotation,
                weapon_hand_holder_original_rotation,
                Time.deltaTime * 6
            );
        }
        Transform target = switchWeapon._switch ? save_weapon_pos : rightHand_target;
        float speed = switchWeapon._switch ? rightHandSwitchSpeed : rightHandReturnSpeed;

        // Posição base do target
        Vector3 targetPosition = target.position;
        Quaternion targetRotation = target.rotation;

        // Ajustar posição e rotação se não estiver no chão
        if (!playerProperties.isGrounded)
        {
            // Movimentar um pouco para baixo (ajuste o valor conforme necessário)
            targetPosition += Vector3.down * 0.6f;

            // Rotacionar no eixo X (ajuste o ângulo conforme necessário)
            targetRotation *= Quaternion.Euler(0, 0f, -20f);
        }

        rightHand.position = Vector3.Lerp(
            rightHand.position,
            targetPosition,
            Time.deltaTime * speed
        );

        rightHand.rotation = Quaternion.Slerp(
            rightHand.rotation,
            targetRotation,
            Time.deltaTime * speed
        );
    }

    #endregion

    #region Left Hand IK

    private void UpdateLeftHandIK()
    {
        if (leftHand_target == null)
        {
            left_twoBoneIKConstraint_weight = 0;
            leftHand_twoBoneIKConstraint.weight = 0;
            return;
        }

        leftHand.position = leftHand_target.position;
        leftHand.rotation = leftHand_target.rotation;

        if (!playerProperties.isProneTransition && !playerProperties.is_dead && !playerProperties.is_in_vehicle)
        {
            left_twoBoneIKConstraint_weight += Time.deltaTime * 7f;
        }
        else
        {
            left_twoBoneIKConstraint_weight -= Time.deltaTime * 7f;
        }

        left_twoBoneIKConstraint_weight = Mathf.Clamp01(left_twoBoneIKConstraint_weight);
        leftHand_twoBoneIKConstraint.weight = left_twoBoneIKConstraint_weight;
    }

    #endregion

    #region Right Hand IK

    private void UpdateRightHandIK()
    {

        if (switchWeapon._switch)
        {

            SwitchWeapon(switchWeapon.currentWeapon);

            right_hand_transition_speed = 4f;
        }
        else
        {
            right_hand_transition_speed = 7f;
        }

        if ((playerProperties.is_aiming || playerProperties.is_firing || switchWeapon._switch) && leftHand_target != null && !playerProperties.is_in_vehicle)
        {
            right_twoBoneIKConstraint_weight +=
                Time.deltaTime * right_hand_transition_speed;
        }
        else
        {
            right_twoBoneIKConstraint_weight -=
                Time.deltaTime * right_hand_transition_speed / 2f;
        }

        right_twoBoneIKConstraint_weight =
            Mathf.Clamp01(right_twoBoneIKConstraint_weight);

        rightHand_twoBoneIKConstraint.weight =
            right_twoBoneIKConstraint_weight;
    }

    #endregion

    #region Switch_weapon

    private void SwitchWeapon(int index)
    {
        if (current_weapon != null) Destroy(current_weapon);

        switch (index)
        {
            case 1:
                if (primary != null) current_weapon = Instantiate(primary, itens_parent);
                break;

            case 2:
                if (secondary != null) current_weapon = Instantiate(secondary, itens_parent);
                break;

            case 3:
                if (gadget1 != null) current_weapon = Instantiate(gadget1, itens_parent);
                break;

            case 4:
                if (gadget2 != null) current_weapon = Instantiate(gadget2, itens_parent);
                break;

            default:
                break;

        }

        if (current_weapon == null) return;
        leftHand_target = null;
        foreach (Transform t in current_weapon.GetComponentsInChildren<Transform>(true))
        {
            if (t.CompareTag("LeftHandPosition"))
            {
                leftHand_target = t;
                break;
            }
        }

        if (thirdPersonWeapon != null) thirdPersonWeapon.Reestart();
    }

    #endregion
}
