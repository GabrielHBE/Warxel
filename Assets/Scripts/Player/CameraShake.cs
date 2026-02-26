using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public enum ShakeType
    {
        Jump,
        Reload,
        Explosion,
        PickWeapon,
        Crouch,
        Sniper,
        SideGrip,
        Damage,
    }

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

    private Coroutine activeShakeCoroutine;
    private float timer = 0f;

    // Parâmetros para cada tipo de shake - MAIS SUAVES
    private readonly float JUMP_SHAKE_INTENSITY = 2.5f; 
    private readonly float JUMP_SHAKE_DURATION = 0.25f; 

    private readonly float RELOAD_SHAKE_INTENSITY = 1f; 
    private readonly float RELOAD_SHAKE_DURATION = 0.5f; 

    private readonly float PICK_WEAPON_SHAKE_INTENSITY = 4f;
    private readonly float PICK_WEAPON_SHAKE_DURATION = 0.5f;

    private readonly float SIDE_GRIP_SHAKE_INTENSITY = 2f;
    private readonly float SIDE_GRIP_SHAKE_DURATION = 0.1f;

    private readonly float CROUCH_SHAKE_INTENSITY = 1f;
    private readonly float CROUCH_SHAKE_DURATION = 0.2f;

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
        if (playerProperties.isGrounded && !playerProperties.is_dead && !playerProperties.is_in_vehicle &&
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

    void ApplyAllTransformations()
    {
        // Ordem: Base → Walk Bob → Shake
        Vector3 finalPosition = basePosition + walkOffsetPosition;
        Vector3 finalRotation = baseRotation + walkOffsetRotation + shakeOffset;

        transform.localPosition = finalPosition;
        transform.localEulerAngles = finalRotation;
    }

    public void RequestShake(ShakeType type, float intensity = 1f, float duration = 0.1f)
    {
        // Usa valores padrão baseados no tipo se não especificados
        if (type == ShakeType.Jump)
        {
            intensity = JUMP_SHAKE_INTENSITY;
            duration = JUMP_SHAKE_DURATION;
        }
        else if (type == ShakeType.Reload)
        {
            intensity = RELOAD_SHAKE_INTENSITY;
            duration = RELOAD_SHAKE_DURATION;
        }
        else if (type == ShakeType.PickWeapon)
        {
            intensity = PICK_WEAPON_SHAKE_INTENSITY;
            duration = PICK_WEAPON_SHAKE_DURATION;
        }
        else if (type == ShakeType.SideGrip)
        {
            intensity = SIDE_GRIP_SHAKE_INTENSITY;
            duration = SIDE_GRIP_SHAKE_DURATION;
        }
        else if (type == ShakeType.Crouch)
        {
            intensity = CROUCH_SHAKE_INTENSITY;
            duration = CROUCH_SHAKE_DURATION;
        }

        // Se já houver um shake ativo, substitui pelo novo
        if (activeShakeCoroutine != null)
        {
            StopCoroutine(activeShakeCoroutine);
        }

        activeShakeCoroutine = StartCoroutine(ShakeRoutine(type, intensity, duration));
    }

    private IEnumerator ShakeRoutine(ShakeType type, float intensity, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Calcula o offset do shake
            shakeOffset = CalculateShakeOffset(type, intensity, elapsed / duration);

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

    private Vector3 CalculateShakeOffset(ShakeType type, float intensity, float progress)
    {
        // Usa noise para suavidade
        float time = Time.time * 20f;

        switch (type)
        {
            case ShakeType.Jump:
                // Para jump shake, adiciona rotação aleatória em todos os eixos
                // Usa uma mistura de funções de noise diferentes para cada eixo
                float jumpTime = Time.time * 7f; // Frequência diferente para cada eixo

                return new Vector3(
                    // Eixo X: mistura de Perlin noise com seno para movimento orgânico
                    (Mathf.PerlinNoise(jumpTime * 1.2f, 0) * 2f - 1f) * intensity * 2f,

                    // Eixo Y: usa combinação de noises para variação
                    ((Mathf.PerlinNoise(0, jumpTime * 1.5f) * 2f - 1f) * 0.4f +
                     (Mathf.Sin(jumpTime * 3f) * 0.6f)) * intensity * 0.8f,

                    // Eixo Z: noise com offset diferente para variar
                    (Mathf.PerlinNoise(jumpTime * 0.8f, jumpTime * 0.8f) * 2f - 1f) * intensity * 0.7f
                );

            case ShakeType.Reload:
                // Shake leve para recarga
                return new Vector3(
                    Mathf.Sin(progress * Mathf.PI * 2) * intensity,
                    (Mathf.PerlinNoise(time * 0.7f, 0) - 0.5f) * intensity * 0.5f,
                    (Mathf.PerlinNoise(0, time * 0.7f) - 0.5f) * intensity * 0.5f
                );

            case ShakeType.PickWeapon:
                // Shake moderado para pegar arma
                return new Vector3(
                    Mathf.Sin(progress * Mathf.PI * 3) * intensity * 0.7f,
                    (Mathf.PerlinNoise(time * 0.8f, 0) - 0.5f) * intensity * 0.4f,
                    (Mathf.PerlinNoise(0, time * 0.8f) - 0.5f) * intensity * 0.4f
                );

            case ShakeType.SideGrip:
                // Shake muito leve
                return new Vector3(
                    (Mathf.PerlinNoise(time * 0.5f, 0) - 0.5f) * intensity * 0.3f,
                    (Mathf.PerlinNoise(0, time * 0.5f) - 0.5f) * intensity * 0.3f,
                    (Mathf.PerlinNoise(time * 0.5f, time * 0.5f) - 0.5f) * intensity * 0.3f
                );

            case ShakeType.Crouch:
                // Shake para agachar
                // Para jump shake, adiciona rotação aleatória em todos os eixos
                // Usa uma mistura de funções de noise diferentes para cada eixo
                float crouchTime = Time.time * 5f; // Frequência diferente para cada eixo

                return new Vector3(
                    // Eixo X: mistura de Perlin noise com seno para movimento orgânico
                    (Mathf.PerlinNoise(crouchTime * 1.2f, 0) * 2f - 1f) * intensity * 3f,

                    // Eixo Y: usa combinação de noises para variação
                    ((Mathf.PerlinNoise(0, crouchTime * 1.5f) * 2f - 1f) * 0.4f +
                     (Mathf.Sin(crouchTime * 3f) * 0.6f)) * intensity * 0.8f,

                    // Eixo Z: noise com offset diferente para variar
                    (Mathf.PerlinNoise(crouchTime * 0.8f, crouchTime * 0.8f) * 2f - 1f) * intensity * 0.7f
                );

            case ShakeType.Sniper:
                // Shake para sniper
                float sniperDecay = 1f - (progress * progress * progress);
                return new Vector3(
                    (Mathf.PerlinNoise(time * 2f, 0) - 0.5f) * intensity * sniperDecay,
                    (Mathf.PerlinNoise(0, time * 2f) - 0.5f) * intensity * sniperDecay,
                    (Mathf.PerlinNoise(time * 2f, time * 2f) - 0.5f) * intensity * sniperDecay
                );

            case ShakeType.Explosion:
                // Shake para explosão
                float explosionDecay = 1f - (progress * progress);
                return new Vector3(
                    (Mathf.PerlinNoise(time * 3f, 0) - 0.5f) * intensity * explosionDecay,
                    (Mathf.PerlinNoise(0, time * 3f) - 0.5f) * intensity * explosionDecay,
                    (Mathf.PerlinNoise(time * 3f, time * 3f) - 0.5f) * intensity * explosionDecay
                );

            case ShakeType.Damage:
                // Shake para explosão
                float damagedecay = 1f - (progress * progress);
                return new Vector3(
                    (Mathf.PerlinNoise(time * 3f, 0) - 0.5f) * intensity * damagedecay,
                    (Mathf.PerlinNoise(0, time * 3f) - 0.5f) * intensity * damagedecay,
                    (Mathf.PerlinNoise(time * 3f, time * 3f) - 0.5f) * intensity * damagedecay
                );

            default:
                return Vector3.zero;
        }
    }

    // Métodos públicos para compatibilidade (opcional)
    public void RequestJumpShake() => RequestShake(ShakeType.Jump);
    public void RequestReloadShake() => RequestShake(ShakeType.Reload);
    public void RequestPickWeaponShake() => RequestShake(ShakeType.PickWeapon);
    public void RequestSideGripShake() => RequestShake(ShakeType.SideGrip);
    public void RequestCrouchShake() => RequestShake(ShakeType.Crouch);
    public void RequestSniperShake(float tension, float duration) => RequestShake(ShakeType.Sniper, tension, duration);
    public void RequestExplosionShake(float tension, float duration) => RequestShake(ShakeType.Explosion, tension, duration);
}