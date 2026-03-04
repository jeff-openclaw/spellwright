using UnityEngine;

namespace Spellwright.Rendering
{
    /// <summary>
    /// CRT effect settings. Attach to a GameObject in the scene to control CRT parameters.
    /// Provides a toggle and tunable parameters for the CRT post-processing effect.
    /// </summary>
    public class CRTSettings : MonoBehaviour
    {
        public static CRTSettings Instance { get; private set; }

        [Header("Toggle")]
        [Tooltip("Enable/disable the CRT effect (accessibility toggle)")]
        public bool crtEnabled = true;

        [Header("Scanlines")]
        [Range(0f, 0.5f)]
        [Tooltip("Intensity of horizontal scanline darkening")]
        public float scanlineIntensity = 0.25f;

        [Range(100f, 1200f)]
        [Tooltip("Frequency of scanlines (higher = more lines)")]
        public float scanlineFrequency = 320f;

        [Header("Barrel Distortion")]
        [Range(0f, 0.5f)]
        [Tooltip("Curved screen effect strength (higher = more center zoom)")]
        public float barrelDistortion = 0.3f;

        [Header("Chromatic Aberration")]
        [Range(0f, 0.01f)]
        [Tooltip("RGB channel offset (subtle color fringing)")]
        public float chromaticAberration = 0.003f;

        [Header("Noise (Purici)")]
        [Range(0f, 0.15f)]
        [Tooltip("Animated pixel static/grain intensity")]
        public float noiseIntensity = 0.04f;

        [Range(0f, 20f)]
        [Tooltip("Noise animation speed (frames per second)")]
        public float noiseSpeed = 8f;

        [Header("Vignette")]
        [Range(0f, 2f)]
        [Tooltip("Edge darkening intensity")]
        public float vignetteIntensity = 0.8f;

        [Range(0.5f, 2f)]
        [Tooltip("Vignette shape roundness")]
        public float vignetteRoundness = 0.8f;

        [Header("Phosphor Grid")]
        [Range(0f, 0.5f)]
        [Tooltip("RGB subpixel mask intensity")]
        public float phosphorIntensity = 0.08f;

        [Header("Screen Border (Bezel)")]
        [Range(0.5f, 1.5f)]
        [Tooltip("Screen edge curvature — how rounded the CRT bezel is")]
        public float screenCurvature = 1.15f;

        [Range(0.01f, 0.3f)]
        [Tooltip("Softness of the border falloff to black")]
        public float borderSoftness = 0.12f;

        [Header("Brightness")]
        [Range(0.8f, 1.5f)]
        [Tooltip("Overall brightness compensation")]
        public float brightness = 1.12f;

        // Shader property IDs (cached)
        private static readonly int PropScanlineIntensity = Shader.PropertyToID("_ScanlineIntensity");
        private static readonly int PropScanlineFrequency = Shader.PropertyToID("_ScanlineFrequency");
        private static readonly int PropBarrelDistortion = Shader.PropertyToID("_BarrelDistortion");
        private static readonly int PropChromaticAberration = Shader.PropertyToID("_ChromaticAberration");
        private static readonly int PropNoiseIntensity = Shader.PropertyToID("_NoiseIntensity");
        private static readonly int PropNoiseSpeed = Shader.PropertyToID("_NoiseSpeed");
        private static readonly int PropVignetteIntensity = Shader.PropertyToID("_VignetteIntensity");
        private static readonly int PropVignetteRoundness = Shader.PropertyToID("_VignetteRoundness");
        private static readonly int PropPhosphorIntensity = Shader.PropertyToID("_PhosphorIntensity");
        private static readonly int PropScreenCurvature = Shader.PropertyToID("_ScreenCurvature");
        private static readonly int PropBorderSoftness = Shader.PropertyToID("_BorderSoftness");
        private static readonly int PropBrightness = Shader.PropertyToID("_Brightness");

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Applies the current settings to the CRT material.
        /// Called each frame by the CRTRenderFeature.
        /// </summary>
        public void ApplyToMaterial(Material mat)
        {
            if (mat == null) return;

            mat.SetFloat(PropScanlineIntensity, scanlineIntensity);
            mat.SetFloat(PropScanlineFrequency, scanlineFrequency);
            mat.SetFloat(PropBarrelDistortion, barrelDistortion);
            mat.SetFloat(PropChromaticAberration, chromaticAberration);
            mat.SetFloat(PropNoiseIntensity, noiseIntensity);
            mat.SetFloat(PropNoiseSpeed, noiseSpeed);
            mat.SetFloat(PropVignetteIntensity, vignetteIntensity);
            mat.SetFloat(PropVignetteRoundness, vignetteRoundness);
            mat.SetFloat(PropPhosphorIntensity, phosphorIntensity);
            mat.SetFloat(PropScreenCurvature, screenCurvature);
            mat.SetFloat(PropBorderSoftness, borderSoftness);
            mat.SetFloat(PropBrightness, brightness);
        }

        /// <summary>Toggles the CRT effect on/off.</summary>
        public void ToggleCRT()
        {
            crtEnabled = !crtEnabled;
        }
    }
}
