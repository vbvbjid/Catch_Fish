using System.Collections;
using UnityEngine;
using Oculus.Interaction;

public class FishGrabHandler : MonoBehaviour
{
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
    [Tooltip("Show timer debug information in console")]
    public bool debugTimerLogging = false;

    [Header("Component References")]
    [Tooltip("Will automatically find Grabbable in children if not assigned")]
    public Grabbable grabbable;
    [Tooltip("Will automatically find GrabInteractable in children if not assigned")]
    public GrabInteractable grabInteractable;
    [Tooltip("Will automatically find GrabbableTimer in children if not assigned")]
    public GrabbableTimer grabbableTimer;
    public Animator animator;

    // Grab handling and timer variables
    private bool wasGrabbed = false;
    private bool previousGrabState = false;
    private bool timerStarted = false;
    private float grabStartTime = 0f;

    // Events
    public System.Action OnFishCaught;
    public System.Action OnFishGrabbed;
    public System.Action OnFishReleased;
    public System.Action OnFishForceReleased;

    public float AccumulatedGrabTime => grabbableTimer != null ? grabbableTimer.AccumulatedGrabTime : 0f;
    public float TimeSinceLastRelease => grabbableTimer != null ? grabbableTimer.TimeSinceLastRelease : 0f;
    public bool IsCurrentlyGrabbed => grabbable != null && grabbable.SelectingPointsCount > 0;
    public bool WasGrabbed => wasGrabbed;

    void Start()
    {
        InitializeComponents();
    }

    void Update()
    {
        HandleGrabInteraction();
        CheckGrabState();
    }

    public void ResetGrabState()
    {
        wasGrabbed = false;
        previousGrabState = false;
        timerStarted = false;

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

        if (debugTimerLogging)
            Debug.Log("Grab state reset", this);
    }

    private void InitializeComponents()
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

        // Validate required components and log where they were found
        if (grabbable == null)
        {
            Debug.LogError("FishGrabHandler requires a Grabbable component on this GameObject or its children!", this);
        }
        else if (debugTimerLogging)
        {
            Debug.Log($"Found Grabbable component on: {grabbable.gameObject.name}", this);
        }

        if (grabbableTimer == null)
        {
            Debug.LogWarning("FishGrabHandler: GrabbableTimer component not found on this GameObject or children. Timer functionality will be limited.", this);
        }
        else if (debugTimerLogging)
        {
            Debug.Log($"Found GrabbableTimer component on: {grabbableTimer.gameObject.name}", this);
        }

        if (grabInteractable == null)
        {
            Debug.LogWarning("FishGrabHandler: GrabInteractable component not found. Forced release functionality will be limited.", this);
        }
        else if (debugTimerLogging)
        {
            Debug.Log($"Found GrabInteractable component on: {grabInteractable.gameObject.name}", this);
        }
    }

    private void HandleGrabInteraction()
    {
        if (grabbable == null) return;

        bool isGrabbing = grabbable.SelectingPointsCount > 0;

        // Check for successful catch
        if (AccumulatedGrabTime >= catchTimeout)
        {
            if (debugTimerLogging)
                Debug.Log($"Fish caught! Accumulated grab time: {AccumulatedGrabTime}s >= {catchTimeout}s", this);

            OnFishCaught?.Invoke();
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

            ForceRelease();
        }
    }

    private void CheckGrabState()
    {
        if (grabbable == null) return;

        bool currentlyGrabbed = grabbable.SelectingPointsCount > 0;

        // Detect grab state changes
        if (currentlyGrabbed && !previousGrabState)
        {
            // Just got grabbed
            wasGrabbed = true;
            OnFishGrabbed?.Invoke();
        }
        else if (!currentlyGrabbed && previousGrabState)
        {
            // Just got released
            if (wasGrabbed)
            {
                OnFishReleased?.Invoke();
            }
        }

        previousGrabState = currentlyGrabbed;
    }

    private void ForceRelease()
    {
        if (debugTimerLogging)
            Debug.Log("Forcing fish release due to timeout", this);

        OnFishForceReleased?.Invoke();
        StartCoroutine(DisableInteractionTemporarily(disableColliderDuration));
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
}