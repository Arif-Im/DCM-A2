#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

using MD_Package;

namespace MD_Package_Editor
{
    // Custom utilities for Mesh Deformation editor scripts.
    // Written by Matej Vanco in 2014. Last maintenance in 2023.

    /// <summary>
    /// Essential editor utilities for internal purpose
    /// </summary>
    public abstract class MD_EditorUtilities : Editor
    {
        protected void MDE_DrawProperty(string p, string Text, string ToolTip = "", bool includeChilds = false, bool identOffset = false)
        {
            if (identOffset) MDE_plus();
            SerializedProperty sp = serializedObject.FindProperty(p);
            if(sp == null)
            {
                Debug.LogError("[MD Editor error] Property '" + p + "' cannot be found");
                return;
            }
            EditorGUILayout.PropertyField(sp, new GUIContent(Text, ToolTip), includeChilds, null);
            if (identOffset) MDE_minus();
            serializedObject.ApplyModifiedProperties();
        }

        protected void MDE_AddMeshCollider(GameObject sender)
        {
            if (sender.GetComponent<MeshCollider>()) return;
            ColorUtility.TryParseHtmlString("#a6e0b5", out Color c);
            Color oc = GUI.color;
            GUI.color = c;
            if (MDE_b("Add Mesh Collider")) sender.AddComponent<MeshCollider>();
            GUI.color = oc;
        }
        protected void MDE_AddMeshColliderRefresher(GameObject sender)
        {
            if (sender.GetComponent<MD_MeshColliderRefresher>()) return;
            ColorUtility.TryParseHtmlString("#49de71", out Color c);
            Color oc = GUI.color;
            GUI.color = c;
            if (MDE_b("Add Mesh Collider Refresher")) sender.AddComponent<MD_MeshColliderRefresher>();
            GUI.color = oc;
        }
        protected void MDE_BackToMeshEditor(MonoBehaviour Sender)
        {
            ColorUtility.TryParseHtmlString("#f2d0d0", out Color c);
            Color oc = GUI.color;
            GUI.color = c;
            if (MDE_b("Back To Mesh Pro Editor"))
            {
                GameObject gm = Sender.gameObject;
                DestroyImmediate(Sender);
                gm.AddComponent<MD_MeshProEditor>();
            }
            GUI.color = oc;
        }

        protected bool MDE_dd(string title, string msg, string ok = "Ok", string no = "")
        {
            if(!string.IsNullOrEmpty(no))
                return EditorUtility.DisplayDialog(title, msg, ok, no);
            else
                return EditorUtility.DisplayDialog(title, msg, ok);
        }
        protected void MDE_hb(string msg, MessageType msgt = MessageType.None)
        {
            EditorGUILayout.HelpBox(msg, msgt);
        }
        protected void MDE_l(string s, bool bold = false)
        {
            if(bold)
                GUILayout.Label(s, EditorStyles.boldLabel);
            else
                GUILayout.Label(s);
        }
        protected void MDE_l(Texture s)
        {
            GUILayout.Label(s);
        }
        protected void MDE_v(bool box = true)
        {
            if (!box) GUILayout.BeginVertical();
            else GUILayout.BeginVertical("Box");
        }
        protected void MDE_ve()
        {
            GUILayout.EndVertical();
        }
        protected void MDE_h(bool box = true)
        {
            if (!box) GUILayout.BeginHorizontal();
            else GUILayout.BeginHorizontal("Box");
        }
        protected void MDE_he()
        {
            GUILayout.EndHorizontal();
        }
        protected bool MDE_b(string s, string tooltip = "")
        {
            if(string.IsNullOrEmpty(tooltip))
                return GUILayout.Button(s);
            else
                return GUILayout.Button(new GUIContent(s, tooltip));
        }
        protected bool MDE_b(GUIContent gui)
        {
            return GUILayout.Button(gui);
        }
        protected bool MDE_b(string s, float width)
        {
            return GUILayout.Button(s, GUILayout.Width(width));
        }
        protected void MDE_s(int s = 10)
        {
            GUILayout.Space(s);
        }
        protected bool MDE_f(bool inbool, string txt, bool bold = true)
        {
            GUIStyle style = new GUIStyle(EditorStyles.foldout);
            if(bold) style.fontStyle = FontStyle.Bold;
            return EditorGUI.Foldout(EditorGUILayout.GetControlRect(), inbool, txt, true, style);
        }
        protected void MDE_plus()
        {
            EditorGUI.indentLevel++;
        }
        protected void MDE_minus()
        {
            EditorGUI.indentLevel--;
        }
    }

