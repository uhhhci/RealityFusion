using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using Microsoft.MixedReality.Toolkit.Input;
using turtlebot.control;
using turtlebot;
using UnityEngine.XR;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using Microsoft.MixedReality.Toolkit.UI;
using RosMessageTypes.Nav;

namespace turtlebot.control
{
    public class TurtlebotGoToNavigation : MonoBehaviour
    {


        public GameObject GoalStatePrefab;
        public GameObject OdomTracker;

        string goalTopic = "/move_base_simple/goal";
        string mapTopic  = "/map";
        ROSConnection  ros;
        PoseStampedMsg poseMsg;
        OccupancyGridMsg gridMsg;
        InputDevice    XRRightController;
        GameObject     goalObj;
        bool           lastRightTriggerPressed = false;
        void Start()
        {
            ros = ROSConnection.GetOrCreateInstance();
            ros.RegisterPublisher<PoseStampedMsg>(goalTopic);
            ros.Subscribe<OccupancyGridMsg>(mapTopic, UpdateGridMsg);
            GetRightController();
        }

        void Update()
        {
            HandleJoyStickInput();
        }

        void UpdateGridMsg(OccupancyGridMsg msg)
        {
            gridMsg = msg;
        }

        public void AddGoalPoint(MixedRealityPointerEventData eventData)
        {
            if (goalObj != null)
            {
                Destroy(goalObj);
                goalObj = null;
            }

            var result = eventData.Pointer.Result;
            goalObj = Instantiate(GoalStatePrefab, result.Details.Point, Quaternion.identity);
            Debug.Log("clicked goal pos: " + goalObj.transform.position);
            // add event listener to the button clicking function
            Interactable goalObjInteractable= goalObj.transform.Find("ConfirmButton").GetComponent<Interactable>();
            goalObjInteractable.OnClick.AddListener(PublishGoalPoint);
        }
        public void PublishGoalPoint()
        {
            poseMsg = new PoseStampedMsg();
            // frame_id has to be map for direct conversion!! Using base_link will results in robot's local coordinate frame!!!
            poseMsg.header.frame_id = "map"; 
            poseMsg.pose.position    = goalObj.transform.position.To<FLU>();
            poseMsg.pose.orientation = goalObj.transform.rotation.To<FLU>();
            
            ros.Publish(goalTopic, poseMsg);

        }

        void HandleJoyStickInput()
        {
            // Trigger to confirm spot
            bool rightTriggerState;
            XRRightController.TryGetFeatureValue(CommonUsages.triggerButton, out rightTriggerState);

            if (rightTriggerState && lastRightTriggerPressed == false)
            {
                lastRightTriggerPressed = true;
            }
            if (!rightTriggerState && lastRightTriggerPressed == true)
            {
                lastRightTriggerPressed = false;
            }
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
    }
}