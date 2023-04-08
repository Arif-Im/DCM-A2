using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;

using MD_Package;
using MD_Package.Modifiers;
#endif

namespace MD_Package.Modifiers
{
    /// <summary>
    /// MDM(Mesh Deformation Modifier): Mesh Damage.
    /// Damage/ distort physical mesh by the specific parameters.
    /// Written by Matej Vanco (2018, updated in 2023).
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    [AddComponentMenu(MD_Debug.ORGANISATION + MD_Debug.PACKAGENAME + "Modifiers/Mesh Damage")]
    public sealed class MDM_MeshDamage : MD_ModifierBase
    {
        public bool detectForceImpact = true;
        public float forceAmount = 0.15f;
        public float forceMultiplier = 0.075f;
        public bool detectRadiusSize = false;
        public float radius = 0.5f;
        public float radiusMultiplier = 1.0f;
        public float radiusSoftness = 1.0f;
        public float forceDetection = 1.5f;

        public bool continousDamage = false;

        public bool collisionWithSpecificTag = false;
        public string collisionTag = "";

        public bool enableEvent;
        public UnityEvent eventOnDamage;

        /// <summary>
        /// When the component is added to an object (called once)
        /// </summary>
        private void Reset()
        {
            if (MbIsInitialized)
                return;
            MDModifier_InitializeBase(affectUpdateEveryFrameField: false);
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
        /// Unused override
        /// </summary>
        public override void MDModifier_ProcessModifier()
        {}

        #endregion

        private void Start()
        {
            if (Application.isPlaying)
                MeshDamage_RefreshVertices();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!Application.isPlaying)
                return;
            if (collision.contactCount == 0)
                return;
            if (forceDetection != 0 && collision.relativeVelocity.magnitude < forceDetection)
                return;
            if (collisionWithSpecificTag && collisionTag != collision.gameObject.tag)
                return;
            if (detectForceImpact)
                forceAmount = collision.relativeVelocity.magnitude * forceMultiplier;
            if (detectRadiusSize)
                radius = collision.transform.localScale.magnitude / 4;
            for(int i = 0; i < collision.contactCount; i++)
                MeshDamage_ModifyMesh(collision.GetContact(i).point, radius, forceAmount, collision.relativeVelocity, continousDamage);
        }

        /// <summary>
        /// Modify current mesh by the point (world space), radius, force and direction
        /// </summary>
        public void MeshDamage_ModifyMesh(Vector3 point, float radius, float force, Vector3 direction, bool continuousEffect = false)
        {
            if (!MbIsInitialized)
                return;

            radius *= radiusMultiplier;
            for (int i = 0; i < MbWorkingMeshData.vertices.Length; i++)
            {
                Vector3 ppp = transform.TransformPoint(MbBackupMeshData.vertices[i]);
                float distance = Vector3.Distance(point, ppp);
                if (distance < radius)
                {
                    float radDist = (radius - distance);
                    ppp += (direction.normalized * force) * (radiusSoftness == 1.0f ? radDist : Mathf.Pow(radDist, radiusSoftness));
                    MbWorkingMeshData.vertices[i] = transform.InverseTransformPoint(ppp);
                }
            }

            MbMeshFilter.mesh.SetVertices(MbWorkingMeshData.vertices);
            MDMeshBase_RecalculateMesh();

            if (continuousEffect)
                MeshDamage_RefreshVertices();

            if (enableEvent) eventOnDamage?.Invoke();
        }

        /// <summary>
        /// Refresh vertices & register brand new original vertices state
        /// </summary>
        public void MeshDamage_RefreshVertices()
        {
            MDModifier_InitializeMeshData(false);
        }

        /// <summary>
        /// Restore deformed mesh by the specified speed value
        /// </summary>
        public void MeshDamage_RestoreMesh(float restorationSpeed = 0.1f)
        {
            var selVerts = MbInitialMeshData.vertices.Length != MbBackupMeshData.vertices.Length ? MbBackupMeshData : MbInitialMeshData;
            for (int i = 0; i < MbWorkingMeshData.vertices.Length; i++)
                MbWorkingMeshData.vertices[i] = Vector3.Lerp(MbWorkingMeshData.vertices[i], selVerts.vertices[i], restorationSpeed);

            MbMeshFilter.mesh.SetVertices(MbWorkingMeshData.vertices);
            MDMeshBase_RecalculateMesh();
        }

