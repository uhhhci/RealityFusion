using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosOdom = RosMessageTypes.Nav.OdometryMsg;

using System;
using System.Linq;
using System.Text;

namespace turtlebot
{
    public class TurtlebotPoseTracker : MonoBehaviour
    {
        // Start is called before the first frame update
        public GameObject turtlebotAvatar;
        public Vector3 offset;
        public Quaternion roff;

        private Vector3 position = Vector3.zero;
        private float positionVel = 0f;
        private Quaternion rotation = Quaternion.identity;
        private float rotationVel = 0f;
        private bool isMessageReceived = false;

        private float sumVels = 0f;
        private float countVels = 0f;

        public void Start()
        {
            ROSConnection.GetOrCreateInstance().Subscribe<RosOdom>("odom", OdomChange);
            Debug.Log("Odometry Subscriber Setup.");
        }

        private void FixedUpdate()
        {

            if (isMessageReceived)
            {

                var vel = countVels > 0 ? sumVels / countVels : positionVel;
                vel = Mathf.Max(vel, positionVel);
                //Debug.Log("Found " + countVels + " entries. Average = " + vel);
                sumVels = countVels = 0;

                //turtlebotAvatar.transform.position = Vector3.LerpUnclamped(turtlebotAvatar.transform.position, position, Time.fixedDeltaTime * vel);
                //turtlebotAvatar.transform.rotation = Quaternion.Lerp(turtlebotAvatar.transform.rotation, rotation, Time.fixedDeltaTime * rotationVel);
                
            }

        }

        //callback for subscription
        private void OdomChange(RosOdom odomMessage)
        {
            //Debug.Log("received");
            var p = GetPosition(odomMessage);
            position = new Vector3(p.y, -p.z * 1.5f, p.x);
            position += offset;

            var q = GetRotation(odomMessage);
            rotation = new Quaternion(q.y * roff.y, q.z * roff.z, q.x * roff.x, q.w * roff.w);

            positionVel = GetLinearVelocity(odomMessage);
            rotationVel = GetAngularVelocity(odomMessage);

            sumVels += positionVel;
            countVels++;
            //Debug.Log("added");

            isMessageReceived = true;
        }

        private Vector3 GetPosition(RosOdom odomMessage)
        {
            return new Vector3(
                (float)odomMessage.pose.pose.position.x,
                (float)odomMessage.pose.pose.position.y,
                (float)odomMessage.pose.pose.position.z);
        }

        private Quaternion GetRotation(RosOdom odomMessage)
        {
            return new Quaternion(
                (float)odomMessage.pose.pose.orientation.x,
                (float)odomMessage.pose.pose.orientation.y,
                (float)odomMessage.pose.pose.orientation.z,
                (float)odomMessage.pose.pose.orientation.w);
        }

        private float GetLinearVelocity(RosOdom odomMessage)
        {
            var R = new Vector4(
                (float)odomMessage.pose.pose.orientation.x,
                (float)odomMessage.pose.pose.orientation.y,
                (float)odomMessage.pose.pose.orientation.z,
                (float)odomMessage.pose.pose.orientation.w);
            var vel_body = new Vector4(
                (float)odomMessage.twist.twist.linear.x,
                (float)odomMessage.twist.twist.linear.y,
                (float)odomMessage.twist.twist.linear.z,
                1.0f);
            var v = Mathf.Abs(Vector4.Dot(R, vel_body));
            //Debug.Log("Linear velocity: " + v);
            return v;
        }

        private float GetAngularVelocity(RosOdom odomMessage)
        {
            var R = new Vector4(
                (float)odomMessage.pose.pose.orientation.x,
                (float)odomMessage.pose.pose.orientation.y,
                (float)odomMessage.pose.pose.orientation.z,
                (float)odomMessage.pose.pose.orientation.w);
            var vel_body = new Vector4(
                (float)odomMessage.twist.twist.angular.x,
                (float)odomMessage.twist.twist.angular.y,
                (float)odomMessage.twist.twist.angular.z,
                1.0f);
            var v = Mathf.Abs(Vector4.Dot(R, vel_body));
            //Debug.Log("Angular velocity: " + v);
            return v;
        }
    }
}
