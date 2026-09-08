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
CBUFFER_END

// ── Global Variables ──
float4 _PlayerPosition;
float4 _PlayerTramplePos;
float4 _GrassTrailCenter;
float  _GrassTrailSize;

// ── Safe Displacement (100% Anti-NaN) ──
float3 ApplyGrassDisplacement(float3 posWS, float heightFactor)
{
    // Bagian tengah ke atas saja yang goyang terkena angin (akar tetap kokoh di tanah)
    float windMask = smoothstep(0.25, 1.0, heightFactor);

    // === 1. WIND WAVE SEJAJAR DENGAN 7063-BUMP.JPG (TERKONTROL, TIDAK REBAH BERLEBIHAN) ===
    if (_WindIntensity > 0.001)
    {
        float2 rawDir = float2(_WindDirX, _WindDirZ);
        float dirLen = length(rawDir);
        float2 windDir = (dirLen > 0.001) ? (rawDir / dirLen) : float2(1.0, 0.0);

        float waveProj = (posWS.x * windDir.x + posWS.z * windDir.y) * _WindTiling;
        float waveTime = _Time.y * _WindSpeed;

        // Modulasi tekstur pola angin 7063-bump.jpg
        float2 windUV = posWS.xz * _WindTiling + windDir * (waveTime * 0.15);
        float windSample = SAMPLE_TEXTURE2D_LOD(_WindTex, sampler_WindTex, windUV, 0).r;
        float bumpFactor = (!isnan(windSample) && !isinf(windSample)) ? (windSample * 0.6 + 0.4) : 1.0;

        float wave = sin(waveTime + waveProj) * (_WindIntensity * 0.35) * windMask * bumpFactor;

        // Batasi kemiringan maksimal agar rumput tidak pernah miring berlebihan
        wave = clamp(wave, -0.30, 0.30);

        if (!isnan(wave) && !isinf(wave))
        {
            posWS.xz += windDir * wave;
            posWS.y  -= abs(wave) * 0.08;
        }
    }

    // === 2. PLAYER INTERACTION REAL-TIME (TANGIBLE TRAMPLE & SIDE-PARTING) ===
    float4 pPos = (_PlayerTramplePos.w > 0.05) ? _PlayerTramplePos : _PlayerPosition;
    if (pPos.w > 0.05)
    {
        float3 diff = posWS - pPos.xyz;
        float distXZ = length(diff.xz);

        // Ketinggian vertikal: merespon saat karakter di sekitar rumput (tidak merespon jika loncat tinggi di udara)
        float vertDist = diff.y;
        if (distXZ < pPos.w && vertDist > -2.0 && vertDist < 2.0)
        {
            float f = 1.0 - (distXZ / pPos.w);
            float bend = smoothstep(0.0, 1.0, f) * heightFactor * _BendStrength;
            float2 pushDir = (distXZ > 0.01) ? normalize(diff.xz) : float2(0.0, 1.0);

            // Menyibak jelas ke samping & merunduk wajar (terasa mantap diinjak, tidak tembus tanah)
            posWS.xz += pushDir * (bend * 0.75);
            posWS.y  -= bend * 0.28;
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
