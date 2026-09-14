#ifndef WUWA_TOON_CORE_INCLUDED
#define WUWA_TOON_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// ── Textures & Samplers ──
TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
TEXTURE2D(_1stShadowMap);   SAMPLER(sampler_1stShadowMap);
TEXTURE2D(_BumpMap);        SAMPLER(sampler_BumpMap);
TEXTURE2D(_OcclusionMap);   SAMPLER(sampler_OcclusionMap);
TEXTURE2D(_MetalMap);       SAMPLER(sampler_MetalMap);
TEXTURE2D(_EmissionMap);    SAMPLER(sampler_EmissionMap);
TEXTURE2D(_OutlineMask);    SAMPLER(sampler_OutlineMask);

// ── Per-Material Constant Buffer (Strict 16-Byte Aligned for SRP Batcher) ──
CBUFFER_START(UnityPerMaterial)
    // 16-Byte Block 1 (Colors)
    float4 _BaseColor;
    // 16-Byte Block 2
    float4 _1stShadowColor;
    // 16-Byte Block 3
    float4 _2ndShadowColor;
    // 16-Byte Block 4 (Texture ST)
    float4 _BaseMap_ST;
    // 16-Byte Block 5
    float4 _BumpMap_ST;
    // 16-Byte Block 6
    float4 _EmissionColor;
    // 16-Byte Block 7
    float4 _RimColor;
    // 16-Byte Block 8
    float4 _OutlineColor;
    // 16-Byte Block 9 (Skin SSS Color)
    float4 _SkinSSSColor;
    // 16-Byte Block 10 (Metal Colors)
    float4 _MetalColor;
    // 16-Byte Block 11
    float4 _MetalSpecColor;
    // 16-Byte Block 12 (Compatibility Fallback Shading Color)
    float4 _Shading_Color;
    // 16-Byte Block 13 (Hair Angel Ring Color)
    float4 _HairSpecColor;

    // 16-Byte Block 14 (Floats Group 1: Material & Alpha)
    float  _MaterialType;        // 0: Standard (Cloth), 1: Metal, 2: Skin, 3: Hair
    float  _AlphaClip;
    float  _Cutoff;
    float  _EnableNormalMap;

    // 16-Byte Block 15 (Floats Group 2: Normal & Cavity)
    float  _BumpScale;
    float  _NormalCreaseBoost;
    float  _EnableOcclusion;
    float  _OcclusionStrength;

    // 16-Byte Block 16 (Floats Group 3: Shadows & Cavity Deepening)
    float  _CavityDeepening;
    float  _ShadowThreshold;
    float  _ShadowFeather;
    float  _2ndShadowThreshold;

    // 16-Byte Block 17 (Floats Group 4: Specular & Rim)
    float  _2ndShadowFeather;
    float  _SpecularIntensity;
    float  _SpecularRoughness;
    float  _RimPower;

    // 16-Byte Block 18 (Floats Group 5: Rim, Emission)
    float  _RimIntensity;
    float  _RimLightThreshold;
    float  _EnableEmission;
    float  _EmissionIntensity;

    // 16-Byte Block 19 (Floats Group 6: Skin SSS)
    float  _SkinSSSRange;
    float  _SkinSSSIntensity;
    float  _SkinShadowSoftness;
    float  _SkinSmoothFace;

    // 16-Byte Block 20 (Floats Group 7: Metal)
    float  _MetalSharpness;
    float  _MetalIntensity;
    float  _MetalShadowContrast;
    float  _MetalUseMatCap;

    // 16-Byte Block 21 (Floats Group 8: Outline & Render Settings)
    float  _OutlineWidth;
    float  _OutlineZOffset;
    float  _OutlineDepthFade;
    float  _Cull;

    // 16-Byte Block 22 (Floats Group 9: Normal Toon Control & Fallbacks)
    float  _NormalToonInfluence;
    float  _UseNormalMap;
    float  _Cel_Shader_Offset;
    float  _Cel_Ramp_Smoothness;

    // 16-Byte Block 23 (NekoLegends Enhanced Controls)
    float  _Use_2nd_Shadow;
    float  _Specular_Intensity;
    float  _Rim_Intensity_Legacy;
    float  _Rim_Power_Legacy;

    // 16-Byte Block 24 (WuWa v2.0: Texture & Crease Controls)
    float  _TexturePreserve;      // How much texture detail to keep in shadows (0=old, 1=full)
    float  _CreaseHighlight;      // Screen-space crease/cavity visibility strength
    float  _ShadowColorShift;     // Warm (+) / Cool (-) shadow color shift
    float  _OutlineAlbedoBlend;   // Blend outline color with texture albedo

    // 16-Byte Block 25 (WuWa v2.5: Hair Angel Ring Settings)
    float  _HairSpecIntensity;    // Angel ring brightness
    float  _HairSpecShift;        // Vertical shift of angel ring on head
    float  _HairSpecWidth;        // Band width
    float  _HairSpecSharpness;    // Band edge sharpness

    // 16-Byte Block 26 (Hair Jitter & Secondary Ring)
    float  _HairSpecJitter;       // Strand fiber separation
    float  _HairSecondaryRing;    // Secondary halo blend
    float  _HairSpecNoiseScale;   // Strand frequency
    float  _HairPadding;          // 16-byte alignment padding
CBUFFER_END

