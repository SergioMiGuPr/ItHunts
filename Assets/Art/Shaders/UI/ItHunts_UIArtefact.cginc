// ItHunts : outils communs des artefacts de l'écran de choix (nuit 7). Tous les calques d'un artefact partagent le même
// rectangle (cadre 400 x 300 de la maquette) : le flottement se fait en décalant les UV, à l'identique sur chaque calque,
// et ne déforme rien. _ItHuntsMenuFige = 1 : tout au repos (flottement nul, respiration à mi-course).
#ifndef ITHUNTS_UI_ARTEFACT
#define ITHUNTS_UI_ARTEFACT

#include "ItHunts_UICommun.cginc"

float _Flotte;          // amplitude du flottement (fraction de la hauteur du cadre)
float _FlottePeriode;   // secondes
float _Periode;         // période de la respiration (secondes)
float _Graine;

// décalage vertical des UV (flottement lent, deux sinus pour éviter un va-et-vient mécanique)
inline float2 Flottement()
{
    float t = UiTemps() / max(_FlottePeriode, 0.1);
    float s = sin(t * 6.2832) * 0.8 + sin(t * 6.2832 * 0.43 + 1.7) * 0.2;
    return float2(0, s * _Flotte * UiActif());
}

// respiration 0..1 : montée lente, descente plus lente, et un léger vacillement
inline float Respire()
{
    float ph = frac(UiTemps() / max(_Periode, 0.1) + _Graine * 0.37);
    float r = ph < 0.42 ? smoothstep(0.0, 0.42, ph) : 1.0 - smoothstep(0.42, 1.0, ph);
    return lerp(0.5, r, UiActif());
}

inline float Lum(float3 c) { return dot(c, float3(0.2126, 0.7152, 0.0722)); }

#endif
