using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Audio System")]
    public AudioSource audioSource;
    public AudioClip gameOverSound;
    public AudioClip successSound;
    public List<AudioClip> levelAudioClips = new List<AudioClip>();

    [Header("Player & Life System")]
    public GameObject player;
    public float life = 10.0f;
    private float timer = 0f;

    [Header("Fish & Level System")]
    public int fishCount;
    public int currentLevel = 0;
    public bool gameEnded = false;
    public bool playerDead = false;

    [Header("Level Configuration")]
    [Tooltip("Life points gained per fish caught (multiplied by fish count)")]
    public List<float> lifePointsPerFish = new List<float>();
    [Tooltip("Life decrease rate per second for each level")]
    public List<float> lifeDecreaseRates = new List<float>();
    [Tooltip("Player positions for each level")]
    public List<Vector3> levelPositions = new List<Vector3>();
    [Tooltip("Number of fish required to advance to next level")]
    public List<int> fishRequiredForUpgrade = new List<int>();

    [Header("Fish Pool Management")]
    [Tooltip("Fish spawner pools for each level")]
    public List<GameObject> fishSpawnerPools = new List<GameObject>();

    [Header("Level Transition")]
    public float acceleration = 5f;
    public float maxSpeed = 10f;
    public float levelTransitionTime = 2.0f;

    [Header("Post-Processing")]
    public PostProcessingController postProcessingController;

    // Private state variables
    private Coroutine levelTransitionCoroutine;
    private bool deathEffectTriggered = false;

    #region Unity Lifecycle

    void Start()
    {
        InitializeGame();
    }

    void Update()
    {
        if (playerDead) return;

        UpdateLifeSystem();
        CheckForDeath();
    }

    #endregion

    #region Initialization

    private void InitializeGame()
    {
        // Initialize audio source
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Reset game state
        fishCount = 0;
        playerDead = false;
        gameEnded = false;
        deathEffectTriggered = false;

        // Initialize post-processing effects for starting level
        if (postProcessingController != null)
        {
            postProcessingController.InitializeLevelEffects(currentLevel);
        }
        fishSpawnerPools[0].SetActive(true);
        Debug.Log($"Game initialized at level {currentLevel}", this);
    }

    #endregion

    #region Life System

    private void UpdateLifeSystem()
    {
        timer += Time.deltaTime;

        if (timer >= 1.0f)
        {
            DecreaseLife();
            timer = 0f;
        }
    }

    private void DecreaseLife()
    {
        if (currentLevel < lifeDecreaseRates.Count)
        {
            life -= lifeDecreaseRates[currentLevel];
            
            if (life < 0)
                life = 0;
        }
    }

    private void CheckForDeath()
    {
        if (life <= 0 && !deathEffectTriggered)
        {
            TriggerDeathSequence();
        }
    }

    #endregion

    #region Fish Catching System

    public void CatchFish()
    {
        fishCount++;
        
        // Play success sound
        if (audioSource != null && successSound != null)
        {
            audioSource.PlayOneShot(successSound, 2.0f);
        }

        // Add life points based on current level and fish count
        AddLifePoints();

        Debug.Log($"Fish caught! Count: {fishCount}, Life: {life:F1}", this);

        // Check if we should advance to next level
        CheckForLevelUpgrade();
    }

    private void AddLifePoints()
    {
        if (currentLevel < lifePointsPerFish.Count)
        {
            float pointsToAdd = fishCount * lifePointsPerFish[currentLevel];
            life += pointsToAdd;
            
            Debug.Log($"Added {pointsToAdd:F1} life points", this);
        }
    }

    private void CheckForLevelUpgrade()
    {
        if (currentLevel < fishRequiredForUpgrade.Count && 
            fishCount >= fishRequiredForUpgrade[currentLevel])
        {
            AdvanceToNextLevel();
        }
    }

    #endregion

    #region Level Management

    private void AdvanceToNextLevel()
    {
        Debug.Log($"Advancing from level {currentLevel} to next level", this);

        // Start audio fade
        FadeOutAudio(3.0f);

        // Move player to next level position
        if (currentLevel < levelPositions.Count)
        {
            player.transform.position = levelPositions[currentLevel];
        }

        // Manage fish spawners
        ManageFishSpawnersForLevelChange();

        // Update level index (cycle back to 0 if at max level)
        int nextLevel = (currentLevel + 1) % levelPositions.Count;
        if (nextLevel == 0 && currentLevel == levelPositions.Count - 1)
        {
            Debug.Log("Completed all levels, cycling back to level 0", this);
        }

        currentLevel = nextLevel;

        // Start level transition
        levelTransitionCoroutine = StartCoroutine(TransitionToLevel(currentLevel));

        // Reset fish count for new level
        fishCount = 0;
    }

    private void ManageFishSpawnersForLevelChange()
    {
        // Stop and clear current level's spawner
        if (currentLevel < fishSpawnerPools.Count && fishSpawnerPools[currentLevel] != null)
        {
            var spawner = fishSpawnerPools[currentLevel].GetComponent<SimpleFishSpawner>();
            if (spawner != null)
            {
                spawner.StopSpawning();
                spawner.ClearSpawnedFish();
            }
        }
    }

    private IEnumerator TransitionToLevel(int targetLevel)
    {
        if (targetLevel >= levelPositions.Count)
        {
            Debug.LogError($"Invalid target level: {targetLevel}", this);
            yield break;
        }

        Vector3 targetPosition = levelPositions[targetLevel];
        Debug.Log($"Transitioning to level {targetLevel} at position: {targetPosition}", this);

        // Start post-processing transition
        if (postProcessingController != null)
        {
            postProcessingController.SetLevelEffects(targetLevel, levelTransitionTime);
        }

        // Move player to target Y position
        yield return StartCoroutine(MovePlayerToHeight(targetPosition.y));

        // Activate new level's spawner
        ActivateLevelSpawner(targetLevel);
    }

    private IEnumerator MovePlayerToHeight(float targetY)
    {
        float currentSpeed = 0f;

        while (Mathf.Abs(player.transform.position.y - targetY) > 0.01f)
        {
            // Accelerate up to maxSpeed
            currentSpeed += acceleration * Time.deltaTime;
            currentSpeed = Mathf.Min(currentSpeed, maxSpeed);

            // Only move Y, keep X and Z the same
            Vector3 currentPos = player.transform.position;
            float newY = Mathf.MoveTowards(currentPos.y, targetY, currentSpeed * Time.deltaTime);
            player.transform.position = new Vector3(currentPos.x, newY, currentPos.z);

            yield return null;
        }

        // Snap exactly to final Y position
        Vector3 finalPos = player.transform.position;
        player.transform.position = new Vector3(finalPos.x, targetY, finalPos.z);
    }

    private void ActivateLevelSpawner(int level)
    {
        if (level < fishSpawnerPools.Count && fishSpawnerPools[level] != null)
        {
            fishSpawnerPools[level].SetActive(true);
            Debug.Log($"Activated spawner for level {level}", this);
        }
        else
        {
            Debug.LogWarning($"No spawner found for level {level}", this);
        }
    }

    #endregion

    #region Audio Management

    private void FadeOutAudio(float duration)
    {
        StartCoroutine(FadeOutAudioCoroutine(duration));
    }
    
    private IEnumerator FadeOutAudioCoroutine(float duration)
    {
        if (audioSource == null) yield break;

        float startVolume = audioSource.volume;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }
        
        audioSource.volume = 0f;

        // Switch to new level's audio
        PlayLevelAudio();
    }

    private void PlayLevelAudio()
    {
        if (audioSource == null) return;

        if (currentLevel < levelAudioClips.Count && levelAudioClips[currentLevel] != null)
        {
            audioSource.clip = levelAudioClips[currentLevel];
            audioSource.volume = 1.0f;
            audioSource.Play();
        }
    }

    public void StopAllAudio()
    {
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

    #endregion

    #region Death System

    private void TriggerDeathSequence()
    {
        deathEffectTriggered = true;
        playerDead = true;

        Debug.Log("Player has died - triggering death effects", this);

        // Stop all ongoing processes
        StopOngoingProcesses();

        // Play death sound
        if (audioSource != null && gameOverSound != null)
        {
            audioSource.PlayOneShot(gameOverSound);
        }

        // Trigger visual death effects
        if (postProcessingController != null)
        {
            postProcessingController.TriggerDeathEffect();
        }

        // Deactivate all spawners
        DeactivateAllSpawners();
    }

    private void StopOngoingProcesses()
    {
        // Stop level transition
        if (levelTransitionCoroutine != null)
        {
            StopCoroutine(levelTransitionCoroutine);
            levelTransitionCoroutine = null;
        }

        // Stop all audio
        StopAllAudio();
    }

    public void DeactivateAllSpawners()
    {
        foreach (GameObject spawnerPool in fishSpawnerPools)
        {
            if (spawnerPool != null)
            {
                spawnerPool.SetActive(false);
            }
        }
        
        Debug.Log($"Deactivated {fishSpawnerPools.Count} spawner pools", this);
    }

    #endregion

    #region Public Properties

    public bool IsPlayerDead => playerDead;
    public bool IsGameEnded => gameEnded;
    public float CurrentLife => life;
    public int CurrentFishCount => fishCount;
    public int CurrentLevel => currentLevel;

    #endregion

    #region Debug Methods

    [ContextMenu("Force Next Level")]
    public void ForceNextLevel()
    {
        if (Application.isPlaying && !playerDead)
        {
            AdvanceToNextLevel();
        }
    }

    [ContextMenu("Add Life")]
    public void AddLife()
    {
        if (Application.isPlaying)
        {
            life += 10f;
            Debug.Log($"Added 10 life. Current life: {life}", this);
        }
    }

    [ContextMenu("Simulate Fish Catch")]
    public void SimulateFishCatch()
    {
        if (Application.isPlaying && !playerDead)
        {
            CatchFish();
        }
    }

    #endregion
}