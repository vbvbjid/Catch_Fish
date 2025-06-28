using UnityEngine;
using UnityEngine.Animations;

public class FishAI : MonoBehaviour
{
    [Header("References")]
    public GameManager gm;
    public SimpleFishSpawner spawner;

    [Header("Debug Settings")]
    public bool showDebugGizmos = true;

    // Component references
    //private FishMovement fishMovement;
    //private FishVerticalMovement verticalMovement;
    private AIMove aiMove;
    //private FishFleeBehavior fleeBehavior;
    private FishGrabHandler grabHandler;
    private FishAudioManager audioManager;
    private FishStateMachine stateMachine;
    private FishVisualEffect fishVisualEffect;

    //private bool fishCaught = false;

    void Start()
    {
        // Find GameManager if not assigned
        if (gm == null)
            gm = GameObject.Find("GM")?.GetComponent<GameManager>();

        // Initialize all components
        InitializeComponents();

        // Set up event subscriptions
        SetupEventSubscriptions();

        // Initialize fish
        //InitializeFish();
    }

    void Update()
    {
        // Update vertical movement (now speed-synchronized)
        // if (verticalMovement != null)
        // {
        //     verticalMovement.UpdateVerticalMovement(Time.deltaTime);

        //     // Update movement component's orbit center with new height
        //     if (fishMovement != null)
        //     {
        //         Vector3 newOrbitCenter = new Vector3(
        //             fishMovement.orbitCenter.x,
        //             verticalMovement.CurrentHeight,
        //             fishMovement.orbitCenter.z
        //         );
        //         fishMovement.SetOrbitCenter(newOrbitCenter);
        //     }
        // }

        // Update flee detection
        // if (fleeBehavior != null)
        // {
        //     fleeBehavior.UpdateFleeDetection();
        // }

        // Update state machine
        UpdateStateMachine();

        // Update movement
        // if (fishMovement != null)
        // {
        //     fishMovement.UpdateMovement(Time.deltaTime);
        // }
    }

    private void InitializeComponents()
    {
        // Get or add components
        // fishMovement = GetComponent<FishMovement>();
        // if (fishMovement == null) fishMovement = gameObject.AddComponent<FishMovement>();

        // verticalMovement = GetComponent<FishVerticalMovement>();
        // if (verticalMovement == null) verticalMovement = gameObject.AddComponent<FishVerticalMovement>();
        aiMove = GetComponent<AIMove>();
        if (aiMove == null) aiMove = gameObject.AddComponent<AIMove>();

        // fleeBehavior = GetComponent<FishFleeBehavior>();
        // if (fleeBehavior == null) fleeBehavior = gameObject.AddComponent<FishFleeBehavior>();

        grabHandler = GetComponent<FishGrabHandler>();
        if (grabHandler == null) grabHandler = gameObject.AddComponent<FishGrabHandler>();

        audioManager = GetComponent<FishAudioManager>();
        if (audioManager == null) audioManager = gameObject.AddComponent<FishAudioManager>();

        stateMachine = GetComponent<FishStateMachine>();
        if (stateMachine == null) stateMachine = gameObject.AddComponent<FishStateMachine>();

        fishVisualEffect = GetComponent<FishVisualEffect>();
        if (fishVisualEffect == null) fishVisualEffect = gameObject.AddComponent<FishVisualEffect>();

        // Get spawner reference if not assigned
        // if (spawner == null)
        //     spawner = GetComponentInParent<SimpleFishSpawner>();
    }

    private void SetupEventSubscriptions()
    {
        // Subscribe to grab handler events
        if (grabHandler != null)
        {
            grabHandler.OnFishCaught += HandleFishCaught;
            grabHandler.OnFishGrabbed += HandleFishGrabbed;
            grabHandler.OnFishReleased += HandleFishReleased;
            grabHandler.OnFishForceReleased += HandleFishForceReleased;
        }

        // Subscribe to state machine events
        if (stateMachine != null)
        {
            stateMachine.OnEnterIdle += HandleEnterIdle;
            // stateMachine.OnEnterFleeing += HandleEnterFleeing;
            stateMachine.OnEnterGrabbed += HandleEnterGrabbed;
            // stateMachine.OnEnterRecovering += HandleEnterRecovering;
        }
    }

