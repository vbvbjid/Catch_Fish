using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;
#if UNITY_EDITOR
using UnityEditor;
#endif
public class FishAI_old : MonoBehaviour
{
    public GameManager gm;
    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip success;
    public AudioClip struggle;
    public AudioClip flee;
    public AudioClip swim;

    [Header("Orbit Settings")]
    public Vector3 orbitCenter;
    public float orbitRadius = 3f;
    public float baseOrbitSpeed = 30f; // degrees per second
    public float speedVariationRange = 1.5f;
    public float speedChangeRate = 0.8f;
    public float minSpeed = 10f;
    public float maxSpeed = 80f;

    [Header("Vertical Movement Settings")]
    [Tooltip("Enable/disable all vertical movement functionality")]
    public bool enableVerticalMovement = true;
    [Tooltip("Base height will be automatically set from the externally assigned orbitCenter.y")]
    [SerializeField] private float baseHeight = 0f;
    [Tooltip("Maximum height variation above and below base height")]
    public float heightVariationRange = 2f;
    [Tooltip("How smoothly the fish transitions between heights (lower = smoother)")]
    public float heightChangeRate = 0.5f;
    [Tooltip("How often height changes occur (higher = more frequent)")]
    public float heightChangeFrequency = 0.3f;
    [Tooltip("Minimum time between dramatic height changes")]
    public float minHeightChangeInterval = 2f;
    [Tooltip("Enable wave-like vertical movement")]
    public bool enableWaveMovement = true;
    [Tooltip("Wave frequency for subtle vertical oscillation")]
    public float waveFrequency = 0.8f;
    [Tooltip("Wave amplitude for subtle vertical oscillation")]
    public float waveAmplitude = 0.1f;

    [Header("Detection Settings")]
    public float detectionDistance = 2f;
    public float safeDistance = 4f;
    public LayerMask playerLayer = -1;

    [Header("Movement Settings")]
    public float fleeForce = 2f;
    public float recoverySpeed = 2f;
    public float rotationSpeed = 90f;
    public float fleeSpeedMultiplier = 2.2f;
    public float maxFleeDistance = 1.5f;

    [Header("Timer & Interaction Settings")]
    [Tooltip("Time in seconds to successfully catch the fish")]
    public float catchTimeout = 3f;
    [Tooltip("Time after release before accumulated grab time resets")]
    public float releaseTimeout = 5f;
    [Tooltip("Maximum time fish can be held before forced release")]
    public float maxGrabTime = 3f;
    [Tooltip("Duration collider is disabled after forced release")]
    public float disableColliderDuration = 3f;

    [Header("Debug Settings")]
    public bool showDebugGizmos = true;
    [Tooltip("Enable/disable flee behavior for debugging")]
    public bool enableFlee = true;
    [Tooltip("Show timer debug information in console")]
    public bool debugTimerLogging = false;
    [Tooltip("Show vertical movement debug information")]
    public bool debugVerticalMovement = false;

    [Header("Component References")]
    [Tooltip("Will automatically find Grabbable in children if not assigned")]
    public Grabbable grabbable;
    [Tooltip("Will automatically find GrabInteractable in children if not assigned")]
    public GrabInteractable grabInteractable;
    [Tooltip("Will automatically find GrabbableTimer in children if not assigned")]
    public GrabbableTimer grabbableTimer;
    public Animator animator;
    public Collider grabCollider;

    [Header("Spawner Reference")]
    [Tooltip("Reference to the spawner that created this fish (auto-assigned)")]
    public SimpleFishSpawner_old spawner;

    // States
    public enum FishState { Idle, Fleeing, Grabbed, Recovering }
    public FishState currentState = FishState.Idle;

    // Orbit variables
    [SerializeField] private float currentAngle = 0f;
    public float currentSpeed;
    private float targetSpeed;
    private Vector3 originalOrbitCenter;

    // Vertical movement variables
    private float currentHeight;
    private float targetHeight;
    private float lastHeightChangeTime = 0f;
    private float waveTime = 0f;

    // Fleeing variables
    private Vector3 fleeDirection = Vector3.zero;
    private List<Transform> nearbyPlayers = new List<Transform>();

    // Recovery variables
    private Vector3 recoveryStartPos;
    private float recoveryProgress = 0f;

