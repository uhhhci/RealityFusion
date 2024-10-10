using UnityEngine;
using UnityEngine.XR;
using Microsoft.MixedReality.Toolkit.UI;
using AOT;
using System.IO;
using UnityEngine.Rendering;
public class GaussianRendererImagePlane : MonoBehaviour
{
    // Start is called before the first frame update
    [Header("Gaussian Model Settings")]
    public string modelPath;
    public string appPath;
    // adopt from the other plugiin
    [Range(0.2f, 1.0f)]
    public float resScale = 0.5f;
    public float renderScale = 0.5f;

    [Header("Gaussian Unity Game Settings")]
    public Material leftMaterial;
    public Material rightMaterial;
    public Camera GSLeftCam, GSRightCam;
    public GameObject LeftPlane, RightPlane;
   // public Vector3 CamRotOffset = Vector3.zero; 

    Texture2D leftTexture, rightTexture;
    Texture2D leftDepthTex, rightDepthTex;
    int renderWidth = 1000;
    int renderHeight = 1000;

    public Transform GSObjectTransform;
    private Matrix4x4 _mTRS;

    [Range(0.001f, 1.0f)]
    public float scaling_modifier = 0.001f;

    static bool graphics_initialized = false;
    static bool texture_created = false;
    static bool initialized = false;
    private const int INIT_EVENT = 0x0001;
    private const int DEINIT_EVENT = 0x0003;
    private const int CREATE_TEX = 0x0004;
    private const int DRAW_EVENT_LEFT = 0x0005;
    private const int DRAW_EVENT_RIGHT = 0x0006;

    // VR related
    static System.IntPtr leftHandle = System.IntPtr.Zero;
    static System.IntPtr rightHandle = System.IntPtr.Zero;
    static System.IntPtr leftHandleDepth = System.IntPtr.Zero;
    static System.IntPtr rightHandleDepth = System.IntPtr.Zero;

    // Transform related
    Vector3 leftEyePos = Vector3.zero;
    Vector3 rightEyePos = Vector3.zero;
    Quaternion leftRot = Quaternion.identity;
    Quaternion rightRot = Quaternion.identity;

    void Start()
    {

        // testing stuffs
        graphics_initialized = false;
        texture_created = false;
        initialized = false;
        if (File.Exists(modelPath))
        {
            GaussianRFRenderPlugin.unity_gaussian_set_init_values(modelPath, true, false);
            GL.IssuePluginEvent(GaussianRFRenderPlugin.GetRenderEventFunc(), INIT_EVENT);
        }
        else
        {
            Debug.LogError("3GDS model : " + modelPath + " not found");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
         Application.Quit();
#endif
        }

    }

