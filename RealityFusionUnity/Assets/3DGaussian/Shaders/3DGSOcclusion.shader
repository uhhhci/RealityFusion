Shader "Unlit/3DGSOcclusion"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass 
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            

            #include "UnityCG.cginc"


            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
           
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 screenPosReproj : TEXCOORD2;
                float2 uv : TEXCOORD0;
                float depth : TEXCOORD1;
            };

            sampler2D _MainTex;
        
            // stereo depth maps
            sampler2D _3DGSRightCamDepth;
            sampler2D _3DGSLeftCamDepth;

            v2f vert(appdata v)
            {

                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                o.depth = o.pos.z / o.pos.w;
                
                 o.screenPosReproj = ComputeScreenPos(v.vertex);
            //#if !defined(UNITY_REVERSED_Z) // basically only OpenGL
            //    o.depth = o.depth * 0.5 + 0.5; // remap -1 to 1 range to 0.0 to 1.0
            //#endif

                o.uv = v.uv;
                return o;
            }


            fixed4 frag(v2f i) : SV_Target
            {

                float4 color = tex2D(_MainTex, i.uv);

                // Get the eye index (0 for left, 1 for right)
                float eyeIndex = unity_StereoEyeIndex;
    
                if (eyeIndex == 0)
                {
        
                    //float GSLeftDepth = tex2D(_3DGSLeftCamDepth, i.uv);
                    float GSLeftDepth = tex2D(_3DGSLeftCamDepth, i.screenPosReproj.xy / i.screenPosReproj.w).r;
                    
                    if (GSLeftDepth < i.depth)
                    {
                        discard;
                    }
                }

                if (eyeIndex == 1)
                {
                 //float GSRightDepth = tex2D(_3DGSRightCamDepth, i.uv);
                float GSRightDepth = tex2D(_3DGSRightCamDepth, i.screenPosReproj.xy / i.screenPosReproj.w).r;
                    
                if (GSRightDepth < i.depth)
                {
                    discard;
                }

                }
 
               return half4(i.depth, i.depth, i.depth, 1);
            }

            ENDCG
        }
    }
}
