using UnityEngine;

public class SniperAimShake : MonoBehaviour
{
    [SerializeField] private float tension;

    PlayerProperties playerProperties;
    CameraShake cameraShake;


    float elapsed=0;
    float duration = 2f;

    void Start()
    {
        playerProperties = GetComponentInParent<PlayerProperties>();
        cameraShake = GetComponentInParent<CameraShake>();
    }

    // Update is called once per frame
    void Update()
    {
        if (playerProperties.is_aiming)
        {

            if (elapsed == 0)
            {
                StartCoroutine(cameraShake.SniperShake(tension, duration / 2));
            }

            if (elapsed < duration)
            {
                elapsed += Time.deltaTime;
            }
            else
            {
                elapsed = 0;
            }

        }
    }
}
