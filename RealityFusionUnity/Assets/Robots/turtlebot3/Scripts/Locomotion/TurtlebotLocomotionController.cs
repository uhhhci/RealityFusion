using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using turtlebot.control;
using turtlebot;
using UnityEngine.UIElements;
using System.Dynamic;

namespace turtlebot.control {
    public class TurtlebotLocomotionController : MonoBehaviour
    {

        ROSConnection ros;
        public bool keyboardControl = true;
        public bool VRControl = true;
        public bool joystickControl = false; 

        TwistMsg msg;
        float status = 0;
        float target_linear_vel   = 0.0f;
        float target_angular_vel  = 0.0f;
        float control_linear_vel  = 0.0f;
        float control_angular_vel = 0.0f;

        // handling OpenVR controllers
        private InputDevice XRLeftController;
        private InputDevice XRRightController;
        bool lastLeftAPressed = false;
        bool lastLeftBPressed = false;
        bool lastRightAPressed = false;
        bool lastRightBPressed = false;
        bool lastGripPressed = false;


        // Start is called before the first frame update
        void Start()
        {
            ros = ROSConnection.GetOrCreateInstance();
            ros.RegisterPublisher<TwistMsg>("cmd_vel");
            msg = new TwistMsg(new Vector3Msg(0, 0, 0), new Vector3Msg(0, 0, 0)); ;
            GetLeftController();
            GetRightController();
        }

        public float getLinearVel()
        {
            return target_linear_vel;
        }

        public float getAngularVel()
        {
            return target_angular_vel;
        }
        void GetLeftController()
        {
            var leftHandedControllers = new List<InputDevice>();
            var desiredCharacteristics = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller;
            InputDevices.GetDevicesWithCharacteristics(desiredCharacteristics, leftHandedControllers);


            foreach (var device in leftHandedControllers)
            {
                //Debug.Log(string.Format("Device name '{0}' has characteristics '{1}'", device.name, device.characteristics.ToString()));
                XRLeftController = device;
            }
        }

        void GetRightController()
        {
            var rightHandedControllers = new List<InputDevice>();
            var desiredCharacteristics = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller;
            InputDevices.GetDevicesWithCharacteristics(desiredCharacteristics, rightHandedControllers);


            foreach (var device in rightHandedControllers)
            {
                //Debug.Log(string.Format("Device name '{0}' has characteristics '{1}'", device.name, device.characteristics.ToString()));
                XRRightController = device;
            }

        }

        public string vels(float target_linear_vel, float target_angular_vel)
        {
            return string.Format("currently:\tlinear vel {0}\t angular vel {1}", target_linear_vel, target_angular_vel);
        }

        public float makeSimpleProfile(float output, float input, float slop)
        {
            if (input > output)
            {
                output = Mathf.Min(input, output + slop);
            }
            else if (input < output)
            {
                output = Mathf.Max(input, output - slop);
            }
            else
            {
                output = input;
            }

            return output;
        }

        public float constrain(float input, float low, float high)
        {
            if (input < low)
            {
                input = low;
            }
            else if (input > high)
            {
                input = high;
            }
            else
            {
                input = input;
            }

            return input;
        }

        public float checkLinearLimitVelocity(float vel)
        {
            if (TurtlebotConfig.turtlebot3_model == "burger")
            {
                vel = constrain(vel, -TurtlebotConfig.BURGER_MAX_LIN_VEL, TurtlebotConfig.BURGER_MAX_LIN_VEL);
            }
            else if (TurtlebotConfig.turtlebot3_model == "waffle" || TurtlebotConfig.turtlebot3_model == "waffle_pi")
            {
                vel = constrain(vel, -TurtlebotConfig.WAFFLE_MAX_LIN_VEL, TurtlebotConfig.WAFFLE_MAX_LIN_VEL);
            }
            else
            {
                vel = constrain(vel, -TurtlebotConfig.BURGER_MAX_LIN_VEL, TurtlebotConfig.BURGER_MAX_LIN_VEL);
            }

            return vel;
        }

