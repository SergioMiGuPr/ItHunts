// Brume sous le plafond (seconde revue de la DA, D-120) : plan transparent posé juste sous le plafond des actes I et II. Presque
// invisible au-dessus du joueur, il s'épaissit avec la distance : le plafond lointain se fond dans un voile chaud au lieu d'un aplat
// noir, puis dans le brouillard de la scène. Sans ombre, double face, sans écriture de profondeur.
Shader "ItHunts/BrumePlafond"
{
    Properties
    {
        _Color ("Couleur du voile", Color) = (0.08, 0.055, 0.045, 1)
        _MaxAlpha ("Opacité au loin", Range(0, 1)) = 0.7
        _Start ("Début du voile (m)", Range(0, 60)) = 10
        _Range ("Longueur du dégradé (m)", Range(1, 120)) = 45
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent-10" "IgnoreProjector" = "True" "PreviewType" = "Plane" }

        Pass
        {
            Name "Brume"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _MaxAlpha;
                half _Start;
                half _Range;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 ws : TEXCOORD0;
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                float3 ws = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(ws);
                o.ws = ws;                                  // distance au pixel (le plan est grand : pas d'interpolation)
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float d = distance(i.ws, GetCameraPositionWS());
                half t = saturate((d - _Start) / _Range);
                half a = _MaxAlpha * t * t * (3 - 2 * t);
                // Brouillard linéaire de la scène (SceneAmbiance), calculé au pixel : au loin le voile prend sa couleur.
                half f = saturate(d * unity_FogParams.z + unity_FogParams.w);
                half3 c = lerp(unity_FogColor.rgb, _Color.rgb, f);
                return half4(c, a);
            }
            ENDHLSL
        }
    }
}
