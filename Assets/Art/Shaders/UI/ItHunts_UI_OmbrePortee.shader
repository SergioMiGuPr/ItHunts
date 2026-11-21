// ItHunts : ombre d'un personnage peint (campement), calculée depuis l'alpha de son sprite et posée SOUS lui, en
// multiplication (le décor peint est assombri, jamais voilé de gris). Deux termes :
// - contact : la silhouette floutée, décalée vers le bas et à l'opposé du feu, limitée au bas du personnage (_ContactHaut) :
//   occlusion sous les cuisses sur le siège, autour des pieds et de la queue ;
// - portée : la silhouette projetée sur le sol et la bûche derrière le personnage (le feu est devant lui et sur le côté) :
//   depuis la ligne des pieds (_Sol) vers le haut de l'écran, écrasée (_Ecrase) et cisaillée à l'opposé du feu (_Cisaille),
//   de plus en plus floue et faible en s'éloignant des pieds.
// L'Image qui porte ce matériau est plus grande que le sprite : _Cadre donne les uv du sprite aux coins de l'Image
// (x0, y0, x1, y1). Teinte : celle des ombres du décor (_Couleur). Gelé ou non, l'ombre est fixe.
Shader "ItHunts/UI/OmbrePortee"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite (alpha de la silhouette)", 2D) = "white" {}
        _Color ("Teinte", Color) = (1, 1, 1, 1)
        _Couleur ("Couleur de l'ombre (multipliée)", Color) = (0.32, 0.30, 0.42, 1)
        _Cadre ("Uv du sprite aux coins de l'image (x0, y0, x1, y1)", Vector) = (0, 0, 1, 1)
        _ContactForce ("Contact : force", Range(0, 2)) = 0.85
        _ContactDecal ("Contact : décalage (uv du sprite)", Vector) = (-0.01, -0.02, 0, 0)
        _ContactFlou ("Contact : flou (pixels du sprite)", Range(0, 30)) = 6
        _ContactHaut ("Contact : hauteur gardée (uv y depuis le bas)", Range(0, 1)) = 0.45
        _Sol ("Ligne du sol (uv y des pieds)", Range(-0.5, 1)) = 0.03
        _PorteeForce ("Portée : force", Range(0, 2)) = 0.6
        _Ecrase ("Portée : écrasement vertical", Range(0.02, 1)) = 0.16
        _Cisaille ("Portée : cisaillement (uv x par uv de hauteur, signe = côté opposé au feu)", Range(-3, 3)) = -0.6
        _PorteeFlou ("Portée : flou au pied (pixels du sprite)", Range(0, 30)) = 4
        _PorteeFlouHaut ("Portée : flou au bout (pixels du sprite)", Range(0, 60)) = 22
        _PorteeLongueur ("Portée : hauteur projetée prise en compte (uv)", Range(0.05, 1.5)) = 0.8
        _Max ("Opacité maximale", Range(0, 1)) = 0.8

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
        Blend DstColor Zero
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "ItHunts_UICommun.cginc"
            sampler2D _MainTex; float4 _MainTex_ST; float4 _MainTex_TexelSize; fixed4 _Color; float4 _ClipRect;
            fixed4 _Couleur; float4 _Cadre, _ContactDecal;
            float _ContactForce, _ContactFlou, _ContactHaut, _Sol, _PorteeForce, _Ecrase, _Cisaille, _PorteeFlou, _PorteeFlouHaut, _PorteeLongueur, _Max;

            float A(float2 uv)
            {
                float dedans = step(0.0, uv.x) * step(uv.x, 1.0) * step(0.0, uv.y) * step(uv.y, 1.0);
                return tex2Dlod(_MainTex, float4(uv, 0, 0)).a * dedans;
            }

            // alpha flouté : centre + deux anneaux de 6 prises (rayon r et r / 2, pixels du sprite)
            float AFlou(float2 uv, float r)
            {
                float2 px = _MainTex_TexelSize.xy * r;
                float s = A(uv) * 2.0, n = 2.0;
                [unroll] for (int k = 0; k < 6; k++)
                {
                    float an = k * 1.0472;
                    float2 d = float2(cos(an), sin(an));
                    float2 d2 = float2(cos(an + 0.5236), sin(an + 0.5236));
                    s += A(uv + d * px) + A(uv + d2 * px * 0.5) * 1.5;
                    n += 2.5;
                }
                return s / n;
            }

            v2f_ui vert(appdata_ui v)
            {
                v2f_ui o;
                ITHUNTS_UI_VERT_COMMUN(v, o);
                return o;
            }

            fixed4 frag(v2f_ui i) : SV_Target
            {
                float2 p = lerp(_Cadre.xy, _Cadre.zw, i.uv);
                // contact : silhouette floutée et décalée, bas du personnage seulement
                float2 q = p - _ContactDecal.xy;
                float garde = 1.0 - smoothstep(_ContactHaut * 0.6, _ContactHaut, q.y);
                float c = AFlou(q, _ContactFlou) * garde * _ContactForce;
                // portée : le feu est devant et sur le côté, l'ombre part vers l'arrière (plus haut à l'écran) et à
                // l'opposé du feu : au-dessus de la ligne des pieds, la silhouette projetée (écrasée, cisaillée)
                float h = (p.y - _Sol) / _Ecrase;                     // hauteur de la silhouette qui tombe ici
                float dansSol = step(0.0, h) * (1.0 - smoothstep(_PorteeLongueur * 0.5, _PorteeLongueur, h));
                float2 qp = float2(p.x - _Cisaille * h, _Sol + h);
                float fl = lerp(_PorteeFlou, _PorteeFlouHaut, saturate(h / _PorteeLongueur));
                float pr = h >= 0.0 ? AFlou(qp, fl) * dansSol * _PorteeForce * (1.0 - 0.6 * saturate(h / _PorteeLongueur)) : 0.0;
                float o = saturate(1.0 - (1.0 - saturate(c)) * (1.0 - saturate(pr))) * _Max * i.color.a;
                fixed4 col = fixed4(lerp(float3(1, 1, 1), _Couleur.rgb, o), 1);
                #ifdef UNITY_UI_CLIP_RECT
                col.rgb = lerp(float3(1, 1, 1), col.rgb, UnityGet2DClipping(i.worldPosition.xy, _ClipRect));
                #endif
                return col;
            }
            ENDCG
        }
    }
}
