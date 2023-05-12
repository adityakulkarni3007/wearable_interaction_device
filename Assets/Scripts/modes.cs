using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Microsoft.MixedReality.Toolkit.UI;

public class modes : MonoBehaviour
{
    // text objects
    public TMP_Text[] modeText;
    private string[] textTags;

    private int translation_mode;
    private float[] q; 
    private string translation_method;
    private float qx, qy, qz, qw, curr_theta, prev_theta, startTime, maintainTime, delta;
    private GameObject rotationGoalObj, translationGoalObj, slicerGoalObj, opacityGoalObj, indexFingerText, middleFingerText, ringFingerText, littleFingerText;
    private GameObject[] opacityText, rotationText, slicerText, translationText, translationReferenceAxes, resultText, spikes, envelope, insides, translationAxis, axisText, freeText;
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
    
    private Vector3 initialPosition;

    // Start is called before the first frame update
    void Start()
    {
        // initialize gameobjects
        cuttingPlane                = GameObject.FindWithTag("cuttingPlane");
        indexFingerText             = GameObject.FindWithTag("indexFinger");
        middleFingerText            = GameObject.FindWithTag("middleFinger");
        ringFingerText              = GameObject.FindWithTag("ringFinger");
        littleFingerText            = GameObject.FindWithTag("littleFinger");
        axisText                    = GameObject.FindGameObjectsWithTag("axisText");
        freeText                    = GameObject.FindGameObjectsWithTag("freeText");
        translationAxis             = GameObject.FindGameObjectsWithTag("translationAxis");
        opacityText                 = GameObject.FindGameObjectsWithTag("opacityText");
        rotationText                = GameObject.FindGameObjectsWithTag("rotationText");
        translationText             = GameObject.FindGameObjectsWithTag("translationText");
        slicerText                  = GameObject.FindGameObjectsWithTag("slicerText");
        spikes                      = GameObject.FindGameObjectsWithTag("spikes");
        envelope                    = GameObject.FindGameObjectsWithTag("envelope");
        insides                     = GameObject.FindGameObjectsWithTag("insides");
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
        scaling_factor              = 0.1f;
        maintainTime                = 0.0f;
        curr_theta                  = 0.0f; 
        prev_theta                  = 0.0f;
        delta                       = 0.0f;
        FliterUpdate                = GameObject.FindObjectOfType<fliterUpdate>();
        inMode                      = false;   
        initialPosition             = body.transform.position;

        // initializes the mode text by finding the objects with matching tags
        for (int i=0; i<textTags.Length; i++){
            modeText[i] = GameObject.FindWithTag(textTags[i]).GetComponent<TMP_Text>();
        }
        // sets reference axis colors
        translationReferenceAxes[0].GetComponent<Renderer>().material.SetColor("_Color", Color.red);
        translationReferenceAxes[1].GetComponent<Renderer>().material.SetColor("_Color", Color.green);
        translationReferenceAxes[2].GetComponent<Renderer>().material.SetColor("_Color", Color.blue);

        // sets the colors of the coronavirus object materials
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
        GameObject modePanel = GameObject.FindWithTag("modePanel");
        modePanel.SetActive(false);
    }

