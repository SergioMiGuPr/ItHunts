// ItHunts : spectre (livre du hibou). Objet translucide et lumineux, bleu glacé : cœur sombre et transparent, bords vifs
// (fresnel en aplats : 2 paliers nets, style toon), lignes de « texte » qui défilent lentement sur les pages, bruit qui
// monte (volutes), et frémissement des pages libres par déplacement de sommets : couleur de sommet R = poids (0 au dos,
// 1 au bord libre). Mélange additif + alpha (le noir du décor reste noir derrière). Double face.
Shader "ItHunts/Spectre"
{
    Properties
    {
        [HDR] _Couleur ("Couleur du spectre (HDR)", Color) = (0.45, 0.85, 1.4, 1)
        [HDR] _Bord ("Couleur des bords (HDR)", Color) = (1.2, 2.2, 3.0, 1)
        _Coeur ("Opacité du cœur", Range(0, 1)) = 0.18
        _BordLargeur ("Largeur du bord (fresnel)", Range(0, 1)) = 0.45
        _BordPaliers ("Paliers du bord", Range(1, 4)) = 2
        _Lignes ("Lignes de texte (nombre par uv y, 0 = aucune)", Range(0, 60)) = 22
        _LignesForce ("Lignes : force", Range(0, 2)) = 0.6
        _Defile ("Défilement (uv par seconde : x lignes, y bruit)", Vector) = (0.0, 0.25, 0, 0)
        _Bruit ("Bruit des volutes (échelle, force)", Vector) = (6, 0.5, 0, 0)
        _Fremit ("Frémissement des pages (m, cycles par seconde)", Vector) = (0.006, 2.3, 0, 0)
        _Pulse ("Pulsation de la lumière (amplitude, vitesse)", Vector) = (0.15, 1.1, 0, 0)
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Faces", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Spectre"
            Tags { "LightMode" = "UniversalForward" }
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull [_Cull]
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Couleur, _Bord;
                half _Coeur, _BordLargeur, _BordPaliers, _Lignes, _LignesForce;
                float4 _Defile, _Bruit, _Fremit, _Pulse;
                float _Cull;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; half4 color : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float3 posOS : TEXCOORD3;
                half w : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash(float3 p) { p = frac(p * 0.3183099 + 0.1); p *= 17.0; return frac(p.x * p.y * p.z * (p.x + p.y + p.z)); }
            float Noise(float3 x)
            {
                float3 i = floor(x), f = frac(x); f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(lerp(Hash(i), Hash(i + float3(1, 0, 0)), f.x), lerp(Hash(i + float3(0, 1, 0)), Hash(i + float3(1, 1, 0)), f.x), f.y),
                            lerp(lerp(Hash(i + float3(0, 0, 1)), Hash(i + float3(1, 0, 1)), f.x), lerp(Hash(i + float3(0, 1, 1)), Hash(i + float3(1, 1, 1)), f.x), f.y), f.z);
            }

            Varyings Vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float t = _Time.y;
                // frémissement : la page ondule le long de sa normale, plus fort au bord libre
                float w = v.color.r;
                float onde = sin(t * _Fremit.y * 6.2832 + v.positionOS.y * 40.0 + v.positionOS.x * 25.0) * 0.7
                           + sin(t * _Fremit.y * 9.7 + v.positionOS.y * 70.0) * 0.3;
                float3 p = v.positionOS.xyz + v.normalOS * onde * _Fremit.x * w * w;
                VertexPositionInputs vp = GetVertexPositionInputs(p);
                o.positionCS = vp.positionCS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.viewWS = GetWorldSpaceViewDir(vp.positionWS);
                o.uv = v.uv;
                o.posOS = v.positionOS.xyz;
                o.w = w;
                return o;
            }

            half4 Frag(Varyings i, bool face : SV_IsFrontFace) : SV_Target
            {
                float t = _Time.y;
                float3 n = normalize(i.normalWS) * (face ? 1 : -1);
                float3 vd = normalize(i.viewWS);
                float fr = 1.0 - saturate(abs(dot(n, vd)));
                // bord en paliers nets (toon)
                float b = saturate((fr - (1.0 - _BordLargeur)) / max(_BordLargeur, 1e-3));
                b = floor(b * _BordPaliers + 0.5) / _BordPaliers;
                // bruit qui monte (volutes, matière du spectre)
                float nz = Noise(i.posOS * _Bruit.x * 10.0 + float3(0, -t * _Defile.y * 4.0, 0));
                nz = smoothstep(0.35, 0.75, nz);
                // lignes de texte (pages) : traits fins qui défilent
                float lg = 0.0;
                if (_Lignes > 0.5)
                {
                    float y = i.uv.y * _Lignes + t * _Defile.x;
                    float l = abs(frac(y) - 0.5);
                    float trou = step(0.35, Noise(float3(i.uv.x * 18.0, floor(y), 3.0)));
                    lg = (1.0 - smoothstep(0.06, 0.12, l)) * trou * step(0.08, i.uv.x) * step(i.uv.x, 0.92);
                }
                float pulse = 1.0 + _Pulse.x * sin(t * _Pulse.y * 6.2832);
                half3 c = _Couleur.rgb * (_Coeur + nz * _Bruit.y + lg * _LignesForce) + _Bord.rgb * b;
                c *= pulse;
                half a = saturate(_Coeur + b * 0.8 + lg * 0.3 + nz * 0.2);
                return half4(c, a);
            }
            ENDHLSL
        }
    }
}
