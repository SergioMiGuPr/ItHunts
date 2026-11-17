# UI-HANDOFF : habillage de l'interface

Ce que `main` doit savoir pour brancher l'interface habillée : sprites, bordures, tailles, ancrages, couleurs, polices,
états et réglages des animations. Tenu par la piste DA (branche `art-ui`). Les sprites sont **visuels seulement** :
tout le texte (nombres, noms, boutons, descriptions) est du **texte Unity**, sauf le logo IT HUNTS.

Maquettes et pièces d'origine (images générées par IA, Higgsfield, Nano Banana Pro) : `docs/art/ui/maquettes/`.
Elles servent de référence et de matière à découper ; aucune n'est intégrée telle quelle.

## 0. Conventions

| Règle | Valeur |
|---|---|
| Canvas | `Screen Space - Camera` (comme `PlayerHud`), `CanvasScaler` **Scale With Screen Size**, référence **1920 × 1080**, **Match = 1 (hauteur)**, Reference Pixels Per Unit **100** |
| Sprites | **double définition** : **Pixels Per Unit 200**. *Set Native Size* donne la taille prévue en 1080p (pixels du fichier / 2) ; net jusqu'en 2160p |
| Réduire un élément à bordures | multiplier la taille par *k* **et** régler `Image.pixelsPerUnitMultiplier = 1 / k`, sinon les bordures (emblème, pointe) se déforment |
| Écran partagé | chaque joueur a son canvas ; en 960 × 1080 l'échelle reste 1 (Match hauteur), seule la largeur disponible baisse : ancrer aux coins, pas au centre |
| Import | Sprite (Single), Mesh **Full Rect** (obligatoire pour Sliced / Tiled), pas de mipmaps, Bilinear, Clamp, compression **High Quality** |
| Atlas | `Assets/Art/UI/Atlas_HUD.spriteatlas` (HUD + mini-carte, marge 4 px, sans rotation ni découpe serrée) |
| Noms | `Groupe_Element[_Partie]` : `BarreVie_Cadre_Embleme`, `MiniCarte_Coffre_Rare`… |

**Réglage à faire sur `main` (ProjectSettings, hors périmètre art)** : *Project Settings > Editor > Sprite Packer > Mode* est
sur **Disabled** (`m_SpritePackerMode: 0`), donc l'atlas n'est pas empaqueté. Le passer sur **Sprite Atlas V2 - Enabled**
(Unity propose alors de convertir `Atlas_HUD.spriteatlas` en `.spriteatlasv2` : accepter). Sans ce réglage tout s'affiche
quand même, avec un appel de rendu par texture.

### Couleurs

| Rôle | Hex | Note |
|---|---|---|
| Âmes (flamme, nombre) | `#E0FAFD` cœur, `#7EC2DB` milieu, `#647A8D` bord ; texte `#D6E0EB` | gris-bleu clair spectral, plus pâle et plus gris que le hibou |
| Joueur renard (rouge) | `#DD373F` | pastille, bord de vignette, lanterne |
| Joueur koala (vert) | `#55C851` | |
| Joueur caméléon (orange) | `#DEA13F` | |
| Joueur hibou (bleu) | `#5FB9ED` | |
| Métal des cadres | `#96849C` moyen, `#D8C6CD` reflets | |
| Fond des cartouches | `#0E0B12`, opacité 86 % | |
| Vie (remplissage) | `#C41E26` haut, `#8A0A14` milieu, `#3E040A` bas (médiane `#7D0912`) | rouge sang |
| Vie perdue (traînée) | `#ECD6CE` → `#967E7C` | se teinte dans `Image.color` si besoin |
| Violet | réservé aux objets attrapables et au marchand | voir § 1.4 (coffre normal et flamme de la mini-carte) |

### Polices proposées (licence OFL, usage commercial libre, à valider)

| Usage | Proposition | Repli | Pourquoi |
|---|---|---|---|
| Texte (menus, nombres, descriptions) | **EB Garamond** (SemiBold pour les nombres du HUD) | **Cormorant Garamond** (plus élégant, mais déliés fins : à réserver aux grands corps) | empattements proches de « Jouer / Quitter » des maquettes, lisible petit |
| Titres gothiques (écrans, ACTE I/II/III) | **Grenze Gotisch** | **UnifrakturMaguntia** | gothique lisible, plusieurs graisses |

