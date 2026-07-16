using UnityEngine;

public abstract class LocalPooledObject : MonoBehaviour
{
    public virtual void Deactivate()
    {
        gameObject.SetActive(false);
    }

    public virtual void Activate()
    {
        gameObject.SetActive(true);
    }

    public abstract void LocalUpdate();
    
    public abstract void LocalFixedUpdate();

}