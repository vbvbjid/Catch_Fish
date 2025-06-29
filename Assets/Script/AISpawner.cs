using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.AI;

[System.Serializable]
public class AIObjects
{
    public string AIGroupName { get { return m_aiGroupName; } }
    public GameObject objPrefab { get { return m_prefab; } }
    public int maxAI { get { return m_maxAI; } }
    public int spawnRate { get { return m_spawnRate; } }
    public int spawnAmount { get { return m_maxSpawnAmount; } }
    public bool randomzieStates { get { return m_randomizeStates; } }
    public bool enableSpawner { get { return m_enableSpawner; } }

    [Header("AI Group Status")]
    [SerializeField]
    private string m_aiGroupName;
    [SerializeField]
    private GameObject m_prefab;
    [SerializeField]
    [Range(0f, 20f)]
    private int m_maxAI;
    [SerializeField]
    [Range(0f, 20f)]
    private int m_spawnRate;
    [SerializeField]
    [Range(0f, 20f)]
    private int m_maxSpawnAmount;
    [Header("Main Settings")]
    [SerializeField]
    private bool m_enableSpawner;
    [SerializeField]
    private bool m_randomizeStates;

    // Object Pool for this AI type
    [System.NonSerialized]
    public Queue<GameObject> objectPool = new Queue<GameObject>();
    [System.NonSerialized]
    public List<GameObject> activeObjects = new List<GameObject>();

    public AIObjects(string Name, GameObject Prefab, int MaxAI, int SpawnRate, int SpawnAmount, bool randomzieStates)
    {
        this.m_aiGroupName = Name;
        this.m_prefab = Prefab;
        this.m_maxAI = MaxAI;
        this.m_spawnRate = SpawnRate;
        this.m_maxSpawnAmount = SpawnAmount;
        this.m_randomizeStates = randomzieStates;
        this.objectPool = new Queue<GameObject>();
        this.activeObjects = new List<GameObject>();
    }

    public void setValues(int MaxAI, int SpawnRate, int SpawnAmount)
    {
        this.m_maxAI = MaxAI;
        this.m_spawnRate = SpawnRate;
        this.m_maxSpawnAmount = SpawnAmount;
    }
}

// Interface for pooled objects to implement
public interface IPoolable
{
    void OnSpawn();
    void OnDespawn();
    void ReturnToPool();
}

public class AISpawner : MonoBehaviour
{
    public List<Transform> Waypoints = new List<Transform>();

    public float spwanTimer { get { return m_SpawnTimer; } }
    public UnityEngine.Vector3 spawnArea { get { return m_SpawnArea; } }
    
    [Header("Global Stats")]
    [SerializeField]
    [Range(0f, 600f)]
    private float m_SpawnTimer;
    [SerializeField]
    private Color m_SpawnColor = new Color(1.0f, 0.0f, 0.0f, 0.3f);
    [SerializeField]
    private UnityEngine.Vector3 m_SpawnArea = new UnityEngine.Vector3(20f, 10f, 20f);
    
    [Header("Object Pool Settings")]
    [SerializeField]
    private int poolSizePerGroup = 50; // How many objects to pre-instantiate per AI group

    [Header("AI Groups Settings")]
    public AIObjects[] AIObjects = new AIObjects[6];

    void Start()
    {
        GetWayPoints();
        RandomiseGroups();
        CreateAIGroups();
        InitializeObjectPools();
        InvokeRepeating("SpawnNPC", 0.5f, spwanTimer);
    }

    void InitializeObjectPools()
    {
        for (int i = 0; i < AIObjects.Length; i++)
        {
            // Check if AIObjects[i] is null or if objPrefab is null
            if (AIObjects[i] == null)
            {
                Debug.LogWarning($"AIObjects[{i}] is null! Please assign it in the inspector.");
                continue;
            }
            
            if (AIObjects[i].objPrefab == null)
            {
                Debug.LogWarning($"AIObjects[{i}].objPrefab is null! Please assign a prefab.");
                continue;
            }
            
            // Initialize the pool and active list if they're null
            if (AIObjects[i].objectPool == null)
                AIObjects[i].objectPool = new Queue<GameObject>();
            if (AIObjects[i].activeObjects == null)
                AIObjects[i].activeObjects = new List<GameObject>();
            
            GameObject groupParent = GameObject.Find(AIObjects[i].AIGroupName);
            if (groupParent == null)
            {
                Debug.LogWarning($"Could not find group parent for {AIObjects[i].AIGroupName}");
                continue;
            }
            
            // Pre-instantiate objects for the pool
            for (int j = 0; j < poolSizePerGroup; j++)
            {
                GameObject pooledObj = Instantiate(AIObjects[i].objPrefab);
                pooledObj.layer = LayerMask.NameToLayer("Water");
                pooledObj.transform.parent = groupParent.transform;
                pooledObj.SetActive(false);
                
                // Add poolable component if it doesn't exist
                if (pooledObj.GetComponent<PoolableAI>() == null)
                {
                    pooledObj.AddComponent<PoolableAI>();
                }
                
                // Set reference to this spawner and AI group index
                pooledObj.GetComponent<PoolableAI>().Initialize(this, i);
                
                AIObjects[i].objectPool.Enqueue(pooledObj);
            }
        }
    }

    void SpawnNPC()
    {
        for (int i = 0; i < AIObjects.Length; i++)
        {
            // Check for null AIObjects
            if (AIObjects[i] == null) continue;
            if (!AIObjects[i].enableSpawner) continue;
            
            // Check if we haven't reached the max active AI limit
            if (AIObjects[i].activeObjects.Count < AIObjects[i].maxAI)
            {
                int spawnCount = Random.Range(1, AIObjects[i].spawnAmount + 1);
                
                for (int j = 0; j < spawnCount; j++)
                {
                    // Stop if we've reached the limit
                    if (AIObjects[i].activeObjects.Count >= AIObjects[i].maxAI) break;
                    
                    SpawnFromPool(i);
                }
            }
        }
    }

