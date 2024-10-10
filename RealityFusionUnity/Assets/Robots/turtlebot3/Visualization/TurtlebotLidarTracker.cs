using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RosMessageTypes.Nav;
using RosMessageTypes.Geometry;
using RosMessageTypes.Sensor;
using Unity.Robotics.Visualizations;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using Unity.Robotics.ROSTCPConnector;


namespace turtlebot.visualization
{
    public class TurtlebotLidarTracker : MonoBehaviour
    {
        // visualization settings
        [SerializeField]
        bool m_UseIntensitySize;
        public bool UseIntensitySize { get => m_UseIntensitySize; set => m_UseIntensitySize = value; }
        [SerializeField]
        float m_PointRadius = 0.02f;
        public float PointRadius { get => m_PointRadius; set => m_PointRadius = value; }
        [HideInInspector, SerializeField]
        float m_MaxIntensity = 100.0f;
        public float MaxIntensity { get => m_MaxIntensity; set => m_MaxIntensity = value; }

        public enum ColorModeType
        {
            Distance,
            Intensity,
            Angle,
        }

        [SerializeField]
        ColorModeType m_ColorMode;
        public ColorModeType ColorMode { get => m_ColorMode; set => m_ColorMode = value; }

        // get the tracked odom data
        public GameObject OdomTracker; 

        ROSConnection ros;
        Drawing3d drawing;

        void Start()
        {
            ros = ROSConnection.GetOrCreateInstance();
            ros.Subscribe<LaserScanMsg>("/scan", UpdateLidarVisualization);
            drawing = Drawing3d.Create();
        }

        void UpdateLidarVisualization(LaserScanMsg message)
        {
            // similar to LaserScanVisualizerSettings.Draw, but need to customize the coordinate system again 
            drawing.Clear();
            PointCloudDrawing pointCloud = drawing.AddPointCloud(message.ranges.Length);
            // negate the angle because ROS coordinates are right-handed, unity coordinates are left-handed
            float angle = -message.angle_min;
            ColorModeType mode = m_ColorMode;
            if (mode == ColorModeType.Intensity && message.intensities.Length != message.ranges.Length)
                mode = ColorModeType.Distance;
            for (int i = 0; i < message.ranges.Length; i++)
            {
                if (message.ranges[i] < 100)
                {
                    
                    Vector3 point = OdomTracker.transform.TransformPoint(Quaternion.Euler(0, Mathf.Rad2Deg * angle, 0) * Vector3.forward * message.ranges[i]) ;

                    Color32 c = Color.white;
                    switch (mode)
                    {
                        case ColorModeType.Distance:
                            c = Color.HSVToRGB(Mathf.InverseLerp(message.range_min, message.range_max, message.ranges[i]), 1, 1);
                            break;
                        case ColorModeType.Intensity:
                            c = new Color(1, message.intensities[i] / m_MaxIntensity, 0, 1);
                            break;
                        case ColorModeType.Angle:
                            c = Color.HSVToRGB((1 + angle / (Mathf.PI * 2)) % 1, 1, 1);
                            break;
                    }

                    float radius = m_PointRadius;
                    if (m_UseIntensitySize && message.intensities.Length > 0)
                    {
                        radius = Mathf.InverseLerp(0, m_MaxIntensity, message.intensities[i]);
                    }
                    pointCloud.AddPoint(point, c, radius);
                }
                angle -= message.angle_increment;

            }
            pointCloud.Bake();
        }

    }
}