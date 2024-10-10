Shader "Custom/3DGSBasic"
{
	Properties
	{

		_MainTex("Texture", 2D) = "white" {}
		_GaussianSplattingTex("GSText", 2D) = "white" {}
		_GaussianSplattingDepthTex("GSTextDepth", 2D) = "white" {}

		// visibility parameter controls the mid-foveate region
		_MidFoveateRadius("MidFoveateRadius", Range(0.0,10.0)) = 10

		// central radius conrtols the radius of the high resolution foveate region
		_CentralRadius("CentralRadius", Range(0.0,0.5)) = 0.4

		_CentralCoord("CentralCoordinate", vector) = (0.5,0.5,0,0)


	}
	SubShader
	{

		Tags { "Queue" = "AlphaTest"
			   "RenderType" = "Transparent"}
		Cull Off ZWrite Off ZTest Off


		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma target 2.0
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float4 color    : COLOR;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID

			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				fixed4 color : COLOR;
				float4 vertex : SV_POSITION;

				UNITY_VERTEX_OUTPUT_STEREO

			};

			v2f vert(appdata v)
			{
				v2f o;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.color = v.color;
				return o;
			}

			//sampler2D _MainTex;
			sampler2D _GaussianSplattingTex;
			sampler2D _GaussianSplattingDepthTex;

			UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
			UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);
			half4 _MainTex_ST;

			float _MidFoveateRadius;
			float _CentralRadius;
			float4 _CentralCoord;


			fixed4 frag(v2f i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);


				float gsDepth   = SAMPLE_DEPTH_TEXTURE(_GaussianSplattingDepthTex, i.uv);
				float mainDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);

//#if defined(UNITY_REVERSED_Z)
//				mainDepth = mainDepth * 0.5 + 0.5;
//#endif

				return tex2D(_GaussianSplattingTex, i.uv);

				//if (gsDepth < mainDepth) {
				//	return tex2D(_GaussianSplattingTex, i.uv);

				//}
				//else {
				//	return UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, UnityStereoScreenSpaceUVAdjust(i.uv, _MainTex_ST));

				//}

				
				/*
				if (depth >= 0.9) {
					return res;
				}
				else {
					return UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, UnityStereoScreenSpaceUVAdjust(i.uv, _MainTex_ST));
				}*/
				//if (distance(i.uv, _CentralCoord) < _CentralRadius) {

				//	res.a = 1;
				//}

				//else {

				//	res.a = lerp(1, 0, abs(distance(i.uv, _CentralCoord) - _CentralRadius) * _MidFoveateRadius);

				//	if (res.a < 0.001) {
				//		discard;
				//	}

				//}

				//if (res.r == 0 && res.b == 0 && res.g == 0) {

				//	res.a = 0;
				//}
			}
		ENDCG
		}
	}
}