    void SpawnFromPool(int aiGroupIndex)
    {
        // Safety checks
        if (aiGroupIndex < 0 || aiGroupIndex >= AIObjects.Length) return;
        if (AIObjects[aiGroupIndex] == null) return;
        
        if (AIObjects[aiGroupIndex].objectPool.Count > 0)
        {
            // Get object from pool
            GameObject spawnObj = AIObjects[aiGroupIndex].objectPool.Dequeue();
            
            // Set random position and rotation
            Quaternion randomRotation = Quaternion.Euler(Random.Range(-20, 20), Random.Range(0, 360), 0);
            spawnObj.transform.position = RandomPosition();
            spawnObj.transform.rotation = randomRotation;
            
            // Activate the object
            spawnObj.SetActive(true);
            
            // Add to active objects list
            AIObjects[aiGroupIndex].activeObjects.Add(spawnObj);
            
            // Call spawn event
            IPoolable poolable = spawnObj.GetComponent<IPoolable>();
            if (poolable != null)
            {
                poolable.OnSpawn();
            }
        }
        else
        {
            Debug.LogWarning($"Object pool for {AIObjects[aiGroupIndex].AIGroupName} is empty!");
        }
    }

    public void ReturnToPool(GameObject obj, int aiGroupIndex)
    {
        if (aiGroupIndex < 0 || aiGroupIndex >= AIObjects.Length) return;
        
        // Remove from active objects
        AIObjects[aiGroupIndex].activeObjects.Remove(obj);
        
        // Call despawn event
        IPoolable poolable = obj.GetComponent<IPoolable>();
        if (poolable != null)
        {
            poolable.OnDespawn();
        }
        
        // Deactivate and return to pool
        obj.SetActive(false);
        AIObjects[aiGroupIndex].objectPool.Enqueue(obj);
    }

    // Method to manually despawn a specific object (useful for respawning)
    public void DespawnObject(GameObject obj)
    {
        PoolableAI poolable = obj.GetComponent<PoolableAI>();
        if (poolable != null)
        {
            poolable.ReturnToPool();
        }
    }

    // Method to respawn all objects of a specific AI group
    public void RespawnAIGroup(int aiGroupIndex)
    {
        if (aiGroupIndex < 0 || aiGroupIndex >= AIObjects.Length) return;
        
        // Return all active objects to pool
        List<GameObject> objectsToReturn = new List<GameObject>(AIObjects[aiGroupIndex].activeObjects);
        foreach (GameObject obj in objectsToReturn)
        {
            ReturnToPool(obj, aiGroupIndex);
        }
        
        // Clear the active objects list
        AIObjects[aiGroupIndex].activeObjects.Clear();
    }

    // Method to respawn all AI groups
    public void RespawnAllAI()
    {
        for (int i = 0; i < AIObjects.Length; i++)
        {
            RespawnAIGroup(i);
        }
    }

    public Vector3 RandomPosition()
    {
        Vector3 randomPos = new Vector3(
            Random.Range(-spawnArea.x, spawnArea.x),
            Random.Range(-spawnArea.y, spawnArea.y),
            Random.Range(-spawnArea.z, spawnArea.z)
        );
        randomPos = transform.TransformPoint(randomPos * .5f);
        return randomPos;
    }

    public Vector3 RandomWayPoint()
    {
        int randomWP = Random.Range(0, Waypoints.Count);
        Vector3 randomWayPoint = Waypoints[randomWP].transform.position;
        return randomWayPoint;
    }
    
    void RandomiseGroups()
    {
        for (int i = 0; i < AIObjects.Length; i++)
        {
            // Skip null AIObjects
            if (AIObjects[i] == null) continue;
            
            if (AIObjects[i].randomzieStates)
            {
                AIObjects[i].setValues(Random.Range(1, 30), Random.Range(1, 20), Random.Range(1, 10));
            }
        }
    }

    void CreateAIGroups()
    {
        for (int i = 0; i < AIObjects.Length; i++)
        {
            // Skip null AIObjects
            if (AIObjects[i] == null)
            {
                Debug.LogWarning($"AIObjects[{i}] is null! Skipping group creation.");
                continue;
            }

            if (string.IsNullOrEmpty(AIObjects[i].AIGroupName))
            {
                Debug.LogWarning($"AIObjects[{i}] has empty AIGroupName! Skipping group creation.");
                continue;
            }

            GameObject AIGroupSpawn;
            AIGroupSpawn = new GameObject(AIObjects[i].AIGroupName);
            AIGroupSpawn.transform.parent = this.gameObject.transform;
        }
    }

    void GetWayPoints()
    {
        Transform[] wpList = this.transform.GetComponentsInChildren<Transform>();
        for (int i = 0; i < wpList.Length; i++)
        {
            if (wpList[i].tag == "WayPoint")
            {
                Waypoints.Add(wpList[i]);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = m_SpawnColor;
        Gizmos.DrawCube(transform.position, spawnArea);   
    }

    // Debug methods to show pool status
    public void LogPoolStatus()
    {
        for (int i = 0; i < AIObjects.Length; i++)
        {
            Debug.Log($"{AIObjects[i].AIGroupName}: Active: {AIObjects[i].activeObjects.Count}, Pool: {AIObjects[i].objectPool.Count}");
        }
    }
}