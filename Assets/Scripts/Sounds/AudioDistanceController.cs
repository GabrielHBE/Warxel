using System.Collections.Generic;
using UnityEngine;

public class AudioDistanceController : MonoBehaviour
{
    [Header("Growth Settings")]
    public float initialGrowth = 1f;
    private float finalGrowth;
    private float growthSpeed = 1000f;

    [Header("Audio Settings")]
    private AudioSource audioSource;
    public bool playOnEnter = true;
    public bool stopOnExit = false;
    public bool preventRepeating = true;
    [SerializeField] private DistanceSounds[] distanceSounds;

    [Header("Detection Settings")]
    [SerializeField] private LayerMask cameraLayer;
    public bool useLayerMask = true;

    private SphereCollider sphereCollider;
    private bool isGrowing = false;
    private float currentSize;

    // Lista para rastrear objetos que já tocaram áudio
    private HashSet<GameObject> alreadyPlayedObjects = new HashSet<GameObject>();

    // Dicionário para rastrear qual som está tocando para cada objeto
    private Dictionary<GameObject, AudioClip> currentSoundForObject = new Dictionary<GameObject, AudioClip>();

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
        
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        finalGrowth = audioSource.maxDistance;

        // Configurar o AudioSource para ser 3D
        audioSource.spatialBlend = 1f; // Áudio totalmente 3D
        audioSource.rolloffMode = AudioRolloffMode.Linear;
    }

    void Update()
    {
        if (isGrowing)
        {
            currentSize += growthSpeed * Time.deltaTime;
            sphereCollider.radius = currentSize;

            if (currentSize >= finalGrowth)
            {
                Destroy(gameObject, audioSource.clip != null ? audioSource.clip.length : 0);
                isGrowing = false;
            }
        }
    }

    public void StartGrowth()
    {
        transform.SetParent(null);
        sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider == null)
        {
            sphereCollider = gameObject.AddComponent<SphereCollider>();
        }

        sphereCollider.isTrigger = true;
        currentSize = initialGrowth;
        sphereCollider.radius = currentSize;

        isGrowing = true;
    }

    public void StopGrowth()
    {
        isGrowing = false;
    }

    public void ResetSize()
    {
        isGrowing = false;
        currentSize = initialGrowth;
        if (sphereCollider != null)
        {
            sphereCollider.radius = currentSize;
        }
    }

    // Método para obter o som apropriado baseado na distância
    private AudioClip GetSoundForDistance(float distance)
    {
        if (distanceSounds == null || distanceSounds.Length == 0)
            return null;

        // Ordenar os sons por distância mínima (assumindo que estão em ordem)
        System.Array.Sort(distanceSounds, (a, b) => a.min_distance.CompareTo(b.min_distance));

        AudioClip selectedClip = null;
        float highestMinDistance = -1;

        foreach (var sound in distanceSounds)
        {
            if (distance >= sound.min_distance && distance <= sound.max_distance)
            {
                // Se estamos dentro do range deste som, seleciona-o
                selectedClip = sound.audio;
            }
            else if (distance > sound.max_distance)
            {
                // Se a distância é maior que o max_distance, ainda podemos usar
                // este som se não houver outro mais específico
                if (distance > sound.max_distance && sound.max_distance > highestMinDistance)
                {
                    highestMinDistance = sound.max_distance;
                    selectedClip = sound.audio;
                }
            }
        }

        return selectedClip;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isGrowing) return;

        GameObject targetObject = other.gameObject;
        AudioListener listener = targetObject.GetComponent<AudioListener>();

        if (listener == null || !listener.enabled)
        {
            return;
        }

        if (useLayerMask)
        {
            if ((cameraLayer.value & (1 << targetObject.layer)) == 0)
            {
                return;
            }
        }

        if (preventRepeating && alreadyPlayedObjects.Contains(targetObject))
        {
            return;
        }

        if (playOnEnter && audioSource != null)
        {
            // Calcular a distância do objeto até o centro do trigger
            float distanceToCenter = Vector3.Distance(transform.position, targetObject.transform.position);

            // Obter o som baseado na distância
            AudioClip selectedClip = GetSoundForDistance(distanceToCenter);

            if (selectedClip != null)
            {
                audioSource.PlayOneShot(selectedClip);
            }
            else
            {
                audioSource.PlayOneShot(audioSource.clip);
            }

            // Armazenar qual som foi tocado para este objeto
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
        if (listener == null)
        {
            return;
        }

        if (useLayerMask)
        {
            if ((cameraLayer.value & (1 << other.gameObject.layer)) == 0)
            {
                return;
            }
        }

        if (stopOnExit && audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log("Audio stopped.");
        }
    }

    // Método para verificar se um objeto específico está dentro de um range de distância
    public bool IsObjectInDistanceRange(GameObject targetObject, float minDistance, float maxDistance)
    {
        if (targetObject == null) return false;

        float distance = Vector3.Distance(transform.position, targetObject.transform.position);
        return distance >= minDistance && distance <= maxDistance;
    }

    void OnDrawGizmos()
    {
        if (sphereCollider != null)
        {
            // Desenhar círculos para cada range de distância
            if (distanceSounds != null)
            {
                foreach (var sound in distanceSounds)
                {
                    if (sound != null && sound.audio != null)
                    {
                        // Cor diferente para cada range
                        Color gizmoColor = Color.Lerp(Color.green, Color.red, sound.min_distance / finalGrowth);
                        Gizmos.color = gizmoColor;
                        Gizmos.DrawWireSphere(transform.position, sound.max_distance);

                        // Adicionar label com o nome do som
#if UNITY_EDITOR
                        UnityEditor.Handles.Label(transform.position + Vector3.up * sound.max_distance,
                                                $"{sound.audio.name}: {sound.min_distance}-{sound.max_distance}");
#endif
                    }
                }
            }

            // Desenhar o trigger atual
            Gizmos.color = isGrowing ? Color.red : Color.green;
            Gizmos.DrawWireSphere(transform.position, sphereCollider.radius);
        }
        else
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, initialGrowth);
        }
    }

    public void ConfigureCameraLayer(string layerName)
    {
        int layerIndex = LayerMask.NameToLayer(layerName);
        if (layerIndex != -1)
        {
            cameraLayer = 1 << layerIndex;
            Debug.Log($"Layer configured to: {layerName}");
        }
        else
        {
            Debug.LogError($"Layer '{layerName}' not found!");
        }
    }

    public void ClearPlayedObjects()
    {
        alreadyPlayedObjects.Clear();
        currentSoundForObject.Clear();
        Debug.Log("Cleared played objects list");
    }

    public bool HasObjectPlayed(GameObject targetObject)
    {
        return alreadyPlayedObjects.Contains(targetObject);
    }

    // Método para obter qual som foi tocado para um objeto específico
    public AudioClip GetPlayedSoundForObject(GameObject targetObject)
    {
        if (currentSoundForObject.ContainsKey(targetObject))
            return currentSoundForObject[targetObject];
        return null;
    }

    [System.Serializable]
    private class DistanceSounds
    {
        public AudioClip audio;
        public float min_distance;
        public float max_distance;
    }
}