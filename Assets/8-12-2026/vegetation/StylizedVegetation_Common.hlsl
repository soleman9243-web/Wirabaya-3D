#ifndef STYLIZED_VEGETATION_COMMON_INCLUDED
#define STYLIZED_VEGETATION_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// ── Textures & Samplers (Explicitly Declared for DX11) ──
TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
TEXTURE2D(_GrassTrailRT);   SAMPLER(sampler_GrassTrailRT);

// ── Per-Material Constant Buffer (Strict 16-Byte Aligned for SRP Batcher) ──
CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;

    float  _Cutoff;
    float  _WindSpeed;
    float  _WindIntensity;
    float  _WindDirX;

    float  _WindDirZ;
    float  _WindTiling;
    float  _GrassHeight;
    float  _BendStrength;

    float  _Smoothness;
    float  _RecoveryTime;
    float  _TrampleBendAmount;
    float  _EmissionIntensity;

    float4 _EmissionColor;

    float  _EmissionTipBoost;
    float3 _Pad0;
CBUFFER_END

// ── Global Variables ──
float4 _PlayerPosition;
float4 _PlayerTramplePos;
float4 _PlayerForwardDir;
float4 _GrassTrailCenter;
float  _GrassTrailSize;

// ── Safe Displacement (Anti Nembus Terrain / Permukaan Tanah) ──
float3 ApplyGrassDisplacement(float3 posWS, float heightFactor, float3 rootWS)
{
    // Bagian pangkal tetap di tanah, makin ke ujung makin bebas meliuk
    float windMask = pow(saturate(heightFactor), 1.35);

    // === 1. WIND WAVE (PERSIS SEPERTI DI STYLIZED GRASS, TANPA TEKSTUR) ===
    if (_WindIntensity > 0.001)
    {
        float2 rawDir = float2(_WindDirX, _WindDirZ);
        float dirLen = length(rawDir);
        float2 windDir = (dirLen > 0.001) ? (rawDir / dirLen) : float2(1.0, 0.0);
        float2 perpDir = float2(-windDir.y, windDir.x);

        float tiling = max(_WindTiling, 0.005);
        float speed = _WindSpeed * 0.50;

        float2 uv1 = posWS.xz * (tiling * 0.7) - windDir * (_Time.y * speed);
        float2 uv2 = posWS.xz * (tiling * 1.6) - windDir * (_Time.y * speed * 1.4) + float2(0.35, 0.65);

        float sample1 = sin(uv1.x + uv1.y) * 0.5 + 0.5;
        float sample2 = cos(uv2.x - uv2.y) * 0.5 + 0.5;

        float gustWave = sample1 * 0.65 + sample2 * 0.35;
        float gustPush = smoothstep(0.20, 0.85, gustWave);
        float pushAmount = gustPush * (_WindIntensity * 1.5) * windMask;

        float flutter = sin(_Time.y * (_WindSpeed * 3.2) + posWS.x * 1.8 + posWS.z * 1.8) * (0.10 * _WindIntensity * windMask);

        if (!isnan(pushAmount) && !isinf(pushAmount))
        {
            posWS.xz += windDir * (pushAmount * 0.85) + perpDir * flutter;
            posWS.y  -= pushAmount * 0.18;
        }
    }

    // === 2. PLAYER INTERACTION REAL-TIME (TANGIBLE, RAPI & SATU ARAH) ===
    float4 pPos = (_PlayerTramplePos.w > 0.05) ? _PlayerTramplePos : _PlayerPosition;
    float playerBend = 0.0;
    if (pPos.w > 0.05)
    {
        float3 diff = posWS - pPos.xyz;
        float distXZ = length(diff.xz);
        float vertDist = diff.y;

        if (distXZ < pPos.w && vertDist > -2.0 && vertDist < 2.0)
        {
            float f = 1.0 - (distXZ / pPos.w);
            playerBend = smoothstep(0.0, 1.0, f) * heightFactor * _BendStrength;
            float2 pushDir = (distXZ > 0.01) ? normalize(diff.xz) : float2(0.0, 1.0);

            float2 fwdDir = normalize(_PlayerForwardDir.xy);
            if (length(_PlayerForwardDir.xy) < 0.01) fwdDir = float2(0.0, 1.0);
            float2 unifiedBendDir = normalize(fwdDir * 0.80 + pushDir * 0.20);

            posWS.xz += unifiedBendDir * (playerBend * 0.65);
        }
    }

    // === 3. TRAIL INTERACTION (JEJAK KAKI — TERKUNCI ARAH, RECOVERY HALUS) ===
    float trailBend = 0.0;
    if (_GrassTrailSize > 1.0)
    {
        float2 trailUV = (posWS.xz - _GrassTrailCenter.xy) / _GrassTrailSize + 0.5;
        if (trailUV.x >= 0.001 && trailUV.x <= 0.999 && trailUV.y >= 0.001 && trailUV.y <= 0.999)
        {
            float4 trailSample = SAMPLE_TEXTURE2D_LOD(_GrassTrailRT, sampler_GrassTrailRT, trailUV, 0);
            float trailIntensity = trailSample.r;
            if (!isnan(trailIntensity) && !isinf(trailIntensity))
            {
                float trailFactor = saturate(1.0 - trailIntensity) * heightFactor;
                if (trailFactor > 0.01)
                {
                    float2 bakedDir;
                    bakedDir.x = trailSample.g * 2.0 - 1.0;
                    bakedDir.y = trailSample.b * 2.0 - 1.0;
                    float bLen = length(bakedDir);
                    float2 fwdDir = (bLen > 0.01) ? (bakedDir / bLen) : float2(0.0, 1.0);

                    trailBend = trailFactor * max(_TrampleBendAmount, 0.0);
                    float bendFwd = trailBend * 0.55;

                    posWS.xz += fwdDir * bendFwd;
                }
            }
        }
    }

    // === 4. BATASI PENURUNAN TINGGI (ANTI TEMBUS TERRAIN / TANAH) ===
    float totalBend = playerBend * 0.40 + trailBend * 0.50;
    // Maksimal penurunan hanya boleh sampai tepat di atas permukaan tanah (akar)
    float heightAboveRoot = max(posWS.y - rootWS.y, 0.0);
    // Batasi drop agar menyisakan minimal 0.04m (4cm) di atas tanah
    float maxDrop = max(heightAboveRoot - 0.04, 0.0);
    float actualDrop = min(totalBend, maxDrop);
    posWS.y -= actualDrop;

    // Hard floor guard: Dijamin 100% tidak pernah tembus ke bawah permukaan tanah
    posWS.y = max(posWS.y, rootWS.y + 0.03);

    return posWS;
}

#endif
