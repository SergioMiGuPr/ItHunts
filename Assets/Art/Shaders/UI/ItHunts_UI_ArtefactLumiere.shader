// ItHunts : lumières des artefacts de l'écran de choix (mélange additif One One). Modes :
// - Lueur   : halo peint (_MainTex) qui respire (_Base + _Amplitude x respiration) ;
// - Braises : braises procédurales qui montent de _Source (uv x, y, étalement x, y), dérivent, scintillent et
//             refroidissent (_Chaud -> _Color) ;
// - Reflet  : un reflet qui balaie le métal (poids = R du masque x luminance du corps _Corps), puis une pause ;
//             la rune (G du masque) respire en ambre ;
// - Runes   : runes peintes (_MainTex) qui pulsent, l'onde courant le long de la lame (_Plage : uv x début, fin) ;
// - Volutes : canaux peints (_MainTex, R) traversés par un bruit qui monte et s'enroule (domaine déformé).
// Gelé (_ItHuntsMenuFige = 1) : lueurs et runes à mi-respiration (image peinte), rune du barillet au repos ; braises, éclat
// du reflet et volutes éteints ; aucun flottement.
Shader "ItHunts/UI/ArtefactLumiere"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "black" {}
        _Color ("Couleur", Color) = (1, 1, 1, 1)
        _Intensite ("Intensité", Range(0, 4)) = 1
        [KeywordEnum(Lueur, Braises, Reflet, Runes, Volutes)] _Mode ("Mode", Float) = 0
        _Base ("Base (intensité au creux)", Range(0, 2)) = 0.6
        _Amplitude ("Amplitude", Range(0, 2)) = 0.4
        _Periode ("Période (s)", Float) = 3.6
        _Graine ("Graine", Float) = 0
        _Flotte ("Flottement (fraction de la hauteur)", Float) = 0.012
        _FlottePeriode ("Période du flottement (s)", Float) = 5.5
        [Header(Braises)]
        _Nombre ("Nombre", Range(1, 24)) = 9
        _Taille ("Taille (pixels du cadre)", Range(0.5, 8)) = 2.2
        _Source ("Source (uv x, y, étalement x, y)", Vector) = (0.5, 0.45, 0.05, 0.08)
        _Hauteur ("Montée (fraction de la hauteur)", Float) = 0.45
        _Cadre ("Taille du cadre (pixels, x y)", Vector) = (800, 600, 0, 0)
        _Chaud ("Couleur chaude (naissance)", Color) = (1, 0.8, 0.45, 1)
        [Header(Reflet)]
        _Corps ("Corps (luminance)", 2D) = "black" {}
        _Direction ("Direction du balayage (uv)", Vector) = (0.8, -0.6, 0, 0)
        _Largeur ("Largeur du reflet (uv)", Float) = 0.05
        _Balayage ("Durée du balayage (fraction de la période)", Range(0.05, 1)) = 0.3
        _Rune ("Couleur de la rune", Color) = (1, 0.55, 0.15, 1)
        [Header(Runes)]
        _Plage ("Plage des runes (uv x début, fin)", Vector) = (0.4, 0.46, 0, 0)
        [Header(Volutes)]
        _Echelle ("Échelle du bruit (par uv)", Float) = 7
        _Montee ("Montée (uv par seconde)", Float) = 0.05
        _Seuil ("Seuil du bruit", Range(0, 1)) = 0.4

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
        Blend One One
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma shader_feature_local _MODE_LUEUR _MODE_BRAISES _MODE_REFLET _MODE_RUNES _MODE_VOLUTES
            #include "ItHunts_UIArtefact.cginc"
            sampler2D _MainTex; float4 _MainTex_ST; fixed4 _Color; float4 _ClipRect;
            sampler2D _Corps;
            float _Intensite, _Base, _Amplitude, _Nombre, _Taille, _Hauteur, _Largeur, _Balayage, _Echelle, _Montee, _Seuil;
            float4 _Source, _Cadre, _Direction, _Plage;
            fixed4 _Chaud, _Rune;

            v2f_ui vert(appdata_ui v) { v2f_ui o; ITHUNTS_UI_VERT_COMMUN(v, o); return o; }

            fixed4 frag(v2f_ui i) : SV_Target
            {
                float actif = UiActif();
                float t = UiTemps();
                float2 uv = i.uv - Flottement();
                float3 c = 0;
                float resp = Respire();
                #if defined(_MODE_BRAISES)
                    float2 px = i.uv * _Cadre.xy;
                    float n3 = 0; float3 col = 0;
                    for (int k = 0; k < 24; k++)
                    {
                        if (k >= (int)_Nombre) break;
                        float r = Hash21(float2(k * 1.37, _Graine + 3.1));
                        float vie = 2.8 + r * 2.2;
                        float cyc = t / vie + r * 7.3;
                        float ph = frac(cyc);
                        float id = floor(cyc);
                        float hx = Hash21(float2(k * 3.1, id + _Graine)) - 0.5;
                        float hy = Hash21(float2(k * 5.7, id * 1.3 + _Graine)) - 0.5;
                        float2 nais = (_Source.xy + float2(hx, hy) * 2.0 * _Source.zw);
                        // montée qui ralentit, dérive lente et tortillement
                        float mont = (1.0 - (1.0 - ph) * (1.0 - ph)) * _Hauteur * (0.7 + 0.6 * r);
                        float derive = (hx * 0.6 + sin(ph * 6.2832 * (0.7 + r) + r * 9.0) * 0.35) * 0.06 * ph;
                        float2 pos = (nais + float2(derive, mont) + Flottement()) * _Cadre.xy;
                        float d = length(px - pos);
                        float taille = _Taille * (1.0 - 0.55 * ph) * (0.7 + 0.6 * Hash21(float2(k, id + 9.0)));
                        float sc = 0.65 + 0.35 * Valeur(float2(t * 11.0 + k * 7.0, k));
                        float vieI = smoothstep(0.0, 0.08, ph) * pow(1.0 - ph, 1.4) * sc;
                        float coeur = saturate(1.0 - d / taille);
                        float halo = exp(-d / (taille * 2.2)) * 0.35;
                        float e = (coeur * coeur + halo) * vieI;
                        col += lerp(_Chaud.rgb, _Color.rgb, saturate(ph * 1.4)) * e;
                    }
                    c = col * actif;
                #elif defined(_MODE_REFLET)
                    fixed4 m = tex2D(_MainTex, uv);
                    fixed4 corps = tex2D(_Corps, uv);
                    float2 dir = normalize(_Direction.xy);
                    float s = dot(uv - 0.5, dir);
                    float ph = frac(t / max(_Periode, 0.1) + _Graine * 0.21);
                    float av = saturate(ph / _Balayage);
                    float pos = lerp(-0.45, 0.45, smoothstep(0.0, 1.0, av));
                    // éclat net (largeur _Largeur) + voile large et faible : un vrai reflet de métal poli
                    float dx = s - pos;
                    float bande = (exp(-pow(dx / max(_Largeur, 1e-3), 2.0)) + 0.25 * exp(-pow(dx / max(_Largeur * 4.0, 1e-3), 2.0)))
                                * step(ph, _Balayage) * smoothstep(0.0, 0.15, av) * (1.0 - smoothstep(0.85, 1.0, av));
                    float poids = m.r * pow(saturate(Lum(corps.rgb) * 1.3), 1.5) * corps.a;
                    c = _Color.rgb * bande * poids * actif;
                    c += _Rune.rgb * m.g * (_Base + _Amplitude * resp);
                #elif defined(_MODE_RUNES)
                    fixed4 tc = tex2D(_MainTex, uv);
                    float x = saturate((uv.x - _Plage.x) / max(_Plage.y - _Plage.x, 1e-3));
                    float onde = pow(saturate(0.5 + 0.5 * cos(6.2832 * (x * 0.9 - t / max(_Periode, 0.1)))), 3.0);
                    float k2 = _Base + _Amplitude * lerp(0.5, onde * (0.6 + 0.4 * resp), actif);
                    c = tc.rgb * _Color.rgb * k2;
                #elif defined(_MODE_VOLUTES)
                    // filets : bruit étiré à la verticale (fins en x, longs en y) qui monte, et dont la position en x
                    // ondule lentement avec la hauteur -> fumée qui s'élève en se tordant
                    float m = tex2D(_MainTex, uv).r;
                    float2 p = uv * _Echelle * float2(_Cadre.x / _Cadre.y, 1.0);
                    float y = p.y - t * _Montee * _Echelle;
                    float ond = (Fbm(float2(p.x * 0.6 + 4.3, y * 0.5 + t * 0.05)) - 0.5) * 1.6;
                    float n = Fbm(float2((p.x + ond) * 3.2, y * 0.7));
                    n = saturate((n - _Seuil) / max(1.0 - _Seuil, 1e-3));
                    float fil = n * n * (3.0 - 2.0 * n);
                    c = _Color.rgb * m * fil * (_Base + _Amplitude * resp) * actif;
                #else
                    fixed4 tc = tex2D(_MainTex, uv);
                    float fl = 1.0 + Vacille(t * 0.6, _Graine) * 0.05 * actif;
                    c = tc.rgb * _Color.rgb * (_Base + _Amplitude * resp) * fl;
                #endif
                fixed4 o = fixed4(c * _Intensite * i.color.rgb * i.color.a, 1);
                ITHUNTS_UI_CLIP(i, o)
                o.rgb *= o.a;
                o.a = 0;
                return o;
            }
            ENDCG
        }
    }
}
