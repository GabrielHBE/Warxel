using UnityEngine;

public class LocalPooledObject : MonoBehaviour
{
    private Transform poolParentFolder;

    // Método para o ObjectPooling avisar qual é a pasta deste item
    public void SetupPoolParent(Transform folder)
    {
        poolParentFolder = folder;
    }

    public virtual void Deactivate()
    {
        if(ObjectPooling.Instance == null) return;

        gameObject.SetActive(false);
        
        // Retorna para a "pasta" organizada dele, em vez da raiz do ObjectPooling
        transform.SetParent(poolParentFolder != null ? poolParentFolder : ObjectPooling.Instance.transform);
    }

    public virtual void Activate()
    {
        gameObject.SetActive(true);
        transform.SetParent(null);
    }
}