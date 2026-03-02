using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

namespace Spellwright.Rendering
{
    /// <summary>
    /// URP Renderer Feature that applies a CRT post-processing effect.
    /// Add this to the URP Renderer asset (PC_Renderer or Mobile_Renderer).
    /// Uses URP 17 Render Graph API.
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
            if (CRTSettings.Instance == null || !CRTSettings.Instance.crtEnabled) return;

            CRTSettings.Instance.ApplyToMaterial(_material);
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            if (_material != null)
                CoreUtils.Destroy(_material);
        }
    }

    /// <summary>
    /// Render pass that blits the screen through the CRT shader using Render Graph.
    /// </summary>
    public class CRTRenderPass : ScriptableRenderPass
    {
        private readonly Material _material;
        private static readonly int ResolutionId = Shader.PropertyToID("_Resolution");

        public CRTRenderPass(Material material)
        {
            _material = material;
            profilingSampler = new ProfilingSampler("CRT Effect");
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_material == null) return;

            var resourceData = frameData.Get<UniversalResourceData>();

            if (resourceData.isActiveTargetBackBuffer)
                return;

            var source = resourceData.activeColorTexture;
            if (!source.IsValid()) return;

            // Set resolution for phosphor grid calculation
            var cameraData = frameData.Get<UniversalCameraData>();
            var camDesc = cameraData.cameraTargetDescriptor;
            _material.SetVector(ResolutionId, new Vector4(camDesc.width, camDesc.height, 0, 0));

            // Create temp texture matching source
            var desc = renderGraph.GetTextureDesc(source);
            desc.name = "_CRTTemp";
            desc.clearBuffer = false;
            var destination = renderGraph.CreateTexture(desc);

            // Blit source -> destination through CRT material
            renderGraph.AddBlitPass(
                new RenderGraphUtils.BlitMaterialParameters(source, destination, _material, 0),
                passName: "CRT Effect");

            // Redirect URP's color output to our processed texture
            resourceData.cameraColor = destination;
        }
    }
}
