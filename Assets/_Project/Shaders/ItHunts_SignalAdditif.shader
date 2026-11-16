// Signal de survie (bloc 4 de rendu-integration) : lueur additive qui traverse le brouillard (colonne rouge de la zone de prise du
// traqueur, halo des âmes). Couleur HDR (au-delà du seuil du bloom), sans ombre ni brouillard, double face.
Shader "ItHunts/SignalAdditif"
{
    Properties
    {
        [HDR] _BaseColor ("Couleur (HDR)", Color) = (1, 0.1, 0.05, 1)
        _EdgeFade ("Fondu des bords (0 : aucun)", Range(0, 4)) = 1.5
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Signal"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _EdgeFade;
            CBUFFER_END

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(half4, _InstanceColor)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                float3 ws = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(ws);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.viewWS = GetWorldSpaceViewDir(ws);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                // Plus lumineux face à la caméra, s'efface sur les bords (silhouette douce d'une lueur).
                float facing = abs(dot(normalize(i.normalWS), normalize(i.viewWS)));
                float k = _EdgeFade > 0 ? pow(saturate(facing), _EdgeFade) : 1;
                return half4(_BaseColor.rgb * k, 1);
            }
            ENDHLSL
        }
    }
}
