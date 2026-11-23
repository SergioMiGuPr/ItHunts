// Marquage au sol des attaques du boss (nuit 4, lot 7). Sobre et lisible, à la manière des jeux de boss exigeants :
// bord net (encre rouge sang éteint), remplissage sombre et translucide qui laisse voir le décor (le sol s'assombrit, il n'est
// pas recouvert d'une couleur vive), front qui se remplit pendant l'annonce (du centre vers le bord, ou du boss vers la pointe),
// montée en intensité sur les 40 derniers pour cent, éclair bref et chaud au moment de l'impact.
// Formes : 0 cercle, 1 couloir (rectangle, le boss en bas, v = 0), 2 cône (sommet au boss, en bas au centre), 3 anneau.
// Posé sur un quad à plat (uv 0..1), piloté par MarquageBoss.
Shader "ItHunts/VFX/MarquageSol"
{
    Properties
    {
        [Enum(Cercle,0,Couloir,1,Cone,2,Anneau,3)] _Forme ("Forme", Float) = 0
        _Progression ("Remplissage (0..1)", Range(0, 1)) = 0
        _Intensite ("Intensité (montée avant l'impact)", Range(0, 1)) = 0
        _Flash ("Éclair d'impact", Range(0, 1)) = 0
        _Fondu ("Disparition", Range(0, 1)) = 1
        _DemiAngle ("Cône : demi-angle (degrés)", Range(5, 90)) = 35
        _RayonInterieur ("Anneau : rayon intérieur (0..1)", Range(0, 0.95)) = 0.55
        _Bord ("Épaisseur du bord (fraction du rayon)", Range(0.005, 0.1)) = 0.025
        _CouleurBord ("Bord", Color) = (0.52, 0.07, 0.06, 1)
        _CouleurBordChaud ("Bord à l'impact", Color) = (1.0, 0.78, 0.55, 1)
        _CouleurFond ("Remplissage sombre", Color) = (0.12, 0.014, 0.02, 1)
        _OpaciteFond ("Opacité du remplissage (avant / rempli)", Vector) = (0.3, 0.58, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-10" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Marquage"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            Offset -2, -2
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float _Forme, _Progression, _Intensite, _Flash, _Fondu, _DemiAngle, _RayonInterieur, _Bord;
            float4 _CouleurBord, _CouleurBordChaud, _CouleurFond, _OpaciteFond;
            CBUFFER_END

            struct A { float4 pos : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            V vert(A i) { V o; o.pos = TransformObjectToHClip(i.pos.xyz); o.uv = i.uv; return o; }

            // d : distance signée normalisée (< 0 dedans, 0 au bord) ; p : coordonnée du front (0 départ, 1 fin)
            void Forme(float2 uv, out float d, out float p)
            {
                int f = (int)round(_Forme);
                if (f == 1)
                {
                    float2 q = abs(uv * 2 - 1);
                    d = max(q.x, q.y) - 1;
                    p = uv.y;
                }
                else if (f == 2)
                {
                    float2 c = float2(uv.x * 2 - 1, uv.y);              // sommet en (0, 0), axe vers +v
                    float r = length(c);
                    float a = degrees(atan2(abs(c.x), max(c.y, 1e-4)));
                    float da = (a - _DemiAngle) / max(_DemiAngle, 1) * r;
                    d = max(r - 1, da);
                    p = r;
                }
                else
                {
                    float r = length(uv * 2 - 1);
                    d = r - 1;
                    p = r;
                    if (f == 3) { d = max(d, _RayonInterieur - r); p = (r - _RayonInterieur) / max(1 - _RayonInterieur, 1e-3); }
                }
            }

            half4 frag(V i) : SV_Target
            {
                float d, p;
                Forme(i.uv, d, p);
                float aa = max(fwidth(d), 1e-4) * 1.2;
                float dedans = 1 - smoothstep(-aa, aa, d);
                if (dedans <= 0) discard;
                // bord net : bande de _Bord à l'intérieur du contour
                float bord = smoothstep(-_Bord - aa, -_Bord + aa, d);
                // remplissage : plus sombre derrière le front ; le front lui-même est un fin filet
                float rempli = 1 - smoothstep(_Progression - aa * 2, _Progression + aa * 2, p);
                float front = (1 - smoothstep(0, aa * 3 + 0.004, abs(p - _Progression))) * step(0.001, _Progression) * (1 - step(0.999, _Progression));
                float monte = _Intensite;
                float aFond = lerp(_OpaciteFond.x, _OpaciteFond.y, rempli) * (1 + 0.2 * monte);
                float3 cBord = lerp(_CouleurBord.rgb * (0.75 + 0.6 * monte), _CouleurBordChaud.rgb, _Flash);
                float3 col = _CouleurFond.rgb;
                float a = aFond;
                col = lerp(col, cBord, max(bord, front * 0.7));
                a = lerp(a, 0.85 + 0.15 * monte, max(bord, front * 0.6));
                // éclair : le bord devient chaud, le remplissage rougeoie à peine (jamais une plaque claire)
                col = lerp(col, _CouleurBordChaud.rgb * 0.22, _Flash * 0.3 * (1 - bord));
                return half4(col, saturate(a * dedans * _Fondu));
            }
            ENDHLSL
        }
    }
}
