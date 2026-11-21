# Eau toon des niveaux (lot F, 2026-11-21, branche `art-eau`)

Planche : `docs/art/rendus/Eau/Planche_Eau.jpg` (scène d'essai `Assets/Art/Scenes/Test_Eau.unity` : canal d'égout de 6 m,
quais, pente peu profonde, piliers et caisse dans l'eau, trois torches). Constructeur : `Assets/Art/Source~/Eau/scripts/EauBuilder.cs`.

## Shader `ItHunts/EauToon` (`Assets/Art/Eau/Shaders/`), matériau `M_Eau_Toon`

| Effet | Comment | Réglages |
|---|---|---|
| Texture d'eau existante | `texture_eau_egout_v01_tuile` en uv monde (xz), deux couches qui défilent à des vitesses différentes | `_Tuile` (6 m), `_Courant` (xy : direction et vitesse en m/s ; z : vitesse de l'ondulation) |
| Ondulation et courant | bruit lent qui déforme les uv des deux couches | `_Ondulation` (force, échelle) |
| Couleur selon la profondeur | profondeur sous la surface lue dans la texture de profondeur, **3 aplats** | `_PeuProfond`, `_Moyen`, `_Profond`, `_Paliers` (0.35 m et 1.4 m), `_Texture` (part de la texture peinte) |
| Transparence sur le fond | texture opaque de l'URP, déformée par les vagues, visible en eau peu profonde (aplats) | `_Transparence`, `_Refraction` |
| Écume | sur les bords et **autour de tout objet** qui traverse la surface (même calcul de profondeur), découpée par un bruit qui suit le courant | `_Ecume`, `_EcumeLargeur` (0.22 m), `_EcumeBruit` |
| Contour encré | trait d'encre juste derrière l'écume | `_Encre`, `_EncreLargeur` (4 cm) |
| Reflets stylisés des torches | chaque lumière ponctuelle (Forward+ compris) donne des **taches nettes** sur la normale ondulée | `_Reflets`, `_RefletsTaille`, `_RefletsSeuil` ; `_Ciel` (reflet de la voûte en aplat, rasant) |

**Requis** : texture de profondeur et texture opaque de l'URP (activées dans `PC_RPAsset` ; **désactivées dans
`Mobile_RPAsset`** : sur ce profil, pas d'écume ni de transparence). L'eau est un `Quad` ou un plan à la hauteur de la surface,
sans collider (la physique reste celle de `DeepWater`).

## Éclaboussure

- `Assets/Art/Eau/Prefabs/VFX_Eau_Eclaboussure.prefab` : couronne d'eau peinte (planche 4 × 4, 0.7 s, trait d'encre) + onde
  horizontale qui s'élargit (deux vagues). Particules URP *Unlit* (couleurs dans la texture), textures
  `T_Eau_Eclaboussure_4x4`, `T_Eau_Onde` (générées par `Assets/Art/Source~/Eau/scripts/eau_textures.py`, sans IA).
- `EauEclaboussure` (assemblée `ItHunts.Art.Eau`) : sur un **déclencheur** à la surface (`BoxCollider` en trigger) ; un objet
  qui entre crée une éclaboussure à sa position, de taille proportionnelle à la sienne (`echelle` : 1.2 par mètre, bornée entre
  0.4 et 8 : une caisse ≈ 1.5, le traqueur ≈ 8). Le jeu peut aussi appeler `Eclabousse(point, taille)` (par exemple depuis
  `DeepWater.OnTriggerEnter`).

## Solutions existantes (notées, **rien d'installé**)

Prix indicatifs relevés de mémoire, **à vérifier** sur l'Asset Store avant tout achat.

| Solution | Prix | Pour | Contre |
|---|---|---|---|
| **Stylized Water 3** (Staggart Creations, Asset Store) | payant, ~50 à 60 $ | eau stylisée complète pour l'URP (écume, vagues, réfraction, intersections, rivières), très suivie, compatible Forward+ | style « cartoon propre » à repeindre pour l'encre et les aplats ; dépendance à un éditeur tiers |
| **KWS Water System (URP)** | payant, ~80 à 100 $ | eau réaliste très complète (caustiques, mousse, flottaison) | réaliste : contraire à la DA ; lourd |
| **Crest Ocean System** (URP) | version open source ancienne gratuite ; Crest 5 payant (~100 $) | vagues et flottaison de qualité | océan, pas des canaux d'égout ; lourd pour notre usage |
| **Boat Attack** (démo Unity, GitHub, licence libre) | gratuit | eau URP officielle, code lisible | réaliste et figée sur une ancienne version de l'URP |
| Tutoriels d'eau toon (par exemple l'eau toon de Roystan, licence MIT) | gratuit | même principe que notre shader (profondeur, écume) | à adapter : c'est ce que fait déjà `ItHunts/EauToon` |

Recommandation : garder `ItHunts/EauToon` (aplats, encre, texture peinte du projet, coût faible) ; un achat n'apporterait
que la flottaison et les vagues 3D, que le jeu n'utilise pas.
