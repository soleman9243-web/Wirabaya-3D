Shader "Custom/VideoDissolve"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1,1,1,1)
        _Cutoff ("Dissolve Amount", Range(0, 1.0001)) = 0.0
        _NoiseScale ("Noise Scale", Float) = 20.0
        [HDR] _EdgeColor ("Edge Color", Color) = (1, 0.4, 0, 1)
        _EdgeWidth ("Edge Width", Range(0, 0.1)) = 0.04
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="TransparentCutout" 
            "RenderPipeline"="UniversalPipeline"
            "Queue"="AlphaTest"
        }
        LOD 300

        // PASS 1: Rendering Warna Utama
        Pass
        {
            Name "UniversalForward"
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _Cutoff;
                float _NoiseScale;
                float4 _EdgeColor;
                float _EdgeWidth;
            CBUFFER_END

            float Unity_SimpleNoise_RandomValue_float(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            float Unity_SimpleNoise_float(float2 uv, float noiseScale)
            {
                uv *= noiseScale;
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);

                float a = Unity_SimpleNoise_RandomValue_float(i);
                float b = Unity_SimpleNoise_RandomValue_float(i + float2(1.0, 0.0));
                float c = Unity_SimpleNoise_RandomValue_float(i + float2(0.0, 1.0));
                float d = Unity_SimpleNoise_RandomValue_float(i + float2(1.0, 1.0));

                return a * (1.0 - f.x) * (1.0 - f.y) + b * f.x * (1.0 - f.y) + c * (1.0 - f.x) * f.y + d * f.x * f.y;
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                float noiseVal = Unity_SimpleNoise_float(i.uv, _NoiseScale);
                
                clip(noiseVal - _Cutoff);

                if (noiseVal - _Cutoff < _EdgeWidth && _Cutoff > 0.001)
                {
                    col.rgb += _EdgeColor.rgb;
                }

                return col;
            }
            ENDHLSL
        }

        // PASS 2: Shadow Caster
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { 
                float4 positionOS : POSITION; 
                float2 uv : TEXCOORD0; 
            };
            struct Varyings { 
                float4 positionHCS : SV_POSITION; 
                float2 uv : TEXCOORD0; 
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _Cutoff;
                float _NoiseScale;
                float4 _EdgeColor;
                float _EdgeWidth;
            CBUFFER_END

            float random(float2 uv) { return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453); }
            float noise(float2 uv, float scale) {
                uv *= scale; float2 i = floor(uv); float2 f = frac(uv); f = f*f*(3.0-2.0*f);
                return (random(i)*(1.0-f.x)*(1.0-f.y) + random(i+float2(1.0,0.0))*f.x*(1.0-f.y) + random(i+float2(0.0,1.0))*(1.0-f.x)*f.y + random(i+float2(1.0,1.0))*f.x*f.y);
            }

            Varyings vert(Attributes v) {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
                return o;
            }

            half4 frag(Varyings i) : SV_Target {
                clip(noise(i.uv, _NoiseScale) - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        // PASS 3: Depth Only
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask 0 Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { 
                float4 positionOS : POSITION; 
                float2 uv : TEXCOORD0; 
            };
            struct Varyings { 
                float4 positionHCS : SV_POSITION; 
                float2 uv : TEXCOORD0; 
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _Cutoff;
                float _NoiseScale;
                float4 _EdgeColor;
                float _EdgeWidth;
            CBUFFER_END

            float random(float2 uv) { return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453); }
            float noise(float2 uv, float scale) {
                uv *= scale; float2 i = floor(uv); float2 f = frac(uv); f = f*f*(3.0-2.0*f);
                return (random(i)*(1.0-f.x)*(1.0-f.y) + random(i+float2(1.0,0.0))*f.x*(1.0-f.y) + random(i+float2(0.0,1.0))*(1.0-f.x)*f.y + random(i+float2(1.0,1.0))*f.x*f.y);
            }

            Varyings vert(Attributes v) {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
                return o;
            }

            half4 frag(Varyings i) : SV_Target {
                clip(noise(i.uv, _NoiseScale) - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }
}
