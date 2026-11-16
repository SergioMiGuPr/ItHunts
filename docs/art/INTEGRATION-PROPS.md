# Intégration des modèles dans les prefabs du jeu

Fiche pour la session principale (`main`) : comment remplacer les greybox par les modèles livrés sur `art` (PR #3), sans
rien mesurer. Toutes les coordonnées sont **dans le repère Unity** (+Y en haut, +Z vers l'avant du modèle), en mètres.
Mesures relevées le 2026-11-15 sur les FBX réimportés (Blender et Unity) et sur les prefabs greybox de `origin/main`
(`Assets/_Project/Prefabs/Props/`, `Kit/`, `BossArenaBuilder.cs`), en lecture seule.

**Règles communes**
- Chaque FBX arrive à rotation nulle, échelle 1, matériaux déjà remappés vers les `.mat` toon (`Assets/Art/Materials/`).
- Placer le FBX **en enfant** de la racine du prefab, à la position indiquée dans « décalage » (colonne vide = 0) ; ne
  jamais l'échelonner, sauf mention.
- Garder la boîte de collision de la racine (la taille conseillée est donnée ; les collisions plus fines attendront que
  le jeu dise où une boîte ne suffit pas).
- Props : ajouter l'étiquette « DA » au prefab (le constructeur ne le recrée plus). Pièces du kit : pas de collider dans
  le modèle (boîte fusionnée posée par le code).
- Objets mobiles : chaque pièce mobile est un **enfant séparé du FBX**, avec son pivot sur son axe, rotation nulle au
  repos. Repères `FX_*` : objets vides, leur +Z local donne la direction (avant du modèle).
- Les matériaux à émission (« prête mais éteinte ») ont le mot-clé `_EMISSION` actif et une couleur noire : le code
  écrit `_EmissionColor` (bloc de propriétés ou matériau), sans activer de mot-clé.

## 1. Pièces du kit (carte aléatoire, `Assets/_Project/Prefabs/Kit/`)

Ordre de priorité d'`ART-HANDOFF.md` (« Kit de construction de la carte aléatoire »).

| Priorité | Prefab | Modèle | État | Pivot du modèle | Remarques |
|---|---|---|---|---|---|
| 1 | `Kit_Mur4x4` | `SM_Kit_Mur4x4` | **livré (art)** | (au pied, au milieu ; x −2..2, y 0..4, z −0.5..0.5) | ≈ 60 000 pièces sur une carte : modèle très léger (≤ 200 triangles), texture de pierre partagée |
| 2 | `Kit_Sol4` | `SM_Kit_Sol4` | **livré (art)** | (milieu du dessus ; y −1..0) | dalles, même tuile que `M_Dalles` |
| 3 | `Kit_Escalier` | `SM_Kit_Escalier` | **livré (art)** | (milieu du bas de la contremarche ; x −2..2, y 0..0.25, z 0..1) | **répétée le long de la pente** (une marche par 0.25 m de hauteur, chaque marche allongée vers le bas jusqu'au sol), pas étirée en profondeur ; collision : la pente lisse du code |
| 4 | `Kit_Arc` | `SM_Arc_Couloir` | intégré sur `main` | au sol, au milieu entre les deux pieds | 14 × 11 × 1.6 m |
| 5 | `Kit_Pilier` | `SM_Colonne` (+ `SM_Colonne_Anneau`) | intégré sur `main` | au sol, au centre | 1.4 × 8 × 1.4 m ; anneau à 3 m sur la face avant |
| 6 | `Kit_Pont` | `SM_Kit_Pont` | **livré (art)** | (milieu du dessus) | passerelle de 4 × 3 m, tablier de 1 m, étirée en largeur |
| 7 | `Kit_Poutre` | `SM_Poutre` (+ `SM_Poutre_Crochet`) | intégré sur `main` | centre du dessous du bois | 6 × 0.5 × 0.5 m |
| 8 | `Kit_PortePiece` | `SM_Porte_Piece` | intégré sur `main` | au sol, au centre de l'ouverture | 4.4 × 5 × 1 m, ouverture 3 × 3 m |
| 9 | `Kit_GrandePorte` | `SM_GrandePorte` | intégré sur `main` | cadre au sol, au centre ; battants sur l'axe des gonds | cadre 13 × 14 × 2 m |
| 10 | `Kit_PontEgout4` / `14` | `SM_Pont_Egout_4m` / `_14m` | intégré sur `main` | centre du tablier, à la cote 0 | |
| 11 | `Kit_Mur2x4`, `Kit_Mur4x3` | `SM_Kit_Mur2x4`, `SM_Kit_Mur4x3` | **livré (art)** ; prefabs `Kit_*` à créer sur `main` | comme `Kit_Mur4x4` | demi-pièces (2 × 4 et 4 × 3 m) |

**Lot du 2026-11-16 : UV en coordonnées monde branchées.** Les 6 pièces `SM_Kit_*` utilisent `M_Pierre_Monde` (murs,
flancs) et `M_Dalles_Monde` (dessus du sol, giron de l'escalier, tablier du pont) : la texture est projetée en
coordonnées monde par le shader (`_WORLD_UV`, RENDU.md § 5 ter), donc les assises et les dalles raccordent d'une pièce
à l'autre, même quand le code étire, coupe ou empile les pièces (captures `rendus/Lot/Kit_*.jpg`). **Read/Write est
activé** à l'import (fusion des maillages par le jeu possible), comme pour tous les modèles d'architecture et de props
du niveau 1 (pas pour la vue FPS ni les ennemis).

**Pièces livrées le 2026-11-15** (`Assets/Art/Models/Niveau1/SM_Kit_*.fbx`, script `sm_kit.py`) : cotes et pivots
exacts des greybox (vérifiés au réimport), 12 triangles chacune, à remplacer **sans décalage** dans les prefabs `Kit_*`
(même pivot que la racine du prefab ; supprimer l'enfant « (greybox) »). Volumes nets à faces planes, normales vers
l'extérieur, arêtes vives : prêts pour les UV en coordonnées monde (RENDU.md § 5 ter) ; UV de repli fournies (projection
monde, mêmes tuiles). Matériaux : murs `M_Pierre` ; sol, giron de l'escalier et dessus du pont `M_Dalles`, le reste
`M_Pierre`. Aucun relief qui doive se raccorder d'une pièce à l'autre (chanfreins, socles, joints modelés : une
couture tous les 4 m sur 60 000 pièces, et faux dès que le code étire ou coupe une pièce) : le dessin des pierres
vient de la texture projetée en monde. **Escalier (décision du 2026-11-15)** : construire l'escalier en **répétant `SM_Kit_Escalier` le
long de la pente, une marche par hauteur de marche (0.25 m)**, et non en étirant une seule marche ; chaque marche peut
être allongée vers le bas jusqu'au sol (marche pleine) ; la collision reste la pente lisse du code. **Pont** : dessous plat (la hauteur libre de 10 m sous les
ponts est gardée). Le caractère gothique vient des pièces déjà livrées (arcs, colonnes, portes, poutres) ; variantes
ponctuelles **gardées pour plus tard** (décision du 2026-11-15), à mélanger aux murs pleins : `Kit_Mur4x4_Contrefort` (contrefort à
glacis), `Kit_Mur4x4_Niche` (niche en arc brisé), `Kit_Mur4x4_Meurtriere`.

**Point d'attention pour les murs et les sols du kit :** la texture de pierre (`M_Pierre`) a une tuile de **4 × 3 m**.
Sur un mur de 4 m de haut, les assises peintes ne se raccordent pas d'une pièce à l'autre en hauteur (4 n'est pas un
multiple de 3). À trancher avant de modéliser `Kit_Mur4x4` : refaire la tuile de pierre en 4 × 4 m (ou 4 × 2 m), ou
calculer les UV en coordonnées monde dans le shader pour ces pièces (triplanaire simple, sans couture entre pièces,
et compatible avec le raccourcissement des pièces du bord). La deuxième option règle aussi les pièces étirées
(`Kit_Escalier`, `Kit_Pont`, pièces raccourcies).

## 2. Props du niveau (`Assets/_Project/Prefabs/Props/`)

| Prefab | Modèle (FBX `Assets/Art/Models/Niveau1/`) | Décalage du modèle | Boîte de collision conseillée (centre ; taille) | Repères FX | Objets mobiles (axe) | Matériaux | Attrapable |
|---|---|---|---|---|---|---|---|
| `Prop_Torche` | `SM_Torche` (pivot : centre du dos de la plaque, sur le mur) | **(0, 0.545, 0)** : le haut de la tête à 0.96 m comme la greybox | aucune (décor sans collision) | `FX_Flamme` en (0, 0.443, 0.354) dans le FBX, soit **(0, 0.988, 0.354)** dans le prefab : y mettre la lumière (aujourd'hui en (0, 1.1, 0.5), 15 cm trop loin du mur) et la flamme | — | **`M_Torche`** (un seul matériau depuis le 2026-11-16 : atlas `T_Torche_BaseColor` de 512 px, 1 draw par torche au lieu de 4) | non |
| `Prop_TorcheEteinte` | `SM_Torche` (même modèle) | (0, 0.545, 0) | aucune | `FX_Flamme` : flamme sans lumière | — | idem | non |
| `Prop_Cage` | `SM_Cage` + enfant `SM_Cage_Os` | — | (0, 1.5, 0) ; 2.2 × 3 × 2.2 (inchangée) | — | — (porte fermée, modélisée avec la cage) | `M_FerRouille` ; os : `M_Os` | non |
| `Prop_Grille` | `SM_Grille` | — | (0, 1.5, 0) ; 3 × 3 × 0.2 (inchangée) | — | — (porte fermée, modélisée avec la grille) | `M_FerRouille` | non |
| `Prop_Tapisserie` | `SM_Tapisserie` + enfant `SM_Tapisserie_Tringle` (pivot : centre de la tringle, en haut) | **(0, 4.0, 0)** : la greybox a son pivot au sol | aucune | — | — | `M_Tapisserie` (découpe alpha, double face), `M_Bois_Brut` | non |
| `Prop_Chaine` | `SM_Chaine` (pivot : sommet de l'anneau) | **(0, 3.95, 0)** : la greybox a son pivot au sol | aucune | — | — | `M_FerRouille` | non |
| `Prop_Tuyau` | `SM_Tuyau` | — | **(0, 0.457, 0.04) ; 0.91 × 0.91 × 6.08** (rebord et colliers compris) | — | — | `M_FerRouille`, `M_Egout`, `M_Fond_Noir` | non |
| `Prop_Echafaudage` | `SM_Echafaudage` | — | **variante retenue** : deux plateaux et six poteaux, intérieur libre (tableau ci-dessous) | — | — | `M_Bois_Brut`, `M_Corde` | non |
| `Prop_ColonneBrisee` | `SM_ColonneBrisee` | — | (0, 1.3, 0) ; 1.4 × 2.6 × 1.4 (inchangée) | — | — | `M_Pierre` | non |
| `Prop_Colonne` | `SM_Colonne` (+ anneau) | — | (0, 4, 0) ; 1.4 × 8 × 1.4 (inchangée) | — | — | `M_Pierre`, `M_FerRouille` | non |
| `Prop_Poutre` | `SM_Poutre` (+ crochet) | — | (0, 0.25, 0) ; 6 × 0.5 × 0.5 (inchangée) | — | — | `M_Poutre_Bois`, `M_FerRouille` | non |

**Échafaudage : variante retenue (décision du 2026-11-15) ; modèle conforme livré le même jour** (croisillons seulement à l'étage, jambes de force courtes sous le plateau du bas : le rez-de-chaussée est libre, 1 032 triangles). On doit pouvoir y monter, **les goules aussi**. Remplacer la
boîte pleine de `Prop_Echafaudage` par des collisions qui laissent l'intérieur libre (repère du prefab, modèle sans
décalage ; le modèle est recentré de 2.7 cm en Z à cause de l'échelle, d'où les z ci-dessous) :

| Pièce | Centre | Taille | Remarque |
|---|---|---|---|
| Plateau du bas | (0, 2.965, −0.027) | 4 × 0.07 × 2 | dessus à 3 m |
| Plateau du haut | (0, 5.965, −0.027) | 4 × 0.07 × 2 | dessus à 6 m |
| 6 poteaux | x = −1.925, 0, 1.925 ; z = −0.952 et 0.898 ; y = 3 | 0.15 × 6 × 0.15 | du sol à 6 m |
| Échelle (option) | (−1.0, 3, 1.013) | 0.57 × 6 × 0.05 | sur l'avant (+Z), à droite vu de face (x Unity négatif) |

- Accès : l'échelle est visuelle ; le joueur monte par le saut, le wall jump ou la traction, les goules en grimpant
  (`ART-HANDOFF` : elles grimpent les flancs des plateformes). Pour que les goules grimpent, les flancs des plateaux
  doivent être des surfaces que leur logique accepte (même couche que les plateformes) ; l'échelle peut servir de
  « mur » grimpable si on lui donne une boîte.
- NavMesh : les deux plateaux sont des surfaces à cuire (4 × 2 m chacun, au-dessus des poteaux) ; le dessous reste
  traversable (2.9 m de passage libre sous le plateau du bas, 1.85 m entre les poteaux sur la longueur).
- Les croisillons en X et les longerons restent sans collision (décor), pour ne pas bloquer le passage sous les
  plateaux ni accrocher le contrôleur.

Écarts connus : chaîne de 3.89 m (au lieu de 4 : étirer de 3 % en Y si besoin) ; tuyau de 0.91 m de haut (rebord) ;
colonne brisée sans les débris au sol de la greybox (ses morceaux restent sur les gradins, dans l'emprise de 1.4 m) ;
cadenas de la cage et de la grille qui dépassent de 2 à 3 cm à l'avant.

## 3. Arène du boss (`BossArenaBuilder.cs` : boîtes posées par leur centre)

| Élément | Modèle | Placement | Objets mobiles (axe) | Collision | Lueur / FX |
|---|---|---|---|---|---|
| Herse (à `hc` = centre + (−18, 0, 0)) | `SM_Herse` + enfant `SM_Herse_Grille` | **rotation (0, 90, 0)** : le passage du modèle est selon Z, celui de l'arène selon X ; pivot au sol, au centre du passage, en `hc`. Les piliers tombent à z = ±6.5 comme les boîtes actuelles (3 × 28 × 3) | `SM_Herse_Grille` : translation **Y** ; pivot en bas au centre (pointes au sol). Position haute : **y = 16** (la boîte actuelle est centrée à 22 pour 12 m de haut) ; elle descend de 16 m jusqu'à 0 | garder les boîtes des piliers (3 × 28 × 3) et de la grille (1.2 × 12 × 10, plus épaisse que le modèle de 0.25 m : voulu pour le jeu) | — |
| Leviers (6, pied en `feet`) | `SM_Levier` + enfant `SM_Levier_Manche` | pivot au sol, en `feet` ; socle (1 × 1.2 × 1 m) et plaque (1.2 × 0.8 × 1.2, dessus à 2 m) aux mêmes hauteurs que les boîtes actuelles | `SM_Levier_Manche` : rotation **X** autour de son pivot (axe à 2.108 m) ; repos à **+35°** (vers l'avant, déjà dans le modèle) ; activé : **−70°** de plus, soit −35° | boîte de la plaque (1.2 × 0.8 × 1.2, centre à 1.6 m) | `M_Levier_Lueur` (cristal et anneau de rune) : `_EmissionColor` orange quand le levier est prêt, vert quand il est activé (remplace l'échange de matériau `Trap_Lever_Off` / `_On` de `BossLever`) ; la lumière à 2.8 m reste |
| Traversée de la fosse (5 plateformes, `CrumblePlatform`) | `SM_PlateformeTombe` | pivot au centre du **dessous** de la dalle : dessus du modèle = dessus de la boîte actuelle → modèle à **(0, −0.4, 0)** sous le dessus, ou à (0, −0.15, 0) du centre de la boîte de 0.5 m | la dalle entière (le code la fait tomber) ; les 4 morceaux font partie du même maillage | boîte actuelle 2.4 × 0.5 × 2.4 (le modèle fait 0.4 m d'épaisseur : passer la boîte à 0.4, centre −0.2, ou accepter 10 cm sous le modèle) | — |
| Treuil (point `winch` = centre + (0, 6, h − 1.2), galerie nord à 6 m) | `SM_Treuil` + enfant `SM_Treuil_Manivelle` | **rotation (0, 180, 0)** (le modèle regarde vers +Z, le mur nord est en +Z) ; pivot au sol de la galerie, contre le mur : (0, 6, h). Le haut du tambour (départ de la corde) est alors en **(0, 7.9, h − 0.82)** : y déplacer `winch` | `SM_Treuil_Manivelle` : rotation **X** sur l'axe du tambour (1.3 m au-dessus du pivot, 0.82 m du mur) ; le tambour fait partie de `SM_Treuil` (pour le faire tourner aussi, demander une pièce séparée) | une boîte de 2.5 × 2.5 × 1.5 m contre le mur suffit | la corde qui brille (`Lustre : corde`) part du haut du tambour ; le brin de chaîne du modèle (≈ 1 m) peut rester |
| Lustre géant | — | **à produire** (12 × 1.5 × 12 m, accroché à 24 m) | | | |

## 4. Vue FPS (`Assets/Art/Models/FPS/`)

| Objet | Modèle | Placement | Objets mobiles | Matériaux pilotés par le code |
|---|---|---|---|---|
| Orbe (main droite, sur `Orb_Socket`) | `SM_Orbe` (racine vide) | pivot au centre de l'orbe sur `Orb_Socket` ; **échelle à régler dans FpsView** : la greybox fait Ø 0.08 m, le modèle 0.18 m (anneaux) et 0.295 × 0.228 × 0.182 m avec les tablettes | `SM_Orbe_Anneau_1` (axe **X**), `_2` (**Y**), `_3` (**Z**), modélisés à leur pose de repos ; `SM_Orbe_Tablette_01` à `_06` (positions de la couronne) ; repère `FX_Orbe` au centre | `M_Orbe_Energie` (`_EmissionColor`, couleur du personnage) ; glyphe de chaque tablette : **index de matériau 1** de `SM_Orbe_Tablette_0X` (`M_Orbe_Tablette_0X`) ; jauge et animations : `MATERIAUX.md`, planche `Orbe_Poses_*.jpg` |
| Lanterne de feu (artefact du renard, main gauche) | `SM_LanterneFeu` | pivot au centre de l'anneau de portage (la lanterne pend dessous) | — | `M_LanterneFeu_Verre` : translucide (file 3000) et `_EmissionColor` ; **`M_LanterneFeu_Flamme`** (enfant `SM_LanterneFeu_Flamme`, flamme en goutte au-dessus de la mèche, depuis le 2026-11-16) : `_EmissionColor` = couleur du personnage (par défaut, orange du renard) ; **passe transparente des mains à ajouter** (la passe « ItHunts Mains : mains » ne dessine que l'opaque) ; repères `FX_Flamme` (mèche) et `FX_Jet` (départ du jet, +Z = avant, dégagé sur 6 m) |
| Bras des 4 personnages (remplacent les mains greybox) | `SK_BrasFPS_<Perso>_D` / `_G` (armature `BrasFPS`, 17 os, mêmes noms partout) | placer le FBX pour que **`Socket_Poignet` coïncide avec la main greybox** (position et rotation de repos de FpsView, § 5 bis d'ART-HANDOFF) : le pivot est au coude, `Socket_Poignet` est en (0, 0, 0.27) dans le FBX, axes de l'ancrage (+Z doigts, +Y dos de la main) ; aucun recalage d'axe | os des doigts : remplacent l'angle unique `rightFingers` / `leftFingers` (rotation X) par les clips `BrasFPS|Pose_*` (mêmes clips pour les 4 personnages ; un jeu de clips par côté, le gauche est le miroir) ; `Pose_Attraper` a 3 clés (repos, ouverte, refermée), les 9 poses fixes 2 clés identiques (images 1 et 2) pour que chaque clip garde ses 180 courbes à l'import ; les déplacements de la main entière restent dans FirstPersonView | `M_BrasFPS_<Perso>_*` (aplats) ; contour par coque inversée (RENDU.md § 5 bis) ; repères fixes enfants de `BrasFPS` : `Socket_Orbe` (0, 0.06, 0.045) depuis le poignet (droit), `Socket_Artefact` (0, 0.055, 0.05) et `Socket_Artefact_Anneau` (0, −0.05, 0.105) pour l'anneau de la lanterne dans `Pose_PriseAnneau` (gauche, à ajuster en jeu) ; planches `BrasFPS_*.jpg` |
| Barillet (caméléon, main gauche) | `SM_Barillet` | intégré sur `main` (branche `barillet-modele`) | | |

## 4 bis. Caisse attrapable : `SM_Caisse` (modèle généré par IA avec Tripo, nettoyé le 2026-11-15)

| Prefab | Modèle | Décalage du modèle | Boîte de collision | Matériau | Attrapable |
|---|---|---|---|---|---|
| `Prop_Caisse_Attrapable` (cube unité à l'échelle 0.8 : **pivot au centre du cube**) | `SM_Caisse` (cube exact de 0.8 m, **pivot au sol, au centre**, +Z vers l'avant ; **rune sur les quatre faces latérales**, même sens de lecture, planche `Caisse_rune_4_faces.jpg`) | **(0, −0.4, 0)** dans un prefab dont la racine n'est pas mise à l'échelle ; si la racine garde son échelle 0.8, mettre le modèle à l'échelle 1.25 et à (0, −0.5, 0) | celle de la greybox : 0.8 m (centre (0, 0, 0)) | `M_Caisse` (créé le 2026-11-16 ; capture `rendus/Lot/Caisse_eteinte_et_lueur.jpg`) | **oui** (Rigidbody, `Grabbable`, 15 kg, lueur pilotée par le jeu via le masque de la rune) |

- **Autres caisses du jeu** (cubes de 0.5, 0.7, 0.9, 1.1 m posés par le code selon la masse : 5, 15, 25, 40 kg) : le même
  modèle mis à l'échelle convient (× 0.625, 0.875, 1.125, 1.375), puisque toutes sont des cubes.
- 1 499 triangles, une texture de couleur de 512 px et un masque d'émission de 512 px.

## 4 ter. Ennemis (`Assets/Art/Models/Ennemis/`, lot du 2026-11-16, modèles générés par IA avec Tripo)

Matériaux dans `Assets/Art/Materials/Ennemis/` (shader toon, masque d'émission). Tous regardent vers +Z, pivot au
sol au centre, 1 unité = 1 m. Captures : `rendus/Lot/Goule_*.jpg`, `rendus/Lot/Traqueur_*.jpg`.

**Goule (`SM_Goule`, statique, sans squelette).** 22 000 triangles, 0.67 × 1.1 × 0.70 m (capsule de jeu Ø 0.9 ×
1.1 m), quadrupède, entrave gardée, peau gris-mauve.
**LOD (jusqu'à 80 goules) :** le FBX contient `SM_Goule_LOD0` (22 000), `SM_Goule_LOD1` (11 000) et `SM_Goule_LOD2` (5 000),
mêmes UV, même `M_Goule` et même masque d'émission ; **Unity crée le `LODGroup` à la racine de `SM_Goule`** (seuils par défaut
0.25 / 0.125 / 0.01). Seuils conseillés dans le prefab (hauteur d'écran, champ de vision de 60°) : **LOD1 sous 0.20 (≈ 5 m),
LOD2 sous 0.08 (≈ 12 m), disparition sous 0.01** (≈ 95 m, bien au-delà du brouillard). Le signal du bond doit écrire
`_EmissionColor` sur les **trois** renderers (bloc de propriétés sur chacun, ou sur le matériau partagé).
Planche : `docs/art/rendus/Lot/Goule_LOD_eteint_et_lueur.jpg`.

| Prefab (construit par `TestArenaSceneBuilder`) | Modèle | Placement | Matériau piloté par le code |
|---|---|---|---|
| goule : enfant « Corps (visuel) » (capsule greybox en (0, 0.55, 0)) | `SM_Goule` (le modèle entier, avec son `LODGroup`) | en (0, 0, 0) sous « Corps (visuel) », rotation nulle ; retirer la capsule | `M_Goule` : `_EmissionColor` (noir au repos) allume les **deux yeux** (disque, pupille plus claire) et la **lueur entre les dents** : c'est le signal rouge avant le bond (0.3 s) ; l'écrasement à 60 % et la bascule de 80° sur les murs s'appliquent au visuel comme aujourd'hui |

**Traqueur, version statique provisoire (en attendant le squelette).** Deux façons de l'intégrer :

| Pièce | Remplace | Placement | Remarques |
|---|---|---|---|
| **`SM_Traqueur_Corps`** (recommandé) : le géant **sans les bras**, 14 m, épaules refermées | les greybox de « Corps (se baisse) » (jambes, torse, tête, yeux) | en (0, 0, 0) sous « Corps (se baisse) », rotation nulle (le visage regarde +Z, comme la racine) | yeux : les pupilles rouges brillent par l'émission de `M_Traqueur` (rouge HDR (4, 0.25, 0.15) par défaut, comme les sphères greybox) ; l'épaule du modèle est à **11.1 m** de haut et **1.8 m** du centre |
| **`SM_Traqueur_Bras`** : tronçon de bras ramené à une **boîte unité** (Ø 1, longueur 1 le long de +Z, centré) | le maillage des cubes « Bras qui attrape » et « Bras gauche » (garder leurs transforms) | aucun : `PoseLimb` met déjà l'échelle (armThickness, armThickness, longueur) et l'oriente de l'épaule à la main | `MeshFilter.sharedMesh` = maillage de `SM_Traqueur_Bras`, matériau **`M_Traqueur_Mains`** (liseré clair de silhouette) |
| **`SM_Traqueur_Main_D`**, **`SM_Traqueur_Main_G`** : les mains d'origine (doigts, griffes), ramenées à une **boîte unité** (doigts vers +Z, paume vers −Y, poignet en −Z) | le maillage des cubes « Main (zone de prise) » et « Main gauche » | aucun : l'échelle `handSize` les met à la taille de la zone qui tue | la BoxCollider unité de la main coïncide exactement avec la main visible. Proportions naturelles de la main : 4.93 × 2.46 × 5.82 m pour 14 m de haut, soit **(3.8, 1.9, 4.5)** pour une main de 4.5 m ; avec `handSize` = (4.9, 1.5, 4.5) (décision du 2026-11-16), la main est un peu élargie et aplatie. Matériau **`M_Traqueur_Mains`** : griffes éclaircies, liseré clair, **griffes émissives pilotées par le jeu** : `_EmissionColor` au repos (0.35, 0.32, 0.25) (déjà dans le matériau), pendant la prise **(3.5, 0.49, 0.21)** (rouge HDR, comme la colonne), par `MaterialPropertyBlock` sur les renderers des deux mains (et des bras, sans effet visible : pas de griffes) ; planches `docs/art/rendus/Lot/Traqueur_sombre_{10,20,30}m.jpg` |
| `SM_Traqueur` : le géant **d'un seul tenant**, 14 m, bras baissés (bout des doigts au genou), épaissis à 1.5 m, mains à 0.67 (3.1 m de long à cette échelle) | — | aperçu immobile seulement | ses bras ne bougent pas : à n'utiliser qu'en masquant les bras et mains posés par le code |

Points à régler sur `main` (code) : l'épaule du code est à `width` × 0.4 = **2.8 m** du centre et **12 m** de haut, celle
du modèle à **1.8 m** et **11.1 m** (prévoir un réglage d'épaule séparé de `width`, ou accepter 1 m d'écart caché
par l'épaisseur du bras) ; `eyeHeight` 12.5 m (yeux du modèle à 12.9 m : cohérent). Captures de l'assemblage dans la
pose du code (main droite levée, main gauche au repos) : `rendus/Lot/Traqueur_pieces_{face,34}.jpg`.

## 5. Props encore à produire (catalogue d'`ART-HANDOFF`)

Greybox actuelles (bornes de leur maillage) : à garder comme cotes et pivots. **Attrapables** : gardent leur Rigidbody,
leur `Grabbable` et leur lueur ; leur modèle doit rester dans la boîte (la collision est celle de la greybox).

| Prefab | Dimensions (l × h × p) | Nombre | Rôle | Remarque |
|---|---|---|---|---|
| `Prop_Caisse_Attrapable` | 0.8 × 0.8 × 0.8 | 279 | **attrapable** (15 kg) | **modèle livré : `SM_Caisse`** (voir ci-dessous) |
| `Prop_Tonneau` / `_Attrapable` | 0.9 × 1.1 × 0.9 / 0.6 × 0.9 × 0.6 | 212 / 131 | fixe / **attrapable** (20 kg) | |
| `Prop_Etagere` | 2.6 × 3.2 × 0.7 | 117 | fixe | |
| `Prop_Caisse` | 1.3 × 1.3 × 1.3 | 100 | fixe | même modèle que l'attrapable, à l'échelle |
| `Prop_CoffreDeco` | 1.2 × 0.8 × 0.8 | 85 | fixe | |
| `Prop_Table` | 2.6 × 0.95 × 1.2 | 82 | fixe | |
| `Prop_Banc` | 2.6 × 0.55 × 0.6 | 79 | fixe | |
| `Prop_PileDeCaisses` | 2.6 × 2.6 × 2.6 | 78 | fixe | assemblage de `Prop_Caisse` |
| `Prop_Paille` | 2 × 0.3 × 2 | 77 | décor sans collision | |
| `Prop_Gravats` | 3.2 × 1.2 × 3 | 72 | fixe | |
| `Prop_Statue` | 1.4 × 3.6 × 1.4 | 69 | fixe | |
| `Prop_Chaise_Attrapable` | 0.55 × 1 × 0.55 | 63 | **attrapable** (8 kg) | |
| `Prop_Sarcophage` | 1.2 × 1 × 2.5 | 52 | fixe | |
| `Prop_Ratelier` | 2.6 × 2.1 × 0.5 | 40 | fixe | |
| `Prop_Armoire` | 1.6 × 2.7 × 0.8 | 30 | fixe | |
| `Prop_Lit` | 1.3 × 0.7 × 2.3 | 23 | fixe | |
| `Prop_Lanterne` | 0.5 × 0.8 × 0.5 | 258 | lumière, sans collision | peut reprendre `SM_LanterneFeu` à l'échelle ≈ 2.7, verre et lueur neutres |
| `Prop_Chaudron` | 1.3 × 1 × 1.3 | 14 | fixe | |
| `Prop_MurEffondre` | 5 × 3 × 1.2 | 11 | fixe | |
| `Prop_Autel` | 2.6 × 1.2 × 1.3 | 4 | fixe | |
| Lustre (salles) | 1.6 × 0.3 × 1.6, suspendu | — | **attrapable** (25 kg, tombe) | pivot au point d'accroche |
| Lustre géant (arène) | 12 × 1.5 × 12, à 24 m | 1 | piège | |
