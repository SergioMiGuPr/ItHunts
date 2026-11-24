// Vitre qui montre l'extérieur (nuit 5, lot 3) : le paysage peint (ciel étoilé, lune, vallée) est lu selon la direction du
// regard, comme s'il était à l'infini : la vitre ne montre jamais le mur ou la salle qui se trouvent derrière la fenêtre.
// Panorama cylindrique : u = cap (en miroir au-delà de la largeur couverte : aucune couture), v = hauteur angulaire.
// Teinte du vitrail (verre ancien coloré), saleté et épaisseur du verre (bruit), légère lueur de la lune, brouillard du jeu.
Shader "ItHunts/VueExterieure"
{
    Properties
    {
        [NoScaleOffset] _Panorama ("Panorama (paysage peint)", 2D) = "black" {}
        _Couverture ("Largeur couverte par le panorama (degrés)", Range(30, 360)) = 150
        _Cap ("Cap du centre du panorama (degrés, monde)", Range(-180, 180)) = 0
        _HauteurBas ("Hauteur angulaire du bas de l'image (degrés)", Range(-60, 0)) = -28
        _HauteurHaut ("Hauteur angulaire du haut de l'image (degrés)", Range(10, 90)) = 52
        _Exposition ("Exposition", Range(0, 3)) = 0.85
        _Teinte ("Teinte du verre", Color) = (0.82, 0.88, 1, 1)
        _TeinteForce ("Force de la teinte", Range(0, 1)) = 0.35
        _Salete ("Saleté du verre", Range(0, 1)) = 0.35
        _SaleteEchelle ("Saleté : taille des taches (m)", Float) = 0.35
        _Ondes ("Verre soufflé : déformation", Range(0, 0.05)) = 0.012
        _Ciel ("Haut de la vue qui luit (ciel, lune)", Range(0, 2)) = 0.6
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Vue"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_Panorama); SAMPLER(sampler_Panorama);
            CBUFFER_START(UnityPerMaterial)
                float _Couverture, _Cap, _HauteurBas, _HauteurHaut, _Exposition, _TeinteForce, _Salete, _SaleteEchelle, _Ondes, _Ciel;
                float4 _Teinte;
            CBUFFER_END

            struct A { float4 positionOS : POSITION; };
            struct V { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float fog : TEXCOORD1; };

            V vert(A i)
            {
                V o;
                o.positionWS = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            float h21(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }
            float bruit(float2 p)
            {
                float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f);
                return lerp(lerp(h21(i), h21(i + float2(1, 0)), f.x), lerp(h21(i + float2(0, 1)), h21(i + float2(1, 1)), f.x), f.y);
            }

            half4 frag(V i) : SV_Target
            {
                float3 d = normalize(i.positionWS - GetCameraPositionWS());
                // verre soufflé : le regard est un peu dévié selon la position sur la vitre
                float2 w = i.positionWS.xz + i.positionWS.yy * 0.7;
                d.xz += (float2(bruit(w * 2.1), bruit(w * 2.1 + 7.3)) - 0.5) * _Ondes;
                d.y += (bruit(i.positionWS.xy * 1.7 + 3.1) - 0.5) * _Ondes;
                d = normalize(d);
                float cap = degrees(atan2(d.x, d.z)) - _Cap;
                cap = cap - 360.0 * round(cap / 360.0);
                float u = cap / _Couverture + 0.5;
                u = 1.0 - abs(frac(u * 0.5) * 2.0 - 1.0);           // miroir au-delà de la largeur couverte
                float haut = degrees(asin(clamp(d.y, -1, 1)));
                float v = saturate((haut - _HauteurBas) / (_HauteurHaut - _HauteurBas));
                half3 c = SAMPLE_TEXTURE2D_LOD(_Panorama, sampler_Panorama, float2(u, v), 0).rgb;
                c *= _Exposition * (1.0 + _Ciel * smoothstep(0.55, 1.0, v) * 0.5);
                // verre ancien : teinte, taches de saleté (plus denses en bas)
                c = lerp(c, c * _Teinte.rgb, _TeinteForce);
                float s = bruit(i.positionWS.xy / max(_SaleteEchelle, 0.01)) * 0.6 + bruit(i.positionWS.zy / max(_SaleteEchelle, 0.01) * 2.3) * 0.4;
                c *= 1.0 - _Salete * smoothstep(0.35, 0.9, s) * 0.55;
                c = MixFog(c, i.fog);
                return half4(c, 1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ColorMask R
            Cull Off
            HLSLPROGRAM
            #pragma vertex v2
            #pragma fragment f2
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 v2(float4 p : POSITION) : SV_POSITION { return TransformObjectToHClip(p.xyz); }
            half f2() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