// ── Vertex Attributes & Varyings ──
struct ToonAttributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 uv           : TEXCOORD0;
    float4 color        : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct ToonVaryings
{
    float4 positionCS   : SV_POSITION;
    float2 uv           : TEXCOORD0;
    float3 positionWS   : TEXCOORD1;
    float3 normalWS     : TEXCOORD2;
    float4 tangentWS    : TEXCOORD3; // xyz = tangentWS, w = sign
    float4 vertexColor  : TEXCOORD4;
    float4 shadowCoord  : TEXCOORD5;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

// ── Outline Attributes & Varyings ──
struct OutlineAttributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 uv           : TEXCOORD0;
    float4 color        : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct OutlineVaryings
{
    float4 positionCS   : SV_POSITION;
    float2 uv           : TEXCOORD0;
    float4 vertexColor  : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

// ══════════════════════════════════════════════════════════════════
//  HELPER: Screen-Space Curvature Detection (ddx/ddy)
//  Mendeteksi lekukan geometri secara otomatis tanpa perlu AO map.
//  Semakin tinggi curvature = semakin dalam lekukan.
// ══════════════════════════════════════════════════════════════════
half CalculateScreenCurvature(float3 normalWS)
{
    float3 dx = ddx(normalWS);
    float3 dy = ddy(normalWS);
    float curvature = sqrt(dot(dx, dx) + dot(dy, dy));
    return saturate(curvature * 8.0); // Scale ke range 0-1 yang useful
}

// ══════════════════════════════════════════════════════════════════
//  HELPER: Luminance-Aware Shadow Tint (Texture Quality Fixer)
//  Menggelapkan bayangan TANPA menghilangkan detail texture.
// ══════════════════════════════════════════════════════════════════
half3 ApplyShadowTint(half3 albedo, half3 shadowColor, float preserveAmount)
{
    // Multiplicative direct shadow
    half3 directShadow = albedo * shadowColor;

    // Contrast-preserved shadow: menjaga ketajaman tekstur di bayangan
    half3 contrastShadow = shadowColor * pow(max(albedo, half3(0.001, 0.001, 0.001)), half3(1.10, 1.10, 1.10)) * 1.12;

    // preserveAmount: 0 = flat (crisp anime), 1 = detail preserved
    half3 blended = lerp(directShadow, contrastShadow, saturate(preserveAmount));

    // Floor pengaman: tidak boleh membuat tekstur menjadi hitam pekat
    return max(blended, albedo * shadowColor * 0.80);
}

// ══════════════════════════════════════════════════════════════════
//  HELPER: WuWa Shadow Color Shift (Warm/Cool tint on shadows)
// ══════════════════════════════════════════════════════════════════
half3 ShiftShadowColor(half3 shadowColor, float shift)
{
    if (abs(shift) < 0.01) return shadowColor;

    half3 warm = half3(1.08, 0.95, 0.88);
    half3 cool = half3(0.88, 0.92, 1.08);
    half3 tint = (shift > 0) ? lerp(half3(1,1,1), warm, shift) : lerp(half3(1,1,1), cool, -shift);
    return shadowColor * tint;
}

// ── Vertex Shader (Universal Forward) ──
ToonVaryings ToonForwardVert(ToonAttributes input)
{
    ToonVaryings output = (ToonVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput   = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    output.positionCS   = vertexInput.positionCS;
    output.positionWS   = vertexInput.positionWS;
    output.uv           = TRANSFORM_TEX(input.uv, _BaseMap);
    output.normalWS     = normalInput.normalWS;

    real sign = input.tangentOS.w * GetOddNegativeScale();
    output.tangentWS    = half4(normalInput.tangentWS.xyz, sign);
    output.vertexColor  = input.color;

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
        output.shadowCoord = GetShadowCoord(vertexInput);
    #endif

    return output;
}

// ── Helper: Normal & Tangent Mapping Standar URP (Presisi & Anti-Distorsi) ──
float3 CalculateWorldNormal(ToonVaryings input, float2 uv)
{
    float3 normalWS = normalize(input.normalWS);

    #if defined(_NORMALMAP)
        if (_EnableNormalMap > 0.5 || _UseNormalMap > 0.5)
        {
            float4 normalSample = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv);
            float3 normalTS = UnpackNormalScale(normalSample, _BumpScale);

            float sgn = input.tangentWS.w;
            float3 bitangent = sgn * cross(normalWS, input.tangentWS.xyz);
            half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent, normalWS);
            normalWS = normalize(TransformTangentToWorld(normalTS, tangentToWorld));
        }
    #endif

    return normalWS;
}

// ══════════════════════════════════════════════════════════════════
//  1. METAL: Stylized Anime Armor/Weapon — Super Shiny, Glint, High Contrast
//  Khas WuWa / Genshin / Anime AAA:
//  - Multi-band anime reflection (Horizon glint, dynamic angle streaks, sky/ground gradient)
//  - Intense 2-stage specular flash: blinding core highlight + broad glint
//  - Grazing edge Fresnel sheen
//  - High dynamic shadow contrast (dark cool shadow makes reflections POP!)
// ══════════════════════════════════════════════════════════════════
half3 CalculateStylizedMetal(float3 normalWS, float3 viewDirWS, Light light, half3 baseAlbedo, float litFactor, half3 ambient, half curvature)
{
    half3 mColor = (_MetalColor.r + _MetalColor.g + _MetalColor.b > 0.01) ? _MetalColor.rgb : half3(0.95, 0.95, 0.98);
    half3 mSpec  = (_MetalSpecColor.r + _MetalSpecColor.g + _MetalSpecColor.b > 0.01) ? _MetalSpecColor.rgb : half3(1.0, 1.0, 1.0);
    float mIntensity = max((_MetalIntensity > 0.01) ? _MetalIntensity : 1.8, 0.5);

    // 1. MatCap / Procedural Multi-Band Reflection (SUPER SHINY!)
    float3 normalVS = TransformWorldToViewNormal(normalWS, true);
    float2 matcapUV = normalVS.xy * 0.5 + 0.5;

    half3 matcapCol = half3(0, 0, 0);
    if (_MetalUseMatCap > 0.5)
    {
        matcapCol = SAMPLE_TEXTURE2D(_MetalMap, sampler_MetalMap, matcapUV).rgb * mColor * mIntensity;
    }
    else
    {
        // ── Stylized Multi-Band Anime Reflection ──
        // Band A: Horizon Flash (Intense white line reflecting world horizon)
        float horizonDist = abs(normalVS.y - 0.20);
        float horizonFlash = saturate(1.0 - horizonDist * 4.5);
        horizonFlash = smoothstep(0.3, 0.8, horizonFlash);

        // Band B: Dynamic Diagonal Reflection Streaks (Anime Armor Flow)
        float streak = saturate(sin(normalVS.y * 8.0 + normalVS.x * 5.0) * 0.5 + 0.5);
        streak = smoothstep(0.35, 0.70, streak);

        // Band C: Sky-to-Ground Anime Gradient
        half3 skyCol    = mColor * 1.5;
        half3 groundCol = mColor * 0.20;
        half3 envRefl   = lerp(groundCol, skyCol, saturate(normalVS.y * 0.5 + 0.5));

        // Combine Reflection Bands
        matcapCol = envRefl + mSpec * (horizonFlash * 1.8 + streak * 0.9);
        matcapCol *= mIntensity;

        // Grazing Fresnel Rim Glow (Anime edge glint)
        half NdotV = saturate(dot(normalWS, viewDirWS));
        half metalFresnel = pow(1.0 - NdotV, 3.2) * (0.8 * mIntensity);
        matcapCol += mSpec * metalFresnel;
    }

    // 2. Dual-Stage Specular Highlight — BLINDING SHINE (Cling / Ting!)
    float3 hDir = light.direction + viewDirWS;
    float hLen = length(hDir);
    float3 halfDir = (hLen > 0.0001) ? (hDir / hLen) : normalWS;
    float NdotH = saturate(dot(normalWS, halfDir));

    // Primary Blinding Core Specular
    float sharpness = max(_MetalSharpness * 60.0, 12.0);
    float specCore = pow(NdotH, sharpness);
    float steppedCore = smoothstep(0.12, 0.38, specCore) * 2.8;

    // Secondary Broad Metal Sheen
    float specBroad = pow(NdotH, max(sharpness * 0.25, 4.0));
    float steppedBroad = smoothstep(0.20, 0.55, specBroad) * 1.2;

    float finalSpec = (steppedCore + steppedBroad) * mIntensity;

    // 3. Metal Shadow — High Dynamic Cool Contrast
    half3 shadow1Tint = (_1stShadowColor.a > 0.01 && (_1stShadowColor.r + _1stShadowColor.g + _1stShadowColor.b) > 0.01) ? _1stShadowColor.rgb : half3(0.28, 0.28, 0.35);

    float texPres = max(_TexturePreserve, 0.20);
    float contrast = (_MetalShadowContrast > 0.01) ? _MetalShadowContrast : 0.40;
    half3 metalShadow = ApplyShadowTint(baseAlbedo, shadow1Tint, texPres) * contrast;
    metalShadow = max(metalShadow, baseAlbedo * 0.08);

    // Cool blue-violet slate tint in shadows (classic anime armor look)
    metalShadow *= half3(0.88, 0.90, 1.08);

    // Curvature cavity deepening on metal
    metalShadow *= (1.0 - curvature * _CreaseHighlight * 0.40);

    // 4. Lit area metal: bright, colorful, super shiny
    half3 metalLit = baseAlbedo * mColor * light.color + matcapCol * 0.7;
    metalLit = max(metalLit, baseAlbedo * mColor * 0.6);

    half3 metalResult = lerp(metalShadow, metalLit, litFactor);
    // Specular highlight adds blinding light on lit surface
    metalResult += mSpec * finalSpec * light.color * litFactor;
    // Ambient is kept minimal so metal shadow stays deep
    metalResult += ambient * 0.05;

    return metalResult;
}

// ══════════════════════════════════════════════════════════════════
//  2. SKIN: WuWa SSS & Warm Fringe — Lembut, Warm Undertone, Glowing Edge
//  Khas WuWa / Genshin / Anime AAA:
//  - SSS Fringe oranye-kemerahan terang di terminator bayangan
//  - Warm shadow tint (bebas dari kesan kusam/abu-abu kotor)
//  - Soft organic cel transition
// ══════════════════════════════════════════════════════════════════
half3 CalculateWuWaSkin(half3 albedo, half halfLambert, half litFactor, Light light, float shadowThresh, half3 ambient, half curvature)
{
    // SSS Fringe — garis merah/oranye hangat di perbatasan shadow/lit (khas WuWa & Genshin)
    float sssDist = abs(halfLambert - shadowThresh);
    float sssWidth = max((_SkinSSSRange > 0.001) ? _SkinSSSRange : 0.12, 0.06);
    float sssFringe = saturate(1.0 - (sssDist / sssWidth));
    sssFringe = smoothstep(0.0, 1.0, sssFringe) * max((_SkinSSSIntensity > 0.001) ? _SkinSSSIntensity : 1.3, 0.6);

    half3 shadow1Tint = (_1stShadowColor.a > 0.01 && (_1stShadowColor.r + _1stShadowColor.g + _1stShadowColor.b) > 0.01) ? _1stShadowColor.rgb : half3(0.62, 0.46, 0.44);
    half3 sssCol = (_SkinSSSColor.r + _SkinSSSColor.g + _SkinSSSColor.b > 0.01) ? _SkinSSSColor.rgb : half3(1.0, 0.45, 0.28);

    // Skin shadow — warm-tinted, visible contrast
    float texPres = max(_TexturePreserve, 0.30);
    half3 skinShadow = ApplyShadowTint(albedo, shadow1Tint, texPres);
    skinShadow = max(skinShadow, albedo * 0.16);

    // Warm shadow shift (kulit selalu warm di shadow, ala WuWa)
    skinShadow *= half3(1.12, 0.92, 0.86);

    // Curvature cavity on skin (gentle softening)
    skinShadow *= (1.0 - curvature * _CreaseHighlight * 0.15);

    half3 skinLit = albedo * light.color;

    // SSS Color — garis hangat yang jelas di perbatasan shadow
    half3 sssColor = sssCol * sssFringe * light.color * 0.55;
    sssColor = max(sssColor, half3(0, 0, 0));

    // Blend: shadow -> SSS fringe -> lit
    half3 skinResult = lerp(skinShadow, skinLit, litFactor);
    skinResult += sssColor;

    // Subtle ambient
    skinResult += ambient * 0.06;

    return skinResult;
}

// ══════════════════════════════════════════════════════════════════
//  3. HAIR: Anime Angel Ring (Tenshi no Wa) — "Tuing-Tuing" Dynamic Gloss
//  Khas WuWa / Genshin / Anime AAA:
//  - Anisotropic Specular Band (Kajiya-Kay Tangent Flow)
//  - View-Space Curved Halo yang dinamis mengikuti sudut pandang ("tuing-tuing")
//  - Micro-Strand Jitter (serat helai rambut bergaris alami)
//  - Dual-Layer Ring (lingkaran tajam primer + lingkaran halus sekunder)
//  - 2-Tone cel shadow tajam untuk poni dan ikal rambut
// ══════════════════════════════════════════════════════════════════
half3 CalculateWuWaHair(ToonVaryings input, float3 normalWS, float3 geomNormalWS, float3 viewDirWS, Light light, half3 baseAlbedo, half halfLambert, half litFactor, float offset1, float celStep1, float celStep2, half3 ambient, half curvature)
{
    half3 hairSpecCol = (_HairSpecColor.r + _HairSpecColor.g + _HairSpecColor.b > 0.01) ? _HairSpecColor.rgb : half3(1.0, 1.0, 1.0);
    float hairIntensity = (_HairSpecIntensity > 0.01) ? _HairSpecIntensity : 2.0;

    // 1. Hair Shadows — 2-Tone Anime Cel Shading (Clean, high-contrast bangs & locks)
    half3 shadow1Tint = (_1stShadowColor.a > 0.01 && (_1stShadowColor.r + _1stShadowColor.g + _1stShadowColor.b) > 0.01) ? _1stShadowColor.rgb : half3(0.35, 0.35, 0.40);
    half3 shadow2Tint = (_2ndShadowColor.a > 0.01 && (_2ndShadowColor.r + _2ndShadowColor.g + _2ndShadowColor.b) > 0.01) ? _2ndShadowColor.rgb : (shadow1Tint * 0.55);

    float texPres = max(_TexturePreserve, 0.35);
    half3 cShadow1 = ApplyShadowTint(baseAlbedo, shadow1Tint, texPres);
    half3 cShadow2 = ApplyShadowTint(baseAlbedo, shadow2Tint, texPres);
    cShadow1 = max(cShadow1, baseAlbedo * 0.10);
    cShadow2 = max(cShadow2, baseAlbedo * 0.05);

    // Warm-shift hair shadows slightly for healthy anime hair tone
    cShadow1 *= half3(1.04, 0.96, 0.94);

    half3 hairLit = baseAlbedo * light.color;
    half3 hairDiffuse = lerp(cShadow2, cShadow1, celStep2);
    hairDiffuse = lerp(hairDiffuse, hairLit, litFactor);

    // 2. Dynamic Half-Vector for Hair Specular
    float3 hDir = light.direction + viewDirWS;
    float hLen = length(hDir);
    float3 halfDir = (hLen > 0.0001) ? (hDir / hLen) : normalWS;

    // 3. Tangent-based Kajiya-Kay Anisotropic Highlight
    float3 tangentWS = normalize(input.tangentWS.xyz);
    float3 bitangentWS = normalize(cross(normalWS, tangentWS) * input.tangentWS.w);

    // Shift strand direction along normal (controls vertical height of angel ring on head)
    float shift = _HairSpecShift;
    float3 shiftedStrand = normalize(bitangentWS + normalWS * shift);
    float dotTH = dot(shiftedStrand, halfDir);
    float sinTH = sqrt(max(0.0, 1.0 - dotTH * dotTH));

    float sharpness = max((_HairSpecSharpness > 0.01) ? _HairSpecSharpness : 18.0, 2.0);
    float kajiyaSpec = pow(sinTH, sharpness * 2.5);

    // 4. View-Space / Curvature Curved Halo ("Tuing-Tuing" Effect)
    // Ensures angel ring works brilliantly on any head/hair mesh
    // Glides and rotates dynamically as the camera revolves around the character
    float3 headUpWS = float3(0.0, 1.0, 0.0);
    float ringAlign = dot(normalWS, halfDir + float3(0.0, shift * 0.6, 0.0));
    float haloCurve = saturate(1.0 - abs(ringAlign) * 1.8);
    float haloSpec = pow(haloCurve, max(sharpness * 0.4, 2.0));

    // Combine Kajiya-Kay + Curved Halo
    float combinedSpec = lerp(haloSpec, kajiyaSpec, 0.60);

    // 5. Procedural Hair Strand Jitter (Helai-helai rambut bergaris)
    float uvCoord = input.uv.x * 250.0 + input.uv.y * 30.0;
    float jitter1 = sin(uvCoord);
    float jitter2 = sin(uvCoord * 2.3 + 1.2);
    float strandMod = saturate((jitter1 * 0.35 + jitter2 * 0.25 + 0.65));
    float jitterAmount = saturate((_HairSpecJitter > 0.001) ? _HairSpecJitter : 0.45);
    float modulatedSpec = combinedSpec * lerp(1.0, strandMod, jitterAmount);

    // 6. Stepped Cel Angel Ring (Primary Sharp Ring)
    float bandWidth = max((_HairSpecWidth > 0.001) ? _HairSpecWidth : 0.25, 0.05);
    float ringThresh = 1.0 - bandWidth;
    float ringFeather = 0.06;
    float primaryRing = smoothstep(ringThresh, ringThresh + ringFeather, modulatedSpec);

    // 7. Secondary Softer Ring (Warm Ambient Halo for Layered 3D Depth)
    float secBlend = (_HairSecondaryRing > 0.001) ? _HairSecondaryRing : 0.50;
    float secThresh = ringThresh * 0.75;
    float secondaryRing = smoothstep(secThresh, secThresh + 0.12, combinedSpec) * secBlend * 0.45;

    // 8. Dynamic Light & Lit Mask (Angel ring only visible in lit/specular regions)
    half ringMask = litFactor * saturate(dot(geomNormalWS, light.direction) * 0.5 + 0.5);
    half3 finalAngelRing = hairSpecCol * (primaryRing + secondaryRing) * (hairIntensity * 1.5) * light.color * ringMask;

    // Subtle edge rim for hair silhouettes
    half NdotV = saturate(dot(normalWS, viewDirWS));
    half hairRim = pow(1.0 - NdotV, 3.5) * 0.25 * litFactor;
    hairDiffuse += hairSpecCol * hairRim * light.color;

    // Combine diffuse + Angel Ring
    half3 hairResult = hairDiffuse + finalAngelRing;
    hairResult += ambient * 0.05;

    return hairResult;
}

// ══════════════════════════════════════════════════════════════════
//  FRAGMENT SHADER (Universal Forward) — WuWa v2.0 Overhaul
// ══════════════════════════════════════════════════════════════════
half4 ToonForwardFrag(ToonVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    // ── Albedo & Alpha ──
    half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
    half4 baseCol = (_BaseColor.r + _BaseColor.g + _BaseColor.b + _BaseColor.a > 0.01) ? _BaseColor : half4(1.0, 1.0, 1.0, 1.0);
    half4 albedoColor = baseMap * baseCol;
    half alpha = albedoColor.a;

    #if defined(_ALPHATEST_ON)
        if (_AlphaClip > 0.5)
        {
            clip(alpha - _Cutoff);
        }
    #endif

    // ── World Normal & View Direction ──
    float3 geomNormalWS = normalize(input.normalWS);
    float3 normalWS     = CalculateWorldNormal(input, input.uv);
    float3 camVec       = GetCameraPositionWS() - input.positionWS;
    float camDist       = length(camVec);
    float3 viewDirWS    = (camDist > 0.0001) ? (camVec / camDist) : float3(0.0, 0.0, 1.0);

    // ── Screen-Space Curvature (Lekukan Otomatis, Tanpa AO Map) ──
    half curvature = CalculateScreenCurvature(normalWS);

    // ── Cavity AO Map (Opsional, Tambahan di Atas Curvature) ──
    half cavity = 1.0;
    #if defined(_OCCLUSIONMAP)
        if (_EnableOcclusion > 0.5)
        {
            cavity = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, input.uv).r;
            cavity = lerp(1.0, cavity, _OcclusionStrength);
        }
    #endif

    // ── Pencahayaan Utama (Main Light) ──
    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
        float4 shadowCoord = input.shadowCoord;
    #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
        float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
    #else
        float4 shadowCoord = float4(0, 0, 0, 0);
    #endif

    Light mainLight = GetMainLight(shadowCoord, input.positionWS, half4(1, 1, 1, 1));

    half3 lightColor = (max(mainLight.color.r, max(mainLight.color.g, mainLight.color.b)) > 0.05) ? mainLight.color : half3(1.0, 1.0, 1.0);
    float3 lightDir  = (length(mainLight.direction) > 0.001) ? normalize(mainLight.direction) : float3(0.5, 0.8, 0.3);

    // ── Half-Lambert Diffuse Term (Mulus & Anti-Bercak Normal Map) ──
    float toonNormInfluence = saturate(_NormalToonInfluence);
    if (_EnableNormalMap < 0.5 && _UseNormalMap < 0.5)
    {
        toonNormInfluence = 0.0;
    }
    float3 rampNormalWS = normalize(lerp(geomNormalWS, normalWS, toonNormInfluence));
    half NdotL = dot(rampNormalWS, lightDir);
    half halfLambert = saturate(NdotL * 0.5 + 0.5);

    // ── Curvature + Cavity Deepening pada Half-Lambert ──
    #if defined(_OCCLUSIONMAP)
        if (_EnableOcclusion > 0.5 && _CavityDeepening > 0.01)
        {
            halfLambert = saturate(halfLambert - (1.0 - cavity) * (_CavityDeepening * 0.35));
        }
    #endif

    // Screen-space curvature deepening (selalu aktif jika _CreaseHighlight > 0)
    if (_CreaseHighlight > 0.01)
    {
        halfLambert = saturate(halfLambert - curvature * _CreaseHighlight * 0.20);
    }

    // ── Cel Shadow Ramp (WuWa 3-Tier Style — Crisp & Punchy Toon Contrast) ──
    float offset1 = (_Cel_Shader_Offset > 0.001) ? _Cel_Shader_Offset : ((_ShadowThreshold > 0.001) ? _ShadowThreshold : 0.5);
    float smooth1 = (_Cel_Ramp_Smoothness > 0.001) ? _Cel_Ramp_Smoothness : _ShadowFeather;
    smooth1 = clamp(smooth1, 0.015, 0.20); // Anti-aliased but crisp anime cel boundary
    half celStep1 = smoothstep(offset1 - smooth1 * 0.5, offset1 + smooth1 * 0.5, halfLambert);

    // Deep Shadow Tier 2
    float offset2 = offset1 * 0.50;
    float smooth2 = smooth1 * 1.3;
    half celStep2 = smoothstep(offset2 - smooth2 * 0.5, offset2 + smooth2 * 0.5, halfLambert);

    // Real-time cast shadow — shadow atten meredupkan litFactor
    half shadowAtten = saturate(mainLight.shadowAttenuation);
    half litFactor = celStep1 * lerp(0.35, 1.0, shadowAtten); // Shadow lebih kuat

    // ── Ambient Light (SH) — rendah supaya toon kontras keliatan ──
    half3 shAmbient = SampleSH(geomNormalWS);
    half3 ambient = max(shAmbient, half3(0.12, 0.12, 0.14));

    // ── Tentukan Mode Shading (0: Cloth/Standard, 1: Metal, 2: Skin, 3: Hair) ──
    int matType = (int)_MaterialType;
    #if defined(_MATERIALTYPE_HAIR) || defined(_MATERIAL_HAIR)
        matType = 3;
    #elif defined(_MATERIALTYPE_SKIN) || defined(_MATERIAL_SKIN)
        matType = 2;
    #elif defined(_MATERIALTYPE_METAL) || defined(_MATERIAL_METAL)
        matType = 1;
    #elif defined(_MATERIALTYPE_STANDARD) || defined(_MATERIAL_STANDARD)
        matType = 0;
    #endif

    half3 diffuseColor = half3(0, 0, 0);
    float texPres = saturate(_TexturePreserve);

    if (matType == 3) // ── RAMBUT (WUWA HAIR WITH ANGEL RING "TUING-TUING") ──
    {
        diffuseColor = CalculateWuWaHair(input, rampNormalWS, geomNormalWS, viewDirWS, mainLight, albedoColor.rgb, halfLambert, litFactor, offset1, celStep1, celStep2, ambient, curvature);
    }
    else if (matType == 2) // ── KULIT (WUWA SKIN WITH SSS WARM FRINGE) ──
    {
        diffuseColor = CalculateWuWaSkin(albedoColor.rgb, halfLambert, litFactor, mainLight, offset1, ambient, curvature);

        // Soft organic specular
        float specIntensity = max(_SpecularIntensity, _Specular_Intensity);
        if (specIntensity > 0.01)
        {
            float3 hDir = lightDir + viewDirWS;
            float hLen = length(hDir);
            float3 halfDir = (hLen > 0.0001) ? (hDir / hLen) : rampNormalWS;
            float NdotH = saturate(dot(rampNormalWS, halfDir));
            float skinSpec = pow(NdotH, 16.0) * (specIntensity * 0.10) * litFactor;
            diffuseColor += skinSpec * lightColor;
        }
    }
    else if (matType == 1) // ── METAL (WUWA STYLIZED SHINING METAL) ──
    {
        diffuseColor = CalculateStylizedMetal(rampNormalWS, viewDirWS, mainLight, albedoColor.rgb, litFactor, ambient, curvature);
    }
    else // ══ STANDARD (CLOTH / PROPS / STONE) — Toon Kontras Tinggi ══
    {
        // Shadow Colors — DEFAULT GELAP JELAS (bukan abu-abu tipis!)
        half3 s1 = half3(0.35, 0.35, 0.40); // Default shadow GELAP — toon HARUS keliatan!
        if (_1stShadowColor.a > 0.01 && (_1stShadowColor.r + _1stShadowColor.g + _1stShadowColor.b) > 0.01)
            s1 = _1stShadowColor.rgb;
        else if (_Shading_Color.a > 0.01 && (_Shading_Color.r + _Shading_Color.g + _Shading_Color.b) > 0.01)
            s1 = _Shading_Color.rgb;

        // Auto-contrast guard: jika s1 dari material lama terlalu terang (> 0.50 lum), scale down agar cel terlihat jelas
        half s1Lum = dot(s1, half3(0.2126, 0.7152, 0.0722));
        if (s1Lum > 0.50)
        {
            s1 *= (0.42 / s1Lum);
        }

        half3 s2 = (_2ndShadowColor.a > 0.01 && (_2ndShadowColor.r + _2ndShadowColor.g + _2ndShadowColor.b) > 0.01) ? _2ndShadowColor.rgb : (s1 * 0.55);
        half s2Lum = dot(s2, half3(0.2126, 0.7152, 0.0722));
        if (s2Lum > 0.30)
        {
            s2 *= (0.22 / s2Lum);
        }

        // Apply shadow color shift
        s1 = ShiftShadowColor(s1, _ShadowColorShift);
        s2 = ShiftShadowColor(s2, _ShadowColorShift);

        // ── Area Terang: full texture ──
        half3 cLit = albedoColor.rgb * lightColor;

        // ── Area Bayangan: GELAP & JELAS! (bukan abu-abu tipis) ──
        // Tidak ada sunRef boost — shadow harus benar-benar gelap!
        half3 cShadow1 = ApplyShadowTint(albedoColor.rgb, s1, texPres);
        half3 cShadow2 = ApplyShadowTint(albedoColor.rgb, s2, texPres);

        // Brightness floor RENDAH — toon shadow harus kontras!
        cShadow1 = max(cShadow1, albedoColor.rgb * 0.08);
        cShadow2 = max(cShadow2, albedoColor.rgb * 0.04);

        // Curvature Cavity Darkening (Lekukan Keliatan!)
        if (_CreaseHighlight > 0.01)
        {
            half cavityDarken = 1.0 - (curvature * _CreaseHighlight * 0.40);
            cShadow1 *= cavityDarken;
            cShadow2 *= cavityDarken;
        }

        // Cavity AO Map Darkening (opsional)
        #if defined(_OCCLUSIONMAP)
            if (_EnableOcclusion > 0.5)
            {
                half aoMult = lerp(1.0, cavity, _OcclusionStrength);
                cShadow1 *= aoMult;
                cShadow2 *= aoMult;
            }
        #endif

        // ── Cel Shading Blending — TEGAS (bukan gradien tipis!) ──
        if (_Use_2nd_Shadow > 0.5)
        {
            diffuseColor = lerp(cShadow2, cShadow1, celStep2);
            diffuseColor = lerp(diffuseColor, cLit, litFactor);
        }
        else
        {
            diffuseColor = lerp(cShadow1, cLit, litFactor);
        }

        // Minimal ambient pada shadow (biar shadow tetap gelap)
        diffuseColor += ambient * 0.04 * (1.0 - litFactor);

        // Stepped Anime Specular Highlight
        float specIntensity = max(_SpecularIntensity, _Specular_Intensity);
        if (specIntensity > 0.01)
        {
            float3 hDir = lightDir + viewDirWS;
            float hLen = length(hDir);
            float3 halfDir = (hLen > 0.0001) ? (hDir / hLen) : rampNormalWS;
            float NdotH = saturate(dot(rampNormalWS, halfDir));
            float specPower = lerp(16.0, 128.0, 1.0 - _SpecularRoughness);
            float spec = pow(NdotH, specPower);
            spec = smoothstep(0.35, 0.65, spec) * specIntensity * litFactor;
            diffuseColor += spec * lightColor * lerp(half3(1.0, 1.0, 1.0), albedoColor.rgb, 0.5);
        }
    }

    // ── Stylized Rim Light (Anime Directional Backlight — WuWa Enhanced) ──
    float rimIntensity = max(_RimIntensity, _Rim_Intensity_Legacy);
    if (rimIntensity > 0.01)
    {
        float rimPower = (_Rim_Power_Legacy > 0.1) ? _Rim_Power_Legacy : _RimPower;
        half NdotV = saturate(dot(rampNormalWS, viewDirWS));
        half rim = pow(1.0 - NdotV, max(rimPower, 2.0));

        // Directional mask: rim hanya dari arah backlight (WuWa-style)
        half rimLightMask = saturate(dot(lightDir, -viewDirWS) * 0.5 + 0.5);
        rimLightMask = smoothstep(0.2, 0.8, rimLightMask);

        half3 finalRim = rim * _RimColor.rgb * (rimIntensity * 0.6 * rimLightMask) * lightColor * (litFactor * 0.7 + 0.3);
        diffuseColor += finalRim;
    }

    // ── Additional Lights (Point & Spot Lights) ──
    #if defined(_ADDITIONAL_LIGHTS)
        uint pixelLightCount = GetAdditionalLightsCount();
        for (uint i = 0u; i < pixelLightCount; ++i)
        {
            Light addLight = GetAdditionalLight(i, input.positionWS, half4(1, 1, 1, 1));
            half addNdotL = dot(normalWS, addLight.direction) * 0.5 + 0.5;
            half addTerm = addNdotL * addLight.distanceAttenuation * addLight.shadowAttenuation;
            float aMin = offset1 - 0.1;
            float aMax = offset1 + 0.101;
            half addStep = smoothstep(aMin, aMax, addTerm);
            diffuseColor += albedoColor.rgb * addLight.color * (addStep * 0.6);
        }
    #endif

    // ── HDR Emission ──
    #if defined(_EMISSION)
        half4 emissionMap = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv);
        half3 emission = emissionMap.rgb * _EmissionColor.rgb * _EmissionIntensity;
        diffuseColor += emission;
    #endif

    return half4(diffuseColor, 1.0);
}

