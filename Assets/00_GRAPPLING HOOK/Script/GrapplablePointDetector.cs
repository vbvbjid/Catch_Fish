using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrapplablePointDetector : MonoBehaviour
{
    void OnTriggerEnter(Collider other){
        if(other.gameObject.CompareTag("Grappable")){
            Material mat = other.gameObject.GetComponent<Renderer>().material;
            mat.color = Color.green;
        }
    }
    void OnTriggerExit(Collider other){
        if(other.gameObject.CompareTag("Grappable")){
            Material mat = other.gameObject.GetComponent<Renderer>().material;
            mat.color = Color.red;
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
