using System.Collections;
using FishNet.Object;
using UnityEngine;

public class Ragdoll : NetworkBehaviour
{
    [SerializeField] private GameObject thirdPersonPlayer;
    [SerializeField] private SkinApplier skinApplier;
    [SerializeField] private PlayerProperties playerProperties;
    [SerializeField] private PlayerController playerController;

    private Coroutine resetRagdollItensTransforms;
    private const float RESET_RAGDOLL_ITENS_TRANSFORM_TIMER = 1f;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (IsOwner) playerProperties.isDead.OnChange += OnIsDeadChange;
    }

    private void OnDisable()
    {
        if (IsOwner) playerProperties.isDead.OnChange -= OnIsDeadChange;
    }

    private void OnIsDeadChange(bool prev, bool next, bool asServer)
    {
        if (prev == next) return;

        if (next)
        {
            Vector3 velocity = playerController.GetVelocoty();

            if (velocity.magnitude < 0.1f) velocity = playerController.transform.forward * 0.5f;

            ServerStartRagdoll(velocity);
        }
        else ServerStopRagdoll();

    }

    #region Network Synchronization (RPCs)
    [ServerRpc]
    private void ServerStartRagdoll(Vector3 velocity) => ObserversStartRagdoll(velocity);

    [ObserversRpc(BufferLast = true)]
    private void ObserversStartRagdoll(Vector3 velocity) => StartRagdollLocal(velocity);

    [ServerRpc]
    private void ServerStopRagdoll() => ObserversStopRagdoll();

    [ObserversRpc(BufferLast = true)]
    private void ObserversStopRagdoll() => StopRagdollLocal();
    #endregion

    #region Ragdoll Logic
    private void StartRagdollLocal(Vector3 velocity)
    {
        thirdPersonPlayer.SetActive(false);

        if (resetRagdollItensTransforms != null) StopCoroutine(resetRagdollItensTransforms);

        SetupRigidBody(false);
        SetupCollider(false);
        RemoveParent();

        ApplyForceToAllParts(velocity);
    }

    private void StopRagdollLocal()
    {
        thirdPersonPlayer.SetActive(true);

        SetupRigidBody(true);
        SetupCollider(true);
        ResetParent();

        resetRagdollItensTransforms = StartCoroutine(ResetRagdollItensCoroutine());
    }

    private void ApplyForceToAllParts(Vector3 velocity)
    {
        float forceMultiplier = 0.5f;
        Vector3 force = velocity * forceMultiplier;

        ApplyForceToPart(skinApplier.instantiatedHead, force);
        ApplyForceToPart(skinApplier.instantiatedTorso, force);
        ApplyForceToPart(skinApplier.instantiatedLeftUpperArm, force);
        ApplyForceToPart(skinApplier.instantiatedLeftLowerArm, force);
        ApplyForceToPart(skinApplier.instantiatedLeftHand, force);
        ApplyForceToPart(skinApplier.instantiatedRightUpperArm, force);
        ApplyForceToPart(skinApplier.instantiatedRightLowerArm, force);
        ApplyForceToPart(skinApplier.instantiatedRightHand, force);
        ApplyForceToPart(skinApplier.instantiatedLeftUpperLeg, force);
        ApplyForceToPart(skinApplier.instantiatedLeftLowerLeg, force);
        ApplyForceToPart(skinApplier.instantiatedLeftFoot, force);
        ApplyForceToPart(skinApplier.instantiatedRightUpperLeg, force);
        ApplyForceToPart(skinApplier.instantiatedRightLowerLeg, force);
        ApplyForceToPart(skinApplier.instantiatedRightFoot, force);
    }

    private void ApplyForceToPart(SkinPart part, Vector3 force)
    {
        if (part == null || part.rb == null) return;

        // Acorda o Rigidbody para garantir que a força surta efeito imediato
        part.rb.isKinematic = false;
        part.rb.WakeUp();

        part.rb.AddForce(force * part.rb.mass, ForceMode.Impulse);

        Vector3 randomTorque = new Vector3(
            Random.Range(-1, 1),
            Random.Range(-1, 1),
            Random.Range(-1, 1)
        );
        part.rb.AddTorque(randomTorque * part.rb.mass, ForceMode.Impulse);
    }

    private IEnumerator ResetRagdollItensCoroutine()
    {
        float timer = 0;
        while (timer <= RESET_RAGDOLL_ITENS_TRANSFORM_TIMER)
        {
            float progress = timer / RESET_RAGDOLL_ITENS_TRANSFORM_TIMER;

            ResetPartTransform(skinApplier.instantiatedHead, progress);
            ResetPartTransform(skinApplier.instantiatedTorso, progress);
            ResetPartTransform(skinApplier.instantiatedLeftUpperArm, progress);
            ResetPartTransform(skinApplier.instantiatedLeftLowerArm, progress);
            ResetPartTransform(skinApplier.instantiatedLeftHand, progress);
            ResetPartTransform(skinApplier.instantiatedRightUpperArm, progress);
            ResetPartTransform(skinApplier.instantiatedRightLowerArm, progress);
            ResetPartTransform(skinApplier.instantiatedRightHand, progress);
            ResetPartTransform(skinApplier.instantiatedLeftUpperLeg, progress);
            ResetPartTransform(skinApplier.instantiatedLeftLowerLeg, progress);
            ResetPartTransform(skinApplier.instantiatedLeftFoot, progress);
            ResetPartTransform(skinApplier.instantiatedRightUpperLeg, progress);
            ResetPartTransform(skinApplier.instantiatedRightLowerLeg, progress);
            ResetPartTransform(skinApplier.instantiatedRightFoot, progress);

            timer += Time.deltaTime;
            yield return null;
        }
    }

    private void ResetPartTransform(SkinPart part, float progress)
    {
        if (part == null) return;
        part.transform.localPosition = Vector3.Lerp(part.transform.localPosition, Vector3.zero, progress);
        part.transform.localRotation = Quaternion.Lerp(part.transform.localRotation, Quaternion.identity, progress);
    }

    private void SetupRigidBody(bool state)
    {
        SetPartKinematic(skinApplier.instantiatedHead, state);
        SetPartKinematic(skinApplier.instantiatedTorso, state);
        SetPartKinematic(skinApplier.instantiatedLeftUpperArm, state);
        SetPartKinematic(skinApplier.instantiatedLeftLowerArm, state);
        SetPartKinematic(skinApplier.instantiatedLeftHand, state);
        SetPartKinematic(skinApplier.instantiatedRightUpperArm, state);
        SetPartKinematic(skinApplier.instantiatedRightLowerArm, state);
        SetPartKinematic(skinApplier.instantiatedRightHand, state);
        SetPartKinematic(skinApplier.instantiatedLeftUpperLeg, state);
        SetPartKinematic(skinApplier.instantiatedLeftLowerLeg, state);
        SetPartKinematic(skinApplier.instantiatedLeftFoot, state);
        SetPartKinematic(skinApplier.instantiatedRightUpperLeg, state);
        SetPartKinematic(skinApplier.instantiatedRightLowerLeg, state);
        SetPartKinematic(skinApplier.instantiatedRightFoot, state);
    }

    private void SetPartKinematic(SkinPart part, bool state)
    {
        if (part != null && part.rb != null) part.rb.isKinematic = state;
    }

    private void SetupCollider(bool state)
    {
        SetPartTrigger(skinApplier.instantiatedHead, state);
        SetPartTrigger(skinApplier.instantiatedTorso, state);
        SetPartTrigger(skinApplier.instantiatedLeftUpperArm, state);
        SetPartTrigger(skinApplier.instantiatedLeftLowerArm, state);
        SetPartTrigger(skinApplier.instantiatedLeftHand, state);
        SetPartTrigger(skinApplier.instantiatedRightUpperArm, state);
        SetPartTrigger(skinApplier.instantiatedRightLowerArm, state);
        SetPartTrigger(skinApplier.instantiatedRightHand, state);
        SetPartTrigger(skinApplier.instantiatedLeftUpperLeg, state);
        SetPartTrigger(skinApplier.instantiatedLeftLowerLeg, state);
        SetPartTrigger(skinApplier.instantiatedLeftFoot, state);
        SetPartTrigger(skinApplier.instantiatedRightUpperLeg, state);
        SetPartTrigger(skinApplier.instantiatedRightLowerLeg, state);
        SetPartTrigger(skinApplier.instantiatedRightFoot, state);
    }

    private void SetPartTrigger(SkinPart part, bool state)
    {
        if (part != null && part.col != null) part.col.isTrigger = state;
    }

    private void RemoveParent()
    {
        SetPartParent(skinApplier.instantiatedHead, null);
        SetPartParent(skinApplier.instantiatedTorso, null);
        SetPartParent(skinApplier.instantiatedLeftUpperArm, null);
        SetPartParent(skinApplier.instantiatedLeftLowerArm, null);
        SetPartParent(skinApplier.instantiatedLeftHand, null);
        SetPartParent(skinApplier.instantiatedRightUpperArm, null);
        SetPartParent(skinApplier.instantiatedRightLowerArm, null);
        SetPartParent(skinApplier.instantiatedRightHand, null);
        SetPartParent(skinApplier.instantiatedLeftUpperLeg, null);
        SetPartParent(skinApplier.instantiatedLeftLowerLeg, null);
        SetPartParent(skinApplier.instantiatedLeftFoot, null);
        SetPartParent(skinApplier.instantiatedRightUpperLeg, null);
        SetPartParent(skinApplier.instantiatedRightLowerLeg, null);
        SetPartParent(skinApplier.instantiatedRightFoot, null);
    }

    private void ResetParent()
    {
        SetPartParent(skinApplier.instantiatedHead, skinApplier.headParent);
        SetPartParent(skinApplier.instantiatedTorso, skinApplier.torsoParent);
        SetPartParent(skinApplier.instantiatedLeftUpperArm, skinApplier.leftUpperArmParent);
        SetPartParent(skinApplier.instantiatedLeftLowerArm, skinApplier.leftLowerArmParent);
        SetPartParent(skinApplier.instantiatedLeftHand, skinApplier.leftHandParent);
        SetPartParent(skinApplier.instantiatedRightUpperArm, skinApplier.rightUpperArmParent);
        SetPartParent(skinApplier.instantiatedRightLowerArm, skinApplier.rightLowerArmParent);
        SetPartParent(skinApplier.instantiatedRightHand, skinApplier.rightHandParent);
        SetPartParent(skinApplier.instantiatedLeftUpperLeg, skinApplier.leftUpperLegParent);
        SetPartParent(skinApplier.instantiatedLeftLowerLeg, skinApplier.leftLowerLegParent);
        SetPartParent(skinApplier.instantiatedLeftFoot, skinApplier.leftFootParent);
        SetPartParent(skinApplier.instantiatedRightUpperLeg, skinApplier.rightUpperLegParent);
        SetPartParent(skinApplier.instantiatedRightLowerLeg, skinApplier.rightLowerLegParent);
        SetPartParent(skinApplier.instantiatedRightFoot, skinApplier.rightFootParent);
    }

    private void SetPartParent(SkinPart part, Transform parent)
    {
        if (part != null) part.transform.SetParent(parent);
    }
    #endregion
}