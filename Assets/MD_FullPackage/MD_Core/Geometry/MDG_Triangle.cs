using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

using MD_Package;
using MD_Package.Geometry;
#endif

namespace MD_Package.Geometry
{
    /// <summary>
    /// MDG(Mesh Deformation Geometry): Triangle.
    /// Simple triangle/pyramid primitive generated at runtime in Unity Engine.
    /// Inherits from MD_GeometryBase.
    /// Written by Matej Vanco (2016, updated in 2023).
    /// </summary>
    [ExecuteInEditMode]
    [AddComponentMenu(MD_Debug.ORGANISATION + MD_Debug.PACKAGENAME + "Geometry/Triangle")]
    public sealed class MDG_Triangle : MD_GeometryBase
    {
        public bool rightAngle = false;
        public bool pyramid = false;

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

            int c = pyramid ? 6 : 1;

            geometryVertices = new MDVertices((geometryResolution * geometryResolution * 3) * c);
            geometryTriangles = new MDTriangles((geometryResolution * geometryResolution * 3 * geometryResolution) * c);
            geometryUVs = new MDUvs(geometryVertices.vertices.Length);
            currVertex = currTriangle = 0;

            if (!pyramid)
            {
                CreateTriangleChunk(geometryCenterMesh ? new Vector3(0, -0.25f, 1.0f) : Vector3.zero, Vector3.zero, rightAngle);
                return;
            }
            geometryResolution = Mathf.Min(80, geometryResolution);
            CreateTriangleChunk(new Vector3(0, 0, 1), new Vector3(-60, 0, 0));
            CreateTriangleChunk(new Vector3(0, 0, 0), new Vector3(-60, 90, 0));
            CreateTriangleChunk(new Vector3(-1, 0, 0), new Vector3(-60, 180, 0));
            CreateTriangleChunk(new Vector3(-1, 0, 1), new Vector3(-60, -90, 0));
            CreateTriangleChunk(new Vector3(0, 0, 0), new Vector3(180, 0, 0), true);
            CreateTriangleChunk(new Vector3(-1, 0, 1), new Vector3(180, 180, 0), true);
        }

        public void MDGTriangle_ChangeRightAngle(bool rAngle)
        {
            rightAngle = rAngle;
            if (!MbUpdateEveryFrame)
                MDMeshBase_ProcessCompleteMeshUpdate();
        }

        public void MDGTriangle_ChangeToPyramid(bool isPyramid)
        {
            pyramid = isPyramid;
            if (!MbUpdateEveryFrame)
                MDMeshBase_ProcessCompleteMeshUpdate();
        }

        public void MDGTriangle_ChangeScale(float scale)
        {
            transform.localScale = Vector3.one * scale;
        }

        private int currVertex;
        private int currTriangle;
        private void CreateTriangleChunk(Vector3 locationOffset, Vector3 rotationOffset, bool rightAngle = false)
        {
            float trisSizeHalf = geometryCenterMesh ? 0.5f : 0;
            Vector3 centeredTris = new Vector3(trisSizeHalf, trisSizeHalf / 2f, -trisSizeHalf) - geometryOffset;
            Vector3 leftBot, rightBot, middleTop;
            Quaternion rotOffset = Quaternion.Euler(rotationOffset);

            float step = 1.0f / geometryResolution;
            float stepXHalf = step / 2f;
            int xResolution = geometryResolution;
            int v = currVertex;
            int t = currTriangle;

            for (int z = 0; z < geometryResolution; z++)
            {
                float stepXHalfMulti = stepXHalf * z;
                float currStepZ = step * z;

                for (int x = 0; x < xResolution; x++)
                {
                    float currStepX = step * x;

                    if (!rightAngle)
                    {
                        leftBot = new Vector3(currStepX + stepXHalfMulti, 0, currStepZ);
                        rightBot = new Vector3(step + currStepX + stepXHalfMulti, 0, currStepZ);
                        middleTop = new Vector3(rightBot.x - stepXHalf, 0, step + currStepZ);
                    }
                    else
                    {
                        leftBot = new Vector3(currStepX + currStepZ, 0, currStepZ);
                        rightBot = new Vector3(leftBot.x + step, 0, currStepZ);
                        middleTop = new Vector3(rightBot.x, 0, step + currStepZ);
                    }

                    geometryVertices.SetVertice(v, (rotOffset * leftBot) - centeredTris - locationOffset);
                    geometryVertices.SetVertice(v + 1, ((rotOffset * rightBot) - centeredTris - locationOffset));
                    geometryVertices.SetVertice(v + 2, ((rotOffset * middleTop) - centeredTris - locationOffset));

                    geometryUVs.SetUV(v, new Vector2(leftBot.x, leftBot.z));
                    geometryUVs.SetUV(v + 1, new Vector2(rightBot.x, rightBot.z));
                    geometryUVs.SetUV(v + 2, new Vector2(middleTop.x, middleTop.z));

                    geometryTriangles.SetTriangle(t++, t++, t++, v, v + 2, v + 1);
                    if (x > 0 && geometryResolution > 1)
                        geometryTriangles.SetTriangle(t++, t++, t++, v, v - 1, v + 2);

                    v += 3;
                }
                xResolution--;
            }
            currVertex = v;
            currTriangle = t;
        }

#if UNITY_EDITOR
        [MenuItem("GameObject/3D Object" + MD_Debug.PACKAGENAME + "Primitives/Triangle", priority = 30)]
#endif
        public static GameObject CreateGeometry_Triangle()
        {
            GameObject newGm = new GameObject("Triangle");
            CreateGeometry<MDG_Triangle>(newGm);
            return PrepareGeometryInstance(newGm);
        }

#if UNITY_EDITOR
        [MenuItem("GameObject/3D Object" + MD_Debug.PACKAGENAME + "Primitives/Triangle - Right Angled", priority = 31)]
#endif
        public static GameObject CreateGeometry_TriangleRightAngle()
        {
            GameObject newGm = new GameObject("Triangle Right Angle");
            var tr = CreateGeometry<MDG_Triangle>(newGm);
            tr.rightAngle = true;
            tr.MDMeshBase_ProcessCompleteMeshUpdate();
            return PrepareGeometryInstance(newGm);
        }

#if UNITY_EDITOR
        [MenuItem("GameObject/3D Object" + MD_Debug.PACKAGENAME + "Primitives/Pyramid", priority = 32)]
#endif
        public static GameObject CreateGeometry_Pyramid()
        {
            GameObject newGm = new GameObject("Pyramid");
            var p = CreateGeometry<MDG_Triangle>(newGm);
            p.pyramid = true;
            p.MDMeshBase_ProcessCompleteMeshUpdate();
            return PrepareGeometryInstance(newGm);
        }
    }
}

#if UNITY_EDITOR
namespace MD_Package_Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MDG_Triangle))]
    public sealed class MDG_Triangle_Editor : MD_GeometryBase_Editor
    {
        private MDG_Triangle mb;

        public override void OnEnable()
        {
            mMeshBase = (MD_MeshBase)target;
            mGeoBase = (MD_GeometryBase)target;
            mb = (MDG_Triangle)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            MDE_s(5);
            MDE_l("Procedural Plane Settings", true);
            MDE_v();
            if (!mb.pyramid)
                MDE_DrawProperty("rightAngle", "Right Angle");
            if(!mb.rightAngle)
                MDE_DrawProperty("pyramid", "Pyramid");
            MDE_ve();
            MDE_AddMeshColliderRefresher(mb.gameObject);
            MDE_BackToMeshEditor(mb);
        }
    }
}
#endif