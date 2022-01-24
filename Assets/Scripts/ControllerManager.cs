using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class ControllerManager : MonoBehaviour
{
    public InputDevice leftController;
    public InputDevice rightController;
    // Start is called before the first frame update
    void Start()
    {
        List<InputDevice> leftDevices = new List<InputDevice>();
        List<InputDevice> rightDevices = new List<InputDevice>();

        InputDeviceCharacteristics rightControllerCharacteristics = InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller;
        InputDeviceCharacteristics leftControllerCharacteristics = InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller;

        InputDevices.GetDevicesWithCharacteristics(leftControllerCharacteristics, leftDevices);
        InputDevices.GetDevicesWithCharacteristics(rightControllerCharacteristics, rightDevices);


        if (leftDevices.Count > 0) {
            leftController = leftDevices[0];
        }
        if (rightDevices.Count > 0) {
            rightController = rightDevices[0];
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
