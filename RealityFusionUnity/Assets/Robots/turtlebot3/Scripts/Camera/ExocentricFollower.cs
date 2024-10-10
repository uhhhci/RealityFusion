using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace turtlebot.camera
{
    public class ExocentricFollower : MonoBehaviour
    {
        public GameObject odomTracker;
        public GameObject mixedRealityPlaySpace;

        public float smoothspeed = 5f;
        public Vector3 offset = new Vector3 (0, 0, -0.9f);
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if(odomTracker == null)
            {
                return;

            }

            Vector3 targetPos  = odomTracker.transform.position + offset;
            Vector3 smoothPos  = Vector3.Lerp (transform.position, targetPos, smoothspeed * Time.deltaTime);
            transform.position = smoothPos;
            //if (mixedRealityPlaySpace != null)
            //{
            //    mixedRealityPlaySpace.transform.position = smoothPos;
            //}

           // transform.LookAt (odomTracker);
        }
    }
}