    // Update is called once per frame
    void LateUpdate()
    {
        // Set the current position and normal of the plane
        changeModeTextOpacity();
        Debug.Log("Mode: " + mode);
        Debug.Log("In Mode: " + inMode);
        Debug.Log("ButtonDelta4: " + buttonDelta4);
        if (buttonDelta4 == 1 && inMode)
        {
            inMode = false;
            mode = "menu";
            translation_method = "noMode";
            buttonDelta4 = 0;
        }
        else if(inMode==false){
            modeSelection();
        }

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
                for (int i=0; i<translationAxis.Length; i++){
                    translationAxis[i].transform.position = initialPosition;
                }
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


    /**
     * Sets the fliterUpdate global variable
     * @param update instance of fliterUpdate to use
     */
    public void getData(ref fliterUpdate update)
    {
        FliterUpdate = update;
    }

    // Executes each mode based on the value of the mode variable
    void checkMode()
    {
        if (mode == "rotation")
        {
            Debug.Log("Enter Rotation Mode");
            if (qx != null && qy != null && qz != null && qw != null)
            {
                rotation(body);   
            }
            setRotationPalmText();
        }
        else{
            changeModeTextOpacity();
            setGameObjectArrayActive(ref rotationText, false);
        }
        if (mode == "translation")
        {
            if (translation_method == "free"){
                setGameObjectArrayActive(ref translationAxis, true);
                translation_wrt_world(body);
            }
            else if (translation_method == "axis"){
                setGameObjectArrayActive(ref translationAxis, false);
                selectAxis(body);
                setGameObjectArrayActive(ref translationReferenceAxes, true);
                for (int i=0; i<translationReferenceAxes.Length; i++){
                    if (translation_mode == i){
                        translationReferenceAxes[i].GetComponent<Renderer>().material.SetFloat("_opacity", 1.0f);
                    }
                    else{
                        translationReferenceAxes[i].GetComponent<Renderer>().material.SetFloat("_opacity", 0.5f);
                    }
                }
            }
            else if(translation_method == "noMode"){
                // Check which translation mode the user wants to select
                if (buttonDelta1==1){
                    translation_method = "axis";
                }
                else if (buttonDelta2==1){
                    translation_method = "free";
                }
            }
            if (translation_method=="noMode"){
                setGameObjectArrayActive(ref translationText, true);
            }
            else if(translation_method=="axis"){
                setGameObjectArrayActive(ref axisText, true);
                setGameObjectArrayActive(ref freeText, false);
                setGameObjectArrayActive(ref translationText, false);
            }
            else if(translation_method=="free"){
                setGameObjectArrayActive(ref freeText, true);
                setGameObjectArrayActive(ref axisText, false);
                setGameObjectArrayActive(ref translationText, false);
            }
            setTranslationPalmText();
        }
        else{
            translation_method = "noMode";
            setGameObjectArrayActive(ref translationReferenceAxes, false);
            setGameObjectArrayActive(ref translationText, false);
            setGameObjectArrayActive(ref axisText, false);
            setGameObjectArrayActive(ref freeText, false);
            setGameObjectArrayActive(ref translationAxis, false);
            changeModeTextOpacity();
        }
        if (mode == "slicing")
        {
            for (int i=0; i<mat1.Length; i++)
                mat1[i].SetInt("_opacityMode", 0);
            rotation(cuttingPlane);
            for (int i = 0; i < mat1.Length; i++){
                mat1[i].SetVector("_planePosition", cuttingPlane.transform.position);
                mat1[i].SetVector("_planeNormal", cuttingPlane.transform.up);
            }
            if (button1==1){
                translation_wrt_gameObject(cuttingPlane, true);
            }
            if (button2==1){
                translation_wrt_gameObject(cuttingPlane, false);
            }
            setSlicingPalmText();
        }
        else{
            cuttingPlane.SetActive(false);
            setGameObjectArrayActive(ref slicerText, false);
            for (int i = 0; i < mat1.Length; i++){
                mat1[i].SetVector("_planePosition", new Vector3(0, 100000000 ,0));
                mat1[i].SetVector("_planeNormal", new Vector3(0,-1,0));
            }
            changeModeTextOpacity();
        }
        if (mode == "opacity")
        {
            opacity();
            setOpacityPalmText();
        }
        else{
            opacitySlider.gameObject.SetActive(false);
            setGameObjectArrayActive(ref opacityText, false);
            changeModeTextOpacity();
        }
    }

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

    // initializes rotation mode
    public void checkRotationMode()
    {
        buttonDelta1 = 0;
        inMode = true;
        mode = "rotation";
        Debug.Log("Mode inside checkRotationMode: " + mode);
        setGameObjectArrayActive(ref rotationText, true);
    }

    // initializes translation mode
    public void checkTranslationMode()
    {
        buttonDelta2 = 0;
        inMode = true;
        mode = "translation";
    }

    // initializes slicing mode
    public void checkSlicingMode()
    {
        buttonDelta3 = 0;
        inMode = true;
        mode = "slicing";
        cuttingPlane.SetActive(true);
        setGameObjectArrayActive(ref slicerText, true);
        // Set the cutting plane's position and rotation to match the body's position and rotation.
        cuttingPlane.transform.position = body.transform.position;
        cuttingPlane.transform.rotation = body.transform.rotation;
    }

    // initializes opacity mode
    public void checkOpacityMode()
    {
        buttonDelta4 = 0;
        inMode = true;
        mode = "opacity";
        opacitySlider.gameObject.SetActive(true);
        setGameObjectArrayActive(ref opacityText, true);
    }

    // sets 3-axis translation mode
    public void checkTranslation3Axis()
    {
        translation_method = "axis";
    }

    // sets free translation mode
    public void checkTranslationFree()
    {
        translation_method = "free";
    }

    // set mode text based on current mode
    void changeModeTextOpacity()
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

    // sets the axis for 3-axis translation mode 
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
    
    // translates the object with respect to the world
    void translation_wrt_world(GameObject obj)
    {
        for (int i=0; i<translationAxis.Length; i++){
            rotation(translationAxis[i]);
            if (button1==1){
                body.transform.position += translationAxis[i].transform.TransformDirection(new Vector3(0.0f,0.0f, 0.01f*1.0f));
            }
            if (button2==1){
                body.transform.position -= translationAxis[i].transform.TransformDirection(new Vector3(0.0f,0.0f, 0.01f*1.0f));
            }
        }
    }

    // object translation based on which translation axis is selected
    void translation_three_axis(GameObject obj)
    {
        if (translation_mode == 1){
            obj.transform.position = obj.transform.position + new Vector3(0, 0, -scaling_factor*delta);
        }
        else if (translation_mode == 0){
            obj.transform.position = obj.transform.position + new Vector3(-scaling_factor*delta, 0, 0);
        }
        else if (translation_mode == 2){
            obj.transform.position = obj.transform.position + new Vector3(0, -scaling_factor*delta, 0);
        }
    }

    // translates the object with respect to it's current position 
    void translation_wrt_gameObject(GameObject obj, bool dir)
    {
        if (dir){
            obj.transform.position += obj.transform.TransformDirection(new Vector3(0.0f,0.01f*1.0f,0.0f));
        }
        else{
            obj.transform.position -= obj.transform.TransformDirection(new Vector3(0.0f,0.01f*1.0f,0.0f));
        }
    }

    /** 
     * Rotates the object by converting from IMU reference frame to Unity frame before transforming the object
     * @param obj the object to be rotated
     */
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


    // adjust the opacity of the object
    void opacity()
    {
        float outOpacity = (-curr_theta + 3.14f) / (2*3.14f);
        for (int i = 0; i < mat1.Length; i++) {
            mat1[i].SetInt("_opacityMode", 1);
            mat1[i].SetFloat("_opacity", outOpacity);
        }
        opacitySlider.SliderValue = outOpacity;
    }

    /**
     * Sets object(s) active status
     * @param obj array of objects to set
     * @param enable true if setting to active, false if setting to inactive
     */
    void setGameObjectArrayActive(ref GameObject[] obj, bool enable)
    {
        for (int i=0; i<obj.Length; i++){
            obj[i].SetActive(enable);
        }
    }

    // sets the text on the palm to menu mode
    void setDefaultPalmText()
    {
        indexFingerText.GetComponent<TMP_Text>().text = "Rotation";
        middleFingerText.GetComponent<TMP_Text>().text = "Translation";
        ringFingerText.GetComponent<TMP_Text>().text = "Slicer";
        littleFingerText.GetComponent<TMP_Text>().text = "Opacity";
    }

    // sets the text on the palm to rotation mode
    void setRotationPalmText()
    {
        indexFingerText.GetComponent<TMP_Text>().text = " ";
        middleFingerText.GetComponent<TMP_Text>().text = " ";
        ringFingerText.GetComponent<TMP_Text>().text = "Freeze";
        littleFingerText.GetComponent<TMP_Text>().text = "Main Menu";
    }

    // sets the text on the palm to translation mode
    void setTranslationPalmText()
    {
        // mode text for free translation
        if (translation_method == "free"){ 
            indexFingerText.GetComponent<TMP_Text>().text = "Move";
            indexFingerText.GetComponent<TMP_Text>().color = new Color((109f/255f), (109f/255f), 1f, 1.0f);
            middleFingerText.GetComponent<TMP_Text>().text = "Move";
            middleFingerText.GetComponent<TMP_Text>().color = new Color(1f, (250f/255f), (50f/255f), 1.0f);
            ringFingerText.GetComponent<TMP_Text>().text = "Freeze";
            littleFingerText.GetComponent<TMP_Text>().text = "Main Menu";
        }
        // mode text for 3-axis translation
        else if(translation_method == "axis"){
            indexFingerText.GetComponent<TMP_Text>().text = "Next Axis";
            middleFingerText.GetComponent<TMP_Text>().text = "Previous Axis";
            ringFingerText.GetComponent<TMP_Text>().text = "Freeze";
            littleFingerText.GetComponent<TMP_Text>().text = "Main Menu";
        }
        // mode text for translation mode selection
        else if(translation_method == "noMode")
        {
            indexFingerText.GetComponent<TMP_Text>().text = "3 Axis Mode";
            middleFingerText.GetComponent<TMP_Text>().text = "Free Mode";
            ringFingerText.GetComponent<TMP_Text>().text = " ";
            littleFingerText.GetComponent<TMP_Text>().text = "Main Menu";
        }
    }

    // sets the text on the palm to slicing mode
    void setSlicingPalmText()
    {
        indexFingerText.GetComponent<TMP_Text>().text = "Move";
        indexFingerText.GetComponent<TMP_Text>().color = new Color((109f/255f), (109f/255f), 1f, 1.0f);
        middleFingerText.GetComponent<TMP_Text>().text = "Move";
        middleFingerText.GetComponent<TMP_Text>().color = new Color(1f, (250f/255f), (50f/255f), 1.0f);
        ringFingerText.GetComponent<TMP_Text>().text = "Freeze";
        littleFingerText.GetComponent<TMP_Text>().text = "Main Menu";
    }

    // sets the text on the palm to opacity mode
    void setOpacityPalmText()
    {
        indexFingerText.GetComponent<TMP_Text>().text = " ";
        middleFingerText.GetComponent<TMP_Text>().text = " ";
        ringFingerText.GetComponent<TMP_Text>().text = "Freeze";
        littleFingerText.GetComponent<TMP_Text>().text = "Main Menu";
    }

    // gets the current state of al the buttons
    public float[] getButtonState()
    {
        return new float[] {button1, button2, button3, button4};
    }

    // returns the current mode 
    public string getMode()
    {
        return mode;
    }

    // sets the reset flag to true for the next update
    public void reset()
    {
        resetFlag = true;
    }

    /** 
     * Computes the modulus of x%m 
     * @param x dividend
     * @param m divisor
     * @return x%m
     */
    int mod(int x, int m) {
        int r = x%m;
        return r<0 ? r+m : r;
    }
}