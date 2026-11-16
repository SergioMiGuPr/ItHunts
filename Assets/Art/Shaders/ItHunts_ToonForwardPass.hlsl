// ItHunts : passe principale du shader toon (Forward et Forward+).
// Éclairage : couleur de base × (teinte d'ombre + ambiance + lumière principale en bandes + lumières ponctuelles) + rim +
// émission, puis brouillard. Ni métal ni reflet spéculaire (règle de style).
// Retours du 2026-11-16 (lumière coupée net, cercles durs) : chaque terme mélange des paliers peints (ToonSteps) et un
// dégradé continu (_BandGradient, _PointGradient) ; l'atténuation des lumières ponctuelles est douce jusqu'au bord de la
// portée (pente nulle : aucun bord) ; la couleur des lumières est saturée (_LightSaturation) pour teinter les surfaces.
#ifndef ITHUNTS_TOON_FORWARD_PASS_INCLUDED
#define ITHUNTS_TOON_FORWARD_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/AmbientOcclusion.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    float2 uv         : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv         : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
    float3 normalWS   : TEXCOORD2;
    half   fogFactor  : TEXCOORD3;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

// Somme de marches : 0, 1/n, 2/n … 1. La marche k tombe à x = (k - 1 + seuil) / n ; softness : demi-largeur de chaque
// transition (grande : marches fondues en dégradé).
half ToonSteps(half x, half steps, half threshold, half softness)
{
    half n = max(1.0h, round(steps));
    half w = max(softness, 1e-3h);
    half sum = 0.0h;
    [unroll] for (int k = 1; k <= 8; k++)
    {
        if (k <= n)
        {
            half t = (k - 1 + threshold) / n;
            sum += smoothstep(t - w, t + w, x);
        }
    }
    return saturate(sum / n);
}

// Portée normalisée d'une lumière additionnelle : 0 au centre, 1 au bord de sa portée (Range). Mêmes données que
// GetAdditionalPerObjectLight d'URP 17.3 ; 0 pour une lumière directionnelle additionnelle.
float ToonRangeFraction(uint lightIndex, float3 positionWS, out half spotAttenuation)
{
#if USE_CLUSTER_LIGHT_LOOP
    int i = lightIndex;
#else
    int i = GetPerObjectLightIndex(lightIndex);
#endif
#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
    float4 lightPositionWS = _AdditionalLightsBuffer[i].position;
    half4 attenuation = _AdditionalLightsBuffer[i].attenuation;
    half4 spotDirection = _AdditionalLightsBuffer[i].spotDirection;
#else
    float4 lightPositionWS = _AdditionalLightsPosition[i];
    half4 attenuation = _AdditionalLightsAttenuation[i];
    half4 spotDirection = _AdditionalLightsSpotDir[i];
#endif
    float3 lightVector = lightPositionWS.xyz - positionWS * lightPositionWS.w;
    float distanceSqr = max(dot(lightVector, lightVector), HALF_MIN);
    half3 lightDirection = half3(lightVector * rsqrt(distanceSqr));
    spotAttenuation = AngleAttenuation(spotDirection.xyz, lightDirection, attenuation.zw);
    // attenuation.x = 1 / portée² (URP) : distance² × x = (d / portée)².
    return sqrt(saturate(distanceSqr * attenuation.x));
}

// Dégradé de la lumière principale : Lambert légèrement décalé par le seuil des bandes (même ligne d'ombre).
half MainGradient(half x)
{
    return smoothstep(_BandThreshold * 0.5h, 1.0h, x);
}

// Couleur de la lumière plus ou moins saturée, à luminance égale (la lanterne teinte la surface de sa couleur).
half3 LightTint(half3 c)
{
    half l = dot(c, half3(0.299h, 0.587h, 0.114h));
    return max(0.0h, lerp(l.xxx, c, _LightSaturation));
}

Varyings ToonVert(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
    output.positionCS = pos.positionCS;
    output.positionWS = pos.positionWS;
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
    output.fogFactor = ComputeFogFactor(pos.positionCS.z);
    return output;
}

half4 ToonFrag(Varyings input, FRONT_FACE_TYPE isFrontFace : FRONT_FACE_SEMANTIC) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#if defined(_WORLD_UV)
    // Architecture : UV calculées en coordonnées monde (les UV du modèle et le tiling du matériau sont ignorés)
    float2 uvDdx, uvDdy;
    float2 uv = ToonWorldUV(input.positionWS, normalize(input.normalWS), uvDdx, uvDdy);
    half4 albedo = SAMPLE_TEXTURE2D_GRAD(_BaseMap, sampler_BaseMap, uv, uvDdx, uvDdy) * _BaseColor;
#else
    float2 uv = input.uv;
    half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;
#endif
#if defined(_ALPHATEST_ON)
    clip(albedo.a - _Cutoff);
#endif

    float3 positionWS = input.positionWS;
    half3 normalWS = normalize(input.normalWS) * IS_FRONT_VFACE(isFrontFace, 1.0h, -1.0h);
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(positionWS);

    // InputData : nommée « inputData », comme l'attendent LIGHT_LOOP_BEGIN (Forward+) et l'occlusion ambiante.
    InputData inputData = (InputData)0;
    inputData.positionWS = positionWS;
    inputData.normalWS = normalWS;
    inputData.viewDirectionWS = viewDirWS;
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.shadowCoord = TransformWorldToShadowCoord(positionWS);
    half4 shadowMask = half4(1, 1, 1, 1);

