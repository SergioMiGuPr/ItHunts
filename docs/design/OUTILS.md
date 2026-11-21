# Outils Unity utiles au projet (2026-11-21)

Liste côté code et intégration (Unity 6.3 LTS, URP). La liste côté art (Blender, retopologie, gréement, peinture) est dans
`docs/art/OUTILS.md`. **Rien n'a été acheté.** Prix indicatifs relevés de mémoire : **à vérifier** sur l'Asset Store
(licence, usage commercial, nombre de postes) avant tout achat.

## Déjà installés

| Package | Version | Sert à |
|---|---|---|
| AI Navigation (`com.unity.ai.navigation`) | 2.0.16 | NavMesh des goules et du traqueur, **NavMesh Links** typés du traqueur (point 9 : bond, escalade, chaîne, porte étroite) |
| Cinemachine | 3.1.7 | caméras (menus, captures) |
| Input System | 1.20.1 | entrées, écran partagé, parade (E / LB) |
| URP | 17.3.0 | rendu, eau toon (texture de profondeur et opaque), effets peints |
| Timeline | 1.8.13 | séquences (non utilisé en jeu pour l'instant) |
| Test Framework | 1.6.0 | tests EditMode |

**Installé cette nuit : rien.** Aucun point de la nuit n'en avait besoin : les liens du traqueur passent par AI Navigation
(déjà là), les chaînes d'os par un script maison (`BoneSpringChain`), l'eau et les effets par les shaders de la DA.

## Gratuits (officiels Unity), à installer quand le besoin arrive

| Package | Pour | Quand |
|---|---|---|
| **Animation Rigging** (`com.unity.animation.rigging`) | IK à l'exécution : pattes du traqueur posées sur les rebords et les chaînes, regard de la tête, mains sur les armes | à l'arrivée du traqueur v4 et des vraies animations (après les références de l'utilisateur) |
| **ProBuilder** (`com.unity.probuilder`) | greybox des nouveaux modules à la main (escaliers en spirale, coursives) quand un module doit être retouché sans passer par le générateur | si l'utilisateur veut éditer des salles à la main |
| **Splines** (`com.unity.splines`) | trajectoires des bonds du traqueur, ponts de cordes, chaînes courbes, escalier en spirale du puits | si les chaînes et les ponts doivent suivre une courbe éditable |
| **Recorder** (`com.unity.recorder`) | vidéos de contrôle (traqueur qui saute et grimpe, playtests) | pour les enregistrements demandés (point 9) en meilleure qualité |
| **Profiler / Memory Profiler** (`com.unity.memoryprofiler`) | mémoire des textures et des effets en build | avant la publication |
| **Addressables** | chargement des niveaux et des musiques en arrière-plan | quand la taille du jeu le demandera |
| **Netcode for GameObjects** + Multiplayer Services | coop en ligne (étape 11) | à l'étape 11 (`/setup-multiplayer-services`) |
| **Shader Graph** (fourni avec l'URP) | variantes de shaders | si la DA veut éditer les shaders sans code |

## Payants (indicatif, à vérifier)

| Outil | Prix indicatif | Pour | Avis |
|---|---|---|---|
| **Final IK** (RootMotion) | ~90 $ | IK complète (quadrupède, grimpe, regard) | puissant pour le traqueur à quatre pattes ; Animation Rigging gratuit couvre l'essentiel |
| **Magica Cloth 2** | ~30 à 40 $ | tissus et cheveux (lambeaux du traqueur, capes) | meilleur rendu que notre ressort simple ; compatible URP, multithread |
| **Dynamic Bone** | ~20 $ | chaînes d'os à ressort | ce que fait déjà `BoneSpringChain` (gratuit, à nous) |
| **Stylized Water 3** (Staggart) | ~50 à 60 $ | eau stylisée URP complète | la DA recommande de garder `ItHunts/EauToon` (docs/art/EAU.md) |
| **KWS Water System** | ~80 à 100 $ | eau réaliste | contraire à la DA |
| **Animancer Pro** | ~70 $ | animations par code (un clip par type de lien du traqueur, transitions) | utile quand les clips du traqueur v4 arriveront ; l'Animator suffit pour l'instant |
| **A\* Pathfinding Project Pro** | ~100 $ | pathfinding avancé (liens, grandes créatures) | inutile tant qu'AI Navigation suffit |
| **Odin Inspector** | ~55 $ (indépendant) | réglages plus lisibles dans l'inspecteur | confort ; pas nécessaire |

Recommandation : rien à acheter pour l'instant. Le premier achat utile serait **Magica Cloth 2** (lambeaux et cheveux du
traqueur v4) ou **Final IK**, à décider quand le modèle v4 sera validé.
