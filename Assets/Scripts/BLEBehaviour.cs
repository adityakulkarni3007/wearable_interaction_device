using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using UnityEngine.UI;
using TMPro;
using Random = System.Random;

public class BLEBehaviour : MonoBehaviour
{
    public TMP_Text TextIsScanning, TextTargetDeviceConnection, TextTargetDeviceData, TextDiscoveredDevices;
    public Button ButtonStartScan;
    private fliterUpdate FliterUpdate;
    private modes Modes;
    
    // Change this to match your device.
    string targetDeviceName = "Arduino";
    string serviceUuid = "{4fafc201-1fb5-459e-8fcc-c5c9c331914b}";
    string[] characteristicUuids = {
        "{beb5483e-36e1-4688-b7f5-ea07361b26a8}",      // CUUID 1
        //    "{00002a01-0000-1000-8000-00805f9b34fb}",       // CUUID 2
        //   "{00002a57-0000-1000-8000-00805f9b34fb}",       // CUUID 3
    };

    BLE ble;
    BLE.BLEScan scan;
    bool isScanning = false, isConnected = false;
    string deviceId = null, remoteAngle = null;  
    float[] q = null, sensorData = null; 
    string[] rawIMUData = null;
    IDictionary<string, string> discoveredDevices = new Dictionary<string, string>();
    int devicesCount = 0;
    byte[] valuesToWrite;

    // BLE Threads 
    Thread scanningThread, connectionThread, readingThread, writingThread;
    
    void Start()
    {
        ble = new BLE();
        
        TextTargetDeviceConnection.text = targetDeviceName + " not found.";
        readingThread = new Thread(ReadBleData);
    }

    void Update()
    {  
        if (isScanning)
        {
            if (discoveredDevices.Count > devicesCount)
            {
                UpdateGuiText("scan");

                devicesCount = discoveredDevices.Count;
            }                
        } else
        {
            if (TextIsScanning.text != "Not scanning.")
            {
                TextIsScanning.color = Color.white;
                TextIsScanning.text = "Not scanning.";
            }
        }

        // The target device was found.
        if (deviceId != null && deviceId != "-1")
        {
            // Target device is connected and GUI knows.
            if (ble.isConnected && isConnected)
            {
                //UpdateGuiText("writeData");
            }
            // Target device is connected, but GUI hasn't updated yet.
            else if (ble.isConnected && !isConnected)
            {
                UpdateGuiText("connected");
                isConnected = true;
                // Device was found, but not connected yet. 
            } else if (!isConnected)
            {
                TextTargetDeviceConnection.text = "Found target device:\n" + targetDeviceName;
            } 
        }
    }
    
    public void StartScanHandler()
    {
        devicesCount = 0;
        isScanning = true;
        discoveredDevices.Clear();
        scanningThread = new Thread(ScanBleDevices);
        scanningThread.Start();
        TextIsScanning.color = new Color(244, 180, 26);
        TextIsScanning.text = "Scanning...";
        TextIsScanning.text +=
            $"Searching for {targetDeviceName} with \nservice {serviceUuid} and \ncharacteristic {characteristicUuids[0]}";
        TextDiscoveredDevices.text = "";
    }
    
    void ScanBleDevices()
    {
        scan = BLE.ScanDevices();
        Debug.Log("BLE.ScanDevices() started.");
        scan.Found = (_deviceId, deviceName) =>
        {
            if (!discoveredDevices.ContainsKey(_deviceId))
            {
                Debug.Log("found device with name: " + deviceName);
                discoveredDevices.Add(_deviceId, deviceName);
            }

            if (deviceId == null && deviceName == targetDeviceName)
            {
                deviceId = _deviceId;
            }
        };

        scan.Finished = () =>
        {
            isScanning = false;
            Debug.Log("scan finished");
            if (deviceId == null)
                deviceId = "-1";
        };
        while (deviceId == null)
            Thread.Sleep(500);
        scan.Cancel();
        scanningThread = null;
        isScanning = false;
        
        if (deviceId == "-1")
        {
            Debug.Log($"Scan is finished. {targetDeviceName} was not found.");
            return;
        }
        Debug.Log($"Found {targetDeviceName} device with id {deviceId}.");
        StartConHandler();
    }
    
