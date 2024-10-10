using System.Collections;
using System.Collections.Generic;
using RosMessageTypes.Nav;
using RosMessageTypes.Geometry;
using Unity.Robotics.Visualizations;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;

namespace turtlebot.visualization { 
    public class TurtlebotOdomTracker : MonoBehaviour
    {
        // no need to add a mesh, just leave it as a child of this game object
        //public GameObject TurtlebotMesh;
        //public float thickness = 0.01f;
        //public float lengthScale = 1.0f;
        //public float sphereRadius = 1.0f;
        public bool miniature = false;
        ROSConnection ros;
        Vector3 m_botpos;
        Quaternion m_botrot;

        public Vector3 BotPos { get => m_botpos;}
        public Quaternion BotRot { get => m_botrot;}

        void Start()
        {
            ros = ROSConnection.GetOrCreateInstance();
            ros.Subscribe<OdometryMsg>("/odom", UpdatePose);
        }


        void UpdatePose(OdometryMsg message) 
        {
            // coordinate system conversion rules copied from Unity
            // public static Vector3 ConvertToRUF(Vector3 v) => new Vector3(-v.y, v.z, v.x);
            // public static Quaternion ConvertToRUF(Quaternion q) => new Quaternion(-q.y, q.z, q.x, -q.w);
            m_botpos = new Vector3(-(float)message.pose.pose.position.y, (float)message.pose.pose.position.z, (float)message.pose.pose.position.x);
            m_botrot = new Quaternion(-(float)message.pose.pose.orientation.y, (float)message.pose.pose.orientation.z, (float)message.pose.pose.orientation.x, -(float)message.pose.pose.orientation.w);

            if(miniature)
            {
                transform.position = m_botpos*0.2f;
                transform.rotation = m_botrot;

                //transform.rotation = Quaternion.Euler(m_botrot.eulerAngles * 0.2f);
            }
            else
            {
                transform.position = m_botpos;
                transform.rotation = m_botrot;
            }

        }

    }
}