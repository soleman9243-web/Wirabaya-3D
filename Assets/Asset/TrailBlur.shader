Shader "Custom/URP/TrailBlur"
{
    Properties
    {
        _BlurStrength ("Blur Strength", Range(0, 0.05)) = 0.01
    }
    SubShader
    {
        Tags { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
        }
        LOD 100

        Pass
        {
            Name "BlurPass"
            
            // Standard transparent blending
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // Include URP core library
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR; // Mengambil warna/alpha dari TrailRenderer
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float4 color : COLOR;
            };

            // Texture layar dari URP (wajib aktifkan Opaque Texture di pengaturan URP)
            TEXTURE2D_X(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            CBUFFER_START(UnityPerMaterial)
                float _BlurStrength;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                // Konversi posisi dari 3D ke layar
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.screenPos = ComputeScreenPos(output.positionCS);
                
                // Teruskan warna/alpha dari TrailRenderer ke fragment
                output.color = input.color; 
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Hitung koordinat UV layar
                float2 uv = input.screenPos.xy / input.screenPos.w;
                
                half4 col = half4(0,0,0,0);
                float weightSum = 0;
                
                // Kita gunakan 25 titik sampel (5x5 grid) agar blurnya halus (tidak 'burik')
                // Offset diperkecil sedikit agar titik-titiknya lebih rapat
                float offset = _BlurStrength * 0.5; 
                
                for (int x = -2; x <= 2; x++)
                {
                    for (int y = -2; y <= 2; y++)
                    {
                        float2 offsetUV = float2(x, y) * offset;
                        
                        // Memberikan bobot lebih besar untuk piksel di tengah (Gaussian approximation)
                        float weight = 1.0 - (length(float2(x, y)) * 0.25); 
                        if(weight < 0) weight = 0.1;
                        
                        col += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv + offsetUV) * weight;
                        weightSum += weight;
                    }
                }
                
                // Rata-ratakan warna berdasarkan total bobot
                col /= weightSum;
                
                // Return warna blur dengan tingkat transparansi mengikuti Alpha dari TrailRenderer
                return half4(col.rgb, input.color.a);
            }
            ENDHLSL
        }
    }
}