    // Grab handling and timer variables
    private bool wasGrabbed = false;
    private bool previousGrabState = false;
    private bool timerStarted = false;
    private float grabStartTime = 0f;

    // Pool reference for fish removal
    // Note: orbitalObjectPool is for backward compatibility, spawner is the preferred reference

    // Smoothing
    private Vector3 currentPosition;
    private Vector3 targetPosition;

    // Public properties for external access
    public float CurrentAngle
    {
        get { return currentAngle; }
        set { currentAngle = value; }
    }

    public float AccumulatedGrabTime => grabbableTimer != null ? grabbableTimer.AccumulatedGrabTime : 0f;
    public float TimeSinceLastRelease => grabbableTimer != null ? grabbableTimer.TimeSinceLastRelease : 0f;

    void Start()
    {
        gm = GameObject.Find("GM").GetComponent<GameManager>();

        // Store the externally assigned orbit center
        originalOrbitCenter = orbitCenter;

        // Initialize vertical movement
        InitializeVerticalMovement();

        // Initialize components
        InitializeComponents();

        // Set initial position on orbit
        if (currentAngle == 0f && Random.Range(0f, 1f) > 0.5f)
        {
            currentAngle = Random.Range(0f, 360f);
        }

        currentSpeed = baseOrbitSpeed;
        targetSpeed = baseOrbitSpeed + Random.Range(-speedVariationRange * 0.5f, speedVariationRange * 0.5f);

        UpdateIdlePosition();
        currentPosition = targetPosition;
        transform.position = currentPosition;
    }

    void InitializeVerticalMovement()
    {
        if (!enableVerticalMovement)
        {
            // Use the Y component of the externally assigned orbit center as base height
            baseHeight = orbitCenter.y;
            currentHeight = baseHeight;
            targetHeight = baseHeight;
            waveTime = 0f;
            return;
        }

        // Use the Y component of the externally assigned orbit center as base height
        baseHeight = orbitCenter.y;

        // Set initial height to base height with some random variation
        currentHeight = baseHeight + Random.Range(-heightVariationRange * 0.3f, heightVariationRange * 0.3f);
        targetHeight = currentHeight;

        // Initialize wave time with random offset to desynchronize multiple fish
        waveTime = Random.Range(0f, 2f * Mathf.PI);

        if (debugVerticalMovement)
            Debug.Log($"Initialized vertical movement - Base: {baseHeight}, Current: {currentHeight}, Target: {targetHeight}", this);
    }

    void InitializeComponents()
    {
        // Use assigned references if available, otherwise search for components
        if (grabbable == null)
        {
            grabbable = GetComponent<Grabbable>();
            if (grabbable == null)
                grabbable = GetComponentInChildren<Grabbable>();
        }

        if (grabInteractable == null)
        {
            grabInteractable = GetComponent<GrabInteractable>();
            if (grabInteractable == null)
                grabInteractable = GetComponentInChildren<GrabInteractable>();
        }

        if (grabbableTimer == null)
        {
            grabbableTimer = GetComponent<GrabbableTimer>();
            if (grabbableTimer == null)
                grabbableTimer = GetComponentInChildren<GrabbableTimer>();
        }

        if (animator == null)
            animator = GetComponent<Animator>();

        if (grabCollider == null)
        {
            grabCollider = GetComponent<Collider>();
            // If not found on parent, try to find it on the same object as grabbable
            if (grabCollider == null && grabbable != null)
                grabCollider = grabbable.GetComponent<Collider>();
        }

        // Get pool reference (check both new and old systems)
        if (spawner == null)
            spawner = GetComponentInParent<SimpleFishSpawner_old>();

        // Validate required components and log where they were found
        if (grabbable == null)
        {
            Debug.LogError("FishAI requires a Grabbable component on this GameObject or its children!", this);
        }
        else if (debugTimerLogging)
        {
            Debug.Log($"Found Grabbable component on: {grabbable.gameObject.name}", this);
        }

        if (grabbableTimer == null)
        {
            Debug.LogWarning("FishAI: GrabbableTimer component not found on this GameObject or children. Timer functionality will be limited.", this);
        }
        else if (debugTimerLogging)
        {
            Debug.Log($"Found GrabbableTimer component on: {grabbableTimer.gameObject.name}", this);
        }

        if (grabInteractable == null)
        {
            Debug.LogWarning("FishAI: GrabInteractable component not found. Forced release functionality will be limited.", this);
        }
        else if (debugTimerLogging)
        {
            Debug.Log($"Found GrabInteractable component on: {grabInteractable.gameObject.name}", this);
        }
    }

