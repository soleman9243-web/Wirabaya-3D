Shader "FantasyKingdom/StylizedVegetation_Mesh"
{
    // =====================================================================
    // Stylized Interactive Vegetation Shader
    // Struktur, gerakan angin, dan interaksi sama persis dengan Grass Shader,
    // Dilengkapi pelindung agar rebahan tanaman tidak tembus terrain (anti clipping).
    // Menghilangkan:
    //  - Near/Far Distance Color Blend
    //  - Bottom Height Blend
    //  - Trample Recovery Color
    //  - Tekstur Wind Pattern (sekarang murni prosedural)
    // =====================================================================

    Properties
    {
        [Header(Texture)]
        _BaseMap ("Main Texture (Albedo)", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0.0, 0.95)) = 0.35

        [Header(Wind)]
        _WindSpeed ("Wind Speed", Float) = 1.0
        _WindIntensity ("Wind Intensity", Range(0.0, 2.0)) = 0.25
        _WindDirX ("Wind Direction X", Float) = 1.0
        _WindDirZ ("Wind Direction Z", Float) = 0.5
        _WindTiling ("Wind Tiling", Float) = 0.08

        [Header(Grass Mesh)]
        _GrassHeight ("Grass Height", Float) = 1.0

        [Header(Player Interaction)]
        _BendStrength ("Bend Strength", Range(0.0, 2.0)) = 0.8
        _Smoothness ("Smoothness", Range(0.0, 1.0)) = 0.0

        [Header(Trample Grass Recovery)]
        _RecoveryTime ("Recovery Duration (Detik)", Range(0.5, 20.0)) = 4.0
        _TrampleBendAmount ("Trample Bend Amount", Range(0.0, 2.0)) = 1.0

        [Header(Adjustable Emission Glow)]
        [HDR] _EmissionColor ("Emission Color", Color) = (0, 0, 0, 1)
        _EmissionIntensity ("Emission Intensity", Range(0.0, 10.0)) = 1.0
        _EmissionTipBoost ("Emission on Tips Only", Range(0.0, 1.0)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 200
        Cull Off

        // =================================================================
        // PASS 1: UniversalForward (Main Lit Pass)
        // =================================================================
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex VegetationVert
            #pragma fragment VegetationFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "StylizedVegetation_Common.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 positionWS   : TEXCOORD1;
                float  heightFactor : TEXCOORD2;
                float3 normalWS     : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings VegetationVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float heightFactor = max(saturate(input.color.a), saturate(input.uv.y));
                output.heightFactor = heightFactor;

                float3 originalPosWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 rootWS = TransformObjectToWorld(float3(0.0, 0.0, 0.0));
                float3 posWS = ApplyGrassDisplacement(originalPosWS, heightFactor, rootWS);

                output.positionWS = posWS;
                output.positionCS = TransformWorldToHClip(posWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                // Normal dunia yang merespon pembengkokan rumput/tanaman oleh angin dan injakan kaki
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 disp = posWS - originalPosWS;
                normalWS = normalize(normalWS + float3(disp.x, -abs(disp.y) * 0.5, disp.z) * 1.5);
                output.normalWS = normalWS;

                return output;
            }

            half4 VegetationFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 texCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                clip(texCol.a - _Cutoff);

                // Albedo murni tekstur
                half3 albedo = texCol.rgb;

                // ── Soft Stylized Lighting & Shading (Persis Grass Shader) ──
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord, input.positionWS, half4(1, 1, 1, 1));
                half shadowAtten = mainLight.shadowAttenuation;

                // Remap shadow agar bayangan karakter tetap terlihat jelas namun sangat halus (soft shadow)
                half softShadow = lerp(0.72, 1.0, shadowAtten);

                float3 N = NormalizeNormalPerPixel(input.normalWS);
                N = normalize(lerp(float3(0.0, 1.0, 0.0), N, 0.60));

                float3 V = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float3 L = mainLight.direction;
                float3 H = normalize(L + V);

                // Diffuse dinamis yang berubah saat rumput/tanaman meliuk ditiup angin
                half NdotL = saturate(dot(N, L) * 0.35 + 0.65);

                // Specular sheen halus saat helai meliuk memantulkan cahaya matahari
                float NdotH = saturate(dot(N, H));
                float specular = pow(NdotH, 16.0) * (input.heightFactor * 0.35);

                half3 direct = mainLight.color * ((NdotL * softShadow) + specular);
                half3 ambient = half3(0.32, 0.38, 0.28);
                half3 litColor = albedo * (direct + ambient);

                // ── Adjustable Emission Glow ──
                float tipFactor = lerp(1.0, input.heightFactor, _EmissionTipBoost);
                half3 emission = _EmissionColor.rgb * (_EmissionIntensity * tipFactor);
                litColor += emission;

                return half4(litColor, 1.0);
            }
            ENDHLSL
        }

        // =================================================================
        // PASS 2: DepthOnly (Mandatory for Depth Priming Mode)
        // =================================================================
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ZTest LEqual
            ColorMask R
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthOnlyVert
            #pragma fragment DepthOnlyFrag
            #pragma multi_compile_instancing

            #include "StylizedVegetation_Common.hlsl"

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            DepthVaryings DepthOnlyVert(DepthAttributes input)
            {
                DepthVaryings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float heightFactor = max(saturate(input.color.a), saturate(input.uv.y));
                float3 originalPosWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 rootWS = TransformObjectToWorld(float3(0.0, 0.0, 0.0));
                float3 posWS = ApplyGrassDisplacement(originalPosWS, heightFactor, rootWS);

                output.positionCS = TransformWorldToHClip(posWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                return output;
            }

            half DepthOnlyFrag(DepthVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 texCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                clip(texCol.a - _Cutoff);

                return input.positionCS.z;
            }
            ENDHLSL
        }

        // =================================================================
        // PASS 3: DepthNormals (Mandatory for SSAO)
        // =================================================================
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag
            #pragma multi_compile_instancing

            #include "StylizedVegetation_Common.hlsl"

            struct DepthNormalsAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthNormalsVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            DepthNormalsVaryings DepthNormalsVert(DepthNormalsAttributes input)
            {
                DepthNormalsVaryings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float heightFactor = max(saturate(input.color.a), saturate(input.uv.y));
                float3 originalPosWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 rootWS = TransformObjectToWorld(float3(0.0, 0.0, 0.0));
                float3 posWS = ApplyGrassDisplacement(originalPosWS, heightFactor, rootWS);

                output.positionCS = TransformWorldToHClip(posWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);

                return output;
            }

            void DepthNormalsFrag(DepthNormalsVaryings input, out half4 outNormalWS : SV_Target0)
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 texCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                clip(texCol.a - _Cutoff);

                outNormalWS = half4(NormalizeNormalPerPixel(input.normalWS), 0.0);
            }
            ENDHLSL
        }

        // =================================================================
        // PASS 4: ShadowCaster (Shadow Mapping)
        // =================================================================
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing

            #include "StylizedVegetation_Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float3 _LightDirection;

            ShadowVaryings ShadowVert(ShadowAttributes input)
            {
                ShadowVaryings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float heightFactor = max(saturate(input.color.a), saturate(input.uv.y));
                float3 originalPosWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 rootWS = TransformObjectToWorld(float3(0.0, 0.0, 0.0));
                float3 posWS = ApplyGrassDisplacement(originalPosWS, heightFactor, rootWS);

                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(posWS, normalWS, _LightDirection));
                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                return output;
            }

            half4 ShadowFrag(ShadowVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 texCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                clip(texCol.a - _Cutoff);

                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
