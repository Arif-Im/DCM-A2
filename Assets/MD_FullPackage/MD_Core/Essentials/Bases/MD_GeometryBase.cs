using UnityEngine;
using System.Linq;

using MD_Package.Modifiers;
using MD_Package.Utilities;

#if UNITY_EDITOR
using UnityEditor;

using MD_Package.Geometry;
#endif

namespace MD_Package.Geometry
{
    /// <summary>
    /// MD(Mesh Deformation): Geometry Base.
    /// Base geometry class for all the geometry-related instances. Implement this base class to any script that will constantly work with Unity-meshes and frequent updates will be necessary.
    /// Inherits from MD_MeshBase.
    /// Written by Matej Vanco (2022, updated in 2023).
    /// </summary>
    public abstract class MD_GeometryBase : MD_MeshBase
    {
        public struct MDVertices
        {
            public bool Initialized { get => vertices != null; }
            public Vector3[] vertices;

            public MDVertices(int count)
            {
                vertices = new Vector3[count];
            }

            public void SetVertice(int i, Vector3 localPosition)
            {
                vertices[i] = localPosition;
            }
        }
        public struct MDTriangles
        {
            public bool Initialized { get => triangles != null; }
            public int[] triangles;

            public MDTriangles(int count)
            {
                triangles = new int[count];
            }

            public void SetTriangle(int t0, int t1, int t2, int v0, int v1, int v2)
            {
                triangles[t0] = v0;
                triangles[t1] = v1;
                triangles[t2] = v2;
            }

            public void ReverseTriangles()
            {
                triangles = triangles.Reverse().ToArray();
            }
        }
        public struct MDUvs
        {
            public bool Initialized { get => uvs != null; }
            public Vector2[] uvs;

            public MDUvs(int count)
            {
                uvs = new Vector2[count];
            }

            public void SetUV(int i, Vector2 localCoords)
            {
                uvs[i] = localCoords;
            }
        }

        /// <summary>
        /// Is the current GeometryBase initialized? Returns true if all the geometry mesh data are set
        /// </summary>
        public bool IsInitialized { get => geometryVertices.Initialized && geometryTriangles.Initialized && geometryUVs.Initialized; }

        // Essential temp mesh data
        public MDVertices geometryVertices;
        public MDTriangles geometryTriangles;
        public MDUvs geometryUVs;

        // Mesh extensions
        public string geometryName;
        [Range(1, 150)] public int geometryResolution = 1;
        public bool geometryUseResolution = true;
        public bool geometryCenterMesh = true;
        public Vector3 geometryOffset = Vector3.zero;

        protected Mesh geometryWorkingMesh;

        /// <summary>
        /// Create new geometry instance with specific type
        /// </summary>
        /// <typeparam name="T">Geometry type</typeparam>
        /// <param name="entry">Sender object</param>
        /// <returns>Returns initialized geometry instance</returns>
        public static T CreateGeometry<T>(GameObject entry, bool updateJustOnce = true) where T : MD_GeometryBase
        {
            T m = entry.AddComponent<T>();
            m.MDMeshBase_InitializeBase();
            if(updateJustOnce)
            {
                m.MDMeshBase_ProcessCompleteMeshUpdate();
                m.MbUpdateEveryFrame = false;
            }
            return m;
        }

        /// <summary>
        /// Create new geometry instance with specific type, process calculations just once and remove the component. This will leave just a complete mesh on the object in one frame
        /// </summary>
        /// <typeparam name="T">Geometry type</typeparam>
        /// <param name="entry">Sender object</param>
        /// <param name="geometryName">Name for the new mesh geometry</param>
        /// <returns>Returns initialized geometry instance</returns>
        public static void CreateGeometryAndDispose<T>(GameObject entry, string geometryName = "NewGeometryMesh") where T : MD_GeometryBase
        {
            T m = entry.AddComponent<T>();
            m.MDMeshBase_InitializeBase();
            m.geometryName = geometryName;
            m.MDMeshBase_ProcessCompleteMeshUpdate();
            if (Application.isPlaying)
                Destroy(m);
            else
                DestroyImmediate(m);
        }

