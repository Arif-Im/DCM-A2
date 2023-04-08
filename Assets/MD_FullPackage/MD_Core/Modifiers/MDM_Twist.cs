using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

using MD_Package;
using MD_Package.Modifiers;
#endif

namespace MD_Package.Modifiers
{
    /// <summary>
    /// MDM(Mesh Deformation Modifier): Mesh Twist.
    /// Twist mesh by the specific value to the specific direction.
    /// Written by Matej Vanco (2015, updated in 2023).
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    [AddComponentMenu(MD_Debug.ORGANISATION + MD_Debug.PACKAGENAME + "Modifiers/Mesh Twist")]
    public sealed class MDM_Twist : MD_ModifierBase
    {
        public enum TwistDirection { X, Y, Z }
        public TwistDirection twistDimension = TwistDirection.X;
        public TwistDirection twistDirection = TwistDirection.X;

        public float twistValue = 0;
        public bool twistMirrored = true;

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
            OnMeshSubdivided += ResetTwistParams;
            OnMeshSmoothed += ResetTwistParams;
            OnMeshBaked += ResetTwistParams;
            OnMeshRestored += ResetTwistParams;
            OnNewMeshReferenceCreated += ResetTwistParams;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            OnMeshSubdivided -= ResetTwistParams;
            OnMeshSmoothed -= ResetTwistParams;
            OnMeshBaked -= ResetTwistParams;
            OnMeshRestored -= ResetTwistParams;
            OnNewMeshReferenceCreated -= ResetTwistParams;
        }

        protected override void OnDestroy()
        {
            OnMeshSubdivided -= ResetTwistParams;
            OnMeshSmoothed -= ResetTwistParams;
            OnMeshBaked -= ResetTwistParams;
            OnMeshRestored -= ResetTwistParams;
            OnNewMeshReferenceCreated -= ResetTwistParams;
            base.OnDestroy();
        }

        private void ResetTwistParams()
        {
            twistValue = 0.0f;
            MDModifier_ProcessModifier();
        }

        #endregion

        private void Update()
        {
            if (MbUpdateEveryFrame)
                MDModifier_ProcessModifier();
        }

        #region Base overrides

        /// <summary>
        /// Base modifier initialization
        /// </summary>
        protected override void MDModifier_InitializeBase(MeshReferenceType meshReferenceType = MeshReferenceType.GetFromPreferences, bool forceInitialization = false, bool affectUpdateEveryFrameField = true)
        {
            base.MDModifier_InitializeBase(meshReferenceType, forceInitialization, affectUpdateEveryFrameField);

            MDModifier_InitializeMeshData();

            Twist_RegisterCurrentState();
        }

        /// <summary>
        /// Process the bend function on the current mesh
        /// </summary>
        public override void MDModifier_ProcessModifier()
        {
            if (!MbIsInitialized)
                return;

            for (int i = 0; i < MbBackupMeshData.vertices.Length; i++)
                MbWorkingMeshData.vertices[i] = TwistVertex(MbBackupMeshData.vertices[i], GetTwistDimension(MbBackupMeshData.vertices[i]) * twistValue);

            MbMeshFilter.sharedMesh.vertices = MbWorkingMeshData.vertices;

            MDMeshBase_RecalculateMesh();
        }

        #endregion

        private float GetTwistDimension(Vector3 entry)
        {
            switch(twistDimension)
            {
                case TwistDirection.X: return entry.x;
                case TwistDirection.Y: return entry.y;
                case TwistDirection.Z: return entry.z;
            }
            return entry.x;
        }

        private Vector3 TwistVertex(Vector3 vert, float val)
        {
            if (val == 0.0f) return vert;
            if (!twistMirrored && vert.y < 0) return vert;

            float sin = Mathf.Sin(val);
            float cos = Mathf.Cos(val);
            Vector3 final = Vector3.zero;
            switch (twistDirection)
            {
                case TwistDirection.X:
                    final.y = vert.y * cos - vert.z * sin;
                    final.z = vert.y * sin + vert.z * cos;
                    final.x = vert.x;
                    break;
                case TwistDirection.Y:
                    final.x = vert.x * cos - vert.z * sin;
                    final.z = vert.x * sin + vert.z * cos;
                    final.y = vert.y;
                    break;
                case TwistDirection.Z:
                    final.x = vert.x * cos - vert.y * sin;
                    final.y = vert.x * sin + vert.y * cos;
                    final.z = vert.z;
                    break;
            }
            return final;
        }

        /// <summary>
        /// Refresh & register current mesh state. This will override the backup vertices to the current mesh state
        /// </summary>
        public void Twist_RegisterCurrentState()
        {
            MDModifier_InitializeMeshData(false);
            twistValue = 0;
        }

        /// <summary>
        /// Twist object by the UI Slider value
        /// </summary>
        public void Twist_TwistObject(UnityEngine.UI.Slider entry)
        {
            twistValue = entry.value;
            if (!MbUpdateEveryFrame) MDModifier_ProcessModifier();
        }

        /// <summary>
        /// Twist object by the float value
        /// </summary>
        public void Twist_TwistObject(float entry)
        {
            twistValue = entry;
            if (!MbUpdateEveryFrame) MDModifier_ProcessModifier();
        }
    }

}

#if UNITY_EDITOR
namespace MD_Package_Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MDM_Twist))]
    public sealed class MDM_Twist_Editor : MD_ModifierBase_Editor
    {
        private MDM_Twist mb;

        public override void OnEnable()
        {
            mMeshBase = (MD_MeshBase)target;
            mModifierBase = (MD_ModifierBase)target;
            mb = (MDM_Twist)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            MDE_l("Twist Modifier", true);
            MDE_v();
            MDE_DrawProperty("twistDimension", "Twist Dimension");
            MDE_DrawProperty("twistDirection", "Twist Direction");
            MDE_DrawProperty("twistValue", "Twist Value");
            MDE_DrawProperty("twistMirrored", "Mirrored", "If enabled, the twist will process on both sides of the mesh");
            MDE_ve();
            MDE_s();
            MDE_v();
            if (MDE_b("Register Mesh")) mb.Twist_RegisterCurrentState();
            MDE_hb("Refresh current mesh & register backup vertices to the edited vertices");
            MDE_ve();
            MDE_AddMeshColliderRefresher(mModifierBase.gameObject);
            MDE_BackToMeshEditor(mModifierBase);
        }
    }
}
#endif