    void Update()
    {
        // Update vertical movement (only if enabled)
        if (enableVerticalMovement)
        {
            UpdateVerticalMovement();
        }
        else
        {
            // Keep fish at base height when vertical movement is disabled
            currentHeight = baseHeight;
        }

        // Update orbit center with new height
        orbitCenter = new Vector3(originalOrbitCenter.x, currentHeight, originalOrbitCenter.z);

        // Handle grab interactions and timers
        HandleGrabInteraction();

        // Check grab state changes
        CheckGrabState();

        // Update state machine
        switch (currentState)
        {
            case FishState.Idle:
                UpdateIdle();
                break;
            case FishState.Fleeing:
                UpdateFleeing();
                break;
            case FishState.Grabbed:
                UpdateGrabbed();
                break;
            case FishState.Recovering:
                UpdateRecovering();
                break;
        }

        // Smooth movement
        currentPosition = Vector3.Lerp(currentPosition, targetPosition, Time.deltaTime * 5f);
        transform.position = currentPosition;

        // Update rotation to face movement direction
        UpdateRotation();
    }

    void UpdateVerticalMovement()
    {
        // Skip all vertical movement if disabled
        if (!enableVerticalMovement)
        {
            currentHeight = baseHeight;
            return;
        }

        // Update wave time for continuous wave motion
        waveTime += Time.deltaTime * waveFrequency;

        // Smoothly interpolate toward target height
        currentHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * heightChangeRate);

        // Add wave motion if enabled
        float waveOffset = 0f;
        if (enableWaveMovement)
        {
            waveOffset = Mathf.Sin(waveTime) * waveAmplitude;
        }

        // Apply wave offset to current height
        currentHeight += waveOffset;

        // Randomly change target height
        if (Time.time - lastHeightChangeTime > minHeightChangeInterval)
        {
            if (Random.Range(0f, 1f) < Time.deltaTime * heightChangeFrequency)
            {
                ChangeTargetHeight();
                lastHeightChangeTime = Time.time;
            }
        }

