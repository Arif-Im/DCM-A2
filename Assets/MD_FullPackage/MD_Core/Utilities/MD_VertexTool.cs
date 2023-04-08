using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

using MD_Package;
using MD_Package.Utilities;

namespace MD_Package_Editor
{
    /// <summary>
    /// Editor Only! Use MD_VertexToolUtility instead
    /// </summary>
    public sealed class MD_VertexTool : MD_EditorWindowUtilities
    {
        [MenuItem("Window/MD_Package/Vertex Tool")]
        public static void Init()
        {
            MD_VertexTool vt = (MD_VertexTool)GetWindow(typeof(MD_VertexTool));
            vt.maxSize = new Vector2(110, 370);
            vt.minSize = new Vector2(109, 369);
            vt.titleContent = new GUIContent("VT");
            vt.Show();
        }

        private bool Active = false;

        private Object GroupObject;

        private int MultiplyCounter = 1;
        private Vector3 MultiplyAngle;
        private Vector3 MultiplyMoveOffset = new Vector3(1, 0, 0);

        public Texture2D _Attach;
        public Texture2D _Clone;
        public Texture2D _Weld;
        public Texture2D _Relax;

        private void OnGUI()
        {
            if (!Active)
                GUI.color = Color.gray;
            else
                GUI.color = Color.white;
            GUILayout.BeginVertical("Box");

            if (Selection.gameObjects.Length > 0)
                Active = true;
            else
                Active = false;

            GUI.color = Color.yellow;
            GUILayout.BeginVertical("Box");
            GUILayout.Label("Element");
            GUILayout.EndVertical();

            if (!Active)
                GUI.color = Color.gray;
            else
                GUI.color = Color.white;

            //---------------Attach Function----------------
            //----------------------------------------------

            if (GUILayout.Button(new GUIContent("Attach", _Attach)))
            {
                if (!Active)
                    return;
                MD_VertexToolUtility.V_TOOL_Element_Attach(Selection.gameObjects[0], !EditorUtility.DisplayDialog("Question","Would you like to attach current selection or children of the first selected object?","Selection","Children"));
            }
            GUILayout.Space(5);

            if (!Active)
                GUI.color = Color.gray;
            else
                GUI.color = Color.white;

            //---------------Clone Function----------------
            //----------------------------------------------

            if (GUILayout.Button(new GUIContent("Clone", _Clone)))
            {
                if (!Active)
                    return;
                if (Selection.gameObjects.Length == 0)
                    return;
                MD_VertexToolUtility.V_TOOL_Element_Clone(Selection.gameObjects[0], MultiplyCounter, MultiplyAngle, MultiplyMoveOffset);
            }
            MultiplyCounter = EditorGUILayout.IntField(MultiplyCounter);
            MultiplyMoveOffset = EditorGUILayout.Vector3Field("", MultiplyMoveOffset);
            MultiplyAngle = EditorGUILayout.Vector3Field("", MultiplyAngle);
            GUILayout.Space(10);

            GUI.color = Color.cyan;
            GUILayout.BeginVertical("Box");
            GUILayout.Label("Vertex");
            GUILayout.EndVertical();

            if (!Active)
                GUI.color = Color.gray;
            else
                GUI.color = Color.white;

            //---------------Weld Function----------------
            //----------------------------------------------

            if (GUILayout.Button(new GUIContent("Weld", _Weld)))
            {
                if (!Active)
                    return;
                if (Selection.gameObjects.Length == 0 || Selection.gameObjects.Length <= 1)
                    return;
                if (Selection.gameObjects[0] == null || Selection.gameObjects[1] == null)
                    return;
                MD_VertexToolUtility.V_TOOL_Vertex_Weld(Selection.gameObjects[0].transform, Selection.gameObjects[1].transform);
            }
            GUILayout.Space(5);
            if (GUILayout.Button(new GUIContent("Relax", _Relax)))
            {
                if (!Active)
                    return;
                MD_VertexToolUtility.V_TOOL_Vertex_Relax(Selection.activeGameObject.transform.GetComponent<MD_MeshProEditor>());
            }

            GUILayout.Space(10);

            if (Selection.gameObjects.Length == 0)
                GUI.color = Color.gray;
            else
                GUI.color = Color.white;
            GUILayout.BeginVertical("Box");
            GUILayout.Label("Scene");
            GUILayout.EndVertical();

            if (GUILayout.Button("Group [" + Selection.gameObjects.Length.ToString() + "]"))
            {
                if (Selection.gameObjects.Length < 1 || GroupObject == null)
                    return;
                foreach (GameObject gm in Selection.gameObjects)
                {
                    gm.transform.parent = ((GameObject)GroupObject as GameObject).transform;
                }
            }
            if (Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<MD_MeshProEditor>() && Selection.activeGameObject.GetComponent<MD_MeshProEditor>().meshEnableZoneGenerator)
                if (GUILayout.Button("Group Enabled"))
                {
                    MD_MeshProEditor p = Selection.activeGameObject.GetComponent<MD_MeshProEditor>();
                    if (p.meshWorkingPoints.Count == 0 || GroupObject == null)
                        return;
                    foreach (Transform gm in p.meshWorkingPoints)
                    {
                        if (gm.gameObject.activeInHierarchy)
                            gm.transform.parent = ((GameObject)GroupObject as GameObject).transform;
                    }
                }
            GroupObject = EditorGUILayout.ObjectField(GroupObject, typeof(GameObject), true);
            GUILayout.EndVertical();
        }
    }
}
#endif

