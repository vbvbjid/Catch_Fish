// ====================
// FishInteraction.cs - Grab Handling & Timers
// ====================
using System.Collections;
using UnityEngine;
using Oculus.Interaction;

[System.Serializable]
public class FishInteractionHandler : MonoBehaviour
{
    [Header("Timer Settings")]
    public float catchTimeout = 3f;
    public float releaseTimeout = 5f;
    public float maxGrabTime = 3f;
    public float disableColliderDuration = 3f;
    
    [Header("Component References")]
    public Grabbable grabbable;
    public GrabInteractable grabInteractable;
    public GrabbableTimer grabbableTimer;
    public Animator animator;
    
    [Header("Debug")]
    public bool debugTimerLogging = false;
    
    // Private variables
    private FishAI2 fishAI;
    private bool wasGrabbed = false;
    private bool previousGrabState = false;
    private bool timerStarted = false;
    private float grabStartTime = 0f;
    
    public float AccumulatedGrabTime => grabbableTimer != null ? grabbableTimer.AccumulatedGrabTime : 0f;
    public float TimeSinceLastRelease => grabbableTimer != null ? grabbableTimer.TimeSinceLastRelease : 0f;
    
    public void Initialize(FishAI2 ai)
    {
        fishAI = ai;
        InitializeComponents();
    }
    
    void InitializeComponents()
    {
        if (grabbable == null)
        {
            grabbable = GetComponent<Grabbable>() ?? GetComponentInChildren<Grabbable>();
        }
        
        if (grabInteractable == null)
        {
            grabInteractable = GetComponent<GrabInteractable>() ?? GetComponentInChildren<GrabInteractable>();
        }
        
        if (grabbableTimer == null)
        {
            grabbableTimer = GetComponent<GrabbableTimer>() ?? GetComponentInChildren<GrabbableTimer>();
        }
        
        if (animator == null)
            animator = GetComponent<Animator>();
    }
    
    public void UpdateInteraction()
    {
        if (grabbable == null) return;
        
        HandleGrabInteraction();
        CheckGrabState();
    }
    
    void HandleGrabInteraction()
    {
        bool isGrabbing = grabbable.SelectingPointsCount > 0;
        
        // Check for successful catch
        if (AccumulatedGrabTime >= catchTimeout)
        {
            if (debugTimerLogging)
                Debug.Log($"Fish caught! Accumulated grab time: {AccumulatedGrabTime}s >= {catchTimeout}s", this);
            
            fishAI.OnFishCaught();
            return;
        }
        
        // Check for release timeout
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
        
        // Check for forced release
        if (timerStarted && Time.time - grabStartTime > maxGrabTime)
        {
            if (debugTimerLogging)
                Debug.Log($"Forced release after {maxGrabTime}s", this);
            
            ForceRelease();
        }
    }
    
    void CheckGrabState()
    {
        bool currentlyGrabbed = grabbable.SelectingPointsCount > 0;
        
        if (currentlyGrabbed && !previousGrabState)
        {
            fishAI.TransitionToGrabbed();
        }
        else if (!currentlyGrabbed && previousGrabState)
        {
            if (wasGrabbed)
            {
                wasGrabbed = false;
                fishAI.TransitionToRecovering();
            }
        }
        
        previousGrabState = currentlyGrabbed;
    }
    
    void ForceRelease()
    {
        fishAI.TransitionToRecovering();
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
    
    public void ResetState()
    {
        wasGrabbed = false;
        previousGrabState = false;
        timerStarted = false;
        
        if (animator != null)
        {
            animator.SetBool("Struggle", false);
        }
        
        if (grabInteractable != null)
        {
            grabInteractable.enabled = true;
        }
        
        if (grabbableTimer != null)
        {
            grabbableTimer.ResetTimers();
        }
    }
    
    public void DrawGizmos()
    {
        // Draw timer information if needed
        if (Application.isPlaying && debugTimerLogging)
        {
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, 
                $"Grab Time: {AccumulatedGrabTime:F1}s\nSince Release: {TimeSinceLastRelease:F1}s");
#endif
        }
    }
}