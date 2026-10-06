using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Wirabaya.Rendering
{
    /// <summary>
    /// Motion blur ala game AAA:
    /// - Menggunakan motion vectors URP & depth-aware bilateral rejection (anti-ghosting).
    /// - Menggunakan Golden-Ratio dither + Gaussian falloff (silky smooth, anti-banding).
    /// - Karakter pemain bertanda ForceNoMotion 100% bebas dari blur dan tidak meninggalkan siluet ke latar.
    /// </summary>
    public class SmoothMotionBlurFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            public bool enabled = true;
            [Range(0f, 2f)] [Tooltip("Panjang blur relatif terhadap gerakan (seperti shutter kamera). 0.5-0.8 natural.")]
            public float shutter = 0.6f;
            [Range(0.005f, 0.1f)] [Tooltip("Batas maksimum panjang blur (fraksi layar).")]
            public float maxBlur = 0.035f;
            [Range(8, 48)] [Tooltip("Jumlah sampel. Makin tinggi makin halus.")]
            public int samples = 24;
        }

        public Settings settings = new Settings();

        Material m_Material;
        BlurPass m_Pass;

        public override void Create()
        {
            Shader shader = Shader.Find("Hidden/Wirabaya/SmoothMotionBlur");
            if (shader == null)
            {
                Debug.LogWarning("[SmoothMotionBlur] Shader Hidden/Wirabaya/SmoothMotionBlur tidak ditemukan.");
                return;
            }

            CoreUtils.Destroy(m_Material);
            m_Material = CoreUtils.CreateEngineMaterial(shader);
            m_Pass = new BlurPass(m_Material)
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
            };
            m_Pass.ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Motion);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (m_Material == null || m_Pass == null || !settings.enabled)
                return;
            if (renderingData.cameraData.cameraType != CameraType.Game)
                return;

            m_Pass.Setup(settings);
            renderer.EnqueuePass(m_Pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(m_Material);
            m_Material = null;
        }

        class BlurPass : ScriptableRenderPass
        {
            static readonly MaterialPropertyBlock s_Block = new MaterialPropertyBlock();
            static readonly int s_BlitTexture = Shader.PropertyToID("_BlitTexture");
            static readonly int s_BlitScaleBias = Shader.PropertyToID("_BlitScaleBias");
            static readonly int s_MotionVectorTexture = Shader.PropertyToID("_MotionVectorTexture");
            static readonly int s_Shutter = Shader.PropertyToID("_Shutter");
            static readonly int s_MaxBlur = Shader.PropertyToID("_MaxBlur");
            static readonly int s_SampleCount = Shader.PropertyToID("_SampleCount");

            readonly Material m_Material;
            Settings m_Settings;

            public BlurPass(Material material)
            {
                m_Material = material;
                profilingSampler = new ProfilingSampler("Smooth Motion Blur");
            }

            public void Setup(Settings settings)
            {
                m_Settings = settings;
            }

            class BlurPassData
            {
                public Material material;
                public TextureHandle source;
                public TextureHandle depth;
                public TextureHandle motionVectors;
                public float shutter;
                public float maxBlur;
                public float samples;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                if (!resourceData.cameraColor.IsValid() ||
                    !resourceData.cameraDepthTexture.IsValid() ||
                    !resourceData.motionVectorColor.IsValid())
                    return;

                RenderTextureDescriptor colorDesc = cameraData.cameraTargetDescriptor;
                colorDesc.depthStencilFormat = GraphicsFormat.None;
                colorDesc.msaaSamples = 1;
                TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(renderGraph, colorDesc, "_SmoothMotionBlurColor", false);

                using (var builder = renderGraph.AddRasterRenderPass<BlurPassData>("Smooth Motion Blur", out var blurData, profilingSampler))
                {
                    blurData.material = m_Material;
                    blurData.source = resourceData.cameraColor;
                    blurData.depth = resourceData.cameraDepthTexture;
                    blurData.motionVectors = resourceData.motionVectorColor;
                    blurData.shutter = m_Settings.shutter;
                    blurData.maxBlur = m_Settings.maxBlur;
                    blurData.samples = m_Settings.samples;

                    builder.UseTexture(blurData.source, AccessFlags.Read);
                    builder.UseTexture(blurData.depth, AccessFlags.Read);
                    builder.UseTexture(blurData.motionVectors, AccessFlags.Read);
                    builder.SetRenderAttachment(destination, 0, AccessFlags.Write);

                    builder.SetRenderFunc((BlurPassData data, RasterGraphContext context) =>
                    {
                        RTHandle source = data.source;
                        RTHandle motion = data.motionVectors;

                        s_Block.Clear();
                        s_Block.SetTexture(s_BlitTexture, source);
                        s_Block.SetTexture(s_MotionVectorTexture, motion);
                        s_Block.SetVector(s_BlitScaleBias, new Vector4(1, 1, 0, 0));
                        s_Block.SetFloat(s_Shutter, data.shutter);
                        s_Block.SetFloat(s_MaxBlur, data.maxBlur);
                        s_Block.SetFloat(s_SampleCount, data.samples);

                        context.cmd.DrawProcedural(Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, 3, 1, s_Block);
                    });
                }

                resourceData.cameraColor = destination;
            }
        }
    }
}
