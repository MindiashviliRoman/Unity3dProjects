// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "LineGradient2" {
	Properties {
		_Color1 ("Color1", Color) = (1,1,1,1)
		_Color2 ("Color2", Color) = (1,1,1,1)
		_Magnitude ("Leght", float) = 0
		_Pos1 ("Pos1", Vector) = (0,0,0,0)
		_Pos2 ("Pos2", Vector) = (0,0,0,0)
		_Manipule("Value", Range(0,1)) = 0.99
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
			float _Magnitude;
			float3 _Pos1;
			float _Manipule;
			struct v2f{
				float4 intensity : COLOR0;
				float4 vertex : SV_POSITION;
			};
			struct appdata{
				float4 vertex : POSITION;
			};
//			inline float4 UnityObjectToClipPos( in float3 pos ){
//				#if defined(UNITY_SINGLE_PASS_STEREO) || defined(UNITY_USE_CONCATENATED_MATRICES)
//					return mul(UNITY_MATRIX_VP, mul(unity_ObjectToWorld, float4(pos, 1.0)));
//				#else
//					return UnityObjectToClipPos(float4(pos, 1.0));
//				#endif
//			}
			
			v2f vert (appdata v){
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
//				o.intensity = lerp(_Color1, _Color2, length(v.vertex-_Pos1)/_Magnitude);//pow(2*_Manipule*length(v.vertex-_Pos1)/_Magnitude,2-2*_Manipule));
				o.intensity = lerp(_Color1, _Color2, pow(2*_Manipule*length(v.vertex-_Pos1)/_Magnitude,2-2*_Manipule));
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
