using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public AudioSource audioSource;
    public List<AudioClip> audioClips = new List<AudioClip>();
    public GameObject Player;
    public float life = 10.0f;
    public int fishCount;
    public bool death = false;
    public int currentLevel = 0;
    public bool End = false;
    public List<float> levelPoint = new List<float>();
    public List<float> decreaseRate = new List<float>();
    public List<Vector3> levelPosition = new List<Vector3>();
    public List<GameObject> Pool = new List<GameObject>();
    [Tooltip("Number of fish to catch in order to move to next level")]
    public List<int> UngradePoint = new List<int>();

    public float acceleration = 5f;   // How quickly speed increases
    public float maxSpeed = 10f;      // Top speed
    private float currentSpeed = 0f;
    private Coroutine moveRoutine;
    private float timer = 0;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        fishCount = 0;
    }
    void nextLevel()
    {
        Player.transform.position = levelPosition[currentLevel];
        if (currentLevel != 2)
        {
            Pool[currentLevel].GetComponent<SimpleFishSpawner>().StopSpawning();
            Pool[currentLevel].GetComponent<SimpleFishSpawner>().ClearSpawnedFish();
            currentLevel++;
        }
        else currentLevel = 0;
        moveRoutine = StartCoroutine(moveToNextLevel(currentLevel));
        fishCount = 0;
    }
    IEnumerator moveToNextLevel(int Level)
    {
        Vector3 targetPosition = levelPosition[Level];
        Debug.Log("targetPos: " + targetPosition);
        float speed = 0f;

        while (Mathf.Abs(Player.transform.position.y - targetPosition.y) > 0.01f)
        {
            // Accelerate up to maxSpeed
            speed += acceleration * Time.deltaTime;
            speed = Mathf.Min(speed, maxSpeed);

            // Only move Y, keep X and Z the same
            Vector3 current = Player.transform.position;
            float newY = Mathf.MoveTowards(current.y, targetPosition.y, speed * Time.deltaTime);
            Player.transform.position = new Vector3(current.x, newY, current.z);

            yield return null; // Wait for the next frame
        }

        // Snap exactly to final Y
        Player.transform.position = new Vector3(
            Player.transform.position.x,
            targetPosition.y,
            Player.transform.position.z
        );

        Pool[currentLevel].SetActive(true);
        audioSource.clip = audioClips[currentLevel];
        audioSource.Play();
    }
    public void CatchFish()
    {
        fishCount++;
        life += fishCount * levelPoint[currentLevel];
        if (fishCount >= UngradePoint[currentLevel])
        {
            nextLevel();
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (life <= 0)
        {
            death = true;
            return; // No need to process further
        }

        timer += Time.deltaTime;

        if (timer >= 1.0f)
        {
            life -= decreaseRate[currentLevel];
            timer = 0;
        }
    }
}