    // Update is called once per frame
    void Update()
    {


        if (GaussianRFRenderPlugin.get_initialization_state() && !graphics_initialized)
        {
            Debug.Log("native graphics initialized");
            graphics_initialized = true;
        }

        if (graphics_initialized && !texture_created)
        {
            Debug.Log("native graphics texture creation");
            renderWidth = (int)((float)GSLeftCam.pixelWidth * resScale);
            renderHeight = (int)((float)GSLeftCam.pixelHeight * resScale);
            GaussianRFRenderPlugin.update_render_resolution(renderWidth, renderHeight);
            Debug.Log("render width: " + renderWidth + "render height" + renderHeight);
            GL.IssuePluginEvent(GaussianRFRenderPlugin.GetRenderEventFunc(), CREATE_TEX);
            texture_created = true;
        }

        if (leftHandle.ToInt32() == 0 || rightHandle.ToInt32() == 0 || leftHandleDepth.ToInt32() == 0 || rightHandleDepth.ToInt32() == 0)
        {
            rightHandle = GaussianRFRenderPlugin.get_right_handle();
            leftHandle = GaussianRFRenderPlugin.get_left_handle();
            rightHandleDepth = GaussianRFRenderPlugin.get_right_depth_handle();
            leftHandleDepth = GaussianRFRenderPlugin.get_left_depth_handle();
        }


        if (leftHandle.ToInt32() != 0 && rightHandle.ToInt32() != 0 && rightHandleDepth.ToInt32() != 0 && leftHandleDepth.ToInt32() != 0 && !initialized)
        {
            //renderWidth  = (int)((float)GSLeftCam.pixelWidth * resScale);
            //renderHeight = (int)((float)GSLeftCam.pixelHeight * resScale);


            rightTexture = Texture2D.CreateExternalTexture(renderWidth, renderHeight, TextureFormat.RGBAFloat, false, true, rightHandle);
            leftTexture = Texture2D.CreateExternalTexture(renderWidth, renderHeight, TextureFormat.RGBAFloat, false, true, leftHandle);

            rightDepthTex = Texture2D.CreateExternalTexture(renderWidth, renderHeight, TextureFormat.RFloat, false, true, rightHandleDepth);
            leftDepthTex = Texture2D.CreateExternalTexture(renderWidth, renderHeight, TextureFormat.RFloat, false, true, leftHandleDepth);

            leftMaterial.SetTexture("_GaussianSplattingTex", leftTexture);
            leftMaterial.SetTexture("_GaussianSplattingDepthTex", leftDepthTex);

            rightMaterial.SetTexture("_GaussianSplattingTex", rightTexture);
            rightMaterial.SetTexture("_GaussianSplattingDepthTex", rightDepthTex);


            GaussianRFRenderPlugin.set_camera_fov(Mathf.Deg2Rad * GSLeftCam.fieldOfView);
            GaussianRFRenderPlugin.set_camera_aspect(GSLeftCam.aspect);

            GaussianRFRenderPlugin.update_proj_matrix_left(Matrix4fToArray(Matrix4x4.Perspective(GSLeftCam.fieldOfView, GSLeftCam.aspect, GSLeftCam.nearClipPlane, GSLeftCam.farClipPlane)));
            GaussianRFRenderPlugin.update_proj_matrix_right(Matrix4fToArray(Matrix4x4.Perspective(GSLeftCam.fieldOfView, GSLeftCam.aspect, GSLeftCam.nearClipPlane, GSLeftCam.farClipPlane)));

            Debug.Log("quest pro fov: " + GSLeftCam.fieldOfView);
            Debug.Log("quest pro aspect : " + GSLeftCam.aspect);

            FillLeftCamera(GSLeftCam, LeftPlane);
            FillRightCamera(GSRightCam, RightPlane);

            Camera.onPreRender += onPreRenderCamera;
            initialized = true;


        }
    }
    private void FillLeftCamera(Camera cam, GameObject plane)
    {
        float pos = (cam.nearClipPlane + 0.01f);

        //  plane.transform.position = cam.transform.position + cam.transform.forward * pos;

        float h = Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad * 0.5f) * pos * 2f;

