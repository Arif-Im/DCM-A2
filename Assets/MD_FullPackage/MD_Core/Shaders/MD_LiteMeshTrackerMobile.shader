Shader "Matej Vanco/Mesh Deformation Package/MD_LiteMeshTracker_Mobile" 
{	
	// Mesh Tracker Lite shader (Built-in RP) for Surface Tracking modifier (Mobile version, Tessellation not included)
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
	    _DispTex("Displacement Track", 2D) = "gray" {}
	}

	SubShader 
	{
		Tags { "RenderType"="Opaque" "Queue"="Geometry"}
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Standard addshadow fullforwardshadows vertex:vert
		#pragma target 3.0
		#include "MD_LiteMeshTrackerSrc.cginc"
		ENDCG
	}
	FallBack "Diffuse"
}