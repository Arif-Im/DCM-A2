using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

using MD_Package;
using MD_Package.Geometry;
#endif

namespace MD_Package.Geometry
{
    /// <summary>
    /// MDG(Mesh Deformation Geometry): Sphere.
    /// Simple sphere primitive generated at runtime in Unity Engine.
    /// Inherits from MD_GeometryBase.
    /// Written by Matej Vanco (2016, updated in 2023).
    /// </summary>
    [ExecuteInEditMode]
    [AddComponentMenu(MD_Debug.ORGANISATION + MD_Debug.PACKAGENAME + "Geometry/Sphere")]
    public sealed class MDG_Sphere : MD_GeometryBase
    {
        public float sphereRadius = 1f;
        [Range(3, 100)] public int sphereSegments = 12;
        [Range(3, 100)] public int sphereSlices = 18;
        [Range(0, 360)] public int sphereSliceMax = 360;
        [Range(0, 180)] public int sphereVerticalSliceMax = 180;

        private void Reset()
        {
            geometryUseResolution = false;
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
            sphereSegments = Mathf.Max(3, sphereSegments);
            sphereSlices = Mathf.Max(3, sphereSlices);

            geometryWorkingMesh = new Mesh();

            Vector3 offset = !geometryCenterMesh ? new Vector3(0, -sphereRadius, 0) : Vector3.zero;
            offset -= geometryOffset;

            geometryVertices = new MDVertices((sphereSegments + 1) * (sphereSlices + 1));
            geometryTriangles = new MDTriangles(sphereSegments * sphereSlices * 6);
            geometryUVs = new MDUvs(geometryVertices.vertices.Length);

            float stacksAngle = sphereVerticalSliceMax * Mathf.Deg2Rad;
            float slicesAngle = sphereSliceMax * Mathf.Deg2Rad;
            float phiStep = stacksAngle / sphereSlices;
            float thetaStep = slicesAngle / sphereSegments;

            int v = 0;
            for (int i = 0; i <= sphereSlices; i++)
            {
                float phi = i * phiStep;

                for (int j = 0; j <= sphereSegments; j++)
                {
                    float theta = j * thetaStep;
                    Vector3 position = new Vector3(sphereRadius * Mathf.Sin(phi) * Mathf.Cos(theta), sphereRadius * Mathf.Cos(phi), sphereRadius * Mathf.Sin(phi) * Mathf.Sin(theta));

                    geometryVertices.SetVertice(v, position - offset);
                    geometryUVs.SetUV(v++, new Vector2(theta / slicesAngle, 1f - phi / stacksAngle));
                }
            }

            int ringVertexCount = sphereSegments + 1;
            int t = 0;
            for (int i = 0; i < sphereSlices; ++i)
            {
                for (int j = 0; j < sphereSegments; ++j)
                {
                    geometryTriangles.SetTriangle(t++, t++, t++, i * ringVertexCount + j, i * ringVertexCount + j + 1, (i + 1) * ringVertexCount + j);
                    geometryTriangles.SetTriangle(t++, t++, t++, (i + 1) * ringVertexCount + j, i * ringVertexCount + j + 1, (i + 1) * ringVertexCount + j + 1);
                }
            }
        }

        public void MDGSphere_ChangeSize(float size)
        {
            sphereRadius = size;
            if (!MbUpdateEveryFrame)
                MDMeshBase_ProcessCompleteMeshUpdate();
        }

        public override void MDGeometryBase_SyncUnsharedValues()
        {
            sphereSegments = geometryResolution;
            sphereSlices = geometryResolution;
        }


#if UNITY_EDITOR
        [MenuItem("GameObject/3D Object" + MD_Debug.PACKAGENAME + "Primitives/Sphere", priority = 3)]
#endif
        public static GameObject CreateGeometry()
        {
            GameObject newGm = new GameObject("Sphere");
            CreateGeometry<MDG_Sphere>(newGm);
            return PrepareGeometryInstance(newGm);
        }
    }
}

#if UNITY_EDITOR
namespace MD_Package_Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MDG_Sphere))]
    public sealed class MDG_Sphere_Editor : MD_GeometryBase_Editor
    {
        private MDG_Sphere mb;

        public override void OnEnable()
        {
            mMeshBase = (MD_MeshBase)target;
            mGeoBase = (MD_GeometryBase)target;
            mb = (MDG_Sphere)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            MDE_s(5);
            MDE_l("Sphere Settings", true);
            MDE_v();
            MDE_DrawProperty("sphereRadius", "Radius");
            MDE_DrawProperty("sphereSliceMax", "Slice Max");
            MDE_DrawProperty("sphereVerticalSliceMax", "Vertical Max");
            MDE_DrawProperty("sphereSegments", "Segments");
            MDE_DrawProperty("sphereSlices", "Slices");
            MDE_ve();
            MDE_AddMeshColliderRefresher(mb.gameObject);
            MDE_BackToMeshEditor(mb);
        }
    }
}
#endif