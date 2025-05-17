using UnityEngine;
public class OrbitalTangentMovement : MonoBehaviour
{
    [Header("Center Settings")]
    public bool useCenterTransform = true;
    public Transform centerPoint;
    public Vector3 centerPosition;

    [Header("Orbit Settings")]
    public float orbitRadius = 5f;
    public float startingAngle = 0f;
    public float orbitSpeed = 30f;
    public bool useLocalSpace = false;

    [Header("Speed Cycle Settings")]
    public float minSpeed = 10f;
    public float maxSpeed = 50f;
    public float minCycleTime = 3f;
    public float maxCycleTime = 8f;
    public bool useRandomCycles = true;

    [Header("Tangent Settings")]
    public float tangentLength = 2f;

    [Header("Recovery Settings")]
    public float recoverySpeed = 0.1f;
    public float rotationRecoverySpeed = 2f;
    
    private LineRenderer tangentLine;

    private float currentAngle = 0f;
    private Vector3 orbitPosition;
    private Vector3 actualPosition;

    private float targetSpeed;
    private float cycleTimer;
    private float currentCycleDuration;
    private bool accelerating = true;
    public bool dontMove = false;
    
    // Recovery state tracking
    public bool isRecovering = false;
    private Vector3 recoveryTargetPosition;
    private Quaternion recoveryTargetRotation;
    public float recoveryProgress = 0f;
    [Tooltip("Reference to the GrabbableTimer component to get timing information from.")]
    [SerializeField] private GrabbableTimer _grabbableTimer;
    private void OnEnable()
    {
        if (_grabbableTimer != null)
        {
            _grabbableTimer.OnObjectReleased += HandleObjectReleased;
        }
    }

    private void OnDisable()
    {
        if (_grabbableTimer != null)
        {
            _grabbableTimer.OnObjectReleased -= HandleObjectReleased;
        }
    }
    private void HandleObjectReleased(float grabDuration)
    {
        // Call StartRecovery() to begin the recovery process when the object is released
        StartRecovery();
    }

    private void FixedUpdate() { }

    private Vector3 GetCenterPosition()
    {
        return (useCenterTransform && centerPoint != null) ? centerPoint.position : centerPosition;
    }

    private void UpdatePosition()
    {
        float angleRad = currentAngle * Mathf.Deg2Rad;
        Vector3 orbitOffset = new Vector3(Mathf.Sin(angleRad) * orbitRadius, 0f, Mathf.Cos(angleRad) * orbitRadius);
        Vector3 centerPos = GetCenterPosition();

        orbitPosition = (useLocalSpace && centerPoint != null && useCenterTransform)
            ? centerPoint.TransformPoint(orbitOffset)
            : centerPos + orbitOffset;

        Vector3 tangentDirection = new Vector3(Mathf.Cos(angleRad), 0f, -Mathf.Sin(angleRad)).normalized;

        actualPosition = orbitPosition;
        transform.position = actualPosition;

        transform.rotation = Quaternion.LookRotation(tangentDirection);

        if (tangentLine != null && tangentLength > 0)
        {
            tangentLine.SetPosition(0, actualPosition);
            tangentLine.SetPosition(1, actualPosition + (tangentDirection * tangentLength));
        }
    }

    private void Start()
    {
        currentAngle = startingAngle;

        if (useCenterTransform && centerPoint == null)
        {
            GameObject center = new GameObject("Orbit Center");
            center.transform.position = Vector3.zero;
            centerPoint = center.transform;
        }

        InitializeNewSpeedCycle();
        UpdatePosition();
        actualPosition = orbitPosition;
        transform.position = actualPosition;
    }

    private void InitializeNewSpeedCycle()
    {
        cycleTimer = 0f;
        accelerating = !accelerating;
        targetSpeed = accelerating ? maxSpeed : minSpeed;
        currentCycleDuration = Random.Range(minCycleTime, maxCycleTime);
    }

    private void Update()
    {
        if (dontMove) return;

        if (isRecovering)
        {
            UpdateRecovery();
            return;
        }

        if (useRandomCycles)
        {
            cycleTimer += Time.deltaTime;

            if (cycleTimer >= currentCycleDuration)
            {
                InitializeNewSpeedCycle();
            }

            orbitSpeed = Mathf.Lerp(orbitSpeed, targetSpeed, Time.deltaTime);
        }

        currentAngle += orbitSpeed * Time.deltaTime;
        if (currentAngle >= 360f) currentAngle -= 360f;

        UpdatePosition();
    }
    
