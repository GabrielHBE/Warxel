using UnityEngine;

public class JDAMController : BombsController
{
    [SerializeField] private Camera jdam_camera;
    [SerializeField] private Vehicle vehicle;
    
    private Jdam currentJdam;
    private Vector3 lastPredictedImpact;

    protected override void Start()
    {
        base.Start();
        vehicle = GetComponentInParent<Vehicle>();
        InitializeMissiles<Jdam>();
        current_bomb = bombs[current_bomb_index];
        currentJdam = current_bomb as Jdam;
    }

    protected override void Update()
    {
        base.Update();

        if (bombs.Count == 0 && can_reload_bomb)
        {
            spawnInterval -= Time.deltaTime;

            if (spawnInterval <= 0)
            {
                ReloadMissiles<Jdam>();
                spawnInterval = original_spawn_interval;
                if (bombs.Count > 0)
                {
                    currentJdam = current_bomb as Jdam;
                }
            }
        }

        if (is_active)
        {
            if (Input.GetKey(KeyCode.Mouse1))
            {
                UpdateJDAMCamera();
                jdam_camera.enabled = true;
                vehicle.vehicle_camera.enabled = false;
            }
            else
            {
                jdam_camera.enabled = false;
                vehicle.vehicle_camera.enabled = true;
            }

            UpdateRocketsHUD();
        }
    }

    private void UpdateJDAMCamera()
    {
        if (currentJdam != null)
        {
            Vector3 targetLookAt;
            
            if (currentJdam.HasPredictedImpact())
            {
                // Usar ponto de impacto previsto
                targetLookAt = currentJdam.GetPredictedImpactPoint();
                lastPredictedImpact = targetLookAt;
            }
            else if (lastPredictedImpact != Vector3.zero)
            {
                // Usar último ponto previsto conhecido
                targetLookAt = lastPredictedImpact;
            }
            else
            {
                // Fallback: olhar para a bomba ou para frente
                targetLookAt = currentJdam.transform.position + currentJdam.transform.forward * 100f;
            }
            
            //jdam_camera.transform.LookAt(targetLookAt);
            jdam_camera.transform.LookAt(currentJdam.transform);
        }
        else
        {
            // Se não houver bomba, olhar para frente do veículo
            jdam_camera.transform.LookAt(transform.forward);
        }
    }

    public override void Shoot(KeyCode keyCode)
    {
        if (CanShoot() && Input.GetKeyDown(keyCode))
        {
            shoot_delay = original_shoot_delay;
            currentJdam = current_bomb as Jdam;
            currentJdam.Shoot();
            MoveToNextMissile();
            
            // Atualizar referência para a próxima bomba
            if (bombs.Count > current_bomb_index && current_bomb_index >= 0)
            {
                currentJdam = current_bomb as Jdam;
            }

        }
    }
    
    protected override void MoveToNextMissile()
    {
        base.MoveToNextMissile();
        
        // Atualizar referência do JDAM atual
        if (bombs.Count > 0 && current_bomb_index < bombs.Count)
        {
            currentJdam = bombs[current_bomb_index] as Jdam;
        }

    }
}