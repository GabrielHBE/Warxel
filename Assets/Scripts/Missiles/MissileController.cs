using System.Collections.Generic;
using FishNet;
using FishNet.Object;
using TMPro;
using UnityEngine;

public abstract class MissileController : NetworkBehaviour, IsVehicleCustomizationPart, IVehicleArmory
{    public Sprite image_hud;
    public GameObject parent_gameobject;
    [SerializeField] protected bool can_reload_missiles;
    [SerializeField] protected bool only_show_missiles_when_shoot;
    [SerializeField] private GameObject missile;
    [SerializeField] protected List<Transform> spawnPoints = new List<Transform>();
    [SerializeField] protected float spawnInterval = 10f;
    [SerializeField] protected float shoot_delay = 0.5f;

    protected List<Missiles> missiles = new List<Missiles>();
    protected int current_missile_index = 0;
    protected bool is_active;
    protected Missiles current_missile;
    protected float original_shoot_delay;
    protected float original_spawn_interval;

    public Transform shootDirection;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        original_shoot_delay = shoot_delay;
        original_spawn_interval = spawnInterval;
        shoot_delay = 0;
    }

    protected virtual void Update()
    {
        if (InputManager.GetKeyDown(KeyCode.R) && is_active)
        {
            RequestDestroyMissiles();
        }
        shoot_delay -= Time.deltaTime;
    }

    // Apenas o servidor pode inicializar os mísseis na rede
    [Server]
    protected void InitializeMissilesServer<T>() where T : Missiles
    {
        // Cria uma array temporária para guardar todos os mísseis gerados de uma vez
        Missiles[] newlySpawned = new Missiles[spawnPoints.Count];

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            Transform spawnPoint = spawnPoints[i];

            // Instancia sem setar o parent ainda
            GameObject currentItem = Instantiate(missile, spawnPoint);
            currentItem.GetComponent<Rigidbody>().isKinematic = true;

            T rocket = currentItem.GetComponent<T>();
            rocket.parent_gameobject = parent_gameobject;

            // Spawna na rede
            Spawn(currentItem, Owner);

            // Adiciona na lista do servidor e na array temporária
            missiles.Add(rocket);
            newlySpawned[i] = rocket;
        }

        if (missiles.Count > 0) current_missile = missiles[0];

        // Manda TODOS os mísseis de uma vez em uma única chamada!
        ObserversAddAllMissiles(newlySpawned);
    }
    // Todos os clientes recebem essa chamada para sincronizar a parte visual e listas locais
    [ObserversRpc(BufferLast = true)]
    private void ObserversAddAllMissiles(Missiles[] spawnedMissiles)
    {
        // Se for apenas cliente, limpa a lista por segurança antes de repovoar
        if (!IsServerInitialized)
        {
            missiles.Clear();
        }

        for (int i = 0; i < spawnedMissiles.Length; i++)
        {
            if (spawnedMissiles[i] == null) continue;

            Missiles rocket = spawnedMissiles[i];

            // 1. TRAVA A FÍSICA PARA OS CLIENTES
            rocket.GetComponent<Rigidbody>().isKinematic = true;
            rocket.GetComponent<Rigidbody>().useGravity = false;

            // 2. Parenteia e reseta posições
            rocket.transform.SetParent(spawnPoints[i]);
            rocket.transform.localPosition = Vector3.zero;
            rocket.transform.localRotation = Quaternion.identity;
            rocket.parent_gameobject = parent_gameobject;

            if (only_show_missiles_when_shoot) rocket.mesh.enabled = false;

            // 3. Adiciona na lista local para que o cliente saiba que tem munição
            if (!IsServerInitialized)
            {
                missiles.Add(rocket);
            }
        }

        // Atualiza a mira/seleção atual do cliente
        if (!IsServerInitialized && missiles.Count > 0)
        {
            current_missile = missiles[0];
        }
    }

    public List<T> GetMissilesOfType<T>() where T : Missiles
    {
        List<T> typedMissiles = new List<T>();
        foreach (Missiles m in missiles)
        {
            if (m is T typedMissile) typedMissiles.Add(typedMissile);
        }
        return typedMissiles;
    }

    [ServerRpc]
    protected void RequestDestroyMissiles()
    {
        DestroyMissiles();
    }

    [Server]
    protected void DestroyMissiles()
    {
        for (int i = 0; i < missiles.Count; i++)
        {
            if (missiles[i] != null && missiles[i].IsSpawned)
                ServerManager.Despawn(missiles[i].gameObject);
        }
        missiles.Clear();
        ObserversClearMissiles();
    }

    [ObserversRpc]
    private void ObserversClearMissiles()
    {
        if (!IsServerInitialized) missiles.Clear();
    }

    [Server]
    protected void ReloadMissilesServer<T>() where T : Missiles
    {
        missiles.Clear();
        InitializeMissilesServer<T>();
        current_missile_index = 0;
        current_missile = missiles.Count > 0 ? missiles[current_missile_index] : null;
        ObserversResetIndex();
    }

    [ObserversRpc]
    private void ObserversResetIndex()
    {
        if (!IsServerInitialized)
        {
            current_missile_index = 0;
            current_missile = missiles.Count > 0 ? missiles[0] : null;
        }
    }

    protected void MoveToNextMissile()
    {
        if (current_missile_index < missiles.Count - 1)
        {
            current_missile_index++;
            current_missile = missiles[current_missile_index];
        }
        else
        {
            missiles.Clear();
            current_missile = null;
            current_missile_index = 0;
        }
    }

    protected virtual bool CanShoot()
    {
        if (missiles.Count == 0 || current_missile == null || shoot_delay > 0) return false;
        return true;
    }

    public virtual void ShootMissile() { }

    // Update ServerRpc to receive the GameObject
    [ServerRpc(RequireOwnership = false)]
    protected void CmdEnableMesh(GameObject missileObject)
    {
        EnableMesh(missileObject);
    }

    // Update ObserversRpc to receive the GameObject and apply the change
    [ObserversRpc(ExcludeOwner = true)]
    private void EnableMesh(GameObject missileObject)
    {
        if (missileObject != null)
        {
            MeshRenderer renderer = missileObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.enabled = true;
            }
        }
    }

    #region Interface implementations
    //IsVehicleCustomizationPart
    public void Activate()
    {
        throw new System.NotImplementedException();
    }

    public void Deactivate()
    {
        throw new System.NotImplementedException();
    }

    public VehicleCustomizableParts GetCustomizationPart()
    {
        throw new System.NotImplementedException();
    }

    public string GetCustomizationPartName()
    {
        throw new System.NotImplementedException();
    }

    //IVehicleArmory
    public void Shoot()
    {
        if (InputManager.GetKeyDown(Settings.Instance._keybinds.JET_shootVehicleKey))
        {
            ShootMissile();
        }
    }

    public Sprite GetArmoryIcon()
    {
        return image_hud;
    }

    public void ActivateArmory() => is_active = true;

    public void DeactivateArmory() => is_active = false;

    public string GetCurrentAmmo()
    {

        string text = "";

        if (missiles.Count != 0)
        {
            text = (spawnPoints.Count - current_missile_index).ToString("F0");
        }
        else
        {
            text = "Reloading... " + spawnInterval.ToString("F1");
        }

        return text;
    }

    public float GetHeatingLevel() => 0;
    public float GetMaxOverheat() => 0;
    #endregion
}