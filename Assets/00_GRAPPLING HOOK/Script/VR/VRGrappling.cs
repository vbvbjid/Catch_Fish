using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

public class VRGrappling : MonoBehaviour
{
    [Header("VR References")]
    public Transform vrCamera; // VR headset camera
    public Transform gunTip; // Where the grapple line originates (e.g., controller position)
    public XRController controller; // Optional: for haptic feedback
    
    [Header("Grappling Settings")]
    public LayerMask whatIsGrappleable;
    public LineRenderer lr;
    [SerializeField] 
    private VRPlayerMovementGrappling playerMovement;

    [Header("Grappling Parameters")]
    public float maxGrappleDistance = 50f;
    public float grappleDelayTime = 0.25f;
    private Vector3 grapplePoint;
    public float overshootYAxis = 2f;

    [Header("Aim Assist / Thick Ray")]
    public bool useSphereCast = true;
    [Range(0.01f, 1.0f)]
    public float sphereCastRadius = 0.15f;   // widen detection
    public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Cooldown")]
    public float grappleCooldown = 1f;
    private float grappleCooldownTimer;

    [Header("VR Input")]
    public InputActionReference grappleAction; // Assign in inspector (e.g., Right Grip or Trigger)
    
    [Header("Visual Feedback")]
    public bool showGrapplePreview = true;
    public Color lineColorNormal = Color.white;
    public Color lineColorInvalid = Color.red;

    [Header("Grapple Hint (Reticle)")]
    public Transform grappleHint;                 // assign a small sphere/quad in scene or prefab instance
    public Color hintValidColor = Color.cyan;
    public Color hintInvalidColor = Color.red;
    public float hintBaseScale = 0.04f;
    public float hintScaleWhenValid = 1.35f;
    public float hintSmooth = 14f;

[Header("Preview Performance")]
public int previewEveryNFrames = 2;           // 1 = every frame, 2 = every other frame


    private bool grappling;
    
    void Start()
    {
        // Get player movement component
        if (playerMovement == null)
        {
            playerMovement = GetComponent<VRPlayerMovementGrappling>();
        }
        
        // Setup line renderer
        if (lr != null)
        {
            lr.enabled = false;
            lr.startColor = lineColorNormal;
            lr.endColor = lineColorNormal;
        }
        
        // Enable and subscribe to grapple input action
        if (grappleAction != null)
        {
            grappleAction.action.Enable();
            grappleAction.action.performed += OnGrapplePerformed;
        }
        
        // Auto-find VR camera if not assigned
        if (vrCamera == null && Camera.main != null)
        {
            vrCamera = Camera.main.transform;
        }
    }

    private void OnGrapplePerformed(InputAction.CallbackContext context)
    {
        StartGrapple();
    }

    private void StartGrapple()
    {
        // Check cooldown
        if (grappleCooldownTimer > 0) return;
        
        // Check if already grappling
        if (grappling) return;

        // Freeze player movement
        playerMovement.FreezePlayer();
        grappling = true;

        RaycastHit hit;
        Vector3 rayOrigin = gunTip.position;
        Vector3 rayDirection = gunTip.forward;

        bool hasHit;

        if (useSphereCast)
        {
            hasHit = Physics.SphereCast(
                rayOrigin,
                sphereCastRadius,
                rayDirection,
                out hit,
                maxGrappleDistance,
                whatIsGrappleable,
                triggerInteraction
            );
        }
        else
        {
            hasHit = Physics.Raycast(
                rayOrigin,
                rayDirection,
                out hit,
                maxGrappleDistance,
                whatIsGrappleable,
                triggerInteraction
            );
        }

        if (hasHit)
        {
            grapplePoint = hit.point;

            if (lr != null)
            {
                lr.startColor = lineColorNormal;
                lr.endColor = lineColorNormal;
            }

            SendHapticFeedback(0.3f, 0.1f);
            Invoke(nameof(ExecuteGrapple), grappleDelayTime);
        }
        else
        {
            grapplePoint = rayOrigin + rayDirection * maxGrappleDistance;

            if (lr != null)
            {
                lr.startColor = lineColorInvalid;
                lr.endColor = lineColorInvalid;
            }

            SendHapticFeedback(0.1f, 0.05f);
            Invoke(nameof(StopGrapple), grappleDelayTime);
        }

        // Enable line renderer
        if (lr != null)
        {
            lr.enabled = true;
            lr.SetPosition(1, gunTip.position);
        }
    }

    private void ExecuteGrapple()
    {
        // Unfreeze player for grapple movement
        playerMovement.UnfreezePlayer();

        // Calculate trajectory
        Vector3 lowestPoint = new Vector3(transform.position.x, transform.position.y - 1f, transform.position.z);
        float grapplePointRelativeYPos = grapplePoint.y - lowestPoint.y;
        float highestPointOnArc = grapplePointRelativeYPos + overshootYAxis;

        // Ensure minimum arc height
        if (grapplePointRelativeYPos < 0) 
            highestPointOnArc = overshootYAxis;

        // Execute grapple jump
        playerMovement.JumpToPosition(grapplePoint, highestPointOnArc);

        // Stronger haptic feedback on execute
        SendHapticFeedback(0.5f, 0.2f);

        // Auto-stop grapple after 1 second
        Invoke(nameof(StopGrapple), 1f);
    }

    public void StopGrapple()
    {   
        // Unfreeze player
        playerMovement.UnfreezePlayer();
        
        grappling = false;
        grappleCooldownTimer = grappleCooldown;
        
        // Disable line renderer
        if (lr != null)
        {
            lr.enabled = false;
        }
    }

    void LateUpdate()
    {
        // Update line renderer positions while grappling
        if (grappling && lr != null && lr.enabled)
        {
            lr.SetPosition(0, grapplePoint);
            lr.SetPosition(1, gunTip.position);
        }
    }

    void Update()
    {
        // Update cooldown timer
        if (grappleCooldownTimer > 0)
        {
            grappleCooldownTimer -= Time.deltaTime;
        }
        
        // Optional: Show grapple preview
        if (showGrapplePreview && !grappling)
        {
            ShowGrapplePreview();
        }
    }

    private void ShowGrapplePreview()
    {
        // Optional: You can implement a preview line or reticle here
        // This could show where the grapple would connect
    }

    private void SendHapticFeedback(float amplitude, float duration)
    {
        // Send haptic feedback to VR controller if available
        if (controller != null)
        {
            controller.SendHapticImpulse(amplitude, duration);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from input action to prevent memory leaks
        if (grappleAction != null)
        {
            grappleAction.action.performed -= OnGrapplePerformed;
        }
    }

    private void OnDrawGizmos()
    {
        // Debug visualization - draw grapple ray
        if (vrCamera != null)
        {
            Gizmos.color = grappleCooldownTimer > 0 ? Color.red : Color.green;
            Gizmos.DrawRay(gunTip.position, gunTip.forward * maxGrappleDistance);
        }
    }
}