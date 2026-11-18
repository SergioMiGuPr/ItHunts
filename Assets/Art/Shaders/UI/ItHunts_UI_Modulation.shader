// ItHunts : modulation de la lumière déjà peinte (fenêtres, lanternes, halo de lune, reflets des flaques, lueur du feu
// au sol). Mélange « multiplication x2 » : une sortie de 0,5 laisse l'image inchangée, donc au repos (ou gelé) le rendu
// est la maquette au pixel près. Le masque (un canal de _MainTex, choisi par _Canal) dit où la lumière bouge ;
// _Amplitude dit de combien (0,2 = +/-20 %). Modes : vacillement (flamme), pulsation (sinus lent), scintillement (bruit
// fin qui défile, pour les reflets).
Shader "ItHunts/UI/Modulation"
{
    Properties
    {
        [PerRendererData] _MainTex ("Masques (RGBA)", 2D) = "black" {}
        _Color ("Teinte", Color) = (1, 1, 1, 1)
        _Canal ("Canal du masque (R, G, B, A)", Vector) = (1, 0, 0, 0)
        [KeywordEnum(Vacillement, Pulsation, Scintillement)] _Mode ("Mode", Float) = 0
        _Amplitude ("Amplitude (fraction de la lumière)", Range(0, 1)) = 0.2
        _Vitesse ("Vitesse", Range(0, 10)) = 1
        _Echelle ("Échelle spatiale du bruit (désynchronise les sources)", Float) = 0.01
        _Graine ("Graine", Float) = 0
        _Chaleur ("Teinte de la hausse", Color) = (1, 1, 1, 1)

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
        Blend DstColor SrcColor
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma shader_feature_local _MODE_VACILLEMENT _MODE_PULSATION _MODE_SCINTILLEMENT
            #include "ItHunts_UICommun.cginc"
            sampler2D _MainTex; float4 _MainTex_ST; fixed4 _Color; float4 _ClipRect;
            float4 _Canal; float _Amplitude, _Vitesse, _Echelle, _Graine; fixed4 _Chaleur;

            v2f_ui vert(appdata_ui v) { v2f_ui o; ITHUNTS_UI_VERT_COMMUN(v, o); return o; }

            fixed4 frag(v2f_ui i) : SV_Target
            {
                float m = dot(tex2D(_MainTex, i.uv), _Canal) * i.color.a;
                float t = UiTemps() * _Vitesse;
                float k;
                #if defined(_MODE_PULSATION)
                    k = sin(t * 1.3 + _Graine) * 0.7 + sin(t * 0.47 + _Graine * 2.0) * 0.3;
                #elif defined(_MODE_SCINTILLEMENT)
                    float2 p = i.local * _Echelle;
                    k = (Fbm(p * float2(1.0, 3.0) + float2(t * 0.9, -t * 0.35)) - 0.5) * 2.2;
                    k *= 0.6 + 0.4 * sin(t * 2.1 + p.x * 3.0);
                #else
                    float g = Valeur(i.local * _Echelle + _Graine) * 10.0;   // chaque source a sa phase
                    k = Vacille(t, g);
                #endif
                k *= _Amplitude * m * UiActif();
                float3 gain = 1.0 + k * lerp(1.0.xxx, _Chaleur.rgb, saturate(k * 4.0));
                float4 clip = float4(1, 1, 1, 1);
                ITHUNTS_UI_CLIP(i, clip)
                gain = lerp(1.0.xxx, gain, clip.a);
                return fixed4(saturate(gain * 0.5), 1);
            }
            ENDCG
        }
    }
}
