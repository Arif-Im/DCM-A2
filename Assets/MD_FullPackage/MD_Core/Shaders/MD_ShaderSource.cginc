struct Input
{
    float2 uv_MainTex;
    float2 uv_MainNormal;
    float2 uv_MainMetallic;
    float2 uv_MainEmission;
    float3 worldPos;
};

sampler2D _MainTex;
sampler2D _MainNormal;
sampler2D _MainMetallic;
sampler2D _MainEmission;

float4 _Color;
float _Specular;
float _Metallic;
float _Normal;
float4 _Emission;

// Essential deformer variables
half _DEFAnim;
half4 _DEFDirection;
float _DEFFrequency;
float _DEFEdges;
half4 _DEFEdgesAmount;

float _DEFFract, _DEFFractValue;
float _DEFAbsolute;

float _DEFExtrusion;

float _DEFClipping;
float _DEFClipType;
float _DEFClipTile;
float _DEFClipSize;
float _DEFAnimateClip;
float _DEFClipAnimSpeed;

float _DEFNoise;
float4 _DEFNoiseDirection;
float _DEFNoiseSpeed;

void vert(inout appdata_full v)
{
    float3 input = 0;
    switch (_DEFAnim)
    {
        case 0: input = v.vertex.xyz; break;
        case 1: input = v.vertex.x; break;
        case 2: input = v.normal.x; break;
        case 3: input = v.vertex.y; break;
        case 4: input = v.normal.y; break;
    }
    input = _DEFAnim != 0 ? sin(input + _Time.y * _DEFFrequency) * _DEFDirection : ((input * input) * (_DEFFrequency * _DEFDirection));
    input = (_DEFFract == 0 ? input : input * frac(input * (_DEFFractValue + _DEFFrequency)));
    input = (_DEFAbsolute == 0 ? input : abs(input));
    v.vertex.xyz += input + v.normal * _DEFExtrusion + (_DEFNoise == 1 ? _DEFNoiseDirection * sin(v.vertex.xyz * (_Time.y * _DEFNoiseSpeed)) : 0);
    v.vertex.x += (v.vertex.xyz * v.vertex.xyz * _DEFEdgesAmount.x) * _DEFEdges;
    v.vertex.y += (v.vertex.xyz * v.vertex.xyz * _DEFEdgesAmount.y) * _DEFEdges;
    v.vertex.z += (v.vertex.xyz * v.vertex.xyz * _DEFEdgesAmount.z) * _DEFEdges;
}

void SetMetallic(Input IN, inout SurfaceOutputStandard o)
{
    fixed4 metallic = tex2D(_MainMetallic, IN.uv_MainMetallic);
    o.Metallic = metallic.r * _Metallic;
    o.Smoothness = metallic.a * _Specular;
}

void surf(Input IN, inout SurfaceOutputStandard o)
{
    half4 c = tex2D(_MainTex, IN.uv_MainTex);
    fixed3 n = UnpackNormal(tex2D(_MainNormal, IN.uv_MainNormal));
    n.z = n.z / _Normal;
    o.Albedo = c.rgb * _Color.rgb;
    o.Normal = normalize(n);
    o.Emission = c.rgb * tex2D(_MainEmission, IN.uv_MainEmission) * _Emission;
    SetMetallic(IN, o);

    if (_DEFClipping == 1) clip(frac(((_DEFClipType == 0 ? IN.worldPos.x : _DEFClipType == 1 ? IN.worldPos.y : IN.worldPos.z) + (_DEFAnimateClip == 1 ? _Time.y * _DEFClipAnimSpeed : 0)) * _DEFClipTile) - _DEFClipSize);
}