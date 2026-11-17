// Enragé (lumière d'ambiance horreur, HorrorLook) : bascule du monde vers un rouge sang presque monochrome, passe plein écran avant le
// post-process. La vue FPS (mains, orbe, tablettes, artefact) est marquée dans le stencil (bit 1) par les passes « ItHunts Mains » :
// elle garde la couleur du personnage. Paramètres globaux écrits par HorrorLook : poids (0..1), teinte, gain (compense la luminance
// perdue par le monochrome rouge).
Shader "ItHunts/EnrageMonde"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "Enrage"
            Stencil
            {
                Ref 1
                ReadMask 1
                Comp NotEqual
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _ItHunts_EnrageWeight;
            float4 _ItHunts_EnrageTint;
            float _ItHunts_EnrageGain;

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half4 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);
                half l = dot(c.rgb, half3(0.2126h, 0.7152h, 0.0722h));
                half3 mono = l * _ItHunts_EnrageTint.rgb * _ItHunts_EnrageGain;
                c.rgb = lerp(c.rgb, mono, saturate(_ItHunts_EnrageWeight));
                return c;
            }
            ENDHLSL
        }
    }
}
