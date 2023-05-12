//IMU LIBRARIES
#include "ICM42670P.h"

//BLUETOOTH LIBRARIES
#include <BLEDevice.h>
#include <BLEServer.h>
#include <BLEUtils.h>
#include <BLE2902.h>

//OLED LIBRARIES
#include <SPI.h>
#include <Wire.h>
#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>

/* ****IMU GLOBAL VARIABLES**** */
// Instantiate an ICM42670P with LSB address set to 0
ICM42670P IMU(Wire,0);

//*****FILTERING VARIABLES
// System constants
#define deltat .05f                                         // sampling period in seconds (shown as 100 ms)
#define gyroMeasError 3.14159265358979f * (5.0f / 180.0f)     // gyroscope measurement error in rad/s (shown as 5 deg/s)
#define beta sqrt(3.0f / 4.0f) * gyroMeasError        
#define MOTOR_PIN 1
        // compute beta
//Global System Variables                                       
float SEq_1 = 1.0f, SEq_2 = 0.0f, SEq_3 = 0.0f, SEq_4 = 0.0f;	// estimated orientation quaternion elements with initial conditions
float theta_curr = 0;

int time_counter=0;

/* ****BLE GLOBAL VARIABLES**** */
BLEServer* pServer = NULL;
BLECharacteristic* pCharacteristic = NULL;
bool deviceConnected = false;
bool oldDeviceConnected = false;
#define SERVICE_UUID        "4fafc201-1fb5-459e-8fcc-c5c9c331914b"
#define CHARACTERISTIC_UUID "beb5483e-36e1-4688-b7f5-ea07361b26a8"

/* *OLED GLOBAL VARIABLES* */
#define SCREEN_WIDTH 128 // OLED display width, in pixels
#define SCREEN_HEIGHT 32 // OLED display height, in pixels

#define OLED_RESET     -1 // Reset pin # (or -1 if sharing Arduino reset pin)
#define SCREEN_ADDRESS 0x3C ///< See datasheet for Address; 0x3D for 128x64, 0x3C for 128x32
Adafruit_SSD1306 display(SCREEN_WIDTH, SCREEN_HEIGHT, &Wire, OLED_RESET);
std::string mode = "pleaseWork";

// BUTTON GLOBAL VARIABLES
int b0=0, b1=0, b2=0, b3=0;
#define BUTTON0 5
#define BUTTON1 3
#define BUTTON2 4
#define BUTTON3 2

void Displayloop() {
  display.clearDisplay(); // clear display
  display.setTextSize(1);          // text size
  display.setTextColor(WHITE);     // text color
  display.setCursor(0, 10);        // position to display

  display.print("Status: ");
  if (deviceConnected) {
    display.println("Connected");
    display.setCursor(0,20);
    display.print("Mode: ");
    display.println(String(mode.c_str())); // text to display
  } else {
    display.println("Not Connected");
    display.setCursor(0,20);
    display.print("Welcome");
  } 

  display.display();       // show on OLED  
}

class MyServerCallbacks: public BLEServerCallbacks {
    void onConnect(BLEServer* pServer) {
      deviceConnected = true;
      Displayloop();
    };

    void onDisconnect(BLEServer* pServer) {
      deviceConnected = false;
      Displayloop();
    }
};

class writeCallbacks: public BLECharacteristicCallbacks {
    void onWrite(BLECharacteristic *pCharacteristic) {
      mode = pCharacteristic->getValue();
      Displayloop();
//       /*
//       if (value.length() > 0) {
//         Serial.println("*********");
//         Serial.print("New value: ");
//         for (int i = 0; i < value.length(); i++)
//           Serial.print(value[i]);

//         Serial.println();
//         Serial.println("*********");
//       } */
    }
};

void setup() {
  //Serial.begin(9600);
  //while(! Serial) {}
  IMUsetup();
  BTsetup();

  // setup display 
  // SSD1306_SWITCHCAPVCC = generate display voltage from 3.3V internally
  if(!display.begin(SSD1306_SWITCHCAPVCC, SCREEN_ADDRESS)) {
   //Serial.println(F("SSD1306 allocation failed"));
   for(;;); // Don't proceed, loop forever
  }
  Displayloop();


  // setup buttons
  for (int i=2; i<=5; i++) {
    pinMode(i,INPUT_PULLUP);
    //digitalWrite(i,LOW);
  }
  pinMode(BUTTON0,INPUT_PULLUP);
  pinMode(BUTTON1,INPUT_PULLUP);
  pinMode(BUTTON2,INPUT_PULLUP);
  pinMode(BUTTON3,INPUT_PULLUP);

  // setup haptics
  pinMode(MOTOR_PIN,OUTPUT);
}

