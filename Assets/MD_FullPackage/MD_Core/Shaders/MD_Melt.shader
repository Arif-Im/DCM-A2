Shader "Matej Vanco/Mesh Deformation Package/MD_Melt" 
{
	// Simple melt shader (Built-in RP)
	// Written by Matej Vanco 2016

	Properties 
	{
		[Space]
		[Header(_Use Melt modifier for advanced settings_)]
		[Space]
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB) Texture", 2D) = "white" {}
		_MainNormal ("Normal Texture", 2D) = "bump" {}
		_NormalAmount ("Normal Power", Range(0.01,2)) = 0.5
		_Specular ("Specular", Range(-1,1)) = 0.5
		_Emissive ("Emission Intensity", Range(0,5)) = 0

		[Header(Noise Settings)]
		[Space]
		_Amount("Noise Multiplier", Float) = 0.0
		_Speed("Noise Speed", Float) = 5
	    _Blend("Noise Blend", Range(0,0.1)) = 0.0
		[Space]
		[Header(Melt Settings)]
		[Space]
		_M_Trans("Melt Transition", Range(0.1,10)) = 2
	    _M_Zone("Melt Zone", Float) = 0
		_M_StartMelt("Melt Start", Range(0.05,5)) = 1
		_MeltAmount("Melt Amount", Range(0.1,10)) = 1
	    _M_Multiplier("Melt Amount Multiplier", Float) = 1
	}

	SubShader 
	{
		Tags { "RenderType" = "Opaque" "Queue" = "Geometry"}
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Standard vertex:vert addshadow fullforwardshadows
		#pragma target 3.0

		struct Input
		{
			float2 uv_MainTex;
			float2 uv_MainNormal;
		};

		sampler2D _MainTex;
		sampler2D _MainNormal;
		half4 _Color;
		float _Specular;
		float _NormalAmount;
		float _Emissive;

		half _Amount;
		half _Blend;
		half _Speed;

		float _M_Trans;
		float _M_Zone;
		float _M_StartMelt;
		float _MeltAmount, _M_Multiplier;

		// Conversion current local obj space vec4 to world vec4
		float4 ConvertWorldPos(float4 objectSpacePosition, float3 objectSpaceNormal)
		{
			float4 worldSpacePosition = mul(unity_ObjectToWorld, objectSpacePosition);
			float4 worldSpaceNormal = mul(unity_ObjectToWorld, float4(objectSpaceNormal, 0));
			float melt = (worldSpacePosition.y - _M_Zone) / _M_StartMelt;
			melt = 1 - saturate(melt);
			melt = pow(melt, _M_Trans) * _MeltAmount * _M_Multiplier;
			worldSpacePosition.xz += worldSpaceNormal.xz * melt;
			return mul(unity_WorldToObject, worldSpacePosition);
		}

		void vert(inout appdata_full v)
		{
			float4 vertPosition = ConvertWorldPos(v.vertex, v.normal);
			float4 bitangent = float4(cross(v.normal, v.tangent), 0);

			float vertOffset = 0.01;
			float4 v1 = ConvertWorldPos(v.vertex + v.tangent * vertOffset, v.normal);
			float4 v2 = ConvertWorldPos(v.vertex + bitangent * vertOffset, v.normal);
			float4 newTangent = v1 - vertPosition;
			float4 newBitangent = v2 - vertPosition;
			half4 vrts = v.vertex + sin(v.vertex * _Time.y * _Speed)* _Amount;

			v.vertex = lerp(vertPosition, vrts, _Blend);
		}

		// Regular surface shader
		void surf (Input IN, inout SurfaceOutputStandard o) 
		{
			half4 c = tex2D (_MainTex, IN.uv_MainTex);
			float3 n = UnpackNormal( tex2D(_MainNormal,IN.uv_MainNormal));
			n.z =  n.z / _NormalAmount;
			o.Albedo = c.rgb * _Color.rgb;
			o.Normal = normalize(n);
			o.Emission = c.rgb * _Emissive;
			o.Smoothness = _Specular;
			o.Alpha = c.a * _Color.a;
		}

		ENDCG
	}
	FallBack "Diffuse"
}