using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

using MD_Package;
using MD_Package.Geometry;
#endif

namespace MD_Package.Geometry
{
    /// <summary>
    /// MDG(Mesh Deformation Geometry): Torus.
    /// Simple torus primitive generated at runtime in Unity Engine.
    /// Inherits from MD_GeometryBase.
    /// Written by Matej Vanco (2016, updated in 2023).
    /// </summary>
    [ExecuteInEditMode]
    [AddComponentMenu(MD_Debug.ORGANISATION + MD_Debug.PACKAGENAME + "Geometry/Torus")]
    public sealed class MDG_Torus : MD_GeometryBase
    {
        public float torusThickness = 0.4f;
        public float torusRadius = 1.0f;
        [Range(3, 220)] public int torusSegments = 24;
        [Range(3, 220)] public int torusSides = 18;

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
            torusSegments = Mathf.Max(3, torusSegments);
            torusSides = Mathf.Max(3, torusSides);

            geometryWorkingMesh = new Mesh();

            Vector3 offset = !geometryCenterMesh ? new Vector3(0, -torusThickness, 0) : Vector3.zero;
            offset -= geometryOffset;

            geometryVertices = new MDVertices((torusSegments + 1) * (torusSides + 1));
            int nbFaces = geometryVertices.vertices.Length;
            int nbTriangles = nbFaces * 2;
            int nbIndexes = nbTriangles * 3;
            geometryTriangles = new MDTriangles(nbIndexes);
            geometryUVs = new MDUvs(geometryVertices.vertices.Length);

            float _2pi = Mathf.PI * 2f;
            for (int seg = 0; seg <= torusSegments; seg++)
            {
                int currSeg = seg == torusSegments ? 0 : seg;

                float t1 = (float)currSeg / torusSegments * _2pi;
                Vector3 r1 = new Vector3(Mathf.Cos(t1) * torusRadius, 0f, Mathf.Sin(t1) * torusRadius);

                for (int side = 0; side <= torusSides; side++)
                {
                    int currSide = side == torusSides ? 0 : side;

                    float t2 = (float)currSide / torusSides * _2pi;
                    Vector3 r2 = Quaternion.AngleAxis(-t1 * Mathf.Rad2Deg, Vector3.up) * new Vector3(Mathf.Sin(t2) * torusThickness, Mathf.Cos(t2) * torusThickness);

                    geometryVertices.SetVertice(side + seg * (torusSides + 1), r1 + r2 - offset);
                }
            }

            for (int seg = 0; seg <= torusSegments; seg++)
                for (int side = 0; side <= torusSides; side++)
                    geometryUVs.SetUV(side + seg * (torusSides + 1), new Vector2((float)seg / torusSegments, (float)side / torusSides));
           
            int i = 0;
            for (int seg = 0; seg <= torusSegments; seg++)
            {
                for (int side = 0; side <= torusSides - 1; side++)
                {
                    int current = side + seg * (torusSides + 1);
                    int next = side + (seg < (torusSegments) ? (seg + 1) * (torusSides + 1) : 0);

                    if (i < geometryTriangles.triangles.Length - 6)
                    {
                        geometryTriangles.SetTriangle(i++, i++, i++, current, next, next + 1);
                        geometryTriangles.SetTriangle(i++, i++, i++, current, next + 1, current + 1);
                    }
                }
            }
        }

        public void MDGTorus_ChangeRadius(float radius)
        {
            torusRadius = radius;
            if (!MbUpdateEveryFrame)
                MDMeshBase_ProcessCompleteMeshUpdate();
        }

        public void MDGTorus_ChangeThickness(float thickness)
        {
            torusThickness = thickness;
            if (!MbUpdateEveryFrame)
                MDMeshBase_ProcessCompleteMeshUpdate();
        }

        public override void MDGeometryBase_SyncUnsharedValues()
        {
            torusSegments = geometryResolution;
            torusSides = geometryResolution;
        }

#if UNITY_EDITOR
        [MenuItem("GameObject/3D Object" + MD_Debug.PACKAGENAME + "Primitives/Torus", priority = 34)]
#endif
        public static GameObject CreateGeometry()
        {
            GameObject newGm = new GameObject("Torus");
            CreateGeometry<MDG_Torus>(newGm);
            return PrepareGeometryInstance(newGm);
        }
    }
}

#if UNITY_EDITOR
namespace MD_Package_Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MDG_Torus))]
    public sealed class MDG_Torus_Editor : MD_GeometryBase_Editor
    {
        private MDG_Torus mb;

        public override void OnEnable()
        {
            mMeshBase = (MD_MeshBase)target;
            mGeoBase = (MD_GeometryBase)target;
            mb = (MDG_Torus)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            MDE_s(5);
            MDE_l("Torus Settings", true);
            MDE_v();
            MDE_DrawProperty("torusThickness", "Thickness");
            MDE_DrawProperty("torusRadius", "Radius");
            MDE_DrawProperty("torusSegments", "Segments");
            MDE_DrawProperty("torusSides", "Sides");
            MDE_ve();
            MDE_AddMeshColliderRefresher(mb.gameObject);
            MDE_BackToMeshEditor(mb);
        }
    }
}
#endif