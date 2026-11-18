// ItHunts : bouton découpé des maquettes. La couleur du sommet est lue x2 : gris 0,5 = pixels d'origine, au-dessus plus
// lumineux, en dessous plus sombre. Les états (normal, survol, pressé, désactivé) se règlent dans le ColorBlock du Button
// (transition Color Tint), teinte et luminosité seulement, sans redessiner le bouton.
Shader "ItHunts/UI/Bouton"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Teinte", Color) = (1, 1, 1, 1)

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
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "ItHunts_UICommun.cginc"
            sampler2D _MainTex; float4 _MainTex_ST; fixed4 _Color; float4 _ClipRect;
            v2f_ui vert(appdata_ui v) { v2f_ui o; ITHUNTS_UI_VERT_COMMUN(v, o); return o; }
            fixed4 frag(v2f_ui i) : SV_Target
            {
                fixed4 t = tex2D(_MainTex, i.uv);
                // la teinte des états est donnée en gris « perceptif » (0,5 = inchangé) : en espace linéaire, uGUI
                // convertit la couleur du sommet, on la ramène donc en gamma avant de la lire x2
                float3 g = i.color.rgb;
                #ifndef UNITY_COLORSPACE_GAMMA
                    g = LinearToGammaSpace(g);
                #endif
                fixed4 c = fixed4(saturate(t.rgb * g * 2.0), t.a * i.color.a);
                ITHUNTS_UI_CLIP(i, c)
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
