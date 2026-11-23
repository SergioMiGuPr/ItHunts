// Annonce discrète (nuit 4, lot 7) : remplace la boule rouge vif des volants (annonce du piqué, « Flyer_Telegraph ») et sert
// à tout signal d'annonce sur une sphère. Le centre reste transparent ; seul un fin liseré rouge sang éteint, sombre, dessine
// le contour (fresnel), avec une pulsation lente. Aucune couleur vive, aucune boule pleine : on voit le volant au travers.
Shader "ItHunts/VFX/HaloSombre"
{
    Properties
    {
        _Couleur ("Liseré", Color) = (0.42, 0.06, 0.05, 1)
        _Puissance ("Finesse du liseré (fresnel)", Range(1, 8)) = 3.5
        _Opacite ("Opacité maximale", Range(0, 1)) = 0.55
        _Pulsation ("Pulsation (Hz)", Range(0, 6)) = 2.2
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _Couleur; float _Puissance, _Opacite, _Pulsation;
            CBUFFER_END
            struct A { float4 pos : POSITION; float3 n : NORMAL; };
            struct V { float4 pos : SV_POSITION; float3 n : TEXCOORD0; float3 v : TEXCOORD1; };
            V vert(A i)
            {
                V o; float3 w = TransformObjectToWorld(i.pos.xyz);
                o.pos = TransformWorldToHClip(w); o.n = TransformObjectToWorldNormal(i.n); o.v = GetWorldSpaceViewDir(w);
                return o;
            }
            half4 frag(V i) : SV_Target
            {
                float f = pow(1 - saturate(dot(normalize(i.n), normalize(i.v))), _Puissance);
                float p = 0.75 + 0.25 * sin(_Time.y * 6.2831 * _Pulsation);
                return half4(_Couleur.rgb, saturate(f * _Opacite * p));
            }
            ENDHLSL
        }
    }
}
