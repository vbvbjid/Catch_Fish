using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip gameOver;
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
    public AudioClip success;

    public float acceleration = 5f;
    public float maxSpeed = 10f;
    private float currentSpeed = 0f;
    private Coroutine moveRoutine;
    private float timer = 0;

    [Header("Post-Processing Controller")]
    public PostProcessingController postProcessingController;
    public float levelTransitionTime = 2.0f;
    
    private bool deathEffectTriggered = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        fishCount = 0;
        
        // Initialize post-processing effects for starting level
        if (postProcessingController != null)
        {
            postProcessingController.InitializeLevelEffects(currentLevel);
        }
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
        else 
            currentLevel = 0;
        
        moveRoutine = StartCoroutine(moveToNextLevel(currentLevel));
        fishCount = 0;
    }

    IEnumerator moveToNextLevel(int Level)
    {
        Vector3 targetPosition = levelPosition[Level];
        Debug.Log("targetPos: " + targetPosition);
        float speed = 0f;

        // Start post-processing transition
        if (postProcessingController != null)
        {
            postProcessingController.SetLevelEffects(Level, levelTransitionTime);
        }

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
    }

    public void FadeOut(float duration)
    {
        StartCoroutine(FadeOutCoroutine(duration));
    }
    
    private IEnumerator FadeOutCoroutine(float duration)
    {
        float startVolume = audioSource.volume;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }
        
        audioSource.volume = 0f;
        
        audioSource.clip = audioClips[currentLevel];
        audioSource.volume = 1.0f;
        audioSource.Play();
    }

    public void CatchFish()
    {
        FadeOut(3.0f);
        fishCount++;
        audioSource.PlayOneShot(success);
        life += fishCount * levelPoint[currentLevel];
        if (fishCount >= UngradePoint[currentLevel])
        {
            nextLevel();
        }
    }

    void Update()
    {
        if (life <= 0)
        {
            if (!deathEffectTriggered)
            {
                // Trigger death effect only once
                TriggerDeathSequence();
                audioSource.PlayOneShot(gameOver);
                deathEffectTriggered = true;
            }
            death = true;
            return;
        }

        timer += Time.deltaTime;

        if (timer >= 1.0f)
        {
            life -= decreaseRate[currentLevel];
            timer = 0;
        }
    }

    private void TriggerDeathSequence()
    {
        // Stop all ongoing level transitions
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }

        // Stop all audio immediately
        StopAllAudio();

        // Deactivate all pool objects
        DeactivateAllPools();

        // Trigger the death visual effect
        if (postProcessingController != null)
        {
            postProcessingController.TriggerDeathEffect();
        }

        // Optional: Add death sound, particle effects, etc. here
        Debug.Log("Player has died - triggering death effects");
    }

    public void DeactivateAllPools()
    {
        // Deactivate all pool GameObjects
        foreach (GameObject poolObject in Pool)
        {
            if (poolObject != null)
            {
                poolObject.SetActive(false);
            }
        }
        
        Debug.Log($"Deactivated {Pool.Count} pool objects");
    }

    public void StopAllAudio()
    {
        // Stop the main audio source
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        // Stop all AudioSource components in the scene
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
        foreach (AudioSource source in allAudioSources)
        {
            source.Stop();
        }
    }

    public void PauseAllAudio()
    {
        AudioListener.pause = true;
    }

    public void ResumeAllAudio()
    {
        AudioListener.pause = false;
    }

    public void MuteAllAudio()
    {
        AudioListener.volume = 0f;
    }

    public void UnmuteAllAudio()
    {
        AudioListener.volume = 1f;
    }
}