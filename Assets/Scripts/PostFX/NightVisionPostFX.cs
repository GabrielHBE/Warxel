using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class NightVisionPostFX : PostFX
{
    private Vignette vignette;
    private ColorAdjustments colorAdjustments;
    private FilmGrain filmGrain;

    // Valores originais configurados no Inspector
    private float defaultVignette;
    private float defaultGrain;
    private float defaultExposure;

    // Tempos de transição
    private const float timeToPeak = 0.1f;   // Tempo para atingir a força máxima (120%)
    private const float timeToSettle = 0.25f; // Tempo para estabilizar (120% -> 100%)
    private const float timeToTurnOff = 0.1f; // Tempo para apagar gradualmente

    protected override void InitializeVolume()
    {
        if (volume != null && volume.profile != null)
        {
            if (volume.profile.TryGet(out vignette))
                defaultVignette = vignette.intensity.value;

            if (volume.profile.TryGet(out filmGrain))
                defaultGrain = filmGrain.intensity.value;

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
            // Fase 1: Sobe até 120% (Pico/Overshoot)
            float startMultiplier = currentMultiplier;
            while (timer < timeToPeak)
            {
                timer += Time.deltaTime;
                currentMultiplier = Mathf.Lerp(startMultiplier, 2f, timer / timeToPeak);
                ApplyEffectMultiplier(currentMultiplier);
                yield return null;
            }

            // Fase 2: Desce e estabiliza em 100%
            timer = 0f;
            while (timer < timeToSettle)
            {
                timer += Time.deltaTime;
                currentMultiplier = Mathf.Lerp(2f, 1f, timer / timeToSettle);
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
        if (vignette != null) vignette.intensity.value = defaultVignette * multiplier;
        if (filmGrain != null) filmGrain.intensity.value = defaultGrain * multiplier;
        if (colorAdjustments != null) colorAdjustments.postExposure.value = defaultExposure * multiplier;
    }

    private void ToggleComponents(bool state)
    {
        if (filmGrain != null) filmGrain.active = state;
        if (colorAdjustments != null) colorAdjustments.active = state;
        if (vignette != null) vignette.active = state;
    }
}