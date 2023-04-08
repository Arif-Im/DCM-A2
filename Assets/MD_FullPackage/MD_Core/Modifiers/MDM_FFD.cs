using System.Collections.Generic;
using UnityEngine;

using MD_Package.Geometry;
using MD_Package.Utilities;

#if UNITY_EDITOR
using UnityEditor;

using MD_Package;
using MD_Package.Modifiers;
#endif

namespace MD_Package.Modifiers
{
    /// <summary>
    /// MDM(Mesh Deformation Modifier): FFD (FreeFormDeformer).
    /// Deform mesh by the specific weight values that correspond to the specific weight nodes.
    /// Written by Matej Vanco (2020, updated in 2023).
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    [AddComponentMenu(MD_Debug.ORGANISATION + MD_Debug.PACKAGENAME + "Modifiers/FFD")]
    public sealed class MDM_FFD : MD_ModifierBase
    {
        public enum FFDType {ffd2x2x2, ffd3x3x3, ffd4x4x4, CustomSigned};
        public FFDType ffdType = FFDType.ffd2x2x2;
        public enum FFDShape { Octahedron, Sphere, Cube, Custom};
        public FFDShape ffdShape = FFDShape.Sphere;
        public GameObject ffdCustomShape;

        public float ffdNodeSize = 0.25f;
        [Range(0.0f, 1.0f)] public float ffdOffset = 0.2f;
        public float ffdOffsetMultiplier = 1.0f;
        [Range(2,12)] public int ffdCustomCount = 2;

        [Range(0,1)] public float weight = 0.5f;
        public float threshold = 0.15f;
        public float density = 1.35f;
        public float bias = 1.0f;
        public float multiplier = 1.0f;

        public float distanceLimitation = Mathf.Infinity;

        [SerializeField] private List<Transform> generatedNodes = new List<Transform>();
        [SerializeField] private List<Vector3> initialNodePositions = new List<Vector3>();
        public Transform NodeRoot { get => _nodeRoot; private set => _nodeRoot = value; }
        [SerializeField] private Transform _nodeRoot;

        /// <summary>
        /// When the component is added to an object (called once)
        /// </summary>
        private void Reset()
        {
            if (MbIsInitialized)
                return;
            MDModifier_InitializeBase();
        }

        #region Event Subscription

        protected override void OnEnable()
        {
            base.OnEnable();
            OnMeshSubdivided += ResetFFDParams;
            OnMeshSmoothed += ResetFFDParams;
            OnMeshBaked += ResetFFDParams;
            OnMeshRestored += ResetFFDParams;
            OnNewMeshReferenceCreated += ResetFFDParams;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            OnMeshSubdivided -= ResetFFDParams;
            OnMeshSmoothed -= ResetFFDParams;
            OnMeshBaked -= ResetFFDParams;
            OnMeshRestored -= ResetFFDParams;
            OnNewMeshReferenceCreated -= ResetFFDParams;
        }

        protected override void OnDestroy()
        {
            OnMeshSubdivided -= ResetFFDParams;
            OnMeshSmoothed -= ResetFFDParams;
            OnMeshBaked -= ResetFFDParams;
            OnMeshRestored -= ResetFFDParams;
            OnNewMeshReferenceCreated -= ResetFFDParams;
            FFD_ClearFFDGrid();
            base.OnDestroy();
        }

        private void ResetFFDParams()
        {
            if (NodeRoot)
                FFD_RefreshFFDGrid();
        }

        #endregion

        private void OnDrawGizmos()
        {
            if (NodeRoot == null) return;
            if (generatedNodes.Count == 0) return;
            if (initialNodePositions.Count == 0) return;
            if (generatedNodes.Count != initialNodePositions.Count) return;

            Gizmos.color = Color.green;
            for (int i = 0; i < initialNodePositions.Count; i++)
            {
                if (generatedNodes[i] != null)
                    Gizmos.DrawLine(initialNodePositions[i], generatedNodes[i].position);
            }
        }

        #region Base overrides

        /// <summary>
        /// Base modifier initialization
        /// </summary>
        protected override void MDModifier_InitializeBase(MeshReferenceType meshReferenceType = MeshReferenceType.GetFromPreferences, bool forceInitialization = false, bool affectUpdateEveryFrameField = true)
        {
            base.MDModifier_InitializeBase(meshReferenceType, forceInitialization, affectUpdateEveryFrameField);
            // Initialize initial verts and working verts (backup verts not necessary)
            MDModifier_InitializeMeshData(true, false, true);
        }

