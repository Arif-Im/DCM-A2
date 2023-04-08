using System;
using System.Threading;
using UnityEngine;

using MD_Package;
using MD_Package.Geometry;
using MD_Package.Utilities;

#if UNITY_EDITOR
using UnityEditor;

using MD_Package.Modifiers;
#endif

namespace MD_Package.Modifiers
{
    /// <summary>
    /// MD(Mesh Deformation): Modifier Base.
    /// Base modifier class for all the modifier instances. Implement this base class to any script that should behave like a 'mesh-modifier'.
    /// Inherits from MD_MeshBase.
    /// Written by Matej Vanco (2022, updated in 2023).
    /// </summary>
    public abstract class MD_ModifierBase : MD_MeshBase
    {
        /// <summary>
        /// Is the current mesh-modifier initialized? (Read only)
        /// </summary>
        public bool MbIsInitialized { get => _mbIsInitialized; private set => _mbIsInitialized = value;  }
        [SerializeField] private bool _mbIsInitialized;

        // Threading
        /// <summary>
        /// Does the current mesh-modifier support multithreading features? (Read only)
        /// </summary>
        public bool MbMultithreadedModifier { get => _mbMultithreadedModifier; private set => _mbMultithreadedModifier = value; }
        [SerializeField] private bool _mbMultithreadedModifier;
        public Thread ThreadInstance { get; private set; }
        public ManualResetEvent ThreadEvent { get; protected set; }
        private bool ThreadWasRunning = false;

        public bool MbUseModifierMeshFeatures = true;

        // Mesh data declaration
        [Serializable]
        /// <summary>
        /// Mesh data data structure
        /// </summary>
        public struct MbMeshData
        {
            public Vector3[] vertices;
            public int[] triangles;
            public Vector3[] normals;
            public Color[] colors;
            public Vector2[] uv;
            public Vector2[] uv2;
            public Vector2[] uv3;

            /// <summary>
            /// Mesh data constructor - copies included array parameters to the current body (not referencing!)
            /// </summary>
            public MbMeshData(Vector3[] verts, int[] tris, Vector3[] norms, Color[] col, Vector2[] u, Vector2[] u2, Vector2[] u3)
            {
                vertices = new Vector3[verts.Length];
                Array.Copy(verts, vertices, vertices.Length);
                triangles = new int[tris.Length];
                Array.Copy(tris, triangles, triangles.Length);
                normals = new Vector3[norms.Length];
                Array.Copy(norms, normals, normals.Length);
                colors = new Color[col.Length];
                Array.Copy(col, colors, colors.Length);
                uv = new Vector2[u.Length];
                Array.Copy(u, uv, uv.Length);
                uv2 = new Vector2[u2.Length];
                Array.Copy(u2, uv2, uv2.Length);
                uv3 = new Vector2[u3.Length];
                Array.Copy(u3, uv3, uv3.Length);
            }

            /// <summary>
            /// Is the current MeshData structure initialized?
            /// </summary>
            public bool MbDataInitialized()
            {
                return vertices != null && vertices.Length > 0;
            }
        }
        public MbMeshData MbInitialMeshData { get => _mbInitialMeshData; private set => _mbInitialMeshData = value; }
        [SerializeField] private MbMeshData _mbInitialMeshData;
        public MbMeshData MbBackupMeshData { get => _mbBackupVertices; private set => _mbBackupVertices = value; }
        [SerializeField] private MbMeshData _mbBackupVertices;
        public MbMeshData MbWorkingMeshData { get => _mbWorkingVertices; private set => _mbWorkingVertices = value; }
        [SerializeField] private MbMeshData _mbWorkingVertices;

