using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;

using MD_Package.Utilities;

#if UNITY_EDITOR
using UnityEditor;

using MD_Package;
using MD_Package.Modifiers;
#endif

namespace MD_Package.Modifiers
{
    /// <summary>
    /// MDM(Mesh Deformation Modifier): Mesh Morpher.
    /// Blend mesh between list of stored & captured shapes.
    /// Written by Matej Vanco (2017, updated in 2023).
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    [AddComponentMenu(MD_Debug.ORGANISATION + MD_Debug.PACKAGENAME + "Modifiers/Morpher")]
    public sealed class MDM_Morpher : MD_ModifierBase, MD_ModifierBase.IMDThreadingSupport
    {
        // IMDThreading implementation
        public bool ThreadUseMultithreading { get => _threadUseMultithreading; set => _threadUseMultithreading = value; }
        [SerializeField] private bool _threadUseMultithreading;
        public bool ThreadEditorThreadSupported { get => _threadEditorThreadSupported; private set => _threadEditorThreadSupported = value; }
        [SerializeField] private bool _threadEditorThreadSupported;
        public bool ThreadIsDone { get; private set; }
        public int ThreadSleep { get => _threadSleep; set => _threadSleep = value; }
        [SerializeField, Range(5, 60)] private int _threadSleep = 20;

        // Modifier fields
        public bool enableInterpolation = false;
        public float interpolationSpeed = 0.5f;

        [Range(0.0f, 1.0f)] public float blendValue = 0;

        public Mesh[] targetBlendShapes;
        public int targetBlendShapeIndex = 0;

        public bool resetVertexStateOnChange = true;

        [Serializable]
        public struct RegisteredBlendShapes
        {
            public List<Vector3> vertices;
            public List<int> indexes;
        }
        public List<RegisteredBlendShapes> registeredBlendShapes = new List<RegisteredBlendShapes>();
        [field: SerializeField] public int PreviousRegisteredBlends { get; private set; }

        /// <summary>
        /// When the component is added to an object (called once)
        /// </summary>
        private void Reset()
        {
            if (MbIsInitialized)
                return;
            MDModifier_InitializeBase();
        }

        #region Base overrides

        /// <summary>
        /// Base modifier initialization
        /// </summary>
        protected override void MDModifier_InitializeBase(MeshReferenceType meshReferenceType = MeshReferenceType.GetFromPreferences, bool forceInitialization = false, bool affectUpdateEveryFrameField = true)
        {
            base.MDModifier_InitializeBase(meshReferenceType, forceInitialization, affectUpdateEveryFrameField);

            MDModifier_InitializeMeshData();

            MbUseModifierMeshFeatures = false;
            ThreadEditorThreadSupported = true;
        }

        /// <summary>
        /// Process the Morpher base update function
        /// </summary>
        public override void MDModifier_ProcessModifier()
        {
            if (!MbIsInitialized)
                return;

            if (ThreadUseMultithreading)
            {
                if (ThreadIsDone) Morpher_UpdateMesh();
                else ThreadEvent?.Set();
                return;
            }

            Morpher_ProcessMorph();
            Morpher_UpdateMesh();
        }

        #endregion

        private void Start()
        {
            if (Application.isPlaying && ThreadUseMultithreading)
                MDModifierThreading_StartThread();
        }

        private void Update()
        {
            if (MbUpdateEveryFrame)
                MDModifier_ProcessModifier();
        }

        #region Morpher essentials

        private void Morpher_ProcessMorph()
        {
            if (registeredBlendShapes.Count > 0 && (targetBlendShapeIndex >= 0 && targetBlendShapeIndex < registeredBlendShapes.Count))
            {
                for (int i = 0; i < registeredBlendShapes[targetBlendShapeIndex].vertices.Count; i++)
                {
                    if (!enableInterpolation)
                        MbWorkingMeshData.vertices[registeredBlendShapes[targetBlendShapeIndex].indexes[i]] = MD_Utilities.Math3D.CustomLerp(MbBackupMeshData.vertices[registeredBlendShapes[targetBlendShapeIndex].indexes[i]], registeredBlendShapes[targetBlendShapeIndex].vertices[i], blendValue);
                    else
                        MbWorkingMeshData.vertices[registeredBlendShapes[targetBlendShapeIndex].indexes[i]] = Vector3.Lerp(MbWorkingMeshData.vertices[registeredBlendShapes[targetBlendShapeIndex].indexes[i]],
                            MD_Utilities.Math3D.CustomLerp(MbBackupMeshData.vertices[registeredBlendShapes[targetBlendShapeIndex].indexes[i]], registeredBlendShapes[targetBlendShapeIndex].vertices[i], blendValue), interpolationSpeed);
                }
            }
        }

