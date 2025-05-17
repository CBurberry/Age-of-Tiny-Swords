Shader "Hidden/FogBrush"
{
    Properties {
        _MainTex ("Source", 2D) = "white" {}
        _Center ("Center", Vector) = (0.5, 0.5, 0, 0)
        _Size ("Size", Float) = 0.1
    }
    SubShader {
        Tags { "RenderType" = "Opaque" }
        Pass {
            ZTest Always Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _Center;
            float _Size;

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata_base v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                fixed4 col = tex2D(_MainTex, i.uv);
                float dimFactor = 0.25;
                float dist = distance(i.uv, _Center.xy);
                col.rgb *= dimFactor;
                return dist < _Size ? col : 0;
            }
            ENDCG
        }
    }
}