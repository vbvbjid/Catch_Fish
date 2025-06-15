using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleFishSpawner : MonoBehaviour
{
    [Header("Basic Settings")]
    public bool spawnOnStart = true;
    public bool randomizeDistribution = true;

    [Header("Debug")]
    [SerializeField] private int totalPossibleFish = 50;

    // Component references
    private SpawnerParameterGenerator parameterGenerator;
    private SpawnerPoolManager poolManager;
    private SpawnerBatchController batchController;
    private SpawnerFishFactory fishFactory;
    private SpawnerGizmoDrawer gizmoDrawer;

    // Cached parameters
    private List<FishSpawnParameters> originalParameters = new List<FishSpawnParameters>();

    void Start()
    {
        InitializeComponents();
        SetupEventSubscriptions();

        if (spawnOnStart)
            StartCoroutine(DelayedInitialSpawn());
    }

    private IEnumerator DelayedInitialSpawn()
    {
        yield return null; // Wait one frame
        SpawnAllFish();
    }

    private void InitializeComponents()
    {
        parameterGenerator = GetOrAddComponent<SpawnerParameterGenerator>();
        poolManager = GetOrAddComponent<SpawnerPoolManager>();
        batchController = GetOrAddComponent<SpawnerBatchController>();
        fishFactory = GetOrAddComponent<SpawnerFishFactory>();
        gizmoDrawer = GetOrAddComponent<SpawnerGizmoDrawer>();
        
        parameterGenerator.Initialize();
    }

    private T GetOrAddComponent<T>() where T : Component
    {
        var component = GetComponent<T>();
        return component ?? gameObject.AddComponent<T>();
    }

    private void SetupEventSubscriptions()
    {
        if (batchController != null)
        {
            batchController.OnSpawnSingleFish += HandleSpawnSingleFish;
            batchController.OnBatchSpawningStarted += () => Debug.Log("Batch spawning started", this);
            batchController.OnBatchSpawningCompleted += () => Debug.Log($"Batch spawning completed. Active: {poolManager.ActiveFishCount}", this);
        }

        if (poolManager != null)
            poolManager.OnFishNeedsRespawn += HandleFishNeedsRespawn;
    }

    #region Public API Methods

    [ContextMenu("Spawn All Fish")]
    public void SpawnAllFish()
    {
        if (fishFactory.fishPrefab == null)
        {
            Debug.LogError("Fish prefab is not assigned!");
            return;
        }

        StopSpawning();
        ClearSpawnedFish();

        originalParameters = parameterGenerator.GenerateAllParameterCombinations();
        totalPossibleFish = originalParameters.Count;

        if (poolManager.enablePooling && originalParameters.Count > poolManager.fixedPoolSize)
        {
            Debug.LogWarning($"Generated {originalParameters.Count} combinations, limited to {poolManager.fixedPoolSize}", this);
        }

        batchController.StartSpawning(originalParameters);
    }

    [ContextMenu("Clear All Fish")]
    public void ClearSpawnedFish() => poolManager?.ClearAllFish();

    [ContextMenu("Stop Spawning")]
    public void StopSpawning()
    {
        batchController?.StopSpawning();
        poolManager?.StopAllRespawnCoroutines();
    }

    public void ReturnFishToPool(GameObject fish) => poolManager?.ReturnFishToPool(fish);

    #endregion

    #region Event Handlers

    private void HandleSpawnSingleFish(FishSpawnParameters parameters)
    {
        var pooledFish = poolManager?.GetPooledFish();
        var newFish = fishFactory.CreateFish(parameters, pooledFish);
        
        if (newFish != null)
            poolManager?.RegisterFish(newFish, parameters);
        else
            poolManager?.ReleaseReservation();
    }

    private void HandleFishNeedsRespawn(GameObject originalFish, FishSpawnParameters parameters)
{
    // Check if the fish needing respawn is in recovery
    if (originalFish != null)
    {
        FishAI fishAI = originalFish.GetComponent<FishAI>();
        if (fishAI != null && fishAI.CurrentState == FishStateMachine.FishState.Recovering)
        {
            Debug.LogWarning($"[SPAWNER DEBUG] Fish '{originalFish.name}' needs respawn while in RECOVERY state!", this);
        }
    }

    if (batchController.IsSpawning)
    {
        Debug.Log($"[SPAWNER DEBUG] Adding fish to pending spawns during recovery: {parameters}", this);
        batchController.AddToPendingSpawns(parameters);
    }
    else if (poolManager.ReserveSlot())
    {
        Debug.Log($"[SPAWNER DEBUG] Directly spawning fish replacement during recovery: {parameters}", this);
        HandleSpawnSingleFish(parameters);
    }
}


    #endregion

    #region Properties

    public int ActiveFishCount => poolManager?.ActiveFishCount ?? 0;
    public int PooledFishCount => poolManager?.PooledFishCount ?? 0;
    public int TotalFishCount => poolManager?.TotalFishCount ?? 0;
    public int PendingSpawnCount => batchController?.PendingSpawnCount ?? 0;
    public bool IsSpawning => batchController?.IsSpawning ?? false;
    public int TotalPossibleCombinations => parameterGenerator?.TotalPossibleCombinations ?? 0;
    public int AvailablePoolCapacity => poolManager?.AvailablePoolCapacity ?? 0;

    #endregion

    #region Context Menu Methods

    [ContextMenu("Spawn Single Batch")]
    public void SpawnSingleBatch()
    {
        if (originalParameters.Count == 0)
            originalParameters = parameterGenerator.GenerateAllParameterCombinations();
        batchController.SpawnSingleBatch(originalParameters);
    }

    [ContextMenu("Spawn Random Distribution")]
    public void SpawnRandomDistribution()
    {
        var settings = SaveRandomizationSettings();
        SetRandomizationSettings(true);
        SpawnAllFish();
        RestoreRandomizationSettings(settings);
    }

    [ContextMenu("Spawn Uniform Distribution")]
    public void SpawnUniformDistribution()
    {
        var settings = SaveRandomizationSettings();
        SetRandomizationSettings(false);
        SpawnAllFish();
        RestoreRandomizationSettings(settings);
    }

    [ContextMenu("Spawn By Layer")]
    public void SpawnByLayer()
    {
        if (fishFactory.fishPrefab == null) return;
        StopSpawning();
        ClearSpawnedFish();
        StartCoroutine(SpawnLayersWithDelay());
    }

    private IEnumerator SpawnLayersWithDelay()
    {
        if (parameterGenerator.YPositions == null) parameterGenerator.Initialize();

        foreach (float y in parameterGenerator.YPositions)
        {
            StartCoroutine(SpawnLayerWithDelay(y));
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator SpawnLayerWithDelay(float yPosition)
    {
        yield return new WaitForSeconds(0.5f);

        foreach (float radius in parameterGenerator.RadiusValues)
        {
            foreach (float angle in parameterGenerator.AngleValues)
            {
                if (poolManager.enablePooling && !poolManager.ReserveSlot()) yield break;

                var parameters = parameterGenerator.CreateParametersFromBase(yPosition, radius, angle);
                HandleSpawnSingleFish(parameters);
                yield return new WaitForSeconds(0.02f);
            }
        }
    }

    #endregion

    #region Randomization Settings

    private struct RandomizationSettings
    {
        public bool randomizeSpawnOrder, randomizeAngles, randomizeCenters, randomizeRadii, randomizeInitialSpeeds;
    }

    private RandomizationSettings SaveRandomizationSettings()
    {
        return new RandomizationSettings
        {
            randomizeSpawnOrder = batchController.randomizeSpawnOrder,
            randomizeAngles = parameterGenerator.randomizeAngles,
            randomizeCenters = parameterGenerator.randomizeCenters,
            randomizeRadii = parameterGenerator.randomizeRadii,
            randomizeInitialSpeeds = fishFactory.randomizeInitialSpeeds
        };
    }

    private void SetRandomizationSettings(bool enableAll)
    {
        batchController.randomizeSpawnOrder = enableAll;
        parameterGenerator.randomizeAngles = enableAll;
        parameterGenerator.randomizeCenters = enableAll;
        parameterGenerator.randomizeRadii = enableAll;
        fishFactory.randomizeInitialSpeeds = enableAll;
    }

    private void RestoreRandomizationSettings(RandomizationSettings settings)
    {
        batchController.randomizeSpawnOrder = settings.randomizeSpawnOrder;
        parameterGenerator.randomizeAngles = settings.randomizeAngles;
        parameterGenerator.randomizeCenters = settings.randomizeCenters;
        parameterGenerator.randomizeRadii = settings.randomizeRadii;
        fishFactory.randomizeInitialSpeeds = settings.randomizeInitialSpeeds;
    }

    #endregion

    void OnValidate()
    {
        if (parameterGenerator != null)
            totalPossibleFish = parameterGenerator.TotalPossibleCombinations;
    }
}