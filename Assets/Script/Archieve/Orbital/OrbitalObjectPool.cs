using System.Collections.Generic;
using UnityEngine;

public class OrbitalObjectPool : MonoBehaviour
{
    [Header("Pool Settings")]
    [SerializeField] private GameObject orbitalObjectPrefab;
    [SerializeField] private int poolSize = 10;
    [SerializeField] private Transform poolParent;

    [Header("Center Settings")]
    [SerializeField] private Vector2 centerXZ = Vector2.zero;
    [SerializeField] private float minYHeight = 0.2f;
    [SerializeField] private float maxYHeight = 2.0f;
    [SerializeField] private float yHeightInterval = 0.2f;

    [Header("Orbit Settings")]
    [SerializeField] private float minRadius = 1.0f;
    [SerializeField] private float maxRadius = 2.0f;
    [SerializeField] private float radiusInterval = 0.2f;
    [SerializeField] private float angleInterval = 20.0f;
    [SerializeField] private float orbitSpeed = 50.0f;

    // Pool of orbital objects
    private List<GameObject> objectPool = new List<GameObject>();
    private List<Vector3> centerPositions = new List<Vector3>();
    private List<float> radiusValues = new List<float>();
    private List<float> startingAngles = new List<float>();

    private void Awake()
    {
        // Create pool parent if not assigned
        if (poolParent == null)
        {
            GameObject poolParentObj = new GameObject("Orbital Object Pool");
            poolParent = poolParentObj.transform;
        }

        // Generate uniformly distributed center positions, radii, and angles
        GenerateUniformDistributions();

        // Initialize object pool
        InitializePool();
    }

    private void GenerateUniformDistributions()
    {
        // Calculate possible Y heights (using interval)
        int ySteps = Mathf.FloorToInt((maxYHeight - minYHeight) / yHeightInterval) + 1;
        for (int i = 0; i < ySteps; i++)
        {
            float yPos = minYHeight + (i * yHeightInterval);
            if (yPos <= maxYHeight)
            {
                centerPositions.Add(new Vector3(centerXZ.x, yPos, centerXZ.y));
            }
        }

        // Calculate possible radius values (using interval)
        int radiusSteps = Mathf.FloorToInt((maxRadius - minRadius) / radiusInterval) + 1;
        for (int i = 0; i < radiusSteps; i++)
        {
            float radius = minRadius + (i * radiusInterval);
            if (radius <= maxRadius)
            {
                radiusValues.Add(radius);
            }
        }

        // Calculate possible angles (using interval)
        int angleSteps = Mathf.FloorToInt(360f / angleInterval);
        for (int i = 0; i < angleSteps; i++)
        {
            startingAngles.Add(i * angleInterval);
        }

        // Shuffle the lists for more randomness while maintaining uniform distribution
        ShuffleList(centerPositions);
        ShuffleList(radiusValues);
        ShuffleList(startingAngles);
    }

    private void ShuffleList<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = CreateOrbitalObject();
            obj.SetActive(false);
            objectPool.Add(obj);
        }
    }

    private GameObject CreateOrbitalObject()
    {
        GameObject obj = Instantiate(orbitalObjectPrefab, Vector3.zero, Quaternion.identity, poolParent);
        OrbitalTangentMovement orbitalMovement = obj.GetComponent<OrbitalTangentMovement>();

        if (orbitalMovement != null)
        {
            // Configure orbital movement component with uniform distributed values
            ConfigureOrbitalMovement(orbitalMovement);
        }
        else
        {
            Debug.LogError("Prefab must have OrbitalTangentMovement component!");
        }

        return obj;
    }

    private void ConfigureOrbitalMovement(OrbitalTangentMovement orbitalMovement)
    {
        Debug.Log("config start");
        // Select values from our pre-generated uniform distributions
        Vector3 centerPos = GetNextCenterPosition();
        float radius = GetNextRadius();
        float startAngle = GetNextAngle();

        // Configure the orbital component
        orbitalMovement.useCenterTransform = false;
        orbitalMovement.centerPosition = centerPos;
        orbitalMovement.orbitRadius = radius;
        orbitalMovement.startingAngle = startAngle;
        orbitalMovement.orbitSpeed = orbitSpeed;
        
        // Keep other settings as default from original script
        orbitalMovement.useLocalSpace = false;
        orbitalMovement.minSpeed = 10f;  
        orbitalMovement.maxSpeed = 50f;
        orbitalMovement.minCycleTime = 3f;
        orbitalMovement.maxCycleTime = 8f;
        orbitalMovement.useRandomCycles = true;
        orbitalMovement.tangentLength = 2f;
    }

    private Vector3 GetNextCenterPosition()
    {
        if (centerPositions.Count == 0)
        {
            // Regenerate if we've used all positions
            GenerateUniformDistributions();
        }
        
        Vector3 pos = centerPositions[0];
        centerPositions.RemoveAt(0);
        return pos;
    }

    private float GetNextRadius()
    {
        if (radiusValues.Count == 0)
        {
            // Regenerate if we've used all radii
            GenerateUniformDistributions();
        }
        
        float radius = radiusValues[0];
        radiusValues.RemoveAt(0);
        return radius;
    }

    private float GetNextAngle()
    {
        if (startingAngles.Count == 0)
        {
            // Regenerate if we've used all angles
            GenerateUniformDistributions();
        }
        
        float angle = startingAngles[0];
        startingAngles.RemoveAt(0);
        return angle;
    }

    // Get an inactive object from the pool
    public GameObject GetPooledObject()
    {
        // First try to find an inactive object
        foreach (GameObject obj in objectPool)
        {
            if (!obj.activeInHierarchy)
            {
                obj.SetActive(true);
                return obj;
            }
        }

        // If all objects are active and we need another one, create a new one
        GameObject newObj = CreateOrbitalObject();
        objectPool.Add(newObj);
        return newObj;
    }

    // Return an object to the pool
    public void ReturnToPool(GameObject obj)
    {
        Debug.Log("return pool");
        obj.SetActive(false);
        
        // Reset position and rotation
        obj.transform.position = Vector3.zero;
        obj.transform.rotation = Quaternion.identity;
        
        // Reconfigure the orbital settings for next use
        OrbitalTangentMovement orbitalMovement = obj.GetComponent<OrbitalTangentMovement>();
        if (orbitalMovement != null)
        {
            ConfigureOrbitalMovement(orbitalMovement);
        }
    }

    // Spawn a given number of objects from the pool
    public List<GameObject> SpawnObjects(int count)
    {
        List<GameObject> spawnedObjects = new List<GameObject>();
        
        for (int i = 0; i < count; i++)
        {
            GameObject obj = GetPooledObject();
            spawnedObjects.Add(obj);
        }
        
        return spawnedObjects;
    }

    // Deactivate all objects and return them to the pool
    public void ReturnAllToPool()
    {
        foreach (GameObject obj in objectPool)
        {
            if (obj.activeInHierarchy)
            {
                ReturnToPool(obj);
            }
        }
    }
}