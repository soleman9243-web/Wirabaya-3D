Shader "Wirabaya/UI/FrostedGlass"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

        [Header(Frosted Glass)]
        _BlurAmount ("Kekaburan (Blur Amount)", Range(0, 1)) = 0.75
        _BlurSpread ("Sebaran Tambahan (Extra Blur)", Range(0, 12)) = 2.5
        _TintStrength ("Kepekatan Warna Tint", Range(0, 1)) = 0.55
        _BackgroundBrightness ("Kecerahan Latar", Range(0.2, 1.5)) = 0.85
        _BackgroundSaturation ("Saturasi Latar", Range(0, 1.5)) = 0.85
        _FrostGrain ("Tekstur Kaca (Frost Grain)", Range(0, 0.03)) = 0.004
        [Toggle] _UseVertexAlphaAsTint ("Gunakan Alpha Image sebagai Tint", Float) = 0
        [Toggle] _FlipBackgroundY ("Flip Latar Vertikal", Float) = 0

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [HideInInspector] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "UIFrostedGlass"

            HLSLPROGRAM
            #pragma vertex VertUI
            #pragma fragment FragUI

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color       : COLOR;
                float2 uv          : TEXCOORD0;
                float4 worldPos    : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D_X(_WirabayaUISharpTex);
            TEXTURE2D_X(_WirabayaUIBlurTex);
            float4 _WirabayaUIBackgroundReady; // x = 1 if capture ran this frame

            float4 _Color;
            float4 _MainTex_ST;
            float4 _TextureSampleAdd;
            float4 _ClipRect;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;

            float _BlurAmount;
            float _BlurSpread;
            float _TintStrength;
            float _BackgroundBrightness;
            float _BackgroundSaturation;
            float _FrostGrain;
            float _UseVertexAlphaAsTint;
            float _FlipBackgroundY;

            static const float2 kPoisson8[8] =
            {
                float2(-0.7071,  0.7071),
                float2( 0.7071,  0.7071),
                float2(-0.7071, -0.7071),
                float2( 0.7071, -0.7071),
                float2( 0.0000,  1.0000),
                float2( 0.0000, -1.0000),
                float2(-1.0000,  0.0000),
                float2( 1.0000,  0.0000)
            };

            float2 Hash22(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.xx + p3.yz) * p3.zy) - 0.5;
            }

            half ComputeRectMaskAlpha(float2 posXY)
            {
                #ifdef UNITY_UI_CLIP_RECT
                float2 softness = float2(_UIMaskSoftnessX, _UIMaskSoftnessY);
                float2 inside = min(posXY - _ClipRect.xy, _ClipRect.zw - posXY);
                float2 clamped = saturate(inside / max(softness, float2(0.0001, 0.0001)));
                return (half)(clamped.x * clamped.y);
                #else
                return 1.0h;
                #endif
            }

            Varyings VertUI(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.worldPos = input.positionOS;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color;
                return output;
            }

            half4 FragUI(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 spriteSample = (SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) + _TextureSampleAdd);
                half4 uiTint = spriteSample * input.color;

                half maskAlpha = ComputeRectMaskAlpha(input.worldPos.xy);
                half spriteMask = spriteSample.a * maskAlpha;

                #ifdef UNITY_UI_ALPHACLIP
                clip(uiTint.a * maskAlpha - 0.001h);
                #endif

                float2 screenUV = input.positionHCS.xy / _ScreenParams.xy;
                if (_FlipBackgroundY > 0.5)
                {
                    screenUV.y = 1.0 - screenUV.y;
                }

                // Micro refraksi noise untuk tekstur kaca buram
                if (_FrostGrain > 0.00001 && _BlurAmount > 0.001)
                {
                    float2 noiseOffset = Hash22(floor(input.positionHCS.xy)) * (_FrostGrain * saturate(_BlurAmount));
                    screenUV = saturate(screenUV + noiseOffset);
                }

                half3 bgColor = uiTint.rgb;

                if (_WirabayaUIBackgroundReady.x > 0.5)
                {
                    float blurT = saturate(_BlurAmount);
                    float2 texel = rcp(_ScreenParams.xy);

                    // Sampel tajam (1/2 res) & sampel buram Kawase (1/4 res)
                    half3 sharpCol = SAMPLE_TEXTURE2D_X(_WirabayaUISharpTex, sampler_LinearClamp, screenUV).rgb;

                    // Extra Poisson scatter agar tingkat blur bisa diatur terus hingga sangat pekat
                    float scatterRadius = lerp(0.0, _BlurSpread * 2.5, saturate((blurT - 0.15) / 0.85));
                    half3 blurAccum = SAMPLE_TEXTURE2D_X(_WirabayaUIBlurTex, sampler_LinearClamp, screenUV).rgb;

                    if (scatterRadius > 0.05)
                    {
                        float2 rotNoise = Hash22(input.positionHCS.xy * 0.5);
                        float angle = rotNoise.x * 6.2831853;
                        float s = sin(angle);
                        float c = cos(angle);

                        [unroll]
                        for (int k = 0; k < 8; k++)
                        {
                            float2 dir = kPoisson8[k];
                            float2 rotated = float2(dir.x * c - dir.y * s, dir.x * s + dir.y * c);
                            float2 sampleUV = saturate(screenUV + rotated * texel * scatterRadius);
                            blurAccum += SAMPLE_TEXTURE2D_X(_WirabayaUIBlurTex, sampler_LinearClamp, sampleUV).rgb;
                        }
                        blurAccum /= 9.0h;
                    }

                    // Jika blur kecil (0..0.35), haluskan transisi dari sharp ke Kawase dengan sedikit tap di sharp
                    if (blurT < 0.35)
                    {
                        float microRadius = blurT * 4.5;
                        half3 microSharp = sharpCol;
                        if (microRadius > 0.05)
                        {
                            [unroll]
                            for (int j = 0; j < 4; j++)
                            {
                                float2 sampleUV = saturate(screenUV + kPoisson8[j] * texel * microRadius);
                                microSharp += SAMPLE_TEXTURE2D_X(_WirabayaUISharpTex, sampler_LinearClamp, sampleUV).rgb;
                            }
                            microSharp *= 0.2h;
                        }
                        bgColor = lerp(microSharp, blurAccum, saturate(blurT / 0.35));
                    }
                    else
                    {
                        bgColor = blurAccum;
                    }

                    // Atur saturasi & kecerahan latar di balik kaca
                    half lum = Luminance(bgColor);
                    bgColor = lerp(half3(lum, lum, lum), bgColor, (half)_BackgroundSaturation);
                    bgColor *= (half)_BackgroundBrightness;

                    #if !UNITY_COLORSPACE_GAMMA
                    if (_WirabayaUIBackgroundReady.y > 0.5)
                    {
                        bgColor = LinearToSRGB(max(bgColor, 0.0h));
                    }
                    #endif
                }

                half panelTintWeight = (_UseVertexAlphaAsTint > 0.5) ? uiTint.a : (half)_TintStrength;
                half3 finalRGB = lerp(bgColor, uiTint.rgb, saturate(panelTintWeight));
                half finalAlpha = (_UseVertexAlphaAsTint > 0.5) ? spriteMask : (spriteMask * input.color.a);

                return half4(finalRGB, finalAlpha);
            }
            ENDHLSL
        }
    }

    Fallback "UI/Default"
}
