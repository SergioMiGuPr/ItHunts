// ItHunts : cel-shading (bandes nettes, îlots des lumières ponctuelles, rim, émission HDR).
// Noms de propriétés d'URP Lit gardés (_BaseMap, _BaseColor, _EmissionColor, mot-clé _EMISSION) : le code du jeu les
// pilote (lueurs des objets attrapables, chambres du barillet, retour de dégâts).
Shader "ItHunts/Toon"
{
    Properties
    {
        [Header(Couleur)]
        [MainTexture] _BaseMap ("Texture de couleur", 2D) = "white" {}
        [MainColor] _BaseColor ("Teinte", Color) = (1, 1, 1, 1)
        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Découpe alpha", Float) = 0
        _Cutoff ("Seuil de découpe", Range(0, 1)) = 0.5

        [Header(Bandes de la lumiere principale)]
        [IntRange] _BandCount ("Nombre de bandes (ombre comprise)", Range(2, 4)) = 3
        _BandThreshold ("Seuil de la première bande", Range(0, 1)) = 0.15
        _BandSoftness ("Largeur des transitions", Range(0.001, 0.25)) = 0.02
        _ShadowColor ("Teinte de la bande d'ombre", Color) = (0.028, 0.034, 0.075, 1)
        _AmbientInfluence ("Part de l'ambiance de la scène", Range(0, 2)) = 1
        [ToggleOff(_RECEIVE_SHADOWS_OFF)] _ReceiveShadows ("Reçoit les ombres", Float) = 1

        [Header(Lumieres ponctuelles)]
        [IntRange] _PointSteps ("Paliers d'atténuation", Range(1, 5)) = 3
        _PointThreshold ("Position du premier palier", Range(0.05, 1)) = 0.35
        _PointSoftness ("Bord des paliers", Range(0.001, 0.2)) = 0.015
        _PointFalloff ("Courbe de portée (< 1 : îlots larges)", Range(0.2, 3)) = 0.8
        _PointWrap ("Enveloppe (faces de biais)", Range(0, 1)) = 0.2

        [Header(Rim light)]
        [Toggle(_RIM_ON)] _Rim ("Rim light", Float) = 0
        [HDR] _RimColor ("Couleur du rim", Color) = (0.35, 0.45, 0.7, 1)
        _RimWidth ("Largeur du rim", Range(0, 1)) = 0.25
        _RimSoftness ("Bord du rim", Range(0.001, 0.25)) = 0.02
        _RimIntensity ("Intensité du rim", Range(0, 4)) = 0.5
        _RimLitOnly ("Rim seulement à la lumière", Range(0, 1)) = 0

        [Header(Emission)]
        [Toggle(_EMISSION)] _Emission ("Émission", Float) = 0
        [HDR] _EmissionColor ("Couleur d'émission (HDR)", Color) = (0, 0, 0, 1)
        _EmissionMap ("Masque d'émission", 2D) = "white" {}
        _EmissionThroughFog ("Émission à travers le brouillard", Range(0, 1)) = 0

        [Header(Rendu)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Faces", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardToon"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ToonVert
            #pragma fragment ToonFrag

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _RIM_ON
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _RECEIVE_SHADOWS_OFF

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "ItHunts_ToonInput.hlsl"
            #include "ItHunts_ToonForwardPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ToonShadowVert
            #pragma fragment ToonShadowFrag
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "ItHunts_ToonInput.hlsl"
            #include "ItHunts_ToonUtilityPasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ToonDepthVert
            #pragma fragment ToonDepthFrag
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "ItHunts_ToonInput.hlsl"
            #include "ItHunts_ToonUtilityPasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ToonDepthVert
            #pragma fragment ToonDepthNormalsFrag
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "ItHunts_ToonInput.hlsl"
            #include "ItHunts_ToonUtilityPasses.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
