﻿
///======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============﻿
///
 /// Basic wireframe shader that can be used for rendering spatial mapping meshes.
 ///
	Shader "Custom/Spatial Mapping/ WireframeViedoOverlay"
{
	Properties
	{
		_WireColor("Wire color", Color) = (1.0, 1.0, 1.0, 1.0)
	}
		SubShader
	{
		Tags{ "RenderType" = "Opaque" }
		Lighting Off


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
		float2 uv : TEXCOORD0;
		float4 vertex : SV_POSITION;
		float dist : TEXCOORD1;
	};

	sampler2D _MainTex;
	float4 _MainTex_ST;

	float4 _WireColor;
	v2f vert(appdata v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv = TRANSFORM_TEX(v.uv, _MainTex);
		o.dist = length(ObjSpaceViewDir(v.vertex));
		return o;
	}

	float4 frag(v2f i) : SV_Target
	{
		float4 color = _WireColor;
		//#if AWAYS
		color.a = 10 / (0.1 + i.dist*i.dist);
		//#endif
		return color;
	}
		ENDCG
	}
	}
		Fallback off
}
