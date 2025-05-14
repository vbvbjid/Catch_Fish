using UnityEngine;
using Oculus.Interaction;
using System.Collections;

public class FlyingGrabbableObject : MonoBehaviour
{
    public Transform player;
    public float orbitRadius = 2.5f;
    public float orbitSpeed = 1f;
    public float flyAwaySpeed = 5f;
    public float returnDistance = 10f;

    private Grabbable grabbable;
    private GrabInteractable grabInteractable;
    private Rigidbody rb;

    private bool isFlyingAway = false;
    private float grabTime;
    private bool timerStarted = false;
    private Vector3 flyDirection;

    public Collider grabCollider; // assign the collider used for grabbing

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        grabbable = GetComponent<Grabbable>();
        grabInteractable = GetComponent<GrabInteractable>();
    }

    void Update()
    {
        bool isGrabbing = grabbable.SelectingPointsCount > 0;
        if (!isGrabbing && !isFlyingAway)
        {
            Orbit();
            timerStarted = false;
        }
        else if (isGrabbing && !timerStarted)
        {
            grabTime = Time.time;
            timerStarted = true;
        }
        else if (timerStarted && Time.time - grabTime > 3f)
        {
            MakeUngrabableAndFlyAway();
        }
        else if (isFlyingAway)
        {
            FlyAwayFromPlayer();

            if (Vector3.Distance(transform.position, player.position) > returnDistance)
            {
                ResetToOrbit();
            }
        }
    }

    void Orbit()
    {
        float angle = Time.time * orbitSpeed;
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * orbitRadius;
        Vector3 targetPos = player.position + offset;
        rb.MovePosition(Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 2f));
    }

    void MakeUngrabableAndFlyAway()
    {
        // Force release
        if (grabCollider == null) return;
        Debug.Log("timeOut");
        StartCoroutine(DisableTemporarily(3.0f));
        // Disable grabbable to prevent further grabs
        grabInteractable.enabled = false;

        isFlyingAway = true;
        timerStarted = false;
        flyDirection = (transform.position - player.position).normalized;
    }
    private IEnumerator DisableTemporarily(float duration)
    {
        grabCollider.enabled = false;
        yield return new WaitForSeconds(duration);
        grabCollider.enabled = true;
    }

    void FlyAwayFromPlayer()
    {
        Debug.Log("fly away");
        rb.MovePosition(transform.position + flyDirection * flyAwaySpeed * Time.deltaTime);
    }

    void ResetToOrbit()
    {
        isFlyingAway = false;
        grabInteractable.enabled = true;
    }
}