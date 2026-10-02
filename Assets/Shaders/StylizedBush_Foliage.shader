Shader "FantasyKingdom/StylizedBush_Foliage"
{
    Properties
    {
        [Header(Base Texture)]
        _BaseMap ("Bush Leaf Texture (Albedo)", 2D) = "white" {}
        _BaseColor ("Base Color Tint", Color) = (1, 1, 1, 1)
        _Cutoff ("Alpha Cutoff", Range(0.01, 0.95)) = 0.45

        [Header(Painterly Grass Palette Matching)]
        [Toggle(_USE_PALETTE)] _UsePalette ("Enable Stylized Palette Tint (Warna Tambahan)", Float) = 0
        _PaletteBlend ("Grass Palette Harmony (Blend)", Range(0.0, 1.0)) = 0.85
        _TopColor ("Top Sunlit Color (Serasi Rumput Cerah)", Color) = (0.55, 0.88, 0.24, 1.0)
        _MidColor ("Mid Foliage Color (Serasi Badan Rumput)", Color) = (0.28, 0.65, 0.18, 1.0)
        _DarkColor ("Dark Under Color (Serasi Pangkal Rumput)", Color) = (0.067, 0.42, 0.22, 1.0)
        _LumaMin ("Texture Luma Min", Range(0.0, 0.5)) = 0.10
        _LumaMax ("Texture Luma Max", Range(0.3, 1.0)) = 0.52

        [Header(Emission)]
        [Toggle(_EMISSION_ON)] _UseEmission ("Enable Emission", Float) = 0
        [HDR] _EmissionColor ("Emission Color", Color) = (0.0, 0.0, 0.0, 1.0)

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
            float4 _EmissionColor;

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
            float  _UseEmission;
            float  _UsePalette;
        CBUFFER_END

        float4 _PlayerPosition;
        float4 _PlayerTramplePos;

        // Perhitungan deformasi angin Variatif & Alami (Multi-Tier Organic Wind)
        float3 ApplyBushFoliageDisplacement(float3 posOS, float3 posWS, float heightFactor)
        {
            // Menjamin daun dan dahan memiliki ayunan alami proporsional dari pangkal ke pucuk
            float effectiveHeight = max(heightFactor, 0.35);
            float bendWeight = pow(effectiveHeight, 1.25);

            if (_WindIntensity > 0.001)
            {
                // Arah angin yang dinormalisasi
                float2 rawDir = float2(_WindDirX, _WindDirZ);
                float dirLen = length(rawDir);
                float2 windDir = (dirLen > 0.001) ? (rawDir / dirLen) : float2(1.0, 0.0);
                float2 perpDir = float2(-windDir.y, windDir.x);

                float freq = max(_WindTiling, 0.005);
                float speed = _WindSpeed * 1.5;

                // 1. Variasi per-objek (Setiap semak memiliki fase acak unik agar tidak berayun serempak seperti klon)
                float3 rootWS = TransformObjectToWorld(float3(0.0, 0.0, 0.0));
                float objectSeed = sin(dot(rootWS.xz, float2(12.9898, 78.233))) * 43758.5453;
                float objOffset = frac(objectSeed) * 6.28318;

                // 2. Siklus Hembusan Angin Dinamis (Gusting / Breathing: angin kadang kencang, kadang sepoi mereda)
                float gustEnvelope = sin(_Time.y * (speed * 0.35) + dot(posWS.xz, windDir * (freq * 0.4)) + objOffset) * 0.35 + 0.75;

                // 3. Ombak Utama Kanopi (Main Canopy Sway - Ayunan anggun batang & semak utama)
                float mainPhase = dot(posWS.xz, windDir * freq) - _Time.y * speed + objOffset;
                float mainSway = sin(mainPhase);

                // 4. Dinamika Dahan Sekunder (Sub-Branch Sway - Gerakan dahan dengan rasio frekuensi emas non-repetitif)
                float branchPhase = dot(posWS.xz, perpDir * (freq * 1.618)) - _Time.y * (speed * 1.42) + (posOS.y * 0.03) + objOffset;
                float branchSway = sin(branchPhase) * 0.35;

                // 5. Getaran Mikro Daun Hidup (Turbulent Leaf Flutter - Daun berdesir bervariasi mengikuti posisi daun)
                float leafNoise = sin(posOS.x * 0.15 + posOS.z * 0.15 + posOS.y * 0.10);
                float leafPhase = _Time.y * max(_FlutterSpeed, 2.0) + (posWS.x * 2.2 + posWS.z * 2.2) + leafNoise * 3.1415;
                float leafFlutter = sin(leafPhase) * (max(_FlutterAmount, 0.02) * 1.8);

                // 6. Penggabungan Gerakan 3D Bergelombang Alami
                float totalForward = (mainSway * 0.75 + branchSway * 0.35) * gustEnvelope * (_WindIntensity * 0.48) * bendWeight;
                float totalSide    = (branchSway * 0.65 + leafFlutter) * gustEnvelope * (_WindIntensity * 0.28) * bendWeight;

                posWS.xz += windDir * totalForward + perpDir * totalSide;
                // Fisika lentur dahan: merunduk anggun saat di puncak ayunan
                posWS.y  -= (totalForward * totalForward) * 0.08;
            }

            // 2. Interaksi senggolan pemain
            float3 playerDist = posWS - _PlayerPosition.xyz;
            float distXZ = length(playerDist.xz);
            float pushRadius = 1.35;
            if (distXZ < pushRadius && abs(playerDist.y) < 2.0)
            {
                float pushFactor = (1.0 - (distXZ / pushRadius)) * _PlayerPushStrength * heightFactor;
                float2 pushDir = (distXZ > 0.001) ? (playerDist.xz / distXZ) : float2(0, 1);
                posWS.xz += pushDir * pushFactor;
                posWS.y -= pushFactor * 0.35;
            }

            // 3. Efek injakan kaki pemain
            float3 trampleDist = posWS - _PlayerTramplePos.xyz;
            float trampleLen = length(trampleDist.xz);
            if (trampleLen < 1.0 && abs(trampleDist.y) < 1.2)
            {
                float trampleFactor = (1.0 - (trampleLen / 1.0)) * 0.5 * heightFactor;
                posWS.y -= trampleFactor * 0.4;
            }

            return posWS;
        }

        // Rekonstruksi Normal Bulat (Spherical Normals) + Sky Bias
        float3 CalculateBushNormalWS(float3 posOS, float3 rawNormalOS, float3 displacedPosWS)
        {
            float3 origNormalWS = TransformObjectToWorldNormal(rawNormalOS);
            float3 centerOS = float3(0.0, _CenterYOffset, 0.0);
            float3 sphereDirOS = normalize(posOS - centerOS + float3(0.0001, 0.0001, 0.0001));
            float3 sphereNormalWS = TransformObjectToWorldNormal(sphereDirOS);

            float3 blendedN = normalize(lerp(origNormalWS, sphereNormalWS, _SphericalNormalBlend));
            blendedN = normalize(lerp(blendedN, float3(0.0, 1.0, 0.0), _UpNormalBlend));
            return blendedN;
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
            #pragma shader_feature_local _USE_PALETTE
            #pragma shader_feature_local _EMISSION_ON

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
                half3 pureBase = texCol.rgb * _BaseColor.rgb;
                #if defined(_USE_PALETTE)
                    half effectivePaletteBlend = (_UsePalette > 0.5) ? _PaletteBlend : 0.0;
                #else
                    half effectivePaletteBlend = 0.0;
                #endif
                half3 albedo = lerp(pureBase, mappedCol, effectivePaletteBlend);

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

                // Sisi bayangan: segar dan luminous (mengikuti mode palet aktif vs natural)
                half3 shadowTint = lerp(half3(0.72, 0.72, 0.72), _ShadowColor.rgb, effectivePaletteBlend);
                half3 shadowAmbient = albedo * shadowTint;
                // Sisi terang: hangat tersorot sinar mentari
                half3 litSunlight = albedo * mainLight.color * _SunlightColor.rgb;
                half3 diffuseLight = lerp(shadowAmbient, litSunlight, toonBand);

                // Subsurface Scattering (SSS / Efek daun tembus cahaya alami mengikuti warna daun albedo)
                float3 backLightDir = normalize(L + N * 0.35);
                half backDot = saturate(dot(-V, backLightDir));
                half sss = pow(backDot, _SSSPower) * _SSSStrength * input.heightFactor;
                half3 sssColor = albedo * _SSSColor.rgb * sss * mainLight.color;

                // Fresnel Rim halus di siluet tepi dedaunan
                half fresnel = pow(1.0 - saturate(dot(N, V)), 3.5);
                half3 rimLight = albedo * mainLight.color * _RimColor.rgb * (fresnel * _RimStrength * input.heightFactor);

                // Ambient Spherical Harmonics untuk beradaptasi dengan pencahayaan langit & scene
                half3 ambientSH = SampleSH(N) * albedo * 0.25;

                half3 finalColor = diffuseLight + sssColor + rimLight + ambientSH;

                // 5. Fitur Emission (Toggleable On / Off)
                #if defined(_EMISSION_ON)
                if (_UseEmission > 0.5)
                {
                    finalColor += _EmissionColor.rgb;
                }
                #endif

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
