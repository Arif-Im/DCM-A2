using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

using MD_Package;
using MD_Package.Modifiers;
#endif

namespace MD_Package.Modifiers
{
    /// <summary>
    /// MDM(Mesh Deformation Modifier): Mesh Slime.
    /// Interactive soft 'slime' modifier with multiple advanced settings.
    /// Written by Matej Vanco (2020, updated in 2023).
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    [AddComponentMenu(MD_Debug.ORGANISATION + MD_Debug.PACKAGENAME + "Modifiers/Mesh Slime")]
    public sealed class MDM_MeshSlime : MD_ModifierBase
    {
        public bool useControls = true;
        public KeyCode legacyInput = KeyCode.Mouse0;
        public bool mobilePlatformSupport = false;
        public Camera mainCameraInstance;

        public enum DragAxisType { Y_Only, TowardsObjectsPivot, CrossProduct, NormalsDirection };
        public DragAxisType dragAxisType = DragAxisType.Y_Only;

        public bool restore = false;
        public float restorationSpeed = 1f;

        public float mainRadius = 0.1f;
        [Range(0.01f, 1.0f)] public float mainFalloff = 1.0f;
        public float mainIntensity = 0.1f;

        public bool reverseDrag = false;
        public float dragValue = 0.16f;
        [Range(0.01f, 1.0f)] public float dragFalloff = 0.8f;
        public float maxDragSpeed = 0.5f;

        /// <summary>
        /// When the component is added to an object (called once)
        /// </summary>
        private void Reset()
        {
            if (MbIsInitialized)
                return;
            MDModifier_InitializeBase();
        }

        private void Start()
        {
            if(!Application.isPlaying)
                return;
            if (!useControls)
                return;

            if(!mainCameraInstance)
                mainCameraInstance = Camera.main;
            if (!mainCameraInstance)
                MD_Debug.Debug(this, "Main camera instance is missing!", MD_Debug.DebugType.Error);
        }

        #region Base overrides

        /// <summary>
        /// Base modifier initialization
        /// </summary>
        protected override void MDModifier_InitializeBase(MeshReferenceType meshReferenceType = MeshReferenceType.GetFromPreferences, bool forceInitialization = false, bool affectUpdateEveryFrameField = true)
        {
            base.MDModifier_InitializeBase(meshReferenceType, forceInitialization, affectUpdateEveryFrameField);

            MDModifier_InitializeMeshData();
        }

        /// <summary>
        /// Process the mesh slime function on the current mesh (use 'MeshSlime_ModifyMesh' method for more customized setting)
        /// </summary>
        public override void MDModifier_ProcessModifier()
        {
            if (!Application.isPlaying)
                return;
            if (!MbIsInitialized)
                return;

            if (restore)
            {
                for (int i = 0; i < MbWorkingMeshData.vertices.Length; i++)
                    MbWorkingMeshData.vertices[i] = Vector3.Lerp(MbWorkingMeshData.vertices[i], MbBackupMeshData.vertices[i], restorationSpeed * Time.deltaTime);
                MbMeshFilter.mesh.vertices = MbWorkingMeshData.vertices;

                MDMeshBase_RecalculateMesh();
            }

            if (!useControls)
                return;
            if (!mainCameraInstance)
            {
                MD_Debug.Debug(this, "Main camera instance is missing!", MD_Debug.DebugType.Error);
                return;
            }

            Ray r = new Ray();
            if (!mobilePlatformSupport)
                r = mainCameraInstance.ScreenPointToRay(Input.mousePosition);
            else if (Input.touchCount > 0)
                r = mainCameraInstance.ScreenPointToRay(Input.GetTouch(0).position);

            bool c;
            if (!mobilePlatformSupport)
                c = Input.GetKey(legacyInput);
            else
                c = Input.touchCount > 0;


            if (!c) { oldhPos = Vector3.zero; return; }

            if (Physics.Raycast(r, out RaycastHit h))
            {
                if (h.collider)
                {
                    currhPos = ((!reverseDrag) ? (h.point - oldhPos) : (oldhPos - h.point));
                    if (currhPos.magnitude > maxDragSpeed)
                        oldhPos = Vector3.zero;
                    MeshSlime_ModifyMesh(h.point);
                    oldhPos = h.point;
                }
            }
        }

        #endregion

        private Vector3 oldhPos;
        private Vector3 currhPos;

        private void Update()
        {
            MDModifier_ProcessModifier();
        }

