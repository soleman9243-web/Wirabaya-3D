Shader "FantasyKingdom/GenshinGrassFoliage"
{
    Properties
    {
        [Header(Base Textures and Color)]
        [MainTexture] _BaseMap("Base Map (RGB) + Alpha (A)", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color Tint", Color) = (1, 1, 1, 1)
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Range(0, 2)) = 1.0

        [Header(Stylized Shading)]
        _Cutoff("Alpha Cutoff", Range(0.01, 1.0)) = 0.45
        _NormalUpwardBias("Normal Upward Bias (Fluffy Anime Look)", Range(0, 1)) = 0.5
        _Smoothness("Smoothness", Range(0, 1)) = 0.15
        _Metallic("Metallic", Range(0, 1)) = 0.0

        [Header(Genshin and WuWa GPU Wind System)]
        _WindStrength("Wind Strength (Kekuatan Goyangan)", Range(0, 1.5)) = 0.35
        _WindSpeed("Wind Speed (Kecepatan Ombak)", Range(0.1, 8.0)) = 2.2
        _WindWaveFrequency("Wave Frequency (Kerapatan Ombak)", Range(0.01, 1.0)) = 0.18
        _WindDirection("Wind Direction (Arah Angin XZ)", Vector) = (0.7, 0.7, 0, 0)
        _WindBending("Downward Bend on Wind", Range(0, 1)) = 0.25
        _MicroFlutter("Micro Flutter (Getaran Halus Ujung)", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "TransparentCutout" 
            "Queue" = "AlphaTest" 
            "RenderPipeline" = "UniversalPipeline" 
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }

        LOD 300
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _BumpScale;
            half _Cutoff;
            half _NormalUpwardBias;
            half _Smoothness;
            half _Metallic;

            // Wind Parameters
            half _WindStrength;
            half _WindSpeed;
            half _WindWaveFrequency;
            float4 _WindDirection;
            half _WindBending;
            half _MicroFlutter;
        CBUFFER_END

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        TEXTURE2D(_BumpMap);
        SAMPLER(sampler_BumpMap);

        // Core GPU Vertex Wind function
        float3 ApplyGenshinWind(float3 positionOS, float2 uv, float3 positionWS)
        {
            // Root is pinned at UV.y = 0, tips sway more at UV.y = 1
            // Use quadratic curve for natural stem bending
            float heightFactor = saturate(uv.y);
            float bendWeight = heightFactor * heightFactor;

            // Normalize wind direction
            float2 dir2D = normalize(_WindDirection.xy + float2(0.001, 0.001));
            float3 windDirWS = normalize(float3(dir2D.x, -_WindBending * 0.5, dir2D.y));

            // Travelling large wave (Genshin field wave)
            float wavePhase = dot(positionWS.xz, dir2D * _WindWaveFrequency) + _Time.y * _WindSpeed;
            float mainWave = sin(wavePhase);

            // Gust / harmonic wave (cross ripple)
            float gustPhase = dot(positionWS.xz, float2(dir2D.y, -dir2D.x) * _WindWaveFrequency * 0.6) + _Time.y * (_WindSpeed * 1.35);
            float gustWave = sin(gustPhase) * 0.4;

            // High frequency micro flutter (leaves trembling in the wind)
            float flutterPhase = dot(positionWS.xz, float2(1.7, 2.3)) + _Time.y * (_WindSpeed * 3.8);
            float microWave = sin(flutterPhase) * (0.25 * _MicroFlutter);

            float totalDisplacement = (mainWave + gustWave + microWave) * _WindStrength * bendWeight;

            // Convert world displacement back to object space
            float3 worldOffset = windDirWS * totalDisplacement;
            float3 objectOffset = TransformWorldToObjectDir(worldOffset);

            return positionOS + objectOffset;
        }
        ENDHLSL

        // ------------------------------------------------------------------
        // Forward Lit Pass
        // ------------------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vertex
            #pragma fragment Fragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                // Initial world position
                float3 initialWS = TransformObjectToWorld(input.positionOS.xyz);

                // Apply Genshin Wind Displacement
                float3 animatedOS = ApplyGenshinWind(input.positionOS.xyz, input.uv, initialWS);

                output.positionWS = TransformObjectToWorld(animatedOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);

                // Calculate normal in world space with upward bias for soft anime fluffiness
                float3 nWS = TransformObjectToWorldNormal(input.normalOS);
                output.normalWS = normalize(lerp(nWS, float3(0, 1, 0), _NormalUpwardBias));

                return output;
            }

            half4 Fragment(Varyings input, half facing : VFACE) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                clip(albedoAlpha.a - _Cutoff);

                // Double-sided normal correction
                float3 normalWS = normalize(input.normalWS);
                normalWS = facing > 0 ? normalWS : -normalWS;

                // Lighting
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half NdotL = saturate(dot(normalWS, mainLight.direction));
                // Stylized half-lambert wrap
                half halfLambert = NdotL * 0.5 + 0.5;

                half3 ambient = SampleSH(normalWS) * 0.7;
                half3 directLight = mainLight.color * (halfLambert * mainLight.shadowAttenuation);
                half3 finalColor = albedoAlpha.rgb * (directLight + ambient);

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        // Shadow Caster Pass
        // ------------------------------------------------------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float3 _LightDirection;

            Varyings Vertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                float3 initialWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 animatedOS = ApplyGenshinWind(input.positionOS.xyz, input.uv, initialWS);
                float3 positionWS = TransformObjectToWorld(animatedOS);

                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, TransformObjectToWorldNormal(input.normalOS), _LightDirection));
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                clip(albedoAlpha.a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        // Depth Only Pass
        // ------------------------------------------------------------------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                float3 initialWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 animatedOS = ApplyGenshinWind(input.positionOS.xyz, input.uv, initialWS);
                output.positionCS = TransformWorldToHClip(TransformObjectToWorld(animatedOS));

                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                clip(albedoAlpha.a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
