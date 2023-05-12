# Creating Novel 3D User Interaction Devices for Augmented Reality

Our team has developed an innovative AR device in the form of a wearable glove that offers a unique and natural way to interact with virtual content. The device utilizes inertial sensors to accurately track the orientation of the user's wrist, providing a high level of precision in movement. The device's multi-modal interaction approach combines the established HoloLens AirTap gestures with our device's intuitive gestures. The latter employs a combination of inertial sensor data and finger taps, enabling a hybrid approach that lies between embodied gestures and handheld devices. This combination of modalities offers an intuitive and seamless means of interacting with virtual content, enhancing the user experience and productivity. Through this approach, we aim to bridge the gap between the virtual and physical worlds, providing a more natural and immersive AR experience.
<img src="images/Hardware.jpg" width="35%" height="35%" style="display: block; margin: 0 auto">

## Hardware Implementation
The glove is controlled using an ESP32-C3 microcontroller mounted on the back of the hand. More specificaly, Expressif's RUST development board was used and selected because it has ICM-46270 IMU with 6 degrees of freedom on-board as well as a wide range of IO pins to interface with various other sensors. The ESP32-C3 also contains a Bluetooth module that is used to connect the device to the Hololens2. 

Copper conductive tape is placed at each fingertip of the glove. The conductive tape on the thumb will be wired to ground, and the other four fingers will be each connected to an input pin of the microcontroller with a pull-up resistor to set the default state to high. If the user taps any other finger to their thumb,  this completes the circuit, pulling the input pin state down to low, thus notifying the microcontroller about the finger tap. 

The 3.7V battery and the SSD1306 OLED display are mounted at the back of the wrist. The OLED displays the relevant information about the system such as the board's connection status, current mode, and battery percentage.


## Sotfware Dependencies

- Unity Version: 2021.3.10. 
- For communication between the ESP32C3 and HoloLens2 via Bluetooth, our repository is heavily derived from https://github.com/marianylund/BleWinrtDll.git. 
- The User-Interface uses Mixed Reality Toolkit 2 Example Prefabs. 
- Visual Studio 2019

## Replicating the demo
1. Upload the Arduino code (provided in the repo) to ESP32-C3. Make sure that Serial communication is not enables since it interferes with the BLE channel. 
2. Open Unity, import TextMeshPro and add restore features using the Mixed Reality Feature Tool. The features that would be restored are: Mixed Reality Toolkit Examples, Mixed Reality Toolkit Extentions, Mixed Reality Toolkit Foundation, Mixed Reality Toolkit Standard Assets and Mixed Reality OpenXR Plugin.
3. Change the build settings to the following configuration:
<img src="images/buildConfiguration.png" width="60%" height="60%" style="display: block; margin: 0 auto">
4. Open the .sln file in Visual Studio, set the Solution Configuration to Release, the Solution Platform to ARM64 and Deploy to HoloLens2 using your preferred device.
5. Once in the game, select a scene: Demo or Labyrinth. After connecting to BlueTooth using the connection UI, the application would switch to the scene-specific UI to interact with virtual content.
7. The demo scene contains the following modes:
    - Rotation: Virtual content will mirror the user's wrist movement.
    - Translation: Choose between 3-Axis and Free Movement.
    - Slicing: Use wrist movement to rotate the cutting plane and use fingerTaps to move it along the normal of the cutting plane.
    - Opacity: Rotate the wrist to change the opacity of virtual conent. (Slider-based applications)
7. To switch between scenes, restart the application and repeat step 5.
8. Enjoy the application!
<img src="images/coolSlice.png" width="50%" height="50%" style="display: block; margin: 0 auto">
<img src="images/Labyrinth.png" width="50%" height="50%" style="display: block; margin: 0 auto">
<p align="center">
    Labyrinth
<p>
