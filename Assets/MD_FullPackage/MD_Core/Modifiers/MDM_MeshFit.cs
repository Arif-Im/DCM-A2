using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

using MD_Package;
using MD_Package.Modifiers;
#endif

using MD_Package.Geometry;

namespace MD_Package.Modifiers
{
    /// <summary>
    /// MDM(Mesh Deformation Modifier): Mesh Fit.
    /// Modify mesh vertices by surface height or fit mesh to any collider.
    /// Written by Matej Vanco (2017, updated in 2023).
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    [AddComponentMenu(MD_Debug.ORGANISATION + MD_Debug.PACKAGENAME + "Modifiers/Mesh Fit")]
    public sealed class MDM_MeshFit : MD_ModifierBase
    {
        public LayerMask allowedLayers = ~0;
        public float meshFitterOffset = 0.03f;
        public float meshFitterSurfaceDetection = 3;
        public enum MeshFitterMode { FitWholeMesh, FitSpecificVertices };
        public MeshFitterMode meshFitterType = MeshFitterMode.FitWholeMesh;
        public bool meshFitterContinuousEffect = false;

        [field: SerializeField] public Transform[] generatedPoints { get; private set; }
        public GameObject[] selectedPoints;

        [SerializeField] private GameObject vertexRoot;

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
            OnMeshSubdivided += ResetParams;
            OnMeshSmoothed += ResetParams;
            OnMeshBaked += ResetParams;
            OnMeshRestored += ResetParams;
            OnNewMeshReferenceCreated += ResetParams;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            OnMeshSubdivided -= ResetParams;
            OnMeshSmoothed -= ResetParams;
            OnMeshBaked -= ResetParams;
            OnMeshRestored -= ResetParams;
            OnNewMeshReferenceCreated -= ResetParams;
        }

        protected override void OnDestroy()
        {
            OnMeshSubdivided -= ResetParams;
            OnMeshSmoothed -= ResetParams;
            OnMeshBaked -= ResetParams;
            OnMeshRestored -= ResetParams;
            OnNewMeshReferenceCreated -= ResetParams;
            base.OnDestroy();
        }

        private void ResetParams()
        {
            MeshFit_ClearPoints();
        }

        #endregion

        #region Base overrides

        /// <summary>
        /// Base modifier initialization
        /// </summary>
        protected override void MDModifier_InitializeBase(MeshReferenceType meshReferenceType = MeshReferenceType.GetFromPreferences, bool forceInitialization = false, bool affectUpdateEveryFrameField = true)
        {
            base.MDModifier_InitializeBase(meshReferenceType, forceInitialization, affectUpdateEveryFrameField);

            MDModifier_InitializeMeshData();
        }

        public override void MDModifier_ProcessModifier()
        {
            if (!MbIsInitialized)
                return;
            if (!MbWorkingMeshData.MbDataInitialized())
                return;
            MeshFit_UpdateMeshState();
        }

        #endregion

        private void Update()
        {
            if (MbUpdateEveryFrame) 
                MDModifier_ProcessModifier();
        }

        /// <summary>
        /// Refresh currently selected points state. Once the selected points are assigned, this will separate selected and unselected points
        /// </summary>
        internal void MeshFit_RefreshSelectedPointsState()
        {
            if (generatedPoints == null || selectedPoints == null)          return;
            if (generatedPoints.Length == 0 || selectedPoints.Length == 0)  return;

            foreach (Transform gm in generatedPoints)
                gm.gameObject.SetActive(false);
            foreach (GameObject gm in selectedPoints)
                gm.SetActive(true);
        }

        /// <summary>
        /// Show/ Hide renderers of the generated points (if exist)
        /// </summary>
        public void MeshFit_ShowHidePoints(bool activation)
        {
            if (generatedPoints == null) return;
            if (generatedPoints.Length == 0) return;

            if (meshFitterType == MeshFitterMode.FitSpecificVertices && selectedPoints != null && selectedPoints.Length > 0)
            {
                foreach (GameObject p in selectedPoints)
                {
                    if (p && p.GetComponent<Renderer>())
                        p.GetComponent<Renderer>().enabled = activation;
                }
                return;
            }

            foreach (Transform p in generatedPoints)
            {
                Renderer ren = p.GetComponent<Renderer>();
                if (ren) ren.enabled = activation;
            }
        }

