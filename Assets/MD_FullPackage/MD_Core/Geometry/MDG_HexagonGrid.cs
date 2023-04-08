using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

using MD_Package;
using MD_Package.Modifiers;
using MD_Package.Geometry;

namespace MD_Package.Geometry
{
    /// <summary>
    /// MDG(Mesh Deformation Geometry): Hexagon Grid.
    /// Procedural hexagon-grid generator at runtime in Unity Engine.
    /// Inherits from MD_GeometryBase.
    /// Written by Matej Vanco (2018, updated in 2023).
    /// </summary>
    [ExecuteInEditMode]
    [AddComponentMenu(MD_Debug.ORGANISATION + MD_Debug.PACKAGENAME + "Geometry/Hexagon Grid")]
    public sealed class MDG_HexagonGrid : MD_GeometryBase
    {
        public float hexaCellSize = 1;
        [Range(0.0f, 10.0f)] public float hexaOffsetX = 0.0f;
        [Range(0.0f, 10.0f)] public float hexaOffsetZ = 0.0f;

        public float hexaMaximumHeightRange = 1;
        public float hexaMiminumHeightRange = 0.1f;

        public bool hexaPlanarHexagon = true;
        public bool hexaRevertTriangles = false;

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
            MDGHexagon_Setup();

            MDGHexagon_ModifyMesh();
        }

        private void MDGHexagon_Setup()
        {
            geometryResolution = Mathf.Min(90, geometryResolution);

            geometryWorkingMesh = new Mesh();

            geometryVertices = new MDVertices(geometryResolution * geometryResolution * (hexaPlanarHexagon ? 7 : 14));
            geometryTriangles = new MDTriangles(geometryResolution * geometryResolution * (hexaPlanarHexagon ? 18 : 72));
            geometryUVs = new MDUvs(geometryResolution * geometryResolution * (hexaPlanarHexagon ? 7 : 14));
        }

        public void MDGHexagon_ChangePlanarHexagon(bool isPlanar)
        {
            hexaPlanarHexagon = isPlanar;
            if (!MbUpdateEveryFrame)
                MDMeshBase_ProcessCompleteMeshUpdate();
        }

