using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnerPoolManager : MonoBehaviour
{
    [Header("Pool Management")]
    [Tooltip("Enable pooling system for caught fish")]
    public bool enablePooling = true;
    [Tooltip("Fixed pool size - fish will only spawn if pool has capacity")]
    public int fixedPoolSize = 50;
    [Tooltip("Time before respawning caught fish (0 = instant)")]
    public float respawnDelay = 5f;

    [Header("Debug")]
    public bool debugPooling = true; // Enable by default for testing

    // Pool management
    private List<GameObject> activeFish = new List<GameObject>();
    private Queue<GameObject> fishPool = new Queue<GameObject>();
    private Dictionary<GameObject, FishSpawnParameters> fishParametersMap = new Dictionary<GameObject, FishSpawnParameters>();
    private int reservedSlots = 0; // Track fish that are being spawned but not yet registered

    // Events
    public System.Action<GameObject, FishSpawnParameters> OnFishNeedsRespawn;

    public int ActiveFishCount => activeFish.Count;
    public int PooledFishCount => fishPool.Count;
    public int TotalFishCount => activeFish.Count + fishPool.Count;
    public int ReservedSlots => reservedSlots; // Make reserved slots accessible
    public int EffectiveActiveCount => activeFish.Count + reservedSlots; // Include reserved slots
    public int AvailablePoolCapacity => fixedPoolSize - EffectiveActiveCount;
    public bool HasCapacity => EffectiveActiveCount < fixedPoolSize;

    void Start()
    {
        // Pre-allocate the pool to the fixed size to avoid runtime allocations
        InitializePool();
    }

    private void InitializePool()
    {
        // Clear any existing data
        activeFish.Clear();
        fishPool.Clear();
        // Clear reservations too
        reservedSlots = 0;

        if (debugPooling)
            Debug.Log($"Initialized pool with fixed size: {fixedPoolSize}", this);
    }

    public bool ReserveSlot()
    {
        if (!enablePooling) return true;
        
        if (EffectiveActiveCount >= fixedPoolSize)
        {
            if (debugPooling)
                Debug.Log($"Cannot reserve slot - at capacity ({EffectiveActiveCount}/{fixedPoolSize})", this);
            return false;
        }
        
        reservedSlots++;
        if (debugPooling)
            Debug.Log($"Reserved slot. Active: {ActiveFishCount}, Reserved: {reservedSlots}, Effective: {EffectiveActiveCount}/{fixedPoolSize}", this);
        return true;
    }

    public void ReleaseReservation()
    {
        if (reservedSlots > 0)
        {
            reservedSlots--;
            if (debugPooling)
                Debug.Log($"Released reservation. Active: {ActiveFishCount}, Reserved: {reservedSlots}, Effective: {EffectiveActiveCount}/{fixedPoolSize}", this);
        }
    }

    public bool CanSpawnFish()
    {
        return enablePooling ? HasCapacity : true;
    }
    public void RegisterFish(GameObject fish, FishSpawnParameters parameters)
    {
        if (fish == null) return;

        // Convert reservation to actual registration
        if (reservedSlots > 0)
        {
            reservedSlots--;
        }

        activeFish.Add(fish);
        fishParametersMap[fish] = parameters;

        if (debugPooling)
            Debug.Log($"Registered fish: {fish.name}. Active: {ActiveFishCount}/{fixedPoolSize}, Reserved: {reservedSlots}, Pooled: {PooledFishCount}", this);
    }

    public void UnregisterFish(GameObject fish)
    {
        if (fish == null) return;

        activeFish.Remove(fish);
        fishParametersMap.Remove(fish);

        if (debugPooling)
            Debug.Log($"Unregistered fish: {fish.name}. Active: {ActiveFishCount}/{fixedPoolSize}, Pooled: {PooledFishCount}", this);
    }

    public GameObject GetPooledFish()
    {
        if (!enablePooling || fishPool.Count == 0)
            return null;

        GameObject fish = fishPool.Dequeue();
        
        if (debugPooling)
            Debug.Log($"Retrieved fish from pool. Pooled: {PooledFishCount}, Active: {ActiveFishCount}", this);

        return fish;
    }

    public void ReturnFishToPool(GameObject fish)
    {
        if (fish == null) return;

        if (debugPooling)
            Debug.Log($"Fish caught: {fish.name}. Managing with pool system.", this);

        // Remove from active list
        activeFish.Remove(fish);

        if (!enablePooling)
        {
            // Pooling disabled, just clean up and destroy
            fishParametersMap.Remove(fish);
            Destroy(fish);
            return;
        }

        // Get the original parameters for this fish
        if (fishParametersMap.TryGetValue(fish, out FishSpawnParameters originalParams))
        {
            // Deactivate the fish and add to pool (no size check needed since we maintain fixed size)
            fish.SetActive(false);
            fishPool.Enqueue(fish);
            
            if (debugPooling)
                Debug.Log($"Added fish to pool. Pooled: {PooledFishCount}, Active: {ActiveFishCount}", this);

            // Schedule respawn with original parameters
            if (respawnDelay > 0)
            {
                StartCoroutine(RespawnFishAfterDelay(originalParams, respawnDelay));
            }
            else
            {
                // Respawn immediately
                OnFishNeedsRespawn?.Invoke(null, originalParams);
            }
        }
        else
        {
            Debug.LogWarning($"Could not find original parameters for fish: {fish.name}", this);
            fishParametersMap.Remove(fish);
            Destroy(fish);
        }
    }

    private IEnumerator RespawnFishAfterDelay(FishSpawnParameters parameters, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (debugPooling)
            Debug.Log($"Respawning fish after {delay}s delay", this);
        
        OnFishNeedsRespawn?.Invoke(null, parameters);
    }

    public void ClearAllFish()
    {
        // Clear active fish
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

        // Clear pooled fish
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

        fishParametersMap.Clear();

        if (debugPooling)
            Debug.Log("Cleared all fish from pool and active lists", this);
    }

    public void StopAllRespawnCoroutines()
    {
        StopAllCoroutines();
        
        if (debugPooling)
            Debug.Log("Stopped all respawn coroutines", this);
    }

    void OnValidate()
    {
        // Ensure pool size is always positive
        fixedPoolSize = Mathf.Max(1, fixedPoolSize);
    }
}