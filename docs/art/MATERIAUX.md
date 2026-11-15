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
| `texture_fer_rouille_v01_tuile` | **1024 px/m**, 4 fois plus dense que la pierre (256 px/m) : la rouille paraît plus fine que tout le reste | tuile de 2 × 2 m (changer `TILES` dans `it_common.py` puis réexporter), ou texture refaite à plus gros grain |
| `texture_mur_egout_v01_tuile` | 512 px/m, deux fois plus dense que la pierre : écart visible là où égout et pierre se touchent (culées du pont) | tuile de 4 × 4 m, ou garder (contraste voulu entre égout et pierre ?) |
| `texture_mur_pierre_v01_tuile` | répétition visible sur les longs murs : la grande tache blanche revient tous les 4 m (couloir de 160 m dans `Test_Rendu`) | 1 ou 2 variantes de tuile, ou taches en décalques |
| `texture_sol_dalles_v01_tuile` | raccord de bord légèrement visible (changement de ton entre dalles au raccord) | retouche du bord |
| `texture_voute_briques_v01_tuile`, `texture_eau_egout_v01_tuile` | taille de tuile non définie | la fixer dans `TILES` (2 × 2 m proposé) |
| `texture_eau_egout_v01_tuile` | motif très contrasté : sous une bougie, la répétition tous les 2 m fait un damier | tuile plus grande et moins contrastée, ou eau en shader |

Coutures : pas de couture visible sur les tuiles de pierre, d'égout, de fer et de voûte (mosaïques 2 × 2 contrôlées).

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
2. **Après la fusion, les FBX remappés apportent leurs matériaux** : les prefabs de kit (`Assets/Prefabs/Kit/`) qui
   contiennent ces FBX passent au toon sans rien faire, **sauf** s'ils ont des remplacements de matériaux sur leurs
   renderers (à vérifier : un remplacement l'emporte sur le remappage).
3. **Barillet : `BarrelView` doit écrire `_EmissionColor` au lieu de `_BaseColor`** pour les chambres. Aujourd'hui il
   remplace la couleur de base par la couleur HDR (orangé, gris, noir) : avec le toon, une chambre chargée serait
   éclairée comme une surface, sans briller. Garder `_BaseColor` = #1A120C, écrire la lueur (et l'état grisé) dans
   `_EmissionColor`.
4. Les matériaux greybox du jeu (`Assets/_Project/Art/Greybox/`) peuvent passer sur `ItHunts/Toon` (mêmes noms de
   propriétés que URP Lit) pour que tout le décor ait le même rendu en attendant les vrais modèles.
5. Lanterne : si le DA la confirme, l'ajouter au prefab joueur (`RENDU.md` § 5, point 6).