namespace MD_Package.Utilities
{
    /// <summary>
    /// Vertex tool modifier for advanced functionality with MeshProEditor generated points
    /// </summary>
    public static class MD_VertexToolUtility
    {
        /// <summary>
        /// Attach 2 or more meshes - meshes will be combined and will share the same material
        /// </summary>
        /// <param name="attachToGameObject">Main attach target - the object to attach to</param>
        /// <param name="attachChildren">If true, all the RootObject's children will be included</param>
        public static void V_TOOL_Element_Attach(GameObject attachToGameObject, bool attachChildren = true)
        {
            if (!attachToGameObject)
            {
                MD_Debug.Debug(null, "VertexTool: {ATTACH FUNCTION} At least, one object must be selected", MD_Debug.DebugType.Error);
                return;
            }
            if (!attachToGameObject.GetComponent<MeshFilter>())
            {
                MD_Debug.Debug(null, "VertexTool: {ATTACH FUNCTION} The sender object doesn't contain Mesh Filter component", MD_Debug.DebugType.Error);
                return;
            }
            if (!attachToGameObject.GetComponent<MD_MeshProEditor>())
            {
                MD_Debug.Debug(null, "VertexTool: {ATTACH FUNCTION} The sender object doesn't contain Mesh Pro Editor component", MD_Debug.DebugType.Error);
                return;
            }
			
#if UNITY_EDITOR
            if (!EditorUtility.DisplayDialog("Are you sure?", "Are you sure to process Attach feature? There's no way back, Undo won't record this.", "Yes", "No"))
                return;
#endif

            if (attachChildren)
            {
                foreach (MeshFilter gm in attachToGameObject.GetComponentsInChildren<MeshFilter>())
                {
                    if (!gm) continue;
                    if (gm.TryGetComponent(out MD_MeshProEditor mpe))
                    {
                        mpe.meshAnimationMode = false;
                        mpe.MPE_ClearPointsEditor();
                    }
                    if (!gm) continue;

                    if (attachToGameObject == gm.gameObject) continue;
                    if (gm.GetComponent<Renderer>())
                        gm.transform.parent = attachToGameObject.transform;
                }
            }
#if UNITY_EDITOR
            else
            {
                GameObject[] selection = Selection.gameObjects;
                foreach (GameObject gm in selection)
                {
                    if (gm.TryGetComponent(out MD_MeshProEditor mpe))
                    {
                        mpe.meshAnimationMode = false;
                        mpe.MPE_ClearPointsEditor();
                    }
                    if (attachToGameObject == gm.gameObject) continue;
                    if (gm.GetComponent<Renderer>())
                        gm.transform.parent = attachToGameObject.transform;
                }
            }
#else
            MD_Debug.Debug(null, "VertexTool: {ATTACH FUNCTION} The ATTACH function couldn't be proceeded because 'attachChildren' was set to false, which is prohibited in non-editor application.", MD_Debug.DebugType.Error);
#endif
            attachToGameObject.GetComponent<MD_MeshProEditor>().MPE_CombineMeshQuick();
        }

