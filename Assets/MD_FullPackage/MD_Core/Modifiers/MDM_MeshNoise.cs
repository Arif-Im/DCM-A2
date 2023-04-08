using UnityEngine;

using MD_Package.Utilities;

#if UNITY_EDITOR
using UnityEditor;

using MD_Package;
using MD_Package.Modifiers;
#endif

namespace MD_Package.Modifiers
{
    /// <summary>
    /// MDM(Mesh Deformation Modifier): Mesh Noise.
    /// Physically-based perlin noise generator with vertical and spatial features.
    /// Written by Matej Vanco (2016, updated in 2023).
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    [AddComponentMenu(MD_Debug.ORGANISATION + MD_Debug.PACKAGENAME + "Modifiers/Mesh Noise")]
    public sealed class MDM_MeshNoise : MD_ModifierBase
    {
        public enum NoiseType { SpatialNoise, VerticalNoise }
        public NoiseType noiseType = NoiseType.SpatialNoise;

        public float noiseAmount = 1;
        public float noiseSpeed = 0.5f;
        public float noiseIntensity = 0.5f;

        private MD_Utilities.Math3D.Perlin perlinInstance = new MD_Utilities.Math3D.Perlin();

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

            perlinInstance = new MD_Utilities.Math3D.Perlin();
        }

        /// <summary>
        /// Process the noise function on the current mesh
        /// </summary>
        public override void MDModifier_ProcessModifier()
        {
            if (!MbIsInitialized)
                return;

            if (noiseType == NoiseType.VerticalNoise)
                MeshNoise_UpdateVerticalNoise();
            else if (noiseType == NoiseType.SpatialNoise)
                MeshNoise_UpdateSpatialNoise();

            MDMeshBase_RecalculateMesh();
        }

        #endregion

        private void Update()
        {
            if (MbUpdateEveryFrame)
                MDModifier_ProcessModifier();
        }

        /// <summary>
        /// Process vertical noise manually
        /// </summary>
        public void MeshNoise_UpdateVerticalNoise()
        {
            for (int i = 0; i < MbWorkingMeshData.vertices.Length; i++)
            {
                float pX = (MbWorkingMeshData.vertices[i].x * noiseAmount) + (Time.timeSinceLevelLoad * noiseSpeed);
                float pZ = (MbWorkingMeshData.vertices[i].z * noiseAmount) + (Time.timeSinceLevelLoad * noiseSpeed);

                MbWorkingMeshData.vertices[i].y = (Mathf.PerlinNoise(pX, pZ) - 0.5f) * noiseIntensity;
            }

            MbMeshFilter.sharedMesh.vertices = MbWorkingMeshData.vertices;
        }

        /// <summary>
        /// Process spatial noise manually
        /// </summary>
        public void MeshNoise_UpdateSpatialNoise()
        {
            float timex = (Time.time * noiseSpeed) + 0.1365143f;
            float timey = (Time.time * noiseSpeed) + 1.21688f;
            float timez = (Time.time * noiseSpeed) + 2.5564f;

            for (var i = 0; i < MbBackupMeshData.vertices.Length; i++)
            {
                Vector3 vertex = MbBackupMeshData.vertices[i];
                vertex.x += perlinInstance.Noise(timex + vertex.x, timex + vertex.y, timex + vertex.z) * noiseIntensity;
                vertex.y += perlinInstance.Noise(timey + vertex.x, timey + vertex.y, timey + vertex.z) * noiseIntensity;
                vertex.z += perlinInstance.Noise(timez + vertex.x, timez + vertex.y, timez + vertex.z) * noiseIntensity;
                MbWorkingMeshData.vertices[i] = vertex;
            }

            MbMeshFilter.sharedMesh.vertices = MbWorkingMeshData.vertices;
        }

        /// <summary>
        /// Change overall noise intensity
        /// </summary>
        public void MeshNoise_ChangeIntensity(UnityEngine.UI.Slider sliderEntry)
        {
            noiseIntensity = sliderEntry.value;
        }

        /// <summary>
        /// Change overall noise intensity
        /// </summary>
        public void MeshNoise_ChangeIntensity(float entry)
        {
            noiseIntensity = entry;
        }
    }
}

#if UNITY_EDITOR
namespace MD_Package_Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MDM_MeshNoise))]
    public sealed class MDM_MeshNoise_Editor : MD_ModifierBase_Editor
    {
        private MDM_MeshNoise mb;

        public override void OnEnable()
        {
            mMeshBase = (MD_MeshBase)target;
            mModifierBase = (MD_ModifierBase)target;
            mb = (MDM_MeshNoise)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            MDE_l("Noise Modifier", true);
            MDE_v();
            MDE_DrawProperty("noiseType", "Noise Type");
            MDE_DrawProperty("noiseIntensity", "Intensity");
            MDE_DrawProperty("noiseSpeed", "Speed");
            if (mb.noiseType == MDM_MeshNoise.NoiseType.VerticalNoise)
            {
                MDE_DrawProperty("noiseAmount", "Amount");
                if (!mModifierBase.MbUpdateEveryFrame)
                    if (MDE_b("Update Noise in Editor"))
                        mb.MeshNoise_UpdateVerticalNoise();
            }
            else
            {
                if (!mModifierBase.MbUpdateEveryFrame)
                    if (MDE_b("Update Noise in Editor"))
                        mb.MeshNoise_UpdateSpatialNoise();
            }
            MDE_ve();
            MDE_s();
            MDE_AddMeshColliderRefresher(mb.gameObject);
            MDE_BackToMeshEditor(mb);
        }
    }
}
#endif