Toutes deux sur Google Fonts (SIL Open Font License 1.1 : intégration et vente autorisées, la police seule ne se vend pas).
Les nombres du HUD gagnent à avoir un contour sombre (`Outline` 1,5 px `#0A050A` à 90 %). Rien n'est encore importé :
dès validation, j'ajoute les fichiers `.ttf` et leurs fontes TextMesh Pro dans `Assets/Art/UI/Polices/`.

## 1. Lot A : HUD (fait)

Scène d'essai : `Assets/Art/Scenes/Test_UI_HUD.unity` (HUD complet, valeurs factices ; planche des icônes à 48 et 24 px en
bas à droite). Planche : `docs/art/rendus/UI/LotA_HUD_avant_apres.jpg`.

Les tailles « 1080p » sont la taille native (*Set Native Size*). Bordures en pixels du sprite (gauche, bas, droite, haut).

### 1.1 Barre de vie (`Assets/Art/UI/HUD/`)

| Sprite | Pixels | 1080p natif | Bordures | Usage |
|---|---|---|---|---|
| `BarreVie_Cadre` | 862 × 262 | 431 × 131 | 245, 0, 106, 0 | cadre entier : `Image` **Sliced** (centre étiré) ; la longueur se règle par la largeur |
| `BarreVie_Fond` | 862 × 262 | 431 × 131 | 245, 0, 106, 0 | fond sombre de l'emblème et du canal, même rect que le cadre, **dessous** |
| `BarreVie_Cadre_Embleme` / `_Milieu` / `_Bout` | 245 / 128 / 106 × 262 | 122,5 / 64 / 53 × 131 | — | même cadre en 3 parties : le milieu est **raccordable** (`Image` Tiled, ou répété) |
| `BarreVie_Fond_Embleme` / `_Milieu` / `_Bout` | idem | idem | — | fond en 3 parties, alignées pixel pour pixel sur le cadre |
| `BarreVie_Remplissage` | 610 × 46 | 305 × 23 | 23, 0, 28, 0 | rouge sang ; `Image` **Filled**, Horizontal, origine Left, `fillAmount = PV / PVmax` |
| `BarreVie_Remplissage_Retard` | 610 × 46 | 305 × 23 | 23, 0, 28, 0 | traînée claire des dégâts récents, sous le remplissage, rattrape la valeur en ~0,4 s |

**Canal** (zone du remplissage) dans le cadre natif : à **100 px du bord gauche**, **26 px du bord droit**, **54 px du haut**,
**23 px de haut** (à multiplier par *k*). Le remplissage s'ancre en étirement horizontal sur ce canal, il suit donc la longueur
de la barre. Ordre : `Fond` → `Remplissage_Retard` → `Remplissage` → `Cadre` → texte « 45/100 » (texte Unity, à 14 px du début
du canal, 17 px en 1080p). Dans `Test_UI_HUD` : *k* = 0,9 (118 px de haut), 440 px de long, coin bas gauche à (32, 32).

### 1.2 Cartouche des âmes (`Assets/Art/UI/HUD/`)

| Sprite | Pixels | 1080p natif | Bordures | Usage |
|---|---|---|---|---|
| `Ames_Cadre` | 607 × 204 | 303,5 × 102 | 190, 0, 93, 0 | cadre entier, `Image` Sliced |
| `Ames_Fond` | 607 × 204 | 303,5 × 102 | 190, 0, 93, 0 | fond sombre du rond et du cartouche, dessous |
| `Ames_Cadre_*`, `Ames_Fond_*` (Embleme / Milieu / Bout) | 190 / 96 / 93 × 204 | 95 / 48 / 46,5 × 102 | — | version 3 parties (milieu raccordable) |
| `Ames_Flamme` | 153 × 256 | 76,5 × 128 | — | flamme des âmes ; dans le rond : hauteur ≈ 0,8 × diamètre du rond, `preserveAspect` |

Rond de l'icône : centre à **(58, 51) px** du coin haut gauche (natif), diamètre intérieur **51 px**. Zone du nombre : commence à
**86 px** du bord gauche, centrée verticalement (hauteur utile 46 px). Dans `Test_UI_HUD` : *k* = 0,75 (76 px de haut),
250 px de long, coin haut gauche à (32, −32), nombre en 30 px.

