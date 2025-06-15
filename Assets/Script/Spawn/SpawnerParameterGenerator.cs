using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct FishSpawnParameters
{
    public Vector3 orbitCenter;
    public float orbitRadius;
    public float initialAngle;

    // Base values for identification (before randomization)
    public float baseY;
    public float baseRadius;
    public float baseAngle;

    public override string ToString()
    {
        return $"Base: Y{baseY:F1}_R{baseRadius:F1}_A{baseAngle:F0}° | Final: Center{orbitCenter}, Radius{orbitRadius:F1}, Angle{initialAngle:F0}°";
    }
}

public class SpawnerParameterGenerator : MonoBehaviour
{
    [Header("Y Position Settings")]
    [Tooltip("Minimum Y position offset from spawner")]
    public float minCenterY = 0.4f;
    [Tooltip("Maximum Y position offset from spawner")]
    public float maxCenterY = 2.0f;
    [Tooltip("Minimum separation between Y layers")]
    public float minYSeparation = 0.2f;

    [Header("Radius Settings")]
    [Tooltip("Minimum orbit radius")]
    public float minRadius = 1.0f;
    [Tooltip("Maximum orbit radius")]
    public float maxRadius = 2.0f;
    [Tooltip("Minimum separation between radius rings")]
    public float minRadiusSeparation = 0.3f;

    [Header("Angular Settings")]
    [Tooltip("Minimum separation between fish on same orbit (degrees)")]
    public float minAngleSeparation = 30f;

    [Header("Randomization Settings")]
    [Tooltip("Add random offset to initial angles (clamped to not violate separation)")]
    public bool randomizeAngles = true;
    public float angleRandomRange = 10f; // Will be clamped to maintain separation

    [Tooltip("Add random variation to Y position (clamped to not exceed bounds)")]
    public bool randomizeCenters = true;
    public float centerYRandomRange = 0.05f; // Will be clamped to stay within bounds

    [Tooltip("Add random variation to orbit radii (clamped to not violate separation)")]
    public bool randomizeRadii = true;
    public float radiusRandomRange = 0.1f; // Will be clamped to maintain separation

    [Header("Pool Integration")]
    [Tooltip("Reference to pool manager to get actual fish count for distribution")]
    public SpawnerPoolManager poolManager;

    [Header("Debug")]
    public bool debugParameterGeneration = false;

    // Generated parameter arrays
    private float[] yPositions;
    private float[] radiusValues;
    private float[] angleValues;

    public int TotalPossibleCombinations =>
        (yPositions?.Length ?? 0) *
        (radiusValues?.Length ?? 0) *
        (angleValues?.Length ?? 0);

    public float[] YPositions => yPositions;
    public float[] RadiusValues => radiusValues;
    public float[] AngleValues => angleValues;

    void Start()
    {
        if (poolManager == null)
            poolManager = GetComponent<SpawnerPoolManager>();

        Initialize();
    }

    public void Initialize()
    {
        // Get the actual number of fish that will be spawned
        int targetFishCount = GetTargetFishCount();

        // Calculate optimal distribution
        CalculateOptimalDistribution(targetFishCount, out int yLayers, out int radiusRings, out int anglePoints);

        // Generate positions with proper separation
        GenerateYPositions(yLayers);
        GenerateRadiusValues(radiusRings);
        GenerateAngleValues(anglePoints);

        if (debugParameterGeneration)
        {
            Debug.Log($"Generated distribution for {targetFishCount} fish: " +
                     $"{yLayers} Y layers, {radiusRings} radius rings, {anglePoints} angle points", this);
        }
    }

    private int GetTargetFishCount()
    {
        if (poolManager != null && poolManager.enablePooling)
        {
            return poolManager.fixedPoolSize;
        }

        // Fallback: calculate a reasonable number based on space
        float yRange = maxCenterY - minCenterY;
        float radiusRange = maxRadius - minRadius;

        int maxYLayers = Mathf.Max(1, Mathf.FloorToInt(yRange / minYSeparation) + 1);
        int maxRadiusRings = Mathf.Max(1, Mathf.FloorToInt(radiusRange / minRadiusSeparation) + 1);
        int maxAnglePoints = Mathf.Max(1, Mathf.FloorToInt(360f / minAngleSeparation));

        return maxYLayers * maxRadiusRings * maxAnglePoints;
    }

