using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Microsoft.MixedReality.Toolkit.UI;

public class modes : MonoBehaviour
{
    private int translation_mode;
    private float[] q; 
    private string[] textTags;
    private string translation_method;
    private float qx, qy, qz, qw, curr_theta, prev_theta, startTime, maintainTime, delta;
    private GameObject rotationGoalObj, translationGoalObj, slicerGoalObj, opacityGoalObj, translationAxis;
    private GameObject[] opacityText, slicerText, translationText, translationReferenceAxes, resultText, spikes, envelope, insides;
    private bool goalFlag, inMode, resetFlag;
    private fliterUpdate FliterUpdate;
    private int button1, button2, button3, button4;
    private int buttonDelta1, buttonDelta2, buttonDelta3, buttonDelta4, b1_prev, b2_prev, b3_prev, b4_prev;
    private Material[] mat1;
    public Material mat;
    public string mode;
    public float scaling_factor;
    public GameObject cuttingPlane, body;
    public PinchSlider opacitySlider;
    public TMP_Text[] modeText;
    private Vector3 initialPosition;

    // Start is called before the first frame update
    void Start()
    {
        cuttingPlane                = GameObject.FindWithTag("cuttingPlane");
        translationAxis             = GameObject.FindWithTag("translationAxis");
        opacityText                 = GameObject.FindGameObjectsWithTag("opacityText");
        slicerText                  = GameObject.FindGameObjectsWithTag("slicerText");
        spikes                      = GameObject.FindGameObjectsWithTag("spikes");
        envelope                    = GameObject.FindGameObjectsWithTag("envelope");
        insides                     = GameObject.FindGameObjectsWithTag("insides");
        // resultText                  = GameObject.FindGameObjectsWithTag("result");
        translationText             = GameObject.FindGameObjectsWithTag("translationText");
        translationReferenceAxes    = new GameObject[3];
        translationReferenceAxes[0] = GameObject.FindWithTag("RefX");
        translationReferenceAxes[1] = GameObject.FindWithTag("RefY");
        translationReferenceAxes[2] = GameObject.FindWithTag("RefZ");
        opacitySlider               = GameObject.FindWithTag("opacitySlider").GetComponent<PinchSlider>();
        textTags                    = new string[4]{"rotation","translation","slicing","opacity"};
        modeText                    = new TMP_Text[textTags.Length];
        mode                        = "menu";
        translation_method          = "noMode";
        goalFlag                    = false;
        translation_mode            = 0;
        body                        = GameObject.FindWithTag("body");
        scaling_factor              = 0.005f;
        maintainTime                = 0.0f;
        curr_theta                  = 0.0f; 
        prev_theta                  = 0.0f;
        delta                       = 0.0f;
        // rotationGoalObj             = GameObject.FindWithTag("rotationGoal");
        // translationGoalObj          = GameObject.FindWithTag("translationGoal");
        // slicerGoalObj               = GameObject.FindWithTag("slicerGoal");
        // opacityGoalObj              = GameObject.FindWithTag("opacityGoal");
        FliterUpdate                = GameObject.FindObjectOfType<fliterUpdate>();
        inMode                      = false;   
        initialPosition             = body.transform.position;

        // rotationGoalObj.SetActive(false);
        // translationGoalObj.SetActive(false);
        // slicerGoalObj.SetActive(false);
        // opacityGoalObj.SetActive(false);
        // setGameObjectArrayActive(ref resultText, false);
        for (int i=0; i<textTags.Length; i++){
            modeText[i] = GameObject.FindWithTag(textTags[i]).GetComponent<TMP_Text>();
        }
        mat1 = new Material[spikes.Length + envelope.Length + insides.Length];
        for (int i = 0; i < spikes.Length; i++){   
            spikes[i].GetComponent<Renderer>().material.SetColor("_Color", Color.red);   
            spikes[i].GetComponent<Renderer>().material.SetFloat("_opacity", 1.0f);
            mat1[i] = spikes[i].GetComponent<Renderer>().material;
        }
        for (int i = 0; i < envelope.Length; i++){
            envelope[i].GetComponent<Renderer>().material.SetFloat("_opacity", 0.75f);
            envelope[i].GetComponent<Renderer>().material.SetColor("_Color", new Color(0.0f, 0.5f, 0.7f, 1.0f));
            mat1[i + spikes.Length] = envelope[i].GetComponent<Renderer>().material;
        }
        for (int i = 0; i < insides.Length; i++){
            insides[i].GetComponent<Renderer>().material.SetFloat("_opacity", 1.0f);
            insides[i].GetComponent<Renderer>().material.SetColor("_Color", new Color(1.0f, 1.0f, 0.0f, 1.0f));
            mat1[i + spikes.Length + envelope.Length] = insides[i].GetComponent<Renderer>().material;
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        // Set the current position and normal of the plane
        changeTextOpacity();
        Debug.Log("Mode: " + mode);
        Debug.Log("In Mode: " + inMode);
        Debug.Log("ButtonDelta4: " + buttonDelta4);
        if (buttonDelta4 == 1 && inMode)
        {
            inMode = false;
            mode = "menu";
            buttonDelta4 = 0;
        }
        else if(inMode==false){
            modeSelection();
        }
        // checkTask();
        checkMode();
        if (resetFlag)
        {
            if (mode == "rotation"){
                // Reset Rotation
                FliterUpdate.updateQuaternion(0.0f,0.0f,0.0f,1.0f);
            }
            if (mode == "translation"){
                // Reset Translation
                body.transform.position = initialPosition;
                translationAxis.transform.position = initialPosition;
                FliterUpdate.updateQuaternion(0.0f,0.0f,0.0f,1.0f);
            }
            if (mode == "slicing"){
                // Reset Slicing
                cuttingPlane.transform.position = body.transform.position;
                FliterUpdate.updateQuaternion(0.0f,0.0f,0.0f,1.0f);
            }
            if (mode == "opacity"){
                // Reset Opacity
                FliterUpdate.setTheta(0.0f);
            }
            resetFlag = false;
        }
        float[] q = FliterUpdate.getQuaternion();
        Debug.Log("1: " + button1 + " 2: " + button2 + " 3: " + button3 + " 4: " + button4);
        Debug.Log("1: " + buttonDelta1 + " 2: " + buttonDelta2 + " 3: " + buttonDelta3 + " 4: " + buttonDelta4);
    }

    public void getData(ref fliterUpdate update)
    {
        FliterUpdate = update;
    }

    void checkMode()
    {
        if (mode == "rotation")
        {
            Debug.Log("Enter Rotation Mode");
            if (qx != null && qy != null && qz != null && qw != null)
            {
                rotation(body);    
                // checkPoseGoal(rotationGoalObj, body);
            }
        }
        if (mode == "translation")
        {
            // checkPoseGoal(translationGoalObj, body);
            if (translation_method == "free"){
                translationAxis.SetActive(true);
                translation_wrt_world(body);
            }
            else if (translation_method == "axis"){
                translationAxis.SetActive(false);
                selectAxis(body);
            }
            for (int i=0; i<translationReferenceAxes.Length; i++){
                if (translation_mode == i){
                    translationReferenceAxes[i].GetComponent<Renderer>().material.SetFloat("_opacity", 1.0f);
                }
                else{
                    translationReferenceAxes[i].GetComponent<Renderer>().material.SetFloat("_opacity", 0.5f);
                }
            }
        }
        else{
            setGameObjectArrayActive(ref translationReferenceAxes, false);
            setGameObjectArrayActive(ref translationText, false);
            translationAxis.SetActive(false);
        }
        if (mode == "slicing")
        {
            for (int i=0; i<mat1.Length; i++)
                mat1[i].SetInt("_opacityMode", 0);
            // checkPoseGoal(slicerGoalObj, cuttingPlane);
            rotation(cuttingPlane);
            for (int i = 0; i < mat1.Length; i++){
                mat1[i].SetVector("_planePosition", cuttingPlane.transform.position);
                mat1[i].SetVector("_planeNormal", cuttingPlane.transform.up);
            }
            if (button2==1){
                translation_wrt_gameObject(cuttingPlane, true);
            }
            if (button2==1){
                translation_wrt_gameObject(cuttingPlane, false);
            }
        }
        else{
            cuttingPlane.SetActive(false);
            setGameObjectArrayActive(ref slicerText, false);
            for (int i = 0; i < mat1.Length; i++){
                mat1[i].SetVector("_planePosition", new Vector3(0, 100000000 ,0));
                mat1[i].SetVector("_planeNormal", new Vector3(0,-1,0));
            }
        }
        if (mode == "opacity")
        {
            // checkOpacityGoal();
            opacity();
            // Debug.Log(opacitySlider.SliderValue);
        }
        else{
            opacitySlider.gameObject.SetActive(false);
            setGameObjectArrayActive(ref opacityText, false);
        }
    }

/*
    void checkTask()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            setGameObjectArrayActive(ref resultText, false);
            maintainTime    = 0.0f;
            goalFlag        = false;
            startTime       = Time.time;
            if (mode == "rotation")
            {
                rotationGoalObj.SetActive(true);
            }
            if (mode == "translation")
            {
                translationGoalObj.SetActive(true);
                translationGoalObj.transform.rotation = body.transform.rotation;
            }
            if (mode == "slicing")
            {
                slicerGoalObj.SetActive(true);
            }
            if (mode == "opacity")
            {
                opacityGoalObj.SetActive(true);
            }
        }
    }
*/
    // This method is responsible for updating the IMU data.
    public void updateIMU(float[] q, float[] sensorData)
    {
        // If the imuData array is null, return.
        if (q == null || sensorData == null)
        {
            return;
        }

        // If the length of the q array is less than 5, return.
        if (q.Length < 4)
        {
            return;
        }

        // Attempt to parse the values in the q array.
        try
        {
            qw = q[0];
            qx = q[1];
            qy = q[2];
            qz = q[3];
            
            prev_theta = curr_theta;
            curr_theta = FliterUpdate.getTheta();
            // Debug.Log("Delta: " + delta + " curr: " + curr_theta + " prev: " + prev_theta);
            delta = curr_theta - prev_theta;

            button1 = 1 - (int)sensorData[0];
            button2 = 1 - (int)sensorData[1];
            button3 = 1 - (int)sensorData[2];
            button4 = 1 - (int)sensorData[3];
            // TODO: Make this an array 
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
        // If there is an error parsing the values, log an error message and return.
        catch
        {
            Debug.LogError("Cannot Parse IMU data");
            return;
        }

        // If any of the parsed values are NaN, log an error message and return.
        if (qx == float.NaN || qy == float.NaN || qz == float.NaN || qw == float.NaN)
        {
            Debug.LogError("IMU data is nan");
            return;
        }
    }


    // This method is responsible for handling the mode selection logic.
    void modeSelection()
    {
        if (buttonDelta1 == 1){checkRotationMode();}
        if (buttonDelta2 == 1){checkTranslationMode();}
        if (buttonDelta3 == 1){checkSlicingMode();}
        if (buttonDelta4 == 1){checkOpacityMode();}
    }

    public void checkRotationMode()
    {
        buttonDelta1 = 0;
        inMode = true;
        mode = "rotation";
        Debug.Log("Mode inside checkRotationMode: " + mode);
        // disableGoalObjects();
    }

    public void checkTranslationMode()
    {
        buttonDelta2 = 0;
        inMode = true;
        mode = "translation";
        translationAxis.SetActive(true);
        // disableGoalObjects();
        setGameObjectArrayActive(ref translationText, true);
        setGameObjectArrayActive(ref translationReferenceAxes, true);
    }

    public void checkSlicingMode()
    {
        buttonDelta3 = 0;
        inMode = true;
        mode = "slicing";
        // disableGoalObjects();
        cuttingPlane.SetActive(true);
        setGameObjectArrayActive(ref slicerText, true);
        // Set the cutting plane's position and rotation to match the body's position and rotation.
        cuttingPlane.transform.position = body.transform.position;
        cuttingPlane.transform.rotation = body.transform.rotation;
    }

    public void checkOpacityMode()
    {
        buttonDelta4 = 0;
        inMode = true;
        mode = "opacity";
        // disableGoalObjects();
        opacitySlider.gameObject.SetActive(true);
        setGameObjectArrayActive(ref opacityText, true);
    }

    public void checkTranslation3Axis()
    {
        translation_method = "axis";
    }

    public void checkTranslationFree()
    {
        translation_method = "free";
    }

    void changeTextOpacity()
    {
        for (int i = 0; i < modeText.Length; i++) {
            // If the current modeText GameObject has a tag that matches the current mode, set its color alpha value to 1.0f.
            if (mode == textTags[i]) {
                Color temp = modeText[i].color;
                temp.a = 1.0f;
                modeText[i].color = temp;
            }
            // Otherwise, set its color alpha value to 0.5f.
            else {
                Color temp = modeText[i].color;
                temp.a = 0.5f;
                modeText[i].color = temp;
            }
        }
    }

    void selectAxis(GameObject obj)
    {
        if (buttonDelta1==1) {
            translation_mode = mod(translation_mode + 1,3);
            buttonDelta1 = 0;
        }
        else if (buttonDelta2==1){
            translation_mode = mod(translation_mode - 1,3);
            buttonDelta2 = 0;
        }
        translation_three_axis(obj);
        Debug.Log("Translation mode: " + translation_mode);
    }

    void translation_wrt_world(GameObject obj)
    {
        rotation(translationAxis);
           if (button1==1){
                body.transform.position += translationAxis.transform.TransformDirection(new Vector3(0.0f,0.01f*1.0f, 0.0f));
            }
            if (button2==1){
                body.transform.position -= translationAxis.transform.TransformDirection(new Vector3(0.0f,0.01f*1.0f, 0.0f));
            }
    }

    void translation_three_axis(GameObject obj)
    {
        if (translation_mode == 0){
            obj.transform.position = obj.transform.position + new Vector3(0, 0, -scaling_factor*delta);
        }
        else if (translation_mode == 1){
            obj.transform.position = obj.transform.position + new Vector3(-scaling_factor*delta, 0, 0);
        }
        else if (translation_mode == 2){
            obj.transform.position = obj.transform.position + new Vector3(0, -scaling_factor*delta, 0);
        }
    }

    void translation_wrt_gameObject(GameObject obj, bool dir)
    {
        // transform.position = transform.position + new Vector3(0, delta_x, delta_y);
        if (dir){
            obj.transform.position += obj.transform.TransformDirection(new Vector3(0.0f,0.01f*1.0f,0.0f));
        }
        else{
            obj.transform.position -= obj.transform.TransformDirection(new Vector3(0.0f,0.01f*1.0f,0.0f));
        }
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

    void opacity()
    {
        float outOpacity = (-curr_theta + 3.14f) / (2*3.14f);
        for (int i = 0; i < mat1.Length; i++) {
            mat1[i].SetInt("_opacityMode", 1);
            mat1[i].SetFloat("_opacity", outOpacity);
        }
        // Debug.Log(outOpacity);
        opacitySlider.SliderValue = outOpacity;
    }

/*
    void checkOpacityGoal() {
        float errorThresh = .05f; //maximum error between goal and curr position
        float goalVal = opacityGoalObj.GetComponent<Renderer>().material.GetFloat("_opacity"); // goal position
        float error = Mathf.Abs(opacitySlider.value - goalVal);
        Debug.Log(error);
        if (error < errorThresh) {
            string errorMsg = "Error: " + error.ToString();
            // Check if the user maintains the goal position for 3 seconds
            isGoalMaintained(errorMsg);
        }
    }
*/
    // void checkPoseGoal(GameObject obj, GameObject goalObj)
    // {
    //     Vector3 goalPos = goalObj.transform.position; // goal position
    //     float errorTThresh = 20.0f; //maximum error between goal and curr position
    //     Quaternion goalRot = goalObj.transform.rotation; // goal rotation
    //     float errorRThresh = 0.1f; //maximum errorR between goal and curr orientation

    //     // get errors
    //     Vector3 errorT = new Vector3();
    //     errorT[0] = Mathf.Abs(goalPos.x - obj.transform.position.x);
    //     errorT[1] = Mathf.Abs(goalPos.y - obj.transform.position.y);
    //     errorT[2] = Mathf.Abs(goalPos.z - obj.transform.position.z);

        
        
    //     // get errorR
    //     Vector4 errorR = new Vector4();
    //     errorR[0] = Mathf.Abs(goalRot.x - obj.transform.rotation.x);
    //     errorR[1] = Mathf.Abs(goalRot.y - obj.transform.rotation.y);
    //     errorR[2] = Mathf.Abs(goalRot.z - obj.transform.rotation.z);
    //     errorR[3] = Mathf.Abs(goalRot.w - obj.transform.rotation.w);

    //     //calculate magnitude of errorT
    //     float errorTMag = Mathf.Sqrt(Mathf.Pow(errorT[0],2.0f) + Mathf.Pow(errorT[1], 2.0f) + Mathf.Pow(errorT[2], 2.0f));
    //     // Debug.Log("ErrorT: " + errorTMag);

    //     //calculate magnitude of errorR
    //     float errorRMag = Mathf.Sqrt(Mathf.Pow(errorR[0],2.0f) + Mathf.Pow(errorR[1], 2.0f) + Mathf.Pow(errorR[2], 2.0f) + Mathf.Pow(errorR[3], 2.0f));
    //     // Debug.Log("ErrorR: " + errorRMag);
        
    //     if (errorTMag < errorTThresh & errorRMag < errorRThresh)
    //     {
    //         string errorMsg = "Error(position): " + errorTMag.ToString() + " Error(rotation): " + errorRMag.ToString();
    //         // Check if the user maintains the goal position for 3 seconds
    //         isGoalMaintained(errorMsg);
    //     }
    //     else
    //     {
    //         maintainTime = 0.0f;
    //     }
    // }

    // void checkSlicerGoal()
    // {

    // }

    // Utils
    void setGameObjectArrayActive(ref GameObject[] obj, bool enable)
    {
        for (int i=0; i<obj.Length; i++){
            obj[i].SetActive(enable);
        }
    }
/*
    void disableGoalObjects()
    {
        //TODO: Disable the textbox
        goalFlag = true;
        rotationGoalObj.SetActive(false);
        translationGoalObj.SetActive(false);
        slicerGoalObj.SetActive(false);
        opacityGoalObj.SetActive(false);
        setGameObjectArrayActive(ref resultText, false);
        maintainTime = 0.0f;
    }
*/
    // void goalAchieved(string errorMsg)
    // {
    //     float timeToCompletion = 0.0f;
    //     timeToCompletion = Time.time - startTime - 1.5f;
    //     goalFlag = true;
    //     resultText[0].GetComponent<TMP_Text>().text = "Goal Reached!" + "\n" + 
    //                                             "Time to Completion: " + timeToCompletion.ToString() + " seconds" +
    //                                             "\n" + "Accuracy: " + errorMsg;
    //     setGameObjectArrayActive(ref resultText, true);
    //     // Debug.Log(timeToCompletion);
    // }

    // void isGoalMaintained(string errorMsg)
    // {
        
    //         if (maintainTime == 0.0f)
    //         {
    //             maintainTime = Time.time;
    //         }
    //         else if (Time.time - maintainTime > 1.5f)
    //         {
    //             if (goalFlag == false)
    //             { //goal reached
    //                 goalAchieved(errorMsg);
    //             } else
    //             {
    //                 // Debug.Log("Goal Not Reached");
    //             }        
    //         }
    // }

    public float[] getButtonState()
    {
        return new float[] {button1, button2, button3, button4};
    }

    public void reset()
    {
        resetFlag = true;
    }

    int mod(int x, int m) {
        int r = x%m;
        return r<0 ? r+m : r;
    }
}