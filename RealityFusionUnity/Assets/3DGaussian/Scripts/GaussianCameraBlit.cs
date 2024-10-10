using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class GaussianCameraBlit : MonoBehaviour
{
    public Material mat; 
    Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }
    // [ImageEffectAfterScale]
    [ImageEffectOpaque]
   
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {


        if (mat == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        Graphics.Blit(source, destination, mat);

    }
}