using System.Collections;
using UnityEngine;

public static class Recoil
{
    public const float MIN_RECOIL_VALUE = 0;
    public const float MAX_RECOIL_VALUE = 10;

    public const float MIN_FIRTSHOTINCREASER_VALUE = 0;
    public const float MAX_FIRTSHOTINCREASER_VALUE = 10;

    #region Camera Recoil
    public static (float vertical, float horizontal) CalculateCameraRecoil(
        float baseVertical,
        float baseHorizontal,
        float firstShotMultiplier,
        bool isFirstShot,
        int bulletsInMag)
    {
        float finalfirstShotMultiplier = System.Math.Clamp(firstShotMultiplier, MIN_FIRTSHOTINCREASER_VALUE, MAX_FIRTSHOTINCREASER_VALUE);
        float finalVertical = baseVertical;
        float finalHorizontal = baseHorizontal;

        // Aplica o multiplicador no primeiro tiro da sequência e na última bala do pente.
        if (isFirstShot || bulletsInMag == 1)
        {
            finalVertical *= finalfirstShotMultiplier;
            finalHorizontal *= finalfirstShotMultiplier;
        }

        return (finalVertical, finalHorizontal);
    }

    public static Vector3 CalculateVisualRecoilOffset(Vector3 baseVisualRecoil, bool aiming)
    {
        float randomSignX = Random.value > 0.5f ? 1f : -1f;
        float randomSignY = Random.value > 0.5f ? 1f : -1f;

        // Multiplica apenas o eixo X pelo valor sorteado
        Vector3 randomizedRecoil = new Vector3(
            (baseVisualRecoil.x / 100) * randomSignX,
            (baseVisualRecoil.y / 100) * randomSignY,
            -baseVisualRecoil.z / 100
        );

        if (!aiming) return randomizedRecoil;

        return randomizedRecoil / 2f;
    }

    public static Vector3 CalculateVisualRotationOffset(Vector3 maxRotationRecoil, bool aiming)
    {
        // Sorteia um valor entre o negativo e o positivo da rotação máxima para cada eixo
        float randomX = Random.Range(-maxRotationRecoil.x, 0);
        float randomY = Random.Range(-maxRotationRecoil.y, maxRotationRecoil.y);
        float randomZ = Random.Range(-maxRotationRecoil.z, maxRotationRecoil.z);

        Vector3 randomizedRotation = new Vector3(randomX, randomY, randomZ);

        // Se estiver mirando, reduz a rotação pela metade (seguindo sua lógica de posição)
        if (!aiming) return randomizedRotation;

        return randomizedRotation / 2f;
    }

    public static float CalculateCameraZRoll(float horizontal, float vertical) => ((horizontal + vertical) / 5) * (Random.value > 0.5f ? 1f : -1f);

    public static float GetHorizontalRecoilDirection(HorizontalRecoil horizontalRecoil)
    {
        if (horizontalRecoil.type == HorizontalRecoilType.Left) return horizontalRecoil.value * -1;

        return horizontalRecoil.value;
    }

    public static float GetVerticalRecoilDirection(VerticalRecoil verticalRecoil)
    {
        if (verticalRecoil.type == VerticalRecoilType.Down) return verticalRecoil.value * -1;

        return verticalRecoil.value;
    }
    #endregion

    #region Visual Recoil
    public static void StartRecoilAnimation(
        Vector3 start,
        Vector3 target,
        float applyRecoilSpeed,
        float resetRecoilSpeed,
        AnimationCurve applyCurve,
        AnimationCurve resetCurve,
        Transform appliedTransform)
        => ApplyPositionRecoilAnimation(start, target, applyRecoilSpeed, resetRecoilSpeed, applyCurve, resetCurve, appliedTransform);

    public static IEnumerator ApplyRotationRecoilAnimation(
        Quaternion start,
        Quaternion target,
        float applyRecoilSpeed,
        float resetRecoilSpeed,
        AnimationCurve applyCurve,
        AnimationCurve resetCurve,
        Transform appliedTransform)
    {
        float elapsed = 0f;
        appliedTransform.localRotation = start;

        // Fase de Aplicação da Rotação
        while (elapsed < applyRecoilSpeed)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = applyRecoilSpeed > 0 ? Mathf.Clamp01(elapsed / applyRecoilSpeed) : 1f;
            float curveValue = applyCurve != null ? applyCurve.Evaluate(normalizedTime) : normalizedTime;

            appliedTransform.localRotation = Quaternion.Slerp(start, target, curveValue);
            yield return null;
        }

        appliedTransform.localRotation = target;
        elapsed = 0f;

        // Fase de Reset da Rotação
        while (elapsed < resetRecoilSpeed)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = resetRecoilSpeed > 0 ? Mathf.Clamp01(elapsed / resetRecoilSpeed) : 1f;
            float curveValue = resetCurve != null ? resetCurve.Evaluate(normalizedTime) : normalizedTime;

