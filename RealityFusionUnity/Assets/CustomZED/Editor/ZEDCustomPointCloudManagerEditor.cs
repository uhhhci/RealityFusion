using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ZEDCustomPointCloudManager))]
public class ZEDCustomPointCloudManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ZEDCustomPointCloudManager manager = (ZEDCustomPointCloudManager)target;

        DrawDefaultInspector();

        if (GUILayout.Button("Load ZED Cloud to Plugin"))
        {
           // manager.SetZedPointCloudInNativePlugin();
        }

        if (GUILayout.Button("Register ZED Cloud"))
        {
            //manager.RegisterzedPointCloudwithGS();
        }
    }
}