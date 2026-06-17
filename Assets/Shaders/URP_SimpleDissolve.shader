Shader "Custom/URP_SimpleDissolve"
{
    Properties
    {
        _BaseMap("Base Map (RGB)", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _DissolveAmount("Dissolve Amount", Range(0.0, 1.0)) = 0.0
        _NoiseScale("Noise Scale", Float) = 20.0
        [HDR] _EdgeColor("Edge Glow Color", Color) = (1, 0.2, 0, 1)
        _EdgeWidth("Edge Glow Width", Range(0.0, 0.3)) = 0.05
    }
    SubShader
    {
        Tags 
        { 
            "RenderType" = "TransparentCutout" 
            "RenderPipeline" = "UniversalPipeline" 
            "Queue" = "AlphaTest" 
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _DissolveAmount;
                float _NoiseScale;
                float4 _EdgeColor;
                float _EdgeWidth;
            CBUFFER_END

            // Fungsi Pseudo-Random untuk membuat Noise
            float random(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453123);
            }

            float valueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                
                // Four corners
                float a = random(i);
                float b = random(i + float2(1.0, 0.0));
                float c = random(i + float2(0.0, 1.0));
                float d = random(i + float2(1.0, 1.0));

                // Smooth Interpolation
                float2 u = f * f * (3.0 - 2.0 * f);

                return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. Dapatkan warna dari Tekstur utama
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                
                // 2. Buat procedural noise berdasarkan UV dan _NoiseScale
                float noiseVal = valueNoise(input.uv * _NoiseScale);
                
                // 3. Logika Disintegrate/Dissolve
                float clipVal = noiseVal - _DissolveAmount;
                clip(clipVal); // pixel dibuang jika nilainya kurang dari 0

                // Simple shading (Blinn-Phong dasar agar tidak terlalu flat)
                Light mainLight = GetMainLight();
                half NdotL = saturate(dot(normalize(input.normalWS), mainLight.direction));
                color.rgb *= (NdotL * mainLight.color + 0.3); // + 0.3 for ambient

                // 4. Edge Glow (Garis warna di pinggiran yang hancur)
                if (clipVal < _EdgeWidth && _DissolveAmount > 0.0)
                {
                    // Beri warna glow yang menyala
                    color.rgb = _EdgeColor.rgb;
                }

                return color;
            }
            ENDHLSL
        }
        
        // Pass ShadowCaster agar bayangan ikut menghilang saat dissolve
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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

            CBUFFER_START(UnityPerMaterial)
                float _DissolveAmount;
                float _NoiseScale;
            CBUFFER_END

            float random(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453123);
            }

            float valueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                float a = random(i);
                float b = random(i + float2(1.0, 0.0));
                float c = random(i + float2(0.0, 1.0));
                float d = random(i + float2(1.0, 1.0));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float noiseVal = valueNoise(input.uv * _NoiseScale);
                clip(noiseVal - _DissolveAmount);
                return 0;
            }
            ENDHLSL
        }
    }
}
