// ItHunts : lumière additive peinte (nuit du 2026-11-22). Deux usages :
// - rayon de lune (volume allongé, uv.y = 0 à la fenêtre, 1 au sol) : s'efface vers le sol (_FadeLength), sur les bords
//   vus de biais (_EdgeSoft) et dans le brouillard ; bandes de poussière lumineuse qui descendent lentement (_Dust) ;
// - poussière en suspension (particules) : texture × couleur des sommets × _Color, sans effet de rayon (_Rayon = 0).
// Aplats : l'intensité est posterisée (_Steps) pour rester dans le rendu peint. Additif, sans profondeur écrite.
Shader "ItHunts/Lumiere"
{
    Properties
    {
        _MainTex ("Texture (particules ; blanc pour le rayon)", 2D) = "white" {}
        [HDR] _Color ("Couleur", Color) = (0.55, 0.58, 0.62, 1)
        _Intensity ("Intensité", Range(0, 4)) = 0.6
        [Toggle] _Rayon ("Rayon (fondus de longueur et de bord)", Float) = 1
        _FadeLength ("Rayon : fondu vers le sol (0 aucun, 1 fort)", Range(0, 1)) = 0.65
        _EdgeSoft ("Rayon : fondu des bords vus de biais", Range(0, 4)) = 1.6
        _Dust ("Rayon : bandes de poussière", Range(0, 1)) = 0.35
        _DustSpeed ("Rayon : vitesse de descente de la poussière", Range(0, 2)) = 0.15
        [IntRange] _Steps ("Paliers (0 = continu)", Range(0, 6)) = 3
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+10" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Lumiere"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _Intensity, _Rayon, _FadeLength, _EdgeSoft, _Dust, _DustSpeed, _Steps;
            CBUFFER_END

            struct A { float4 pos : POSITION; float3 n : NORMAL; float2 uv : TEXCOORD0; half4 col : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 cs : SV_POSITION; float2 uv : TEXCOORD0; half4 col : COLOR; float3 ws : TEXCOORD1; float3 n : TEXCOORD2; half fog : TEXCOORD3; UNITY_VERTEX_OUTPUT_STEREO };

            V Vert(A i)
            {
                V o;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.ws = TransformObjectToWorld(i.pos.xyz);
                o.cs = TransformWorldToHClip(o.ws);
                o.n = TransformObjectToWorldNormal(i.n);
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                o.col = i.col;
                o.fog = ComputeFogFactor(o.cs.z);
                return o;
            }

            float H(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float N(float2 p) { float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f);
                return lerp(lerp(H(i), H(i + float2(1, 0)), f.x), lerp(H(i + float2(0, 1)), H(i + 1), f.x), f.y); }

            half4 Frag(V i, bool face : SV_IsFrontFace) : SV_Target
            {
                half4 t = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half k = t.a * i.col.a;
                if (_Rayon > 0.5)
                {
                    half len = lerp(1.0h, saturate(1.0h - i.uv.y), _FadeLength);
                    float3 vd = normalize(GetWorldSpaceViewDir(i.ws));
                    half edge = pow(saturate(abs(dot(normalize(i.n), vd))), _EdgeSoft);
                    float dust = N(float2(i.ws.x * 1.3 + i.ws.z * 1.1, i.ws.y * 0.9 + _Time.y * _DustSpeed * 3.0)) * 0.6
                               + N(float2(i.ws.x * 4.1 - i.ws.z * 3.7, i.ws.y * 2.6 + _Time.y * _DustSpeed * 7.0)) * 0.4;
                    half d = lerp(1.0h, 0.55h + dust * 0.9h, _Dust);
                    k = len * edge * d;
                    // bord haut net (fenêtre) adouci sur 3 % de la longueur
                    k *= smoothstep(0.0, 0.03, i.uv.y);
                }
                if (_Steps > 0.5) k = floor(k * _Steps + 0.5) / _Steps;
                half3 c = t.rgb * i.col.rgb * _Color.rgb * _Intensity * k;
                // l'additif s'efface dans le brouillard (vers le noir, pas vers la couleur du brouillard)
                c *= ComputeFogIntensity(i.fog);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
