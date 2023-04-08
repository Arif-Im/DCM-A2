#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using MD_Package;
using MD_Package.Modifiers;

namespace MD_Package_Editor
{
    /// <summary>
    /// Simple editor window for selecting certain points - used in MDM_MeshFit modifier
    /// </summary>
    public sealed class MD_PointSelectorTool : EditorWindow
    {
        public MDM_MeshFit sender;
        public List<GameObject> selectedPoints = new List<GameObject>();

        private void OnGUI()
        {
            if (GUILayout.Button("Assign selected points [" + Selection.gameObjects?.Length + "]"))
            {
                if(sender == null)
                {
                    MD_Debug.Debug(null, "Sender is null!", MD_Debug.DebugType.Error);
                    return;
                }
                selectedPoints.Clear();
                if (Selection.activeGameObject != null && Selection.gameObjects.Length > 1)
                {
                    foreach (GameObject gm in Selection.gameObjects)
                    {
                        if (gm.transform.root == sender.transform)
                            selectedPoints.Add(gm);
                    }
                    sender.selectedPoints = selectedPoints.ToArray();
                    sender.MeshFit_RefreshSelectedPointsState();
                    Selection.activeObject = sender.gameObject;
                    this.Close();
                }
            }
        }
    }
}
#endif