    // private void InitializeFish()
    // {
    //     // Initialize vertical movement with orbit center Y
    //     if (verticalMovement != null && fishMovement != null)
    //     {
    //         verticalMovement.Initialize(fishMovement.orbitCenter.y);
    //     }

    //     // Initialize movement
    //     if (fishMovement != null)
    //     {
    //         fishMovement.Initialize();
    //     }
    // }

    private void UpdateStateMachine()
    {
        if (stateMachine == null) return;

        switch (stateMachine.CurrentState)
        {
            // case FishStateMachine.FishState.Idle:
            //     UpdateIdleState();
            //     break;
            // case FishStateMachine.FishState.Fleeing:
            //     UpdateFleeingState();
            //     break;
            case FishStateMachine.FishState.Grabbed:
                UpdateGrabbedState();
                break;
            // case FishStateMachine.FishState.Recovering:
            //     UpdateRecoveringState();
            //     break;
        }
    }

    // private void UpdateIdleState()
    // {
    //     //Update orbital movement
    //     if (fishMovement != null)
    //     {
    //         fishMovement.UpdateOrbitMovement(Time.deltaTime);
    //     }

    //     // Check if we should start fleeing
    //     if (fleeBehavior != null && fleeBehavior.ShouldStartFleeing())
    //     {
    //         stateMachine.TransitionToFleeing();
    //     }
    // }

    // private void UpdateFleeingState()
    // {
    //     // Update flee direction
    //     if (fleeBehavior != null)
    //     {
    //         fleeBehavior.UpdateFleeDirection(Time.deltaTime);

    //         //Update flee movement
    //         if (fishMovement != null)
    //         {
    //             fishMovement.UpdateFleeMovement(
    //                 Time.deltaTime,
    //                 fleeBehavior.FleeDirection,
    //                 fleeBehavior.maxFleeDistance,
    //                 fleeBehavior.fleeForce
    //             );
    //         }

    //         // Check if we should stop fleeing
    //         if (fleeBehavior.ShouldStopFleeing())
    //         {
    //             stateMachine.TransitionToIdle();
    //         }
    //     }
    // }

    private void UpdateGrabbedState()
    {
        // Position is controlled by grab system
        // Keep movement component's position tracking in sync with actual transform position
        if (aiMove != null)
        {
            // Force the movement component to track the actual grabbed position
            // This prevents it from trying to move to orbit position while grabbed
            aiMove.ToggleGrab();
        }
    }

    // private void UpdateRecoveringState()
    // {
    //     if (stateMachine != null && fishMovement != null)
    //     {
    //         stateMachine.UpdateRecoveryProgress(Time.deltaTime, fishMovement.recoverySpeed);

    //         fishMovement.UpdateRecoveryMovement(
    //             Time.deltaTime,
    //             stateMachine.RecoveryStartPos,
    //             stateMachine.RecoveryProgress
    //         );

    //         if (stateMachine.IsRecoveryComplete())
    //         {
    //             stateMachine.TransitionToIdle();
    //         }
    //     }
    // }

    // Event Handlers
    private async void HandleFishCaught()
    {
        //fishCaught = true;

        if (audioManager != null)
            audioManager.PlaySuccessSound();

        if (fishVisualEffect != null)
        {
            await fishVisualEffect.DissoveEffect();
            if (gm != null)
                gm.CatchFish();
            CatchFish();
        }
        else
        {
            if (gm != null)
                gm.CatchFish();
            CatchFish();
        }
    }

    private void HandleFishGrabbed()
    {
        stateMachine?.TransitionToGrabbed();
    }

