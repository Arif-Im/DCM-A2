using UnityEngine;
using System.Threading;

#if UNITY_EDITOR
using UnityEditor;

using MD_Package;
using MD_Package.Modifiers;
#endif

namespace MD_Package.Modifiers
{
    /// <summary>
    /// MDM(Mesh Deformation Modifier): Mesh Effector.
    /// Deform mesh by the specific weight values & sphere-based effector attributes.
    /// Written by Matej Vanco (2021, updated in 2023).
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    [AddComponentMenu(MD_Debug.ORGANISATION + MD_Debug.PACKAGENAME + "Modifiers/Mesh Effector")]
    public sealed class MDM_MeshEffector : MD_ModifierBase, MD_ModifierBase.IMDThreadingSupport
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
        public enum EffectorType { OnePointed, TwoPointed, ThreePointed, FourPointed};
        public EffectorType effectorType = EffectorType.OnePointed;

        public Transform weightNode0;
        public Transform weightNode1;
        public Transform weightNode2;
        public Transform weightNode3;

        private Vector3? node0;
        private Vector3? node1;
        private Vector3? node2;
        private Vector3? node3;

        [Range(0.0f, 1.0f)] public float weight = 0.5f;
        public float weightMultiplier = 1.0f;
        public float weightDensity = 3.0f;
        [Range(0.0f, 1.0f)] public float weightEffectorA = 0.5f;
        [Range(0.0f, 1.0f)] public float weightEffectorB = 0.5f;
        [Range(0.0f, 1.0f)] public float weightEffectorC = 0.5f;

        public bool clampEffector = false;
        public float clampVectorValue = 1.0f;
        public float minClamp = -5.0f;
        public float maxClamp = 5.0f;

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

