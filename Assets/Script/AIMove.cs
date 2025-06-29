using System.Collections;
using System.Collections.Generic;
using Meta.WitAi;
using UnityEngine;

public class AIMove : MonoBehaviour
{
    private FishAudioManager AudioManager;
    private AISpawner m_AIManager;

    private bool m_hasTarget = false;
    private bool m_isTruning;
    private bool m_isFleeing = false; // New variable to track flee state

    private Vector3 m_wayPoint;
    private Vector3 m_lastWaypoint = new Vector3(0f, 0f, 0f);

    private Animator m_animator;
    private float m_speed;

    private Collider m_collider;
    private RaycastHit m_hit;
    [SerializeField]
    private bool isGrabbed = false;

    [SerializeField]
    private float fleeDistance = 15f; // How far to flee
    [SerializeField]
    private float fleeSpeedMultiplier = 2f; // How much faster to move when fleeing

    void Start()
    {
        AudioManager = transform.GetComponent<FishAudioManager>();
        m_AIManager = transform.parent.GetComponentInParent<AISpawner>();
        m_animator = GetComponent<Animator>();

        SetUpNPC();
    }

    public void PlayerDetection()
    {
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        
        // Use collider bounds for detection instead of raycast
        Vector3 detectionCenter = transform.position + transform.forward * 5f; // 5 units forward
        Vector3 detectionSize = new Vector3(capsule.radius * 2f, capsule.height, 10f); // Width, height, detection distance
        
        // Create detection bounds based on capsule collider shape
        Bounds detectionBounds = new Bounds(detectionCenter, detectionSize);
        
        // Find all colliders with "Player" tag in the scene
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        
        foreach (GameObject player in players)
        {
            Collider playerCollider = player.GetComponent<Collider>();
            if (playerCollider != null)
            {
                // Check if player's bounds intersect with our detection bounds
                if (detectionBounds.Intersects(playerCollider.bounds))
                {
                    // Additional forward-facing check using dot product
                    Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;
                    float dotProduct = Vector3.Dot(transform.forward, directionToPlayer);
                    
                    // Only detect if player is roughly in front (dot product > 0.3 means within ~70 degrees)
                    if (dotProduct > 0.3f)
                    {
                        Debug.Log("Alert!! Player detected - initiating flee behavior");
                        InitiateFlee(player.transform.position);
                        return; // Exit after detecting first player
                    }
                }
            }
            else
            {
                // Fallback: use transform position if no collider
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance <= 10f)
                {
                    Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;
                    float dotProduct = Vector3.Dot(transform.forward, directionToPlayer);
                    
                    if (dotProduct > 0.3f)
                    {
                        Debug.Log("Alert!! Player detected - initiating flee behavior");
                        InitiateFlee(player.transform.position);
                        return;
                    }
                }
            }
        }
    }

    void InitiateFlee(Vector3 playerPosition)
    {
        // Calculate flee direction (opposite to player)
        Vector3 fleeDirection = (transform.position - playerPosition).normalized;
        
        // Set flee waypoint
        m_wayPoint = transform.position + fleeDirection * fleeDistance;
        
        // Ensure the flee point is within bounds (you might want to add bounds checking here)
        // m_wayPoint = ClampToPlayArea(m_wayPoint); // Implement this method if needed
        
        // Set flee state
        m_isFleeing = true;
        m_hasTarget = true;
        
        // Increase speed for fleeing
        m_speed = Random.Range(3f, 8f) * fleeSpeedMultiplier;
        m_animator.speed = m_speed / 2;

        AudioManager.PlayFleeSound();
        Debug.Log($"Fleeing to position: {m_wayPoint}");
    }

    void SetUpNPC()
    {
        float m_scale = Random.Range(0.2f, 1f);
        transform.localScale += new Vector3(m_scale * 1.5f, m_scale, m_scale);

        if (transform.GetComponent<Collider>() != null && transform.GetComponent<Collider>().enabled == true)
        {
            m_collider = transform.GetComponent<Collider>();
        }
    }

    void Update()
    {
        if (isGrabbed) return;
        
        // Check for player detection every frame
        PlayerDetection();
        
        if (!m_hasTarget)
        {
            m_hasTarget = CanFindTarget();
        }
        else
        {
            RotateNPC(m_wayPoint, m_speed);
            transform.position = Vector3.MoveTowards(transform.position, m_wayPoint, m_speed * Time.deltaTime);
            ColliderNPC();
        }

        if (transform.position == m_wayPoint)
        {
            m_hasTarget = false;
            
            // Reset flee state when reaching flee destination
            if (m_isFleeing)
            {
                m_isFleeing = false;
                Debug.Log("Flee behavior completed");
            }
        }
    }

    void ColliderNPC()
    {
        RaycastHit hit;
        int layerMask = LayerMask.GetMask("Water", "Ground"); // only hit these layers
        if (Physics.Raycast(transform.position, transform.forward, out hit, transform.localScale.z, layerMask))
        {
            if (hit.collider == m_collider | hit.collider.tag == "WayPoint")
            {
                return;
            }
            
            // When fleeing, be more aggressive about finding new paths
            int randomNum = Random.Range(1, 100);
            int threshold = m_isFleeing ? 60 : 40; // Higher chance to change direction when fleeing
            
            if (randomNum < threshold)
            {
                m_hasTarget = false;
                
                // If we're fleeing and hit an obstacle, try to find an alternative flee path
                if (m_isFleeing)
                {
                    // Find a new flee direction
                    Vector3 alternateDirection = Vector3.Cross(transform.forward, Vector3.up).normalized;
                    if (Random.Range(0, 2) == 0)
                        alternateDirection = -alternateDirection;
                    
                    m_wayPoint = transform.position + alternateDirection * fleeDistance;
                    m_hasTarget = true;
                }
            }
            
            if (hit.collider.transform.parent == null)
            {
                Debug.Log(hit.collider.transform.name + " " + hit.collider.transform.position);
            }
            else
            {
                Debug.Log(hit.collider.transform.parent.name + " " + hit.collider.transform.parent.position);
            }
        }
    }

    Vector3 GetWaypoint(bool isRandom)
    {
        if (isRandom)
        {
            return m_AIManager.RandomPosition();
        }
        else
        {
            return m_AIManager.RandomWayPoint();
        }
    }

    bool CanFindTarget(float start = 1f, float end = 7f)
    {
        // Don't override flee behavior with normal waypoint finding
        if (m_isFleeing)
        {
            return true;
        }
        
        m_wayPoint = m_AIManager.RandomWayPoint();
        if (m_lastWaypoint == m_wayPoint)
        {
            m_wayPoint = GetWaypoint(true);
            return false;
        }
        else
        {
            m_lastWaypoint = m_wayPoint;
            m_speed = Random.Range(start, end);
            m_animator.speed = m_speed / 2;
            return true;
        }
    }

    void RotateNPC(Vector3 waypoint, float currentSpeed)
    {
        float TurnSpeed = currentSpeed * Random.Range(1f, 3f);
        
        // Turn faster when fleeing
        if (m_isFleeing)
        {
            TurnSpeed *= 1.5f;
        }

        Vector3 LookAt = waypoint - this.transform.position;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(LookAt), TurnSpeed * Time.deltaTime);
    }

    public void ToggleGrab()
    {
        isGrabbed = !isGrabbed;
    }
}