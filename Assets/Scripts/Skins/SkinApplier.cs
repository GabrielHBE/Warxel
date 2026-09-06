using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.Rendering;

public class SkinApplier : NetworkBehaviour
{
    [SerializeField] private FirstPersonSkinApplier firstPersonSkinApplier;

    [Header("Core")]
    public Transform headParent;
    public Transform torsoParent;

    [Header("Left Arm")]
    public Transform leftUpperArmParent;
    public Transform leftLowerArmParent;
    public Transform leftHandParent;

    [Header("Right Arm")]
    public Transform rightUpperArmParent;
    public Transform rightLowerArmParent;
    public Transform rightHandParent;

    [Header("Left Leg")]
    public Transform leftUpperLegParent;
    public Transform leftLowerLegParent;
    public Transform leftFootParent;

    [Header("Right Leg")]
    public Transform rightUpperLegParent;
    public Transform rightLowerLegParent;
    public Transform rightFootParent;

    public SkinPart instantiatedHead { get; private set; }
    public SkinPart instantiatedTorso { get; private set; }
    public SkinPart instantiatedLeftUpperArm { get; private set; }
    public SkinPart instantiatedLeftLowerArm { get; private set; }
    public SkinPart instantiatedLeftHand { get; private set; }
    public SkinPart instantiatedRightUpperArm { get; private set; }
    public SkinPart instantiatedRightLowerArm { get; private set; }
    public SkinPart instantiatedRightHand { get; private set; }
    public SkinPart instantiatedLeftUpperLeg { get; private set; }
    public SkinPart instantiatedLeftLowerLeg { get; private set; }
    public SkinPart instantiatedLeftFoot { get; private set; }
    public SkinPart instantiatedRightUpperLeg { get; private set; }
    public SkinPart instantiatedRightLowerLeg { get; private set; }
    public SkinPart instantiatedRightFoot { get; private set; }

    public void ApplySkin(PlayerController pc)
    {

        Skin skin = SkinsManager.GetSkin(SkinSelectionManager.LoadCurrentSkinForClass(AccountManager.Instance.selectedClass), AccountManager.Instance.selectedClass);
        if (skin == null) return;
        SpawnSkinParts(skin, pc);
        RequestCreateSkin(GetSelectedSkinNameForClass(AccountManager.Instance.selectedClass), AccountManager.Instance.selectedClass, pc);
    }


    [ServerRpc]
    private void RequestCreateSkin(string skinName, ClassManager.Class skinClass, PlayerController pc) => CmdCreateSkin(skinName, skinClass, pc);

    [ObserversRpc(ExcludeOwner = true, BufferLast = true)]
    private void CmdCreateSkin(string skinName, ClassManager.Class skinClass, PlayerController pc)
    {
        Skin skin = SkinsManager.GetSkin(skinName, skinClass);
        if (skin == null) return;

        SpawnSkinParts(skin, pc);
    }

    private void SpawnSkinParts(Skin skin, PlayerController pc)
    {
        try
        {
            // Core
            instantiatedHead = InstantiatePart(skin.head, headParent);
            instantiatedHead.GetComponent<ProcessInfantryDamage>().SetPlayerController(pc);
            if (IsOwner) instantiatedHead.GetComponentInChildren<MeshRenderer>().shadowCastingMode = ShadowCastingMode.ShadowsOnly;

            instantiatedTorso = InstantiatePart(skin.torso, torsoParent);
            instantiatedTorso.GetComponent<ProcessInfantryDamage>().SetPlayerController(pc);

            // Left Arm
            instantiatedLeftUpperArm = InstantiatePart(skin.leftUpperArm, leftUpperArmParent);
            if (IsOwner) firstPersonSkinApplier.InstantiateLeftUpperArm(skin.leftUpperArm);
            instantiatedLeftUpperArm.GetComponent<ProcessInfantryDamage>().SetPlayerController(pc);

            instantiatedLeftLowerArm = InstantiatePart(skin.leftLowerArm, leftLowerArmParent);
            if (IsOwner) firstPersonSkinApplier.InstantiateLeftLowerArm(skin.leftLowerArm);
            instantiatedLeftLowerArm.GetComponent<ProcessInfantryDamage>().SetPlayerController(pc);

            instantiatedLeftHand = InstantiatePart(skin.leftHand, leftHandParent);
            if (IsOwner) firstPersonSkinApplier.InstantiateLeftHand(skin.leftHand);
            instantiatedLeftHand.GetComponent<ProcessInfantryDamage>().SetPlayerController(pc);

            // Right Arm
            instantiatedRightUpperArm = InstantiatePart(skin.rightUpperArm, rightUpperArmParent);
            if (IsOwner) firstPersonSkinApplier.InstantiateRightUpperArm(skin.rightUpperArm);
            instantiatedRightUpperArm.GetComponent<ProcessInfantryDamage>().SetPlayerController(pc);

            instantiatedRightLowerArm = InstantiatePart(skin.rightLowerArm, rightLowerArmParent);
            if (IsOwner) firstPersonSkinApplier.InstantiateRightLowerArm(skin.rightLowerArm);
            instantiatedRightLowerArm.GetComponent<ProcessInfantryDamage>().SetPlayerController(pc);

            instantiatedRightHand = InstantiatePart(skin.rightHand, rightHandParent);
            if (IsOwner) firstPersonSkinApplier.InstantiateRightHand(skin.rightHand);
            instantiatedRightHand.GetComponent<ProcessInfantryDamage>().SetPlayerController(pc);

            // Left Leg
            instantiatedLeftUpperLeg = InstantiatePart(skin.leftUpperLeg, leftUpperLegParent);
            instantiatedLeftUpperLeg.GetComponent<ProcessInfantryDamage>().SetPlayerController(pc);

            instantiatedLeftLowerLeg = InstantiatePart(skin.leftLowerLeg, leftLowerLegParent);
            instantiatedLeftLowerLeg.GetComponent<ProcessInfantryDamage>().SetPlayerController(pc);

            instantiatedLeftFoot = InstantiatePart(skin.leftFoot, leftFootParent);
            instantiatedLeftFoot.GetComponent<ProcessInfantryDamage>().SetPlayerController(pc);

            // Right Leg
            instantiatedRightUpperLeg = InstantiatePart(skin.rightUpperLeg, rightUpperLegParent);
            instantiatedRightUpperLeg.GetComponent<ProcessInfantryDamage>().SetPlayerController(pc);

            instantiatedRightLowerLeg = InstantiatePart(skin.rightLowerLeg, rightLowerLegParent);
            instantiatedRightLowerLeg.GetComponent<ProcessInfantryDamage>().SetPlayerController(pc);

            instantiatedRightFoot = InstantiatePart(skin.rightFoot, rightFootParent);
            instantiatedRightFoot.GetComponent<ProcessInfantryDamage>().SetPlayerController(pc);

        }
        catch (Exception) { }
    }

    private SkinPart InstantiatePart(GameObject prefab, Transform parent)
    {
        GameObject instance = Instantiate(prefab, parent, false);
        if (!instance.activeSelf) instance.SetActive(true);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localScale = Vector3.one * 1.2f;

        return instance.GetComponent<SkinPart>();
    }

    private string GetSelectedSkinNameForClass(ClassManager.Class classType) => PlayerPrefs.GetString($"Skin_Selected_{classType}", "");
}