            // This modifier supports using the multithreading right in the editor
            ThreadEditorThreadSupported = true;
        }

        /// <summary>
        /// Process the Mesh Effector base update function (use 'Effector_UpdateMesh' method for more customized setting)
        /// </summary>
        public override void MDModifier_ProcessModifier()
        {
            if (!MbIsInitialized)
                return;
            if (!MbWorkingMeshData.MbDataInitialized())
                return;

            if (weightNode0) node0 = GetPosToLocalObject(weightNode0); else node0 = null;
            if (weightNode1) node1 = GetPosToLocalObject(weightNode1); else node1 = null;
            if (weightNode2) node2 = GetPosToLocalObject(weightNode2); else node2 = null;
            if (weightNode3) node3 = GetPosToLocalObject(weightNode3); else node3 = null;

            if (ThreadUseMultithreading)
            {
                if (ThreadIsDone) Effector_UpdateMesh();
                else ThreadEvent?.Set();
                return;
            }

            Effector_ProcessEffector(node0, node1, node2, node3);
            Effector_UpdateMesh();
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

        #region Mesh Effector essentials

        private void Effector_ProcessEffector(Vector3? node0, Vector3? node1, Vector3? node2, Vector3? node3)
        {
            switch (effectorType)
            {
                case EffectorType.OnePointed:
                    if (node0 != null)
                        for (int i = 0; i < MbWorkingMeshData.vertices.Length; i++)
                            MbWorkingMeshData.vertices[i] = VecInterpolation(MbBackupMeshData.vertices[i], (Vector3)node0, CalculateEffector(MbBackupMeshData.vertices[i], (Vector3)node0));
                    break;

                case EffectorType.TwoPointed:
                    if (node0 != null && node1 != null)
                        for (int i = 0; i < MbWorkingMeshData.vertices.Length; i++)
                            MbWorkingMeshData.vertices[i] = InterpolationOfTwoPointed(MbBackupMeshData.vertices[i], (Vector3)node0, (Vector3)node1);
                    break;

                case EffectorType.ThreePointed:
                    if (node0 != null && node1 != null && node2 != null)
                        for (int i = 0; i < MbWorkingMeshData.vertices.Length; i++)
                            MbWorkingMeshData.vertices[i] = InterpolationOfThreePointed(MbBackupMeshData.vertices[i], (Vector3)node0, (Vector3)node1, (Vector3)node2);
                    break;

                case EffectorType.FourPointed:
                    if (node0 != null && node1 != null && node2 != null && node3 != null)
                        for (int i = 0; i < MbWorkingMeshData.vertices.Length; i++)
                            MbWorkingMeshData.vertices[i] = InterpolationOfFourPointed(MbBackupMeshData.vertices[i], (Vector3)node0, (Vector3)node1, (Vector3)node2, (Vector3)node3);
                    break;
            }
        }

        /// <summary>
        /// Update current mesh state (if Update Every Frame is disabled)
        /// </summary>
        public void Effector_UpdateMesh()
        {
            if (!MbIsInitialized)
                return;
            if (!MbWorkingMeshData.MbDataInitialized())
                return;

            MbMeshFilter.sharedMesh.vertices = MbWorkingMeshData.vertices;
            MDMeshBase_RecalculateMesh();
        }

        /// <summary>
        /// Process four-pointed mesh effector
        /// </summary>
        /// <param name="p">Vertex point (Local Space)</param>
        /// <param name="n0">Node 0</param>
        /// <param name="n1">Node 1</param>
        /// <param name="n2">Node 2</param>
        /// <param name="n3">Node 3</param>
        /// <returns>Returns calculated vertice in local space between four nodes</returns>
        private Vector3 InterpolationOfFourPointed(Vector3 p, Vector3 n0, Vector3 n1, Vector3 n2, Vector3 n3)
        {
            return VecInterpolation(
                VecInterpolation(
                    VecInterpolation(
                        VecInterpolation(p, n0, CalculateEffector(p, n0)),
                        VecInterpolation(p, n1, CalculateEffector(p, n1)),
                        weightEffectorA),
                    VecInterpolation(
                        VecInterpolation(p, n0, CalculateEffector(p, n0)),
                        VecInterpolation(p, n2, CalculateEffector(p, n2)),
                        weightEffectorA), 
                    weightEffectorB),
                 VecInterpolation(
                    VecInterpolation(
                        VecInterpolation(p, n0, CalculateEffector(p, n0)),
                        VecInterpolation(p, n3, CalculateEffector(p, n3)),
                        weightEffectorA),
                    VecInterpolation(
                        VecInterpolation(p, n0, CalculateEffector(p, n0)),
                        VecInterpolation(p, n3, CalculateEffector(p, n3)),
                        weightEffectorA),
                    weightEffectorB), 
                weightEffectorC);
        }

        /// <summary>
        /// Process three-pointed mesh effector
        /// </summary>
        /// <param name="p">Vertex point (Local Space)</param>
        /// <param name="n0">Node 0</param>
        /// <param name="n1">Node 1</param>
        /// <param name="n2">Node 2</param>
        /// <returns>Returns calculated vertice in local space between three nodes</returns>
        private Vector3 InterpolationOfThreePointed(Vector3 p, Vector3 n0, Vector3 n1, Vector3 n2)
        {
            return VecInterpolation(VecInterpolation(
                VecInterpolation(p, n0, CalculateEffector(p, n0)),
                VecInterpolation(p, n1, CalculateEffector(p, n1)), 
                weightEffectorA), 
                VecInterpolation(
                VecInterpolation(p, n0, CalculateEffector(p, n0)),
                VecInterpolation(p, n2, CalculateEffector(p, n2)),
                weightEffectorA), weightEffectorB);
        }

        /// <summary>
        /// Process two-pointed mesh effector
        /// </summary>
        /// <param name="p">Vertex point (Local Space)</param>
        /// <param name="n0">Node 0</param>
        /// <param name="n1">Node 1</param>
        /// <returns>Returns calculated vertice in local space between two nodes</returns>
        private Vector3 InterpolationOfTwoPointed(Vector3 p, Vector3 n0, Vector3 n1)
        {
            return VecInterpolation(
                VecInterpolation(p, n0, CalculateEffector(p, n0)),
                VecInterpolation(p, n1, CalculateEffector(p, n1)),
                weightEffectorA);
        }

        /// <summary>
        /// Base vector interpolation
        /// </summary>
        /// <param name="a">Vector A</param>
        /// <param name="b">Vector B</param>
        /// <param name="t">T value (0-1)</param>
        /// <returns>Returns linearly-interpolated vector in range of 0-1 with defined clamp</returns>
        private Vector3 VecInterpolation(Vector3 a, Vector3 b, float t)
        {
            Vector3 final = t * (b - a) + a;
            return clampEffector ? Vector3.ClampMagnitude(final, clampVectorValue) : final;
        }

        /// <summary>
        /// Get local position of the world object
        /// </summary>
        /// <param name="obj">Object in world space</param>
        /// <returns>Returns vector converted to this local object space</returns>
        private Vector3 GetPosToLocalObject(Transform obj)
        {
            return transform.InverseTransformPoint(obj.position);
        }

        /// <summary>
        /// Calculate T value of the specific vertice effector
        /// </summary>
        /// <param name="vertice">Vectice input (Local Space)</param>
        /// <param name="node">Node input (Local Space)</param>
        /// <returns>Returns calculated effector T value with defined clamp</returns>
        private float CalculateEffector(Vector3 vertice, Vector3 node)
        {
            float formula = ((1.0f / Mathf.Pow(Vector3.Distance(vertice, node), weightDensity)) * weightMultiplier) * weight;
            return clampEffector ? Mathf.Clamp(formula, minClamp, maxClamp) : formula;
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

                Effector_ProcessEffector(node0, node1, node2, node3);

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
    [CustomEditor(typeof(MDM_MeshEffector))]
    public sealed class MDM_MeshEffectorEditor : MD_ModifierBase_Editor
    {
        private MDM_MeshEffector md;

        public override void OnEnable()
        {
            mMeshBase = (MD_MeshBase)target;
            mModifierBase = (MD_ModifierBase)target;
            md = (MDM_MeshEffector)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            MDE_l("Mesh Effector Modifier", true);
            MDE_v();
            MDE_DrawProperty("effectorType", "Effector Type");
            MDE_DrawProperty("weightNode0", "Weight Node 0");
            if (md.effectorType == MDM_MeshEffector.EffectorType.TwoPointed)
                MDE_DrawProperty("weightNode1", "Weight Node 1");
            else if(md.effectorType == MDM_MeshEffector.EffectorType.ThreePointed)
            {
                MDE_DrawProperty("weightNode1", "Weight Node 1");
                MDE_DrawProperty("weightNode2", "Weight Node 2");
            }
            else if (md.effectorType == MDM_MeshEffector.EffectorType.FourPointed)
            {
                MDE_DrawProperty("weightNode1", "Weight Node 1");
                MDE_DrawProperty("weightNode2", "Weight Node 2");
                MDE_DrawProperty("weightNode3", "Weight Node 3");
            }
            MDE_ve();
            MDE_s();
            MDE_l("Effector Parameters", true);
            MDE_v();
            MDE_DrawProperty("weight", "Weight","Total weight");
            MDE_DrawProperty("weightMultiplier", "Weight Multiplier", "Additional weight multiplier");
            MDE_DrawProperty("weightDensity", "Weight Density");
            if(md.effectorType == MDM_MeshEffector.EffectorType.TwoPointed)
                MDE_DrawProperty("weightEffectorA", "Weight Effector", "Effector value between two weight nodes");
            else if(md.effectorType == MDM_MeshEffector.EffectorType.ThreePointed)
            {
                MDE_DrawProperty("weightEffectorA", "Weight Effector A", "Effector value between two weight nodes");
                MDE_DrawProperty("weightEffectorB", "Weight Effector B", "Effector value between three weight nodes");
            }
            else if (md.effectorType == MDM_MeshEffector.EffectorType.FourPointed)
            {
                MDE_DrawProperty("weightEffectorA", "Weight Effector A", "Effector value between two weight nodes");
                MDE_DrawProperty("weightEffectorB", "Weight Effector B", "Effector value between three weight nodes");
                MDE_DrawProperty("weightEffectorC", "Weight Effector C", "Effector value between Four weight nodes");
            }
            MDE_s();
            MDE_v();
            MDE_DrawProperty("clampEffector", "Clamp Effector", "If enabled, the affected vertices will be limited by the specific value below");
            if (md.clampEffector)
            {
                MDE_plus();
                MDE_DrawProperty("clampVectorValue", "Clamp Vector Magnitude");
                MDE_DrawProperty("minClamp", "Clamp Min");
                MDE_DrawProperty("maxClamp", "Clamp Max");
                MDE_minus();
            }
            MDE_ve();
            MDE_ve();            
            MDE_AddMeshColliderRefresher(md.gameObject);
            MDE_BackToMeshEditor(md);
        }
    }
}
#endif