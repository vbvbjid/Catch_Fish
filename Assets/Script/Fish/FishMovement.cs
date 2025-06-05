// ====================
// FishMovement.cs - Movement & Orbit Logic
// ====================
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
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
    
    [Header("Detection Settings")]
    public float detectionDistance = 2f;
    public LayerMask playerLayer = -1;
    public bool enableFlee = true;
    
    [Header("Movement Settings")]
    public float fleeForce = 2f;
    public float recoverySpeed = 2f;
    public float rotationSpeed = 90f;
    public float fleeSpeedMultiplier = 2.2f;
    public float maxFleeDistance = 1.5f;
    
    // Private variables
    private FishAI2 fishAI;
    private float currentAngle = 0f;
    private float currentSpeed;
    private float targetSpeed;
    private Vector3 originalOrbitCenter;
    private Vector3 fleeDirection = Vector3.zero;
    private List<Transform> nearbyPlayers = new List<Transform>();
    private Vector3 currentPosition;
    private Vector3 targetPosition;
    private Vector3 recoveryStartPos;
    private float recoveryProgress = 0f;
    
    public float CurrentAngle
    {
        get { return currentAngle; }
        set { currentAngle = value; }
    }
    
    public void Initialize(FishAI2 ai)
    {
        fishAI = ai;
        originalOrbitCenter = orbitCenter;
        
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
    
    public void UpdateMovement()
    {
        currentPosition = Vector3.Lerp(currentPosition, targetPosition, Time.deltaTime * 5f);
        transform.position = currentPosition;
        UpdateRotation();
    }
    
    public void UpdateIdle()
    {
        // Speed variation logic
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * speedChangeRate);
        currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);
        
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
    }
    
    public void UpdateFleeing()
    {
        if (!enableFlee)
        {
            fishAI.TransitionToIdle();
            return;
        }
        
        currentAngle += currentSpeed * fleeSpeedMultiplier * Time.deltaTime;
        if (currentAngle >= 360f) currentAngle -= 360f;
        
        Vector3 baseOrbitPos = GetOrbitPosition(currentAngle);
        CalculateFleeDirection();
        
        Vector3 fleeOffset = fleeDirection * Mathf.Min(fleeForce, maxFleeDistance);
        targetPosition = baseOrbitPos + fleeOffset;
        
        CheckForPlayers();
        
        if (nearbyPlayers.Count == 0)
        {
            fleeDirection = Vector3.Lerp(fleeDirection, Vector3.zero, Time.deltaTime * 2f);
            
            if (fleeDirection.magnitude < 0.1f)
            {
                fishAI.TransitionToIdle();
            }
        }
    }
    
    public void UpdateGrabbed()
    {
        currentPosition = transform.position;
        targetPosition = currentPosition;
    }
    
    public void UpdateRecovering()
    {
        recoveryProgress += Time.deltaTime * recoverySpeed;
        
        Vector3 currentPos = transform.position;
        Vector3 toFish = currentPos - originalOrbitCenter;
        toFish.y = 0;
        
        Vector3 closestOrbitPoint;
        if (toFish.magnitude > 0.001f)
        {
            Vector3 directionToFish = toFish.normalized;
            closestOrbitPoint = originalOrbitCenter + directionToFish * orbitRadius;
        }
        else
        {
            closestOrbitPoint = originalOrbitCenter + Vector3.forward * orbitRadius;
        }
        
        targetPosition = Vector3.Lerp(recoveryStartPos, closestOrbitPoint, recoveryProgress);
        
        float distanceToOrbit = Vector3.Distance(transform.position, closestOrbitPoint);
        
        if (distanceToOrbit < 0.5f || recoveryProgress >= 0.8f)
        {
            Vector3 offsetFromCenter = closestOrbitPoint - originalOrbitCenter;
            currentAngle = Mathf.Atan2(offsetFromCenter.z, offsetFromCenter.x) * Mathf.Rad2Deg;
            
            float orbitBlend = Mathf.Clamp01((recoveryProgress - 0.8f) / 0.2f);
            Vector3 orbitPos = GetOrbitPosition(currentAngle);
            targetPosition = Vector3.Lerp(targetPosition, orbitPos, orbitBlend);
            
            currentAngle += currentSpeed * Time.deltaTime * orbitBlend;
        }
        
        if (recoveryProgress >= 1f)
        {
            fishAI.TransitionToIdle();
        }
    }
    
    public bool HasNearbyThreats()
    {
        if (!enableFlee) return false;
        
        CheckForPlayers();
        return nearbyPlayers.Count > 0;
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
        return originalOrbitCenter + offset;
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
    
    // State callbacks
    public void OnEnterIdle()
    {
        fleeDirection = Vector3.zero;
        targetSpeed = baseOrbitSpeed + Random.Range(-speedVariationRange * 0.3f, speedVariationRange * 0.3f);
    }
    
    public void OnEnterFleeing()
    {
        currentSpeed = Mathf.Min(currentSpeed * 1.8f, maxSpeed);
        targetSpeed = currentSpeed;
    }
    
    public void OnEnterGrabbed()
    {
        // Position controlled by grab system
    }
    
    public void OnEnterRecovering()
    {
        recoveryStartPos = transform.position;
        recoveryProgress = 0f;
    }
    
    public void ResetState()
    {
        fleeDirection = Vector3.zero;
        recoveryProgress = 0f;
        SetCorrectRotation();
    }
    
    public void ForcePositionUpdate()
    {
        UpdateIdlePosition();
        currentPosition = targetPosition;
        transform.position = currentPosition;
        SetCorrectRotation();
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
    
    public void DrawGizmos()
    {
        // Draw orbit path
        if (orbitCenter != null)
        {
            Gizmos.color = Color.cyan;
            DrawWireCircle(orbitCenter, orbitRadius);
        }
        
        // Draw detection radius
        if (enableFlee)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionDistance);
        }
        
        // Draw flee direction
        if (fishAI.currentState == FishAI2.FishState.Fleeing && enableFlee)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, fleeDirection * 2f);
        }
        
        // Draw current target position
        Gizmos.color = Color.white;
        Gizmos.DrawSphere(targetPosition, 0.1f);
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
}
