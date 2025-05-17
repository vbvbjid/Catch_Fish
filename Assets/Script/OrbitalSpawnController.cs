using UnityEngine;

// A simple spawner script that demonstrates how to use the OrbitalObjectPool in a more automated way
public class OrbitalSpawnController : MonoBehaviour
{
    [SerializeField] private OrbitalObjectPool objectPool;
    
    [Header("Automatic Spawning")]
    [SerializeField] private bool autoSpawnEnabled = true;
    [SerializeField] private float spawnInterval = 1.5f;
    [SerializeField] private int maxActiveObjects = 8;
    
    [Header("Automatic Despawning")]
    [SerializeField] private bool autoDespawnEnabled = true;
    [SerializeField] private float despawnInterval = 3.0f;
    
    private float nextSpawnTime = 0f;
    private float nextDespawnTime = 0f;
    
    private void Start()
    {
        // If the object pool wasn't assigned, try to find it
        if (objectPool == null)
        {
            objectPool = FindObjectOfType<OrbitalObjectPool>();
            if (objectPool == null)
            {
                Debug.LogError("OrbitalObjectPool not found. Please assign it in the inspector.");
                enabled = false;
                return;
            }
        }
    }
    
    private void Update()
    {
        // Handle automatic spawning
        if (autoSpawnEnabled && Time.time >= nextSpawnTime)
        {
            // Check how many objects are currently active
            int activeCount = CountActiveObjects();
            
            // Only spawn if we haven't reached the max
            if (activeCount < maxActiveObjects)
            {
                objectPool.GetPooledObject();
                Debug.Log($"Spawned object. Active count: {CountActiveObjects()}");
            }
            
            // Set next spawn time
            nextSpawnTime = Time.time + spawnInterval;
        }
        
        // Handle automatic despawning
        if (autoDespawnEnabled && Time.time >= nextDespawnTime)
        {
            GameObject objectToDespawn = FindRandomActiveObject();
            if (objectToDespawn != null)
            {
                objectPool.ReturnToPool(objectToDespawn);
                Debug.Log($"Despawned object. Active count: {CountActiveObjects()}");
            }
            
            // Set next despawn time
            nextDespawnTime = Time.time + despawnInterval;
        }
    }
    
    // Count how many objects are active in the pool
    private int CountActiveObjects()
    {
        int count = 0;
        
        foreach (Transform child in objectPool.transform)
        {
            if (child.gameObject.activeInHierarchy)
            {
                count++;
            }
        }
        
        return count;
    }
    
    // Find a random active object to despawn
    private GameObject FindRandomActiveObject()
    {
        // Collect all active objects
        GameObject[] activeObjects = new GameObject[CountActiveObjects()];
        int index = 0;
        
        foreach (Transform child in objectPool.transform)
        {
            if (child.gameObject.activeInHierarchy)
            {
                activeObjects[index] = child.gameObject;
                index++;
            }
        }
        
        // Return a random active object if any exist
        if (activeObjects.Length > 0)
        {
            return activeObjects[Random.Range(0, activeObjects.Length)];
        }
        
        return null;
    }
    
    // Public methods for button interactions
    
    public void SpawnObject()
    {
        objectPool.GetPooledObject();
    }
    
    public void DespawnAllObjects()
    {
        objectPool.ReturnAllToPool();
    }
    
    public void ToggleAutoSpawn()
    {
        autoSpawnEnabled = !autoSpawnEnabled;
    }
    
    public void ToggleAutoDespawn()
    {
        autoDespawnEnabled = !autoDespawnEnabled;
    }
}