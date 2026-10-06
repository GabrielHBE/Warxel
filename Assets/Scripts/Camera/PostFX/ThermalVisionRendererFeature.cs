using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class ThermalVisionRendererFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader maskShader;
    [SerializeField] private Shader compositeShader;
    private Material maskMaterial;
    private Material compositeMaterial;
    private ThermalPass pass;

    public override void Create()
    {
        ReleaseResources();
        if (maskShader == null) maskShader = Shader.Find("Hidden/Warxel/ThermalMask");
        if (compositeShader == null) compositeShader = Shader.Find("Hidden/Warxel/ThermalComposite");
        if (maskShader == null || compositeShader == null) return;

        maskMaterial = CoreUtils.CreateEngineMaterial(maskShader);
        compositeMaterial = CoreUtils.CreateEngineMaterial(compositeShader);
        pass = new ThermalPass(maskMaterial, compositeMaterial);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        Camera camera = renderingData.cameraData.camera;
        if (pass == null || camera.cameraType != CameraType.Game ||
            !ThermalVisionPostFX.TryGetRenderSettings(camera, out _)) return;

        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing) => ReleaseResources();

    private void ReleaseResources()
    {
        pass?.Dispose();
        pass = null;
        CoreUtils.Destroy(maskMaterial);
        CoreUtils.Destroy(compositeMaterial);
        maskMaterial = null;
        compositeMaterial = null;
    }

    private sealed class ThermalPass : ScriptableRenderPass
    {
        private static readonly int MaskId = Shader.PropertyToID("_ThermalMask");
        private static readonly int ImageId = Shader.PropertyToID("_ThermalImage");
        private static readonly int SensorId = Shader.PropertyToID("_ThermalSensor");
        private static readonly int TexelSizeId = Shader.PropertyToID("_ThermalMask_TexelSize");
        private static readonly List<ShaderTagId> ShaderTags = new List<ShaderTagId>
        {
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("UniversalForwardOnly"),
            new ShaderTagId("SRPDefaultUnlit"),
            new ShaderTagId("UniversalGBuffer"),
            new ShaderTagId("LightweightForward")
        };

        private readonly Material maskMaterial;
        private readonly Material compositeMaterial;
        private RTHandle mask;
        private RTHandle result;

        public ThermalPass(Material maskMaterial, Material compositeMaterial)
        {
            this.maskMaterial = maskMaterial;
            this.compositeMaterial = compositeMaterial;
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            requiresIntermediateTexture = true;
        }

        private static void SetImageSettings(Material material, ThermalVisionPostFX.RenderSettings settings, int width, int height)
        {
            material.SetVector(ImageId, settings.image);
            material.SetVector(SensorId, settings.sensor);
            material.SetVector(TexelSizeId, new Vector4(1f / width, 1f / height, width, height));
        }

#pragma warning disable 618, 672 // URP 17 compatibility path, alongside RecordRenderGraph.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
            RenderingUtils.ReAllocateHandleIfNeeded(ref mask, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_ThermalMask");

            descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;
            RenderingUtils.ReAllocateHandleIfNeeded(ref result, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_ThermalResult");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!ThermalVisionPostFX.TryGetRenderSettings(renderingData.cameraData.camera, out var settings)) return;
            ScriptableRenderer renderer = renderingData.cameraData.renderer;
            CommandBuffer cmd = CommandBufferPool.Get("Thermal Vision");
            try
            {
                // Clear only the mask color. Preserve the scene depth to hide targets behind walls.
                CoreUtils.SetRenderTarget(cmd, mask, renderer.cameraDepthTargetHandle, ClearFlag.Color, Color.black);
                var drawing = CreateDrawingSettings(ShaderTags, ref renderingData, SortingCriteria.CommonOpaque);
                drawing.overrideMaterial = maskMaterial;
                drawing.overrideMaterialPassIndex = 0;
                var filtering = new FilteringSettings(RenderQueueRange.all, settings.layerMask);
                var parameters = new RendererListParams(renderingData.cullResults, drawing, filtering);
                cmd.DrawRendererList(context.CreateRendererList(ref parameters));
                cmd.SetGlobalTexture(MaskId, mask.nameID);

                SetImageSettings(compositeMaterial, settings, mask.rt.width, mask.rt.height);
                Blitter.BlitCameraTexture(cmd, renderer.cameraColorTargetHandle, result, compositeMaterial, 0);
                Blitter.BlitCameraTexture(cmd, result, renderer.cameraColorTargetHandle);
                context.ExecuteCommandBuffer(cmd);
            }
            finally
            {
                CommandBufferPool.Release(cmd);
            }
        }
#pragma warning restore 618, 672

        private sealed class MaskData
        {
            public RendererListHandle renderers;
        }

        private sealed class CompositeData
        {
            public TextureHandle source;
            public Material material;
            public ThermalVisionPostFX.RenderSettings settings;
            public int width;
            public int height;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            if (!ThermalVisionPostFX.TryGetRenderSettings(cameraData.camera, out var settings)) return;
            var resources = frameData.Get<UniversalResourceData>();
            if (resources.isActiveTargetBackBuffer) return;

            var maskDescriptor = renderGraph.GetTextureDesc(resources.activeColorTexture);
            maskDescriptor.name = "Thermal Hot Layers";
            maskDescriptor.colorFormat = GraphicsFormat.R8G8B8A8_UNorm;
            maskDescriptor.depthBufferBits = DepthBits.None;
            maskDescriptor.msaaSamples = renderGraph.GetTextureDesc(resources.activeDepthTexture).msaaSamples;
            maskDescriptor.bindTextureMS = false;
            maskDescriptor.clearBuffer = true;
            maskDescriptor.clearColor = Color.black;
            TextureHandle hotMask = renderGraph.CreateTexture(maskDescriptor);

            var renderingData = frameData.Get<UniversalRenderingData>();
            var lightData = frameData.Get<UniversalLightData>();
            var drawing = RenderingUtils.CreateDrawingSettings(ShaderTags, renderingData, cameraData, lightData, SortingCriteria.CommonOpaque);
            drawing.overrideMaterial = maskMaterial;
            drawing.overrideMaterialPassIndex = 0;
            var filtering = new FilteringSettings(RenderQueueRange.all, settings.layerMask);
            var parameters = new RendererListParams(renderingData.cullResults, drawing, filtering);

            using (var builder = renderGraph.AddRasterRenderPass<MaskData>("Thermal: visible hot objects", out var data))
            {
                data.renderers = renderGraph.CreateRendererList(parameters);
                builder.UseRendererList(data.renderers);
                builder.SetRenderAttachment(hotMask, 0);
                builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.Read);
                builder.SetGlobalTextureAfterPass(hotMask, MaskId);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((MaskData passData, RasterGraphContext context) => context.cmd.DrawRendererList(passData.renderers));
            }

            var resultDescriptor = renderGraph.GetTextureDesc(resources.activeColorTexture);
            resultDescriptor.name = "Thermal Vision Color";
            resultDescriptor.clearBuffer = false;
            resultDescriptor.depthBufferBits = DepthBits.None;
            resultDescriptor.msaaSamples = MSAASamples.None;
            TextureHandle destination = renderGraph.CreateTexture(resultDescriptor);
            using (var builder = renderGraph.AddRasterRenderPass<CompositeData>("Thermal: white hot composite", out var data))
            {
                data.source = resources.activeColorTexture;
                data.material = compositeMaterial;
                data.settings = settings;
                data.width = maskDescriptor.width;
                data.height = maskDescriptor.height;
                builder.UseTexture(data.source, AccessFlags.Read);
                builder.UseTexture(hotMask, AccessFlags.Read);
                builder.SetRenderAttachment(destination, 0);
                builder.SetRenderFunc((CompositeData passData, RasterGraphContext context) =>
                {
                    SetImageSettings(passData.material, passData.settings, passData.width, passData.height);
                    Blitter.BlitTexture(context.cmd, passData.source, new Vector4(1f, 1f, 0f, 0f), passData.material, 0);
                });
            }

            resources.cameraColor = destination;
        }

        public void Dispose()
        {
            mask?.Release();
            result?.Release();
        }
    }
}