    private void HandleFishReleased()
    {
        if (grabHandler != null && grabHandler.WasGrabbed)
        {
            // IMPORTANT: Make sure the movement component knows the fish's actual release position
            // before starting recovery
            // if (fishMovement != null)
            // {
            //     fishMovement.SetCurrentPosition(transform.position);
            // }
            if (aiMove != null)
            {
                aiMove.ToggleGrab();
            }

            stateMachine?.TransitionToRecovering();
        }
    }

    private void HandleFishForceReleased()
    {
        if (audioManager != null)
            audioManager.PlayFleeSound();

        stateMachine?.TransitionToRecovering();
    }

    private void HandleEnterIdle()
    {
        if (audioManager != null)
            audioManager.SwitchFromStruggleToSwim();

        // if (fishMovement != null)
        // {
        //     fishMovement.ResetSpeed();
        // }

        // if (fleeBehavior != null)
        // {
        //     fleeBehavior.ResetFleeDirection();
        // }
    }

    // private void HandleEnterFleeing()
    // {
    //     if (fishMovement != null)
    //     {
    //         fishMovement.IncreaseFleeSpeed();
    //     }
    // }

    private void HandleEnterGrabbed()
    {
        if (audioManager != null)
            audioManager.PlayStruggleSound();
    }

    // private void HandleEnterRecovering()
    // {
    //     if (audioManager != null)
    //         audioManager.SwitchFromStruggleToSwim();

    //     // Make sure the fish starts recovery from its actual current position
    //     if (fishMovement != null)
    //     {
    //         fishMovement.SetCurrentPosition(transform.position);
    //     }
    // }

    // Public API
    // public void SetOrbitCenter(Vector3 newOrbitCenter)
    // {
    //     if (fishMovement != null)
    //     {
    //         fishMovement.SetOrbitCenter(newOrbitCenter);
    //     }

    //     if (verticalMovement != null)
    //     {
    //         verticalMovement.SetBaseHeight(newOrbitCenter.y);
    //     }
    // }

    public void ResetFishState()
    {
        //fishCaught = false;

        // Reset all components
        stateMachine?.ResetState();
        grabHandler?.ResetGrabState();
        // fleeBehavior?.ResetFleeDirection();
        // verticalMovement?.ResetVerticalMovement();
        // fishMovement?.ResetSpeed();
    }

    public void ForcePositionUpdate()
    {
        //fishMovement?.ForcePositionUpdate();
    }

    private void CatchFish()
    {
        if (spawner != null)
        {
            spawner.ReturnFishToPool(gameObject);
        }
        else
        {
            Debug.Log("No pool system found, deactivating fish", this);
            gameObject.SetActive(false);
        }
        fishVisualEffect.RestoreMaterial();
        grabHandler.ResetIsCaught();
        audioManager.PlaySwimSound();
    }

    // Properties for external access
    /*
        public float CurrentAngle
        {
            get
            {
                return fishMovement?.CurrentAngle ?? 0f;
            }
        }
    */
    //public float CurrentAngle => fishMovement?.CurrentAngle ?? 0f;
    public float AccumulatedGrabTime => grabHandler?.AccumulatedGrabTime ?? 0f;
    public float TimeSinceLastRelease => grabHandler?.TimeSinceLastRelease ?? 0f;
    public FishStateMachine.FishState CurrentState => stateMachine?.CurrentState ?? FishStateMachine.FishState.Idle;

    // Expose orbit center for spawner compatibility
    // public Vector3 orbitCenter
    // {
    //     /*
    //         get
    //         {
    //             return fishMovement?.orbitCenter ?? Vector3.zero;
    //         }
    //     */
    //     // usage: x = orbitCenter 
    //     /*
    //         if(fishMovement != null)
    //         {
    //             if(fishMovement.orbitCenter != null) x = fishMovement.orbitCenter;
    //             else x = Vector3.zero;
    //         }
    //     */
    //     get => fishMovement?.orbitCenter ?? Vector3.zero;
    //     // usage orbitCenter = x
    //     // if (fishMovement != null) fishMovement.orbitCenter = x;
    //     set
    //     {
    //         if (fishMovement != null)
    //             fishMovement.orbitCenter = value;
    //     }
    // }

