// Carte ouverte (Tab), UI-HANDOFF § 2 bis : deux passes d'interface.
// - Pass 0 « Encre » : la texture des salles découvertes (alpha) dessinée en traits d'encre claire (#CDBEA5) sur leurs bords,
//   avec un remplissage très léger. Épaisseur du trait en texels (_Ink.w).
// - « ItHunts/UICarteBrume » (plus bas) : brume de la zone non découverte, raccordable (frac des UV), masquée par les salles
//   découvertes (adoucies) et par le pinceau de dévoilement peint au passage des joueurs.
Shader "ItHunts/UICarteEncre"
{
    Properties
    {
        [PerRendererData] _MainTex ("Salles découvertes (alpha)", 2D) = "black" {}
        _Ink ("Encre (rgb) et épaisseur (texels, w)", Vector) = (0.804, 0.745, 0.647, 1.5)
        _Fill ("Remplissage (alpha)", Range(0, 1)) = 0.07
        _Color ("Teinte", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" "CanUseSpriteAtlas" = "True" }
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            sampler2D _MainTex; float4 _MainTex_TexelSize; float4 _Ink; float _Fill; float4 _Color;
            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color * _Color; return o; }
            float A(float2 uv) { return tex2D(_MainTex, uv).a > 0.3 ? 1.0 : 0.0; }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 d = _MainTex_TexelSize.xy * _Ink.w;
                float c = A(i.uv);
                float n = A(i.uv + float2(d.x, 0)) + A(i.uv - float2(d.x, 0)) + A(i.uv + float2(0, d.y)) + A(i.uv - float2(0, d.y));
                float edge = c * saturate(4.0 - n) + (1.0 - c) * saturate(n) * 0.0;   // le bord intérieur des salles
                float a = max(saturate(edge), c * _Fill);
                return fixed4(_Ink.rgb, a) * i.color;
            }
            ENDCG
        }
    }
}
