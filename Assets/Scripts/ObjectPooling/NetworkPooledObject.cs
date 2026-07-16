using System.Collections;
using FishNet.Object;
using UnityEngine;

public abstract class NetworkPooledObject : NetworkBehaviour
{
    private Transform poolFolder;
    private bool isRegistered = false;

    protected virtual void Awake()
    {
        StartCoroutine(WaitForServerObjectPooling());
        if (IsServerInitialized) SetDespawnType(DespawnType.Pool);
    }

    protected void SetDespawnType(DespawnType despawnType)
    {
        ServerManager.Despawn(NetworkObject, despawnType);
    }

    private IEnumerator WaitForServerObjectPooling()
    {
        float timer = 0f;

        while (ServerObjectPooling.Instance == null && timer < 5f)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (ServerObjectPooling.Instance != null)
        {
            SetupFolder();
        }
        else
        {
            Debug.LogWarning("Timeout: O ServerObjectPooling não foi carregado após 5 segundos.");
        }
    }

    private void SetupFolder()
    {
        string cleanName = gameObject.name.Replace("(Clone)", "").Trim();
        string folderName = $"[Pool Server] {cleanName}";

        Transform existingFolder = ServerObjectPooling.Instance.transform.Find(folderName);
        if (existingFolder == null)
        {
            GameObject newFolder = new GameObject(folderName);
            newFolder.transform.SetParent(ServerObjectPooling.Instance.transform);
            poolFolder = newFolder.transform;
        }
        else
        {
            poolFolder = existingFolder;
        }

        transform.SetParent(poolFolder);
    }

    // Método chamado quando o objeto é ativado do pool
    protected virtual void OnEnable()
    {
        if (ServerObjectPooling.Instance != null && !isRegistered)
        {
            ServerObjectPooling.Instance.RegisterPooledObject(this);
            isRegistered = true;
        }
    }

    // Método chamado quando o objeto é desativado/retornado ao pool
    protected virtual void OnDisable()
    {
        if (ServerObjectPooling.Instance != null && isRegistered)
        {
            ServerObjectPooling.Instance.UnregisterPooledObject(this);
            isRegistered = false;
        }
    }

    // Método chamado quando o objeto é destruído
    protected virtual void OnDestroy()
    {
        if (ServerObjectPooling.Instance != null && isRegistered)
        {
            ServerObjectPooling.Instance.UnregisterPooledObject(this);
            isRegistered = false;
        }
    }

    protected abstract void Enable();
    protected abstract void Disable();
    public abstract void LocalUpdate();
    public abstract void LocalFixedUpdate();

    // Método para verificar se o objeto está registrado
    public bool IsRegistered()
    {
        return isRegistered;
    }
}