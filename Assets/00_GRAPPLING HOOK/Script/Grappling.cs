using UnityEngine;
using UnityEngine.InputSystem; // Required for new input system

public class Grappling : MonoBehaviour
{
    [Header("References")]
    public Transform cam;
    public Transform gunTip;
    public LayerMask whatIsGrappleable;
    public LineRenderer lr;
    [SerializeField] 
    private PlayerMovementGrappling playerMovement;

    [Header("Grappling")]
    public float maxGrappleDistance;
    public float grappleDelayTime;
    private Vector3 grapplePoint;
    public float overshootYAxis;

    [Header("Cooldown")]
    public float grappleCooldown;
    private float grappleCooldownTimer;

    [Header("Input (New System)")]
    public InputActionReference grappleAction; // Assign this in inspector

    private bool grappling;
    
    void Start()
    {
        playerMovement = GetComponent<PlayerMovementGrappling>();
        // Enable the action
        grappleAction.action.Enable();

        // Subscribe to event
        grappleAction.action.performed += _ => StartGrapple();
    }

    private void StartGrapple()
    {
        if (grappleCooldownTimer > 0) return;
        playerMovement.freeze = true;
        grappling = true;
        RaycastHit hit;

        if (Physics.Raycast(cam.position, cam.forward, out hit, maxGrappleDistance, whatIsGrappleable))
        {
            grapplePoint = hit.point;
            Invoke(nameof(ExecuteGrapple), grappleDelayTime);
        }
        else
        {
            grapplePoint = cam.position + cam.forward * maxGrappleDistance;
            Invoke(nameof(StopGrapple), grappleDelayTime);
        }

        lr.enabled = true;
        lr.SetPosition(1, gunTip.position);
    }

    private void ExecuteGrapple()
    {
        // Your grapple movement logic goes here
        playerMovement.freeze = false;
        // Debug.Log("isFreeze: " + playerMovement.freeze);    

        Vector3 lowestPoint = new Vector3(transform.position.x, transform.position.y - 1f, transform.position.z);

        float grapplePointRelativeYPos = grapplePoint.y - lowestPoint.y;
        float highestPointOnArc = grapplePointRelativeYPos + overshootYAxis;

        if (grapplePointRelativeYPos < 0) highestPointOnArc = overshootYAxis;

        playerMovement.JumpToPosition(grapplePoint, highestPointOnArc);

        Invoke(nameof(StopGrapple), 1f);
    }

    public void StopGrapple()
    {   
        playerMovement.freeze = false;
        grappling = false;
        grappleCooldownTimer = grappleCooldown;
        lr.enabled = false;
    }

    void LateUpdate()
    {
        if (!grappling) return;
        lr.SetPosition(0, grapplePoint);
        lr.SetPosition(1, gunTip.position);
    }

    void Update()
    {
        if (grappleCooldownTimer > 0)
        {
            grappleCooldownTimer -= Time.deltaTime;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe to avoid memory leaks
        grappleAction.action.performed -= _ => StartGrapple();
    }
}