### 1.3 Mini-carte (`Assets/Art/UI/MiniCarte/`)

| Sprite | Pixels | 1080p natif | Usage |
|---|---|---|---|
| `MiniCarte_Cadre` | 562 × 590 | 281 × 295 | anneau gothique, **au-dessus** de tout |
| `MiniCarte_Masque` | 562 × 590 | 281 × 295 | disque blanc : `Image` + `Mask` (`Show Mask Graphic` décoché) ; la carte et les icônes sont ses enfants |
| `MiniCarte_Fond` | 562 × 590 | 281 × 295 | disque sombre (82 %) légèrement vignetté, sous la carte |

Les trois ont le même rect : les empiler en étirement. Centre de la zone visible : (140, 143) px du coin haut gauche (natif),
**rayon 96 px** (bord intérieur de l'anneau ; les ornements débordent jusqu'à 75 px du centre). Dans `Test_UI_HUD` :
*k* = 0,85, coin haut droit à (−32, −32).

**Icônes** (96 × 96 px, natif 48 ; pastilles 48 × 48 px, natif 24) : trait d'encre net, sans lueur ni fond. Lisibles à **24
et 48 px** sur fond sombre (vérifié dans la scène, 1920 × 1080 et 960 × 1080).

| Sprite | Sens | Taille conseillée (1080p) |
|---|---|---|
| `MiniCarte_Fleche` | joueur local, pointe vers le haut au repos : tourner selon le cap | 24 |
| `MiniCarte_Joueur_Rouge` / `_Vert` / `_Orange` / `_Bleu` | alliés (renard, koala, caméléon, hibou) | 14 à 18 |
| `MiniCarte_Coffre_Normal` / `_Rare` / `_Ouvert` | coffres | 24 |
| `MiniCarte_Porte_Verrouillee` / `_Ouverte` | porte finale | 24 |
| `MiniCarte_Allie_Mort` | allié mort (tête de mort) | 24 |
| `MiniCarte_Bougie` | bougie | 20 à 24 |
| `MiniCarte_Flamme` | flamme | 24 |

Non découpées (décision) : cage, orbe fêlée, losange.

### 1.4 Points ouverts du lot A

- **Violet** : le coffre normal et la flamme de la mini-carte sont violets sur la maquette, alors que le violet est réservé
  aux attrapables et au marchand. Je peux les repasser (coffre normal en fer gris-bleu ; flamme en gris-bleu des âmes si
  elle désigne une source d'âmes) : à décider.
- Sens de `MiniCarte_Bougie` et `MiniCarte_Flamme` dans le jeu : à préciser par `main`.

## 2. Lot B : fonds de menu vivants (à venir)

Panorama unique (titre à gauche, campement à droite, même ciel et même château), calques et prefab
`Assets/Art/UI/Menus/Fond_Panorama.prefab`. Images à générer : `docs/art/ui/IMAGES-A-GENERER.md`.

## 3. Lot C : personnages 2D animés (à venir)

Personnages en 2D peinte, animés par shader (masque peint) et 2 ou 3 pièces au besoin, sans os 2D. Test sur le renard
d'abord. Prefabs dans `Assets/Art/UI/Personnages/`. Images à générer : `docs/art/ui/IMAGES-A-GENERER.md`.

## 4. Lot D : écran de choix du personnage (à venir)

Cadre gothique, 4 vignettes (bord à la couleur du personnage), grande pose debout animée, panneau d'artefact (icône +
texte Unity), boutons Valider et Prêt (normal, survol, pressé, désactivé ; Prêt désactivé bien lisible pour les doublons).
Aucun personnage verrouillé, pas de skins, pas de passif.

## 5. Décisions de l'utilisateur (2026-11-17)

- Âmes : gris-bleu clair spectral (référence `ui_flamme_ames_v01`) ; violet réservé aux attrapables et au marchand.
- Difficulté **ACTE I / ACTE II / ACTE III** en haut à gauche du campement ; chat en bas à gauche (place prévue, masqué
  tant que le jeu en ligne n'existe pas).
- Personnages des menus en 2D peinte avec de légers mouvements.
- Choix du personnage : doublons permis ; c'est le bouton Prêt/Jouer qui se désactive (logique côté `main`).
- « Settings » devient « Paramètres » (texte Unity).
