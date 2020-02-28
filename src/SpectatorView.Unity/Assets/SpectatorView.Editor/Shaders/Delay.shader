Shader "SV/Delay"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_PrevTex ("Previous Texture", 2D) = "black" {}
		_PercPrev ("Percentage of Previous Frame", Float) = 0.3
		_Threshold ("Threshold", Float) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
			sampler2D _PrevTex;
			float4 _PrevTex_ST;
			float _PercPrev;
			float _Threshold;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float depth = tex2D(_MainTex, i.uv).r;
				float trueDepth = depth * 65535 * 0.001; // Incoming texture is normalized R16
				float prevDepth = tex2D(_PrevTex, i.uv).r;
				float truePrevDepth = prevDepth * 65535 * 0.001; // Incoming texture is normalized R16

				return depth;
				//if (trueDepth > 1 && truePrevDepth < 1)
				//{
				//	return depth;
				//}
				//else if (trueDepth > 1  && abs(trueDepth - truePrevDepth) > _Threshold)
				//{
				//	return depth;
				//}
				//else
				//{
				//	float addedDepth = trueDepth < 1 ? 1.0 : depth;
				//	return (_PercPrev * prevDepth) + ((1.0 - _PercPrev) * addedDepth);
				//}
            }
            ENDCG
        }
    }
}
