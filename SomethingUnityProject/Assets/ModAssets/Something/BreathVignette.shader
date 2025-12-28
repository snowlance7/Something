Shader "UI/BreathEdgeVignette"
{
    Properties
    {
        // Unity UI expects these:
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _LightColor ("Light Color", Color) = (0.6,0.9,1,0.85)

        // 0 = only very edge; 1 = reaches all the way to center
        _Inset ("Inset (inward reach)", Range(0,1)) = 0.25

        _Feather ("Feather", Range(0.001,0.5)) = 0.15
        _Intensity ("Intensity", Range(0,3)) = 1.0
        _CenterOffset ("Center Offset", Vector) = (0.5,0.5,0,0)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            fixed4 _LightColor;
            float _Inset;
            float _Feather;
            float _Intensity;
            float4 _CenterOffset;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Respect UI sprite alpha/tint (keeps UI system happy)
                fixed4 baseCol = tex2D(_MainTex, i.uv) * i.color;

                // Normalize distance so corners ~= 1.0
                float2 center = _CenterOffset.xy;
                float dist = length(i.uv - center);
                float maxDist = 0.70710678; // distance from center to corner in UV space
                float d = saturate(dist / maxDist); // 0 center, 1 edges/corners

                // Edge vignette: 0 until inner threshold, then ramps to 1 near edges
                // inner = how close to edge it starts; bigger _Inset -> starts closer to center
                float inner = saturate(1.0 - _Inset);
                float feather = max(0.001, _Feather);

                float edgeMask = smoothstep(inner, inner + feather, d); // 0 center, 1 edges

                float alpha = edgeMask * _LightColor.a * _Intensity * baseCol.a;

                fixed4 outCol = fixed4(_LightColor.rgb * alpha, alpha);
                return outCol;
            }
            ENDCG
        }
    }
}
