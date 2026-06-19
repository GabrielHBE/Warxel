using System.Collections.Generic;
using UnityEngine;

// 1. Herda de LocalPooledObject em vez de MonoBehaviour
public class AudioDistanceController : LocalPooledObject
{
    [Header("Growth Settings")]
    private float finalGrowth;
    private float growthSpeed = 2000f;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    public bool playOnEnter = true;
    public bool stopOnExit = false;
    public bool preventRepeating = true;
    [SerializeField] private DistanceSounds[] distanceSounds;

    [Header("Detection Settings")]
    [SerializeField] private LayerMask cameraLayer;
    public bool useLayerMask = true;

    [Header("Camera Shake")]
    [SerializeField] private bool enableCameraShakeOnHit;
    [SerializeField] private float cameraShakeIntensity = 2;
    [SerializeField] private float cameraShakeDuration = 1;

    [SerializeField] private SphereCollider sphereCollider;
    private bool isGrowing = false;
    private float currentSize;

    private HashSet<GameObject> alreadyPlayedObjects = new HashSet<GameObject>();
    private Dictionary<GameObject, AudioClip> currentSoundForObject = new Dictionary<GameObject, AudioClip>();

    private float initialGrowth = 0;

    // 2. Limpa o estado quando sair do Pool
    void OnEnable()
    {
        ClearPlayedObjects();
        isGrowing = false;
        currentSize = initialGrowth;
        if (sphereCollider != null) sphereCollider.radius = currentSize;
    }

    void Update()
    {
        if (isGrowing)
        {
            currentSize += growthSpeed * Time.deltaTime;
            sphereCollider.radius = currentSize;

            if (currentSize >= finalGrowth)
            {
                isGrowing = false;

                // 3. Em vez de Destroy, iniciamos a rotina para devolver ao Pool
                float delay = audioSource.clip != null ? audioSource.clip.length : 0;
                StartCoroutine(DeactivateAfterDelay(delay));
            }
        }
    }

    private System.Collections.IEnumerator DeactivateAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Deactivate(); // Método herdado que devolve ao pool
    }

    public void Setup(AudioClip clip, SoundManager.SoundProperties soundProperties)
    {
        ApplySoundProperties(clip, soundProperties);

        finalGrowth = audioSource.maxDistance;
    }

    public void StartGrowth()
    {
        sphereCollider.isTrigger = true;
        currentSize = initialGrowth;
        sphereCollider.radius = currentSize;
        isGrowing = true;
    }

    public void StopGrowth() => isGrowing = false;

    public void ResetSize()
    {
        isGrowing = false;
        currentSize = initialGrowth;
        if (sphereCollider != null) sphereCollider.radius = currentSize;
    }

    private AudioClip GetSoundForDistance(float distance)
    {
        if (distanceSounds == null || distanceSounds.Length == 0) return null;
        System.Array.Sort(distanceSounds, (a, b) => a.min_distance.CompareTo(b.min_distance));

        AudioClip selectedClip = null;
        float highestMinDistance = -1;

        foreach (var sound in distanceSounds)
        {
            if (distance >= sound.min_distance && distance <= sound.max_distance) selectedClip = sound.audio;
            else if (distance > sound.max_distance && sound.max_distance > highestMinDistance)
            {
                highestMinDistance = sound.max_distance;
                selectedClip = sound.audio;
            }
        }
        return selectedClip;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isGrowing) return;

        GameObject targetObject = other.gameObject;
        AudioListener listener = targetObject.GetComponent<AudioListener>();

        if (listener == null || !listener.enabled) return;
        if (useLayerMask && (cameraLayer.value & (1 << targetObject.layer)) == 0) return;
        if (preventRepeating && alreadyPlayedObjects.Contains(targetObject)) return;

        if (playOnEnter && audioSource != null)
        {
            float distanceToCenter = Vector3.Distance(transform.position, targetObject.transform.position);
            AudioClip selectedClip = GetSoundForDistance(distanceToCenter);

            if (selectedClip != null) audioSource.PlayOneShot(selectedClip);
            else audioSource.PlayOneShot(audioSource.clip);

            if (enableCameraShakeOnHit)
            {
                CameraShake cameraShake = targetObject.GetComponentInParent<CameraShake>();
                if (cameraShake != null) cameraShake.RequestShake(cameraShakeIntensity, cameraShakeDuration);
            }

            if (preventRepeating)
            {
                alreadyPlayedObjects.Add(targetObject);
                currentSoundForObject[targetObject] = selectedClip;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!isGrowing) return;

        AudioListener listener = other.GetComponent<AudioListener>();
        if (listener == null) return;
        if (useLayerMask && (cameraLayer.value & (1 << other.gameObject.layer)) == 0) return;

        if (stopOnExit && audioSource != null && audioSource.isPlaying) audioSource.Stop();
    }

    public void ClearPlayedObjects()
    {
        alreadyPlayedObjects.Clear();
        currentSoundForObject.Clear();
    }

    public void ApplySoundProperties(AudioClip clip, SoundManager.SoundProperties properties)
    {
        if (audioSource == null) return;
        audioSource.playOnAwake = false;
        audioSource.clip = clip;
        audioSource.priority = properties.priority;
        audioSource.volume = properties.volume;
        audioSource.pitch = properties.pitch;
        audioSource.panStereo = properties.stereoPan;
        audioSource.spatialBlend = properties.spatialBlend;
        audioSource.reverbZoneMix = properties.reverbZoneMix;
        audioSource.dopplerLevel = properties.dopplerLevel;
        audioSource.spread = properties.spread;
        audioSource.rolloffMode = properties.rolloffMode;
        audioSource.minDistance = properties.minDistance;
        audioSource.maxDistance = properties.maxDistance;
    }

    [System.Serializable]
    private class DistanceSounds
    {
        public AudioClip audio;
        public float min_distance;
        public float max_distance;
    }
}