using System.Collections.Generic;
using UnityEngine;
// Object pool for efficient boid management
public class BoidPool
{
    private Queue<Boid> availableBoids = new Queue<Boid>();
    private List<Boid> activeBoids = new List<Boid>();
    private GameObject boidPrefab;
    private Transform parent;

    public BoidPool(GameObject prefab, Transform parentTransform, int initialSize = 50)
    {
        boidPrefab = prefab;
        parent = parentTransform;

        // Pre-populate pool
        for (int i = 0; i < initialSize; i++)
        {
            CreateNewBoid();
        }
    }

    private Boid CreateNewBoid()
    {
        GameObject boidObj = Object.Instantiate(boidPrefab, parent);
        Boid boid = boidObj.GetComponent<Boid>();
        boidObj.SetActive(false);
        availableBoids.Enqueue(boid);
        return boid;
    }

    public Boid GetBoid(Vector3 position, BoidManager manager)
    {
        Boid boid;

        if (availableBoids.Count == 0)
        {
            boid = CreateNewBoid();
        }
        else
        {
            boid = availableBoids.Dequeue();
        }

        boid.transform.position = position;
        boid.transform.rotation = Random.rotation;
        boid.gameObject.SetActive(true);
        boid.Initialize(manager);
        boid.ResetBoid();
        activeBoids.Add(boid);

        return boid;
    }

    public void ReturnBoid(Boid boid)
    {
        if (activeBoids.Remove(boid))
        {
            boid.gameObject.SetActive(false);
            availableBoids.Enqueue(boid);
        }
    }

    public List<Boid> GetActiveBoids()
    {
        return new List<Boid>(activeBoids);
    }

    public int ActiveCount => activeBoids.Count;
    public int AvailableCount => availableBoids.Count;
}
// Main boid system manager
public class BoidManager : MonoBehaviour
{
    [Header("Flock Settings")]
    public int maxBoids = 100;
    public float spawnRadius = 10f;
    public Vector3 flockCenter = Vector3.zero;
    public float flockRadius = 20f;
    
    [Header("Prefabs")]
    public GameObject boidPrefab;
    
    [Header("Spawn Settings")]
    public bool spawnOnStart = true;
    public int initialBoidCount = 50;
    public float spawnInterval = 1f;
    
    [Header("Obstacles")]
    public List<Transform> obstacles = new List<Transform>();
    public LayerMask obstacleLayerMask = -1;
    public bool autoFindObstacles = true;

    private BoidPool boidPool;
    private float lastSpawnTime;
    private List<Boid> allBoids = new List<Boid>();

    void Start()
    {
        if (boidPrefab == null)
        {
            Debug.LogError("Boid prefab not assigned!");
            return;
        }

        // Initialize object pool
        boidPool = new BoidPool(boidPrefab, transform, maxBoids);

        // Auto-find obstacles if enabled
        if (autoFindObstacles)
        {
            FindObstacles();
        }

        // Spawn initial boids
        if (spawnOnStart)
        {
            SpawnInitialBoids();
        }
    }

    void Update()
    {
        allBoids = boidPool.GetActiveBoids();
        
        // Auto-spawn boids if below threshold
        if (allBoids.Count < maxBoids && Time.time - lastSpawnTime >= spawnInterval)
        {
            SpawnBoid();
            lastSpawnTime = Time.time;
        }
    }

    private void SpawnInitialBoids()
    {
        for (int i = 0; i < Mathf.Min(initialBoidCount, maxBoids); i++)
        {
            SpawnBoid();
        }
    }

    public void SpawnBoid()
    {
        if (boidPool.ActiveCount >= maxBoids) return;

        Vector3 spawnPosition = flockCenter + Random.insideUnitSphere * spawnRadius;
        boidPool.GetBoid(spawnPosition, this);
    }

    public void SpawnBoids(int count)
    {
        for (int i = 0; i < count && boidPool.ActiveCount < maxBoids; i++)
        {
            SpawnBoid();
        }
    }

