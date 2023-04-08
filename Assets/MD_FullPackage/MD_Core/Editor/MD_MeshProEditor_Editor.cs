using UnityEngine;
using UnityEditor;
using MD_Package;
using MD_Package.Modifiers;
using MD_Package.Utilities;

namespace MD_Package_Editor
{
    [CustomEditor(typeof(MD_MeshProEditor))]
    public sealed class MD_MeshProEditor_Editor : MD_EditorUtilities
    {
        private MD_MeshProEditor m;

        private void OnEnable()
        {
            m = (MD_MeshProEditor)target;
            style = new GUIStyle();
            style.richText = true;
        }

        //----GUI Stuff-------------
        private GUIStyle style;

        public GUIStyle styleTest;
        public Texture2D VerticesIcon;
        public Texture2D ColliderIcon;
        public Texture2D IdentityIcon;
        public Texture2D ModifyIcon;
        public Texture2D SmoothIcon;
        public Texture2D SubdivisionIcon;

        //----Adds-------------------
        private readonly bool[] Foldout = new bool[3];
        private float SmoothMeshIntens = 0.5f;
        private readonly int[] DivisionLevels = new int[] { 2, 3, 4, 6, 8};
        private int DivisionlevelSelection = 2;
        private bool _HaveMeshFilter;
        private bool _HaveMeshSkinned;

        private void OnSceneGUI()
        {
            if (m.meshEnableZoneGenerator && m.meshSelectedModification == MD_MeshProEditor.EditorModification.Vertices)
            {
                Vector3 Zone = m.meshZoneGeneratorPosition;
                float radius = m.meshZoneGeneratorRadius;

                Handles.color = Color.magenta;
                Handles.CircleHandleCap(0, Zone, Quaternion.identity, radius, EventType.DragUpdated);
                Handles.CircleHandleCap(0, Zone, Quaternion.Euler(0, 90, 0), radius, EventType.DragUpdated);

                EditorGUI.BeginChangeCheck();
                Handles.color = Color.magenta;
                Handles.DrawWireDisc(Zone, Vector3.up, radius);
                Handles.DrawWireDisc(Zone, Vector3.right, radius);
                Zone = Handles.DoPositionHandle(Zone, m.transform.rotation);
                if (EditorGUI.EndChangeCheck())
                {
                    m.meshZoneGeneratorRadius = radius;
                    m.meshZoneGeneratorPosition = Zone;
                }
            }
        }

