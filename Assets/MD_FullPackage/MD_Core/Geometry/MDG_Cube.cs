using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

using MD_Package;
using MD_Package.Geometry;
#endif

namespace MD_Package.Geometry
{
    /// <summary>
    /// MDG(Mesh Deformation Geometry): Cube.
    /// Simple cube primitive generated at runtime in Unity Engine.
    /// Inherits from MD_GeometryBase.
    /// Written by Matej Vanco (2015, updated in 2023).
    /// </summary>
    [ExecuteInEditMode]
    [AddComponentMenu(MD_Debug.ORGANISATION + MD_Debug.PACKAGENAME + "Geometry/Cube")]
    public sealed class MDG_Cube : MD_GeometryBase
    {
        public float cubeSize = 1.0f;

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
            geometryResolution = Mathf.Min(50, geometryResolution);

            geometryWorkingMesh = new Mesh();

            geometryVertices = new MDVertices((geometryResolution * geometryResolution * 4) * 6);
            geometryTriangles = new MDTriangles((geometryResolution * geometryResolution * 6) * 6);
            geometryUVs = new MDUvs(geometryVertices.vertices.Length);

            currVertex = currTriangle = 0;

            //Top
            CreatePlaneChunk(new Vector3(0, -0.5f, 0), new Vector3(0, 0, 0));
            //Back
            CreatePlaneChunk(new Vector3(0, 0, -0.5f), new Vector3(90, 0, 0));
            //Bot
            CreatePlaneChunk(new Vector3(0, 0.5f, 0), new Vector3(180, 0, 0));
            //Front
            CreatePlaneChunk(new Vector3(0, 0, 0.5f), new Vector3(-90, 0, 0));
            //Right
            CreatePlaneChunk(new Vector3(0.5f, 0, 0), new Vector3(-90, 90, 0));
            //Left
            CreatePlaneChunk(new Vector3(-0.5f, 0, 0), new Vector3(90, 90, 0));

            currVertex = currTriangle = 0;
        }

        public void MDGCube_ChangeCubeSize(float size)
        {
            cubeSize = size;
            if (!MbUpdateEveryFrame)
                MDMeshBase_ProcessCompleteMeshUpdate();
        }

        private int currVertex;
        private int currTriangle;
        private void CreatePlaneChunk(Vector3 localOffset, Vector3 localRotation)
        {
            float step = 1.0f / geometryResolution;
            float chunkSize = step;
            localOffset.y -= !geometryCenterMesh ? 0.5f : 0;
            Quaternion rot = Quaternion.Euler(localRotation);

            int v = currVertex;
            int t = currTriangle;

            for (int x = 0; x < geometryResolution; x++)
            {
                float currX = step * x;

                for (int z = 0; z < geometryResolution; z++)
                {
                    float currZ = step * z;
                    Vector3 cellOffset = new Vector3(currX - 0.5f, 0, currZ - 0.5f);

                    geometryVertices.vertices[v] = ((rot * (new Vector3(0, 0, 0) + cellOffset)) - localOffset + geometryOffset) * cubeSize;
                    geometryVertices.vertices[v + 1] = ((rot * (new Vector3(0, 0, chunkSize) + cellOffset)) - localOffset + geometryOffset) * cubeSize;
                    geometryVertices.vertices[v + 2] = ((rot * (new Vector3(chunkSize, 0, 0) + cellOffset)) - localOffset + geometryOffset) * cubeSize;
                    geometryVertices.vertices[v + 3] = ((rot * (new Vector3(chunkSize, 0, chunkSize) + cellOffset)) - localOffset + geometryOffset)* cubeSize;

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
            }

            currVertex = v;
            currTriangle = t;
        }

#if UNITY_EDITOR
        [MenuItem("GameObject/3D Object" + MD_Debug.PACKAGENAME + "Primitives/Cube", priority = 2)]
#endif
        public static GameObject CreateGeometry()
        {
            GameObject newGm = new GameObject("Cube");
            CreateGeometry<MDG_Cube>(newGm);
            return PrepareGeometryInstance(newGm);
        }
    }
}

#if UNITY_EDITOR
namespace MD_Package_Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MDG_Cube))]
    public sealed class MDG_Cube_Editor : MD_GeometryBase_Editor
    {
        private MDG_Cube mb;

        public override void OnEnable()
        {
            mMeshBase = (MD_MeshBase)target;
            mGeoBase = (MD_GeometryBase)target;
            mb = (MDG_Cube)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            MDE_s(5);
            MDE_l("Cube Settings", true);
            MDE_v();
            MDE_DrawProperty("cubeSize", "Cube Size");
            MDE_ve();
            MDE_AddMeshColliderRefresher(mb.gameObject);
            MDE_BackToMeshEditor(mb);
        }
    }
}
#endif