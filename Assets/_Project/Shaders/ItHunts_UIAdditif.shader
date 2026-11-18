// Interface additive (UI-HANDOFF § 2.1 : brume, halo de la lune, feu, fenêtres) : PNG RGB sur noir pur, Blend One One ; l'opacité
// s'anime par la couleur de l'Image (le noir n'ajoute rien).
Shader "ItHunts/UIAdditif"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "black" {}
        _Color ("Teinte", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend One One
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            sampler2D _MainTex; float4 _Color;
            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color * _Color; return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                return fixed4(c.rgb * c.a * i.color.rgb * i.color.a, 1);
            }
            ENDCG
        }
    }
}