        public float checkAngularLimitVelocity(float vel)
        {
            if (TurtlebotConfig.turtlebot3_model == "burger")
            {
                vel = constrain(vel, -TurtlebotConfig.BURGER_MAX_ANG_VEL, TurtlebotConfig.BURGER_MAX_ANG_VEL);
            }
            else if (TurtlebotConfig.turtlebot3_model == "waffle" || TurtlebotConfig.turtlebot3_model == "waffle_pi")
            {
                vel = constrain(vel, -TurtlebotConfig.WAFFLE_MAX_ANG_VEL, TurtlebotConfig.WAFFLE_MAX_ANG_VEL);
            }
            else
            {
                vel = constrain(vel, -TurtlebotConfig.BURGER_MAX_ANG_VEL, TurtlebotConfig.BURGER_MAX_ANG_VEL);
            }

            return vel;
        }
        #region robot locomotion action
        void stopRobot()
        {
            target_linear_vel = 0.0f;
            control_linear_vel = 0.0f;
            target_angular_vel = 0.0f;
            control_angular_vel = 0.0f;
            //Debug.Log(vels(target_linear_vel, target_angular_vel));
        }

        void moveLeft()
        {
            target_angular_vel = checkAngularLimitVelocity(target_angular_vel + TurtlebotConfig.ANG_VEL_STEP_SIZE);
            status = status + 1;
            //Debug.Log(vels(target_linear_vel, target_angular_vel));
        }

        void moveRight()
        {
            target_angular_vel = checkAngularLimitVelocity(target_angular_vel - TurtlebotConfig.ANG_VEL_STEP_SIZE);
            status = status + 1;
            //Debug.Log(vels(target_linear_vel, target_angular_vel));
        }

        void moveForward()
        {
            target_linear_vel = checkLinearLimitVelocity(target_linear_vel + TurtlebotConfig.LIN_VEL_STEP_SIZE);
            status = status + 1;
            //Debug.Log(vels(target_linear_vel, target_angular_vel));
        }

        void moveBackward()
        {
            target_linear_vel = checkLinearLimitVelocity(target_linear_vel - TurtlebotConfig.LIN_VEL_STEP_SIZE);
            status = status + 1;
            //Debug.Log(vels(target_linear_vel, target_angular_vel));
        }
        #endregion
        void HandleKeyboardControlInput() {

            // control mode: keyboard testing
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.S))
            {
                stopRobot();
            }

            if (Input.GetKeyDown(KeyCode.W))
            {
                moveForward();

            }

            if (Input.GetKeyDown(KeyCode.X))
            {

                moveBackward();
            }