    // public float orbitRadius
    // {
    //     get => fishMovement?.orbitRadius ?? 0f;
    //     set
    //     {
    //         if (fishMovement != null)
    //             fishMovement.orbitRadius = value;
    //     }
    // }

    // public float baseOrbitSpeed => fishMovement?.baseOrbitSpeed ?? 0f;
    // public float minSpeed => fishMovement?.minSpeed ?? 0f;
    // public float currentSpeed
    // {
    //     get => fishMovement?.currentSpeed ?? 0f;
    //     set
    //     {
    //         if (fishMovement != null)
    //             fishMovement.currentSpeed = value;
    //     }
    // }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // if (!showDebugGizmos) return;

        // // Draw movement gizmos
        // if (fishMovement != null)
        // {
        //     // Draw orbit path at current height
        //     Vector3 orbitCenter = fishMovement.orbitCenter;
        //     Gizmos.color = Color.cyan;
        //     DrawWireCircle(orbitCenter, fishMovement.orbitRadius);
        // }

        // // Draw vertical movement gizmos - simplified for new system
        // if (verticalMovement != null && verticalMovement.enableVerticalMovement)
        // {
        //     Gizmos.color = Color.magenta;
        //     Vector3 baseCenter = new Vector3(fishMovement.orbitCenter.x, verticalMovement.BaseHeight, fishMovement.orbitCenter.z);
        //     Vector3 heightRangeBottom = new Vector3(baseCenter.x, verticalMovement.BaseHeight - verticalMovement.maxAmplitude, baseCenter.z);
        //     Vector3 heightRangeTop = new Vector3(baseCenter.x, verticalMovement.BaseHeight + verticalMovement.maxAmplitude, baseCenter.z);

        //     // Draw max amplitude constraint bounds
        //     Gizmos.DrawWireCube(heightRangeBottom, new Vector3(fishMovement.orbitRadius * 2, 0.05f, fishMovement.orbitRadius * 2));
        //     Gizmos.DrawWireCube(heightRangeTop, new Vector3(fishMovement.orbitRadius * 2, 0.05f, fishMovement.orbitRadius * 2));
        //     Gizmos.DrawLine(heightRangeBottom, heightRangeTop);

        //     // Draw current height
        //     Gizmos.color = Color.yellow;
        //     Vector3 currentHeightCenter = new Vector3(baseCenter.x, verticalMovement.CurrentHeight, baseCenter.z);
        //     Gizmos.DrawWireCube(currentHeightCenter, new Vector3(fishMovement.orbitRadius * 2, 0.02f, fishMovement.orbitRadius * 2));
        // }

        // // Draw flee behavior gizmos
        // fleeBehavior?.DrawDebugGizmos();

        // // Draw safe distance
        // if (fleeBehavior != null)
        // {
        //     Gizmos.color = Color.green;
        //     DrawWireCircle(fishMovement.orbitCenter, fleeBehavior.safeDistance);
        // }

        // // Draw current target position
        // if (fishMovement != null)
        // {
        //     Gizmos.color = Color.white;
        //     Gizmos.DrawSphere(fishMovement.TargetPosition, 0.1f);
        // }

        // // Draw debug information
        // if (Application.isPlaying)
        // {
        //     string debugText = $"State: {CurrentState}";
        //     if (grabHandler != null && grabHandler.debugTimerLogging)
        //         debugText += $"\nGrab Time: {AccumulatedGrabTime:F1}s\nSince Release: {TimeSinceLastRelease:F1}s";
        //     if (verticalMovement != null && verticalMovement.debugVerticalMovement)
        //     {
        //         if (verticalMovement.enableVerticalMovement)
        //         {
        //             float currentSpeed = fishMovement?.currentSpeed ?? 0f;
        //             debugText += $"\nHeight: {verticalMovement.CurrentHeight:F2}\nSpeed: {currentSpeed:F1}";
        //         }
        //         else
        //             debugText += $"\nVertical Movement: DISABLED";
        //     }

        //     UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, debugText);
        // }
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