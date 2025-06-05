// ====================
// SimpleFishSpawner.cs - Main Spawner (Simplified)
// ====================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleFishSpawner2 : MonoBehaviour
{
    [Header("Fish Spawning")]
    public GameObject fishPrefab;
    public Transform parentContainer;

    [Header("Spawning Control")]
    public FishSpawnSettings spawnSettings;
    public FishPoolManager poolManager;
    public FishParameterGenerator parameterGenerator;
    
    [Header("Debug")]
    public bool showSpawnGizmos = true;
    public bool debugPooling = false;

    void Start()
    {
        InitializeComponents();
        
        if (spawnSettings.spawnOnStart)
        {
            SpawnAllFish();
        }
    }
    
    void InitializeComponents()
    {
        if (spawnSettings == null) spawnSettings = GetComponent<FishSpawnSettings>();
        if (poolManager == null) poolManager = GetComponent<FishPoolManager>();
        if (parameterGenerator == null) parameterGenerator = GetComponent<FishParameterGenerator>();
        
        spawnSettings?.Initialize(this);
        poolManager?.Initialize(this, debugPooling);
        parameterGenerator?.Initialize(this);
    }

    [ContextMenu("Spawn All Fish")]
    public void SpawnAllFish()
    {
        if (fishPrefab == null)
        {
            Debug.LogError("Fish prefab is not assigned!");
            return;
        }

        poolManager?.StopSpawning();
        poolManager?.ClearSpawnedFish();

        var parameters = parameterGenerator?.GenerateAllParameters();
        if (parameters != null)
        {
            poolManager?.StartSpawning(parameters);
        }
    }

    public void SpawnFish(FishParameters parameters)
    {
        GameObject newFish = poolManager?.GetFishFromPool();
        
        if (newFish == null)
        {
            newFish = Instantiate(fishPrefab);
            if (debugPooling)
                Debug.Log("Created new fish instance", this);
        }

        ConfigureFish(newFish, parameters);
        poolManager?.RegisterActiveFish(newFish, parameters);
    }
    
    void ConfigureFish(GameObject fish, FishParameters parameters)
    {
        if (parentContainer != null)
        {
            fish.transform.SetParent(parentContainer);
        }

        // Set position and rotation
        Vector3 initialPosition = parameterGenerator.CalculateOrbitPosition(
            parameters.orbitCenter, parameters.orbitRadius, parameters.initialAngle);
        
        fish.transform.position = initialPosition;
        
        // Set rotation
        float radians = parameters.initialAngle * Mathf.Deg2Rad;
        Vector3 tangentDirection = new Vector3(-Mathf.Sin(radians), 0f, Mathf.Cos(radians));
        if (tangentDirection.magnitude > 0.1f)
        {
            fish.transform.rotation = Quaternion.LookRotation(tangentDirection);
        }
        
        fish.SetActive(true);

        // Configure components
        var fishAI = fish.GetComponent<FishAI>();
        var fishMovement = fish.GetComponent<FishMovement>();
        var fishSpawnerRef = fish.GetComponent<FishSpawnerRef>();
        
        if (fishMovement != null)
        {
            fishMovement.orbitCenter = parameters.orbitCenter;
            fishMovement.orbitRadius = parameters.orbitRadius;
            fishMovement.CurrentAngle = parameters.initialAngle;
            
            if (spawnSettings.randomizeInitialSpeeds)
            {
                float randomSpeed = fishMovement.baseOrbitSpeed + 
                    Random.Range(-spawnSettings.speedRandomRange, spawnSettings.speedRandomRange);
                // Note: You'll need to expose currentSpeed in FishMovement or add a setter
            }
        }
        
        if (fishSpawnerRef != null)
        {
            fishSpawnerRef.spawner = this;
        }
        
        if (fishAI != null)
        {
            fishAI.ResetFishState();
        }
        
        if (fishMovement != null)
        {
            fishMovement.ForcePositionUpdate();
        }

        fish.name = $"Fish_Y{parameters.baseY:F1}_R{parameters.baseRadius:F1}_A{parameters.baseAngle:F0}";
    }

    public void ReturnFishToPool(GameObject fish)
    {
        poolManager?.ReturnFishToPool(fish);
    }

    // Context menu methods
    [ContextMenu("Clear All Fish")]
    public void ClearSpawnedFish()
    {
        poolManager?.ClearSpawnedFish();
    }
    
    [ContextMenu("Stop Spawning")]
    public void StopSpawning()
    {
        poolManager?.StopSpawning();
    }

    // Public properties for debugging
    public int ActiveFishCount => poolManager?.ActiveFishCount ?? 0;
    public int PooledFishCount => poolManager?.PooledFishCount ?? 0;
    public int TotalFishCount => poolManager?.TotalFishCount ?? 0;

    void OnDrawGizmos()
    {
        if (!showSpawnGizmos) return;
        parameterGenerator?.DrawGizmos();
        poolManager?.DrawDebugInfo();
    }
}