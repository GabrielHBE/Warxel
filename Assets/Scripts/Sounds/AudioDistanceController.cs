using System.Collections.Generic;
using UnityEngine;

public class AudioDistanceController : MonoBehaviour
{
    [Header("Growth Settings")]
    public float initialGrowth = 1f;
    public float finalGrowth = 500f;
    public float growthSpeed = 600f;

    [Header("Audio Settings")]
    private AudioSource audioSource;
    public bool playOnEnter = true;
    public bool stopOnExit = false;
    public bool preventRepeating = true; // Nova opção

    [Header("Detection Settings")]
    [SerializeField] private LayerMask cameraLayer; // Camera layer
    public bool useLayerMask = true; // Enable/disable layer filter

    private SphereCollider sphereCollider;
    private bool isGrowing = false;
    private float currentSize;

    // Lista para rastrear objetos que já tocaram áudio
    private HashSet<GameObject> alreadyPlayedObjects = new HashSet<GameObject>();

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        // Ensure there's an AudioSource
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                finalGrowth = audioSource.maxDistance;
            }
            else
            {
                Debug.LogWarning("AudioSource not found. Adding one.");
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    void Update()
    {
        if (isGrowing)
        {
            // Increase the trigger size
            currentSize += growthSpeed * Time.deltaTime;
            sphereCollider.radius = currentSize;

            // Stop growth when reaching maximum
            if (currentSize >= finalGrowth)
            {
                Destroy(gameObject, audioSource.clip.length);
                isGrowing = false;
            }
        }
    }

    public void StartGrowth()
    {
        transform.SetParent(null);
        // Ensure there's a SphereCollider
        sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider == null)
        {
            sphereCollider = gameObject.AddComponent<SphereCollider>();
        }

        // Configure as trigger
        sphereCollider.isTrigger = true;

        // Set initial size
        currentSize = initialGrowth;
        sphereCollider.radius = currentSize;

        isGrowing = true;
    }

    // Method to stop growth
    public void StopGrowth()
    {
        isGrowing = false;
    }

    // Method to reset to initial size
    public void ResetSize()
    {
        isGrowing = false;
        currentSize = initialGrowth;
        if (sphereCollider != null)
        {
            sphereCollider.radius = currentSize;
        }
    }

    // Detect when AudioListener enters the trigger
    private void OnTriggerEnter(Collider other)
    {
        if (!isGrowing) return;

        GameObject targetObject = other.gameObject;
        AudioListener listener = targetObject.GetComponent<AudioListener>();

        // SEMPRE verificar se tem AudioListener (requisito principal)
        if (listener == null || !listener.enabled)
        {
            return;
        }

        // Se useLayerMask estiver ativado, também verificar a camada
        if (useLayerMask)
        {
            if ((cameraLayer.value & (1 << targetObject.layer)) == 0)
            {
                return;
            }
        }

        // Check if we should prevent repeating on same object
        if (preventRepeating && alreadyPlayedObjects.Contains(targetObject))
        {
            //Debug.Log("Audio already played for this object: " + targetObject.name);
            return;
        }

        // If reached here, object is valid and hasn't played before
        if (playOnEnter && audioSource != null)
        {
            //audioSource.Play();
            audioSource.PlayOneShot(audioSource.clip);
            // Add to the list of objects that have played
            if (preventRepeating)
            {
                alreadyPlayedObjects.Add(targetObject);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!isGrowing) return;

        // Verificar se tem AudioListener
        AudioListener listener = other.GetComponent<AudioListener>();
        if (listener == null)
        {
            return;
        }

        // Se useLayerMask estiver ativado, também verificar a camada
        if (useLayerMask)
        {
            // Use bitwise AND to check if object's layer is in LayerMask
            if ((cameraLayer.value & (1 << other.gameObject.layer)) == 0)
            {
                return;
            }
        }

        // If reached here, object has AudioListener and is in correct layer (if using layer mask)
        if (stopOnExit && audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log("Audio stopped.");
        }
    }

    // Method to force detection of a specific object (useful for tests)
    public bool IsInsideTrigger(GameObject targetObject)
    {
        if (sphereCollider == null) return false;

        float distance = Vector3.Distance(transform.position, targetObject.transform.position);
        return distance <= sphereCollider.radius;
    }

    // Method for editor visualization
    void OnDrawGizmos()
    {
        if (sphereCollider != null)
        {
            Gizmos.color = isGrowing ? Color.red : Color.green;
            Gizmos.DrawWireSphere(transform.position, sphereCollider.radius);

            // Show current radius as text
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * sphereCollider.radius,
                                    $"Radius: {sphereCollider.radius:F1}");
#endif
        }
        else
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, initialGrowth);
        }
    }

    // Method to configure layer manually via code
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

    // Clear the played objects list (useful for resetting)
    public void ClearPlayedObjects()
    {
        alreadyPlayedObjects.Clear();
        Debug.Log("Cleared played objects list");
    }

    // Check if an object has already triggered audio
    public bool HasObjectPlayed(GameObject targetObject)
    {
        return alreadyPlayedObjects.Contains(targetObject);
    }
}