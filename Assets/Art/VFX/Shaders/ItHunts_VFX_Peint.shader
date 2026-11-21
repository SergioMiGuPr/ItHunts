// ItHunts : effets peints (particules, traînées, liens). La texture code la forme (R = ton : 0 encre, 1/3 sombre, 2/3 moyen,
// 1 cœur ; G = bruit de dissolution ; A = couverture) ; la couleur vient d'une rampe de 3 tons + encre, en aplats (pas de
// dégradé), réglable par matériau ou par bloc de propriétés. La couleur de sommet (couleur des particules au fil de la vie)
// teinte la rampe ; son alpha ne fait pas un fondu : il dissout la forme par morceaux (G), comme un trait qui se défait.
// _Intensite pousse les tons clairs au-delà de 1 (lueur par le bloom) ; l'encre reste noire. _Defilement fait glisser la
// texture (liens, traces). Mélange : alpha (par défaut) ou additif (_Additif = 1 : encre ignorée, pour les lueurs).
Shader "ItHunts/VFX/Peint"
{
    Properties
    {
        _MainTex ("Forme (R ton, G dissolution, A couverture)", 2D) = "white" {}
        _Encre ("Encre", Color) = (0.03, 0.02, 0.03, 1)
        [HDR] _Ton1 ("Ton sombre", Color) = (0.55, 0.12, 0.05, 1)
        [HDR] _Ton2 ("Ton moyen", Color) = (1.0, 0.45, 0.1, 1)
        [HDR] _Ton3 ("Cœur", Color) = (1.0, 0.9, 0.55, 1)
        _Intensite ("Intensité des tons clairs (bloom)", Range(0, 8)) = 1.5
        _EncreForce ("Force de l'encre (0 = pas de trait)", Range(0, 1)) = 1
        _Defilement ("Défilement uv (x, y par seconde)", Vector) = (0, 0, 0, 0)
        _Coupe ("Seuil de couverture", Range(0, 1)) = 0.5
        [Toggle] _Additif ("Additif (lueur, sans encre)", Float) = 0
        [HideInInspector] _SrcBlend ("", Float) = 5
        [HideInInspector] _DstBlend ("", Float) = 10
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Faces", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "PreviewType" = "Plane" }

        Pass
        {
            Name "Peint"
            Tags { "LightMode" = "UniversalForward" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Encre, _Ton1, _Ton2, _Ton3;
                half _Intensite, _EncreForce, _Coupe, _Additif;
                float4 _Defilement;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float fog : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex) + _Defilement.xy * _Time.y;
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                half4 t = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                // couverture nette (1 px adouci par la texture) et dissolution par morceaux
                half cov = smoothstep(_Coupe - 0.15, _Coupe + 0.15, t.a);
                half dis = step(1.0 - i.color.a, t.g * 0.98 + 0.01);
                half a = cov * dis;
                clip(a - 0.001);
                // rampe en aplats : 0 encre, 1 sombre, 2 moyen, 3 cœur
                half k = round(saturate(t.r) * 3.0);
                half3 ton = k < 0.5 ? _Encre.rgb : (k < 1.5 ? _Ton1.rgb : (k < 2.5 ? _Ton2.rgb : _Ton3.rgb * _Intensite));
                if (k > 1.5 && k < 2.5) ton *= lerp(1.0, _Intensite, 0.5);
                half encre = k < 0.5 ? 1.0 : 0.0;
                if (encre > 0.5) ton = lerp(_Ton1.rgb, _Encre.rgb, _EncreForce);
                half3 c = encre > 0.5 ? ton : ton * i.color.rgb;
                if (_Additif > 0.5)
                {
                    // lueur : pas d'encre, couleur prémultipliée par la couverture
                    a *= (1.0 - encre);
                    c = ton * i.color.rgb * a;
                    c = MixFogColor(c, half3(0, 0, 0), i.fog);
                    return half4(c, a);
                }
                c = MixFog(c, i.fog);
                return half4(c, a);
            }
            ENDHLSL
        }
    }
}
