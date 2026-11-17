// Contour des mains et des artefacts (vue FPS) : coque inversée (docs/art/RENDU.md § 5 bis, option A). Les mains sont dessinées
// par les passes « ItHunts Mains » après le monde : la passe de contours plein écran ne les voit pas. Ce matériau est ajouté à
// leurs renderers (HandsOutline) : les sommets sont poussés le long de la normale en espace écran (épaisseur en pixels d'une
// image 1080p, comme M_Contours), faces avant coupées, couleur d'encre. Étiquette « ItHuntsOutline » : dessinée seulement par
// la passe des mains (le monde ne la dessine pas). Pas de traits intérieurs (ils viennent de la texture peinte).
Shader "ItHunts/ContourMains"
{
    Properties
    {
        _OutlineColor ("Encre", Color) = (0.01, 0.008, 0.012, 1)
        _Thickness ("Épaisseur (px à 1080p ; négative pour un maillage retourné)", Range(-6, 6)) = 1.5
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Faces cachées (Front ; Back pour un maillage retourné)", Float) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            Name "ItHuntsOutline"
            Tags { "LightMode" = "ItHuntsOutline" }
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _Thickness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float4 cs = TransformObjectToHClip(v.positionOS.xyz);
                float3 nWS = TransformObjectToWorldNormal(v.normalOS);
                float2 nCS = mul((float3x3)UNITY_MATRIX_VP, nWS).xy;
                float2 dir = nCS * _ScreenParams.xy;
                float len = length(dir);
                dir = len > 1e-5 ? dir / len : float2(0, 0);
                float px = _Thickness * _ScreenParams.y / 1080.0;
                cs.xy += dir * px * 2.0 / _ScreenParams.xy * cs.w;
                o.positionCS = cs;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                return half4(_OutlineColor.rgb, 1);
            }
            ENDHLSL
        }
    }
}