        /// <summary>
        /// Modify mesh on the specific world point with current mesh slime settings
        /// </summary>
        public void MeshSlime_ModifyMesh(Vector3 worldPoint)
        {
            if (!Application.isPlaying) 
                return;
            if (!MbIsInitialized)
                return;

            currhPos.y = 0;
            worldPoint = transform.InverseTransformPoint(worldPoint);
            for (int i = 0; i < MbWorkingMeshData.vertices.Length; i++)
            {
                Vector3 vv = MbWorkingMeshData.vertices[i];
                if (Vector3.Distance(vv, worldPoint) > mainRadius)
                    continue;
                float mult = mainFalloff * (mainRadius - (Vector3.Distance(worldPoint, vv)));
                Vector3 dir = Vector3.zero;
                switch (dragAxisType)
                {
                    case DragAxisType.Y_Only:
                        dir = Vector3.up;
                        break;
                    case DragAxisType.TowardsObjectsPivot:
                        dir = -(transform.position - transform.TransformPoint(vv));
                        break;
                    case DragAxisType.CrossProduct:
                        dir = Vector3.Cross(vv, worldPoint);
                        break;
                    case DragAxisType.NormalsDirection:
                        dir = MbMeshFilter.mesh.normals[i];
                        break;
                }
                vv -= (mainIntensity * mult * dir) + ((oldhPos == Vector3.zero) ? Vector3.zero : (currhPos * (reverseDrag ? (dragValue / 2f) * dragFalloff : dragValue * dragFalloff)));
                MbWorkingMeshData.vertices[i] = vv;
            }
            MbMeshFilter.mesh.vertices = MbWorkingMeshData.vertices;
            MDMeshBase_RecalculateMesh();
        }

        /// <summary>
        /// Modify mesh by the specific RaycastEvent with current mesh slime settings
        /// </summary>
        public void MeshSlime_ModifyMesh(MDM_RaycastEvent entry)
        {
            if (!Application.isPlaying) 
                return;
            if (entry.RayEventHits.Length > 0 && entry.RayEventHits[0].collider.gameObject != this.gameObject)
                return;
            foreach (RaycastHit hit in entry.RayEventHits)
                MeshSlime_ModifyMesh(hit.point);
        }
    }
}

#if UNITY_EDITOR
namespace MD_Package_Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MDM_MeshSlime))]
    public sealed class MDM_MeshSlime_Editor : MD_ModifierBase_Editor
    {
        private MDM_MeshSlime mb;

        public override void OnEnable()
        {
            mMeshBase = (MD_MeshBase)target;
            mModifierBase = (MD_ModifierBase)target;
            mb = (MDM_MeshSlime)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            MDE_l("Mesh Slime Modifier", true);
            MDE_v();
            MDE_DrawProperty("useControls", "Use Contnrols", "If enabled, you will be able to drag the slime mesh with mouse/finger");
            MDE_DrawProperty("mobilePlatformSupport", "Mobile Platform", "If enabled, the slime mesh will be ready for Mobile devices");
            if (mb.useControls && !mb.mobilePlatformSupport)
                MDE_DrawProperty("legacyInput", "Legacy Input Key");
            MDE_DrawProperty("mainCameraInstance", "Main Camera", "Main Camera target instance (leave this field empty if the main camera is in the scene tagged as MainCamera)");
            MDE_DrawProperty("dragAxisType", "Drag Axis Type", "Select proper axis type. Each axis type has a specific and unique functionality");
            MDE_ve();
            MDE_s();
            MDE_l("Restoration Settings");
            MDE_v();
            MDE_DrawProperty("restore", "Restore Slime", "If enabled, the mesh will restore to its initial shape");
            if (mb.restore)
                MDE_DrawProperty("restorationSpeed", "Restoration Speed");
            MDE_ve();
            MDE_s();
            MDE_l("Main Interaction");
            MDE_v();
            MDE_DrawProperty("mainRadius", "Main Radius", "What's the radius to interact with vertices on the given location?");
            MDE_DrawProperty("mainFalloff", "Main Falloff Radius", "Falloff value to the main radius");
            MDE_DrawProperty("mainIntensity", "Main Intensity", "Intensity for the main radius");
            MDE_ve();
            MDE_s();
            MDE_l("Drag Settings");
            MDE_v();
            MDE_DrawProperty("reverseDrag", "Reverse Drag", "If enabled, the dragged vertices will move towards the cursor/finger move position");
            MDE_DrawProperty("dragValue", "Drag Value", "Force in which the drag is proceed - the higher the value, the more the vertices will move towards the dragged location");
            MDE_DrawProperty("dragFalloff", "Drag Density Falloff", "Falloff for the drag location");
            MDE_DrawProperty("maxDragSpeed", "Max Drag Speed", "How far can the dragged vertices go?");
            MDE_ve();
            MDE_s();
            MDE_AddMeshColliderRefresher(mb.gameObject);
            MDE_BackToMeshEditor(mb);
        }
    }
}
#endif
