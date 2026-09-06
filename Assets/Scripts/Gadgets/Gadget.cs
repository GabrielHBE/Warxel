using UnityEngine;

public class Gadget : MonoBehaviour, UpgradeLevel
{
    [Header("Progression / Category / Settings")]
    public string gadgetName;
    public GameObject thirdPersonPrefab;
    public ClassManager.Class[] class_gadget;
    public int gadget_level;
    public float points_to_up_level;
    public float gadget_level_progression;
    public int level_to_unlock;
    public string category;
    public Sprite iconHud;

    [Header("Hands Config")]
    protected EquippableItemHandTargets equippableItemHandTargets;
    protected EquippableItemAudio equippableItemAudio;
    protected EquippableItemAnimator equippableItemAnimator;

    [Header("Handling")]
    public float drawGadgetSped;
    public float StoreGadgetSpeed;

    [Header("Sway and Bob")]
    public SwayNBobScript.SwayAndBobValues swayAndBobValues;

    protected PlayerNetworkObjectSpawner playerNetworkObjectSpawner;
    protected SoldierHudManager soldierHudManager;
    protected AdsBehaviour adsBehaviour;
    protected PlayerController playerController;
    protected CameraShake cameraShake;
    protected bool is_active;

    public virtual void Initialize()
    {
        equippableItemHandTargets = GetComponent<EquippableItemHandTargets>();
        equippableItemAnimator = GetComponent<EquippableItemAnimator>();
        equippableItemAudio = GetComponent<EquippableItemAudio>();
        adsBehaviour = GetComponentInParent<AdsBehaviour>();
        playerController = GetComponentInParent<PlayerController>();
        playerNetworkObjectSpawner = GetComponentInParent<PlayerNetworkObjectSpawner>();

        cameraShake = playerController.cameraShake;
        soldierHudManager = playerController.soldierHudManager;
    }

    public virtual void Restart() => equippableItemHandTargets.ResetHandTargets();

    public void SetActive(bool is_active) => this.is_active = is_active;

    public void AddKill()
    {
        gadget_level_progression += 1;

        if (gadget_level_progression >= points_to_up_level)
        {
            gadget_level += 1;
            gadget_level_progression = 0;
        }
    }

}
