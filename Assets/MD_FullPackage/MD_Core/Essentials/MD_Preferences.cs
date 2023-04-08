using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

using MD_Package;

namespace MD_Package
{
    /// <summary>
    /// MD(Mesh Deformation) Preferences class - here are stored all the global-scoped data
    /// </summary>
    public static class MD_GlobalPreferences
    {
        /// <summary>
        /// Create a new mesh reference if any modifier/ mesh deformator is applied to an object
        /// </summary>
        public static bool CreateNewReference { get; internal set; }
        /// <summary>
        /// Popup editor windows if any important notification occurs
        /// </summary>
        public static bool PopupEditorWindow { get; internal set; }
        /// <summary>
        /// Auto recalculate normals as default
        /// </summary>
        public static bool AutoRecalcNormals { get; internal set; }
        /// <summary>
        /// Alternative technique for normals recalculation. If disabled, mesh normals will be recalculated through the default Unity's RecalculateNormals
        /// </summary>
        public static bool AlternateNormalsRecalc { get; internal set; }
        /// <summary>
        /// Auto recalculate bounds as default
        /// </summary>
        public static bool AutoRecalcBounds { get; internal set; }
        /// <summary>
        /// Maximum vertex count limit allowed in the package
        /// </summary>
        public static int VertexLimit { get; internal set; }
    }
}

#if UNITY_EDITOR
namespace MD_Package_Editor
{
    [InitializeOnLoad]
    public sealed class MD_Preferences : MD_EditorWindowUtilities
    {
        static MD_Preferences()
        {
            RefreshValues();
        }

        private static void RefreshValues()
        {
            MD_GlobalPreferences.CreateNewReference = MdPreference_CreateNewMeshReference;
            MD_GlobalPreferences.PopupEditorWindow = MdPreference_PopupEditorWindows;
            MD_GlobalPreferences.AutoRecalcNormals = MdPreference_AutoRecalculateNormals;
            MD_GlobalPreferences.AlternateNormalsRecalc = MdPreference_AlternateNormalsRecalculation;
            MD_GlobalPreferences.AutoRecalcBounds = MdPreference_AutoRecalculateBounds;
            MD_GlobalPreferences.VertexLimit = MdPreference_VertexLimit;
        }

        [MenuItem("Window/MD_Package/Preferences")]
        private static void Init()
        {
            MD_Preferences vt = (MD_Preferences)GetWindow(typeof(MD_Preferences));
            vt.maxSize = new Vector2(400, 400);
            vt.minSize = new Vector2(350, 350);
            vt.titleContent = new GUIContent("MD Package Preferences");
            vt.Show();
            RefreshValues();
        }

        #region Preferences

        private static bool mdpCreateNewReference = true;
        private static bool MdPreference_CreateNewMeshReference
        {
            get
            {
                if (EditorPrefs.HasKey("mdpCreateNewReference"))
                    return EditorPrefs.GetBool("mdpCreateNewReference");
                else
                    return mdpCreateNewReference;
            }
            set
            {
                mdpCreateNewReference = value;
                MD_GlobalPreferences.CreateNewReference = mdpCreateNewReference;
                EditorPrefs.SetBool("mdpCreateNewReference", mdpCreateNewReference);
            }
        }

        private static int mdpVertexLimit = 2000;
        private static int MdPreference_VertexLimit
        {
            get
            {
                if (EditorPrefs.HasKey("mdpVertexLimit"))
                    return EditorPrefs.GetInt("mdpVertexLimit");
                else
                    return mdpVertexLimit;
            }
            set
            {
                mdpVertexLimit = value;
                MD_GlobalPreferences.VertexLimit = mdpVertexLimit;
                EditorPrefs.SetInt("mdpVertexLimit", mdpVertexLimit);
            }
        }

