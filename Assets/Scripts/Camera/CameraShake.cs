using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    private PlayerController playerController;
    private PlayerProperties playerProperties;

    public float bobSpeed;
    public float rotationAmount;

    // Sistema de camadas separadas
    private Vector3 basePosition;
    private Vector3 baseRotation;

    private Vector3 walkOffsetPosition = Vector3.zero;
    private Vector3 walkOffsetRotation = Vector3.zero;
    private Vector3 shakeOffset = Vector3.zero;
    private Vector3 aimShakeOffset = Vector3.zero;

    private Coroutine activeShakeCoroutine;
    private float timer = 0f;

    void Start()
    {
        playerController = GetComponentInParent<PlayerController>();
        playerProperties = GetComponentInParent<PlayerProperties>();

        // Posição e rotação base inicial
        basePosition = transform.localPosition;
        baseRotation = transform.localEulerAngles;
    }

    void Update()
    {
        // Calcula movimento de caminhada (bob) separadamente
        CalculateWalkBob();

        // Aplica TODAS as transformações em ordem
        ApplyAllTransformations();

        bobSpeed = playerController.currentMoveSpeed;
    }

    void CalculateWalkBob()
    {
        if (playerProperties.isGrounded && !playerProperties.is_dead.Value && !playerProperties.is_in_vehicle &&
            (Mathf.Abs(playerController.moveHorizontal) > .1f || Mathf.Abs(playerController.moveForward) > 0.1f))
        {
            timer += Time.deltaTime * bobSpeed;

            // Movimento vertical (bob)
            float newY = Mathf.Sin(timer) * bobSpeed;
            walkOffsetPosition = new Vector3(0, newY / 250, 0);

            // Rotação Z do movimento
            float rotationZ = Mathf.Sin(timer) * rotationAmount;
            walkOffsetRotation = new Vector3(0, 0, rotationZ);
        }
        else
        {
            timer = 0;

            // Suavemente retorna à posição/rotação neutra
            walkOffsetPosition = Vector3.Lerp(walkOffsetPosition, Vector3.zero, Time.deltaTime * 10f);
            walkOffsetRotation = Vector3.Lerp(walkOffsetRotation, Vector3.zero, Time.deltaTime * 10f);
        }
    }

    float timer_scope = 0f;
    public void CalculateScopeAimShake(float tension)
    {
        timer_scope += Time.deltaTime;

        float rotation = Mathf.Sin(timer_scope) * tension;

        aimShakeOffset = new Vector3(rotation, 0, rotation);

    }

    public void ResetAimShake()
    {
        timer_scope = 0f;
        if (aimShakeOffset != Vector3.zero) aimShakeOffset = Vector3.Lerp(aimShakeOffset, Vector3.zero, Time.deltaTime * 10f);
    }

    void ApplyAllTransformations()
    {
        // Ordem: Base → Walk Bob → Shake
        Vector3 finalPosition = basePosition + walkOffsetPosition;
        Vector3 finalRotation = baseRotation + walkOffsetRotation + shakeOffset + aimShakeOffset;

        transform.localPosition = finalPosition;
        transform.localEulerAngles = finalRotation;
    }

    public void RequestShake(float intensity = 1f, float duration = 0.1f)
    {
        if (!gameObject.activeSelf) return;

        // Se já houver um shake ativo, substitui pelo novo
        if (activeShakeCoroutine != null)
        {
            StopCoroutine(activeShakeCoroutine);
        }

        activeShakeCoroutine = StartCoroutine(ShakeRoutine(intensity, duration));
    }

    private IEnumerator ShakeRoutine(float intensity, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Calcula o offset do shake
            shakeOffset = CalculateShakeOffset(intensity);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Retorna suavemente à posição sem shake
        float returnTime = Mathf.Min(0.1f, duration * 0.5f);
        elapsed = 0f;
        Vector3 startingShake = shakeOffset;

        while (elapsed < returnTime)
        {
            float t = elapsed / returnTime;
            // Interpola suavemente de volta a zero
            shakeOffset = Vector3.Lerp(startingShake, Vector3.zero, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        shakeOffset = Vector3.zero;
        activeShakeCoroutine = null;
    }

    private Vector3 CalculateShakeOffset(float intensity)
    {
        float timer = Time.time * 5f; // Frequência diferente para cada eixo

        return new Vector3(
            // Eixo X: mistura de Perlin noise com seno para movimento orgânico
            (Mathf.PerlinNoise(timer * 1.2f, 0) * 2f - 1f) * intensity * 3f,

            // Eixo Y: usa combinação de noises para variação
            ((Mathf.PerlinNoise(0, timer * 1.5f) * 2f - 1f) * 0.4f +
             (Mathf.Sin(timer * 3f) * 0.6f)) * intensity * 0.8f,

            // Eixo Z: noise com offset diferente para variar
            (Mathf.PerlinNoise(timer * 0.8f, timer * 0.8f) * 2f - 1f) * intensity * 0.7f
        );
    }
}