// ItHunts : ciel nocturne peint de l'acte III (nuit du 2026-11-22). Sur une sphère vue de l'intérieur (prefab Jardin_Ciel) :
// dégradé de l'horizon au zénith en aplats, étoiles, lune (disque, mers sombres, halo en paliers), bandes de nuages
// peints qui dérivent lentement et passent devant la lune. Pas de brouillard (le ciel est au-delà de la portée).
Shader "ItHunts/Ciel"
{
    Properties
    {
        _Zenith ("Zénith", Color) = (0.02, 0.025, 0.06, 1)
        _Horizon ("Horizon", Color) = (0.07, 0.08, 0.14, 1)
        _Sol ("Sous l'horizon", Color) = (0.01, 0.012, 0.02, 1)
        [IntRange] _Paliers ("Paliers du dégradé (0 : continu)", Range(0, 8)) = 0
        [NoScaleOffset] _LuneTex ("Lune peinte (RGBA, celle du menu principal)", 2D) = "black" {}
        _LuneTexOn ("Utiliser la lune peinte", Range(0, 1)) = 1
        _LuneDir ("Direction de la lune (monde)", Vector) = (-0.35, 0.42, 0.84, 0)
        _LuneTaille ("Taille de la lune (rad)", Range(0.01, 0.2)) = 0.075
        [HDR] _LuneCouleur ("Couleur de la lune", Color) = (2.4, 2.45, 2.6, 1)
        _HaloCouleur ("Halo", Color) = (0.16, 0.19, 0.3, 1)
        _HaloTaille ("Taille du halo (× lune)", Range(1, 12)) = 4
        _Etoiles ("Densité des étoiles", Range(0, 1)) = 0.4
        _Nuages ("Nuages : couverture", Range(0, 1)) = 0.45
        _NuageCouleur ("Nuages : couleur", Color) = (0.05, 0.055, 0.09, 1)
        _NuageBord ("Nuages : bord éclairé par la lune", Color) = (0.28, 0.3, 0.42, 1)
        _Vent ("Nuages : vitesse", Range(0, 0.05)) = 0.006
    }
    SubShader
    {
        Tags { "RenderType" = "Background" "Queue" = "Background" "RenderPipeline" = "UniversalPipeline" "PreviewType" = "Skybox" }
        Pass
        {
            Name "Ciel"
            Tags { "LightMode" = "UniversalForward" }
            Cull Front
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Zenith, _Horizon, _Sol, _LuneCouleur, _HaloCouleur, _NuageCouleur, _NuageBord;
                float4 _LuneDir;
                half _Paliers, _LuneTaille, _HaloTaille, _Etoiles, _Nuages, _Vent, _LuneTexOn;
            CBUFFER_END
            TEXTURE2D(_LuneTex); SAMPLER(sampler_LuneTex);
            struct A { float4 pos : POSITION; };
            struct V { float4 cs : SV_POSITION; float3 dir : TEXCOORD0; };
            V Vert(A i)
            {
                V o;
                float3 ws = TransformObjectToWorld(i.pos.xyz);
                o.cs = TransformWorldToHClip(ws);
                o.dir = ws - _WorldSpaceCameraPos;
                #if UNITY_REVERSED_Z
                    o.cs.z = o.cs.w * 1e-5;
                #else
                    o.cs.z = o.cs.w * (1 - 1e-5);
                #endif
                return o;
            }
            float H(float3 p) { p = frac(p * float3(0.1031, 0.1030, 0.0973)); p += dot(p, p.yxz + 33.33); return frac((p.x + p.y) * p.z); }
            float N2(float2 p) { float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f);
                float a = H(float3(i, 1)), b = H(float3(i + float2(1, 0), 1)), c = H(float3(i + float2(0, 1), 1)), d = H(float3(i + 1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y); }
            float Fbm(float2 p) { return N2(p) * 0.55 + N2(p * 2.1 + 7.3) * 0.3 + N2(p * 4.3 + 1.7) * 0.15; }
            half4 Frag(V i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float h = d.y;
                // dégradé continu (nuit 4 : plus de bandes dans le ciel), paliers en option
                float t = saturate(h * 1.6);
                if (_Paliers >= 2) t = floor(t * _Paliers) / _Paliers;
                half3 c = h < 0 ? lerp(_Horizon.rgb * 0.6, _Sol.rgb, saturate(-h * 6)) : lerp(_Horizon.rgb, _Zenith.rgb, t);
                // étoiles (grille sur la sphère), plus nombreuses en haut
                float3 g = floor(d * 220);
                float s = H(g);
                float tw = 0.6 + 0.4 * sin(_Time.y * (1 + s * 3) + s * 40);
                c += step(1 - _Etoiles * 0.02 * saturate(h * 3), s) * tw * 0.8 * saturate(h * 4);
                // lune et halo
                float3 ld = normalize(_LuneDir.xyz);
                float ang = acos(saturate(dot(d, ld)));
                float r = ang / _LuneTaille;
                // halo continu (nuit 4 : plus de halo en paliers) : décroissance douce
                float halo = saturate(1 - r / _HaloTaille);
                halo = halo * halo * (3 - 2 * halo);
                halo *= halo;
                c += _HaloCouleur.rgb * halo * 0.7;
                // disque : mers sombres
                float3 u = normalize(cross(ld, float3(0, 1, 0)));
                float3 v = cross(u, ld);
                float2 q = float2(dot(d, u), dot(d, v)) / _LuneTaille;
                float mer = step(0.62, Fbm(q * 1.6 + 3.1));
                half3 lune = _LuneCouleur.rgb * (1 - 0.32 * mer) * (0.86 + 0.14 * step(0.0, q.x + q.y * 0.3));
                float disque = 1 - smoothstep(0.97, 1.0, r);
                if (_LuneTexOn > 0.5)
                {
                    // lune peinte du menu principal, plaquée sur le disque (q dans -1..1)
                    half4 lt = SAMPLE_TEXTURE2D_LOD(_LuneTex, sampler_LuneTex, q * 0.5 + 0.5, 0);
                    lune = lt.rgb * max(max(_LuneCouleur.r, _LuneCouleur.g), _LuneCouleur.b) * 0.75;
                    disque = lt.a * step(max(abs(q.x), abs(q.y)), 1);
                }
                // nuages en bandes, devant la lune
                float2 np = d.xz / max(d.y + 0.25, 0.05) * 0.9 + float2(_Time.y * _Vent, _Time.y * _Vent * 0.3);
                float n = Fbm(np * float2(0.7, 1.8));
                float cov = smoothstep(1 - _Nuages, 1 - _Nuages + 0.06, n) * saturate(h * 5);
                float bord = smoothstep(1 - _Nuages - 0.05, 1 - _Nuages, n) * (1 - cov) * saturate(h * 5);
                c = lerp(c, lune, disque);
                c = lerp(c, _NuageCouleur.rgb + _NuageBord.rgb * halo * 0.6, cov);
                c += _NuageBord.rgb * bord * (0.25 + halo);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
