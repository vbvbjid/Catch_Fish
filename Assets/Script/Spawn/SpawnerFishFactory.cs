using UnityEngine;

public class SpawnerFishFactory : MonoBehaviour
{
    [Header("Fish Creation")]
    public GameObject fishPrefab;
    public Transform parentContainer; // Optional container for organization

    [Header("Speed Randomization")]
    [Tooltip("Randomize initial speeds")]
    public bool randomizeInitialSpeeds = true;
    [Tooltip("±n degrees/second from base speed")]
    public float speedRandomRange = 10f; // ±10 degrees/second from base speed

    [Header("Debug")]
    public bool debugFishCreation = false;

    public GameObject CreateFish(FishSpawnParameters parameters, GameObject pooledFish = null)
    {
        if (fishPrefab == null)
        {
            Debug.LogError("Fish prefab is not assigned!");
            return null;
        }

        GameObject newFish = null;

        // Use pooled fish if provided, otherwise create new
        if (pooledFish != null)
        {
            newFish = pooledFish;
            if (debugFishCreation)
                Debug.Log($"Reusing pooled fish: {newFish.name}", this);
        }
        else
        {
            // Create new fish
            newFish = Instantiate(fishPrefab);
            if (debugFishCreation)
                Debug.Log("Created new fish instance", this);
        }

        // Set parent if specified
        if (parentContainer != null)
        {
            newFish.transform.SetParent(parentContainer);
        }

        // Configure the fish
        ConfigureFish(newFish, parameters);

        return newFish;
    }

    private void ConfigureFish(GameObject fish, FishSpawnParameters parameters)
    {
        // Check if fish is in recovery before configuring
        FishAI fishAI = fish.GetComponent<FishAI>();
        if (fishAI != null && fishAI.CurrentState == FishStateMachine.FishState.Recovering)
        {
            Debug.LogWarning($"[SPAWN DEBUG] ConfigureFish called on fish '{fish.name}' that is in RECOVERY state! This will interfere with recovery.", this);
        }

        // Calculate initial position based on orbit parameters
        Vector3 initialPosition = CalculateOrbitPosition(
            parameters.orbitCenter, 
            parameters.orbitRadius, 
            parameters.initialAngle
        );
        
        // IMPORTANT: Reset fish position and rotation BEFORE configuring AI
        Vector3 oldPosition = fish.transform.position;
        fish.transform.position = initialPosition;
       
        
        // Calculate correct rotation based on orbital movement direction
        SetCorrectRotation(fish, parameters.initialAngle);
        
        fish.SetActive(true);

        // Configure the FishAI component
        if (fishAI != null)
        {
            ConfigureFishAI(fishAI, parameters);
        }
        else
        {
            Debug.LogWarning($"Fish prefab does not have FishAI component: {fish.name}", this);
        }

        // Name the fish for easier identification (using base values for consistency)
        fish.name = GenerateFishName(parameters);

        if (debugFishCreation)
            Debug.Log($"Configured fish: {fish.name} at position {initialPosition}", this);
    }


    private void ConfigureFishAI(FishAI fishAI, FishSpawnParameters parameters)
    {

        // Set spawner reference for pool management
        SimpleFishSpawner spawner = GetComponentInParent<SimpleFishSpawner>();
        if (spawner == null)
            spawner = GetComponent<SimpleFishSpawner>();
        
        fishAI.spawner = spawner;
        
        // Configure the movement component directly (since FishAI was refactored)
        FishMovement fishMovement = fishAI.GetComponent<FishMovement>();
        if (fishMovement == null)
        {
            Debug.LogError($"FishMovement component not found on {fishAI.name}!", this);
            return;
        }

        // Set orbit parameters directly on the movement component
        Vector3 oldOrbitCenter = fishMovement.orbitCenter;
        float oldOrbitRadius = fishMovement.orbitRadius;
        float oldAngle = fishMovement.CurrentAngle;
        
        fishMovement.orbitCenter = parameters.orbitCenter;
        fishMovement.orbitRadius = parameters.orbitRadius;
        fishMovement.CurrentAngle = parameters.initialAngle;

        // Randomize initial speed if enabled
        if (randomizeInitialSpeeds)
        {
            fishMovement.ResetSpeed(); // This should set a random speed within range
        }

        // Configure vertical movement component
        FishVerticalMovement verticalMovement = fishAI.GetComponent<FishVerticalMovement>();
        if (verticalMovement != null)
        {
            verticalMovement.SetBaseHeight(parameters.orbitCenter.y);
        }
        
        fishAI.ResetFishState();
        
        fishAI.ForcePositionUpdate();
    }

    private void SetCorrectRotation(GameObject fish, float angle)
    {
        float radians = angle * Mathf.Deg2Rad;
        Vector3 tangentDirection = new Vector3(-Mathf.Sin(radians), 0f, Mathf.Cos(radians));
        
        if (tangentDirection.magnitude > 0.1f)
        {
            Quaternion correctRotation = Quaternion.LookRotation(tangentDirection);
            fish.transform.rotation = correctRotation;
        }
        else
        {
            fish.transform.rotation = Quaternion.identity;
        }
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

    private string GenerateFishName(FishSpawnParameters parameters)
    {
        return $"Fish_Y{parameters.baseY:F1}_R{parameters.baseRadius:F1}_A{parameters.baseAngle:F0}";
    }
}