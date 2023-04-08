using System;
using System.Collections.Generic;
using UnityEngine;

namespace MD_Package.Geometry
{
    /// <summary>
    /// MDG(Mesh Deformation Geometry): Mesh Paint.
    /// Complete solution of simple mesh painting in Unity engine for multiple platforms. Apply this script to any gameObject and assign required fields.
    /// Written by Matej Vanco (2018, updated in 2023).
    /// </summary>
    [AddComponentMenu(MD_Debug.ORGANISATION + MD_Debug.PACKAGENAME + "Geometry/Mesh Paint")]
    public sealed class MDG_MeshPaint : MonoBehaviour
    {
        public enum MeshPaintStatus { StartPaint, Painting, EndPaint };
        public MeshPaintStatus paintCurrentPaintStatus;

        //Mesh data
        [SerializeField] private List<Vector3> paintWorkingVertices = new List<Vector3>();
        [SerializeField] private List<int> paintWorkingTriangles = new List<int>();
        [SerializeField] private List<Vector2> paintWorkingUVs = new List<Vector2>();

        [SerializeField] private Transform internal_p1;
        [SerializeField] private Transform internal_p2;
        [SerializeField] private Transform internal_p3;
        [SerializeField] private Transform internal_p4;
        [SerializeField] private Transform internal_p5;
        [SerializeField] private Transform internal_p6;
        [SerializeField] private Transform internal_p7;
        [SerializeField] private Transform internal_p8;

        [SerializeField] private Transform internal_BrushHelper;
        [SerializeField] private Transform internal_BrushRoot;

        // Platform selection
        public enum MeshPaintTargetPlatform { PC, VR, Mobile };
        public MeshPaintTargetPlatform paintTargetPlatform = MeshPaintTargetPlatform.PC;

        // Legacy input handling
        public KeyCode paintPCLegacyInput = KeyCode.Mouse0;

        // Brush settings
        public bool paintBrushUniformSize = true;
        public float paintBrushSize = 0.5f;
        public Vector3 paintBrushVectorSize = Vector3.one;

        public bool paintSmoothBrushMovement = true;
        public float paintSmoothBrushMovementSpeed = 10f;
        public bool paintSmoothBrushRotation = true;
        public float paintSmoothBrushRotationSpeed = 20;

        public bool paintVertexDistanceLimitation = true;
        public float paintMinVertexDistance = 0.5f;

        public bool paintConnectMeshOnRelease = false;

        public enum MeshPaintType { DrawOnScreen, DrawOnRaycastHit, CustomDraw };
        public MeshPaintType paintPaintingType;
        public enum MeshPaintRotationType { FollowOneAxis, FollowSpatialAxis };
        public MeshPaintRotationType paintPaintingRotationType = MeshPaintRotationType.FollowOneAxis;
        public Vector3 paintRotationOffsetEuler = new Vector3(0f, 0f, -1f);

        public enum MeshPaintGeometryType { Plane, Triangle, Cube};
        public MeshPaintGeometryType paintGeometryType;

        // Type - screen
        public bool paint_TypeScreen_UseMainCamera = true;
        public Camera paint_TypeScreen_TargetCamera;
        public float paint_TypeScreen_Depth = 10.0f;
        // Type - raycast
        public bool paint_TypeRaycast_RaycastFromCursor = true;
        public Transform paint_TypeRaycast_RaycastOriginFORWARD;
        public LayerMask paint_TypeRaycast_AllowedLayers = -1;
        public bool paint_TypeRaycast_CastAllObjects = true;
        public string paint_TypeRaycast_TagForRaycast;
        public Vector3 paint_TypeRaycast_BrushOffset = new Vector3(0, 1, 0);
        public bool paint_TypeRaycast_IgnoreSelfCasting = true;
        // Type - custom
        public bool paint_TypeCustom_IsPainting = false;
        private bool paint_TypeCustom_PaintingStarted = false;
        public bool paint_TypeCustom_CustomBrushTransform = true;
        public bool paint_TypeCustom_EnableSmartRotation = true;
        public Transform paint_TypeCustom_BrushParent;

        // Appearance
        public int paintSelectedAppearanceSlot = 0;
        public bool paintUseMaterialSlots = false;
        public Material[] paintMaterialSlots;
        public Color[] paintColorSlots = new Color[1] { Color.blue };

        public bool paintUseCustomBrushTransform = false;
        public Transform paintCompleteCustomBrush;
        public bool paintHideCustomBrushIfNotRaycasting = true;

        public bool paintHandleMeshCollider = true;

        // Public events to subscribe

