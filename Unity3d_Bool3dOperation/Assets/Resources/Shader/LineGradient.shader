// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "LineGradient" {
	Properties {
		_Color1 ("Color1", Color) = (1,1,1,1)
      	_Color2 ("Color2", Color) = (1,1,1,1)
     	_Rang ("Pose", Vector) = (0,0,0,0)
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		
		Pass{
			CGPROGRAM
		    #pragma vertex vert
		    #pragma fragment frag

			fixed4 _Color1;
		    fixed4 _Color2;
			float4 _Rang;
			struct v2f{
//				float2 uv : TEXCOORD0;
				float4 intensity : COLOR0;
				float4 vertex : SV_POSITION;
			};
			struct appdata{
				float4 vertex : POSITION;
//				float3 normal: NORMAL;
//				float2 uv : TEXCOORD0;
			};
			
			
			inline float4 UnityObjectToClipPos( in float3 pos ){
				#if defined(UNITY_SINGLE_PASS_STEREO) || defined(UNITY_USE_CONCATENATED_MATRICES)
		   			return mul(UNITY_MATRIX_VP, mul(unity_ObjectToWorld, float4(pos, 1.0)));
				#else
		    		return UnityObjectToClipPos(float4(pos, 1.0));
				#endif
			}
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				if(v.vertex.x ==_Rang.x && v.vertex.y==_Rang.y && v.vertex.z == _Rang.z){
					o.intensity=_Color1;
				}
				else{
					o.intensity=_Color2;
				}
				return o;
			}

			fixed4 frag (v2f i) : SV_Target{
				return i.intensity;
			}
			ENDCG
		}
	} 
	FallBack "Diffuse"
}
