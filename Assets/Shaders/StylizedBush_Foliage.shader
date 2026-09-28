Shader "FantasyKingdom/StylizedBush_Foliage"
{
    Properties
    {
        [Header(Base Texture)]
        _BaseMap ("Bush Leaf Texture (Albedo)", 2D) = "white" {}
        _BaseColor ("Base Color Tint", Color) = (1, 1, 1, 1)
        _Cutoff ("Alpha Cutoff", Range(0.01, 0.95)) = 0.45

        [Header(Painterly Grass Palette Matching)]
        _PaletteBlend ("Grass Palette Harmony (Blend)", Range(0.0, 1.0)) = 0.85
        _TopColor ("Top Sunlit Color (Serasi Rumput Cerah)", Color) = (0.55, 0.88, 0.24, 1.0)
        _MidColor ("Mid Foliage Color (Serasi Badan Rumput)", Color) = (0.28, 0.65, 0.18, 1.0)
        _DarkColor ("Dark Under Color (Serasi Pangkal Rumput)", Color) = (0.067, 0.42, 0.22, 1.0)
        _LumaMin ("Texture Luma Min", Range(0.0, 0.5)) = 0.10
        _LumaMax ("Texture Luma Max", Range(0.3, 1.0)) = 0.52

        [Header(Stylized Cel Lighting)]
        _ShadowColor ("Shadow Ambient Tint (Luminous Anime)", Color) = (0.65, 0.78, 0.68, 1.0)
        _SunlightColor ("Sunlight Tint", Color) = (1.12, 1.15, 1.00, 1.0)
        _ToonThreshold ("Cel Banding Threshold", Range(-0.5, 0.5)) = 0.02
        _ToonSmoothness ("Cel Banding Softness", Range(0.01, 0.5)) = 0.15

        [Header(Volume and Normal Smoothing)]
        _SphericalNormalBlend ("Spherical Center Normal Blend", Range(0.0, 1.0)) = 0.70
        _UpNormalBlend ("Upward Sky Normal Blend", Range(0.0, 1.0)) = 0.35
        _CenterYOffset ("Normal Center Y (cm)", Float) = 100.0
        _BushBaseY ("Bush Base Y (cm)", Float) = -75.0
        _BushTopY ("Bush Top Y (cm)", Float) = 280.0

        [Header(Subsurface Translucency SSS)]
        [HDR] _SSSColor ("Backlight SSS Glow Color", Color) = (0.50, 0.90, 0.30, 1.0)
        _SSSStrength ("SSS Intensity", Range(0.0, 2.0)) = 0.60
        _SSSPower ("SSS Sharpness", Range(1.0, 10.0)) = 3.5

        [Header(Rim Light)]
        [HDR] _RimColor ("Rim Light Color", Color) = (1.0, 1.0, 0.8, 1.0)
        _RimStrength ("Rim Light Intensity", Range(0.0, 2.0)) = 0.25

        [Header(Wind Foliage Animation)]
        _WindSpeed ("Wind Speed", Float) = 1.0
        _WindIntensity ("Wind Intensity", Range(0.0, 2.0)) = 0.22
        _WindDirX ("Wind Direction X", Float) = 1.0
        _WindDirZ ("Wind Direction Z", Float) = 0.5
        _WindTiling ("Wind Tiling", Float) = 0.08
        _FlutterSpeed ("Leaf Micro-Flutter Speed", Float) = 3.5
        _FlutterAmount ("Leaf Micro-Flutter Amount", Range(0.0, 0.2)) = 0.035

        [Header(Player Touch Interaction)]
        _PlayerPushStrength ("Player Push Strength", Range(0.0, 2.0)) = 0.4
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

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _BaseColor;
            float4 _TopColor;
            float4 _MidColor;
            float4 _DarkColor;
            float4 _ShadowColor;
            float4 _SunlightColor;
            float4 _SSSColor;
            float4 _RimColor;

            float  _Cutoff;
            float  _PaletteBlend;
            float  _LumaMin;
            float  _LumaMax;

            float  _ToonThreshold;
            float  _ToonSmoothness;
            float  _SphericalNormalBlend;
            float  _UpNormalBlend;

            float  _CenterYOffset;
            float  _BushBaseY;
            float  _BushTopY;
            float  _SSSStrength;

            float  _SSSPower;
            float  _RimStrength;
            float  _WindSpeed;
            float  _WindIntensity;

            float  _WindDirX;
            float  _WindDirZ;
            float  _WindTiling;
            float  _FlutterSpeed;

            float  _FlutterAmount;
            float  _PlayerPushStrength;
            float2 _PadMaterial;
        CBUFFER_END

        float4 _PlayerPosition;
        float4 _PlayerTramplePos;

        // Perhitungan deformasi angin dan interaksi player pada bush foliage
        float3 ApplyBushFoliageDisplacement(float3 posOS, float3 posWS, float heightFactor)
        {
            float windMask = heightFactor * heightFactor;

            // 1. Sapuan angin makro (selaras dengan gelombang rumput di terrain)
            if (_WindIntensity > 0.001)
            {
                float2 rawDir = float2(_WindDirX, _WindDirZ);
                float dirLen = length(rawDir);
                float2 windDir = (dirLen > 0.001) ? (rawDir / dirLen) : float2(1.0, 0.0);
                float2 perpDir = float2(-windDir.y, windDir.x);

                float speed = _WindSpeed * 0.50;
                float tiling = max(_WindTiling, 0.005);

                float2 waveUV = posWS.xz * (tiling * 0.8) - windDir * (_Time.y * speed);
                float wave = sin(waveUV.x + waveUV.y) * 0.5 + 0.5;
                float push = smoothstep(0.2, 0.8, wave) * (_WindIntensity * 0.25) * windMask;

                // Micro flutter daun individual (skala centimeter posOS disesuaikan)
                float flutter = sin(_Time.y * _FlutterSpeed + posOS.x * 0.05 + posOS.z * 0.05) * (_FlutterAmount * windMask);

                posWS.xz += windDir * push + perpDir * flutter;
                posWS.y  -= push * 0.15;
            }

            // 2. Interaksi senggolan pemain
            float4 pPos = (_PlayerTramplePos.w > 0.05) ? _PlayerTramplePos : _PlayerPosition;
            if (pPos.w > 0.05 && _PlayerPushStrength > 0.01)
            {
                float3 diff = posWS - pPos.xyz;
                float distXZ = length(diff.xz);
                if (distXZ < pPos.w && abs(diff.y) < 2.0)
                {
                    float pushFactor = (1.0 - (distXZ / pPos.w)) * heightFactor * _PlayerPushStrength;
                    float2 pushDir = (distXZ > 0.01) ? normalize(diff.xz) : float2(0.0, 1.0);
                    posWS.xz += pushDir * (pushFactor * 0.5);
                }
            }

            return posWS;
        }

        // Normal volumetrik sferis terpusat pada kanopi bush (Y = 100 cm)
        float3 CalculateBushNormalWS(float3 posOS, float3 rawNormalOS, float3 posWS)
        {
            float3 centerOS = float3(0.0, _CenterYOffset, 0.0);
            float3 centerWS = TransformObjectToWorld(centerOS);
            float3 sphericalNormalWS = normalize(posWS - centerWS);

            float3 rawNormalWS = TransformObjectToWorldNormal(rawNormalOS);
            float3 normalWS = normalize(lerp(rawNormalWS, sphericalNormalWS, _SphericalNormalBlend));
            normalWS = normalize(lerp(normalWS, float3(0.0, 1.0, 0.0), _UpNormalBlend));
            return normalWS;
        }
        ENDHLSL

        // =================================================================
        // PASS 1: UniversalForward (Main Lit Cel-Shaded Pass)
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
            #pragma vertex ForwardVert
            #pragma fragment ForwardFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
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

            Varyings ForwardVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float heightFactor = saturate((input.positionOS.y - _BushBaseY) / max(_BushTopY - _BushBaseY, 1.0));
                output.heightFactor = heightFactor;

                float3 originalPosWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 displacedPosWS = ApplyBushFoliageDisplacement(input.positionOS.xyz, originalPosWS, heightFactor);

                output.positionWS = displacedPosWS;
                output.positionCS = TransformWorldToHClip(displacedPosWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = CalculateBushNormalWS(input.positionOS.xyz, input.normalOS, displacedPosWS);

                return output;
            }

            half4 ForwardFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 texCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                clip(texCol.a - _Cutoff);

                // 1. Ekstrak tonal luma asli pelukis (100% goresan kuas, kontur, & gumpalan daun terjaga)
                half rawLuma = dot(texCol.rgb, half3(0.299, 0.587, 0.114));
                half normLuma = saturate((rawLuma - _LumaMin) / max(_LumaMax - _LumaMin, 0.01));

                // 2. Petakan ke palet warna rumput (Spring Lime -> Grass Green -> Deep Emerald)
                half3 mappedCol;
                if (normLuma < 0.5)
                {
                    mappedCol = lerp(_DarkColor.rgb, _MidColor.rgb, normLuma * 2.0);
                }
                else
                {
                    mappedCol = lerp(_MidColor.rgb, _TopColor.rgb, (normLuma - 0.5) * 2.0);
                }

                // 3. Gradasi halus kanopi atas terkena sinar matahari
                mappedCol = lerp(mappedCol, mappedCol * 1.15, input.heightFactor * 0.35);

                // 4. Blend harmonis warna rumput dengan tekstur asli
                half3 albedo = lerp(texCol.rgb * _BaseColor.rgb, mappedCol, _PaletteBlend);

                // URP Main Light & Soft Shadow Attenuation
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord, input.positionWS, half4(1, 1, 1, 1));
                half shadowAtten = mainLight.shadowAttenuation;
                half shadowCel = smoothstep(0.15, 0.40, shadowAtten);

                float3 N = NormalizeNormalPerPixel(input.normalWS);
                float3 V = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float3 L = mainLight.direction;

                // 2-Tone Crisp Anime Cel Banding (Serasi dengan rumput anime di sekitarnya)
                half rawNdotL = dot(N, L);
                half toonBand = smoothstep(_ToonThreshold - _ToonSmoothness, _ToonThreshold + _ToonSmoothness, rawNdotL) * shadowCel;

                // Sisi bayangan: segar, terang, luminous (sama dengan StylizedGrass_Mesh: 65% - 78% brightness)
                half3 shadowAmbient = albedo * _ShadowColor.rgb;
                // Sisi terang: hangat tersorot sinar mentari
                half3 litSunlight = albedo * mainLight.color * _SunlightColor.rgb;
                half3 diffuseLight = lerp(shadowAmbient, litSunlight, toonBand);

                // Subsurface Scattering (SSS / Efek daun tembus cahaya saat membelakangi matahari)
                float3 backLightDir = normalize(L + N * 0.35);
                half backDot = saturate(dot(-V, backLightDir));
                half sss = pow(backDot, _SSSPower) * _SSSStrength * input.heightFactor;
                half3 sssColor = _SSSColor.rgb * sss * mainLight.color;

                // Fresnel Rim halus di siluet tepi dedaunan
                half fresnel = pow(1.0 - saturate(dot(N, V)), 3.5);
                half3 rimLight = mainLight.color * _RimColor.rgb * (fresnel * _RimStrength * input.heightFactor);

                // Ambient Spherical Harmonics untuk beradaptasi dengan pencahayaan langit & scene
                half3 ambientSH = SampleSH(N) * albedo * 0.25;

                half3 finalColor = diffuseLight + sssColor + rimLight + ambientSH;
                return half4(finalColor, 1.0);
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
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            DepthVaryings DepthVert(DepthAttributes input)
            {
                DepthVaryings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float heightFactor = saturate((input.positionOS.y - _BushBaseY) / max(_BushTopY - _BushBaseY, 1.0));
                float3 originalPosWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 displacedPosWS = ApplyBushFoliageDisplacement(input.positionOS.xyz, originalPosWS, heightFactor);

                output.positionCS = TransformWorldToHClip(displacedPosWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half DepthFrag(DepthVaryings input) : SV_Target
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

            struct DepthNormalsAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
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

                float heightFactor = saturate((input.positionOS.y - _BushBaseY) / max(_BushTopY - _BushBaseY, 1.0));
                float3 originalPosWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 displacedPosWS = ApplyBushFoliageDisplacement(input.positionOS.xyz, originalPosWS, heightFactor);

                output.positionCS = TransformWorldToHClip(displacedPosWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = CalculateBushNormalWS(input.positionOS.xyz, input.normalOS, displacedPosWS);
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
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

                float heightFactor = saturate((input.positionOS.y - _BushBaseY) / max(_BushTopY - _BushBaseY, 1.0));
                float3 originalPosWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 displacedPosWS = ApplyBushFoliageDisplacement(input.positionOS.xyz, originalPosWS, heightFactor);

                float3 normalWS = CalculateBushNormalWS(input.positionOS.xyz, input.normalOS, displacedPosWS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(displacedPosWS, normalWS, _LightDirection));

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