        /// <summary>
        /// Invoked when the mesh has been painted (Invoked once at the end of the painting) - includes the painted mesh-gameObject in the parameters
        /// </summary>
        public event Action<GameObject> PaintOnMeshPaintEnd;
        /// <summary>
        /// Invoked when the mesh is going to be painted (Invoked once at the beginning of the painting) - includes the painting mesh-gameObject in the parameters
        /// </summary>
        public event Action<GameObject> PaintOnMeshPaintStart;

        // Private fields

        private GameObject tempPaintingMesh;
        private Vector3 lastBrushPosition;
        private Vector3 lastDrawingPosition;
        private Quaternion lastDrawingRotation;

        private void Awake()
        {
            if (!internal_BrushHelper)
                internal_BrushHelper = new GameObject("MD_MESHPAINT_BrushHelper").transform;
            internal_BrushHelper.hideFlags = HideFlags.HideInHierarchy;
            if (!internal_BrushRoot)
                internal_BrushRoot = new GameObject("MD_MESHPAINT_BrushRoot").transform;
            internal_BrushRoot.hideFlags = HideFlags.HideInHierarchy;

            Vector3 vp1 = Vector3.zero, vp2 = Vector3.zero, vp3 = Vector3.zero, vp4 = Vector3.zero,
                vp5 = Vector3.zero, vp6 = Vector3.zero, vp7 = Vector3.zero, vp8 = Vector3.zero;

            switch(paintGeometryType)
            {
                case MeshPaintGeometryType.Plane:
                    vp1 = new Vector3(0.5f, 0, 0);
                    vp2 = new Vector3(-0.5f, 0, 0);
                    vp3 = new Vector3(-0.5f, 0, 0.5f);
                    vp4 = new Vector3(0.5f, 0, 0.5f);
                    break;

                case MeshPaintGeometryType.Triangle:
                    vp1 = new Vector3(0.5f, -0.5f, 0);
                    vp2 = new Vector3(-0.5f, -0.5f, 0);
                    vp3 = new Vector3(0, 0.5f, 0);
                    break;

                case MeshPaintGeometryType.Cube:
                    vp1 = new Vector3(-0.5f, -0.5f, -0.5f);
                    vp2 = new Vector3(-0.5f, 0.5f, -0.5f);
                    vp3 = new Vector3(0.5f, 0.5f, -0.5f);
                    vp4 = new Vector3(0.5f, -0.5f, -0.5f);

                    vp5 = new Vector3(-0.5f, -0.5f, 0.5f);
                    vp6 = new Vector3(-0.5f, 0.5f, 0.5f);
                    vp7 = new Vector3(0.5f, 0.5f, 0.5f);
                    vp8 = new Vector3(0.5f, -0.5f, 0.5f);
                    break;
            }

            if (!internal_p1)
                internal_p1 = new GameObject("MD_MESHPAINT_P1").transform;
            internal_p1.parent = internal_BrushRoot;
            internal_p1.localPosition = vp1;
            if (!internal_p2)
                internal_p2 = new GameObject("MD_MESHPAINT_P2").transform;
            internal_p2.parent = internal_BrushRoot;
            internal_p2.localPosition = vp2;
            if (!internal_p3)
                internal_p3 = new GameObject("MD_MESHPAINT_P3").transform;
            internal_p3.parent = internal_BrushRoot;
            internal_p3.localPosition = vp3;

            if (paintGeometryType == MeshPaintGeometryType.Plane)
            {
                if (!internal_p4)
                    internal_p4 = new GameObject("MD_MESHPAINT_P4").transform;
                internal_p4.parent = internal_BrushRoot;
                internal_p4.localPosition = vp4;
            }
            else if (paintGeometryType == MeshPaintGeometryType.Cube)
            {
                if (!internal_p4)
                    internal_p4 = new GameObject("MD_MESHPAINT_P4").transform;
                internal_p4.parent = internal_BrushRoot;
                internal_p4.localPosition = vp4;
                if (!internal_p5)
                    internal_p5 = new GameObject("MD_MESHPAINT_P5").transform;
                internal_p5.parent = internal_BrushRoot;
                internal_p5.localPosition = vp5;
                if (!internal_p6)
                    internal_p6 = new GameObject("MD_MESHPAINT_P6").transform;
                internal_p6.parent = internal_BrushRoot;
                internal_p6.localPosition = vp6;
                if (!internal_p7)
                    internal_p7 = new GameObject("MD_MESHPAINT_P7").transform;
                internal_p7.parent = internal_BrushRoot;
                internal_p7.localPosition = vp7;
                if (!internal_p8)
                    internal_p8 = new GameObject("MD_MESHPAINT_P8").transform;
                internal_p8.parent = internal_BrushRoot;
                internal_p8.localPosition = vp8;
            }
            else
            {
                if (internal_p4)
                    Destroy(internal_p4.gameObject);
                if (internal_p5)
                    Destroy(internal_p5.gameObject);
                if (internal_p6)
                    Destroy(internal_p6.gameObject);
                if (internal_p7)
                    Destroy(internal_p7.gameObject);
                if (internal_p8)
                    Destroy(internal_p8.gameObject);
            }
        }

