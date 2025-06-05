using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostProcessingController : MonoBehaviour
{
    [Header("Post-Processing References")]
    public Volume globalVolume;
    public Light directionalLight;
    
    [Header("Level Settings")]
    [Tooltip("Bloom intensity for each level - increases with depth")]
    public float[] levelBloomIntensity = { 0.2f, 0.7f, 1.5f };
    
    [Tooltip("Color Adjustments Exposure for each level - more negative = darker")]
    public float[] levelExposure = { 0.3f, -0.8f, -2.0f };
    
    [Tooltip("Shadows adjustment for each level - more negative = darker shadows")]
    public float[] levelShadows = { 0.0f, -0.3f, -0.6f };
    
    [Tooltip("Midtones adjustment for each level - more negative = darker midtones")]
    public float[] levelMidtones = { 0.0f, -0.4f, -0.8f };
    
    [Tooltip("Color Adjustments Saturation for each level - more negative = desaturated")]
    public float[] levelSaturation = { 0.1f, -0.4f, -0.7f };
    
    [Tooltip("Directional light intensity for each level - decreases with depth")]
    public float[] levelLightIntensity = { 1.2f, 0.4f, 0.1f };
    
    [Tooltip("Time to fade effects when changing levels")]
    public float transitionDuration = 2.0f;
    
    [Header("Death Effect Settings")]
    [Tooltip("Exposure value for death effect (very negative = complete darkness)")]
    public float deathExposure = -5.0f;
    
    [Tooltip("Light intensity for death effect")]
    public float deathLightIntensity = 0.0f;
    
    [Tooltip("Bloom intensity for death effect")]
    public float deathBloomIntensity = 0.0f;
    
    [Tooltip("Saturation for death effect (-1 = completely desaturated)")]
    public float deathSaturation = -1.0f;
    
    [Tooltip("Shadows for death effect")]
    public float deathShadows = -1.0f;
    
    [Tooltip("Midtones for death effect")]
    public float deathMidtones = -1.5f;
    
    [Tooltip("Duration for death transition")]
    public float deathTransitionDuration = 3.0f;
    
    // Post-processing effect references
    private Bloom bloomEffect;
    private ColorAdjustments colorAdjustments;
    private ShadowsMidtonesHighlights shadowsMidtonesEffect;

    void Start()
    {
        InitializePostProcessingEffects();
    }

    private void InitializePostProcessingEffects()
    {
        if (globalVolume != null && globalVolume.profile != null)
        {
            globalVolume.profile.TryGet<Bloom>(out bloomEffect);
            globalVolume.profile.TryGet<ColorAdjustments>(out colorAdjustments);
            globalVolume.profile.TryGet<ShadowsMidtonesHighlights>(out shadowsMidtonesEffect);
        }
    }

    public void SetLevelEffects(int level)
    {
        if (level < 0 || level >= levelBloomIntensity.Length)
        {
            Debug.LogWarning($"Invalid level {level}! Valid range: 0-{levelBloomIntensity.Length - 1}");
            return;
        }

        SetLevelEffects(level, transitionDuration);
    }

    public void SetLevelEffects(int level, float duration)
    {
        if (level < 0 || level >= levelBloomIntensity.Length)
        {
            Debug.LogWarning($"Invalid level {level}! Valid range: 0-{levelBloomIntensity.Length - 1}");
            return;
        }

        // Start all transitions simultaneously
        StartCoroutine(TransitionBloomIntensity(levelBloomIntensity[level], duration));
        StartCoroutine(TransitionExposure(levelExposure[level], duration));
        StartCoroutine(TransitionShadows(levelShadows[level], duration));
        StartCoroutine(TransitionMidtones(levelMidtones[level], duration));
        StartCoroutine(TransitionSaturation(levelSaturation[level], duration));
        StartCoroutine(TransitionLightIntensity(levelLightIntensity[level], duration));
    }

    public void InitializeLevelEffects(int level)
    {
        if (level < 0 || level >= levelBloomIntensity.Length) return;

        // Set effects instantly without transition
        if (bloomEffect != null)
            bloomEffect.intensity.value = levelBloomIntensity[level];
        
        if (colorAdjustments != null)
        {
            colorAdjustments.postExposure.value = levelExposure[level];
            colorAdjustments.saturation.value = levelSaturation[level];
        }
        
        if (shadowsMidtonesEffect != null)
        {
            shadowsMidtonesEffect.shadows.value = new Vector4(levelShadows[level], levelShadows[level], levelShadows[level], 0);
            shadowsMidtonesEffect.midtones.value = new Vector4(levelMidtones[level], levelMidtones[level], levelMidtones[level], 0);
        }
        
        if (directionalLight != null)
            directionalLight.intensity = levelLightIntensity[level];
    }

    public void TriggerDeathEffect()
    {
        TriggerDeathEffect(deathTransitionDuration);
    }

    public void TriggerDeathEffect(float duration)
    {
        // Transition to complete darkness
        StartCoroutine(TransitionBloomIntensity(deathBloomIntensity, duration));
        StartCoroutine(TransitionExposure(deathExposure, duration));
        StartCoroutine(TransitionShadows(deathShadows, duration));
        StartCoroutine(TransitionMidtones(deathMidtones, duration));
        StartCoroutine(TransitionSaturation(deathSaturation, duration));
        StartCoroutine(TransitionLightIntensity(deathLightIntensity, duration));
    }

    public void InstantDeathEffect()
    {
        // Set death effects instantly without transition
        if (bloomEffect != null)
            bloomEffect.intensity.value = deathBloomIntensity;
        
        if (colorAdjustments != null)
        {
            colorAdjustments.postExposure.value = deathExposure;
            colorAdjustments.saturation.value = deathSaturation;
        }
        
        if (shadowsMidtonesEffect != null)
        {
            shadowsMidtonesEffect.shadows.value = new Vector4(deathShadows, deathShadows, deathShadows, 0);
            shadowsMidtonesEffect.midtones.value = new Vector4(deathMidtones, deathMidtones, deathMidtones, 0);
        }
        
        if (directionalLight != null)
            directionalLight.intensity = deathLightIntensity;
    }

    #region Individual Effect Controls
    public void SetBloomIntensity(float intensity, float duration = -1f)
    {
        float d = duration < 0 ? transitionDuration : duration;
        StartCoroutine(TransitionBloomIntensity(intensity, d));
    }

    public void SetExposure(float exposure, float duration = -1f)
    {
        float d = duration < 0 ? transitionDuration : duration;
        StartCoroutine(TransitionExposure(exposure, d));
    }

    public void SetShadows(float shadows, float duration = -1f)
    {
        float d = duration < 0 ? transitionDuration : duration;
        StartCoroutine(TransitionShadows(shadows, d));
    }

    public void SetMidtones(float midtones, float duration = -1f)
    {
        float d = duration < 0 ? transitionDuration : duration;
        StartCoroutine(TransitionMidtones(midtones, d));
    }

    public void SetSaturation(float saturation, float duration = -1f)
    {
        float d = duration < 0 ? transitionDuration : duration;
        StartCoroutine(TransitionSaturation(saturation, d));
    }

    public void SetLightIntensity(float intensity, float duration = -1f)
    {
        float d = duration < 0 ? transitionDuration : duration;
        StartCoroutine(TransitionLightIntensity(intensity, d));
    }
    #endregion

    #region Transition Coroutines
    private IEnumerator TransitionBloomIntensity(float targetIntensity, float duration)
    {
        if (bloomEffect == null)
        {
            Debug.LogWarning("Bloom effect not found in Volume Profile!");
            yield break;
        }

        float startIntensity = bloomEffect.intensity.value;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            bloomEffect.intensity.value = Mathf.Lerp(startIntensity, targetIntensity, Mathf.SmoothStep(0, 1, t));
            
            yield return null;
        }

        bloomEffect.intensity.value = targetIntensity;
    }

    private IEnumerator TransitionExposure(float targetExposure, float duration)
    {
        if (colorAdjustments == null)
        {
            Debug.LogWarning("Color Adjustments effect not found in Volume Profile!");
            yield break;
        }

        float startExposure = colorAdjustments.postExposure.value;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            colorAdjustments.postExposure.value = Mathf.Lerp(startExposure, targetExposure, Mathf.SmoothStep(0, 1, t));
            
            yield return null;
        }

        colorAdjustments.postExposure.value = targetExposure;
    }

    private IEnumerator TransitionShadows(float targetShadows, float duration)
    {
        if (shadowsMidtonesEffect == null)
        {
            Debug.LogWarning("Shadows Midtones Highlights effect not found in Volume Profile!");
            yield break;
        }

        float startShadows = shadowsMidtonesEffect.shadows.value.x;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            float currentShadow = Mathf.Lerp(startShadows, targetShadows, Mathf.SmoothStep(0, 1, t));
            shadowsMidtonesEffect.shadows.value = new Vector4(currentShadow, currentShadow, currentShadow, 0);
            
            yield return null;
        }

        shadowsMidtonesEffect.shadows.value = new Vector4(targetShadows, targetShadows, targetShadows, 0);
    }

    private IEnumerator TransitionMidtones(float targetMidtones, float duration)
    {
        if (shadowsMidtonesEffect == null)
        {
            Debug.LogWarning("Shadows Midtones Highlights effect not found in Volume Profile!");
            yield break;
        }

        float startMidtones = shadowsMidtonesEffect.midtones.value.x;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            float currentMidtone = Mathf.Lerp(startMidtones, targetMidtones, Mathf.SmoothStep(0, 1, t));
            shadowsMidtonesEffect.midtones.value = new Vector4(currentMidtone, currentMidtone, currentMidtone, 0);
            
            yield return null;
        }

        shadowsMidtonesEffect.midtones.value = new Vector4(targetMidtones, targetMidtones, targetMidtones, 0);
    }

    private IEnumerator TransitionSaturation(float targetSaturation, float duration)
    {
        if (colorAdjustments == null)
        {
            Debug.LogWarning("Color Adjustments effect not found in Volume Profile!");
            yield break;
        }

        float startSaturation = colorAdjustments.saturation.value;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            colorAdjustments.saturation.value = Mathf.Lerp(startSaturation, targetSaturation, Mathf.SmoothStep(0, 1, t));
            
            yield return null;
        }

        colorAdjustments.saturation.value = targetSaturation;
    }

    private IEnumerator TransitionLightIntensity(float targetIntensity, float duration)
    {
        if (directionalLight == null)
        {
            Debug.LogWarning("Directional Light is not assigned!");
            yield break;
        }

        float startIntensity = directionalLight.intensity;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            directionalLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, Mathf.SmoothStep(0, 1, t));
            
            yield return null;
        }

        directionalLight.intensity = targetIntensity;
    }
    #endregion
}