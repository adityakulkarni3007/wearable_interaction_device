using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotation : MonoBehaviour
{
    public GameObject body;
    private bool inGame;
    private fliterUpdate FliterUpdate;
    private float qw, qx, qy, qz;

    // Start is called before the first frame update
    void Start()
    {
        body            = GameObject.FindWithTag("ground");
        FliterUpdate    = GameObject.FindObjectOfType<fliterUpdate>();
        inGame          = true;
    }

    // Update is called once per frame
    void Update()
    {
        float[] q = FliterUpdate.getQuaternion();
        qw = q[0];
        qx = q[1];
        qy = q[2];
        qz = q[3];
        if (inGame){
            rotation(body);
        }
    }

    void startGame()
    {
        inGame = true;
    }

    void stopGame()
    {
        inGame = false;
    }

    void rotation(GameObject obj)
    {
        // Unity accepts x,y,z,w
        Quaternion rot = new Quaternion(qx, qy, qz, qw);
        Quaternion spin1 = Quaternion.Euler(new Vector3(-90, 0, 0));
        Quaternion spin2 = Quaternion.Euler(new Vector3(0, 0, -90));
        Quaternion spin3 = Quaternion.Euler(new Vector3(180, 0, 0));
        Quaternion spin4 = Quaternion.Euler(new Vector3(0, 0, 90));
        Quaternion spin5 = Quaternion.Euler(new Vector3(0, 90, 0));
        // Unity and the IMU are both left handed coordinate systems
        obj.transform.rotation = spin5*spin1*spin2*spin3*spin4*rot;
    }
}