        private void Start()
        {
            if (paintUseMaterialSlots && paintMaterialSlots.Length == 0)
                MD_Debug.Debug(this, "At least one material must be assigned", MD_Debug.DebugType.Error);
            else if (!paintUseMaterialSlots && paintColorSlots.Length == 0)
                MD_Debug.Debug(this, "At least one color must be added", MD_Debug.DebugType.Error);

            if (paintPaintingType == MeshPaintType.DrawOnScreen && paint_TypeScreen_UseMainCamera && Camera.main == null)
                MD_Debug.Debug(this, "Main Camera is null. Please choose one camera and change its tag to the MainCamera", MD_Debug.DebugType.Error);
            else if (paintPaintingType == MeshPaintType.DrawOnScreen && !paint_TypeScreen_UseMainCamera && paint_TypeScreen_TargetCamera == null)
                MD_Debug.Debug(this, "Target camera is null", MD_Debug.DebugType.Error);
        }

        private void Update()
        {
            switch (paintPaintingType)
            {
                case MeshPaintType.DrawOnScreen:
                    MeshPaint_UpdateOnScreen();
                    break;
                case MeshPaintType.DrawOnRaycastHit:
                    MeshPaint_UpdateOnRaycast();
                    break;
                case MeshPaintType.CustomDraw:
                    MeshPaint_UpdateOnCustom();
                    break;
            }

            if (paint_TypeCustom_IsPainting)
            {
                if (!paint_TypeCustom_PaintingStarted)
                    MeshPaint_Paint(internal_BrushRoot.position, MeshPaintStatus.StartPaint);
                else
                    MeshPaint_Paint(internal_BrushRoot.position, MeshPaintStatus.Painting);
            }
            else
            {
                if (paint_TypeCustom_PaintingStarted)
                    MeshPaint_Paint(internal_BrushRoot.position, MeshPaintStatus.EndPaint);
            }

            lastBrushPosition = internal_BrushHelper.transform.position;

            if (paintPaintingType == MeshPaintType.CustomDraw)
            {
                if (paint_TypeCustom_CustomBrushTransform && paint_TypeCustom_BrushParent)
                    internal_BrushHelper.position = paint_TypeCustom_BrushParent.position;
                else
                    internal_BrushHelper.position = transform.position;
            }

            if (paintUseCustomBrushTransform)
            {
                if (paintCompleteCustomBrush)
                {
                    paintCompleteCustomBrush.SetPositionAndRotation(internal_BrushRoot.position, internal_BrushRoot.rotation);
                    paintCompleteCustomBrush.localScale = paintBrushUniformSize ? Vector3.one * paintBrushSize : Vector3.one + paintBrushVectorSize;
                }
            }
        }

        #region Private methods & essential generators

        //---TYPE _ SCREEN
        private void MeshPaint_UpdateOnScreen()
        {
            Vector3 location = MeshPaint_GetScreenPosition();
            Vector3 rotationdirection = internal_BrushHelper.InverseTransformDirection(location - lastBrushPosition);
            if (rotationdirection != Vector3.zero)
            {
                if (paintPaintingRotationType == MeshPaintRotationType.FollowOneAxis)
                    lastDrawingRotation = Quaternion.LookRotation(rotationdirection, paintRotationOffsetEuler);
                else
                    lastDrawingRotation = Quaternion.LookRotation(rotationdirection);
            }

            internal_BrushHelper.position = location;

            if (paintSmoothBrushMovement)
                internal_BrushRoot.position = Vector3.Lerp(internal_BrushRoot.position, internal_BrushHelper.position, Time.deltaTime * paintSmoothBrushMovementSpeed);
            else
                internal_BrushRoot.position = internal_BrushHelper.position;

            if (paintSmoothBrushRotation)
                internal_BrushRoot.rotation = Quaternion.Lerp(internal_BrushRoot.rotation, lastDrawingRotation, Time.deltaTime * paintSmoothBrushRotationSpeed);
            else
                internal_BrushRoot.rotation = lastDrawingRotation;

            if (!paint_TypeCustom_IsPainting)
            {
                if (MeshPaint_GetInputInternal(false))
                    paint_TypeCustom_IsPainting = true;
            }
            else
            {
                if (MeshPaint_GetInputInternal(true))
                    paint_TypeCustom_IsPainting = false;
            }
        }
        private Vector3 MeshPaint_GetScreenPosition()
        {
            Vector3 p = Input.mousePosition;
            p.z = paint_TypeScreen_Depth;
            if (paint_TypeScreen_UseMainCamera)
                paint_TypeScreen_TargetCamera = Camera.main;

            p = paint_TypeScreen_TargetCamera.ScreenToWorldPoint(p);
            return p;
        }