        /// <summary>
        /// Update current mesh state (if Update Every Frame is disabled)
        /// </summary>
        public void Morpher_UpdateMesh()
        {
            if (!MbIsInitialized)
                return;
            if (!MbWorkingMeshData.MbDataInitialized())
                return;

            MbMeshFilter.sharedMesh.vertices = MbWorkingMeshData.vertices;
            MDMeshBase_RecalculateMesh();
        }

        /// <summary>
        /// Change current target morph mesh index
        /// </summary>
        public void Morpher_ChangeMeshIndex(int entry)
        {
            if (resetVertexStateOnChange) 
                MbMeshFilter.sharedMesh.vertices = MbBackupMeshData.vertices;
            targetBlendShapeIndex = entry;
        }

        /// <summary>
        /// Set current blend value
        /// </summary>
        public void Morpher_SetBlendValue(Slider entry)
        {
            blendValue = entry.value;
            if (!MbUpdateEveryFrame) MDModifier_ProcessModifier();
        }

        /// <summary>
        /// Set current blend value
        /// </summary>
        public void Morpher_SetBlendValue(float entry)
        {
            blendValue = entry;
            if (!MbUpdateEveryFrame) MDModifier_ProcessModifier();
        }

        /// <summary>
        /// Register assigned blend shapes and create unique list of vertices ready for blending
        /// </summary>
        public void Morpher_RegisterBlendShapes()
        {
            if (registeredBlendShapes == null)
                registeredBlendShapes = new List<RegisteredBlendShapes>();
            else
                registeredBlendShapes.Clear();

            foreach (Mesh m in targetBlendShapes)
            {
                if (m == null)
                {
                    MD_Debug.Debug(this, "One of the target blend shapes in the list is null", MD_Debug.DebugType.Error);
                    return;
                }

                if (MbBackupMeshData.vertices.Length != m.vertices.Length)
                {
                    MD_Debug.Debug(this, "Assigned blend shape (mesh) must have the same vertex count & mesh identity as its original reference. Mesh name:" + m.name, MD_Debug.DebugType.Error);
                    return;
                }

                RegisteredBlendShapes regBlend = new RegisteredBlendShapes()
                {
                    vertices = new List<Vector3>(),
                    indexes = new List<int>()
                };
                for (int i = 0; i < m.vertices.Length; i++)
                {
                    regBlend.vertices.Add(m.vertices[i]);
                    regBlend.indexes.Add(i);
                }
                registeredBlendShapes.Add(regBlend);
            }
            PreviousRegisteredBlends = targetBlendShapes.Length;
        }

        #endregion

        /// <summary>
        /// Main separate thread worker for this modifier
        /// </summary>
        public void MDThreading_ProcessThreadWorker()
        {
            while (true)
            {
                ThreadIsDone = false;
                ThreadEvent?.WaitOne();

                Morpher_ProcessMorph();

                ThreadIsDone = true;
                Thread.Sleep(ThreadSleep);

                ThreadEvent?.Reset();
            }
        }
    }
}

#if UNITY_EDITOR
namespace MD_Package_Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MDM_Morpher))]
    public sealed class MDM_Morpher_Editor : MD_ModifierBase_Editor
    {
        private MDM_Morpher md;

        public override void OnEnable()
        {
            mMeshBase = (MD_MeshBase)target;
            mModifierBase = (MD_ModifierBase)target;
            md = (MDM_Morpher)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            MDE_l("Morpher Modifier", true);
            MDE_s();
            MDE_v();
            MDE_DrawProperty("enableInterpolation", "Enable Interpolation", "Enable smooth transition of vertices");
            if (md.enableInterpolation)
                MDE_DrawProperty("interpolationSpeed", "Interpolation Speed");
            MDE_s(5);
            MDE_DrawProperty("blendValue", "Blend Value", "Blend value between original mesh & target mesh");
            MDE_v();
            MDE_DrawProperty("targetBlendShapes", "Target Blend Shapes", "List of target blend shapes/ meshes", true);
            MDE_ve();
            MDE_v();
            MDE_DrawProperty("targetBlendShapeIndex", "Selected Blend Index");
            MDE_ve();
            MDE_DrawProperty("resetVertexStateOnChange", "Restart Vertex State", "Restart mesh state after changing index");
            MDE_s(5);
            MDE_v();
            bool nonReg = md.targetBlendShapes != null && md.PreviousRegisteredBlends != md.targetBlendShapes.Length;
            if (nonReg) GUI.backgroundColor = Color.red;
            if (MDE_b("Register Target Morphs"))
                md.Morpher_RegisterBlendShapes();
            if (nonReg)
            {
                MDE_hb("^ Please register target meshes ^", MessageType.Warning);
                GUI.backgroundColor = Color.white;
            }
            MDE_ve();
            MDE_ve();
            MDE_AddMeshColliderRefresher(md.gameObject);
            MDE_BackToMeshEditor(md);
        }
    }
}
#endif