        plane.transform.localScale = new Vector3(h * cam.aspect, h, 1);
        plane.transform.localPosition = new Vector3(- getIPDFromXRPlugin() / 2, 0, pos);
    }

    private void FillRightCamera(Camera cam, GameObject plane)
    {
        float pos = (cam.nearClipPlane + 0.01f);

        //  plane.transform.position = cam.transform.position + cam.transform.forward * pos;

        float h = Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad * 0.5f) * pos * 2f;

        plane.transform.localScale = new Vector3(h * cam.aspect, h, 1);
        plane.transform.localPosition = new Vector3(getIPDFromXRPlugin() / 2, 0, pos);

    }

    void onPreRenderCamera(Camera cam)
    {

        if (leftHandle.ToInt32() != 0 && rightHandle.ToInt32() != 0 && rightHandleDepth.ToInt32() != 0 && leftHandleDepth.ToInt32() != 0 && initialized && cam.name == GSLeftCam.name)
        {
            GL.IssuePluginEvent(GaussianRFRenderPlugin.GetRenderEventFunc(), DRAW_EVENT_LEFT);
            GL.InvalidateState();
        }

        if (leftHandle.ToInt32() != 0 && rightHandle.ToInt32() != 0 && rightHandleDepth.ToInt32() != 0 && leftHandleDepth.ToInt32() != 0 && initialized && cam.name == GSRightCam.name)
        {
            GL.IssuePluginEvent(GaussianRFRenderPlugin.GetRenderEventFunc(), DRAW_EVENT_RIGHT);
            GL.InvalidateState();
        }

        updateCameraPoses();
    }


    public bool getIntializationState()
    {
        return initialized;
    }

    public void OnScalingFactorChange(SliderEventData eventData)
    {
        if (initialized)
        {
            scaling_modifier = eventData.NewValue;
            GaussianRFRenderPlugin.set_scaling_modifier(scaling_modifier);
        }
    }
    private float[] Vec2Float(Vector3 vec)
    {
        return new float[3] { vec.x, vec.y, vec.z };
    }

    private void updateCameraPoses()
    {


        GaussianRFRenderPlugin.update_leftview_matrix(Matrix4fToArrayLeft(GSLeftCam.worldToCameraMatrix));
        GaussianRFRenderPlugin.update_rightview_matrix(Matrix4fToArrayRight(GSRightCam.worldToCameraMatrix));

        if (GSObjectTransform != null)
        {
            _mTRS = Matrix4x4.TRS(GSObjectTransform.transform.position, GSObjectTransform.transform.rotation, GSObjectTransform.transform.localScale);
            GaussianRFRenderPlugin.update_model_matrix(Matrix4fToArray(_mTRS));
        }
    }
    private float[] Matrix4fToArrayLeft(Matrix4x4 m)
    {
        float IPD = getIPDFromXRPlugin();
        // flip z axis to compensate for openGL coordinate. Don't know why we have to flip the fourth column too but it works ....
        float[] arr = new float[4 * 4]
        {
            m.m00, m.m01, -m.m02, -(m.m03+IPD/2),
            m.m10, m.m11, -m.m12, -m.m13,
            m.m20, m.m21, -m.m22, -m.m23,
            m.m30, m.m31, m.m32, m.m33
        };

        return arr;
    }

    private float[] Matrix4fToArrayRight(Matrix4x4 m)
    {
        // flip z axis to compensate for openGL coordinate. Don't know why we have to flip the fourth column too but it works ....

        float IPD = getIPDFromXRPlugin();
        float[] arr = new float[4 * 4]
        {
            m.m00, m.m01,- m.m02, -(m.m03-IPD/2),
            m.m10, m.m11,- m.m12, -m.m13,
            m.m20, m.m21,- m.m22, -m.m23,
            m.m30, m.m31, m.m32, m.m33
        };

        return arr;
    }


    private float[] Matrix4fToArrayCamView(Matrix4x4 m)
    {
        // flip z axis to compensate for openGL coordinate. Don't know why we have to flip the fourth column too but it works ....
        float[] arr = new float[4 * 4]
        {
            m.m00, m.m01, m.m02, -m.m03,
            m.m10, m.m11, m.m12, -m.m13,
            -m.m20, -m.m21, -m.m22, m.m23,
            m.m30, m.m31, m.m32, m.m33
        };

        return arr;
    }


    private float[] Matrix4fToArray(Matrix4x4 m)
    {
        float[] arr = new float[4 * 4]
        {
            m.m00, m.m01, m.m02, m.m03,
            m.m10, m.m11, m.m12, m.m13,
            m.m20, m.m21, m.m22, m.m23,
            m.m30, m.m31, m.m32, m.m33
        };

        return arr;
    }

    private void OnApplicationQuit()
    {
        if (initialized)
        {
            GaussianRendererCleanup();
        }
    }

    public float getIPDFromXRPlugin()
    {
        Vector3 leftEyePos2 = InputDevices.GetDeviceAtXRNode(XRNode.LeftEye).TryGetFeatureValue(CommonUsages.leftEyePosition, out Vector3 leftEyePosValue) ? leftEyePosValue : Vector3.zero;
        Vector3 rightEyePos2 = InputDevices.GetDeviceAtXRNode(XRNode.RightEye).TryGetFeatureValue(CommonUsages.rightEyePosition, out Vector3 rightEyePosValue) ? rightEyePosValue : Vector3.zero;
        Quaternion leftRot2 = InputDevices.GetDeviceAtXRNode(XRNode.LeftEye).TryGetFeatureValue(CommonUsages.leftEyeRotation, out Quaternion leftEyeRotValue) ? leftEyeRotValue : Quaternion.identity;
        Quaternion rightRot2 = InputDevices.GetDeviceAtXRNode(XRNode.RightEye).TryGetFeatureValue(CommonUsages.rightEyeRotation, out Quaternion rightEyeRotValue) ? rightEyeRotValue : Quaternion.identity;

        return (Quaternion.Inverse(rightRot2) * rightEyePos2).x - (Quaternion.Inverse(leftRot2) * leftEyePos2).x;
    }


    void GaussianRendererCleanup()
    {

        Camera.onPreRender -= onPreRenderCamera;
        GL.IssuePluginEvent(GaussianRFRenderPlugin.GetRenderEventFunc(), DEINIT_EVENT);
        leftHandle = System.IntPtr.Zero;
        rightHandle = System.IntPtr.Zero;
        leftHandleDepth = System.IntPtr.Zero;
        rightHandleDepth = System.IntPtr.Zero;
        initialized = false;
        graphics_initialized = false;
        texture_created = false;

    }
}
