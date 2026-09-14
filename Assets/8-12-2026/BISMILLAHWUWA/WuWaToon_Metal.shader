// v2.5 — WuWa Toon Character: Dedicated Metal Shader (Super Shiny Armor & Weapon)
Shader "WuWa/Toon - Metal (Logam - Shining Armor)"
{
    Properties
    {
        [HideInInspector] _MaterialType ("Material Type", Float) = 1

        // ── Albedo & Alpha ──
        [Header(Base Color)]
        _BaseMap ("Base Texture (Albedo)", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha Clipping", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        // ── Cel Shading & Shadows (High Contrast Anime) ──
        [Header(Cel Shading and Shadows)]
        _1stShadowColor ("1st Shadow Color", Color) = (0.28, 0.28, 0.35, 1.0)
        _2ndShadowColor ("2nd Shadow Color (Deep)", Color) = (0.12, 0.12, 0.18, 1.0)
        _ShadowThreshold ("Shadow Threshold", Range(0.0, 1.0)) = 0.5
        _ShadowFeather ("Shadow Feather / Softness", Range(0.001, 0.5)) = 0.03
        _2ndShadowThreshold ("2nd Shadow Threshold", Range(0.0, 1.0)) = 0.25
        _NormalToonInfluence ("Normal Influence on Cel Shadow", Range(0.0, 1.0)) = 0.0
        [Toggle] _Use_2nd_Shadow ("Use 2nd Deep Shadow (Anime Depth)", Float) = 1

        // ── Penonjolan Lekukan (Creases and Cavities) ──
        [Header(Creases and Cavity Depth)]
        [Toggle(_NORMALMAP)] _EnableNormalMap ("Enable Normal Map", Float) = 0
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Strength", Range(0.0, 3.0)) = 1.0
        _NormalCreaseBoost ("Crease Shadow Boost", Range(0.0, 2.0)) = 0.0
        [Toggle(_OCCLUSIONMAP)] _EnableOcclusion ("Enable Cavity AO Map", Float) = 0
        _OcclusionMap ("Cavity / AO Map", 2D) = "white" {}
        _OcclusionStrength ("Cavity AO Strength", Range(0.0, 1.0)) = 0.0
        _CavityDeepening ("Deep Crease Shadowing", Range(0.0, 1.0)) = 0.0

        // ── WuWa Metal Settings (Super Shiny Armor & Glint!) ──
        [Header(WuWa Metal Reflection Settings)]
        _MetalColor ("Metal Tint Color", Color) = (1.0, 0.95, 0.85, 1.0)
        _MetalSpecColor ("Metal Specular Glint", Color) = (1.0, 1.0, 1.0, 1.0)
        _MetalMap ("Metal MatCap (Optional)", 2D) = "black" {}
        [Toggle] _MetalUseMatCap ("Use MatCap Texture", Float) = 0
        _MetalSharpness ("Metal Specular Sharpness", Range(0.1, 3.0)) = 1.5
        _MetalIntensity ("Metal Reflection Intensity", Range(0.0, 5.0)) = 2.0
        _MetalShadowContrast ("Metal Shadow Contrast", Range(0.1, 1.0)) = 0.35

        // ── Stylized Rim Light (Anime Fresnel) ──
        [Header(Stylized Rim Light)]
        _RimColor ("Rim Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _RimPower ("Rim Power", Range(1.0, 10.0)) = 4.0
        _RimIntensity ("Rim Intensity", Range(0.0, 3.0)) = 0.0
        _RimLightThreshold ("Rim Light Direction Bias", Range(-1.0, 1.0)) = 0.0

        // ── HDR Emission ──
        [Header(Emission Glow)]
        [Toggle(_EMISSION)] _EnableEmission ("Enable Emission", Float) = 0
        _EmissionMap ("Emission Map", 2D) = "white" {}
        [HDR] _EmissionColor ("Emission Color", Color) = (0, 0, 0, 1)
        _EmissionIntensity ("Emission Intensity", Range(0.0, 10.0)) = 1.0

        // ── Inverted Hull Outline ──
        [Header(Inverted Hull Outline)]
        _OutlineColor ("Outline Color", Color) = (0.2, 0.18, 0.22, 1.0)
        _OutlineWidth ("Outline Width", Range(0.0, 10.0)) = 0.0
        _OutlineZOffset ("Outline Depth Offset", Range(0.0, 5.0)) = 1.0
        _OutlineDepthFade ("Outline Distance Scaling", Range(0.0, 1.0)) = 0.5

        // ── WuWa v2.0: Texture & Crease Controls ──
        [Header(WuWa v2.0 Quality)]
        _TexturePreserve ("Texture Detail in Shadow", Range(0.0, 1.0)) = 0.25
        _CreaseHighlight ("Crease/Cavity Visibility", Range(0.0, 1.0)) = 0.0
        _ShadowColorShift ("Shadow Warm(+) / Cool(-) Shift", Range(-1.0, 1.0)) = 0.0
        _2ndShadowFeather ("2nd Shadow Feather", Range(0.001, 0.5)) = 0.12
        _OutlineAlbedoBlend ("Outline Texture Blend", Range(0.0, 1.0)) = 0.5

        // ── Rendering Settings ──
        [Header(Render Options)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 0

        // ── Compatibility Fallbacks (Seamless saat nimpa material lama) ──
        [HideInInspector] _MainTex ("BaseMap Fallback", 2D) = "white" {}
        [HideInInspector] _Color ("BaseColor Fallback", Color) = (1, 1, 1, 1)
        [HideInInspector] _NormalMap ("NormalMap Fallback", 2D) = "bump" {}
        [HideInInspector] _Shading_Color ("Shading Color Fallback", Color) = (0.7, 0.7, 0.7, 1)
        [HideInInspector] _Cel_Shader_Offset ("Cel Offset Fallback", Float) = 0.5
        [HideInInspector] _Cel_Ramp_Smoothness ("Cel Smoothness Fallback", Float) = 0.1
        [HideInInspector] _UseNormalMap ("Use Normal Map Fallback", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ToonForwardVert
            #pragma fragment ToonForwardFrag

            #define _MATERIAL_METAL 1

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _OCCLUSIONMAP
            #pragma shader_feature_local _EMISSION
            #pragma shader_feature_local _ALPHATEST_ON

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_instancing

            #include "WuWaToon_Core.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "Outline" }

            Cull Front
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ToonOutlineVert
            #pragma fragment ToonOutlineFrag

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_instancing

            #include "WuWaToon_Core.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ToonShadowVert
            #pragma fragment ToonShadowFrag

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_instancing

            #include "WuWaToon_Core.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ToonDepthOnlyVert
            #pragma fragment ToonDepthOnlyFrag

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_instancing

            #include "WuWaToon_Core.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            ZTest LEqual
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ToonDepthNormalsVert
            #pragma fragment ToonDepthNormalsFrag

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_instancing

            #include "WuWaToon_Core.hlsl"
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
