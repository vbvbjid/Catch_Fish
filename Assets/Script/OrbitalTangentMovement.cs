using UnityEngine;

public class OrbitalTangentMovement : MonoBehaviour
{
    [Header("Center Settings")]
    public bool useCenterTransform = true;   // Whether to use a Transform reference or Vector3 position
    public Transform centerPoint;            // The center point Transform reference
    public Vector3 centerPosition;           // Direct position for the center point

    [Header("Orbit Settings")]
    public float orbitRadius = 5f;           // Radius of the orbit
    public float startingAngle = 0f;         // Starting angle in degrees (0 = positive X axis)
    public float orbitSpeed = 30f;           // Degrees per second
    public bool useLocalSpace = false;       // Whether to use local or world space

    [Header("Speed Cycle Settings")]
    public float minSpeed = 10f;             // Minimum orbital speed
    public float maxSpeed = 50f;             // Maximum orbital speed
    public float minCycleTime = 3f;          // Minimum time for a speed-up or slow-down cycle
    public float maxCycleTime = 8f;          // Maximum time for a speed-up or slow-down cycle
    public bool useRandomCycles = true;      // Whether to use random speed cycles

    [Header("Tangent Settings")]
    public float tangentLength = 2f;         // Length of the tangent line visualization
    
    [Header("Orbit Recovery Settings")]
    public bool enableOrbitRecovery = false;  // Whether to recover orbit after displacement
    public float maxAllowedDeviation = 1.0f; // Maximum allowed distance from orbit path
    public float recoverySpeed = 2.0f;       // Speed of return to orbit (higher = faster)
    public bool visualizeDeviation = true;   // Whether to show deviation in the editor
    
    // Visual representation of the tangent (optional)
    private LineRenderer tangentLine;
    
    private float currentAngle = 0f;
    private Vector3 orbitPosition;           // Ideal orbit position
    private Vector3 actualPosition;          // Actual position (may deviate from orbit)
    private bool isRecovering = false;       // Whether currently in recovery mode
    
    // Speed cycle variables
    private float targetSpeed;
    private float cycleTimer;
    private float currentCycleDuration;
    private bool accelerating = true;
    
    // For tracking if we've been moved by external forces
    private bool positionManuallySet = false;
    // Check if position was changed externally (like by physics or manual positioning)
    private void LateUpdate()
    {
        if (enableOrbitRecovery && !positionManuallySet)
        {
            // Compare current position with what we set
            float positionDiff = Vector3.Distance(transform.position, actualPosition);

            // If the position was changed by something else, mark it
            if (positionDiff > 0.01f)
            {
                positionManuallySet = true;
            }
        }
    }
    
    private void FixedUpdate()
    {
        // Reset the flag so we can detect changes again
        positionManuallySet = false;
    }    // Get the center position based on settings
    private Vector3 GetCenterPosition()
    {
        if (useCenterTransform && centerPoint != null)
        {
            return centerPoint.position;
        }
        else
        {
            return centerPosition;
        }
    }
    
    // Calculate and update the orbit position and orientation
    private void UpdatePosition()
    {
        // Convert angle to radians for calculations
        float angleRad = currentAngle * Mathf.Deg2Rad;
        
        // Calculate position on XZ plane orbit
        Vector3 orbitOffset = new Vector3(
            Mathf.Sin(angleRad) * orbitRadius,
            0f,
            Mathf.Cos(angleRad) * orbitRadius
        );
        
        // Set position based on center point
        Vector3 centerPos = GetCenterPosition();
        if (useLocalSpace && centerPoint != null && useCenterTransform)
        {
            orbitPosition = centerPoint.TransformPoint(orbitOffset);
        }
        else
        {
            orbitPosition = centerPos + orbitOffset;
        }
        
        // Calculate tangent direction (perpendicular to radius vector on XZ plane)
        Vector3 tangentDirection = new Vector3(
            Mathf.Cos(angleRad),
            0f,
            -Mathf.Sin(angleRad)
        ).normalized;
        
        // Check if we need to recover from displacement
        if (enableOrbitRecovery)
        {
            // Get the actual current position (which may have been altered by external forces)
            actualPosition = transform.position;
            
            // Calculate distance from ideal orbit position
            float deviationDistance = Vector3.Distance(actualPosition, orbitPosition);
            
            // If we're too far from the orbit, gradually recover
            if (deviationDistance > maxAllowedDeviation)
            {
                isRecovering = true;
                
                // Maintain the Y coordinate (height) if there's a significant difference
                float targetY = Mathf.Abs(actualPosition.y - orbitPosition.y) > 0.5f ? 
                    actualPosition.y : orbitPosition.y;
                
                // Lerp toward the correct orbit position
                actualPosition = Vector3.Lerp(
                    actualPosition, 
                    new Vector3(orbitPosition.x, targetY, orbitPosition.z), 
                    Time.deltaTime * recoverySpeed
                );
                
                transform.position = actualPosition;
            }
            else
            {
                isRecovering = false;
                // When close enough, snap exactly to orbit
                if (deviationDistance < 0.05f)
                {
                    actualPosition = orbitPosition;
                    transform.position = actualPosition;
                }
            }
        }
        else
        {
            // No recovery logic, just set to orbit position
            actualPosition = orbitPosition;
            transform.position = actualPosition;
        }
        
        // Set object rotation to face along the tangent direction
        transform.rotation = Quaternion.LookRotation(tangentDirection);
        
        // Update tangent line visualization if available
        if (tangentLine != null && tangentLength > 0)
        {
            tangentLine.SetPosition(0, actualPosition);
            tangentLine.SetPosition(1, actualPosition + (tangentDirection * tangentLength));
        }
    }
    private void Start()
    {
        enableOrbitRecovery = false;
        // Set starting angle
        currentAngle = startingAngle;
        
        // If using transform but none assigned, create an empty GameObject at origin
        if (useCenterTransform && centerPoint == null)
        {
            GameObject center = new GameObject("Orbit Center");
            center.transform.position = Vector3.zero;
            centerPoint = center.transform;
        }
        
        // Initialize speed cycle
        InitializeNewSpeedCycle();
        
        // Initial position update
        UpdatePosition();
        actualPosition = orbitPosition;
        transform.position = actualPosition;
    }
    