        //---TYPE _ RAYCAST
        private void MeshPaint_UpdateOnRaycast()
        {
            Vector3 location = MeshPaint_GetRaycastPosition();
            location += paint_TypeRaycast_BrushOffset;

            Vector3 rotationdirection = internal_BrushHelper.InverseTransformDirection(location - lastBrushPosition);

            internal_BrushHelper.position = location;

            if (rotationdirection != Vector3.zero)
            {
                if (paintPaintingRotationType == MeshPaintRotationType.FollowOneAxis)
                    lastDrawingRotation = Quaternion.LookRotation(rotationdirection, paintRotationOffsetEuler);
                else
                    lastDrawingRotation = Quaternion.LookRotation(rotationdirection);
            }

            if (paintSmoothBrushMovement)
                internal_BrushRoot.position = Vector3.Lerp(internal_BrushRoot.position, internal_BrushHelper.position, Time.deltaTime * paintSmoothBrushMovementSpeed);
            else
                internal_BrushRoot.position = internal_BrushHelper.position;

            if (paintSmoothBrushRotation)
                internal_BrushRoot.rotation = Quaternion.Lerp(internal_BrushRoot.rotation, lastDrawingRotation, Time.deltaTime * paintSmoothBrushRotationSpeed);
            else
                internal_BrushRoot.rotation = lastDrawingRotation;

            if (location == Vector3.zero)
                return;

            if (!paint_TypeCustom_IsPainting)
            {
                if (MeshPaint_GetInputInternal())
                    paint_TypeCustom_IsPainting = true;
            }
            else
            {
                if (MeshPaint_GetInputInternal(true))
                    paint_TypeCustom_IsPainting = false;
            }

        }
        private Vector3 MeshPaint_GetRaycastPosition()
        {
            Camera c = null;
            if (paint_TypeScreen_UseMainCamera)
                c = Camera.main;
            else
                c = paint_TypeScreen_TargetCamera;

            Vector3 result = Vector3.zero;
            Ray r = new Ray();
            if (paint_TypeRaycast_RaycastFromCursor)
                r = c.ScreenPointToRay(Input.mousePosition);
            else
                r = new Ray(paint_TypeRaycast_RaycastOriginFORWARD.position, paint_TypeRaycast_RaycastOriginFORWARD.forward);

            RaycastHit hit = new RaycastHit();

            if (paintUseCustomBrushTransform)
                paintCompleteCustomBrush.gameObject.SetActive(true);

            if (Physics.Raycast(r, out hit, Mathf.Infinity, paint_TypeRaycast_AllowedLayers))
            {
                if (paint_TypeRaycast_CastAllObjects)
                {
                    if (hit.collider)
                        return hit.point;
                }
                else
                {
                    if (hit.collider.tag == paint_TypeRaycast_TagForRaycast)
                        return hit.point;
                    else if (paintUseCustomBrushTransform && paintHideCustomBrushIfNotRaycasting)
                    {
                        paintCompleteCustomBrush.gameObject.SetActive(false);
                        paint_TypeCustom_IsPainting = false;
                    }
                    else
                        paint_TypeCustom_IsPainting = false;
                }
            }
            else if (paintUseCustomBrushTransform && paintHideCustomBrushIfNotRaycasting)
            {
                paintCompleteCustomBrush.gameObject.SetActive(false);
                paint_TypeCustom_IsPainting = false;
            }
            else
                paint_TypeCustom_IsPainting = false;

            return Vector3.zero;
        }