        /// <summary>
        /// Modify spatial Hexagon Grid with min and max values. Process value corresponds to the randomized height
        /// </summary>
        public void MDGHexagon_ModifyMesh(float min = 0, float max = 0, float processValue = 0)
        {
            int v = 0;
            int t = 0;

            for (int x = 0; x < geometryResolution; x++)
            {
                for (int z = 0; z < geometryResolution; z++)
                {
                    float off = geometryCenterMesh ? (geometryResolution / 2.0f) : 0;
                    Vector3 Center = new Vector3(x + (z * 0.5f) - (z / 2) - (off - 0.5f ), 0, z - (off - 0.5f));
                    Center.x += x * hexaOffsetX;
                    Center.z += z * (hexaOffsetZ - 0.25f);

                    GeneratePlanar(Center);

                    if(hexaPlanarHexagon)
                    {
                        v += 7;
                        continue;
                    }

                    Vector3 AddHeight = new Vector3(0, 1, 0);
                    float RandomHeight = Random.Range(min, max);
                    if (randomizedHeights != null && x + z < randomizedHeights.Length)
                    {
                        int xz = x + z;
                        if (randomizedHeights[xz] == 0)
                            randomizedHeights[xz] = RandomHeight;
                        else
                            RandomHeight = ((Mathf.Lerp(randomizedHeights[xz], Random.Range(min, max) * randomizedHeights[xz], 0.12f)) * ((x + 1) * 0.1f)) * processValue;
                    }

                    if (RandomHeight != 0)
                        Center.y = RandomHeight;

                    GenerateSpatial(Center, AddHeight);

                    v += 14;
                }
            }
            if (hexaPlanarHexagon || hexaRevertTriangles)
                geometryTriangles.ReverseTriangles();

            void GeneratePlanar(Vector3 Center)
            {
                //Center
                geometryVertices.SetVertice(v, Center + geometryOffset);
                geometryUVs.SetUV(v, new Vector2(0.5f, 0.5f));
                //Top
                geometryVertices.SetVertice(v + 1, (new Vector3(0, 0, 0.5f) * hexaCellSize) + Center + geometryOffset);
                geometryUVs.SetUV(v + 1, new Vector2(0.5f, 1.0f));
                //Top-Right
                geometryVertices.SetVertice(v + 2, (new Vector3(0.5f, 0, 0.25f) * hexaCellSize) + Center + geometryOffset);
                geometryUVs.SetUV(v + 2, new Vector2(1.0f, 0.75f));
                //Bot-Right
                geometryVertices.SetVertice(v + 3, (new Vector3(0.5f, 0, -0.25f) * hexaCellSize) + Center + geometryOffset);
                geometryUVs.SetUV(v + 3, new Vector2(1.0f, 0.25f));
                //Bot
                geometryVertices.SetVertice(v + 4, (new Vector3(0f, 0, -0.5f) * hexaCellSize) + Center + geometryOffset);
                geometryUVs.SetUV(v + 4, new Vector2(0.5f, 0));
                //Bot-Left
                geometryVertices.SetVertice(v + 5, (new Vector3(-0.5f, 0, -0.25f) * hexaCellSize) + Center + geometryOffset);
                geometryUVs.SetUV(v + 5, new Vector2(0.0f, 0.25f));
                //Top_Left
                geometryVertices.SetVertice(v + 6, (new Vector3(-0.5f, 0, 0.25f) * hexaCellSize) + Center + geometryOffset);
                geometryUVs.SetUV(v + 6, new Vector2(0.0f, 0.75f));

                // Top-Face
                geometryTriangles.SetTriangle(t++, t++, t++, v, v + 2, v + 1);
                geometryTriangles.SetTriangle(t++, t++, t++, v, v + 3, v + 2);
                geometryTriangles.SetTriangle(t++, t++, t++, v, v + 4, v + 3);
                geometryTriangles.SetTriangle(t++, t++, t++, v, v + 5, v + 4);
                geometryTriangles.SetTriangle(t++, t++, t++, v, v + 6, v + 5);
                geometryTriangles.SetTriangle(t++, t++, t++, v, v + 1, v + 6);
            }

            void GenerateSpatial(Vector3 Center, Vector3 Height)
            {
                //Center
                geometryVertices.SetVertice(v + 7, Center + geometryOffset + Height);
                geometryUVs.SetUV(v + 7, new Vector2(0.5f, 0.5f));
                //Top
                geometryVertices.SetVertice(v + 8, (new Vector3(0, 0, 0.5f) * hexaCellSize) + Center + geometryOffset + Height);
                geometryUVs.SetUV(v + 8, new Vector2(0.5f, 1.0f));
                //Top-Right
                geometryVertices.SetVertice(v + 9, (new Vector3(0.5f, 0, 0.25f) * hexaCellSize) + Center + geometryOffset + Height);
                geometryUVs.SetUV(v + 9, new Vector2(1.0f, 0.75f));
                //Bot-Right
                geometryVertices.SetVertice(v + 10, (new Vector3(0.5f, 0, -0.25f) * hexaCellSize) + Center + geometryOffset + Height);
                geometryUVs.SetUV(v + 10, new Vector2(1.0f, 0.25f));
                //Bot
                geometryVertices.SetVertice(v + 11, (new Vector3(0f, 0, -0.5f) * hexaCellSize) + Center + geometryOffset + Height);
                geometryUVs.SetUV(v + 11, new Vector2(0.5f, 0));
                //Bot-Left
                geometryVertices.SetVertice(v + 12, (new Vector3(-0.5f, 0, -0.25f) * hexaCellSize) + Center + geometryOffset + Height);
                geometryUVs.SetUV(v + 12, new Vector2(0.0f, 0.25f));
                //Top_Left
                geometryVertices.SetVertice(v + 13, (new Vector3(-0.5f, 0, 0.25f) * hexaCellSize) + Center + geometryOffset + Height);
                geometryUVs.SetUV(v + 13, new Vector2(0.0f, 0.75f));

                // Bottom-Face
                geometryTriangles.SetTriangle(t++, t++, t++, v + 7, v + 8, v + 9);
                geometryTriangles.SetTriangle(t++, t++, t++, v + 7, v + 9, v + 10);
                geometryTriangles.SetTriangle(t++, t++, t++, v + 7, v + 10, v + 11);
                geometryTriangles.SetTriangle(t++, t++, t++, v + 7, v + 11, v + 12);
                geometryTriangles.SetTriangle(t++, t++, t++, v + 7, v + 12, v + 13);
                geometryTriangles.SetTriangle(t++, t++, t++, v + 7, v + 13, v + 8);

                // SIDES
                geometryTriangles.SetTriangle(t++, t++, t++, v + 2, v + 8, v + 1);
                geometryTriangles.SetTriangle(t++, t++, t++, v + 8, v + 2, v + 9);
                geometryTriangles.SetTriangle(t++, t++, t++, v + 3, v + 9, v + 2);
                geometryTriangles.SetTriangle(t++, t++, t++, v + 9, v + 3, v + 10);
                geometryTriangles.SetTriangle(t++, t++, t++, v + 4, v + 10, v + 3);
                geometryTriangles.SetTriangle(t++, t++, t++, v + 10, v + 4, v + 11);
                geometryTriangles.SetTriangle(t++, t++, t++, v + 5, v + 11, v + 4);
                geometryTriangles.SetTriangle(t++, t++, t++, v + 11, v + 5, v + 12);
                geometryTriangles.SetTriangle(t++, t++, t++, v + 6, v + 12, v + 5);
                geometryTriangles.SetTriangle(t++, t++, t++, v + 12, v + 6, v + 13);
                geometryTriangles.SetTriangle(t++, t++, t++, v + 1, v + 13, v + 6);
                geometryTriangles.SetTriangle(t++, t++, t++, v + 13, v + 1, v + 8);
            }
        }

