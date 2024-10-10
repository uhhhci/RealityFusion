using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RosMessageTypes.Nav;
using Unity.Robotics.Visualizations;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using static UnityEditor.PlayerSettings;

namespace turtlebot.visualization
{
    public class TurtlebotOccupanceMapTracker : MonoBehaviour
    {
        static readonly int k_Color0 = Shader.PropertyToID("_Color0");
        static readonly int k_Color100 = Shader.PropertyToID("_Color100");
        static readonly int k_ColorUnknown = Shader.PropertyToID("_ColorUnknown");
        [SerializeField]
        Vector3 m_Offset = Vector3.zero;
        [SerializeField]
        Material m_Material;
        [SerializeField]
        TFTrackingSettings m_TFTrackingSettings;
        [Header("Cell Colors")]
        [SerializeField]
        Color m_Unoccupied = Color.gray;
        [SerializeField]
        Color m_Occupied = Color.black;
        [SerializeField]
        Color m_Unknown = Color.clear;

        Mesh m_Mesh;
        Texture2D m_Texture;
        bool m_TextureIsDirty = true;
        bool m_IsDrawingEnabled;
        public bool IsDrawingEnabled => m_IsDrawingEnabled;
        float m_LastDrawingFrameTime = -1;

        Drawing3d drawing;
        ROSConnection ros;
        OccupancyGridMsg m_Message;

        public uint Width => m_Message.info.width;
        public uint Height => m_Message.info.height;

        void Start()
        {
            ros = ROSConnection.GetOrCreateInstance();
            ros.Subscribe<OccupancyGridMsg>("/map", AddMessage);
            drawing = Drawing3d.Create();
        }

        public void AddMessage(Message message)
        {
            if (!VisualizationUtils.AssertMessageType<OccupancyGridMsg>(message, "/map"))
                return;

            m_Message = (OccupancyGridMsg)message;
            m_TextureIsDirty = true;

            if (m_IsDrawingEnabled && Time.time > m_LastDrawingFrameTime)
                Redraw();

            m_LastDrawingFrameTime = Time.time;
        }

        public void Redraw()
        {
            if (m_Mesh == null)
            {
                m_Mesh = new Mesh();
                m_Mesh.vertices = new[]
                { Vector3.zero, new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(1, 0, 0) };
                m_Mesh.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
                m_Mesh.triangles = new[] { 0, 1, 2, 2, 3, 0 };
            }

            if (m_Material == null)
            {
                m_Material = (m_Material != null) ? new Material(m_Material) : new Material(Shader.Find("Unlit/OccupancyGrid"));
            }
            m_Material.mainTexture = GetTexture();
            m_Material.SetColor(k_Color0, m_Unoccupied);
            m_Material.SetColor(k_Color100, m_Occupied);
            m_Material.SetColor(k_ColorUnknown, m_Unknown);

            var origin = m_Message.info.origin.position.From<FLU>();
            var rotation = m_Message.info.origin.orientation.From<FLU>();
            rotation.eulerAngles += new Vector3(0, -90, 0); // TODO: Account for differing texture origin
            var scale = m_Message.info.resolution;

            if (drawing == null)
            {
                drawing = Drawing3dManager.CreateDrawing();
            }
            else
            {
                drawing.Clear();
            }

            drawing.SetTFTrackingSettings(m_TFTrackingSettings, m_Message.header);
            // offset the mesh by half a grid square, because the message's position defines the CENTER of grid square 0,0
            Vector3 drawOrigin = origin - rotation * new Vector3(scale * 0.5f, 0, scale * 0.5f) + m_Offset;
            drawing.DrawMesh(m_Mesh, drawOrigin, rotation,
                new Vector3(m_Message.info.width * scale, 1, m_Message.info.height * scale), m_Material);
        }

        public void DeleteDrawing()
        {
            if (drawing != null)
            {
                drawing.Destroy();
            }

            drawing = null;
        }

        public Texture2D GetTexture()
        {
            if (!m_TextureIsDirty)
                return m_Texture;

            if (m_Texture == null)
            {
                m_Texture = new Texture2D((int)m_Message.info.width, (int)m_Message.info.height, TextureFormat.R8, true);
                m_Texture.wrapMode = TextureWrapMode.Clamp;
                m_Texture.filterMode = FilterMode.Point;
            }
            else if (m_Message.info.width != m_Texture.width || m_Message.info.height != m_Texture.height)
            {
                m_Texture.Resize((int)m_Message.info.width, (int)m_Message.info.height);
            }

            m_Texture.SetPixelData(m_Message.data, 0);
            m_Texture.Apply();
            m_TextureIsDirty = false;
            return m_Texture;
        }
    }

}
