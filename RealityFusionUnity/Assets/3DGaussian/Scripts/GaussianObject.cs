using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GaussianObject : MonoBehaviour
{
    // create object space for gaussian
    public GaussianRFRenderer gsRenderer;

    Matrix4x4 _mTRS = Matrix4x4.identity;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (gsRenderer.getIntializationState())
        {
            _mTRS = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
            GaussianRFRenderPlugin.update_model_matrix(Matrix4fToArray(_mTRS));
        }
    }

    private float[] Matrix4fToArray(Matrix4x4 m)
    {
        // since eigen is column major, this data sequence can be directly loaded to create an eigen matrix
        float[] arr = new float[4 * 4]
        {
                m.m00, m.m01, m.m02, m.m03,
                m.m10, m.m11, m.m12, m.m13,
                m.m20, m.m21, m.m22, m.m23,
                m.m30, m.m31, m.m32, m.m33
        };

        return arr;
    }
}
