// Source of Mesh Tracker Lite version - included in default and mobile versions
// Written by Matej Vanco 2017

sampler2D _MainTex;
sampler2D _MainNormal;
half4 _ColorUp;
half4 _ColorDown;
float _Specular;
float _NormalAmount;
float _Emissive;
float4 _LocalPos;
float _TrackFactor;
float _MinDistance;
float _MaxDistance;

// In/out data initialization
struct appdata
{
	float4 vertex : POSITION;
	float4 tangent : TANGENT;
	float3 normal : NORMAL;
	float2 texcoord : TEXCOORD0;
	float2 texcoord1 : TEXCOORD1;
	float2 texcoord2 : TEXCOORD2;
};

struct v2f
{
	float4 vertex : POSITION;
	float2 texcoord : TEXCOORD0;
};

// Displacement feature
sampler2D _DispTex;
float _Displacement;

void vert(inout appdata v)
{
	float d = tex2Dlod(_DispTex, float4(v.texcoord.xy, 0, 0)).r * _TrackFactor;
	v.vertex.y += d;
}

// Regular surface shader for Built-in RP
struct Input
{
	float2 uv_MainTex;
	float2 uv_DispTex;
};

void surf(Input IN, inout SurfaceOutputStandard o)
{
	float val = tex2Dlod(_DispTex, float4(IN.uv_DispTex, 0, 0)).r;

	float3 c = lerp(tex2D(_MainTex, IN.uv_MainTex) * _ColorUp.rgb, tex2D(_MainTex, IN.uv_MainTex) * _ColorDown.rgb, val);
	fixed3 n = UnpackNormal(tex2D(_MainNormal, IN.uv_MainTex));
	n.z = n.z / _NormalAmount;
	o.Albedo = c.rgb;
	o.Normal = normalize(n);
	o.Emission = c.rgb * _Emissive;
	o.Smoothness = _Specular;
}