#if defined(_SCREEN_SPACE_OCCLUSION)
    AmbientOcclusionFactor ao = GetScreenSpaceAmbientOcclusion(inputData.normalizedScreenSpaceUV);
    half indirectAO = ao.indirectAmbientOcclusion;
#else
    half indirectAO = 1.0h;
#endif

    // Plancher : teinte d'ombre + ambiance de la scène.
    half3 lighting = _ShadowColor.rgb + SampleSH(normalWS) * _AmbientInfluence * indirectAO;
    half lightPresence = 0.0h;

    // Lumière principale (la lune) : ombre reçue comprise dans l'entrée, donc ombres en bandes elles aussi.
    // Calques de lumière (Rendering Layers) : une lumière n'éclaire que les objets dont un calque est dans son masque
    // (la lanterne du joueur exclut le calque des mains et des artefacts de la vue FPS).
    uint meshRenderingLayers = GetMeshRenderingLayer();

    Light mainLight = GetMainLight(inputData.shadowCoord, positionWS, shadowMask);
#if defined(_RECEIVE_SHADOWS_OFF)
    mainLight.shadowAttenuation = 1.0h;
#endif
    half mainX = saturate(dot(normalWS, mainLight.direction)) * mainLight.shadowAttenuation * mainLight.distanceAttenuation;
    half mainBand = lerp(ToonSteps(mainX, _BandCount - 1.0h, _BandThreshold, _BandSoftness), MainGradient(mainX), _BandGradient);
#if defined(_LIGHT_LAYERS)
    if (!IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
        mainBand = 0.0h;
#endif
    lighting += LightTint(mainLight.color) * mainBand;
    lightPresence = max(lightPresence, mainBand);

#if defined(_ADDITIONAL_LIGHTS)
    uint pixelLightCount = GetAdditionalLightsCount();

    #if USE_CLUSTER_LIGHT_LOOP
    [loop] for (uint dirIndex = 0; dirIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); dirIndex++)
    {
        Light dirLight = GetAdditionalLight(dirIndex, positionWS, shadowMask);
        half dx = saturate(dot(normalWS, dirLight.direction)) * dirLight.shadowAttenuation;
        half dBand = lerp(ToonSteps(dx, _BandCount - 1.0h, _BandThreshold, _BandSoftness), MainGradient(dx), _BandGradient);
        #if defined(_LIGHT_LAYERS)
        if (!IsMatchingLightLayer(dirLight.layerMask, meshRenderingLayers))
            dBand = 0.0h;
        #endif
        lighting += LightTint(dirLight.color) * dBand;
        lightPresence = max(lightPresence, dBand);
    }
    #endif

    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, positionWS, shadowMask);
        half spot;
        half range = ToonRangeFraction(lightIndex, positionWS, spot);
        // Atténuation : fenêtre douce (1 - r²)² (pente nulle au bord de la portée : la lumière s'éteint jusqu'au noir sans
        // cercle), courbée par _PointFalloff ; les paliers peints se superposent selon _PointGradient.
        half window = saturate(1.0h - range * range);
        half falloff = pow(window * window, max(_PointFalloff, 0.2h)) * spot;
        half att = lerp(ToonSteps(falloff, _PointSteps, _PointThreshold, _PointSoftness), falloff, _PointGradient);
        // Face tournée vers la lumière (ombre portée comprise) : une marche, fondue par _PointGradient.
        half ndl = saturate((dot(normalWS, light.direction) + _PointWrap) / (1.0h + _PointWrap)) * light.shadowAttenuation;
        half face = lerp(ToonSteps(ndl, 1.0h, _BandThreshold, _BandSoftness), smoothstep(0.0h, 0.6h, ndl), _PointGradient);
        half contribution = att * face;
        #if defined(_LIGHT_LAYERS)
        if (!IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
            contribution = 0.0h;
        #endif
        lighting += LightTint(light.color) * contribution;
        lightPresence = max(lightPresence, contribution);
    LIGHT_LOOP_END
#endif

    half3 color = albedo.rgb * lighting;

#if defined(_RIM_ON)
    half fresnel = 1.0h - saturate(dot(normalWS, viewDirWS));
    half rim = smoothstep(1.0h - _RimWidth - _RimSoftness, 1.0h - _RimWidth + _RimSoftness, fresnel);
    rim *= lerp(1.0h, saturate(lightPresence), _RimLitOnly);
    color += _RimColor.rgb * rim * _RimIntensity;
#endif

    half fogFactor = InitializeInputDataFog(float4(positionWS, 1.0), input.fogFactor);
    color = MixFog(color, fogFactor);

#if defined(_EMISSION)
    // L'émission traverse plus ou moins le brouillard (0 : comme le décor ; 1 : pas du tout atténuée).
    half fogVisibility = MixFogColor(half3(1, 1, 1), half3(0, 0, 0), fogFactor).r;
#if defined(_WORLD_UV)
    half3 emission = SAMPLE_TEXTURE2D_GRAD(_EmissionMap, sampler_EmissionMap, uv, uvDdx, uvDdy).rgb * _EmissionColor.rgb;
#else
    half3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv).rgb * _EmissionColor.rgb;
#endif
    color += emission * lerp(fogVisibility, 1.0h, _EmissionThroughFog);
#endif

    // alpha : utilisé seulement si le matériau mélange (translucide) ; ignoré en opaque (One / Zero)
    return half4(color, albedo.a);
}

#endif
