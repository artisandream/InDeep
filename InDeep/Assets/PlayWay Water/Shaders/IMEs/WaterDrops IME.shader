Shader "PlayWay Water/IME/Water Drops" {
	Properties {
		_MainTex ("", 2D) = "" {}
		_NormalMap ("Overlay", 2D) = "" {}
		_Intensity ("Intensity", Float) = 0.5
	}
	SubShader { 
		Pass {			// 0
 			ZTest Always Cull Off ZWrite Off
			//ZTest Off Cull Off ZWrite Off Blend Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			sampler2D _MainTex;
			sampler2D _NormalMap;

			half _Intensity;

			struct appdata_t {
				float4 vertex : POSITION;
				half2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				half2 texcoord0 : TEXCOORD0;
				half2 texcoord1 : TEXCOORD1;
			};

			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.texcoord0 = v.texcoord.xy;
				o.texcoord1 = v.texcoord.xy;
				o.texcoord1.y *= max(0.85, _ScreenParams.y / _ScreenParams.x);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				half3 normal = UnpackNormal(tex2D(_NormalMap, i.texcoord1));

				fixed4 color = tex2D(_MainTex, i.texcoord0 + normal * _Intensity);

				return color;
			}
			ENDCG 
		}
	}

	Fallback Off 
}
