# Matériaux définitifs (étape 2 du chantier rendu)

**Branche `art`, 2026-11-15.** Tous les matériaux utilisent le shader maison `ItHunts/Toon` (voir `docs/art/RENDU.md`)
avec ses **valeurs par défaut** : 3 bandes, seuil 0.15, transitions 0.02, teinte d'ombre bleu nuit
(0.028, 0.034, 0.075), ambiance × 1, ombres reçues, 3 paliers pour les lumières ponctuelles, rim désactivé.
**Ni métal ni brillance** (le shader n'a pas de reflet spéculaire). **Aucune émission sur le décor.**
Le réglage d'ambiance retenu (**intermédiaire**) est un réglage de scène, pas de matériau.

## Calage des textures

Les UV des FBX sont **déjà en échelle monde** : les scripts Blender (`Assets/Art/Source~/Niveau1/scripts/it_common.py`,
`world_uv`) projettent les faces sur une tuile de taille fixe, et règlent le fût de la colonne et les blocs modélisés
pour qu'ils tombent sur les pierres peintes. Les matériaux Blender n'ont **aucun nœud de mapping**. Donc, dans Unity :
**tiling (1, 1), offset (0, 0) pour tous les matériaux** : ils reproduisent exactement le calage Blender.

| Texture | Taille de tuile (Blender) | Résolution | Densité |
|---|---|---|---|
| `texture_mur_pierre_v01_tuile` | 4 × 3 m | 1024 × 768 | 256 px/m |
| `texture_sol_dalles_v01_tuile` | 4 × 4 m | 1024 × 1024 | 256 px/m |
| `texture_mur_egout_v01_tuile` | 2 × 2 m | 1024 × 1024 | 512 px/m |
| `texture_fer_rouille_v01_tuile` | 1 × 1 m | 1024 × 1024 | 1024 px/m |
| `texture_voute_briques_v01_tuile` | **non définie** (2 × 2 m dans `Test_Rendu`) | 1024 × 1024 | 512 px/m si 2 m |
| `texture_eau_egout_v01_tuile` | **non définie** (2 × 2 m dans `Test_Rendu`) | 1024 × 1024 | 512 px/m si 2 m |
| `T_GrandePorte_Bois_BaseColor` | atlas des deux battants (10 × 12.7 m) | 1024 × 1024 | **≈ 50 × 80 px/m** |
| `T_Poutre_Bois_BaseColor` | atlas long + bout (poutre de 6 m) | 1024 × 512 | **≈ 170 px/m** sur la longueur |
| `T_Barillet_Laiton_BaseColor` | atlas (barillet de 10 cm) | 1024 × 1024 | — (vue FPS) |

## Matériaux

### Niveau 1 (`Assets/Art/Materials/Niveau1/`)

| Matériau | Utilisé par (FBX remappés) | Texture | Tiling / offset | Couleur | Émission |
|---|---|---|---|---|---|
| `M_Pierre` | `SM_Arc_Couloir`, `SM_Colonne`, `SM_GrandePorte` (cadre), `SM_Pont_Egout_4m`, `SM_Pont_Egout_14m`, `SM_Porte_Piece` | `texture_mur_pierre_v01_tuile` | (1, 1) / (0, 0) | blanc | non |
| `M_FerRouille` | `SM_Colonne` (anneau), `SM_GrandePorte` (ferrures, anneaux), `SM_Porte_Piece` (gonds), `SM_Poutre` (cerclages, crochet) | `texture_fer_rouille_v01_tuile` | (1, 1) / (0, 0) | blanc | non |
| `M_Dalles` | `SM_Pont_Egout_4m`, `SM_Pont_Egout_14m` (tablier) | `texture_sol_dalles_v01_tuile` | (1, 1) / (0, 0) | blanc | non |
| `M_Egout` | `SM_Pont_Egout_4m`, `SM_Pont_Egout_14m` (douelle de l'arche, culées côté canal) | `texture_mur_egout_v01_tuile` | (1, 1) / (0, 0) | blanc | non |
| `M_GrandePorte_Bois` | `SM_GrandePorte` (battants) | `T_GrandePorte_Bois_BaseColor` | (1, 1) / (0, 0) | blanc | non |
| `M_GrandePorte_Serrure` | `SM_GrandePorte` (deux moitiés de la serrure) | `texture_fer_rouille_v01_tuile` | (1, 1) / (0, 0) | blanc | non (voir questions) |
| `M_Poutre_Bois` | `SM_Poutre` (bois) | `T_Poutre_Bois_BaseColor` | (1, 1) / (0, 0) | blanc | non |
| `M_Voute_Briques` *(proposé, aucun FBX)* | `Test_Rendu` : voûte du couloir | `texture_voute_briques_v01_tuile` | (1, 1) / (0, 0), UV à 2 × 2 m | blanc | non |
| `M_Eau_Egout` *(proposé, aucun FBX)* | `Test_Rendu` : eau du canal | `texture_eau_egout_v01_tuile` | (1, 1) / (0, 0), UV à 2 × 2 m | blanc | non |

### Barillet, vue FPS (`Assets/Art/Materials/FPS/`)

| Matériau | Utilisé par | Texture | Couleur de base | Émission |
|---|---|---|---|---|
| `M_Barillet_Laiton` | `SM_Barillet` (corps) | `T_Barillet_Laiton_BaseColor`, (1, 1) / (0, 0) | blanc | non |
| `M_Barillet_Rune` | `SM_Barillet` (rune gravée) — **remappé par défaut** | aucune | or (0.86, 0.70, 0.34) **linéaire, comme dans Blender** (sRGB ≈ #EDDA9E) | non |
| `M_Barillet_Rune_Emission` | variante, au choix du DA | aucune | même or | **oui**, or chaud HDR (1.6, 1.15, 0.45) (provisoire, à choisir) |
| `M_Barillet_Chambre` | `SM_Barillet_Chambre_01` à `_06` | aucune | **#1A120C** | **mot-clé actif, couleur noire** : rien ne brille par défaut, le code allume chaque chambre |

**Pourquoi l'émission de la chambre est « active mais noire » :** le code allume les chambres une à une par un bloc de
propriétés (`MaterialPropertyBlock`), qui peut changer `_EmissionColor` mais pas activer un mot-clé de shader. Avec le
mot-clé actif et une couleur noire, une chambre vide est sombre et le code n'a qu'à écrire la couleur d'émission
(démonstration : `Comparaison_Barillet_dos_Unity-Blender.jpg`, 3 chambres allumées en orangé provisoire).

Dans un vrai matériau de barillet, les FBX remappés n'ont plus de sous-matériaux : tous les prefabs et scènes qui
utilisent ces FBX prennent ces `.mat` sans autre réglage.

### Matériaux de rendu (`Assets/Art/Materials/Rendu/`)

- `M_Contours` : passe de contours plein écran (mode « clair dans le noir », fondu 40 → 100 m) ; voir `RENDU.md` § 2.
- `M_Rendu_*` (mannequin, traqueur provisoire, visage, yeux, cire et flamme des bougies) : outils de `Test_Rendu`
  seulement, pas des matériaux du jeu.
- `M_Signal_*_Regle` (zone du traqueur et son ombre, bond de goule, caisse attrapable, surface magnétique, âme) :
  versions toon « à travers le brouillard » des signaux du jeu, pour l'essai de lisibilité (`RENDU.md` § 3 ter). Ce
  sont des propositions de réglage : les matériaux du jeu (`Assets/_Project/Art/Greybox/`) n'ont pas été modifiés.

### Props du niveau 1 (`Assets/Art/Materials/Niveau1/`, lot de modélisation, groupe B)

Les props réutilisent `M_FerRouille`, `M_Pierre` et `M_Egout` (UV en échelle monde, mêmes tuiles). Nouveaux
matériaux, tous sur `ItHunts/Toon`, ni métal ni brillance :

| Matériau | Utilisé par | Texture | Couleur de base | Particularité |
|---|---|---|---|---|
| `M_Bois_Brut` | manche de la torche, tringle de la tapisserie, échafaudage | `T_Bois_Brut_BaseColor` (tuile peinte de 1 m, 256 px/m, fil vertical) | blanc | — |
| `M_Chiffon_Brule` | tête de la torche | `T_Chiffon_Brule_BaseColor` (tuile peinte de 1 m) | blanc | — |
| `M_Corde` | ligatures (torche, échafaudage) | aucune | #7A6548 | — |
| `M_Os` | `SM_Cage_Os` | aucune | #CFC3A6 | — |
| `M_Fond_Noir` | fond de la bouche du tuyau (trous, fonds d'ombre) | aucune | #070606 | — |
| `M_Levier_Lueur` | cristal et anneau de rune de `SM_Levier` (arène, groupe C) | aucune | #5A3420 (ambre sombre, éteint) | **mot-clé actif, couleur noire** : orange (prêt) puis vert (activé) en jeu ; traverse un peu le brouillard (0.6) |
| `M_Tapisserie` | `SM_Tapisserie` | `texture_tapisserie_v02_alpha` (import : alpha transparent, bord en Clamp) | blanc | **découpe alpha** (seuil 0.5), **double face** (Cull Off), file AlphaTest |

### Caisse attrapable (`SM_Caisse`, modèle Tripo nettoyé ; `M_Caisse` créé le 2026-11-16)

| Matériau | Utilisé par | Texture | Émission |
|---|---|---|---|
| `M_Caisse` | `SM_Caisse` | `T_Caisse_BaseColor` (512 px, tiling (1, 1)) : bois, fer, corde ; **rune gravée, neutre (sans violet), sur les quatre faces latérales** | **mot-clé actif, couleur noire** ; `_EmissionMap` = `T_Caisse_Emission` (512 px, blanc = cœur de la rune) : le jeu écrit `_EmissionColor` (lueur des objets attrapables), seule la rune s'allume |

Le violet de la rune peinte par Tripo (et ses reflets mauves) a été ramené au brun du bois dans la couleur : le violet
est réservé à la télékinésie et la lueur passe par l'émission (`sm_caisse.py`, fonction `textures`).

### Lot du 2026-11-16 (UV monde, torche, ennemis, flamme de la lanterne, bras FPS)

| Matériau | Utilisé par | Texture | Réglages | Émission |
|---|---|---|---|---|
| `M_Pierre_Monde`, `M_Dalles_Monde`, `M_Voute_Briques_Monde`, `M_Egout_Monde` (`Niveau1/`) | pièces du kit `SM_Kit_*` (pierre et dalles) ; voûte et égout prêts pour l'architecture construite par le code | celles de `M_Pierre`, `M_Dalles`, `M_Voute_Briques`, `M_Egout` | **`_WORLD_UV` actif**, `_TileSize` (4, 3), (4, 4), (3, 3), (2, 2) m ; `_TileOffset` (0, 0, 0) ; `_TopRotation` 0 | non |
| `M_Torche` (`Niveau1/`) | `SM_Torche` (remplace `M_FerRouille`, `M_Bois_Brut`, `M_Chiffon_Brule`, `M_Corde` sur la torche seulement) | `T_Torche_BaseColor` (atlas de 512 px, couleurs des 4 matériaux cuites dans Blender, `n1_props.bake_atlas`) | tiling (1, 1) | non |
| `M_Goule` (`Ennemis/`) | `SM_Goule_LOD0`, `_LOD1`, `_LOD2` (statique `SM_Goule` et gréé `SK_Goule`) | `T_Goule_BaseColor` (2048 px, **repeinte le 2026-11-16** : peau mauve sombre et saturée en aplats, plis et craquelures à l'encre, ecchymoses ; yeux plus petits rouge sombre ; `peinture.py`) | — | mot-clé actif, **noir** ; `_EmissionMap` = `T_Goule_Emission` (yeux : petit disque 1 ; entre les dents 0.5 ; lueur discrète voulue, le jeu choisit l'intensité) ; à travers le brouillard 0.8 |
| `M_Traqueur` (`Ennemis/`) | `SM_Traqueur` (corps), `SM_Traqueur_Corps` | `T_Traqueur_BaseColor` (4096 px, peau gris-mauve, pupilles rouges peintes) | — | mot-clé actif, **rouge HDR (4, 0.25, 0.15) par défaut** ; `_EmissionMap` = `T_Traqueur_Emission` (2048 px : pupilles et petit halo) ; le code peut l'augmenter (enragé) |
| `M_Traqueur_Mains` (`Ennemis/`) | bras et mains : `SM_Traqueur_Bras`, `_Main_D`, `_Main_G`, et le second emplacement de `SM_Traqueur` | `T_Traqueur_BaseColor` (griffes éclaircies vers l'os clair) | **liseré** (`_RIM_ON`) couleur (0.58, 0.54, 0.72), largeur 0.30, bord 0.05, intensité 0.35, même dans le noir (`_RimLitOnly` 0) | mot-clé actif, `_EmissionMap` = `T_Traqueur_Griffes_Emission` (2048 px : griffes des deux mains) ; **repos (0.35, 0.32, 0.25)**, **prise (3.5, 0.49, 0.21)** écrit par le jeu ; à travers le brouillard 0.6 |
| `M_LanterneFeu_Flamme` (`FPS/`) | `SM_LanterneFeu_Flamme` | aucune (couleur (1, 0.76, 0.48)) | double face | mot-clé actif, **orange du renard (3, 0.96, 0.24) par défaut** ; le jeu écrit `_EmissionColor` à la couleur du personnage ; à travers le brouillard 1 |
| `M_BrasFPS_<Perso>_<Partie>` (`FPS/BrasFPS/`, 37 matériaux) | `SK_BrasFPS_<Perso>_D` / `_G` | aucune (aplats) | couleur de base reprise des aplats du FBX | non |

### Lanterne de feu et orbe, vue FPS (`Assets/Art/Materials/FPS/`, lot de modélisation, groupe A)

Tous sur `ItHunts/Toon`, ni métal ni brillance, tiling (1, 1) / offset (0, 0) : les UV des FBX sont en échelle monde
(tuile de **0.05 m**), calées dans Blender.

| Matériau | Utilisé par | Texture | Couleur de base | Émission |
|---|---|---|---|---|
| `M_LanterneFeu_Fer` | `SM_LanterneFeu` (base, montants, chapeau, fleuron, anneau, coupelle) | `T_LanterneFeu_Fer_BaseColor` | blanc | non |
| `M_LanterneFeu_Cuivre` | `SM_LanterneFeu` (bandeaux, liserés, tête du fleuron) | `T_LanterneFeu_Cuivre_BaseColor` | blanc | non |
| `M_LanterneFeu_Meche` | `SM_LanterneFeu_Meche` | aucune | #17110D | non |
| `M_LanterneFeu_Verre` | `SM_LanterneFeu_Verre` | aucune | **#7A0A12, alpha 0.55** (couleur du renard) | **mot-clé actif, couleur noire** (le code l'allume) |
| `M_Orbe_Pierre` | dalles des 3 anneaux, corps des 6 tablettes | `T_Orbe_Pierre_BaseColor` | blanc | non |
| `M_Orbe_Fer` | gorges et rivets des anneaux | `T_Orbe_Fer_BaseColor` | blanc | non |
| `M_Orbe_Energie` | `SM_Orbe_Energie` | aucune (UV sphériques réservées au shader de vortex) | #8E1B12 (rouge renard ; **couleur du personnage**, réglée par le code) | **mot-clé actif, couleur noire** |
| `M_Orbe_Tablette_01` à `_06` | **le glyphe seulement** de chaque tablette (2e matériau du renderer, index 1) | aucune | #D9D3C7 (glyphe pâle) | **mot-clé actif, couleur noire** : une tablette s'allume quand sa charge est prête |

**Textures peintes** (`Assets/Art/Textures/FPS/`) : `T_LanterneFeu_Fer`, `T_LanterneFeu_Cuivre`, `T_Orbe_Pierre`,
`T_Orbe_Fer` (`_BaseColor`), **256 × 256 px pour 0.05 m, soit 5 120 px/m**, la densité de `T_Barillet_Laiton`
(512 px pour Ø 0.1 m). Tuiles sans couture, peintes par script (`Assets/Art/Source~/FPS/scripts/fps_common.py`,
`PAINT`) dans la palette relevée sur les fiches : aplats en paliers, traînées de pinceau, quelques taches de rouille
(fer), fissures et éclats (pierre). Couleur de base seule, pas de normal map. Réglages d'import par défaut (sRGB,
répétition, mipmaps), comme le barillet.

**Verre translucide :** `ItHunts/Toon` a maintenant un mélange réglable par matériau (`_SrcBlend`, `_DstBlend`,
`_ZWrite` ; par défaut opaque, rien ne change pour les autres matériaux). Le verre est en SrcAlpha / OneMinusSrcAlpha,
ZWrite Off, file 3000, passes ShadowCaster, DepthOnly et DepthNormals coupées (il ne fait pas d'ombre et n'a pas de
contour ; ses montants, si). **À faire sur `main` :** la passe « ItHunts Mains : mains » du renderer ne dessine que la
file opaque (`RenderQueueType: 0`) : sur la couche des mains, le verre ne serait pas dessiné. Ajouter une passe
transparente pour les couches 8 à 11 (même événement, après la passe opaque), ou passer le verre en opaque.

Captures (`docs/art/rendus/FPS/`, `Test_Rendu`, réglage intermédiaire, contours clairs dans le noir) :
`Unity_LanterneFeu_eteint.jpg` / `_allume.jpg` et `Unity_Orbe_eteint.jpg` / `_allume.jpg` dans l'îlot d'une bougie,
`Unity_*_lune.jpg` hors des bougies (seules l'émission et les contours se lisent). « Allumé » est une démonstration
faite comme le ferait le code (bloc de propriétés) : verre et énergie en rouge renard, tablettes 01, 02 et 04.
Rendus de modélisation (Blender, lumière plate) : `SM_LanterneFeu_*.jpg`, `SM_Orbe_*.jpg`.

## Orbe : couleur du personnage, animations par action, jauge des tablettes

**Décision (Sergio, 2026-11-15) : l'orbe garde la couleur de son personnage en permanence** (rouge renard, orange
caméléon, vert koala, bleu hibou) : énergie, glyphes allumés, et verre de la lanterne pour le renard. **Les actions
ne se lisent plus à la couleur de l'orbe, mais au mouvement des anneaux et des tablettes.** Cela remplace la couleur
par action de D-024 (`ART-HANDOFF.md` § 0 quater, à mettre à jour sur `main`). Les autres codes couleur du jeu
(trait et ancre de traction, cibles, rouge d'annonce et de refus) ne sont pas concernés.

Repères : les anneaux tournent autour de leur axe local (anneau 1 : X, 2 : Y, 3 : Z, repère Unity, pivot au centre),
depuis leur pose de repos (0°, 60°, 120°, déjà dans la géométrie : rotation nulle au repos). L'orbe est petite à
l'écran : il faut des mouvements **amples, rapides et francs** (grands angles, changements de vitesse nets), lisibles
en vision périphérique à pleine vitesse, plutôt que des détails.

### Animations proposées (rien n'est codé ; valeurs de départ, à mettre dans le ScriptableObject de l'orbe)

| État | Anneaux | Tablettes et énergie | Lecture à pleine vitesse |
|---|---|---|---|
| **Repos** | rotation lente et désynchronisée : 20, 30 et 45°/s ; léger flottement (±2 mm, 0.5 Hz) | tablettes immobiles en couronne, allumées selon les charges ; énergie qui pulse doucement (±10 %, 0.5 Hz) | calme : tout bouge lentement, dans des sens différents |
| **Traction** (maintenue) | les 3 anneaux se **calent dans le même plan** (0.1 s, perpendiculaire à la visée), puis **tournent vite dans le même sens** (720°/s) : un disque qui vrille | la couronne de tablettes tourne avec eux (90°/s) ; énergie plus vive (+50 %) | le seul état où tout tourne ensemble, vite, dans le même sens |
| **Répulsion** (coup, ou onde maintenue 0.8 s) | les anneaux **s'écartent d'un coup** : échelle ×1.35 en 0.06 s, chacun basculé de 25° sur son axe, puis retour en 0.25 s (amorti) ; onde maintenue : ils restent écartés et vibrent (±3°, 20 Hz) | tablettes projetées vers l'extérieur (rayon 0.13 → 0.17 m) puis retour ; l'énergie flashe puis se contracte | une « explosion » de la silhouette, puis le retour |
| **Objet tenu** (télékinésie) | anneaux **resserrés autour de l'énergie** (échelle ×0.8 en 0.15 s), alignés en gyroscope serré, rotation régulière (180°/s, sens alternés) | couronne resserrée (0.13 → 0.10 m), tablettes inclinées vers le centre ; énergie stable | une boule compacte et tenue : silhouette plus petite et plus dense |
| **Lancer** (éclat bref, l'emporte sur le reste) | coup d'arrêt, puis les anneaux pivotent de 90° en 0.08 s dans le sens du lancer | **éclat bref des glyphes** (0.12 s, émission ×3), l'énergie flashe | un flash et un « coup de fouet » |
| **Traction + objet tenu** | la traction l'emporte pour les anneaux (règle de D-024) ; la couronne reste resserrée | | |
| **Recharge** (après un lancer) | ralentis (×0.5) | glyphes éteints jusqu'à la fin de la recharge, puis un tic d'allumage | |
| **Refus** (plus de charge, surface non magnétique, trop lourd) | secousse sèche (±8°, 3 allers-retours en 0.15 s) | les tablettes de la charge manquante clignotent deux fois | se lit même sans regarder les tablettes |
| **Charge rechargée** | | la tablette s'allume par un éclat (0.1 s, ×2), puis reste allumée | |

Interpolations : courbes franches (sortie rapide) pour les déclenchements, amorties pour les retours. Vitesses et
durées : des points de départ pour le playtest.

**Planche des poses** (couleur du caméléon, vue de l'œil du joueur, mouvement en surimpression de 3 instants) :
`docs/art/rendus/FPS/Orbe_Poses_Planche.jpg` et une image par état (`Orbe_Poses_01_Repos.jpg` à
`Orbe_Poses_08_ChargeRechargee.jpg`) ; scripts `sm_orbe_poses.py` (Blender) puis `orbe_poses_legendes.py` (légendes).

### Jauge des tablettes : les 6 charges du jeu

**6 tablettes = 6 charges : 2 de traction, 1 de répulsion, 3 de mouvement** (la traction est passée à 2 charges, ce
n'est plus 3 + 3). **Une tablette s'allume quand sa charge est prête** (émission de son glyphe, couleur du
personnage), et s'éteint quand la charge est utilisée.

Répartition proposée (vue du joueur, l'orbe en bas à droite de l'écran) :

| Tablettes | Position | Glyphes | Charge |
|---|---|---|---|
| `_02`, `_03` | en haut à droite, en haut à gauche (40°, 140°) | arête de poisson, peigne | **traction** (2) |
| `_04` | à gauche (180°) | triangle sur tige | **répulsion** (1) |
| `_01`, `_06`, `_05` | à droite, en bas à droite, en bas à gauche (0°, 320°, 220°) | losange, spirale carrée, chevrons | **mouvement** (3) |

Les deux charges de traction sont ensemble en haut, la répulsion seule sur le côté, le mouvement en bas : chaque
groupe se lit à sa position, pas à sa couleur. La numérotation est celle des objets du FBX (angles mesurés depuis +X,
sens trigonométrique vu du joueur).

## FBX remappés (`.meta` modifiés, seule exception à la règle des `.meta`)

Onglet *Materials* de l'import : chaque matériau intégré est remappé vers le `.mat` du même nom
(`externalObjects`). Aucun autre réglage d'import n'a changé.

| `.meta` modifié | Remappages |
|---|---|
| `Assets/Art/Models/FPS/SM_Barillet.fbx.meta` | `M_Barillet_Laiton`, `M_Barillet_Rune`, `M_Barillet_Chambre` |
| `Assets/Art/Models/Niveau1/SM_Arc_Couloir.fbx.meta` | `M_Pierre` |
| `Assets/Art/Models/Niveau1/SM_Colonne.fbx.meta` | `M_Pierre`, `M_FerRouille` |
| `Assets/Art/Models/Niveau1/SM_GrandePorte.fbx.meta` | `M_Pierre`, `M_FerRouille`, `M_GrandePorte_Bois`, `M_GrandePorte_Serrure` |
| `Assets/Art/Models/Niveau1/SM_Pont_Egout_14m.fbx.meta` | `M_Pierre`, `M_Dalles`, `M_Egout` |
| `Assets/Art/Models/Niveau1/SM_Pont_Egout_4m.fbx.meta` | `M_Pierre`, `M_Dalles`, `M_Egout` |
| `Assets/Art/Models/Niveau1/SM_Porte_Piece.fbx.meta` | `M_Pierre`, `M_FerRouille` |
| `Assets/Art/Models/Niveau1/SM_Poutre.fbx.meta` | `M_Poutre_Bois`, `M_FerRouille` |

## Comparaison avec les rendus Blender

Planches `docs/art/rendus/Rendu/Comparaison_<modèle>_Unity-Blender.jpg` (à gauche Unity, shader toon, lumière
blanche de présentation et contours ; à droite le rendu Blender `_34` de `docs/art/rendus/`).

**Calage : aucun écart.** Joints des blocs de l'arc et de la colonne, fût de la colonne, dalles du tablier, voussoirs
des ponts, ferrures et anneaux de la grande porte, cerclages de la poutre, chambres et rune du barillet tombent aux
mêmes endroits que dans Blender.

**Écarts de rendu (voulus ou à trancher) :**
1. **Dessous des arches des ponts** (`M_Egout`) : lisible dans Blender (brique brune et mousse), **presque noir** dans
   Unity. Ce n'est pas un problème de calage : la face est à l'ombre, et le toon ne lui laisse que la teinte d'ombre et
   l'ambiance. En jeu, une bougie sous le pont ou la lanterne du joueur l'éclaire (`Materiaux_intermediaire_pont.jpg`).
2. **Laiton du barillet** plus brun et plus sombre que dans Blender (pas de reflet, deux bandes nettes au lieu d'un
   dégradé). À voir en vue FPS sous la lumière du jeu.
3. **Ferrures de la grande porte** plus sombres et plus plates (même raison).
4. Le contour encré du toon remplace les arêtes adoucies du rendu Blender ; la silhouette est plus lisible.

## Matériaux qui pourraient être partagés

- `M_GrandePorte_Serrure` utilise la **même texture** que `M_FerRouille` : il pourrait être remplacé par `M_FerRouille`.
  Je le garde séparé, car la serrure est un **état de jeu** (porte rouge si verrouillée, orange si payable, D-048) :
  c'est le seul morceau de la porte qui pourrait recevoir une lueur.
- `M_Pierre` sert déjà à 6 modèles et `M_FerRouille` à 4 : c'est le bon niveau de partage. Les murs, sols et voûtes des
  niveaux devraient réutiliser `M_Pierre`, `M_Dalles` et `M_Voute_Briques` plutôt que d'en créer d'autres.
- `M_Rendu_Mannequin` et le gris du traqueur provisoire pourraient devenir un seul matériau « greybox toon » pour les
  formes provisoires du jeu.

## Matériaux qui manquent

- **Voûtes et plafonds** : la texture `texture_voute_briques_v01_tuile` existe, mais **aucun modèle ne l'utilise** et sa
  taille de tuile n'est pas définie dans `it_common.py` (`TILES`). `M_Voute_Briques` est proposé, calé à 2 × 2 m dans la
  scène de test. **La voûte de l'arche du pont** utilise `M_Egout` (brique d'égout), cohérent avec le canal ; si le DA
  veut une voûte en briques de voûte, il faut un 4e sous-matériau dans le pont.
- **Eau du canal** : `texture_eau_egout_v01_tuile` existe, aucun modèle ne la porte. `M_Eau_Egout` est proposé (plan
  d'eau dans `Test_Rendu`). Une eau statique avec ombrage toon ressemble à un sol ; un vrai matériau d'eau (défilement,
  sans ombres, peut-être transparent) sera à faire.
- **Masques d'émission** (`T_<Asset>_Emission`) : aucun pour l'instant. Il en faudra pour les objets interactifs qui
  brillent (props attrapables, coffre, marchand) et, si le DA le veut, pour la serrure de la grande porte.
- **Mains et orbe** (`SK_Mains`) : pas encore livrés ; leur contour devra venir du matériau (coque inversée), la passe
  plein écran ne les voit pas.

## Textures à refaire ou à compléter

| Texture | Problème | Proposition |
|---|---|---|
| `T_GrandePorte_Bois_BaseColor` | **≈ 50 à 80 px/m** pour des battants de 10 × 12.7 m, loin des 256–512 px/m visés : flou de près (la porte de sortie se voit de près) | 2048 × 2048, ou une tuile de planches répétée + ferrures en trim |
| `T_Poutre_Bois_BaseColor` | ≈ 170 px/m sur la longueur (6 m sur 1024 px) | 2048 × 512, ou tuile de bois répétable |
| `texture_fer_rouille_v01_tuile` | ~~1024 px/m, 4 fois plus dense que la pierre~~ | **Fait (2026-11-15)** : image ramenée à 256 × 256 px pour la tuile de 1 m, soit **256 px/m**, la densité de la pierre ; UV des modèles inchangées. Avant / après : `rendus/Textures/Retouche_Fer.jpg` |
| `texture_mur_egout_v01_tuile` | 512 px/m, deux fois plus dense que la pierre : écart visible là où égout et pierre se touchent (culées du pont) | tuile de 4 × 4 m, ou garder (contraste voulu entre égout et pierre ?) |
| `texture_mur_pierre_v01_tuile` | ~~la grande coulure blanche revient tous les 4 m~~ | **Fait (2026-11-15)** : coulure effacée (≈ 11 800 px, assise du bas, à cheval sur les bords), par diffusion périodique et grain d'une pierre propre ; joints et contours gardés. Avant / après : `rendus/Textures/Retouche_Pierre.jpg` |
| `texture_sol_dalles_v01_tuile` | raccord de bord légèrement visible (changement de ton entre dalles au raccord) | retouche du bord |
| `texture_voute_briques_v01_tuile`, `texture_eau_egout_v01_tuile` | ~~taille de tuile non définie~~ | **Fixé (2026-11-15) dans `TILES`** : voûte **3 × 3 m** (16 assises : briques de 0.44 × 0.19 m, 341 px/m, lisibles à pleine vitesse) ; eau **4 × 4 m** (256 px/m) |
| `texture_eau_egout_v01_tuile` | ~~damier~~ : la tuile se répétait elle-même tous les 512 px en hauteur | **Fait (2026-11-15)** : moitié basse = moitié haute décalée de 352 px, raccords recousus par une coupe d'erreur minimale (traits nets) ; plus de répétition interne (écart 51 au lieu de 4.8), écume en quinconce irrégulier ; avec la tuile de 4 m, une tache d'écume tous les 2 à 4 m en diagonale. Avant / après : `rendus/Textures/Retouche_Eau.jpg` |

Coutures : pas de couture visible sur les tuiles de pierre, d'égout, de fer et de voûte (mosaïques 2 × 2 contrôlées).

Les retouches sont faites par `Assets/Art/Source~/Niveau1/scripts/textures_retouches.py` (Pillow et numpy), sur les
fichiers en place (mêmes noms, mêmes GUID : les matériaux suivent sans réglage). Les originaux sont dans l'historique
git (`git show <commit>:<fichier> | git lfs smudge`).

**UV des modèles (défaut corrigé le 2026-11-15).** `it_common.Builder.finish` choisissait la projection monde d'après
la normale de chaque face **avant** de l'avoir calculée : toutes les faces sans UV explicites recevaient la projection
du dessus, et les faces verticales des boîtes, prismes, tubes et révolutions avaient des UV dégénérées (texture étirée
en traînées). Corrigé et réexporté : kit, échafaudage, groupes A à C (props, arène, lanterne, orbe, bras). **Restent à
réexporter**, déjà sur `main` (contenu des FBX seulement, `.meta` inchangés) : `SM_GrandePorte` (1 076 faces : ferrures,
serrure, flancs du bois, un peu de pierre), `SM_Poutre` (288, ferrures), `SM_Porte_Piece` (48, pierre),
`SM_Colonne` (6, anneau). Le barillet (72 faces de son atlas calé à la main) n'est pas concerné.

## Captures du réglage retenu

`docs/art/rendus/Rendu/Materiaux_intermediaire_couloir.jpg`, `_pont.jpg`, `_traqueur.jpg` : `Test_Rendu` avec les
matériaux définitifs, réglage intermédiaire, contours clairs dans le noir. Planches de comparaison avec Blender :
`Comparaison_*_Unity-Blender.jpg`. Lanterne : voir `RENDU.md` § 3 bis.

## Questions pour le DA

1. **Couleur d'émission des chambres chargées du barillet.** Pas le violet de la télékinésie ; mais l'**orange est
   réservé à la répulsion** (`ART-HANDOFF.md` § 7), alors que le code actuel allume les chambres en orangé (HDR).
   Propositions : or chaud (1.6, 1.15, 0.45), blanc chaud, ou rouge braise (rouge réservé au refus). À trancher.
2. **Rune du barillet :** sans émission (`M_Barillet_Rune`, remappée par défaut) ou avec (`M_Barillet_Rune_Emission`) ?
   Si elle brille, quelle couleur, et doit-elle s'éteindre quand le barillet est inactif ?
3. **Serrure de la grande porte :** une lueur (rouge verrouillée, orange payable) sur la serrure seulement, avec un
   masque d'émission ? Ce serait la seule émission d'un objet de décor (il est interactif).
4. **Voûte de l'arche du pont :** brique d'égout (actuel) ou brique de voûte ?
5. **Eau :** texture statique (actuelle) ou matériau d'eau animé ?
6. **Densités :** accepter le fer à 1024 px/m et l'égout à 512 px/m, ou tout ramener vers 256 px/m ?
7. **Bandes :** 3 bandes par défaut ; la bande du milieu se voit peu sous la lune faible. Passer à 2 ?
8. **Lanterne** (`RENDU.md` § 3 bis) : la garder, avec quelle intensité ?

## Pour la session principale

1. **Contours et brouillard dans les vraies scènes** (d'après `docs/art/RENDU.md` § 5, qui fait foi) :
   - Full Screen Pass Renderer Feature dans `PC_Renderer` : Before Rendering Transparents, Depth + Normal, Fetch Color
     Buffer, matériau `Assets/Art/Materials/Rendu/M_Contours.mat` (mode clair dans le noir, fondu 40 → 100 m) ;
   - `Prepass Layer Mask` sans les couches 8 à 11 (corrige les contours fantômes des mains, et la SSAO) ;
   - brouillard linéaire du réglage intermédiaire : couleur (0.012, 0.014, 0.024), 10 → 85 m, et fond des caméras de
     la même couleur ;
   - ambiance (0.055, 0.058, 0.080) en mode Flat ; lune 0.25 ; torches et bougies vers 1.8 d'intensité, 9 à 13 m de
     portée (le toon dessine l'îlot sur la portée).
2. **Après la fusion, les FBX remappés apportent leurs matériaux** : les prefabs du kit (`Assets/_Project/Prefabs/Kit/Kit_*`, voir `docs/art/INTEGRATION-PROPS.md`) qui
   contiennent ces FBX passent au toon sans rien faire, **sauf** s'ils ont des remplacements de matériaux sur leurs
   renderers (à vérifier : un remplacement l'emporte sur le remappage).
3. **Barillet : `BarrelView` doit écrire `_EmissionColor` au lieu de `_BaseColor`** pour les chambres. Aujourd'hui il
   remplace la couleur de base par la couleur HDR (orangé, gris, noir) : avec le toon, une chambre chargée serait
   éclairée comme une surface, sans briller. Garder `_BaseColor` = #1A120C, écrire la lueur (et l'état grisé) dans
   `_EmissionColor`.
4. Les matériaux greybox du jeu (`Assets/_Project/Art/Greybox/`) peuvent passer sur `ItHunts/Toon` (mêmes noms de
   propriétés que URP Lit) pour que tout le décor ait le même rendu en attendant les vrais modèles.
5. Lanterne : si le DA la confirme, l'ajouter au prefab joueur (`RENDU.md` § 5, point 6).
6. **Orbe (`SM_Orbe`) dans FpsView :** la greybox est une sphère de Ø 0.08 m ; le modèle fait **0.18 m** (anneaux)
   et **0.295 × 0.228 × 0.182 m** avec les tablettes (couronne à 0.13 m, décision de Sergio). Régler l'échelle à
   l'écran dans FpsView (pivot au centre, sur `Orb_Socket`). Couleur : celle du personnage, en permanence ;
   animations et jauge des tablettes : section « Orbe : couleur du personnage, animations par action, jauge des
   tablettes » (rien n'est codé ici). À piloter par bloc de propriétés : `_EmissionColor` de `M_Orbe_Energie` et du
   glyphe de chaque tablette (index de matériau 1 sur `SM_Orbe_Tablette_0X`). Repère `FX_Orbe` au centre.
7. **Lanterne de feu (`SM_LanterneFeu`, artefact du renard) :** pivot au centre de l'anneau de portage (elle pend en
   dessous). Repères à brancher : `FX_Flamme` (flamme de la mèche, lumière ponctuelle) et `FX_Jet` (départ du jet de
   feu ; son +Z local est l'avant de la lanterne ; dégagé sur 6 m, aucun élément de la lanterne devant lui).
   `_EmissionColor` de `M_LanterneFeu_Verre` à allumer en jeu ; passe transparente des mains à ajouter (voir le verre
   translucide plus haut).
8. **`ART-HANDOFF.md` § 0 quater et § 5 bis** : remplacer la couleur de l'orbe par action (D-024) par la couleur du
   personnage, et la sphère de 0.08 m par les mesures de `SM_Orbe`.
