using UnityEngine;

public class FishStateMachine : MonoBehaviour
{
    public enum FishState { Idle, Fleeing, Grabbed, Recovering }
    
    [Header("Current State")]
    [SerializeField] private FishState currentState = FishState.Idle;
    
    // Recovery variables
    private Vector3 recoveryStartPos;
    private float recoveryProgress = 0f;

    // State change events
    public System.Action OnEnterIdle;
    public System.Action OnEnterFleeing;
    public System.Action OnEnterGrabbed;
    public System.Action OnEnterRecovering;
    public System.Action OnExitState;

    public FishState CurrentState => currentState;
    public float RecoveryProgress => recoveryProgress;
    public Vector3 RecoveryStartPos => recoveryStartPos;

    public void TransitionToIdle()
    {
        if (currentState == FishState.Idle) return;
        
        OnExitState?.Invoke();
        currentState = FishState.Idle;
        OnEnterIdle?.Invoke();
    }

    public void TransitionToFleeing()
    {
        if (currentState == FishState.Fleeing) return;
        
        OnExitState?.Invoke();
        currentState = FishState.Fleeing;
        OnEnterFleeing?.Invoke();
    }

    public void TransitionToGrabbed()
    {
        if (currentState == FishState.Grabbed) return;
        
        OnExitState?.Invoke();
        currentState = FishState.Grabbed;
        OnEnterGrabbed?.Invoke();
    }

    public void TransitionToRecovering()
    {
        if (currentState == FishState.Recovering) return;
        
        OnExitState?.Invoke();
        recoveryStartPos = transform.position;
        recoveryProgress = 0f;
        currentState = FishState.Recovering;
        OnEnterRecovering?.Invoke();
    }

   public void UpdateRecoveryProgress(float deltaTime, float recoverySpeed)
    {
        if (currentState == FishState.Recovering)
        {
            recoveryProgress += deltaTime * recoverySpeed;
            recoveryProgress = Mathf.Clamp01(recoveryProgress); // Ensure it doesn't exceed 1.0
        }
    }

    public bool IsRecoveryComplete()
    {
        return currentState == FishState.Recovering && recoveryProgress >= 1f;
    }

    public void ResetState()
    {
        currentState = FishState.Idle;
        recoveryProgress = 0f;
    }
}