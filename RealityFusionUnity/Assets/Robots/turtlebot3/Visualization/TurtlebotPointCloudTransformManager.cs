using UnityEngine;


namespace turtlebot.visualization
{
    // this script aligns the projected point clouds with the tracked robot odom, so that the point cloud always projects from the 
    // robot's ZED camera frustum 
    public class TurtlebotPointCloudTransformManager : MonoBehaviour
    {
        public GameObject OdomTracker;
        //public GameObject RobotVisual;
        // TODO: compensate for rotation error!!!
        public Vector3 cameraOffset = new Vector3(0, 0.167f, 0.05f);
        void Start()
        {
            //Vector3 robotvisualRot = RobotVisual.transform.rotation.eulerAngles;  
            //this.transform.rotation = Quaternion.EulerRotation(new Vector3(robotvisualRot.x, 0, robotvisualRot.z));
        }

        // Update is called once per frame
        void Update()
        {

            this.transform.rotation = OdomTracker.transform.rotation;
            Vector3 odomFoward = OdomTracker.transform.forward * cameraOffset.z;
            Vector3 odomRight  = OdomTracker.transform.right   * cameraOffset.x;
            Vector3 odomUp = OdomTracker.transform.up * cameraOffset.y;
            this.transform.position = OdomTracker.transform.position + odomUp + odomFoward+ odomRight;
        }
    }
}