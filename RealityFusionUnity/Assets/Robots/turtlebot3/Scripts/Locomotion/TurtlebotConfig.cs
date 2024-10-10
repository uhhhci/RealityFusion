using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace turtlebot {

    public static class TurtlebotConfig
    {
        // note: at the moment we only support burger
        public static string turtlebot3_model = "burger";

        public static float BURGER_MAX_LIN_VEL = 0.05f; // 0.22f;
        public static float BURGER_MAX_ANG_VEL = 0.5f; //2.84f; 

        public static float WAFFLE_MAX_LIN_VEL = 0.26f;
        public static float WAFFLE_MAX_ANG_VEL = 1.82f;

        public static float LIN_VEL_STEP_SIZE = 0.01f;
        public static float ANG_VEL_STEP_SIZE = 0.1f;
    }
    public enum NavigationMethod
    {
        Manual,
        GoTo,
        Speech,
    }
}
