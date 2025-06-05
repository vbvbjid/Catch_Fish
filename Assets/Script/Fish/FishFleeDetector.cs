// ============================================================================
// 5. FISH FLEE DETECTOR (Player detection + flee direction)
// ============================================================================
using UnityEngine;
using System.Collections.Generic;
using System;

public class FishFleeDetector : MonoBehaviour
{
    [Header("Detection Settings")]
    public float detectionDistance = 2f;
    public LayerMask playerLayer = -1;
    
    // Events
    public event Action OnPlayerDetected;
    public event Action OnPlayerLost;
    
    // Private state
    private List<Transform> nearbyPlayers = new List<Transform>();
    private Vector3 fleeDirection = Vector3.zero;
    private bool playersDetected = false;
    
    void Update()
    {
        CheckForPlayers();
        CalculateFleeDirection();
        HandlePlayerDetectionEvents();
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
            
            float influence = Mathf.Pow(detectionDistance / Mathf.Max(distance, 0.1f), 2f);
            combinedThreat += directionToPlayer.normalized * influence;
        }
        
        if (combinedThreat.magnitude > 0.1f)
        {
            Vector3 targetFleeDirection = -combinedThreat.normalized;
            fleeDirection = Vector3.Lerp(fleeDirection, targetFleeDirection, Time.deltaTime * 4f);
        }
    }
    
    void HandlePlayerDetectionEvents()
    {
        bool hasPlayers = nearbyPlayers.Count > 0;
        
        if (hasPlayers && !playersDetected)
        {
            playersDetected = true;
            OnPlayerDetected?.Invoke();
        }
        else if (!hasPlayers && playersDetected)
        {
            playersDetected = false;
            OnPlayerLost?.Invoke();
        }
    }
    
    public Vector3 GetFleeDirection() => fleeDirection;
    
    void OnDrawGizmos()
    {
        // Detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionDistance);
        
        // Flee direction
        if (fleeDirection.magnitude > 0.1f)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, fleeDirection * 2f);
        }
    }
}