using System.Collections;
using UnityEngine;

public class AirStrikeCall : Gadget
{
    [SerializeField] private GameObject airStrikeMissilePrefab;
    [SerializeField] private LineRenderer line;
    [SerializeField] private Color laserColor;
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private Transform laserPos;
    [SerializeField] private Transform leftHandPos;

    private float distance = 300;
    private float callInDelay = 5;
    private float originalCallInDelay;
    private float nextAvailableTime = 0; // Tempo em que o gadget estará disponível novamente

    void Start()
    {
        originalCallInDelay = callInDelay;
        nextAvailableTime = 0; // Começa disponível
        line = GetComponent<LineRenderer>();
        if (line == null)
        {
            line = gameObject.AddComponent<LineRenderer>();
        }

        line.positionCount = 2;
        line.startWidth = 0.01f;
        line.endWidth = 0.01f;
        line.material = new Material(Shader.Find("Unlit/Color"));
        line.material.color = laserColor;
    }

    void Update()
    {
        if (!is_active) return;

        // Calcula o tempo restante de cooldown

        
        float remainingCooldown = Mathf.Max(0, nextAvailableTime - Time.time);
        UpdateAmmoHUD(remainingCooldown);

        UpdateLaser();

        if (InputManager.GetKeyDown(Settings.Instance._keybinds.WEAPON_shootKey) && remainingCooldown <= 0)
        {
            if (Physics.Raycast(new Ray(laserPos.position, laserPos.forward), out RaycastHit hit, distance))
            {
                // Define o próximo tempo disponível
                nextAvailableTime = Time.time + originalCallInDelay;

                print(hit.transform.gameObject.name);
                playerNetworkObjectSpawner.ServerSpawnAirStrike(airStrikeMissilePrefab, hit.point);
            }
        }
    }

    void UpdateLaser()
    {
        Ray ray = new Ray(laserPos.position, laserPos.forward);
        RaycastHit hit;
        Vector3 raihitpos;
        if (Physics.Raycast(ray, out hit, distance, layerMask))
        {
            raihitpos = hit.point;
        }
        else
        {
            raihitpos = ray.origin + ray.direction * distance;
        }

        line.SetPosition(0, transform.position);
        line.SetPosition(1, raihitpos);
    }


    private void UpdateAmmoHUD(float remainingCooldown)
    {

        if (soldierHudManager != null)
        {
            if (remainingCooldown > 0)
            {
                soldierHudManager.SetCurrentAmmo("Wait: " + remainingCooldown.ToString("F1"));
            }
            else
            {
                soldierHudManager.SetCurrentAmmo("Ready!");
            }
        }
    }

}