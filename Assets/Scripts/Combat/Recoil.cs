
using UnityEngine;

public static class Recoil
{
    public const float MIN_RECOIL_VALUE = -10;
    public const float MAX_RECOIL_VALUE = 10;

    public const float MIN_FIRTSHOTINCREASER_VALUE = 0;
    public const float MAX_FIRTSHOTINCREASER_VALUE = 10;

    /// <summary>
    /// Calcula a força do recuo vertical e horizontal que deve ser enviada para a câmera do jogador.
    /// </summary>
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

        // Aplica o multiplicador do primeiro tiro caso seja o primeiro disparo ou a última bala
        if (!isFirstShot || bulletsInMag == 1)
        {
            finalVertical *= finalfirstShotMultiplier;
            finalHorizontal *= finalfirstShotMultiplier;
        }
        finalVertical = System.Math.Clamp(finalVertical, MIN_RECOIL_VALUE, MAX_RECOIL_VALUE);
        finalHorizontal = System.Math.Clamp(finalHorizontal, MIN_RECOIL_VALUE, MAX_RECOIL_VALUE);

        return (finalVertical, finalHorizontal);
    }

    /// <summary>
    /// Calcula o deslocamento (offset) físico da arma na tela do jogador.
    /// </summary>
    public static Vector3 CalculateVisualRecoilOffset(Vector3 baseVisualRecoil, bool isAiming)
    {
        float randomSignX = Random.value > 0.5f ? 1f : -1f;
        float randomSignY = Random.value > 0.5f ? 1f : -1f;

        // Multiplica apenas o eixo X pelo valor sorteado
        Vector3 randomizedRecoil = new Vector3(
            (baseVisualRecoil.x / 100) * randomSignX,
            (baseVisualRecoil.y / 100) * randomSignY,
            -baseVisualRecoil.z / 10
        );

        if (!isAiming)
        {
            return randomizedRecoil;
        }

        // Se estiver mirando, o recuo visual é reduzido pela metade
        return randomizedRecoil / 2f;
    }

    /// <summary>
    /// Calcula a rotação local aplicada ao modelo da arma (wobble/stability).
    /// </summary>
    public static Quaternion CalculateWeaponRotation(Quaternion currentRotation, float stability, bool isAiming)
    {
        if (isAiming)
        {
            return new Quaternion(
                currentRotation.x + -stability / 500f,
                currentRotation.y + stability / 500f,
                currentRotation.z + -stability / 500f,
                currentRotation.w
            );
        }

        return new Quaternion(
            currentRotation.x + Random.Range(-0.02f, 0.02f),
            currentRotation.y + Random.Range(-0.02f, 0.02f),
            currentRotation.z + Random.Range(-0.02f, 0.02f),
            currentRotation.w
        );
    }

    /// <summary>
    /// Calcula a rotação aleatória no eixo Z da câmera (efeito de trepidação lateral).
    /// </summary>
    public static float CalculateCameraZRoll(float horizontalMedia, float verticalMedia)
    {
        // Calcula o limite do recuo Z somando as médias e multiplicando por 2
        float range = (horizontalMedia + verticalMedia) * 2f;
        return Random.Range(-range, range);
    }

    [System.Serializable]
    public struct RecoilPattern
    {
        [Range(MIN_RECOIL_VALUE, MAX_RECOIL_VALUE)] public float verticalRecoil;
        [Range(MIN_RECOIL_VALUE, MAX_RECOIL_VALUE)] public float horizontalRecoil;
    }
}