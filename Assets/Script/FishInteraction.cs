using UnityEngine;
using Oculus.Interaction;
using System.Collections;

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

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        //grabbable = GetComponent<Grabbable>();
        //grabInteractable = GetComponent<GrabInteractable>();
    }

    void Update()
    {
        bool isGrabbing = grabbable.SelectingPointsCount > 0;
        if (!isGrabbing)
        {
            timerStarted = false;
        }
        else if (isGrabbing && !timerStarted)
        {
            grabTime = Time.time;
            timerStarted = true;
            animator.SetBool("Struggle", true);
        }
        else if (timerStarted && Time.time - grabTime > 3f)
        {
            MakeUngrabable();
            animator.SetBool("Struggle", false);
        }
    }

    void MakeUngrabable()
    {
        // Force release
        if (grabCollider == null) return;
        Debug.Log("timeOut");
        StartCoroutine(DisableTemporarily(3.0f));
    }
    private IEnumerator DisableTemporarily(float duration)
    {
        //grabCollider.enabled = false;
        // Disable grabbable to prevent further grabs
        grabInteractable.enabled = false;
        orbitalTangentMovement.enableOrbitRecovery = true;
        yield return new WaitForSeconds(duration);
        //grabCollider.enabled = true;
        grabInteractable.enabled = true;
        orbitalTangentMovement.enableOrbitRecovery = false;
    }
}