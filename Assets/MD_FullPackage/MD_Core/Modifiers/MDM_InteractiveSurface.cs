using UnityEngine;
using System.Threading;

using MD_Package.Utilities;

#if UNITY_EDITOR
using UnityEditor;

using MD_Package;
using MD_Package.Modifiers;
#endif

namespace MD_Package.Modifiers
{
    /// <summary>
    /// MDM(Mesh Deformation Modifier): Interactive Surface.
    /// Deform mesh with physically based system that reacts with the outer world.
    /// Written by Matej Vanco (2016, updated in 2023).
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    [AddComponentMenu(MD_Debug.ORGANISATION + MD_Debug.PACKAGENAME + "Modifiers/Interactive Surface")]
    public sealed class MDM_InteractiveSurface : MD_ModifierBase, MD_ModifierBase.IMDThreadingSupport
    {
        // IMDThreading implementation
        public bool ThreadUseMultithreading { get => _threadUseMultithreading; set => _threadUseMultithreading = value; }
        [SerializeField] private bool _threadUseMultithreading;
        public bool ThreadEditorThreadSupported { get => _threadEditorThreadSupported; private set => _threadEditorThreadSupported = value; }
        [SerializeField] private bool _threadEditorThreadSupported;
        public bool ThreadIsDone { get; private set; }
        public int ThreadSleep { get => _threadSleep; set => _threadSleep = value; }
        [SerializeField, Range(5, 60)] private int _threadSleep = 30;

        // Modifier fields
        public bool rigidbodiesAllowed = true;

        public bool customInteractionSpeed = false;
        public bool continuousEffect = false;
        public float interactionSpeed = 1.5f;

        public bool exponentialDeformation = true;
        public float radiusSoftness = 1.5f;

        public Vector3 direction = new Vector3(0, -1, 0);
        public float radius = 0.8f;
        public float radiusMultiplier = 1.0f;
        public bool detectRadiusSize = true;
        public float minimumForceDetection = 0;

        public bool restoreSurface;
        public float restorationSpeed = 0.5f;

        public bool collideWithSpecificObjects = false;
        public string collisionTag = "";

        private Vector3[] helperVertices;

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

            ThreadEditorThreadSupported = false;
        }

        /// <summary>
        /// Process the Interactive Surface base update function (use 'InteractiveSurface_ModifyMesh' method for more customized setting)
        /// </summary>
        public override void MDModifier_ProcessModifier()
        {
            if (!Application.isPlaying)
                return;
            if (!MbIsInitialized)
                return;

            //Exit if multithreading is enabled
            if (ThreadUseMultithreading) 
                return;

            //Update custom interaction
            if (customInteractionSpeed)
            {
                if (checkForUpdate_InterSpeed)
                {
                    int doneAll = 0;
                    if (continuousEffect)
                    {
                        for (int i = 0; i < MbBackupMeshData.vertices.Length; i++)
                        {
                            if (MbBackupMeshData.vertices[i] == MbWorkingMeshData.vertices[i])
                                doneAll++;
                            MbBackupMeshData.vertices[i] = Vector3.Lerp(MbBackupMeshData.vertices[i], MbWorkingMeshData.vertices[i], interactionSpeed * Time.deltaTime);
                        }
                        if (doneAll == MbBackupMeshData.vertices.Length)
                            checkForUpdate_InterSpeed = false;
                        MbMeshFilter.mesh.SetVertices(MbBackupMeshData.vertices);
                    }
                    else if(helperVertices != null)
                    {
                        for (int i = 0; i < helperVertices.Length; i++)
                        {
                            if (helperVertices[i] == MbWorkingMeshData.vertices[i])
                                doneAll++;
                            helperVertices[i] = Vector3.Lerp(helperVertices[i], MbWorkingMeshData.vertices[i], interactionSpeed * Time.deltaTime);
                        }
                        if (doneAll == helperVertices.Length)
                            checkForUpdate_InterSpeed = false;
                        MbMeshFilter.mesh.SetVertices(helperVertices);
                    }

                    MDMeshBase_RecalculateMesh();
                }
            }

            //Restore surface if possible
            if (restoreSurface && !continuousEffect)
            {
                if (!checkForUpdate_Repair)
                    return;

                int doneAll = 0;
                for (int i = 0; i < MbWorkingMeshData.vertices.Length; i++)
                {
                    if (MbBackupMeshData.vertices[i] == MbWorkingMeshData.vertices[i])
                        doneAll++;
                    MbWorkingMeshData.vertices[i] = Vector3.Lerp(MbWorkingMeshData.vertices[i], MbBackupMeshData.vertices[i], restorationSpeed * Time.deltaTime);
                }
                if (doneAll == MbWorkingMeshData.vertices.Length)
                    checkForUpdate_Repair = false;
                if (!customInteractionSpeed)
                {
                    MbMeshFilter.mesh.SetVertices(MbWorkingMeshData.vertices);
                    MDMeshBase_RecalculateMesh();
                }
            }
        }

