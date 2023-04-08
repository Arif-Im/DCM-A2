using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

using MD_Package;
#endif

namespace MD_Package
{
    /// <summary>
    /// MD(Mesh Deformation): Mesh Editor Runtime.
    /// Essential component for general mesh-vertex-editing at runtime [VR]. Required component for editing: MD_MeshProEditor.
    /// Written by Matej Vanco (2016, updated in 2023).
    /// </summary>
    [AddComponentMenu(MD_Debug.ORGANISATION + MD_Debug.PACKAGENAME + "Mesh Editor Runtime VR")]
    public sealed class MD_MeshEditorRuntimeVR : MonoBehaviour
    {
        // Runtime Editor Type - Point Manipulation
        public enum VertexControlMode { GrabDropVertex, PushVertex, PullVertex };
        public VertexControlMode vertexControlMode = VertexControlMode.GrabDropVertex;

        // Appearance
        public bool switchAppearance = true;
        public Color switchAppearanceToColor = Color.green;
        public Color switchAppearanceFromColor = Color.red;
        public bool switchAppearanceUseMaterial = false;
        public Material switchAppearanceMaterialTarget;
        public Material switchAppearanceMaterialInitial;

        // Pull-Push Settings
        public float pullPushVertexSpeed = 0.15f;
        public float maxMinPullPushDistance = Mathf.Infinity;
        public bool continuousPullPushDetection = false;
        public enum PullPushType { Radial, Directional };
        public PullPushType pullPushType = PullPushType.Directional;

        // Conditions
        public bool allowSpecificPoints = false;
        public string allowedPointsTag;

        // Raycast
        public bool useRaycasting = true;
        public bool allowBackfaces = true;
        public LayerMask allowedLayerMask = -1;
        public float raycastDistance = 5.0f;
        public float raycastRadius = 0.25f;

        // DEBUG
        public bool enableGizmos = true;

        // VR Input - for debug purposes
        public bool InputDown { get; private set; }

        private struct PotentialPoints
        {
            public Transform parent;
            public Transform point;
        }
        private List<PotentialPoints> potentialPoints = new List<PotentialPoints>();


        private void Start()
        {
            //It's required to have rigidbody while using Trigger version
            if(!useRaycasting)
            {
                if (!GetComponent<Rigidbody>())
                    gameObject.AddComponent<Rigidbody>().isKinematic = true;
            }
        }

        private void Update()
        {
            //Process raycast editor if enabled
            if(useRaycasting)
                InternalProcess_RaycastingRuntimeEditor();
            //Otherwise process trigger editor
            else
            {
                if (!VREditor_GetControlInput())
                {
                    if(InputDown)
                    {
                        if(vertexControlMode == VertexControlMode.GrabDropVertex)
                            foreach(PotentialPoints tr in potentialPoints)
                                tr.point.parent = tr.parent;
                        InputDown = false;
                    }
                    return;
                }
                //Reset all potential points if input is UP
                if (InputDown)
                {
                    if (vertexControlMode != VertexControlMode.GrabDropVertex)
                        InternalProcess_ProcessPullPush();
                    return;
                }
                if (vertexControlMode == VertexControlMode.GrabDropVertex)
                    foreach (PotentialPoints tr in potentialPoints)
                        tr.point.parent = this.transform;
                InputDown = true;
            }
        }

        private void OnDrawGizmos()
        {
            if (!enableGizmos)       return;
            if (!useRaycasting)   return;
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * raycastDistance);
            Gizmos.DrawWireSphere(transform.position + transform.forward * raycastDistance, raycastRadius);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (useRaycasting)    return;
            if (InputDown)         return;

            if (allowSpecificPoints)
            {
                if (!other.CompareTag(allowedPointsTag))
                    return;
            }

            if (other.transform.GetComponentInParent<MD_MeshProEditor>() == false) 
                return;

            PotentialPoints ppp = new PotentialPoints() { point = other.transform, parent = other.transform.parent };
            potentialPoints.Add(ppp);
            VREditor_ChangeMaterialToPoints(ppp, true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (useRaycasting) return;

            for (int i = potentialPoints.Count - 1; i >= 0; i--)
                if (other.transform == potentialPoints[i].point)
                {
                    VREditor_ChangeMaterialToPoints(potentialPoints[i], false);
                    potentialPoints.RemoveAt(i);
                }
        }

