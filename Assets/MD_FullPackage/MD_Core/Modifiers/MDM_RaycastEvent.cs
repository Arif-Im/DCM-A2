using System;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;

using MD_Package.Modifiers;
#endif

namespace MD_Package.Modifiers
{
    /// <summary>
    /// MDM(Mesh Deformation Modifier): Raycast Event.
    /// Simple raycast behaviour with customizable events.
    /// Written by Matej Vanco (2015, updated in 2023).
    /// </summary>
    [AddComponentMenu(MD_Debug.ORGANISATION + MD_Debug.PACKAGENAME + "Modifiers/Raycast Event")]
    public sealed class MDM_RaycastEvent : MonoBehaviour
    {
        public bool updateRayPerFrame = true;
        public bool usesPhysics = false;

        public float rayLength = 5.0f;
        public bool pointRay = true;
        public float sphericalRadius = 0.2f;
        public bool localRay = true;
        public Vector3 globalRayDir = new Vector3(0, -1, 0);

        public LayerMask rayLayer = ~0;
        public bool raycastWithSpecificTag = false;
        public string raycastTag = "";

        public UnityEvent eventOnRaycast;
        public UnityEvent eventOnRaycastExit;
        public event Action eventActionOnRaycast;
        public event Action eventActionOnRaycastExit;

        public RaycastHit[] RayEventHits { get; private set; }
        public Ray RayEventRay { get; private set; }

        private void OnDrawGizmosSelected()
        {
            if (!pointRay) Gizmos.DrawWireSphere(transform.position + (localRay ? transform.forward : globalRayDir.normalized) * rayLength, sphericalRadius);
            Gizmos.DrawLine(transform.position, transform.position + (localRay ? transform.forward : globalRayDir.normalized) * rayLength);
        }

        private void Update()
        {
            if (!usesPhysics && updateRayPerFrame)
                RayEvent_UpdateRaycastState();
        }

        private void FixedUpdate()
        {
            if (usesPhysics && updateRayPerFrame)
                RayEvent_UpdateRaycastState();
        }

        /// <summary>
        /// Get Raycast state
        /// </summary>
        public bool RayEvent_IsRaycasting()
        {
            return raycastingState;
        }

        private bool raycastingState = false;

        /// <summary>
        /// Update current Raycast (If 'Update Ray Per Frame' is disabled)
        /// </summary>
        public void RayEvent_UpdateRaycastState()
        {
            RayEventRay = new Ray(transform.position, localRay ? transform.forward : globalRayDir.normalized);
            if (!Physics.Raycast(RayEventRay, out RaycastHit hit, rayLength, rayLayer))
            {
                if (raycastingState)
                {
                    eventOnRaycastExit?.Invoke();
                    eventActionOnRaycastExit?.Invoke();
                    raycastingState = false;
                }
                return;
            }
            else if (raycastWithSpecificTag && hit.collider.tag != raycastTag)
            {
                if (raycastingState)
                {
                    eventOnRaycastExit?.Invoke();
                    eventActionOnRaycastExit?.Invoke();
                    raycastingState = false;
                }
                return;
            }

            raycastingState = true;

            if (pointRay)
                RayEventHits = Physics.RaycastAll(RayEventRay, rayLength, rayLayer);
            else
                RayEventHits = Physics.SphereCastAll(RayEventRay, sphericalRadius, rayLength, rayLayer);

            if (RayEventHits.Length > 0)
            {
                eventOnRaycast?.Invoke();
                eventActionOnRaycast?.Invoke();
            }
        }
    }
}
#if UNITY_EDITOR
namespace MD_Package_Editor
{
    [CustomEditor(typeof(MDM_RaycastEvent))]
    [CanEditMultipleObjects]
    public sealed class MDM_RaycastEvent_Editor : MD_EditorUtilities
    {
        private MDM_RaycastEvent m;

        private void OnEnable()
        {
            m = (MDM_RaycastEvent)target;
        }

        public override void OnInspectorGUI()
        {
            MDE_s();
            MDE_v();
            MDE_DrawProperty("updateRayPerFrame", "Update Ray Per Frame", "If disabled, you are able to invoke your own method to Update ray state");
            MDE_DrawProperty("usesPhysics", "Uses Physics", "If enabled, the update loop with be replace by the FixedUpdate");
            MDE_s();
            MDE_v();
            MDE_DrawProperty("rayLength", "Ray Length");
            MDE_DrawProperty("pointRay", "Pointed Ray", "If disabled, raycast will be generated as a 'Spherical Ray'");
            if (!m.pointRay)
                MDE_DrawProperty("sphericalRadius", "Radius");
            MDE_ve();
            MDE_s(5);
            MDE_v();
            MDE_DrawProperty("localRay", "Local Direction","If disabled, the ray's direction will be related to the world-space");
            if (m.localRay == false)
                MDE_DrawProperty("globalRayDir", "Global Ray Direction");
            MDE_ve();
            MDE_s(5);
            MDE_v();
            MDE_DrawProperty("rayLayer", "Allowed Layer", "Allowed layer list for the ray");
            MDE_DrawProperty("raycastWithSpecificTag", "Raycast Specific Tag", "If disabled, raycast will accept every object with collider");
            if (m.raycastWithSpecificTag)
            {
                MDE_plus();
                MDE_DrawProperty("raycastTag", "Raycast Tag");
                MDE_minus();
            }
            MDE_ve();
            MDE_ve();
            MDE_s();
            MDE_DrawProperty("eventOnRaycast", "Event Raycast Hit", "Event on raycast enter");
            MDE_DrawProperty("eventOnRaycastExit", "Event Raycast Exit", "Event on raycast exit");
            if (target != null) serializedObject.Update();
        }
    }
}
#endif
