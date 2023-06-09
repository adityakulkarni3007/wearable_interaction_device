using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ballController : MonoBehaviour
{
    private Vector3 initialPosition;
    private GameObject[] lava;
    private rotate_board RotateBoard;
    // Start is called before the first frame update
    void Start()
    {   
        initialPosition = transform.localPosition; 
        lava = GameObject.FindGameObjectsWithTag("lava");  
        RotateBoard     = GameObject.FindObjectOfType<rotate_board>();
    }

    // Update is called once per frame
    void Update()
    {
        if ((transform.localPosition.y > 10f) || (transform.localPosition.y < -10f) || (transform.localPosition.x > 10f) || (transform.localPosition.x < -10f) || (transform.localPosition.y > 10f) || (transform.localPosition.y < -10f))
        {
            reset_position();
        }
    }

    // Reset the position of the ball to the initial position
    public void reset_position()
    {
        gameObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
        gameObject.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        transform.localPosition = new Vector3(initialPosition.x, initialPosition.y, initialPosition.z-1f);
    }

    // Reset Position when ball collides with lava
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "lava")
        {
            reset_position();
        }
        if (collision.gameObject.tag == "goalObj")
        {
            RotateBoard.reachedGoal();
        }
    }

}