        //---TYPE _ CUSTOM
        private void MeshPaint_UpdateOnCustom()
        {
            if (!paint_TypeCustom_CustomBrushTransform)
                return;

            Vector3 rotationdirection = internal_BrushHelper.InverseTransformDirection((paint_TypeCustom_BrushParent == null ? transform.position : paint_TypeCustom_BrushParent.position) - lastBrushPosition);
            if (rotationdirection != Vector3.zero && paint_TypeCustom_EnableSmartRotation)
            {
                if (paintPaintingRotationType == MeshPaintRotationType.FollowOneAxis)
                    lastDrawingRotation = Quaternion.FromToRotation(Vector3.forward, rotationdirection);
                else
                    lastDrawingRotation = Quaternion.LookRotation(rotationdirection);
            }

            if (paintSmoothBrushMovement)
                internal_BrushRoot.position = Vector3.Lerp(internal_BrushRoot.position, internal_BrushHelper.position, Time.deltaTime * paintSmoothBrushMovementSpeed);
            else
                internal_BrushRoot.position = internal_BrushHelper.position;

            if (paintSmoothBrushRotation)
                internal_BrushRoot.rotation = Quaternion.Lerp(internal_BrushRoot.rotation, lastDrawingRotation, Time.deltaTime * paintSmoothBrushRotationSpeed);
            else
                internal_BrushRoot.rotation = lastDrawingRotation;
        }


        // Input and mesh generation
        private bool MeshPaint_GetInputInternal(bool isUp = false)
        {
            switch (paintTargetPlatform)
            {
                case MeshPaintTargetPlatform.PC:
                    if (!isUp)
                        return Input.GetKeyDown(paintPCLegacyInput);
                    else
                        return Input.GetKeyUp(paintPCLegacyInput);

                case MeshPaintTargetPlatform.Mobile:
                    if (!isUp && Input.touchCount > 0)
                        return true;
                    else if (isUp && Input.touchCount == 0)
                        return true;
                    else
                        return false;

                default:
                    return false;
            }
        }

        private void MeshPaint_ChangeVectorBrushSize(Vector3 size)
        {
            if (paintGeometryType == MeshPaintGeometryType.Triangle)
            {
                internal_p1.transform.localPosition = new Vector3(size.x, -size.y, 0);
                internal_p2.transform.localPosition = new Vector3(-size.x, -size.y, 0);
                internal_p3.transform.localPosition = new Vector3(0, size.y, 0);
            }
            else if (paintGeometryType == MeshPaintGeometryType.Plane)
            {
                internal_p1.transform.localPosition = new Vector3(size.x, 0, -size.z);
                internal_p2.transform.localPosition = new Vector3(-size.x, 0, -size.z);
                internal_p3.transform.localPosition = new Vector3(-size.x, 0, size.z);
                internal_p4.transform.localPosition = new Vector3(size.x, 0, size.z);
            }
            else if (paintGeometryType == MeshPaintGeometryType.Cube)
            {
                internal_p1.transform.localPosition = new Vector3(-size.x, -size.y, -size.z);
                internal_p2.transform.localPosition = new Vector3(-size.x, size.y, -size.z);
                internal_p3.transform.localPosition = new Vector3(size.x, size.y, -size.z);
                internal_p4.transform.localPosition = new Vector3(size.x, -size.y, -size.z);

                internal_p5.transform.localPosition = new Vector3(-size.x, -size.y, size.z);
                internal_p6.transform.localPosition = new Vector3(-size.x, size.y, size.z);
                internal_p7.transform.localPosition = new Vector3(size.x, size.y, size.z);
                internal_p8.transform.localPosition = new Vector3(size.x, -size.y, size.z);
            }
        }

        /// <summary>
        /// Create new painting pattern such as New Mesh Filter & make it ready for painting
        /// </summary>
        private void MeshPaint_CreateNewPaintPattern(string meshName = "PaintingMesh", bool addCollider = true)
        {
            GameObject newMesh = new GameObject(meshName);
            MeshFilter mf = newMesh.AddComponent<MeshFilter>();
            newMesh.AddComponent<MeshRenderer>();
            Renderer mr = newMesh.GetComponent<Renderer>();
            Mesh m = new Mesh();
            mf.mesh = m;

            if (paintUseMaterialSlots)
                mr.material = paintMaterialSlots[paintSelectedAppearanceSlot];
            else
            {
                Material mat = new Material(Utilities.MD_Utilities.MD_Specifics.GetProperPipelineDefaultShader());
                mat.color = paintColorSlots[paintSelectedAppearanceSlot];
                mr.material = mat;
            }

            if (addCollider)
            {
                newMesh.AddComponent<MeshCollider>();
                newMesh.layer = 2;
            }

            tempPaintingMesh = newMesh;
            PaintOnMeshPaintStart?.Invoke(tempPaintingMesh);
        }

