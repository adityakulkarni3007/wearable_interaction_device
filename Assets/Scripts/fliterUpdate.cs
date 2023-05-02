using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class fliterUpdate : MonoBehaviour
{
    float SEq_1 = 1.0f, SEq_2 = 0.0f, SEq_3 = 0.0f, SEq_4 = 0.0f, theta_curr = 0;
    float a_x, a_y, a_z, w_x, w_y, w_z;
    Vector3 v_running_avg = new Vector3(0.0f, 0.0f, 0.0f);
    Vector3 prev_v_running_avg = new Vector3(0.0f, 0.0f, 0.0f);
    Vector3 v_curr = new Vector3(0.0f, 0.0f, 0.0f);
    Queue<Vector3> v_history = new Queue<Vector3>();
    static float deltat = .05f; // sampling period in seconds
    static float gyroMeasError = 3.14159265358979f * (5.0f / 180.0f); // gyroscope measurement error in rad/s (shown as 5 deg/s)
    static float beta = Mathf.Sqrt(3.0f / 4.0f) * gyroMeasError;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void updateQuaternion(float qx, float qy, float qz, float qw)
    {
        SEq_1 = qw;
        SEq_2 = qx;
        SEq_3 = qy;
        SEq_4 = qz;
    }

    public float[] getQuaternion()
    {
        float[] q = new float[4] { SEq_1, SEq_2, SEq_3, SEq_4 };
        return q;
    }

    public float[] getrawIMU()
    {
        return new float[6] {a_x, a_y, a_z, w_x, w_y, w_z};
    }

    public float getTheta()
    {
        return theta_curr;
    }
    
    public void setTheta(float theta)
    {
        theta_curr = theta;
    }

    public float[] updateFilter(string[] rawIMUData)
    {
        // IMU data
        a_x = float.Parse(rawIMUData[0]);
        a_y = float.Parse(rawIMUData[1]);
        a_z = float.Parse(rawIMUData[2]);
        w_x = float.Parse(rawIMUData[3]);
        w_y = float.Parse(rawIMUData[4]);
        w_z = float.Parse(rawIMUData[5]);


        float[] imu_data = new float[6] {a_x, a_y, a_z, w_x, w_y, w_z};

        for (int i=0; i<6;i++){
            // The following expression will always return False if imu_data[i] is NaN.
            if (!(imu_data[i]==imu_data[i])){
                return null;
            }
        }

        // Quaternion elements of the estimated orientation

        float norm;                                                             // vector norm
        float SEqDot_omega_1, SEqDot_omega_2, SEqDot_omega_3, SEqDot_omega_4; 	// quaternion derrivative from gyroscopes elements
        float f_1, f_2, f_3;                                                    // objective function elements
        float J_11or24, J_12or23, J_13or22, J_14or21, J_32, J_33;               // objective function Jacobian elements
        float SEqHatDot_1, SEqHatDot_2, SEqHatDot_3, SEqHatDot_4;               // estimated direction of the gyrocscope error
    
        float halfSEq_1 = 0.5f * SEq_1;
        float halfSEq_2 = 0.5f * SEq_2;
        float halfSEq_3 = 0.5f * SEq_3;
        float halfSEq_4 = 0.5f * SEq_4;
        float twoSEq_1 = 2.0f * SEq_1;
        float twoSEq_2 = 2.0f * SEq_2;
        float twoSEq_3 = 2.0f * SEq_3;
  
        // Normalise the accelerometer measurement
        norm = Mathf.Sqrt(a_x * a_x + a_y * a_y + a_z * a_z);
        if (norm==0) {
            return null;
        }
        a_x /= norm;
        a_y /= norm;
        a_z /= norm;
        
        // Compute the objective function and Jacobian
        f_1 = twoSEq_2 * SEq_4 - twoSEq_1 * SEq_3 - a_x;
        f_2 = twoSEq_1 * SEq_2 + twoSEq_3 * SEq_4 - a_y;
        f_3 = 1.0f - twoSEq_2 * SEq_2 - twoSEq_3 * SEq_3 - a_z; J_11or24 = twoSEq_3;
        J_12or23 = 2.0f * SEq_4;
        J_13or22 = twoSEq_1;
        J_14or21 = twoSEq_2;
        J_32 = 2.0f * J_14or21;
        J_33 = 2.0f * J_11or24;
        
        // Compute the gradient (matrix multiplication)
        SEqHatDot_1 = J_14or21 * f_2 - J_11or24 * f_1;
        SEqHatDot_2 = J_12or23 * f_1 + J_13or22 * f_2 - J_32 * f_3;
        SEqHatDot_3 = J_12or23 * f_2 - J_33 * f_3 - J_13or22 * f_1;
        SEqHatDot_4 = J_14or21 * f_1 + J_11or24 * f_2;

        // Normalise the gradient
        norm = Mathf.Sqrt(SEqHatDot_1 * SEqHatDot_1 + SEqHatDot_2 * SEqHatDot_2 + SEqHatDot_3 * SEqHatDot_3 + SEqHatDot_4 * SEqHatDot_4);
        if (norm==0) {
            return null;
        }
        SEqHatDot_1 /= norm;
        SEqHatDot_2 /= norm;
        SEqHatDot_3 /= norm;
        SEqHatDot_4 /= norm;
        
        // Compute the quaternion derrivative measured by gyroscopes
        SEqDot_omega_1 = -halfSEq_2 * w_x - halfSEq_3 * w_y - halfSEq_4 * w_z;
        SEqDot_omega_2 = halfSEq_1 * w_x + halfSEq_3 * w_z - halfSEq_4 * w_y;
        SEqDot_omega_3 = halfSEq_1 * w_y - halfSEq_2 * w_z + halfSEq_4 * w_x;
        SEqDot_omega_4 = halfSEq_1 * w_z + halfSEq_2 * w_y - halfSEq_3 * w_x;
        
        // Compute then integrate the estimated quaternion derrivative
        SEq_1 += (SEqDot_omega_1 - (beta * SEqHatDot_1)) * deltat;
        SEq_2 += (SEqDot_omega_2 - (beta * SEqHatDot_2)) * deltat;
        SEq_3 += (SEqDot_omega_3 - (beta * SEqHatDot_3)) * deltat;
        SEq_4 += (SEqDot_omega_4 - (beta * SEqHatDot_4)) * deltat;
        
        // Normalise quaternion
        norm = Mathf.Sqrt(SEq_1 * SEq_1 + SEq_2 * SEq_2 + SEq_3 * SEq_3 + SEq_4 * SEq_4);
        SEq_1 /= norm;
        SEq_2 /= norm;
        SEq_3 /= norm;
        SEq_4 /= norm;

        theta_curr = theta_curr + w_x * deltat;
        v_curr[0] = v_curr[0] + a_x * deltat;
        v_curr[1] = v_curr[1] + a_y * deltat;
        v_curr[2] = v_curr[2] + (1.0f - a_z) * deltat;
        prev_v_running_avg = v_running_avg;
        v_history.Enqueue(v_curr);
        v_running_avg += v_curr;
        if (v_history.Count > 10){
            Vector3 toSub = v_history.Dequeue();
            v_running_avg -= toSub;
        }

        return new float[4] {SEq_1, SEq_2, SEq_3, SEq_4};
    }
}
