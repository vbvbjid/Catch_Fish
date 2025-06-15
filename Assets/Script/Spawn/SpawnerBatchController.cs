using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnerBatchController : MonoBehaviour
{
    [Header("Batch Settings")]
    public int batchSize = 50;
    public float batchDelay = 2f;
    public bool randomizeSpawnOrder = true;

    [Header("Debug")]
    public bool debugBatching = true;

    private List<FishSpawnParameters> pendingSpawns = new List<FishSpawnParameters>();
    private Coroutine currentSpawnCoroutine;
    private bool isSpawning = false;
    private SpawnerPoolManager poolManager;

    // Events
    public System.Action<FishSpawnParameters> OnSpawnSingleFish;
    public System.Action OnBatchSpawningStarted;
    public System.Action OnBatchSpawningCompleted;

    public int PendingSpawnCount => pendingSpawns.Count;
    public bool IsSpawning => isSpawning;

    void Awake() => poolManager = GetComponent<SpawnerPoolManager>();

    public void StartSpawning(List<FishSpawnParameters> allParameters)
    {
        StopSpawning();
        
        var parametersToSpawn = FilterByCapacity(allParameters);
        if (parametersToSpawn.Count == 0) return;

        if (randomizeSpawnOrder) ShuffleList(parametersToSpawn);

        if (batchSize > 0 && batchSize < parametersToSpawn.Count)
            StartBatchSpawning(parametersToSpawn);
        else
            SpawnAllAtOnce(parametersToSpawn);
    }

    private List<FishSpawnParameters> FilterByCapacity(List<FishSpawnParameters> allParameters)
    {
        if (poolManager?.enablePooling != true) return new List<FishSpawnParameters>(allParameters);
        
        int capacity = poolManager.AvailablePoolCapacity;
        int toTake = Mathf.Min(allParameters.Count, capacity);
        return toTake > 0 ? allParameters.GetRange(0, toTake) : new List<FishSpawnParameters>();
    }

    private void SpawnAllAtOnce(List<FishSpawnParameters> parameters)
    {
        foreach (var param in parameters)
        {
            if (poolManager?.ReserveSlot() ?? true)
                OnSpawnSingleFish?.Invoke(param);
            else
                break;
        }
    }

    private void StartBatchSpawning(List<FishSpawnParameters> allParameters)
    {
        pendingSpawns = new List<FishSpawnParameters>(allParameters);
        currentSpawnCoroutine = StartCoroutine(SpawnInBatches());
        isSpawning = true;
        OnBatchSpawningStarted?.Invoke();
    }

    private IEnumerator SpawnInBatches()
    {
        int spawnedCount = 0;
        
        while (pendingSpawns.Count > 0)
        {
            var batchToSpawn = ReserveBatch();
            if (batchToSpawn.Count == 0) break;

            foreach (var param in batchToSpawn)
                OnSpawnSingleFish?.Invoke(param);
            
            spawnedCount += batchToSpawn.Count;
            
            if (debugBatching)
                Debug.Log($"Spawned batch: {batchToSpawn.Count} fish (Total: {spawnedCount})", this);

            if (pendingSpawns.Count > 0 && HasCapacityForMore())
                yield return new WaitForSeconds(batchDelay);
        }
        
        isSpawning = false;
        OnBatchSpawningCompleted?.Invoke();
    }

    private List<FishSpawnParameters> ReserveBatch()
    {
        var batch = new List<FishSpawnParameters>();
        int maxBatchSize = Mathf.Min(batchSize, pendingSpawns.Count, poolManager?.AvailablePoolCapacity ?? int.MaxValue);
        
        for (int i = 0; i < maxBatchSize && pendingSpawns.Count > 0; i++)
        {
            if (poolManager?.ReserveSlot() ?? true)
            {
                batch.Add(pendingSpawns[0]);
                pendingSpawns.RemoveAt(0);
            }
            else break;
        }
        
        return batch;
    }

    private bool HasCapacityForMore()
    {
        return poolManager == null || poolManager.EffectiveActiveCount < poolManager.fixedPoolSize;
    }

    public void AddToPendingSpawns(FishSpawnParameters parameters)
    {
        if (poolManager?.EffectiveActiveCount >= poolManager?.fixedPoolSize) return;
        
        if (isSpawning)
            pendingSpawns.Add(parameters);
        else
            OnSpawnSingleFish?.Invoke(parameters);
    }

    public void SpawnSingleBatch(List<FishSpawnParameters> availableParameters)
    {
        if (batchSize <= 0) return;
        
        var filtered = FilterByCapacity(availableParameters);
        var batchToSpawn = new List<FishSpawnParameters>();
        
        for (int i = 0; i < Mathf.Min(batchSize, filtered.Count); i++)
        {
            if (poolManager?.ReserveSlot() ?? true)
                batchToSpawn.Add(filtered[i]);
            else break;
        }
        
        foreach (var param in batchToSpawn)
            OnSpawnSingleFish?.Invoke(param);
    }

    public void StopSpawning()
    {
        if (currentSpawnCoroutine != null)
        {
            StopCoroutine(currentSpawnCoroutine);
            currentSpawnCoroutine = null;
        }
        isSpawning = false;
        pendingSpawns.Clear();
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}