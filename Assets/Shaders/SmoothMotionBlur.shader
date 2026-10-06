Shader "Hidden/Wirabaya/SmoothMotionBlur"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off
        Blend Off

        Pass
        {
            Name "SmoothMotionBlur"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment BlurFrag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_MotionVectorTexture);

            float _Shutter;
            float _MaxBlur;
            float _SampleCount;

            half4 BlurFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;

                half4 centerColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float2 velocity = SAMPLE_TEXTURE2D_X(_MotionVectorTexture, sampler_PointClamp, uv).xy * _Shutter;
                float len = length(velocity);

                // Jika piksel tidak bergerak (kecepatan nol, seperti karakter bertanda ForceNoMotion),
                // langsung kembalikan warna asli tanpa blur sama sekali.
                if (len < 1e-5)
                    return centerColor;

                // Batasi panjang blur secara halus (soft clamp)
                velocity *= min(len, _MaxBlur) / len;

                // Kedalaman kamera pada piksel pusat (dalam meter linear)
                float rawCenterDepth = SampleSceneDepth(uv);
                float centerEye = LinearEyeDepth(rawCenterDepth, _ZBufferParams);

                // Threshold kedalaman bilateral: diskalakan dengan jarak kamera
                float depthThreshold = max(0.08, centerEye * 0.02);

                // Dither noise acak berbasis interleaved gradient noise
                float baseNoise = InterleavedGradientNoise(uv * _ScreenParams.xy, 0);

                half3 accumColor = centerColor.rgb;
                half totalWeight = 1.0h;

                int sampleCount = max(4, (int)_SampleCount);

                [loop]
                for (int i = 0; i < sampleCount; i++)
                {
                    // Golden-ratio interleaved jitter per-sampel:
                    // Menghilangkan bayangan bertingkat (stepped ghosting) menjadi sapuan halus sinematik
                    float jitter = frac(baseNoise + (float)i * 0.61803398875);
                    float t = ((float)i + jitter) / (float)sampleCount - 0.5; // rentang -0.5 .. +0.5
                    float2 sampleUV = uv + velocity * t;

                    // Abaikan sampel di luar batas layar
                    if (any(sampleUV < 0.0) || any(sampleUV > 1.0))
                        continue;

                    // Bilateral Depth Rejection (Anti-Ghosting / Anti-Smear):
                    // Jika titik sampel berada lebih dekat ke kamera daripada piksel pusat (misal tubuh Wirabaya di depan tanah),
                    // piksel tanah DILARANG menyedot warna karakter!
                    float sampleRawDepth = SampleSceneDepth(sampleUV);
                    float sampleEye = LinearEyeDepth(sampleRawDepth, _ZBufferParams);
                    float depthDiff = centerEye - sampleEye; // Positif jika sampel berada di depan titik pusat

                    if (depthDiff > depthThreshold)
                        continue; // Buang sampel karakter agar tidak mencetak siluet berulang ke latar!

                    // Bobot Gaussian halus agar ekor blur memudar secara natural
                    half w = (half)exp(-4.5 * t * t);

                    accumColor += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, sampleUV).rgb * w;
                    totalWeight += w;
                }

                return half4(accumColor / totalWeight, centerColor.a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
