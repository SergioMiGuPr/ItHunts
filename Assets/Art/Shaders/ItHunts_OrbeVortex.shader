// ItHunts : énergie de l'orbe (SM_Orbe_Energie) en tourbillon. Bras de spirale en aplats (style toon) qui tournent autour
// de l'axe de visée : vu du joueur, c'est toujours un vortex, quelle que soit la rotation de la sphère. Cœur plus clair,
// bord plus vif (fresnel). Couleur : _EmissionColor (HDR, écrite par le jeu par bloc de propriétés, comme avant).
// Vitesse : _SpinSpeed (tours/s) × _SpinBoost (le jeu le monte en traction) ; pour accélérer sans à-coup, le jeu peut
// intégrer lui-même la phase (_SpinPhase += vitesse × dt, en tours) et laisser _SpinSpeed à 0.
Shader "ItHunts/OrbeVortex"
{
    Properties
    {
        [HDR] _EmissionColor ("Couleur de l'énergie (HDR, couleur du personnage)", Color) = (2.2, 0.3, 0.2, 1)
        [Header(Rotation)]
        _SpinSpeed ("Vitesse (tours par seconde)", Range(-4, 4)) = 0.6
        _SpinBoost ("Multiplicateur (traction : 3 à 4)", Range(0, 10)) = 1
        _SpinPhase ("Phase (tours, écrite par le jeu)", Float) = 0
        [Header(Spirale)]
        [IntRange] _Arms ("Bras de la spirale", Range(1, 8)) = 4
        _Twist ("Enroulement (tours du centre au bord)", Range(-3, 3)) = 1.6
        _ArmWidth ("Largeur des bras", Range(0.05, 0.95)) = 0.5
        _ArmSoftness ("Bord des bras (0 : aplat net)", Range(0.001, 0.3)) = 0.04
        _Radius ("Rayon de la sphère (repère de l'objet, m)", Float) = 0.045
        [Header(Lumiere)]
        _Base ("Fond entre les bras", Range(0, 1)) = 0.35
        _Core ("Cœur clair", Range(0, 3)) = 1.2
        _CoreSize ("Taille du cœur", Range(0.05, 1)) = 0.55
        _CoreWhite ("Cœur vers le blanc", Range(0, 1)) = 0.45
        _Rim ("Bord vif (fresnel)", Range(0, 3)) = 0.9
        _RimPower ("Finesse du bord", Range(0.5, 8)) = 2.5
        _Pulse ("Pulsation des bras", Range(0, 1)) = 0.15
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "Vortex"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _EmissionColor;
                float _SpinSpeed, _SpinBoost, _SpinPhase;
                half _Arms, _Twist, _ArmWidth, _ArmSoftness;
                float _Radius;
                half _Base, _Core, _CoreSize, _CoreWhite, _Rim, _RimPower, _Pulse;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionVS : TEXCOORD0;
                float3 centerVS   : TEXCOORD1;
                float3 normalVS   : TEXCOORD2;
                float  radiusVS   : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 ws = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(ws);
                o.positionVS = TransformWorldToView(ws);
                o.centerVS = TransformWorldToView(TransformObjectToWorld(float3(0, 0, 0)));
                o.normalVS = TransformWorldToViewDir(TransformObjectToWorldNormal(v.normalOS), true);
                // rayon à l'écran : rayon de l'objet × échelle (la plus grande des trois)
                float sx = length(UNITY_MATRIX_M._m00_m10_m20), sy = length(UNITY_MATRIX_M._m01_m11_m21), sz = length(UNITY_MATRIX_M._m02_m12_m22);
                o.radiusVS = _Radius * max(sx, max(sy, sz));
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                // coordonnées polaires autour de l'axe de visée, centrées sur l'orbe
                float2 d = i.positionVS.xy - i.centerVS.xy;
                float r = saturate(length(d) / max(i.radiusVS, 1e-4));
                float a = atan2(d.y, d.x) / (2.0 * PI);
                float phase = _SpinPhase + _Time.y * _SpinSpeed * _SpinBoost;
                // bras : spirale d'Archimède, en aplat (une marche nette, adoucie par _ArmSoftness)
                float s = frac(a * _Arms + r * _Twist - phase * _Arms);
                float w = _ArmWidth * (1.0 + _Pulse * sin(_Time.y * 6.2832 * 0.8 + r * 6.0));
                half arm = smoothstep(0.0, _ArmSoftness, s) * (1.0 - smoothstep(w - _ArmSoftness, w, s));
                // seconde nuance, plus fine, au milieu des bras (un peu de peinture)
                half inner = smoothstep(w * 0.35 - _ArmSoftness, w * 0.35, s) * (1.0 - smoothstep(w * 0.6, w * 0.6 + _ArmSoftness, s));
                half level = lerp(_Base, 1.0h, arm) + 0.25h * inner * arm;
                // cœur clair (vers le blanc) et bord vif
                half core = (1.0h - smoothstep(0.0h, _CoreSize, r)) * _Core;
                half3 n = normalize(i.normalVS);
                half rim = pow(1.0h - saturate(n.z), _RimPower) * _Rim;
                half3 col = _EmissionColor.rgb * (level + rim) + lerp(_EmissionColor.rgb, max(_EmissionColor.rgb, dot(_EmissionColor.rgb, 0.33h).xxx * 2.0h), _CoreWhite) * core;
                return half4(col, 1);
            }
            ENDHLSL
        }

        // profondeur (contours, SSAO, tri) : même géométrie, sans couleur
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex V
            #pragma fragment F
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _EmissionColor;
                float _SpinSpeed, _SpinBoost, _SpinPhase;
                half _Arms, _Twist, _ArmWidth, _ArmSoftness;
                float _Radius;
                half _Base, _Core, _CoreSize, _CoreWhite, _Rim, _RimPower, _Pulse;
            CBUFFER_END
            struct A { float4 p : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Vv { float4 p : SV_POSITION; };
            Vv V(A v) { Vv o; UNITY_SETUP_INSTANCE_ID(v); o.p = TransformObjectToHClip(v.p.xyz); return o; }
            half F(Vv i) : SV_Target { return i.p.z; }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
