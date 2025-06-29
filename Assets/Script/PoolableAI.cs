using UnityEngine;

// Component that should be added to pooled AI objects
public class PoolableAI : MonoBehaviour, IPoolable
{
    private AISpawner spawner;
    private int aiGroupIndex;
    private float lifetime = 30f; // How long before auto-returning to pool
    private float currentLifetime;

    public void Initialize(AISpawner spawner, int groupIndex)
    {
        this.spawner = spawner;
        this.aiGroupIndex = groupIndex;
    }

    public void OnSpawn()
    {
        currentLifetime = lifetime;

        // Add AIMove component if it doesn't exist
        if (GetComponent<AIMove>() == null)
        {
            gameObject.AddComponent<AIMove>();
        }

        // Reset any AI states here
        // For example, reset health, animations, etc.
    }

    public void OnDespawn()
    {
        // Clean up when returning to pool
        // Stop animations, reset positions, etc.

        // Remove or reset components as needed
        AIMove aiMove = GetComponent<AIMove>();
        if (aiMove != null)
        {
            // Reset AIMove state if needed
        }
    }

    public void ReturnToPool()
    {
        if (spawner != null)
        {
            spawner.ReturnToPool(gameObject, aiGroupIndex);
        }
    }

    void Update()
    {
        // Auto return to pool after lifetime expires
        if (gameObject.activeInHierarchy)
        {
            currentLifetime -= Time.deltaTime;
            if (currentLifetime <= 0)
            {
                ReturnToPool();
            }
        }
    }

    // Call this method when AI should die/be destroyed
    public void Die()
    {
        ReturnToPool();
    }

    // Call this method to extend lifetime (useful for AI that's in combat, etc.)
    public void ExtendLifetime(float additionalTime)
    {
        currentLifetime += additionalTime;
    }
}