        /// <summary>
        /// Subscribe to delegate action when mesh is restored
        /// </summary>
        public event Action OnMeshRestored;
        /// <summary>
        /// Subscribe to delegate action when mesh is baked
        /// </summary>
        public event Action OnMeshBaked;
        /// <summary>
        /// Subscribe to delegate action when mesh is subdivided
        /// </summary>
        public event Action OnMeshSubdivided;
        /// <summary>
        /// Subscribe to delegate action when mesh is smoothed
        /// </summary>
        public event Action OnMeshSmoothed;
        /// <summary>
        /// Subscribe to delegate action when new mesh reference is created
        /// </summary>
        public event Action OnNewMeshReferenceCreated;

        public enum MeshReferenceType { GetFromPreferences, CreateNewReference, KeepReference };
        /// <summary>
        /// Create new modifier with a specific type
        /// </summary>
        /// <typeparam name="T">Modifier type</typeparam>
        /// <param name="entry">Sender object</param>
        /// <param name="meshReferenceType">Mesh Reference Type</param>
        /// <returns>Returns initialized modifier</returns>
        public static T CreateModifier<T>(GameObject entry, MeshReferenceType meshReferenceType) where T : MD_ModifierBase
        {
            T m = entry.AddComponent<T>();
            m.MDModifier_InitializeBase(meshReferenceType);
            return m;
        }

        #region MD_MeshBase implementations

        /// <summary>
        /// MD_MeshBase implementation - Update current mesh with working vertices (if possible)
        /// </summary>
        public override void MDMeshBase_UpdateMesh()
        {
            if (!MbWorkingMeshData.MbDataInitialized())
                return;
            MbMeshFilter.sharedMesh.vertices = MbWorkingMeshData.vertices;
        }

        /// <summary>
        /// MD_MeshBase implementation - Process certain calculations on the mesh
        /// </summary>
        public override void MDMeshBase_ProcessCalculations()
        {
            MDModifier_ProcessModifier();
        }

        #endregion

        #region Essential initializations

        /// <summary>
        /// Base modifier initialization - is required to invoke for every class that inherits from the ModifierBase
        /// </summary>
        /// <param name="meshReferenceType">What should happen with the initial mesh reference? Creating a new mesh reference is recommended, so you don't lose original data</param>
        /// <param name="forceInitialization">Force initialization in case if the modifier was already initialized</param>
        /// <param name="affectUpdateEveryFrameField">Affect 'Update every frame' field? This field may get disabled if vertex count is exceeded</param>
        protected virtual void MDModifier_InitializeBase(MeshReferenceType meshReferenceType = MeshReferenceType.GetFromPreferences, bool forceInitialization = false, bool affectUpdateEveryFrameField = true)
        {
            if(MbIsInitialized && !forceInitialization)
            {
                MD_Debug.Debug(this, "The modifier is already initialized. If you would like to initialize this modifier again, call this method with 'force-call' parameter");
                return;
            }

            // Init base
            MDMeshBase_InitializeBase(affectUpdateEveryFrameField);

            // Prevent from adding multiple modifiers/ geometries at once
            if(!MD_Utilities.MD_Specifics.PrepareMeshDeformationModifier(this, MbMeshFilter, new Type[3] { typeof(MD_ModifierBase), typeof(MD_GeometryBase), typeof(MD_MeshProEditor) }, true, meshReferenceType))
            {
                MbUpdateEveryFrame = false;
                MDMeshBase_DestroySelf();
                return;
            }

            if (this is IMDThreadingSupport imdts)
            {
                MbMultithreadedModifier = true;
                imdts.ThreadUseMultithreading = MD_GlobalPreferences.VertexLimit < MbMeshFilter.sharedMesh.vertexCount;
            }
            else MbMultithreadedModifier = false;

            MbIsInitialized = true;
        }

