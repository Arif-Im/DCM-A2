#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

using MD_Package_Editor;

namespace MD_Package.Geometry
{
    public sealed class MDG_TunnelCreatorEditorWindow : MD_EditorWindowUtilities
    {
        /// <summary>
        /// Initialize Tunnel-Creator editor window
        /// </summary>
        /// <param name="tcSender"></param>
        public static void Init(MDG_TunnelCreator tcSender)
        {
            MDG_TunnelCreatorEditorWindow newWin = (MDG_TunnelCreatorEditorWindow)GetWindow(typeof(MDG_TunnelCreatorEditorWindow));
            newWin.minSize = new Vector2(400, 500);
            newWin.maxSize = new Vector2(500, 600);
            newWin.titleContent = new GUIContent("Tunnel Creator - Editor Window");
            if (tc)
                tc = null;
            tc = tcSender;
            newWin.Show();
        }

        private void OnDestroy()
        {
            tc = null;
        }

        private static MDG_TunnelCreator tc;

        public Texture2D tc_ICON_Add;
        public Texture2D tc_ICON_Rem;

        private GameObject obj2;
        private GameObject obj1;
        private GameObject obj_UngroupTo;

        private bool groupAfterCreation = true;

        private float value0 = 0.5f;

        private int selectedEditorFeature = 0;

        private void OnGUI()
        {
            Lw(false);

            MDE_s(20);
            MDE_l("Nodes Creation");
            MDE_h();
            if (MDE_b("Add Node",0, tc_ICON_Add))
                tc.Tunnel_AddNode(Vector3.zero, groupAfterCreation);
            if (MDE_b("Remove Node",0, tc_ICON_Rem))
                tc.Tunnel_RemoveLastNode();
            MDE_he();
            groupAfterCreation = GUILayout.Toggle(groupAfterCreation, "Group new node to the last added node on 'Add'");

            MDE_s(15);

            MDE_l("Nodes Management");
            MDE_v();
            MDE_h();
            if (MDE_b("Group All Together") && tc.TunnelCurrentNodes.Count > 0)
                tc.Tunnel_GroupAllNodesTogether();
            if (MDE_b("Ungroup All") && tc.TunnelCurrentNodes.Count > 0)
                tc.Tunnel_UngroupAllNodes((obj_UngroupTo) ? obj_UngroupTo.transform : null);
            MDE_he();
            obj_UngroupTo = EditorGUILayout.ObjectField(new GUIContent("Ungroup To", "Leave this field empty if the parent will be null"), (Object)obj_UngroupTo, typeof(GameObject), true) as GameObject;

            MDE_s(25);

            MDE_l("Nodes Editor Features");
            if (tc.TunnelCurrentNodes.Count < 4)
            {
                EditorGUILayout.HelpBox("To access nodes editor features, there must be more than 3 nodes...", MessageType.Warning);
                MDE_ve();
                Lw(true);
                return;
            }
            MDE_v();
            MDE_h();
            if (MDE_b("Make Turn") && tc.TunnelCurrentNodes.Count > 0)
            {
                selectedEditorFeature = 1;
                value0 = 0f;
            }
            if (MDE_b("Make Straight Line") && tc.TunnelCurrentNodes.Count > 0)
            {
                selectedEditorFeature = 2;
                value0 = 2.5f;
            }
            if (MDE_b("Connect Tunnel") && tc.TunnelCurrentNodes.Count > 0)
                selectedEditorFeature = 3;
            MDE_he();
            MDE_s();

            switch (selectedEditorFeature)
            {
                case 1:
                    value0 = EditorGUILayout.Slider(new GUIContent("Turn Value"), value0, -5f, 5f);
                    MDE_h();
                    EditorGUIUtility.labelWidth -= 15;
                    obj2 = EditorGUILayout.ObjectField(new GUIContent("From Node"), (Object)obj2, typeof(GameObject), true) as GameObject;
                    obj1 = EditorGUILayout.ObjectField(new GUIContent("To Node"), (Object)obj1, typeof(GameObject), true) as GameObject;
                    EditorGUIUtility.labelWidth += 15;
                    MDE_ve();
                    MDE_h();
                    if (MDE_b("Assign Selected to From Node", 180) && Selection.activeGameObject)
                        obj2 = Selection.activeGameObject;
                    if (MDE_b("Assign Selected to To Node", 180) && Selection.activeGameObject)
                        obj1 = Selection.activeGameObject;
                    MDE_he();
                    break;
                case 2:
                    value0 = EditorGUILayout.Slider(new GUIContent("Distance"), value0, 0.1f, 5f);
                    MDE_h();
                    EditorGUIUtility.labelWidth -= 15;
                    obj2 = EditorGUILayout.ObjectField(new GUIContent("From Node"), (Object)obj2, typeof(GameObject), true) as GameObject;
                    obj1 = EditorGUILayout.ObjectField(new GUIContent("To Node"), (Object)obj1, typeof(GameObject), true) as GameObject;
                    EditorGUIUtility.labelWidth += 15;
                    MDE_ve();
                    MDE_h();
                    if (MDE_b("Assign Selected to From Node", 180) && Selection.activeGameObject)
                        obj2 = Selection.activeGameObject;
                    if (MDE_b("Assign Selected to To Node", 180) && Selection.activeGameObject)
                        obj1 = Selection.activeGameObject;
                    MDE_he();
                    break;
                case 3:
                    MDE_h();
                    EditorGUIUtility.labelWidth += 20;
                    obj2 = EditorGUILayout.ObjectField(new GUIContent("Ending Node"), (Object)obj2, typeof(GameObject), true) as GameObject;
                    obj1 = EditorGUILayout.ObjectField(new GUIContent("Starting Node"), (Object)obj1, typeof(GameObject), true) as GameObject;
                    EditorGUIUtility.labelWidth -= 20;
                    MDE_ve();
                    break;
            }
            if (selectedEditorFeature != 0)
                if (MDE_b("Apply Editor Feature"))
                    ProcessEditorFeature();
            MDE_ve();

            MDE_ve();
            MDE_s();

            Lw(true);

            void Lw(bool add)
            {
                if(add)
                    EditorGUIUtility.labelWidth += 40;
                else
                    EditorGUIUtility.labelWidth -= 40;
            }
        }

