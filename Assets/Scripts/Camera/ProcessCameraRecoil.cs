using UnityEngine;

public class ProcessCameraRecoil : MonoBehaviour
{
    [Header("Recoil Settings")]
    [SerializeField] private float applyRecoilSpeed = 0.05f;
    [SerializeField] private float resetRecoilSpeed = 4f;

    // Recoil Vertical (Eixo X)
    private float recoilVerticalTarget;
    private float recoilVerticalCurrent;
    private float recoilVerticalVelocity;

    // Recoil Horizontal (Eixo Y)
    private float horizontalRecoilTarget;
    private float horizontalRecoilCurrent;
    private float horizontalRecoilVelocity;

    // Recoil Roll (Eixo Z)
    private float targetRecoilZ;
    private float currentRecoilZ;
    private float recoilZVelocity;

    /// <summary>
    /// Adiciona impulso de recoil vertical e horizontal.
    /// </summary>
    public void ApplyRecoil(float verticalRecoil, float horizontalRecoil)
    {
        recoilVerticalTarget += verticalRecoil;
        horizontalRecoilTarget += horizontalRecoil;

        // Calcula a trepidação/roll no eixo Z usando o helper da classe Recoil
        float newRecoilZ = Recoil.CalculateCameraZRoll(horizontalRecoil, verticalRecoil);
        targetRecoilZ += newRecoilZ;
    }

    /// <summary>
    /// Processa a suavização do recoil e retorna os deltas a serem somados à rotação do PlayerController.
    /// </summary>
    public void ProcessRecoil(out float horizontalRecoil, out float verticalRecoil, out float recoilZ)
    {
        UpdateRecoilReset();

        // Interpolação suave dos valores atuais em direção aos alvos
        horizontalRecoilCurrent = Mathf.SmoothDamp(horizontalRecoilCurrent, horizontalRecoilTarget, ref horizontalRecoilVelocity, applyRecoilSpeed);
        recoilVerticalCurrent = Mathf.SmoothDamp(recoilVerticalCurrent, recoilVerticalTarget, ref recoilVerticalVelocity, applyRecoilSpeed);
        currentRecoilZ = Mathf.SmoothDamp(currentRecoilZ, targetRecoilZ, ref recoilZVelocity, applyRecoilSpeed);

        horizontalRecoil = horizontalRecoilCurrent;
        verticalRecoil = recoilVerticalCurrent;
        recoilZ = currentRecoilZ;

        // Reseta os alvos acumulados neste passo (comportamento original)
        horizontalRecoilTarget = 0f;
        recoilVerticalTarget = 0f;
    }

    private void UpdateRecoilReset()
    {
        // Retorna o recoil do eixo Z suavemente a zero
        if (Mathf.Abs(targetRecoilZ) > 0.01f)
        {
            targetRecoilZ = Mathf.Lerp(targetRecoilZ, 0f, resetRecoilSpeed * Time.deltaTime);
            if (Mathf.Abs(targetRecoilZ) < 0.001f) targetRecoilZ = 0f;
        }
    }

    public void SetApplyRecoilSpeed(float speed) => applyRecoilSpeed = speed;
}