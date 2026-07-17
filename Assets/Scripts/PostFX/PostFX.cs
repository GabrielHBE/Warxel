using UnityEngine;
using UnityEngine.Rendering;

public abstract class PostFX : MonoBehaviour
{
    [SerializeField] protected Volume volume;

    // Controle da animação
    protected Coroutine transitionCoroutine;
    protected float currentMultiplier = 0f;
    
    protected virtual void Awake()
    {
        InitializeVolume();
    }

    protected abstract void InitializeVolume();
    public abstract void SetActive(bool active);

}
