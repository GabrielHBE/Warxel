using System.Collections;
using FishNet.Object;
using UnityEngine;
using UnityEngine.Rendering;
using static VolumetricClouds;

public class CloudManager : MonoBehaviour
{
    [SerializeField] private Volume volume;
    private VolumetricClouds clouds;
    
    public void Initialize()
    {
        if (volume == null)
        {
            Debug.LogError("Volume is not assigned to CloudManager!");
            return;
        }
        
        if (volume.profile.TryGet<VolumetricClouds>(out var volumetricClouds))
            clouds = volumetricClouds;
        else
            clouds = volume.profile.Add<VolumetricClouds>();
    }

    public CloudPreset GetCurrentPreset()
    {
        if (clouds == null) return new CloudPreset();
        
        return new CloudPreset
        {
            densityMultiplier = clouds.densityMultiplier.value,
            shapeFactor = clouds.shapeFactor.value,
            erosionFactor = clouds.erosionFactor.value,
            bottomAltitude = clouds.bottomAltitude.value,
            altitudeRange = clouds.altitudeRange.value,
            microErosionFactor = clouds.microErosionFactor.value,
            globalSpeed = clouds.globalSpeed.value,
            globalOrientation = clouds.globalOrientation.value,
            sunLightDimmer = clouds.sunLightDimmer.value,
            altitudeDistortion = clouds.altitudeDistortion.value,
            verticalShapeWindSpeed = clouds.verticalShapeWindSpeed.value,
            verticalErosionWindSpeed = clouds.verticalErosionWindSpeed.value,
            erosionOcclusion = clouds.erosionOcclusion.value,
            scatteringTint = clouds.scatteringTint.value,
            powderEffectIntensity = clouds.powderEffectIntensity.value,
            multiScattering = clouds.multiScattering.value,
            shadowDistance = clouds.shadowDistance.value,
            shadowOpacity = clouds.shadowOpacity.value,
        };
    }
    
    
    public void TransitionToPreset(CloudPreset target, float duration, System.Action onComplete = null)
    {
        if (clouds == null)
        {
            onComplete?.Invoke();
            return;
        }
        
        CloudPreset startPreset = GetCurrentPreset();
        StartCoroutine(TransitionRoutine(startPreset, target, duration, onComplete));
    }
    
    
    private IEnumerator TransitionRoutine(CloudPreset start, CloudPreset target, float duration, System.Action onComplete)
    {
        float elapsed = 0;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            ApplyLerpedPreset(start, target, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        ApplyPreset(target);
        onComplete?.Invoke();
    }

    private void ApplyLerpedPreset(CloudPreset start, CloudPreset target, float t)
    {
        if (clouds == null) return;
        
        clouds.cloudPreset = CloudPresets.Custom;
        
        // Propriedades básicas
        clouds.densityMultiplier.value = Mathf.Lerp(start.densityMultiplier, target.densityMultiplier, t);
        clouds.shapeFactor.value = Mathf.Lerp(start.shapeFactor, target.shapeFactor, t);
        clouds.erosionFactor.value = Mathf.Lerp(start.erosionFactor, target.erosionFactor, t);
        clouds.bottomAltitude.value = Mathf.Lerp(start.bottomAltitude, target.bottomAltitude, t);
        clouds.altitudeRange.value = Mathf.Lerp(start.altitudeRange, target.altitudeRange, t);
        clouds.microErosionFactor.value = Mathf.Lerp(start.microErosionFactor, target.microErosionFactor, t);
        clouds.globalSpeed.value = Mathf.Lerp(start.globalSpeed, target.globalSpeed, t);
        clouds.globalOrientation.value = Mathf.Lerp(start.globalOrientation, target.globalOrientation, t);
        clouds.sunLightDimmer.value = Mathf.Lerp(start.sunLightDimmer, target.sunLightDimmer, t);
        
        // Novas propriedades
        clouds.altitudeDistortion.value = Mathf.Lerp(start.altitudeDistortion, target.altitudeDistortion, t);
        clouds.verticalShapeWindSpeed.value = Mathf.Lerp(start.verticalShapeWindSpeed, target.verticalShapeWindSpeed, t);
        clouds.verticalErosionWindSpeed.value = Mathf.Lerp(start.verticalErosionWindSpeed, target.verticalErosionWindSpeed, t);
        clouds.erosionOcclusion.value = Mathf.Lerp(start.erosionOcclusion, target.erosionOcclusion, t);
        clouds.scatteringTint.value = Color.Lerp(start.scatteringTint, target.scatteringTint, t);
        clouds.powderEffectIntensity.value = Mathf.Lerp(start.powderEffectIntensity, target.powderEffectIntensity, t);
        clouds.multiScattering.value = Mathf.Lerp(start.multiScattering, target.multiScattering, t);

        // Distância e opacidade da sombra
        clouds.shadowDistance.value = Mathf.Lerp(start.shadowDistance, target.shadowDistance, t);
        clouds.shadowOpacity.value = Mathf.Lerp(start.shadowOpacity, target.shadowOpacity, t);

    }
    
    private void ApplyPreset(CloudPreset preset)
    {
        if (clouds == null) return;
        
        clouds.cloudPreset = CloudPresets.Custom;
        
        // Propriedades básicas
        clouds.densityMultiplier.value = preset.densityMultiplier;
        clouds.shapeFactor.value = preset.shapeFactor;
        clouds.erosionFactor.value = preset.erosionFactor;
        clouds.bottomAltitude.value = preset.bottomAltitude;
        clouds.altitudeRange.value = preset.altitudeRange;
        clouds.microErosionFactor.value = preset.microErosionFactor;
        clouds.globalSpeed.value = preset.globalSpeed;
        clouds.globalOrientation.value = preset.globalOrientation;
        clouds.sunLightDimmer.value = preset.sunLightDimmer;
        
        // Novas propriedades
        clouds.altitudeDistortion.value = preset.altitudeDistortion;
        clouds.verticalShapeWindSpeed.value = preset.verticalShapeWindSpeed;
        clouds.verticalErosionWindSpeed.value = preset.verticalErosionWindSpeed;
        clouds.erosionOcclusion.value = preset.erosionOcclusion;
        clouds.scatteringTint.value = preset.scatteringTint;
        clouds.powderEffectIntensity.value = preset.powderEffectIntensity;
        clouds.multiScattering.value = preset.multiScattering;
        clouds.shadowDistance.value = preset.shadowDistance;
        clouds.shadowOpacity.value = preset.shadowOpacity;

    }
}

// Definição completa da classe CloudPreset
[System.Serializable]
public class CloudPreset
{

    // Shape Properties
    [Range(0, 1)]
    public float densityMultiplier = 0.4f;
    [Range(0, 1)]
    public float shapeFactor = 0.9f;
    [Range(0, 1)]
    public float erosionFactor = 0.8f;
    [Range(0, 1)]
    public float microErosionFactor = 0.5f;
    public float bottomAltitude = 1200.0f;
    public float altitudeRange = 2000.0f;
    
    // Wind Properties
    public float globalSpeed = 0.0f;
    public float globalOrientation = 0.0f;
    public float altitudeDistortion = 0.25f;
    public float verticalShapeWindSpeed = 0.0f;
    public float verticalErosionWindSpeed = 0.0f;
    
    // Lighting Properties
    [Range(0, 2)]
    public float sunLightDimmer = 1.0f;
    [Range(0, 1)]
    public float erosionOcclusion = 0.1f;
    public Color scatteringTint = new Color(0.0f, 0.0f, 0.0f, 1.0f);
    [Range(0, 1)]
    public float powderEffectIntensity = 0.25f;
    [Range(0, 1)]
    public float multiScattering = 0.5f;
    
    // Shadow Properties
    public float shadowDistance = 8000.0f;
    public float shadowOpacity = 1.0f;

}