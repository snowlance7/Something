Shader "UI/BreathVignette"
{
    Properties
    {
        _LightColor ("Light Color", Color) = (0.6,0.9,1,0.9)
        _DarkColor ("Dark Color", Color) = (0,0,0,0.9)
        _DarknessAmount ("Darkness Amount", Range(0,1)) = 0.5
        _LightSize ("Light Size", Range(0,1)) = 0.25
        _EdgeFeather ("Edge Feather", Range(0.01,0.5)) = 0.15
        _CenterOffset ("Center Offset", Vector) = (0.5,0.5,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            fixed4 _LightColor;
            fixed4 _DarkColor;
            float _DarknessAmount;
            float _LightSize;
            float _EdgeFeather;
            float4 _CenterOffset; // .xy is center in normalized screen space

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Normalized UV (0..1)
                float2 uv = i.uv;

                // distance from center (normalized)
                float2 center = _CenterOffset.xy;
                float2 d = uv - center;
                float dist = length(d);

                // Darkness mask: grows from edges inward. When _DarknessAmount==0 => no darkness.
                // We'll compute a radial radius that defines the light area; darkness outside it.
                float lightRadius = _LightSize; // inner radius of light
                float darknessEdge = lerp(0.5, 0.0, _DarknessAmount); // tweak mapping: higher darkness -> smaller safe radius

                // We want darkness strength as function of dist: near center = light, near edges = dark
                // Compute a smoothstep between lightRadius and 1.0 (screen far edge)
                float dToEdge = dist; // normalized 0..~1.4 depends on center position
                float feather = _EdgeFeather;
                float darkMix = smoothstep(lightRadius + feather, 0.995, dToEdge); // 0=center, 1=edge
                // Bias by _DarknessAmount
                darkMix = saturate(darkMix * _DarknessAmount);

                // Compose colors: blue light at center, dark at edges; but we want scene to show through so alpha used
                fixed4 colLight = _LightColor;
                fixed4 colDark = _DarkColor;

                // final alpha: how much overlay should darken the scene
                float alpha = darkMix * colDark.a; // darker near edges

                // compute additive light factor at center to push darkness back
                float lightFactor = smoothstep(lightRadius, lightRadius - feather*0.7, dist);
                lightFactor = 1.0 - saturate(lightFactor);
                lightFactor *= colLight.a * (1.0 - _DarknessAmount); // when darkness is huge, center light has to fight

                // final overlay color: lerp between light and dark by darkMix
                fixed4 overlayColor = lerp(colLight, colDark, darkMix);
                overlayColor.a = alpha;

                // Additive bluish glow in center: output color premultiplied for UI blending
                overlayColor.rgb = overlayColor.rgb * overlayColor.a + colLight.rgb * lightFactor;

                return overlayColor;
            }
            ENDCG
        }
    }
}