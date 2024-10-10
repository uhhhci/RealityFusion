using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using turtlebot.control;

namespace turtlebot.camera
{
    public class EgocentricView : MonoBehaviour
    {
        public GameObject odomTracker;
        public GameObject mrtkSpace;
        public Vector3 offset = new Vector3(0, 0,0.1f);

        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            Vector3 targetPos = odomTracker.transform.position + offset;
            Vector3 smoothPos = Vector3.Lerp(transform.position, targetPos, 5 * Time.deltaTime);

            Quaternion targetRot = odomTracker.transform.rotation;
            Quaternion smoothRot = Quaternion.Lerp(transform.rotation, targetRot, 5 * Time.deltaTime);
            transform.position = smoothPos;
            transform.rotation = smoothRot;

            mrtkSpace.transform.position = smoothPos;
            mrtkSpace.transform.rotation = smoothRot;

        }


    }
}