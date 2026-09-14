Shader "Hidden/FantasyKingdom/GrassTrailSystem"
{
    // Shader internal untuk sistem trail rumput.
    // Pass 0: ScrollFade — geser isi RT mengikuti pergerakan player + pudarkan jejak (recovery).
    // Pass 1: Stamp — cap/stempel posisi player sebagai lingkaran gelap di RT + simpan arah langkah.
    //
    // Format RT: ARGB32
    //   R = trail intensity (1 = bersih, 0 = terinjak penuh)
    //   G = dirX encoded (0..1, decode: *2-1 → -1..1)
    //   B = dirZ encoded (0..1, decode: *2-1 → -1..1)
    //   A = 1 (unused)

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
        // sekaligus memudarkan (fade) R channel ke arah putih (recovery).
        // G & B (arah) dipertahankan apa adanya selama masih ada jejak.
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

                // Jika UV sumber di luar batas [0,1], kembalikan bersih (area baru yang bersih)
                float inBounds = step(0.0, srcUV.x) * step(srcUV.x, 1.0)
                               * step(0.0, srcUV.y) * step(srcUV.y, 1.0);

                half4 existing = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, srcUV);
                // clean = R=1 (no trail), G=0.5 (neutral dir X), B=0.5 (neutral dir Z), A=1
                half4 clean = half4(1.0, 0.5, 0.5, 1.0);

                // Pilih existing jika masih di dalam batas, clean jika di luar
                half4 scrolled = lerp(clean, existing, inBounds);

                // Pudarkan R (intensity) menuju 1.0 (recovery)
                scrolled.r = lerp(scrolled.r, 1.0, _FadeAmount);

                // G & B (arah) tetap dipertahankan selama jejak masih ada.
                // Saat jejak sudah pulih (R ≈ 1.0), kembalikan G & B ke netral (0.5)
                float trailStrength = saturate(1.0 - scrolled.r);
                scrolled.g = lerp(0.5, scrolled.g, step(0.01, trailStrength));
                scrolled.b = lerp(0.5, scrolled.b, step(0.01, trailStrength));

                return scrolled;
            }
            ENDHLSL
        }

        // ============================================================
        // PASS 1: Stamp
        // Menggambar lingkaran gelap halus (soft circle) di posisi player.
        // R = gelapkan (trail intensity), G & B = simpan arah langkah player saat stamp.
        // Arah di-bake agar rumput yang sudah terinjak TIDAK ikut berputar saat player berputar.
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
            float2 _StampDir; // Encoded arah forward player: (dirX*0.5+0.5, dirZ*0.5+0.5)

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

                // Kekuatan stamp
                float stampInfluence = circle * _StampStrength;

                // R: Gelapkan area (semakin gelap = semakin kuat jejak)
                float darken = 1.0 - stampInfluence;
                existing.r = min(existing.r, darken);

                // G & B: Bake arah forward player saat ini ke dalam texel.
                // Hanya di area yang terkena stamp, dan overwrite jika stamp lebih kuat.
                existing.g = lerp(existing.g, _StampDir.x, stampInfluence);
                existing.b = lerp(existing.b, _StampDir.y, stampInfluence);

                return existing;
            }
            ENDHLSL
        }
    }
}
