using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AAMissilleController : MonoBehaviour, JetUpgradeController
{
    [SerializeField] private GameObject item;
    [SerializeField] private float spawnInterval = 10f;
    [SerializeField] private float shoot_delay = 1f;
    [SerializeField] private float lock_on_timer = 2;
    [SerializeField] private float lock_on_range = 200;
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    [SerializeField] private Camera local_camera;
    [SerializeField] private AudioListener camera_audio_listenner;
    [SerializeField] private Vehicle parent_vehicle;
    [SerializeField] private AudioSource locking_missile_sound;
    [SerializeField] private AudioSource locked_missile_sound;

    private List<AAMissile> AA_missile = new List<AAMissile>();
    private int current_rocket_index = 0;

    private AAMissile current_agm;
    private bool is_active;
    private bool is_locking;
    private bool is_locked;
    private bool can_fire;
    private bool is_shooting = false;

    private float locking_timer = 0;

    private Vehicle vehicle;


    float raycast_radius = 1f;

    void Start()
    {
        parent_vehicle = GetComponentInParent<Vehicle>();
        InitializeMissiles();
    }

    void Update()
    {

        if (AA_missile.Count < spawnPoints.Count)
        {
            spawnInterval -= Time.deltaTime;

            if (spawnInterval <= 0)
            {
                ReloadMissile();
                spawnInterval = 10f;
            }
        }

        if (!is_active) return;

        RaycastHit hit;

        if (Physics.SphereCast(transform.position, raycast_radius, transform.forward, out hit, lock_on_range, LayerMask.GetMask("Vehicle")))
        {
            if (hit.collider.gameObject.tag == "AirVehicle")
            {
                locking_timer += Time.deltaTime;

                is_locking = true;

                if (locking_timer <= lock_on_timer)
                {
                    vehicle = hit.collider.GetComponent<Vehicle>();
                    is_locked = true;
                    is_locking = false;
                }
                else
                {
                    vehicle = null;
                    is_locked = false;
                    can_fire = false;
                    is_locking = false;

                }

            }
            else
            {
                vehicle = null;
                is_locked = false;
                is_locking = false;
                can_fire = false;
                locking_timer = 0;
            }


        }
        else
        {
            is_locked = false;
            is_locking = false;
            vehicle = null;
            can_fire = false;
            locking_timer = 0;
        }


    }



    void InitializeMissiles()
    {
        foreach (Transform spawnPoint in spawnPoints)
        {
            GameObject currentItem = Instantiate(item, spawnPoint.position, spawnPoint.rotation);
            currentItem.GetComponent<Rigidbody>().isKinematic = true;
            currentItem.transform.SetParent(transform);
            currentItem.transform.position = spawnPoint.position;
            currentItem.transform.rotation = spawnPoint.rotation;


            AAMissile rocket = currentItem.GetComponent<AAMissile>();
            rocket.parent_vehicle = parent_vehicle;
            AA_missile.Add(rocket);
        }
    }

    void MoveToNextMissile()
    {
        if (current_rocket_index < AA_missile.Count - 1)
        {
            current_rocket_index++;
            current_agm = AA_missile[current_rocket_index];
        }
        else
        {
            AA_missile.Clear();
            current_agm = null;
            current_rocket_index = 0;
        }
    }

    void ReloadMissile()
    {
        Debug.Log("recarregado");
        InitializeMissiles();
        current_rocket_index = 0;
        current_agm = AA_missile[current_rocket_index];
    }

    public bool CanShoot()
    {
        if (vehicle != null && current_agm != null && is_locked)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void Shoot()
    {
        if (CanShoot() && !is_shooting)
        {
            current_agm.SetVehicle(vehicle);
            current_agm.Shoot();
            MoveToNextMissile();

            StartCoroutine(ShootDelay());
        }
    }


    private IEnumerator ShootDelay()
    {
        is_shooting = true;

        yield return new WaitForSeconds(shoot_delay);

        is_shooting = false;
    }


    public void UseCamera(bool active)
    {

    }

    public void SetActive(bool active)
    {
        is_active = active;
    }
}
