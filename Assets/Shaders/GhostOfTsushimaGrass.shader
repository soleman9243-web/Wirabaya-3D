Shader "Custom/GhostOfTsushimaGrass"
{
    Properties
    {
        [Header(Texture and Alpha)]
        _BaseMap ("Grass Texture (Albedo)", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0.1, 0.8)) = 0.35

        [Header(Tsushima Colors)]
        _TopColor ("Top Color (Sunlit)", Color) = (0.45, 0.88, 0.22, 1)
        _BottomColor ("Bottom Color (Earth Root)", Color) = (0.12, 0.38, 0.08, 1)
        _Translucency ("Sunlight Glow Strength", Range(0.0, 1.5)) = 0.5

        [Header(Wind Waves)]
        _WindSpeed ("Wind Speed", Range(0.1, 6.0)) = 2.0
        _WindFrequency ("Wind Wave Frequency", Range(0.05, 1.5)) = 0.3
        _WindStrength ("Wind Sway Strength", Range(0.0, 1.0)) = 0.22
        _WindAngle ("Wind Angle (Degrees)", Range(0, 360)) = 45.0

        [Header(Player Interaction)]
        _TrampleStrength ("Player Bend Strength", Range(0.1, 3.0)) = 1.4
        _TrampleRadius ("Interaction Radius", Range(0.5, 3.5)) = 1.8
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

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float  heightFrac : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _TopColor;
                float4 _BottomColor;
                float  _Cutoff;
                float  _Translucency;
                float  _WindSpeed;
                float  _WindFrequency;
                float  _WindStrength;
                float  _WindAngle;
                float  _TrampleStrength;
                float  _TrampleRadius;
            CBUFFER_END

            float4 _PlayerTramplePos;

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 localPos = input.positionOS.xyz;
                float height = saturate(localPos.y * 1.5);
                output.heightFrac = height;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                float3 posWS = TransformObjectToWorld(localPos);

                // 1. ANGIN 1 ARAH MENGALIR LEMBUT DI GPU
                float windRad = _WindAngle * (3.14159265 / 180.0);
                float2 windDir = float2(sin(windRad), cos(windRad));
                float windTime = _Time.y * _WindSpeed;
                float waveProj = (posWS.x * windDir.x + posWS.z * windDir.y) * _WindFrequency;
                float wave = sin(windTime + waveProj) * _WindStrength * height;

                posWS.x += windDir.x * wave;
                posWS.z += windDir.y * wave;
                posWS.y -= abs(wave) * 0.15;

                // 2. REBAH INJAKAN PLAYER DI GPU
                float3 playerPos = _PlayerTramplePos.xyz;
                float trampleRad = (_PlayerTramplePos.w > 0.05) ? _PlayerTramplePos.w : _TrampleRadius;
                float3 toGrass = posWS - playerPos;
                float distXZ = length(toGrass.xz);

                if (distXZ < trampleRad && abs(toGrass.y) < 2.5)
                {
                    float f = (1.0 - distXZ / trampleRad);
                    f = f * f;
                    float2 pushDir = (distXZ > 0.001) ? normalize(toGrass.xz) : windDir;
                    posWS.xz += pushDir * (f * height * _TrampleStrength * 0.8);
                    posWS.y  -= f * height * _TrampleStrength * 0.9;
                }

                output.positionWS = posWS;
                output.positionCS = TransformWorldToHClip(posWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 texCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                clip(texCol.a - _Cutoff);

                // Gradasi Tsushima: Akar gelap -> Pucuk cerah bersinar matahari
                half3 foliageGradient = lerp(_BottomColor.rgb, _TopColor.rgb, input.heightFrac);
                half3 albedo = texCol.rgb * foliageGradient * 1.35;

                // Pencahayaan Foliage Lembut Tanpa Bayangan Hitam Pekat (Soft Upward Normals)
                Light mainLight = GetMainLight();
                half NdotL = saturate(dot(float3(0, 1, 0), mainLight.direction));
                
                // Translucency / Backlight Glow
                half3 backlight = mainLight.color * _Translucency * input.heightFrac;
                half3 directLight = mainLight.color * (NdotL * 0.35 + 0.65);
                half3 ambient = half3(0.25, 0.32, 0.22); // Ambient hijau langit

                half3 finalColor = albedo * (directLight + ambient) + (albedo * backlight * 0.4);

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