        /// <summary>
        /// Clone selected mesh by the certain count, rotation and move offset​​
        /// </summary>
        /// <param name="targetGameObject">Target gameObject to clone (must contain MeshFilter)</param>
        /// <param name="count">How many times the mesh will get cloned?</param>
        /// <param name="rotationOffset">In what angle the mesh will get cloned?</param>
        /// <param name="positionOffset">In what direction the mesh will get cloned?</param>
        public static void V_TOOL_Element_Clone(GameObject targetGameObject, int count, Vector3 rotationOffset, Vector3 positionOffset)
        {
            if (!targetGameObject.TryGetComponent(out MD_MeshProEditor mpe))
            {
                MD_Debug.Debug(null, "VertexTool: {CLONE FUNCTION} The selected object must contains Mesh Pro Editor to clone meshes inside...", MD_Debug.DebugType.Error);
                return;
            }

            if (!targetGameObject.TryGetComponent(out MeshFilter mf))
            {
                MD_Debug.Debug(null, "VertexTool: {CLONE FUNCTION} The selected object must contains Mesh Filter to clone meshes inside...", MD_Debug.DebugType.Error);
                return;
            }
#if UNITY_EDITOR
            if (!EditorUtility.DisplayDialog("Are you sure?", "Are you sure to process Clone feature? There's no way back, Undo won't record this.", "Yes", "No"))
                return;
#endif

            mpe.MPE_ClearPointsEditor();
            mpe.meshSelectedModification = MD_MeshProEditor.EditorModification.None;

            Vector3 offset = targetGameObject.transform.position;
            if (mpe.meshStartBounds != mf.sharedMesh.bounds.max)
                offset = targetGameObject.transform.position + new Vector3(mf.sharedMesh.bounds.max.x * positionOffset.x, mf.sharedMesh.bounds.max.y * positionOffset.y, mf.sharedMesh.bounds.max.z * positionOffset.z);

            Vector3 rotOffset = Vector3.zero;
            List<GameObject> clones = new List<GameObject>();
            for (int i = 0; i < count; i++)
            {
                offset += positionOffset;
                rotOffset += rotationOffset;
                GameObject clon = Object.Instantiate(targetGameObject, null);
                Object.DestroyImmediate(clon.GetComponent<MD_MeshProEditor>());
                clon.transform.position = offset;
                clon.transform.rotation = Quaternion.Euler(rotOffset);
                clones.Add(clon);
            }
            foreach (GameObject g in clones)
                g.transform.parent = targetGameObject.transform;
            mpe.MPE_CombineMeshQuick();
        }

        /// <summary>
        /// Weld selected points - points will split into one
        /// </summary>
        public static void V_TOOL_Vertex_Weld(Transform weldFrom, Transform weldTo)
        {
#if UNITY_EDITOR
            Undo.RegisterCompleteObjectUndo(new Object[2] { weldFrom, weldFrom.gameObject }, "Weld Points");
            Undo.SetTransformParent(weldFrom, weldTo, "Weld Points [Parent]");
#endif
            weldFrom.position = weldTo.position;
            weldFrom.gameObject.SetActive(false);
            weldFrom.parent = weldTo;
            weldFrom.hideFlags = HideFlags.HideInHierarchy;
        }

        /// <summary>
        /// Relax mesh vertices - vertices will be normalized and their offset will be multiplied by the position of their mesh
        /// </summary>
        public static void V_TOOL_Vertex_Relax(MD_MeshProEditor mpeSender)
        {
            if (mpeSender == null)
                return;
            if (mpeSender.meshWorkingPoints == null)
                return;
            if (mpeSender.meshWorkingPoints.Count > 0)
            {
#if UNITY_EDITOR
                Undo.RegisterCompleteObjectUndo(mpeSender.meshWorkingPoints.ToArray(), "Relax Points");
#endif
                foreach (Transform points in mpeSender.meshWorkingPoints)
                {
                    points.transform.LookAt(points.transform.root.transform);
                    points.transform.position += points.transform.forward * Vector3.Distance(points.transform.localPosition, Vector3.zero) / 2;
                }
            }
        }
    }
}