    private void CalculateOptimalDistribution(int targetFishCount, out int yLayers, out int radiusRings, out int anglePoints)
    {
        // Calculate maximum possible values based on separation constraints
        float yRange = maxCenterY - minCenterY;
        float radiusRange = maxRadius - minRadius;

        int maxYLayers = Mathf.Max(1, Mathf.FloorToInt(yRange / minYSeparation) + 1);
        int maxRadiusRings = Mathf.Max(1, Mathf.FloorToInt(radiusRange / minRadiusSeparation) + 1);
        int maxAnglePoints = Mathf.Max(1, Mathf.FloorToInt(360f / minAngleSeparation));

        if (debugParameterGeneration)
        {
            Debug.Log($"Maximum possible: Y={maxYLayers}, R={maxRadiusRings}, A={maxAnglePoints} " +
                     $"(total={maxYLayers * maxRadiusRings * maxAnglePoints})", this);
        }

        // If we can fit all fish with maximum distribution, use that
        int maxPossible = maxYLayers * maxRadiusRings * maxAnglePoints;
        if (targetFishCount <= maxPossible)
        {
            // Find the most balanced distribution for the target count
            FindBalancedDistribution(targetFishCount, maxYLayers, maxRadiusRings, maxAnglePoints,
                                   out yLayers, out radiusRings, out anglePoints);
        }
        else
        {
            // Use maximum possible and warn
            yLayers = maxYLayers;
            radiusRings = maxRadiusRings;
            anglePoints = maxAnglePoints;

            Debug.LogWarning($"Target fish count ({targetFishCount}) exceeds maximum possible with current separation " +
                           $"constraints ({maxPossible}). Using maximum possible distribution.", this);
        }
    }

    private void FindBalancedDistribution(int targetCount, int maxY, int maxR, int maxA,
                                        out int yLayers, out int radiusRings, out int anglePoints)
    {
        // Start with a balanced cube root distribution
        int basePerDimension = Mathf.Max(1, Mathf.RoundToInt(Mathf.Pow(targetCount, 1f / 3f)));

        yLayers = Mathf.Min(basePerDimension, maxY);
        radiusRings = Mathf.Min(basePerDimension, maxR);
        anglePoints = Mathf.Min(basePerDimension, maxA);

        // Adjust to get closer to target count
        int currentTotal = yLayers * radiusRings * anglePoints;

        // If we're under target, try to expand the most efficient dimension
        while (currentTotal < targetCount)
        {
            bool improved = false;

            // Try expanding angles first (most efficient for circular distribution)
            if (anglePoints < maxA && (yLayers * radiusRings * (anglePoints + 1)) <= targetCount)
            {
                anglePoints++;
                improved = true;
            }
            // Then radius rings
            else if (radiusRings < maxR && (yLayers * (radiusRings + 1) * anglePoints) <= targetCount)
            {
                radiusRings++;
                improved = true;
            }
            // Finally Y layers
            else if (yLayers < maxY && ((yLayers + 1) * radiusRings * anglePoints) <= targetCount)
            {
                yLayers++;
                improved = true;
            }

            if (!improved) break;
            currentTotal = yLayers * radiusRings * anglePoints;
        }

        // Ensure minimum of 1 for each dimension
        yLayers = Mathf.Max(1, yLayers);
        radiusRings = Mathf.Max(1, radiusRings);
        anglePoints = Mathf.Max(1, anglePoints);
    }

    public List<FishSpawnParameters> GenerateAllParameterCombinations()
    {
        if (yPositions == null || radiusValues == null || angleValues == null)
        {
            Initialize();
        }

        List<FishSpawnParameters> combinations = new List<FishSpawnParameters>();

        // Generate all possible combinations for uniform distribution
        foreach (float y in yPositions)
        {
            foreach (float radius in radiusValues)
            {
                foreach (float angle in angleValues)
                {
                    var parameters = CreateParametersFromBase(y, radius, angle);
                    combinations.Add(parameters);
                }
            }
        }

        if (debugParameterGeneration)
            Debug.Log($"Generated {combinations.Count} parameter combinations", this);

        return combinations;
    }

