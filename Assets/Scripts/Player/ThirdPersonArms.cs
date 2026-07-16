using System.Collections;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class ThirdPersonArms : NetworkBehaviour
{
    [SerializeField] private ThirdPersonWeaponController thirdPersonWeaponController;
    [SerializeField] private Rig rightHandRig;
    [SerializeField] private Rig leftHandRig;
    [SerializeField] private Transform leftHandPos;
    [SerializeField] private Transform rightHandPos;
    [SerializeField] private Transform aimingRightHandTarget;
    [SerializeField] private Transform defaultRightHandTarget;

    

    private float currentRightHandtwoBoneIKConstraintWeight;
    private float currentLeftHandtwoBoneIKConstraintWeight;
    private const float TIME_TO_UPDATE_RIG_WEIGHT = 5f;
    private readonly SyncVar<Transform> leftHandTarget = new SyncVar<Transform>(new SyncTypeSettings(WritePermission.ClientUnsynchronized));
    private readonly SyncVar<GameObject> syncPrimary = new SyncVar<GameObject>();
    private readonly SyncVar<GameObject> syncSecondary = new SyncVar<GameObject>();
    private readonly SyncVar<GameObject> syncGadget1 = new SyncVar<GameObject>();
    private readonly SyncVar<GameObject> syncGadget2 = new SyncVar<GameObject>();
    private readonly SyncVar<Vector3> rightHandAimingOffset = new SyncVar<Vector3>();
    private readonly SyncVar<SwitchWeapon.WeaponSlot> currentWeaponSlot = new SyncVar<SwitchWeapon.WeaponSlot>();

    #region Unity Lifecycle
    void Awake()
    {

        currentRightHandtwoBoneIKConstraintWeight = 0;
        currentLeftHandtwoBoneIKConstraintWeight = 0;

        currentWeaponSlot.OnChange += OnWeaponSlotChanged;

        syncPrimary.OnChange += OnWeaponInstantiated;
        syncSecondary.OnChange += OnWeaponInstantiated;
        syncGadget1.OnChange += OnWeaponInstantiated;
        syncGadget2.OnChange += OnWeaponInstantiated;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!IsOwner)
        {
            _SwitchWeapon(currentWeaponSlot.Value);
        }

    }

    private void OnDestroy()
    {
        currentWeaponSlot.OnChange -= OnWeaponSlotChanged;
        syncPrimary.OnChange -= OnWeaponInstantiated;
        syncSecondary.OnChange -= OnWeaponInstantiated;
        syncGadget1.OnChange -= OnWeaponInstantiated;
        syncGadget2.OnChange -= OnWeaponInstantiated;
    }
    #endregion


    #region Rig Weights
    public void UpdateRightRandRigValue(bool shouldIncreaseIk)
    {
        if (shouldIncreaseIk)
        {
            if (currentRightHandtwoBoneIKConstraintWeight > 0.95)
            {
                currentRightHandtwoBoneIKConstraintWeight = 1;
                return;
            }
            currentRightHandtwoBoneIKConstraintWeight = Mathf.MoveTowards(currentRightHandtwoBoneIKConstraintWeight, 1, Time.deltaTime * TIME_TO_UPDATE_RIG_WEIGHT);
        }
        else
        {
            if (currentRightHandtwoBoneIKConstraintWeight < 0.05)
            {
                currentRightHandtwoBoneIKConstraintWeight = 0;
                return;
            }
            currentRightHandtwoBoneIKConstraintWeight = Mathf.MoveTowards(currentRightHandtwoBoneIKConstraintWeight, 0, Time.deltaTime * TIME_TO_UPDATE_RIG_WEIGHT);
        }
    }

    public void UpdateLeftRandRigValue(bool shouldIncreaseIk)
    {
        if (shouldIncreaseIk)
        {
            if (currentLeftHandtwoBoneIKConstraintWeight > 0.95)
            {
                currentLeftHandtwoBoneIKConstraintWeight = 1;
                return;
            }
            currentLeftHandtwoBoneIKConstraintWeight = Mathf.MoveTowards(currentLeftHandtwoBoneIKConstraintWeight, 1, Time.deltaTime * TIME_TO_UPDATE_RIG_WEIGHT);
        }
        else
        {
            if (currentLeftHandtwoBoneIKConstraintWeight < 0.05)
            {
                currentLeftHandtwoBoneIKConstraintWeight = 0;
                return;
            }
            currentLeftHandtwoBoneIKConstraintWeight = Mathf.MoveTowards(currentLeftHandtwoBoneIKConstraintWeight, 0, Time.deltaTime * TIME_TO_UPDATE_RIG_WEIGHT);
        }
    }

    public void UpdateRigWeight()
    {
        rightHandRig.weight = currentRightHandtwoBoneIKConstraintWeight;
        leftHandRig.weight = currentLeftHandtwoBoneIKConstraintWeight;
    }
    #endregion

    #region Positions
    public void SetLeftHandFollowerPosition()
    {
        if (leftHandTarget.Value != null)
        {
            leftHandPos.position = leftHandTarget.Value.position;
        }
        else
        {
            leftHandPos.position = aimingRightHandTarget.position;
        }
    }

    public void SetRightHandFollowerPosition(bool isAiming, bool isStoringWeapon)
    {
        if (!isAiming && !isStoringWeapon)
        {
            rightHandPos.transform.position = Vector3.Lerp(rightHandPos.transform.position, defaultRightHandTarget.position, Time.deltaTime * TIME_TO_UPDATE_RIG_WEIGHT);
            rightHandPos.transform.rotation = Quaternion.Lerp(rightHandPos.transform.rotation, defaultRightHandTarget.rotation, Time.deltaTime * TIME_TO_UPDATE_RIG_WEIGHT);
        }

        if (isAiming)
        {
            rightHandPos.transform.position = Vector3.Lerp(rightHandPos.transform.position, aimingRightHandTarget.position + rightHandAimingOffset.Value, Time.deltaTime * TIME_TO_UPDATE_RIG_WEIGHT);
            rightHandPos.transform.rotation = Quaternion.Lerp(rightHandPos.transform.rotation, aimingRightHandTarget.rotation, Time.deltaTime * TIME_TO_UPDATE_RIG_WEIGHT);
        }

    }
    #endregion

    #region Instantiate Weapons
    private void OnWeaponInstantiated(GameObject prev, GameObject next, bool asServer)
    {
        if (next != null)
        {
            Quaternion originalLocalRotation = next.transform.localRotation;
            next.transform.SetParent(thirdPersonWeaponController.transform);
            next.transform.localPosition = Vector3.zero;
            next.transform.localRotation = originalLocalRotation;
            next.transform.localScale = Vector3.one;

            _SwitchWeapon(currentWeaponSlot.Value);
        }
    }
    [ServerRpc]
    public void RequestInstantiateWeapons(GameObject prefabPrimary, GameObject prefabSecondary, GameObject prefabGadget1, GameObject prefabGadget2) => InstantiateWeapons(prefabPrimary, prefabSecondary, prefabGadget1, prefabGadget2);

    private void InstantiateWeapons(GameObject prefabPrimary, GameObject prefabSecondary, GameObject prefabGadget1, GameObject prefabGadget2)
    {
        // Alterado para usar a rotação original de cada prefab na hora de instanciar
        if (prefabPrimary != null)
        {
            GameObject instPrimary = Instantiate(prefabPrimary, thirdPersonWeaponController.transform.position, prefabPrimary.transform.rotation);
            Spawn(instPrimary, null);
            syncPrimary.Value = instPrimary;
        }
        if (prefabSecondary != null)
        {
            GameObject instSecondary = Instantiate(prefabSecondary, thirdPersonWeaponController.transform.position, prefabSecondary.transform.rotation);
            Spawn(instSecondary, null);
            syncSecondary.Value = instSecondary;
        }
        if (prefabGadget1 != null)
        {
            GameObject instGadget1 = Instantiate(prefabGadget1, thirdPersonWeaponController.transform.position, prefabGadget1.transform.rotation);
            Spawn(instGadget1, null);

            syncGadget1.Value = instGadget1;
        }
        if (prefabGadget2 != null)
        {
            GameObject instGadget2 = Instantiate(prefabGadget2, thirdPersonWeaponController.transform.position, prefabGadget2.transform.rotation);
            Spawn(instGadget2, null);
            syncGadget2.Value = instGadget2;
        }
    }
    #endregion

    #region Switch Weapon
    [ServerRpc]
    public void RequestSwitchWeapon(SwitchWeapon.WeaponSlot slot)
    {
        currentWeaponSlot.Value = slot;
    }

    private void OnWeaponSlotChanged(SwitchWeapon.WeaponSlot prev, SwitchWeapon.WeaponSlot next, bool asServer)
    {
        _SwitchWeapon(next);
    }

    private void _SwitchWeapon(SwitchWeapon.WeaponSlot slot)
    {
        switch (slot)
        {
            case SwitchWeapon.WeaponSlot.Primary:
                if (syncPrimary.Value != null)
                {
                    syncPrimary.Value.SetActive(true);
                    SetupThirdPersonWeaponProperties(syncPrimary.Value);
                }
                if (syncSecondary.Value != null) syncSecondary.Value.SetActive(false);
                if (syncGadget1.Value != null) syncGadget1.Value.SetActive(false);
                if (syncGadget2.Value != null) syncGadget2.Value.SetActive(false);
                break;

            case SwitchWeapon.WeaponSlot.Secondary:
                if (syncPrimary.Value != null) syncPrimary.Value.SetActive(false);
                if (syncSecondary.Value != null)
                {
                    syncSecondary.Value.SetActive(true);
                    SetupThirdPersonWeaponProperties(syncSecondary.Value);
                }
                if (syncGadget1.Value != null) syncGadget1.Value.SetActive(false);
                if (syncGadget2.Value != null) syncGadget2.Value.SetActive(false);
                break;

            case SwitchWeapon.WeaponSlot.Gadget1:
                if (syncPrimary.Value != null) syncPrimary.Value.SetActive(false);
                if (syncSecondary.Value != null) syncSecondary.Value.SetActive(false);
                if (syncGadget1.Value != null)
                {
                    syncGadget1.Value.SetActive(true);
                    SetupThirdPersonWeaponProperties(syncGadget1.Value);
                }
                if (syncGadget2.Value != null) syncGadget2.Value.SetActive(false);
                break;

            case SwitchWeapon.WeaponSlot.Gadget2:
                if (syncPrimary.Value != null) syncPrimary.Value.SetActive(false);
                if (syncSecondary.Value != null) syncSecondary.Value.SetActive(false);
                if (syncGadget1.Value != null) syncGadget1.Value.SetActive(false);
                if (syncGadget2.Value != null)
                {
                    syncGadget2.Value.SetActive(true);
                    SetupThirdPersonWeaponProperties(syncGadget2.Value);
                }
                break;
        }

        if (IsOwner) thirdPersonWeaponController.HideWeapon();
    }

    private void SetupThirdPersonWeaponProperties(GameObject weapon)
    {
        if (!IsOwner) return;

        ThirdPersonWeapon thirdPersonWeapon = weapon.GetComponent<ThirdPersonWeapon>();
        if (thirdPersonWeapon != null)
        {
            SetLeftHandTarget(thirdPersonWeapon.left_hand_position);
            SetRightHandAimingOffset(thirdPersonWeapon.aim_position_offset);
        }
    }
    #endregion

    #region  Helper
    public bool HasLeftHandTarget() => leftHandTarget.Value != null;

    [ServerRpc]
    private void SetLeftHandTarget(Transform target) => leftHandTarget.Value = target;

    [ServerRpc]
    private void SetRightHandAimingOffset(Vector3 offset) => rightHandAimingOffset.Value = offset;

    [ServerRpc]
    public void RequestToEnableWeapon() => CmdEnableWeapon();

    [ObserversRpc(ExcludeOwner = true)]
    private void CmdEnableWeapon() => thirdPersonWeaponController.ShowWeapon();

    [ServerRpc]
    public void RequestToDisableWeapon() => CmdDisableWeapon();

    [ObserversRpc(ExcludeOwner = true)]
    private void CmdDisableWeapon() => thirdPersonWeaponController.HideWeapon();
    #endregion
}