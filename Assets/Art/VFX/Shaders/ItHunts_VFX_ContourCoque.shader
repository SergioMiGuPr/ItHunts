// Contour discret collé à la silhouette (nuit 4, lot 7) : coque inversée (le maillage gonflé le long des normales, faces
// avant écartées), semi-transparente, sans éclairage. Sert à l'annonce du piqué des volants (encre rouge sang éteint, pulsée)
// et au volant tenu par la télékinésie (ton d'os neutre, fixe). Pas de boule, pas de remplissage : seul le bord de la
// silhouette est cerné. Posé par ContourDiscret sur des copies des rendus de l'ennemi.
Shader "ItHunts/VFX/ContourCoque"
{
    Properties
    {
        _Couleur ("Couleur du contour", Color) = (0.45, 0.07, 0.05, 1)
        _Epaisseur ("Épaisseur (m)", Range(0.002, 0.08)) = 0.018
        _Opacite ("Opacité", Range(0, 1)) = 0.8
        _Pulsation ("Pulsation (Hz, 0 : fixe)", Range(0, 6)) = 0
        _PulsationMin ("Opacité au creux de la pulsation (fraction)", Range(0, 1)) = 0.35
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Front
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _Couleur; float _Epaisseur, _Opacite, _Pulsation, _PulsationMin;
            CBUFFER_END
            struct A { float4 pos : POSITION; float3 n : NORMAL; };
            struct V { float4 pos : SV_POSITION; };
            V vert(A i)
            {
                V o;
                float3 w = TransformObjectToWorld(i.pos.xyz);
                float3 n = normalize(TransformObjectToWorldNormal(i.n));
                o.pos = TransformWorldToHClip(w + n * _Epaisseur);
                return o;
            }
            half4 frag(V i) : SV_Target
            {
                float p = _Pulsation > 0 ? lerp(_PulsationMin, 1, 0.5 + 0.5 * sin(_Time.y * 6.2831 * _Pulsation)) : 1;
                return half4(_Couleur.rgb, _Opacite * p);
            }
            ENDHLSL
        }
    }
}
