using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

using MD_Package;
using MD_Package.Geometry;
#endif

namespace MD_Package.Geometry
{
    /// <summary>
    /// MDG(Mesh Deformation Geometry): Octahedron.
    /// Simple octahedron primitive generated at runtime in Unity Engine.
    /// Inherits from MD_GeometryBase.
    /// Written by Matej Vanco (2018, updated in 2023).
    /// </summary>
    [ExecuteInEditMode]
    [AddComponentMenu(MD_Debug.ORGANISATION + MD_Debug.PACKAGENAME + "Geometry/Octahedron")]
    public sealed class MDG_Octahedron : MD_GeometryBase
    {
        public float octaSize = 1.0f;
        public float octaTop = 1.0f;
        public float octaMid = 1.0f;
        public float octaBot = 1.0f;

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
            geometryResolution = Mathf.Min(120, geometryResolution);
            Vector3 offset = geometryCenterMesh ? Vector3.zero : Vector3.down * (octaSize * octaBot);

            geometryWorkingMesh = new Mesh();

            geometryVertices = new MDVertices(6);
            geometryTriangles = new MDTriangles(24);
            geometryUVs = new MDUvs(6);

            geometryVertices.SetVertice(0, ((Vector3.down * octaBot) * octaSize) - offset);
            geometryVertices.SetVertice(1, ((Vector3.forward * octaMid) * octaSize) - offset);
            geometryVertices.SetVertice(2, ((Vector3.left * octaMid) * octaSize) - offset);
            geometryVertices.SetVertice(3, ((Vector3.back * octaMid) * octaSize) - offset);
            geometryVertices.SetVertice(4, ((Vector3.right * octaMid) * octaSize) - offset);
            geometryVertices.SetVertice(5, ((Vector3.up * octaTop) * octaSize) - offset);

            geometryTriangles.SetTriangle(0, 1, 2, 0, 1, 2);
            geometryTriangles.SetTriangle(3, 4, 5, 0, 2, 3);
            geometryTriangles.SetTriangle(6, 7, 8, 0, 3, 4);
            geometryTriangles.SetTriangle(9, 10, 11, 0, 4, 1);

            geometryTriangles.SetTriangle(12, 13, 14, 5, 2, 1);
            geometryTriangles.SetTriangle(15, 16, 17, 5, 3, 2);
            geometryTriangles.SetTriangle(18, 19, 20, 5, 4, 3);
            geometryTriangles.SetTriangle(21, 22, 23, 5, 1, 4);

            for (int i = 0; i < geometryUVs.uvs.Length; i++)
            {
                Vector3 v = geometryVertices.vertices[i];
                Vector2 textureCoordinates;
                textureCoordinates.x = Mathf.Atan2(v.x, v.z) / (-2.0f * Mathf.PI);
                if (textureCoordinates.x < 0f)
                    textureCoordinates.x += 1.0f;
                textureCoordinates.y = Mathf.Asin(v.y) / Mathf.PI + 0.5f;
                geometryUVs.SetUV(i, textureCoordinates);
            }
        }

#if UNITY_EDITOR
        [MenuItem("GameObject/3D Object" + MD_Debug.PACKAGENAME + "Primitives/Octahedron", priority = 36)]
#endif
        public static GameObject CreateGeometry()
        {
            GameObject newGm = new GameObject("Octahedron");
            CreateGeometry<MDG_Octahedron>(newGm);
            return PrepareGeometryInstance(newGm);
        }
    }
}

#if UNITY_EDITOR
namespace MD_Package_Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MDG_Octahedron))]
    public sealed class MDG_Octahedron_Editor : MD_GeometryBase_Editor
    {
        private MDG_Octahedron mb;

        public override void OnEnable()
        {
            mMeshBase = (MD_MeshBase)target;
            mGeoBase = (MD_GeometryBase)target;
            mb = (MDG_Octahedron)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            MDE_s(5);
            MDE_l("Octahedron Settings", true);
            MDE_v();
            MDE_DrawProperty("octaSize", "Size");
            MDE_s(5);
            MDE_DrawProperty("octaTop", "Top");
            MDE_DrawProperty("octaMid", "Middle");
            MDE_DrawProperty("octaBot", "Bottom");
            MDE_ve();
            MDE_AddMeshColliderRefresher(mb.gameObject);
            MDE_BackToMeshEditor(mb);
        }
    }
}
#endif
