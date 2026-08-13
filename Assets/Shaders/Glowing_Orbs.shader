Shader "keebar/GlowingOrbs" {
    Properties {
        _Color0 ("CoreColor0", Color) = (1,1,0)
		_Size0("Size of Color0",Range(0,100)) = 1
		_Transparency0("Alpha0",Range(0,1)) = 1
		_Frequency0("Frequency0",Range(0,100)) = 1
		_Amplitude0("Amplitude0",Range(0,10)) = 0.3
		_Color1 ("CoreColor1", Color) = (1,0,1)
		_Size1("Size of Color1",Range(0,100)) = 1
		_Transparency1("Alpha1",Range(0,1)) = 1
		_Frequency1("Frequency1",Range(0,100)) = 1
		_Amplitude1("Amplitude1",Range(0,10)) = 0.3
		_Color2 ("CoreColor2", Color) = (0,1,1)
		_Size2("Size of Color2",Range(0,100)) = 1
		_Transparency2("Alpha2",Range(0,1)) = 1
		_Frequency2("Frequency2",Range(0,100)) = 1
		_Amplitude2("Amplitude2",Range(0,10)) = 0.3
		_VirtualPos("centerPos",Vector) = (0,0,0)
		//_Size("Size",Range(0,100)) = 1
		_GlowSharpness("Glow Sharpness",Range(0,2)) = 0.25
		_Speed("Slow Down",Range(0,1)) = 1
		_Seed("Random Seed",Range(0,100)) = 12.345
		_RandomWithPos("Random with Pos&Angle",Range(0,1)) = 0
		
		[KeywordEnum(Low, Medium, High)] _NumberOfOrbs ("Number of orbs", Float) = 0
		
		[Enum(UnityEngine.Rendering.BlendMode)] _ScrBlend1 ("_ScrBlend1", Float) = 5
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend1 ("_DstBlend1", Float) = 1
		
    }
    SubShader {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass {
            // 设置光照模式为前向渲染基础 Pass
            Tags { "LightMode"="ForwardBase" }
			
			BlendOp add
			Blend [_ScrBlend1] [_DstBlend1]
			
			/*Stencil
			{
				Ref 128//Reference Value
				ReadMask 128
				WriteMask 128
				Comp greater  //Comparison Function
				Pass Replace
				//Fail Replace
				//ZFail Replace
			}*/
			
			ZWrite off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma multi_compile _NUMBEROFORBS_LOW _NUMBEROFORBS_MEDIUM _NUMBEROFORBS_HIGH
			#pragma multi_compile_instancing
            #include "UnityCG.cginc" // 包含常用的内置变量和宏
            #include "Lighting.cginc" // 包含光照计算相关的变量

            struct appdata {
                float4 vertex : POSITION;
                //float2 uv : TEXCOORD0;
                //float3 normal : NORMAL; 
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 pos : SV_POSITION;
				float4 screenPos : TEXCOORD0;
				#if defined(_NUMBEROFORBS_LOW)
					float4 virtualSreenPos[3] : TEXCOORD1;
				#elif defined(_NUMBEROFORBS_MEDIUM)
					float4 virtualSreenPos[6] : TEXCOORD1;
				#elif defined(_NUMBEROFORBS_HIGH)
					float4 virtualSreenPos[11] : TEXCOORD1;
				#endif
					
				//float3 randomDir : TEXCOORD2;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex;
            fixed3 _Color0;
			float _Size0;
			fixed3 _Color1;
			float _Size1;
			fixed3 _Color2;
			float _Size2;
			float3 _VirtualPos;
			float _Frequency0;
			float _Frequency1;
			float _Frequency2;
			float _Amplitude0;
			float _Amplitude1;
			float _Amplitude2;
			//float _Size;
			float _Seed;
			float _Speed;
			fixed _RandomWithPos;
			float _GlowSharpness;
			fixed _Transparency0;
			fixed _Transparency1;
			fixed _Transparency2;
			
			
			/*float3 hash(float2 p) 
			{
				return float3(frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453123),
								frac(sin(dot(p + fixed2(1,1), float2(12.9898, 78.233))) * 43758.5453123),
								frac(sin(dot(p + fixed2(2,2), float2(12.9898, 78.233))) * 43758.5453123));
			}*/
			
			uint pcg_hash(uint seed)
			{
				// 1. 状态转换（线性同余 LCG 步骤）
				// 使用大质数增加数据的混乱度
				uint state = seed * 747796405u + 2891336453u;
				
				// 2. 置换输出（PCG 的核心：通过位移和异或进一步打乱）
				uint word = ((state >> ((state >> 28u) + 4u)) ^ state) * 277803737u;
				uint result = (word >> 22u) ^ word;
    
				return result;
			}

			// 将生成的 uint 映射到 [0.0, 1.0] 的浮点数
			float3 pcg_01(uint seed)
			{
				// 4294967296.0 是 2的32次方
				return float3((pcg_hash(seed)),(pcg_hash(seed + 1)),(pcg_hash(seed + 2)))/4294967296.0;
			}

            v2f vert (appdata v) {
                v2f o;
				
				 UNITY_SETUP_INSTANCE_ID(v);
				 UNITY_TRANSFER_INSTANCE_ID(v, o);
				
				// 将坐标从模型空间转换到裁剪空间
				o.pos = UnityObjectToClipPos(v.vertex);
				
				//转到屏幕空间齐次坐标
				o.screenPos = ComputeScreenPos(o.pos);
				
				uint posSeed = (uint)(dot(float3(unity_ObjectToWorld[0][3],unity_ObjectToWorld[1][3],unity_ObjectToWorld[2][3]),
								float3(unity_ObjectToWorld[0][3],unity_ObjectToWorld[1][3],unity_ObjectToWorld[2][3]))*10000);
				uint angleSeed = (uint)(dot(float3(unity_ObjectToWorld[0][0],unity_ObjectToWorld[1][1],unity_ObjectToWorld[2][2]),
								float3(unity_ObjectToWorld[0][0],unity_ObjectToWorld[1][1],unity_ObjectToWorld[2][2]))*10000);//mul(unity_ObjectToWorld,float4(0,0,0,1));//UnityObjectToClipPos(float4(0,0,0,1));
				
				//生成伪随机数
				uint posAngleSeed = (posSeed + angleSeed)*_RandomWithPos;
				float randomPhaseDiff = pcg_hash(posAngleSeed)/4294967296.0;
				#if defined(_NUMBEROFORBS_LOW)
					int numOfOrbs = 3;
				#elif defined(_NUMBEROFORBS_MEDIUM)
					int numOfOrbs = 6;
				#else
					int numOfOrbs = 11;
				#endif
				float Amplitude[3] = {_Amplitude0,_Amplitude1,_Amplitude2};
				float Frequency[3] = {_Frequency0,_Frequency1,_Frequency2};
				for(int i = 0; i < numOfOrbs; i++)
				{
				}
				for(int i = 0; i < numOfOrbs; i++)
				{
					int k = i%3;
					
					float time = _Time.y*Frequency[k]*_Speed + i*0.3 + randomPhaseDiff; //当_Time.y*Frequency[k] + i + randomPhaseDiff = PI时sine值为0
					float timeStep = floor(time);
					uint seed = (uint)timeStep + (uint)(_Seed*i) + posAngleSeed;
					float3 randomDir0 = pcg_01(seed)*2 - 1;
					float3 randomDir1 = pcg_01(seed + 1)*2 - 1;
					
					float3 virtualOffset = sin(time*UNITY_PI)*lerp(randomDir0,randomDir1,frac(time))*Amplitude[k];
					
					// 将坐标从模型空间转换到裁剪空间
					float4 virtualPos = UnityObjectToClipPos(_VirtualPos + virtualOffset);
					
					
					//转到屏幕空间齐次坐标
					o.virtualSreenPos[i] = ComputeScreenPos(virtualPos);
				}
				
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
				//虚拟点发光计算
				float scale = UNITY_MATRIX_P[0][0];
				fixed3 colors[3] = {_Color0,_Color1,_Color2};
				float Size[3] = {_Size0,_Size1,_Size2};
				fixed transparency[3] = {_Transparency0,_Transparency1,_Transparency2};
				fixed4 glowingColor = fixed4(0,0,0,0);
				
				#if defined(_NUMBEROFORBS_LOW)
					int numOfOrbs = 3;
				#elif defined(_NUMBEROFORBS_MEDIUM)
					int numOfOrbs = 6;
				#elif defined(_NUMBEROFORBS_HIGH)
					int numOfOrbs = 11;
				#endif
				//return fixed4(numOfOrbs/11.0f,numOfOrbs/11.0f,numOfOrbs/11.0f,1);
				
				for(int j = 0; j < numOfOrbs; j++)
				{
					float2 posDifference = (i.screenPos.xy/i.screenPos.w) - (i.virtualSreenPos[j].xy/i.virtualSreenPos[j].w);
					posDifference.x = posDifference.x*(_ScreenParams.x/_ScreenParams.y);
					
					int k = j%3;
					
					float screenDistanceSquare = dot(posDifference,posDifference)*i.virtualSreenPos[j].w*i.virtualSreenPos[j].w/(scale*scale*Size[k]*Size[k])*32;
					
					float virtualLightIntensity = 1/max(screenDistanceSquare,0.001);
					virtualLightIntensity = smoothstep(0,_GlowSharpness,virtualLightIntensity)*virtualLightIntensity;
					
					glowingColor += fixed4(colors[k]*virtualLightIntensity,transparency[k]*saturate(virtualLightIntensity));
				}
                
                return saturate(glowingColor);
            }
            ENDCG
        }
    }
    FallBack "VertexLit"
}