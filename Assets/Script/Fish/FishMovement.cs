using UnityEngine;

public class FishMovement : MonoBehaviour
{
    [Header("Orbit Settings")]
    public Vector3 orbitCenter;
    public float orbitRadius = 3f;
    public float baseOrbitSpeed = 30f;
    public float speedVariationRange = 1.5f;
    public float speedChangeRate = 0.8f;
    public float minSpeed = 10f;
    public float maxSpeed = 80f;

    [Header("Movement Settings")]
    public float rotationSpeed = 90f;
    public float fleeSpeedMultiplier = 2.2f;
    public float recoverySpeed = 2f;

    // Movement state
    private float currentAngle = 0f;
    public float currentSpeed; // Made public so spawner can access it
    private float targetSpeed;
    private Vector3 originalOrbitCenter;
    private Vector3 currentPosition;
    private Vector3 targetPosition;

    public float CurrentAngle
    {
        get { return currentAngle; }
        set { currentAngle = value; }
    }

    public Vector3 CurrentPosition => currentPosition;
    public Vector3 TargetPosition => targetPosition;

    // Add method to set current position when fish is grabbed
    public void SetCurrentPosition(Vector3 position)
    {
        currentPosition = position;
        targetPosition = position;
    }

    void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        originalOrbitCenter = orbitCenter;
        
        // Set random starting angle
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

    public void UpdateMovement(float deltaTime)
    {
        // Only apply smooth movement if we're not being grabbed/controlled externally
        // This prevents fighting with external position control (like VR grab systems)
        if (Vector3.Distance(currentPosition, transform.position) < 0.1f)
        {
            // Normal case: smooth movement
            currentPosition = Vector3.Lerp(currentPosition, targetPosition, deltaTime * 5f);
            transform.position = currentPosition;
        }
        else
        {
            // Fish position has been changed externally (e.g., by grab system)
            // Update our tracking to match
            currentPosition = transform.position;
        }

        // Update rotation
        UpdateRotation(deltaTime);
    }

    public void UpdateOrbitMovement(float deltaTime)
    {
        // Gradually change speed for natural variation
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, deltaTime * speedChangeRate);
        currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);

        // Speed variation logic
        if (Random.Range(0f, 1f) < deltaTime * 0.8f)
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
        currentAngle += currentSpeed * deltaTime;
        if (currentAngle >= 360f) currentAngle -= 360f;

        UpdateIdlePosition();
    }

    public void UpdateFleeMovement(float deltaTime, Vector3 fleeDirection, float maxFleeDistance, float fleeForce)
    {
        // Continue orbital motion with increased speed during flee
        currentAngle += currentSpeed * fleeSpeedMultiplier * deltaTime;
        if (currentAngle >= 360f) currentAngle -= 360f;

        Vector3 baseOrbitPos = GetOrbitPosition(currentAngle);
        Vector3 fleeOffset = fleeDirection * Mathf.Min(fleeForce, maxFleeDistance);
        targetPosition = baseOrbitPos + fleeOffset;
    }

    public void UpdateRecoveryMovement(float deltaTime, Vector3 recoveryStartPos, float recoveryProgress)
    {
        // Find closest point on orbit
        Vector3 currentPos = transform.position;
        Vector3 toFish = currentPos - orbitCenter;
        toFish.y = 0; // Only consider horizontal distance

        Vector3 closestOrbitPoint;
        float targetAngle;
        
        if (toFish.magnitude > 0.001f)
        {
            Vector3 directionToFish = toFish.normalized;
            closestOrbitPoint = orbitCenter + directionToFish * orbitRadius;
            // Calculate the angle for this closest point
            targetAngle = Mathf.Atan2(directionToFish.z, directionToFish.x) * Mathf.Rad2Deg;
        }
        else
        {
            closestOrbitPoint = orbitCenter + Vector3.forward * orbitRadius;
            targetAngle = 0f;
        }

        // Phase 1: Move towards the orbit (0 to 0.8)
        if (recoveryProgress < 0.8f)
        {
            // Smoothly swim toward the closest orbit point without changing angle
            targetPosition = Vector3.Lerp(recoveryStartPos, closestOrbitPoint, recoveryProgress / 0.8f);
        }
        else
        {
            // Phase 2: Snap to orbit and start following it (0.8 to 1.0)
            float orbitBlendProgress = (recoveryProgress - 0.8f) / 0.2f; // 0 to 1 over the last 20%
            
            // Smoothly transition the current angle to the target angle
            if (orbitBlendProgress == 0f)
            {
                // First frame of phase 2 - set the angle to match closest orbit point
                currentAngle = targetAngle;
            }
            
            // Now start orbital movement
            currentAngle += currentSpeed * deltaTime * orbitBlendProgress;
            if (currentAngle >= 360f) currentAngle -= 360f;
            if (currentAngle < 0f) currentAngle += 360f;
            
            // Calculate position based on current angle
            Vector3 orbitPos = GetOrbitPosition(currentAngle);
            
            // Blend between closest point and calculated orbit position
            targetPosition = Vector3.Lerp(closestOrbitPoint, orbitPos, orbitBlendProgress);
        }
    }

    public void SetOrbitCenter(Vector3 newOrbitCenter)
    {
        orbitCenter = newOrbitCenter;
        originalOrbitCenter = newOrbitCenter;
    }

    public void ForcePositionUpdate()
    {
        UpdateIdlePosition();
        currentPosition = targetPosition;
        transform.position = currentPosition;
        SetCorrectRotation();
    }

    public Vector3 GetTangentDirection()
    {
        float radians = currentAngle * Mathf.Deg2Rad;
        return new Vector3(-Mathf.Sin(radians), 0f, Mathf.Cos(radians));
    }

    private void UpdateIdlePosition()
    {
        targetPosition = GetOrbitPosition(currentAngle);
    }

    private Vector3 GetOrbitPosition(float angle)
    {
        float radians = angle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            Mathf.Cos(radians) * orbitRadius,
            0f,
            Mathf.Sin(radians) * orbitRadius
        );
        return orbitCenter + offset;
    }

    private void UpdateRotation(float deltaTime)
    {
        Vector3 movementDirection = (targetPosition - currentPosition).normalized;

        if (movementDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movementDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation,
                rotationSpeed * deltaTime);
        }
    }

    private void SetCorrectRotation()
    {
        Vector3 movementDirection = GetTangentDirection();

        if (movementDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movementDirection);
            transform.rotation = targetRotation;
        }
    }

    public void IncreaseFleeSpeed()
    {
        currentSpeed = Mathf.Min(currentSpeed * 1.8f, maxSpeed);
        targetSpeed = currentSpeed;
    }

    public void ResetSpeed()
    {
        targetSpeed = baseOrbitSpeed + Random.Range(-speedVariationRange * 0.3f, speedVariationRange * 0.3f);
    }
}