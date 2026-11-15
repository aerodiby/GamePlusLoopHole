Shader "NPR/GBAScreen"
{
    Properties
    {
        _MainTex ("Screen RT", 2D) = "white" {}

        [Header(Color)]
        _Tint       ("Tint",          Color)         = (0.88, 0.95, 1.0, 1)
        _Wash       ("Wash Out",      Range(0,1))    = 0.55
        _Contrast   ("Contrast",      Range(0,2))    = 0.95
        _Brightness ("Brightness",    Range(0,2))    = 1.15
        _Levels     ("Color Levels",  Range(2,64))   = 32
        _Dither     ("Dither",        Range(0,1))    = 0.6

        [Header(LCD)]
        _GridSize  ("Pixel Grid",    Float)          = 240
        _GridDark  ("Grid Darkness", Range(0,1))     = 0.18
        _Scanline  ("Scanline",      Range(0,1))     = 0.10
        _Ghost     ("LCD Ghosting",  Range(0,1))     = 0.35

        [Header(State)]
        _Power  ("Power",      Range(0,1)) = 1
        _Glitch ("Glitch",     Range(0,1)) = 0
        _Roll   ("V-Roll",     Range(0,1)) = 0
        _Seed   ("Seed",       Float)      = 0
        _Off    ("Screen Off", Range(0,1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Overlay"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Tint;
                float  _Wash, _Contrast, _Brightness, _Levels, _Dither;
                float  _GridSize, _GridDark, _Scanline, _Ghost;
                float  _Power, _Glitch, _Roll, _Seed, _Off;
            CBUFFER_END

            struct A { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            V vert(A v)
            {
                V o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv         = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float Hash11(float n) { return frac(sin(n * 127.1) * 43758.5453); }
            float Bayer2(float2 a) { a = floor(a); return frac(a.x * 0.5 + a.y * a.y * 0.75); }
            #define Bayer4(a) (Bayer2(0.5 * (a)) * 0.25 + Bayer2(a))

            half4 frag(V i) : SV_Target
            {
                float2 uv = i.uv;

                // ---- glitch: pita sobek + roll vertikal ----
                float band = floor(uv.y * 30.0);
                float r    = Hash11(band + _Seed);
                uv.x += (r - 0.5) * 0.12 * _Glitch * step(0.75, r);
                uv.y  = frac(uv.y + _Roll * 0.35);

                // ---- sampling + chroma split ----
                float  sp = _Glitch * 0.010;
                float3 c;
                c.r = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( sp, 0)).r;
                c.g = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).g;
                c.b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2( sp, 0)).b;

                // ---- ghosting LCD (smear murah) ----
                float2 dx = float2(_MainTex_TexelSize.x, 0) * 1.5;
                float2 dy = float2(0, _MainTex_TexelSize.y) * 1.5;
                float3 bl = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + dx).rgb
                          + SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - dx).rgb
                          + SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + dy).rgb
                          + SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - dy).rgb;
                c = lerp(c, bl * 0.25, _Ghost * 0.5);

                // ---- color grading GBA (washed out, sedikit kebiruan) ----
                c = (c - 0.5) * _Contrast + 0.5;
                c = c * _Brightness;
                c = lerp(c, 0.30 + c * 0.50, _Wash);   // lift black
                c *= _Tint.rgb;

                // ---- dither + quantize 15-bit (32 level) ----
                float2 px = floor(uv * _GridSize);
                c += (Bayer4(px) - 0.5) / _Levels * _Dither;
                c  = floor(saturate(c) * _Levels + 0.5) / _Levels;

                // ---- pixel grid ----
                float2 g = frac(uv * _GridSize);
                float  grid = saturate(step(0.85, g.x) + step(0.85, g.y));
                c *= 1.0 - grid * _GridDark;

                // ---- scanline ----
                c *= 1.0 - (0.5 + 0.5 * sin(uv.y * _GridSize * 3.14159)) * _Scanline;

                // ---- power on: garis terang membuka ----
                float halfH  = _Power * 0.5;
                float inside = step(abs(uv.y - 0.5), halfH);
                float glow   = smoothstep(0.03, 0.0, abs(abs(uv.y - 0.5) - halfH))
                             * (1.0 - _Power);
                c = c * inside + float3(0.90, 1.0, 1.0) * glow;

                // ---- layar mati ----
                c = lerp(c, float3(0.02, 0.03, 0.04), _Off);

                return half4(c, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}