        private static bool mdpPopupEditorWindows = true;
        private static bool MdPreference_PopupEditorWindows
        {
            get
            {
                if (EditorPrefs.HasKey("mdpPopupEditorWindows"))
                    return EditorPrefs.GetBool("mdpPopupEditorWindows");
                else
                    return mdpPopupEditorWindows;
            }
            set
            {
                mdpPopupEditorWindows = value;
                MD_GlobalPreferences.PopupEditorWindow = mdpPopupEditorWindows;
                EditorPrefs.SetBool("mdpPopupEditorWindows", mdpPopupEditorWindows);
            }
        }

        private static bool mdpAutoRecalculateNormals = true;
        private static bool MdPreference_AutoRecalculateNormals
        {
            get
            {
                if (EditorPrefs.HasKey("mdpAutoRecalculateNormals"))
                    return EditorPrefs.GetBool("mdpAutoRecalculateNormals");
                else
                    return mdpAutoRecalculateNormals;
            }
            set
            {
                mdpAutoRecalculateNormals = value;
                MD_GlobalPreferences.AutoRecalcNormals = mdpAutoRecalculateNormals;
                EditorPrefs.SetBool("mdpAutoRecalculateNormals", mdpAutoRecalculateNormals);
            }
        }

        private static bool mdpAlternateNormalsRecalculation = false;
        private static bool MdPreference_AlternateNormalsRecalculation
        {
            get
            {
                if (EditorPrefs.HasKey("mdpAlternateNormalsRecalculation"))
                    return EditorPrefs.GetBool("mdpAlternateNormalsRecalculation");
                else
                    return mdpAlternateNormalsRecalculation;
            }
            set
            {
                mdpAlternateNormalsRecalculation = value;
                MD_GlobalPreferences.AlternateNormalsRecalc = mdpAlternateNormalsRecalculation;
                EditorPrefs.SetBool("mdpAlternateNormalsRecalculation", mdpAlternateNormalsRecalculation);
            }
        }

        private static bool mdpAutoRecalculateBounds = true;
        private static bool MdPreference_AutoRecalculateBounds
        {
            get
            {
                if (EditorPrefs.HasKey("mdpAutoRecalculateBounds"))
                    return EditorPrefs.GetBool("mdpAutoRecalculateBounds");
                else
                    return mdpAutoRecalculateBounds;
            }
            set
            {
                mdpAutoRecalculateBounds = value;
                MD_GlobalPreferences.AutoRecalcBounds = mdpAutoRecalculateBounds;
                EditorPrefs.SetBool("mdpAutoRecalculateBounds", mdpAutoRecalculateBounds);
            }
        }

        #endregion

        private void OnGUI()
        {
            MDE_s(20);
            MDE_l("MD Package - Preferences", true);
            MDE_s(15);
            MDE_v();
            MDE_v();
            MdPreference_PopupEditorWindows = GUILayout.Toggle(MdPreference_PopupEditorWindows, "Popup Editor Windows if any notification");
            MDE_s();
            MdPreference_CreateNewMeshReference = GUILayout.Toggle(MdPreference_CreateNewMeshReference, "Create New Mesh Reference as Default");
            MDE_ve();
            MDE_s(20);
            MDE_v();
            MdPreference_AutoRecalculateNormals = GUILayout.Toggle(MdPreference_AutoRecalculateNormals, "Auto Recalculate Normals as Default");
            MDE_s();
            MdPreference_AlternateNormalsRecalculation = GUILayout.Toggle(MdPreference_AlternateNormalsRecalculation, "Alternate Normals Recalculation as Default");
            MDE_s();
            MdPreference_AutoRecalculateBounds = GUILayout.Toggle(MdPreference_AutoRecalculateBounds, "Auto Recalculate Bounds as Default");
            MDE_ve();
            MDE_s(20);
            MDE_v();
            MdPreference_VertexLimit = EditorGUILayout.IntField("Max Vertex Limit", MdPreference_VertexLimit);
            if (MdPreference_VertexLimit != 2000)
                MDE_hb("It's very recommended to keep the original value [2000]. The higher value is, the higher risk of application damage may be caused.", MessageType.Warning);
            MDE_ve();
            MDE_ve();
        }
    }
}
#endif