    public void RemoveRandomBoid()
    {
        var activeBoids = boidPool.GetActiveBoids();
        if (activeBoids.Count > 0)
        {
            int randomIndex = Random.Range(0, activeBoids.Count);
            boidPool.ReturnBoid(activeBoids[randomIndex]);
        }
    }

    public void ClearAllBoids()
    {
        var activeBoids = new List<Boid>(boidPool.GetActiveBoids());
        foreach (var boid in activeBoids)
        {
            boidPool.ReturnBoid(boid);
        }
    }

    private void FindObstacles()
    {
        obstacles.Clear();
        GameObject[] obstacleObjects = GameObject.FindGameObjectsWithTag("Obstacle");
        
        foreach (var obj in obstacleObjects)
        {
            obstacles.Add(obj.transform);
        }

        // Alternative: Find by layer mask
        Collider[] colliders = Physics.OverlapSphere(flockCenter, flockRadius * 2f, obstacleLayerMask);
        foreach (var collider in colliders)
        {
            if (!obstacles.Contains(collider.transform))
            {
                obstacles.Add(collider.transform);
            }
        }
    }

    // Public getters for boid access
    public List<Boid> GetActiveBoids() => boidPool.GetActiveBoids();
    public List<Transform> GetObstacles() => obstacles;
    public Vector3 GetFlockCenter() => flockCenter;
    public float GetFlockRadius() => flockRadius;

    // Debug and utility methods
    public int GetActiveBoidCount() => boidPool.ActiveCount;
    public int GetAvailableBoidCount() => boidPool.AvailableCount;

    void OnDrawGizmos()
    {
        // Draw flock boundary
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(flockCenter, flockRadius);
        
        // Draw spawn area
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(flockCenter, spawnRadius);
        
        // Draw obstacles
        Gizmos.color = Color.red;
        foreach (var obstacle in obstacles)
        {
            if (obstacle != null)
            {
                Gizmos.DrawWireCube(obstacle.position, obstacle.localScale);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw more detailed debug information when selected
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(flockCenter, 0.5f);
    }
}

// Optional: Boid statistics and performance monitor
[System.Serializable]
public class BoidStats
{
    public int activeBoids;
    public int pooledBoids;
    public float averageSpeed;
    public float averageNeighbors;
    public Vector3 flockCenterOfMass;
}

public class BoidStatsMonitor : MonoBehaviour
{
    [Header("Stats Display")]
    public bool showStats = true;
    public BoidStats currentStats = new BoidStats();
    
    private BoidManager boidManager;
    
    void Start()
    {
        boidManager = FindObjectOfType<BoidManager>();
    }
    
    void Update()
    {
        if (boidManager == null || !showStats) return;
        
        UpdateStats();
    }
    
    private void UpdateStats()
    {
        var activeBoids = boidManager.GetActiveBoids();
        currentStats.activeBoids = activeBoids.Count;
        currentStats.pooledBoids = boidManager.GetAvailableBoidCount();
        
        if (activeBoids.Count > 0)
        {
            float totalSpeed = 0f;
            Vector3 centerOfMass = Vector3.zero;
            
            foreach (var boid in activeBoids)
            {
                totalSpeed += boid.Velocity.magnitude;
                centerOfMass += boid.Position;
            }
            
            currentStats.averageSpeed = totalSpeed / activeBoids.Count;
            currentStats.flockCenterOfMass = centerOfMass / activeBoids.Count;
        }
    }
    
    void OnGUI()
    {
        if (!showStats || boidManager == null) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 150));
        GUILayout.BeginVertical("box");
        //GUILayout.Label("Boid System Stats", EditorStyles.boldLabel);
        GUILayout.Label($"Active Boids: {currentStats.activeBoids}");
        GUILayout.Label($"Pooled Boids: {currentStats.pooledBoids}");
        GUILayout.Label($"Average Speed: {currentStats.averageSpeed:F2}");
        GUILayout.Label($"Center of Mass: {currentStats.flockCenterOfMass:F1}");
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}