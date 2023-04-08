using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

using MD_Package;
using MD_Package.Modifiers;
#endif

namespace MD_Package.Modifiers
{
    /// <summary>
    /// MDM(Mesh Deformation Modifier): Surface Tracking.
    /// Surface tracking for the specific shader & material (requires Lite Mesh Tracker shader). Create tracks & footprints with a very quick & simple way.
    /// Written by Matej Vanco (2016, updated in 2023).
    /// </summary>
    [ExecuteInEditMode]
    [AddComponentMenu(MD_Debug.ORGANISATION + MD_Debug.PACKAGENAME + "Modifiers/Surface Tracking GPU")]
    public sealed class MDM_SurfaceTracking : MonoBehaviour
    {
        public Camera virtualTrackCamera;
        public RenderTexture trackerSource;

        public bool notInitialized = true;

        public float viewSize = 5;
        public float virtualCameraHeight = 0.2f;

        public bool flip = false;

        private void Update()
        {
            if (!virtualTrackCamera)
                return;

            virtualTrackCamera.transform.localPosition = Vector3.zero + Vector3.up * virtualCameraHeight;
            virtualTrackCamera.transform.localRotation = !flip ? Quaternion.LookRotation(Vector3.down) : Quaternion.LookRotation(Vector3.up);
            virtualTrackCamera.transform.localScale = Vector3.one;

            virtualTrackCamera.orthographicSize = viewSize;

            if (trackerSource != null && virtualTrackCamera.targetTexture == null)
                virtualTrackCamera.targetTexture = trackerSource;
        }

        /// <summary>
        /// Clear & reset current surface (Clears the RenderTexture content)
        /// </summary>
        public void SurfTracking_ResetSurface()
        {
            if (trackerSource)
                trackerSource.Release();
        }
    }
}
#if UNITY_EDITOR
namespace MD_Package_Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MDM_SurfaceTracking))]
    public sealed class MDM_SurfaceTracking_Editor : MD_EditorUtilities
    {
        private MDM_SurfaceTracking m;

        private void OnEnable()
        {
            m = (MDM_SurfaceTracking)target;
        }

        private string LT_LayerName;
        private bool LT_Choose;
        public override void OnInspectorGUI()
        {
            MDE_s();
            MDE_hb("Object must contains MD_LiteMeshTracker shader");
            MDE_s();

            if (m.notInitialized)
            {
                MDE_hb("The Tracking system is not yet set. Please write your custom Tracking Layer name that all trackable objects will have an access to", MessageType.Warning);
                if (!LT_Choose)
                {
                    LT_LayerName = GUILayout.TextField(LT_LayerName);
                    GUILayout.Label("Or you can choose an exists layer manually");
                }
                LT_Choose = GUILayout.Toggle(LT_Choose, "Choose layer manually");

                MDE_s(10);

                if (MDE_b("Apply Layer & Create All Requirements [RT, Camera]"))
                {
                    if (!LT_Choose)
                    {
                        if (string.IsNullOrEmpty(LT_LayerName))
                            EditorUtility.DisplayDialog("Error", "Please fill the layer name", "OK");
                        LT_Internal_CreateLayer(LT_LayerName);
                    }

                    LT_Internal_CreateCamera();
                    LT_Internal_CreateRT();
                    m.notInitialized = false;
                }
                FinishEditor();
                return;
            }
            else
            {
                if (!m.virtualTrackCamera)
                {
                    MDE_hb("There is no Virtual Track Camera. Press the reset button or fill the required field!", MessageType.Warning);
                    MDE_DrawProperty("virtualTrackCamera", "Virtual Track Camera");
                    FinishEditor();
                    return;
                }
                if (!m.trackerSource)
                {
                    MDE_hb("There is no Tracking RT Source. Press the reset button or fill the required field!", MessageType.Warning);
                    MDE_DrawProperty("trackerSource", "VT Tracker Source");
                    FinishEditor();
                    return;
                }

                GUI.color = Color.gray;
                MDE_DrawProperty("virtualTrackCamera", "Virtual Track Camera", "");
                MDE_DrawProperty("trackerSource", "VT Tracker Source", "");

                MDE_s(10);

                GUI.color = Color.white;

                MDE_v();
                MDE_DrawProperty("viewSize", "VT Camera View Size", "");
                MDE_DrawProperty("virtualCameraHeight", "VT Camera Height", "");
                MDE_ve();

                if (MDE_b("Flip"))
                {
                    m.flip = !m.flip;
                    if (m.flip)
                    {
                        Vector3 v = m.transform.localScale;
                        v.x = -v.x;
                        v.z = -v.z;
                        m.transform.localScale = v;
                    }
                    else
                    {
                        Vector3 v = m.transform.localScale;
                        v.x = Mathf.Abs(v.x);
                        v.z = Mathf.Abs(v.z);
                        m.transform.localScale = v;
                    }
                }
                if (MDE_b("Clean Tracker Source RT"))
                    m.trackerSource.Release();
            }

            FinishEditor();
        }

        private void FinishEditor()
        {
            MDE_s();

            MDE_AddMeshColliderRefresher(m.gameObject);
            MDE_BackToMeshEditor(m);
            if (target != null) serializedObject.Update();
        }

        private void LT_Internal_CreateCamera()
        {
            GameObject newCamera = new GameObject("TrackingCamera_" + m.name);
            Camera c = newCamera.AddComponent<Camera>();
            c.gameObject.layer = 30;
            c.transform.parent = m.transform;
            c.orthographic = true;
            c.clearFlags = CameraClearFlags.Nothing;
            c.nearClipPlane = 0.1f;
            c.farClipPlane = 200;
            c.depth = 0;
            c.allowHDR = false;
            c.allowMSAA = false;
            c.useOcclusionCulling = false;
            c.targetDisplay = 0;
            c.cullingMask = 1 << 30;
            m.virtualTrackCamera = c;
            newCamera.transform.parent = m.transform;
        }
        private void LT_Internal_CreateRT()
        {
            RenderTexture rt = new RenderTexture(500, 500, 0, RenderTextureFormat.Depth);
            rt.antiAliasing = 1;
            rt.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
            AssetDatabase.CreateAsset(rt, "Assets/MDM_ST_" + m.name + "_RT.renderTexture");
            AssetDatabase.Refresh();
            if (m.gameObject.GetComponent<Renderer>() && m.gameObject.GetComponent<Renderer>().sharedMaterial && m.gameObject.GetComponent<Renderer>().sharedMaterial.HasProperty("_DispTex"))
                m.gameObject.GetComponent<Renderer>().sharedMaterial.SetTexture("_DispTex", rt);
            else
            {
                try
                {
                    Material mat = new Material(Shader.Find("Matej Vanco/Mesh Deformation Package/MD_LiteMeshTracker"));
                    m.gameObject.GetComponent<Renderer>().sharedMaterial = mat;
                    m.gameObject.GetComponent<Renderer>().sharedMaterial.SetTexture("_DispTex", rt);
                }
                catch
                {
                    MD_Debug.Debug(m, "Your object doesn't contain LiteMeshTracker shader. Create an object with mesh filter and add material with the shader LiteMeshTracker", MD_Debug.DebugType.Error);
                }
            }
            m.trackerSource = rt;
        }
        private void LT_Internal_CreateLayer(string LayerName)
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            SerializedProperty layers = tagManager.FindProperty("layers");
            if (layers == null || !layers.isArray)
                return;

            SerializedProperty layerSP = layers.GetArrayElementAtIndex(30);
            layerSP.stringValue = LayerName;

            tagManager.ApplyModifiedProperties();
        }
    }
}
#endif

