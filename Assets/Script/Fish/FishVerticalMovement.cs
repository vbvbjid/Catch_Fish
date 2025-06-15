using UnityEngine;

public class FishVerticalMovement : MonoBehaviour
{
    [Header("Vertical Movement Settings")]
    [Tooltip("Enable/disable all vertical movement functionality")]
    public bool enableVerticalMovement = true;
    [Tooltip("Base height will be automatically set from the externally assigned orbitCenter.y")]
    [SerializeField] private float baseHeight = 0f;

    [Header("Wave Movement Settings")]
    [Tooltip("Enable wave-like vertical movement")]
    public bool enableWaveMovement = true;
    [Tooltip("Fixed amplitude for vertical oscillation")]
    public float fixedAmplitude = 2f;
    [Tooltip("Fixed speed for vertical oscillation")]
    public float fixedSpeed = 1f;

    [Header("Randomization Settings")]
    [Tooltip("Randomize amplitude on each wave direction change")]
    public bool randomizeAmplitude = false;
    [Tooltip("Minimum amplitude when randomizing")]
    public float minAmplitude = 0.5f;
    [Tooltip("Maximum amplitude when randomizing")]
    public float maxAmplitude = 3f;
    
    [Tooltip("Randomize speed on each wave direction change")]
    public bool randomizeSpeed = false;
    [Tooltip("Minimum speed when randomizing")]
    public float minSpeed = 0.5f;
    [Tooltip("Maximum speed when randomizing")]
    public float maxSpeed = 2f;

    [Header("Legacy Height Change Settings (Deprecated)")]
    [Tooltip("Maximum height variation above and below base height")]
    public float heightVariationRange = 2f;
    [Tooltip("How smoothly the fish transitions between heights (lower = smoother)")]
    public float heightChangeRate = 0.5f;
    [Tooltip("How often height changes occur (higher = more frequent)")]
    public float heightChangeFrequency = 0.3f;
    [Tooltip("Minimum time between dramatic height changes")]
    public float minHeightChangeInterval = 2f;

    [Header("Debug Settings")]
    [Tooltip("Show vertical movement debug information")]
    public bool debugVerticalMovement = false;

    // Wave movement variables
    private float waveTime = 0f;
    private float currentAmplitude;
    private float currentSpeed;
    private float lastWaveDirection = 1f; // Track wave direction to detect changes
    private bool hasChangedDirection = false;

    // Legacy variables (kept for compatibility)
    private float currentHeight;
    private float targetHeight;
    private float lastHeightChangeTime = 0f;

    public float CurrentHeight => currentHeight;
    public float BaseHeight => baseHeight;

    public void Initialize(float initialBaseHeight)
    {
        baseHeight = initialBaseHeight;

        if (!enableVerticalMovement)
        {
            currentHeight = baseHeight;
            targetHeight = baseHeight;
            waveTime = 0f;
            return;
        }

        // Initialize wave parameters
        currentAmplitude = randomizeAmplitude ? Random.Range(minAmplitude, maxAmplitude) : fixedAmplitude;
        currentSpeed = randomizeSpeed ? Random.Range(minSpeed, maxSpeed) : fixedSpeed;

        // Set initial height to base height
        currentHeight = baseHeight;
        targetHeight = baseHeight;

        // Initialize wave time with random offset to desynchronize multiple fish
        waveTime = Random.Range(0f, 2f * Mathf.PI);
        lastWaveDirection = Mathf.Sign(Mathf.Sin(waveTime));

        if (debugVerticalMovement)
            Debug.Log($"Initialized vertical movement - Base: {baseHeight}, Amplitude: {currentAmplitude:F2}, Speed: {currentSpeed:F2}", this);
    }

    public void UpdateVerticalMovement(float deltaTime)
    {
        // Skip all vertical movement if disabled
        if (!enableVerticalMovement)
        {
            currentHeight = baseHeight;
            return;
        }

        if (enableWaveMovement)
        {
            // Update wave time
            waveTime += deltaTime * currentSpeed;

            // Check for direction change
            float currentWaveDirection = Mathf.Sign(Mathf.Sin(waveTime));
            if (currentWaveDirection != lastWaveDirection && !hasChangedDirection)
            {
                OnWaveDirectionChange();
                hasChangedDirection = true;
            }
            else if (currentWaveDirection == lastWaveDirection)
            {
                hasChangedDirection = false;
            }
            lastWaveDirection = currentWaveDirection;

            // Calculate wave offset
            float waveOffset = Mathf.Sin(waveTime) * currentAmplitude;
            currentHeight = baseHeight + waveOffset;
        }
        else
        {
            // Fallback to legacy smooth interpolation behavior
            currentHeight = Mathf.Lerp(currentHeight, targetHeight, deltaTime * heightChangeRate);

            // Legacy random height changes
            if (Time.time - lastHeightChangeTime > minHeightChangeInterval)
            {
                if (Random.Range(0f, 1f) < deltaTime * heightChangeFrequency)
                {
                    ChangeTargetHeight();
                    lastHeightChangeTime = Time.time;
                }
            }
        }

        if (debugVerticalMovement && Time.frameCount % 60 == 0) // Log every 60 frames to avoid spam
        {
            if (enableWaveMovement)
            {
                Debug.Log($"Wave - Height: {currentHeight:F2}, Amplitude: {currentAmplitude:F2}, Speed: {currentSpeed:F2}", this);
            }
            else
            {
                Debug.Log($"Legacy - Current: {currentHeight:F2}, Target: {targetHeight:F2}", this);
            }
        }
    }

    private void OnWaveDirectionChange()
    {
        // Randomize amplitude if enabled
        if (randomizeAmplitude)
        {
            currentAmplitude = Random.Range(minAmplitude, maxAmplitude);
        }

        // Randomize speed if enabled
        if (randomizeSpeed)
        {
            currentSpeed = Random.Range(minSpeed, maxSpeed);
        }

        if (debugVerticalMovement)
        {
            Debug.Log($"Wave direction changed - New Amplitude: {currentAmplitude:F2}, New Speed: {currentSpeed:F2}", this);
        }
    }

    public void SetBaseHeight(float newBaseHeight)
    {
        baseHeight = newBaseHeight;

        // If vertical movement is disabled, keep current height at base height
        if (!enableVerticalMovement)
        {
            currentHeight = baseHeight;
            targetHeight = baseHeight;
        }

        if (debugVerticalMovement)
            Debug.Log($"Base height updated to: {baseHeight}", this);
    }

    public void ResetVerticalMovement()
    {
        Initialize(baseHeight);
    }

    // Legacy method (kept for compatibility)
    private void ChangeTargetHeight()
    {
        if (!enableVerticalMovement)
        {
            targetHeight = baseHeight;
            return;
        }

        float newTargetHeight;

        // 30% chance for dramatic height change, 70% for gradual
        if (Random.Range(0f, 1f) < 0.3f)
        {
            // Dramatic height change
            newTargetHeight = baseHeight + Random.Range(-heightVariationRange, heightVariationRange);
        }
        else
        {
            // Gradual height change
            float currentOffset = targetHeight - baseHeight;
            float maxChange = heightVariationRange * 0.4f;
            float heightChange = Random.Range(-maxChange, maxChange);
            newTargetHeight = baseHeight + Mathf.Clamp(currentOffset + heightChange, -heightVariationRange, heightVariationRange);
        }

        targetHeight = newTargetHeight;

        if (debugVerticalMovement)
            Debug.Log($"Changed target height to: {targetHeight:F2} (Base: {baseHeight})", this);
    }
}