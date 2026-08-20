Shader "Custom/InteractiveGrassURP"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Color (Top Tint)", Color) = (0.7, 0.95, 0.4, 1)
        _BottomColor ("Color (Bottom Tint)", Color) = (0.2, 0.5, 0.15, 1)
        _BendStrength ("Bend Strength Multiplier", Range(0.1, 3.0)) = 1.5
        _BendRadius ("Default Bend Radius", Range(0.2, 4.0)) = 1.3
        _WindSpeed ("Wind Speed", Float) = 2.5
        _WindFrequency ("Wind Frequency", Float) = 1.2
        _WindStrength ("Wind Strength", Float) = 0.08
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 0
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque" 
            "Queue"="Geometry" 
            "RenderPipeline"="UniversalPipeline" 
        }
        LOD 200
        Cull [_Cull]

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
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 positionWS   : TEXCOORD1;
                float3 normalWS     : NORMAL;
                float heightFactor  : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _BottomColor;
                float _BendStrength;
                float _BendRadius;
                float _WindSpeed;
                float _WindFrequency;
                float _WindStrength;
            CBUFFER_END

            // Global Player Position sent from C# script: (x, y, z, radius)
            float4 _PlayerPosition;

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                // Height weight: bagian atas helai rumput lebih lentur bergerak daripada pangkal bawah
                float heightWeight = saturate(input.positionOS.y * 1.5);
                output.heightFactor = heightWeight;

                // 1. Natural Wind Wave Sway
                float windTime = _Time.y * _WindSpeed;
                float windWave = sin(windTime + positionWS.x * _WindFrequency + positionWS.z * _WindFrequency);
                float3 windOffset = float3(
                    windWave * _WindStrength * heightWeight,
                    -abs(windWave) * 0.2 * _WindStrength * heightWeight,
                    cos(windTime * 0.7 + positionWS.z * _WindFrequency) * _WindStrength * heightWeight
                );
                positionWS += windOffset;

                // 2. Interactive Player Bending (Hanya aktif jika Player mengirimkan radius > 0.05)
                if (_PlayerPosition.w > 0.05)
                {
                    float3 playerPos = _PlayerPosition.xyz;
                    float radius = _PlayerPosition.w;
                    
                    float3 toPlayer = positionWS - playerPos;
                    float distXZ = length(toPlayer.xz);

                    // Cek apakah player berada di dekat helai rumput ini
                    if (distXZ < radius && abs(toPlayer.y) < 2.5)
                    {
                        float normalizedDist = distXZ / radius;
                        float pushFactor = (1.0 - normalizedDist) * heightWeight * _BendStrength;
                        pushFactor = pushFactor * pushFactor; // Quadratic curve for smooth bending

                        float2 pushDirXZ = (distXZ > 0.001) ? normalize(toPlayer.xz) : float2(1.0, 0.0);
                        
                        // Dorong rumput ke samping dan tekan ke bawah mendekati tanah
                        positionWS.xz += pushDirXZ * (pushFactor * 0.75);
                        positionWS.y  -= pushFactor * 0.85;
                    }
                }

                output.positionWS = positionWS;
                output.positionHCS = TransformWorldToHClip(positionWS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);

                // Gradient warna: dari bawah agak gelap ke atas hijau terang
                half4 grassColor = lerp(_BottomColor, _BaseColor, input.heightFactor) * texColor;

                // Lighting URP sederhana
                Light mainLight = GetMainLight();
                half NdotL = saturate(dot(input.normalWS, mainLight.direction));
                half3 lighting = (mainLight.color * (NdotL * 0.6 + 0.4)) + half3(0.2, 0.25, 0.2); // Ambient

                half4 finalColor = half4(grassColor.rgb * lighting, 1.0);
                return finalColor;
            }
            ENDHLSL
        }

        // Shadow Caster Pass agar rumput bisa menghasilkan bayangan
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 _PlayerPosition;
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _BottomColor;
                float _BendStrength;
                float _BendRadius;
                float _WindSpeed;
                float _WindFrequency;
                float _WindStrength;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float heightWeight = saturate(input.positionOS.y * 1.5);

                if (_PlayerPosition.w > 0.05)
                {
                    float3 playerPos = _PlayerPosition.xyz;
                    float radius = _PlayerPosition.w;
                    float3 toPlayer = positionWS - playerPos;
                    float distXZ = length(toPlayer.xz);

                    if (distXZ < radius && abs(toPlayer.y) < 2.5)
                    {
                        float pushFactor = (1.0 - (distXZ / radius)) * heightWeight * _BendStrength;
                        float2 pushDirXZ = (distXZ > 0.001) ? normalize(toPlayer.xz) : float2(1.0, 0.0);
                        positionWS.xz += pushDirXZ * (pushFactor * 0.75);
                        positionWS.y  -= pushFactor * 0.85;
                    }
                }

                output.positionHCS = TransformWorldToHClip(positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
