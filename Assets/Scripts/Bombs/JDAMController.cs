using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

public class JDAMController : BombsController
{
    [SerializeField] private Camera jdam_camera;
    [SerializeField] private Canvas jdamCameraHud;
    [SerializeField] private Vehicle vehicle;
    [SerializeField] private LayerMask groundLayerMask; // Configure no Inspector com o layer do chão

    private Jdam currentJdam;
    private Vector3 predictedImpactPoint;

    public override void OnOwnershipClient(NetworkConnection prevOwner)
    {
        base.OnOwnershipClient(prevOwner);
        
        if (jdamCameraHud.gameObject.activeSelf) jdamCameraHud.gameObject.SetActive(false);

        if (IsOwner) CmdRequestInitializeBombs();
    }

    protected override void Update()
    {
        if (!IsSpawned) return;
        if (!IsOwner) return;

        base.Update();

        if (bombs.Count == 0 && can_reload_bomb)
        {
            spawnInterval -= Time.deltaTime;
            if (spawnInterval <= 0)
            {
                CmdRequestReloadBombs();
                spawnInterval = original_spawn_interval;
            }
        }

        // Atualiza referência do JDAM atual
        if (bombs.Count > 0 && current_bomb_index < bombs.Count)
        {
            currentJdam = bombs[current_bomb_index] as Jdam;
        }
        else
        {
            currentJdam = null;
        }

        if (isActive)
        {
            if (Input.GetKey(KeyCode.Mouse1))
            {
                if (vehicle.currentSeat.seatHUD.activeSelf) vehicle.currentSeat.seatHUD.SetActive(false);
                if (!jdamCameraHud.gameObject.activeSelf) jdamCameraHud.gameObject.SetActive(true);
                UpdateJDAMCamera();
                jdam_camera.enabled = true;
                if (vehicle.currentSeat.activeCamera != null)
                    vehicle.currentSeat.activeCamera.enabled = false;
            }
            else
            {
                if (!vehicle.currentSeat.seatHUD.activeSelf) vehicle.currentSeat.seatHUD.SetActive(true);
                if (jdamCameraHud.gameObject.activeSelf) jdamCameraHud.gameObject.SetActive(false);
                jdam_camera.enabled = false;
                if (vehicle.currentSeat.activeCamera != null)
                    vehicle.currentSeat.activeCamera.enabled = true;
            }
        }
    }

    private void UpdateJDAMCamera()
    {
        if (currentJdam != null && currentJdam.GetDidShoot())
        {
            // Calcula o ponto de impacto previsto
            predictedImpactPoint = CalculateImpactPoint(currentJdam);

            // Faz a câmera olhar para o ponto de impacto
            jdam_camera.transform.LookAt(predictedImpactPoint);

            // Opcional: desenha um gizmo para debug
            Debug.DrawLine(currentJdam.transform.position, predictedImpactPoint, Color.red);
        }
        else
        {
            // Fallback: olha para frente do veículo
            jdam_camera.transform.LookAt(transform.position + transform.forward * 500f);
        }
    }

    private Vector3 CalculateImpactPoint(Jdam bomb)
    {
        if (bomb.GetRigidbody() == null) return bomb.transform.position + Vector3.down * 100f;

        // Parâmetros físicos da bomba
        Vector3 position = bomb.transform.position;
        Vector3 velocity = bomb.GetRigidbody().linearVelocity;
        float gravityMagnitude = Physics.gravity.magnitude;

        // Como a bomba usa AddForce(Vector3.down * travel_speed * mass, ForceMode.Acceleration)
        // a aceleração efetiva é travel_speed para baixo
        float effectiveGravity = bomb.GetTravelSpeed(); // travel_speed é usado como aceleração

        // Método 1: Para terreno plano (mais rápido)
        // Equação: y = y0 + vy*t + 0.5 * a * t²
        // Resolvendo para y = groundLevel quando a = -effectiveGravity

        float groundLevel = GetGroundLevel(position);
        float deltaY = position.y - groundLevel;
        float vy = velocity.y;

        // Resolve a equação quadrática: 0.5 * a * t² + vy * t - deltaY = 0
        // Onde a = -effectiveGravity (negativo porque puxa para baixo)
        float a = -0.5f * effectiveGravity;
        float b = vy;
        float c = -deltaY;

        float discriminant = b * b - 4 * a * c;

        if (discriminant >= 0)
        {
            float t1 = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
            float t2 = (-b - Mathf.Sqrt(discriminant)) / (2 * a);

            // Pega o menor tempo positivo
            float timeToImpact = (t1 > 0 && t2 > 0) ? Mathf.Min(t1, t2) : Mathf.Max(t1, t2);

            if (timeToImpact > 0 && timeToImpact < 30f) // Limite de 30 segundos
            {
                // Calcula a posição futura
                Vector3 impactPoint = position + velocity * timeToImpact;
                impactPoint.y = groundLevel; // Ajusta para o nível do chão

                return impactPoint;
            }
        }

        // Fallback: método iterativo (mais preciso, especialmente em terrenos irregulares)
        return CalculateImpactPointIterative(bomb, position, velocity, effectiveGravity);
    }

    private Vector3 CalculateImpactPointIterative(Jdam bomb, Vector3 startPos, Vector3 startVel, float gravity)
    {
        Vector3 pos = startPos;
        Vector3 vel = startVel;

        // Simula a trajetória em passos pequenos
        for (float t = 0; t < 10f; t += 0.05f) // Máximo 10 segundos de simulação
        {
            // Atualiza velocidade (aceleração constante para baixo)
            vel.y -= gravity * 0.05f;

            // Atualiza posição
            pos += vel * 0.05f;

            // Verifica se atingiu o chão
            float groundLevel = GetGroundLevel(pos);
            if (pos.y <= groundLevel)
            {
                pos.y = groundLevel;
                return pos;
            }
        }

        return pos;
    }

    private float GetGroundLevel(Vector3 position)
    {
        RaycastHit hit;
        if (Physics.Raycast(position + Vector3.up * 100f, Vector3.down, out hit, 500f, groundLayerMask))
        {
            return hit.point.y;
        }

        // Se não encontrar chão, assume y = 0
        return 0f;
    }

    public override void Shoot()
    {
        if (Input.GetKeyDown(Settings.Instance._keybinds.JET_shootVehicleKey))
        {
            if (CanShoot())
            {
                if (only_show_bombs_when_shoot && current_bomb.mesh != null)
                {
                    CmdEnableMesh(current_bomb.gameObject);
                    current_bomb.mesh.enabled = true;
                }

                shoot_delay = original_shoot_delay;

                current_bomb.ShootBomb();
                MoveToNextBomb();
            }
        }
    }

    [ServerRpc(RequireOwnership = true)]
    private void CmdRequestReloadBombs()
    {
        ReloadBombsServer<Jdam>();
    }

    [ServerRpc(RequireOwnership = true)]
    private void CmdRequestInitializeBombs()
    {
        InitializeBombsServer<Jdam>();
    }

    public override void DeactivateArmory()
    {
        base.DeactivateArmory();
        if (!vehicle.currentSeat.seatHUD.activeSelf) vehicle.currentSeat.seatHUD.SetActive(true);
        if (jdamCameraHud.gameObject.activeSelf) jdamCameraHud.gameObject.SetActive(false);
        jdam_camera.enabled = false;
        vehicle.currentSeat.activeCamera.enabled = true;
    }
}