        private void ProcessEditorFeature()
        {
            float val0 = 0;
            int iFrom = -1;
            int iTo = 0;
            int c = 0;
            Transform referenceObj;

            if (obj1 == null || obj2 == null)
            {
                Debug.LogError("MDM_TunnelCreatorEditorWindow - 'From Node' or 'To Node' field is null!");
                return;
            }

            switch (selectedEditorFeature)
            {
                case 1:  //---Make Turn
                    for (int i = 0; i < tc.TunnelCurrentNodes.Count; i++)
                    {
                        if (obj2 == tc.TunnelCurrentNodes[i].gameObject && iFrom == -1)
                            iFrom = i + 1;
                        else if (iFrom != -1)
                            c++;
                        if (obj1 == tc.TunnelCurrentNodes[i].gameObject && iTo == 0)
                        {
                            iTo = i;
                            break;
                        }
                    }
                    referenceObj = tc.TunnelCurrentNodes[iFrom];
                    for (int i = iFrom + 1; i <= iTo; i++)
                    {
                        val0 += val0 + value0 / c;
                        tc.TunnelCurrentNodes[i].transform.position += referenceObj.right * val0;
                    }
                    break;


                case 2:  //---Make Straight Line
                    for (int i = 0; i < tc.TunnelCurrentNodes.Count; i++)
                    {
                        if (obj2 == tc.TunnelCurrentNodes[i].gameObject && iFrom == -1)
                            iFrom = i;
                        if (obj1 == tc.TunnelCurrentNodes[i].gameObject && iTo == 0)
                            iTo = i;
                    }
                    referenceObj = tc.TunnelCurrentNodes[iFrom];
                    for (int i = iFrom + 1; i <= iTo; i++)
                    {
                        val0 += value0;
                        tc.TunnelCurrentNodes[i].transform.SetPositionAndRotation(referenceObj.position + referenceObj.forward * val0, Quaternion.identity);
                    }
                    break;

                case 3:  //---Connect Tunnel
                    obj2.transform.position = obj1.transform.position;
                    obj2.transform.rotation = obj1.transform.rotation;
                    break;
            }

        }
    }
}
#endif