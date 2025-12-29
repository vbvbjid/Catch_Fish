using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR;
using Unity.XR.CoreUtils;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class VRPlayerMovementGrappling : MonoBehaviour
{
    [Header("VR References")]
    public Transform cameraTransform; // VR camera (Main Camera)
    
    [Header("Physics Body Setup")]
    public float colliderHeight = 1.8f;
    public float colliderRadius = 0.3f;
    
    [Header("Grappling Settings")]
    public float grappleFov = 95f;
    private Rigidbody rb;
    private CapsuleCollider capsuleCol;
    
    [Header("Movement Control")]
    public bool freeze;
    public bool activeGrapple;
    
    [Header("Ground Check")]
    public LayerMask whatIsGround;
    private bool grounded;

    public MovementState state;
    public enum MovementState
    {
        freeze,
        grappling,
        normal
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        // START KINEMATIC - only enable physics during grapple
        // rb.isKinematic = true;
        // rb.useGravity = false;
        
        capsuleCol = GetComponent<CapsuleCollider>();
        
        // If camera not assigned, use main camera
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        
        // Setup capsule collider
        if (capsuleCol != null)
        {
            capsuleCol.radius = colliderRadius;
            capsuleCol.height = colliderHeight;
            capsuleCol.center = new Vector3(0, colliderHeight / 2f, 0);
        }
    }

    private void Update()
    {
        // Ground check
        Vector3 colliderBottom = transform.position + new Vector3(0, colliderRadius, 0);
        grounded = Physics.Raycast(colliderBottom, Vector3.down, colliderRadius + 0.1f, whatIsGround);
        
        StateHandler();
        
        // Handle drag only when physics is active
        if (!rb.isKinematic)
        {
            if (grounded && !activeGrapple)
                rb.drag = 5f;
            else
                rb.drag = 0;
        }
    }

    private void StateHandler()
    {
        // Mode - Freeze (during grapple preparation)
        if (freeze)
        {
            state = MovementState.freeze;
            if (!rb.isKinematic)
            {
                rb.velocity = Vector3.zero;
            }
        }
        // Mode - Grappling (during grapple movement)
        else if (activeGrapple)
        {
            state = MovementState.grappling;
        }
        // Mode - Normal (kinematic, no physics)
        else
        {
            state = MovementState.normal;
            // Ensure kinematic when not grappling
            // if (!rb.isKinematic)
            // {
            //     rb.isKinematic = true;
            //     rb.useGravity = false;
            // }
        }
    }

    // Called by VR Grappling script to initiate grapple jump
    public void JumpToPosition(Vector3 targetPosition, float trajectoryHeight)
    {
        activeGrapple = true;

        // Enable physics for grappling
        rb.isKinematic = false;
        rb.useGravity = true;

        // Use camera position for trajectory calculation
        Vector3 startPosition = cameraTransform.position;
        Vector3 velocityToSet = CalculateJumpVelocity(startPosition, targetPosition, trajectoryHeight);
        
        // Apply velocity
        rb.velocity = velocityToSet;
        
        // Auto-reset after 3 seconds as safety measure
        Invoke(nameof(ResetRestrictions), 3f);
    }

    public void ResetRestrictions()
    {
        activeGrapple = false;
        freeze = false;
        
        // Disable physics after grapple
        // rb.isKinematic = true;
        // rb.useGravity = false;
        // rb.velocity = Vector3.zero;
    }

    // Calculate the velocity needed for a ballistic trajectory
    public Vector3 CalculateJumpVelocity(Vector3 startPoint, Vector3 endPoint, float trajectoryHeight)
    {
        float gravity = Physics.gravity.y;
        float displacementY = endPoint.y - startPoint.y;
        Vector3 displacementXZ = new Vector3(endPoint.x - startPoint.x, 0f, endPoint.z - startPoint.z);

        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * trajectoryHeight);
        Vector3 velocityXZ = displacementXZ / (Mathf.Sqrt(-2 * trajectoryHeight / gravity) 
            + Mathf.Sqrt(2 * (displacementY - trajectoryHeight) / gravity));

        return velocityXZ + velocityY;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Reset grapple state on collision
        if (activeGrapple)
        {
            ResetRestrictions();
            
            // Notify grappling script if it exists
            VRGrappling grappleScript = GetComponent<VRGrappling>();
            if (grappleScript != null)
            {
                grappleScript.StopGrapple();
            }
        }
    }

    // Freeze player in place (called when starting grapple)
    public void FreezePlayer()
    {
        freeze = true;
        if (!rb.isKinematic)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    // Unfreeze player
    public void UnfreezePlayer()
    {
        freeze = false;
    }

    private void OnDrawGizmos()
    {
        // Visualize the collider in editor
        if (capsuleCol != null)
        {
            Gizmos.color = grounded ? Color.green : Color.red;
            Vector3 center = transform.position + capsuleCol.center;
            
            // Draw wireframe capsule
            Gizmos.DrawWireSphere(center + Vector3.up * (capsuleCol.height / 2f - capsuleCol.radius), capsuleCol.radius);
            Gizmos.DrawWireSphere(center - Vector3.up * (capsuleCol.height / 2f - capsuleCol.radius), capsuleCol.radius);
        }
    }
}