        private void MeshPaint_DrawTriangle(Vector3 currentDrawingPosition, MeshPaintStatus meshPaintStatus)
        {
            Vector3[] newVertArray = new Vector3[] { internal_p1.position, internal_p2.position, internal_p3.position };

            if (meshPaintStatus == MeshPaintStatus.EndPaint)
                newVertArray = new Vector3[] { paintWorkingVertices[paintWorkingVertices.Count - 3], paintWorkingVertices[paintWorkingVertices.Count - 2], paintWorkingVertices[paintWorkingVertices.Count - 1] };

            int last = 0;
            if (paintWorkingTriangles.Count > 0)
                last = paintWorkingVertices.Count-1;
            int[] newTrinArray = new int[] { };

            if (meshPaintStatus == MeshPaintStatus.StartPaint)
            {
                lastDrawingPosition = currentDrawingPosition;
                MeshPaint_CreateNewPaintPattern(addCollider: paintHandleMeshCollider);

                paintWorkingVertices.Clear();
                paintWorkingTriangles.Clear();
                paintWorkingUVs.Clear();

                newTrinArray = new int[]
                 {
                0,1,2
                 };

                paint_TypeCustom_PaintingStarted = true;
            }
            else if (meshPaintStatus == MeshPaintStatus.Painting)
            {
                lastDrawingPosition = currentDrawingPosition;
                newTrinArray = new int[]
                {
                    //----Left-Down
                    last-1,last+2, last+3,
                     //----Left-Up
                    last+3,last, last-1,

                     //----Right-Down
                    last+1,last-2, last,
                     //----Right-Up
                    last, last+3, last+1,

                    //----Down-Right-Down
                    last+1,last+2, last-1,
                     //----Down-Left-Down
                    last-1, last-2, last+1,
                };
            }
            else if (meshPaintStatus == MeshPaintStatus.EndPaint)
            {
                if (!paintConnectMeshOnRelease)
                {
                    newTrinArray = new int[]
                    {
                    //----New Front Side
                    last+3, last+2, last+1,
                    };
                }
                else
                {
                    newTrinArray = new int[]
                    {
                        //----Left-Down
                        last-1, 1, 2,
                         //----Left-Up
                        last-1, 2, last,

                         //----Right-Down
                        0, last-2, last,
                         //----Right-Up
                        0, last, 2,

                        //----Down-Right-Down
                        0, 1, last-1,
                         //----Down-Left-Down
                        0, last-1, last-2,
                    };
                }

                paint_TypeCustom_PaintingStarted = false;

                if (paint_TypeRaycast_IgnoreSelfCasting)
                    tempPaintingMesh.layer = 2;
                else
                    tempPaintingMesh.layer = 0;

                PaintOnMeshPaintEnd?.Invoke(tempPaintingMesh);
            }

            if(newVertArray != null)
                paintWorkingVertices.AddRange(newVertArray);

            paintWorkingTriangles.AddRange(newTrinArray);
            paintWorkingUVs.AddRange(new List<Vector2> { new Vector2(0.5f, -0.5f), new Vector2(-0.5f, -0.5f), new Vector2(0, 0.5f) });
        }

        private int planeUvFixer;
        private void MeshPaint_DrawPlane(Vector3 currentDrawingPosition, MeshPaintStatus meshPaintStatus)
        {
            Vector3[] newVertArray = null;
            if(meshPaintStatus == MeshPaintStatus.StartPaint)
                newVertArray = new Vector3[] { internal_p1.position, internal_p2.position, internal_p3.position, internal_p4.position };
            else if(meshPaintStatus == MeshPaintStatus.Painting)
                newVertArray = new Vector3[] { internal_p3.position, internal_p4.position };

            int last = 0;
            if (paintWorkingTriangles.Count > 0)
                last = paintWorkingVertices.Count-1;

            int[] newTrinArray = new int[] { };

            if (meshPaintStatus == MeshPaintStatus.StartPaint)
            {
                planeUvFixer = 0;
                lastDrawingPosition = currentDrawingPosition;
                MeshPaint_CreateNewPaintPattern(addCollider: paintHandleMeshCollider);

                paintWorkingVertices.Clear();
                paintWorkingTriangles.Clear();
                paintWorkingUVs.Clear();

                newTrinArray = new int[]
                 {
                    0,1,2,
                    0,2,3
                 };

                paint_TypeCustom_PaintingStarted = true;
            }
            else if (meshPaintStatus == MeshPaintStatus.Painting)
            {
                if (planeUvFixer == 0)
                    planeUvFixer = 1;
                else
                    planeUvFixer = 0;
                lastDrawingPosition = currentDrawingPosition;
                newTrinArray = new int[]
                {
                    //----Right
                    last, last-1, last+2,
                     //----Left
                    last-1,last+1, last+2,

                };
            }
            else if (meshPaintStatus == MeshPaintStatus.EndPaint)
            {
                if(paintConnectMeshOnRelease)
                {
                    newTrinArray = new int[]
                    {
                        //----Right
                        last, last-1, 1,
                         //----Left
                        last,1, 0,
                     };
                }

                paint_TypeCustom_PaintingStarted = false;

                if (paint_TypeRaycast_IgnoreSelfCasting)
                    tempPaintingMesh.layer = 2;
                else
                    tempPaintingMesh.layer = 0;
                PaintOnMeshPaintEnd?.Invoke(tempPaintingMesh);
            }

            if (newVertArray != null)
                paintWorkingVertices.AddRange(newVertArray);

            paintWorkingTriangles.AddRange(newTrinArray);
            if (meshPaintStatus == MeshPaintStatus.StartPaint)
                paintWorkingUVs.AddRange(new List<Vector2> { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) });
            else if (meshPaintStatus == MeshPaintStatus.Painting)
            {
                if (planeUvFixer == 0)
                    paintWorkingUVs.AddRange(new List<Vector2> { new Vector2(0, 0), new Vector2(1, 0) });
                else
                    paintWorkingUVs.AddRange(new List<Vector2> { new Vector2(0, 1), new Vector2(1, 1) });
            }
        }

