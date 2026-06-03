using System.Collections.Generic;
using FishNet.Object;
using TMPro;
using UnityEngine;

public abstract class BombsController : NetworkBehaviour, IsVehicleCustomizationPart, IVehicleArmory
{

    public Sprite image_hud;
    public GameObject parent_gameobject;

    [Header("Config")]
    [SerializeField] protected bool can_reload_bomb;
    [SerializeField] protected bool only_show_bombs_when_shoot;
    [SerializeField] protected GameObject bombPrefab;
    [SerializeField] protected List<Transform> spawnPoints = new List<Transform>();
    [SerializeField] protected float spawnInterval = 10f;
    [SerializeField] protected float shoot_delay = 0.5f;

    protected List<Bombs> bombs = new List<Bombs>();
    protected int current_bomb_index = 0;
    protected bool isActive;
    protected Bombs current_bomb;
    protected float original_shoot_delay;
    protected float original_spawn_interval;

    public Transform shootDirection; // Adicionado para consistência

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        original_shoot_delay = shoot_delay;
        original_spawn_interval = spawnInterval;
        shoot_delay = 0;
    }

    protected virtual void Update()
    {
        if (!IsSpawned) return;

        if (InputManager.GetKeyDown(KeyCode.R) && isActive && IsOwner)
        {
            RequestDestroyBombs();
        }

        shoot_delay -= Time.deltaTime;
    }

    [Server]
    protected void InitializeBombsServer<T>() where T : Bombs
    {
        Bombs[] newlySpawned = new Bombs[spawnPoints.Count];

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            Transform spawnPoint = spawnPoints[i];
            GameObject currentItem = Instantiate(bombPrefab, spawnPoint);
            currentItem.GetComponent<Rigidbody>().isKinematic = true;

            T bombScript = currentItem.GetComponent<T>();
            bombScript.parent_gameobject = parent_gameobject;

            Spawn(currentItem, Owner);
            bombs.Add(bombScript);
            newlySpawned[i] = bombScript;
        }

        if (bombs.Count > 0) current_bomb = bombs[0];
        ObserversAddAllBombs(newlySpawned);
    }

    [ObserversRpc(BufferLast = true)]
    private void ObserversAddAllBombs(Bombs[] spawnedBombs)
    {
        if (!IsServerInitialized) bombs.Clear();

        for (int i = 0; i < spawnedBombs.Length; i++)
        {
            if (spawnedBombs[i] == null) continue;

            Bombs bomb = spawnedBombs[i];
            bomb.GetComponent<Rigidbody>().isKinematic = true;
            bomb.GetComponent<Rigidbody>().useGravity = false;

            bomb.transform.SetParent(spawnPoints[i]);
            bomb.transform.localPosition = Vector3.zero;
            bomb.transform.localRotation = Quaternion.identity;
            bomb.parent_gameobject = parent_gameobject;

            if (only_show_bombs_when_shoot && bomb.mesh != null)
                bomb.mesh.enabled = false;

            if (!IsServerInitialized) bombs.Add(bomb);
        }

        if (!IsServerInitialized && bombs.Count > 0) current_bomb = bombs[0];
    }

    [ServerRpc]
    protected void RequestDestroyBombs() => DestroyBombs();

    [Server]
    protected void DestroyBombs()
    {
        foreach (Bombs b in bombs)
        {
            if (b != null && b.IsSpawned)
                ServerManager.Despawn(b.gameObject);
        }
        bombs.Clear();
        ObserversClearBombs();
    }

    [ObserversRpc]
    private void ObserversClearBombs()
    {
        if (!IsServerInitialized) bombs.Clear();
    }

    [Server]
    protected void ReloadBombsServer<T>() where T : Bombs
    {
        bombs.Clear();
        InitializeBombsServer<T>();
        current_bomb_index = 0;
        current_bomb = bombs.Count > 0 ? bombs[current_bomb_index] : null;
        ObserversResetIndex();
    }

    [ObserversRpc]
    private void ObserversResetIndex()
    {
        if (!IsServerInitialized)
        {
            current_bomb_index = 0;
            current_bomb = bombs.Count > 0 ? bombs[0] : null;
        }
    }

    protected virtual void MoveToNextBomb()
    {
        if (current_bomb_index < bombs.Count - 1)
        {
            current_bomb_index++;
            current_bomb = bombs[current_bomb_index];
        }
        else
        {
            bombs.Clear();
            current_bomb = null;
            current_bomb_index = 0;
        }
    }

    protected virtual bool CanShoot()
    {
        if (bombs.Count == 0 || current_bomb == null || shoot_delay > 0) return false;
        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    protected void CmdEnableMesh(GameObject bombObject)
    {
        EnableMesh(bombObject);
    }

    [ObserversRpc(ExcludeOwner = true)]
    private void EnableMesh(GameObject bombObject)
    {
        if (bombObject != null)
        {
            MeshRenderer renderer = bombObject.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.enabled = true;
        }
    }

    public List<T> GetBombsOfType<T>() where T : Bombs
    {
        List<T> typedBombs = new List<T>();
        foreach (Bombs b in bombs)
        {
            if (b is T typedBomb) typedBombs.Add(typedBomb);
        }
        return typedBombs;
    }

    #region Interface Implementations
    public virtual void Shoot() { }

    public string GetCurrentAmmo()
    {
        if (bombs.Count != 0)
            return (spawnPoints.Count - current_bomb_index).ToString("F0");
        return "Reloading... " + spawnInterval.ToString("F1");
    }

    public virtual void ActivateArmory() => isActive = true;
    public virtual void DeactivateArmory() => isActive = false;
    public Sprite GetArmoryIcon() => image_hud;
    public float GetHeatingLevel() => 0;
    public float GetMaxOverheat() => 0;
    public void Activate() { }
    public void Deactivate() { }
    public VehicleCustomizableParts GetCustomizationPart() => throw new System.NotImplementedException();
    public string GetCustomizationPartName() => "Bomb Bay";
    #endregion
}