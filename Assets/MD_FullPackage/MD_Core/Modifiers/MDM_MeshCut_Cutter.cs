using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

using MD_Package.Modifiers;
#endif

namespace MD_Package.Modifiers
{
    /// <summary>
    /// MDM(Mesh Deformation Modifier): Mesh Cut - Cutter source.
    /// Apply this component to any planar object that will represent a cut transform with upwards-cut-direction.
    /// Written by Matej Vanco (2022, updated in 2023).
    /// </summary>
    public sealed class MDM_MeshCut_Cutter : MonoBehaviour
    {
        public bool simpleCutter = true;

        public Color cutterColor = Color.cyan;
        public LayerMask cutterLayerMask = ~0;
        public QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore;

        // Complex cutter 

        public Vector2 cutterSize = new Vector2(2, 2);
        [Range(4, 128)] public int cutterIterations = 8;
        public bool showIterations = true;

        private Vector3 GetHorizontalVec { get { return (((-transform.forward * 0.5f) * transform.localScale.z) * cutterSize.x) * transform.localScale.z; } }
        private Vector3 GetVerticalVec(int index, float step, bool reversed = false)
        {
            return (((((reversed ? -transform.up : transform.up) * 0.5f)) * (step * (index + 1))) * transform.localScale.y);
        }
        private Vector3 GetHorizontalDirection { get { return transform.forward * GetHorizontalMultiplication; } }
        private float GetHorizontalMultiplication { get { return transform.localScale.z * cutterSize.x * transform.localScale.z; } }

        private void OnDrawGizmos()
        {
            Gizmos.color = cutterColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            if (simpleCutter)
            {
                Gizmos.DrawWireCube(Vector3.zero, transform.localScale);
                return;
            }

            Gizmos.DrawWireCube(Vector3.zero, new Vector3(0.0f, cutterSize.y * transform.localScale.y, cutterSize.x * transform.localScale.z));
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(0.0f, cutterSize.y * transform.localScale.y - 0.3f, cutterSize.x * transform.localScale.z - 0.3f));

            cutterSize.x = Mathf.Abs(cutterSize.x);
            cutterSize.y = Mathf.Abs(cutterSize.y);

            if (!showIterations) return;

            int iteraction = cutterIterations < 4 ? 4 : cutterIterations;
            int half = iteraction / 2;
            float y = (cutterSize.y * transform.localScale.y) / half;

            // Ray Visualization
            //------------------------------
            for (int i = 0; i < half; i++)
                Debug.DrawRay(transform.position + GetHorizontalVec + GetVerticalVec(i, y), GetHorizontalDirection);
            Debug.DrawRay(transform.position + GetHorizontalVec, GetHorizontalDirection);
            for (int i = 0; i < half; i++)
                Debug.DrawRay(transform.position + GetHorizontalVec + GetVerticalVec(i, y, true), GetHorizontalDirection);
            //------------------------------
        }

        /// <summary>
        /// Check if the current Mesh Cutter overlaps the specific object
        /// </summary>
        /// <returns>Returns true if the intersection proceeds successfully</returns>
        public bool HitObject(GameObject obj)
        {
            // Simple cutter
            if (simpleCutter)
            {
                var t = transform;
                var b = Physics.OverlapBox(t.position, t.localScale / 2, t.rotation, cutterLayerMask, queryTriggerInteraction);
                foreach (var bb in b)
                {
                    if (bb.gameObject == obj)
                        return true;
                }

                return false;
            }

            // Complex cutter
            int iteraction = cutterIterations < 4 ? 4 : cutterIterations;
            int half = iteraction / 2;
            float y = (cutterSize.y * transform.localScale.y) / half;
            bool res;

            for (int i = 0; i < half; i++)
            {
                res = CastRay(transform.position + GetHorizontalVec + GetVerticalVec(i, y), GetHorizontalDirection, obj);
                if (res) return true;
            }
            res = CastRay(transform.position + GetHorizontalVec, GetHorizontalDirection, obj);
            if (res) return true;

            for (int i = 0; i < half; i++)
            {
                res = CastRay(transform.position + GetHorizontalVec + GetVerticalVec(i, y, true), GetHorizontalDirection, obj);
                if (res) return true;
            }

            return res;
        }

        private bool CastRay(Vector3 p, Vector3 d, GameObject obj)
        {
            Ray r = new Ray(p, d.normalized);

            if (Physics.Raycast(r, out RaycastHit h, GetHorizontalMultiplication, cutterLayerMask, queryTriggerInteraction))
            {
                if (h.collider && h.collider.gameObject == obj)
                    return true;
            }
            return false;
        }
    }
}

#if UNITY_EDITOR

namespace MD_Package_Editor
{
    [CustomEditor(typeof(MDM_MeshCut_Cutter))]
    public sealed class MDM_MeshCut_Cutter_Editor : MD_EditorUtilities
    {
        private MDM_MeshCut_Cutter c;

        private void OnEnable()
        {
            c = (MDM_MeshCut_Cutter)target;
        }

        public override void OnInspectorGUI()
        {
            if (!c)
            {
                DrawDefaultInspector();
                return;
            }

            MDE_s();
            MDE_DrawProperty("simpleCutter", "Use Simple Cutter");
            MDE_s(5);
            MDE_v();
            MDE_DrawProperty("cutterColor", "Cutter Color");
            MDE_DrawProperty("cutterLayerMask", "Cutter Layer Mask");
            MDE_DrawProperty("queryTriggerInteraction", "Query Triggers");
            MDE_ve();
            MDE_v();
            if (c.simpleCutter)
            {
                MDE_hb("Cutter size corresponds to the actual transform scale");
                MDE_ve();
                return;
            }
            MDE_DrawProperty("cutterSize", "Cutter Size", "Virtual cutter size vector. You can naturally re-scale the object");
            MDE_DrawProperty("cutterIterations", "Cutter Iterations", "Quality of the raycast - how many iterations will proceed?");
            MDE_DrawProperty("showIterations", "Show Iterations");
            MDE_ve();
        }
    }
}

#endif
