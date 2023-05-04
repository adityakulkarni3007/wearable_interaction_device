using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ballController : MonoBehaviour
{
    Vector3 initialPosition;
    // Start is called before the first frame update
    void Start()
    {
        initialPosition = gameObject.transform.localPosition;
    }

    // Update is called once per frame
    void Update()
    {
        // if (gameObject.transform.localPosition.z > 200f || gameObject.transform.localPosition.z < -200f)
        // {
            // gameObject.transform.localPosition = initialPosition;
        // }
    }
}
