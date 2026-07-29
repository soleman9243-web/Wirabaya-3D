Shader "Custom/URP_DissolveEmission"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalStrenght ("Normal Strength", Range(0, 1.5)) = 0.5
        _DissolveMap ("Dissolve Map (Mask)", 2D) = "white" {}
        _DissolveAmount ("DissolveAmount", Range(0,1)) = 0
        [HDR] _DissolveColor ("DissolveColor (Edge Color)", Color) = (1,1,1,1)
        _DissolveEmission ("DissolveEmission (Intensity)", Range(0,10)) = 1
        _DissolveWidth ("DissolveWidth (Thickness)", Range(0,0.1)) = 0.05
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="AlphaTest" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Back

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

            TEXTURE2D(_MainTex);          SAMPLER(sampler_MainTex);
            TEXTURE2D(_DissolveMap);      SAMPLER(sampler_DissolveMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _DissolveMap_ST;
                float4 _Color;
                float4 _DissolveColor;
                float _DissolveAmount;
                float _DissolveEmission;
                float _DissolveWidth;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _Color;
                half mask = SAMPLE_TEXTURE2D(_DissolveMap, sampler_DissolveMap, i.uv).r;

                // Hitung Alpha Clip
                if (_DissolveAmount > 0.001)
                {
                    if (mask < _DissolveAmount)
                    {
                        discard;
                    }
                }

                bool isEdge = mask < (_DissolveAmount + _DissolveWidth);
                
                half3 finalColor = albedo.rgb;
                half3 emission = half3(0,0,0);

                if (isEdge && _DissolveAmount > 0.001)
                {
                    finalColor = _DissolveColor.rgb;
                    emission = _DissolveColor.rgb * _DissolveEmission;
                }

                return half4(finalColor + emission, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
