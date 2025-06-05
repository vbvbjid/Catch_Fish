using UnityEngine;

// Example implementation showing how to use the OrbitalObjectPool
public class OrbitalPoolExample : MonoBehaviour
{
    [SerializeField] private OrbitalObjectPool objectPool;
    [SerializeField] private int initialSpawnCount = 5;
    [SerializeField] private GameObject orbitalPrefab;
    
    // UI elements references (if needed)
    [SerializeField] private TMPro.TextMeshProUGUI activeCountText;
    
    private void Start()
    {
        // If pool component not assigned, try to find it
        if (objectPool == null)
        {
            objectPool = FindObjectOfType<OrbitalObjectPool>();
            
            // If still not found, add it to this GameObject
            if (objectPool == null && orbitalPrefab != null)
            {
                objectPool = gameObject.AddComponent<OrbitalObjectPool>();
                
                // Get the OrbitalObjectPool component via reflection and set the prefab
                var serializedObject = new UnityEditor.SerializedObject(objectPool);
                var prefabProperty = serializedObject.FindProperty("orbitalObjectPrefab");
                prefabProperty.objectReferenceValue = orbitalPrefab;
                serializedObject.ApplyModifiedProperties();
            }
        }
        
        if (objectPool != null)
        {
            // Spawn initial objects
            objectPool.SpawnObjects(initialSpawnCount);
            UpdateUI();
        }
        else
        {
            Debug.LogError("Object Pool not found and could not be created!");
        }
    }
    
    // Example method to spawn a single object
    public void SpawnSingleObject()
    {
        if (objectPool != null)
        {
            objectPool.GetPooledObject();
            UpdateUI();
        }
    }
    
    // Example method to spawn multiple objects
    public void SpawnMultipleObjects(int count)
    {
        if (objectPool != null)
        {
            objectPool.SpawnObjects(count);
            UpdateUI();
        }
    }
    
    // Example method to return all objects to pool
    public void ReturnAllToPool()
    {
        if (objectPool != null)
        {
            objectPool.ReturnAllToPool();
            UpdateUI();
        }
    }
    
    // Update UI with active object count (if UI elements are set up)
    private void UpdateUI()
    {
        if (activeCountText != null)
        {
            int activeCount = 0;
            foreach (Transform child in objectPool.transform)
            {
                if (child.gameObject.activeInHierarchy)
                {
                    activeCount++;
                }
            }
            
            activeCountText.text = $"Active Objects: {activeCount}";
        }
    }
}