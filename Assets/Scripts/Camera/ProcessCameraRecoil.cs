using UnityEngine;

public class ProcessCameraRecoil : MonoBehaviour
{
    [Header("Recoil Settings")]
    private float applyRecoilSpeed = 0.05f;
    private float resetRecoilSpeed = 4f;

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

    public void ResetState()
    {
        recoilVerticalTarget = 0f;
        recoilVerticalCurrent = 0f;
        recoilVerticalVelocity = 0f;

        horizontalRecoilTarget = 0f;
        horizontalRecoilCurrent = 0f;
        horizontalRecoilVelocity = 0f;

        targetRecoilZ = 0f;
        currentRecoilZ = 0f;
        recoilZVelocity = 0f;
    }

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

    /*
    Auto - reset recoil function
    private void UpdateRecoilReset()
    {
        // Retorna o recoil horizontal e vertical suavemente a zero
        recoilVerticalTarget = Mathf.Lerp(recoilVerticalTarget, 0f, resetRecoilSpeed * Time.deltaTime);
        horizontalRecoilTarget = Mathf.Lerp(horizontalRecoilTarget, 0f, resetRecoilSpeed * Time.deltaTime);
    
        // Retorna o recoil do eixo Z suavemente a zero
        if (Mathf.Abs(targetRecoilZ) > 0.01f)
        {
            targetRecoilZ = Mathf.Lerp(targetRecoilZ, 0f, resetRecoilSpeed * Time.deltaTime);
            if (Mathf.Abs(targetRecoilZ) < 0.001f) targetRecoilZ = 0f;
        }
    }
    */

    public void SetApplyRecoilSpeed(float speed) => applyRecoilSpeed = speed;
}
