Shader "NPR/AnimeCel2D"
{
    Properties
    {
        [Header(Base)]
        [MainTexture] _BaseMap ("Albedo", 2D) = "white" {}
        [MainColor]   _BaseColor ("Tint", Color) = (1,1,1,1)

        [Header(Cel)]
        _Steps          ("Light Bands", Range(1,6)) = 2
        _Threshold      ("Terminator Shift", Range(-0.5,0.5)) = 0
        _EdgeSharp      ("Edge Sharpness", Range(0.001,0.3)) = 0.02
        _ShadowColor    ("Shadow Tint", Color) = (0.45,0.40,0.68,1)
        _ShadowStrength ("Shadow Strength", Range(0,1)) = 0.85
        _AmbientColor   ("Ambient Color", Color) = (0.22,0.24,0.32,1)

        [Header(Rim)]
        _RimColor    ("Rim Color", Color) = (1,0.96,0.88,1)
        _RimPower    ("Rim Power", Range(0.5,8)) = 3
        _RimStrength ("Rim Strength", Range(0,2)) = 0.5
        _RimHardness ("Rim Hardness", Range(0.001,0.4)) = 0.05

        [Header(Specular)]
        _SpecColor    ("Spec Color", Color) = (1,1,1,1)
        _SpecSize     ("Spec Size", Range(0,1)) = 0.96
        _SpecStrength ("Spec Strength", Range(0,3)) = 0

        [Header(Outline)]
        _OutlineColor ("Outline Color", Color) = (0.06,0.04,0.09,1)
        _OutlineWidth ("Outline Width", Range(0,0.05)) = 0.006

        [Header(Style)]
        _Flat ("Flat (matikan lighting)", Range(0,1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Geometry"
        }

        // ================= OUTLINE =================
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor, _ShadowColor, _AmbientColor;
                float4 _RimColor, _SpecColor, _OutlineColor;
                float  _Steps, _Threshold, _EdgeSharp, _ShadowStrength;
                float  _RimPower, _RimStrength, _RimHardness;
                float  _SpecSize, _SpecStrength;
                float  _OutlineWidth, _Flat;
            CBUFFER_END

            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct V { float4 positionCS : SV_POSITION; };

            V vert(A v)
            {
                V o;
                float4 clipPos = TransformObjectToHClip(v.positionOS.xyz);

                // ekstrusi di clip space -> ketebalan konstan di layar
                float3 nWS = TransformObjectToWorldNormal(normalize(v.normalOS));
                float3 nVP = mul((float3x3)UNITY_MATRIX_VP, nWS);
                float2 dir = normalize(nVP.xy + 1e-6);

                clipPos.xy += dir * _OutlineWidth * clipPos.w;
                o.positionCS = clipPos;
                return o;
            }

            half4 frag(V i) : SV_Target { return _OutlineColor; }
            ENDHLSL
        }

        // ================= FORWARD =================
        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   3.0

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor, _ShadowColor, _AmbientColor;
                float4 _RimColor, _SpecColor, _OutlineColor;
                float  _Steps, _Threshold, _EdgeSharp, _ShadowStrength;
                float  _RimPower, _RimStrength, _RimHardness;
                float  _SpecSize, _SpecStrength;
                float  _OutlineWidth, _Flat;
            CBUFFER_END

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            struct A
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct V
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float  fogFactor  : TEXCOORD3;
            };

            // posterize dengan AA berbasis turunan
            half ToonStep(half x, half bands, half sharp)
            {
                half n = max(1.0, bands - 1.0);
                half v = saturate(x) * n;
                half f = frac(v);
                half w = fwidth(v) * 0.5 + sharp;
                return (floor(v) + smoothstep(0.5 - w, 0.5 + w, f)) / n;
            }

            V vert(A v)
            {
                V o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs   n = GetVertexNormalInputs(v.normalOS);

                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS   = n.normalWS;
                o.uv         = TRANSFORM_TEX(v.uv, _BaseMap);
                o.fogFactor  = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 frag(V i) : SV_Target
            {
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb
                             * _BaseColor.rgb;

                float3 N = normalize(i.normalWS);
                float3 V = normalize(GetWorldSpaceViewDir(i.positionWS));

                float4 sc = TransformWorldToShadowCoord(i.positionWS);
                Light  L  = GetMainLight(sc);

                // ---- half-lambert: terminator di tempat yang enak ----
                half nl      = dot(N, L.direction);
                half lambert = saturate(nl * 0.5 + 0.5) + _Threshold;
                half atten   = L.shadowAttenuation * L.distanceAttenuation;

                half lit = ToonStep(saturate(lambert) * atten, _Steps, _EdgeSharp);

                // ---- warna ----
                half3 shadowCol = albedo * lerp(1.0, _ShadowColor.rgb, _ShadowStrength);
                half3 litCol    = albedo * L.color;
                half3 col       = lerp(shadowCol, litCol, lit);

                // ambient rata (bukan SH, biar nggak ada gradasi)
                col += albedo * _AmbientColor.rgb;

                // ---- rim keras ----
                half rim = pow(1.0 - saturate(dot(N, V)), _RimPower);
                rim = smoothstep(0.5 - _RimHardness, 0.5 + _RimHardness, rim);
                col += _RimColor.rgb * rim * _RimStrength * lit;

                // ---- specular titik keras ----
                half3 H = normalize(L.direction + V);
                half  s = smoothstep(_SpecSize, _SpecSize + 0.02, saturate(dot(N, H)));
                col += _SpecColor.rgb * s * _SpecStrength * lit;

                // ---- flat mode ----
                col = lerp(col, albedo, _Flat);

                col = MixFog(col, i.fogFactor);
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}