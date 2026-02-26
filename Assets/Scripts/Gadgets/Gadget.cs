using UnityEngine;
using UnityEngine.UI;

public class Gadget : MonoBehaviour
{   
    [Header("Progression / Category")]
    public ClassManager.Class[] class_gadget;
    public int gadget_level;
    public float points_to_up_level;
    public float gadget_level_progression;
    public int level_to_unlock;
    public string category;

    protected bool is_active;
    public Sprite icon_hud;
    protected GadgetComponents gadgetComponents;

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

    void Start()
    {
        gadgetComponents = GetComponentInParent<GadgetComponents>();
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

        if(gadget_level_progression >= points_to_up_level)
        {
            gadget_level +=1;
            gadget_level_progression = 0;
        }

    }

}
