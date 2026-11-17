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
| Violet | réservé aux objets interactifs (lueur violette en jeu) et au marchand | le coffre normal de la mini-carte reste violet (décision) |

### Polices (validées, licence OFL)

| Usage | Police | Fichiers (`Assets/Art/UI/Polices/`) |
|---|---|---|
| Texte (menus, nombres, descriptions) | **EB Garamond** | `EBGaramond/EBGaramond-Regular.ttf`, `-Italic`, `-SemiBold` (nombres du HUD, boutons), `-Bold` |
| Titres gothiques (écrans, ACTE I / II / III) | **Grenze Gotisch** | `GrenzeGotisch/GrenzeGotisch-Regular.ttf`, `-SemiBold`, `-Bold` |

Fichiers statiques des dépôts d'origine (`octaviopardo/EBGaramond12`, `Omnibus-Type/Grenze-Gotisch`), licence **SIL Open
Font License 1.1** jointe dans chaque dossier (`OFL.txt`), sans nom de police réservé : intégration dans le jeu et vente du
jeu autorisées, la police seule ne se revend pas, la licence doit accompagner les fichiers. Importées comme `Font` Unity
(texte uGUI). Pour TextMesh Pro : *Window > TextMeshPro > Font Asset Creator* à partir de ces `.ttf` (SDF, 4096, jeu de
caractères latin étendu pour les accents), à faire sur `main` avec l'import des TMP Essentials.

Les nombres du HUD gagnent à avoir un contour sombre (`Outline` 1,5 px `#0A050A` à 90 %).

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
| `MiniCarte_Bougie` | **marchand** | 20 à 24 |
| `MiniCarte_Flamme` | **tas d'âmes / salle d'âmes** (gris-bleu des âmes, plus violet) | 24 |

Non découpées (décision) : cage, orbe fêlée, losange.

### 1.4 Décisions du lot A (2026-11-17)

- Le coffre normal reste violet (les objets interactifs ont une lueur violette en jeu).
- L'icône flamme passe en gris-bleu des âmes ; bougie = marchand, flamme = tas d'âmes / salle d'âmes.

## 2. Lot B : fonds de menu vivants (en cours)

Panorama unique (titre à gauche, campement à droite, même ciel et même château), calques et prefab
`Assets/Art/UI/Menus/Fond_Panorama.prefab` (à venir). Images à générer : `docs/art/ui/IMAGES-A-GENERER.md`.

### 2.1 Découpes faites (2026-11-17, `Assets/Art/UI/Menus/`)

Script : `Assets/Art/Source~/UI/scripts/lotb_decoupes.py` (réglages en tête de chaque fonction) ; planche de contrôle :
`docs/art/rendus/UI/LotB_decoupes.jpg`. Pas encore importées dans Unity (réglages d'import et `.meta` à faire).

| Sprite | Pixels | Mélange | Usage |
|---|---|---|---|
| `Fond/Fond_Ciel` | 1280 × 714 | opaque | ciel (B3) assombri : gamma 1.25, gain 0.72 (bleu un peu gardé) ; étiré sur toute la largeur, parallaxe 10 % |
| `Fond/Fond_Lune` | 1010 × 1010 | alpha | disque (rayon 174 px au centre de la toile) ; le sol de l'image d'origine est ignoré ; parallaxe 12 % |
| `Fond/Fond_Lune_Halo` | 1010 × 1010 | **additif** | halo reconstruit symétrique d'après son profil au-dessus du sol ; **sous** le disque, même centre ; à faire pulser (intensité) |
| `Fond/Fond_Nuages_1` à `_5` | ~1280 × 100–150 | alpha | 5 bandes de nuages peints (contour encré : alpha plein, pas d'additif) ; parallaxe 15 et 25 % |
| `Fond/Fond_Brume_1`, `_2` | 1280 × 238 / 275 | **additif** | bas de bande fondu sur 45 %, bords gauche et droit raccordés (répétables) ; parallaxe 40 et 80 % |
| `Fond/Fond_Chateau` | 1035 × 945 | alpha | château, ponts, falaise, cascades, découpés dans `ui_menu_principal_fond_v01` (B7/B8 abandonnées) ; coin haut gauche à **(1142, 74)** dans ce fond de 2752 × 1536 ; bas fondu dans la brume (y 790 → 1000) ; parallaxe 25 % |
| `Fond/Fond_Chateau_Fenetres` | 1035 × 945 | masque (gris) | fenêtres rouges tirées de leur couleur, même rect que le château : lueur animée (masque × couleur HDR, additif) |
| `Campement/Campement_Fond` | 1280 × 714 | opaque | B1 (campement vide) tel quel, en attendant le panorama découpé en calques |
| `Campement/Campement_Feu` | 550 × 674 | alpha | flammes (B15) avec leur contour encré |
| `Campement/Campement_Feu_Additif` | 550 × 674 | **additif** | mêmes flammes avec la lueur et les étincelles ; superposer les deux, ou l'additif seul pour une flamme plus légère |
| `Campement/Campement_Siege_Tronc`, `_Pierre`, `_Banc`, `_Rocher` | 332 / 209 / 365 / 291 × 219 | alpha | B16 ; **même hauteur de toile** (y 242–461 de l'image d'origine) : alignés par le bas de leur rect, les assises tombent à la même hauteur ; gris de fond retiré des bords |

**Mode additif** : PNG RGB sur noir pur ; matériau UI additif (`Blend One One`, ou *Particles/Additive* sur un `RawImage`). Le
noir n'ajoute rien, on peut donc empiler et animer l'opacité par la couleur (`Image.color`), pas par l'alpha.

### 2.2 Limites connues

- **Définition** : les images reçues font **1280 × 714** (au lieu du 2K demandé, 2752 × 1536) ; étirées en plein écran 1080p,
  elles sont floues (×1.5). À télécharger en 2K si Higgsfield le permet, puis relancer le script (mêmes réglages).
- **Château** : segmentation semi-manuelle (polygone, germes le long des flèches, chemin le plus court guidé par les contours).
  Restent : deux petites taches de ciel aux pointes de la tour gauche, le ciel vu sous les arches du pont (gardé), et des
  branches des arbres proches aux bords bas (gauche et droite). Passe d'édition E1 (`IMAGES-A-GENERER.md`) pour les retirer.
- **B1 et sa passe 1** : calées au pixel (décalage mesuré 0 sur l'image entière), mais **toute l'image est repeinte** à chaque
  passe (écart moyen 12/255, 17 % des pixels à plus de 20/255) : un masque tiré d'une différence entre deux passes demande un
  seuil, un nettoyage et un bord adouci ; seuls les gros objets en sortent proprement.

### 2.3 Reste à faire

B2 (raccord, entrée prête) ; passes d'édition E1–E9 (remplacent B9–B14) puis découpe des calques arbres proches, sol et
premier plan, fond lointain ; prefab `Fond_Panorama` avec parallaxe ; import Unity (Sprite, PPU 200, Full Rect, Clamp,
sans mipmaps), matériau additif.

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
- Polices : EB Garamond (texte) et Grenze Gotisch (titres).
- Lots B et C : liste d'images validée, personnages en 3:4 ; images déposées dans `docs/art/ui/generees/` ; test du renard animé seulement quand les images du lot C sont arrivées.
