Shader "Custom/ZEDEgocentricPlane"
{
	Properties
	{

		_MainTex("Texture", 2D) = "white" {}
		_ZEDRGBATex("ZEDRGBATex", 2D) = "white" {}

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


				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.color = v.color;
				return o;
			}

			//sampler2D _MainTex;
			sampler2D _ZEDRGBATex;
			half4 _MainTex_ST;

			float _MidFoveateRadius;
			float _CentralRadius;
			float4 _CentralCoord;


			fixed4 frag(v2f i) : SV_Target
			{

				fixed4 color = tex2D(_ZEDRGBATex, i.uv);
				
				// swap agbr => rgba BGRA => RGBA
				fixed4  res = fixed4(color.b, color.g, color.r, color.a);

				if (distance(i.uv, _CentralCoord) < _CentralRadius) {

					res.a = 1;
				}
				else {

					res.a = lerp(1, 0, abs(distance(i.uv, _CentralCoord) - _CentralRadius) * _MidFoveateRadius);

				}

				return res;

			}
		ENDCG
		}
	}
}