        /// <summary>
        /// Randomize hexagon grid height by the specific min and max values
        /// </summary>
        public void MDGHexagon_RandomizeHeight(float min, float max)
        {
            MDGHexagon_Setup();
            MDGHexagon_ModifyMesh(min, max);
            MDMeshBase_UpdateMesh();
            MDMeshBase_RecalculateMesh();
        }

        /// <summary>
        /// Randomize hexagon grid height by the uniform value
        /// </summary>
        public void MDGHexagon_RandomizeHeight(float uniformValue)
        {
            MDGHexagon_RandomizeHeight(0.1f, uniformValue);
        }

        private float[] randomizedHeights;
        /// <summary>
        /// React the hexagon grid with any audio - requires spacial hexagon grid
        /// </summary>
        public void MDGHexagon_SoundReact(MDM_SoundReact mdmsr)
        {
            if(randomizedHeights == null)
                randomizedHeights = new float[geometryResolution * geometryResolution];
            MDGHexagon_Setup();
            MDGHexagon_ModifyMesh(hexaMiminumHeightRange, hexaMaximumHeightRange, mdmsr.OutputData);
            MDMeshBase_UpdateMesh();
            MDMeshBase_RecalculateMesh();
        }

#if UNITY_EDITOR
        [MenuItem("GameObject/3D Object" + MD_Debug.PACKAGENAME + "Complex/Hexagon Grid")]
#endif
        public static GameObject CreateGeometry_HexagonGrid()
        {
            GameObject newGm = new GameObject("Hexagon Grid");
            var he = CreateGeometry<MDG_HexagonGrid>(newGm);
            he.geometryResolution = 8;
            he.MDMeshBase_ProcessCompleteMeshUpdate();
            return PrepareGeometryInstance(newGm);
        }

#if UNITY_EDITOR
        [MenuItem("GameObject/3D Object" + MD_Debug.PACKAGENAME + "Complex/Planar Hexagon")]
#endif
        public static GameObject CreateGeometry_PlanarHexagon()
        {
            GameObject newGm = new GameObject("Planar Hexagon");
            var he = CreateGeometry<MDG_HexagonGrid>(newGm);
            he.geometryResolution = 1;
            he.hexaPlanarHexagon = true;
            he.MDMeshBase_ProcessCompleteMeshUpdate();
            return PrepareGeometryInstance(newGm);
        }

#if UNITY_EDITOR
        [MenuItem("GameObject/3D Object" + MD_Debug.PACKAGENAME + "Complex/Spatial Hexagon")]
#endif
        public static GameObject CreateGeometry_SpatialHexagon()
        {
            GameObject newGm = new GameObject("Spatial Hexagon");
            var he = CreateGeometry<MDG_HexagonGrid>(newGm);
            he.geometryResolution = 1;
            he.hexaPlanarHexagon = false;
            he.MDMeshBase_ProcessCompleteMeshUpdate();
            return PrepareGeometryInstance(newGm);
        }
    }
}

#if UNITY_EDITOR
namespace MD_Package_Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MDG_HexagonGrid))]
    public sealed class MD_HexagonGrid_Editor : MD_GeometryBase_Editor
    {
        private MDG_HexagonGrid hg;

        public override void OnEnable()
        {
            mMeshBase = (MD_MeshBase)target;
            mGeoBase = (MD_GeometryBase)target;
            hg = (MDG_HexagonGrid)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            MDE_s(5);
            MDE_l("Hexagon Grid Settings", true);
            MDE_v();

            MDE_v();
            MDE_DrawProperty("hexaCellSize", "Cell Size");
            MDE_DrawProperty("hexaOffsetX", "Cell Offset X");
            MDE_DrawProperty("hexaOffsetZ", "Cell Offset Z");
            MDE_DrawProperty("hexaPlanarHexagon", "Planar Hexagon");
            MDE_DrawProperty("hexaRevertTriangles", "Revert Triangles");
            MDE_ve();
             if (!hg.MbUpdateEveryFrame && !hg.hexaPlanarHexagon)
             {
                 MDE_s(5);
                 if (MDE_b("Randomize Height"))
                     hg.MDGHexagon_RandomizeHeight(hg.hexaMiminumHeightRange, hg.hexaMaximumHeightRange);
                 MDE_DrawProperty("hexaMaximumHeightRange", "Max Random Height Value");
                 MDE_DrawProperty("hexaMiminumHeightRange", "Min Random Height Value");

             }
            MDE_ve();
            MDE_BackToMeshEditor(hg);
        }
    }
}
#endif
