using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class GaussianLocomotion : MonoBehaviour
{
    [Header("Basic Settings")]
    public GameObject HeadObj;
    public CharacterController characterController;
    public CharacterController characterController2;

    [Header("VR Locomotion")]
    [Tooltip("Locomotion settings for VR uses")]
    public bool enabledContinuousLocomotion = true;
    public bool flipDirection = false;
    [SerializeField]
    private float speed = 1f;

    private InputDevice XRLeftController;
    private InputDevice XRRightController;

    void OnEnable()
    {
        if (enabledContinuousLocomotion)
        {
            GetLeftController();
            GetRightController();
        }

    }

    void GetLeftController()
    {
        var leftHandedControllers = new List<InputDevice>();
        var desiredCharacteristics = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller;
        InputDevices.GetDevicesWithCharacteristics(desiredCharacteristics, leftHandedControllers);


        foreach (var device in leftHandedControllers)
        {
            Debug.Log(string.Format("Device name '{0}' has characteristics '{1}'", device.name, device.characteristics.ToString()));
            XRLeftController = device;
        }
        Debug.Log(XRLeftController.name);
    }

    void GetRightController()
    {
        var rightHandedControllers = new List<InputDevice>();
        var desiredCharacteristics = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller;
        InputDevices.GetDevicesWithCharacteristics(desiredCharacteristics, rightHandedControllers);


        foreach (var device in rightHandedControllers)
        {
            Debug.Log(string.Format("Device name '{0}' has characteristics '{1}'", device.name, device.characteristics.ToString()));
            XRRightController = device;
        }

    }
    void Start()
    {
        if (enabledContinuousLocomotion)
        {
            GetLeftController();
            GetRightController();
        }
    }


    // integrate continuous locomotion

    void FixedUpdate()
    {
        if (XRLeftController == null)
        {
            GetLeftController();
        }
        if (XRRightController == null)
        {
            GetRightController();
        }


        float headRoty = HeadObj.transform.eulerAngles.y;
        Quaternion headYaw = Quaternion.Euler(0, headRoty, 0);
        Vector3 direction;

        // left controller controls character horizontal movement, only if the trigger button is pressed! 
        if (XRLeftController != null && enabledContinuousLocomotion && HeadObj != null)
        {
            bool isTriggerDown;
            XRLeftController.TryGetFeatureValue(CommonUsages.triggerButton, out isTriggerDown);

            Vector2 joystickMovement;
            XRLeftController.TryGetFeatureValue(CommonUsages.primary2DAxis, out joystickMovement);
            bool isLeftTouchEvent;

            XRLeftController.TryGetFeatureValue(CommonUsages.primary2DAxisTouch, out isLeftTouchEvent);
            if (isLeftTouchEvent && !isTriggerDown)
            {
                if (headRoty > 0)
                {
                    direction = headYaw * new Vector3(joystickMovement.x, 0, joystickMovement.y);

                }
                else
                {
                    direction = headYaw * new Vector3(-joystickMovement.x, 0, -joystickMovement.y);
                }
                characterController.Move(direction * speed * Time.deltaTime);
                if (characterController2 != null)
                {
                    characterController2.Move(direction * speed * Time.deltaTime);

                }
            }

        }


        // right controller controls character up and down movement
        if (XRRightController != null && enabledContinuousLocomotion)
        {
            bool isTriggerDown;
            XRRightController.TryGetFeatureValue(CommonUsages.triggerButton, out isTriggerDown); 

            Vector2 joystickMovement;
            XRRightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out joystickMovement);

            bool isRightTouchEvent;

            XRRightController.TryGetFeatureValue(CommonUsages.primary2DAxisTouch, out isRightTouchEvent);
            if (isRightTouchEvent && !isTriggerDown)
            {
                if(headRoty > 0)
                {
                    direction = headYaw * new Vector3(joystickMovement.x, joystickMovement.y, 0);

                }
                else
                {
                    direction = headYaw * new Vector3(-joystickMovement.x, joystickMovement.y, 0);
                }
                characterController. Move(direction * speed * Time.deltaTime);
                if(characterController2!= null)
                {
                    characterController2.Move(direction * speed * Time.deltaTime);

                }

            }
        }
        
    }

}