void IMUsetup() {
  int ret;

  // Initializing the ICM42670P
  ret = IMU.begin();
  if (ret != 0) {
    // Serial.print("ICM42670P initialization failed: ");
    // Serial.println(ret);
    while(1);
  }
  // Accel ODR = 100 Hz and Full Scale Range = 16G
  IMU.startAccel(100,16);
  // Gyro ODR = 100 Hz and Full Scale Range = 2000 dps
  IMU.startGyro(100,2000);
  // Wait IMU to start
  delay(100);
  // Plotter axis header
  // Serial.println("AccelX,AccelY,AccelZ,GyroX,GyroY,GyroZ,Temperature");
}

void BTsetup() {
    // Create the BLE Device
  BLEDevice::init("ESP32-RUST");

  // Create the BLE Server
  pServer = BLEDevice::createServer();
  pServer->setCallbacks(new MyServerCallbacks());

  // Create the BLE Service
  BLEService *pService = pServer->createService(SERVICE_UUID);

  // Create a BLE Characteristic
  pCharacteristic = pService->createCharacteristic(
                      CHARACTERISTIC_UUID,
                      BLECharacteristic::PROPERTY_READ   |
                      BLECharacteristic::PROPERTY_WRITE  |
                      BLECharacteristic::PROPERTY_NOTIFY |
                      BLECharacteristic::PROPERTY_INDICATE
                    );
  pCharacteristic->setCallbacks(new writeCallbacks());

  // https://www.bluetooth.com/specifications/gatt/viewer?attributeXmlFile=org.bluetooth.descriptor.gatt.client_characteristic_configuration.xml
  // Create a BLE Descriptor
  pCharacteristic->addDescriptor(new BLE2902());

  // Start the service
  pService->start();

  // Start advertising
  BLEAdvertising *pAdvertising = BLEDevice::getAdvertising();
  pAdvertising->addServiceUUID(SERVICE_UUID);
  pAdvertising->setScanResponse(false);
  pAdvertising->setMinPreferred(0x0);  // set value to 0x00 to not advertise this parameter
  BLEDevice::startAdvertising();
  // Serial.println("Waiting a client connection to notify...");
}

void loop() {
  Buttonloop();
  IMUloop();
  BTloop();
  //Serial.println("loop");
  
}

void Buttonloop() {
  b0 = digitalRead(BUTTON0);
  b1 = digitalRead(BUTTON1);
  b2 = digitalRead(BUTTON2);
  b3 = digitalRead(BUTTON3);
  if (b0==0 || b1==0 || b2==0 || b3==0){
    digitalWrite(MOTOR_PIN, HIGH);
  }
  else{
    digitalWrite(MOTOR_PIN, LOW);
  }
  /*
  Serial.print(b0);  
  Serial.print(" ");  
  Serial.print(b1);  
  Serial.print(" ");  
  Serial.print(b2);  
  Serial.print(" ");  
  Serial.println(b3);  
   */
}

unsigned char temp = 'H';
unsigned char* value = &temp;

bool isNan = false;
float curr_max = 0;

float a_x, a_y, a_z;
float w_x, w_y, w_z;

void IMUloop() {
  inv_imu_sensor_event_t imu_event;

  // Get last event
  // if (IMU.isAccelDataValid(&imu_event) && IMU.isGyroDataValid(&imu_event)) {

  IMU.getDataFromRegisters(&imu_event);

  float conv_to_g = 9.8/2138; 
  
  // convert to m/s2
  a_x = imu_event.accel[0]*conv_to_g;
  a_y = imu_event.accel[1]*conv_to_g;
  a_z = imu_event.accel[2]*conv_to_g;

  //convert to rads/s
  //w_x = 3.14159265358979f * (imu_event.gyro[0]/180.0f);
  //w_y = 3.14159265358979f * (imu_event.gyro[1]/180.0f);
  //w_z = 3.14159265358979f * (imu_event.gyro[2]/180.0f);
  w_x = 2000*imu_event.gyro[0]/32768;
  w_y = 2000*imu_event.gyro[1]/32768;
  w_z = 2000*imu_event.gyro[2]/32768;

  if (w_z > curr_max) {
    curr_max = w_z;
  }

  w_x = 3.14159265358979f * (w_x/180.0f);
  w_y = 3.14159265358979f * (w_y/180.0f);
  w_z = 3.14159265358979f * (w_z/180.0f);

  theta_curr += w_y*deltat;

  // Format data for // Serial Plotter
  // // Serial.print(a_x);
  // // Serial.print(",");
  // // Serial.print(a_y);
  // // Serial.print(",");
  // // Serial.print(a_z);
  // // Serial.print(",");
  // Serial.print(w_x);
  // Serial.print(",");
  // Serial.print(w_y);
  // Serial.print(",");
  // Serial.print(w_z);
  // Serial.print(",");
  // Serial.println(theta_curr);
        

  //// Serial.println(w_z);
  //// Serial.print(",");
  //// Serial.println(w_z);


  /*
  float imu_data[] = {a_x, a_y, a_z, w_x, w_y, w_z};

  for (int i=0; i<6;i++){
    // The following expression will always return False if imu_data[i] is NaN.
      if (!(imu_data[i]==imu_data[i])){
        isNan = true;
        break;
      }
      else{
        isNan = false;
      }
  }
  if (!isNan){
    filterUpdate(w_x,w_y,w_z,a_x,a_y,a_z);
    rpy();
  }
  else{
    // Serial.println("Skipping NaN data");
  }
  // Run @ ODR 10Hz
  */
  delay(deltat * 1000);
}

