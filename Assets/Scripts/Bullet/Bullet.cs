using System.Collections;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using VoxelDestructionPro.Tools;
using VoxelDestructionPro.VoxelObjects;

public class Bullet : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private BulletHitEffects hitEffects;
    [SerializeField] private BulletSoundEffect soundEffects;

    [Header("Settings")]
    [SerializeField] private Collider bullet_collider;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private TrailRenderer trail;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private VoxCollider voxCollider;

    //Private variables
    private float bulletDropMultiplier;
    private float infantary_damage;
    private float damage_dropoff;
    private float damage_dropoff_timer;
    private float minimum_damage;
    private float hs_multiplier;
    private bool can_damage_vehicles;
    private float vehicle_damage;
    private Vector3 original_position;
    private bool did_ricochet;
    private float timer;
    private float delaytoEnableForNonOwner;

    // Variável de controle para o despawn
    private bool isDespawning;

    private Vector3 lastPosition;

    private Transform ignoredTransform;
    private GameObject shoot_root;

    public struct BulletData
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 direction;
        public float speed;
        public float dropMultiplier;
        public float infantaryDamage;
        public float damageDropoff;
        public float damageDropoffTimer;
        public float destructionForce;
        public float minimumDamage;
        public float hsMultiplier;
        public float size;
        public bool canDamageVehicles;
        public float vehicleDamage;
        public float delaytoEnableForNonOwner;
    }

    #region Bullet Creation
    [ObserversRpc]
    public void CreateBullet(BulletData data, Transform ignoredObject = null, GameObject root = null)
    {
        meshRenderer.enabled = false;
        trail.enabled = false;

        ignoredTransform = ignoredObject;

        shoot_root = root;

        isDespawning = false;
        voxCollider.destructionRadius = data.destructionForce;
        did_ricochet = false;
        infantary_damage = data.infantaryDamage;
        damage_dropoff = data.damageDropoff;
        damage_dropoff_timer = data.damageDropoffTimer;
        minimum_damage = data.minimumDamage;
        hs_multiplier = data.hsMultiplier;
        can_damage_vehicles = data.canDamageVehicles;
        vehicle_damage = data.vehicleDamage;
        delaytoEnableForNonOwner = data.delaytoEnableForNonOwner;
        SetDirection(data.direction, data.speed, data.dropMultiplier);

        if (data.size != 0) transform.localScale *= data.size;

        if (IsServerInitialized)
        {
            // Usando o novo método seguro no Invoke
            Invoke(nameof(DespawnLocalSafe), 10f);
        }

        if (IsOwner)
        {
            StartCoroutine(DelayForEnableBulletForOwner());
        }
        else
        {
            // Outros clients começam com a bala invisível
            meshRenderer.enabled = false;
            trail.enabled = false;
        }

        lastPosition = transform.position;
    }

    private IEnumerator DelayForEnableBulletForOwner()
    {
        yield return null;
        meshRenderer.enabled = true;
        trail.enabled = true;
    }

    public void SetDirection(Vector3 direction, float speed, float dropMultiplier)
    {
        original_position = transform.localPosition;
        rb.useGravity = false;
        if (TryGetComponent<FishNet.Component.Transforming.NetworkTransform>(out var nt))
        {
            nt.enabled = false;
        }

        rb.isKinematic = false;
        rb.linearVelocity = direction * speed;

        bulletDropMultiplier = dropMultiplier;
    }

    #endregion

    #region Updates

    // 1. Coloque o Array aqui em cima, nas variáveis globais da classe!
    private RaycastHit[] hit_results = new RaycastHit[128];

    void FixedUpdate()
    {
        rb.AddForce(Vector3.down * bulletDropMultiplier, ForceMode.Acceleration);

        if (!IsOwner) return;

        if (bullet_collider.isTrigger)
        {
            Vector3 currentPosition = transform.position;
            Vector3 direction = currentPosition - lastPosition;
            float distance = direction.magnitude;

            if (distance > 0)
            {
                int layerMask = ~(1 << LayerMask.NameToLayer("Projectile") | 1 << LayerMask.NameToLayer("Player"));

                // 2. Agora o Raycast preenche o Array global, não gerando MAIS NENHUM lixo na memória!
                int hits = Physics.RaycastNonAlloc(lastPosition, direction.normalized, hit_results, distance, layerMask);

                if (hits > 0)
                {
                    // 3. Vamos achar o hit mais próximo manualmente. É muito mais leve do que usar System.Array.Sort
                    RaycastHit closestHit = default;
                    float minDistance = float.MaxValue;
                    bool foundValidHit = false;

                    // O loop vai APENAS até a quantidade de "hits", ignorando os espaços nulos do Array
                    for (int i = 0; i < hits; i++)
                    {
                        RaycastHit hit = hit_results[i];

                        // Ignora o atirador e os filhos dele
                        if (ignoredTransform != null && hit.collider.transform.IsChildOf(ignoredTransform))
                        {
                            continue;
                        }

                        // Se a distância for menor que a gravada, salvamos como o hit mais próximo
                        if (hit.distance < minDistance)
                        {
                            minDistance = hit.distance;
                            closestHit = hit;
                            foundValidHit = true;
                        }
                    }

                    // Se achou um alvo válido, processa o tiro
                    if (foundValidHit)
                    {
                        HandleBulletHit(closestHit.collider.gameObject, closestHit.point, closestHit.normal, closestHit.collider);
                    }
                }
            }
            // Atualiza a posição antiga para o próximo frame
            lastPosition = currentPosition;
        }
    }
    float time_to_enable_view;
    void Update()
    {
        if (!IsOwner && !meshRenderer.enabled)
        {
            time_to_enable_view += Time.deltaTime;
            if (time_to_enable_view > delaytoEnableForNonOwner)
            {
                EnableBulletView();
            }
        }

        if (!IsOwner) return;

        if (infantary_damage > minimum_damage && damage_dropoff != 0)
        {
            timer += Time.deltaTime;
            if (timer >= damage_dropoff_timer)
            {
                infantary_damage -= damage_dropoff;
                timer = 0;
            }
        }

    }

    #endregion

    private void HandleBulletHit(GameObject hitObject, Vector3 hitPoint, Vector3 hitNormal, Collider collider)
    {
        if (did_ricochet)
        {
            DespawnLocalSafe();
            return;
        }

        float calculated_distance = Vector3.Distance(original_position, hitPoint);

        if (calculated_distance > 3)
        {
            hitEffects.RequestCustomHitEffect(hitPoint);

            if (hitObject.layer == LayerMask.NameToLayer("Voxel"))
            {
                ProcessVoxelCollision(collider, hitPoint);
                VoxelObjBase voxelObj = hitObject.GetComponent<VoxelObjBase>();
                if (voxelObj != null)
                {
                    VoxelMaterials.VoxelMaterialType material = voxelObj.material;
                    hitEffects.RequestVoxelHitEffect(hitPoint, material);

                    // NEW: Use the local helper method that maps materials and sends the RPC
                    soundEffects.RequestVoxelHitSound(hitPoint, material);
                }
            }

            if (hitObject.layer == LayerMask.NameToLayer("Ground"))
            {
                ProcessGroundCollision(hitPoint);
            }

            if (hitObject.layer == LayerMask.NameToLayer("Vehicle") && can_damage_vehicles)
            {
                ProcessVehicleCollision(collider);
                hitEffects.RequestMetalHitEffect(hitPoint, Quaternion.LookRotation(hitNormal == Vector3.zero ? -transform.forward : hitNormal));

                // NEW: Use CmdPlayHitSound instead of direct method
                soundEffects.CmdPlayHitSound(BulletSoundEffect.HitSoundType.Metal, hitPoint);
            }

            if (hitObject.layer == LayerMask.NameToLayer("PlayerHitBox"))
            {
                ProcessPlayerCollision(hitObject);
                hitEffects.RequestBloodHitEffect(hitPoint, Quaternion.LookRotation(hitNormal == Vector3.zero ? -transform.forward : hitNormal));
            }

            DespawnLocalSafe();
        }
        else
        {
            if (hitObject.layer == LayerMask.NameToLayer("PlayerHitBox"))
            {
                ProcessPlayerCollision(hitObject);
                hitEffects.RequestBloodHitEffect(hitPoint, Quaternion.LookRotation(hitNormal == Vector3.zero ? -transform.forward : hitNormal));
            }
            else
            {
                Ricochet(hitPoint);
            }

        }
    }


    #region TriggerEnter

    void OnTriggerEnter(Collider collider)
    {
        if (!bullet_collider.isTrigger) return;
        if (!IsOwner) return;

        // Ignora colisões com balas ou física do player
        if (collider.gameObject.layer == LayerMask.NameToLayer("Projectile") ||
            collider.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            return;
        }

        // Ignora o objeto passado no parâmetro (e as hitboxes filhas dele)
        if (ignoredTransform != null && collider.transform.IsChildOf(ignoredTransform))
        {
            return;
        }

        HandleBulletHit(collider.gameObject, transform.position, Vector3.zero, collider);
    }

    #endregion

    #region CollisionEnter

    void OnCollisionEnter(Collision collision)
    {
        if (bullet_collider.isTrigger) return;
        if (!IsOwner) return;

        // Ignora o objeto passado no parâmetro
        if (ignoredTransform != null && collision.gameObject.transform.IsChildOf(ignoredTransform))
        {
            return;
        }

        if (collision.contacts.Length > 0)
        {
            ContactPoint contact = collision.contacts[0];
            HandleBulletHit(contact.otherCollider.gameObject, contact.point, contact.normal, contact.otherCollider);
        }
    }

    #endregion

    #region Collision / Trigger Helper Methods

    private void ProcessVoxelCollision(Collider collision, Vector3 position)
    {
        if (voxCollider.destructionRadius > 2)
        {

            voxCollider.SphereExplosion(position, infantary_damage, vehicle_damage);
        }
        else
        {
            voxCollider.Collide(collision);
        }
    }

    private void ProcessGroundCollision(Vector3 pos)
    {
        voxCollider.SphereExplosion(pos, infantary_damage, vehicle_damage);
    }

    private void ProcessVehicleCollision(Collider collision)
    {

        Vehicle hit_vehicle = collision.gameObject.GetComponent<Vehicle>() ?? collision.gameObject.GetComponentInParent<Vehicle>();

        if (hit_vehicle != null)
        {
            if (!hit_vehicle.vehicle_destroyed.Value)
            {
                hit_vehicle.RequestDamage(vehicle_damage);

                float target_resistance = hit_vehicle.GetResistance();
                float final_actual_damage = vehicle_damage * ((100f - target_resistance) / 100f);

                DamageMarker.Instance.UpdateDamage(final_actual_damage);
            }
            else
            {
                if (shoot_root != null)
                {
                    Vehicle v = shoot_root.GetComponent<Vehicle>();
                    if (v != null)
                    {
                        v.AddKill();
                    }
                }

            }

        }
    }

    private void ProcessPlayerCollision(GameObject collision)
    {
        bool hs_hit = false;
        PlayerController player = collision.GetComponentInParent<PlayerController>();
        PlayerProperties playerProperties = player.GetComponent<PlayerProperties>();

        // Verificamos APENAS a variável real da rede. Ignoramos propriedades corrompidas.
        if (player == null || playerProperties.is_dead.Value) return;

        float start_hp = playerProperties.hp.Value;

        float base_damage;

        if (collision.gameObject.CompareTag("PlayerHead"))
        {
            base_damage = infantary_damage * hs_multiplier;
            hs_hit = true;
        }
        else if (collision.gameObject.CompareTag("Arms and Legs"))
        {
            base_damage = infantary_damage * 0.8f;
        }
        else if (collision.gameObject.CompareTag("Feet and Hands"))
        {
            base_damage = infantary_damage * 0.6f;
        }
        else
        {
            base_damage = infantary_damage;
        }

        player.RequestDamage(base_damage);

        // 1. Simula o cálculo de dano localmente que o servidor vai fazer
        float target_resistance = player.GetResistance();
        float dano_real_esperado = base_damage * ((100f - target_resistance) / 100f);

        // 2. Subtrai o dano da vida inicial que vimos no momento do impacto
        float post_hp = start_hp - dano_real_esperado;

        // 3. Agora sim, se a vida restante simulada for menor ou igual a zero, é letal
        bool is_lethal_shot = post_hp <= 0;

        if (is_lethal_shot)
        {
            EliminationMarker.Instance.InstantiateVehicleImage();

            AccountManager.Instance.status.AddKill();
            if (hs_hit) AccountManager.Instance.status.AddHeadShotKill();
            AccountManager.Instance.AddPointsToLevelUp(10);

            if (shoot_root != null)
            {
                WeaponProperties wp = shoot_root.GetComponent<WeaponProperties>();
                if (wp != null)
                {
                    wp.AddKill();
                }
            }

            KillFeedDisplay.Instance.AddKill(AccountManager.Instance.account_name, playerProperties.player_name.Value, "Placeholder");
        }

        DamageMarker.Instance.UpdateDamage(dano_real_esperado);
    }
    #endregion

    #region Extras
    private void EnableBulletView()
    {
        meshRenderer.enabled = true;
        trail.enabled = true;
    }

    void Ricochet(Vector3 position)
    {
        did_ricochet = true;
        rb.linearVelocity /= 1.5f;

        // NEW: Use CmdPlayHitSound instead of direct method
        soundEffects.CmdPlayHitSound(BulletSoundEffect.HitSoundType.Ricochet, position);
    }

    #endregion


    #region Despawning

    // Novo método para desativar a física antes de enviar o RPC pro servidor
    private void DespawnLocalSafe()
    {
        if (isDespawning || !IsSpawned) return;
        isDespawning = true;

        GetComponent<BoxCollider>().enabled = false;
        rb.isKinematic = true;

        if (TryGetComponent<FishNet.Component.Transforming.NetworkTransform>(out var nt))
        {
            nt.enabled = false;
        }

        CancelInvoke(nameof(DespawnLocalSafe));
        RequestDespawn();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestDespawn()
    {
        if (IsSpawned)
        {
            Despawn(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #endregion

}