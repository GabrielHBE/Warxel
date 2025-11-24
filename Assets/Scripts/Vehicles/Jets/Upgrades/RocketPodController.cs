using System.Collections.Generic;
using UnityEngine;

public class RocketPodController : MonoBehaviour, JetUpgradeController
{
    [SerializeField] private GameObject item;
    [SerializeField] private float spawnInterval = 10f;
    [SerializeField] private float shoot_delay = 0.5f;
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    [SerializeField] private Jet jet;
    private List<RocketPods> rocketPods = new List<RocketPods>();

    private RocketPods current_rocket;
    private int current_rocket_index = 0; // Mudando para índice baseado na lista
    private float original_shoot_delay;

    public bool CanShoot()
    {
        if (rocketPods.Count == 0 || current_rocket == null)
        {
            return false;
        }
        return true;
    }

    public void Shoot()
    {
        if (CanShoot())
        {
            current_rocket.Shoot();
        }
    }

    public void UseCamera(bool active)
    {
        
    }

    void Start()
    {
        original_shoot_delay = shoot_delay;
        jet = GetComponentInParent<Jet>();
        jet.upgrade = transform.GetComponent<JetUpgradeController>();

        InitializeRocketPods();
        
        current_rocket_index = 0;
        current_rocket = rocketPods[current_rocket_index];
    }

    void InitializeRocketPods()
    {
        foreach (Transform spawnPoint in spawnPoints)
        {
            GameObject currentItem = Instantiate(item, spawnPoint.position, spawnPoint.rotation);
            currentItem.GetComponent<Rigidbody>().isKinematic = true;
            currentItem.transform.SetParent(transform);
            currentItem.transform.position = spawnPoint.position;
            currentItem.transform.rotation = spawnPoint.rotation;
            
            RocketPods rocket = currentItem.GetComponent<RocketPods>();
            rocket.vehicle = jet;
            rocket.did_shoot = false;
            rocketPods.Add(rocket);
        }
    }

    void Update()
    {
        if (rocketPods.Count == 0)
        {
            spawnInterval -= Time.deltaTime;
            
            if (spawnInterval <= 0)
            {
                ReloadRocketPods();
                spawnInterval = 10f;
            }
        }

        if (current_rocket != null && current_rocket.did_shoot)
        {
            MoveToNextRocketPod();
        }
    }

    void ReloadRocketPods()
    {
        Debug.Log("recarregado");
        InitializeRocketPods();
        current_rocket_index = 0;
        current_rocket = rocketPods[current_rocket_index];
    }

    void MoveToNextRocketPod()
    {
        if (current_rocket_index < rocketPods.Count - 1)
        {
            current_rocket_index++;
            current_rocket = rocketPods[current_rocket_index];
            shoot_delay = original_shoot_delay;
        }
        else
        {
            rocketPods.Clear();
            current_rocket = null;
            current_rocket_index = 0;
        }
    }

    public void SetActive(bool active)
    {
        throw new System.NotImplementedException();
    }
}