        #endregion

        private void Start()
        {
            if (!Application.isPlaying) 
                return;

            // Initialize custom interaction & continuous effect
            if (customInteractionSpeed && !continuousEffect)
            {
                helperVertices = new Vector3[MbBackupMeshData.vertices.Length];
                System.Array.Copy(MbBackupMeshData.vertices, helperVertices, helperVertices.Length);
            }

            // Initialize separated thread if possible
            if (!ThreadUseMultithreading)
                return;

            Thrd_RealRot = transform.rotation;
            Thrd_RealPos = transform.position;
            Thrd_RealSca = transform.localScale;

            MDModifierThreading_StartThread();
        }

        private bool checkForUpdate_InterSpeed, checkForUpdate_Repair = false;
        private void Update()
        {
            if(MbUpdateEveryFrame)
                MDModifier_ProcessModifier();
        }

        private void OnCollisionStay(Collision collision)
        {
            if (!CheckPhysicalParams(collision))
                return;

            for (int i = 0; i < collision.contactCount; i++)
                InteractiveSurface_ModifyMesh(collision.GetContact(i).point, radius, direction);
        }
        
        private void OnCollisionEnter(Collision collision)
        {
            if (!CheckPhysicalParams(collision))
                return;

            for (int i = 0; i < collision.contactCount; i++)
                InteractiveSurface_ModifyMesh(collision.GetContact(i).point, radius, direction);
        }

        private bool CheckPhysicalParams(Collision collision)
        {
            if (!Application.isPlaying)
                return false;
            if (!rigidbodiesAllowed)
                return false;
            if (collision.contactCount == 0)
                return false;
            if (minimumForceDetection != 0 && collision.relativeVelocity.magnitude < minimumForceDetection)
                return false;
            if (detectRadiusSize)
                radius = collision.transform.localScale.magnitude / 4;
            return true;
        }

        #region Interactive Surface essentials

        private void InteractiveSurface_PushData()
        {
            for (int i = 0; i < MbWorkingMeshData.vertices.Length; i++)
            {
                Vector3 TransformedPoint = MD_Utilities.Transformations.TransformPoint(Thrd_RealPos, Thrd_RealRot, Thrd_RealSca, MbWorkingMeshData.vertices[i]);
                float distance = Vector3.Distance(new Vector3(Thrd_WorldPoint.x, 0, Thrd_WorldPoint.z), new Vector3(TransformedPoint.x, 0, TransformedPoint.z));
                if (distance < Thrd_Radius)
                {
                    Vector3 modifVertex = MbBackupMeshData.vertices[i] + (Thrd_Dir * (exponentialDeformation ? (distance > Thrd_Radius - radiusSoftness ? (Thrd_Radius - (distance)) : 1) : 1));
                    if (exponentialDeformation && (modifVertex.y > MbWorkingMeshData.vertices[i].y)) continue;
                    MbWorkingMeshData.vertices[i] = modifVertex;
                }
            }
        }

