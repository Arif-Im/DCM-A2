using UnityEngine;
using UnityEditor;

using MD_Package;
using MD_Package.Modifiers;

namespace MD_Package_Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MDM_SculptingLite))]
    public class MDM_SculptingLite_Editor : MD_ModifierBase_Editor
    {
        private MDM_SculptingLite md;
        private SceneView sceneView;

        public override void OnEnable()
        {
            mMeshBase = (MD_MeshBase)target;
            mModifierBase = (MD_ModifierBase)target;
            md = (MDM_SculptingLite)target;
            sceneView = SceneView.lastActiveSceneView;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            MDE_l("Sculpting Lite Modifier", true);
            MDE_v();

            MDE_v();
            MDE_DrawProperty("sculptingFocusedAtRuntime", "Sculpting At Runtime", "If enabled, the sculpting will work only at runtime");
            MDE_s(3);
            MDE_DrawProperty("sculptingMobileSupport", "Sculpting For Mobile", "If enabled, the sculpting will be focused only for mobile devices");
            MDE_ve();

            MDE_s(5);

            MDE_v();
            if (md.sculptingUseInput && !md.ThreadUseMultithreading)
                MDE_DrawProperty("sculptingUpdateColliderAfterRelease", "Update Collider After Release", "If disabled, the collider will be updated every frame if the mouse is down");
            else if (md.ThreadUseMultithreading && !md.sculptingUpdateColliderAfterRelease)
                md.sculptingUpdateColliderAfterRelease = true;
            MDE_ve();

            MDE_s(5);
            MDE_v();
            MDE_DrawProperty("sculptingRecordHistory", "Record History", "If enabled, the sculpting will record every step to the history list");
            if (md.sculptingRecordHistory)
            {
                MDE_DrawProperty("sculptingMaxHistoryRecords", "Max History Records", "How many history records can be created?");
                MDE_hb("The history will be automatically recorded on any 'Control-Up' event. Call 'Undo' method to make a step backward or forward");
            }
            MDE_ve();
            MDE_ve();

            if (!md.sculptingFocusedAtRuntime)
            {
                MDE_v();
                if (!md.sculptingEditorInEditMode)
                {
                    if (MDE_b("Enable Edit Mode", "Enable edit mode to start sculpting the current mesh in the editor"))
                    {
                        md.sculptingEditorInEditMode = true;
                        Tools.current = Tool.None;
                    }
                }
                else if (MDE_b("Disable Edit Mode"))
                    md.sculptingEditorInEditMode = false;
                if (sceneView && !sceneView.drawGizmos)
                    MDE_hb("Gizmos must be enabled to make the sculpting in editor work!", MessageType.Warning);
                MDE_hb("LMouse: Raise\nLShift+LMouse: Lower\nLControl+LMouse: Revert\nR: Restore Mesh" + (!md.sculptingRecordHistory ? "" : "\nZ: Undo"), MessageType.Info);
                MDE_ve();
            }

            MDE_s();

            MDE_l("Brush Settings", true);
            MDE_v();
            MDE_DrawProperty("sculptingShowBrushProjection", "Use Brush Projection", "If enabled, the sculpting will use a certain brush gameObject - this helps you to see where the sculpting pointer/brush actually is according to raycast");
            MDE_DrawProperty("sculptingBrushProjection", "Brush Projection","Assign a specific gameObject that will represent a sculpting brush (projection of the raycast)");
            MDE_s(5);
            MDE_v();
            MDE_DrawProperty("sculptingBrushSize", "Brush Size");
            MDE_DrawProperty("sculptingBrushIntensity", "Brush Intensity");
            MDE_ve();
            MDE_s(5);
            MDE_DrawProperty("sculptingBrushStatus", "Brush Status", "Current brush status. The field is visible in case of keeping just ONE brush status");
            MDE_ve();

            MDE_s();

            MDE_l("Sculpting Advanced Settings", true);
            MDE_v();
            MDE_DrawProperty("sculptingMode", "Sculpting Mode", "Specific sculpting mode for vertices - how the vertices will behave during the interaction?");
			MDE_DrawProperty("sculptingLayerMask", "Sculpting Layer Mask", "Layer mask for sculpting raycast - prevents other gameObjects to be received");
            if (md.sculptingMode == MDM_SculptingLite.SculptingMode.CustomDirection)
                MDE_DrawProperty("sculptingCustomDirection", "Custom Direction", "Choose a custom direction in a world space");
            else if (md.sculptingMode == MDM_SculptingLite.SculptingMode.CustomDirectionObject)
            {
                MDE_DrawProperty("sculptingCustomDirectionObject", "Custom Direction Object");
                MDE_DrawProperty("sculptingCustomDirObjDirection", "Direction Towards Object", "Choose a direction of the included object above in a local space");
            }
            MDE_s(5);
            MDE_v();
            MDE_DrawProperty("sculptingEnableHeightLimitations", "Use Height Limitations", "If enabled, you will be able to set the vertices Y limitation (height) in a world space (suits for planar terrains)");
            if (md.sculptingEnableHeightLimitations)
                MDE_DrawProperty("sculptingHeightLimitations", "Height Limitation", "Minimum[X] and Maximum[Y] height limitation in a world space");
            MDE_ve();
            MDE_v();
            MDE_DrawProperty("sculptingEnableDistanceLimitations", "Use Distance Limitations", "If enabled, you will be able to limit the vertices sculpting range on both sides");
            if (md.sculptingEnableDistanceLimitations) MDE_DrawProperty("sculptingDistanceLimitation", "Distance Limitation", "How far can the vertices go from the initial position?");
            MDE_ve();
            MDE_s(5);
            MDE_DrawProperty("sculptingInterpolationType", "Interpolation Type", "Choose a proper interpolation type - how the processed vertices will transite to next positions? Expontential evaluates much smoother results, linear evaluates more sharp results");
            MDE_ve();

            if (md.sculptingFocusedAtRuntime)
            {
                MDE_s();

                MDE_l("Input & Feature Settings", true);
                if (!md.sculptingMobileSupport)
                {
                    MDE_v();
                    MDE_DrawProperty("sculptingUseInput", "Use Input", "Use custom sculpting input controls. Otherwise, you can use internal API functions to interact with the mesh");
                    if (md.sculptingUseInput)
                    {
                        MDE_DrawProperty("sculptingVRInput", "Is VR Input", "If enabled, the sculpting will be ready for VR");
                        if (!md.sculptingVRInput)
                        {
                            MDE_v();
                            MDE_DrawProperty("sculptingUseRaiseFunct", "Use Raise", "Use Raise sculpting feature (the processed vertices will raise)");
                            MDE_DrawProperty("sculptingUseLowerFunct", "Use Lower", "Use Lower sculpting feature (the processed vertices will move lower)");
                            MDE_DrawProperty("sculptingUseRevertFunct", "Use Revert", "Use Revert sculpting feature (the processed vertices will revert to their initial positions)");
                            MDE_DrawProperty("sculptingUseNoiseFunct", "Use Noise", "Use Noise sculpting feature (the processed vertices will move to randomized locations according to the perlin-noise algorithm)");
                            if (md.sculptingUseNoiseFunct)
                            {
                                MDE_v();
                                MDE_DrawProperty("sculptingNoiseTypes", "Noise Type", "Choose a noise type in a world space");
                                MDE_ve();
                            }
                            MDE_DrawProperty("sculptingUseSmoothFunct", "Use Smooth", "Use Smooth sculpting feature (the processed vertices will smooth their positions)");
                            if (md.sculptingUseSmoothFunct)
                            {
                                MDE_v();
                                MDE_DrawProperty("sculptingSmoothingType", "Smoothing Type", "Choose between two smoothing types... HCfilter is less problematic, but takes more time to process. Laplacian is more problematic, but takes much less time. In general, the HCfilter is recommended for spatial meshes, the Laplacian for planar meshes");
                                if (md.sculptingSmoothingType == MDM_SculptingLite.SmoothingType.LaplacianFilter) MDE_DrawProperty("sculptingSmoothIntensity", "Smooth Intensity");
                                MDE_ve();
                            }
                            MDE_DrawProperty("sculptingUseStylizeFunct", "Use Stylize", "Use Stylize sculpting feature (the processed vertices will 'weld' to the nearest neighbours which makes the stylized-low-poly-looking results)");
                            if (md.sculptingUseStylizeFunct)
                            {
                                MDE_v();
                                MDE_DrawProperty("sculptingStylizeIntensity", "Stylize Intensity");
                                MDE_ve();
                            }

                            MDE_ve();

                            if ((md.sculptingUseSmoothFunct || md.sculptingUseStylizeFunct) && !md.ThreadUseMultithreading)
                                MDE_hb("The Smooth & Stylize features are not supported for non-multithreaded modifiers. Please enable Multithreading to make the Smooth or Stylize function work.", MessageType.Warning);

                            MDE_s(2);
                            if (md.sculptingUseRaiseFunct)
                                MDE_DrawProperty("sculptingRaiseInput", "Raise Input");
                            if (md.sculptingUseLowerFunct)
                                MDE_DrawProperty("sculptingLowerInput", "Lower Input");
                            if (md.sculptingUseRevertFunct)
                                MDE_DrawProperty("sculptingRevertInput", "Revert Input");
                            if (md.sculptingUseNoiseFunct)
                                MDE_DrawProperty("sculptingNoiseInput", "Noise Input");
                            if (md.sculptingUseSmoothFunct)
                                MDE_DrawProperty("sculptingSmoothInput", "Smooth Input");
                            if (md.sculptingUseStylizeFunct)
                                MDE_DrawProperty("sculptingStylizeInput", "Stylize Input");

                            MDE_s();
                            MDE_DrawProperty("sculptingFromCursor", "Sculpt Origin Is Cursor", "If enabled, the raycast origin will be cursor");
                        }
                        else
                        {
                            MDE_hb("The Sculpting Lite is now ready for VR sculpting. You will need to create some events to switch between 'Brush Statuses' such as Raise, Lower, Revert etc. Default state is 'Raise'. The target controller should contain specific MDInputVR component.");
                            MDE_v();
                            MDE_l("Noise Advanced Settings");
                            MDE_v();
                            MDE_DrawProperty("sculptingNoiseTypes", "Noise Type", "Choose a noise type in a world space");
                            MDE_ve();
                            MDE_l("Smooth Advanced Settings");
                            MDE_v();
                            MDE_DrawProperty("sculptingSmoothingType", "Smoothing Type", "Choose between two smoothing types... HCfilter is less problematic, but takes more time to process. Laplacian is more problematic, but takes much less time. In general, the HCfilter is recommended for spatial meshes, the Laplacian for planar meshes");
                            if (md.sculptingSmoothingType == MDM_SculptingLite.SmoothingType.LaplacianFilter) MDE_DrawProperty("sculptingSmoothIntensity", "Smooth Intensity");
                            MDE_ve();
                            MDE_l("Stylize Advanced Settings");
                            MDE_v();
                            MDE_DrawProperty("sculptingStylizeIntensity", "Stylize Intensity");
                            MDE_ve();

                            MDE_ve();
                        }
                        if (md.sculptingFromCursor && !md.sculptingVRInput)
                        {
                            MDE_s(5);
                            MDE_v();
                            MDE_DrawProperty("sculptingCameraCache", "Main Camera Cache", "Assign main camera to make the raycast work properly");
                            MDE_DrawProperty("sculptingLayerMask", "Sculpting Layer Mask", "Set the custom sculpting layer mask");
                            MDE_ve();
                        }
                        else
                        {
                            MDE_DrawProperty("sculptingOriginTransform", md.sculptingVRInput ? "Target Controller" : "Sculpt Origin Object", "The raycast origin - transform forward [If VR enabled, assign your target controller]");
                            MDE_DrawProperty("sculptingLayerMask", "Sculpting Layer Mask", "Set the custom sculpting layer mask");
                        }
                    }
                    MDE_ve();
                }
                else
                {
                    MDE_s(3);
                    MDE_v();
                    MDE_DrawProperty("sculptingCameraCache", "Main Camera Cache", "Assign main camera to make the raycast work properly");
                    MDE_DrawProperty("sculptingLayerMask", "Sculpting Layer Mask", "Set the custom sculpting layer mask");
                    MDE_ve();
                }
            }
            MDE_s(20);
            MDE_l("Interested in Pro version?", true);
            if (MDE_b("Sculpting Pro"))
                Application.OpenURL("https://assetstore.unity.com/packages/tools/modeling/sculpting-pro-201873");
            MDE_AddMeshColliderRefresher(md.gameObject);
            MDE_BackToMeshEditor(md);
            MDE_s();
        }

        /// <summary>
        /// Actual Sculpting Lite in scene-editor
        /// </summary>
        private void OnSceneGUI()
        {
            // Not possible to sculpt in editor while playing
            if (Application.isPlaying) return;
            if (md.sculptingFocusedAtRuntime) return;
            if (!md.sculptingEditorInEditMode) return;
            if (md.sculptingBrushProjection == null) return;

            if (md.sculptingBrushProjection.TryGetComponent(out Collider co))
                DestroyImmediate(co);

            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            // Main editor raycasting with the current mesh
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider == md.transform.GetComponent<Collider>())
                {
                    if (!md.sculptingShowBrushProjection)
                    {
                        if (md.sculptingBrushProjection.activeSelf)
                            md.sculptingBrushProjection.SetActive(false);
                    }
                    else if (!md.sculptingBrushProjection.activeSelf)
                        md.sculptingBrushProjection.SetActive(true);

                    var t = md.sculptingBrushProjection.transform;
                    t.SetPositionAndRotation(hit.point, Quaternion.FromToRotation(-Vector3.forward, hit.normal));
                    t.localScale = Vector3.one * md.sculptingBrushSize;

                    switch (md.sculptingBrushStatus)
                    {
                        case MDM_SculptingLite.BrushStatus.Raise:
                            md.Sculpting_DoSculpting(hit.point, t.forward, md.sculptingBrushSize, md.sculptingBrushIntensity, MDM_SculptingLite.BrushStatus.Raise);
                            if (!md.sculptingUpdateColliderAfterRelease)
                                md.Sculpting_RefreshMeshCollider();
                            break;
                        case MDM_SculptingLite.BrushStatus.Lower:
                            md.Sculpting_DoSculpting(hit.point, t.forward, md.sculptingBrushSize, md.sculptingBrushIntensity, MDM_SculptingLite.BrushStatus.Lower);
                            if (!md.sculptingUpdateColliderAfterRelease)
                                md.Sculpting_RefreshMeshCollider();
                            break;

                        case MDM_SculptingLite.BrushStatus.Revert:
                            md.Sculpting_DoSculpting(hit.point, t.forward, md.sculptingBrushSize, md.sculptingBrushIntensity, MDM_SculptingLite.BrushStatus.Revert);
                            if (!md.sculptingUpdateColliderAfterRelease)
                                md.Sculpting_RefreshMeshCollider();
                            break;
                    }
                }
                else if(md.sculptingBrushProjection.activeSelf)
                    md.sculptingBrushProjection.SetActive(false);
            }
            else if(md.sculptingBrushProjection.activeSelf)
                md.sculptingBrushProjection.SetActive(false);

            // Mouse
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && !Event.current.alt)
            {
                if (md.ThreadUseMultithreading && (md.ThreadInstance == null || !md.ThreadInstance.IsAlive))
                    MD_Debug.Debug(md, "In order to sculpt the mesh in the separated editor thread, press 'Start Editor Thread' in the inspector", MD_Debug.DebugType.Error);
                md.Sculpting_RecordControlDown();
                if (!Event.current.control)
                {
                    if (!Event.current.shift)
                        md.sculptingBrushStatus = MDM_SculptingLite.BrushStatus.Raise;
                    else
                        md.sculptingBrushStatus = MDM_SculptingLite.BrushStatus.Lower;
                }
                else
                    md.sculptingBrushStatus = MDM_SculptingLite.BrushStatus.Revert;
            }
            else if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
            {
                md.sculptingBrushStatus = MDM_SculptingLite.BrushStatus.None;
                md.Sculpting_RecordControlUp();
                EditorUtility.SetDirty(md);
            }

            // Keys
            if (Event.current.type == EventType.KeyDown)
            {
                switch (Event.current.keyCode)
                {
                    case KeyCode.R:
                        md.MDModifier_RestoreMesh();
                        md.Sculpting_RefreshMeshCollider();
                        EditorUtility.SetDirty(md);
                        break;
                    case KeyCode.Z:
                        md.Sculpting_Undo();
                        EditorUtility.SetDirty(md);
                        break;
                }
            }
        }
    }
}