        /// <summary>
        /// Base mesh data initialization & caching - this will initialize essential mesh data (vertices, triangles, uvs etc)
        /// </summary>
        /// <param name="initialMeshData">Include initial mesh data (read-only)</param>
        /// <param name="backupMeshData">Include backup mesh data (backup for working mesh data)</param>
        /// <param name="workingMeshData">Include working mesh data (dynamically used while editing)</param>
        protected virtual void MDModifier_InitializeMeshData(bool initialMeshData = true, bool backupMeshData = true, bool workingMeshData = true)
        {
            if (!MDMeshBase_CheckForMeshFilter(checkMeshFilterMesh:true))
                return;
            Mesh sm = MbMeshFilter.sharedMesh;

            if (initialMeshData)
                MbInitialMeshData = new MbMeshData(sm.vertices, sm.triangles, sm.normals, sm.colors, sm.uv, sm.uv2, sm.uv3);
            if (backupMeshData)
                MbBackupMeshData = new MbMeshData(sm.vertices, sm.triangles, sm.normals, sm.colors, sm.uv, sm.uv2, sm.uv3);
            if (workingMeshData)
                MbWorkingMeshData = new MbMeshData(sm.vertices, sm.triangles, sm.normals, sm.colors, sm.uv, sm.uv2, sm.uv3);
        }

        /// <summary>
        /// Deinitialize object when destroyed
        /// </summary>
        protected virtual void OnDestroy()
        {
            MbInitialMeshData = default;
            MbBackupMeshData = default;
            MbWorkingMeshData = default;
            OnNewMeshReferenceCreated = 
                OnMeshSubdivided =
                OnMeshSmoothed =
                OnMeshBaked = 
                OnMeshRestored = 
                OnMeshRestored = null;
            MDModifierThreading_DestroyThread();
        }

        protected virtual void OnDisable()
        {
            if (MbMultithreadedModifier && ThreadInstance != null && ThreadInstance.IsAlive)
                ThreadWasRunning = true;
            MDModifierThreading_DestroyThread();
        }

        protected virtual void OnEnable()
        {
            if (ThreadWasRunning)
                MDModifierThreading_StartThread();
        }

        #endregion

        #region Required modifier implementations

        /// <summary>
        /// Modifier implementation for main processing - use this method for processing the specific modifier
        /// </summary>
        public abstract void MDModifier_ProcessModifier();

        #endregion

        #region Cross-modifier mesh features

        /// <summary>
        /// Restore current mesh from the initial mesh data
        /// </summary>
        public void MDModifier_RestoreMesh()
        {
            if(!MbUseModifierMeshFeatures)
            {
                MD_Debug.Debug(this, "The modifier is not allowed to use Modifier Mesh Features!", MD_Debug.DebugType.Error);
                return;
            }

            if(!MbInitialMeshData.MbDataInitialized())
            {
                MD_Debug.Debug(this, "Initial vertices are not initialized or equal to null", MD_Debug.DebugType.Error);
                return;
            }

            OnMeshRestored?.Invoke();

            MbMeshFilter.sharedMesh.triangles = MbInitialMeshData.triangles;
            MbMeshFilter.sharedMesh.vertices = MbInitialMeshData.vertices;
            MbMeshFilter.sharedMesh.normals = MbInitialMeshData.normals;
            MbMeshFilter.sharedMesh.colors = MbInitialMeshData.colors;
            MbMeshFilter.sharedMesh.uv = MbInitialMeshData.uv;
            MbMeshFilter.sharedMesh.uv2 = MbInitialMeshData.uv2;
            MbMeshFilter.sharedMesh.uv3 = MbInitialMeshData.uv3;

            MDModifier_InitializeMeshData(false, MbBackupMeshData.MbDataInitialized());
            MDMeshBase_RecalculateMesh();
        }

