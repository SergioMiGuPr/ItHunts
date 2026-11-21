// ItHunts : mouvement « sur place » des personnages et des artefacts peints, par déformation de l'image (quelques
// pixels) : respiration (étirement vertical autour de la ligne _Pivot, uv y), cape qui ondule (décalage horizontal qui
// grandit vers le bas), flottement (décalage vertical du graphique), orbite des traînées de lumière (rotation dans une
// ellipse). Poids : texture _Poids (R respiration, G cape, B phase) si _POIDS_TEXTURE, sinon profil vertical.
// Gelé (_ItHuntsMenuFige = 1) : aucune déformation, le rendu est l'image peinte.
// Éclairage de scène (_Eclairage = 1, personnages assis du campement) : valeur et saturation ramenées à celles des
// personnages de la maquette, ombres froides bleu-violet, côté du feu plus chaud et clair, côté opposé sombre et froid,
// liseré orange sur les bords tournés vers le feu (palpite avec lui), lueur de la lanterne sur les jambes. Les positions
// du feu et de la lanterne sont en uv du sprite (posées par CampementApercu selon la place). Gelé : rien ne palpite.
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
            float4 _FeuUV, _LanterneUV; fixed4 _OmbreCouleur, _ChaudCouleur, _FroidCouleur, _LisereCouleur, _LanterneCouleur;

            // lumière de la scène sur un pixel du personnage (couleur droite, linéaire)
            float3 Eclaire(float3 rgb, float2 uv, float a, float t, float actif)
            {
                float2 asp = float2(_MainTex_TexelSize.y / _MainTex_TexelSize.x, 1.0);   // uv -> repère carré (unités de hauteur)
                // valeur et saturation (comme les personnages de la maquette)
                float L = dot(rgb, float3(0.2126, 0.7152, 0.0722));
                float g = _Luminosite * pow(max(L, 1e-5), _Contraste - 1.0);   // L' = lum * L^contraste
                rgb = lerp(L.xxx, rgb, _Saturation) * g;
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
                // liseré : pixel opaque dont le voisin vers le feu est transparent
                float2 dpx = normalize((_FeuUV.xy - uv) / _MainTex_TexelSize.xy + 1e-5);   // direction du feu, en pixels
                float2 pas = dpx * _MainTex_TexelSize.xy * _LisereLargeur;
                float an = tex2D(_MainTex, uv + pas).a;
                float an2 = tex2D(_MainTex, uv + pas * 2.0).a;
                float rim = a * saturate(1.0 - (an + an2) * 0.5);
                rgb += rim * _LisereCouleur.rgb * _LisereForce * (1.0 + fl) * (0.35 + 0.65 * s);
                // lueur de la lanterne (éclaire surtout les jambes : illumination de la couleur + un peu d'ajout)
                float2 dl = (uv - _LanterneUV.xy) * asp;
                float gl = exp(-dot(dl, dl) / max(_LanterneUV.z * _LanterneUV.z, 1e-4));
                float fll = 1.0 + Vacille(t * 1.2, 8.1) * 0.25 * actif;
                rgb += (rgb * 2.5 + 0.01) * gl * _LanterneCouleur.rgb * _LanterneForce * fll;
                return rgb;
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
                fixed4 c = tex2D(_MainTex, uv) * i.color;
                UNITY_BRANCH if (_Eclairage > 0.0)
                    c.rgb = lerp(c.rgb, Eclaire(c.rgb, uv, c.a, t, actif), _Eclairage);
                c.a = lerp(c.a, i.color.a, _Opaque);
                ITHUNTS_UI_CLIP(i, c)
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
