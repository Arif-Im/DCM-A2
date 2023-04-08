using UnityEditor;

namespace MD_Package_Editor
{
    /// <summary>
    /// Main material editor for Standard Deformer shader
    /// </summary>
    public sealed class MD_StandardDeformer_Editor : MD_MaterialEditorUtilities
    {
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            MDE_s();
            MDE_hb("MD Package - Mesh Deformer 1.2 [January 2023]");
            MDE_s();
            MDE_l("Essentials");
            MDE_v();
            MDE_DrawProperty(materialEditor, properties, "_Cull");
            MDE_DrawProperty(materialEditor, properties, "_Color");
            MDE_v();
            MDE_DrawProperty(materialEditor, properties, "_MainTex", true);
            materialEditor.TextureScaleOffsetProperty(FindProperty("_MainTex", properties));
            MDE_ve();
            MDE_v();
            MDE_DrawProperty(materialEditor, properties, "_MainNormal", true);
            materialEditor.TextureScaleOffsetProperty(FindProperty("_MainNormal", properties));
            MDE_DrawProperty(materialEditor, properties, "_Normal");
            MDE_ve();
            MDE_v();
            MDE_DrawProperty(materialEditor, properties, "_Specular");
            MDE_DrawProperty(materialEditor, properties, "_MainMetallic", true);
            materialEditor.TextureScaleOffsetProperty(FindProperty("_MainMetallic", properties));
            MDE_DrawProperty(materialEditor, properties, "_Metallic");
            MDE_ve();
            MDE_v();
            MDE_DrawProperty(materialEditor, properties, "_MainEmission", true);
            materialEditor.TextureScaleOffsetProperty(FindProperty("_MainEmission", properties));
            MDE_DrawProperty(materialEditor, properties, "_Emission");
            MDE_ve();
            MDE_ve();

            MDE_s();
            MDE_l("Deformers");
            MDE_v();
            MDE_DrawProperty(materialEditor, properties, "_DEFAnim", "Deformer Animation Type");
            MDE_DrawProperty(materialEditor, properties, "_DEFDirection");
            MDE_DrawProperty(materialEditor, properties, "_DEFFrequency");
            MDE_v();
            MDE_DrawProperty(materialEditor, properties, "_DEFEdges");
            MDE_DrawProperty(materialEditor, properties, "_DEFEdgesAmount");
            MDE_DrawProperty(materialEditor, properties, "_DEFExtrusion");
            MDE_ve();
            MDE_ve();
            MDE_s();
            MDE_l("Deformer Additional Properties");
            MDE_v();
            MDE_DrawProperty(materialEditor, properties, "_DEFAbsolute");
            MDE_DrawProperty(materialEditor, properties, "_DEFFract");
            if (MDE_CompareProperty(materialEditor, "_DEFFract", 1))
                MDE_DrawProperty(materialEditor, properties, "_DEFFractValue");
            MDE_ve();
            MDE_s();
            MDE_l("Clipping");
            MDE_v();
            MDE_DrawProperty(materialEditor, properties, "_DEFClipping");
            if (MDE_CompareProperty(materialEditor, "_DEFClipping", 1))
            {
                MDE_DrawProperty(materialEditor, properties, "_DEFClipType");
                MDE_DrawProperty(materialEditor, properties, "_DEFClipTile");
                MDE_DrawProperty(materialEditor, properties, "_DEFClipSize");
                MDE_DrawProperty(materialEditor, properties, "_DEFAnimateClip");
                if (MDE_CompareProperty(materialEditor, "_DEFAnimateClip", 1))
                    MDE_DrawProperty(materialEditor, properties, "_DEFClipAnimSpeed");
            }
            MDE_ve();
            MDE_s();
            MDE_l("Noise");
            MDE_v();
            MDE_DrawProperty(materialEditor, properties, "_DEFNoise");
            if (MDE_CompareProperty(materialEditor, "_DEFNoise", 1))
            {
                MDE_DrawProperty(materialEditor, properties, "_DEFNoiseDirection");
                MDE_DrawProperty(materialEditor, properties, "_DEFNoiseSpeed");
            }
            MDE_ve();
           
            MDE_s(40);
        }
    }
}