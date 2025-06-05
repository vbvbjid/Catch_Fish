// ====================
// FishSpawnerRef.cs - Spawner Communication
// ====================
using UnityEngine;

[System.Serializable]
public class FishSpawnerRef : MonoBehaviour
{
    [Header("Spawner Reference")]
    public SimpleFishSpawner2 spawner;
    
    private FishAI2 fishAI;
    
    public void Initialize(FishAI2 ai)
    {
        fishAI = ai;
        
        if (spawner == null)
            spawner = GetComponentInParent<SimpleFishSpawner2>();
    }
    
    public void ReturnToPool()
    {
        if (spawner != null)
        {
            spawner.ReturnFishToPool(gameObject);
        }
        else
        {
            Debug.Log("No pool system found, deactivating fish", this);
            gameObject.SetActive(false);
        }
    }
}