        /// <summary>
        /// Process the FFD function on the current mesh
        /// </summary>
        public override void MDModifier_ProcessModifier()
        {
            if (!MbIsInitialized)
                return;

            if (NodeRoot == null) return;
            if (generatedNodes.Count == 0) return;
            if (initialNodePositions.Count == 0) return;
            if (generatedNodes.Count != initialNodePositions.Count) return;

            threshold = Mathf.Max(threshold, 0.1f);

            Vector3[] vvs = new Vector3[MbWorkingMeshData.vertices.Length];
            for (int i = 0; i < vvs.Length; i++)
            {
                Vector3 curVert = transform.TransformPoint(MbWorkingMeshData.vertices[i]);
                Vector3 originalVert = curVert;
                for (int x = 0; x < initialNodePositions.Count; x++)
                {
                    Transform node = generatedNodes[x];
                    if (node == null)
                        continue;
                    if (Vector3.Distance(initialNodePositions[x], originalVert) > distanceLimitation) continue;
                    curVert += (node.position - originalVert).normalized *
                        ((Vector3.Distance(node.position, originalVert) * multiplier) *
                        (1.0f / Mathf.Pow(Vector3.Distance(initialNodePositions[x], originalVert) * threshold, density)) * bias) *
                        (Vector3.Distance(initialNodePositions[x], node.position) * 0.01f);
                }
                vvs[i] = transform.InverseTransformPoint(MD_Utilities.Math3D.CustomLerp(originalVert, curVert, weight));
            }

            MbMeshFilter.sharedMesh.vertices = vvs;

            MDMeshBase_RecalculateMesh();
        }

        #endregion

        private void Update()
        {
            if (MbUpdateEveryFrame) 
                MDModifier_ProcessModifier();
        }

        #region FFD essentials

        /// <summary>
        /// Refresh selected FFD type & its grid (This will refresh & reset current FFD nodes to default positions)
        /// </summary>
        public void FFD_RefreshFFDGrid()
        {
            float ww = weight;
            weight = 0.0f;
            MDModifier_ProcessModifier();
            switch (ffdType)
            {
                case FFDType.ffd2x2x2:
                    ffdCustomCount = 2;
                    break;
                case FFDType.ffd3x3x3:
                    ffdCustomCount = 3;
                    break;
                case FFDType.ffd4x4x4:
                    ffdCustomCount = 4;
                    break;
            }

            transform.rotation = Quaternion.identity;

            FFD_ClearFFDGrid();

            int ffd = ffdCustomCount - 1;

            Vector3 maxBounds = transform.position + Vector3.Scale(transform.localScale, MbMeshFilter.sharedMesh.bounds.max) + (Vector3.one * (ffdOffset * ffdOffsetMultiplier));
            Vector3 minBounds = transform.position + Vector3.Scale(transform.localScale, MbMeshFilter.sharedMesh.bounds.min) - (Vector3.one * (ffdOffset * ffdOffsetMultiplier));

            float Xstep = (maxBounds.x - minBounds.x) / ffd;
            float Ystep = (maxBounds.y - minBounds.y) / ffd;
            float Zstep = (maxBounds.z - minBounds.z) / ffd;

            Vector3 cp;
            GameObject gm;
            NodeRoot = new GameObject("FFDRoot_" + ffdType.ToString()).transform;
            NodeRoot.position = transform.position;
            for (int x = 0; x < ffd + 1; x++)
            {
                cp = minBounds;
                cp.x += Xstep * x;
                for (int y = 0; y < ffd + 1; y++)
                {
                    cp.y = minBounds.y + (Ystep * y);
                    for (int z = 0; z < ffd + 1; z++)
                    {
                        cp.z = minBounds.z + (Zstep * z);
                        gm = ffdShape == FFDShape.Sphere ? GameObject.CreatePrimitive(PrimitiveType.Sphere) :
                                ffdShape == FFDShape.Cube ? GameObject.CreatePrimitive(PrimitiveType.Cube) :
                                ffdShape == FFDShape.Custom ? (ffdCustomShape == null ? MDG_Octahedron.CreateGeometryAndDispose<MDG_Octahedron>(prepareMaterial: true) : Instantiate(ffdCustomShape)) :
                                MDG_Octahedron.CreateGeometryAndDispose<MDG_Octahedron>(prepareMaterial: true);

                        gm.transform.localScale *= ffdNodeSize;
                        gm.transform.position = cp;
                        gm.transform.parent = NodeRoot;
                        gm.name = "FFDPoint" + NodeRoot.childCount.ToString();

                        initialNodePositions.Add(gm.transform.position);
                        generatedNodes.Add(gm.transform);
                    }
                }
            }

            weight = ww;
            MDModifier_ProcessModifier();
        }

