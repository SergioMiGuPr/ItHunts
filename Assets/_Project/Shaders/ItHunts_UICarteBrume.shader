// Brume de la zone non découverte (carte ouverte, UI-HANDOFF § 2 bis) : Carte_Brume raccordable (répétée par frac des UV, la
// texture reste en Clamp), multipliée par (1 − masque). Masque = salles découvertes (adoucies) et pinceau de dévoilement (au
// passage des joueurs). Les deux masques couvrent l'emprise du niveau (mêmes UV que la carte).
Shader "ItHunts/UICarteBrume"
{
    Properties
    {
        [PerRendererData] _MainTex ("Brume (raccordable)", 2D) = "white" {}
        _Rooms ("Salles découvertes (alpha)", 2D) = "black" {}
        _Brush ("Pinceau de dévoilement (r)", 2D) = "black" {}
        _Tiling ("Répétition de la brume (x, y)", Vector) = (3, 3, 0, 0)
        _Soft ("Adoucissement des salles (texels)", Float) = 6
        _Color ("Teinte", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
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
            sampler2D _MainTex; sampler2D _Rooms; float4 _Rooms_TexelSize; sampler2D _Brush; float4 _Tiling; float _Soft; float4 _Color;
            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color * _Color; return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 fog = tex2D(_MainTex, frac(i.uv * _Tiling.xy));
                float2 d = _Rooms_TexelSize.xy * _Soft;
                float rooms = tex2D(_Rooms, i.uv).a * 0.4
                    + (tex2D(_Rooms, i.uv + float2(d.x, 0)).a + tex2D(_Rooms, i.uv - float2(d.x, 0)).a
                     + tex2D(_Rooms, i.uv + float2(0, d.y)).a + tex2D(_Rooms, i.uv - float2(0, d.y)).a) * 0.15;
                float reveal = saturate(max(rooms * 1.6, tex2D(_Brush, i.uv).r));
                fog.a *= 1.0 - reveal;
                return fog * i.color;
            }
            ENDCG
        }
    }
}
