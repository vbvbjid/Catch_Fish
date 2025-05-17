using UnityEngine;
using Oculus.Interaction;
using System.Collections;
using Meta.WitAi;

public class FishInteraction : MonoBehaviour
{
    public Animator animator;
    public OrbitalTangentMovement orbitalTangentMovement;
    public Grabbable grabbable;
    public GrabInteractable grabInteractable;
    private Rigidbody rb;
    private float grabTime;
    private bool timerStarted = false;
    public Collider grabCollider; // assign the collider used for grabbing
    public float releaseTimeout;
    public float catchTimeout;
    public float maxGrabTime = 3;

    public float AccumulatedGrabTime;
    public float TimeSinceLastRelease;
    public OrbitalObjectPool orbitalObjectPool;
    public bool released = false;

    [Tooltip("Reference to the GrabbableTimer component to get timing information from.")]
    [SerializeField] private GrabbableTimer _grabbableTimer;

    void Start()
    {
        orbitalObjectPool = GetComponentInParent<OrbitalObjectPool>();
        if (_grabbableTimer == null)
        {
            _grabbableTimer = GetComponent<GrabbableTimer>();
        }
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        //grabbable = GetComponent<Grabbable>();
        //grabInteractable = GetComponent<GrabInteractable>();
    }
    public void GrabInteraction()
    {
        //successful catch
        if (_grabbableTimer.AccumulatedGrabTime >= catchTimeout)
        {
            // dissolve absorb fish
            orbitalObjectPool.ReturnToPool(gameObject);
            return;
        }
        //TimeOut
        if (_grabbableTimer.TimeSinceLastRelease >= releaseTimeout)
        {
            //reset accumulated grab time
            _grabbableTimer.ResetTimers();
        }
        bool isGrabbing = grabbable.SelectingPointsCount > 0;
        if (!isGrabbing) // not grabbing
        {
            animator.SetBool("Struggle", false);
            orbitalTangentMovement.dontMove = false;
            timerStarted = false;
        }
        else if (isGrabbing && !timerStarted) // start timer when grabbed
        {
            orbitalTangentMovement.dontMove = true;
            Debug.Log("fist grab");
            grabTime = Time.time;
            timerStarted = true;
            animator.SetBool("Struggle", true);
        }
        // force release
        else if (timerStarted && Time.time - grabTime > maxGrabTime)
        {
            MakeUngrabable();
        }
    }

    void Update()
    {
        AccumulatedGrabTime = _grabbableTimer.AccumulatedGrabTime;
        TimeSinceLastRelease = _grabbableTimer.TimeSinceLastRelease;
        GrabInteraction();
    }

    void MakeUngrabable()
    {
        // Force release
        if (grabCollider == null) return;
        orbitalTangentMovement.StartRecovery();// Call StartRecovery() to begin the recovery process when the object is released
        
        Debug.Log("timeOut");
        StartCoroutine(DisableTemporarily(3.0f));
    }
    private IEnumerator DisableTemporarily(float duration)
    {
        //grabCollider.enabled = false;
        // Disable grabbable to prevent further grabs
        grabInteractable.enabled = false;
        yield return new WaitForSeconds(duration);
        //grabCollider.enabled = true;
        grabInteractable.enabled = true;
    }
}