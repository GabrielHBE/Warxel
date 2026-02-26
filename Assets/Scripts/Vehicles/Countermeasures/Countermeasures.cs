
using UnityEngine;
using UnityEngine.UI;

public class Countermeasures : MonoBehaviour
{
    [HideInInspector] public bool is_active;
    [HideInInspector] public float reload_countermeasures_original_duration;
    [HideInInspector] public bool is_reloading;
    [SerializeField] private Image hud_space;
    [SerializeField] public Sprite image_icon_hud;
    [SerializeField] protected AudioSource sound;
    public float countermeasures_duration = 10;
    public float reload_countermeasures_duration = 10;
    protected Vehicle vehicle;
    protected KeyCode use_countermeasure_key;
    protected float countermeasures_original_duration;
    

    void Start()
    {
        reload_countermeasures_original_duration = reload_countermeasures_duration;
        countermeasures_original_duration = countermeasures_duration;

        reload_countermeasures_duration = 0;
    }

    protected virtual void Update()
    {
        if (Input.GetKeyDown(use_countermeasure_key) && vehicle.is_in_vehicle && reload_countermeasures_duration <= 0) UseCountermeasure();
    }

    public virtual void SetUseCountermeasureKey(KeyCode use_countermeasure_key)
    {
        this.use_countermeasure_key = use_countermeasure_key;
    }

    public virtual void SetVehicle(Vehicle vehicle)
    {
        this.vehicle = vehicle;
    }
    public virtual void UseCountermeasure() { }
    protected virtual void StopCountermeasure() { }
}
