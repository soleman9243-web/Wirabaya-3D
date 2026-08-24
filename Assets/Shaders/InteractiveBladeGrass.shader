Shader "Custom/InteractiveBladeGrass"
{
    Properties
    {
        [Header(Grass Colors)]
        _BaseColor ("Root Dark Color (AO)", Color) = (0.08, 0.22, 0.04, 1.0)
        _MidColor ("Middle Grass Color", Color) = (0.22, 0.58, 0.10, 1.0)
        _TipColor ("Sunlit Tip Color", Color) = (0.62, 0.98, 0.25, 1.0)

        [Header(Wind Dynamics)]
        _WindSpeed ("Wind Speed", Float) = 2.5
        _WindFrequency ("Wind Frequency", Float) = 0.15
        _WindStrength ("Wind Strength", Float) = 0.35

        [Header(Player Trample Reaction)]
        _TrampleStrength ("Trample Push Force", Float) = 1.6
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 200
        Cull Off
        ZWrite On
        ZTest LEqual

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float  heightFrac  : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _MidColor;
                float4 _TipColor;
                float  _WindSpeed;
                float  _WindFrequency;
                float  _WindStrength;
                float  _TrampleStrength;
            CBUFFER_END

            float4 _PlayerPosition; // (x, y, z, radius)

            Varyings vert(Attributes input)
            {
                Varyings output;

                float t = saturate(input.uv.y);
                output.heightFrac = t;

                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);

                // 1. Ombak Angin Dinamis
                float windWave = sin(_Time.y * _WindSpeed + (worldPos.x + worldPos.z) * _WindFrequency);
                float3 windOffset = float3(windWave * _WindStrength * t, 0, cos(_Time.y * _WindSpeed * 0.8 + worldPos.z * _WindFrequency) * _WindStrength * t);
                worldPos += windOffset;

                // 2. Reaksi Injak Kaki Player (MinionsArt)
                if (_PlayerPosition.w > 0.05)
                {
                    float3 playerPos = _PlayerPosition.xyz;
                    float radius = _PlayerPosition.w;
                    float3 diff = worldPos - playerPos;
                    float distXZ = length(diff.xz);

                    if (distXZ < radius && abs(diff.y) < 2.5)
                    {
                        float factor = (1.0 - (distXZ / radius)) * t * _TrampleStrength;
                        factor = factor * factor;

                        float2 pushDir = (distXZ > 0.001) ? normalize(diff.xz) : float2(1.0, 0.0);
                        worldPos.xz += pushDir * (factor * 0.9);
                        worldPos.y  -= factor * 0.8;
                    }
                }

                output.positionWS = worldPos;
                output.positionCS = TransformWorldToHClip(worldPos);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float t = input.heightFrac;

                // Gradasi 3 warna: Akar Gelap -> Tengah Hijau Subur -> Pucuk Lemon Cerah
                half3 col;
                if (t < 0.5)
                {
                    col = lerp(_BaseColor.rgb, _MidColor.rgb, t * 2.0);
                }
                else
                {
                    col = lerp(_MidColor.rgb, _TipColor.rgb, (t - 0.5) * 2.0);
                }

                // Direct Light
                Light mainLight = GetMainLight();
                half NdotL = saturate(dot(float3(0, 1, 0), mainLight.direction));
                half3 lighting = (mainLight.color * (NdotL * 0.4 + 0.6)) + half3(0.20, 0.28, 0.15);
                half3 sunGlow = mainLight.color * (t * 0.25);

                half3 finalColor = col * lighting + sunGlow;
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct AttributesShadow
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct VaryingsShadow
            {
                float4 positionCS   : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _MidColor;
                float4 _TipColor;
                float  _WindSpeed;
                float  _WindFrequency;
                float  _WindStrength;
                float  _TrampleStrength;
            CBUFFER_END

            float4 _PlayerPosition;

            VaryingsShadow vertShadow(AttributesShadow input)
            {
                VaryingsShadow output;
                float t = saturate(input.uv.y);
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                float windWave = sin(_Time.y * _WindSpeed + (worldPos.x + worldPos.z) * _WindFrequency);
                worldPos += float3(windWave * _WindStrength * t, 0, 0);

                if (_PlayerPosition.w > 0.05)
                {
                    float3 playerPos = _PlayerPosition.xyz;
                    float radius = _PlayerPosition.w;
                    float3 diff = worldPos - playerPos;
                    float distXZ = length(diff.xz);

                    if (distXZ < radius && abs(diff.y) < 2.5)
                    {
                        float factor = (1.0 - (distXZ / radius)) * t * _TrampleStrength;
                        float2 pushDir = (distXZ > 0.001) ? normalize(diff.xz) : float2(1.0, 0.0);
                        worldPos.xz += pushDir * (factor * 0.9);
                        worldPos.y  -= factor * 0.8;
                    }
                }

                output.positionCS = TransformWorldToHClip(ApplyShadowBias(worldPos, normalWS, _MainLightPosition.xyz));
                return output;
            }

            half4 fragShadow(VaryingsShadow input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
