using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;  

public class rotate_board : MonoBehaviour
{
    public GameObject body;
    private bool inGame;
    private fliterUpdate FliterUpdate;
    private float qw, qx, qy, qz;
    private ballController BallController;
    private float button1, button2, button3, button4, b1_prev, b2_prev, b3_prev, b4_prev;
    private float buttonDelta1, buttonDelta2, buttonDelta3, buttonDelta4;
    public GameObject resetText;

    // Start is called before the first frame update
    void Start()
    {
        body            = GameObject.FindWithTag("ground");
        resetText       = GameObject.FindWithTag("resetCountdown");
        FliterUpdate    = GameObject.FindObjectOfType<fliterUpdate>();
        BallController  = GameObject.FindObjectOfType<ballController>();
        updateQuaternion();
        rotation(body);
        inGame          = false;
        resetText.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("inGame: " + inGame);
        updateQuaternion();
        Debug.Log("qw: " + qw + " qx: " + qx + " qy: " + qy + " qz: " + qz);
        if (inGame){
            rotation(body);
        }
        if (buttonDelta1 == 1){startGame();}
        if (buttonDelta2 == 1){stopGame();}
        if (buttonDelta4 == 1){StartCoroutine(reset_board());}
    }

    void updateQuaternion()
    {
        float[] q = FliterUpdate.getQuaternion();
        qw = q[0];
        qx = q[1];
        qy = q[2];
        qz = q[3];
    }
    public void startGame()
    {
        Debug.Log("Game Started");
        inGame = true;
    }

    public void stopGame()
    {
        inGame = false;
        StartCoroutine(reset_board());
    }

    IEnumerator reset_board()
    {
        // Everything in this function below the yield return statement will be affected by the delay
        resetText.SetActive(true);
        resetText.GetComponent<TMP_Text>().text = "Resetting in:\n\t3";
        yield return new WaitForSecondsRealtime(1);
        resetText.GetComponent<TMP_Text>().text = "Resetting in:\n\t2";
        yield return new WaitForSecondsRealtime(1);
        resetText.GetComponent<TMP_Text>().text = "Resetting in:\n\t1";
        yield return new WaitForSecondsRealtime(1);
        resetText.SetActive(false);
        FliterUpdate.updateQuaternion(0.0f,0.0f,0.0f,1.0f);
        updateQuaternion();
        rotation(body);
        BallController.reset_position();
    }

    public void getButtonState(ref float[] data)
    {
        button1 = data[0];
        button2 = data[1];
        button3 = data[2];
        button4 = data[3];
        if (b1_prev!=button1) {buttonDelta1 = button1;}
        else{buttonDelta1 = 0;}
        b1_prev = button1;
        if (b2_prev!=button2) {buttonDelta2 = button2;}
        else{buttonDelta2 = 0;}
        b2_prev = button2;
        if (b3_prev!=button3) {buttonDelta3 = button3;}
        else{buttonDelta3 = 0;}
        b3_prev = button3;
        if (b4_prev!=button4) {buttonDelta4 = button4;}
        else{buttonDelta4 = 0;}
        b4_prev = button4;
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