    public FishSpawnParameters CreateParametersFromBase(float baseY, float baseRadius, float baseAngle)
    {
        // Apply randomization with proper clamping to maintain bounds and separation
        Vector3 localCenter = new Vector3(0, baseY, 0);
        float finalRadius = baseRadius;
        float finalAngle = baseAngle;

        // Randomize center Y position with clamping
        if (randomizeCenters)
        {
            float maxYVariation = CalculateMaxYVariation(baseY);
            float clampedVariation = Mathf.Min(centerYRandomRange, maxYVariation);
            float randomOffset = Random.Range(-clampedVariation, clampedVariation);
            localCenter.y = Mathf.Clamp(baseY + randomOffset, minCenterY, maxCenterY);
        }

        // Convert local center to world space
        Vector3 worldCenter = transform.TransformPoint(localCenter);

        // Randomize radius with separation constraints
        if (randomizeRadii)
        {
            float maxRadiusVariation = CalculateMaxRadiusVariation(baseRadius);
            float clampedVariation = Mathf.Min(radiusRandomRange, maxRadiusVariation);
            float randomOffset = Random.Range(-clampedVariation, clampedVariation);
            finalRadius = Mathf.Clamp(baseRadius + randomOffset, minRadius, maxRadius);
        }

        // Randomize angle with separation constraints
        if (randomizeAngles)
        {
            float maxAngleVariation = CalculateMaxAngleVariation(baseAngle);
            float clampedVariation = Mathf.Min(angleRandomRange, maxAngleVariation);
            float randomOffset = Random.Range(-clampedVariation, clampedVariation);
            finalAngle = baseAngle + randomOffset;

            // Normalize angle to 0-360 range
            while (finalAngle < 0) finalAngle += 360f;
            while (finalAngle >= 360f) finalAngle -= 360f;
        }

        return new FishSpawnParameters
        {
            orbitCenter = worldCenter,
            orbitRadius = finalRadius,
            initialAngle = finalAngle,
            baseY = baseY,
            baseRadius = baseRadius,
            baseAngle = baseAngle
        };
    }

    private float CalculateMaxYVariation(float baseY)
    {
        // Calculate maximum variation that won't exceed bounds or violate separation
        float distanceToMin = baseY - minCenterY;
        float distanceToMax = maxCenterY - baseY;
        float maxVariationFromBounds = Mathf.Min(distanceToMin, distanceToMax);

        // Also consider separation from adjacent layers
        float maxVariationFromSeparation = minYSeparation * 0.4f; // Leave some buffer

        return Mathf.Min(maxVariationFromBounds, maxVariationFromSeparation);
    }

    private float CalculateMaxRadiusVariation(float baseRadius)
    {
        float distanceToMin = baseRadius - minRadius;
        float distanceToMax = maxRadius - baseRadius;
        float maxVariationFromBounds = Mathf.Min(distanceToMin, distanceToMax);

        float maxVariationFromSeparation = minRadiusSeparation * 0.4f;

        return Mathf.Min(maxVariationFromBounds, maxVariationFromSeparation);
    }

    private float CalculateMaxAngleVariation(float baseAngle)
    {
        // For angles, we need to be careful about wrapping and adjacent fish
        return minAngleSeparation * 0.4f; // Conservative variation
    }

    public void GenerateYPositions(int layerCount)
    {
        layerCount = Mathf.Max(1, layerCount);
        yPositions = new float[layerCount];

        if (layerCount == 1)
        {
            yPositions[0] = (minCenterY + maxCenterY) * 0.5f;
        }
        else
        {
            // Distribute evenly with proper separation
            float totalRange = maxCenterY - minCenterY;
            float stepSize = totalRange / (layerCount - 1);

            // Ensure minimum separation is maintained
            float minRequiredRange = (layerCount - 1) * minYSeparation;
            if (totalRange < minRequiredRange)
            {
                Debug.LogWarning($"Y range ({totalRange:F2}) too small for {layerCount} layers with minimum separation {minYSeparation:F2}", this);
                stepSize = minYSeparation;
            }

            for (int i = 0; i < layerCount; i++)
            {
                yPositions[i] = minCenterY + (i * stepSize);
            }
        }

        if (debugParameterGeneration)
        {
            Debug.Log($"Generated {layerCount} Y positions: [{string.Join(", ", System.Array.ConvertAll(yPositions, x => x.ToString("F2")))}]", this);
        }
    }

