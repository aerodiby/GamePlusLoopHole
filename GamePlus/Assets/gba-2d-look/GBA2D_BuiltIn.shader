Shader "Retro/GBA 2D Look (Built-in)"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Flat Shading)]
        _Bands ("Shading Bands (1 = flat / unlit)", Range(1,6)) = 1
        _ShadowTint ("Shadow Tint", Color) = (0.55,0.55,0.75,1)

        [Header(GBA Color Depth)]
        _ColorBits ("Bits per Channel (GBA = 5)", Range(2,8)) = 5
        _Dither ("Dither", Range(0,1)) = 0

        [Header(Outline)]
        _OutlineColor ("Outline Color", Color) = (0.05,0.05,0.12,1)
        _OutlineWidth ("Outline Width", Range(0,1)) = 0.35
        _OutlineStrength ("Outline Strength", Range(0,1)) = 1

        [Header(Alpha)]
        [Toggle(_ALPHATEST_ON)] _AlphaTest ("Alpha Test (cutout)", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma shader_feature _ALPHATEST_ON
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float  _Bands;
            fixed4 _ShadowTint;
            float  _ColorBits;
            float  _Dither;
            fixed4 _OutlineColor;
            float  _OutlineWidth;
            float  _OutlineStrength;
            float  _Cutoff;

            static const float bayer4[16] =
            {
                 0.0,  8.0,  2.0, 10.0,
                12.0,  4.0, 14.0,  6.0,
                 3.0, 11.0,  1.0,  9.0,
                15.0,  7.0, 13.0,  5.0
            };

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos       : SV_POSITION;
                float2 uv        : TEXCOORD0;
                float3 normalWS  : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos       = UnityObjectToClipPos(v.vertex);
                o.uv        = TRANSFORM_TEX(v.uv, _MainTex);
                o.normalWS  = UnityObjectToWorldNormal(v.normal);
                o.viewDirWS = WorldSpaceViewDir(v.vertex);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);

                #ifdef _ALPHATEST_ON
                    clip(tex.a - _Cutoff);
                #endif

                // ---------- Flat / banded shading ----------
                // _Bands == 1  -> tidak ada lighting sama sekali (paling "2D")
                // _Bands 2..5  -> cel shading bertingkat
                half3 lighting = half3(1,1,1);

                if (_Bands > 1.001)
                {
                    float3 n    = normalize(i.normalWS);
                    float3 lDir = normalize(_WorldSpaceLightPos0.xyz);
                    float  ndl  = saturate(dot(n, lDir));

                    float steps = _Bands - 1.0;
                    ndl = floor(ndl * steps + 0.5) / steps;   // hard steps, no gradient

                    lighting = lerp(_ShadowTint.rgb, half3(1,1,1), ndl);
                }

                half3 col = tex.rgb * _Color.rgb * lighting;

                // ---------- Silhouette outline (rim) ----------
                float rim  = 1.0 - saturate(dot(normalize(i.normalWS), normalize(i.viewDirWS)));
                float edge = smoothstep(_OutlineWidth, _OutlineWidth + 0.15, rim);
                col = lerp(col, _OutlineColor.rgb, edge * _OutlineStrength);

                // ---------- GBA color depth: 5 bit per channel ----------
                float levels = exp2(_ColorBits) - 1.0;
                int2  p      = (int2)floor(i.pos.xy);
                float d      = (bayer4[(p.y & 3) * 4 + (p.x & 3)] + 0.5) / 16.0;

                col = saturate(col);
                col = floor(col * levels + lerp(0.5, d, _Dither)) / levels;

                return float4(col, tex.a * _Color.a);
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
