// ItHunts : passes utilitaires du shader toon : ombres portées, profondeur, profondeur + normales (contours, SSAO).
#ifndef ITHUNTS_TOON_UTILITY_PASSES_INCLUDED
#define ITHUNTS_TOON_UTILITY_PASSES_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

struct UtilAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    float2 uv         : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct UtilVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv         : TEXCOORD0;
    float3 normalWS   : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void ToonAlphaClip(float2 uv)
{
#if defined(_ALPHATEST_ON)
    clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).a * _BaseColor.a - _Cutoff);
#endif
}

// ---------- Ombres portées (même biais que la passe ShadowCaster d'URP) ----------
float3 _LightDirection;
float3 _LightPosition;

UtilVaryings ToonShadowVert(UtilAttributes input)
{
    UtilVaryings output = (UtilVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
#if _CASTING_PUNCTUAL_LIGHT_SHADOW
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
    float3 lightDirectionWS = _LightDirection;
#endif
    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
    output.positionCS = ApplyShadowClamping(positionCS);
    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
    return output;
}

half4 ToonShadowFrag(UtilVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    ToonAlphaClip(input.uv);
    return 0;
}

// ---------- Profondeur, et profondeur + normales ----------
UtilVaryings ToonDepthVert(UtilAttributes input)
{
    UtilVaryings output = (UtilVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
    return output;
}

half ToonDepthFrag(UtilVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    ToonAlphaClip(input.uv);
    return input.positionCS.z;
}

// Même encodage que DepthNormalsPass.hlsl d'URP 17.3 (_CameraNormalsTexture : normales monde).
half4 ToonDepthNormalsFrag(UtilVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    ToonAlphaClip(input.uv);
#if defined(_GBUFFER_NORMALS_OCT)
    float3 normalWS = normalize(input.normalWS);
    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
    float2 remapped = saturate(octNormalWS * 0.5 + 0.5);
    return half4(PackFloat2To888(remapped), 0.0);
#else
    return half4(NormalizeNormalPerPixel(input.normalWS), 0.0);
#endif
}

#endif
