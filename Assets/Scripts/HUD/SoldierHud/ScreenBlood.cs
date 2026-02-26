using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScreenBlood : MonoBehaviour
{
    [SerializeField] private Image screenBloodImage;
    [SerializeField] private float fadeInSpeed = 3f;
    [SerializeField] private float fadeOutSpeed = 1f;

    private Coroutine currentRoutine;

    void Awake()
    {
        // Garantir que a imagem comece transparente
        if (screenBloodImage != null)
        {
            SetImageAlpha(0f);
        }
    }

    public void TriggerBlood()
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        if (screenBloodImage.gameObject.activeSelf) currentRoutine = StartCoroutine(BloodRoutine());
    }

    private IEnumerator BloodRoutine()
    {
        // Fade in
        while (screenBloodImage.color.a < 1f)
        {
            float newAlpha = screenBloodImage.color.a + Time.deltaTime * fadeInSpeed;
            SetImageAlpha(newAlpha);
            yield return null;
        }

        SetImageAlpha(1f);

        // Hold for 3 seconds
        yield return new WaitForSeconds(3f);

        // Fade out
        while (screenBloodImage.color.a > 0f)
        {
            float newAlpha = screenBloodImage.color.a - Time.deltaTime * fadeOutSpeed;
            SetImageAlpha(newAlpha);
            yield return null;
        }

        SetImageAlpha(0f);
        currentRoutine = null;
    }

    private void SetImageAlpha(float alpha)
    {
        // Clamp o valor entre 0 e 1
        alpha = Mathf.Clamp01(alpha);

        // Atualiza apenas o alpha mantendo as outras cores
        Color newColor = screenBloodImage.color;
        newColor.a = alpha;
        screenBloodImage.color = newColor;
    }
}