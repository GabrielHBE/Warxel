
using FishNet.Object;
using UnityEngine;
using UnityEngine.UI;

public class Countermeasures : NetworkBehaviour, IsVehicleCustomizationPart
{
    [HideInInspector] public bool is_active;
    [HideInInspector] public float reload_countermeasures_original_duration;
    [HideInInspector] public bool reloading;
    [SerializeField] private Image hud_space;
    [SerializeField] public Sprite image_icon_hud;
    [SerializeField] protected AudioSource sound;
    public float countermeasures_duration = 10;
    public float reload_countermeasures_duration = 10;
    protected Vehicle vehicle;
    protected float countermeasures_original_duration;


    void Awake()
    {
        reload_countermeasures_original_duration = reload_countermeasures_duration;
        countermeasures_original_duration = countermeasures_duration;
        reload_countermeasures_duration = 0;
    }

    public virtual void LocalUpdate(){ }
    public virtual void SetVehicle(Vehicle vehicle) => this.vehicle = vehicle;
    public virtual void UseCountermeasure() { }
    protected virtual void StopCountermeasure() { }
    public void Activate() => GetComponentInParent<Vehicle>().countermeasures = this;
    public void Deactivate() => Destroy(gameObject);
    public VehicleCustomizableParts GetCustomizationPart() => VehicleCustomizableParts.Countermeasure;
    public string GetCustomizationPartName() => gameObject.name;
    public bool IsCooldownFinished() => reload_countermeasures_duration <= 0;
}