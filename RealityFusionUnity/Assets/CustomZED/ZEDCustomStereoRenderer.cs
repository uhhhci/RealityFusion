using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class ZEDCustomStereoRenderer : MonoBehaviour
{


    private bool textInitialized = false;
    /// <summary>
    /// Instance of the ZEDManager interface
    /// </summary>
    public ZEDManager zedManager = null;

    /// <summary>
    /// zed Camera controller by zedManager
    /// </summary>
    private sl.ZEDCamera zed = null;

    /// <summary>
    /// Texture that holds the RGBA value of the left image frame
    /// </summary>
    private Texture2D RGBALeftTexture;

    /// <summary>
    /// Texture that holds the RGBA value of the right image frame
    /// </summary>
    private Texture2D RGBARightTexture;

    /// <summary>
    /// Texture that holds the RGBA value of the left image frame
    /// </summary>
    private Material LeftPlaneMaterial;

    /// <summary>
    /// Texture that holds the RGBA value of the right image frame
    /// </summary>
    private Material RightPlaneMaterial;


    /// <summary>
    /// Material used to display the left image frame.
    /// </summary>
    public GameObject leftRenderPlane;

    /// <summary>
    /// Material used to display the left image frame.
    /// </summary>
    public GameObject rightRenderPlane;

    /// <summary>
    /// Material used to display the left image frame.
    /// </summary>
    public Camera XRMainCamera;

    public GameObject OdomTracker;

    public Vector3 cameraOffset = new Vector3(0, 0, 0.05f);

    float vfov, hfov, aspect;
    void Start()
    {
        if (zedManager == null)
        {
            zedManager = FindObjectOfType<ZEDManager>();
            if (ZEDManager.GetInstances().Count > 1) //We chose a ZED arbitrarily, but there are multiple cams present. Warn the user. 
            {
                Debug.Log("Warning: " + gameObject.name + "'s zedManager was not specified, so the first available ZEDManager instance was " +
                    "assigned. However, there are multiple ZEDManager's in the scene. It's recommended to specify which ZEDManager you want to " +
                    "use to display a point cloud.");
            }
        }

        if (zedManager != null)
            zed = zedManager.zedCamera;
    }

    // Update is called once per frame
    void Update()
    {
        if (zed.IsCameraReady) //Don't do anything unless the ZED has been initialized. 
        {
            if (!textInitialized)
            {
                //Create the textures. These will be updated automatically by the ZED.
                //We'll copy them each frame into XYZTextureCopy and ColorTextureCopy, which will be the ones actually displayed. 
                RGBALeftTexture  = zed.CreateTextureImageType(sl.VIEW.LEFT);
                RGBARightTexture = zed.CreateTextureImageType(sl.VIEW.RIGHT);
                
                LeftPlaneMaterial  = leftRenderPlane.GetComponent<Renderer>().material;
                RightPlaneMaterial = rightRenderPlane.GetComponent<Renderer>().material;

                LeftPlaneMaterial.SetTexture("_ZEDRGBATex", RGBALeftTexture);
                RightPlaneMaterial.SetTexture("_ZEDRGBATex", RGBARightTexture);

                // TODO: scale the image plane correctly 
                vfov = zed.VerticalFieldOfView* Mathf.Rad2Deg;
                hfov = zed.HorizontalFieldOfView* Mathf.Rad2Deg;
                aspect = hfov / vfov;

                SetImagePlaneScale(vfov, aspect, leftRenderPlane);
                SetImagePlaneScale(vfov, aspect, rightRenderPlane);

                leftRenderPlane.transform.localPosition  = new Vector3(-getIPDFromXRPlugin() / 2, 0, 0.11f);
                rightRenderPlane.transform.localPosition = new Vector3(getIPDFromXRPlugin() / 2, 0, 0.11f);

                textInitialized = true;

            }else
            {
                // apply inverse transform to the image planes to achieve seamless view synthesis
                // lerp it to make the experience smooth
                // Get the transformation matrix of the source GameObject

                this.transform.rotation = OdomTracker.transform.rotation;
                Vector3 odomFoward = OdomTracker.transform.forward * cameraOffset.z;
                Vector3 odomRight = OdomTracker.transform.right * cameraOffset.x;
                this.transform.position = OdomTracker.transform.position + new Vector3(odomFoward.x, cameraOffset.y, odomFoward.z) + new Vector3(odomRight.x, 0, odomRight.z);

                //Matrix4x4 sourceMatrix = XRMainCamera.transform.localToWorldMatrix;

                // Calculate the inverse transformation matrix
                //Matrix4x4 inverseMatrix = sourceMatrix.inverse;

                // Apply the inverse transformation matrix to the target GameObject
                //leftRenderPlane.transform.position = inverseMatrix.MultiplyPoint(leftRenderPlane.transform.position);
                //leftRenderPlane.transform.rotation = Quaternion.LookRotation(inverseMatrix.GetColumn(2), inverseMatrix.GetColumn(1));

                //rightRenderPlane.transform.position = inverseMatrix.MultiplyPoint(rightRenderPlane.transform.position);
                //rightRenderPlane.transform.rotation = Quaternion.LookRotation(inverseMatrix.GetColumn(2), inverseMatrix.GetColumn(1));
            }

        }
    }

    private void SetImagePlaneScale(float fov,float aspect, GameObject plane)
    {
        float pos = 0.11f;

        //  plane.transform.position = cam.transform.position + cam.transform.forward * pos;

        float h = Mathf.Tan(fov * Mathf.Deg2Rad * 0.5f) * pos * 2f;

        plane.transform.localScale = new Vector3(h * aspect, -h, 1);
        

    }

    public float getIPDFromXRPlugin()
    {
        Vector3 leftEyePos2 = InputDevices.GetDeviceAtXRNode(XRNode.LeftEye).TryGetFeatureValue(CommonUsages.leftEyePosition, out Vector3 leftEyePosValue) ? leftEyePosValue : Vector3.zero;
        Vector3 rightEyePos2 = InputDevices.GetDeviceAtXRNode(XRNode.RightEye).TryGetFeatureValue(CommonUsages.rightEyePosition, out Vector3 rightEyePosValue) ? rightEyePosValue : Vector3.zero;
        Quaternion leftRot2 = InputDevices.GetDeviceAtXRNode(XRNode.LeftEye).TryGetFeatureValue(CommonUsages.leftEyeRotation, out Quaternion leftEyeRotValue) ? leftEyeRotValue : Quaternion.identity;
        Quaternion rightRot2 = InputDevices.GetDeviceAtXRNode(XRNode.RightEye).TryGetFeatureValue(CommonUsages.rightEyeRotation, out Quaternion rightEyeRotValue) ? rightEyeRotValue : Quaternion.identity;

        return (Quaternion.Inverse(rightRot2) * rightEyePos2).x - (Quaternion.Inverse(leftRot2) * leftEyePos2).x;
    }

    void OnApplicationQuit()
    {
        //Free up memory. 
        LeftPlaneMaterial = null;
        RightPlaneMaterial = null;

    }

    void OnRenderObject()
    {

    }
}
