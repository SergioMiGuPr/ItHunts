// ItHunts : passe principale du shader toon (Forward et Forward+).
// Éclairage : couleur de base × (teinte d'ombre + ambiance + lumière principale en bandes + lumières ponctuelles en
// paliers) + rim + émission, puis brouillard. Ni métal ni reflet spéculaire (règle de style).
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

// Somme de marches nettes : 0, 1/n, 2/n … 1. La marche k tombe à x = (k - 1 + seuil) / n.
half ToonSteps(half x, half steps, half threshold, half softness)
{
    half n = max(1.0h, round(steps));
    half w = max(softness, 1e-3h);
    half sum = 0.0h;
    [unroll] for (int k = 1; k <= 5; k++)
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

    half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
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
    Light mainLight = GetMainLight(inputData.shadowCoord, positionWS, shadowMask);
#if defined(_RECEIVE_SHADOWS_OFF)
    mainLight.shadowAttenuation = 1.0h;
#endif
    half mainX = saturate(dot(normalWS, mainLight.direction)) * mainLight.shadowAttenuation * mainLight.distanceAttenuation;
    half mainBand = ToonSteps(mainX, _BandCount - 1.0h, _BandThreshold, _BandSoftness);
    lighting += mainLight.color * mainBand;
    lightPresence = max(lightPresence, mainBand);

#if defined(_ADDITIONAL_LIGHTS)
    uint pixelLightCount = GetAdditionalLightsCount();

    #if USE_CLUSTER_LIGHT_LOOP
    [loop] for (uint dirIndex = 0; dirIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); dirIndex++)
    {
        Light dirLight = GetAdditionalLight(dirIndex, positionWS, shadowMask);
        half dx = saturate(dot(normalWS, dirLight.direction)) * dirLight.shadowAttenuation;
        half dBand = ToonSteps(dx, _BandCount - 1.0h, _BandThreshold, _BandSoftness);
        lighting += dirLight.color * dBand;
        lightPresence = max(lightPresence, dBand);
    }
    #endif

    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, positionWS, shadowMask);
        half spot;
        half range = ToonRangeFraction(lightIndex, positionWS, spot);
        // Atténuation en paliers : îlots de lumière à bord net autour de la bougie.
        half falloff = pow(saturate(1.0h - range), max(_PointFalloff, 0.05h)) * spot;
        half att = ToonSteps(falloff, _PointSteps, _PointThreshold, _PointSoftness);
        // Face tournée vers la lumière : une seule marche nette (ombre portée comprise).
        half ndl = saturate((dot(normalWS, light.direction) + _PointWrap) / (1.0h + _PointWrap));
        half face = ToonSteps(ndl * light.shadowAttenuation, 1.0h, _BandThreshold, _BandSoftness);
        half contribution = att * face;
        lighting += light.color * contribution;
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
    half3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb * _EmissionColor.rgb;
    color += emission * lerp(fogVisibility, 1.0h, _EmissionThroughFog);
#endif

    return half4(color, 1.0h);
}

#endif
