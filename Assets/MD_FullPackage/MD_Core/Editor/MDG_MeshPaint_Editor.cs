using UnityEngine;
using UnityEditor;

using MD_Package.Geometry;

namespace MD_Package_Editor
{
    [CustomEditor(typeof(MDG_MeshPaint))]
    public sealed class MDG_MeshPaint_Editor : MD_EditorUtilities
    {
        public override void OnInspectorGUI()
        {
            MDG_MeshPaint mp = (MDG_MeshPaint)target;

            MDE_s();

            MDE_v();
            MDE_DrawProperty("paintTargetPlatform", "Target Platform", "Choose one of the target platforms");

            if (mp.paintTargetPlatform == MDG_MeshPaint.MeshPaintTargetPlatform.PC)
            {
                MDE_s(5);
                MDE_v();
                MDE_DrawProperty("paintPCLegacyInput", "Paint Legacy Input");
                MDE_ve();
            }
            else if (mp.paintTargetPlatform == MDG_MeshPaint.MeshPaintTargetPlatform.VR)
            {
                MDE_s(5);
                MDE_hb("Add proper MDInputVR component to customize VR input for a specific platform");
            }

            MDE_ve();

            MDE_s();

            MDE_l("Brush Settings", true);
            MDE_v();
            MDE_DrawProperty("paintBrushUniformSize", "Uniform Size");
            if(!mp.paintBrushUniformSize)
                MDE_DrawProperty("paintBrushVectorSize", "Brush Size");
            else
                MDE_DrawProperty("paintBrushSize", "Brush Size");

            MDE_s(5);
            MDE_v();
            MDE_DrawProperty("paintSmoothBrushMovement", "Brush Smooth Movement");
            if (mp.paintSmoothBrushMovement)
                MDE_DrawProperty("paintSmoothBrushMovementSpeed", "Smooth Movement Speed");
            MDE_DrawProperty("paintSmoothBrushRotation", "Brush Smooth Rotation");
            if (mp.paintSmoothBrushRotation)
                MDE_DrawProperty("paintSmoothBrushRotationSpeed", "Smooth Rotation Speed");
            MDE_ve();
            MDE_s(5);

            MDE_DrawProperty("paintVertexDistanceLimitation", "Use Vertex Distance Limitation", "If enabled, vertices will be created after some exteeded distance");
            if (mp.paintVertexDistanceLimitation)
                MDE_DrawProperty("paintMinVertexDistance", "Min Vertex Distance", "How often should the vertices be created while painting?");

            MDE_s(5);

            MDE_DrawProperty("paintConnectMeshOnRelease", "Connect Mesh On Release", "If enabled, the created mesh will be connected on input-release event");

            MDE_s(5);

            MDE_DrawProperty("paintPaintingRotationType", "Brush Rotation Type", "Choose one of the rotation types. One Axis - better fits for 2D drawing; Spatial Axis - better fits for 3D drawing");
            if (mp.paintPaintingRotationType == MDG_MeshPaint.MeshPaintRotationType.FollowOneAxis)
                MDE_DrawProperty("paintRotationOffsetEuler", "Rotation Offset", "Additional rotation parameters (default: 0, 0, 1 = FORWARD)");

            MDE_s(5);
            MDE_v();
            MDE_DrawProperty("paintGeometryType", "Geometry Type", "Choose one of the geometries to draw");
            MDE_ve();
            MDE_ve();

            MDE_s();

            MDE_l("Painting Logic", true);
            MDE_v();
            MDE_DrawProperty("paintPaintingType", "Mesh Painting Type", "Choose one of the mesh painting types");
            MDE_s(5);
            if (mp.paintPaintingType == MDG_MeshPaint.MeshPaintType.DrawOnScreen)
            {
                MDE_l("Type: On Screen", true);
                MDE_v();
                MDE_DrawProperty("paint_TypeScreen_UseMainCamera", "Use MainCamera", "If enabled, the painting will find Camera.main object (camera with tag MainCamera) or you can choose your own camera");
                if (!mp.paint_TypeScreen_UseMainCamera)
                    MDE_DrawProperty("paint_TypeScreen_TargetCamera", "Target Camera");
                MDE_DrawProperty("paint_TypeScreen_Depth", "Painting Depth", "Z Value (distance from the main camera)");
                MDE_ve();
            }
            else if (mp.paintPaintingType == MDG_MeshPaint.MeshPaintType.DrawOnRaycastHit)
            {
                MDE_l("Type: On Raycast Hit", true);
                MDE_v();

                MDE_DrawProperty("paint_TypeRaycast_AllowedLayers", "Allowed Layers");
                MDE_DrawProperty("paint_TypeRaycast_CastAllObjects", "Cast All Objects", "If enabled, all objects will receive raycast from this script");
                if (!mp.paint_TypeRaycast_CastAllObjects)
                    MDE_DrawProperty("paint_TypeRaycast_TagForRaycast", "Tag For Raycast Objects");

                MDE_s(5);

                MDE_DrawProperty("paint_TypeRaycast_RaycastFromCursor", "Raycast From Cursor", "If enabled, raycast origin will be set to cursor");
                if (!mp.paint_TypeRaycast_RaycastFromCursor)
                    MDE_DrawProperty("paint_TypeRaycast_RaycastOriginFORWARD", "Raycast Origin (FORWARD Direction)", "Assign target direction for raycast (raycast direction will be THIS object's FORWARD)");

                MDE_s(5);

                MDE_DrawProperty("paint_TypeRaycast_BrushOffset", "Brush Offset");
                MDE_DrawProperty("paint_TypeRaycast_IgnoreSelfCasting", "Ignore Self Casting", "If enabled, raycast will ignore newly painted meshes");

                MDE_ve();
            }
            else if (mp.paintPaintingType == MDG_MeshPaint.MeshPaintType.CustomDraw)
            {
                MDE_l("Type: Custom", true);
                MDE_v();

                MDE_DrawProperty("paint_TypeCustom_IsPainting", "PAINT", "If enabled, the script will start painting the mesh (this is a manual paint method invokation)");
                MDE_DrawProperty("paint_TypeCustom_CustomBrushTransform", "Customize Brush Transform", "If enabled, you will be able to customize brush parent and its rotation behaviour");
                if (mp.paint_TypeCustom_CustomBrushTransform)
                {
                    MDE_DrawProperty("paint_TypeCustom_EnableSmartRotation", "Use Smart Rotation", "If enabled, the brush will rotate to the direction of its movement");
                    GUI.color = Color.gray;
                    MDE_DrawProperty("paint_TypeCustom_BrushParent", "Brush Parent", "If you won't to parent the brush, leave this field empty. If the parent is assigned, brush will be automatically set to ZERO local position");
                    GUI.color = Color.white;
                }

                MDE_ve();
            }
            MDE_ve();

            MDE_s();

            MDE_l("Appearance Settings", true);
            MDE_v();
            MDE_DrawProperty("paintSelectedAppearanceSlot", "Appearance Index", "Index of the selected appearance slot - Material/ Color (according to the bool-field below)");
            MDE_DrawProperty("paintUseMaterialSlots", "Use Material Slots", "If enabled, color slots will be hidden and you will be able to use specific material slots");
            if (mp.paintUseMaterialSlots)
                MDE_DrawProperty("paintMaterialSlots", "Available Materials", "List of material slots", true);
            else
                MDE_DrawProperty("paintColorSlots", "Available Colors", "List of color slots", true);

            MDE_s(5);

            MDE_DrawProperty("paintHandleMeshCollider", "Add & handle Mesh Collider automatically");

            MDE_ve();

            MDE_s();

            MDE_l("Additional", true);
            MDE_v();
            MDE_DrawProperty("paintUseCustomBrushTransform", "Use Custom Brush Transform", "If enabled, you can assign your own custom brush to follow hidden brush");
            if (mp.paintUseCustomBrushTransform)
            {
                MDE_DrawProperty("paintCompleteCustomBrush", "Custom Brush Object", "Complete custom brush that will follow the actual 'hidden' brush");
                if (mp.paintPaintingType == MDG_MeshPaint.MeshPaintType.DrawOnRaycastHit)
                    MDE_DrawProperty("paintHideCustomBrushIfNotRaycasting", "Hide Brush if not Raycasting", "Custom brush will be hidden if there is no raycast-hit available");
            }

            MDE_ve();

            MDE_s();

            MDE_l("Presets", true);
            if (MDE_b("2D-Ready Preset"))
                SetTo2D();
            if (MDE_b("3D-Ready Preset"))
                SetTo3D();
            if (MDE_b("VR-Ready Preset"))
                SetTo3D(true);

            serializedObject.Update();
        }