        /// <summary>
        /// Increase mesh vertex count
        /// </summary>
        /// <param name="level">Count of subdivision levels</param>
        public void MDModifier_SubdivideMesh(int level = 2)
        {
            if (!MbUseModifierMeshFeatures)
            {
                MD_Debug.Debug(this, "The modifier is not allowed to use Modifier Mesh Features!", MD_Debug.DebugType.Error);
                return;
            }

            if (!MbWorkingMeshData.MbDataInitialized())
            {
                MD_Debug.Debug(this, "Working vertices are not initialized or equal to null", MD_Debug.DebugType.Error);
                return;
            }
            level = Mathf.Max(2, Mathf.Min(24, level));
            if (MD_Utilities.MD_Specifics.CheckVertexCountLimit(MbWorkingMeshData.vertices.Length * level, gameObject))
            {
                OnMeshSubdivided?.Invoke();

                MD_Utilities.Mesh_Subdivision.Subdivide(MbMeshFilter.sharedMesh, level);
                MDModifier_InitializeMeshData(false, MbBackupMeshData.MbDataInitialized());
                MDMeshBase_RecalculateMesh();
            }
        }

        /// <summary>
        /// Make the mesh edges rounded
        /// </summary>
        /// <param name="level">Count of subdivision levels</param>
        public void MDModifier_SmoothMesh(float intensity = 0.5f)
        {
            if (!MbUseModifierMeshFeatures)
            {
                MD_Debug.Debug(this, "The modifier is not allowed to use Modifier Mesh Features!", MD_Debug.DebugType.Error);
                return;
            }

            if (!MbWorkingMeshData.MbDataInitialized())
            {
                MD_Debug.Debug(this, "Working vertices are not initialized or equal to null", MD_Debug.DebugType.Error);
                return;
            }

            intensity = Mathf.Abs(intensity);

            OnMeshSmoothed?.Invoke();

            var smoothVerts = MbWorkingMeshData;
            smoothVerts.vertices = MD_Utilities.Smoothing_HCFilter.HCFilter(MbWorkingMeshData.vertices, MbWorkingMeshData.triangles, 0.0f, intensity);
            MbMeshFilter.sharedMesh.vertices = smoothVerts.vertices;
            MDModifier_InitializeMeshData(false, MbBackupMeshData.MbDataInitialized());
            MDMeshBase_RecalculateMesh();
        }

        /// <summary>
        /// Bake current mesh - initial mesh data will be overrided by the current mesh data
        /// </summary>
        public void MDModifier_BakeMesh(bool forceInitialMeshData = false, bool forceBackupMeshData = false, bool forceWorkingMeshData = false)
        {
            if (!MbUseModifierMeshFeatures)
            {
                MD_Debug.Debug(this, "The modifier is not allowed to use Modifier Mesh Features!", MD_Debug.DebugType.Error);
                return;
            }

            bool mdI = MbInitialMeshData.MbDataInitialized() || forceInitialMeshData;
            bool mdB = MbBackupMeshData.MbDataInitialized() || forceBackupMeshData;
            bool mdW = MbWorkingMeshData.MbDataInitialized() || forceWorkingMeshData;
            MDModifier_InitializeMeshData(mdI, mdB, mdW);
            OnMeshBaked?.Invoke();
        }

        /// <summary>
        /// Create a brand new mesh reference
        /// </summary>
        /// <param name="meshName">Leave this field empty if the future mesh name remains the same</param>
        public void MDModifier_CreateNewMeshReference(string meshName = "")
        {
            if (!MbUseModifierMeshFeatures)
            {
                MD_Debug.Debug(this, "The modifier is not allowed to use Modifier Mesh Features!", MD_Debug.DebugType.Error);
                return;
            }

            string mName = string.IsNullOrEmpty(meshName) ? MbMeshFilter.sharedMesh.name : meshName;
            MbMeshFilter.sharedMesh = MD_Utilities.MD_Specifics.CreateNewMeshReference(MbMeshFilter.sharedMesh);
            MbMeshFilter.sharedMesh.name = mName;
            MDModifier_InitializeMeshData(true, MbBackupMeshData.MbDataInitialized());
            OnNewMeshReferenceCreated?.Invoke();
        }

        #endregion

        #region Threading features

