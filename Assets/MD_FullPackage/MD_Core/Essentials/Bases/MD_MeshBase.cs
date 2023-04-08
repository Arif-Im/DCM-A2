using UnityEngine;

using MD_Package.Utilities;

#if UNITY_EDITOR
using UnityEditor;

using MD_Package;
#endif

namespace MD_Package
{
    /// <summary>
    /// MD(Mesh Deformation): Mesh Base in Mesh Deformation Package.
    /// Base mesh class for all the gameObject instances with mesh-related behaviour. Implement this base class to any script that will work with Unity meshes and Mesh Deformation Package.
    /// Nested inheritation continues to the MD_ModifierBase (modifiers) and MD_GeometryBase (geometry and primitives).
    /// Written by Matej Vanco (2022, updated in 2023).
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    public abstract class MD_MeshBase : MonoBehaviour
    {
        // Base mesh filter
        public MeshFilter MbMeshFilter { get => _mbMeshFilter; protected set => _mbMeshFilter = value; }
        [SerializeField] private MeshFilter _mbMeshFilter;

        // Essential cross-modifier fields
        public bool MbUpdateEveryFrame = true;
        public bool MbRecalculateNormals = true;
        public bool MbAlternativeNormals = false;
        public float MbAlternativeNormalsAngle = 90.0f;
        public bool MbRecalculateBounds = true;

        /// <summary>
        /// Initialize and cache all the required fields - required to call if inherited from this class
        /// </summary>
        /// <param name="affectUpdateEveryFrameField">Affect 'Update every frame' field? This field may get disabled if vertex count is exceeded</param>
        /// <param name="checkMeshFilterMesh">Check if the mesh filter has a mesh source?</param>
        public virtual void MDMeshBase_InitializeBase(bool affectUpdateEveryFrameField = true, bool checkMeshFilterMesh = false)
        {
            // Mesh filter is required for this class
            MbMeshFilter = GetComponent<MeshFilter>();

            // Check for mesh filter data (doublecheck)
            if (!MDMeshBase_CheckForMeshFilter(checkMeshFilterMesh))
                return;

            // Setup rest of the fields
            if (affectUpdateEveryFrameField && MbMeshFilter.sharedMesh)
                MbUpdateEveryFrame = MD_GlobalPreferences.VertexLimit > MbMeshFilter.sharedMesh.vertexCount;

            MbRecalculateBounds = MD_GlobalPreferences.AutoRecalcBounds;
            MbRecalculateNormals = MD_GlobalPreferences.AutoRecalcNormals;
            MbAlternativeNormals = MD_GlobalPreferences.AlternateNormalsRecalc;
        }

        /// <summary>
        /// Internal purpose - check if everything is alright and the MeshFilter is okay!
        /// </summary>
        /// <returns>Returns true if all the required parameters have been met</returns>
        protected bool MDMeshBase_CheckForMeshFilter(bool checkMeshFilterMesh = false)
        {
            if (!MbMeshFilter)
            {
                MD_Debug.Debug(this, "The gameObject '" + this.gameObject.name + "' does not contain MeshFilter component", MD_Debug.DebugType.Error);
                return false;
            }
            if (checkMeshFilterMesh && !MbMeshFilter.sharedMesh)
            {
                MD_Debug.Debug(this, "The gameObject '" + this.gameObject.name + "' does not contain mesh source inside the MeshFilter component", MD_Debug.DebugType.Error);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Async method for safe self-destroy (may cause Unity-crashes if called from 'Reset' method)
        /// </summary>
        protected async void MDMeshBase_DestroySelf()
        {
            await System.Threading.Tasks.Task.Delay(50);
            if (Application.isPlaying)
                Destroy(this);
            else
                DestroyImmediate(this);
        }

        #region Required mesh-base implementations

        /// <summary>
        /// Implement this method for updating the main mesh with the new mesh-data (vertices, triangles etc) - See other classes that inherits from this class.
        /// </summary>
        public abstract void MDMeshBase_UpdateMesh();

        /// <summary>
        /// Implement this method for processing certain mesh calculations with vertices, triangles etc - See other classes that inherits from this class.
        /// </summary>
        public abstract void MDMeshBase_ProcessCalculations();

        #endregion

        /// <summary>
        /// Process complete mesh update sequence - process calculation > pass data to mesh > recalculate surface
        /// </summary>
        public void MDMeshBase_ProcessCompleteMeshUpdate()
        {
            MDMeshBase_ProcessCalculations();
            MDMeshBase_UpdateMesh();
            MDMeshBase_RecalculateMesh();
        }

        /// <summary>
        /// Recalculate mesh bounds & normals
        /// </summary>
        public virtual void MDMeshBase_RecalculateMesh(bool forceNormals = false, bool forceBounds = false)
        {
            if (MbRecalculateNormals || forceNormals)
            {
                if (!MbAlternativeNormals)
                    MbMeshFilter.sharedMesh.RecalculateNormals();
                else
                    MD_Utilities.AlternativeNormals.RecalculateNormals(MbMeshFilter.sharedMesh, MbAlternativeNormalsAngle);
            }

            if (MbRecalculateBounds || forceBounds)
                MbMeshFilter.sharedMesh.RecalculateBounds();
        }
    }
}

#if UNITY_EDITOR
namespace MD_Package_Editor
{
    /// <summary>
    /// Base editor for the MeshBase instances - all the class instances that inherit from the MD_MeshBase MUST have the Unity-editor implemented!
    /// </summary>
    [CustomEditor(typeof(MD_MeshBase), true)]
    public abstract class MD_MeshBase_Editor : MD_EditorUtilities
    {
        protected MD_MeshBase mMeshBase;
        protected bool showUpdateEveryFrame = true;
        private bool meshBaseFoldout = false;

        public abstract void OnEnable();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (!mMeshBase)
            {
                DrawDefaultInspector();
                return;
            }

            MDE_s();
            MDE_v();
            meshBaseFoldout = MDE_f(meshBaseFoldout, "Mesh Base Settings");
            if (meshBaseFoldout)
            {
                MDE_v();
                if (showUpdateEveryFrame)
                    MDE_DrawProperty("MbUpdateEveryFrame", "Update Every Frame", "Update current mesh and modifier every frame (default Update)");
                MDE_DrawProperty("MbRecalculateNormals", "Recalculate Normals", "Recalculate normals automatically");
                MDE_plus();
                MDE_DrawProperty("MbAlternativeNormals", "Alternative Normals", "Use alternative normals recalculation - better looking results, but takes more performance (fits for seam-based meshes)");
                if (mMeshBase.MbAlternativeNormals)
                    MDE_DrawProperty("MbAlternativeNormalsAngle", "Normals Angle");
                MDE_minus();
                MDE_DrawProperty("MbRecalculateBounds", "Recalculate Bounds", "Recalculate bounds automatically");

                if ((!mMeshBase.MbUpdateEveryFrame && showUpdateEveryFrame) || !mMeshBase.MbRecalculateBounds || !mMeshBase.MbRecalculateNormals)
                {
                    MDE_s(5);
                    MDE_l("Manual Mesh Controls", true);
                    MDE_h();
                    if (!mMeshBase.MbUpdateEveryFrame && showUpdateEveryFrame)
                    {
                        if (MDE_b("Update Mesh"))
                        {
                            mMeshBase.MDMeshBase_ProcessCalculations();
                            mMeshBase.MDMeshBase_UpdateMesh();
                            mMeshBase.MDMeshBase_RecalculateMesh();
                        }
                    }
                    if (!mMeshBase.MbRecalculateNormals)
                    {
                        if (MDE_b("Recalculate Normals"))
                            mMeshBase.MDMeshBase_RecalculateMesh(forceNormals: true);
                    }
                    if (!mMeshBase.MbRecalculateBounds)
                    {
                        if (MDE_b("Recalculate Bounds"))
                            mMeshBase.MDMeshBase_RecalculateMesh(forceBounds: true);
                    }
                    MDE_he();
                }
                MDE_ve();
                if (MDE_b("Save Mesh To Assets", "Save current mesh to the assets folder as prefab"))
                {
                    MD_Utilities.MD_Specifics.SaveMeshToTheAssetsDatabase(mMeshBase.MbMeshFilter);
                    return;
                }
            }
            MDE_ve();
        }
    }
}
#endif