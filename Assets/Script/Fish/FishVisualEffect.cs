using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using System.Threading.Tasks;

public class FishVisualEffect : MonoBehaviour
{
    public SkinnedMeshRenderer skinnedMesh;
    public Material[] materials;
    public VisualEffect VFXGraph;
    public float dissolveRate = 0.0125f;
    public float refreshRate = 0.025f;
    // Start is called before the first frame update
    void Start()
    {
        if (skinnedMesh != null) materials = skinnedMesh.materials;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("space");
            //StartCoroutine(DissoveEffect());
        }
    }
    public async Task DissoveEffect()
    {
        if (VFXGraph != null) VFXGraph.Play();
        if (skinnedMesh.materials.Length > 0)
        {
            float timer = 0;
            float counter = 0;
            while (materials[0].GetFloat("_threshold") < 1)
            {
                Debug.Log("threshold =  " + materials[0].GetFloat("_threshold"));
                counter += dissolveRate;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i].SetFloat("_threshold", counter);
                }
                while (timer < refreshRate)
                {
                    timer += Time.deltaTime;
                    await Task.Yield();
                }
                //yield return new WaitForSeconds(refreshRate);
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
