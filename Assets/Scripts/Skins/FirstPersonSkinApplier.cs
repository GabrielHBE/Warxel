using UnityEngine;

public class FirstPersonSkinApplier : MonoBehaviour
{
    [SerializeField] private LayerMask weaponLayer;

    [Header("Left Arm")]
    public Transform leftUpperArmParent;
    public Transform leftLowerArmParent;
    public Transform leftHandParent;

    [Header("Right Arm")]
    public Transform rightUpperArmParent;
    public Transform rightLowerArmParent;
    public Transform rightHandParent;

    #region Left Arm Instantiation
    public void InstantiateLeftUpperArm(GameObject skin) => InstantiateSkinOnParent(leftUpperArmParent, skin);
    public void InstantiateLeftLowerArm(GameObject skin) => InstantiateSkinOnParent(leftLowerArmParent, skin);
    public void InstantiateLeftHand(GameObject skin) => InstantiateSkinOnParent(leftHandParent, skin);
    #endregion

    #region Right Arm Instantiation
    public void InstantiateRightUpperArm(GameObject skin) => InstantiateSkinOnParent(rightUpperArmParent, skin);
    public void InstantiateRightLowerArm(GameObject skin) => InstantiateSkinOnParent(rightLowerArmParent, skin);
    public void InstantiateRightHand(GameObject skin) => InstantiateSkinOnParent(rightHandParent, skin);
    #endregion

    #region Helper Methods
    private void InstantiateSkinOnParent(Transform parent, GameObject skin)
    {
        if (parent == null || skin == null) return;

        foreach (Transform child in parent)
        {
            Destroy(child.gameObject);
        }

        GameObject instance = Instantiate(skin, parent);
        if (!instance.activeSelf) instance.SetActive(true);

        // Converte a LayerMask para o índice do Layer (0 a 31)
        int layerIndex = GetLayerFromMask(weaponLayer);

        // Aplica o layer na instância e em todos os seus objetos filhos
        SetLayerRecursively(instance, layerIndex);

        instance.transform.localPosition = Vector3.zero;
        instance.transform.localScale = Vector3.one;
        
        instance.GetComponent<SkinPart>().meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Destroy(instance.GetComponent<SkinPart>());
        Destroy(instance.GetComponent<ProcessInfantryDamage>());
        Destroy(instance.GetComponent<Collider>());
        Destroy(instance.GetComponent<Rigidbody>());
    }

    private int GetLayerFromMask(LayerMask mask)
    {
        int bitmask = mask.value;
        if (bitmask == 0) return 0;

        int layer = 0;
        while ((bitmask & 1) == 0)
        {
            bitmask >>= 1;
            layer++;
        }
        return layer;
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
    #endregion

}