        private void InteractiveSurface_UpdateMesh()
        {
            MbMeshFilter.mesh.SetVertices(MbWorkingMeshData.vertices);
            MDMeshBase_RecalculateMesh();
        }

        /// <summary>
        /// Modify interactive surface with a specific point, size and vertice direction
        /// </summary>
        /// <param name="WorldPoint">Point of modification (world space)</param>
        /// <param name="Radius">Interaction radius</param>
        /// <param name="Direction">Direction for vertices</param>
        public void InteractiveSurface_ModifyMesh(Vector3 WorldPoint, float Radius, Vector3 Direction)
        {
            if (!Application.isPlaying)
                return;
            if (!MbIsInitialized)
                return;

            if (!detectRadiusSize)
                Radius = radius;
            Radius *= radiusMultiplier;

            Thrd_WorldPoint = WorldPoint;
            Thrd_Radius = Radius;
            Thrd_Dir = Direction;
            Thrd_RealPos = transform.position;
            Thrd_RealRot = transform.rotation;
            Thrd_RealSca = transform.localScale;

            //If multithreading enabled, pass data to cross-thread-wrapper
            if (ThreadUseMultithreading)
            {
                if (ThreadIsDone) InteractiveSurface_UpdateMesh();
                else ThreadEvent?.Set();
                return;
            }
            //Otherwise go for the default main thread
            else InteractiveSurface_PushData();

            //Set vertices & continue
            if (!customInteractionSpeed)
                InteractiveSurface_UpdateMesh();

            checkForUpdate_Repair = true;
            checkForUpdate_InterSpeed = true;
        }

        /// <summary>
        /// Reset current surface (Reset all vertices to their initial positions)
        /// </summary>
        public void InteractiveSurface_ResetSurface()
        {
            for (int i = 0; i < MbWorkingMeshData.vertices.Length; i++)
                MbWorkingMeshData.vertices[i] = MbBackupMeshData.vertices[i];
        }

        //------External thread param wrapper----
        private Vector3 Thrd_WorldPoint;
        private float Thrd_Radius;
        private Vector3 Thrd_Dir;
        private Vector3 Thrd_RealPos;
        private Vector3 Thrd_RealSca;
        private Quaternion Thrd_RealRot;
        //--------------------------------

        /// <summary>
        /// Modify current mesh by custom RaycastEvent
        /// </summary>
        public void InteractiveSurface_ModifyMesh(MDM_RaycastEvent RayEvent)
        {
            if (!Application.isPlaying)
                return;
            if (RayEvent == null)
                return;
            if (RayEvent.RayEventHits.Length > 0 && RayEvent.RayEventHits[0].collider.gameObject != this.gameObject)
                return;
            if (detectRadiusSize)
                radius = RayEvent.pointRay ? radius : RayEvent.sphericalRadius;

            foreach (RaycastHit hit in RayEvent.RayEventHits)
                InteractiveSurface_ModifyMesh(hit.point, radius, direction);
        }

        #endregion

        /// <summary>
        /// Main separate thread worker for this modifier
        /// </summary>
        public void MDThreading_ProcessThreadWorker()
        {
            while (true)
            {
                ThreadIsDone = false;
                ThreadEvent?.WaitOne();

                InteractiveSurface_PushData();

                ThreadIsDone = true;
                Thread.Sleep(ThreadSleep);

                ThreadEvent?.Reset();
            }
        }
    }
}

