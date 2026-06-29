Shader "Neko Legends/Cel Shader/Glass Unity6"
{
    Properties
    {
        _Color("Glass Color", Color) = (1,1,1,1)
        _Transparency("Transparency", Range(0,1)) = 0.8
        _Refraction("Refraction Strength", Range(0,0.1)) = 0.02
        _Diffuse("Diffuse Texture", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        _NormalStrength("Normal Strength", Range(0,2)) = 1.0
        _Scale("Texture Scale", Float) = 1.0
        _Cloudiness("Cloudiness (0 = clear, 1 = textured)", Range(0,1)) = 1.0
        _FresnelStrength("Fresnel Strength", Range(0,100)) = 1.0
    }
        SubShader
        {
            Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
            LOD 300

            Pass
            {
                Name "FORWARD"
                Tags { "LightMode" = "UniversalForward" }
                Blend SrcAlpha OneMinusSrcAlpha
                Cull Off
                ZWrite Off

                HLSLPROGRAM
            // Require at least shader model 3.0.
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            // --------------------------------------------------
            // Structures

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float3 worldPos  : TEXCOORD2;
                float3 worldNormal : TEXCOORD3;
            };

            // --------------------------------------------------
            // Uniforms

            float4 _Color;
            float _Transparency;
            float _Refraction;
            sampler2D _Diffuse;
            float4 _Diffuse_ST;
            sampler2D _NormalMap;
            float4 _NormalMap_ST;
            float _NormalStrength;
            float _Scale;
            float _Cloudiness;
            float _FresnelStrength;

            // The opaque texture (set in URP; ensure “Opaque Texture” is enabled).
            sampler2D _CameraOpaqueTexture;
            // _WorldSpaceCameraPos is automatically provided by Unity—do not redefine it.

            // --------------------------------------------------
            // Vertex Shader

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.pos);
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.worldPos = worldPos.xyz;
                o.worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                o.uv = TRANSFORM_TEX(v.uv, _Diffuse) * _Scale;
                return o;
            }

            // --------------------------------------------------
            // Fragment Shader

            float4 frag(v2f i) : SV_Target
            {
                // Sample the diffuse texture.
                float4 diffSample = tex2D(_Diffuse, i.uv);
                // Use _Cloudiness to blend between a pure white diffuse (clear glass) and the textured diffuse.
                float4 diff = lerp(float4(1,1,1,1), diffSample, _Cloudiness);

                // Sample the normal map and convert from [0,1] to [-1,1].
                float3 normSample = tex2D(_NormalMap, TRANSFORM_TEX(i.uv, _NormalMap)).xyz * 2.0 - 1.0;
                normSample = normalize(normSample);

                // Smoothly interpolate between the “front” and “back” normals.
                float dotViewNormal = dot(normalize(_WorldSpaceCameraPos - i.worldPos), i.worldNormal);
                float blendFactor = smoothstep(-0.1, 0.1, dotViewNormal);
                float3 smoothNormal = normalize(lerp(-i.worldNormal, i.worldNormal, blendFactor));
                // Blend with the normal map.
                float3 finalNormal = normalize(lerp(smoothNormal, normSample, _NormalStrength));

                // Compute the view direction.
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);

                // Calculate the refraction vector using refract() (using 0.66 as the index ratio for air-to-glass).
                float3 refractVec = refract(-viewDir, finalNormal, 0.66);
                float2 offset = refractVec.xy * _Refraction;

                // Compute screen-space UVs.
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                screenUV = clamp(screenUV, 0.001, 0.999);
                float2 refractUV = screenUV + offset;

                // Sample the background (opaque) texture.
                float4 bgColor = tex2D(_CameraOpaqueTexture, refractUV);

                // Compute a Fresnel term.
                float NdotV = saturate(dot(viewDir, finalNormal));
                float fresnel = pow(1.0 - NdotV, 3.0) * _FresnelStrength;
                fresnel = clamp(fresnel, 0.0, 0.8);

                // Blend the clear (or cloudy) diffuse with the refracted background.
                float4 col = lerp(diff, bgColor, fresnel);
                col *= _Color;
                col.a = _Transparency;
                return col;
            }
            ENDHLSL
        }
        }
            FallBack "Transparent/VertexLit"
                CustomEditor "NekoLegends.CelShaderInspectorGlass"

}
