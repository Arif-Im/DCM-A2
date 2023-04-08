using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

using MD_Package;
using MD_Package.Geometry;
#endif

namespace MD_Package.Geometry
{
    /// <summary>
    /// MDG(Mesh Deformation Geometry): Procedural Extended Plane.
    /// Extended procedural plane primitive generated at runtime in Unity Engine.
    /// Inherits from MD_GeometryBase.
    /// Written by Matej Vanco (2016, updated in 2023).
    /// </summary>
    [ExecuteInEditMode]
    [AddComponentMenu(MD_Debug.ORGANISATION + MD_Debug.PACKAGENAME + "Geometry/Extended Plane")]
    public sealed class MDG_ProceduralExtendedPlane : MD_GeometryBase
    {
        public float planeSize = 1.0f;
        public bool planeExpand = false;
        public bool planeUseAngularFeature = false;
        [Range(-1.0f, 1.0f)] public float planeAngleValue = 0;
        [Range(1.0f, 60.0f)] public float planeAngleDensity = 1;
        [Range(-1.0f, 1.0f)] public float planeAngleThreshold = 1.0f;

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
            geometryResolution = Mathf.Min(120, geometryResolution);

            geometryWorkingMesh = new Mesh();

            geometryVertices = new MDVertices(geometryResolution * geometryResolution * 4);
            geometryTriangles = new MDTriangles(geometryResolution * geometryResolution * 6);
            geometryUVs = new MDUvs(geometryVertices.vertices.Length);

            float step = planeSize / geometryResolution;
            float chunkSize = !planeExpand ? step : planeSize;
            float halfSize = planeSize / 2.0f;
            float centralOffset = geometryCenterMesh ? planeExpand ? halfSize * geometryResolution : halfSize : 0;

            int v = 0;
            int t = 0;
            float a = 0;

            for (int x = 0; x < geometryResolution; x++)
            {
                int vOffset = 0;
                if (planeUseAngularFeature && x > 0)
                    vOffset = (4 * geometryResolution) - 2;

                float currX = !planeExpand ? step * x : x * planeSize;

                for (int z = 0; z < geometryResolution; z++)
                {
                    float currZ = !planeExpand ? step * z : z * planeSize;
                    Vector3 cellOffset = new Vector3(currX - centralOffset, geometryOffset.y, currZ - centralOffset);

                    float vo = vOffset == 0 ? 0 : geometryVertices.vertices[v - vOffset].y;
                    geometryVertices.vertices[v] = new Vector3(0, vo, 0) + cellOffset + geometryOffset;
                    geometryVertices.vertices[v + 1] = new Vector3(0, vo, chunkSize) + cellOffset + geometryOffset;
                    geometryVertices.vertices[v + 2] = new Vector3(chunkSize, a, 0) + cellOffset + geometryOffset;
                    geometryVertices.vertices[v + 3] = new Vector3(chunkSize, a, chunkSize) + cellOffset + geometryOffset;

                    geometryTriangles.triangles[t++] = v;
                    geometryTriangles.triangles[t++] = v + 1;
                    geometryTriangles.triangles[t++] = v + 2;
                    geometryTriangles.triangles[t++] = v + 2;
                    geometryTriangles.triangles[t++] = v + 1;
                    geometryTriangles.triangles[t++] = v + 3;

                    geometryUVs.uvs[v] = new Vector2(0, 0);
                    geometryUVs.uvs[v + 1] = new Vector2(1, 0);
                    geometryUVs.uvs[v + 2] = new Vector2(0, 1);
                    geometryUVs.uvs[v + 3] = new Vector2(1, 1);

                    v += 4;
                }
                if (planeUseAngularFeature)
                {
                    a += (planeAngleValue + (a / planeAngleDensity)) * planeAngleThreshold;
                    a = Mathf.Clamp(a, -float.MaxValue, float.MaxValue);
                }
            }
        }

        public void MDGProceduralExtendedPlane_ChangeSize(float size)
        {
            planeSize = size;
            if (!MbUpdateEveryFrame)
                MDMeshBase_ProcessCompleteMeshUpdate();
        }

        public void MDGProceduralExtendedPlane_ChangeExpand(bool expand)
        {
            planeExpand = expand;
            if (!MbUpdateEveryFrame)
                MDMeshBase_ProcessCompleteMeshUpdate();
        }

#if UNITY_EDITOR
        [MenuItem("GameObject/3D Object" + MD_Debug.PACKAGENAME + "Complex/Plane Extended")]
#endif
        public static GameObject CreateGeometry_Plane()
        {
            GameObject newGm = new GameObject("Plane");
            CreateGeometry<MDG_ProceduralExtendedPlane>(newGm);
            return PrepareGeometryInstance(newGm);
        }

#if UNITY_EDITOR
        [MenuItem("GameObject/3D Object" + MD_Debug.PACKAGENAME + "Complex/Plane Extended Angled")]
#endif
        public static GameObject CreateGeometry_PlaneAngled()
        {
            GameObject newGm = new GameObject("Plane");
            var pe = CreateGeometry<MDG_ProceduralExtendedPlane>(newGm);
            pe.planeUseAngularFeature = true;
            pe.planeAngleValue = -0.05f;
            pe.planeAngleThreshold = 0.15f;
            pe.geometryResolution = 24;
            pe.MDMeshBase_ProcessCompleteMeshUpdate();
            return PrepareGeometryInstance(newGm);
        }
    }
}

#if UNITY_EDITOR
namespace MD_Package_Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MDG_ProceduralExtendedPlane))]
    public sealed class MDG_ProceduralExtendedPlane_Editor : MD_GeometryBase_Editor
    {
        private MDG_ProceduralExtendedPlane mb;

        public override void OnEnable()
        {
            mMeshBase = (MD_MeshBase)target;
            mGeoBase = (MD_GeometryBase)target;
            mb = (MDG_ProceduralExtendedPlane)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            MDE_s(5);
            MDE_l("Procedural Extended Plane Settings", true);
            MDE_v();
            MDE_DrawProperty("planeSize", "Plane Size", "Size of the plane in general - how many chunks?");
            MDE_DrawProperty("planeExpand", "Expand Plane", "If enabled, the plane will expand with the chunks");
            MDE_DrawProperty("planeUseAngularFeature", "Use Angular Feature","If enabled, the plane will use the Angular feature");
            if (mb.planeUseAngularFeature)
            {
                MDE_v();
                MDE_DrawProperty("planeAngleValue", "Angle Value", "Angle value - not in degrees!", identOffset: true);
                MDE_DrawProperty("planeAngleDensity", "Angle Density", "Angle density - the higher the value, the less-bendable the plane will be", identOffset: true);
                MDE_DrawProperty("planeAngleThreshold", "Angle Threshold", identOffset: true);
                MDE_ve();
            }
            MDE_ve();
            MDE_AddMeshColliderRefresher(mb.gameObject);
            MDE_BackToMeshEditor(mb);
        }
    }
}
#endif