    /// <summary>
    /// Essential editor Window utilities for internal purpose
    /// </summary>
    public abstract class MD_EditorWindowUtilities : EditorWindow
    {
        protected bool MDE_b(string txt, int size = 0, Texture2D icon = null)
        {
            if (size == 0)
                return GUILayout.Button(new GUIContent(txt, (icon != null) ? icon : null));
            else
                return GUILayout.Button(txt, GUILayout.Width(size));
        }
        protected void MDE_hb(string msg, MessageType msgt = MessageType.None)
        {
            EditorGUILayout.HelpBox(msg, msgt);
        }
        protected void MDE_l(string s, bool bold = false)
        {
            if (bold)
                GUILayout.Label(s, EditorStyles.boldLabel);
            else
                GUILayout.Label(s);
        }
        protected void MDE_v(bool box = true)
        {
            if (!box) GUILayout.BeginVertical();
            else GUILayout.BeginVertical("Box");
        }
        protected void MDE_ve()
        {
            GUILayout.EndVertical();
        }
        protected void MDE_h(bool box = true)
        {
            if (!box) GUILayout.BeginHorizontal();
            else GUILayout.BeginHorizontal("Box");
        }
        protected void MDE_he()
        {
            GUILayout.EndHorizontal();
        }
        protected void MDE_s(int s = 10)
        {
            GUILayout.Space(s);
        }
    }

    /// <summary>
    /// Essential editor Material utilities for internal purpose
    /// </summary>
    public abstract class MD_MaterialEditorUtilities : ShaderGUI
    {
        protected bool MDE_DrawProperty(MaterialEditor matSrc, MaterialProperty[] props, string p, bool texture = false, string tooltip = "")
        {
            bool found = false;
            foreach (MaterialProperty prop in props)
            {
                if (prop.name == p)
                {
                    found = true;
                    break;
                }
            }
            if (!found) return false;
            MaterialProperty property = FindProperty(p, props);
            if (!texture)
                matSrc.ShaderProperty(property, new GUIContent(property.displayName, tooltip));
            else
            {
                Rect last = EditorGUILayout.GetControlRect();
                matSrc.TexturePropertyMiniThumbnail(last, property, property.displayName, "");
            }

            return true;
        }
        protected bool MDE_DrawProperty(MaterialEditor matSrc, MaterialProperty[] props, string p, string s, string tooltip = "")
        {
            bool found = false;
            foreach (MaterialProperty prop in props)
            {
                if (prop.name == p)
                {
                    found = true;
                    break;
                }
            }
            if (!found) return false;
            MaterialProperty property = FindProperty(p, props);
            matSrc.ShaderProperty(property, new GUIContent(s, tooltip));
            return true;
        }

        protected bool MDE_CompareProperty(MaterialEditor matSrc, string a, float b)
        {
            return MaterialEditor.GetMaterialProperty(matSrc.serializedObject.targetObjects, a).floatValue == b;
        }

        protected void MDE_hb(string msg)
        {
            EditorGUILayout.HelpBox(msg, MessageType.None);
        }
        protected void MDE_l(string s, bool bold = true)
        {
            if (bold)
                GUILayout.Label(s, EditorStyles.boldLabel);
            else
                GUILayout.Label(s);
        }
        protected void MDE_v(bool box = true)
        {
            if (!box) GUILayout.BeginVertical();
            else GUILayout.BeginVertical("Box");
        }
        protected void MDE_ve()
        {
            GUILayout.EndVertical();
        }
        protected void MDE_s(int s = 10)
        {
            GUILayout.Space(s);
        }
    }
}
#endif