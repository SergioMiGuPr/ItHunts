// Halo de lumière (lumière d'ambiance horreur, HorrorLook) : billboard additif d'un système de particules autour des torches, des
// lampes et des lanternes. Dégradé radial calculé (pas de texture), couleur et alpha des particules ; il s'efface dans le brouillard
// (comme la lumière qu'il entoure) et tout près de la caméra (sa propre lanterne ne voile pas l'écran). Sans ombre, double face.
Shader "ItHunts/Halo"
{
    Properties
    {
        _Intensity ("Intensité", Range(0, 4)) = 1
        _Falloff ("Dégradé (puissance)", Range(0.5, 6)) = 2.2
        _NearFade ("Fondu près de la caméra (m)", Range(0, 4)) = 1.2
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }

        Pass
        {
            Name "Halo"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _Intensity;
                half _Falloff;
                half _NearFade;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float fog : TEXCOORD1;
                float dist : TEXCOORD2;
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                float3 ws = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(ws);
                o.color = v.color;
                o.uv = v.uv;
                o.fog = ComputeFogFactor(o.positionCS.z);
                o.dist = distance(ws, GetCameraPositionWS());
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float2 d = i.uv * 2 - 1;
                half r = saturate(1 - length(d));
                half a = pow(r, _Falloff) * i.color.a * _Intensity;
                a *= _NearFade > 0 ? saturate((i.dist - 0.3) / _NearFade) : 1;
                half3 c = i.color.rgb * a;
                // Additif : le brouillard l'efface (vers le noir), comme la lumière qu'il entoure.
                c = MixFogColor(c, half3(0, 0, 0), i.fog);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
