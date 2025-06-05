// ====================
// FishAI.cs - Main AI Controller (Simplified)
// ====================
using System.Collections;
using UnityEngine;
using Oculus.Interaction;

public class FishAI2 : MonoBehaviour
{
    [Header("AI References")]
    public FishMovement movement;
    public FishInteractionHandler interaction;
    public FishSpawnerRef spawnerRef;
    
    [Header("Debug")]
    public bool showDebugGizmos = true;
    public bool debugLogging = false;
    
    // States
    public enum FishState { Idle, Fleeing, Grabbed, Recovering }
    public FishState currentState = FishState.Idle;
    
    // Public properties
    public float AccumulatedGrabTime => interaction?.AccumulatedGrabTime ?? 0f;
    public float TimeSinceLastRelease => interaction?.TimeSinceLastRelease ?? 0f;
    
    void Start()
    {
        InitializeComponents();
        TransitionToIdle();
    }
    
    void InitializeComponents()
    {
        if (movement == null) movement = GetComponent<FishMovement>();
        if (interaction == null) interaction = GetComponent<FishInteractionHandler>();
        if (spawnerRef == null) spawnerRef = GetComponent<FishSpawnerRef>();
        
        // Initialize components
        movement?.Initialize(this);
        interaction?.Initialize(this);
        spawnerRef?.Initialize(this);
    }
    
    void Update()
    {
        // Handle grab interactions
        interaction?.UpdateInteraction();
        
        // Update state machine
        switch (currentState)
        {
            case FishState.Idle:
                movement?.UpdateIdle();
                CheckForThreats();
                break;
            case FishState.Fleeing:
                movement?.UpdateFleeing();
                break;
            case FishState.Grabbed:
                movement?.UpdateGrabbed();
                break;
            case FishState.Recovering:
                movement?.UpdateRecovering();
                break;
        }
        
        movement?.UpdateMovement();
    }
    
    void CheckForThreats()
    {
        if (movement?.HasNearbyThreats() == true)
        {
            TransitionToFleeing();
        }
    }
    
    // State Transitions
    public void TransitionToIdle()
    {
        currentState = FishState.Idle;
        movement?.OnEnterIdle();
    }
    
    public void TransitionToFleeing()
    {
        currentState = FishState.Fleeing;
        movement?.OnEnterFleeing();
    }
    
    public void TransitionToGrabbed()
    {
        currentState = FishState.Grabbed;
        movement?.OnEnterGrabbed();
    }
    
    public void TransitionToRecovering()
    {
        currentState = FishState.Recovering;
        movement?.OnEnterRecovering();
    }
    
    public void OnFishCaught()
    {
        spawnerRef?.ReturnToPool();
    }
    
    public void ResetFishState()
    {
        currentState = FishState.Idle;
        movement?.ResetState();
        interaction?.ResetState();
    }
    
    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        movement?.DrawGizmos();
        interaction?.DrawGizmos();
    }
}