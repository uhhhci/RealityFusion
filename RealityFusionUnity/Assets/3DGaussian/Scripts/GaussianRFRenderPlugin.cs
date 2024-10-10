using UnityEngine;
using System.Runtime.InteropServices;
using AOT;
using System.IO;
using UnityScript.Steps;

public class GaussianRFRenderPlugin 
{
    [DllImport("unity_gaussian", EntryPoint = "unity_gaussian_test_func")]
    public static extern void unity_gaussian_test_func();

    [DllImport("unity_gaussian", EntryPoint = "unity_gaussian_set_camera_fov")]
    public static extern void set_camera_fov(float fov);

    [DllImport("unity_gaussian", EntryPoint = "unity_gaussian_set_camera_aspect")]
    public static extern void set_camera_aspect(float aspect);

    [DllImport("unity_gaussian", EntryPoint = "unity_gaussian_set_scaling_modifier")]
    public static extern void set_scaling_modifier(float fov);

    [DllImport("unity_gaussian", EntryPoint = "unity_gaussian_get_initialization_state")]
    public static extern bool get_initialization_state();

    [DllImport("unity_gaussian", EntryPoint = "unity_gaussian_transform_left_camera")]
    public static extern void transform_left_camera(float[] pos);

    [DllImport("unity_gaussian", EntryPoint = "unity_gaussian_transform_right_camera")]
    public static extern void transform_right_camera(float[] pos);

    [DllImport("unity_gaussian", EntryPoint = "unity_gaussian_update_render_resolution")]
    public static extern void update_render_resolution(int width, int height);


    [DllImport("unity_gaussian", EntryPoint = "unity_gaussian_update_model_matrix")]
    public static extern void update_model_matrix(float[] model);
    
    [DllImport("unity_gaussian", EntryPoint = "unity_gaussian_update_leftview_matrix")]
    public static extern void update_leftview_matrix(float[] view);

    [DllImport("unity_gaussian", EntryPoint = "unity_gaussian_update_rightview_matrix")]
    public static extern void update_rightview_matrix(float[] view);

    [DllImport("unity_gaussian", EntryPoint = "unity_gaussian_update_proj_matrix_left")]
    public static extern void update_proj_matrix_left(float[] proj);

    [DllImport("unity_gaussian", EntryPoint = "unity_gaussian_update_proj_matrix_right")]
    public static extern void update_proj_matrix_right(float[] proj);

    [DllImport("unity_gaussian", EntryPoint = "unity_gaussian_get_left_handle")]
    public static extern System.IntPtr get_left_handle();

    [DllImport("unity_gaussian", EntryPoint = "unity_gaussian_get_right_handle")]
    public static extern System.IntPtr get_right_handle();

    [DllImport("unity_gaussian", EntryPoint = "unity_gaussian_get_left_depth_handle")]
    public static extern System.IntPtr get_left_depth_handle();

    [DllImport("unity_gaussian", EntryPoint = "unity_gaussian_get_right_depth_handle")]
    public static extern System.IntPtr get_right_depth_handle();

    [DllImport("unity_gaussian", EntryPoint = "unity_gaussian_set_init_values", CharSet = CharSet.Ansi)]
    public static extern void unity_gaussian_set_init_values(string scene, bool use_depth, bool use_cpu);

    [DllImport("unity_gaussian", EntryPoint = "GetRenderEventFunc")]
    public static extern System.IntPtr GetRenderEventFunc();

    [DllImport("unity_gaussian", EntryPoint = "UnityPluginLoad")]
    public static extern void UnityPluginLoad();

    // interaction 

    [DllImport("unity_gaussian", EntryPoint = "unity_gaussian_hide_sphere_content")]
    public static extern void hide_sphere_content(float[] center, float radius);

    // point processing 

    [DllImport("unity_gaussian", EntryPoint = "unity_point_update_ZED_status")]
    public static extern void update_ZED_status(bool isReady, int camHeight, int camWidth, System.IntPtr zedTexID);

    [DllImport("unity_gaussian", EntryPoint = "unity_point_register_ZED_with_GS")]
    public static extern void register_ZED_with_GS();
}
