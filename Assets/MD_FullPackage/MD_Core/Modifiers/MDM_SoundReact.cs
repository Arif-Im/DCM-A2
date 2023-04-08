using System;
using System.Collections;
using UnityEngine.Events;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

using MD_Package.Modifiers;
#endif

namespace MD_Package.Modifiers
{
    /// <summary>
    /// MDM(Mesh Deformation Modifier): Sound React.
    /// Simple sound reaction modifier - add another modifier to connect the sound react.
    /// Written by Matej Vanco (2022, updated in 2023).
    /// </summary>
    [AddComponentMenu(MD_Debug.ORGANISATION + MD_Debug.PACKAGENAME + "Modifiers/Sound React")]
    public sealed class MDM_SoundReact : MonoBehaviour
    {
        public AudioSource targetAudioSrc;
        public enum SampleDataLength { x128, x256, x512, x1024, x2048, x4096};
        public SampleDataLength sampleDataLength = SampleDataLength.x1024;
        public float transitionSmoothness = 128.0f;
        public float multiplication = 2.0f;
        public float minimumOutputValue = 0.0f;
        public float maximumOutputValue = 10.0f;

        public bool processOnStart = true;
        public float updateInterval = 0.01f;

        /// <summary>
        /// Main output data
        /// </summary>
        public float OutputData { private set; get; }
        public UnityEvent outputEvent;
        public event Action<float> outputActionEvent;

        private float clipRealtimeData;
        private float[] clipSampleData;

        private void Awake()
        {
            if (processOnStart)
                SoundReact_Start();
        }

        /// <summary>
        /// Start sound reaction - start receiving audio data from the target audio source
        /// </summary>
        public void SoundReact_Start()
        {
            StartCoroutine(ProcessSoundReaction());
        }

        /// <summary>
        /// Stop sound reaction - stop receiving audio data from the target audio source
        /// </summary>
        public void SoundReact_Stop()
        {
            StopAllCoroutines();
        }

        /// <summary>
        /// Returns a processed audio OutputData property
        /// </summary>
        public float SoundReact_GetAudioOutpudData()
        {
            return OutputData;
        }

        private IEnumerator ProcessSoundReaction()
        {
            int sampleData = 1024;
            switch(sampleDataLength)
            {
                case SampleDataLength.x128: sampleData = 128; break;
                case SampleDataLength.x256: sampleData = 256; break;
                case SampleDataLength.x512: sampleData = 512; break;
                case SampleDataLength.x1024: sampleData = 1024; break;
                case SampleDataLength.x2048: sampleData = 2048; break;
                case SampleDataLength.x4096: sampleData = 4096; break;

            }
            clipSampleData = new float[sampleData];

            while (true)
            {
                yield return new WaitForSeconds(updateInterval);
                targetAudioSrc.clip.GetData(clipSampleData, targetAudioSrc.timeSamples);
                clipRealtimeData = 0.0f;
                foreach (var s in clipSampleData)
                    clipRealtimeData += Mathf.Abs(s);
                clipRealtimeData /= sampleData;
                float formula = clipRealtimeData * multiplication;
                OutputData = Mathf.Lerp(OutputData, Mathf.Clamp(formula, minimumOutputValue, maximumOutputValue), transitionSmoothness * Time.deltaTime);
                outputEvent?.Invoke();
                outputActionEvent?.Invoke(OutputData);
            }
        }
    }
}

#if UNITY_EDITOR
namespace MD_Package_Editor
{
    [CustomEditor(typeof(MDM_SoundReact))]
    [CanEditMultipleObjects]
    public sealed class MDM_SoundReact_Editor : MD_EditorUtilities
    {
        public override void OnInspectorGUI()
        {
            MDE_s();

            MDE_v();
            MDE_l("Essential Settings", true);
            MDE_v();
            MDE_v();
            MDE_DrawProperty("targetAudioSrc", "Target Audio Source", "Target audio source which holds the audio data for computation");
            MDE_ve();
            MDE_DrawProperty("processOnStart", "Process On Start", "If enabled, the script will start processing right after the startup");
            MDE_DrawProperty("updateInterval", "Update Interval (every N second)");
            MDE_ve();

            MDE_s();

            MDE_l("Output Settings", true);
            MDE_v();
            MDE_DrawProperty("sampleDataLength", "Sample Data Length", "Sample data length value - the higher the value is, the more precise the audio data will result");
            MDE_DrawProperty("transitionSmoothness", "Output Transition Smoothness", "Transition smoothness of the output data - how smooth the value will be?");
            MDE_DrawProperty("multiplication", "Output Multiplier", "Output data value multiplier - simple amplification");
            MDE_v();
            MDE_DrawProperty("minimumOutputValue", "Minimum Output Value", "Minimum value of the output data");
            MDE_DrawProperty("maximumOutputValue", "Maximum Output Value", "Maximum value of the output data");
            MDE_ve();
            MDE_ve();

            MDE_s();

            MDE_DrawProperty("outputEvent", "Output Event", "Unity event of the output data value - connect supported modifiers or any 3rd party methods to receive compiled output data");
            MDE_ve();

            MDE_s();

            if (target != null) serializedObject.Update();
        }
    }
}
#endif
