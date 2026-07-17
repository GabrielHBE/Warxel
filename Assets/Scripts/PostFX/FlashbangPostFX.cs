using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class FlashbangPostFX : PostFX
{
    private ColorAdjustments colorAdjustments;

    private float defaultExposure;

    private const float timeToTurnOn = 0.1f;
    private const float timeToTurnOff = 2f;

    protected override void InitializeVolume()
    {
        if (volume != null && volume.profile != null)
        {
            if (volume.profile.TryGet(out colorAdjustments))
                defaultExposure = colorAdjustments.postExposure.value;

            // Garante que iniciem desligados
            ApplyEffectMultiplier(0f);
            ToggleComponents(false);
        }
    }

    public override void SetActive(bool active)
    {
        // Interrompe qualquer transição que esteja acontecendo no momento
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        if (active)
        {
            // Ativa os componentes imediatamente para começarmos a ver o efeito
            ToggleComponents(true);
        }

        transitionCoroutine = StartCoroutine(TransitionRoutine(active));
    }

    private IEnumerator TransitionRoutine(bool turningOn)
    {
        float timer = 0f;

        if (turningOn)
        {
            float startMultiplier = currentMultiplier;
            while (timer < timeToTurnOn)
            {
                timer += Time.deltaTime;
                currentMultiplier = Mathf.Lerp(startMultiplier, 2f, timer / timeToTurnOn);
                ApplyEffectMultiplier(currentMultiplier);
                yield return null;
            }

            currentMultiplier = 1.0f;
            ApplyEffectMultiplier(currentMultiplier);
        }
        else
        {
            // Fase 3: Diminui gradualmente até 0%
            float startMultiplier = currentMultiplier;
            while (timer < timeToTurnOff)
            {
                timer += Time.deltaTime;
                currentMultiplier = Mathf.Lerp(startMultiplier, 0f, timer / timeToTurnOff);
                ApplyEffectMultiplier(currentMultiplier);
                yield return null;
            }

            currentMultiplier = 0f;
            ApplyEffectMultiplier(currentMultiplier);

            // Desativa os componentes no final para economizar processamento
            ToggleComponents(false);
        }
    }

    private void ApplyEffectMultiplier(float multiplier)
    {
        if (colorAdjustments != null) colorAdjustments.postExposure.value = defaultExposure * multiplier;
    }

    private void ToggleComponents(bool state)
    {
        if (colorAdjustments != null) colorAdjustments.active = state;
    }
}