        private void MeshPaint_DrawCube(Vector3 currentDrawingPosition, MeshPaintStatus meshPaintStatus)
        {
            Vector3[] newVertArray = null;

            int last = 0;
            if (paintWorkingTriangles.Count > 0)
                last = paintWorkingVertices.Count-1;
            int[] newTrinArray = new int[] { };

            if (meshPaintStatus == MeshPaintStatus.StartPaint)
            {
                newVertArray = new Vector3[] { internal_p1.position, internal_p2.position, internal_p3.position, internal_p4.position,
                 internal_p5.position, internal_p6.position, internal_p7.position, internal_p8.position};

                planeUvFixer = 0;
                lastDrawingPosition = currentDrawingPosition;
                MeshPaint_CreateNewPaintPattern(addCollider: paintHandleMeshCollider);

                paintWorkingVertices.Clear();
                paintWorkingTriangles.Clear();
                paintWorkingUVs.Clear();

                newTrinArray = new int[]
                 {
                    //---Back Side
                    3,0,1,
                    3,1,2,

                    //---Front Side
                    4,7,6,
                    4,6,5,

                    //---Right Side
                    7,3,2,
                    7,2,6,

                    //---Left Side
                    0,4,5,
                    0,5,1,

                    //---Upper Side
                    2,1,5,
                    2,5,6,

                    //---Lower Side
                    0,3,7,
                    0,7,4
                 };

                paint_TypeCustom_PaintingStarted = true;
            }
            else if (meshPaintStatus == MeshPaintStatus.Painting)
            {
                newVertArray = new Vector3[] {internal_p5.position, internal_p6.position, internal_p7.position, internal_p8.position};

                if (planeUvFixer == 0)
                    planeUvFixer = 1;
                else
                    planeUvFixer = 0;
                lastDrawingPosition = currentDrawingPosition;
                newTrinArray = new int[]
                {
                    //---Right Side
                    last+4,last,last-1,
                    last+4,last-1,last+3,

                    //---Left Side
                    last-3,last+1,last+2,
                    last-3,last+2,last-2,

                    //---Upper Side
                    last-1,last-2,last+2,
                    last-1,last+2,last+3,

                    //---Lower Side
                    last-3,last,last+4,
                    last-3,last+4,last+1
                };
            }
            else if (meshPaintStatus == MeshPaintStatus.EndPaint)
            {
                if (paintConnectMeshOnRelease)
                {
                    newTrinArray = new int[]
                    {
                        //---Right Side
                        3,last,last-1,
                        3,last-1,2,

                        //---Left Side
                        last-3,0,1,
                        last-3,1,last-2,

                        //---Upper Side
                        last-1,last-2,1,
                        last-1,1,2,

                        //---Lower Side
                        3,last-3,last,
                        3,0,last-3
                    };
                }
                else
                {
                    newTrinArray = new int[]
                     {
                    //---Last Front Side
                    last-3,last,last-1,
                    last-3,last-1,last-2,
                     };
                }

                paint_TypeCustom_PaintingStarted = false;

                if (paint_TypeRaycast_IgnoreSelfCasting)
                    tempPaintingMesh.layer = 2;
                else
                    tempPaintingMesh.layer = 0;

                PaintOnMeshPaintEnd?.Invoke(tempPaintingMesh);
            }

            if(newVertArray != null)
                paintWorkingVertices.AddRange(newVertArray);

            paintWorkingTriangles.AddRange(newTrinArray);

            if (meshPaintStatus == MeshPaintStatus.StartPaint)
                paintWorkingUVs.AddRange(new List<Vector2> { new Vector2(-0.4f,0.4f), new Vector2(0,0.2f), new Vector2(-0.2f,-0.4f), new Vector2(-0.4f,-0.4f),
                new Vector2(0.4f,0.4f),new Vector2(-0.2f,0),new Vector2(0.2f,-0.4f),new Vector2(-0.2f,0)});
            else if (meshPaintStatus == MeshPaintStatus.Painting)
            {
                if(planeUvFixer == 0)
                    paintWorkingUVs.AddRange(new List<Vector2> { new Vector2(-0.4f, 0.4f), new Vector2(0, 0.4f), new Vector2(0, -0.4f), new Vector2(-0.4f, -0.4f) });
                else
                    paintWorkingUVs.AddRange(new List<Vector2> { new Vector2(0.2f, 0.2f), new Vector2(0.4f, 0.2f), new Vector2(0.4f, -0.5f), new Vector2(0.2f, -0.4f) });
            }
        }

