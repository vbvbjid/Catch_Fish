using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fish2 : MonoBehaviour {
    public Vector3 orbitCenter = Vector3.zero;
    public float orbitRadius = 5f;
    public float orbitSpeed = 1f;
    public float recoverySpeed = 2f;
    public float deviationThreshold = 1.5f;

    private float angle = 0f;
    private Rigidbody rb;
    private bool recovering = false;

    void Start() {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
    }

    void FixedUpdate() {
        angle += orbitSpeed * Time.fixedDeltaTime;
        Vector3 targetPos = GetOrbitalPosition(angle);

        if (!recovering) {
            Vector3 tangentDir = GetTangentDirection(angle);
            rb.velocity = tangentDir * orbitSpeed * orbitRadius;
        }

        if (Vector3.Distance(transform.position, orbitCenter) > orbitRadius * deviationThreshold) {
            recovering = true;
        }

        if (recovering) {
            Vector3 direction = (targetPos - transform.position).normalized;
            rb.velocity = direction * orbitSpeed * orbitRadius;
            if (Vector3.Distance(transform.position, targetPos) < 0.1f) {
                recovering = false;
            }
        }
    }

    Vector3 GetOrbitalPosition(float a) {
        return orbitCenter + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * orbitRadius;
    }

    Vector3 GetTangentDirection(float a) {
        return new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a));
    }
}