        public override void OnInspectorGUI()
        {
            if (m == null)
            {
                DrawDefaultInspector();
                return;
            }

            _HaveMeshFilter = m.GetComponent<MeshFilter>();
            _HaveMeshSkinned = m.GetComponent<SkinnedMeshRenderer>();

            if (!_HaveMeshFilter)
            {
                if (_HaveMeshSkinned)
                {
                    if (MDE_b("Convert to Mesh Filter"))
                    {
                        if (MDE_dd("Are you sure?", "Are you sure to convert the skinned mesh renderer to the mesh filter? There is no way back (Undo won't record this process)", "Yes", "No"))
                            m.MPE_ConvertFromSkinnedToFilter();
                    }
                    MDE_hb("Skinned Mesh Renderer is a component that controls your mesh with bones. Press 'Convert To Mesh Filter' to start editing it's mesh source.", MessageType.Info);
                }
                else MDE_hb("No mesh identity. Object must contains Mesh Filter or Skinned Mesh Renderer component to access mesh editor.", MessageType.Error);
                return;
            }

            MDE_s(20);

            #region Upper Categories

            MDE_h(false);
            if (MDE_b(new GUIContent("Vertices", VerticesIcon, "Point/Vertex Modification")))
            {
                if (m.meshSelectedModification == MD_MeshProEditor.EditorModification.Vertices)
                {
                    m.meshSelectedModification = MD_MeshProEditor.EditorModification.None;
                    m.MPE_ClearPointsEditor();
                }
                else
                {
                    m.meshSelectedModification = MD_MeshProEditor.EditorModification.Vertices;
                    m.MPE_CreatePointsEditor();
                }
            }
            if (MDE_b(new GUIContent("Collider", ColliderIcon, "Collider Modification")))
            {
                m.MPE_ClearPointsEditor();
                if (m.meshSelectedModification == MD_MeshProEditor.EditorModification.Collider)
                    m.meshSelectedModification = MD_MeshProEditor.EditorModification.None;
                else
                    m.meshSelectedModification = MD_MeshProEditor.EditorModification.Collider;
            }
            if (MDE_b(new GUIContent("Identity", IdentityIcon, "Identity Modification")))
            {
                m.MPE_ClearPointsEditor();
                if (m.meshSelectedModification == MD_MeshProEditor.EditorModification.Identity)
                    m.meshSelectedModification = MD_MeshProEditor.EditorModification.None;
                else
                    m.meshSelectedModification = MD_MeshProEditor.EditorModification.Identity;
            }
            if (MDE_b(new GUIContent("Mesh", ModifyIcon, "Mesh Modification")))
            {
                m.MPE_ClearPointsEditor();
                if (m.meshSelectedModification == MD_MeshProEditor.EditorModification.Mesh)
                    m.meshSelectedModification = MD_MeshProEditor.EditorModification.None;
                else
                    m.meshSelectedModification = MD_MeshProEditor.EditorModification.Mesh;
            }
            MDE_he();

            #endregion


            #region Category_Vertices

            if (m.meshSelectedModification == MD_MeshProEditor.EditorModification.Vertices)
            {
                ColorUtility.TryParseHtmlString("#f2d3d3", out Color c);
                GUI.color = c;
                MDE_l("| Vertices Modification", true);
                MDE_v();
				MDE_h();
                MDE_DrawProperty("meshVertexEditor_PointSizeMultiplier", "Point Size Multiplier", "Adjust generated points size. Press 'Vertices' button above to refresh. Keep the value '1' for default size (without any effect)");
                MDE_he();
                MDE_h();
                MDE_DrawProperty("meshAnimationMode", "Animation Mode", "If enabled, the script will not refresh vertices and the mesh will keep the generated points");
                MDE_he();
                MDE_v();
                MDE_DrawProperty("meshVertexEditor_CustomPointPattern", "Custom Point Pattern", "If enabled, you will be able to choose your own point/vertex object pattern");
                if (m.meshVertexEditor_CustomPointPattern)
                {
                    MDE_plus();
                    MDE_DrawProperty("meshVertexEditor_PointPatternObject", "Point Object Pattern");
                    MDE_DrawProperty("meshVertexEditor_UseCustomColor", "Use Custom Color");
                    if (m.meshVertexEditor_UseCustomColor)
                        MDE_DrawProperty("meshVertexEditor_CustomPointColor", "Custom Point Color");
                    MDE_hb("Refresh vertice editor to show a new point-pattern by clicking the 'Vertices Modification' button");
                    MDE_minus();
                }
                MDE_ve();
                MDE_s(5);
                if (MDE_b("Open Vertex-Tool Window")) MD_VertexTool.Init();
                MDE_s(5);
                if (m.meshInfoVertices > MD_GlobalPreferences.VertexLimit)
                {
                    GUI.color = Color.yellow;
                    MDE_v();
                    MDE_hb("Your mesh has more than " + MD_GlobalPreferences.VertexLimit.ToString() + " vertices. All points have been automatically hidden to prevent performance issues. If the mesh is below 10 000 vertex count, use Zone Generator (if possible) to show specific points only. It is still possible to edit mesh beyond 2000 vertices, but the performance might get worse.");
                    if (MDE_b("Activate All Points"))
                    {
                        if (m.MyMeshFilter.sharedMesh.vertices.Length > 10000)
                        {
                            EditorUtility.DisplayDialog("I'm Sorry", "The mesh has too many vertices [" + m.MyMeshFilter.sharedMesh.vertexCount + "]. You won't be able to process this function due to possibly endless freeze. [This message can be disabled in the code on your own risk and responsibility]", "OK");
                            return;
                        }
                        if (m.meshWorkingPoints.Count > 0)
                        {
                            foreach (Transform p in m.meshWorkingPoints)
                                p.gameObject.SetActive(true);
                        }
                        else if (EditorUtility.DisplayDialog("Are you sure?", "Are you sure to continue? This will first generate new points (which may take a while) and then you can activate/ deactivate points on your own performance risk.", "Yes", "No"))
                            m.MPE_CreatePointsEditor(true);
                    }
                    if (MDE_b("Deactivate All Points"))
                    {
                        if (m.MyMeshFilter.sharedMesh.vertices.Length > 10000)
                        {
                            EditorUtility.DisplayDialog("I'm Sorry", "The mesh has too many vertices [" + m.MyMeshFilter.sharedMesh.vertexCount + "]. You won't be able to process this function due to possibly endless freeze. [This message can be disabled in the code on your own risk and responsibility]", "OK");
                            return;
                        }
                        if (m.meshWorkingPoints.Count > 0)
                        {
                            foreach (Transform p in m.meshWorkingPoints)
                                p.gameObject.SetActive(false);
                        }
                    }
                    MDE_ve();
                }
                ColorUtility.TryParseHtmlString("#f2d3d3", out c);
                GUI.color = c;
                MDE_s(5);
                MDE_DrawProperty("meshEnableZoneGenerator", "Enable Zone Generator", "If enabled, you will be able to generate points in a certain zone-radius");
                if (m.meshEnableZoneGenerator)
                {
                    MDE_v();
                    MDE_DrawProperty("meshZoneGeneratorPosition", "Zone Location");
                    MDE_hb("Use axis-handle in the SceneView editor");
                    MDE_DrawProperty("meshZoneGeneratorRadius", "Zone Radius");

                    MDE_h();
                    if (MDE_b("Generate Points In The Zone"))
                        m.MPE_GeneratePointsInTheZone();
                    if (m.meshWorkingPoints.Count > 0)
                    {
                        if (MDE_b("Show All Points"))
                        {
                            if (m.MyMeshFilter.sharedMesh.vertices.Length > 10000)
                            {
                                EditorUtility.DisplayDialog("I'm Sorry", "The mesh has too many vertices [" + m.MyMeshFilter.sharedMesh.vertexCount + "]. You won't be able to process this function due to possibly endless freeze. [This message can be disabled in the code on your own risk & responsibility]", "OK");
                                return;
                            }
                            if (m.meshWorkingPoints.Count > 0)
                                for (int i = 0; i < m.meshWorkingPoints.Count; i++)
                                    m.meshWorkingPoints[i].gameObject.SetActive(true);
                            return;
                        }
                    }
                    MDE_he();
                    MDE_s(5);
                    if (MDE_b("Reset Zone Position"))
                        m.meshZoneGeneratorPosition = m.transform.position;
                    MDE_ve();
                }
                MDE_s(5);
                if (m.meshAnimationMode)
                {
                    MDE_v();
                    MDE_l("Animation Mode | Vertices Manager");
                    MDE_h();
                    if (MDE_b("Show Vertices"))
                        m.MPE_ShowHidePoints(true);
                    if (MDE_b("Hide Vertices"))
                        m.MPE_ShowHidePoints(false);
                    MDE_he();
                    MDE_s(5);
                    MDE_h();
                    if (MDE_b("Ignore Raycast"))
                        m.MPE_IgnoreRaycastForPoints(true);
                    if (MDE_b("Default Layer"))
                        m.MPE_IgnoreRaycastForPoints(false);
                    MDE_he();
                    MDE_ve();
                }
                MDE_ve();
            }

            #endregion

            #region Category_Collider

            if (m.meshSelectedModification == MD_MeshProEditor.EditorModification.Collider)
            {
                ColorUtility.TryParseHtmlString("#7beb99", out Color c);
                GUI.color = c;
                MDE_l("| Collider Modification", true);
                if (!m.GetComponent<MD_MeshColliderRefresher>())
                {
                    MDE_v();

                    if (MDE_b("Add Mesh Collider Refresher"))
                        Undo.AddComponent<MD_MeshColliderRefresher>(m.gameObject);

                    MDE_ve();
                }
                else
                    MDE_hb("The selected object already contains Mesh Collider Refresher component", MessageType.Info);
                if(m.TryGetComponent(out MeshCollider mc) && MDE_b("Refresh Mesh Collider"))
                    mc.sharedMesh = m.MyMeshFilter.sharedMesh;
            }

            #endregion

            #region Category_Identity

            if (m.meshSelectedModification == MD_MeshProEditor.EditorModification.Identity)
            {
                ColorUtility.TryParseHtmlString("#baefff", out Color c);
                GUI.color = c;
                MDE_l("| Identity Modification", true);

                MDE_v();

                if (MDE_b(new GUIContent("Create New Mesh Reference", "Create a brand new object with new mesh reference. This will create a new mesh reference and all your components & behaviours on this gameObject will be removed")))
                {
                    if (!EditorUtility.DisplayDialog("Are you sure?", "Are you sure to create a new mesh reference? This will create a brand new object with new mesh reference and all your components and behaviours on this gameObject will be lost.", "Yes", "No"))
                        return;
                    m.MPE_CreateNewReference();
                    return;
                }
                if (m.transform.childCount > 0 && m.transform.GetChild(0).GetComponent<MeshFilter>())
                {
                    if (MDE_b(new GUIContent("Combine All SubMeshes", "Combine all the meshes attached to the current object")))
                    {
                        m.MPE_CombineMesh();
                        return;
                    }
                }
                if (MDE_b("Save Mesh To Assets"))
                    MD_Utilities.MD_Specifics.SaveMeshToTheAssetsDatabase(m.MyMeshFilter);

                MDE_s(5);
                if (m.meshOptimizeMesh)
                {
                    if (MDE_b("Recalculate Normals & Bounds"))
                        m.MPE_RecalculateMesh(true);
                }
                if (!m.meshUpdateEveryFrame)
                {
                    if (MDE_b("Update Mesh"))
                        m.MPE_UpdateMesh();
                }
                MDE_s(5);

                MDE_DrawProperty("meshNewReferenceAfterCopy", "Create New Reference On Copy-Paste", "If enabled, the new mesh reference will be created with brand new mesh data on copy-paste process");
                MDE_DrawProperty("meshUpdateEveryFrame", "Update Every Frame", "If enabled, the mesh will be updated every frame and you will be able to deform the mesh at runtime");
                MDE_DrawProperty("meshOptimizeMesh", "Optimize Mesh", "If enabled, the mesh will stop refreshing and recalculating Bounds and Normals & you will be able to recalculate them manually");
                MDE_DrawProperty("meshAlternativeNormals", "Alternative Normals", "If disabled, the mesh normals will be recalculated through the default Unity's Recalculate Normals method");
                if(m.meshAlternativeNormals)
                {
                    MDE_plus();
                    MDE_DrawProperty("meshAlternativeNormalsAngle", "Angle");
                    MDE_minus();
                }
                MDE_ve();
            }

            #endregion

            #region Category_Mesh

            if (m.meshSelectedModification == MD_MeshProEditor.EditorModification.Mesh)
            {
                ColorUtility.TryParseHtmlString("#dee7ff", out Color c);
                GUI.color = c;
                MDE_l("| Mesh Modification", true);

                MDE_v();

                MDE_l("Internal Mesh Features");
                MDE_plus();
                MDE_v();
                Foldout[0] = EditorGUILayout.Foldout(Foldout[0], new GUIContent("Mesh Smooth", SmoothIcon, "Smooth mesh by the smooth level"), true, EditorStyles.foldout);
                if (Foldout[0])
                {
                    MDE_plus();
                    SmoothMeshIntens = EditorGUILayout.Slider("Smooth Level", SmoothMeshIntens, 0.5f, 0.05f);
                    MDE_h(false);
                    MDE_s(EditorGUI.indentLevel * 10);
                    if (MDE_b(new GUIContent("Smooth Mesh", SmoothIcon)))
                        m.MPE_SmoothMesh(SmoothMeshIntens);
                    MDE_he();
                    MDE_hb("Undo won't record this process");
                    MDE_minus();
                }
                MDE_ve();
                MDE_v();
                Foldout[1] = EditorGUILayout.Foldout(Foldout[1], new GUIContent("Mesh Subdivide", SubdivisionIcon, "Subdivide mesh by the subdivision level"), true, EditorStyles.foldout);
                if (Foldout[1])
                {
                    MDE_plus();
                    DivisionlevelSelection = EditorGUILayout.IntSlider("Subdivision Level", DivisionlevelSelection, 2, DivisionLevels[DivisionLevels.Length - 1]);
                    MDE_h(false);
                    MDE_s(EditorGUI.indentLevel * 10);
                    if (MDE_b(new GUIContent("Subdivide Mesh", SubdivisionIcon)))
                        m.MPE_SubdivideMesh(DivisionlevelSelection);
                    MDE_he();
                    MDE_hb("Undo won't record this process");
                    MDE_minus();
                }
                MDE_ve();
                MDE_minus();
                serializedObject.Update();
                MDE_s();

                MDE_l("Mesh Modifiers");
                MDE_plus();
                MDE_v();
                Foldout[2] = EditorGUILayout.Foldout(Foldout[2], "Modifiers", true, EditorStyles.foldout);
                if (Foldout[2])
                {
                    MDE_plus();

                    ColorUtility.TryParseHtmlString("#e3badb", out c);
                    GUI.color = c;
                    MDE_l("Logical Deformers");
                    if (MDE_b(new GUIContent("Mesh Morpher")))
                    {
                        GameObject gm = m.gameObject;
                        Undo.DestroyObjectImmediate(m);
                        Undo.AddComponent<MDM_Morpher>(gm);
                        return;
                    }
                    if (MDE_b(new GUIContent("Mesh Effector")))
                    {
                        GameObject gm = m.gameObject;
                        Undo.DestroyObjectImmediate(m);
                        Undo.AddComponent<MDM_MeshEffector>(gm);
                        return;
                    }
                    if (MDE_b(new GUIContent("Mesh FFD")))
                    {
                        GameObject gm = m.gameObject;
                        Undo.DestroyObjectImmediate(m);
                        Undo.AddComponent<MDM_FFD>(gm);
                        return;
                    }
                    if (MDE_b(new GUIContent("Mesh Cut")))
                    {
                        GameObject gm = m.gameObject;
                        Undo.DestroyObjectImmediate(m);
                        Undo.AddComponent<MDM_MeshCut>(gm);
                        return;
                    }

                    ColorUtility.TryParseHtmlString("#dedba0", out c);
                    GUI.color = c;
                    MDE_l("World Interactive");
                    if (MDE_b(new GUIContent("Interactive Surface [CPU]")))
                    {
                        GameObject gm = m.gameObject;
                        Undo.DestroyObjectImmediate(m);
                        Undo.AddComponent<MDM_InteractiveSurface>(gm);
                        return;
                    }
                    if (MDE_b(new GUIContent("Surface Tracking [GPU]")))
                    {
                        GameObject gm = m.gameObject;
                        Undo.DestroyObjectImmediate(m);
                        Undo.AddComponent<MDM_SurfaceTracking>(gm);
                        return;
                    }
                    if (MDE_b(new GUIContent("Mesh Damage")))
                    {
                        GameObject gm = m.gameObject;
                        Undo.DestroyObjectImmediate(m);
                        Undo.AddComponent<MDM_MeshDamage>(gm);
                        return;
                    }
                    if (MDE_b(new GUIContent("Mesh Fit")))
                    {
                        GameObject gm = m.gameObject;
                        Undo.DestroyObjectImmediate(m);
                        Undo.AddComponent<MDM_MeshFit>(gm);
                        return;
                    }
                    if (MDE_b(new GUIContent("Melt Controller")))
                    {
                        GameObject gm = m.gameObject;
                        Undo.DestroyObjectImmediate(m);
                        Undo.AddComponent<MDM_MeltController>(gm);
                        return;
                    }
                    if (MDE_b(new GUIContent("Mesh Slime")))
                    {
                        GameObject gm = m.gameObject;
                        Undo.DestroyObjectImmediate(m);
                        Undo.AddComponent<MDM_MeshSlime>(gm);
                        return;
                    }

                    ColorUtility.TryParseHtmlString("#aae0b2", out c);
                    GUI.color = c;
                    MDE_l("Basics");
                    if (MDE_b(new GUIContent("Twist")))
                    {
                        GameObject gm = m.gameObject;
                        Undo.DestroyObjectImmediate(m);
                        Undo.AddComponent<MDM_Twist>(gm);
                        return;
                    }
                    if (MDE_b(new GUIContent("Bend")))
                    {
                        GameObject gm = m.gameObject;
                        Undo.DestroyObjectImmediate(m);
                        Undo.AddComponent<MDM_Bend>(gm);
                        return;
                    }
                    if (MDE_b(new GUIContent("Mesh Noise")))
                    {
                        GameObject gm = m.gameObject;
                        Undo.DestroyObjectImmediate(m);
                        Undo.AddComponent<MDM_MeshNoise>(gm);
                        return;
                    }

                    ColorUtility.TryParseHtmlString("#aad2e0", out c);
                    GUI.color = c;
                    MDE_l("Sculpting");
                    if (MDE_b(new GUIContent("Sculpting Lite")))
                    {
                        GameObject gm = m.gameObject;
                        Undo.DestroyObjectImmediate(m);
                        Undo.AddComponent<MDM_SculptingLite>(gm);
                        return;
                    }

                    ColorUtility.TryParseHtmlString("#ebebeb", out c);
                    GUI.color = c;
                    MDE_l("Additional Events");
                    if (MDE_b(new GUIContent("Raycast Event")))
                    {
                        GameObject gm = m.gameObject;
                        Undo.DestroyObjectImmediate(m);
                        Undo.AddComponent<MDM_RaycastEvent>(gm);
                        return;
                    }

                    ColorUtility.TryParseHtmlString("#dee7ff", out c);
                    GUI.color = c;
                    MDE_minus();
                }
                MDE_ve();
                MDE_minus();
                MDE_ve();
            }

            #endregion

            #region Bottom Category

            MDE_s(20);
            GUI.color = Color.white;
            MDE_l("Mesh Information");
            MDE_v();

            MDE_h(false);
            MDE_DrawProperty("meshInfoMeshName", "Mesh Name", "Change mesh name and Refresh Identity.");
            MDE_he();

            MDE_h(false);
            MDE_l("Vertices:");
            GUILayout.TextField(m.meshInfoVertices.ToString());
            MDE_l("Triangles:");
            GUILayout.TextField(m.meshInfoTriangles.ToString());
            MDE_l("Normals:");
            GUILayout.TextField(m.meshInfoNormals.ToString());
            MDE_l("UVs:");
            GUILayout.TextField(m.meshInfoUVs.ToString());
            MDE_he();

            if (MDE_b("Restore Initial Mesh"))
                m.MPE_RestoreMeshToOriginal();
            MDE_ve();

            #endregion

            MDE_s();

            serializedObject.Update();
        }
    }
}