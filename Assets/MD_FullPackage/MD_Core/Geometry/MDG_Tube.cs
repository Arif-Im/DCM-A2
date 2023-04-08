using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

using MD_Package;
using MD_Package.Geometry;
#endif

namespace MD_Package.Geometry
{
    /// <summary>
    /// MDG(Mesh Deformation Geometry): Tube.
    /// Simple tube primitive generated at runtime in Unity Engine.
    /// Inherits from MD_GeometryBase.
    /// Written by Matej Vanco (2017, updated in 2023).
    /// </summary>
    [ExecuteInEditMode]
    [AddComponentMenu(MD_Debug.ORGANISATION + MD_Debug.PACKAGENAME + "Geometry/Tube")]
    public sealed class MDG_Tube : MD_GeometryBase
    {
        public float tubeInnerRadius = 0.5f;
        public float tubeOuterRadius = 1.0f;
        public float tubeHeight = 1.0f;

        private void Reset()
        {
            MDMeshBase_InitializeBase();
        }

        private void Update()
        {
            if (!MbUpdateEveryFrame)
                return;

            MDMeshBase_ProcessCompleteMeshUpdate();
        }

        public override void MDMeshBase_ProcessCalculations()
        {
            geometryResolution = Mathf.Max(3, geometryResolution);
            geometryWorkingMesh = new Mesh();

            Vector3 offset = geometryCenterMesh ? new Vector3(0, tubeHeight / 2.0f, 0) : Vector3.zero;
            offset -= geometryOffset;

            geometryVertices = new MDVertices((geometryResolution + 1) * 8);
            geometryTriangles = new MDTriangles((geometryResolution + 1) * 24);
            geometryUVs = new MDUvs(geometryVertices.vertices.Length);

            if (geometryResolution < 3) geometryResolution = 3;
            if (tubeInnerRadius > tubeOuterRadius) tubeInnerRadius = tubeOuterRadius;

            int v = 0;
            float deltaTheta = Mathf.PI * 2.0f / geometryResolution;
            float radiusDiff = tubeInnerRadius - tubeOuterRadius;
            float total = 2f * (tubeHeight + radiusDiff);
            float v1 = tubeHeight / total;
            float v2 = v1 + radiusDiff / total;
            float v3 = v2 + v1;

            for (int i = 0; i <= geometryResolution; ++i)
            {
                float theta = i * deltaTheta;

                float x = Mathf.Cos(theta);
                float y = Mathf.Sin(theta);

                Vector3 position = new Vector3(tubeOuterRadius * x, 0f, tubeOuterRadius * y) - offset;
                Vector2 uv = new Vector2((float)i / geometryResolution, 0f);

                geometryVertices.SetVertice(v, position);
                geometryVertices.SetVertice(v + 1, position);
                geometryUVs.SetUV(v, new Vector2(uv.x, 1f));
                geometryUVs.SetUV(v + 1, uv);
                v += 2;

                position.y = tubeHeight - offset.y;
                uv.y = v1;

                geometryVertices.SetVertice(v, position);
                geometryVertices.SetVertice(v + 1, position);
                geometryUVs.SetUV(v, uv); 
                geometryUVs.SetUV(v + 1, uv);
                v += 2;

                position = new Vector3(tubeInnerRadius * x, tubeHeight, tubeInnerRadius * y) - offset;
                uv.y = v2;

                geometryVertices.SetVertice(v, position);
                geometryVertices.SetVertice(v + 1, position);
                geometryUVs.SetUV(v, uv);
                geometryUVs.SetUV(v + 1, uv);
                v += 2;

                position.y = 0f - offset.y;
                uv.y = v3;

                geometryVertices.SetVertice(v, position);
                geometryVertices.SetVertice(v + 1, position);
                geometryUVs.SetUV(v, uv);
                geometryUVs.SetUV(v + 1, uv);
                v += 2;
            }

            int i0, i1, i2, i3;
            int t = 0;
            for (int i = 0; i < geometryResolution; ++i)
            {
                // Front
                i0 = i * 8 + 1;
                i1 = i0 + 1;
                i2 = (i + 1) * 8 + 1;
                i3 = i2 + 1;

                geometryTriangles.SetTriangle(t++, t++, t++, i0, i1, i3);
                geometryTriangles.SetTriangle(t++, t++, t++, i0, i3, i2);

                // Top
                i0 = i1 + 1;
                i1 = i0 + 1;
                i2 = i3 + 1;
                i3 = i2 + 1;

                geometryTriangles.SetTriangle(t++, t++, t++, i0, i1, i3);
                geometryTriangles.SetTriangle(t++, t++, t++, i0, i3, i2);

                // Back
                i0 = i1 + 1;
                i1 = i0 + 1;
                i2 = i3 + 1;
                i3 = i2 + 1;

                geometryTriangles.SetTriangle(t++, t++, t++, i0, i1, i3);
                geometryTriangles.SetTriangle(t++, t++, t++, i0, i3, i2);

                // Bottom
                i0 = i1 + 1;
                i1 = i * 8;
                i2 = i3 + 1;
                i3 = (i + 1) * 8;

                geometryTriangles.SetTriangle(t++, t++, t++, i0, i1, i3);
                geometryTriangles.SetTriangle(t++, t++, t++, i0, i3, i2);
            }
        }

        public void MDGTube_ChangeInnerRadius(float radius)
        {
            tubeInnerRadius = radius;
            if (!MbUpdateEveryFrame)
                MDMeshBase_ProcessCompleteMeshUpdate();
        }

        public void MDGTube_ChangeOuterRadius(float radius)
        {
            tubeOuterRadius = radius;
            if (!MbUpdateEveryFrame)
                MDMeshBase_ProcessCompleteMeshUpdate();
        }

        public void MDGTube_ChangeHeight(float height)
        {
            tubeHeight = height;
            if (!MbUpdateEveryFrame)
                MDMeshBase_ProcessCompleteMeshUpdate();
        }

#if UNITY_EDITOR
        [MenuItem("GameObject/3D Object" + MD_Debug.PACKAGENAME + "Primitives/Tube", priority = 35)]
#endif
        public static GameObject CreateGeometry()
        {
            GameObject newGm = new GameObject("Tube");
            CreateGeometry<MDG_Tube>(newGm);
            return PrepareGeometryInstance(newGm);
        }
    }
}

#if UNITY_EDITOR
namespace MD_Package_Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MDG_Tube))]
    public sealed class MDG_Tube_Editor : MD_GeometryBase_Editor
    {
        private MDG_Tube mb;

        public override void OnEnable()
        {
            mMeshBase = (MD_MeshBase)target;
            mGeoBase = (MD_GeometryBase)target;
            mb = (MDG_Tube)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            MDE_s(5);
            MDE_l("Tube Settings", true);
            MDE_v();
            MDE_DrawProperty("tubeHeight", "Height");
            MDE_DrawProperty("tubeInnerRadius", "Inner Radius");
            MDE_DrawProperty("tubeOuterRadius", "Outer Radius");
            MDE_ve();
            MDE_AddMeshColliderRefresher(mb.gameObject);
            MDE_BackToMeshEditor(mb);
        }
    }
}
#endif