#if UNITY_EDITOR
namespace MD_Package_Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MDM_InteractiveSurface))]
    public sealed class MDM_InteractiveLandscape_Editor : MD_ModifierBase_Editor
    {
        private MDM_InteractiveSurface mb;

        public override void OnEnable()
        {
            mMeshBase = (MD_MeshBase)target;
            mModifierBase = (MD_ModifierBase)target;
            mb = (MDM_InteractiveSurface)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            MDE_l("Interactive Surface Modifier", true);
            MDE_s();
            MDE_v();
            MDE_l("General Settings", true);
            MDE_v();
            MDE_DrawProperty("direction", "Overall Direction", "Main direction of the vertices after interaction", identOffset: true);
            MDE_v();
            MDE_DrawProperty("exponentialDeformation", "Exponential Deform", "If enabled, the mesh will be deformed expontentially (the results will be much smoother)", identOffset: true);
            if (mb.exponentialDeformation)
            {
                MDE_plus();
                MDE_DrawProperty("radiusSoftness", "Radius Softness", "If 'Exponential Deform' is enabled, vertices inside the Radius will be instantly affected. This value tells how 'soft' the radius should be to outer vertices nearly touching the inner radius", identOffset: true);
                MDE_minus();
            }
            MDE_ve();
            if (mb.rigidbodiesAllowed)
            {
                MDE_v();
                MDE_DrawProperty("detectRadiusSize", "Detect Radius Size", "Adjust radius size by the collided objects. This will try to auto-detect the interaction radius with the collided rigidbody transform scales (recommended if 'Allow Rigidbodies' is enabled)", identOffset: true);
                if (!mb.detectRadiusSize)
                    MDE_DrawProperty("radius", "Interactive Radius", "Radius of vertices to be interacted with. Please keep in mind that if the input for mesh modify is RayEvent with Spherical Raycast, this field is no more required as the radius is received from the RayEvent - Non-Pointed Ray Radius", identOffset: true);
                MDE_ve();
            }
            else MDE_DrawProperty("radius", "Interactive Radius", "Radius of vertices to be interacted. Please keep in mind that if the input for mesh modify is RayEvent with Spherical Raycast, this field is no more required", identOffset: true);
            MDE_DrawProperty("radiusMultiplier", "Radius Multiplier", "General radius multiplier. Multiplies the radius constant or auto-detected radius - default value is 1", identOffset: true);
            MDE_ve();
            MDE_s(20);
            MDE_l("Conditions", true);
            MDE_v();
            MDE_DrawProperty("rigidbodiesAllowed", "Allow Rigidbodies", "Allow Collision Enter & Collision Stay functions for Rigidbodies & other physically-based entities", identOffset: true);
            if (mb.rigidbodiesAllowed)
            {
                MDE_plus();
                MDE_DrawProperty("minimumForceDetection", "Force Detection Level", "Minimum rigidbody velocity detection [zero is default = without detection]", identOffset: true);
                MDE_DrawProperty("collideWithSpecificObjects", "Collision With Specific Tag", "If enabled, the collision will pass only if the tag below matches the collided rigidbody tag", identOffset: true);
                if (mb.collideWithSpecificObjects)
                {
                    MDE_plus();
                    MDE_DrawProperty("collisionTag", "Collision Tag", identOffset: true);
                    MDE_minus();
                }
                MDE_minus();
            }
            MDE_ve();
            if (!mb.ThreadUseMultithreading)
            {
                MDE_s(20);
                MDE_l("Additional Interaction Settings", true);
                MDE_v();
                MDE_DrawProperty("customInteractionSpeed", "Custom Interaction Speed", "If enabled, you will be able to customize vertices speed after its interaction/ collision", identOffset: true);
                if (mb.customInteractionSpeed)
                {
                    MDE_plus();
                    MDE_DrawProperty("interactionSpeed", "Interaction Speed", identOffset: true);
                    MDE_DrawProperty("continuousEffect", "Continuous Effect", "If enabled, interacted vertices will keep moving beyond their initial positions", identOffset: true);
                    MDE_minus();
                }
                MDE_ve();
                if (!mb.continuousEffect)
                {
                    MDE_v();
                    MDE_DrawProperty("restoreSurface", "Restore Mesh", "Restore mesh to its initial state after some time", identOffset: true);
                    if (mb.restoreSurface)
                    {
                        MDE_plus();
                        MDE_DrawProperty("restorationSpeed", "Restoration Speed", identOffset: true);
                        MDE_minus();
                    }
                    MDE_ve();
                }
            }
            MDE_ve();
            MDE_AddMeshColliderRefresher(mb.gameObject);
            MDE_BackToMeshEditor(mb);
        }
    }
}
#endif
