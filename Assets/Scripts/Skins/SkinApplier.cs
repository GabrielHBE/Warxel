using FishNet.Object;
using UnityEngine;

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

    public void ApplySkin()
    {
        Skin skin = SkinsManager.GetSkin(SkinSelectionManager.LoadCurrentSkinForClass(AccountManager.Instance.selected_class), AccountManager.Instance.selected_class);
        if (skin == null) return;
        CreateSkin(skin);

        RequestCreateSkin(GetSelectedSkinNameForClass(AccountManager.Instance.selected_class), AccountManager.Instance.selected_class);
    }

    [ServerRpc]
    private void RequestCreateSkin(string skinName, ClassManager.Class skinClass) => CmdCreateSkin(skinName, skinClass);

    [ObserversRpc(ExcludeOwner = true, BufferLast = true)]
    private void CmdCreateSkin(string skinName, ClassManager.Class skinClass)
    {
        Skin skin = SkinsManager.GetSkin(skinName, skinClass);
        if (skin == null) return;
        CreateSkin(skin);
    }

    private void CreateSkin(Skin skin)
    {
        // Core
        InstantiatePart(skin.head, headParent);
        InstantiatePart(skin.torso, torsoParent);

        // Left Arm
        InstantiatePart(skin.leftUpperArm, leftUpperArmParent);
        InstantiatePart(skin.leftLowerArm, leftLowerArmParent);
        InstantiatePart(skin.leftHand, leftHandParent);

        // Right Arm
        InstantiatePart(skin.rightUpperArm, rightUpperArmParent);
        InstantiatePart(skin.rightLowerArm, rightLowerArmParent);
        InstantiatePart(skin.rightHand, rightHandParent);

        // Left Leg
        InstantiatePart(skin.leftUpperLeg, leftUpperLegParent);
        InstantiatePart(skin.leftLowerLeg, leftLowerLegParent);
        InstantiatePart(skin.leftFoot, leftFootParent);

        // Right Leg
        InstantiatePart(skin.rightUpperLeg, rightUpperLegParent);
        InstantiatePart(skin.rightLowerLeg, rightLowerLegParent);
        InstantiatePart(skin.rightFoot, rightFootParent);
    }

    private void InstantiatePart(GameObject prefab, Transform parent)
    {
        if (prefab == null || parent == null) return;

        GameObject instance = Instantiate(prefab, parent, false);
        instance.SetActive(true);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localScale = Vector3.one * 1.2f;
    }

    private string GetSelectedSkinNameForClass(ClassManager.Class classType) => PlayerPrefs.GetString($"Skin_Selected_{classType}", "");
    
}