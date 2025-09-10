using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{

    private PlayerController playerController;
    private PlayerProperties playerProperties;

    public float bobSpeed;

    public float bobAmount;
    public float rotationAmount;

    private float defaultYPos = 0f;
    private Vector3 defaultRotation;
    private float timer = 0f;

    void Start()
    {
        playerController = GetComponentInParent<PlayerController>();
        playerProperties = GetComponentInParent<PlayerProperties>();

        defaultYPos = transform.localPosition.y;
        defaultRotation = transform.localEulerAngles;


    }
    void Update()
    {
        if (playerProperties.isGrounded)
        {
            Run();
        }

        bobSpeed = playerController.MoveSpeed * 2;

    }

    void Run()
    {

        if (Mathf.Abs(playerController.moveHorizontal) > .1f || Mathf.Abs(playerController.moveForward) > 0.1f)
        {
            timer += Time.deltaTime * bobSpeed;
            float newY = Mathf.Sin(timer) * bobSpeed;

            float rotationZ = Mathf.Sin(timer) * rotationAmount;

            transform.localPosition = new Vector3(transform.localPosition.x, defaultYPos + newY / 100, transform.localPosition.z);
            transform.localEulerAngles = new Vector3(defaultRotation.x, defaultRotation.y, defaultRotation.z + rotationZ);
        }
        else
        {
            timer = 0;
            transform.localPosition = new Vector3(transform.localPosition.x, defaultYPos, transform.localPosition.z);
            transform.localEulerAngles = defaultRotation;
        }
    }

    public IEnumerator JumpCameraShake()
    {
        Quaternion originalRot = transform.localRotation;

        Quaternion upRot = originalRot * Quaternion.Euler(2, Random.Range(-2, 2), Random.Range(-2, 2));

        float duration = 0.1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            transform.localRotation = Quaternion.Lerp(originalRot, upRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localRotation = upRot;

        elapsed = 0f;
        while (elapsed < duration)
        {
            transform.localRotation = Quaternion.Lerp(upRot, originalRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localRotation = originalRot;
    }


    public IEnumerator ReloadShake()
    {
        Quaternion originalRot = transform.localRotation;

        Quaternion upRot = originalRot * Quaternion.Euler(Random.Range(-0.7f, 0.7f), Random.Range(-0.7f, 0.7f), Random.Range(-0.7f, 0.7f));


        float duration = 0.1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            transform.localRotation = Quaternion.Lerp(originalRot, upRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localRotation = upRot;

        elapsed = 0f;
        while (elapsed < duration)
        {
            transform.localRotation = Quaternion.Lerp(upRot, originalRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localRotation = originalRot;

    }

    public IEnumerator PickWeaponShake()
    {
        Quaternion originalRot = transform.localRotation;

        Quaternion upRot = originalRot * Quaternion.Euler(Random.Range(-1, 1f), Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f));

        float duration = 0.1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            transform.localRotation = Quaternion.Lerp(originalRot, upRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localRotation = upRot;

        elapsed = 0f;
        while (elapsed < duration)
        {
            transform.localRotation = Quaternion.Lerp(upRot, originalRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localRotation = originalRot;
    }

    public IEnumerator SideGripActivateShake()
    {
        Quaternion originalRot = transform.localRotation;

        Quaternion upRot = originalRot * Quaternion.Euler(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f));

        float duration = 0.1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            transform.localRotation = Quaternion.Lerp(originalRot, upRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localRotation = upRot;

        elapsed = 0f;
        while (elapsed < duration)
        {
            transform.localRotation = Quaternion.Lerp(upRot, originalRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localRotation = originalRot;
    }
    
    public IEnumerator SniperShake(float tension, float duration)
    {
        Quaternion originalRot = transform.localRotation;

        Quaternion upRot = originalRot * Quaternion.Euler(Random.Range(-tension, tension), Random.Range(-tension, tension), Random.Range(-tension, tension));

        float elapsed = 0f;

        while (elapsed < duration)
        {
            transform.localRotation = Quaternion.Lerp(originalRot, upRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localRotation = upRot;

        elapsed = 0f;
        while (elapsed < duration)
        {
            transform.localRotation = Quaternion.Lerp(upRot, originalRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localRotation = originalRot;
    }
    
    
    


}
