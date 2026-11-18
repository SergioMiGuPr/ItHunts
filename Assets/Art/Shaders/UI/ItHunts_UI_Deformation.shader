// ItHunts : mouvement « sur place » des personnages et des artefacts peints, par déformation de l'image (quelques
// pixels) : respiration (étirement vertical autour de la ligne _Pivot, uv y), cape qui ondule (décalage horizontal qui
// grandit vers le bas), flottement (décalage vertical du graphique), orbite des traînées de lumière (rotation dans une
// ellipse). Poids : texture _Poids (R respiration, G cape, B phase) si _POIDS_TEXTURE, sinon profil vertical.
// Gelé (_ItHuntsMenuFige = 1) : aucune déformation, le rendu est l'image peinte.
Shader "ItHunts/UI/Deformation"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Teinte", Color) = (1, 1, 1, 1)
        [Toggle(_POIDS_TEXTURE)] _UsePoids ("Poids par texture", Float) = 0
        _Poids ("Poids (R respiration, G cape, B phase)", 2D) = "white" {}
        _Pivot ("Ligne fixe de la respiration (uv y)", Range(0, 1)) = 0.2
        _Respiration ("Respiration (pixels au sommet)", Range(0, 6)) = 1.6
        _Rythme ("Respirations par seconde", Range(0, 1)) = 0.22
        _Cape ("Cape (pixels)", Range(0, 6)) = 1.2
        _CapeHaut ("Début de la cape (depuis le haut, 0..1)", Range(0, 1)) = 0.45
        _Phase ("Phase", Range(0, 1)) = 0
        _Flottement ("Flottement (pixels)", Range(0, 20)) = 0
        _FlotteVitesse ("Flottement (cycles par seconde)", Range(0, 2)) = 0.35
        _Orbite ("Orbite (tours par seconde, 0 = aucune)", Range(-0.5, 0.5)) = 0
        _Centre ("Centre de l'orbite (uv x y), aplatissement (z)", Vector) = (0.5, 0.5, 0.45, 0)
        _Opaque ("Opaque (recadrage du fond)", Range(0, 1)) = 0

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
            #pragma shader_feature_local _ _POIDS_TEXTURE
            #include "ItHunts_UICommun.cginc"
            sampler2D _MainTex; float4 _MainTex_ST; float4 _MainTex_TexelSize; fixed4 _Color; float4 _ClipRect;
            sampler2D _Poids;
            float _Pivot, _Respiration, _Rythme, _Cape, _CapeHaut, _Phase, _Flottement, _FlotteVitesse, _Orbite, _Opaque;
            float4 _Centre;

            v2f_ui vert(appdata_ui v)
            {
                v2f_ui o;
                v.vertex.y += sin((UiTemps() * _FlotteVitesse + _Phase) * 6.2832) * _Flottement * UiActif();
                ITHUNTS_UI_VERT_COMMUN(v, o);
                return o;
            }

            fixed4 frag(v2f_ui i) : SV_Target
            {
                float actif = UiActif();
                float t = UiTemps();
                float2 uv = i.uv;
                float wb = 1.0, wc, ph = _Phase;
                #if defined(_POIDS_TEXTURE)
                    float4 w = tex2D(_Poids, uv);
                    wb = w.r; wc = w.g; ph = w.b;
                #else
                    wc = smoothstep(_CapeHaut, 1.0, 1.0 - uv.y) * smoothstep(0.0, 0.08, uv.y);
                #endif
                float hpx = 1.0 / _MainTex_TexelSize.y;
                // respiration : uv y étiré autour du pivot, au plus _Respiration pixels au sommet
                float s = sin((t * _Rythme + ph) * 6.2832) * 0.5 + 0.5;
                float k = _Respiration * s / max(hpx * (1.0 - _Pivot), 1.0) * wb * actif;
                uv.y = _Pivot + (uv.y - _Pivot) / (1.0 + k);
                // cape : onde horizontale
                float wave = sin(t * 1.7 + uv.y * 9.0 + ph * 6.2832) * 0.6 + sin(t * 2.9 + uv.y * 17.0 + ph * 3.0) * 0.4;
                uv.x += wave * _Cape * wc * _MainTex_TexelSize.x * actif;
                // orbite (traînées de lumière) : rotation dans l'ellipse
                if (abs(_Orbite) > 0.0)
                {
                    float a = t * _Orbite * 6.2832 * actif;
                    float2 d = uv - _Centre.xy;
                    float asp = _MainTex_TexelSize.y / _MainTex_TexelSize.x;
                    d.x /= asp; d.y /= max(_Centre.z, 0.05);
                    float cs = cos(a), sn = sin(a);
                    d = float2(d.x * cs - d.y * sn, d.x * sn + d.y * cs);
                    d.y *= max(_Centre.z, 0.05); d.x *= asp;
                    uv = _Centre.xy + d;
                }
                fixed4 c = tex2D(_MainTex, uv) * i.color;
                c.a = lerp(c.a, i.color.a, _Opaque);
                ITHUNTS_UI_CLIP(i, c)
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
