// ItHunts : outils communs des shaders d'interface des menus (uGUI). Même structure que UI/Default (stencil, masque
// rectangulaire, couleur du sommet), plus le temps des effets et le gel des menus.
// _ItHuntsMenuFige (global, 0 ou 1) : 1 = effets au repos (captures « à l'arrêt » comparées aux maquettes).
#ifndef ITHUNTS_UI_COMMUN
#define ITHUNTS_UI_COMMUN

#include "UnityCG.cginc"
#include "UnityUI.cginc"

float _ItHuntsMenuFige;
float _ItHuntsMenuTemps;   // décalage de temps (captures reproductibles), 0 en jeu

struct appdata_ui
{
    float4 vertex   : POSITION;
    float4 color    : COLOR;
    float2 texcoord : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f_ui
{
    float4 vertex        : SV_POSITION;
    fixed4 color         : COLOR;
    float2 uv            : TEXCOORD0;
    float4 worldPosition : TEXCOORD1;
    float2 local         : TEXCOORD2;   // position dans le repère du canvas (pixels de la maquette)
    UNITY_VERTEX_OUTPUT_STEREO
};

inline float UiTemps() { return _Time.y + _ItHuntsMenuTemps; }
inline float UiActif() { return 1.0 - saturate(_ItHuntsMenuFige); }

// bruit de valeur lissé (sans texture), 0..1
inline float Hash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

inline float Valeur(float2 p)
{
    float2 i = floor(p), f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    float a = Hash21(i), b = Hash21(i + float2(1, 0)), c = Hash21(i + float2(0, 1)), d = Hash21(i + float2(1, 1));
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

inline float Fbm(float2 p)
{
    float s = 0.0, a = 0.5;
    for (int k = 0; k < 4; k++) { s += a * Valeur(p); p = p * 2.03 + 17.1; a *= 0.5; }
    return s / 0.9375;
}

// vacillement de flamme : somme de sinus incommensurables + bruit, moyenne 0, amplitude ~1
inline float Vacille(float t, float graine)
{
    float v = sin(t * 7.3 + graine * 11.0) * 0.35 + sin(t * 13.1 + graine * 5.0) * 0.25 + sin(t * 2.9 + graine * 3.0) * 0.2;
    v += (Valeur(float2(t * 9.0, graine * 31.0)) - 0.5) * 0.6;
    return v;
}

#define ITHUNTS_UI_VERT_COMMUN(v, o) \
    UNITY_SETUP_INSTANCE_ID(v); \
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); \
    o.worldPosition = v.vertex; \
    o.local = v.vertex.xy; \
    o.vertex = UnityObjectToClipPos(o.worldPosition); \
    o.uv = TRANSFORM_TEX(v.texcoord, _MainTex); \
    o.color = v.color * _Color;

#ifdef UNITY_UI_CLIP_RECT
#define ITHUNTS_UI_CLIP(i, c) c.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
#else
#define ITHUNTS_UI_CLIP(i, c)
#endif

#endif
