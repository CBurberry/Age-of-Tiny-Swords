Shader "Custom/FogOfWar2D"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _FogTex ("Fog Texture", 2D) = "black" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _FogTex;

            struct appdata_t {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 fogColor = tex2D(_FogTex, i.uv);
                fixed4 revealedAreaColor = tex2D(_MainTex, i.uv);
                float fog = fogColor.r;
                float revealedArea = revealedAreaColor.r;
                float revealedAreaAlpha = step(0.1, revealedArea);
    
                fixed4 colorToUse = lerp(fogColor, fixed4(0, 0, 0, 0), revealedAreaAlpha);
                return colorToUse;
            }
            ENDCG
        }
    }
}