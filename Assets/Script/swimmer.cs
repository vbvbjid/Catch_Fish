using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class swimmer : MonoBehaviour
{
    [Header("Values")]
    [SerializeField] public float swimForce;
    [SerializeField] public float dragForce;
    [SerializeField] public float minForce;
    [SerializeField] public float minTimeBetweenStrokes;

    [Header("References")]
    [SerializeField] InputActionReference l_ControllerSwimRef;
    [SerializeField] InputActionReference l_ControllerSwimVelocity;
    [SerializeField] InputActionReference r_ControllerSwimRef;
    [SerializeField] InputActionReference r_ControllerSwimVelocity;
    [SerializeField] Transform trackingRef;

    Rigidbody _rigidbody;
    float _coolDownTimer;

    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.useGravity = false;
        _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
    }

    void FixedUpdate()
    {
        _coolDownTimer += Time.deltaTime;
        if (_coolDownTimer > minTimeBetweenStrokes
            && l_ControllerSwimRef.action.IsPressed()
            && r_ControllerSwimRef.action.IsPressed())
        {
            var l_handVelocity = l_ControllerSwimVelocity.action.ReadValue<Vector3>();
            var r_handVelocity = r_ControllerSwimVelocity.action.ReadValue<Vector3>();
            Vector3 localVelocity = l_handVelocity + r_handVelocity;
            localVelocity *= -1;

            if (localVelocity.sqrMagnitude > minForce * minForce)
            {
                Vector3 worldVelocity = trackingRef.TransformDirection(localVelocity);
                _rigidbody.AddForce(worldVelocity * swimForce, ForceMode.Acceleration);
                _coolDownTimer = 0f;
            }

            if(_rigidbody.velocity.sqrMagnitude > 0.01f){
                _rigidbody.AddForce(-_rigidbody.velocity * dragForce, ForceMode.Acceleration);
            }
        }  
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
