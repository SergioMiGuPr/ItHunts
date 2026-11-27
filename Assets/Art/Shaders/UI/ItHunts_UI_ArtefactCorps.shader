// ItHunts : corps peint d'un artefact de l'écran de choix (mélange alpha classique). Flotte avec ses lumières (même
// décalage d'UV) ; option « pages » : frémissement des pages d'un livre, poids lu dans _Pages (R, nul à la reliure,
// maximal aux bords libres). Le frémissement vient par bouffées (_PeriodePages) : un frisson rapide qui s'amortit, sur
// un fond de respiration très lente, avec une onde qui court le long des pages.
Shader "ItHunts/UI/ArtefactCorps"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Teinte", Color) = (1, 1, 1, 1)
        _Flotte ("Flottement (fraction de la hauteur)", Float) = 0.012
        _FlottePeriode ("Période du flottement (s)", Float) = 5.5
        _Periode ("Période (inutilisée)", Float) = 3.6
        _Graine ("Graine", Float) = 0
        [Toggle(_PAGES)] _AvecPages ("Pages qui frémissent", Float) = 0
        _Pages ("Poids des pages (R)", 2D) = "black" {}
        _Frisson ("Amplitude du frisson (fraction de la hauteur)", Float) = 0.004
        _PeriodePages ("Intervalle des frissons (s)", Float) = 3.4

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" "CanUseSpriteAtlas" = "True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        ColorMask [_ColorMask]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma shader_feature_local _ _PAGES
            #include "ItHunts_UIArtefact.cginc"
            sampler2D _MainTex; float4 _MainTex_ST; fixed4 _Color; float4 _ClipRect;
            sampler2D _Pages; float _Frisson, _PeriodePages;

            v2f_ui vert(appdata_ui v) { v2f_ui o; ITHUNTS_UI_VERT_COMMUN(v, o); return o; }

            fixed4 frag(v2f_ui i) : SV_Target
            {
                float2 uv = i.uv - Flottement();
                #if defined(_PAGES)
                    float w = tex2D(_Pages, uv).r;
                    float t = UiTemps();
                    float cyc = t / max(_PeriodePages, 0.1);
                    float ph = frac(cyc) * _PeriodePages;                       // secondes depuis la bouffée
                    float force = 0.55 + 0.45 * Hash21(float2(floor(cyc), _Graine));
                    float bouffee = exp(-ph * 3.2) * sin(ph * 34.0 - uv.x * 40.0) * force;
                    float fond = sin(t * 1.9 + uv.x * 9.0) * 0.18;
                    float d = (bouffee + fond) * w * _Frisson * UiActif();
                    uv += float2(d * 0.35, d);
                #endif
                fixed4 c = tex2D(_MainTex, uv) * i.color;
                ITHUNTS_UI_CLIP(i, c)
                return c;
            }
            ENDCG
        }
    }
}