// ══════════════════════════════════════════════════════════════════
//  OUTLINE VERTEX SHADER (Inverted Hull)
// ══════════════════════════════════════════════════════════════════
OutlineVaryings ToonOutlineVert(OutlineAttributes input)
{
    OutlineVaryings output = (OutlineVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    if (_OutlineWidth < 0.01)
    {
        output.positionCS = float4(0.0, 0.0, 0.0, 1.0);
        return output;
    }

    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 normalWS   = TransformObjectToWorldNormal(input.normalOS);

    float3 positionVS = TransformWorldToView(positionWS);
    float3 normalVS   = TransformWorldToViewNormal(normalWS, true);

    normalVS.z = -0.15;
    normalVS = normalize(normalVS);

    float distToCam = abs(positionVS.z);
    float widthFactor = lerp(1.0, saturate(distToCam * 0.1), _OutlineDepthFade);
    float outlineOffset = _OutlineWidth * 0.001 * widthFactor;

    float vtxAlpha = input.color.a;
    if (vtxAlpha > 0.01)
    {
        outlineOffset *= vtxAlpha;
    }

    positionVS.xy += normalVS.xy * outlineOffset;

    output.positionCS = TransformWViewToHClip(positionVS);

    #if UNITY_REVERSED_Z
        output.positionCS.z -= _OutlineZOffset * 0.0001;
    #else
        output.positionCS.z += _OutlineZOffset * 0.0001;
    #endif

    output.uv          = TRANSFORM_TEX(input.uv, _BaseMap);
    output.vertexColor = input.color;

    return output;
}

// ── Outline Fragment Shader (v2.0: Albedo-Blended) ──
half4 ToonOutlineFrag(OutlineVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    if (_OutlineWidth < 0.01)
    {
        discard;
        return 0;
    }

    #if defined(_ALPHATEST_ON)
        if (_AlphaClip > 0.5)
        {
            half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
            clip(alpha - _Cutoff);
        }
    #endif

    // v2.0: Outline blended with albedo texture
    half3 finalOutlineColor = _OutlineColor.rgb;

    if (_OutlineAlbedoBlend > 0.01)
    {
        half3 albedoSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;
        half3 darkAlbedo = albedoSample * 0.35;
        finalOutlineColor = lerp(_OutlineColor.rgb, darkAlbedo, _OutlineAlbedoBlend);
    }

    return half4(finalOutlineColor, 1.0);
}

// ── Shadow Caster & Depth Only Helpers ──
struct ShadowAttributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float2 uv           : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct ShadowVaryings
{
    float4 positionCS   : SV_POSITION;
    float2 uv           : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

ShadowVaryings ToonShadowVert(ShadowAttributes input)
{
    ShadowVaryings output = (ShadowVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 normalWS   = TransformObjectToWorldNormal(input.normalOS);

    output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _MainLightPosition.xyz));
    output.uv         = TRANSFORM_TEX(input.uv, _BaseMap);

    return output;
}

half4 ToonShadowFrag(ShadowVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    #if defined(_ALPHATEST_ON)
        if (_AlphaClip > 0.5)
        {
            half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
            clip(alpha - _Cutoff);
        }
    #endif
    return 0;
}

struct DepthOnlyVaryings
{
    float4 positionCS   : SV_POSITION;
    float2 uv           : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

DepthOnlyVaryings ToonDepthOnlyVert(ShadowAttributes input)
{
    DepthOnlyVaryings output = (DepthOnlyVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.uv         = TRANSFORM_TEX(input.uv, _BaseMap);

    return output;
}

half4 ToonDepthOnlyFrag(DepthOnlyVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    #if defined(_ALPHATEST_ON)
        if (_AlphaClip > 0.5)
        {
            half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
            clip(alpha - _Cutoff);
        }
    #endif
    return 0;
}

// ── DepthNormals Helpers (Mandatory for SSAO & Depth Priming) ──
struct DepthNormalsAttributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float2 uv           : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct DepthNormalsVaryings
{
    float4 positionCS   : SV_POSITION;
    float2 uv           : TEXCOORD0;
    float3 normalWS     : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

DepthNormalsVaryings ToonDepthNormalsVert(DepthNormalsAttributes input)
{
    DepthNormalsVaryings output = (DepthNormalsVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.uv         = TRANSFORM_TEX(input.uv, _BaseMap);
    output.normalWS   = TransformObjectToWorldNormal(input.normalOS);

    return output;
}

void ToonDepthNormalsFrag(DepthNormalsVaryings input, out half4 outNormalWS : SV_Target0)
{
    UNITY_SETUP_INSTANCE_ID(input);
    #if defined(_ALPHATEST_ON)
        if (_AlphaClip > 0.5)
        {
            half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
            clip(alpha - _Cutoff);
        }
    #endif

    outNormalWS = half4(NormalizeNormalPerPixel(input.normalWS), 0.0);
}

#endif // WUWA_TOON_CORE_INCLUDED
