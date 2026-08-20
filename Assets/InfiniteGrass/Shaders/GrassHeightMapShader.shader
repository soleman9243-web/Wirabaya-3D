Shader "InfiniteGrass/GrassHeightMapShader"
{
    Properties
    {
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque" 
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "GrassHeightMap"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float4 color        : COLOR;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 data        : TEXCOORD0;
            };

            float2 _BoundsYMinMax;

            float Remap(float In, float2 InMinMax, float2 OutMinMax)
            {
                float denom = InMinMax.y - InMinMax.x;
                if (abs(denom) < 0.0001) return OutMinMax.x;
                return OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / denom;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(worldPos);

                float rChannel = Remap(worldPos.y, _BoundsYMinMax, float2(0, 1)); // Altitude
                float gChannel = 1.0; // Mark as valid terrain ground for grass spawning

                output.data = float2(rChannel, gChannel);
                return output;
            }

            float2 frag(Varyings input) : SV_Target
            {
                return input.data;
            }
            ENDHLSL
        }
    }
}
