using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

using MD_Package;
using MD_Package.Geometry;
#endif

namespace MD_Package.Geometry
{
    /// <summary>
    /// MDG(Mesh Deformation Geometry): Cone.
    /// Simple cone primitive generated at runtime in Unity Engine.
    /// Inherits from MD_GeometryBase.
    /// Written by Matej Vanco (2015, updated in 2023).
    /// </summary>
    [ExecuteInEditMode]
    [AddComponentMenu(MD_Debug.ORGANISATION + MD_Debug.PACKAGENAME + "Geometry/Cone")]
    public sealed class MDG_Cone : MD_GeometryBase
    {
        public float coneHeight = 2f;
        public float coneBotRadius = 0.5f;
        public float coneTopRadius = 0.25f;

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

            Vector3 offset = geometryCenterMesh ? new Vector3(0, coneHeight / 2.0f, 0) : Vector3.zero;
            offset -= geometryOffset;

            int nbVerticesCap = geometryResolution + 1;
            geometryVertices = new MDVertices(nbVerticesCap + nbVerticesCap + geometryResolution * 2 + 2);
            int nbTriangles = geometryResolution + geometryResolution + geometryResolution * 2;
            geometryTriangles = new MDTriangles(nbTriangles * 3 + 3);
            geometryUVs = new MDUvs(geometryVertices.vertices.Length);

            int vert = 0;
            float _2pi = Mathf.PI * 2f;

            geometryVertices.SetVertice(vert++, new Vector3(0f, 0f, 0f) - offset);
            while (vert <= geometryResolution)
            {
                float rad = (float)vert / geometryResolution * _2pi;
                geometryVertices.SetVertice(vert, new Vector3(Mathf.Cos(rad) * coneBotRadius, 0f, Mathf.Sin(rad) * coneBotRadius) - offset);
                vert++;
            }

            geometryVertices.SetVertice(vert++, new Vector3(0f, coneHeight, 0f) - offset);
            while (vert <= geometryResolution * 2 + 1)
            {
                float rad = (float)(vert - geometryResolution - 1) / geometryResolution * _2pi;
                geometryVertices.SetVertice(vert, new Vector3(Mathf.Cos(rad) * coneTopRadius, coneHeight, Mathf.Sin(rad) * coneTopRadius) - offset);
                vert++;
            }

            int v = 0;
            while (vert <= geometryVertices.vertices.Length - 4)
            {
                float rad = (float)v / geometryResolution * _2pi;
                geometryVertices.SetVertice(vert, new Vector3(Mathf.Cos(rad) * coneTopRadius, coneHeight, Mathf.Sin(rad) * coneTopRadius) - offset);
                geometryVertices.SetVertice(vert + 1, new Vector3(Mathf.Cos(rad) * coneBotRadius, 0, Mathf.Sin(rad) * coneBotRadius) - offset);
                vert += 2;
                v++;
            }
            geometryVertices.SetVertice(vert, geometryVertices.vertices[geometryResolution * 2 + 2]);
            geometryVertices.SetVertice(vert + 1, geometryVertices.vertices[geometryResolution * 2 + 3]);

            // Bottom cap
            int u = 0;
            geometryUVs.SetUV(u++, new Vector2(0.5f, 0.5f));
            while (u <= geometryResolution)
            {
                float rad = (float)u / geometryResolution * _2pi;
                geometryUVs.SetUV(u, new Vector2(Mathf.Cos(rad) * .5f + .5f, Mathf.Sin(rad) * .5f + .5f));
                u++;
            }

            // Top cap
            geometryUVs.SetUV(u++, new Vector2(0.5f, 0.5f));
            while (u <= geometryResolution * 2 + 1)
            {
                float rad = (float)u / geometryResolution * _2pi;
                geometryUVs.SetUV(u, new Vector2(Mathf.Cos(rad) * .5f + .5f, Mathf.Sin(rad) * .5f + .5f));
                u++;
            }

