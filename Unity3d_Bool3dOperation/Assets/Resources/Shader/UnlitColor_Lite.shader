// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "UnlitColor_Lite"
{
	Properties {
		_MainTex("Texture",2D) = "white" {}
      _Color ("Color1", Color) = (1,1,1,1)
      _Color2 ("Color2", Color) = (1,1,1,1)
      _Rang ("Leight", Vector) = (0,0,0,0)
 //     _tV ("___",  Range (0,1)) = 0.5
 		_tV ("___",  Color) =(0,0,0,1)
    }
    SubShader {

      Tags {      
//          "Queue" = "Opaque"   
//		  "Queue" = "Transparent"
		  "RenderType" = "Opaque" 
//		  "RenderType" = "GrassBillboard" 
//		  "RenderType"="Transparent"
//		  "IgnoreProjector"="True"

      }
//      Cull front
      ZTest NOTEQUAL
//      Offset -1, -1
      LOD 200
//      Offset 10000000, 10000
//      Blend SrcAlpha OneMinusSrcAlpha
//      Blend DstColor Zero
//		ZWrite on
//	AlphaToMask On
//	ZTest Always
//	Depth Only
//	AlwaysOnTop On

      Pass{
	      CGPROGRAM
//	      #include "test.cginc"
	      #pragma vertex vert
	      #pragma fragment frag
	      
		  sampler2D _MainTex;
		  
 //         struct v2f {
//              float2 uv : TEXCOORD0;
//              float4 pos : SV_POSITION;
//          };
	      
        

	      inline float4 UnityObjectToClipPos( in float3 pos ){
			#if defined(UNITY_SINGLE_PASS_STEREO) || defined(UNITY_USE_CONCATENATED_MATRICES)
    		// More efficient than computing M*VP matrix product
   				return mul(UNITY_MATRIX_VP, mul(unity_ObjectToWorld, float4(pos, 1.0)));
			#else
    			return UnityObjectToClipPos(float4(pos, 1.0));
//			  	return mul(UNITY_MATRIX_MVP, float4 (1000,1000,1000,1.0));
			#endif
		  }

//          v2f vert (
//                float4 vertex : POSITION, // vertex position input
//                float2 uv : TEXCOORD0 // first texture coordinate input
//          			)
//          {
//                v2f o;
//                o.pos = UnityObjectToClipPos(vertex);
//                o.uv = uv;
//                return o;
//          }
      
	      fixed4 _Color;
	      fixed4 _Color2;
			fixed4 _tV;
			float4 _Rang;
			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 intensity : COLOR0;
				float4 vertex : SV_POSITION;
			};
			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal: NORMAL;
				float2 uv : TEXCOORD0;
			};

		v2f vert (appdata v)
		{
			v2f o;
			o.vertex = UnityObjectToClipPos(v.vertex);
//			o.uv = TRANSFORM_TEX(v.uv, _MainTex);
//			float4 worldVertex = mul(unity_ObjectToWorld, v.vertex);
//			float3 normWorld = normalize(mul(unity_ObjectToWorld, v.normal));
//			float3 I = normalize(worldVertex - _WorldSpaceCameraPos.xyz);
//			float fresnel = saturate(_Bias + _Scale * pow(1.0 + dot(I, normWorld), _Power));
			if(v.vertex.x ==_Rang.x && v.vertex.y==_Rang.y && v.vertex.z == _Rang.z){
				o.intensity=_Color+fixed4(-0.9,-0.9,-0.9,1);
			}
			else{
				o.intensity=_Color2;
			}
//			o.intensity =v.vertex;//_Rang; //o.vertex;mul(unity_ObjectToWorld, v.vertex);
			return o;
		}


//	      float4 vert(float4 v:POSITION) : SV_POSITION{
//	      	_tV = fixed4(1,0,0,1);//UnityObjectToClipPos(v);
//	      	return UnityObjectToClipPos(v);
//	      }

 //         fixed4 frag (v2f i) : SV_Target
//          {
 //           return lerp(_Color, _Color2, _MainTex.uv.x);//0.5+i.uv.x*i.uv.y);
// 				fixed4 col = tex2D(_MainTex, i.uv)*lerp(_Color, _Color2, 0.5+i.uv.x*i.uv.y);
// 				return col;
//          }
		fixed4 frag (v2f i) : SV_Target{
			// sample the texture
//			fixed4 transparencyMask = tex2D(_MainTex, i.uv);
//			fixed4 dd = lerp(i.intensity, _Color, i.vertex*100);
			fixed4 dd = i.intensity;
			return dd;//fixed4( 0, 0, i.intensity, 0);
		}
//	      fixed4 frag() : SV_Target{
//	      	return lerp(_Color, _Color2,1);
//			return fixed4(_tV.x*1,_tV.y*1,_tV.z*1, 1);
//	      }
	      ENDCG
    	} 
    }
    Fallback "Diffuse"
}

  //Проблема была в alpha:blend.
//У кого такая же проблема, вместо alpha:blend пишите alphatest:_Cutout и да будет вам счастье.
  