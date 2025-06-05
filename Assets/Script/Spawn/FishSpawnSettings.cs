// ====================
// FishSpawnSettings.cs - Spawn Configuration
// ====================
using UnityEngine;

[System.Serializable]
public class FishSpawnSettings : MonoBehaviour
{
    [Header("Spawning Options")]
    public bool spawnOnStart = true;
    public bool randomizeDistribution = true;

    [Header("Fish Count Control")]
    public int maxFishCount = 0;
    public int batchSize = 50;
    public float batchDelay = 2f;

    [Header("Randomization")]
    public bool randomizeSpawnOrder = true;
    public bool randomizeAngles = true;
    public bool randomizeCenters = true;
    public bool randomizeRadii = true;
    public bool randomizeInitialSpeeds = true;
    
    [Header("Random Ranges")]
    public float angleRandomRange = 15f;
    public float centerYRandomRange = 0.1f;
    public float radiusRandomRange = 0.1f;
    public float speedRandomRange = 10f;

    [Header("Y Position Settings")]
    public float minCenterY = 0.4f;
    public float maxCenterY = 2.0f;
    [Range(1, 20)]
    public int yLayerCount = 9;
    
    [Header("Radius Settings")]
    public float minRadius = 1.0f;
    public float maxRadius = 2.0f;
    [Range(1, 20)]
    public int radiusRingCount = 6;

    private SimpleFishSpawner2 spawner;

    public void Initialize(SimpleFishSpawner2 fishSpawner)
    {
        spawner = fishSpawner;
    }

    void OnValidate()
    {
        // Ensure min is not greater than max for Y positions
        if (minCenterY > maxCenterY)
        {
            float temp = minCenterY;
            minCenterY = maxCenterY;
            maxCenterY = temp;
        }
        
        // Ensure min is not greater than max for radius
        if (minRadius > maxRadius)
        {
            float temp = minRadius;
            minRadius = maxRadius;
            maxRadius = temp;
        }
        
        // Ensure minimum radius is positive
        minRadius = Mathf.Max(0.1f, minRadius);
        maxRadius = Mathf.Max(0.1f, maxRadius);
    }
}