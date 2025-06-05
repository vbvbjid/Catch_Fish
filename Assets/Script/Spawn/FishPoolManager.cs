// ====================
// FishPoolManager.cs - Pool & Batch Management
// ====================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FishPoolManager : MonoBehaviour
{
    [Header("Pool Settings")]
    public bool enablePooling = true;
    public float respawnDelay = 5f;
    public int maxPoolSize = 50;

    private SimpleFishSpawner2 spawner;
    private FishSpawnSettings settings;
    private bool debugPooling;

    // Pool management
    private List<GameObject> activeFish = new List<GameObject>();
    private Queue<GameObject> fishPool = new Queue<GameObject>();
    private Dictionary<GameObject, FishParameters> fishParametersMap = new Dictionary<GameObject, FishParameters>();
    
    // Batch spawning
    private List<FishParameters> pendingSpawns = new List<FishParameters>();
    private Coroutine currentSpawnCoroutine;
    private bool isSpawning = false;

    public int ActiveFishCount => activeFish.Count;
    public int PooledFishCount => fishPool.Count;
    public int TotalFishCount => activeFish.Count + fishPool.Count;
    public bool IsSpawning => isSpawning;

    public void Initialize(SimpleFishSpawner2 fishSpawner, bool debug)
    {
        spawner = fishSpawner;
        settings = spawner.spawnSettings;
        debugPooling = debug;
    }

    public void StartSpawning(List<FishParameters> allParameters)
    {
        if (settings.batchSize > 0 && settings.batchSize < allParameters.Count)
        {
            StartBatchSpawning(allParameters);
        }
        else
        {
            foreach (var parameters in allParameters)
            {
                spawner.SpawnFish(parameters);
            }
            Debug.Log($"Spawned {activeFish.Count} fish instantly");
        }
    }

    void StartBatchSpawning(List<FishParameters> allParameters)
    {
        pendingSpawns = new List<FishParameters>(allParameters);
        currentSpawnCoroutine = StartCoroutine(SpawnInBatches());
        isSpawning = true;
        
        Debug.Log($"Starting batch spawning: {allParameters.Count} fish in batches of {settings.batchSize}");
    }

    IEnumerator SpawnInBatches()
    {
        int totalToSpawn = pendingSpawns.Count;
        int spawnedCount = 0;
        
        while (pendingSpawns.Count > 0)
        {
            int currentBatchSize = Mathf.Min(settings.batchSize, pendingSpawns.Count);
            
            Debug.Log($"Spawning batch: {currentBatchSize} fish ({spawnedCount + currentBatchSize}/{totalToSpawn})");
            
            for (int i = 0; i < currentBatchSize; i++)
            {
                if (pendingSpawns.Count > 0)
                {
                    var parameters = pendingSpawns[0];
                    pendingSpawns.RemoveAt(0);
                    spawner.SpawnFish(parameters);
                    spawnedCount++;
                }
            }
            
            if (pendingSpawns.Count > 0)
            {
                yield return new WaitForSeconds(settings.batchDelay);
            }
        }
        
        isSpawning = false;
        Debug.Log($"Batch spawning complete: {spawnedCount} fish spawned");
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
        
        if (debugPooling)
            Debug.Log("Spawning process stopped");
    }

    public GameObject GetFishFromPool()
    {
        if (enablePooling && fishPool.Count > 0)
        {
            var fish = fishPool.Dequeue();
            if (debugPooling)
                Debug.Log($"Reusing fish from pool. Pool size: {fishPool.Count}", this);
            return fish;
        }
        return null;
    }

    public void RegisterActiveFish(GameObject fish, FishParameters parameters)
    {
        activeFish.Add(fish);
        fishParametersMap[fish] = parameters;
    }

    public void ReturnFishToPool(GameObject fish)
    {
        if (fish == null) return;

        if (debugPooling)
            Debug.Log($"Fish caught: {fish.name}. Managing with pool system.", this);

        activeFish.Remove(fish);

        if (enablePooling)
        {
            if (fishParametersMap.TryGetValue(fish, out FishParameters originalParams))
            {
                if (fishPool.Count < maxPoolSize)
                {
                    fish.SetActive(false);
                    fishPool.Enqueue(fish);
                    
                    if (debugPooling)
                        Debug.Log($"Added fish to pool. Pool size: {fishPool.Count}", this);
                }
                else
                {
                    if (debugPooling)
                        Debug.Log("Pool is full, destroying fish", this);
                    
                    fishParametersMap.Remove(fish);
                    Destroy(fish);
                }

                // Schedule respawn
                if (respawnDelay > 0)
                {
                    StartCoroutine(RespawnFishAfterDelay(originalParams, respawnDelay));
                }
                else if (!isSpawning)
                {
                    spawner.SpawnFish(originalParams);
                }
                else
                {
                    pendingSpawns.Add(originalParams);
                    if (debugPooling)
                        Debug.Log("Added caught fish to pending spawns during batch spawning", this);
                }
            }
            else
            {
                Debug.LogWarning($"Could not find original parameters for fish: {fish.name}", this);
                fishParametersMap.Remove(fish);
                Destroy(fish);
            }
        }
        else
        {
            fishParametersMap.Remove(fish);
            Destroy(fish);
        }
    }

    IEnumerator RespawnFishAfterDelay(FishParameters parameters, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (debugPooling)
            Debug.Log($"Respawning fish after {delay}s delay", this);
        
        spawner.SpawnFish(parameters);
    }

    public void ClearSpawnedFish()
    {
        foreach (GameObject fish in activeFish)
        {
            if (fish != null)
            {
                fishParametersMap.Remove(fish);
#if UNITY_EDITOR
                if (Application.isPlaying)
                    Destroy(fish);
                else
                    DestroyImmediate(fish);
#else
                Destroy(fish);
#endif
            }
        }
        activeFish.Clear();

        while (fishPool.Count > 0)
        {
            GameObject pooledFish = fishPool.Dequeue();
            if (pooledFish != null)
            {
                fishParametersMap.Remove(pooledFish);
#if UNITY_EDITOR
                if (Application.isPlaying)
                    Destroy(pooledFish);
                else
                    DestroyImmediate(pooledFish);
#else
                Destroy(pooledFish);
#endif
            }
        }

        fishParametersMap.Clear();
    }

    public void DrawDebugInfo()
    {
        if (Application.isPlaying && debugPooling)
        {
#if UNITY_EDITOR
            string statusText = $"Active Fish: {ActiveFishCount}\nPooled Fish: {PooledFishCount}\nTotal: {TotalFishCount}";
            if (isSpawning)
            {
                statusText += $"\nSpawning: {pendingSpawns.Count} pending";
            }
            UnityEditor.Handles.Label(spawner.transform.position + Vector3.up * 3f, statusText);
#endif
        }
    }
}