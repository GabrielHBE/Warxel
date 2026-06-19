using FishNet.Object;
using UnityEngine;

public class NetworkPooledObject : NetworkBehaviour
{
    private Transform poolFolder;
    private bool isDestroying = false;

    private void Awake()
    {
        SetupFolder();
    }

    private void SetupFolder()
    {
        // Garante que o nosso Manager existe na cena
        if (ObjectPooling.Instance == null) return;

        string cleanName = gameObject.name.Replace("(Clone)", "").Trim();
        string folderName = $"[Pool Server] {cleanName}";

        Transform existingFolder = ObjectPooling.Instance.transform.Find(folderName);
        if (existingFolder == null)
        {
            GameObject newFolder = new GameObject(folderName);
            newFolder.transform.SetParent(ObjectPooling.Instance.transform);
            poolFolder = newFolder.transform;
        }
        else
        {
            poolFolder = existingFolder;
        }

        transform.SetParent(poolFolder);
    }

    public override void OnStopServer()
    {
        ReturnToFolder();
    }

    public override void OnStopClient()
    {
        if (!IsServerStarted) 
        {
            ReturnToFolder();
        }
    }

    private void ReturnToFolder()
    {
        // Se a Unity já avisou que vai destruir o objeto ou a pasta sumiu, aborta a operação
        if (isDestroying || poolFolder == null) return;

        // Tenta reparentar. Se a Unity bloquear (ex: troca de cena imediata), ignoramos o erro silenciosamente
        try 
        {
            transform.SetParent(poolFolder);
        }
        catch 
        {
            isDestroying = true;
        }
    }

    // Travas de segurança padrão da Unity para quando o jogo/cena estiver desligando
    private void OnApplicationQuit()
    {
        isDestroying = true;
    }

    private void OnDestroy()
    {
        isDestroying = true;
    }
}