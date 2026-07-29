Shader "Custom/URP_AnimeTrail"
{
    Properties
    {
        _MainTex ("Main Texture (Anime Shape)", 2D) = "white" {}
        _NoiseTex ("Noise Texture (Erosion)", 2D) = "white" {}
        [HDR] _Color ("Emission Color", Color) = (1,1,1,1)
        _PanSpeed ("Panning Speed", Vector) = (1, 0, 0, 0)
        _ErosionPower ("Erosion Power", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha // Alpha blending
        ZWrite Off
        Cull Off // Don't cull backfaces so the trail is visible from all angles

        Pass
        {
            Name "Unlit"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
                float fogCoord      : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _NoiseTex_ST;
                float4 _Color;
                float2 _PanSpeed;
                float _ErosionPower;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                
                // Calculate panning UVs
                o.uv = v.uv * _MainTex_ST.xy + _MainTex_ST.zw + (_Time.y * _PanSpeed);
                
                // Pass vertex colors (TrailRenderer uses this to fade out the tail)
                o.color = v.color;
                o.fogCoord = ComputeFogFactor(o.positionHCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // Sample main texture
                half4 mainCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                
                // Sample noise texture with panning
                float2 noiseUV = i.uv * _NoiseTex_ST.xy + _NoiseTex_ST.zw + (_Time.y * _PanSpeed);
                half noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;
                
                float vertexAlpha = i.color.a;
                
                // Agar warna gelap (seperti biru tua) tidak ikut transparan, kita buat
                // semua warna yang bukan hitam murni (0,0,0) menjadi solid 1.0.
                half shapeMask = saturate((mainCol.r + mainCol.g + mainCol.b) * 10.0);
                half alpha = shapeMask * vertexAlpha;
                
                // Logika erosi sederhana
                float noiseMask = saturate(noise + vertexAlpha - 0.2); 
                alpha *= lerp(1.0, noiseMask, _ErosionPower);
                
                // Emission color dari tekstur utama dikalikan dengan HDR Color dan Vertex Color
                half3 finalColor = mainCol.rgb * _Color.rgb * i.color.rgb;
                
                half4 col = half4(finalColor, alpha);
                
                // Apply fog
                col.rgb = MixFog(col.rgb, i.fogCoord);
                
                return col;
            }
            ENDHLSL
        }
    }
}
