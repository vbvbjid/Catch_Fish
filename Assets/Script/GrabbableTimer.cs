using System;
using UnityEngine;
using Oculus.Interaction;

/// <summary>
/// Companion component to Grabbable that tracks grab duration and time since last release.
/// This script should be attached to the same GameObject as the Grabbable component.
/// </summary>
public class GrabbableTimer : MonoBehaviour
{
    [Tooltip("Reference to the Grabbable component. If not assigned, will try to find it on the same GameObject.")]
    [SerializeField] private Grabbable _grabbable;

    [Header("Debug")]
    [SerializeField] private bool _debugLogging = false;
    
    // Tracking variables
    private bool _wasGrabbed = false;
    private float _grabStartTime = 0f;
    private float _lastReleaseTime = 0f;
    private float _accumulatedGrabTime = 0f;
    
    /// <summary>
    /// The total accumulated time this object has been grabbed since the component was enabled.
    /// </summary>
    public float AccumulatedGrabTime => _accumulatedGrabTime + (IsGrabbed ? (Time.time - _grabStartTime) : 0f);
    
    /// <summary>
    /// Time in seconds since the object was last released. Returns 0 if the object is currently grabbed or has never been grabbed.
    /// </summary>
    public float TimeSinceLastRelease => _lastReleaseTime == 0f ? 0f : (IsGrabbed ? 0f : Time.time - _lastReleaseTime);
    
    /// <summary>
    /// Duration of the current grab in seconds. Returns 0 if the object is not currently grabbed.
    /// </summary>
    public float CurrentGrabDuration => IsGrabbed ? Time.time - _grabStartTime : 0f;
    
    /// <summary>
    /// Whether the object is currently being grabbed.
    /// </summary>
    public bool IsGrabbed => _grabbable != null && _grabbable.SelectingPointsCount > 0;
    private void Awake()
    {
        if (_grabbable == null)
        {
            _grabbable = GetComponent<Grabbable>();
        }
        
        if (_grabbable == null)
        {
            Debug.LogError("GrabbableTimer requires a Grabbable component on the same GameObject or assigned in the inspector.", this);
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        // Register for pointer events
        if (_grabbable != null)
        {
            _grabbable.WhenPointerEventRaised += HandlePointerEvent;
        }
        
        // Reset timers
        _wasGrabbed = false;
        _grabStartTime = 0f;
        _lastReleaseTime = 0f;
        _accumulatedGrabTime = 0f;
    }

    private void OnDisable()
    {
        // Unregister from pointer events
        if (_grabbable != null)
        {
            _grabbable.WhenPointerEventRaised -= HandlePointerEvent;
        }
    }

    private void HandlePointerEvent(PointerEvent evt)
    {
        switch (evt.Type)
        {
            case PointerEventType.Select:
                // If this is the first grab point, record the start time
                if (!_wasGrabbed)
                {
                    _wasGrabbed = true;
                    _grabStartTime = Time.time;
                    if (_debugLogging) Debug.Log($"Object grab started at {_grabStartTime}", this);
                }
                break;
                
            case PointerEventType.Unselect:
            case PointerEventType.Cancel:
                // Check if this was the last grab point
                if (_wasGrabbed && _grabbable.SelectingPointsCount <= 1)
                {
                    float grabDuration = Time.time - _grabStartTime;
                    _accumulatedGrabTime += grabDuration;
                    _lastReleaseTime = Time.time;
                    _wasGrabbed = false;
                    
                    if (_debugLogging) Debug.Log($"Object released after {grabDuration}s. Total grab time: {_accumulatedGrabTime}s", this);
                }
                break;
        }
    }

    /// <summary>
    /// Resets all the timer data including accumulated grab time and last release time.
    /// </summary>
    public void ResetTimers()
    {
        _accumulatedGrabTime = 0f;
        _lastReleaseTime = 0f;
        if (IsGrabbed)
        {
            _grabStartTime = Time.time;
        }
    }
}