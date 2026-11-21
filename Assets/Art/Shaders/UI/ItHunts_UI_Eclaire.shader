// ItHunts : lumière posée sur la peinture (lueur d'une lanterne sur le sol, la bûche, le siège), en éclaircissement :
// couleur finale = décor x (1 + lumière). Contrairement à un ajout, les sombres restent sombres et les textures peintes
// gardent leur grain : la lumière se lit comme reçue par les surfaces, pas comme un brouillard coloré.
// Forme : ellipse douce (gaussienne) remplissant l'Image, centre décalable (_Centre en uv), force et vacillement.
Shader "ItHunts/UI/Eclaire"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite (inutilisé)", 2D) = "white" {}
        _Color ("Couleur de la lumière (alpha = force)", Color) = (1, 0.6, 0.4, 1)
        _Force ("Force", Range(0, 6)) = 1.6
        _Centre ("Centre (uv) et rayon de la zone pleine (z)", Vector) = (0.5, 0.5, 0.0, 0)
        _Durete ("Dureté de la chute (exposant)", Range(0.5, 4)) = 1.4
        _Vacille ("Vacillement", Range(0, 1)) = 0.25
        _Vitesse ("Vacillement : vitesse", Range(0, 4)) = 1.2
        _Graine ("Graine", Float) = 1

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        ColorMask [_ColorMask]
        Blend DstColor One
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "ItHunts_UICommun.cginc"
            sampler2D _MainTex; float4 _MainTex_ST; fixed4 _Color; float4 _ClipRect;
            float4 _Centre; float _Force, _Durete, _Vacille, _Vitesse, _Graine;

            v2f_ui vert(appdata_ui v)
            {
                v2f_ui o;
                ITHUNTS_UI_VERT_COMMUN(v, o);
                return o;
            }

            fixed4 frag(v2f_ui i) : SV_Target
            {
                // distance normalisée à l'ellipse inscrite dans l'Image (1 au bord)
                float2 d = (i.uv - _Centre.xy) * 2.0;
                float r = length(d);
                float k = saturate((r - _Centre.z) / max(1.0 - _Centre.z, 1e-3));
                float f = pow(saturate(1.0 - k), _Durete) * exp(-k * k * 2.0);
                float fl = 1.0 + Vacille(UiTemps() * _Vitesse, _Graine) * _Vacille * UiActif();
                float3 l = i.color.rgb * i.color.a * _Force * f * fl;
                #ifdef UNITY_UI_CLIP_RECT
                l *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                return fixed4(l, 0);
            }
            ENDCG
        }
    }
}
