using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;
using System.Threading.Tasks;
using Unity.VisualScripting;

public class FishVisualEffect : MonoBehaviour
{
    public SkinnedMeshRenderer skinnedMesh;
    public Material[] materials;
    public VisualEffect VFXGraph;
    public float dissolveRate = 0.0125f;
    public float refreshRate = 0.025f;
    public float dissolveDuration = 2.0f;
    [SerializeField] private Transform targetTransform;
    [SerializeField] private GameObject vfxChild; // Assign the VFX child GameObject

    // Start is called before the first frame update
    void Start()
    {
        if (skinnedMesh != null) materials = skinnedMesh.materials;
        targetTransform = GameObject.Find("Heart").transform;
        VFXBinderBase[] binders = vfxChild.GetComponents<VFXBinderBase>();
        
        foreach (var binder in binders)
        {
            // Check if it's a position binder by type name
            if (binder.GetType().Name.Contains("Position"))
            {
                SetBinderTarget(binder, targetTransform, "AttractTarget");
                break;
            }
        }
    }
    void SetBinderTarget(VFXBinderBase binder, Transform target, string propertyName)
    {
        try
        {
            // Set the Target field
            var targetField = binder.GetType().GetField("Target");
            targetField?.SetValue(binder, target);
            
            // Try Property setter first (cleaner)
            var propertyProperty = binder.GetType().GetProperty("Property");
            if (propertyProperty != null)
            {
                propertyProperty.SetValue(binder, propertyName);
            }
            else
            {
                // Fallback to field access with proper type conversion
                var propertyField = binder.GetType().GetField("m_Property", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (propertyField != null)
                {
                    ExposedProperty exposedProperty = propertyName;
                    propertyField.SetValue(binder, exposedProperty);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to set binder target: {e.Message}");
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
    
    public async Task DissoveEffect()
    {
        if (VFXGraph != null) VFXGraph.Play();
        if (skinnedMesh.materials.Length > 0)
        {
            float CurrentThreshold = 0;
            float startTime = Time.time;
            //float endTime = startTime + dissolveDuration;
            while (materials[0].GetFloat("_threshold") < 1)
            {
                CurrentThreshold = (Time.time - startTime) / dissolveDuration;
                CurrentThreshold = Mathf.Clamp01(CurrentThreshold);
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i].SetFloat("_threshold", CurrentThreshold); 
                }
                Debug.Log("Dissolve Threshold = " + CurrentThreshold);
                await Task.Yield();
            }
        }
    }
    public void RestoreMaterial()
    {
        if (skinnedMesh.materials.Length > 0)
        {
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i].SetFloat("_threshold", 0);
            }
        }

    }
}
