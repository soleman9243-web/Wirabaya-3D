Shader "Hidden/Wirabaya/UIBackgroundBlur"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off
        Blend Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        float4 _BlurTexelSize;
        float _BlurOffset;
        ENDHLSL

        Pass
        {
            Name "CopySharp"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragCopy

            half4 FragCopy(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half3 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord).rgb;
                return half4(col, 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "KawaseDownsample"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragDownsample

            half4 FragDownsample(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float2 halfPixel = _BlurTexelSize.xy * _BlurOffset;

                half3 sum = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb * 4.0h;
                sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, saturate(uv - halfPixel)).rgb;
                sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, saturate(uv + halfPixel)).rgb;
                sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, saturate(uv + float2(halfPixel.x, -halfPixel.y))).rgb;
                sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, saturate(uv - float2(halfPixel.x, -halfPixel.y))).rgb;

                return half4(sum * 0.125h, 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "KawaseUpsample"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragUpsample

            half4 FragUpsample(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float2 halfPixel = _BlurTexelSize.xy * _BlurOffset;

                half3 sum = 0.0h;
                sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, saturate(uv + float2(-halfPixel.x * 2.0, 0.0))).rgb;
                sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, saturate(uv + float2(-halfPixel.x, halfPixel.y))).rgb * 2.0h;
                sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, saturate(uv + float2(0.0, halfPixel.y * 2.0))).rgb;
                sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, saturate(uv + float2(halfPixel.x, halfPixel.y))).rgb * 2.0h;
                sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, saturate(uv + float2(halfPixel.x * 2.0, 0.0))).rgb;
                sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, saturate(uv + float2(halfPixel.x, -halfPixel.y))).rgb * 2.0h;
                sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, saturate(uv + float2(0.0, -halfPixel.y * 2.0))).rgb;
                sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, saturate(uv + float2(-halfPixel.x, -halfPixel.y))).rgb * 2.0h;

                return half4(sum / 12.0h, 1.0h);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
