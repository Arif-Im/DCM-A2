Shader "Matej Vanco/Mesh Deformation Package/MD_StandardDeformer"
{
    // Simple Standard Deformer shader (Built-in RP)
    // Written by Matej Vanco 2016

    Properties
    { 
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
        _Color ("Main Color", Color) = (1,1,1,1)

        _MainTex ("Albedo (RGB) Texture", 2D) = "white" {}
        [Normal]_MainNormal("Normal Texture", 2D) = "bump" {}
        _Normal("Normal Power", Range(0.01,2)) = 0.5

        _Specular ("Specular", Range(0,1)) = 0.5

        _MainMetallic("Metallic Texture", 2D) = "white" {}
        _Metallic("Metallic Power", Range(0,1)) = 1

        _MainEmission("Emission Texture", 2D) = "white" {}
        [HDR]_Emission("Emission Color",Color) = (0,0,0)

        //--------------------------Deformer variables
        [KeywordEnum(NoAnimation, Fish, SoftFish, Jump, SoftJump)] _DEFAnim("Animation Mode", Float) = 0
        _DEFDirection("Deformer Direction",Vector) = (0,1,0,1)
        _DEFFrequency("Deformer Frequency",Float) = 0.5

        [Toggle]_DEFAbsolute("Absolute Value",Float) = 0
        [Toggle]_DEFFract("Frac Value",Float) = 0
        _DEFFractValue("Frac Value Frequency",Float) = 1.0
        //Edges
        _DEFEdges("Edges Multiplier", Float) = 1.0
        _DEFEdgesAmount("Additional Edges",Vector) = (0,0,0,0)
        //Extrusion
        _DEFExtrusion("Overall Extrusion", Float) = 0.0
        //Clipping
        [Toggle]_DEFClipping("Enable Clipping", Float) = 0.0
        [KeywordEnum(X,Y,Z)] _DEFClipType("Clip Type", Float) = 1
        _DEFClipTile("Clip Tilling",Float) = 1
        _DEFClipSize("Clip Size",Range(0.0,1.0)) = 0.5
        [Toggle]_DEFAnimateClip("Animate Clipping", Float) = 0.0
        _DEFClipAnimSpeed("Clipping Speed", Float) = 1.0
        //Noise
        [Toggle]_DEFNoise("Enable Noise", Float) = 0.0
        _DEFNoiseDirection("Noise Direction",Vector) = (0.01,0.01,0.01,0)
        _DEFNoiseSpeed("Noise Speed", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        Cull[_Cull]

        CGPROGRAM

        #pragma surface surf Standard vertex:vert addshadow fullforwardshadows
        #pragma target 3.0

        #include "MD_ShaderSource.cginc"	

        ENDCG
    }
    CustomEditor "MD_Package_Editor.MD_StandardDeformer_Editor"
    FallBack "Diffuse"
}
