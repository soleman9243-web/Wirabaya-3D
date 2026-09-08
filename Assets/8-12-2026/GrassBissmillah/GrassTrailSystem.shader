Shader "Hidden/FantasyKingdom/GrassTrailSystem"
{
    // Shader internal untuk sistem trail rumput.
    // Pass 0: ScrollFade — geser isi RT mengikuti pergerakan player + pudarkan jejak (recovery).
    // Pass 1: Stamp — cap/stempel posisi player sebagai lingkaran gelap di RT.

    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZTest Always ZWrite Off Cull Off

        // ============================================================
        // PASS 0: Scroll + Fade
        // Menggeser konten RT berdasarkan _ScrollDelta (agar jejak tetap di posisi dunia)
        // sekaligus memudarkan (fade) seluruh RT ke arah putih (recovery).
        // ============================================================
        Pass
        {
            Name "ScrollFade"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float2 _ScrollDelta;
            float _FadeAmount;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 srcUV = input.uv + _ScrollDelta;

                // Jika UV sumber di luar batas [0,1], kembalikan putih (area baru yang bersih)
                float inBounds = step(0.0, srcUV.x) * step(srcUV.x, 1.0)
                               * step(0.0, srcUV.y) * step(srcUV.y, 1.0);

                half4 existing = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, srcUV);
                half4 clean = half4(1, 1, 1, 1);

                // Pilih existing jika masih di dalam batas, putih jika di luar
                half4 scrolled = lerp(clean, existing, inBounds);

                // Pudarkan menuju putih (recovery)
                scrolled.rgb = lerp(scrolled.rgb, half3(1, 1, 1), _FadeAmount);

                return scrolled;
            }
            ENDHLSL
        }

        // ============================================================
        // PASS 1: Stamp
        // Menggambar lingkaran gelap halus (soft circle) di posisi player.
        // Membaca RT yang sudah ada, lalu menggelapkan area sekitar _StampUV.
        // ============================================================
        Pass
        {
            Name "Stamp"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float2 _StampUV;
            float _StampRadius;
            float _StampStrength;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 existing = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // Jarak dari titik stamp
                float dist = distance(input.uv, _StampUV);

                // Lingkaran halus (soft circle): 1 di tengah, 0 di pinggir
                float circle = 1.0 - smoothstep(_StampRadius * 0.3, _StampRadius, dist);

                // Gelapkan area: semakin gelap = semakin kuat jejak
                float darken = 1.0 - circle * _StampStrength;

                existing.rgb = min(existing.rgb, half3(darken, darken, darken));

                return existing;
            }
            ENDHLSL
        }
    }
}
