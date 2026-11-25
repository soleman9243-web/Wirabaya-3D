using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Wirabaya.Rendering
{
    /// <summary>
    /// Renderer Feature URP untuk menangkap gambar akhir layar (termasuk fog & post-processing)
    /// lalu memprosesnya dengan Dual Kawase Blur agar bisa dipakai sebagai efek kaca buram (Frosted Glass)
    /// pada elemen Canvas UI (termasuk Screen Space - Overlay).
    /// </summary>
    public class UIFrostedGlassFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            public bool enabled = true;

            [Tooltip("Shader blur latar belakang UI (Hidden/Wirabaya/UIBackgroundBlur).")]
            public Shader blurShader;

            [Tooltip("Titik injeksi di pipeline URP. AfterRenderingPostProcessing menangkap cahaya, fog, dan tonemapping akhir.")]
            public RenderPassEvent passEvent = RenderPassEvent.AfterRenderingPostProcessing;

            [Range(0.5f, 6f)]
            [Tooltip("Sebaran dasar Kawase Blur global sebelum diatur per-panel oleh UIFrostedGlass.")]
            public float globalBlurSpread = 2.0f;

            [Range(1, 4)]
            [Tooltip("Jumlah iterasi downsample/upsample Kawase (2-3 sudah sangat halus).")]
            public int iterations = 3;

            [Tooltip("Aktifkan hanya untuk kamera Game (termasuk Game View Edit Mode).")]
            public bool gameCameraOnly = true;

            [Tooltip("Konversi Linear ke sRGB di shader UI saat target kamera HDR/Linear.")]
            public bool convertLinearToSrgbInUI = false;
        }

        public Settings settings = new Settings();

        Material m_Material;
        FrostedGlassCapturePass m_Pass;

        public override void Create()
        {
            Shader shader = settings.blurShader != null
                ? settings.blurShader
                : Shader.Find("Hidden/Wirabaya/UIBackgroundBlur");

            if (shader == null)
            {
                Debug.LogWarning("[UIFrostedGlassFeature] Shader Hidden/Wirabaya/UIBackgroundBlur tidak ditemukan.");
                return;
            }

            settings.blurShader = shader;

            CoreUtils.Destroy(m_Material);
            m_Material = CoreUtils.CreateEngineMaterial(shader);

            m_Pass?.DisposeHandles();
            m_Pass = new FrostedGlassCapturePass(m_Material)
            {
                renderPassEvent = settings.passEvent
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (m_Material == null || m_Pass == null || !settings.enabled)
                return;

            if (settings.gameCameraOnly && renderingData.cameraData.cameraType != CameraType.Game)
                return;

            if (renderingData.cameraData.isPreviewCamera)
                return;

            m_Pass.renderPassEvent = settings.passEvent;
            m_Pass.Setup(settings);
            renderer.EnqueuePass(m_Pass);
        }

        protected override void Dispose(bool disposing)
        {
            m_Pass?.DisposeHandles();
            m_Pass = null;
            CoreUtils.Destroy(m_Material);
            m_Material = null;
            Shader.SetGlobalVector(FrostedGlassCapturePass.s_BackgroundReadyId, Vector4.zero);
        }

        class FrostedGlassCapturePass : ScriptableRenderPass
        {
            static readonly MaterialPropertyBlock s_Block = new MaterialPropertyBlock();
            static readonly int s_BlitTextureId = Shader.PropertyToID("_BlitTexture");
            static readonly int s_BlitScaleBiasId = Shader.PropertyToID("_BlitScaleBias");
            static readonly int s_BlurTexelSizeId = Shader.PropertyToID("_BlurTexelSize");
            static readonly int s_BlurOffsetId = Shader.PropertyToID("_BlurOffset");
            static readonly int s_SharpTexId = Shader.PropertyToID("_WirabayaUISharpTex");
            static readonly int s_BlurTexId = Shader.PropertyToID("_WirabayaUIBlurTex");
            public static readonly int s_BackgroundReadyId = Shader.PropertyToID("_WirabayaUIBackgroundReady");

            const int PassCopySharp = 0;
            const int PassKawaseDown = 1;
            const int PassKawaseUp = 2;

            readonly Material m_Material;
            Settings m_Settings;

            RTHandle m_SharpHandle;
            RTHandle m_BlurHandle;
            int m_AllocatedW;
            int m_AllocatedH;
            GraphicsFormat m_AllocatedFormat;

            public FrostedGlassCapturePass(Material material)
            {
                m_Material = material;
                profilingSampler = new ProfilingSampler("UI Frosted Glass Blur");
            }

            public void Setup(Settings settings)
            {
                m_Settings = settings;
            }

            public void DisposeHandles()
            {
                m_SharpHandle?.Release();
                m_SharpHandle = null;
                m_BlurHandle?.Release();
                m_BlurHandle = null;
                m_AllocatedW = 0;
                m_AllocatedH = 0;
            }

            void EnsurePersistentHandles(int srcWidth, int srcHeight, GraphicsFormat colorFormat)
            {
                GraphicsFormat safeFormat = colorFormat != GraphicsFormat.None
                    ? colorFormat
                    : GraphicsFormat.R8G8B8A8_UNorm;

                int halfW = Mathf.Max(4, srcWidth / 2);
                int halfH = Mathf.Max(4, srcHeight / 2);

                if (m_SharpHandle != null && m_BlurHandle != null &&
                    m_AllocatedW == halfW && m_AllocatedH == halfH &&
                    m_AllocatedFormat == safeFormat)
                {
                    return;
                }

                DisposeHandles();

                int quarterW = Mathf.Max(4, halfW / 2);
                int quarterH = Mathf.Max(4, halfH / 2);

                m_SharpHandle = RTHandles.Alloc(
                    halfW, halfH,
                    colorFormat: safeFormat,
                    filterMode: FilterMode.Bilinear,
                    wrapMode: TextureWrapMode.Clamp,
                    name: "_WirabayaUISharpTex");

                m_BlurHandle = RTHandles.Alloc(
                    quarterW, quarterH,
                    colorFormat: safeFormat,
                    filterMode: FilterMode.Bilinear,
                    wrapMode: TextureWrapMode.Clamp,
                    name: "_WirabayaUIBlurTex");

                m_AllocatedW = halfW;
                m_AllocatedH = halfH;
                m_AllocatedFormat = safeFormat;
            }

            class BlitPassData
            {
                public Material material;
                public TextureHandle source;
                public int passIndex;
                public Vector4 texelSize;
                public float blurOffset;
                public bool useSourceScale;
            }

            static void EnqueueBlitPass(
                RenderGraph renderGraph,
                string passName,
                Material material,
                TextureHandle source,
                TextureHandle destination,
                int passIndex,
                int dstWidth,
                int dstHeight,
                float blurOffset,
                bool useSourceScale,
                bool preventCulling)
            {
                using (var builder = renderGraph.AddRasterRenderPass<BlitPassData>(passName, out var passData))
                {
                    passData.material = material;
                    passData.source = source;
                    passData.passIndex = passIndex;
                    passData.texelSize = new Vector4(1f / Mathf.Max(1, dstWidth), 1f / Mathf.Max(1, dstHeight), dstWidth, dstHeight);
                    passData.blurOffset = blurOffset;
                    passData.useSourceScale = useSourceScale;

                    builder.UseTexture(source, AccessFlags.Read);
                    builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                    if (preventCulling)
                    {
                        builder.AllowPassCulling(false);
                    }

                    builder.SetRenderFunc((BlitPassData data, RasterGraphContext context) =>
                    {
                        RTHandle src = data.source;
                        Vector4 scaleBias = Vector4.zero;
                        if (data.useSourceScale && src.useScaling)
                        {
                            Vector2 rtScale = src.rtHandleProperties.rtHandleScale;
                            scaleBias = new Vector4(rtScale.x, rtScale.y, 0f, 0f);
                        }
                        else
                        {
                            scaleBias = new Vector4(1f, 1f, 0f, 0f);
                        }

                        s_Block.Clear();
                        s_Block.SetTexture(s_BlitTextureId, src);
                        s_Block.SetVector(s_BlitScaleBiasId, scaleBias);
                        s_Block.SetVector(s_BlurTexelSizeId, data.texelSize);
                        s_Block.SetFloat(s_BlurOffsetId, data.blurOffset);

                        context.cmd.DrawProcedural(Matrix4x4.identity, data.material, data.passIndex, MeshTopology.Triangles, 3, 1, s_Block);
                    });
                }
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                TextureHandle sourceColor = resourceData.activeColorTexture;
                if (!sourceColor.IsValid() || resourceData.isActiveTargetBackBuffer)
                {
                    if (resourceData.cameraColor.IsValid())
                        sourceColor = resourceData.cameraColor;
                    else
                        return;
                }

                RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
                EnsurePersistentHandles(desc.width, desc.height, desc.graphicsFormat);

                TextureHandle sharpHandle = renderGraph.ImportTexture(m_SharpHandle);
                TextureHandle blurHandle = renderGraph.ImportTexture(m_BlurHandle);

                int halfW = m_AllocatedW;
                int halfH = m_AllocatedH;
                int quarterW = Mathf.Max(4, halfW / 2);
                int quarterH = Mathf.Max(4, halfH / 2);

                // 1. Copy setengah resolusi (tajam untuk transisi blur 0 -> sedang)
                EnqueueBlitPass(
                    renderGraph,
                    "UI Frosted Glass Copy",
                    m_Material,
                    sourceColor,
                    sharpHandle,
                    PassCopySharp,
                    halfW,
                    halfH,
                    1f,
                    true,
                    true);

                int iterations = Mathf.Clamp(m_Settings.iterations, 1, 4);
                float spread = Mathf.Clamp(m_Settings.globalBlurSpread, 0.5f, 6f);

                // Siapkan rantai downsample sementara di RenderGraph
                TextureHandle[] downTargets = new TextureHandle[iterations];
                int[] downWidths = new int[iterations];
                int[] downHeights = new int[iterations];

                int curW = quarterW;
                int curH = quarterH;

                for (int i = 0; i < iterations; i++)
                {
                    downWidths[i] = curW;
                    downHeights[i] = curH;

                    RenderTextureDescriptor stepDesc = desc;
                    stepDesc.width = curW;
                    stepDesc.height = curH;
                    stepDesc.depthStencilFormat = GraphicsFormat.None;
                    stepDesc.msaaSamples = 1;
                    stepDesc.useMipMap = false;

                    downTargets[i] = UniversalRenderer.CreateRenderGraphTexture(
                        renderGraph,
                        stepDesc,
                        $"_UIFrostedDown_{i}",
                        false,
                        FilterMode.Bilinear,
                        TextureWrapMode.Clamp);

                    TextureHandle prev = i == 0 ? sharpHandle : downTargets[i - 1];
                    EnqueueBlitPass(
                        renderGraph,
                        $"UI Frosted Down {i}",
                        m_Material,
                        prev,
                        downTargets[i],
                        PassKawaseDown,
                        curW,
                        curH,
                        spread * (1f + i * 0.35f),
                        i > 0 ? false : false,
                        false);

                    curW = Mathf.Max(4, curW / 2);
                    curH = Mathf.Max(4, curH / 2);
                }

                // Rantai upsample kembali ke quarter-res persistent blurHandle
                if (iterations == 1)
                {
                    EnqueueBlitPass(
                        renderGraph,
                        "UI Frosted Final Upsample",
                        m_Material,
                        downTargets[0],
                        blurHandle,
                        PassKawaseUp,
                        quarterW,
                        quarterH,
                        spread,
                        false,
                        true);
                }
                else
                {
                    TextureHandle current = downTargets[iterations - 1];
                    for (int i = iterations - 2; i >= 0; i--)
                    {
                        bool isFinal = i == 0;
                        TextureHandle target;
                        int targetW = downWidths[i];
                        int targetH = downHeights[i];

                        if (isFinal)
                        {
                            target = blurHandle;
                        }
                        else
                        {
                            RenderTextureDescriptor upDesc = desc;
                            upDesc.width = targetW;
                            upDesc.height = targetH;
                            upDesc.depthStencilFormat = GraphicsFormat.None;
                            upDesc.msaaSamples = 1;
                            upDesc.useMipMap = false;

                            target = UniversalRenderer.CreateRenderGraphTexture(
                                renderGraph,
                                upDesc,
                                $"_UIFrostedUp_{i}",
                                false,
                                FilterMode.Bilinear,
                                TextureWrapMode.Clamp);
                        }

                        EnqueueBlitPass(
                            renderGraph,
                            isFinal ? "UI Frosted Final Upsample" : $"UI Frosted Up {i}",
                            m_Material,
                            current,
                            target,
                            PassKawaseUp,
                            targetW,
                            targetH,
                            spread * (1f + i * 0.25f),
                            false,
                            isFinal);

                        current = target;
                    }
                }

                Shader.SetGlobalTexture(s_SharpTexId, m_SharpHandle);
                Shader.SetGlobalTexture(s_BlurTexId, m_BlurHandle);
                Shader.SetGlobalVector(s_BackgroundReadyId, new Vector4(1f, m_Settings.convertLinearToSrgbInUI ? 1f : 0f, 0f, 0f));
            }
        }
    }
}
