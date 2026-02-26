using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MissileController : MonoBehaviour
{
    [SerializeField] protected TextMeshProUGUI rockets_left_hud;
    public Sprite image_hud;
    public GameObject parent_gameobject;
    [SerializeField] protected bool can_reload_missiles;
    [SerializeField] protected bool only_show_missiles_when_shoot;
    [SerializeField] private GameObject missile;
    [SerializeField] protected List<Transform> spawnPoints = new List<Transform>();
    [SerializeField] protected float spawnInterval = 10f;
    [SerializeField] protected float shoot_delay = 0.5f;
    [SerializeField] protected List<Missiles> missiles = new List<Missiles>(); // Lista base
    protected int current_missile_index = 0;
    protected bool is_active;
    protected Missiles current_missile;
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

        if (Input.GetKeyDown(KeyCode.R) && is_active)
        {
            DestroyMissiles();
        }
        shoot_delay -= Time.deltaTime;
    }

    protected void InitializeMissiles<T>() where T : Missiles
    {
        foreach (Transform spawnPoint in spawnPoints)
        {
            GameObject currentItem;
            currentItem = Instantiate(missile, spawnPoint);
            currentItem.GetComponent<Rigidbody>().isKinematic = true;

            T rocket = currentItem.GetComponent<T>();

            if (rocket == null)
            {
                Debug.LogError($"Componente do tipo {typeof(T)} não encontrado no prefab do míssil!");
                continue;
            }

            //rocket.did_shoot = false;
            missiles.Add(rocket); // Adiciona à lista base
            current_missile = rocket;
            current_missile.GetComponent<Missiles>().parent_gameobject = parent_gameobject;

            if (only_show_missiles_when_shoot) current_missile.GetComponent<MeshRenderer>().enabled = false;

        }
    }

    // Método para obter mísseis de um tipo específico
    public List<T> GetMissilesOfType<T>() where T : Missiles
    {
        List<T> typedMissiles = new List<T>();

        foreach (Missiles missile in missiles)
        {
            if (missile is T typedMissile)
            {
                typedMissiles.Add(typedMissile);
            }
        }

        return typedMissiles;
    }

    protected void DestroyMissiles()
    {
        for (int i = 0; i < missiles.Count; i++)
        {
            Destroy(missiles[i]);
        }
        missiles.Clear();
    }


    protected void ReloadMissiles<T>() where T : Missiles
    {
        InitializeMissiles<T>();
        current_missile_index = 0;
        current_missile = missiles[current_missile_index];
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
        if (missiles.Count == 0 || current_missile == null || shoot_delay > 0)
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
        if (missiles.Count != 0)
        {
            rockets_left_hud.text = (spawnPoints.Count - current_missile_index).ToString("F0");
        }
        else
        {
            rockets_left_hud.text = "Reloading... " + spawnInterval.ToString("F1");
        }

    }

}
