using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Microsoft.MixedReality.Toolkit.UI;

public class GaussianDrawing : MonoBehaviour
{

    [Header("Basic Components")]
    public Transform gsUnityObject;
    public GaussianRFRenderer gsUnityRenderer;
    public GameObject EraserSphere;
    public GameObject RevealSphere;

    private float armDistance = 1.0f;
    private float reveal_radius = 1.0f;
    private float erase_radius = 1.0f;

    private GameObject XRLeftControllerGameobj;
    private GameObject XRRightControllerGameobj;
    private InputDevice XRLeftController;
    private InputDevice XRRightController;

    // Start is called before the first frame update
    void Start()
    {
        GetLeftController();
        GetRightController();
    }

    // Update is called once per frame
    void Update()
    {
        if (XRRightControllerGameobj == null)
        {
            XRRightControllerGameobj = GameObject.Find("/MixedRealityPlayspace/Right_Right OpenVR Controller");
        }
        if (XRLeftControllerGameobj == null)
        {
            XRLeftControllerGameobj = GameObject.Find("/MixedRealityPlayspace/Left_Left OpenVR Controller");
        }

        if (XRLeftController != null && XRLeftControllerGameobj != null)
        {

            // use left hand to do reveal

            bool secondaryButtonState;
            XRLeftController.TryGetFeatureValue(CommonUsages.secondaryButton, out secondaryButtonState);

            if (secondaryButtonState)
            {
                EraserSphere.SetActive(true);
                Vector3 left_controller_pos = XRLeftControllerGameobj.transform.position;
                Vector3 left_controller_forward = XRLeftControllerGameobj.transform.forward;
                Vector3 eraser_pos_unity = EraserSphere.transform.position = left_controller_pos + left_controller_forward * armDistance;
               // Vector3 eraser_pos_gs = getEraserPosGS(eraser_pos_unity);
                GaussianRFRenderPlugin.hide_sphere_content(new float[3] { eraser_pos_unity.x, eraser_pos_unity.y, -eraser_pos_unity.z}, erase_radius);
            }
            else
            {
                EraserSphere.SetActive(false);
            }
 
        }
        else
        {
            GetLeftController();
        }

    }

    public Vector3 getEraserPosGS(Vector4 eraserPosUnity)
    {
        Matrix4x4 trs  = Matrix4x4.TRS(gsUnityObject.transform.position, gsUnityObject.transform.rotation, gsUnityObject.transform.localScale);
        Matrix4x4 itrs = Matrix4x4.Inverse(trs);
         
        return itrs*eraserPosUnity;
    }

    public void eraseSphereAtPoint(Vector3 pointPosUnity, float radius)
    {
        //Vector3 pos_ngp = getEraserPosNGP(pointPosUnity);
        //NerfRendererPlugin.mark_density_grid_empty(new float[3] { -pos_ngp.x, pos_ngp.y, pos_ngp.z }, radius);
    }

    public void revealSphereAtPoint(Vector3 pointPosUnity, float radius)
    {

    }

    // controllers

    void GetLeftController()
    {
        var leftHandedControllers = new List<InputDevice>();
        var desiredCharacteristics = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller;
        InputDevices.GetDevicesWithCharacteristics(desiredCharacteristics, leftHandedControllers);
        foreach (var device in leftHandedControllers)
        {
            XRLeftController = device;
        }
    }

    void GetRightController()
    {
        var rightHandedControllers = new List<InputDevice>();
        var desiredCharacteristics = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller;
        InputDevices.GetDevicesWithCharacteristics(desiredCharacteristics, rightHandedControllers);


        foreach (var device in rightHandedControllers)
        {
            Debug.Log(string.Format("Device name '{0}' has characteristics '{1}'", device.name, device.characteristics.ToString()));
            XRRightController = device;
        }

    }
}
