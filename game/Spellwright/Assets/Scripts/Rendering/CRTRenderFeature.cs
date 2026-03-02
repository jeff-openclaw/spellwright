using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Spellwright.Rendering
{
    /// <summary>
    /// URP Renderer Feature that applies a CRT post-processing effect.
    /// Add this to the URP Renderer asset (PC_Renderer or Mobile_Renderer).
    /// </summary>
    public class CRTRenderFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class CRTFeatureSettings
        {
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
            public Shader crtShader;
        }

        public CRTFeatureSettings settings = new CRTFeatureSettings();
        private CRTRenderPass _pass;
        private Material _material;

        public override void Create()
        {
            if (settings.crtShader == null)
                settings.crtShader = Shader.Find("Hidden/Spellwright/CRT");

            if (settings.crtShader != null)
            {
                _material = CoreUtils.CreateEngineMaterial(settings.crtShader);
                _pass = new CRTRenderPass(_material)
                {
                    renderPassEvent = settings.renderPassEvent
                };
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_pass == null || _material == null) return;

            // Check if CRT is enabled via CRTSettings singleton
            if (CRTSettings.Instance == null || !CRTSettings.Instance.crtEnabled) return;

            // Apply settings to material
            CRTSettings.Instance.ApplyToMaterial(_material);

            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            _pass?.Dispose();
            if (_material != null)
                CoreUtils.Destroy(_material);
        }
    }

    /// <summary>
    /// Render pass that blits the screen through the CRT shader.
    /// </summary>
    public class CRTRenderPass : ScriptableRenderPass
    {
        private readonly Material _material;
        private RTHandle _tempTexture;
        private static readonly int TempTexId = Shader.PropertyToID("_CRTTemp");

        public CRTRenderPass(Material material)
        {
            _material = material;
            profilingSampler = new ProfilingSampler("CRT Effect");
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            RenderingUtils.ReAllocateHandleIfNeeded(ref _tempTexture, desc, name: "_CRTTemp");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_material == null) return;

            var cmd = CommandBufferPool.Get("CRT Effect");

            using (new ProfilingScope(cmd, profilingSampler))
            {
                var source = renderingData.cameraData.renderer.cameraColorTargetHandle;

                // Set resolution for phosphor grid calculation
                var desc = renderingData.cameraData.cameraTargetDescriptor;
                _material.SetVector("_Resolution", new Vector4(desc.width, desc.height, 0, 0));

                Blitter.BlitCameraTexture(cmd, source, _tempTexture, _material, 0);
                Blitter.BlitCameraTexture(cmd, _tempTexture, source);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            _tempTexture?.Release();
        }
    }
}
