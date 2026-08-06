using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(BoxCollider))]
public abstract class PostFX : MonoBehaviour
{
    [SerializeField] protected Volume volume;

    // Controle da animação
    protected Coroutine transitionCoroutine;
    protected float currentMultiplier = 0f;
    
    protected virtual void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
        InitializeVolume();
    }

    protected abstract void InitializeVolume();
    public abstract void SetActive(bool active);

}