void filterUpdate(float w_x, float w_y, float w_z, float a_x, float a_y, float a_z) {
  
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
  norm = sqrt(a_x * a_x + a_y * a_y + a_z * a_z);
  if (norm==0) {
    return;
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
  norm = sqrt(SEqHatDot_1 * SEqHatDot_1 + SEqHatDot_2 * SEqHatDot_2 + SEqHatDot_3 * SEqHatDot_3 + SEqHatDot_4 * SEqHatDot_4);
  if (norm==0) {
    return;
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
  norm = sqrt(SEq_1 * SEq_1 + SEq_2 * SEq_2 + SEq_3 * SEq_3 + SEq_4 * SEq_4);
  SEq_1 /= norm;
  SEq_2 /= norm;
  SEq_3 /= norm;
  SEq_4 /= norm;
}

void BTloop() {
  // notify changed value
    if (deviceConnected) {
        time_counter++;
        // pCharacteristic->setValue((unsigned char*)value, 4);
        String q1 = String(SEq_2);
        String q2 = String(SEq_3);
        String q3 = String(SEq_4);
        String q4 = String(SEq_1);

        String b0s = String(b0);
        String b1s = String(b1);
        String b2s = String(b2);
        String b3s = String(b3);

        
        //String toSend = q1 + "," + q2 + "," + q3 + "," + q4 + "," + String(time_counter) + "," + b0s + "," + b1s + "," + b2s + "," + b3s + ",";
        String toSend = String(a_x) + "," + String(a_y) + "," + String(a_z) + "," +  String(w_x) + "," +  String(w_y) + "," +  String(w_z) +  "," + b0s + "," + b1s + "," + b2s + "," + b3s + ",";
        pCharacteristic->setValue(toSend.c_str());
        pCharacteristic->notify();
        //value++;   
        //DELAY REMOVED SO IT DIDN'T MESS WITH IMU DELAY     
        //delay(3); // bluetooth stack will go into congestion, if too many packets are sent, in 6 hours test i was able to go as low as 3ms
        // Format data for // Serial Plotter
        //// Serial.println(toSend.c_str());
        /*
        // Serial.print(q1);
        // Serial.print(",");
        // Serial.print(q2);
        // Serial.print(",");
        // Serial.print(q3);
        // Serial.print(",");
        // Serial.println(q4);
        */
        


       
    }

    // disconnecting
    if (!deviceConnected && oldDeviceConnected) {
        delay(500); // give the bluetooth stack the chance to get things ready
        pServer->startAdvertising(); // restart advertising
        // Serial.println("start advertising");
        oldDeviceConnected = deviceConnected;
    }
    // connecting
    if (deviceConnected && !oldDeviceConnected) {
        // do stuff here on connecting
        oldDeviceConnected = deviceConnected;
    }
}

void rpy() {
  // roll (x-axis rotation)
  
    double sinr_cosp = 2 * (SEq_1 * SEq_2 + SEq_3 * SEq_4);
    double cosr_cosp = 1 - 2 * (SEq_2 * SEq_2 + SEq_3 * SEq_3);
    float r = std::atan2(sinr_cosp, cosr_cosp);

    // pitch (y-axis rotation)
    double sinp = std::sqrt(1 + 2 * (SEq_1 * SEq_3 - SEq_2 * SEq_4));
    double cosp = std::sqrt(1 - 2 * (SEq_1 * SEq_3 - SEq_2 * SEq_4));
    float p = 2 * std::atan2(sinp, cosp) - M_PI / 2;

    // yaw (z-axis rotation)
    double siny_cosp = 2 * (SEq_1 * SEq_4 + SEq_2 * SEq_3);
    double cosy_cosp = 1 - 2 * (SEq_3 * SEq_3 + SEq_4 * SEq_4);
    float y = std::atan2(siny_cosp, cosy_cosp);
    //// Serial.print(r*180.0/3.14159);
    //// Serial.print(" ");
    //// Serial.print(p*180.0/3.14159);
    //// Serial.print(" ");
    //// Serial.println(y*180.0/3.14159);
    
}
