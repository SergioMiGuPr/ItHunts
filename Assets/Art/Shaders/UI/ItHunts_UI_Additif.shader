// ItHunts : lumières ajoutées des menus (mélange additif, One One). Modes :
// - Texture : _MainTex x couleur x vacillement (lueurs des lanternes, apparition d'un joueur) ;
// - Brume : masque (_MainTex, R) x bruit fractal qui dérive (_Derive, pixels par seconde) x couleur ;
// - Flammes : _MainTex (flammes peintes, en additif) déformé vers le haut (ondulation qui grandit avec la hauteur
//   au-dessus de _Base, en uv) et scintillement d'intensité ;
// - Etincelles : points procéduraux qui montent du foyer (rect du graphique), sans texture.
// Gelé (_ItHuntsMenuFige = 1) : Texture et Flammes restent à leur image peinte (x _AuRepos), Brume et Etincelles
// s'éteignent.
Shader "ItHunts/UI/Additif"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Couleur", Color) = (1, 1, 1, 1)
        _Intensite ("Intensité", Range(0, 4)) = 1
        [KeywordEnum(Texture, Brume, Flammes, Etincelles)] _Mode ("Mode", Float) = 0
        _Amplitude ("Amplitude du vacillement", Range(0, 1)) = 0.2
        _Vitesse ("Vitesse", Range(0, 10)) = 1
        _Graine ("Graine", Float) = 0
        _AuRepos ("Intensité au repos (gelé) : 1 = image peinte, 0 = éteint", Range(0, 1)) = 1
        [Header(Brume)]
        _Echelle ("Échelle du bruit (par pixel)", Float) = 0.004
        _Derive ("Dérive (pixels par seconde)", Vector) = (12, 2, 0, 0)
        _Seuil ("Seuil du bruit", Range(0, 1)) = 0.35
        [Header(Flammes)]
        _Base ("Pied des flammes (uv y)", Range(0, 1)) = 0.12
        _Ondulation ("Ondulation (pixels au sommet)", Range(0, 30)) = 6
        _Montee ("Vitesse de montée du bruit", Range(0, 10)) = 2.2
        [Header(Etincelles)]
        _Nombre ("Nombre", Range(1, 24)) = 10
        _Taille ("Taille (pixels)", Range(0.5, 6)) = 1.6
        _Taille2 ("Taille du graphique (pixels, x y)", Vector) = (280, 360, 0, 0)

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
            #pragma shader_feature_local _MODE_TEXTURE _MODE_BRUME _MODE_FLAMMES _MODE_ETINCELLES
            #include "ItHunts_UICommun.cginc"
            sampler2D _MainTex; float4 _MainTex_ST; float4 _MainTex_TexelSize; fixed4 _Color; float4 _ClipRect;
            float _Intensite, _Amplitude, _Vitesse, _Graine, _AuRepos, _Echelle, _Seuil, _Base, _Ondulation, _Montee, _Nombre, _Taille;
            float4 _Derive, _Taille2;

            v2f_ui vert(appdata_ui v) { v2f_ui o; ITHUNTS_UI_VERT_COMMUN(v, o); return o; }

            fixed4 frag(v2f_ui i) : SV_Target
            {
                float actif = UiActif();
                float t = UiTemps() * _Vitesse;
                float3 c = 0;
                #if defined(_MODE_BRUME)
                    float m = tex2D(_MainTex, i.uv).r;
                    float2 p = (i.local - _Derive.xy * UiTemps()) * _Echelle;
                    float n = Fbm(p) * 0.65 + Fbm(p * 2.3 + 7.0 - _Derive.xy * UiTemps() * _Echelle * 0.6) * 0.35;
                    n = saturate((n - _Seuil) / max(1.0 - _Seuil, 1e-3));
                    c = _Color.rgb * m * n * n * actif;
                #elif defined(_MODE_FLAMMES)
                    float2 uv = i.uv;
                    float h = saturate((uv.y - _Base) / max(1.0 - _Base, 1e-3));
                    float2 q = float2(uv.x * 6.0, uv.y * 3.0 - UiTemps() * _Montee);
                    float dx = (Fbm(q) - 0.5) * 2.0;
                    float dy = (Fbm(q + 13.7) - 0.5) * 2.0;
                    float amp = _Ondulation * h * h * actif;
                    uv += float2(dx, dy * 0.5) * amp * _MainTex_TexelSize.xy;
                    float fl = 1.0 + Vacille(t, _Graine) * _Amplitude * actif;
                    c = tex2D(_MainTex, uv).rgb * fl * lerp(_AuRepos, 1.0, actif);
                #elif defined(_MODE_ETINCELLES)
                    float2 px = i.uv * _Taille2.xy;
                    float n = 0;
                    for (int k = 0; k < 24; k++)
                    {
                        if (k >= (int)_Nombre) break;
                        float r = Hash21(float2(k, _Graine));
                        float vie = 2.2 + r * 1.8;
                        float cyc = UiTemps() / vie + r * 7.0;
                        float ph = frac(cyc);
                        float x0 = _Taille2.x * (0.35 + 0.3 * Hash21(float2(k * 3.1, floor(cyc))));
                        float2 pos = float2(x0 + sin(ph * 6.28 * (1.0 + r) + r * 9.0) * 14.0 * ph, _Taille2.y * (0.18 + ph * 0.8));
                        float d = length(px - pos);
                        n += saturate(1.0 - d / _Taille) * (1.0 - ph) * smoothstep(0.0, 0.08, ph);
                    }
                    c = _Color.rgb * n * actif;
                #else
                    fixed4 tc = tex2D(_MainTex, i.uv);
                    float fl = 1.0 + Vacille(t, _Graine) * _Amplitude * actif;
                    c = tc.rgb * tc.a * _Color.rgb * fl * lerp(_AuRepos, 1.0, actif);
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