        private void InternalProcess_RaycastingRuntimeEditor()
        {
            //If input is pressed/down, process the runtime editor
            if (InputDown && potentialPoints.Count > 0)
            {
                if (vertexControlMode != VertexControlMode.GrabDropVertex)
                {
                    InternalProcess_ProcessPullPush();
                    if (continuousPullPushDetection) InputDown = false;
                }

                //Check for input-UP
                if (!VREditor_GetControlInput())
                {
                    foreach (PotentialPoints tr in potentialPoints)
                        tr.point.parent = tr.parent;
                    InputDown = false;
                }

                if (InputDown) return;
            }

            //If input is up, raycast for potential points in sphere radius
            Ray ray = new Ray(transform.position, transform.forward);
            RaycastHit[] raycast = Physics.SphereCastAll(ray, raycastRadius, raycastDistance, allowedLayerMask);

            //Reset a potential points list
            if(potentialPoints.Count > 0)
            {
                if (switchAppearance)
                    foreach (PotentialPoints tr in potentialPoints)
                        VREditor_ChangeMaterialToPoints(tr, false);
                potentialPoints.Clear();
            }

            if (raycast.Length == 0) return;

            //Declare a new potential points chain
            foreach (RaycastHit h in raycast)
            {
                if (!h.transform.GetComponentInParent<MD_MeshProEditor>())
                    continue;
                if (allowSpecificPoints && !h.transform.CompareTag(allowedPointsTag))
                    continue;
                if(!allowBackfaces && Vector3.Distance(transform.position + transform.forward * raycastDistance, h.transform.position) > raycastRadius)
                    continue;

                PotentialPoints ppp = new PotentialPoints() { point = h.transform, parent = h.transform.parent };
                potentialPoints.Add(ppp);
                VREditor_ChangeMaterialToPoints(ppp, true);
            }

            //Manage final control_down = if pressed, process the runtime editor next frame
            if (VREditor_GetControlInput())
            {
                foreach (PotentialPoints tr in potentialPoints)
                {
                    VREditor_ChangeMaterialToPoints(tr, false);
                    if (vertexControlMode == VertexControlMode.GrabDropVertex)
                        tr.point.parent = transform;
                }
                InputDown = true;
            }
        }

        private void InternalProcess_ProcessPullPush()
        {
            foreach (PotentialPoints tr in potentialPoints)
            {
                Vector3 raycastPos = transform.position + (transform.forward * raycastDistance);
                Vector3 tvector = pullPushType == PullPushType.Radial ? (tr.point.position - raycastPos) : transform.forward;
                float dist = (tr.point.position - raycastPos).magnitude;
                if (vertexControlMode == VertexControlMode.PushVertex && dist > maxMinPullPushDistance)
                    continue;
                if (vertexControlMode == VertexControlMode.PullVertex && dist < maxMinPullPushDistance && maxMinPullPushDistance != Mathf.Infinity)
                    continue;
                tr.point.position += (vertexControlMode == VertexControlMode.PushVertex ? tvector : -tvector) * pullPushVertexSpeed * Time.deltaTime;
            }
        }

        private void VREditor_ChangeMaterialToPoints(PotentialPoints p, bool selected)
        {
            if (!switchAppearance)
                return;

            Renderer r = p.point.GetComponent<Renderer>();
            if (selected)
            {
                if (switchAppearanceUseMaterial)
                    r.material = switchAppearanceMaterialTarget;
                else
                    r.material.color = switchAppearanceToColor;
            }
            else
            {
                if (switchAppearanceUseMaterial)
                    r.material = switchAppearanceMaterialInitial;
                else
                    r.material.color = switchAppearanceFromColor;
            }
        }

        #region Available Public Methods

        /// <summary>
        /// Switch current control mode by index [1-Grab/Drop,2-Push,3-Pull]
        /// </summary>
        public void VREditor_SwitchControlMode(int index)
        {
            vertexControlMode = (VertexControlMode)index;
        }

        /// <summary>
        /// Get current built-in control VR input of the specified attributes
        /// </summary>
        /// <returns>returns true if pressed</returns>
        public bool VREditor_GetControlInput()
        {
            if (!Application.isPlaying) return false;

            return INPUT_DOWN_Secondary;
        }

        private bool INPUT_DOWN_Secondary = false;

        /// <summary>
        /// Set control input from 3rd party source (such as SteamVR, Oculus or other)
        /// </summary>
        /// <param name="setInputTo">Input down or up?</param>
        public void GlobalReceived_SetControlInput(bool setInputTo)
        {
            INPUT_DOWN_Secondary = setInputTo;
        }

