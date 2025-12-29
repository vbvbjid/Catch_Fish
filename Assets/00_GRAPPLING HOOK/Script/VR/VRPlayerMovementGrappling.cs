using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR;
using Unity.XR.CoreUtils;

[RequireComponent(typeof(Rigidbody))]
public class VRPlayerMovementGrappling : MonoBehaviour
{
    [Header("VR References")]
    public XROrigin xrOrigin; // Or XRRig depending on your XR Interaction Toolkit version
    public Transform cameraTransform; // VR camera
    
    [Header("Grappling Settings")]
    public float grappleFov = 95f;
    private Rigidbody rb;
    
    [Header("Movement Control")]
    public bool freeze;
    public bool activeGrapple;
    
    [Header("Ground Check")]
    public float playerHeight = 2f;
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
        
        // If XR Origin not assigned, try to find it
        if (xrOrigin == null)
        {
            xrOrigin = GetComponentInParent<XROrigin>();
        }
        
        // If camera not assigned, use main camera
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        // Ground check
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);
        
        StateHandler();
        
        // Handle drag
        if (grounded && !activeGrapple)
            rb.drag = 5f;
        else
            rb.drag = 0;
    }

    private void StateHandler()
    {
        // Mode - Freeze (during grapple preparation)
        if (freeze)
        {
            state = MovementState.freeze;
            rb.velocity = Vector3.zero;
        }
        // Mode - Grappling (during grapple movement)
        else if (activeGrapple)
        {
            state = MovementState.grappling;
        }
        // Mode - Normal
        else
        {
            state = MovementState.normal;
        }
    }

    // Called by VR Grappling script to initiate grapple jump
    public void JumpToPosition(Vector3 targetPosition, float trajectoryHeight)
    {
        activeGrapple = true;

        Vector3 velocityToSet = CalculateJumpVelocity(transform.position, targetPosition, trajectoryHeight);
        
        // Small delay before applying velocity
        StartCoroutine(SetVelocityDelayed(velocityToSet, 0.1f));
        
        // Auto-reset after 3 seconds as safety measure
        StartCoroutine(ResetRestrictionsDelayed(3f));
    }

    private IEnumerator SetVelocityDelayed(Vector3 velocity, float delay)
    {
        yield return new WaitForSeconds(delay);
        rb.velocity = velocity;
    }

    private IEnumerator ResetRestrictionsDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        ResetRestrictions();
    }

    public void ResetRestrictions()
    {
        activeGrapple = false;
        freeze = false;
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
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    // Unfreeze player
    public void UnfreezePlayer()
    {
        freeze = false;
    }
}