        /// <summary>
        /// Modify current mesh by the custom RaycastEvent
        /// </summary>
        public void MeshDamage_ModifyMesh(MDM_RaycastEvent RayEvent)
        {
            if (!Application.isPlaying)
                return;
            if (RayEvent == null)
                return;
            if (RayEvent.RayEventHits.Length > 0 && RayEvent.RayEventHits[0].collider.gameObject != this.gameObject)
                return;
            if (detectRadiusSize)
            {
                if (!RayEvent.pointRay)
                    radius = RayEvent.sphericalRadius;
                else
                    radius = 0.1f;
            }

            foreach (RaycastHit hit in RayEvent.RayEventHits)
                MeshDamage_ModifyMesh(hit.point, radius, forceAmount, RayEvent.RayEventRay.direction, continousDamage);
        }
    }
}

#if UNITY_EDITOR
namespace MD_Package_Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MDM_MeshDamage))]
    public sealed class MDM_MeshDamage_Editor : MD_ModifierBase_Editor
    {
        private MDM_MeshDamage md;

        public override void OnEnable()
        {
            mMeshBase = (MD_MeshBase)target;
            mModifierBase = (MD_ModifierBase)target;
            md = (MDM_MeshDamage)target;
            showUpdateEveryFrame = false;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            MDE_l("Mesh Damage Modifier", true);
            MDE_v();
            MDE_s(5);
            MDE_l("General Settings", true);
            MDE_v();
            MDE_v();
            MDE_DrawProperty("detectRadiusSize", "Detect Radius Size", "Adjust radius size by the collided objects. This will try to auto-detect the interaction radius with the collided rigidbody transform scales");
            if (!md.detectRadiusSize)
            {
                MDE_plus();
                MDE_DrawProperty("radius", "Interaction Radius");
                MDE_minus();
            }
            MDE_DrawProperty("radiusMultiplier","Radius Multiplier", "General radius multiplier. Multiplies the radius constant or auto-detected radius - default value is 1");
            MDE_DrawProperty("radiusSoftness", "Radius Softness", "General radius softness - the higher the value is, the softer the results are. Default value is 1 - if the value is different, takes more performance");
            MDE_ve();
            MDE_s(5);
            MDE_v();
            MDE_DrawProperty("detectForceImpact", "Detect Force Impact", "If enabled (Recommended), the system will try to detect a force impact automatically with the collided rigidbodies");
            if (!md.detectForceImpact)
                MDE_DrawProperty("forceAmount", "Impact Force", "Constant force value applied to the interacted vertices");
            else
            {
                MDE_plus();
                MDE_DrawProperty("forceMultiplier", "Force Multiplier", "Multiplier of the applied force from the rigidbody");
                MDE_minus();
            }
            MDE_ve();
            MDE_ve();
            MDE_s();
            MDE_l("Conditions", true);
            MDE_v();
            MDE_v();
            MDE_DrawProperty("forceDetection", "Force Detection Level", "Minimum relative velocity impact detection level - what should be the minimum velocity for the rigidbodies to damage this mesh?");
            MDE_ve();
            MDE_s(5);
            MDE_v();
            MDE_DrawProperty("continousDamage", "Continuous Effect", "If enabled, vertices of the mesh will be able to move beyond their initial location");
            MDE_ve();
            MDE_s(5);
            MDE_v();
            MDE_DrawProperty("collisionWithSpecificTag", "Collision With Specific Tag", "If enabled, collision will be allowed for objects with the tag below");
            if (md.collisionWithSpecificTag)
            {
                MDE_plus();
                MDE_DrawProperty("collisionTag", "Collision Tag");
                MDE_minus();
            }
            MDE_ve();
            MDE_ve();
            MDE_s();
            MDE_v();
            MDE_DrawProperty("enableEvent", "Enable Event System");
            if (md.enableEvent)
                MDE_DrawProperty("eventOnDamage", "Event On Damage-Collision", "Event will be proceeded after successful collision");
            MDE_ve();
            MDE_ve();
            MDE_AddMeshColliderRefresher(mModifierBase.gameObject);
            MDE_BackToMeshEditor(mModifierBase);
        }
    }
}
#endif
