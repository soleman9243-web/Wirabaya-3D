#ifndef STYLIZED_GRASS_COMMON_INCLUDED
#define STYLIZED_GRASS_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// ── Textures & Samplers (Explicitly Declared for DX11) ──
TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
TEXTURE2D(_WindTex);        SAMPLER(sampler_WindTex);
TEXTURE2D(_GrassTrailRT);   SAMPLER(sampler_GrassTrailRT);

// ── Per-Material Constant Buffer (Strict 16-Byte Aligned for SRP Batcher) ──
CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _NearColor;
    float4 _FarColor;
    float4 _BottomColor;

    float  _Cutoff;
    float  _NearDist;
    float  _FarDist;
    float  _HighBlend;

    float  _WindSpeed;
    float  _WindIntensity;
    float  _WindDirX;
    float  _WindDirZ;

    float  _WindTiling;
    float  _GrassHeight;
    float  _BendStrength;
    float  _Smoothness;

    float4 _RecoveryColor;
    float  _RecoveryColorStrength;
    float3 _Pad0;

    float4 _EmissionColor;
    float  _EmissionIntensity;
    float  _EmissionTipBoost;
    float2 _Pad1;
CBUFFER_END

// ── Global Variables ──
float4 _PlayerPosition;
float4 _PlayerTramplePos;
float4 _PlayerForwardDir;
float4 _GrassTrailCenter;
float  _GrassTrailSize;

// ── Safe Displacement (100% Anti-NaN) ──
float3 ApplyGrassDisplacement(float3 posWS, float heightFactor)
{
    // Bagian pangkal tetap di tanah, makin ke ujung makin bebas meliuk
    float windMask = pow(saturate(heightFactor), 1.35);

    // === 1. WIND WAVE BERBASIS TEKSTUR 7063-BUMP.JPG (ORGANIK, ALAMI, SEPERTI INFINITE GRASS) ===
    if (_WindIntensity > 0.001)
    {
        float2 rawDir = float2(_WindDirX, _WindDirZ);
        float dirLen = length(rawDir);
        float2 windDir = (dirLen > 0.001) ? (rawDir / dirLen) : float2(1.0, 0.0);
        float2 perpDir = float2(-windDir.y, windDir.x);

        float tiling = max(_WindTiling, 0.005);
        float speed = _WindSpeed * 0.50;

        // Sampling tekstur 7063-bump.jpg yang mengalir di world space
        // Menggunakan 2 layer UV bertingkat untuk memecah pengulangan dan menciptakan gelombang hembusan angin organik
        float2 uv1 = posWS.xz * (tiling * 0.7) - windDir * (_Time.y * speed);
        float2 uv2 = posWS.xz * (tiling * 1.6) - windDir * (_Time.y * speed * 1.4) + float2(0.35, 0.65);

        float sample1 = SAMPLE_TEXTURE2D_LOD(_WindTex, sampler_WindTex, uv1, 0).r;
        float sample2 = SAMPLE_TEXTURE2D_LOD(_WindTex, sampler_WindTex, uv2, 0).r;

        if (isnan(sample1) || isinf(sample1)) sample1 = 0.5;
        if (isnan(sample2) || isinf(sample2)) sample2 = 0.5;

        // Gelombang hembusan angin gabungan (0.0 s/d 1.0)
        float gustWave = sample1 * 0.65 + sample2 * 0.35;

        // Kontras hembusan: zona tenang vs zona gelombang angin bergulung
        float gustPush = smoothstep(0.20, 0.85, gustWave);

        // Kekuatan dorongan fisik saat gelombang hembusan lewat
        float pushAmount = gustPush * (_WindIntensity * 1.5) * windMask;

        // Getaran mikro melintang (side flutter) agar rumput tampak alami dan berdesir
        float flutter = sin(_Time.y * (_WindSpeed * 3.2) + posWS.x * 1.8 + posWS.z * 1.8) * (0.10 * _WindIntensity * windMask);

        if (!isnan(pushAmount) && !isinf(pushAmount))
        {
            // Mendorong rumput meliuk searah angin dan merunduk saat puncak hembusan lewat
            posWS.xz += windDir * (pushAmount * 0.85) + perpDir * flutter;
            posWS.y  -= pushAmount * 0.28;
        }
    }

    // === 2. PLAYER INTERACTION REAL-TIME (TANGIBLE, RAPI & SATU ARAH) ===
    float4 pPos = (_PlayerTramplePos.w > 0.05) ? _PlayerTramplePos : _PlayerPosition;
    if (pPos.w > 0.05)
    {
        float3 diff = posWS - pPos.xyz;
        float distXZ = length(diff.xz);

        // Ketinggian vertikal: merespon saat karakter di sekitar rumput
        float vertDist = diff.y;
        if (distXZ < pPos.w && vertDist > -2.0 && vertDist < 2.0)
        {
            float f = 1.0 - (distXZ / pPos.w);
            float bend = smoothstep(0.0, 1.0, f) * heightFactor * _BendStrength;
            float2 pushDir = (distXZ > 0.01) ? normalize(diff.xz) : float2(0.0, 1.0);

            // Arah rebah SATU ARAH: dominan searah hadap/langkah player (_PlayerForwardDir),
            // dipadukan sedikit dengan arah dorongan radial agar rumput melipat rapi ke satu arah
            float2 fwdDir = normalize(_PlayerForwardDir.xy);
            if (length(_PlayerForwardDir.xy) < 0.01) fwdDir = float2(0.0, 1.0);
            float2 unifiedBendDir = normalize(fwdDir * 0.80 + pushDir * 0.20);

            // Rebah rapi satu arah (tidak mekar berantakan ke segala arah)
            posWS.xz += unifiedBendDir * (bend * 0.60);
            posWS.y  -= bend * 0.22;
        }
    }

    // === 3. TRAIL INTERACTION (JEJAK KAKI DENGAN RECOVERY JELAS) ===
    if (_GrassTrailSize > 1.0)
    {
        float2 trailUV = (posWS.xz - _GrassTrailCenter.xy) / _GrassTrailSize + 0.5;
        if (trailUV.x >= 0.001 && trailUV.x <= 0.999 && trailUV.y >= 0.001 && trailUV.y <= 0.999)
        {
            float trailSample = SAMPLE_TEXTURE2D_LOD(_GrassTrailRT, sampler_GrassTrailRT, trailUV, 0).r;
            if (!isnan(trailSample) && !isinf(trailSample))
            {
                // 1.0 = normal/tanpa jejak, < 1.0 = ada jejak kaki
                float trailFactor = saturate(1.0 - trailSample) * heightFactor;
                if (trailFactor > 0.01)
                {
                    posWS.y -= trailFactor * 0.25;
                    posWS.xz += float2(0.25, 0.20) * (trailFactor * 0.50);
                }
            }
        }
    }

    return posWS;
}

#endif
