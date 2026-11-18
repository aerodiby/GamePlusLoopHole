Shader "Retro/GBA 2D Look (URP)"
{
    Properties
    {
        _BaseMap ("Sprite Texture", 2D) = "white" {}
        _BaseColor ("Tint", Color) = (1,1,1,1)

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
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local_fragment _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float  _Bands;
                float4 _ShadowTint;
                float  _ColorBits;
                float  _Dither;
                float4 _OutlineColor;
                float  _OutlineWidth;
                float  _OutlineStrength;
                float  _Cutoff;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            static const float bayer4[16] =
            {
                 0.0,  8.0,  2.0, 10.0,
                12.0,  4.0, 14.0,  6.0,
                 3.0, 11.0,  1.0,  9.0,
                15.0,  7.0, 13.0,  5.0
            };

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 viewDirWS  : TEXCOORD2;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                VertexPositionInputs pos = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs   nrm = GetVertexNormalInputs(v.normalOS);

                o.positionCS = pos.positionCS;
                o.uv         = TRANSFORM_TEX(v.uv, _BaseMap);
                o.normalWS   = nrm.normalWS;
                o.viewDirWS  = GetWorldSpaceViewDir(pos.positionWS);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);

                #ifdef _ALPHATEST_ON
                    clip(tex.a - _Cutoff);
                #endif

                // ---------- Flat / banded shading ----------
                half3 lighting = half3(1,1,1);

                if (_Bands > 1.001)
                {
                    Light  mainLight = GetMainLight();
                    float  ndl = saturate(dot(normalize(i.normalWS), mainLight.direction));

                    float steps = _Bands - 1.0;
                    ndl = floor(ndl * steps + 0.5) / steps;

                    lighting = lerp(_ShadowTint.rgb, half3(1,1,1), ndl);
                }

                half3 col = tex.rgb * _BaseColor.rgb * lighting;

                // ---------- Silhouette outline (rim) ----------
                float rim  = 1.0 - saturate(dot(normalize(i.normalWS), normalize(i.viewDirWS)));
                float edge = smoothstep(_OutlineWidth, _OutlineWidth + 0.15, rim);
                col = lerp(col, _OutlineColor.rgb, edge * _OutlineStrength);

                // ---------- GBA color depth: 5 bit per channel ----------
                float levels = exp2(_ColorBits) - 1.0;
                int2  p      = (int2)floor(i.positionCS.xy);
                float d      = (bayer4[(p.y & 3) * 4 + (p.x & 3)] + 0.5) / 16.0;

                col = saturate(col);
                col = floor(col * levels + lerp(0.5, d, _Dither)) / levels;

                return half4(col, tex.a * _BaseColor.a);
            }
            ENDHLSL
        }
    }
}
