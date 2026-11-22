// ItHunts : cel-shading peint (bandes réglables du net au dégradé, lumières ponctuelles qui s'éteignent en douceur
// jusqu'au noir et teintent les surfaces, rim, émission HDR).
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
        [IntRange] _BandCount ("Nombre de bandes (ombre comprise)", Range(2, 8)) = 3
        _BandThreshold ("Seuil de la première bande", Range(0, 1)) = 0.15
        _BandSoftness ("Largeur des transitions", Range(0.001, 0.5)) = 0.02
        _BandGradient ("Dégradé sous les bandes (0 : net, 1 : continu)", Range(0, 1)) = 0.45
        _ShadowColor ("Teinte de la bande d'ombre", Color) = (0.028, 0.034, 0.075, 1)
        _AmbientInfluence ("Part de l'ambiance de la scène", Range(0, 2)) = 1
        [ToggleOff(_RECEIVE_SHADOWS_OFF)] _ReceiveShadows ("Reçoit les ombres", Float) = 1

        [Header(Lumieres ponctuelles)]
        [IntRange] _PointSteps ("Paliers d'atténuation", Range(1, 8)) = 3
        _PointThreshold ("Position du premier palier", Range(0.05, 1)) = 0.35
        _PointSoftness ("Bord des paliers", Range(0.001, 0.5)) = 0.015
        _PointFalloff ("Courbe de portée (< 1 : halo large, > 1 : resserré)", Range(0.2, 3)) = 0.8
        _PointWrap ("Enveloppe (faces de biais)", Range(0, 1)) = 0.2
        _PointGradient ("Dégradé jusqu'au noir (0 : paliers nets, 1 : continu)", Range(0, 1)) = 0.8
        _LightSaturation ("Couleur des lumières sur les surfaces (saturation)", Range(0, 2)) = 1.35

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

        [Header(UV en coordonnees monde (architecture))]
        [Toggle(_WORLD_UV)] _WorldUV ("UV en coordonnées monde", Float) = 0
        _TileSize ("Taille de la tuile en mètres (x : largeur, y : hauteur ou profondeur)", Vector) = (4, 4, 0, 0)
        _TileOffset ("Décalage de la grille en mètres (x, y, z)", Vector) = (0, 0, 0, 0)
        [Enum(0 deg, 0, 90 deg, 1, 180 deg, 2, 270 deg, 3)] _TopRotation ("Rotation des dalles (dessus et dessous)", Float) = 0
        [Toggle] _TopRandomRotation ("Quart de tour au hasard par tuile (dessus et dessous)", Float) = 0

        [Header(Decor (variation, bas des murs sale et humide, couche melangee en hauteur))]
        // Mode décor (lot matériaux, 2026-11-22) : docs/art/MATERIAUX.md, section bibliothèque. Alpha de la texture de
        // couleur = hauteur peinte (joints bas, pierres hautes) : la saleté remplit d'abord les joints, la couche monte
        // par les creux.
        [Toggle(_DECOR)] _Decor ("Mode décor", Float) = 0
        _MacroVariation ("Variation à grande échelle : force (aplats)", Range(0, 1)) = 0.35
        _MacroScale ("Variation : taille des taches (m)", Float) = 6
        _MacroTint ("Variation : teinte des taches", Color) = (0.78, 0.82, 0.74, 1)
        [Toggle] _VertexPaint ("Peinture de sommets (R saleté, G couche, B teinte par pièce)", Float) = 0
        _GrimeColor ("Saleté : couleur multipliée", Color) = (0.42, 0.40, 0.36, 1)
        _GrimeHeight ("Saleté : hauteur de la bande (m)", Float) = 1.8
        _GrimeGroundY ("Saleté : hauteur du sol (m, monde ; sans peinture de sommets)", Float) = 0
        _GrimeStrength ("Saleté : force", Range(0, 1)) = 0.75
        _WetColor ("Humidité : teinte des coulures", Color) = (0.55, 0.62, 0.62, 1)
        _WetStrength ("Humidité : coulures sous la bande sale", Range(0, 1)) = 0.5
        _LayerMap ("Couche (mousse, salpêtre… ; alpha = hauteur)", 2D) = "black" {}
        _LayerTile ("Couche : taille de la tuile (m)", Float) = 2
        _LayerAmount ("Couche : quantité partout", Range(0, 1)) = 0
        _LayerLow ("Couche : en bas des murs", Range(0, 1)) = 0.5
        _LayerTop ("Couche : sur les dessus", Range(0, 1)) = 0
        _HeightBlend ("Mélange en hauteur : largeur de la transition", Range(0.01, 0.5)) = 0.06

        [Header(Rendu)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Faces", Float) = 2
        // Translucide (verre de la lanterne) : SrcAlpha / OneMinusSrcAlpha, ZWrite Off, file 3000, et passes
        // ShadowCaster, DepthOnly et DepthNormals coupées sur le matériau. Par défaut : opaque (One / Zero, ZWrite On).
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Mélange : source", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Mélange : destination", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite ("Écrit la profondeur", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardToon"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ToonVert
            #pragma fragment ToonFrag

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _RIM_ON
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local_fragment _WORLD_UV
            #pragma shader_feature_local _DECOR

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _LIGHT_LAYERS
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
