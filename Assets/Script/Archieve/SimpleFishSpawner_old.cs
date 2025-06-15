using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleFishSpawner_old : MonoBehaviour
{
    [Header("Fish Spawning")]
    public GameObject fishPrefab;
    public Transform parentContainer; // Optional container for organization

    [Header("Spawning Options")]
    public bool spawnOnStart = true;
    public bool randomizeDistribution = true; // Changed default to true

    [Header("Fish Count Control")]
    [Tooltip("Maximum number of fish to spawn (0 = spawn all combinations)")]
    public int maxFishCount = 0;
    [Tooltip("Number of fish to spawn at once (0 = spawn all at once)")]
    public int batchSize = 50;
    [Tooltip("Delay between batches when spawning gradually")]
    public float batchDelay = 2f;
    [Tooltip("Show total possible combinations in inspector")]
    [SerializeField] private int totalPossibleFish = 648;

    [Header("Pool Management")]
    [Tooltip("Enable pooling system for caught fish")]
    public bool enablePooling = true;
    [Tooltip("Time before respawning caught fish (0 = instant)")]
    public float respawnDelay = 5f;
    [Tooltip("Maximum number of fish in pool waiting to respawn")]
    public int maxPoolSize = 50;

    [Header("Randomization Settings")]
    [Tooltip("Randomize the order of spawning (affects distribution pattern)")]
    public bool randomizeSpawnOrder = true;

    [Tooltip("Add random offset to initial angles")]
    public bool randomizeAngles = true;
    public float angleRandomRange = 15f; // ±15 degrees from base angles

    [Tooltip("Add random variation to Y position only (X,Z stay at 0)")]
    public bool randomizeCenters = true;
    public float centerYRandomRange = 0.1f; // ±0.1 units in Y direction only

    [Tooltip("Add random variation to orbit radii")]
    public bool randomizeRadii = true;
    public float radiusRandomRange = 0.1f; // ±0.1 units from base radius

    [Tooltip("Randomize initial speeds")]
    public bool randomizeInitialSpeeds = true;
    public float speedRandomRange = 10f; // ±10 degrees/second from base speed

    [Header("Y Position Settings")]
    [Tooltip("Minimum Y position offset from spawner")]
    public float minCenterY = 0.4f;
    [Tooltip("Maximum Y position offset from spawner")]
    public float maxCenterY = 2.0f;
    [Tooltip("Number of Y layers to create between min and max")]
    [Range(1, 20)]
    public int yLayerCount = 9;
    
    [Header("Radius Settings")]
    [Tooltip("Minimum orbit radius")]
    public float minRadius = 1.0f;
    [Tooltip("Maximum orbit radius")]
    public float maxRadius = 2.0f;
    [Tooltip("Number of radius rings to create between min and max")]
    [Range(1, 20)]
    public int radiusRingCount = 6;
    
    [Header("Debug")]
    public bool showSpawnGizmos = true;
    public bool debugPooling = false;

    // Pool management
    private List<GameObject> activeFish = new List<GameObject>();
    private Queue<GameObject> fishPool = new Queue<GameObject>();
    private List<FishParameters> originalParameters = new List<FishParameters>();
    private Dictionary<GameObject, FishParameters> fishParametersMap = new Dictionary<GameObject, FishParameters>();
    
    // Batch spawning management
    private List<FishParameters> pendingSpawns = new List<FishParameters>();
    private Coroutine currentSpawnCoroutine;
    private bool isSpawning = false;

    // Parameter arrays - these are now local offsets from the spawner's position
    private float[] yPositions; // Will be generated based on min/max/count
    private float[] radiusValues; // Will be generated based on min/max/count
    private readonly float[] angleValues = { 0f, 30f, 60f, 90f, 120f, 150f, 180f, 210f, 240f, 270f, 300f, 330f }; // 12 values

    void Start()
    {
        // Generate Y positions and radius values based on min/max/count settings
        GenerateYPositions();
        GenerateRadiusValues();
        
        // Calculate total possible combinations for inspector display
        totalPossibleFish = yPositions.Length * radiusValues.Length * angleValues.Length;

        if (spawnOnStart)
        {
            SpawnAllFish();
        }
    }
    
    void GenerateYPositions()
    {
        // Ensure we have at least 1 layer
        int layers = Mathf.Max(1, yLayerCount);
        yPositions = new float[layers];
        
        if (layers == 1)
        {
            // Single layer at midpoint
            yPositions[0] = (minCenterY + maxCenterY) * 0.5f;
        }
        else
        {
            // Multiple layers evenly distributed
            for (int i = 0; i < layers; i++)
            {
                float t = (float)i / (layers - 1); // 0 to 1
                yPositions[i] = Mathf.Lerp(minCenterY, maxCenterY, t);
            }
        }
        
        // Debug log the generated positions
        if (Application.isPlaying && debugPooling)
        {
            Debug.Log($"Generated {layers} Y positions from {minCenterY} to {maxCenterY}: [{string.Join(", ", System.Array.ConvertAll(yPositions, x => x.ToString("F2")))}]", this);
        }
    }
    
    void GenerateRadiusValues()
    {
        // Ensure we have at least 1 ring
        int rings = Mathf.Max(1, radiusRingCount);
        radiusValues = new float[rings];
        
        if (rings == 1)
        {
            // Single ring at midpoint
            radiusValues[0] = (minRadius + maxRadius) * 0.5f;
        }
        else
        {
            // Multiple rings evenly distributed
            for (int i = 0; i < rings; i++)
            {
                float t = (float)i / (rings - 1); // 0 to 1
                radiusValues[i] = Mathf.Lerp(minRadius, maxRadius, t);
            }
        }
        
        // Debug log the generated radius values
        if (Application.isPlaying && debugPooling)
        {
            Debug.Log($"Generated {rings} radius values from {minRadius} to {maxRadius}: [{string.Join(", ", System.Array.ConvertAll(radiusValues, x => x.ToString("F2")))}]", this);
        }
    }

    [ContextMenu("Spawn All Fish")]
    public void SpawnAllFish()
    {
        if (fishPrefab == null)
        {
            Debug.LogError("Fish prefab is not assigned!");
            return;
        }

        // Stop any current spawning process
        StopSpawning();

        // Regenerate Y positions and radius values in case settings changed
        GenerateYPositions();
        GenerateRadiusValues();

        // Clear existing fish
        ClearSpawnedFish();

        // Generate all parameter combinations
        originalParameters = GenerateParameterCombinations();

        // Always randomize spawn order for random initialization
        if (randomizeSpawnOrder || randomizeDistribution)
        {
            ShuffleList(originalParameters);
        }

        // Limit the number of fish if maxFishCount is set
        if (maxFishCount > 0 && maxFishCount < originalParameters.Count)
        {
            originalParameters = originalParameters.GetRange(0, maxFishCount);
            Debug.Log($"Limited to {maxFishCount} fish out of {totalPossibleFish} possible combinations");
        }

        // Spawn fish based on batch settings
        if (batchSize > 0 && batchSize < originalParameters.Count)
        {
            // Spawn in batches
            StartBatchSpawning(originalParameters);
        }
        else
        {
            // Spawn all at once (traditional behavior)
            foreach (var parameters in originalParameters)
            {
                SpawnFish(parameters);
            }
            Debug.Log($"Spawned {activeFish.Count} fish instantly");
        }
    }
    
    /// <summary>
    /// Starts spawning fish in batches
    /// </summary>
    private void StartBatchSpawning(List<FishParameters> allParameters)
    {
        pendingSpawns = new List<FishParameters>(allParameters);
        currentSpawnCoroutine = StartCoroutine(SpawnInBatches());
        isSpawning = true;
        
        Debug.Log($"Starting batch spawning: {allParameters.Count} fish in batches of {batchSize} with {batchDelay}s delay");
    }
    
    /// <summary>
    /// Coroutine that spawns fish in batches
    /// </summary>
    private IEnumerator SpawnInBatches()
    {
        int totalToSpawn = pendingSpawns.Count;
        int spawnedCount = 0;
        
        while (pendingSpawns.Count > 0)
        {
            // Calculate how many to spawn in this batch
            int currentBatchSize = Mathf.Min(batchSize, pendingSpawns.Count);
            
            Debug.Log($"Spawning batch: {currentBatchSize} fish ({spawnedCount + currentBatchSize}/{totalToSpawn})");
            
            // Spawn current batch
            for (int i = 0; i < currentBatchSize; i++)
            {
                if (pendingSpawns.Count > 0)
                {
                    var parameters = pendingSpawns[0];
                    pendingSpawns.RemoveAt(0);
                    SpawnFish(parameters);
                    spawnedCount++;
                }
            }
            
            // Wait before next batch (unless this was the last batch)
            if (pendingSpawns.Count > 0)
            {
                yield return new WaitForSeconds(batchDelay);
            }
        }
        
        isSpawning = false;
        Debug.Log($"Batch spawning complete: {spawnedCount} fish spawned");
    }
    
    /// <summary>
    /// Stops any ongoing spawning process
    /// </summary>
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

    private List<FishParameters> GenerateParameterCombinations()
    {
        List<FishParameters> combinations = new List<FishParameters>();

        // Generate all possible combinations for uniform distribution
        foreach (float y in yPositions)
        {
            foreach (float radius in radiusValues)
            {
                foreach (float angle in angleValues)
                {
                    // Apply randomization to base parameters
                    // Create local orbit center (relative to spawner's position)
                    Vector3 localCenter = new Vector3(0, y, 0); // Local offset from spawner
                    float finalRadius = radius;
                    float finalAngle = angle;

                    // Randomize center Y position only (X,Z stay at 0)
                    if (randomizeCenters)
                    {
                        localCenter.y += Random.Range(-centerYRandomRange, centerYRandomRange);
                    }

                    // Convert local center to world space
                    Vector3 worldCenter = transform.TransformPoint(localCenter);

                    // Randomize radius
                    if (randomizeRadii)
                    {
                        finalRadius += Random.Range(-radiusRandomRange, radiusRandomRange);
                        finalRadius = Mathf.Max(0.5f, finalRadius); // Ensure minimum radius
                    }

                    // Randomize angle
                    if (randomizeAngles)
                    {
                        finalAngle += Random.Range(-angleRandomRange, angleRandomRange);
                        if (finalAngle < 0) finalAngle += 360f;
                        if (finalAngle >= 360f) finalAngle -= 360f;
                    }

                    combinations.Add(new FishParameters
                    {
                        orbitCenter = worldCenter,
                        orbitRadius = finalRadius,
                        initialAngle = finalAngle,
                        baseY = y,
                        baseRadius = radius,
                        baseAngle = angle
                    });
                }
            }
        }

        return combinations;
    }

    private void SpawnFish(FishParameters parameters)
    {
        GameObject newFish = null;

        // Try to get fish from pool first
        if (enablePooling && fishPool.Count > 0)
        {
            newFish = fishPool.Dequeue();
            if (debugPooling)
                Debug.Log($"Reusing fish from pool. Pool size: {fishPool.Count}", this);
        }
        else
        {
            // Create new fish
            newFish = Instantiate(fishPrefab);
            if (debugPooling)
                Debug.Log("Created new fish instance", this);
        }

        // Set parent if specified
        if (parentContainer != null)
        {
            newFish.transform.SetParent(parentContainer);
        }

        // Calculate initial position based on orbit parameters
        Vector3 initialPosition = CalculateOrbitPosition(parameters.orbitCenter, parameters.orbitRadius, parameters.initialAngle);
        
        // IMPORTANT: Reset fish position and rotation BEFORE configuring AI
        newFish.transform.position = initialPosition;
        
        // Calculate correct rotation based on orbital movement direction
        float radians = parameters.initialAngle * Mathf.Deg2Rad;
        Vector3 tangentDirection = new Vector3(-Mathf.Sin(radians), 0f, Mathf.Cos(radians));
        if (tangentDirection.magnitude > 0.1f)
        {
            Quaternion correctRotation = Quaternion.LookRotation(tangentDirection);
            newFish.transform.rotation = correctRotation;
        }
        else
        {
            newFish.transform.rotation = Quaternion.identity;
        }
        
        newFish.SetActive(true);

        // Configure the FishAI component
        FishAI_old fishAI = newFish.GetComponent<FishAI_old>();
        if (fishAI != null)
        {
            // Set spawner reference for pool management
            fishAI.spawner = this;
            
            // Set orbit parameters
            fishAI.orbitCenter = parameters.orbitCenter;
            fishAI.orbitRadius = parameters.orbitRadius;

            // Set initial angle using the public property
            fishAI.CurrentAngle = parameters.initialAngle;

            // Randomize initial speed if enabled
            if (randomizeInitialSpeeds)
            {
                float randomSpeed = fishAI.baseOrbitSpeed + Random.Range(-speedRandomRange, speedRandomRange);
                fishAI.currentSpeed = Mathf.Max(fishAI.minSpeed, randomSpeed);
            }

            // Reset fish state AFTER setting orbit parameters
            fishAI.ResetFishState();
            
            // Force position update to ensure fish is at correct orbit position
            fishAI.ForcePositionUpdate();
        }
        else
        {
            Debug.LogWarning($"Fish prefab does not have FishAI component: {newFish.name}");
        }

        // Name the fish for easier identification (using base values for consistency)
        newFish.name = $"Fish_Y{parameters.baseY:F1}_R{parameters.baseRadius:F1}_A{parameters.baseAngle:F0}";

        // Track fish and its parameters
        activeFish.Add(newFish);
        fishParametersMap[newFish] = parameters;
    }

    /// <summary>
    /// Called by FishAI when a fish is caught. Handles pooling and respawning.
    /// </summary>
    public void ReturnFishToPool(GameObject fish)
    {
        if (fish == null) return;

        if (debugPooling)
            Debug.Log($"Fish caught: {fish.name}. Managing with pool system.", this);

        // Remove from active list
        activeFish.Remove(fish);

        if (enablePooling)
        {
            // Get the original parameters for this fish
            if (fishParametersMap.TryGetValue(fish, out FishParameters originalParams))
            {
                // IMPORTANT: Store the original parameters for respawn
                FishParameters respawnParams = originalParams;
                
                // Add to pool if there's space
                if (fishPool.Count < maxPoolSize)
                {
                    // Deactivate the fish but keep it in memory
                    fish.SetActive(false);
                    fishPool.Enqueue(fish);
                    
                    if (debugPooling)
                        Debug.Log($"Added fish to pool. Pool size: {fishPool.Count}", this);
                }
                else
                {
                    // Pool is full, remove from tracking and destroy
                    if (debugPooling)
                        Debug.Log("Pool is full, destroying fish", this);
                    
                    fishParametersMap.Remove(fish);
                    Destroy(fish);
                }

                // Schedule respawn with original parameters (not current fish position)
                // Only respawn if we're not currently in a spawning process or if batch spawning is complete
                if (respawnDelay > 0)
                {
                    StartCoroutine(RespawnFishAfterDelay(respawnParams, respawnDelay));
                }
                else if (!isSpawning)
                {
                    // Respawn immediately only if not batch spawning
                    SpawnFish(respawnParams);
                }
                else
                {
                    // Add to pending spawns if batch spawning is active
                    pendingSpawns.Add(respawnParams);
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
            // Pooling disabled, just destroy
            fishParametersMap.Remove(fish);
            Destroy(fish);
        }
    }

    private IEnumerator RespawnFishAfterDelay(FishParameters parameters, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (debugPooling)
            Debug.Log($"Respawning fish after {delay}s delay", this);
        
        SpawnFish(parameters);
    }

    private Vector3 CalculateOrbitPosition(Vector3 center, float radius, float angle)
    {
        float radians = angle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            Mathf.Cos(radians) * radius,
            0f,
            Mathf.Sin(radians) * radius
        );
        return center + offset;
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

    [ContextMenu("Clear All Fish")]
    public void ClearSpawnedFish()
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
    }

    // Public properties for debugging
    public int ActiveFishCount => activeFish.Count;
    public int PooledFishCount => fishPool.Count;
    public int TotalFishCount => activeFish.Count + fishPool.Count;
    public int PendingSpawnCount => pendingSpawns.Count;
    public bool IsSpawning => isSpawning;

    [ContextMenu("Preview Y Positions")]
    public void PreviewYPositions()
    {
        GenerateYPositions();
        Debug.Log($"Y Positions Preview ({yLayerCount} layers from {minCenterY} to {maxCenterY}):");
        for (int i = 0; i < yPositions.Length; i++)
        {
            Vector3 worldPos = transform.TransformPoint(new Vector3(0, yPositions[i], 0));
            Debug.Log($"  Layer {i + 1}: Local Y = {yPositions[i]:F2}, World Y = {worldPos.y:F2}");
        }
    }
    
    [ContextMenu("Preview Radius Values")]
    public void PreviewRadiusValues()
    {
        GenerateRadiusValues();
        Debug.Log($"Radius Values Preview ({radiusRingCount} rings from {minRadius} to {maxRadius}):");
        for (int i = 0; i < radiusValues.Length; i++)
        {
            Debug.Log($"  Ring {i + 1}: Radius = {radiusValues[i]:F2}");
        }
    }
    
    [ContextMenu("Preview All Parameters")]
    public void PreviewAllParameters()
    {
        GenerateYPositions();
        GenerateRadiusValues();
        int totalCombinations = yPositions.Length * radiusValues.Length * angleValues.Length;
        
        Debug.Log($"=== Spawn Parameters Preview ===");
        Debug.Log($"Y Layers: {yLayerCount} ({minCenterY} to {maxCenterY})");
        Debug.Log($"Radius Rings: {radiusRingCount} ({minRadius} to {maxRadius})");
        Debug.Log($"Angle Points: {angleValues.Length}");
        Debug.Log($"Total Fish Combinations: {totalCombinations}");
        Debug.Log($"Y Values: [{string.Join(", ", System.Array.ConvertAll(yPositions, x => x.ToString("F1")))}]");
        Debug.Log($"Radius Values: [{string.Join(", ", System.Array.ConvertAll(radiusValues, x => x.ToString("F1")))}]");
    }
    
    [ContextMenu("Spawn Single Batch")]
    public void SpawnSingleBatch()
    {
        if (batchSize <= 0)
        {
            Debug.LogWarning("Batch size must be greater than 0 to spawn a single batch");
            return;
        }
        
        // Generate parameters if not already done
        if (originalParameters.Count == 0)
        {
            GenerateYPositions();
            GenerateRadiusValues();
            originalParameters = GenerateParameterCombinations();
            
            if (randomizeSpawnOrder || randomizeDistribution)
            {
                ShuffleList(originalParameters);
            }
            
            if (maxFishCount > 0 && maxFishCount < originalParameters.Count)
            {
                originalParameters = originalParameters.GetRange(0, maxFishCount);
            }
        }
        
        // Spawn one batch worth of fish
        int toSpawn = Mathf.Min(batchSize, originalParameters.Count - activeFish.Count);
        
        if (toSpawn <= 0)
        {
            Debug.Log("No more fish to spawn - all combinations already active");
            return;
        }
        
        Debug.Log($"Spawning single batch: {toSpawn} fish");
        
        for (int i = 0; i < toSpawn; i++)
        {
            int paramIndex = activeFish.Count; // Use active fish count as index
            if (paramIndex < originalParameters.Count)
            {
                SpawnFish(originalParameters[paramIndex]);
            }
        }
    }
    
    [ContextMenu("Stop Spawning")]
    public void StopSpawningMenu()
    {
        StopSpawning();
    }
    
    [ContextMenu("Spawn Random Distribution")]
    public void SpawnRandomDistribution()
    {
        // Temporarily enable all randomization options for maximum randomness
        bool tempRandomizeOrder = randomizeSpawnOrder;
        bool tempRandomizeAngles = randomizeAngles;
        bool tempRandomizeCenters = randomizeCenters;
        bool tempRandomizeRadii = randomizeRadii;
        bool tempRandomizeSpeeds = randomizeInitialSpeeds;

        randomizeSpawnOrder = true;
        randomizeAngles = true;
        randomizeCenters = true;
        randomizeRadii = true;
        randomizeInitialSpeeds = true;

        SpawnAllFish();

        // Restore original settings
        randomizeSpawnOrder = tempRandomizeOrder;
        randomizeAngles = tempRandomizeAngles;
        randomizeCenters = tempRandomizeCenters;
        randomizeRadii = tempRandomizeRadii;
        randomizeInitialSpeeds = tempRandomizeSpeeds;
    }

    [ContextMenu("Spawn Uniform Distribution")]
    public void SpawnUniformDistribution()
    {
        // Temporarily disable all randomization for perfect uniformity
        bool tempRandomizeOrder = randomizeSpawnOrder;
        bool tempRandomizeAngles = randomizeAngles;
        bool tempRandomizeCenters = randomizeCenters;
        bool tempRandomizeRadii = randomizeRadii;
        bool tempRandomizeSpeeds = randomizeInitialSpeeds;

        randomizeSpawnOrder = false;
        randomizeAngles = false;
        randomizeCenters = false;
        randomizeRadii = false;
        randomizeInitialSpeeds = false;

        SpawnAllFish();

        // Restore original settings
        randomizeSpawnOrder = tempRandomizeOrder;
        randomizeAngles = tempRandomizeAngles;
        randomizeCenters = tempRandomizeCenters;
        randomizeRadii = tempRandomizeRadii;
        randomizeInitialSpeeds = tempRandomizeSpeeds;
    }

    [ContextMenu("Spawn By Layer")]
    public void SpawnByLayer()
    {
        if (fishPrefab == null)
        {
            Debug.LogError("Fish prefab is not assigned!");
            return;
        }

        // Stop any current spawning
        StopSpawning();

        // Regenerate Y positions and radius values in case settings changed
        GenerateYPositions();
        GenerateRadiusValues();

        ClearSpawnedFish();

        // Spawn layer by layer for better visualization
        for (int layerIndex = 0; layerIndex < yPositions.Length; layerIndex++)
        {
            float y = yPositions[layerIndex];
            StartCoroutine(SpawnLayerWithDelay(y, layerIndex * 0.5f));
        }
    }

    private IEnumerator SpawnLayerWithDelay(float yPosition, float delay)
    {
        yield return new WaitForSeconds(delay);

        Debug.Log($"Spawning fish at local Y = {yPosition}");

        foreach (float radius in radiusValues)
        {
            foreach (float angle in angleValues)
            {
                // Convert local Y position to world space
                Vector3 localCenter = new Vector3(0, yPosition, 0);
                Vector3 worldCenter = transform.TransformPoint(localCenter);

                var parameters = new FishParameters
                {
                    orbitCenter = worldCenter,
                    orbitRadius = radius,
                    initialAngle = angle,
                    baseY = yPosition,
                    baseRadius = radius,
                    baseAngle = angle
                };

                SpawnFish(parameters);

                // Small delay between individual fish spawns for visual effect
                yield return new WaitForSeconds(0.02f);
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!showSpawnGizmos) return;

        // Draw orbit centers and radii for visualization (now in local space)
        foreach (float y in yPositions)
        {
            // Convert local position to world space for gizmo drawing
            Vector3 localCenter = new Vector3(0, y, 0);
            Vector3 worldCenter = transform.TransformPoint(localCenter);

            // Draw center point
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(worldCenter, 0.05f);

            // Draw orbit circles for each radius at this height
            foreach (float radius in radiusValues)
            {
                // Use different colors for different radii
                float normalizedRadius = (radius - radiusValues[0]) / (radiusValues[radiusValues.Length - 1] - radiusValues[0]);
                Gizmos.color = Color.Lerp(Color.cyan, Color.blue, normalizedRadius);
                DrawWireCircle(worldCenter, radius);

                // Draw angle markers
                Gizmos.color = Color.yellow;
                foreach (float angle in angleValues)
                {
                    Vector3 anglePos = CalculateOrbitPosition(worldCenter, radius, angle);
                    Gizmos.DrawSphere(anglePos, 0.02f);
                }
            }
        }

        // Draw debug info for pool system
        if (Application.isPlaying && debugPooling)
        {
#if UNITY_EDITOR
            string statusText = $"Active Fish: {ActiveFishCount}\nPooled Fish: {PooledFishCount}\nTotal: {TotalFishCount}";
            if (isSpawning)
            {
                statusText += $"\nSpawning: {PendingSpawnCount} pending";
            }
            UnityEditor.Handles.Label(transform.position + Vector3.up * 3f, statusText);
#endif
        }
    }

    void DrawWireCircle(Vector3 center, float radius, int segments = 32)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 currentPoint = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0,
                Mathf.Sin(angle) * radius
            );

            Gizmos.DrawLine(prevPoint, currentPoint);
            prevPoint = currentPoint;
        }
    }

    // Helper struct for parameter combinations
    [System.Serializable]
    private struct FishParameters
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
        
        // Regenerate values when settings change in editor
        if (Application.isPlaying)
        {
            GenerateYPositions();
            GenerateRadiusValues();
            // Recalculate total possible fish
            totalPossibleFish = yPositions.Length * radiusValues.Length * angleValues.Length;
        }
        else
        {
            // Preview calculation for editor display
            int layers = Mathf.Max(1, yLayerCount);
            int rings = Mathf.Max(1, radiusRingCount);
            totalPossibleFish = layers * rings * angleValues.Length;
        }
    }
}