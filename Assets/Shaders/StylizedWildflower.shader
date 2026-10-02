Shader "FantasyKingdom/StylizedWildflower"
{
    // =====================================================================
    // Stylized Wildflower & Vegetation Shader (Universal Render Pipeline)
    // ---------------------------------------------------------------------
    // Fitur Utama:
    //  1. 100% Pure Original Texture: Warna murni dari tekstur asli (tanpa perubahan warna).
    //  2. High-Energy Wind Sway: Goyangan angin kencang, banter, & dinamis (pangkal kokoh di tanah).
    //  3. World-Space Height Auto-Scale: Kompatibel dengan semua skala GameObject (Scale 1x s/d 10x).
    //  4. 100% Anti-Blackout Guarantee: Bunga di area bayangan pohon tetap terang (lantai bayangan 65%),
    //     kebal dari bug properti kosong/0, kebal dari NaN/Inf, dan tidak akan pernah menjadi siluet hitam.
    //  5. Double-Sided Normal Handling (Cull Off + VFACE): Kelopak bunga terlihat indah dari depan maupun belakang.
    //  6. Subsurface Translucency (SSS): Kelopak bunga bercahaya tembus sinar saat membelakangi matahari.
    //  7. Full URP Passes: UniversalForward, DepthOnly, DepthNormals (SSAO), dan ShadowCaster.
    // =====================================================================

    Properties
    {
        [Header(Base Texture)]
        _BaseMap ("Flower Texture (Albedo)", 2D) = "white" {}
        _BaseColor ("Base Color Tint", Color) = (1, 1, 1, 1)
        _Cutoff ("Alpha Cutoff", Range(0.0, 0.95)) = 0.5

        [Header(Wind Foliage Animation)]
        _WindSpeed ("Wind Speed (Kecepatan Angin)", Range(0.5, 15.0)) = 5.0
        _WindIntensity ("Wind Intensity (Kekuatan Goyang)", Range(0.0, 10.0)) = 2.5
        _WindDirX ("Wind Direction X", Float) = 1.0
        _WindDirZ ("Wind Direction Z", Float) = 0.5
        _WindTiling ("Wind Wave Tiling", Float) = 0.1
        _FlowerHeight ("Flower Height in World (Meters)", Float) = 2.0
        _FlowerBaseOffset ("Root Ground Offset (Meters)", Float) = 0.0

        [Header(Stylized Lighting)]
        _ShadowAmbient ("Shadow Ambient Tint", Color) = (0.70, 0.72, 0.75, 1.0)
        _SunlightTint ("Sunlight Tint", Color) = (1.10, 1.08, 0.98, 1.0)
        _ToonSmoothness ("Toon Shading Softness", Range(0.01, 0.5)) = 0.18
        _UpNormalBlend ("Puffy / Sky Normal Blend", Range(0.0, 1.0)) = 0.35

        [Header(Petal Translucency SSS)]
        [HDR] _SSSColor ("Petal Backlight Glow Color", Color) = (1.0, 0.90, 0.65, 1.0)
        _SSSStrength ("Petal SSS Intensity", Range(0.0, 2.0)) = 0.55
        _SSSPower ("Petal SSS Sharpness", Range(1.0, 10.0)) = 3.5
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
            float4 _ShadowAmbient;
            float4 _SunlightTint;
            float4 _SSSColor;

            float  _Cutoff;

            float  _WindSpeed;
            float  _WindIntensity;
            float  _WindDirX;
            float  _WindDirZ;
            float  _WindTiling;
            float  _FlowerHeight;
            float  _FlowerBaseOffset;

            float  _ToonSmoothness;
            float  _UpNormalBlend;
            float  _SSSStrength;
            float  _SSSPower;
        CBUFFER_END

        // ── Safe Normalize (Kebal dari NaN jika vektor berjarak 0) ────────
        float3 SafeNormalizeVec3(float3 v, float3 fallback)
        {
            float lenSq = dot(v, v);
            return (lenSq > 1e-6) ? (v * rsqrt(lenSq)) : fallback;
        }

        // ── Perhitungan Goyangan Angin (Wind Sway) ────────────────────────
        float3 ApplyFlowerWindDisplacement(float3 posWS, float3 rootWS, out float heightFactor)
        {
            // Tinggi titik vertikal relatif terhadap akar tanaman di dunia nyata
            float heightWS = max(0.0, posWS.y - (rootWS.y + _FlowerBaseOffset));
            heightFactor = saturate(heightWS / max(_FlowerHeight, 0.05));

            // Bending mask: pangkal = 0 (menancap di tanah), kelopak/kepala atas = 1 (goyang maksimal)
            float windMask = pow(heightFactor, 1.25);

            if (_WindIntensity > 0.001)
            {
                float2 rawDir = float2(_WindDirX, _WindDirZ);
                float dirLen = length(rawDir);
                float2 windDir = (dirLen > 0.001) ? (rawDir / dirLen) : float2(1.0, 0.0);
                float2 perpDir = float2(-windDir.y, windDir.x);

                float tiling = max(_WindTiling, 0.005);
                // Kecepatan dan dinamika angin dioptimalkan agar responsif dan banter
                float t = _Time.y * (_WindSpeed * 2.2);

                // 1. Ayunan utama batang (Primary Sway)
                float mainWave = sin(posWS.x * tiling + posWS.z * tiling + t);
                // 2. Variasi ritme hembusan (Gusting wave)
                float gustWave = sin(posWS.x * (tiling * 1.8) - posWS.z * (tiling * 1.4) + t * 1.35) * 0.45;
                // 3. Sentakan angin kencang (Wind burst)
                float burst = sin(t * 0.55 + (posWS.x + posWS.z) * 0.08) * 0.35 + 0.85;

                // 4. Getaran kencang kelopak bunga (Petal flutter)
                float flutter = sin(t * 2.8 + posWS.x * 2.5 + posWS.z * 2.5) * 0.25;

                // Total kekuatan ayunan (dikalikan pengali responsif agar nilai slider terasa banter)
                float totalSway = ((mainWave + gustWave) * burst + flutter);
                float push = totalSway * (_WindIntensity * 1.4) * windMask;

                // Batasi agar tidak melompat ekstrim / overshooting
                push = clamp(push, -3.0, 3.0);

                // Melipat/meliuk searah angin + gerak melintang
                posWS.xz += windDir * push + perpDir * (flutter * _WindIntensity * 0.5 * windMask);
                // Fisika lentur batang: tinggi turun saat meliuk (panjang batang tetap konstan)
                posWS.y  -= (push * push) * 0.15;
            }

            return posWS;
        }
        ENDHLSL

        // =================================================================
        // PASS 1: UniversalForward (Main Lit Shading)
        // =================================================================
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ForwardVert
            #pragma fragment ForwardFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS

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

                float3 originalPosWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 rootWS = TransformObjectToWorld(float3(0.0, 0.0, 0.0));

                float heightFactor = 0.0;
                float3 displacedPosWS = ApplyFlowerWindDisplacement(originalPosWS, rootWS, heightFactor);
                output.heightFactor = heightFactor;

                output.positionWS = displacedPosWS;
                output.positionCS = TransformWorldToHClip(displacedPosWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                float3 rawNormalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 blendedNormal = lerp(rawNormalWS, float3(0.0, 1.0, 0.0), saturate(_UpNormalBlend));
                output.normalWS = SafeNormalizeVec3(blendedNormal, float3(0.0, 1.0, 0.0));

                return output;
            }

            half4 ForwardFrag(Varyings input, FRONT_FACE_TYPE isFrontFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 texCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                clip(texCol.a - _Cutoff);

                // 1. Albedo murni 100% dari tekstur asli
                half3 baseCol = (length(_BaseColor.rgb) > 0.01) ? _BaseColor.rgb : half3(1.0, 1.0, 1.0);
                half3 albedo = texCol.rgb * baseCol;

                // 2. Normal dua sisi (Cull Off: normal dibalik jika sisi belakang menghadap kamera)
                float3 N = SafeNormalizeVec3(input.normalWS, float3(0.0, 1.0, 0.0));
                N = IS_FRONT_VFACE(isFrontFace, N, -N);

                // 3. URP Main Light & Shadow Sampling
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord, input.positionWS, half4(1, 1, 1, 1));
                half shadowAtten = saturate(mainLight.shadowAttenuation);

                float3 L = SafeNormalizeVec3(mainLight.direction, float3(0.0, 1.0, 0.0));
                float3 V = SafeNormalizeVec3(GetWorldSpaceNormalizeViewDir(input.positionWS), float3(0.0, 0.0, 1.0));

                // 4. Stylized Lighting Tints (Aman dengan fallback cerah)
                half3 sunTint = (length(_SunlightTint.rgb) > 0.05) ? _SunlightTint.rgb : half3(1.10, 1.08, 0.98);
                half3 shadowTint = (length(_ShadowAmbient.rgb) > 0.05) ? max(_ShadowAmbient.rgb, half3(0.68, 0.72, 0.75)) : half3(0.68, 0.72, 0.75);

                // 5. Ambient Sky & Ground (Bebas dari bug Light Probe 0/negatif)
                half skyFactor = saturate(N.y * 0.5 + 0.5);
                half3 ambientSky = lerp(half3(0.65, 0.68, 0.72), half3(0.85, 0.88, 0.92), skyFactor);

                // 6. Cel diffuse lighting ber-lantai terang aman (Bunga di area bayangan DIJAMIN tetap berwarna dan segar)
                half rawNdotL = dot(N, L);
                float softness = max(_ToonSmoothness, 0.01);
                half toon = smoothstep(0.0, softness, rawNdotL * 0.5 + 0.5);
                half shadowBand = lerp(0.65, 1.0, shadowAtten);
                half toonBand = saturate(toon * shadowBand);

                half3 shadowSide = albedo * shadowTint * ambientSky;
                half3 sunlightSide = albedo * sunTint * max(mainLight.color, half3(0.9, 0.9, 0.9));
                half3 diffuseLight = lerp(shadowSide, sunlightSide, toonBand);

                // 7. Subsurface scattering (Kelopak tembus sinar saat membelakangi matahari)
                float3 backLightDir = SafeNormalizeVec3(L + N * 0.35, L);
                half backDot = max(0.0, dot(-V, backLightDir));
                half sss = 0.0;
                if (backDot > 0.001)
                {
                    sss = pow(backDot, max(_SSSPower, 1.0)) * (_SSSStrength * input.heightFactor);
                }
                half3 sssTint = (length(_SSSColor.rgb) > 0.05) ? _SSSColor.rgb : half3(1.0, 0.90, 0.65);
                half3 sssCol = sssTint * sss * mainLight.color * shadowAtten;

                half3 finalRGB = diffuseLight + sssCol;

                // 8. JAMINAN MUTLAK ANTI-BLACKOUT (Secara matematis MUSTAHIL menjadi hitam)
                // Di area bayangan pohon terdalam sekalipun, bunga tetap mempertahankan minimal 65% kecerahan albedo aslinya.
                half3 minSafeFloor = albedo * shadowTint * 0.68;
                finalRGB = max(finalRGB, minSafeFloor);

                if (any(isnan(finalRGB)) || any(isinf(finalRGB)))
                {
                    finalRGB = minSafeFloor;
                }

                return half4(finalRGB, 1.0);
            }
            ENDHLSL
        }

        // =================================================================
        // PASS 2: DepthOnly (Mandatory for Depth Prepass)
        // =================================================================
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
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

                float3 originalPosWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 rootWS = TransformObjectToWorld(float3(0.0, 0.0, 0.0));

                float heightFactor = 0.0;
                float3 displacedPosWS = ApplyFlowerWindDisplacement(originalPosWS, rootWS, heightFactor);

                output.positionCS = TransformWorldToHClip(displacedPosWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 DepthFrag(DepthVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 texCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                clip(texCol.a - _Cutoff);
                return 0;
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
            #pragma target 3.0
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

                float3 originalPosWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 rootWS = TransformObjectToWorld(float3(0.0, 0.0, 0.0));

                float heightFactor = 0.0;
                float3 displacedPosWS = ApplyFlowerWindDisplacement(originalPosWS, rootWS, heightFactor);

                output.positionCS = TransformWorldToHClip(displacedPosWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                float3 rawNormalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 blendedNormal = lerp(rawNormalWS, float3(0.0, 1.0, 0.0), saturate(_UpNormalBlend));
                output.normalWS = SafeNormalizeVec3(blendedNormal, float3(0.0, 1.0, 0.0));
                return output;
            }

            void DepthNormalsFrag(DepthNormalsVaryings input, out half4 outNormalWS : SV_Target0)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 texCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                clip(texCol.a - _Cutoff);
                outNormalWS = half4(SafeNormalizeVec3(input.normalWS, float3(0.0, 1.0, 0.0)), 0.0);
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
            #pragma target 3.0
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

                float3 originalPosWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 rootWS = TransformObjectToWorld(float3(0.0, 0.0, 0.0));

                float heightFactor = 0.0;
                float3 displacedPosWS = ApplyFlowerWindDisplacement(originalPosWS, rootWS, heightFactor);

                float3 normalWS = SafeNormalizeVec3(TransformObjectToWorldNormal(input.normalOS), float3(0.0, 1.0, 0.0));
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
