using System.Collections.Generic;
using UnityEngine;

public class FishFleeBehavior : MonoBehaviour
{
    [Header("Detection Settings")]
    public float detectionDistance = 2f;
    public float safeDistance = 4f;
    public LayerMask playerLayer = -1;

    [Header("Flee Settings")]
    public float fleeForce = 2f;
    public float maxFleeDistance = 1.5f;
    [Tooltip("Enable/disable flee behavior for debugging")]
    public bool enableFlee = true;

    // Fleeing variables
    private Vector3 fleeDirection = Vector3.zero;
    private List<Transform> nearbyPlayers = new List<Transform>();

    public Vector3 FleeDirection => fleeDirection;
    public bool HasNearbyPlayers => nearbyPlayers.Count > 0;
    public int NearbyPlayerCount => nearbyPlayers.Count;

    public void UpdateFleeDetection()
    {
        if (!enableFlee) return;

        CheckForPlayers();
    }

    public void UpdateFleeDirection(float deltaTime)
    {
        if (!enableFlee)
        {
            fleeDirection = Vector3.zero;
            return;
        }

        CalculateFleeDirection(deltaTime);
    }

    public bool ShouldStartFleeing()
    {
        return enableFlee && nearbyPlayers.Count > 0;
    }

    public bool ShouldStopFleeing()
    {
        if (!enableFlee) return true;

        CheckForPlayers();

        if (nearbyPlayers.Count == 0)
        {
            // Gradually reduce flee force when no threats
            fleeDirection = Vector3.Lerp(fleeDirection, Vector3.zero, Time.deltaTime * 2f);

            // Return to idle when flee force is minimal
            return fleeDirection.magnitude < 0.1f;
        }

        return false;
    }

    public void ResetFleeDirection()
    {
        fleeDirection = Vector3.zero;
        nearbyPlayers.Clear();
    }

    private void CheckForPlayers()
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

    private void CalculateFleeDirection(float deltaTime)
    {
        if (nearbyPlayers.Count == 0)
        {
            fleeDirection = Vector3.Lerp(fleeDirection, Vector3.zero, deltaTime * 3f);
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
            
            // Get tangent direction from movement component if available
            FishMovement movement = GetComponent<FishMovement>();
            Vector3 tangentDir = movement != null ? movement.GetTangentDirection() : Vector3.forward;
            
            Vector3 fluidFleeDir = (rawFleeDir * 0.7f + tangentDir * 0.3f).normalized;

            Vector3 targetFleeDirection = fluidFleeDir * Mathf.Clamp01(combinedThreat.magnitude);
            fleeDirection = Vector3.Lerp(fleeDirection, targetFleeDirection, deltaTime * 4f);
        }
    }

#if UNITY_EDITOR
    public void DrawDebugGizmos()
    {
        if (!enableFlee) return;

        // Draw detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionDistance);

        // Draw flee direction
        if (fleeDirection.magnitude > 0.1f)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, fleeDirection * 2f);
        }
    }
#endif
}