        /// <summary>
        /// Create new geometry instance with specific type, process calculations just once and remove the component. This will return a completely new gameObject with mesh in one frame
        /// </summary>
        /// <typeparam name="T">Geometry type</typeparam>
        /// <param name="entry">Sender object</param>
        /// <param name="gameObjectName">Name for the new gameObject</param>
        /// <param name="geometryName">Name for the new mesh geometry</param>
        /// <param name="prepareMaterial">If enabled, the gameObject will get a default material</param>
        /// <returns>Returns initialized geometry instance</returns>
        public static GameObject CreateGeometryAndDispose<T>(string gameObjectName = "NewGeometryObject", string geometryName = "NewGeometryMesh", bool prepareMaterial = true) where T : MD_GeometryBase
        {
            T m = new GameObject(gameObjectName).gameObject.AddComponent<T>();
            m.MDMeshBase_InitializeBase();
            m.geometryName = geometryName;
            m.MDMeshBase_ProcessCompleteMeshUpdate();
            GameObject gm = m.gameObject;
            if (prepareMaterial)
                gm = PrepareGeometryInstance(gm, false);
            if (Application.isPlaying)
                Destroy(m);
            else
                DestroyImmediate(m);
            return gm;
        }

        /// <summary>
        /// Prepare created geometry instance in the scene. Ignore advanced params if not in editor
        /// </summary>
        public static GameObject PrepareGeometryInstance(GameObject sender, bool selectAndJumpToCamera = true)
        {
            if (!sender.TryGetComponent(out Renderer rend)) return null;

            Shader shad = MD_Utilities.MD_Specifics.GetProperPipelineDefaultShader();
            Material mat = new Material(shad);
            rend.sharedMaterial = mat;

            if (Application.isPlaying)
                selectAndJumpToCamera = false;

#if UNITY_EDITOR
            if (selectAndJumpToCamera)
            {
                Selection.activeGameObject = sender;
                var cs = SceneView.lastActiveSceneView;
                if (cs && cs.camera)
                    sender.transform.position = cs.camera.transform.position + cs.camera.transform.forward * 4.0f;
            }
#endif
            return sender;
        }

        #region MD_MeshBase implementations & initialization

        /// <summary>
        /// Base geometry initialization - is required to invoke for every class that inherits from the GeometryBase
        /// </summary>
        /// <param name="affectUpdateEveryFrameField">Affect 'Update every frame' field? This field may get disabled if vertex count is exceeded</param>
        /// <param name="checkMeshFilterMesh">Check if the mesh filter has a mesh source?</param>
        public override void MDMeshBase_InitializeBase(bool affectUpdateEveryFrameField = true, bool checkMeshFilterMesh = false)
        {
            if (!MD_Utilities.MD_Specifics.RestrictFromOtherTypes(this.gameObject, this.GetType(), new System.Type[3] { typeof(MD_ModifierBase), typeof(MD_GeometryBase), typeof(MD_MeshProEditor) }))
            {
#if UNITY_EDITOR
                if (!Application.isPlaying && MD_GlobalPreferences.PopupEditorWindow)
                    EditorUtility.DisplayDialog("Warning", "The geometry cannot be applied to this object, because the object already contains other modifiers or components that work with mesh-vertices. Please remove the existing modifiers to access the selected geometry.", "OK");
                else
                    MD_Debug.Debug(this, "The geometry cannot be applied to this object, because the object already contains other modifiers or components that work with mesh-vertices. Please remove the existing modifiers to access the selected geometry");
#else
                    MD_Debug.Debug(this, "The object contains another modifier or component that work with mesh-vertices, which is prohibited. The geometry will be destroyed");
#endif
                MbUpdateEveryFrame = false;
                MDMeshBase_DestroySelf();
                return;
            }

            // Setup mesh-renderers if its a geometry from scratch
            if (!GetComponent<MeshFilter>())
                gameObject.AddComponent<MeshFilter>();
            if (!GetComponent<MeshRenderer>())
                gameObject.AddComponent<MeshRenderer>();

            // Init base
            base.MDMeshBase_InitializeBase(affectUpdateEveryFrameField, checkMeshFilterMesh);
            geometryName = "GeometryMesh" + Random.Range(0, 999999).ToString();

        }