        #endregion
    }
}

#if UNITY_EDITOR
namespace MD_Package_Editor
{
    [CustomEditor(typeof(MD_MeshEditorRuntimeVR))]
    [CanEditMultipleObjects]
    public sealed class MD_MeshEditorRuntimeVR_Editor : MD_EditorUtilities
    {
        private MD_MeshEditorRuntimeVR m;

        private void OnEnable()
        {
            m = (MD_MeshEditorRuntimeVR)target;
        }

        public override void OnInspectorGUI()
        {
            MDE_s();
            MDE_hb("Mesh Editor Runtime VR should be added to one of the VR controllers");
            MDE_hb("Add proper MDInputVR component to customize VR input for specific platform");

            MDE_s();

            MDE_v();
            MDE_v();
            MDE_DrawProperty("vertexControlMode", "Vertex Control Mode", "Choose one of the feature modes for the Mesh Editor at runtime");
            if (m.vertexControlMode != MD_MeshEditorRuntimeVR.VertexControlMode.GrabDropVertex)
            {
                MDE_v();
                MDE_DrawProperty("pullPushVertexSpeed", "Motion Speed", "Pull/Push effect speed", default);
                MDE_DrawProperty("pullPushType", "Motion Type", "Select one of the motion types of Pull/Push effect", default);
                MDE_ve();
                MDE_s(3);
                if (m.vertexControlMode == MD_MeshEditorRuntimeVR.VertexControlMode.PullVertex)
                    MDE_DrawProperty("maxMinPullPushDistance", "Minimum Distance", "How close can the points be?", default, true);
                else
                    MDE_DrawProperty("maxMinPullPushDistance", "Maximum Distance", "How far can the points go?", default, true);
                MDE_s(3);
                MDE_DrawProperty("continuousPullPushDetection", "Continuous Detection", "If enabled, the potential points will be refreshed every frame", default, true);
            }
            MDE_ve();
            MDE_s();
            MDE_l("Vertex Selection Appearance", true);
            MDE_v();
            MDE_DrawProperty("switchAppearance", "Use Appearance Feature", "If enabled, you will be able to customize vertex appearance");
            if (m.switchAppearance)
            {
                MDE_DrawProperty("switchAppearanceUseMaterial", "Use Custom Material", "If enabled, you will be able to use custom material instance instead of color");
                if (m.switchAppearanceUseMaterial)
                {
                    MDE_DrawProperty("switchAppearanceMaterialTarget", "Target Material", "Target material when selected", default, true);
                    MDE_DrawProperty("switchAppearanceMaterialInitial", "Initial Material", "Original material", default, true);
                }
                else
                {
                    MDE_DrawProperty("switchAppearanceToColor", "Change To Color", "Target color if system catches potential vertexes", default, true);
                    MDE_DrawProperty("switchAppearanceFromColor", "Change From Color", "Initial original color if system releases potential vertexes", default, true);
                }
            }
            MDE_ve();
            MDE_s();
            MDE_l("Conditions", true);
            MDE_v();
            MDE_DrawProperty("allowSpecificPoints", "Allow Specific Points", "If disabled, all points will be interactive");
            if (m.allowSpecificPoints)
                MDE_DrawProperty("allowedPointsTag", "Allowed Tag", default, default, true);
            MDE_ve();
            MDE_s();
            MDE_l("Editor Method", true);
            MDE_v();
            MDE_DrawProperty("useRaycasting", "Use Raycasting", "If enabled, the system will use the raycasting technique [more precise], otherwise the system will use trigger system [less precise]");
            if (m.useRaycasting)
            {
                MDE_v();
                MDE_DrawProperty("allowedLayerMask", "Allowed Layer Masks", default, default, true);
                MDE_DrawProperty("raycastDistance", "Raycast Distance", default, default, true);
                MDE_DrawProperty("raycastRadius", "Raycast Radius", default, default, true);
                MDE_ve();
                MDE_DrawProperty("allowBackfaces", "Allow Backfaces", "Allow points behind the point of view", default, true);
            }
            else MDE_hb("The system is set to Trigger System editor method. The object should contain primitive collider checked to IsTrigger");
            MDE_ve();
            MDE_s(15);
            MDE_DrawProperty("enableGizmos", "Show Scene Gizmos", default, true);
            MDE_ve();

            serializedObject.Update();
        }
    }
}
#endif