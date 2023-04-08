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
    /// Essential component for general mesh-vertex-editing at runtime [Non-VR]. Required component for editing: MD_MeshProEditor.
    /// Written by Matej Vanco (2014, updated in 2023).
    /// </summary>
    [AddComponentMenu(MD_Debug.ORGANISATION + MD_Debug.PACKAGENAME + "Mesh Editor Runtime")]
    public sealed class MD_MeshEditorRuntime : MonoBehaviour
    {
        public bool isAxisEditor = false;

        public Camera mainCameraCache;

        private Vector3 centerPoint;
        private Transform selectionHelper;

        #region Non-Axis mesh editor

        public enum VertexControlMode {GrabDropVertex, PushVertex, PullVertex };
        public VertexControlMode nonAxis_vertexControlMode = VertexControlMode.GrabDropVertex;

        public bool nonAxis_isMobileFocused = false;

        // Axis lock
        public bool nonAxis_lockAxisX = false;
        public bool nonAxis_lockAxisY = false;

        // Input
        public KeyCode nonAxis_legacyPCInput = KeyCode.Mouse0;

        // Cursor
        public bool nonAxis_cursorIsOrigin = true;
        public bool nonAxis_lockAndHideCursor = true;

        // Appearance
        public bool nonAxis_switchAppearance = true;
        public Color nonAxis_switchAppearanceToColor = Color.green;
        public bool nonAxis_switchAppearanceUseMaterial = false;
        public Material nonAxis_switchAppearanceMaterialTarget;

        // Pull-Push Settings
        public float nonAxis_pullPushVertexSpeed = 0.15f;
        public float nonAxis_maxMinPullPushDistance = Mathf.Infinity;
        public bool nonAxis_continuousPullPushDetection = false;
        public enum PullPushType { Radial, Directional };
        public PullPushType nonAxis_pullPushType = PullPushType.Directional;

        // Conditions
        public bool nonAxis_allowSpecificPoints = false;
        public string nonAxis_allowedPointsTag;

        // Raycast
        public bool nonAxis_allowBackfaces = true;
        public LayerMask nonAxis_allowedLayerMask = -1;
        public float nonAxis_raycastDistance = 1000.0f;
        public float nonAxis_raycastRadius = 0.25f;

        // DEBUG
        public bool nonAxis_enableGizmos = true;

        public bool NonAxis_InputDown { get; private set; }

        private struct PotentialPoints
        {
            public Transform parent;
            public Transform point;
            public Material material;
            public Color originalCol;
        }
        private readonly List<PotentialPoints> nonAxis_potentialPoints = new List<PotentialPoints>();

        #endregion

        #region Axis mesh editor

        public GameObject axis_axisObject;

        public MD_MeshProEditor axis_targetMeshProEditor;

        public KeyCode axis_LegacyInput_SelectPoints = KeyCode.Mouse0;
        public KeyCode axis_LegacyInput_AddPoints = KeyCode.LeftShift;
        public KeyCode axis_LegacyInput_RemovePoints = KeyCode.LeftAlt;

        public bool axis_inLocalSpace = false;
        public float axis_manipulationSpeed = 16.0f;
        private Color axis_pointColorStorage;
        public Color axis_selectedPointColor = Color.green;
        public Color axis_selectionGridColor = Color.black;

        private bool Axis_IsSelecting = false;
        private bool Axis_IsMoving = false;

        public enum AxisMoveAxisTo {X, Y, Z};
        public AxisMoveAxisTo axis_movePointsToAxis;

        private Vector3 ppAXIS_CursorPosOrigin;
        private readonly List<Transform> ppAXIS_TotalPoints = new List<Transform>();
        private readonly List<Transform> ppAXIS_SelectedPoints = new List<Transform>();
        private GameObject ppAXIS_GroupSelector;
        private readonly List<Transform> ppAXIS_UndoStoredObjects = new List<Transform>();

        #endregion

        // Base setup & initialization
        private void Start () 
        {
            if (mainCameraCache == null)
                mainCameraCache = Camera.main;
            if (mainCameraCache == null && TryGetComponent(out Camera c))
                mainCameraCache = c;

            if (mainCameraCache == null)
            {
                MD_Debug.Debug(this, "Main Camera is missing!", MD_Debug.DebugType.Error);
                return;
            }

            if (isAxisEditor)
            {
                if(axis_targetMeshProEditor == null)
                {
                    MD_Debug.Debug(this, "Target MeshProEditor object is empty! Script was disabled.", MD_Debug.DebugType.Error);
                    this.enabled = false;
                    return;
                }
                axis_axisObject.SetActive(false);
                AXIS_SwitchTarget(axis_targetMeshProEditor);
                return;
            }

            selectionHelper = new GameObject("MeshEditorRuntimeHelper").transform;
        }

        private void Update () 
        {
            if (!isAxisEditor)
                InternalProcess_NonAxisEditor();
            else
                InternalProcess_AxisEditor();
        }

        private void OnDrawGizmos()
        {
            if (isAxisEditor) 
                return;
            if (!nonAxis_enableGizmos)
                return;
            if (nonAxis_cursorIsOrigin) 
                return;
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * nonAxis_raycastDistance);
            Gizmos.DrawWireSphere(transform.position + transform.forward * nonAxis_raycastDistance, nonAxis_raycastRadius);
        }

        private void InternalProcess_NonAxisEditor()
        {
            if (!nonAxis_cursorIsOrigin && nonAxis_lockAndHideCursor && !nonAxis_isMobileFocused)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            //If input is pressed/down, process the runtime editor
            if (NonAxis_InputDown && nonAxis_potentialPoints.Count > 0)
            {
                if (nonAxis_vertexControlMode != VertexControlMode.GrabDropVertex)
                {
                    InternalProcess_ProcessPullPush();
                    if (nonAxis_continuousPullPushDetection) NonAxis_InputDown = false;
                }
                else
                {
                    Vector3 origPos = selectionHelper.position;
                    Vector3 futurePos = mainCameraCache.ScreenToWorldPoint(new Vector3(
                        nonAxis_lockAxisX ? mainCameraCache.WorldToScreenPoint(origPos).x : Input.mousePosition.x,
                        nonAxis_lockAxisY ? mainCameraCache.WorldToScreenPoint(origPos).y : Input.mousePosition.y, 
                        (mainCameraCache.transform.position - centerPoint).magnitude));
                    selectionHelper.position = futurePos;
                }
                //Check for input-UP
                if (!Internal_GetControlInput())
                {
                    if (nonAxis_vertexControlMode == VertexControlMode.GrabDropVertex)
                        foreach (PotentialPoints tr in nonAxis_potentialPoints)
                            tr.point.parent = tr.parent;
                    NonAxis_InputDown = false;
                }
                if (NonAxis_InputDown) return;
            }

            if (!nonAxis_switchAppearance && !Internal_GetControlInput()) return;

            Ray ray = new Ray();

            if (!nonAxis_isMobileFocused)
            {
                if (nonAxis_cursorIsOrigin)
                    ray = mainCameraCache.ScreenPointToRay(Input.mousePosition);
                else
                    ray = new Ray(transform.position, transform.forward);
            }
            else
            {
                if (Input.touchCount > 0)
                    ray = mainCameraCache.ScreenPointToRay(Input.GetTouch(0).position);
            }

            //If input is up, raycast for potential points in sphere radius
            RaycastHit[] raycast = Physics.SphereCastAll(ray, nonAxis_raycastRadius, nonAxis_raycastDistance, nonAxis_allowedLayerMask);

            //Reset a potential points list
            if (nonAxis_potentialPoints.Count > 0)
            {
                if (nonAxis_switchAppearance)
                    foreach (PotentialPoints tr in nonAxis_potentialPoints)
                        InternalProcess_ChangeMaterialToPoints(tr, false);
                nonAxis_potentialPoints.Clear();
            }

            if (raycast.Length == 0) return;

            //Declare a new potential points chain
            foreach (RaycastHit h in raycast)
            {
                if (!h.transform.GetComponentInParent<MD_MeshProEditor>())
                    continue;
                if (nonAxis_allowSpecificPoints && !h.transform.CompareTag(nonAxis_allowedPointsTag))
                    continue;
                if (!nonAxis_allowBackfaces && !h.transform.gameObject.GetComponent<Renderer>().isVisible)
                    continue;
                Renderer r = h.transform.gameObject.GetComponent<Renderer>();
                PotentialPoints ppp = new PotentialPoints() { point = h.transform, parent = h.transform.parent, material = r.material, originalCol = r.material.color };
                nonAxis_potentialPoints.Add(ppp);
                InternalProcess_ChangeMaterialToPoints(ppp, true);
            }

            //Manage final control_down = if pressed, process the runtime editor next frame
            if (Internal_GetControlInput())
            {
                //Getting the center point of all vectors
                Vector3 center = new Vector3(0, 0, 0);
                foreach (PotentialPoints tr in nonAxis_potentialPoints)
                    center += tr.point.position;
                centerPoint = center / nonAxis_potentialPoints.Count;

                selectionHelper.position = mainCameraCache.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, (mainCameraCache.transform.position - centerPoint).magnitude));
                foreach (PotentialPoints tr in nonAxis_potentialPoints)
                {
                    if(nonAxis_vertexControlMode == VertexControlMode.GrabDropVertex)
                        tr.point.parent = selectionHelper;
                    InternalProcess_ChangeMaterialToPoints(tr, false);
                }
                NonAxis_InputDown = true;
            }
        }

        private void InternalProcess_ChangeMaterialToPoints(PotentialPoints p, bool selected)
        {
            if (!nonAxis_switchAppearance)
                return;

            if (selected)
            {
                if (nonAxis_switchAppearanceUseMaterial)
                    p.point.GetComponent<Renderer>().material = nonAxis_switchAppearanceMaterialTarget;
                else
                    p.point.GetComponent<Renderer>().material.color = nonAxis_switchAppearanceToColor;
            }
            else
            {
                if (nonAxis_switchAppearanceUseMaterial)
                    p.point.GetComponent<Renderer>().material = p.material;
                else
                    p.point.GetComponent<Renderer>().material.color = p.originalCol;
            }
        }

        private void InternalProcess_ProcessPullPush()
        {
            foreach (PotentialPoints tr in nonAxis_potentialPoints)
            {
                Vector3 tvector = nonAxis_pullPushType == PullPushType.Radial ? (tr.point.position - centerPoint) : transform.forward;
                float dist = (tr.point.position - centerPoint).magnitude;
                if (nonAxis_vertexControlMode == VertexControlMode.PushVertex && dist > nonAxis_maxMinPullPushDistance)
                    continue;
                if (nonAxis_vertexControlMode == VertexControlMode.PullVertex && dist < nonAxis_maxMinPullPushDistance && nonAxis_maxMinPullPushDistance != Mathf.Infinity)
                    continue;
                tr.point.position += (nonAxis_vertexControlMode == VertexControlMode.PushVertex ? tvector : -tvector) * nonAxis_pullPushVertexSpeed * Time.deltaTime;
            }
        }


        private void InternalProcess_AxisEditor()
        {
            //---BEFORE SELECTION
            if (Input.GetKeyDown(axis_LegacyInput_SelectPoints))
            {
                Ray ray = mainCameraCache.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    bool hitt;
                    switch(hit.collider.name)
                    {
                        case "AXIS_X":
                            Axis_IsMoving = true;
                            axis_movePointsToAxis = AxisMoveAxisTo.X;
                            return;
                        case "AXIS_Y":
                            Axis_IsMoving = true;
                            axis_movePointsToAxis = AxisMoveAxisTo.Y;
                            return;
                        case "AXIS_Z":
                            Axis_IsMoving = true;
                            axis_movePointsToAxis = AxisMoveAxisTo.Z;
                            return;
                        default:
                            hitt = false;
                            break;
                    }
                    if(!hitt)
                    {
                        if(hit.collider.transform.parent != null)
                        {
                            if (InternalAxis_CheckSideFunctions() && ppAXIS_SelectedPoints.Count > 0)
                            {
                                if (Input.GetKey(axis_LegacyInput_AddPoints) && hit.collider.transform.parent == axis_targetMeshProEditor.meshVertexEditor_PointsRoot.transform)
                                {
                                    hit.collider.gameObject.GetComponentInChildren<Renderer>().material.color = axis_selectedPointColor;
                                    hit.collider.gameObject.transform.parent = ppAXIS_GroupSelector.transform;
                                    ppAXIS_SelectedPoints.Add(hit.collider.gameObject.transform);
                                    Axis_IsMoving = true;
                                    axis_axisObject.SetActive(true);

                                    InternalAxis_RefreshBounds();
                                    return;
                                }
                                else if (Input.GetKey(axis_LegacyInput_RemovePoints) && hit.collider.transform.parent == ppAXIS_GroupSelector.transform)
                                {
                                    hit.collider.gameObject.GetComponentInChildren<Renderer>().material.color = axis_pointColorStorage;
                                    hit.collider.gameObject.transform.parent = axis_targetMeshProEditor.meshVertexEditor_PointsRoot;
                                    ppAXIS_SelectedPoints.Remove(hit.collider.gameObject.transform);
                                    Axis_IsMoving = true;
                                    axis_axisObject.SetActive(true);

                                    InternalAxis_RefreshBounds();
                                    return;
                                }
                            }
                            else if (ppAXIS_SelectedPoints.Count == 0 && hit.collider.transform.parent == axis_targetMeshProEditor.meshVertexEditor_PointsRoot.transform)
                            {
                                ppAXIS_SelectedPoints.Add(hit.collider.gameObject.transform);

                                Axis_IsMoving = true;
                                axis_axisObject.SetActive(true);
                                ppAXIS_GroupSelector.transform.position = hit.collider.transform.position;

                                axis_pointColorStorage = hit.collider.transform.GetComponentInChildren<Renderer>().material.color;
                                hit.collider.gameObject.GetComponentInChildren<Renderer>().material.color = axis_selectedPointColor;

                                InternalAxis_RefreshBounds(hit.collider.gameObject.transform);

                                ppAXIS_UndoStoredObjects.Clear();
                                ppAXIS_UndoStoredObjects.Add(ppAXIS_SelectedPoints[0]);
                                return;
                            }
                        }
                    }
                }

                Axis_IsSelecting = true;
                ppAXIS_CursorPosOrigin = Input.mousePosition;
                if (!InternalAxis_CheckSideFunctions())
                {
                    if (ppAXIS_SelectedPoints.Count > 0)
                    {
                        ppAXIS_UndoStoredObjects.Clear();
                        foreach (Transform t in ppAXIS_SelectedPoints)
                        {
                            t.GetComponentInChildren<Renderer>().material.color = axis_pointColorStorage;
                            t.transform.parent = axis_targetMeshProEditor.meshVertexEditor_PointsRoot.transform;
                            ppAXIS_UndoStoredObjects.Add(t);
                        }
                    }
                    axis_axisObject.SetActive(false);
                    ppAXIS_SelectedPoints.Clear();
                }
            }

            if(Axis_IsMoving)
            {
                if (axis_movePointsToAxis == AxisMoveAxisTo.X)
                {
                    float PosFix = 1;
                    if (mainCameraCache.transform.position.z > axis_axisObject.transform.position.z)
                        PosFix *= -1;
                    ppAXIS_GroupSelector.transform.position += (ppAXIS_GroupSelector.transform.right * (Input.GetAxis("Mouse X") * PosFix) * axis_manipulationSpeed) * Time.deltaTime;
                }
                if (axis_movePointsToAxis == AxisMoveAxisTo.Y)
                    ppAXIS_GroupSelector.transform.position += (ppAXIS_GroupSelector.transform.up * Input.GetAxis("Mouse Y") * axis_manipulationSpeed) * Time.deltaTime;
                if (axis_movePointsToAxis == AxisMoveAxisTo.Z)
                {
                    float PosFix = 1;
                    if (mainCameraCache.transform.position.x < axis_axisObject.transform.position.x)
                        PosFix *= -1;
                    ppAXIS_GroupSelector.transform.position += (ppAXIS_GroupSelector.transform.forward * (Input.GetAxis("Mouse X") * PosFix) * axis_manipulationSpeed) * Time.deltaTime;
                }

                axis_axisObject.transform.position = ppAXIS_GroupSelector.transform.position;
            }

            //---AFTER SELECTION
            if (Input.GetKeyUp(axis_LegacyInput_SelectPoints))
            {
                if(Axis_IsMoving)
                {
                    Axis_IsMoving = false;
                    return;
                }

                if (ppAXIS_TotalPoints.Count == 0)
                    return;

                int c = 0;
                foreach (Transform t in ppAXIS_TotalPoints)
                {
                    if (t == null)
                        continue;
                    if (AxisEditor_Utilities.IsInsideSelection(mainCameraCache, ppAXIS_CursorPosOrigin, t.gameObject))
                    {
                        if (!Input.GetKey(axis_LegacyInput_RemovePoints))
                        {
                            if (c == 0)
                                axis_pointColorStorage = t.GetComponentInChildren<Renderer>().material.color;
                            ppAXIS_SelectedPoints.Add(t);
                            t.GetComponentInChildren<Renderer>().material.color = axis_selectedPointColor;
                        }
                        else
                        {
                            t.GetComponentInChildren<Renderer>().material.color = axis_pointColorStorage;
                            t.transform.parent = axis_targetMeshProEditor.meshVertexEditor_PointsRoot;
                            ppAXIS_SelectedPoints.Remove(t);
                            continue;
                        }
                        c++;
                    }
                }
                Axis_IsSelecting = false;
                if (ppAXIS_SelectedPoints.Count>0)
                {
                    axis_axisObject.SetActive(true);

                    InternalAxis_RefreshBounds();
                }
                else
                    axis_axisObject.SetActive(false);
            }
        }

        #region AXIS EDITOR Methods

        private bool InternalAxis_CheckSideFunctions()
        {
            return (Input.GetKey(axis_LegacyInput_AddPoints) || Input.GetKey(axis_LegacyInput_RemovePoints));
        }

        private void InternalAxis_RefreshBounds(Transform center = null)
        {
            if (InternalAxis_CheckSideFunctions())
            {
                foreach (Transform p in ppAXIS_SelectedPoints)
                    p.parent = null;
            }

            Vector3 Center = AxisEditor_Utilities.FindCenterPoint(ppAXIS_SelectedPoints.ToArray());
            ppAXIS_GroupSelector.transform.position = Center;

            if (!axis_inLocalSpace)
                ppAXIS_GroupSelector.transform.rotation = Quaternion.identity;
            else
            {
                if (!center)
                    ppAXIS_GroupSelector.transform.rotation = axis_targetMeshProEditor.meshVertexEditor_PointsRoot.transform.rotation;
                else
                    ppAXIS_GroupSelector.transform.rotation = center.rotation;
            }

            foreach (Transform p in ppAXIS_SelectedPoints)
                p.parent = ppAXIS_GroupSelector.transform;

            axis_axisObject.transform.position = ppAXIS_GroupSelector.transform.position;
            axis_axisObject.transform.rotation = ppAXIS_GroupSelector.transform.rotation;
        }

        private void OnGUI()
        {
            if (!isAxisEditor)
                return;

            if (Axis_IsSelecting)
            {
                var rect = AxisEditor_Utilities.GetScreenRect(ppAXIS_CursorPosOrigin, Input.mousePosition);
                AxisEditor_Utilities.DrawScreenRect(rect, new Color(0.8f, 0.8f, 0.95f, 0.25f), axis_selectionGridColor);
                AxisEditor_Utilities.DrawScreenRectBorder(rect, 2, new Color(0.8f, 0.8f, 0.95f), axis_selectionGridColor);
            }
        }

        /// <summary>
        /// Axis method - switch editor target
        /// </summary>
        /// <param name="Target"></param>
        public void AXIS_SwitchTarget(MD_MeshProEditor Target)
        {
            axis_targetMeshProEditor = Target;

            ppAXIS_UndoStoredObjects.Clear();
            ppAXIS_TotalPoints.Clear();
            ppAXIS_SelectedPoints.Clear();

            if (!axis_targetMeshProEditor)
            {
                MD_Debug.Debug(this, "Target Object is missing!", MD_Debug.DebugType.Error);
                return;
            }
            if (!axis_targetMeshProEditor.meshVertexEditor_PointsRoot)
            {
                MD_Debug.Debug(this, "Target Objects vertices root is missing!", MD_Debug.DebugType.Error);
                return;
            }
            if (!ppAXIS_GroupSelector)
                ppAXIS_GroupSelector = new GameObject("AxisEditor_GroupSelector");
            foreach (Transform t in axis_targetMeshProEditor.meshVertexEditor_PointsRoot.transform)
                ppAXIS_TotalPoints.Add(t);
        }

        /// <summary>
        /// Axis method - undo selection
        /// </summary>
        public void AXIS_Undo()
        {
            if (ppAXIS_UndoStoredObjects.Count == 0)
                return;

            if (ppAXIS_SelectedPoints.Count > 0)
                foreach (Transform t in ppAXIS_SelectedPoints)
                {
                    t.GetComponentInChildren<Renderer>().material.color = axis_pointColorStorage;
                    t.transform.parent = axis_targetMeshProEditor.meshVertexEditor_PointsRoot.transform;
                }

            foreach (Transform t in ppAXIS_UndoStoredObjects)
            {
                if (t != null)
                    ppAXIS_SelectedPoints.Add(t);
            }
            ppAXIS_UndoStoredObjects.Clear();

            Axis_IsSelecting = false;
            axis_axisObject.SetActive(true);

            Vector3 Center = AxisEditor_Utilities.FindCenterPoint(ppAXIS_SelectedPoints.ToArray());
            ppAXIS_GroupSelector.transform.position = Center;
            if (axis_inLocalSpace && ppAXIS_SelectedPoints.Count == 1)
                ppAXIS_GroupSelector.transform.rotation = ppAXIS_SelectedPoints[0].rotation;
            else
                ppAXIS_GroupSelector.transform.rotation = Quaternion.identity;
            axis_pointColorStorage = ppAXIS_SelectedPoints[0].GetComponentInChildren<Renderer>().material.color;
            foreach (Transform p in ppAXIS_SelectedPoints)
            {
                p.parent = ppAXIS_GroupSelector.transform;
                p.GetComponentInChildren<Renderer>().material.color = axis_selectedPointColor;
            }

            axis_axisObject.transform.position = ppAXIS_GroupSelector.transform.position;
            axis_axisObject.transform.rotation = ppAXIS_GroupSelector.transform.rotation;
        }

        /// <summary>
        /// Create an axis handle automatically - this will create a default axis handle for axis-editor
        /// </summary>
        public void AXIS_CreateAxisHandleAutomatically()
        {
            GameObject AxisRoot = new GameObject("AxisObject_Root");
            AxisRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            GameObject X_Axis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject Y_Axis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject Z_Axis = GameObject.CreatePrimitive(PrimitiveType.Cube);

            X_Axis.name = "AXIS_X";
            Y_Axis.name = "AXIS_Y";
            Z_Axis.name = "AXIS_Z";

            X_Axis.transform.parent = AxisRoot.transform;
            Y_Axis.transform.parent = AxisRoot.transform;
            Z_Axis.transform.parent = AxisRoot.transform;

            X_Axis.transform.localPosition = new Vector3(0.6f, 0, 0);
            X_Axis.transform.localRotation = Quaternion.Euler(-90, 0, -90);
            X_Axis.transform.localScale = new Vector3(0.15f, 1, 0.15f);

            Y_Axis.transform.localPosition = new Vector3(0, 0.6f, 0);
            Y_Axis.transform.localRotation = Quaternion.Euler(0, 90, 0);
            Y_Axis.transform.localScale = new Vector3(0.15f, 1, 0.15f);

            Z_Axis.transform.localPosition = new Vector3(0, 0, -0.6f);
            Z_Axis.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            Z_Axis.transform.localScale = new Vector3(0.15f, 1, 0.15f);

            Material mat1 = new Material(Utilities.MD_Utilities.MD_Specifics.GetProperPipelineDefaultShader());
            mat1.color = Color.red;
            X_Axis.GetComponent<Renderer>().material = mat1;
            Material mat2 = new Material(Utilities.MD_Utilities.MD_Specifics.GetProperPipelineDefaultShader());
            mat2.color = Color.green;
            Y_Axis.GetComponent<Renderer>().material = mat2;
            Material mat3 = new Material(Utilities.MD_Utilities.MD_Specifics.GetProperPipelineDefaultShader());
            mat3.color = Color.blue;
            Z_Axis.GetComponent<Renderer>().material = mat3;

            axis_axisObject = AxisRoot;
        }

        private static class AxisEditor_Utilities
        {
            //---Creating Grid Texture
            private static Texture2D GridTexture;
            public static Texture2D GridColor(Color tex)
            {
                if (GridTexture == null)
                {
                    GridTexture = new Texture2D(1, 1);
                    GridTexture.SetPixel(0, 0, tex);
                    GridTexture.Apply();
                }
                return GridTexture;
            }

            //---Drawing Grid Borders
            public static void DrawScreenRectBorder(Rect re, float thic, Color c, Color mainC)
            {
                DrawScreenRect(new Rect(re.xMin, re.yMin, re.width, thic), c, mainC);
                DrawScreenRect(new Rect(re.xMin, re.yMin, thic, re.height), c, mainC);
                DrawScreenRect(new Rect(re.xMax - thic, re.yMin, thic, re.height), c, mainC);
                DrawScreenRect(new Rect(re.xMin, re.yMax - thic, re.width, thic), c, mainC);
            }

            public static Rect GetScreenRect(Vector3 screenPosition1, Vector3 screenPosition2)
            {
                screenPosition1.y = Screen.height - screenPosition1.y;
                screenPosition2.y = Screen.height - screenPosition2.y;
                var topLeft = Vector3.Min(screenPosition1, screenPosition2);
                var bottomRight = Vector3.Max(screenPosition1, screenPosition2);
                return Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
            }

            //---Drawing Screen Rect
            public static void DrawScreenRect(Rect rect, Color color, Color mainCol)
            {
                GUI.color = color;
                GUI.DrawTexture(rect, GridColor(mainCol));
                GUI.color = Color.white;
            }

            //---Generating Bounds
            public static Bounds GetViewportBounds(Camera camera, Vector3 screenPosition1, Vector3 screenPosition2)
            {
                Vector3 v1 = camera.ScreenToViewportPoint(screenPosition1);
                Vector3 v2 = camera.ScreenToViewportPoint(screenPosition2);
                Vector3 min = Vector3.Min(v1, v2);
                Vector3 max = Vector3.Max(v1, v2);
                min.z = camera.nearClipPlane;
                max.z = camera.farClipPlane;

                Bounds bounds = new Bounds();
                bounds.SetMinMax(min, max);
                return bounds;
            }

            //---Checking Selection
            public static bool IsInsideSelection(Camera camSender, Vector3 MousePos, GameObject ObjectInsideSelection)
            {
                Camera camera = camSender;
                Bounds viewportBounds = GetViewportBounds(camera, MousePos, Input.mousePosition);
                return viewportBounds.Contains(camera.WorldToViewportPoint(ObjectInsideSelection.transform.position));
            }

            //---Find Center In List
            public static Vector3 FindCenterPoint(Transform[] Senders)
            {
                if (Senders.Length == 0)
                    return Vector3.zero;
                if (Senders.Length == 1)
                    return Senders[0].position;
                Bounds bounds = new Bounds(Senders[0].position, Vector3.zero);
                for (int i = 1; i < Senders.Length; i++)
                    bounds.Encapsulate(Senders[i].position);
                return bounds.center;
            }
        }

        #endregion

        #region NON-AXIS EDITOR Methods

        /// <summary>
        /// Switch current control mode by index [1-Grab/Drop,2-Push,3-Pull]
        /// </summary>
        public void NON_AXIS_SwitchControlMode(int index)
        {
            nonAxis_vertexControlMode = (VertexControlMode)index;
        }

        #endregion

        private bool Internal_GetControlInput()
        {
            if(!nonAxis_isMobileFocused)  return Input.GetKey(nonAxis_legacyPCInput);
            else                    return Input.touchCount > 0;
        }
    }
}

