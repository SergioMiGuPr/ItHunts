// Marquage au sol des attaques du boss et des apparitions (nuit 4, lot 7). Sobre et lisible, à la manière des jeux de boss
// exigeants : bord net rouge sang éteint d'épaisseur constante EN MÈTRES (12 cm, au moins 1,5 px à l'écran), remplissage
// rouge sang sombre et translucide (le sol rougit et s'assombrit, il n'est jamais couvert d'une plaque claire), zone déjà
// « remplie » un peu plus chaude que la zone à venir, avec un front en dégradé doux (pas un second bord), montée en
// intensité sur les 40 derniers pour cent (remplissage plus chaud qui bat de plus en plus vite, bord qui s'épaissit),
// éclair d'impact porté par le bord. Seules les faces tournées vers le haut sont dessinées (quad, dessus du cylindre).
// Formes : 0 cercle, 1 couloir (rectangle, le boss en bas, -Z local), 2 cône (sommet au boss, en bas au centre), 3 anneau.
// Coordonnées prises dans l'espace de l'objet (x, z de -0.5 à 0.5) : marche sur le quad de MarquageBoss comme sur le cylindre
// aplati de WaveDirector (apparitions), sans dépendre des UV. La taille en mètres est lue dans la matrice de l'objet.
Shader "ItHunts/VFX/MarquageSol"
{
    Properties
    {
        [Enum(Cercle,0,Couloir,1,Cone,2,Anneau,3)] _Forme ("Forme", Float) = 0
        _Progression ("Remplissage (0..1)", Range(0, 1)) = 0
        _Intensite ("Intensité (montée avant l'impact)", Range(0, 1)) = 0
        _Flash ("Éclair d'impact", Range(0, 1)) = 0
        _Fondu ("Disparition", Range(0, 1)) = 1
        _Pulsation ("Pulsation (Hz, 0 : aucune ; marques d'apparition)", Range(0, 4)) = 0
        _DemiAngle ("Cône : demi-angle (degrés)", Range(5, 90)) = 35
        _RayonInterieur ("Anneau : rayon intérieur (0..1)", Range(0, 0.95)) = 0.55
        _BordM ("Épaisseur du bord (m)", Range(0.02, 0.5)) = 0.12
        _FrontM ("Largeur du dégradé du front (m)", Range(0.1, 4)) = 1.2
        _CouleurBord ("Bord", Color) = (0.62, 0.09, 0.07, 1)
        _CouleurBordChaud ("Bord à l'impact", Color) = (1.0, 0.72, 0.48, 1)
        _CouleurFond ("Remplissage : zone à venir", Color) = (0.07, 0.01, 0.012, 1)
        _CouleurRempli ("Remplissage : zone remplie", Color) = (0.24, 0.035, 0.03, 1)
        _OpaciteFond ("Opacité (à venir / remplie)", Vector) = (0.38, 0.6, 0, 0)
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
            float _Forme, _Progression, _Intensite, _Flash, _Fondu, _Pulsation, _DemiAngle, _RayonInterieur, _BordM, _FrontM;
            float4 _CouleurBord, _CouleurBordChaud, _CouleurFond, _CouleurRempli, _OpaciteFond;
            CBUFFER_END

            struct A { float4 pos : POSITION; float3 n : NORMAL; };
            struct V { float4 pos : SV_POSITION; float2 xz : TEXCOORD0; float2 taille : TEXCOORD1; float haut : TEXCOORD2; };

            V vert(A i)
            {
                V o;
                o.pos = TransformObjectToHClip(i.pos.xyz);
                o.xz = i.pos.xz;                                             // -0.5..0.5
                o.haut = i.n.y;                                              // cylindre de WaveDirector : seul le dessus compte
                float4x4 m = GetObjectToWorldMatrix();
                o.taille = float2(length(float3(m._m00, m._m10, m._m20)), length(float3(m._m02, m._m12, m._m22)));
                return o;
            }

            // d : distance signée au contour en mètres (< 0 dedans) ; p : avancée du front (0 départ, 1 fin) ; L : longueur du front (m)
            void Forme(float2 xz, float2 T, out float d, out float p, out float L)
            {
                int f = (int)round(_Forme);
                if (f == 1)
                {
                    float2 h = T * 0.5;
                    float2 q = abs(xz) * T;                                  // mètres depuis le centre
                    d = max(q.x - h.x, q.y - h.y);
                    p = xz.y + 0.5;
                    L = T.y;
                }
                else if (f == 2)
                {
                    float P = T.y;                                           // portée ; largeur du quad = 2 P
                    float2 c = float2(xz.x * T.x, (xz.y + 0.5) * T.y);       // sommet en (0, 0), axe vers +z
                    float r = length(c);
                    float a = atan2(abs(c.x), max(c.y, 1e-4));
                    float da = r * sin(clamp(a - radians(_DemiAngle), -1.5, 1.5));
                    d = max(r - P, da);
                    p = r / P;
                    L = P;
                }
                else
                {
                    float R = 0.5 * T.x;
                    float r = length(xz) * 2;                                // 0..1 au rayon
                    d = (r - 1) * R;
                    p = r;
                    L = R;
                    if (f == 3)
                    {
                        d = max(d, (_RayonInterieur - r) * R);
                        p = (r - _RayonInterieur) / max(1 - _RayonInterieur, 1e-3);
                        L = R * (1 - _RayonInterieur);
                    }
                }
            }

            half4 frag(V i) : SV_Target
            {
                if (i.haut < 0.5) discard;                                   // dessous et flancs du cylindre : pas de double disque
                float d, p, L;
                Forme(i.xz, i.taille, d, p, L);
                float aa = max(fwidth(d), 1e-4);
                float dedans = 1 - smoothstep(-aa, aa, d);
                if (dedans <= 0) discard;
                float monte0 = saturate(_Intensite);
                float bordM = max(_BordM * (1 + 0.7 * monte0), aa * 1.5);    // le bord s'épaissit pendant la montée (12 -> 20 cm)
                float bord = smoothstep(-bordM - aa, -bordM + aa, d);
                // front en dégradé doux : 1 dans la zone remplie, 0 devant, transition de _FrontM mètres
                float largeur = max(_FrontM / max(L, 0.1), 1e-3);
                float rempli = _Progression >= 0.999 ? 1 : 1 - smoothstep(_Progression - largeur, _Progression, p);
                float puls = _Pulsation > 0 ? 0.5 + 0.5 * sin(_Time.y * 6.2831 * _Pulsation) : 0;
                float monte = saturate(_Intensite + 0.35 * puls);
                // montée : le remplissage se réchauffe nettement et bat de plus en plus vite (3 -> 10 Hz)
                float bat = monte0 * 0.3 * (0.5 + 0.5 * sin(_Time.y * 6.2831 * (3 + 7 * monte0)));
                float3 fond = lerp(_CouleurFond.rgb, _CouleurRempli.rgb * (1 + 1.2 * monte + bat), rempli);
                float aFond = lerp(_OpaciteFond.x, _OpaciteFond.y, rempli) * (1 + 0.2 * monte + bat);
                float3 cBord = lerp(_CouleurBord.rgb * (0.8 + 0.5 * monte), _CouleurBordChaud.rgb, _Flash);
                float3 col = lerp(fond, cBord, bord);
                float a = lerp(aFond, 0.9 + 0.1 * monte, bord);
                // éclair : le bord devient chaud, le remplissage rougeoie (jamais une plaque claire)
                col = lerp(col, float3(0.42, 0.11, 0.05), _Flash * 0.5 * (1 - bord));
                return half4(col, saturate(a * dedans * _Fondu));
            }
            ENDHLSL
        }
    }
}