        #endregion

        #region Public methods & general-use APi
       
        /// <summary>
        /// Paint mesh on the specific location by the selected method
        /// </summary>
        public void MeshPaint_Paint(Vector3 currentPosition, MeshPaintStatus meshPaintStatus)
        {
            if (paintVertexDistanceLimitation && Vector3.Distance(currentPosition, lastDrawingPosition) < paintMinVertexDistance)
                return;

            if (UnityEngine.EventSystems.EventSystem.current && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;

            MeshPaint_ChangeVectorBrushSize(paintBrushUniformSize ? Vector3.one * paintBrushSize : paintBrushVectorSize);

            if (paintGeometryType == MeshPaintGeometryType.Triangle)
                MeshPaint_DrawTriangle(currentPosition, meshPaintStatus);
            else if (paintGeometryType == MeshPaintGeometryType.Plane)
                MeshPaint_DrawPlane(currentPosition, meshPaintStatus);
            else if (paintGeometryType == MeshPaintGeometryType.Cube)
                MeshPaint_DrawCube(currentPosition, meshPaintStatus);

            MeshFilter Meshf = tempPaintingMesh.GetComponent<MeshFilter>();
            Meshf.mesh.vertices = paintWorkingVertices.ToArray();
            Meshf.mesh.triangles = paintWorkingTriangles.ToArray();
            Meshf.mesh.uv = paintWorkingUVs.ToArray();
            if (paintHandleMeshCollider)
                tempPaintingMesh.GetComponent<MeshCollider>().sharedMesh = Meshf.mesh;
            Meshf.mesh.RecalculateNormals();
            Meshf.mesh.RecalculateBounds();
        }

        /// <summary>
        /// Change brush size manually
        /// </summary>
        public void MeshPaint_ChangeBrushSize(float size)
        {
            paintBrushSize = size;
            paintBrushVectorSize = Vector3.one * size;
        }

        /// <summary>
        /// Increase brush size manually
        /// </summary>
        public void MeshPaint_IncreaseBrushSize(float size)
        {
            paintBrushSize += size;
            paintBrushVectorSize += Vector3.one * size;
        }

        /// <summary>
        /// Decrease brush size manually
        /// </summary>
        public void MeshPaint_DecreaseBrushSize(float size)
        {
            paintBrushSize -= size;
            paintBrushVectorSize -= Vector3.one * size;
        }

        /// <summary>
        /// Change brush size manually by UI Slider
        /// </summary>
        public void MeshPaint_ChangeBrushSize(UnityEngine.UI.Slider size)
        {
            paintBrushSize = size.value;
            paintBrushVectorSize = Vector3.one * size.value;
        }

        /// <summary>
        /// Enable/ Disable drawing externally
        /// </summary>
        public void MeshPaint_EnableDisablePainting(bool paintStatus)
        {
            paint_TypeCustom_IsPainting = paintStatus;
        }

        /// <summary>
        /// Change currently selected material/color by index
        /// </summary>
        public void MeshPaint_ChangeAppearanceIndex(int index)
        {
            paintSelectedAppearanceSlot = index;
        }

        /// <summary>
        /// Change geometry type index (0 = Plane, 1 = Triangle, 2 = Cube)
        /// </summary>
        public void MeshPaint_ChangeGeometryType(int geometryIndex)
        {
            paintGeometryType = (MeshPaintGeometryType)geometryIndex;
            Awake();
        }


        /// <summary>
        /// Set control input from 3rd party source (such as SteamVR, Oculus or other)
        /// </summary>
        /// <param name="setInputTo">Input down or up?</param>
        public void GlobalReceived_SetControlInput(bool setInputTo)
        {
            paint_TypeCustom_IsPainting = setInputTo;
        }

        #endregion

    }
}
