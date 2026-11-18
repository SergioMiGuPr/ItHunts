// Calques des menus (en attendant les scènes composées de la DA) : fondu en haut et en bas de l'image (part de sa hauteur), érosion du
// détourage (alpha minimum sur un voisinage de _Erode texels, puis bord adouci) ; mélange réglable (alpha ou additif).
Shader "ItHunts/UIFondu"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Teinte", Color) = (1, 1, 1, 1)
        _FadeTop ("Fondu en haut (part de la hauteur)", Range(0, 1)) = 0
        _FadeBottom ("Fondu en bas (part de la hauteur)", Range(0, 1)) = 0
        _Erode ("Érosion du détourage (texels)", Range(0, 8)) = 0
        _Additive ("Additif (0 : alpha)", Range(0, 1)) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination", Float) = 10
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend [_SrcBlend] [_DstBlend]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            sampler2D _MainTex; float4 _MainTex_TexelSize; float4 _Color; float _FadeTop, _FadeBottom, _Erode, _Additive;
            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color * _Color; return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                if (_Erode > 0.01)
                {
                    float2 d = _MainTex_TexelSize.xy * _Erode;
                    float a = min(min(tex2D(_MainTex, i.uv + float2(d.x, 0)).a, tex2D(_MainTex, i.uv - float2(d.x, 0)).a),
                                  min(tex2D(_MainTex, i.uv + float2(0, d.y)).a, tex2D(_MainTex, i.uv - float2(0, d.y)).a));
                    a = min(a, min(min(tex2D(_MainTex, i.uv + d).a, tex2D(_MainTex, i.uv - d).a),
                                   min(tex2D(_MainTex, i.uv + float2(d.x, -d.y)).a, tex2D(_MainTex, i.uv + float2(-d.x, d.y)).a)));
                    c.a = min(c.a, smoothstep(0.15, 0.85, a));
                }
                float fade = 1.0;
                if (_FadeBottom > 0.001) fade *= smoothstep(0.0, _FadeBottom, i.uv.y);
                if (_FadeTop > 0.001) fade *= smoothstep(0.0, _FadeTop, 1.0 - i.uv.y);
                c *= i.color;
                c.a *= fade;
                // Additif : RGB × alpha × fondu sur noir (Blend One One) ; alpha : normal.
                return _Additive > 0.5 ? fixed4(c.rgb * c.a, 1) : c;
            }
            ENDCG
        }
    }
}
