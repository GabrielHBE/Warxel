
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
    /// Calcula a rotação aleatória no eixo Z da câmera (efeito de trepidação lateral).
    /// </summary>
    public static float CalculateCameraZRoll(float horizontal, float vertical)
    {
        return ((horizontal + vertical) / 5) * (Random.value > 0.5f ? 1f : -1f);
    }

    [System.Serializable]
    public struct RecoilPattern
    {
        [Range(MIN_RECOIL_VALUE, MAX_RECOIL_VALUE)] public float verticalRecoil;
        [Range(MIN_RECOIL_VALUE, MAX_RECOIL_VALUE)] public float horizontalRecoil;
    }

    [System.Serializable]
    public class RecoilValues
    {
        public bool manualCalculateRecoil;
        public float resetRecoilSpeed;
        public float applyRecoilSpeed;
        public Vector3 visual_recoil;
        [Range(MIN_FIRTSHOTINCREASER_VALUE, MAX_FIRTSHOTINCREASER_VALUE)]
        public float firstShootRecoilMultiplier = 1;
        public RecoilPattern[] recoilPattern = new RecoilPattern[1];
        [HideInInspector] public float horizontalRecoilMedia;
        [HideInInspector] public float verticalRecoilMedia;

        public void CalculateRecoilSpeed(float interval)
        {
            if (manualCalculateRecoil) return;

            resetRecoilSpeed = interval / 2;
            applyRecoilSpeed = interval / 2;

        }

        float horizonalMedia = 0;
        float verticalMedia = 0;
        public void CalculateRecoilMedia()
        {

            for (int i = 0; i < recoilPattern.Length; i++)
            {
                horizonalMedia = recoilPattern[i].horizontalRecoil;
                verticalMedia = recoilPattern[i].verticalRecoil;
            }
            verticalRecoilMedia = verticalMedia / recoilPattern.Length;
            horizontalRecoilMedia = horizonalMedia / recoilPattern.Length;
        }
    }
}