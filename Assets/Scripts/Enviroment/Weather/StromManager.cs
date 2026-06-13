using FishNet.Object;
using UnityEngine;

public class StromManager : NetworkBehaviour
{
    [Header("System References")]
    [SerializeField] private WeatherStateManager weatherState;

    [Header("Sounds")]
    [SerializeField] private AudioClip thunderSound; // Mudei para AudioClip
    [SerializeField] private SoundManager.SoundProperties soundProperties = SoundManager.SoundProperties.Default;

    [Header("Strom Effects")]
    [SerializeField] private GameObject lightningEffectPrefab; // Mudei para GameObject
    
    [Header("Lightning Settings")]
    [SerializeField] private float minLightningInterval = 5f;
    [SerializeField] private float lightningDestroyDelay = 2f;

    private float currentLightningTimer;
    private float currentLighntingInterval;

    public override void OnStartServer()
    {
        base.OnStartServer();
        currentLighntingInterval = Random.Range(minLightningInterval, minLightningInterval * 2f);
    }

    void Update()
    {
        if (!IsServerInitialized) return;

        if (weatherState.ActiveWeatherType.Value == WeatherStateManager.WeatherType.Storm)
        {
            currentLightningTimer += Time.deltaTime;

            if (currentLightningTimer >= currentLighntingInterval)
            {
                currentLightningTimer = 0f;
                currentLighntingInterval = Random.Range(minLightningInterval, minLightningInterval * 2f);
                TriggerLightning();
            }
        }
    }

    private void TriggerLightning()
    {
        Vector3 lightningPosition = GetRandomLightningPosition();

        if (lightningPosition == Vector3.zero) return;

        // Instancia o efeito visual do raio
        GameObject lightningGO = Instantiate(lightningEffectPrefab, lightningPosition, Quaternion.identity);
        Spawn(lightningGO);

        // Toca o efeito visual se tiver ParticleSystem
        ParticleSystem ps = lightningGO.GetComponent<ParticleSystem>();
        if (ps != null)
            ps.Play();

        SoundManager.Instance.RequestPlay3dSound(thunderSound.name, soundProperties, lightningPosition, true);

        StartCoroutine(DestroyAfterDelay(lightningGO, lightningDestroyDelay));
    }

    private Vector3 GetRandomLightningPosition()
    {

        float x = Random.Range(-1000f, 1000f);
        float z = Random.Range(-1000f, 1000f);

        if (Physics.Raycast(new Vector3(x, MapSettings.Instance.max_altitude, z), Vector3.down, out RaycastHit hit, MapSettings.Instance.max_altitude))
        {
            if (hit.transform.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                DamagePlayer(hit);

            }else if (hit.transform.gameObject.layer == LayerMask.NameToLayer("Vechicle"))
            {
                DamageVehicle(hit);
            }

            return hit.point;
        }

        return Vector3.zero;
    }

    private void DamagePlayer(RaycastHit hit)
    {
        PlayerController player = hit.transform.GetComponent<PlayerController>();
        if (player != null)
        {
            player.RequestDamage(100);
        }
    }

    private void DamageVehicle(RaycastHit hit)
    {
        Vehicle vehicle = hit.transform.GetComponent<Vehicle>();
        if (vehicle != null)
        {
            vehicle.RequestDamage(100);
        }
    }

    private System.Collections.IEnumerator DestroyAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null)
            Destroy(obj);
    }
}