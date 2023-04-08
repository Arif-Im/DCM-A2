Shader "Matej Vanco/Mesh Deformation Package/MD_LiteMeshTracker" 
{
	// Mesh Tracker Lite shader (Built-in RP) for Surface Tracking modifier
	// Written by Matej Vanco 2017

	Properties
	{
		[Space]
		[Header(_Use SurfaceTracking modifier for advanced settings_)]
		[Space]
		_ColorUp("Upper Color", Color) = (1,1,1,1)
		_ColorDown("Lower Color", Color) = (1,1,1,1)
		[Space]
		_MainTex("Albedo (RGB) Texture", 2D) = "white" {}
		_MainNormal("Normal Texture", 2D) = "bump" {}
		_NormalAmount("Normal Power", Range(0.01,2)) = 0.5
		_Specular("Specular", Range(-1,1)) = 0.5
		_Emissive("Emission Intensity", Range(0,5)) = 0
		[Header(Track Settings)]
		[Space]
		_TrackFactor("Track Depth", float) = 0.1
		[Header(Tessalation Settings)]
		[Space]
		_Tess("Tessellation", Range(1,32)) = 4
		_DispTex("Displacement Track", 2D) = "gray" {}
		[Space]
		_MinDistance("Min Distance", float) = 20
		_MaxDistance("Max Distance", float) = 50
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" "Queue" = "Geometry"}
		LOD 200

		CGPROGRAM
		#pragma surface surf Standard addshadow fullforwardshadows vertex:vert tessellate:tessDistance
		#pragma target 5.0
		#include "Tessellation.cginc"
		#include "MD_LiteMeshTrackerSrc.cginc"
		float _Tess;
		float4 tessDistance(appdata v0, appdata v1, appdata v2)
		{
			float minDist = _MinDistance;
			float maxDist = _MaxDistance;
			return UnityDistanceBasedTess(v0.vertex, v1.vertex, v2.vertex, minDist, maxDist, _Tess);
		}
		ENDCG
	}
	FallBack "Diffuse"
}