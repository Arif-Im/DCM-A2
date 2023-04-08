Shader "Matej Vanco/Mesh Deformation Package/MD_ExampleWireframe"
{
    // Example wire-frame shader. Not official a part of the MD_Package

    Properties
    {
        _Thick("Thickness", Range(0, 1)) = 0.5
        _Color("Wireframe Color", Color) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        LOD 100

        Pass
        {
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM

            #pragma vertex vert
            #pragma geometry geo
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct geoToFrag
            {
                float4 vertex : SV_POSITION;
                float4 worldPosition : TEXCOORD0;
                float3 dist : TEXCOORD1;
            };

            struct vertToGeo
            {
                float4 vertex : SV_POSITION;
                float4 worldPosition : TEXCOORD0;
            };

            half _Thick;
            half4 _Color;

            vertToGeo vert(appdata v)
            {
                vertToGeo o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPosition = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            [maxvertexcount(3)]
            void geo(triangle vertToGeo i[3], inout TriangleStream<geoToFrag> stream)
            {
                float2 p0 = i[0].vertex.xy / i[0].vertex.w;
                float2 p1 = i[1].vertex.xy / i[1].vertex.w;
                float2 p2 = i[2].vertex.xy / i[2].vertex.w;

                float2 edge0 = p2 - p1;
                float2 edge1 = p2 - p0;
                float2 edge2 = p1 - p0;

                float area = abs(edge1.x * edge2.y - edge1.y * edge2.x);
                float thickness = lerp(0, 100, _Thick);

                geoToFrag o;
                float v;

                o.worldPosition = i[0].worldPosition;
                o.vertex = i[0].vertex;
                v = area / length(edge0);

                o.dist.xyz = float3(v, 0, 0) * o.vertex.w * thickness;
                stream.Append(o);
                o.worldPosition = i[1].worldPosition;
                o.vertex = i[1].vertex;
                v = area / length(edge1);
                o.dist.xyz = float3(0, v, 0) * o.vertex.w * thickness;
                stream.Append(o);
                o.worldPosition = i[2].worldPosition;
                o.vertex = i[2].vertex;
                v = area / length(edge2);
                o.dist.xyz = float3(0, 0, v) * o.vertex.w * thickness;

                stream.Append(o);
            }

            float4 frag(geoToFrag i) : SV_Target
            {
                float minDistanceToEdge = min(i.dist.x, min(i.dist.y, i.dist.z));
                fixed4 col = _Color;
                col.a *= 1.0 - minDistanceToEdge;
                return col;
            }
            ENDCG
        }
    }
}