using UnityEngine;

public class RollTransitionState : StateMachineBehaviour
{
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        PlayerProperties playerProperties = animator.GetComponentInParent<PlayerProperties>();
        if (!playerProperties.crouched && !playerProperties.is_proned)
        {
            PlayerController playerController = playerProperties.GetComponent<PlayerController>();

            Vector3 origin_ = playerController.playerHead.transform.position;
            float distance = 2;

            Debug.DrawLine(origin_, origin_ + Vector3.up * distance, Color.green, 2);

            if (Physics.SphereCast(origin_, playerController.stand_collider.radius, Vector3.up, out RaycastHit hit, distance, playerController.groundLayer))
            {
                playerProperties.crouched = true;
            }
        }

        animator.GetComponentInParent<PlayerProperties>().roll = false;

    }
}
