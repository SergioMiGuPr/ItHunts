// ItHunts : contours encrés en plein écran (bords de profondeur + normales), pour la Full Screen Pass Renderer Feature
// d'URP. Injection : « Before Rendering Transparents » (avant les passes des mains), Requirements : Depth + Normal,
// Fetch Color Buffer coché. Le contour s'efface avec le brouillard de la scène et avec la distance.
// Modes : 0 = contour de couleur (noir) partout ; 1 = noir à la lumière, un peu plus clair que le fond dans le noir.
Shader "ItHunts/Contours"
{
    Properties
    {
        [KeywordEnum(Noir, ClairDansLeNoir)] _Mode ("Mode", Float) = 0
        _Thickness ("Épaisseur (px à 1080p)", Range(0.5, 4)) = 1.5
        _OutlineColor ("Couleur du contour", Color) = (0.01, 0.008, 0.012, 1)
        _OutlineOpacity ("Opacité", Range(0, 1)) = 1

        [Header(Profondeur)]
        _DepthThreshold ("Seuil profondeur (relatif)", Range(0.001, 0.5)) = 0.06
        _DepthSoftness ("Douceur profondeur", Range(0.001, 0.5)) = 0.03
        _GrazingBoost ("Seuil × aux angles rasants", Range(0, 20)) = 6

        [Header(Normales)]
        _NormalThreshold ("Seuil normales (1 - cos)", Range(0.01, 1)) = 0.3
        _NormalSoftness ("Douceur normales", Range(0.001, 0.5)) = 0.1

        [Header(Distance)]
        _FadeStart ("Fondu : début (m)", Float) = 40
        _FadeEnd ("Fondu : fin (m)", Float) = 120
        [Toggle] _UseFog ("Suivre le brouillard de la scène", Float) = 1

        [Header(Mode clair dans le noir)]
        _LiftColor ("Couleur ajoutée dans le noir", Color) = (0.07, 0.075, 0.1, 1)
        _LiftThreshold ("Luminance sous laquelle le contour s'éclaircit", Range(0.005, 0.5)) = 0.06
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "Contours"

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_local_fragment _MODE_NOIR _MODE_CLAIRDANSLENOIR
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                half  _OutlineOpacity;
                float _Thickness;
                float _DepthThreshold;
                float _DepthSoftness;
                float _GrazingBoost;
                float _NormalThreshold;
                float _NormalSoftness;
                float _FadeStart;
                float _FadeEnd;
                float _UseFog;
                half4 _LiftColor;
                half  _LiftThreshold;
            CBUFFER_END

            float EyeDepth(float2 uv)
            {
                return LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
            }

            bool IsSky(float rawDepth)
            {
            #if UNITY_REVERSED_Z
                return rawDepth <= 1e-6;
            #else
                return rawDepth >= 1.0 - 1e-6;
            #endif
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 scene = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, uv, 0);

                float rawCenter = SampleSceneDepth(uv);
                float dC = LinearEyeDepth(rawCenter, _ZBufferParams);

                // Épaisseur en pixels de l'image de référence 1080p, quelle que soit la résolution.
                float2 texel = (_Thickness * _ScreenParams.y / 1080.0) / _ScreenParams.xy;
                float2 o1 = float2(texel.x, texel.y);
                float2 o2 = float2(-texel.x, texel.y);

                // Croix de Roberts : deux diagonales.
                float d1 = EyeDepth(uv + o1), d2 = EyeDepth(uv - o1);
                float d3 = EyeDepth(uv + o2), d4 = EyeDepth(uv - o2);
                float3 nC = SampleSceneNormals(uv);
                float3 n1 = SampleSceneNormals(uv + o1), n2 = SampleSceneNormals(uv - o1);
                float3 n3 = SampleSceneNormals(uv + o2), n4 = SampleSceneNormals(uv - o2);

                // Angle rasant : on relève le seuil de profondeur pour éviter de noircir les sols vus de loin.
                float3 viewDirWS = normalize(GetCameraPositionWS() - ComputeWorldSpacePosition(uv, rawCenter, UNITY_MATRIX_I_VP));
                float ndv = saturate(dot(nC, viewDirWS));
                float depthThreshold = _DepthThreshold * (1.0 + _GrazingBoost * pow(1.0 - ndv, 4.0));

                float depthDiff = (abs(d1 - d2) + abs(d3 - d4)) / max(dC, 1e-3);
                float edgeDepth = smoothstep(depthThreshold, depthThreshold + _DepthSoftness, depthDiff);

                float normalDiff = max(1.0 - dot(n1, n2), 1.0 - dot(n3, n4));
                float edgeNormal = smoothstep(_NormalThreshold, _NormalThreshold + _NormalSoftness, normalDiff);
                // Le ciel n'a pas de normale fiable : seul le bord de profondeur compte contre lui.
                if (IsSky(rawCenter))
                    edgeNormal = 0;

                float edge = saturate(max(edgeDepth, edgeNormal)) * _OutlineOpacity;

                // Fondu : distance, puis brouillard de la scène (le contour disparaît avec le décor).
                float dNear = min(min(min(d1, d2), min(d3, d4)), dC);
                float fade = 1.0 - smoothstep(_FadeStart, _FadeEnd, dNear);
                if (_UseFog > 0.5)
                {
                    // Visibilité à travers le brouillard (1 sans brouillard) : même calcul que le décor.
                    float fogFactor = ComputeFogFactorZ0ToFar(dNear);
                    fade *= MixFogColor(float3(1, 1, 1), float3(0, 0, 0), fogFactor).r;
                }
                edge *= fade;

                half3 ink = _OutlineColor.rgb;
            #if defined(_MODE_CLAIRDANSLENOIR)
                // Dans le noir : le trait relève un peu le fond au lieu de le noircir (arêtes lisibles sans lumière).
                half lum = Luminance(scene.rgb);
                half dark = 1.0h - smoothstep(0.0h, _LiftThreshold, lum);
                ink = lerp(ink, scene.rgb + _LiftColor.rgb, dark);
            #endif
                return half4(lerp(scene.rgb, ink, edge), scene.a);
            }
            ENDHLSL
        }
    }
}