    // Start a new speed cycle with random parameters
    private void InitializeNewSpeedCycle()
    {
        cycleTimer = 0f;
        
        // Determine if we're speeding up or slowing down
        accelerating = !accelerating;
        
        // Set target speed based on acceleration direction
        targetSpeed = accelerating ? maxSpeed : minSpeed;
        
        // Set a random duration for this cycle
        currentCycleDuration = Random.Range(minCycleTime, maxCycleTime);
    }

    private void Update()
    {
        // Handle speed cycle
        if (useRandomCycles)
        {
            cycleTimer += Time.deltaTime;
            
            // Check if it's time to change speed cycle
            if (cycleTimer >= currentCycleDuration)
            {
                InitializeNewSpeedCycle();
            }
            
            // Smoothly interpolate speed
            orbitSpeed = Mathf.Lerp(orbitSpeed, targetSpeed, Time.deltaTime);
        }
        
        // Update the angle based on orbit speed
        currentAngle += orbitSpeed * Time.deltaTime;
        
        // Ensure angle stays within 0-360 range
        if (currentAngle >= 360f)
            currentAngle -= 360f;
        
        // Update position and orientation
        UpdatePosition();
    }

    // Draw the orbit path in the editor for visualization
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        
        Vector3 center = useCenterTransform && centerPoint != null ? centerPoint.position : centerPosition;
        
        // Draw orbit circle on XZ plane
        const int segments = 36;
        Vector3 prevPoint = center + new Vector3(orbitRadius, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector3 nextPoint = center + new Vector3(Mathf.Sin(angle) * orbitRadius, 0, Mathf.Cos(angle) * orbitRadius);
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
        
        // Mark center position
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(center, 0.3f);
        
        // Draw starting position
        float startRadians = startingAngle * Mathf.Deg2Rad;
        Vector3 startPos = center + new Vector3(
            Mathf.Sin(startRadians) * orbitRadius,
            0f,
            Mathf.Cos(startRadians) * orbitRadius);
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(startPos, 0.25f);
        
        // Draw current position and tangent if in play mode
        if (Application.isPlaying)
        {
            // Draw ideal orbit position
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(orbitPosition, 0.2f);
            
            // Draw actual position and recovery visualization
            if (enableOrbitRecovery && visualizeDeviation)
            {
                // Draw actual position
                Gizmos.color = isRecovering ? Color.red : Color.green;
                Gizmos.DrawSphere(actualPosition, 0.15f);
                
                // Draw line between actual and ideal position if recovering
                if (isRecovering)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(actualPosition, orbitPosition);
                    
                    // Draw max deviation sphere
                    Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f); // Semi-transparent orange
                    Gizmos.DrawWireSphere(orbitPosition, maxAllowedDeviation);
                }
            }
            
            // Calculate tangent direction
            float angleRad = currentAngle * Mathf.Deg2Rad;
            Vector3 tangentDirection = new Vector3(
                Mathf.Cos(angleRad),
                0f,
                -Mathf.Sin(angleRad)
            ).normalized;
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(orbitPosition, tangentDirection * tangentLength);
            
            // Show speed indicator
            if (useRandomCycles)
            {
                Gizmos.color = accelerating ? Color.red : Color.blue;
                float speedRatio = Mathf.InverseLerp(minSpeed, maxSpeed, orbitSpeed);
                Gizmos.DrawSphere(orbitPosition + Vector3.up * 0.5f, 0.1f + speedRatio * 0.2f);
            }
        }
    }
}