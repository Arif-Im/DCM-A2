using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

using MD_Package.Modifiers;
#endif

namespace MD_Package.Modifiers
{
    /// <summary>
    /// MDM(Mesh Deformation Modifier): Melt Controller.
    /// Control melt shader through the script. Required shader: MD_Melt.
    /// Written by Matej Vanco (2017, updated in 2023).
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [AddComponentMenu(MD_Debug.ORGANISATION + MD_Debug.PACKAGENAME + "Modifiers/Melt Controller")]
    public sealed class MDM_MeltController : MonoBehaviour
    {
        public bool meltBySurfaceRaycast = true;
        public Vector3 raycastOriginOffset = new Vector3(0, 0, 0);
        public Vector3 raycastDirection = new Vector3(0, -1, 0);
        public float raycastDistance = Mathf.Infinity;
        public float raycastRadius = 0.5f;
        public LayerMask allowedLayerMasks = -1;

        public bool linearInterpolationBlend = false;
        public float linearInterpolationSpeed = 0.5f;

        private MaterialPropertyBlock mpb;
        private Renderer rer;
        private Material myMaterial;
        private Transform realTarget;

        private float targetValue;
        private float targetLerpValue;

        private void Awake()
        {
            rer = GetComponent<Renderer>();
            myMaterial = Instantiate(rer.material);
            realTarget = transform;
            rer.material = myMaterial;
            mpb = new MaterialPropertyBlock();
        }

        private void Update()
        {
            if (!realTarget)
                return;

            if (!meltBySurfaceRaycast)
            {
                if (!linearInterpolationBlend)
                    mpb.SetFloat("_M_Zone", realTarget.position.y);
                else
                    targetValue = realTarget.position.y;
            }
            else
            {
                Ray r = new Ray(realTarget.transform.position + raycastOriginOffset, raycastDirection.normalized);
                bool gotHit = Physics.SphereCast(r, raycastRadius, out RaycastHit hit, raycastDistance, allowedLayerMasks) && hit.collider;
                if (gotHit)
                {
                    if (!linearInterpolationBlend)
                        mpb.SetFloat("_M_Zone", hit.point.y);
                    else
                        targetValue = hit.point.y;
                }
                else
                {
                    targetValue = realTarget.position.y;
                    if (!linearInterpolationBlend)
                        mpb.SetFloat("_M_Zone", targetValue);
                }
            }

            if (linearInterpolationBlend)
            {
                targetLerpValue = Mathf.Lerp(targetLerpValue, targetValue, Time.deltaTime * linearInterpolationSpeed);
                mpb.SetFloat("_M_Zone", targetLerpValue);
            }

            rer.SetPropertyBlock(mpb);
        }
    }
}

#if UNITY_EDITOR
namespace MD_Package_Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MDM_MeltController))]
    public sealed class MDM_MeltController_Editor : MD_EditorUtilities
    {
        private MDM_MeltController m;

        private void OnEnable()
        {
            m = (MDM_MeltController)target;
        }

        public override void OnInspectorGUI()
        {
            MDE_s();
            MDE_hb("Object must contains MD_Melt shader");
            MDE_v();
            MDE_DrawProperty("meltBySurfaceRaycast", "Melt by Raycast", "If enabled, the Y value will correspond to the modified hit point Y. Otherwise you will be able to customize the value by yourself");
            if (m.meltBySurfaceRaycast)
            {
                MDE_DrawProperty("allowedLayerMasks", "Allowed Layer Masks");
                MDE_DrawProperty("raycastOriginOffset", "Raycast Origin Offset");
                MDE_DrawProperty("raycastDirection", "Raycast Direction");
                MDE_DrawProperty("raycastDistance", "Raycast Distance");
                MDE_DrawProperty("raycastRadius", "Raycast Radius");
                MDE_s(5);
                MDE_DrawProperty("linearInterpolationBlend", "Enable Smooth Transition");
                if (m.linearInterpolationBlend)
                    MDE_DrawProperty("linearInterpolationSpeed", "Smooth Speed");
            }
            MDE_ve();
            MDE_BackToMeshEditor(m);
        }
    }
}
#endif