        /// <summary>
        /// Generate gameObject points on the mesh
        /// </summary>
        public void MeshFit_GeneratePoints()
        {
            MeshFit_ClearPoints();

            Vector3 lastPos = transform.position;
            Quaternion lastRot = transform.rotation;

            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            vertexRoot = new GameObject("VertexRoot_" + this.name);
            vertexRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            generatedPoints = new Transform[MbWorkingMeshData.vertices.Length];

            // Generating points
            Material pMat = new Material(Utilities.MD_Utilities.MD_Specifics.GetProperPipelineDefaultShader(false));
            pMat.color = Color.red;
            for (int i = 0; i < MbWorkingMeshData.vertices.Length; i++)
            {
                GameObject point = MDG_Octahedron.CreateGeometryAndDispose<MDG_Octahedron>();

                if(!Application.isPlaying)
                    DestroyImmediate(point.GetComponent<Collider>());
                else
                    Destroy(point.GetComponent<Collider>());

                point.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
                point.GetComponent<Renderer>().material = pMat;

                point.transform.parent = vertexRoot.transform;
                point.transform.position = MbWorkingMeshData.vertices[i];
                point.name = "P" + i.ToString();

                generatedPoints[i] = point.transform;
            }

            // Fixing point hierarchy
            foreach (Transform vertice in generatedPoints)
            {
                foreach (Transform vertice2 in generatedPoints)
                {
                    if (vertice.transform.position == vertice2.transform.position)
                    {
                        vertice.transform.parent = vertice2.transform;
                        vertice.gameObject.SetActive(false);
                    }
                }
            }

            // Renaming Points
            int counter = 1;
            foreach (Transform vertice in vertexRoot.transform)
            {
                vertice.gameObject.SetActive(true);
                vertice.name = "P" + counter.ToString();
                counter++;
            }

            vertexRoot.transform.parent = transform;

            transform.position = lastPos;
            transform.rotation = lastRot;
        }

        /// <summary>
        /// Clear points on the mesh (if possible)
        /// </summary>
        public void MeshFit_ClearPoints()
        {
            generatedPoints = null;
            selectedPoints = null;

            if (vertexRoot)
            {
                if(!Application.isPlaying)
                    DestroyImmediate(vertexRoot);
                else
                    Destroy(vertexRoot);
            }
        }

        /// <summary>
        /// Reset mesh matrix transform (set scale to 1 and keep the shape)
        /// </summary>
        public void MeshFit_BakeMesh()
        {
            MeshFit_ClearPoints();

            Vector3 lastPos = transform.position;
            Quaternion lastRot = transform.rotation;
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            for (int i = 0; i < MbWorkingMeshData.vertices.Length; i++)
                MbWorkingMeshData.vertices[i] = transform.TransformPoint(MbWorkingMeshData.vertices[i]);
            transform.localScale = Vector3.one;
            MbMeshFilter.sharedMesh.vertices = MbWorkingMeshData.vertices;

            MDModifier_InitializeMeshData();

            transform.position = lastPos;
            transform.rotation = lastRot;
        }