        private void SetTo2D()
        {
            MDG_MeshPaint mp = (MDG_MeshPaint)target;

            mp.paintPaintingRotationType = MDG_MeshPaint.MeshPaintRotationType.FollowOneAxis;
            mp.paintPaintingType = MDG_MeshPaint.MeshPaintType.DrawOnScreen;
            mp.paint_TypeScreen_UseMainCamera = true;
            mp.paint_TypeScreen_Depth = 10;

            mp.paintSelectedAppearanceSlot = 0;
            mp.paintUseMaterialSlots = false;
            mp.paintColorSlots = new Color[] { Color.white, Color.black, Color.blue, Color.red, Color.yellow, Color.green, Color.cyan };

            mp.paintHandleMeshCollider = false;
        }

        private void SetTo3D(bool vr = false)
        {
            MDG_MeshPaint mp = (MDG_MeshPaint)target;

            mp.paintTargetPlatform = vr ? MDG_MeshPaint.MeshPaintTargetPlatform.VR : MDG_MeshPaint.MeshPaintTargetPlatform.PC;
            mp.paintPaintingRotationType = MDG_MeshPaint.MeshPaintRotationType.FollowSpatialAxis;
            mp.paintPaintingType = vr ? MDG_MeshPaint.MeshPaintType.CustomDraw : MDG_MeshPaint.MeshPaintType.DrawOnRaycastHit;
            mp.paint_TypeRaycast_AllowedLayers = -1;
            mp.paint_TypeRaycast_CastAllObjects = true;
            mp.paint_TypeRaycast_RaycastFromCursor = true;
            mp.paint_TypeRaycast_BrushOffset = new Vector3(0, 0.2f, 0);
            mp.paint_TypeRaycast_IgnoreSelfCasting = true;

            mp.paintSelectedAppearanceSlot = 0;
            mp.paintUseMaterialSlots = false;
            mp.paintColorSlots = new Color[] { Color.white, Color.black, Color.blue, Color.red, Color.yellow, Color.green, Color.cyan };

            mp.paintHandleMeshCollider = true;
        }
    }
}