    /// <summary>
    /// Starts the recovery process to return to the orbit path
    /// </summary>
    public void StartRecovery()
    {
        if (isRecovering) return; // Already recovering
        
        isRecovering = true;
        recoveryProgress = 0f;
        
        // Get center position
        Vector3 centerPos = GetCenterPosition();
        
        // Calculate vector from center to current position
        Vector3 directionToObject = transform.position - centerPos;
        
        // Project to XZ plane to match orbit plane
        directionToObject.y = 0;
        
        // Calculate angle to current position
        float angleToObject = Mathf.Atan2(directionToObject.x, directionToObject.z) * Mathf.Rad2Deg;
        if (angleToObject < 0) angleToObject += 360f;
        
        // Set current angle for the orbit calculation
        currentAngle = angleToObject;
        
        // Calculate the correct orbit position at this angle
        float angleRad = currentAngle * Mathf.Deg2Rad;
        Vector3 orbitOffset = new Vector3(Mathf.Sin(angleRad) * orbitRadius, 0f, Mathf.Cos(angleRad) * orbitRadius);
        
        // Set recovery target position
        if (useLocalSpace && centerPoint != null && useCenterTransform)
        {
            recoveryTargetPosition = centerPoint.TransformPoint(orbitOffset);
        }
        else
        {
            recoveryTargetPosition = centerPos + orbitOffset;
        }
        
        // Calculate correct tangent direction and target rotation
        Vector3 tangentDirection = new Vector3(Mathf.Cos(angleRad), 0f, -Mathf.Sin(angleRad)).normalized;
        recoveryTargetRotation = Quaternion.LookRotation(tangentDirection);
        
        // Debug information
        Debug.Log($"Recovery started. Center: {centerPos}, Target: {recoveryTargetPosition}, Angle: {currentAngle}");
    }
    
    /// <summary>
    /// Updates the recovery process each frame
    /// </summary>
    private void UpdateRecovery()
    {
        // Use recoverySpeed as a percentage-per-second instead of direct increment
        recoveryProgress += Time.deltaTime * recoverySpeed;
        
        if (recoveryProgress >= 1.0f)
        {
            // Recovery complete, resume normal orbiting
            isRecovering = false;
            actualPosition = recoveryTargetPosition;
            transform.position = actualPosition;
            transform.rotation = recoveryTargetRotation;
            return;
        }
        
        // Use smooth step for more natural easing
        float smoothProgress = Mathf.SmoothStep(0f, 1f, recoveryProgress);
        
        // Lerp position with smoothProgress
        actualPosition = Vector3.Lerp(transform.position, recoveryTargetPosition, smoothProgress * Time.deltaTime * 2f);
        transform.position = actualPosition;
        
        // Smoothly rotate with separate rotation speed
        transform.rotation = Quaternion.Slerp(transform.rotation, recoveryTargetRotation, 
                                             Time.deltaTime * rotationRecoverySpeed);
        
        // Update the tangent line during recovery
        if (tangentLine != null && tangentLength > 0)
        {
            Vector3 forward = transform.forward;
            tangentLine.SetPosition(0, actualPosition);
            tangentLine.SetPosition(1, actualPosition + (forward * tangentLength));
        }
    }
    
    /// <summary>
    /// Manual position override that also triggers recovery
    /// </summary>
    public void SetPosition(Vector3 position)
    {
        transform.position = position;
        StartRecovery();
    }

//     private void OnDrawGizmosSelected()
//     {
//         Gizmos.color = Color.green;

//         Vector3 center = (useCenterTransform && centerPoint != null) ? centerPoint.position : centerPosition;

//         const int segments = 36;
//         Vector3 prevPoint = center + new Vector3(orbitRadius, 0, 0);

//         for (int i = 1; i <= segments; i++)
//         {
//             float angle = i * Mathf.PI * 2f / segments;
//             Vector3 nextPoint = center + new Vector3(Mathf.Sin(angle) * orbitRadius, 0, Mathf.Cos(angle) * orbitRadius);
//             Gizmos.DrawLine(prevPoint, nextPoint);
//             prevPoint = nextPoint;
//         }

//         Gizmos.color = Color.cyan;
//         Gizmos.DrawSphere(center, 0.3f);

//         float startRadians = startingAngle * Mathf.Deg2Rad;
//         Vector3 startPos = center + new Vector3(Mathf.Sin(startRadians) * orbitRadius, 0f, Mathf.Cos(startRadians) * orbitRadius);
//         Gizmos.color = Color.magenta;
//         Gizmos.DrawSphere(startPos, 0.25f);

//         if (Application.isPlaying)
//         {
//             Gizmos.color = Color.yellow;
//             Gizmos.DrawSphere(orbitPosition, 0.2f);

//             float angleRad = currentAngle * Mathf.Deg2Rad;
//             Vector3 tangentDirection = new Vector3(Mathf.Cos(angleRad), 0f, -Mathf.Sin(angleRad)).normalized;
//             Gizmos.color = Color.yellow;
//             Gizmos.DrawRay(orbitPosition, tangentDirection * tangentLength);

//             if (useRandomCycles)
//             {
//                 Gizmos.color = accelerating ? Color.red : Color.blue;
//                 float speedRatio = Mathf.InverseLerp(minSpeed, maxSpeed, orbitSpeed);
//                 Gizmos.DrawSphere(orbitPosition + Vector3.up * 0.5f, 0.1f + speedRatio * 0.2f);
//             }
            
//             // Draw recovery visualization when recovering
//             if (isRecovering)
//             {
//                 Gizmos.color = Color.red;
//                 Gizmos.DrawLine(transform.position, recoveryTargetPosition);
//                 Gizmos.DrawSphere(recoveryTargetPosition, 0.3f);
                
//                 // Show recovery progress
//                 Gizmos.color = new Color(1f, 0.5f, 0f); // Orange
//                 Gizmos.DrawSphere(transform.position, 0.15f + (0.2f * recoveryProgress));
//             }
//         }
//     }
 }