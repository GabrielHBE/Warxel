using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BombsController : MonoBehaviour
{
    [SerializeField] protected TextMeshProUGUI rockets_left_hud;
    public Sprite image_hud;
    public GameObject parent_gameobject;
    [SerializeField] protected bool can_reload_bomb;
    [SerializeField] protected GameObject bomb;
    [SerializeField] protected bool can_spawn_itens;
    [SerializeField] protected List<Transform> spawnPoints = new List<Transform>();
    [SerializeField] protected float spawnInterval = 10f;
    [SerializeField] protected float shoot_delay = 0.5f;
    [SerializeField] protected List<Bombs> bombs = new List<Bombs>();
    protected int current_bomb_index = 0;
    protected bool is_active;
    protected Bombs current_bomb;
    protected float original_shoot_delay;
    protected float original_spawn_interval;

    protected virtual void Start()
    {
        original_shoot_delay = shoot_delay;
        original_spawn_interval = spawnInterval;
        shoot_delay = 0;
    }

    protected virtual void Update()
    {
        if (is_active)
        {
            rockets_left_hud.gameObject.SetActive(true);
        }
        else
        {
            rockets_left_hud.gameObject.SetActive(false);
        }

        if (Input.GetKeyDown(KeyCode.R) && is_active)
        {
            DestroyMissiles();
        }
        shoot_delay -= Time.deltaTime;

    }

    protected void InitializeMissiles<T>() where T : Bombs
    {
        foreach (Transform spawnPoint in spawnPoints)
        {
            GameObject currentItem;
            currentItem = Instantiate(bomb, spawnPoint);
            currentItem.GetComponent<Rigidbody>().isKinematic = true;

            T rocket = currentItem.GetComponent<T>();

            if (rocket == null)
            {
                Debug.LogError($"Componente do tipo {typeof(T)} não encontrado no prefab do míssil!");
                continue;
            }

            //rocket.did_shoot = false;
            bombs.Add(rocket); // Adiciona à lista base
            current_bomb = rocket;
            current_bomb.GetComponent<Bombs>().parent_gameobject = parent_gameobject;
        }
    }

    // Método para obter mísseis de um tipo específico
    public List<T> GetMissilesOfType<T>() where T : Bombs
    {
        List<T> typedMissiles = new List<T>();

        foreach (Bombs b in bombs)
        {
            if (b is T typedMissile)
            {
                typedMissiles.Add(typedMissile);
            }
        }

        return typedMissiles;
    }

    protected void DestroyMissiles()
    {
        for (int i = 0; i < bombs.Count; i++)
        {
            Destroy(bombs[i]);
        }
        bombs.Clear();
    }


    protected void ReloadMissiles<T>() where T : Bombs
    {
        InitializeMissiles<T>();
        current_bomb_index = 0;
        current_bomb = bombs[current_bomb_index];
    }

    protected virtual void MoveToNextMissile()
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
        if (bombs.Count == 0 || current_bomb == null || shoot_delay > 0)
        {
            return false;
        }

        return true;
    }

    public virtual void Shoot(KeyCode keyCode) { }

    public void SetActive(bool active)
    {
        is_active = active;
    }

    protected virtual void UpdateRocketsHUD()
    {
        if (rockets_left_hud == null) return;

        if (bombs.Count != 0)
        {
            rockets_left_hud.text = (spawnPoints.Count - current_bomb_index).ToString("F0");
        }
        else
        {
            rockets_left_hud.text = "Reloading... " + spawnInterval.ToString("F1");
        }

    }

}
