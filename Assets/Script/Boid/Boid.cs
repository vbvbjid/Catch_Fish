using System.Collections.Generic;
using UnityEngine;

// Vector3 utility extensions for boid calculations
public static class Vector3Extensions
{
    public static Vector3 Limit(this Vector3 vector, float maxMagnitude)
    {
        if (vector.sqrMagnitude > maxMagnitude * maxMagnitude)
        {
            return vector.normalized * maxMagnitude;
        }
        return vector;
    }
}

// Individual boid behavior
public class Boid : MonoBehaviour
{
    [Header("Boid Settings")]
    public float maxSpeed = 5f;
    public float maxForce = 3f;
    public float neighborRadius = 2.5f;
    public float separationRadius = 1.5f;
    public float obstacleAvoidanceRadius = 3f;
    public float obstacleAvoidanceForce = 5f;

    [Header("Behavior Weights")]
    public float separationWeight = 2f;
    public float alignmentWeight = 1f;
    public float cohesionWeight = 1f;
    public float obstacleAvoidanceWeight = 4f;

    private Vector3 velocity;
    private Vector3 acceleration;
    private BoidManager manager;
    private List<Boid> neighbors = new List<Boid>();
    private List<Transform> obstacles = new List<Transform>();

    public Vector3 Velocity => velocity;
    public Vector3 Position => transform.position;

    public void Initialize(BoidManager boidManager)
    {
        manager = boidManager;
        velocity = Random.onUnitSphere * maxSpeed * 0.5f;
    }

    void Update()
    {
        if (manager == null) return;

        FindNeighbors();
        FindObstacles();

        Vector3 separation = Separate();
        Vector3 alignment = Align();
        Vector3 cohesion = Cohesion();
        Vector3 obstacleAvoidance = AvoidObstacles();
        Vector3 bounds = StayInBounds();

        // Apply weighted forces
        acceleration = Vector3.zero;
        acceleration += separation * separationWeight;
        acceleration += alignment * alignmentWeight;
        acceleration += cohesion * cohesionWeight;
        acceleration += obstacleAvoidance * obstacleAvoidanceWeight;
        acceleration += bounds * 2f;

        // Update physics
        velocity += acceleration * Time.deltaTime;
        velocity = velocity.Limit(maxSpeed);
        
        transform.position += velocity * Time.deltaTime;
        
        // Rotate to face movement direction
        if (velocity.magnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(velocity);
        }
    }

    private void FindNeighbors()
    {
        neighbors.Clear();
        var allBoids = manager.GetActiveBoids();
        
        foreach (var boid in allBoids)
        {
            if (boid == this) continue;
            
            float distance = Vector3.Distance(transform.position, boid.transform.position);
            if (distance <= neighborRadius)
            {
                neighbors.Add(boid);
            }
        }
    }

    private void FindObstacles()
    {
        obstacles.Clear();
        var allObstacles = manager.GetObstacles();
        
        foreach (var obstacle in allObstacles)
        {
            float distance = Vector3.Distance(transform.position, obstacle.position);
            if (distance <= obstacleAvoidanceRadius)
            {
                obstacles.Add(obstacle);
            }
        }
    }

    private Vector3 Separate()
    {
        Vector3 steer = Vector3.zero;
        int count = 0;

        foreach (var neighbor in neighbors)
        {
            float distance = Vector3.Distance(transform.position, neighbor.transform.position);
            if (distance > 0 && distance < separationRadius)
            {
                Vector3 diff = (transform.position - neighbor.transform.position).normalized;
                diff /= distance; // Weight by distance
                steer += diff;
                count++;
            }
        }

        if (count > 0)
        {
            steer /= count;
            steer = steer.normalized * maxSpeed;
            steer -= velocity;
            steer = steer.Limit(maxForce);
        }

        return steer;
    }

    private Vector3 Align()
    {
        Vector3 sum = Vector3.zero;
        int count = 0;

        foreach (var neighbor in neighbors)
        {
            sum += neighbor.Velocity;
            count++;
        }

        if (count > 0)
        {
            sum /= count;
            sum = sum.normalized * maxSpeed;
            Vector3 steer = sum - velocity;
            steer = steer.Limit(maxForce);
            return steer;
        }

        return Vector3.zero;
    }

    private Vector3 Cohesion()
    {
        Vector3 sum = Vector3.zero;
        int count = 0;

        foreach (var neighbor in neighbors)
        {
            sum += neighbor.Position;
            count++;
        }

        if (count > 0)
        {
            sum /= count;
            return Seek(sum);
        }

        return Vector3.zero;
    }

    private Vector3 Seek(Vector3 target)
    {
        Vector3 desired = (target - transform.position).normalized * maxSpeed;
        Vector3 steer = desired - velocity;
        steer = steer.Limit(maxForce);
        return steer;
    }

    private Vector3 AvoidObstacles()
    {
        Vector3 steer = Vector3.zero;
        int count = 0;

        foreach (var obstacle in obstacles)
        {
            Vector3 diff = transform.position - obstacle.position;
            float distance = diff.magnitude;

            if (distance > 0 && distance < obstacleAvoidanceRadius)
            {
                diff = diff.normalized;
                diff /= distance; // Weight by distance
                diff *= obstacleAvoidanceForce;
                steer += diff;
                count++;
            }
        }

        if (count > 0)
        {
            steer /= count;
            steer = steer.Limit(maxForce * obstacleAvoidanceWeight);
        }

        return steer;
    }

    private Vector3 StayInBounds()
    {
        Vector3 center = manager.GetFlockCenter();
        float radius = manager.GetFlockRadius();
        Vector3 steer = Vector3.zero;

        float distance = Vector3.Distance(transform.position, center);
        if (distance > radius)
        {
            steer = Seek(center);
        }

        return steer;
    }

    public void ResetBoid()
    {
        velocity = Random.onUnitSphere * maxSpeed * 0.5f;
        acceleration = Vector3.zero;
        neighbors.Clear();
        obstacles.Clear();
    }

    void OnDrawGizmosSelected()
    {
        // Neighbor radius
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, neighborRadius);
        
        // Separation radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, separationRadius);
        
        // Obstacle avoidance radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, obstacleAvoidanceRadius);
        
        // Velocity vector
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, velocity);
    }
}