#if UNITY_EDITOR
namespace MD_Package_Editor
{
    [CustomEditor(typeof(MD_MeshEditorRuntime))]
    [CanEditMultipleObjects]
    public sealed class MD_MeshEditorRuntime_Editor : MD_EditorUtilities
    {
        private MD_MeshEditorRuntime m;

        private void OnEnable()
        {
            m = (MD_MeshEditorRuntime)target;
        }

        public override void OnInspectorGUI()
        {
            if(!m)
            {
                DrawDefaultInspector();
                return;
            }
            MDE_s();

            MDE_v();
            MDE_DrawProperty("mainCameraCache", "Main Camera", "Assign main camera in the scene that will represent 'Origin'. Leave this field empty if the camera (tagged as MainCamera) exists in the scene");
            MDE_ve();

            MDE_v();
            MDE_DrawProperty("isAxisEditor", "Axis Editor Mode", "If enabled, the script will be set to the AXIS EDITOR");
            MDE_ve();

            if (m.isAxisEditor)
            {
                MDE_hb("Axis editor works for PC platform only.");

                MDE_s();

                MDE_v();
                MDE_DrawProperty("axis_targetMeshProEditor", "Target Object", "Required target object to edit");
                MDE_s(5);
                MDE_DrawProperty("axis_axisObject", "Editor Axis Object", "Required 'Movable' Axis object for Axis Editor");
                GUIStyle st = new GUIStyle();
                st.normal.textColor = Color.gray;
                st.richText = true;
                st.fontSize = 10;
                st.fontStyle = FontStyle.Italic;
                if (m.axis_axisObject != null)
                    GUILayout.Label("Required axis child naming: <color=red>AXIS_X</color> - <color=lime>AXIS_Y</color> - <color=cyan>AXIS_Z</color>", st);
                else
                {
                    if (MDE_b("Create Axis Object Automatically"))
                    {
                        m.AXIS_CreateAxisHandleAutomatically();
                        return;
                    }
                }

                MDE_ve();

                MDE_s(10);

                MDE_v();

                MDE_DrawProperty("axis_LegacyInput_SelectPoints", "Selection Input");
                MDE_s(3);
                MDE_DrawProperty("axis_LegacyInput_AddPoints", "Add Input", "If you have selected points, you can add more points from selection by holding this input and holding the selection input.");
                MDE_DrawProperty("axis_LegacyInput_RemovePoints", "Remove Input", "If you have selected points, you can remove points from selection by holding this input and holding the selection input.");
                MDE_s(5);
                MDE_DrawProperty("axis_inLocalSpace", "Local Space", "Axis object orientation");
                MDE_DrawProperty("axis_manipulationSpeed", "Move Speed", "Axis Object move speed");
                MDE_DrawProperty("axis_selectedPointColor", "Selection Color");
                MDE_DrawProperty("axis_selectionGridColor", "Selection Grid Color");

                MDE_ve();

                serializedObject.Update();
                return;
            }

            MDE_s(10);

            MDE_DrawProperty("nonAxis_vertexControlMode", "Editor Control Mode", "Choose a control mode for editor at runtime");
            if (m.nonAxis_vertexControlMode != MD_MeshEditorRuntime.VertexControlMode.GrabDropVertex)
            {
                MDE_v();
                MDE_DrawProperty("nonAxis_pullPushVertexSpeed", "Motion Speed", "Pull/Push effect speed", default);
                MDE_DrawProperty("nonAxis_pullPushType", "Motion Type", "Select one of the motion types of Pull/Push effect", default);
                MDE_s(3);
                if (m.nonAxis_vertexControlMode == MD_MeshEditorRuntime.VertexControlMode.PullVertex)
                    MDE_DrawProperty("nonAxis_maxMinPullPushDistance", "Minimum Distance", "How close can the points be?", default);
                else
                    MDE_DrawProperty("nonAxis_maxMinPullPushDistance", "Maximum Distance", "How far can the points go?", default);
                MDE_s(3);
                MDE_DrawProperty("nonAxis_continuousPullPushDetection", "Continuous Detection", "If enabled, the potential points will be refreshed every frame", default);
                MDE_ve();
            }
            MDE_DrawProperty("nonAxis_isMobileFocused", "Mobile Support", "If enabled, the system will be ready for mobile devices");
            serializedObject.ApplyModifiedProperties();

            MDE_s(5);

            GUI.color = Color.white;

            if (m.nonAxis_vertexControlMode == MD_MeshEditorRuntime.VertexControlMode.GrabDropVertex)
            {
                MDE_v();
                MDE_l("Locks", true);
                MDE_DrawProperty("nonAxis_lockAxisX", "Lock X Axis", "If the axis is locked, selected point won't be able to move in the axis direction", default, true);
                MDE_DrawProperty("nonAxis_lockAxisY", "Lock Y Axis", "If the axis is locked, selected point won't be able to move in the axis direction", default, true);
                MDE_ve();
            }

            MDE_s();

            MDE_l("Vertex Selection Appearance", true);
            MDE_v();
            MDE_DrawProperty("nonAxis_switchAppearance", "Use Appearance Feature", "If enabled, you will be able to customize vertex appearance");
            if (m.nonAxis_switchAppearance)
            {
                MDE_DrawProperty("nonAxis_switchAppearanceUseMaterial", "Use Custom Material", "If enabled, you will be able to use custom material instance instead of color");
                if (m.nonAxis_switchAppearanceUseMaterial)
                    MDE_DrawProperty("nonAxis_switchAppearanceMaterialTarget", "Material Instance", default, default, true);
                else
                    MDE_DrawProperty("nonAxis_switchAppearanceToColor", "Change To Color", "Target color if system catches potential vertexes", default, true);
            }
            MDE_ve();
            MDE_s();

            MDE_v();

            if (!m.nonAxis_isMobileFocused)
            {
                MDE_l("Controls", true);
                MDE_v();
                MDE_DrawProperty("nonAxis_legacyPCInput", "Control Input", "Enter input key for vertex selection");
                MDE_ve();
                MDE_s(5);
            }

            MDE_l("Conditions", true);
            MDE_v();
            MDE_DrawProperty("nonAxis_allowSpecificPoints", "Raycast Specific Points", "If enabled, the raycast will allow only colliders with tag below");
            if (m.nonAxis_allowSpecificPoints)
                MDE_DrawProperty("nonAxis_allowedPointsTag", "Allowed Tag", "Specific allowed tag for raycast", default, true);

            if (!m.nonAxis_isMobileFocused)
            {
                MDE_DrawProperty("nonAxis_cursorIsOrigin", "Raycast from Cursor", "If disabled, the raycast origin will be the transforms position [direction = transform.forward]");
                if (!m.nonAxis_cursorIsOrigin)
                    MDE_DrawProperty("nonAxis_lockAndHideCursor", "Hide & Lock Cursor", default, default, true);
                MDE_s(5);
            }
            MDE_ve();

            MDE_l("Raycast Settings", true);
            MDE_v();
            MDE_v();
            MDE_DrawProperty("nonAxis_allowedLayerMask", "Allowed Layer Masks", default, default);
            MDE_DrawProperty("nonAxis_raycastDistance", "Raycast Distance", default, default);
            MDE_DrawProperty("nonAxis_raycastRadius", "Raycast Radius", default, default);
            MDE_ve();
            MDE_DrawProperty("nonAxis_allowBackfaces", "Allow Backfaces", "Allow points behind the point of view", default);
            MDE_ve();

            MDE_ve();

            MDE_s(5);

            MDE_DrawProperty("nonAxis_enableGizmos", "Show Scene Debug", default, true);

            serializedObject.Update();
        }
    }
}
#endif