        /// <summary>
        /// Clear FFD grid (if possible) and keep the edited mesh
        /// </summary>
        public void FFD_ClearFFDGrid()
        {
            foreach (Transform tn in generatedNodes)
            {
                if (tn)
                {
                    if(Application.isPlaying)
                        Destroy(tn.gameObject);
                    else
                        DestroyImmediate(tn.gameObject);
                }
            }

            generatedNodes.Clear();
            initialNodePositions.Clear();

            if (NodeRoot)
            {
                if (Application.isPlaying)
                    Destroy(NodeRoot.gameObject);
                else
                    DestroyImmediate(NodeRoot.gameObject);
            }
        }

        /// <summary>
        /// Receive a list of currently generated nodes (if possible - might return null)
        /// </summary>
        /// <returns>Returns a list of generated nodes (might return null as well)</returns>
        public List<Transform> FFD_GetNodes()
        {
            return generatedNodes;
        }

        /// <summary>
        /// Is the included node transform a part of this FFD?
        /// </summary>
        /// <returns>Returns true if the included node is a part of this FFD grid</returns>
        public bool FFD_NodesContain(Transform node)
        {
            if (generatedNodes == null)
                return false;
            return generatedNodes.Contains(node);
        }

        #endregion
    }
}

#if UNITY_EDITOR
namespace MD_Package_Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MDM_FFD))]
    public sealed class MDM_FFDEditor : MD_ModifierBase_Editor
    {
        private MDM_FFD mb;

        public override void OnEnable()
        {
            mMeshBase = (MD_MeshBase)target;
            mModifierBase = (MD_ModifierBase)target;
            mb = (MDM_FFD)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            MDE_l("FFD Modifier", true);
            MDE_v();
            MDE_v();
            MDE_v();
            MDE_DrawProperty("ffdType", "FFD Type");
            if (mb.ffdType == MDM_FFD.FFDType.CustomSigned)
                MDE_DrawProperty("ffdCustomCount", "Custom Signed Count");
            MDE_DrawProperty("ffdShape", "FFD Point Shape");
            if (mb.ffdShape == MDM_FFD.FFDShape.Custom)
                MDE_DrawProperty("ffdCustomShape", "Custom Shape");
            MDE_ve();
            MDE_h();
            if (MDE_b(new GUIContent("Refresh FFD Grid", "Refresh current FFD grid - the mesh will switch to its initial state")))
                mb.FFD_RefreshFFDGrid();
            if (MDE_b(new GUIContent("Clear FFD Grid", "Clear current FFD grid - the mesh status will remain and the grid will be removed")))
                mb.FFD_ClearFFDGrid();
            MDE_he();
            MDE_s(5);
            MDE_DrawProperty("ffdNodeSize", "Node Size");
            MDE_DrawProperty("ffdOffset", "FFD Grid Offset");
            MDE_DrawProperty("ffdOffsetMultiplier", "Offset Multiplier", identOffset:true);
            MDE_ve();
            MDE_s();
            MDE_DrawProperty("weight", "Weight");
            MDE_DrawProperty("threshold", "Threshold");
            MDE_DrawProperty("density", "Density");
            MDE_DrawProperty("bias", "Bias");
            MDE_DrawProperty("multiplier", "Overall Multiplier");
            MDE_s(5);
            MDE_DrawProperty("distanceLimitation", "Distance Limitation");
            MDE_ve();
            MDE_s();
            MDE_AddMeshColliderRefresher(mb.gameObject);
            MDE_BackToMeshEditor(mb);
        }
    }
}
#endif