        /// <summary>
        /// Check for the thread initialization and its existence
        /// </summary>
        /// <param name="checkForRunningThread">Is the current thread running?</param>
        /// <returns>Returns true if all is good</returns>
        private bool MDModifierThreading_CheckInit(bool checkForRunningThread = false)
        {
            if(!gameObject.activeSelf || !gameObject.activeInHierarchy)
            {
                MD_Debug.Debug(this, "The gameObject is not active in the hierarchy", MD_Debug.DebugType.Error);
                return false;
            }
            if (!MbIsInitialized)
            {
                MD_Debug.Debug(this, "The modifier is not initialized", MD_Debug.DebugType.Error);
                return false;
            }
            if (!MbMultithreadedModifier)
            {
                MD_Debug.Debug(this, "The modifier does not support multithreading features", MD_Debug.DebugType.Error);
                return false;
            }
            if(checkForRunningThread)
            {
                if (ThreadInstance != null && ThreadInstance.IsAlive)
                {
                    MD_Debug.Debug(this, "The modifier thread is already alive & running", MD_Debug.DebugType.Information);
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Start current thread and initialize the thread event. Enter custom thread name
        /// </summary>
        public virtual void MDModifierThreading_StartThread(string threadName = "DefaultMDThread")
        {
            if (!MDModifierThreading_CheckInit(true))
                return;

            IMDThreadingSupport th = (IMDThreadingSupport)this;
            ThreadInstance = new Thread(th.MDThreading_ProcessThreadWorker);
            ThreadInstance.Name = !string.IsNullOrEmpty(threadName) ? threadName : threadName + UnityEngine.Random.Range(0,99999).ToString();
            ThreadEvent = new ManualResetEvent(true);
            ThreadInstance.Start();
            ThreadWasRunning = false;
            MbUpdateEveryFrame = true;
        }

        /// <summary>
        /// Stop current thread and destroy all the events related to this thread
        /// </summary>
        public virtual void MDModifierThreading_StopThread()
        {
            if (!MDModifierThreading_CheckInit())
                return;

            MDModifierThreading_DestroyThread();
        }

        private void MDModifierThreading_DestroyThread()
        {
            if (!MbMultithreadedModifier)
                return;
            if (ThreadInstance != null && ThreadInstance.IsAlive)
                ThreadInstance.Abort();
            ThreadInstance = null;
            ThreadEvent?.Reset();
            ThreadEvent = null;
        }

        #endregion

        /// <summary>
        /// Interface for modifiers that support multithreading. Implement this interface to an inherited modifier of the ModifierBase
        /// </summary>
        public interface IMDThreadingSupport
        {
            public bool ThreadUseMultithreading { get; set; }
            public bool ThreadEditorThreadSupported { get; }
            public int ThreadSleep { get; set; }
            public bool ThreadIsDone { get; }

            public void MDThreading_ProcessThreadWorker();
        }
    }
}

#if UNITY_EDITOR
namespace MD_Package_Editor
{
    /// <summary>
    /// Base editor for the ModifierBase instances - all the class instances that inherit from the MD_ModifierBase MUST have the Unity-editor implemented!
    /// </summary>
    [CustomEditor(typeof(MD_ModifierBase), true)]
    public abstract class MD_ModifierBase_Editor : MD_MeshBase_Editor
    {
        protected MD_ModifierBase mModifierBase;
        private bool modifierFoldout = false;

        private Vector2 scrollModifiers;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (!mModifierBase)
            {
                DrawDefaultInspector();
                return;
            }

            MDE_v();
            if (mModifierBase.MbUseModifierMeshFeatures)
            {
                modifierFoldout = MDE_f(modifierFoldout, "Modifier Settings");
                if (modifierFoldout)
                {
                    scrollModifiers = GUILayout.BeginScrollView(scrollModifiers);
                    MDE_h();
                    if (MDE_b("Restore Mesh", "Restore current mesh from the initial mesh data"))
                    {
                        if (MDE_dd("Question", "Are you sure to restore the current mesh from the initial mesh data? There is no way back", "Yes", "No"))
                            mModifierBase.MDModifier_RestoreMesh();
                    }
                    if (MDE_b("Bake Mesh", "Bake current mesh - initial mesh data will be overrided by the current mesh data"))
                    {
                        if (MDE_dd("Question", "You are about to bake the current mesh data. The initial mesh data will be overrided by the current mesh data. Are you sure to continue? There is no way back", "Yes", "No"))
                            mModifierBase.MDModifier_BakeMesh();
                    }
                    if (MDE_b("New Reference", "Create a brand new mesh reference"))
                    {
                        if (MDE_dd("Question", "You are about to create a brand new mesh reference of the current mesh. Are you sure to continue? There is no way back", "Yes", "No"))
                            mModifierBase.MDModifier_CreateNewMeshReference();
                    }
                    if (MDE_b("Smooth Mesh", "Round/smooth mesh"))
                    {
                        if (MDE_dd("Question", "You are about to smooth the current mesh vertices. Are you sure to continue? There is no way back. You can restore the initial mesh afterwards", "Yes", "No"))
                            mModifierBase.MDModifier_SmoothMesh();
                    }
                    if (MDE_b("Subdivide Mesh", "Increase mesh vertex count"))
                    {
                        if (MDE_dd("Question", "You are about to subdivide the current mesh vertices. Are you sure to continue? There is no way back. You can restore the initial mesh afterwards", "Yes", "No"))
                            mModifierBase.MDModifier_SubdivideMesh();
                    }
                    MDE_he();
                    GUILayout.EndScrollView();
                    bool dI = mModifierBase.MbInitialMeshData.MbDataInitialized();
                    bool dB = mModifierBase.MbBackupMeshData.MbDataInitialized();
                    bool dW = mModifierBase.MbWorkingMeshData.MbDataInitialized();
                    MDE_h();
                    MDE_l("Initial Vertices: " + (dI ? mModifierBase.MbInitialMeshData.vertices.Length.ToString() : "unused"));
                    MDE_l("Backup Vertices: " + (dB ? mModifierBase.MbBackupMeshData.vertices.Length.ToString() : "unused"));
                    MDE_l("Working Vertices: " + (dW ? mModifierBase.MbWorkingMeshData.vertices.Length.ToString() : "unused"));
                    MDE_he();
                }
            }
            if(mModifierBase.MbMultithreadedModifier)
            {
                MDE_s();
                MDE_l("Multithreading Settings", true);
                MDE_v();
                if (mModifierBase.MbWorkingMeshData.vertices.Length > MD_GlobalPreferences.VertexLimit)
                    MDE_hb("The multithreading is recommended as the mesh has more than " + MD_GlobalPreferences.VertexLimit.ToString() + " vertices", MessageType.Warning);
                MDE_DrawProperty("_threadUseMultithreading", "Use Multithreading");
                if (((MD_ModifierBase.IMDThreadingSupport)mModifierBase).ThreadUseMultithreading)
                {
                    MDE_DrawProperty("_threadSleep", "Thread Sleep", "Thread sleeping delay (in miliseconds; The lower the value is, the faster the thread processing will be; but more performance it may take)");
                    if (((MD_ModifierBase.IMDThreadingSupport)mModifierBase).ThreadEditorThreadSupported)
                    {
                        MDE_h();
                        if (mModifierBase.ThreadInstance != null && mModifierBase.ThreadInstance.IsAlive)
                        {
                            GUI.backgroundColor = Color.red;
                            if (MDE_b("Stop Editor Thread"))
                                mModifierBase.MDModifierThreading_StopThread();
                            GUI.backgroundColor = Color.white;
                        }
                        else if (MDE_b("Start Editor Thread"))
                            mModifierBase.MDModifierThreading_StartThread();
                        MDE_he();
                        MDE_hb("If you are going to edit the mesh in the Editor, it's required to manage editor thread manually. Press 'Start' to start the separate editor thread. Press 'Stop' to stop the separate editor thread.");
                    }
                }
                MDE_ve();
            }
            MDE_ve();
            MDE_s();
        }
    }
}
#endif