        /// <summary>
        /// Update current mesh state (if UpdateEveryFrame is disabled)
        /// </summary>
        public void MeshFit_UpdateMeshState()
        {
            if (generatedPoints == null || generatedPoints.Length == 0)
                return;

            for (int i = 0; i < generatedPoints.Length; i++)
            {
                bool pass = true;
                if (meshFitterType == MeshFitterMode.FitSpecificVertices)
                {
                    pass = generatedPoints[i].gameObject.activeSelf;
                    if(!pass && generatedPoints[i].transform.parent != vertexRoot)
                    {
                        UpdateVert(i);
                        continue;
                    }
                }

                if (!pass)
                    continue;
                Ray ray = new Ray(generatedPoints[i].position + (Vector3.up * meshFitterSurfaceDetection), Vector3.down);
                if (Physics.Raycast(ray, out RaycastHit h, Mathf.Infinity, allowedLayers))
                {
                    if (h.collider)
                        generatedPoints[i].position = new Vector3(h.point.x, h.point.y + meshFitterOffset, h.point.z);
                    else if (!meshFitterContinuousEffect)
                        generatedPoints[i].position = transform.TransformPoint(MbBackupMeshData.vertices[i]);
                }
                else if (!meshFitterContinuousEffect)
                    generatedPoints[i].position = transform.TransformPoint(MbBackupMeshData.vertices[i]);
                UpdateVert(i);
            }

            MbMeshFilter.sharedMesh.vertices = MbWorkingMeshData.vertices;
            MDMeshBase_RecalculateMesh();

            void UpdateVert(int i)
            {
                MbWorkingMeshData.vertices[i] = new Vector3(generatedPoints[i].position.x - (transform.position.x - Vector3.zero.x), generatedPoints[i].position.y - (transform.position.y - Vector3.zero.y), generatedPoints[i].position.z - (transform.position.z - Vector3.zero.z));
            }
        }
    }
}

#if UNITY_EDITOR
namespace MD_Package_Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MDM_MeshFit))]
    public sealed class MDM_MeshFit_Editor : MD_ModifierBase_Editor
    {
        private MDM_MeshFit mb;

        public override void OnEnable()
        {
            mMeshBase = (MD_MeshBase)target;
            mModifierBase = (MD_ModifierBase)target;
            mb = (MDM_MeshFit)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            MDE_l("Mesh Fit Modifier", true);
            MDE_v();
            MDE_h();
            if (MDE_b("Generate Interactive Points"))
                mb.MeshFit_GeneratePoints();
            if (MDE_b("Refresh Mesh Transform"))
                mb.MeshFit_BakeMesh();
            MDE_he();

            if (mb.generatedPoints == null)
            {
                MDE_ve();
                MDE_AddMeshColliderRefresher(mb.gameObject);
                MDE_BackToMeshEditor(mb);
                return;
            }

            MDE_s();
            MDE_h();
            if (MDE_b("Show Points"))
                mb.MeshFit_ShowHidePoints(true);
            if (MDE_b("Hide Points"))
                mb.MeshFit_ShowHidePoints(false);
            if (MDE_b("Clear Points"))
                mb.MeshFit_ClearPoints();
            MDE_he();

            MDE_s();

            MDE_v();
            MDE_s(5);
            MDE_v();
            MDE_DrawProperty("meshFitterType", "Type");
            MDE_ve();
            MDE_s();
            MDE_v();
            MDE_DrawProperty("allowedLayers", "Allowed Layers");
            MDE_DrawProperty("meshFitterOffset", "Raycast Offset", "Vertex position offset after raycast");
            MDE_DrawProperty("meshFitterSurfaceDetection", "Raycast Distance", "Interactivity radius amount");
            MDE_DrawProperty("meshFitterContinuousEffect", "Continuous Effect", "If enabled, every vertex position won't jump to its original state & will continue in the last position");
            MDE_ve();
            MDE_ve();
            if (mb.meshFitterType == MDM_MeshFit.MeshFitterMode.FitSpecificVertices)
            {
                MDE_DrawProperty("selectedPoints", "Selected Points", includeChilds:true);
                if (MDE_b("Open Vertices Assignator"))
                {
                    MD_PointSelectorTool mdvTool = new MD_PointSelectorTool();
                    mdvTool.minSize = new Vector2(400, 20);
                    mdvTool.maxSize = new Vector2(410, 20);
                    mb.transform.parent = null;
                    mdvTool.Show();
                    mdvTool.sender = mb;
                }

                if (mb.selectedPoints != null && mb.selectedPoints.Length > 0)
                    if (MDE_b("Clear Selected Points"))
                    {
                        mb.selectedPoints = null;
                        mb.MeshFit_ShowHidePoints(true);
                    }
            }
            MDE_s();
            MDE_ve();
            MDE_AddMeshColliderRefresher(mb.gameObject);
            MDE_BackToMeshEditor(mb);
        }
    }
}
#endif