            appliedTransform.localRotation = Quaternion.Slerp(target, start, curveValue);
            yield return null;
        }

        appliedTransform.localRotation = start;
    }

    public static IEnumerator ApplyPositionRecoilAnimation(
        Vector3 start,
        Vector3 target,
        float applyRecoilSpeed,
        float resetRecoilSpeed,
        AnimationCurve applyCurve,
        AnimationCurve resetCurve,
        Transform appliedTransform)
    {
        float elapsed = 0f;

        appliedTransform.localPosition = start;

        // Fase de Aplicação do Recuo
        while (elapsed < applyRecoilSpeed)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = applyRecoilSpeed > 0 ? Mathf.Clamp01(elapsed / applyRecoilSpeed) : 1f;

            // Avalia a curva de aplicação, se fornecida; caso contrário, usa linear
            float curveValue = applyCurve != null ? applyCurve.Evaluate(normalizedTime) : normalizedTime;

            appliedTransform.localPosition = Vector3.Lerp(start, target, curveValue);
            yield return null;
        }

        appliedTransform.localPosition = target;
        elapsed = 0f;

        // Fase de Reset do Recuo
        while (elapsed < resetRecoilSpeed)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = resetRecoilSpeed > 0 ? Mathf.Clamp01(elapsed / resetRecoilSpeed) : 1f;

            // Avalia a curva de reset, se fornecida; caso contrário, usa linear
            float curveValue = resetCurve != null ? resetCurve.Evaluate(normalizedTime) : normalizedTime;

            appliedTransform.localPosition = Vector3.Lerp(target, start, curveValue);
            yield return null;
        }

        appliedTransform.localPosition = start;
    }

    public static IEnumerator ApplyVisualRecoilAnimation(
        Vector3 startPositionOffset,
        Vector3 targetPositionOffset,
        Quaternion startRotationOffset,
        Quaternion targetRotationOffset,
        float applyRecoilSpeed,
        float resetRecoilSpeed,
        AnimationCurve applyCurve,
        AnimationCurve resetCurve,
        System.Action<Vector3, Quaternion> applyOffsets)
    {
        yield return AnimateVisualRecoilPhase(
            startPositionOffset,
            targetPositionOffset,
            startRotationOffset,
            targetRotationOffset,
            applyRecoilSpeed,
            applyCurve,
            applyOffsets);

        yield return AnimateVisualRecoilPhase(
            targetPositionOffset,
            Vector3.zero,
            targetRotationOffset,
            Quaternion.identity,
            resetRecoilSpeed,
            resetCurve,
            applyOffsets);

        // Garante o estado final exato, sem erro residual de interpolação.
        applyOffsets(Vector3.zero, Quaternion.identity);
    }

    private static IEnumerator AnimateVisualRecoilPhase(
        Vector3 startPosition,
        Vector3 targetPosition,
        Quaternion startRotation,
        Quaternion targetRotation,
        float duration,
        AnimationCurve curve,
        System.Action<Vector3, Quaternion> applyOffsets)
    {
        if (duration <= 0f)
        {
            applyOffsets(targetPosition, targetRotation);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            float curveValue = curve != null ? curve.Evaluate(normalizedTime) : normalizedTime;

            applyOffsets(
                Vector3.Lerp(startPosition, targetPosition, curveValue),
                Quaternion.Slerp(startRotation, targetRotation, curveValue));

            yield return null;
        }

        applyOffsets(targetPosition, targetRotation);
    }

    public static Quaternion ResetVisualrecoilRotation(Transform transform, float resetVisualRecoilRotationSpeedMultiplier) => Quaternion.Slerp(transform.localRotation, Quaternion.identity, Time.deltaTime * resetVisualRecoilRotationSpeedMultiplier);
    #endregion

    #region Classes/Enums/Structs
    [System.Serializable]
    public struct RecoilPattern
    {
        public HorizontalRecoil horizontalRecoil;
        public VerticalRecoil verticalRecoil;
    }

    [System.Serializable]
    public class RecoilValues
    {
        public bool manualCalculateRecoil;
        public float applyRecoilSpeed;
        public float resetRecoilSpeed;
        [Range(MIN_FIRTSHOTINCREASER_VALUE, MAX_FIRTSHOTINCREASER_VALUE)] public float firstShootRecoilMultiplier = 1;
        public RecoilPattern[] recoilPattern = new RecoilPattern[1];

        [Header("Visual recoil")]
        public Vector3 visualPositionRecoil;
        public Vector3 maxRotationRecoil;
        [Range(1, 40)] public float resetVisualRecoilRotationSpeedMultiplier = 10;
        public AnimationCurve applyCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public AnimationCurve resetCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        public void CalculateRecoilSpeed(float interval)
        {
            if (manualCalculateRecoil) return;

            resetRecoilSpeed = interval / 2;
            applyRecoilSpeed = interval / 2;
        }
    }

    [System.Serializable]
    public struct HorizontalRecoil
    {
        [Range(MIN_RECOIL_VALUE, MAX_RECOIL_VALUE)] public float value;
        public HorizontalRecoilType type;
    }

    [System.Serializable]
    public struct VerticalRecoil
    {
        [Range(MIN_RECOIL_VALUE, MAX_RECOIL_VALUE)] public float value;
        public VerticalRecoilType type;
    }

    public enum HorizontalRecoilType { Left, Right }
    public enum VerticalRecoilType { Up, Down }
    #endregion
}
