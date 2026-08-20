Shader "Custom/StylizedInfiniteGrassURP"
{
    Properties
    {
        [Header(Base Texture)]
        [MainTexture] _BaseTexture ("Base Texture (Albedo)", 2D) = "white" {}
        _BaseMap ("Base Map (Fallback)", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0.0, 0.9)) = 0.35

        [Header(Colors Tinting)]
        _TopColor ("Top Color (Sunlit)", Color) = (0.45, 0.85, 0.25, 1)
        _BottomColor ("Bottom Color (Root)", Color) = (0.12, 0.38, 0.10, 1)

        [Header(Grass Dimensions)]
        _GrassHeight ("Grass Height Multiplier", Range(0.5, 3.0)) = 1.3
        _GrassWidth ("Grass Width Multiplier", Range(0.5, 2.5)) = 1.0

        [Header(Unidirectional Wind)]
        _WindSpeed ("Wind Speed", Range(0.1, 8.0)) = 2.2
        _WindFrequency ("Wind Wave Frequency", Range(0.05, 2.0)) = 0.35
        _WindStrength ("Wind Sway Strength", Range(0.0, 1.5)) = 0.25
        _WindAngle ("Wind Compass Angle (Degrees)", Range(0, 360)) = 45.0

        [Header(Player Trample Interaction)]
        _TrampleStrength ("Player Flatten Strength", Range(0.1, 3.0)) = 1.5
        _TrampleRadius ("Interaction Radius", Range(0.5, 4.0)) = 1.8
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="TransparentCutout" 
            "Queue"="AlphaTest" 
            "RenderPipeline"="UniversalPipeline"
        }
        LOD 200
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 positionWS   : TEXCOORD1;
                float  heightWeight : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_BaseTexture);
            SAMPLER(sampler_BaseTexture);
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseTexture_ST;
                float4 _BaseMap_ST;
                float4 _TopColor;
                float4 _BottomColor;
                float  _Cutoff;
                float  _GrassHeight;
                float  _GrassWidth;
                float  _WindSpeed;
                float  _WindFrequency;
                float  _WindStrength;
                float  _WindAngle;
                float  _TrampleStrength;
                float  _TrampleRadius;
            CBUFFER_END

            // Global Vector dari Player: (pos.x, pos.y, pos.z, radius)
            float4 _PlayerTramplePos;

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 localPos = input.positionOS.xyz;
                localPos.xz *= _GrassWidth;
                localPos.y  *= _GrassHeight;

                float heightWeight = saturate(localPos.y * 1.5);
                output.heightWeight = heightWeight;
                output.uv = TRANSFORM_TEX(input.uv, _BaseTexture);

                float3 posWS = TransformObjectToWorld(localPos);

                // 1. ANGIN 1 ARAH TERATUR DI GPU (0% CPU LOAD)
                float windRad = _WindAngle * (3.14159265 / 180.0);
                float2 windDir = float2(sin(windRad), cos(windRad));
                float windTime = _Time.y * _WindSpeed;
                
                float waveProj = (posWS.x * windDir.x + posWS.z * windDir.y) * _WindFrequency;
                float wave1 = sin(windTime + waveProj);
                float wave2 = cos(windTime * 0.7 + waveProj * 1.3) * 0.35;
                float totalWind = (wave1 + wave2) * _WindStrength * heightWeight;

                posWS.x += windDir.x * totalWind;
                posWS.z += windDir.y * totalWind;
                posWS.y -= abs(totalWind) * 0.2;

                // 2. REBAH INJAKAN KAKI DI GPU (0% CPU LOAD)
                float3 playerPos = _PlayerTramplePos.xyz;
                float trampleRad = (_PlayerTramplePos.w > 0.05) ? _PlayerTramplePos.w : _TrampleRadius;

                float3 toGrass = posWS - playerPos;
                float distXZ = length(toGrass.xz);

                if (distXZ < trampleRad && abs(toGrass.y) < 2.5)
                {
                    float factor = 1.0 - (distXZ / trampleRad);
                    factor = factor * factor;

                    float pushAmount = factor * heightWeight * _TrampleStrength;
                    float2 pushDir = (distXZ > 0.001) ? normalize(toGrass.xz) : windDir;

                    posWS.xz += pushDir * (pushAmount * 0.85);
                    posWS.y  -= pushAmount * 1.0;
                }

                output.positionWS = posWS;
                output.positionCS = TransformWorldToHClip(posWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 texCol = SAMPLE_TEXTURE2D(_BaseTexture, sampler_BaseTexture, input.uv);
                
                // Cutout alpha test jika tekstur punya alpha
                if (texCol.a < _Cutoff)
                {
                    discard;
                }

                // Warna gradasi alami
                half3 gradCol = lerp(_BottomColor.rgb, _TopColor.rgb, input.heightWeight);
                half3 finalCol = texCol.rgb * gradCol * 1.5;

                // Lighting
                Light mainLight = GetMainLight();
                half NdotL = saturate(dot(float3(0, 1, 0), mainLight.direction));
                half3 lighting = mainLight.color * (NdotL * 0.4 + 0.6);

                return half4(finalCol * lighting, 1.0);
            }
            ENDHLSL
        }

        // ShadowCaster Pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_BaseTexture);
            SAMPLER(sampler_BaseTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseTexture_ST;
                float4 _BaseMap_ST;
                float4 _TopColor;
                float4 _BottomColor;
                float  _Cutoff;
                float  _GrassHeight;
                float  _GrassWidth;
                float  _WindSpeed;
                float  _WindFrequency;
                float  _WindStrength;
                float  _WindAngle;
                float  _TrampleStrength;
                float  _TrampleRadius;
            CBUFFER_END

            float4 _PlayerTramplePos;

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 localPos = input.positionOS.xyz;
                localPos.xz *= _GrassWidth;
                localPos.y  *= _GrassHeight;
                float heightWeight = saturate(localPos.y * 1.5);

                float3 posWS = TransformObjectToWorld(localPos);

                float windRad = _WindAngle * (3.14159265 / 180.0);
                float2 windDir = float2(sin(windRad), cos(windRad));
                float wave = sin(_Time.y * _WindSpeed + (posWS.x * windDir.x + posWS.z * windDir.y) * _WindFrequency);
                posWS.xz += windDir * (wave * _WindStrength * heightWeight);

                output.uv = TRANSFORM_TEX(input.uv, _BaseTexture);
                output.positionCS = TransformWorldToHClip(posWS);
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 texCol = SAMPLE_TEXTURE2D(_BaseTexture, sampler_BaseTexture, input.uv);
                if (texCol.a < _Cutoff) discard;
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
