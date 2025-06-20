using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
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
    // Start is called before the first frame update
    void Start()
    {
        if (skinnedMesh != null) materials = skinnedMesh.materials;
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
