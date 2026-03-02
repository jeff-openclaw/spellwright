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
        public float scanlineIntensity = 0.15f;

        [Range(100f, 1200f)]
        [Tooltip("Frequency of scanlines (higher = more lines)")]
        public float scanlineFrequency = 400f;

        [Header("Barrel Distortion")]
        [Range(0f, 0.3f)]
        [Tooltip("Curved screen effect strength")]
        public float barrelDistortion = 0.05f;

        [Header("Chromatic Aberration")]
        [Range(0f, 0.01f)]
        [Tooltip("RGB channel offset (subtle color fringing)")]
        public float chromaticAberration = 0.001f;

        [Header("Vignette")]
        [Range(0f, 2f)]
        [Tooltip("Edge darkening intensity")]
        public float vignetteIntensity = 0.4f;

        [Range(0.5f, 2f)]
        [Tooltip("Vignette shape roundness")]
        public float vignetteRoundness = 0.8f;

        [Header("Phosphor Grid")]
        [Range(0f, 0.5f)]
        [Tooltip("RGB subpixel mask intensity")]
        public float phosphorIntensity = 0.08f;

        [Header("Brightness")]
        [Range(0.8f, 1.5f)]
        [Tooltip("Overall brightness compensation")]
        public float brightness = 1.1f;

        // Shader property IDs (cached)
        private static readonly int PropScanlineIntensity = Shader.PropertyToID("_ScanlineIntensity");
        private static readonly int PropScanlineFrequency = Shader.PropertyToID("_ScanlineFrequency");
        private static readonly int PropBarrelDistortion = Shader.PropertyToID("_BarrelDistortion");
        private static readonly int PropChromaticAberration = Shader.PropertyToID("_ChromaticAberration");
        private static readonly int PropVignetteIntensity = Shader.PropertyToID("_VignetteIntensity");
        private static readonly int PropVignetteRoundness = Shader.PropertyToID("_VignetteRoundness");
        private static readonly int PropPhosphorIntensity = Shader.PropertyToID("_PhosphorIntensity");
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
            mat.SetFloat(PropVignetteIntensity, vignetteIntensity);
            mat.SetFloat(PropVignetteRoundness, vignetteRoundness);
            mat.SetFloat(PropPhosphorIntensity, phosphorIntensity);
            mat.SetFloat(PropBrightness, brightness);
        }

        /// <summary>Toggles the CRT effect on/off.</summary>
        public void ToggleCRT()
        {
            crtEnabled = !crtEnabled;
        }
    }
}
