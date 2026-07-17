using FishNet.Object;
using UnityEngine;

public class LockInMissileController : MissileController
{
    [Header("Lock-On Settings")]
    [SerializeField] private Transform lockInFowardReference;
    [SerializeField] private SoundManager.SoundComponents lockingInSound;
    [SerializeField] private Vehicle.VehicleType lockInVehicleType;
    [SerializeField] private float lockOnTimeRequired = 2f;
    [SerializeField] private float maxLockDistance = 1000f;

    [Header("Raycast Settings")]
    private int lockRadius = 30;

    // CORREÇÃO: Usar LayerMask.NameToLayer para obter o layer index e criar a máscara
    private int vehicleLayerMask => 1 << LayerMask.NameToLayer("Vehicle");

    private Vehicle targetVehicle;
    private Transform currentTarget;
    private float currentLockTimer = 0f;
    private bool canShoot = false;
    private float lockingInSoundDelay = 0;

    protected override void Update()
    {
        base.Update();

        if (!IsOwner || !isActive) return;

        print("Passou da trava de rede! Executando ProcessLockOn...");

        ProcessLockOn();
        HandleLockingInSound();
    }
    RaycastHit hit;
    private RaycastHit[] hitResults = new RaycastHit[10];
    private void ProcessLockOn()
    {
        Vector3 origin = lockInFowardReference.position;
        Vector3 direction = lockInFowardReference.forward;

        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            lockRadius,
            direction,
            hitResults,
            maxLockDistance,
            vehicleLayerMask,
            QueryTriggerInteraction.Ignore
        );

        if (hitCount > 0)
        {
            Transform closestValidTarget = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit currentHit = hitResults[i];

                if (currentHit.transform.root == transform.root)
                    continue;

                Vehicle vehicle = currentHit.transform.GetComponent<Vehicle>();
                if (vehicle == null)
                    vehicle = currentHit.transform.GetComponentInParent<Vehicle>();

                if (vehicle == null || vehicle.vehicleType != lockInVehicleType)
                    continue;

                if (currentHit.distance < closestDistance)
                {
                    closestDistance = currentHit.distance;
                    closestValidTarget = vehicle.transform;
                    hit = currentHit;
                    targetVehicle = vehicle;
                }
            }

            if (closestValidTarget != null)
            {

                if (closestValidTarget == currentTarget)
                {
                    currentLockTimer += Time.deltaTime;

                    if (currentLockTimer >= lockOnTimeRequired)
                    {
                        lockingInSoundDelay = 0.1f;
                        canShoot = true;
                    }
                    else
                    {
                        lockingInSoundDelay = 0.5f;
                        canShoot = false;
                    }
                }
                else
                {
                    lockingInSoundDelay = 0f;
                    canShoot = false;
                    currentTarget = closestValidTarget;
                    currentLockTimer = 0f;
                }

                return;
            }
        }

        if (targetVehicle != null) targetVehicle = null;
        canShoot = false;
        currentTarget = null;
        currentLockTimer = 0f;
        lockingInSoundDelay = 0f;
    }

    float soundTimer = 0;
    private void HandleLockingInSound()
    {
        if (lockingInSoundDelay == 0) return;

        soundTimer += Time.deltaTime;

        if (soundTimer >= lockingInSoundDelay)
        {
            SoundManager.Play2dSoundLocal(lockingInSound.clip, lockingInSound.properties);
            soundTimer = 0;
        }
    }

    protected override void ExecuteShot()
    {
        if (!canShoot) return;
        UpdateCurrentSpawnPointShootIndex();

        if (initializeDummyMissiles) RequestActivateDummyMissile(false);

        Projectile.ProjectileProperties prop = new Projectile.ProjectileProperties
        {
            position = spawnPoints[currentSpawnPointShootIndex.Value].position,
            rotation = spawnPoints[currentSpawnPointShootIndex.Value].rotation,
            ignoredObject = transform.root,
            root = transform.root.gameObject,
            target = currentTarget.GetComponent<NetworkObject>()
        };

        print(prop.target);

        if (ProjectileSpawner.Instance != null) ProjectileSpawner.Instance.CreateProjectile(properties.missilePrefab, properties.dummyMissilePrefab, prop, properties.projectileValues);

        UpdateAmmoAfterShot();
    }

    private void OnDrawGizmos()
    {
        if (lockInFowardReference == null) return;

        Vector3 origin = lockInFowardReference.position;
        Vector3 direction = lockInFowardReference.forward;
        Vector3 endPoint = origin + direction * maxLockDistance;

        // Desenha o cilindro/volume do SphereCast
        int segments = 20;
        float angleStep = 360f / segments;

        // Desenha vários círculos ao longo do caminho
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 center = Vector3.Lerp(origin, endPoint, t);

            // Ajusta a cor baseado na distância
            Color color = Color.Lerp(Color.yellow, Color.red, t);
            Gizmos.color = color;

            // Desenha o círculo neste ponto
            Vector3 up = Vector3.up;
            if (Vector3.Dot(direction, up) > 0.99f)
                up = Vector3.right;

            Vector3 right = Vector3.Cross(direction, up).normalized;
            up = Vector3.Cross(direction, right).normalized;

            for (int j = 0; j <= segments; j++)
            {
                float angle = j * angleStep * Mathf.Deg2Rad;
                Vector3 point = center + (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * lockRadius;

                if (j > 0)
                {
                    float prevAngle = (j - 1) * angleStep * Mathf.Deg2Rad;
                    Vector3 prevPoint = center + (right * Mathf.Cos(prevAngle) + up * Mathf.Sin(prevAngle)) * lockRadius;
                    Gizmos.DrawLine(prevPoint, point);
                }
            }
        }

        // Desenha o alvo se existir
        if (currentTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(origin, currentTarget.position);
            Gizmos.DrawWireSphere(currentTarget.position, 3f);

            // Mostra o tempo de lock atual
            UnityEditor.Handles.Label(
                currentTarget.position + Vector3.up * 5f,
                $"Lock: {currentLockTimer:F1}s / {lockOnTimeRequired:F1}s"
            );
        }

        // Mostra informações no editor
        UnityEditor.Handles.Label(
            origin + Vector3.up * 3f,
            $"Lock Radius: {lockRadius}\nMax Distance: {maxLockDistance}\nCan Shoot: {canShoot}"
        );
    }

}