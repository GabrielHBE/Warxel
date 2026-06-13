using UnityEngine;
using UnityEngine.UI;

public class Gadget : MonoBehaviour, UpgradeLevel
{
    [Header("Progression / Category / Settings")]
    public GameObject third_person_prefab;
    public ClassManager.Class[] class_gadget;
    public int gadget_level;
    public float points_to_up_level;
    public float gadget_level_progression;
    public int level_to_unlock;
    public string category;
    public Sprite icon_hud;

    [Header("Hands Config")]
    [SerializeField] private Transform leftHandTarget;
    [SerializeField] private Transform rightHandTarget;

    [Header("Handling")]
    public float pick_up_gadget_speed;
    public float store_gadget_speed;

    [Header("Sway and Bob")]
    public float bob_walk_exageration;
    public float bob_sprint_exageration;
    public float bob_crouch_exageration;
    public float bob_aim_exageration;
    public Vector3 walk_multiplier;
    public Vector3 sprint_multiplier;
    public Vector3 aim_multiplier;
    public Vector3 crouch_multiplier;
    public float[] vector3Values;
    public float[] quaternionValues;

    protected FirstPersonArms firstPersonArms;
    protected PlayerNetworkObjectSpawner playerNetworkObjectSpawner;
    protected SoldierHudManager soldierHudManager;
    protected AdsBehaviour adsBehaviour;
    protected bool is_active;

    protected virtual void Awake()
    {
        adsBehaviour = GetComponentInParent<AdsBehaviour>();
        soldierHudManager = GetComponentInParent<SoldierHudManager>();
        firstPersonArms = GetComponentInParent<FirstPersonArms>();
        playerNetworkObjectSpawner = GetComponentInParent<PlayerNetworkObjectSpawner>();

    }

    public virtual void Reestart()
    {
        print(firstPersonArms);
        if (firstPersonArms != null)
        {
            if (rightHandTarget != null) firstPersonArms.MoveRightHand(rightHandTarget, 0);
            if (leftHandTarget != null) firstPersonArms.MoveLeftHand(leftHandTarget, 0);
        }
    }

    public void SetActive(bool is_active)
    {
        this.is_active = is_active;
    }
    public Transform GetTransform()
    {
        return transform;
    }

    public void UpgradeGadgetLevel(float points)
    {
        gadget_level_progression += points;

        if (gadget_level_progression >= points_to_up_level)
        {
            gadget_level += 1;
            gadget_level_progression = 0;
        }

    }
    public void AddKill()
    {

    }

}
