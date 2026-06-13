using UnityEngine;

public class LocalPooledObject : MonoBehaviour
{
    protected virtual void Deactivate()
    {
        if(ObjectPooling.Instance == null) return;

        gameObject.SetActive(false);
        transform.SetParent(ObjectPooling.Instance.transform);
    }

    protected virtual void Activate()
    {
        gameObject.SetActive(true);
        transform.SetParent(null);
    }

    
}
