// ItHunts : eau toon des niveaux (égouts, citerne). La texture d'eau peinte (texture_eau_egout_v01_tuile) défile en deux
// couches (courant + ondulation) en uv monde ; la couleur suit la profondeur sous la surface (aplats : peu profond, moyen,
// profond), l'écume se pose sur les bords et autour de tout objet qui traverse la surface (profondeur de la scène), un
// trait d'encre cerne l'écume ; le fond se voit à travers l'eau peu profonde (texture opaque déformée par les vagues) ;
// les torches (lumières ponctuelles) se reflètent en taches nettes allongées. Requiert la texture de profondeur et la
// texture opaque de l'URP (activées dans PC_RPAsset ; sans elles : pas d'écume ni de transparence, l'eau reste opaque).
// Mouvement (2026-11-22, l'eau paraissait fixe en jeu) : _Mouvement multiplie toutes les vitesses ; vaguelettes encrées qui
// dérivent avec le courant (traits clairs qui se forment et se défont), normale des vaguelettes dans les reflets (les taches
// des torches ondulent et scintillent), écume qui avance et recule (_EcumePulse). Tout est animé par _Time : rien ne dépend
// de l'éditeur (vérifié en jeu et dans un build : docs/art/EAU.md).
Shader "ItHunts/EauToon"
{
    Properties
    {
        _BaseMap ("Texture d'eau peinte (tuile)", 2D) = "white" {}
        _Tuile ("Taille de la tuile (m)", Float) = 6
        _Courant ("Courant : direction xz (m/s) et vitesse de l'ondulation", Vector) = (0.35, 0.1, 0.12, 0)
        _Ondulation ("Ondulation : force des vagues sur la texture, échelle", Vector) = (0.09, 0.45, 0, 0)
        _Mouvement ("Mouvement : multiplie toutes les vitesses (0 = eau figée)", Range(0, 4)) = 1.5
        [Header(Vaguelettes)]
        _Vaguelettes ("Vaguelettes : force, échelle (par m), vitesse de déformation, demi-largeur du trait (m)", Vector) = (0.45, 1.6, 0.6, 0.014)
        _VagueCouleur ("Vaguelettes : couleur ajoutée", Color) = (0.16, 0.26, 0.2, 1)
        _RefletsOndule ("Reflets : ondulation par les vaguelettes", Range(0, 3)) = 1.2
        _EcumePulse ("Écume : avance et recul (part de la largeur, cycles par seconde)", Vector) = (0.35, 0.7, 0, 0)
        [Header(Profondeur)]
        _PeuProfond ("Peu profond", Color) = (0.2, 0.32, 0.22, 1)
        _Moyen ("Moyen", Color) = (0.07, 0.15, 0.11, 1)
        _Profond ("Profond", Color) = (0.015, 0.035, 0.035, 1)
        _Paliers ("Profondeurs des paliers (m) : moyen, profond", Vector) = (0.35, 1.4, 0, 0)
        _Texture ("Part de la texture peinte", Range(0, 1)) = 0.75
        _Transparence ("Transparence sur le fond (peu profond)", Range(0, 1)) = 0.55
        _Refraction ("Déformation du fond", Range(0, 0.1)) = 0.02
        [Header(Ecume et encre)]
        [HDR] _Ecume ("Écume", Color) = (0.62, 0.78, 0.6, 1)
        _EcumeLargeur ("Largeur de l'écume (m)", Range(0, 1)) = 0.22
        _EcumeBruit ("Écume découpée (bruit)", Range(0, 1)) = 0.5
        _Encre ("Encre", Color) = (0.01, 0.02, 0.02, 1)
        _EncreLargeur ("Trait d'encre au bord de l'écume (m)", Range(0, 0.2)) = 0.04
        [Header(Reflets)]
        _Reflets ("Reflets des torches : force", Range(0, 4)) = 2.5
        _RefletsTaille ("Reflets : taille (brillance)", Range(4, 400)) = 40
        _RefletsSeuil ("Reflets : seuil des taches nettes", Range(0, 1)) = 0.04
        _Ciel ("Reflet du ciel ou de la voûte (fresnel)", Color) = (0.05, 0.08, 0.1, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-10" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Eau"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            float _ItHuntsEauTemps;   // captures de contrôle seulement : temps imposé (> 0) ; 0 en jeu (temps réel)
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float _Tuile;
                float4 _Courant, _Ondulation, _Paliers, _Vaguelettes, _EcumePulse;
                half4 _VagueCouleur;
                float _Mouvement, _RefletsOndule;
                half4 _PeuProfond, _Moyen, _Profond, _Ecume, _Encre, _Ciel;
                half _Texture, _Transparence, _Refraction, _EcumeLargeur, _EcumeBruit, _EncreLargeur, _Reflets, _RefletsTaille, _RefletsSeuil;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float fog : TEXCOORD2;
            };

            float Hash(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }
            float Bruit(float2 p)
            {
                float2 i = floor(p), f = frac(p); float2 u = f * f * (3 - 2 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), u.x), lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), u.x), u.y);
            }

            Varyings Vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.screenPos = ComputeScreenPos(p.positionCS);
                o.fog = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            // champ des vaguelettes : deux bruits qui dérivent dans des directions différentes (le long du courant et en
            // travers) ; les crêtes sont les lignes où le champ passe par 0,5
            float Vagues(float2 w, float t)
            {
                float2 c = _Courant.xy;
                float2 dv = normalize(float2(c.x, c.y) + 1e-4);
                float a = Bruit(w * _Vaguelettes.y + t * c * 0.9 * _Vaguelettes.y + float2(0, t * _Vaguelettes.z));
                float b = Bruit(w * _Vaguelettes.y * 1.7 + float2(-dv.y, dv.x) * t * 0.35 + 13.7 - float2(t * _Vaguelettes.z * 0.7, 0));
                return a * 0.6 + b * 0.4;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float t = (_ItHuntsEauTemps > 0 ? _ItHuntsEauTemps : _Time.y) * _Mouvement;
                float2 w = i.positionWS.xz;
                // vagues : décalage lent qui déforme les uv (ondulation), deux couches qui défilent avec le courant
                float2 ond = float2(Bruit(w * _Ondulation.y + t * _Courant.z), Bruit(w * _Ondulation.y + 7.3 - t * _Courant.z)) - 0.5;
                float2 uv1 = w / _Tuile + t * _Courant.xy / _Tuile + ond * _Ondulation.x * 2.0;
                float2 uv2 = w / (_Tuile * 1.7) + t * _Courant.xy * 0.6 / _Tuile + float2(0.37, 0.71) - ond * _Ondulation.x;
                half3 tex = min(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv1).rgb, SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv2).rgb * 1.15);
                // profondeur de l'eau sous ce pixel (le long du rayon de vue, approchée en distance)
                float2 suv = i.screenPos.xy / i.screenPos.w;
                float2 suvR = suv + ond * _Refraction;
                float dScene = LinearEyeDepth(SampleSceneDepth(suv), _ZBufferParams);
                float dSceneR = LinearEyeDepth(SampleSceneDepth(suvR), _ZBufferParams);
                float dSurf = i.screenPos.w;
                float prof = max(0, dScene - dSurf);
                if (dSceneR < dSurf) suvR = suv;               // pas de déformation sur ce qui est devant l'eau
                // couleur en aplats selon la profondeur
                half3 c = prof < _Paliers.x ? _PeuProfond.rgb : (prof < _Paliers.y ? _Moyen.rgb : _Profond.rgb);
                half texL = dot(tex, half3(0.3, 0.59, 0.11));
                c = lerp(c, c * 0.6 + tex * 0.8, _Texture);
                // vaguelettes : traits clairs en aplat sur les crêtes, de largeur constante (finesse = demi-largeur en m :
                // distance à la crête = écart du champ divisé par sa pente)
                float vg = Vagues(w, t);
                float ex = 0.06;
                float gx = Vagues(w + float2(ex, 0), t) - vg, gz = Vagues(w + float2(0, ex), t) - vg;
                float pente = max(length(float2(gx, gz)) / ex, 1e-3);
                half crete = step(abs(vg - 0.5) / pente, _Vaguelettes.w);
                // traits coupés en tirets qui suivent le courant (pas de longues lignes continues)
                crete *= step(0.52, Bruit(w * _Vaguelettes.y * 2.6 + t * _Courant.xy * 1.4 + 3.1));
                c = lerp(c, c * 1.25 + _VagueCouleur.rgb, crete * _Vaguelettes.x);
                // fond vu à travers l'eau peu profonde
                half3 fond = SampleSceneColor(suvR);
                half voit = (1.0 - saturate(prof / max(_Paliers.y, 0.01))) * _Transparence;
                voit = floor(voit * 3.0 + 0.5) / 3.0;            // aplats
                c = lerp(c, fond * c * 3.0, voit);
                // écume au bord et autour des objets, découpée par le bruit ; trait d'encre derrière
                float bruitE = Bruit(w * 3.0 + t * _Courant.xy * 2.0) * _EcumeBruit;
                float pulse = sin(t * _EcumePulse.y * 6.2832 + (w.x + w.y) * 0.8) * _EcumePulse.x * _EcumeLargeur;
                float e = prof + bruitE * _EcumeLargeur - pulse;
                half ecume = step(e, _EcumeLargeur);
                half encre = step(e, _EcumeLargeur + _EncreLargeur) * (1 - ecume);
                c = lerp(c, _Ecume.rgb, ecume);
                c = lerp(c, _Encre.rgb, encre);
                // reflets des torches : taches nettes (paliers), allongées vers la caméra par les vagues
                float3 V = normalize(GetWorldSpaceViewDir(i.positionWS));
                // normale : ondulation lente + pente des vaguelettes (différences finies) : les reflets ondulent
                float3 N = normalize(float3(ond.x * 0.9 + (texL - 0.3) * 0.6 - gx / ex * 0.25 * _RefletsOndule, 1.0, ond.y * 0.9 - gz / ex * 0.25 * _RefletsOndule));
                half3 refl = 0;
                #if defined(_ADDITIONAL_LIGHTS) || USE_CLUSTER_LIGHT_LOOP
                uint n = GetAdditionalLightsCount();
                #if USE_CLUSTER_LIGHT_LOOP
                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.normalizedScreenSpaceUV = suv;
                LIGHT_LOOP_BEGIN(n)
                #else
                for (uint li = 0; li < n; li++) {
                    uint lightIndex = li;
                #endif
                    Light L = GetAdditionalLight(lightIndex, i.positionWS);
                    float3 H = normalize(L.direction + V);
                    float s = pow(saturate(dot(N, H)), _RefletsTaille) * L.distanceAttenuation;
                    s = step(_RefletsSeuil, s) * 0.7 + step(_RefletsSeuil * 2.5, s) * 0.3;
                    refl += L.color * s;
                #if USE_CLUSTER_LIGHT_LOOP
                LIGHT_LOOP_END
                #else
                }
                #endif
                #endif
                c += refl * _Reflets * (1 - ecume);
                // reflet de la voûte (fresnel en aplat)
                half fr = step(0.75, 1 - saturate(dot(V, float3(0, 1, 0))));
                c = lerp(c, c + _Ciel.rgb, fr * (1 - ecume));
                half a = saturate(1.0 - voit * 0.25);
                c = MixFog(c, i.fog);
                return half4(c, a);
            }
            ENDHLSL
        }
    }
}
