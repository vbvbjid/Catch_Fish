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

    public AIObjects(string Name, GameObject Prefab, int MaxAI, int SpawnRate, int SpawnAmount, bool randomzieStates)
    {
        this.m_aiGroupName = Name;
        this.m_prefab = Prefab;
        this.m_maxAI = MaxAI;
        this.m_spawnRate = SpawnRate;
        this.m_maxSpawnAmount = SpawnAmount;
        this.m_randomizeStates = randomzieStates;
    }
    public void setValues(int MaxAI, int SpawnRate, int SpawnAmount)
    {
        this.m_maxAI = MaxAI;
        this.m_spawnRate = SpawnRate;
        this.m_maxSpawnAmount = SpawnAmount;
    }
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

    [Header("AI Groups Settings")]
    public AIObjects[] AIObjects = new AIObjects[6];

    void Start()
    {
        GetWayPoints();
        RandomiseGroups();
        CreateAIGroups();
        InvokeRepeating("SpawnNPC", 0.5f, spwanTimer);
    }
    void Update()
    {

    }
    void SpawnNPC()
    {
        for (int i = 0; i < AIObjects.Count(); i++)
        {
            GameObject tempGroup = GameObject.Find(AIObjects[i].AIGroupName);
            if (tempGroup.GetComponentInChildren<Transform>().childCount < AIObjects[i].maxAI)
            {
                for (int j = 0; j < Random.Range(0, AIObjects[i].spawnAmount); j++)
                {
                    Quaternion randomRotation = Quaternion.Euler(Random.Range(-20, 20), Random.Range(0, 360), 0);
                    GameObject tempSpawn;
                    tempSpawn = Instantiate(AIObjects[i].objPrefab, RandomPosition(), randomRotation);
                    tempSpawn.transform.parent = tempGroup.transform;
                    tempSpawn.AddComponent<AIMove>();
                }
            }
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
        int randomWP = Random.Range(0, (Waypoints.Count - 1));
        Vector3 randomWayPoint = Waypoints[randomWP].transform.position;
        return randomWayPoint;
    }
    
    void RandomiseGroups()
    {
        for (int i = 0; i < AIObjects.Count(); i++)
        {
            if (AIObjects[i].randomzieStates)
            {
                //     AIObjects[i] = new AIObjects(AIObjects[i].AIGroupName,
                //                                 AIObjects[i].objPrefab,
                //                                 Random.Range(1, 30),
                //                                 Random.Range(1, 20),
                //                                 Random.Range(1, 10),
                //                                 AIObjects[i].randomzieStates);
                AIObjects[i].setValues(Random.Range(1, 30), Random.Range(1, 20), Random.Range(1, 10));
            }
        }
    }

    void CreateAIGroups()
    {
        for (int i = 0; i < AIObjects.Count(); i++)
        {
            GameObject AIGroupSpawn;
            AIGroupSpawn = new GameObject(AIObjects[i].AIGroupName);
            AIGroupSpawn.transform.parent = this.gameObject.transform;
        }
    }
    // void GetWaypointsLinq()
    // {
    //     WaypointsLinq = GetComponentsInChildren<Transform>() // ✅ gets all children
    //         .Where(c => c.CompareTag("WayPoint"))             // ✅ filters by tag
    //         .ToArray();
    // }

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
    void ODrawGizmosSelected()
    {
        Gizmos.color = m_SpawnColor;
        Gizmos.DrawCube(transform.position, spawnArea);   
    }
}