    public void GenerateRadiusValues(int ringCount)
    {
        ringCount = Mathf.Max(1, ringCount);
        radiusValues = new float[ringCount];

        if (ringCount == 1)
        {
            radiusValues[0] = (minRadius + maxRadius) * 0.5f;
        }
        else
        {
            float totalRange = maxRadius - minRadius;
            float stepSize = totalRange / (ringCount - 1);

            float minRequiredRange = (ringCount - 1) * minRadiusSeparation;
            if (totalRange < minRequiredRange)
            {
                Debug.LogWarning($"Radius range ({totalRange:F2}) too small for {ringCount} rings with minimum separation {minRadiusSeparation:F2}", this);
                stepSize = minRadiusSeparation;
            }

            for (int i = 0; i < ringCount; i++)
            {
                radiusValues[i] = minRadius + (i * stepSize);
            }
        }

        if (debugParameterGeneration)
        {
            Debug.Log($"Generated {ringCount} radius values: [{string.Join(", ", System.Array.ConvertAll(radiusValues, x => x.ToString("F2")))}]", this);
        }
    }

    public void GenerateAngleValues(int angleCount)
    {
        angleCount = Mathf.Max(1, angleCount);
        angleValues = new float[angleCount];

        if (angleCount == 1)
        {
            angleValues[0] = 0f;
        }
        else
        {
            float stepSize = 360f / angleCount;

            // Ensure minimum separation
            if (stepSize < minAngleSeparation)
            {
                Debug.LogWarning($"Too many angle points ({angleCount}) for minimum separation {minAngleSeparation}°. " +
                               $"Maximum recommended: {Mathf.FloorToInt(360f / minAngleSeparation)}", this);
            }

            for (int i = 0; i < angleCount; i++)
            {
                angleValues[i] = i * stepSize;
            }
        }

        if (debugParameterGeneration)
        {
            Debug.Log($"Generated {angleCount} angle values: [{string.Join(", ", System.Array.ConvertAll(angleValues, x => x.ToString("F0")))}]°", this);
        }
    }

    void OnValidate()
    {
        // Ensure min is not greater than max
        if (minCenterY > maxCenterY)
        {
            float temp = minCenterY;
            minCenterY = maxCenterY;
            maxCenterY = temp;
        }

        if (minRadius > maxRadius)
        {
            float temp = minRadius;
            minRadius = maxRadius;
            maxRadius = temp;
        }

        // Ensure minimum values
        minRadius = Mathf.Max(0.1f, minRadius);
        maxRadius = Mathf.Max(0.1f, maxRadius);
        minYSeparation = Mathf.Max(0.01f, minYSeparation);
        minRadiusSeparation = Mathf.Max(0.01f, minRadiusSeparation);
        minAngleSeparation = Mathf.Clamp(minAngleSeparation, 1f, 180f);

        // Clamp randomization ranges to reasonable values
        centerYRandomRange = Mathf.Max(0f, centerYRandomRange);
        radiusRandomRange = Mathf.Max(0f, radiusRandomRange);
        angleRandomRange = Mathf.Clamp(angleRandomRange, 0f, 45f);

        // Regenerate if playing
        if (Application.isPlaying && gameObject.activeInHierarchy)
        {
            Initialize();
        }
    }

    #region Preview Methods
    [ContextMenu("Preview Distribution")]
    public void PreviewDistribution()
    {
        int targetFishCount = GetTargetFishCount();
        CalculateOptimalDistribution(targetFishCount, out int yLayers, out int radiusRings, out int anglePoints);

        Debug.Log($"=== Distribution Preview for {targetFishCount} Fish ===");
        Debug.Log($"Y Layers: {yLayers} (range: {minCenterY:F1} to {maxCenterY:F1}, separation: {minYSeparation:F2})");
        Debug.Log($"Radius Rings: {radiusRings} (range: {minRadius:F1} to {maxRadius:F1}, separation: {minRadiusSeparation:F2})");
        Debug.Log($"Angle Points: {anglePoints} (separation: {minAngleSeparation:F1}°)");
        Debug.Log($"Total Combinations: {yLayers * radiusRings * anglePoints}");

        GenerateYPositions(yLayers);
        GenerateRadiusValues(radiusRings);
        GenerateAngleValues(anglePoints);
    }
    #endregion
}