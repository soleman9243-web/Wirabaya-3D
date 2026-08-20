Shader "Custom/InteractiveGrassPro"
{
    Properties
    {
        [Header(Colors)]
        _TopColor ("Top Color", Color) = (0.4, 0.8, 0.15, 1)
        _BottomColor ("Bottom Color", Color) = (0.1, 0.3, 0.05, 1)

        [Header(Wind)]
        _WindSpeed ("Wind Speed", Range(0.1, 10.0)) = 2.5
        _WindFrequency ("Frequency", Range(0.1, 5.0)) = 1.5
        _WindStrength ("Strength", Range(0.0, 0.5)) = 0.1
        _WindDirection ("Direction (X, Z)", Vector) = (1.0, 0.5, 0, 0)

        [Header(Trample)]
        _TrampleStrength ("Bend Strength", Range(0.1, 3.0)) = 1.4
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite On
        ZTest LEqual

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float  height     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _TopColor;
                float4 _BottomColor;
                float  _WindSpeed;
                float  _WindFrequency;
                float  _WindStrength;
                float4 _WindDirection;
                float  _TrampleStrength;
            CBUFFER_END

            // Global dari PlayerGrassTrampler.cs
            float4 _PlayerTramplePos;

            Varyings vert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);

                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                float hw = saturate(input.positionOS.y * 2.0);
                o.height = hw;

                // === WIND SWAY ===
                float2 wd = normalize(_WindDirection.xy + float2(0.001, 0.001));
                float wt = _Time.y * _WindSpeed;
                float wave1 = sin(wt + dot(posWS.xz, wd) * _WindFrequency);
                float wave2 = cos(wt * 0.7 + posWS.x * 0.9) * 0.35;
                float wind = (wave1 + wave2) * _WindStrength * hw;
                posWS.x += wd.x * wind;
                posWS.z += wd.y * wind;
                posWS.y -= abs(wind) * 0.2;

                // === PLAYER TRAMPLE ===
                if (_PlayerTramplePos.w > 0.05)
                {
                    float3 diff = posWS - _PlayerTramplePos.xyz;
                    float dxz = length(diff.xz);
                    float rad = _PlayerTramplePos.w;
                    if (dxz < rad && abs(diff.y) < 2.5)
                    {
                        float f = (1.0 - dxz / rad) * hw * _TrampleStrength;
                        f = f * f;
                        float2 dir = (dxz > 0.001) ? normalize(diff.xz) : float2(1, 0);
                        posWS.xz += dir * f * 0.7;
                        posWS.y  -= f * 0.9;
                    }
                }

                o.positionCS = TransformWorldToHClip(posWS);
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half3 col = lerp(_BottomColor.rgb, _TopColor.rgb, input.height);
                return half4(col, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex depthVert
            #pragma fragment depthFrag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings  { float4 positionCS : SV_POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };

            CBUFFER_START(UnityPerMaterial)
                float4 _TopColor;
                float4 _BottomColor;
                float  _WindSpeed;
                float  _WindFrequency;
                float  _WindStrength;
                float4 _WindDirection;
                float  _TrampleStrength;
            CBUFFER_END

            float4 _PlayerTramplePos;

            Varyings depthVert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);

                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                float hw = saturate(input.positionOS.y * 2.0);

                float2 wd = normalize(_WindDirection.xy + float2(0.001, 0.001));
                float wt = _Time.y * _WindSpeed;
                float wind = sin(wt + dot(posWS.xz, wd) * _WindFrequency) * _WindStrength * hw;
                posWS.x += wd.x * wind;
                posWS.z += wd.y * wind;

                if (_PlayerTramplePos.w > 0.05)
                {
                    float3 diff = posWS - _PlayerTramplePos.xyz;
                    float dxz = length(diff.xz);
                    if (dxz < _PlayerTramplePos.w && abs(diff.y) < 2.5)
                    {
                        float f = (1.0 - dxz / _PlayerTramplePos.w) * hw * _TrampleStrength;
                        float2 dir = (dxz > 0.001) ? normalize(diff.xz) : float2(1, 0);
                        posWS.xz += dir * f * 0.7;
                        posWS.y  -= f * 0.9;
                    }
                }

                o.positionCS = TransformWorldToHClip(posWS);
                return o;
            }

            half4 depthFrag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
