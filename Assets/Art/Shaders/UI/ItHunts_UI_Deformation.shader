// ItHunts : mouvement « sur place » des personnages et des artefacts peints, par déformation de l'image (quelques
// pixels) : respiration (étirement vertical autour de la ligne _Pivot, uv y), cape qui ondule (décalage horizontal qui
// grandit vers le bas), flottement (décalage vertical du graphique), orbite des traînées de lumière (rotation dans une
// ellipse). Poids : texture _Poids (R respiration, G cape, B phase) si _POIDS_TEXTURE, sinon profil vertical.
// Gelé (_ItHuntsMenuFige = 1) : aucune déformation, le rendu est l'image peinte.
// Éclairage de scène (_Eclairage = 1, personnages assis du campement) : valeur et saturation ramenées à celles des
// personnages de la maquette, ombres froides bleu-violet, côté du feu plus chaud et clair, côté opposé sombre et froid,
// liseré orange sur les bords tournés vers le feu (palpite avec lui), lueur de la lanterne sur les jambes. Les positions
// du feu et de la lanterne sont en uv du sprite (posées par CampementApercu selon la place). Gelé : rien ne palpite.
// Fondu (2026-11-22) : _Douceur floute le sprite (couleur prémultipliée et alpha, 9 prises en tente) pour égaler le grain
// de la peinture et adoucir le contour de 1 px ; le liseré du feu ne vient que sur les bords dont la normale (gradient de
// l'alpha) regarde le feu ; _GardeLumiere laisse intactes les parties lumineuses (verre de la lanterne) ; _Detail atténue
// le contraste des détails fins (encre) sans changer leur finesse.
Shader "ItHunts/UI/Deformation"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Teinte", Color) = (1, 1, 1, 1)
        [Toggle(_POIDS_TEXTURE)] _UsePoids ("Poids par texture", Float) = 0
        _Poids ("Poids (R respiration, G cape, B phase)", 2D) = "white" {}
        _Pivot ("Ligne fixe de la respiration (uv y)", Range(0, 1)) = 0.2
        _Respiration ("Respiration (pixels au sommet)", Range(0, 6)) = 1.6
        _Rythme ("Respirations par seconde", Range(0, 1)) = 0.22
        _Cape ("Cape (pixels)", Range(0, 6)) = 1.2
        _CapeHaut ("Début de la cape (depuis le haut, 0..1)", Range(0, 1)) = 0.45
        _Phase ("Phase", Range(0, 1)) = 0
        _Flottement ("Flottement (pixels)", Range(0, 20)) = 0
        _FlotteVitesse ("Flottement (cycles par seconde)", Range(0, 2)) = 0.35
        _Orbite ("Orbite (tours par seconde, 0 = aucune)", Range(-0.5, 0.5)) = 0
        _Centre ("Centre de l'orbite (uv x y), aplatissement (z)", Vector) = (0.5, 0.5, 0.45, 0)
        _Opaque ("Opaque (recadrage du fond)", Range(0, 1)) = 0

        _Eclairage ("Éclairage de scène (0 ou 1)", Range(0, 1)) = 0
        _Luminosite ("Luminosité", Range(0, 2)) = 1
        _Saturation ("Saturation", Range(0, 2)) = 1
        _Contraste ("Contraste (exposant sur la luminance : noirs plus profonds)", Range(0.5, 3)) = 1
        _OmbreCouleur ("Ombres : teinte froide", Color) = (0.34, 0.33, 0.62, 1)
        _OmbreForce ("Ombres : force de la teinte", Range(0, 1)) = 0.5
        _OmbreSeuil ("Ombres : luminance haute (linéaire)", Range(0.01, 0.5)) = 0.12
        _FeuUV ("Feu (uv x y du sprite)", Vector) = (2, 0.3, 0, 0)
        _ChaudCouleur ("Côté feu : gain chaud", Color) = (1.25, 1.02, 0.82, 1)
        _FroidCouleur ("Côté opposé : gain froid", Color) = (0.62, 0.66, 0.86, 1)
        _Transition ("Largeur de la transition chaud-froid (uv)", Range(0.05, 2)) = 0.7
        _LisereCouleur ("Liseré du feu", Color) = (1, 0.48, 0.16, 1)
        _LisereForce ("Liseré : force", Range(0, 3)) = 0.9
        _LisereLargeur ("Liseré : largeur (pixels du sprite)", Range(0.5, 6)) = 2.2
        _Palpite ("Palpitation avec le feu", Range(0, 1)) = 0.35
        _PalpiteVitesse ("Palpitation : vitesse", Range(0, 4)) = 1.1
        _LanterneUV ("Lanterne (uv x y du sprite, rayon en uv y)", Vector) = (0.5, 0.1, 0.25, 0)
        _LanterneCouleur ("Lanterne : couleur", Color) = (1, 0.8, 0.5, 1)
        _LanterneForce ("Lanterne : force sur les jambes", Range(0, 3)) = 0.8
        _Douceur ("Douceur (rayon du flou, pixels du sprite)", Range(0, 3)) = 0
        _Detail ("Contraste des détails fins (1 = celui du sprite)", Range(0, 1)) = 1
        _DetailRayon ("Échelle des détails atténués (pixels du sprite)", Range(1, 6)) = 2.5
        _LisereFace ("Liseré : exposant de l'orientation vers le feu", Range(0.5, 8)) = 2.5
        _GardeLumiere ("Lumières gardées (luminance linéaire au-delà de laquelle le pixel reste tel quel)", Range(0, 2)) = 2

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" "CanUseSpriteAtlas" = "True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        ColorMask [_ColorMask]
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma shader_feature_local _ _POIDS_TEXTURE
            #include "ItHunts_UICommun.cginc"
            sampler2D _MainTex; float4 _MainTex_ST; float4 _MainTex_TexelSize; fixed4 _Color; float4 _ClipRect;
            sampler2D _Poids;
            float _Pivot, _Respiration, _Rythme, _Cape, _CapeHaut, _Phase, _Flottement, _FlotteVitesse, _Orbite, _Opaque;
            float4 _Centre;
            float _Eclairage, _Luminosite, _Saturation, _Contraste, _OmbreForce, _OmbreSeuil, _Transition, _LisereForce, _LisereLargeur, _Palpite, _PalpiteVitesse, _LanterneForce;
            float _Douceur, _LisereFace, _GardeLumiere, _Detail, _DetailRayon;
            float4 _FeuUV, _LanterneUV; fixed4 _OmbreCouleur, _ChaudCouleur, _FroidCouleur, _LisereCouleur, _LanterneCouleur;

            // lumière de la scène sur un pixel du personnage (couleur droite, linéaire)
            float3 Eclaire(float3 rgb, float2 uv, float a, float t, float actif)
            {
                float2 asp = float2(_MainTex_TexelSize.y / _MainTex_TexelSize.x, 1.0);   // uv -> repère carré (unités de hauteur)
                // valeur et saturation (comme les personnages de la maquette)
                float L = dot(rgb, float3(0.2126, 0.7152, 0.0722));
                float g = _Luminosite * pow(max(L, 1e-5), _Contraste - 1.0);   // L' = lum * L^contraste
                rgb = rgb * g;   // saturation : en fin de calcul (après les teintes du feu, des ombres et de la lanterne)
                L *= g;
                // ombres froides bleu-violet
                float o = (1.0 - smoothstep(0.0, _OmbreSeuil, L)) * _OmbreForce;
                rgb = lerp(rgb, rgb * _OmbreCouleur.rgb * 2.0 + _OmbreCouleur.rgb * 0.004, o);
                // côté feu chaud, côté opposé froid (projection sur la direction du feu, depuis le centre du sprite)
                float2 df = (_FeuUV.xy - 0.5) * asp;
                float2 dir = normalize(df + 1e-5);
                float s = dot((uv - 0.5) * asp, dir) / max(_Transition, 1e-3) + 0.5;
                s = smoothstep(0.0, 1.0, s);
                float fl = Vacille(t * _PalpiteVitesse, 3.7) * _Palpite * actif;
                float3 gain = lerp(_FroidCouleur.rgb, _ChaudCouleur.rgb * (1.0 + fl * 0.5), s);
                rgb *= gain;
                // liseré : seulement sur les bords dont la normale extérieure (opposée au gradient de l'alpha) regarde le
                // feu, fin (_LisereLargeur pixels), plus fort du côté chaud
                float2 tx = _MainTex_TexelSize.xy * _LisereLargeur;
                float gx = tex2D(_MainTex, uv + float2(tx.x, 0)).a - tex2D(_MainTex, uv - float2(tx.x, 0)).a;
                float gy = tex2D(_MainTex, uv + float2(0, tx.y)).a - tex2D(_MainTex, uv - float2(0, tx.y)).a;
                float2 gr = float2(gx, gy);
                float bord = saturate(length(gr) * 0.75);
                float2 nrm = -gr / max(length(gr), 1e-4);
                float2 dpx = normalize((_FeuUV.xy - uv) / _MainTex_TexelSize.xy + 1e-5);   // direction du feu, en pixels
                float face = pow(saturate(dot(nrm, dpx)), _LisereFace);
                float rim = a * bord * face;
                rgb += rim * _LisereCouleur.rgb * _LisereForce * (1.0 + fl) * (0.35 + 0.65 * s);
                // lueur de la lanterne (éclaire surtout les jambes : illumination de la couleur + un peu d'ajout)
                float2 dl = (uv - _LanterneUV.xy) * asp;
                float gl = exp(-dot(dl, dl) / max(_LanterneUV.z * _LanterneUV.z, 1e-4));
                float fll = 1.0 + Vacille(t * 1.2, 8.1) * 0.25 * actif;
                rgb += (rgb * 2.5 + 0.01) * gl * _LanterneCouleur.rgb * _LanterneForce * fll;
                float Lf = dot(rgb, float3(0.2126, 0.7152, 0.0722));
                return lerp(Lf.xxx, rgb, _Saturation);
            }

            v2f_ui vert(appdata_ui v)
            {
                v2f_ui o;
                v.vertex.y += sin((UiTemps() * _FlotteVitesse + _Phase) * 6.2832) * _Flottement * UiActif();
                ITHUNTS_UI_VERT_COMMUN(v, o);
                return o;
            }

            fixed4 frag(v2f_ui i) : SV_Target
            {
                float actif = UiActif();
                float t = UiTemps();
                float2 uv = i.uv;
                float wb = 1.0, wc, ph = _Phase;
                #if defined(_POIDS_TEXTURE)
                    float4 w = tex2D(_Poids, uv);
                    wb = w.r; wc = w.g; ph = w.b;
                #else
                    wc = smoothstep(_CapeHaut, 1.0, 1.0 - uv.y) * smoothstep(0.0, 0.08, uv.y);
                #endif
                float hpx = 1.0 / _MainTex_TexelSize.y;
                // respiration : uv y étiré autour du pivot, au plus _Respiration pixels au sommet
                float s = sin((t * _Rythme + ph) * 6.2832) * 0.5 + 0.5;
                float k = _Respiration * s / max(hpx * (1.0 - _Pivot), 1.0) * wb * actif;
                uv.y = _Pivot + (uv.y - _Pivot) / (1.0 + k);
                // cape : onde horizontale
                float wave = sin(t * 1.7 + uv.y * 9.0 + ph * 6.2832) * 0.6 + sin(t * 2.9 + uv.y * 17.0 + ph * 3.0) * 0.4;
                uv.x += wave * _Cape * wc * _MainTex_TexelSize.x * actif;
                // orbite (traînées de lumière) : rotation dans l'ellipse
                if (abs(_Orbite) > 0.0)
                {
                    float a = t * _Orbite * 6.2832 * actif;
                    float2 d = uv - _Centre.xy;
                    float asp = _MainTex_TexelSize.y / _MainTex_TexelSize.x;
                    d.x /= asp; d.y /= max(_Centre.z, 0.05);
                    float cs = cos(a), sn = sin(a);
                    d = float2(d.x * cs - d.y * sn, d.x * sn + d.y * cs);
                    d.y *= max(_Centre.z, 0.05); d.x *= asp;
                    uv = _Centre.xy + d;
                }
                fixed4 c = tex2D(_MainTex, uv);
                UNITY_BRANCH if (_Douceur > 0.0)
                {
                    // flou en tente 3 x 3 (poids 4 / 2 / 1), couleur prémultipliée
                    float2 d = _MainTex_TexelSize.xy * _Douceur;
                    float4 acc = float4(c.rgb * c.a, c.a) * 4.0;
                    float2 o[8] = { float2(1, 0), float2(-1, 0), float2(0, 1), float2(0, -1), float2(1, 1), float2(-1, 1), float2(1, -1), float2(-1, -1) };
                    [unroll] for (int k = 0; k < 8; k++)
                    {
                        float4 e = tex2D(_MainTex, uv + o[k] * d);
                        acc += float4(e.rgb * e.a, e.a) * (k < 4 ? 2.0 : 1.0);
                    }
                    acc /= 16.0;
                    c = float4(acc.rgb / max(acc.a, 1e-4), acc.a);
                }
                UNITY_BRANCH if (_Detail < 0.999)
                {
                    // détails fins atténués : couleur = flou large + _Detail x (couleur - flou large) ; l'acuité (finesse)
                    // ne change pas, seul le contraste des traits baisse (encre moins dure, comme la peinture autour)
                    float2 d = _MainTex_TexelSize.xy * _DetailRayon;
                    float4 acc = 0;
                    float2 o[9] = { float2(0, 0), float2(1, 0), float2(-1, 0), float2(0, 1), float2(0, -1), float2(1, 1), float2(-1, 1), float2(1, -1), float2(-1, -1) };
                    [unroll] for (int k = 0; k < 9; k++)
                    {
                        float4 e = tex2D(_MainTex, uv + o[k] * d);
                        float w = k == 0 ? 4.0 : (k < 5 ? 2.0 : 1.0);
                        acc += float4(e.rgb * e.a, e.a) * w;
                    }
                    float3 large = acc.rgb / max(acc.a, 1e-4);
                    c.rgb = max(large + _Detail * (c.rgb - large), 0.0);
                }
                c *= i.color;
                UNITY_BRANCH if (_Eclairage > 0.0)
                {
                    float3 lit = Eclaire(c.rgb, uv, c.a, t, actif);
                    float Lo = dot(c.rgb, float3(0.2126, 0.7152, 0.0722));
                    lit = lerp(lit, c.rgb, smoothstep(_GardeLumiere, _GardeLumiere + 0.25, Lo));
                    c.rgb = lerp(c.rgb, lit, _Eclairage);
                }
                c.a = lerp(c.a, i.color.a, _Opaque);
                ITHUNTS_UI_CLIP(i, c)
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