        if (debugVerticalMovement && Time.frameCount % 60 == 0) // Log every 60 frames to avoid spam
        {
            Debug.Log($"Height - Current: {currentHeight:F2}, Target: {targetHeight:F2}, Wave: {waveOffset:F2}", this);
        }
    }

    void ChangeTargetHeight()
    {
        // Don't change height if vertical movement is disabled
        if (!enableVerticalMovement)
        {
            targetHeight = baseHeight;
            return;
        }

        float newTargetHeight;

        // 30% chance for dramatic height change, 70% for gradual
        if (Random.Range(0f, 1f) < 0.3f)
        {
            // Dramatic height change
            newTargetHeight = baseHeight + Random.Range(-heightVariationRange, heightVariationRange);
        }
        else
        {
            // Gradual height change
            float currentOffset = targetHeight - baseHeight;
            float maxChange = heightVariationRange * 0.4f;
            float heightChange = Random.Range(-maxChange, maxChange);
            newTargetHeight = baseHeight + Mathf.Clamp(currentOffset + heightChange, -heightVariationRange, heightVariationRange);
        }

        targetHeight = newTargetHeight;

        if (debugVerticalMovement)
            Debug.Log($"Changed target height to: {targetHeight:F2} (Base: {baseHeight})", this);
    }
    //private bool catched = false;
    void HandleGrabInteraction()
    {
        if (grabbable == null) return;

        bool isGrabbing = grabbable.SelectingPointsCount > 0;

        // Check for successful catch
        if (AccumulatedGrabTime >= catchTimeout)
        {   
            if (debugTimerLogging)
                Debug.Log($"Fish caught! Accumulated grab time: {AccumulatedGrabTime}s >= {catchTimeout}s", this);
            
            //catched = true;
            AudioSource.PlayClipAtPoint(success, transform.position);
            CatchFish();
            gm.CatchFish();
            return;
        }

        // Check for release timeout (reset accumulated grab time)
        if (TimeSinceLastRelease >= releaseTimeout && AccumulatedGrabTime > 0)
        {
            if (debugTimerLogging)
                Debug.Log($"Release timeout reached. Resetting timers. Time since release: {TimeSinceLastRelease}s", this);

            if (grabbableTimer != null)
                grabbableTimer.ResetTimers();
        }

        // Handle grab state changes
        if (!isGrabbing && timerStarted) // Released
        {
            if (animator != null)
                animator.SetBool("Struggle", false);
            timerStarted = false;

            if (debugTimerLogging)
                Debug.Log("Fish released", this);
        }
        else if (isGrabbing && !timerStarted) // Just grabbed
        {
            if (debugTimerLogging)
                Debug.Log("Fish grabbed - starting struggle animation", this);

            grabStartTime = Time.time;
            timerStarted = true;

            if (animator != null)
                animator.SetBool("Struggle", true);
        }

        // Check for forced release due to max grab time
        if (timerStarted && Time.time - grabStartTime > maxGrabTime)
        {
            if (debugTimerLogging)
                Debug.Log($"Forced release after {maxGrabTime}s", this);
            audioSource.PlayOneShot(flee, 10.0f);
            ForceRelease();
        }
    }

    void CheckGrabState()
    {
        if (grabbable == null) return;

        bool currentlyGrabbed = grabbable.SelectingPointsCount > 0;

        // Detect grab state changes
        if (currentlyGrabbed && !previousGrabState)
        {
            // Just got grabbed
            TransitionToGrabbed();
        }
        else if (!currentlyGrabbed && previousGrabState)
        {
            // Just got released
            if (wasGrabbed)
            {
                wasGrabbed = false;
                TransitionToRecovering();
            }
        }

        previousGrabState = currentlyGrabbed;
    }

    void UpdateIdle()
    {
        // Gradually change speed for natural variation
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * speedChangeRate);
        currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);

        // Speed variation logic
        if (Random.Range(0f, 1f) < Time.deltaTime * 0.8f)
        {
            if (Random.Range(0f, 1f) < 0.3f)
            {
                float dramaticChange = Random.Range(0f, 1f) > 0.5f ?
                    speedVariationRange * 2f : -speedVariationRange * 1.5f;
                targetSpeed = baseOrbitSpeed + dramaticChange;
            }
            else
            {
                targetSpeed = baseOrbitSpeed + Random.Range(-speedVariationRange, speedVariationRange);
            }

            targetSpeed = Mathf.Clamp(targetSpeed, minSpeed, maxSpeed);
        }

        // Update orbit position
        currentAngle += currentSpeed * Time.deltaTime;
        if (currentAngle >= 360f) currentAngle -= 360f;

        UpdateIdlePosition();

        // Check for nearby players (only if flee is enabled)
        if (enableFlee)
        {
            CheckForPlayers();

            if (nearbyPlayers.Count > 0)
            {
                TransitionToFleeing();
            }
        }
    }

    void UpdateFleeing()
    {
        // Skip fleeing if disabled
        if (!enableFlee)
        {
            audioSource.PlayOneShot(flee);
            TransitionToIdle();
            return;
        }

        // Continue orbital motion with increased speed during flee
        currentAngle += currentSpeed * fleeSpeedMultiplier * Time.deltaTime;
        if (currentAngle >= 360f) currentAngle -= 360f;

        Vector3 baseOrbitPos = GetOrbitPosition(currentAngle);

        // Calculate flee direction from all nearby players
        CalculateFleeDirection();

        // Apply flee offset with distance limiting
        Vector3 fleeOffset = fleeDirection * Mathf.Min(fleeForce, maxFleeDistance);
        targetPosition = baseOrbitPos + fleeOffset;

        // Check if we should return to idle
        CheckForPlayers();

        if (nearbyPlayers.Count == 0)
        {
            // Gradually reduce flee force when no threats
            fleeDirection = Vector3.Lerp(fleeDirection, Vector3.zero, Time.deltaTime * 2f);

            // Return to idle when flee force is minimal
            if (fleeDirection.magnitude < 0.1f)
            {
                TransitionToIdle();
            }
        }
    }

    void UpdateGrabbed()
    {
        // Position controlled by grab system
        currentPosition = transform.position;
        targetPosition = currentPosition;
    }

    void UpdateRecovering()
    {
        recoveryProgress += Time.deltaTime * recoverySpeed;

        // Find the closest point on the orbit circle (using current orbit center with updated height)
        Vector3 currentPos = transform.position;
        Vector3 toFish = currentPos - orbitCenter;
        toFish.y = 0;

        Vector3 closestOrbitPoint;
        if (toFish.magnitude > 0.001f)
        {
            Vector3 directionToFish = toFish.normalized;
            closestOrbitPoint = orbitCenter + directionToFish * orbitRadius;
        }
        else
        {
            closestOrbitPoint = orbitCenter + Vector3.forward * orbitRadius;
        }

        // Smoothly swim toward the closest orbit point
        targetPosition = Vector3.Lerp(recoveryStartPos, closestOrbitPoint, recoveryProgress);

        float distanceToOrbit = Vector3.Distance(transform.position, closestOrbitPoint);

        if (distanceToOrbit < 0.5f || recoveryProgress >= 0.8f)
        {
            // Calculate angle for the closest orbit point
            Vector3 offsetFromCenter = closestOrbitPoint - orbitCenter;
            currentAngle = Mathf.Atan2(offsetFromCenter.z, offsetFromCenter.x) * Mathf.Rad2Deg;

            // Start following orbit path smoothly
            float orbitBlend = Mathf.Clamp01((recoveryProgress - 0.8f) / 0.2f);
            Vector3 orbitPos = GetOrbitPosition(currentAngle);
            targetPosition = Vector3.Lerp(targetPosition, orbitPos, orbitBlend);

            // Begin orbital movement
            currentAngle += currentSpeed * Time.deltaTime * orbitBlend;
        }

        if (recoveryProgress >= 1f)
        {
            TransitionToIdle();
        }
    }

    void CheckForPlayers()
    {
        nearbyPlayers.Clear();

        Collider[] playersInRange = Physics.OverlapSphere(transform.position, detectionDistance, playerLayer);

        foreach (Collider col in playersInRange)
        {
            if (col.CompareTag("Player"))
            {
                nearbyPlayers.Add(col.transform);
            }
        }
    }

    void CalculateFleeDirection()
    {
        if (nearbyPlayers.Count == 0)
        {
            fleeDirection = Vector3.Lerp(fleeDirection, Vector3.zero, Time.deltaTime * 3f);
            return;
        }

        Vector3 combinedThreat = Vector3.zero;

        foreach (Transform player in nearbyPlayers)
        {
            Vector3 directionToPlayer = (player.position - transform.position);
            directionToPlayer.y = 0;

            float distance = directionToPlayer.magnitude;
            if (distance < 0.1f) continue;

            directionToPlayer = directionToPlayer.normalized;

            float influence = Mathf.Pow(detectionDistance / Mathf.Max(distance, 0.1f), 2f);
            combinedThreat += directionToPlayer * influence;
        }

        if (combinedThreat.magnitude > 0.1f)
        {
            Vector3 rawFleeDir = -combinedThreat.normalized;
            Vector3 tangentDir = GetTangentDirection();
            Vector3 fluidFleeDir = (rawFleeDir * 0.7f + tangentDir * 0.3f).normalized;

            Vector3 targetFleeDirection = fluidFleeDir * Mathf.Clamp01(combinedThreat.magnitude);
            fleeDirection = Vector3.Lerp(fleeDirection, targetFleeDirection, Time.deltaTime * 4f);
        }
    }

    Vector3 GetTangentDirection()
    {
        float radians = currentAngle * Mathf.Deg2Rad;
        return new Vector3(-Mathf.Sin(radians), 0f, Mathf.Cos(radians));
    }

    void UpdateIdlePosition()
    {
        targetPosition = GetOrbitPosition(currentAngle);
    }

    Vector3 GetOrbitPosition(float angle)
    {
        float radians = angle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            Mathf.Cos(radians) * orbitRadius,
            0f,
            Mathf.Sin(radians) * orbitRadius
        );
        return orbitCenter + offset;
    }

    void UpdateRotation()
    {
        Vector3 movementDirection = (targetPosition - currentPosition).normalized;

        if (movementDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movementDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation,
                rotationSpeed * Time.deltaTime);
        }
    }

    // Timer and Interaction Methods
    void CatchFish()
    {
        if (debugTimerLogging)
            Debug.Log("Fish successfully caught!", this);

        if (spawner != null)
        {
            spawner.ReturnFishToPool(gameObject);
        }
        else
        {
            if (debugTimerLogging)
                Debug.Log("No pool system found, deactivating fish", this);
            gameObject.SetActive(false);
        }
    }

    void ForceRelease()
    {
        if (debugTimerLogging)
            Debug.Log("Forcing fish release due to timeout", this);

        // Start recovery and disable interaction temporarily
        TransitionToRecovering();
        StartCoroutine(DisableInteractionTemporarily(disableColliderDuration));
    }

    /// <summary>
    /// Sets the orbit center from an external script. This will automatically update the base height.
    /// </summary>
    /// <param name="newOrbitCenter">The new orbit center position</param>
    public void SetOrbitCenter(Vector3 newOrbitCenter)
    {
        orbitCenter = newOrbitCenter;
        originalOrbitCenter = newOrbitCenter;

        // Update base height from the new orbit center
        baseHeight = newOrbitCenter.y;

        // If vertical movement is disabled, keep current height at base height
        if (!enableVerticalMovement)
        {
            currentHeight = baseHeight;
            targetHeight = baseHeight;
        }

        if (debugVerticalMovement)
            Debug.Log($"Orbit center set to: {newOrbitCenter}, Base height updated to: {baseHeight}", this);
    }
    /// </summary>
    public void ResetFishState()
    {
        // Reset state
        currentState = FishState.Idle;
        wasGrabbed = false;
        previousGrabState = false;
        timerStarted = false;

        // Reset flee direction
        fleeDirection = Vector3.zero;

        // Reset recovery progress
        recoveryProgress = 0f;

        // Reset vertical movement
        InitializeVerticalMovement();

        // Reset animation if available
        if (animator != null)
        {
            animator.SetBool("Struggle", false);
        }

        // Re-enable interaction if it was disabled
        if (grabInteractable != null)
        {
            grabInteractable.enabled = true;
        }

        // Reset timer if available
        if (grabbableTimer != null)
        {
            grabbableTimer.ResetTimers();
        }

        // Set correct rotation immediately for spawning
        SetCorrectRotation();

        if (debugTimerLogging)
            Debug.Log("Fish state reset for respawn", this);
    }

    /// <summary>
    /// Forces the fish position to be updated to the correct orbit position.
    /// Called after respawning to ensure fish is at the right place.
    /// </summary>
    public void ForcePositionUpdate()
    {
        // Update orbit center with current height before calculating position
        orbitCenter = new Vector3(originalOrbitCenter.x, currentHeight, originalOrbitCenter.z);

        // Calculate the correct position based on current orbit parameters
        UpdateIdlePosition();

        // Force both current and target positions to the orbit position
        currentPosition = targetPosition;
        transform.position = currentPosition;

        // Calculate and set the correct rotation immediately
        SetCorrectRotation();

        if (debugTimerLogging)
            Debug.Log($"Forced position and rotation update to orbit position: {currentPosition}", this);
    }

    /// <summary>
    /// Sets the fish rotation to face the correct direction based on its orbital movement
    /// </summary>
    private void SetCorrectRotation()
    {
        // Calculate the tangent direction (movement direction) at current angle
        Vector3 movementDirection = GetTangentDirection();

        if (movementDirection.magnitude > 0.1f)
        {
            // Set rotation immediately to face movement direction
            Quaternion targetRotation = Quaternion.LookRotation(movementDirection);
            transform.rotation = targetRotation;

            if (debugTimerLogging)
                Debug.Log($"Set fish rotation to face movement direction: {movementDirection}", this);
        }
    }

    private IEnumerator DisableInteractionTemporarily(float duration)
    {
        if (grabInteractable != null)
        {
            grabInteractable.enabled = false;

            if (debugTimerLogging)
                Debug.Log($"Disabled fish interaction for {duration}s", this);
        }

        yield return new WaitForSeconds(duration);

        if (grabInteractable != null)
        {
            grabInteractable.enabled = true;

            if (debugTimerLogging)
                Debug.Log("Re-enabled fish interaction", this);
        }
    }

    // State Transitions
    void TransitionToIdle()
    {
        if (audioSource.clip == struggle)
        {
            audioSource.clip = swim;
            audioSource.volume = 0.2f;
            audioSource.Play();
        }
        currentState = FishState.Idle;
        fleeDirection = Vector3.zero;
        targetSpeed = baseOrbitSpeed + Random.Range(-speedVariationRange * 0.3f, speedVariationRange * 0.3f);
    }

    void TransitionToFleeing()
    {
        if (!enableFlee) return;

        currentState = FishState.Fleeing;
        currentSpeed = Mathf.Min(currentSpeed * 1.8f, maxSpeed);
        targetSpeed = currentSpeed;
    }

    void TransitionToGrabbed()
    {
        currentState = FishState.Grabbed;
        audioSource.clip = struggle;
        audioSource.volume = 2.0f;
        audioSource.Play();
        wasGrabbed = true;
    }

    void TransitionToRecovering()
    {
        if (audioSource.clip == struggle)
        {
            audioSource.clip = swim;
            audioSource.volume = 0.2f;
            audioSource.Play();
        }
        currentState = FishState.Recovering;
        recoveryStartPos = transform.position;
        recoveryProgress = 0f;
    }
