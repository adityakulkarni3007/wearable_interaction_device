using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ballController : MonoBehaviour
{
    private Vector3 initialPosition;
    // Start is called before the first frame update
    void Start()
    {   
        initialPosition = transform.localPosition;       
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("Checking Update");
        Debug.Log("Checking position");
        if ((transform.localPosition.y > 50f) || (transform.localPosition.y < -50f))
        {
        Debug.Log("Ball out of bounds");
        transform.localPosition = new Vector3(initialPosition.x, initialPosition.y, initialPosition.z-1f);
        checkPosition();
        }
    }
    
    IEnumerator checkPosition()
    {
        yield return new WaitForSeconds(1);
    }
}