        /// <summary>
        /// Update current mesh with working geometry data (This will not recalculate normals or bounds. Call MDMeshBase_RecalculateMesh for this)
        /// </summary>
        public override void MDMeshBase_UpdateMesh()
        {
            if (!MbMeshFilter)
            {
                MD_Debug.Debug(this, "Mesh filter is null. This object might not be initialized. Please see docs for proper initialization", MD_Debug.DebugType.Error);
                return;
            }
            if (!geometryWorkingMesh || !IsInitialized)
                return;
            geometryWorkingMesh.vertices = geometryVertices.vertices;
            geometryWorkingMesh.triangles = geometryTriangles.triangles;
            geometryWorkingMesh.uv = geometryUVs.uvs;
            geometryWorkingMesh.name = geometryName;
            MbMeshFilter.sharedMesh = geometryWorkingMesh;
        }

        /// <summary>
        /// You can implement this method in case you would like to share/invoke common values/methods that inherited classes do not use
        /// </summary>
        public virtual void MDGeometryBase_SyncUnsharedValues() { }

        #endregion

        #region Public methods

        /// <summary>
        /// Change current geometry resolution
        /// </summary>
        public void MDGeometryBase_ChangeResolution(float res)
        {
            geometryResolution = (int)res;
            if(!MbUpdateEveryFrame)
                MDMeshBase_ProcessCompleteMeshUpdate();
        }

        /// <summary>
        /// Change current geometry center-feature
        /// </summary>
        public void MDGeometryBase_ChangeMeshCenter(bool centerGeo)
        {
            geometryCenterMesh = centerGeo;
            if (!MbUpdateEveryFrame)
                MDMeshBase_ProcessCompleteMeshUpdate();
        }

        /// <summary>
        /// Flip/reverse mesh faces/triangles
        /// </summary>
        public void MDGeometryBase_FlipTriangles()
        {
            if (geometryTriangles.Initialized)
            {
                geometryTriangles.ReverseTriangles();
                if (!MbUpdateEveryFrame)
                    MDMeshBase_ProcessCompleteMeshUpdate();
            }
        }

        #endregion
    }

}

#if UNITY_EDITOR
namespace MD_Package_Editor
{
    /// <summary>
    /// Base editor for the GeometryBase instances - all the class instances that inherit from the MD_GeometryBase MUST have the Unity-editor implemented!
    /// </summary>
    [CustomEditor(typeof(MD_GeometryBase), true)]
    public abstract class MD_GeometryBase_Editor : MD_MeshBase_Editor
    {
        protected MD_GeometryBase mGeoBase;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (!mGeoBase)
            {
                DrawDefaultInspector();
                return;
            }

            MDE_s();
            MDE_l("Base Geometry Settings", true);
            MDE_v();
            MDE_h(false);
            MDE_DrawProperty("geometryName", "Geometry Name", "Main geometry-mesh name");
            GUILayout.ExpandWidth(false);
            if (MDE_b("Random Name",100))
                mGeoBase.geometryName = "Mesh" + Random.Range(0, 9999999).ToString();
            MDE_he();
            MDE_s(3);
            if(mGeoBase.geometryUseResolution)
                MDE_DrawProperty("geometryResolution", "Geometry Resolution", "Resolution of the geometry mesh - how dense the mesh will be?");
            MDE_DrawProperty("geometryOffset", "Geometry Offset", "Position offset of the geometry mesh in a local space");
            MDE_DrawProperty("geometryCenterMesh", "Center Mesh", "If enabled, the mesh pivot point will be set to center");
            if (mGeoBase.IsInitialized)
            {
                MDE_s(3);
                MDE_h();
                MDE_l("Vertices: " + mGeoBase.geometryVertices.vertices.Length);
                if (!mGeoBase.MbUpdateEveryFrame && MDE_b("Flip", 40))
                {
                    mGeoBase.MDMeshBase_ProcessCalculations();
                    mGeoBase.MDGeometryBase_FlipTriangles();
                    mGeoBase.MDMeshBase_UpdateMesh();
                    mGeoBase.MDMeshBase_RecalculateMesh();
                }
                MDE_l("Triangles: " + mGeoBase.geometryTriangles.triangles.Length);
                MDE_l("UVs: " + mGeoBase.geometryUVs.uvs.Length);
                MDE_he();
            }
            MDE_ve();
        }
    }
}
#endif