            // Sides
            int u_sides = 0;
            while (u <= geometryUVs.uvs.Length - 4)
            {
                float t = (float)u_sides / geometryResolution;
                geometryUVs.SetUV(u, new Vector3(t, 1f));
                geometryUVs.SetUV(u + 1, new Vector3(t, 0f));
                u += 2;
                u_sides++;
            }
            geometryUVs.SetUV(u, new Vector2(1f, 1f));
            geometryUVs.SetUV(u + 1, new Vector2(1f, 0f));

            // Bottom cap
            int tri = 0;
            int i = 0;
            while (tri < geometryResolution - 1)
            {
                geometryTriangles.SetTriangle(i, i + 1, i + 2, 0, tri + 1, tri + 2);
                tri++;
                i += 3;
            }
            geometryTriangles.SetTriangle(i, i + 1, i + 2, 0, tri + 1, 1);
            tri++;
            i += 3;

            // Top cap
            //tri++;
            while (tri < geometryResolution * 2)
            {
                geometryTriangles.SetTriangle(i, i + 1, i + 2, tri+2, tri + 1, nbVerticesCap);
                tri++;
                i += 3;
            }
            geometryTriangles.SetTriangle(i, i + 1, i + 2, nbVerticesCap + 1, tri + 1, nbVerticesCap);
            tri++;
            i += 3;
            tri++;

            // Sides
            while (tri <= nbTriangles)
            {
                geometryTriangles.SetTriangle(i, i + 1, i + 2, tri + 2, tri + 1, tri);
                tri++;
                i += 3;
                geometryTriangles.SetTriangle(i, i + 1, i + 2, tri + 1, tri + 2, tri);
                tri++;
                i += 3;
            }
        }

        public void MDGCone_ChangeHeight(float height)
        {
            coneHeight = height;
            if (!MbUpdateEveryFrame)
                MDMeshBase_ProcessCompleteMeshUpdate();
        }

        public void MDGCone_ChangeTopRadius(float topRadius)
        {
            coneTopRadius = topRadius;
            if (!MbUpdateEveryFrame)
                MDMeshBase_ProcessCompleteMeshUpdate();
        }

        public void MDGCone_ChangeBotRadius(float botRadius)
        {
            coneBotRadius = botRadius;
            if (!MbUpdateEveryFrame)
                MDMeshBase_ProcessCompleteMeshUpdate();
        }

#if UNITY_EDITOR
        [MenuItem("GameObject/3D Object" + MD_Debug.PACKAGENAME + "Primitives/Cone", priority = 33)]
#endif
        public static GameObject CreateGeometry_Cone()
        {
            GameObject newGm = new GameObject("Cone");
            var c = CreateGeometry<MDG_Cone>(newGm);
            c.geometryResolution = 8;
            c.MDMeshBase_ProcessCompleteMeshUpdate();
            return PrepareGeometryInstance(newGm);
        }

#if UNITY_EDITOR
        [MenuItem("GameObject/3D Object" + MD_Debug.PACKAGENAME + "Primitives/Cylinder", priority = 4)]
#endif
        public static GameObject CreateGeometry_Cylinder()
        {
            GameObject newGm = new GameObject("Cylinder");
            var c = CreateGeometry<MDG_Cone>(newGm);
            c.coneHeight = 2;
            c.coneTopRadius = c.coneBotRadius = 0.5f;
            c.geometryResolution = 24;
            c.MDMeshBase_ProcessCompleteMeshUpdate();
            return PrepareGeometryInstance(newGm);
        }
    }
}

#if UNITY_EDITOR
namespace MD_Package_Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MDG_Cone))]
    public sealed class MDG_Cone_Editor : MD_GeometryBase_Editor
    {
        private MDG_Cone mb;

        public override void OnEnable()
        {
            mMeshBase = (MD_MeshBase)target;
            mGeoBase = (MD_GeometryBase)target;
            mb = (MDG_Cone)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            MDE_s(5);
            MDE_l("Cone Settings", true);
            MDE_v();
            MDE_DrawProperty("coneHeight", "Height");
            MDE_DrawProperty("coneTopRadius", "Top Radius");
            MDE_DrawProperty("coneBotRadius", "Bottom Radius");
            MDE_ve();
            MDE_AddMeshColliderRefresher(mb.gameObject);
            MDE_BackToMeshEditor(mb);
        }
    }
}
#endif