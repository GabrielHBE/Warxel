using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.Rendering;

public class SkinApplier : NetworkBehaviour
{
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

    private readonly SyncVar<PlayerController> playerController = new SyncVar<PlayerController>();

    public void ApplySkin(PlayerController pc)
    {
        SetPlayerController(pc);
        Skin skin = SkinsManager.GetSkin(SkinSelectionManager.LoadCurrentSkinForClass(AccountManager.Instance.selected_class), AccountManager.Instance.selected_class);
        if (skin == null) return;
        SpawnSkinParts(skin, pc);
        RequestCreateSkin(GetSelectedSkinNameForClass(AccountManager.Instance.selected_class), AccountManager.Instance.selected_class, pc);
    }

    [ServerRpc]
    private void SetPlayerController(PlayerController pc) => playerController.Value = pc;

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
            ProcessInfantryDamage head = InstantiatePart(skin.head, headParent).GetComponent<ProcessInfantryDamage>();
            head.SetPlayerController(pc);
            if (IsOwner) head.GetComponentInChildren<MeshRenderer>().shadowCastingMode = ShadowCastingMode.ShadowsOnly;

            InstantiatePart(skin.torso, torsoParent).GetComponent<ProcessInfantryDamage>().SetPlayerController(pc);

            // Left Arm
            InstantiatePart(skin.leftUpperArm, leftUpperArmParent).GetComponent<ProcessInfantryDamage>().SetPlayerController(pc);
            InstantiatePart(skin.leftLowerArm, leftLowerArmParent).GetComponent<ProcessInfantryDamage>().SetPlayerController(pc);
            InstantiatePart(skin.leftHand, leftHandParent).GetComponent<ProcessInfantryDamage>().SetPlayerController(pc);

            // Right Arm
            InstantiatePart(skin.rightUpperArm, rightUpperArmParent).GetComponent<ProcessInfantryDamage>().SetPlayerController(pc);
            InstantiatePart(skin.rightLowerArm, rightLowerArmParent).GetComponent<ProcessInfantryDamage>().SetPlayerController(pc);
            InstantiatePart(skin.rightHand, rightHandParent).GetComponent<ProcessInfantryDamage>().SetPlayerController(pc);

            // Left Leg
            InstantiatePart(skin.leftUpperLeg, leftUpperLegParent).GetComponent<ProcessInfantryDamage>().SetPlayerController(pc);
            InstantiatePart(skin.leftLowerLeg, leftLowerLegParent).GetComponent<ProcessInfantryDamage>().SetPlayerController(pc);
            InstantiatePart(skin.leftFoot, leftFootParent).GetComponent<ProcessInfantryDamage>().SetPlayerController(pc);

            // Right Leg
            InstantiatePart(skin.rightUpperLeg, rightUpperLegParent).GetComponent<ProcessInfantryDamage>().SetPlayerController(pc);
            InstantiatePart(skin.rightLowerLeg, rightLowerLegParent).GetComponent<ProcessInfantryDamage>().SetPlayerController(pc);
            InstantiatePart(skin.rightFoot, rightFootParent).GetComponent<ProcessInfantryDamage>().SetPlayerController(pc);

        }catch(Exception){}
    }

    private GameObject InstantiatePart(GameObject prefab, Transform parent)
    {
        GameObject instance = Instantiate(prefab, parent, false);
        instance.SetActive(true);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localScale = Vector3.one * 1.2f;

        return instance;
    }

    private string GetSelectedSkinNameForClass(ClassManager.Class classType) => PlayerPrefs.GetString($"Skin_Selected_{classType}", "");
}