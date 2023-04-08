#if UNITY_EDITOR
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;

using MD_Package;

namespace MD_Package_Editor
{
    /// <summary>
    /// Simple startup wizard window for uses of the MD Package.
    /// Internal purpose.
    /// </summary>
    public sealed class MD_StartupWizard : EditorWindow
    {
        public Texture2D Logo;

        public Texture2D Home;
        public Texture2D Web;
        public Texture2D Doc;
        public Texture2D Discord;

        private GUIStyle style;

        [MenuItem("Window/MD_Package/Startup Window")]
        public static void Init()
        {
            MD_StartupWizard md = (MD_StartupWizard)GetWindow(typeof(MD_StartupWizard));
            md.maxSize = new Vector2(400, 700);
            md.minSize = new Vector2(399, 699);
            md.titleContent = new GUIContent("MD Startup");
            md.Show();
        }

        private void OnGUI()
        {
            style = new GUIStyle(GUI.skin.label);
            style.richText = true;
            style.normal.textColor = Color.white;
            style.wordWrap = false;

            GUILayout.Label(Logo);
            style.fontSize = 13;
            style.wordWrap = true;
            style.alignment = TextAnchor.MiddleCenter;
            GUILayout.BeginVertical("Box");
            GUILayout.Label("Thanks for downloading the MD Full Collection!\nPlease read the latest change-log below...", style);
            GUILayout.EndVertical();

            style.alignment = TextAnchor.UpperLeft;
            GUILayout.Space(5);
            style.fontSize = 12;

            GUILayout.BeginVertical("Box");
            GUILayout.Label("<size=14><color=#ffa84a>MD Package version <b>" + MD_Debug.VERSION + "</b> [" + MD_Debug.DATE + " <size=11>dd/mm/yyyy</size>]</color></size>\n- MAJOR update for package APi > I've been working on custom APiDocsToHTML convertor that would help me to convert any code-APi to the readable HTML database\n- Major refactor of the code\n- New editor icons\n- Category 'Shapes' renamed to 'Geometry'\n- Major upgrade related to abstraction & OOP\n- New base classes for each category: MD_Mesh Base, MD_GeometryBase, MD_ModifierBase\n- New collection of primitive geometry(procedural triangle / pyramid, cube and more)\n- Major cleanup of all modifiers(naming, methods, static methods)\n-Major cleanup of all utilities(naming, static methods)\n-Upgraded editor experience(undo issues, warnings cleanup and more)\n-Minor VR cleanup(naming, namespaces &latest VR framework refresh)\n...and many more in the official roadmap below.", style);
            if(GUILayout.Button("Official Roadmap - Trello"))
                Application.OpenURL("https://trello.com/b/MFqllEZE/matej-vanco-unity-extension");
            GUILayout.EndVertical();
            GUILayout.Space(5);
            style.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label("No idea where to start? Open general documentation to learn more!", style);
            GUILayout.Space(5);
            style.alignment = TextAnchor.UpperLeft;
            style = new GUIStyle(GUI.skin.button);
            style.imagePosition = ImagePosition.ImageAbove;

            GUILayout.BeginHorizontal("Box");
            if (GUILayout.Button(new GUIContent("Take Me To Intro", Home), style))
            {
                GenerateScenesToBuild();
                string scene = GetScenePath("MDExample_Introduction");
                if (!string.IsNullOrEmpty(scene))
                    EditorSceneManager.OpenScene(scene);
                else
                    Debug.LogError("Scene is not in Build Settings! Required path: [" + Application.dataPath + "/MD_FullPackage/Examples/Scenes/]");
            }
            if (GUILayout.Button(new GUIContent("Official Website", Web), style))
                Application.OpenURL("https://www.matejvanco.com/md-package");

            if (GUILayout.Button(new GUIContent("Open Documentation", Doc), style))
                Application.OpenURL("https://docs.google.com/presentation/d/13Utk_hVY304c7QoQPSVG7nHXV5W5RjXzgZIhsKFvUDE/edit");      

            GUILayout.EndHorizontal();

            if (GUILayout.Button(new GUIContent("APi Documentation"), style))
                Application.OpenURL("https://struct9.com/matejvanco-assets/md-package/Introduction");

            style.alignment = TextAnchor.MiddleCenter;
            if (GUILayout.Button(new GUIContent(Discord), style))
                Application.OpenURL("https://discord.gg/WdcYHBtCfr");
        }

        public static void GenerateScenesToBuild()
        {
            try
            {
                EditorBuildSettings.scenes = new EditorBuildSettingsScene[0];
                List<EditorBuildSettingsScene> sceneAr = new List<EditorBuildSettingsScene>();

                int cat = 0;
                while (cat < 6)
                {
                    string[] tempPaths;
                    if (cat == 0)       tempPaths = Directory.GetFiles(Application.dataPath + "/MD_FullPackage/MD_Examples/MD_Examples_Scenes/", "*.unity");
                    else if (cat == 1)  tempPaths = Directory.GetFiles(Application.dataPath + "/MD_FullPackage/MD_Examples/MD_Examples_Scenes/Geometry/", "*.unity");
                    else if (cat == 2)  tempPaths = Directory.GetFiles(Application.dataPath + "/MD_FullPackage/MD_Examples/MD_Examples_Scenes/MeshEditor/", "*.unity");
                    else if (cat == 3)  tempPaths = Directory.GetFiles(Application.dataPath + "/MD_FullPackage/MD_Examples/MD_Examples_Scenes/Modifiers/", "*.unity");
                    else if (cat == 4)  tempPaths = Directory.GetFiles(Application.dataPath + "/MD_FullPackage/MD_Examples/MD_Examples_Scenes/Mobile/", "*.unity");
                    else                tempPaths = Directory.GetFiles(Application.dataPath + "/MD_FullPackage/MD_Examples/MD_Examples_Scenes/Shaders/", "*.unity");

                    for (int i = 0; i < tempPaths.Length; i++)
                    {
                        string path = tempPaths[i].Substring(Application.dataPath.Length - "Assets".Length);
                        path = path.Replace('\\', '/');

                        sceneAr.Add(new EditorBuildSettingsScene(path, true));
                    }
                    cat++;
                }
                EditorBuildSettings.scenes = sceneAr.ToArray();
            }
            catch (IOException e)
            {
                Debug.Log("Can't load example scenes! Try to play again. Otherwise you can find all the example scenes in MD_Examples_Scenes.\nException: " + e.Message);
            }

        }

        private static string GetScenePath(string sceneName)
        {
            try
            {
                if (File.Exists(Application.dataPath + "/MD_FullPackage/MD_Examples/MD_Examples_Scenes/" + sceneName + ".unity"))
                    return Application.dataPath + "/MD_FullPackage/MD_Examples/MD_Examples_Scenes/" + sceneName + ".unity";
                else
                    return "";
            }
            catch (IOException e)
            {
                Debug.Log("Can't load example scenes! Go to /MD_FullPackage/MD_Examples/MD_Examples_Scenes/.\nException: " + e.Message);
            }
            return "";
        }
    }

}
#endif