    public void StartConHandler()
    {
        connectionThread = new Thread(ConnectBleDevice);
        connectionThread.Start();
    }

    void ConnectBleDevice()
    {
        if (deviceId != null)
        {
            try
            {
                Debug.Log($"Attempting to connect to {targetDeviceName} device with id {deviceId} ...");
                ble.Connect(deviceId,
                    serviceUuid,
                    characteristicUuids);
            } catch(Exception e)
            {
                Debug.Log("Could not establish connection to device with ID " + deviceId + "\n" + e);
            }
        }
        if (ble.isConnected)
            Debug.Log("Connected to: " + targetDeviceName);
    }
    
    void UpdateGuiText(string action)
    {
        switch(action) {
            case "scan":
                TextDiscoveredDevices.text = "";
                foreach (KeyValuePair<string, string> entry in discoveredDevices)
                {
                    TextDiscoveredDevices.text += "DeviceID: " + entry.Key + "\nDeviceName: " + entry.Value + "\n\n";
                    Debug.Log("Added device: " + entry.Key);
                }
                break;
            case "connected":
                TextTargetDeviceConnection.text = "Connected to target device:\n" + targetDeviceName;
                break;
            case "writeData":
                if (!readingThread.IsAlive)
                {
                    readingThread = new Thread(ReadBleData);
                    readingThread.Start();
                }
                // if (remoteAngle != lastRemoteAngle)
                // {
                //     TextTargetDeviceData.text = "Remote angle: " + remoteAngle;
                //     lastRemoteAngle = remoteAngle;
                // }
                break;
        }
    }
    
    private void OnDestroy()
    {
        CleanUp();
    }

    private void OnApplicationQuit()
    {
        CleanUp();
    }

    // Prevent threading issues and free BLE stack.
    // Can cause Unity to freeze and lead
    // to errors when omitted.
    private void CleanUp()
    {
        try
        {
            scan.Cancel();
            ble.Close();
            scanningThread.Abort();
            connectionThread.Abort();
            readingThread.Abort();
            writingThread.Abort();
        } catch(NullReferenceException e)
        {
            Debug.Log("Thread or object never initialized.\n" + e);
        }        
    }

    public void StartWritingHandler()
    {
        if (deviceId == "-1" || !isConnected || (writingThread?.IsAlive ?? false))
        {
            Debug.Log("Cannot write yet");
            return;
        }
        
        byte[] bytes = new byte[] {0, 1, 2, 3};
        Random random = new Random();
        int start2 = random.Next(0, bytes.Length);
        valuesToWrite = new byte[] {bytes[start2]};
        TextTargetDeviceData.text = "Writing some new: " + valuesToWrite[0];
        
        writingThread = new Thread(WriteBleData);
        writingThread.Start();
    }
    
    private void WriteBleData()
    {
        bool ok = BLE.WritePackage(deviceId,
            serviceUuid,
            characteristicUuids[0],
            valuesToWrite);
        
        Debug.Log($"Writing status: {ok}. {BLE.GetError()}");
        writingThread = null;
    }
    
    private void ReadBleData(object obj)
    {
        byte[] packageReceived = BLE.ReadPackage();
        remoteAngle = System.Text.Encoding.ASCII.GetString(packageReceived).TrimEnd('\0');
        rawIMUData = remoteAngle.Split(",");
        if (rawIMUData != null && rawIMUData.Length > 7)
        {
            q = FliterUpdate.updateFilter(rawIMUData);
            sensorData = new float[] {float.Parse(rawIMUData[6]), float.Parse(rawIMUData[7]), float.Parse(rawIMUData[8]), float.Parse(rawIMUData[9])};
            if (q != null)
            {
                Modes.updateIMU(q, sensorData);
            }
        }
        Thread.Sleep(25);
    }
}