            if (Input.GetKeyDown(KeyCode.A))
            {
                moveLeft();
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                moveRight();
            }
        }

        void HandleOculusVRControl()
        {

            // Discreet control

            //if (OVRInput.GetDown(OVRInput.Button.Two))
            //{
            //    // B button
            //    moveForward();
            //}

            //if (OVRInput.GetDown(OVRInput.Button.One))
            //{
            //    // A Button
            //    moveBackward();
            //}


            //if (OVRInput.GetDown(OVRInput.Button.Three))
            //{
            //    //  X button
            //    moveLeft();
            //}

            //if (OVRInput.GetDown(OVRInput.Button.Four))
            //{
            //    //Y Button
            //    moveRight();
            //}

            //if (OVRInput.GetDown(OVRInput.Button.SecondaryHandTrigger) || OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger))
            //{

            //    stopRobot();
            //}


        }


        void HandleOpenVRControl()
        {

            // Discreet control

            // right A move forward
            bool rightPrimaryButtonState;
            XRRightController.TryGetFeatureValue(CommonUsages.primaryButton, out rightPrimaryButtonState);
            if (rightPrimaryButtonState && lastRightAPressed == false)
            {
                lastRightAPressed = true;
                moveForward();
            }
            if (!rightPrimaryButtonState && lastRightAPressed == true)
            {
                lastRightAPressed = false;
            }

            // right B move backwards
            bool rightSecButtonState;
            XRRightController.TryGetFeatureValue(CommonUsages.secondaryButton, out rightSecButtonState);
            if (rightSecButtonState && lastRightBPressed == false)
            {
                lastRightBPressed = true;
                moveBackward();
            }
            if (!rightSecButtonState && lastRightBPressed == true)
            {
                lastRightBPressed = false;
            }

            // left A left 
            bool leftPrimaryButtonState;
            XRLeftController.TryGetFeatureValue(CommonUsages.primaryButton, out leftPrimaryButtonState);
            if (leftPrimaryButtonState && lastLeftAPressed == false)
            {
                Debug.Log("left primary button pressed");
                lastLeftAPressed = true;
                moveRight();
            }
            if (!leftPrimaryButtonState && lastLeftAPressed == true)
            {
                lastLeftAPressed = false;
            }

            // left B right 
            bool leftSecButtonState;
            XRLeftController.TryGetFeatureValue(CommonUsages.secondaryButton, out leftSecButtonState);
            if (leftSecButtonState && lastLeftBPressed == false)
            {
                Debug.Log("left secondary button pressed");

                lastLeftBPressed = true;
                moveLeft();
            }
            if (!leftSecButtonState && lastLeftBPressed == true)
            {
                lastLeftBPressed = false;
            }


            // Grip stop
            bool leftGripState;
            XRLeftController.TryGetFeatureValue(CommonUsages.gripButton, out leftGripState);
            bool rightGripState;
            XRRightController.TryGetFeatureValue(CommonUsages.gripButton, out rightGripState);
            if (leftGripState || rightGripState && lastGripPressed == false)
            {
                lastGripPressed = true;
                stopRobot();
            }
            if (!(leftGripState && rightGripState) && lastGripPressed == true)
            {
                lastGripPressed = false;
            }

        }

        void HandleOpenVRJoyStickControl()
        {
            bool isRightTriggerDown;
            XRRightController.TryGetFeatureValue(CommonUsages.triggerButton, out isRightTriggerDown);

            Vector2 rightJoystickMovement;
            XRRightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out rightJoystickMovement);

            bool isRightTouchEvent;

            XRRightController.TryGetFeatureValue(CommonUsages.primary2DAxisTouch, out isRightTouchEvent);
            if (isRightTouchEvent && isRightTriggerDown)
            {
                if(Mathf.Abs(rightJoystickMovement.y)>0.1f)
                {
                    target_linear_vel = checkLinearLimitVelocity(rightJoystickMovement.y * 0.05f);
                }
                //if (Mathf.Abs(joystickMovement.x) > 0.5 && Mathf.Abs(joystickMovement.y)<0.5)
                //{
                //    target_angular_vel = checkAngularLimitVelocity(-joystickMovement.x * 0.5f);
                //}

            }
            else
            {
                target_linear_vel = 0;
                control_linear_vel = 0;
               // stopRobot();
            }

            bool isLeftTriggerDown;
            XRLeftController.TryGetFeatureValue(CommonUsages.triggerButton, out isLeftTriggerDown);

            Vector2 leftJoystickMovement;
            XRLeftController.TryGetFeatureValue(CommonUsages.primary2DAxis, out leftJoystickMovement);
            bool isLeftTouchEvent;

            XRLeftController.TryGetFeatureValue(CommonUsages.primary2DAxisTouch, out isLeftTouchEvent);
            if (isLeftTouchEvent && isLeftTriggerDown)
            {
                if (Mathf.Abs(leftJoystickMovement.x) > 0.1f)
                {
                    target_angular_vel = checkAngularLimitVelocity(-leftJoystickMovement.x * 0.3f);
                }
            }
            else
            {
                target_angular_vel= 0;
                control_angular_vel = 0;
            }

        }
        // Update is called once per frame
        void Update()
        {
            // control mode: keyboard testing
            if (keyboardControl)
            {
                HandleKeyboardControlInput();
            }

            if (VRControl)
            {
                //HandleOculusVRControl();
                
                if (XRLeftController == null)
                {
                    GetLeftController();
                }
                if (XRRightController == null)
                {
                    GetRightController();
                }

                HandleOpenVRControl();
            }

            if (joystickControl)
            {
                if (XRLeftController == null)
                {
                    GetLeftController();
                }
                if (XRRightController == null)
                {
                    GetRightController();
                }
                HandleOpenVRJoyStickControl();

            }

            if (status == 20)
            {
                status = 0;
            }

            msg = new TwistMsg(new Vector3Msg(0, 0, 0), new Vector3Msg(0, 0, 0));
            control_linear_vel = makeSimpleProfile(control_linear_vel, target_linear_vel, (TurtlebotConfig.LIN_VEL_STEP_SIZE / 2.0f));
            msg.linear.x = control_linear_vel;
            control_angular_vel = makeSimpleProfile(control_angular_vel, target_angular_vel, (TurtlebotConfig.ANG_VEL_STEP_SIZE / 2.0f));
            msg.angular.z = control_angular_vel;


            if (!ros.HasConnectionError)
            {
                ros.Publish("cmd_vel", msg);
            }
        }
    }

}


