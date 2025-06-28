using System.Collections;
using System.Collections.Generic;
using Meta.WitAi;
using UnityEngine;

public class AIMove : MonoBehaviour
{
    private AISpawner m_AIManager;

    private bool m_hasTarget = false;
    private bool m_isTruning;

    private Vector3 m_wayPoint;
    private Vector3 m_lastWaypoint = new Vector3(0f, 0f, 0f);

    private Animator m_animator;
    private float m_speed;

    private Collider m_collider;
    private RaycastHit m_hit;

    void Start()
    {
        m_AIManager = transform.parent.GetComponentInParent<AISpawner>();
        m_animator = GetComponent<Animator>();

        SetUpNPC();
    }
    void SetUpNPC()
    {
        float m_scale = Random.Range(0f, 2f);
        transform.localScale += new Vector3(m_scale * 1.5f, m_scale, m_scale);

        if (transform.GetComponent<Collider>() != null && transform.GetComponent<Collider>().enabled == true)
        {
            m_collider = transform.GetComponent<Collider>();
        }
    }
    void Update()
    {
        if (!m_hasTarget)
        {
            m_hasTarget = CanFindTarget();
        }
        else
        {
            RotateNPC(m_wayPoint, m_speed);
            transform.position = Vector3.MoveTowards(transform.position, m_wayPoint, m_speed * Time.deltaTime);
            CollierNPC();
        }

        if (transform.position == m_wayPoint)
        {
            m_hasTarget = false;
        }
    }
    void CollierNPC()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, transform.localScale.z))
        {
            if (hit.collider == m_collider | hit.collider.tag == "WayPoint")
            {
                return;
            }
            int randomNum = Random.Range(1, 100);
            if (randomNum < 40)
            {
                m_hasTarget = false;
            }
            Debug.Log(hit.collider.transform.parent.name + " " + hit.collider.transform.parent.position);
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

        Vector3 LookAt = waypoint - this.transform.position;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(LookAt), TurnSpeed * Time.deltaTime);
    }
}