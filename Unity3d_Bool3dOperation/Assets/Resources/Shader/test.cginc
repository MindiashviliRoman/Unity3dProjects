// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

inline float4 UnityObjectToClipPos( in float3 pos ){
	#if defined(UNITY_SINGLE_PASS_STEREO) || defined(UNITY_USE_CONCATENATED_MATRICES)
    // More efficient than computing M*VP matrix product
    	return mul(UNITY_MATRIX_VP, mul(unity_ObjectToWorld, float4(pos, 1.0)));
	#else
    	return UnityObjectToClipPos(float4(pos, 1.0));
//    	return float4 (0.5,0.5,0.5,1);
	#endif
}


/*
inline float4 UnityObjectToClipPos( in float3 pos ){
	#if defined(UNITY_SINGLE_PASS_STEREO) || defined(UNITY_USE_CONCATENATED_MATRICES)
    // More efficient than computing M*VP matrix product
    	return mul(UNITY_MATRIX_VP, mul(unity_ObjectToWorld, float4(pos, 1.0)));
	#else
    	return mul(UNITY_MATRIX_MVP, float4(pos, 1.0));
	#endif
}
*/