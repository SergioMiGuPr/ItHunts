// ItHunts : propriétés communes du shader toon (toutes les passes partagent ce CBUFFER : compatible SRP Batcher).
#ifndef ITHUNTS_TOON_INPUT_INCLUDED
#define ITHUNTS_TOON_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
TEXTURE2D(_EmissionMap);    SAMPLER(sampler_EmissionMap);

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4  _BaseColor;
    half   _Cutoff;

    // Ombrage en bandes (lumière principale)
    half   _BandCount;        // nombre de bandes, ombre comprise (2 à 4)
    half   _BandThreshold;    // où tombe la première transition (0 = sur la ligne d'ombre)
    half   _BandSoftness;     // largeur des transitions
    half4  _ShadowColor;      // teinte de la bande d'ombre (plancher d'éclairage)
    half   _AmbientInfluence; // part de l'ambiance de la scène (Environment Lighting)

    // Lumières ponctuelles : îlots à bord net
    half   _PointSteps;       // paliers d'atténuation (1 à 5)
    half   _PointThreshold;   // position du premier palier (0..1 d'un palier)
    half   _PointSoftness;    // largeur du bord des paliers
    half   _PointFalloff;     // courbe de la portée (< 1 : îlots plus larges)
    half   _PointWrap;        // éclaire un peu les faces de biais (0 = Lambert strict)

    // Rim light
    half4  _RimColor;
    half   _RimWidth;
    half   _RimSoftness;
    half   _RimIntensity;
    half   _RimLitOnly;       // 0 : partout ; 1 : seulement là où une lumière touche

    // Émission (HDR, compatible bloom)
    half4  _EmissionColor;
    half   _EmissionThroughFog; // 0 : l'émission s'efface dans le brouillard comme le décor ; 1 : elle le traverse
CBUFFER_END

#endif
