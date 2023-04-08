using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

using MD_Package;
using MD_Package.Geometry;
#endif

namespace MD_Package.Geometry
{
    /// <summary>
    /// MDG(Mesh Deformation Geometry): Procedural Plane.
    /// Procedural plane primitive generated at runtime in Unity Engine.
    /// Inherits from MD_GeometryBase.
    /// Written by Matej Vanco (2016, updated in 2023).
    /// </summary>
    [ExecuteInEditMode]
    [AddComponentMenu(MD_Debug.ORGANISATION + MD_Debug.PACKAGENAME + "Geometry/Plane")]
    public sealed class MDG_ProceduralPlane : MD_GeometryBase
    {
        public float planeSize = 1f;

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
            geometryWorkingMesh = new Mesh();

            int resolution = Mathf.Max(1, geometryResolution + 1);

            geometryVertices = new MDVertices(resolution * resolution);
            geometryTriangles = new MDTriangles(((resolution - 1) * (resolution - 1)) * 6);
            geometryUVs = new MDUvs(geometryVertices.vertices.Length);
            float centralOffset = !geometryCenterMesh ? planeSize / 2.0f : 0;

            int v = 0;
            int t = 0;

            for (int z = 0; z < resolution; z++)
            {
                float zPos = ((float)z / (resolution - 1) - .5f) * planeSize;
                int vvv = v + (z * resolution);
                for (int x = 0; x < resolution; x++)
                {
                    float xPos = ((float)x / (resolution - 1) - .5f) * planeSize;
                    geometryVertices.SetVertice(vvv + x, new Vector3(xPos + centralOffset, 0f, zPos + centralOffset) + geometryOffset);
                    geometryUVs.SetUV(vvv + x, new Vector2((float)x / (resolution - 1), (float)z / (resolution - 1)));
                }
            }

            for (int face = 0; face < (resolution - 1) * (resolution - 1); face++)
            {
                int i = face % (resolution - 1) + (face / (resolution - 1) * resolution);
                geometryTriangles.SetTriangle(t++, t++, t++, i + resolution, i + 1, i);
                geometryTriangles.SetTriangle(t++, t++, t++, i + resolution, i + resolution + 1, i + 1);
            }
        }

        public void MDGProceduralPlane_ChangeSize(float size)
        {
            planeSize = size;
            if (!MbUpdateEveryFrame)
                MDMeshBase_ProcessCompleteMeshUpdate();
        }

#if UNITY_EDITOR
        [MenuItem("GameObject/3D Object" + MD_Debug.PACKAGENAME + "Primitives/Plane", priority = 1)]
#endif
        public static GameObject CreateGeometry_Plane()
        {
            GameObject newGm = new GameObject("Plane");
            var p = CreateGeometry<MDG_ProceduralPlane>(newGm);
            p.geometryResolution = 6;
            p.MDMeshBase_ProcessCompleteMeshUpdate();
            return PrepareGeometryInstance(newGm);
        }

#if UNITY_EDITOR
        [MenuItem("GameObject/3D Object" + MD_Debug.PACKAGENAME + "Primitives/Quad", priority = 0)]
#endif
        public static GameObject CreateGeometry_Quad()
        {
            GameObject newGm = new GameObject("Quad");
            var p = CreateGeometry<MDG_ProceduralPlane>(newGm);
            p.geometryResolution = 1;
            p.MDMeshBase_ProcessCompleteMeshUpdate();
            return PrepareGeometryInstance(newGm);
        }
    }
}

#if UNITY_EDITOR
namespace MD_Package_Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MDG_ProceduralPlane))]
    public sealed class MDG_ProceduralPlane_Editor : MD_GeometryBase_Editor
    {
        private MDG_ProceduralPlane mb;

        public override void OnEnable()
        {
            mMeshBase = (MD_MeshBase)target;
            mGeoBase = (MD_GeometryBase)target;
            mb = (MDG_ProceduralPlane)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            MDE_s(5);
            MDE_l("Procedural Plane Settings", true);
            MDE_v();
            MDE_DrawProperty("planeSize", "Plane Size", "Size of the plane in general - how many chunks?");
            MDE_ve();
            MDE_AddMeshColliderRefresher(mb.gameObject);
            MDE_BackToMeshEditor(mb);
        }
    }
}
#endif
