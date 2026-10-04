using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(BoxCollider))]
public abstract class CameraPostFX : CameraModifiers
{
    [SerializeField] protected Volume volume;

    protected Coroutine transitionCoroutine;
    protected float currentMultiplier = 0f;
    
    protected virtual void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
        InitializeVolume();
    }

    protected abstract void InitializeVolume();

}