#if UNITY_EDITOR
    //Debug Gizmos
    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // Draw orbit path at current height
        if (orbitCenter != null)
        {
            Gizmos.color = Color.cyan;
            DrawWireCircle(orbitCenter, orbitRadius);

            // Draw base orbit path for reference
            Gizmos.color = Color.cyan * 0.5f;
            DrawWireCircle(new Vector3(originalOrbitCenter.x, baseHeight, originalOrbitCenter.z), orbitRadius);
        }

        // Draw height variation range (only if vertical movement is enabled)
        if (enableVerticalMovement)
        {
            Gizmos.color = Color.magenta;
            Vector3 heightRangeBottom = new Vector3(originalOrbitCenter.x, baseHeight - heightVariationRange, originalOrbitCenter.z);
            Vector3 heightRangeTop = new Vector3(originalOrbitCenter.x, baseHeight + heightVariationRange, originalOrbitCenter.z);
            Gizmos.DrawWireCube(heightRangeBottom, new Vector3(orbitRadius * 2, 0.1f, orbitRadius * 2));
            Gizmos.DrawWireCube(heightRangeTop, new Vector3(orbitRadius * 2, 0.1f, orbitRadius * 2));
            Gizmos.DrawLine(heightRangeBottom, heightRangeTop);
        }

        // Draw detection radius (only if flee is enabled)
        if (enableFlee)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionDistance);
        }

        // Draw safe distance
        Gizmos.color = Color.green;
        DrawWireCircle(orbitCenter, safeDistance);

        // Draw flee direction (only if flee is enabled)
        if (currentState == FishState.Fleeing && enableFlee)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, fleeDirection * 2f);
        }

        // Draw current target position
        Gizmos.color = Color.white;
        Gizmos.DrawSphere(targetPosition, 0.1f);

        // Draw timer and height information
        if (Application.isPlaying && (debugTimerLogging || debugVerticalMovement))
        {
            string debugText = $"State: {currentState}";
            if (debugTimerLogging)
                debugText += $"\nGrab Time: {AccumulatedGrabTime:F1}s\nSince Release: {TimeSinceLastRelease:F1}s";
            if (debugVerticalMovement && enableVerticalMovement)
                debugText += $"\nHeight: {currentHeight:F2} -> {targetHeight:F2}";
            else if (debugVerticalMovement && !enableVerticalMovement)
                debugText += $"\nVertical Movement: DISABLED";

            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, debugText);
        }
    }

    void DrawWireCircle(Vector3 center, float radius, int segments = 32)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 currentPoint = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0,
                Mathf.Sin(angle) * radius
            );

            Gizmos.DrawLine(prevPoint, currentPoint);
            prevPoint = currentPoint;
        }
    }
#endif
}