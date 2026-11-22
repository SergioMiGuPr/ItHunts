// ItHunts : propriétés communes du shader toon (toutes les passes partagent ce CBUFFER : compatible SRP Batcher).
#ifndef ITHUNTS_TOON_INPUT_INCLUDED
#define ITHUNTS_TOON_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
TEXTURE2D(_EmissionMap);    SAMPLER(sampler_EmissionMap);
TEXTURE2D(_LayerMap);       SAMPLER(sampler_LayerMap);

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4  _BaseColor;
    half   _Cutoff;

    // Ombrage en bandes (lumière principale)
    half   _BandCount;        // nombre de bandes, ombre comprise (2 à 8)
    half   _BandThreshold;    // où tombe la première transition (0 = sur la ligne d'ombre)
    half   _BandSoftness;     // largeur des transitions
    half4  _ShadowColor;      // teinte de la bande d'ombre (plancher d'éclairage)
    half   _AmbientInfluence; // part de l'ambiance de la scène (Environment Lighting)
    half   _BandGradient;     // 0 : bandes nettes ; 1 : dégradé continu (lumière principale)

    // Lumières ponctuelles : atténuation douce jusqu'au noir, paliers peints en option
    half   _PointSteps;       // paliers d'atténuation (1 à 8)
    half   _PointThreshold;   // position du premier palier (0..1 d'un palier)
    half   _PointSoftness;    // largeur du bord des paliers
    half   _PointFalloff;     // courbe de la portée (< 1 : îlots plus larges)
    half   _PointWrap;        // éclaire un peu les faces de biais (0 = Lambert strict)
    half   _PointGradient;    // 0 : paliers nets (ancien rendu) ; 1 : dégradé continu jusqu'au noir
    half   _LightSaturation;  // saturation de la couleur des lumières sur les surfaces (1 : telle quelle)

    // Rim light
    half4  _RimColor;
    half   _RimWidth;
    half   _RimSoftness;
    half   _RimIntensity;
    half   _RimLitOnly;       // 0 : partout ; 1 : seulement là où une lumière touche

    // Émission (HDR, compatible bloom)
    half4  _EmissionColor;
    half   _EmissionThroughFog; // 0 : l'émission s'efface dans le brouillard comme le décor ; 1 : elle le traverse

    // UV en coordonnées monde (mot-clé _WORLD_UV, architecture du kit) : RENDU.md § 5 ter
    float4 _TileSize;           // taille de la tuile en mètres : x = largeur, y = hauteur (murs) ou profondeur (sols)
    float4 _TileOffset;         // décalage de la grille en mètres (x, y, z)
    half   _TopRotation;        // rotation des dalles du dessus et du dessous : 0 à 3 quarts de tour
    half   _TopRandomRotation;  // 1 : quart de tour tiré au hasard pour chaque tuile du dessus et du dessous

    // Mode décor (_DECOR) : variation, saleté en bas, couche mélangée en hauteur (MATERIAUX.md)
    half   _MacroVariation;
    float  _MacroScale;
    half4  _MacroTint;
    half   _VertexPaint;
    half4  _GrimeColor;
    float  _GrimeHeight;
    float  _GrimeGroundY;
    half   _GrimeStrength;
    half4  _WetColor;
    half   _WetStrength;
    float4 _LayerMap_ST;
    float  _LayerTile;
    half   _LayerAmount;
    half   _LayerLow;
    half   _LayerTop;
    half   _HeightBlend;
CBUFFER_END

// Projection par face, choisie selon la normale monde (comme it_common.world_uv dans Blender) : faces tournées vers ±X
// -> (z, y), vers ±Z -> (x, y), dessus et dessous -> (x, z). Le signe de la normale retourne u (pas de lecture en
// miroir). Projection « dure » (l'axe dominant), sans mélange triplanaire : une seule lecture de texture. Les dérivées
// sont celles de la projection continue (pas de liseré de mip là où l'axe change). Sur les murs, v suit toujours la
// verticale monde ; dessus et dessous peuvent tourner par quarts de tour, autour du centre de chaque tuile.
float2 ToonWorldUV(float3 positionWS, float3 normalWS, out float2 uvDdx, out float2 uvDdy)
{
    float3 p = positionWS + _TileOffset.xyz;
    float3 dpx = ddx(positionWS);
    float3 dpy = ddy(positionWS);
    float3 a = abs(normalWS);
    float2 size = max(_TileSize.xy, 1e-3);
    float2 t, gx, gy;
    if (a.y >= a.x && a.y >= a.z)
    {
        float s = normalWS.y >= 0.0 ? 1.0 : -1.0;
        t  = float2(p.x * s, p.z) / size;
        gx = float2(dpx.x * s, dpx.z) / size;
        gy = float2(dpy.x * s, dpy.z) / size;
        // quart de tour autour du centre de la tuile : la tuile reste entière, les joints restent sur la grille
        float2 cell = floor(t);
        float h = frac(sin(dot(cell, float2(12.9898, 78.233))) * 43758.5453);
        int k = ((int)round(_TopRotation) + (int)(floor(h * 4.0) * _TopRandomRotation)) & 3;
        float2 l = t - cell - 0.5;
        if (k == 1)      { l = float2(l.y, -l.x);  gx = float2(gx.y, -gx.x);  gy = float2(gy.y, -gy.x); }
        else if (k == 2) { l = -l;                 gx = -gx;                  gy = -gy; }
        else if (k == 3) { l = float2(-l.y, l.x);  gx = float2(-gx.y, gx.x);  gy = float2(-gy.y, gy.x); }
        t = l + 0.5;
    }
    else if (a.x >= a.z)
    {
        float s = normalWS.x >= 0.0 ? 1.0 : -1.0;
        t  = float2(p.z * s, p.y) / size;
        gx = float2(dpx.z * s, dpx.y) / size;
        gy = float2(dpy.z * s, dpy.y) / size;
    }
    else
    {
        float s = normalWS.z >= 0.0 ? -1.0 : 1.0;
        t  = float2(p.x * s, p.y) / size;
        gx = float2(dpx.x * s, dpx.y) / size;
        gy = float2(dpy.x * s, dpy.y) / size;
    }
    uvDdx = gx;
    uvDdy = gy;
    return t;
}

#endif
