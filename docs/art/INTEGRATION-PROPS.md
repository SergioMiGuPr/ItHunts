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
| 4 bis | `Kit_Arc` (variante) | **`SM_Arc_Couloir_Ecoincons`** | **livré (art)** | même pivot que `SM_Arc_Couloir` (au sol, au milieu entre les deux pieds) | 14 × 11 × 1.6 m ; **même anneau**, écoinçons pleins jusqu'au cadre de 14 × 11 m (plaques de 1.2 m d'épaisseur, en retrait de 0.1 m sur la face de l'anneau) : remplace les blocs de pierre provisoires entre l'arc et la voûte ; même collision d'anneau (`ArchMidline`) plus, si besoin, une boîte par écoinçon ; s'élargit comme l'arc (`width / 14`) ; Read/Write ; `M_Pierre` ; 2 588 triangles ; `rendus/Lot2/Arc_Couloir_Ecoincons.jpg` |
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

### Kit gothique du couloir de 14 m, voûte en lancette (2026-11-16, d'après `reference_couloir_gothique`, plan 04 de la cinématique)

Six pièces (`Assets/Art/Models/Niveau1/`, script `sm_gothique.py`), pour approcher la référence : **voûte en ogive** (et non
plate ni en berceau) portée par des **arcs-doubleaux** sur **colonnes engagées**, et des **arcades** et **niches** en ogive
dans les murs. **Travée de 8 m** le long du couloir ; murs latéraux à x = ±7 m, hauteur 8 m (naissance de la voûte).
Pivot au sol pour toutes ; **UV en coordonnées monde** : `M_Pierre_Monde`, et `M_Voute_Briques_Monde` sous la voûte
(remap fait dans les `.meta`) ; Read/Write activé. Repère Unity : pièces de voûte en travers du couloir (x), le long de z ;
murs et colonne : face côté couloir vers **+Z**, le mur s'étend vers −Z.

| Pièce | Dimensions (Unity) | Pivot | Usage | Triangles |
|---|---|---|---|---|
| `SM_Voute_Ogive_8m` | x −7.6..7.6, y 8..21.2, z −4..4 | au sol, milieu du couloir et de la travée | tronçon de voûte répétable tous les 8 m (intrados : arc brisé en **lancette**, centres hors de la portée, de la naissance à 8 m jusqu'à la clé à **20.5 m** ; 0.6 m d'épaisseur ; repose sur le dessus des murs) | 292 |
| `SM_Voute_Ogive_4m` | idem, z −2..2 | idem | demi-tronçon pour finir une longueur de couloir qui n'est pas un multiple de 8 m | 292 |
| `SM_ArcDoubleau_14m` | x −7..7, y 8..20.5, z −0.4..0.4 | au sol, milieu du couloir | arc de structure sous la voûte, un par limite de travée (tous les 8 m, au-dessus des colonnes) ; 0.48 m d'arc + boudin de 0.12 m : **clé de l'intrados à 19.8 m** | 584 |
| `SM_Colonne_Engagee_8m` | x −0.72..0.72, y 0..8, z 0..0.78 | au pied, sur la face du mur | une par mur sous chaque arc-doubleau (socle, base, fût demi-rond Ø 0.76 m, chapiteau et tailloir qui porte l'arc) ; tourner de ±90° pour l'adosser au mur gauche ou droit | 166 |
| `SM_Mur_Arcade_8m` | x −4..4, y 0..8, z −1.2..0.2 | au sol, milieu de la travée, sur la face du mur | mur d'une travée percé d'un **passage en ogive de 4.4 m × 7 m** (naissance 4 m) vers une pièce annexe ; tableaux et intrados modelés, cordon d'archivolte et impostes | 478 |
| `SM_Mur_Niche_8m` | idem | idem | mur d'une travée creusé d'une **niche en ogive** de 2.4 m × 4.7 m (appui à 0.9 m, clé à 5.6 m, 0.6 m de profondeur) : décor, statue, coffre, torche | 445 |

**Montage d'un couloir** (planche `rendus/Lot2/Kit_Gothique_Couloir.jpg`, 5 travées) : pour chaque limite de travée
z = 8k, un `SM_ArcDoubleau_14m` en (0, 0, 8k) et deux `SM_Colonne_Engagee_8m` en (±7, 0, 8k), tournées de −90° (droite)
et +90° (gauche) ; pour chaque travée, un `SM_Voute_Ogive_8m` en (0, 0, 8k + 4) et deux murs (`SM_Mur_Arcade_8m`,
`SM_Mur_Niche_8m`, ou des `SM_Kit_Mur4x4` pour un mur plein) en (±7, 0, 8k + 4), mêmes rotations. Sol : `SM_Kit_Sol4` /
`M_Dalles_Monde`. Torches : sur les trumeaux, près des colonnes.

**Passage du traqueur** : largeur libre sous les arcs-doubleaux (entre les intrados) 12.8 m à 8 m, **11.8 m à 11.75 m**
(épaules), 11.0 m à 13 m, **10.1 m à 14 m**, 9.6 m à 14.5 m ; clé à **19.8 m** (15 m libres largement tenus). Les colonnes
réduisent la largeur de 14 à 12.8 m au sol (socles) et 12.4 m au tailloir (8 m).

**Pointe de la voûte (décision du 2026-11-16)** : naissance **gardée à 8 m** (le passage du traqueur passe avant) ; la
voûte est rendue plus aiguë en **montant la clé** de 15.6 à **20.5 m** (intrados) : arc brisé en lancette, centres sur la
ligne de naissance mais **hors de la portée** (à 7.66 m de l'axe, de l'autre côté ; rayon 14.66 m ; rapport flèche / demi-portée
1.79, contre 1.09 avant). Dessus de la voûte (extrados) à 21.2 m : le prévoir sous le plafond de la salle
ou le niveau supérieur. Avant / après et référence : `rendus/Lot2/Voute_Lancette_avant_apres.jpg`. Réglage :
`PARAMS["cle"]` dans `sm_gothique.py` (au-delà de 8 + 7 × √3 = 20.1 m, les centres sortent de la portée).

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
| Orbe (main droite, sur `Orb_Socket`) | `SM_Orbe` (racine vide) | pivot au centre de l'orbe sur `Orb_Socket` ; **échelle à régler dans FpsView** : la greybox fait Ø 0.08 m, le modèle 0.18 m (anneaux) et 0.295 × 0.228 × 0.182 m avec les tablettes | `SM_Orbe_Anneau_1` (axe **X**), `_2` (**Y**), `_3` (**Z**), modélisés à leur pose de repos ; `SM_Orbe_Tablette_01` à `_06` = **gabarits** (D-069 : le jeu en crée une par charge) ; tablette seule **`SM_Orbe_Tablette`** (FBX séparé) ; repère `FX_Orbe` au centre | `M_Orbe_Energie` (shader **`ItHunts/OrbeVortex`** : `_EmissionColor`, couleur du personnage ; `_SpinBoost` en traction) ; rune de chaque tablette : **index de matériau 1** (`M_Orbe_Tablette`, rune gravée sur les deux faces, masque d'émission) ; voir « Orbe v2 : tablettes et tourbillon » ci-dessous |
| Lanterne de feu (artefact du renard, main gauche) | `SM_LanterneFeu` | pivot au centre de l'anneau de portage (la lanterne pend dessous) | — | `M_LanterneFeu_Verre` : translucide (file 3000) et `_EmissionColor` ; **`M_LanterneFeu_Flamme`** (enfant `SM_LanterneFeu_Flamme`, flamme en goutte au-dessus de la mèche, depuis le 2026-11-16) : `_EmissionColor` = couleur du personnage (par défaut, orange du renard) ; **passe transparente des mains à ajouter** (la passe « ItHunts Mains : mains » ne dessine que l'opaque) ; repères `FX_Flamme` (mèche) et `FX_Jet` (départ du jet, +Z = avant, dégagé sur 6 m) |
| Bras des 4 personnages (remplacent les mains greybox) | `SK_BrasFPS_<Perso>_D` / `_G` (armature `BrasFPS`, 17 os, mêmes noms partout) | placer le FBX pour que **`Socket_Poignet` coïncide avec la main greybox** (position et rotation de repos de FpsView, § 5 bis d'ART-HANDOFF) : le pivot est au coude, `Socket_Poignet` est en (0, 0, 0.27) dans le FBX, axes de l'ancrage (+Z doigts, +Y dos de la main) ; aucun recalage d'axe | os des doigts : remplacent l'angle unique `rightFingers` / `leftFingers` (rotation X) par les clips `BrasFPS|Pose_*` (mêmes clips pour les 4 personnages ; un jeu de clips par côté, le gauche est le miroir) ; `Pose_Attraper` a 3 clés (repos, ouverte, refermée), les 9 poses fixes 2 clés identiques (images 1 et 2) pour que chaque clip garde ses 180 courbes à l'import ; 4 boucles de flottement et nouvelle convention des poses au § 4 quater ; les déplacements de la main entière restent dans FirstPersonView | `M_BrasFPS_<Perso>` (texture peinte, un matériau par personnage) ; contour par coque inversée (RENDU.md § 5 bis) ; repères fixes enfants de `BrasFPS` : `Socket_Orbe` (0, 0.06, 0.045) depuis le poignet (droit), `Socket_Artefact` (0, 0.055, 0.05) et `Socket_Artefact_Anneau` (0, −0.05, 0.105) pour l'anneau de la lanterne dans `Pose_PriseAnneau` (gauche, à ajuster en jeu) ; planches `BrasFPS_*.jpg` |
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

## 4 quater. Animations (lot du 2026-11-16 : retours de playtest)

Noms identiques dans tous les FBX d'un même objet (clips Unity « `<Armature>|<Nom>` »). Boucles réglées à l'import
(*Loop Time*), première et dernière images identiques.

### Orbe v2 : tablettes et tourbillon (2026-11-17, branche `art-orbe`)

Retours de playtest : runes gravées sur une seule face (invisibles selon l'angle) ; nouvelle règle D-069 (une tablette par
charge de traction ou de répulsion, 2 + 1 au départ) ; tourbillon de l'énergie demandé par `main` dans `ART-HANDOFF`.

- **Tablette** : `Assets/Art/Models/FPS/SM_Orbe_Tablette.fbx`, **une tablette seule**, pivot au centre, rotation nulle,
  échelle 1. Repère Unity : **X = rayon** (0.035 m, vers l'extérieur de la couronne), **Y = grand côté** (0.05 m, tangent),
  **Z = épaisseur** (0.008 m). **Rune sur les deux faces** (-Z vers le joueur et +Z), chacune lisible sans miroir de son
  côté ; bords chanfreinés ; 28 triangles. Pour la couronne : position `(cos a, sin a, 0) × 0.13 m` dans le repère de
  l'orbe, rotation `Quaternion.Euler(0, 0, a)` (X de la tablette vers l'extérieur).
- **Matériaux** (même contrat qu'avant) : index 0 = `M_Orbe_Pierre` (bords), **index 1 = `M_Orbe_Tablette`** (les deux
  faces : atlas `T_Orbe_Tablette_BaseColor`, rune gravée, et masque `T_Orbe_Tablette_Emission`). **Allumer** = écrire
  `_EmissionColor` (couleur du personnage × intensité) sur l'index 1, comme `OrbModelView.SetEmission(renderer, 1, …)` le
  fait déjà ; le masque limite la lumière à la rune et à une **lueur douce autour** (halo peint). Éteinte : noir.
- **Glyphe** : atlas de 6 glyphes (3 colonnes × 2 lignes, case 0 en haut à gauche). La tablette seule montre le glyphe 0 ;
  pour le glyphe *k* (0 à 5) : bloc de propriétés sur l'index 1, `_BaseMap_ST = (1, 1, (k % 3) / 3, -(k / 3) / 2)`
  (décalage d'UV ; l'émission suit les mêmes UV). Les gabarits `SM_Orbe_Tablette_01` à `_06` de `SM_Orbe` portent déjà
  les glyphes 0 à 5.
- **`SM_Orbe`** : les 6 gabarits ont le nouveau dessin (mêmes noms, mêmes positions) : `OrbModelInstaller` et
  `OrbModelView` continuent de marcher. **À relancer sur `main` : `OrbModelInstaller`** (le prefab Player garde une copie
  dépaquetée de l'ancien modèle et des anciens matériaux `M_Orbe_Tablette_01` à `_06`, laissés en place mais plus utilisés
  par le FBX). Sphère d'énergie plus ronde (728 triangles au lieu de 224) ; total 2 888 triangles.
- **Tourbillon** : `M_Orbe_Energie` passe sur **`ItHunts/OrbeVortex`** : bras de spirale en aplats qui tournent autour de
  l'axe de visée (vu du joueur, c'est toujours un vortex), cœur clair, bord vif. Propriétés : `_EmissionColor` (HDR,
  inchangé : le jeu l'écrit déjà), **`_SpinSpeed`** (tours/s, 0.6), **`_SpinBoost`** (multiplicateur : **le monter en
  traction**, par exemple 3.5, par bloc de propriétés), `_SpinPhase` (tours : pour accélérer sans à-coup, le jeu peut
  intégrer lui-même `phase += vitesse × boost × dt` et laisser `_SpinSpeed` à 0), `_Arms`, `_Twist`, `_ArmWidth`,
  `_Base`, `_Core`, `_Rim`, `_Pulse`. Faire tourner la transformation de l'énergie (`energySpin`) devient inutile.
- Planche : `docs/art/rendus/FPS/Orbe_v2_Tablettes_Vortex.jpg`.

### Bras FPS (`SK_BrasFPS_<Perso>_D` / `_G`, armature `BrasFPS`, 20 os depuis la v2)

**Correction (2026-11-17, branche `art-bras-normales`)** : les 8 FBX v2 sortaient **retournés** (volume signé négatif) : le
passage du repère de la main au repère du fichier est un miroir (déterminant −1) ; les faces sont maintenant remises vers
l'extérieur à l'export. Vérifié dans Unity sans contournement : volumes **positifs** (1 339 à 1 686 cm³), normales dans le
sens des faces (99.5 à 100 %), mêmes 20 os et 16 clips, même géométrie. Le contournement de `main` (`FpsArmsSetup`,
`HandsOutline`) se désactive tout seul.

**Version 2, les 4 personnages (2026-11-17, branche `art-mains`)** : renard validé par l'utilisateur, puis koala (gant
complet, griffes qui percent le gant, fourrure grise ; 8 230 triangles), caméléon (mitaine, doigts et avant-bras écailleux,
pas de griffes, manche rayée ; 9 524), hibou (pas de gant, main écailleuse en serres, plumes ; 8 005). **Même armature, mêmes
noms d'os, mêmes repères, mêmes 16 clips** pour les 4 : ce qui suit vaut pour tous. Planches
`rendus/FPS/BrasFPS_v2_<Perso>.jpg`.

Historique, renard seul (2026-11-17) — retour de playtest : « les doigts sont des
tubes à anneaux ». Main refaite sur une **base anatomique CC0** (maillage de base MakeHuman), modelée par script (phalanges
qui s'affinent, jointures, coussinets, éminences du pouce, tendons, os du poignet), habillée : gant sans doigts en coque à
rebord épais, griffes, fourrure, manche à runes, sangle. **10 018 triangles par bras**, 4 os au plus par sommet, texture
`T_BrasFPS_Renard_BaseColor` (même fichier, même matériau `M_BrasFPS_Renard`). Planche `rendus/FPS/BrasFPS_v2_Renard.jpg`.
Ce qui change pour `main` :
- **20 os** : les 17 d'origine (mêmes noms) + `Metacarpe_Annulaire` (parent d'`Annulaire_1`), `Metacarpe_Auriculaire`
  (parent d'`Auriculaire_1`) et `Paume_Pouce` (parent de `Pouce_1`), tous enfants de `Poignet`. La hiérarchie change :
  **relancer `FpsArmsSetup`** (les tableaux de `FpsArmPoseSet` passent de 17 à 20 rotations). `FpsArms.Curl` ferme les
  doigts par préfixe de nom : les métacarpes ne bougent que par les clips (voulu).
- **Clips** : les 14 de la v1 (mêmes noms, même convention paume vers +Y du repère de la main ; `Pose_Poing` sans demi-tour)
  + **`Pose_MainOuverte`** et **`Pose_Griffe`** ; boucles `Boucle_*` inchangées.
- **Repères** : `Socket_Poignet`, `Socket_Orbe`, `Socket_Artefact`, `Socket_Artefact_Anneau` aux mêmes places (enfants fixes
  de `BrasFPS`) ; nouveau **`Socket_Flottement`** (D et G), **enfant de l'os `Poignet`**, à 6.5 cm de la paume : point de
  flottement de l'orbe ou de l'artefact qui suit la main (au-dessus de la paume dans les poses de flottement).
- Mêmes mesures de main (poignet → bout du majeur 0.165 m) et même pivot : le placement de FpsView ne change pas.
Koala, caméléon et hibou restent en v1 en attendant l'accord sur le renard.


**Détail (2026-11-16)** : mains et avant-bras repris d'après `fiches/planche_bras_fps_v01.png` (doigts arrondis à
phalanges renflées et bout rond, gant sans doigts arrêté sur un bourrelet, coutures en pointillés sur le dos de la main et la
manchette, paume arrondie et jointures, manche à plis en biais ; fourrure rousse (renard), collerette doublée (koala),
bandes d'écailles (caméléon, hibou)) : 2 874 à 3 834 triangles par bras (budget 6 000) ; **même squelette, mêmes noms
d'os, mêmes 14 clips**. (Ces aplats, dont `_Couture`, `_Poil`, `_Ecaille`, sont remplacés plus bas par la texture peinte et le matériau unique.) Planche
`rendus/Lot2/BrasFPS_Detail_avant_apres.jpg`.

**Maillage d'un seul tenant et poids lissés (2026-11-16)** : chaque bras est une seule surface fermée (manche, ourlet
replié, brassard, gant, paume, doigts et pouce soudés : plus aucune coupure aux articulations) ; seuls les décors restent
des coques collées (sangles, boucles, poils, plumes, liserés, griffes), qui prennent les poids du point du bras le plus proche.
Poids : rampes douces à chaque articulation, plus larges côté dos (jointure arrondie) que côté paume (pli net), lissées sur
la paume et la base du pouce ; 4 influences au plus par sommet. **Repos légèrement fléchi** (16° à la jointure, 20° et 12°
aux phalanges suivantes, doigts en léger éventail) : un pli de 90° ne demande que 70° environ de rotation, ce qui limite
l'écrasement du skinning linéaire d'Unity. Les poses gardent les mêmes angles absolus (le script retire le pli de repos) :
**rebaker `FpsArmPoseSet`** (les rotations locales de repos des os des doigts changent). `Pose_Poing` : pouce replié sur
les doigts. Mesures (doigt plié 90/90/70°) : rayon à l'articulation 0.91 à 0.97 du repos (0.72 à 0.80 sans le repos
fléchi), volume du doigt −5 à −8 % (pli intérieur compris) ; poignet ±60° : −0.8 % sur le bras. Triangles : renard 4 256,
caméléon 4 132, koala 3 702, hibou 3 356. Planche `rendus/Lot2/BrasFPS_Un_seul_tenant_flexions.jpg`.

**Os à ajouter pour des mains plus expressives (proposition, non faite)** : le squelette actuel (17 os) suffit pour plier
et écarter les doigts (`spread` sur les phalanges 1). Il ne sait pas creuser la paume ni rapprocher le pouce de
l'auriculaire. Proposition : `Metacarpe_Annulaire` et `Metacarpe_Auriculaire` (enfants de `Poignet`, parents des
phalanges 1 correspondantes : la paume se creuse, la main se referme en coupe) et `Paume_Pouce` (éminence sous le pouce,
suit `Pouce_1` à moitié). Côté code : `FpsArmsSetup` lit les os par la hiérarchie, les nouveaux os sont cuits sans
changement ; `FpsArms` ferme les doigts par préfixe de nom (`Index`, `Majeur`…), donc les métacarpes ne bougeraient que
par les clips ; les tableaux de rotations de `FpsArmPoseSet` changent de longueur : rebaker. Avec les vidéos de Sergio,
ces 3 os rendraient le creux de la paume et l'opposition du pouce.

**Convention des poses de flottement** (rien n'est tenu, l'orbe et l'artefact flottent au-dessus de la paume) : dans
`Pose_Repos`, `Pose_OrbePaume`, `Pose_Pistolet`, `Pose_PriseAnneau` et leurs boucles, l'avant-bras est tourné de 180° :
**la paume regarde +Y du repère de la main** (`Socket_Poignet`), là où FpsView pose déjà l'orbe (`orbInHand` (0, 0.06,
0.045)), le barillet (`barrelInHand`) et la lanterne (`lanternInHand`). `Socket_Artefact_Anneau` est passé en (0, 0.075,
0.05) depuis le poignet (anneau de la lanterne au-dessus de la paume). **Depuis le 2026-11-16, les poses d'action
suivent la même convention** (`Pose_Traction`, `Pose_Repulsion`, `Pose_Attraper` — sa première clé est `Pose_Repos` —,
`Pose_TenirObjet`, `Pose_Lancer` : paume vers +Y, forme de la main gardée) : plus de demi-tour de l'avant-bras d'une pose
à l'autre (planche `rendus/Lot2/BrasFPS_Poses_action_avant_apres.jpg`). Seule `Pose_Poing` garde l'ancienne convention (poing fermé, pouce replié sur les doigts).

**Placement conseillé, d'après `docs/design/references-visuelles/reference_bras_fps.png`** (2026-11-16 : avant-bras qui
entrent par les coins bas, paumes ouvertes vers le haut et l'une vers l'autre, doigts écartés et détendus ; vérifié à
`viewmodelFov` 60, planche `rendus/Lot2/BrasFPS_Cadrage_reference.jpg`), dans `FpsView_Default` :

| Réglage | Main droite | Main gauche |
|---|---|---|
| `…HandRest` (position du poignet, repère caméra) | (0.21, −0.09, 0.34) | (−0.21, −0.09, 0.34) |
| `…HandRestEuler` | (−45.1, −21.0, 39.7) | (−45.1, 21.0, −39.7) |
| objet qui flotte au-dessus de la paume | `orbInHand` **(0, 0.14, 0.045)** | `lanternInHand` **(0, 0.16, 0.05)**, `barrelInHand` (0, 0.14, 0.05) |

(doigts vers (−0.25, 0.7, 0.65) et paume vers (−0.45, 0.7, −0.55) dans le repère caméra, main droite ; les objets montent de
0.06–0.075 à 0.14–0.16 m pour flotter sans toucher les doigts, comme sur la référence.)

**Aspect (2026-11-16)** : doigts sculptés (pli à chaque articulation entre deux renflements, phalange affinée, bout rond,
griffe ou ongle qui sort du bout du doigt : plus de taches sombres), paume ronde, gants de cuir sans doigts à bourrelet ;
**une texture peinte par personnage**, `T_BrasFPS_<Perso>_BaseColor` (2048 px, partagée par les deux bras), **un seul
matériau** `M_BrasFPS_<Perso>` (cuir marbré et éraflé, tissu à trame et plis, fourrure en mèches, écailles, plumes, coutures
peintes, plis d'encre aux articulations). Lisible dans l'ambiance sombre sans émission : couleurs relevées, liseré discret
(0.4, non lié à la lumière), ambiance comptée double et plancher de la bande d'ombre (0.22) sur ce matériau. Les anciens aplats
`M_BrasFPS_<Perso>_<Pièce>` sont supprimés. **Conseil côté `main`** : en ambiance sombre, une lumière d'appoint faible sur le
calque « FPS » (directionnelle depuis la caméra, ≈ 0.3) rendrait les mains encore plus lisibles sans toucher au décor.
Renard d'après la référence : manche rouge déchirée, gant de cuir brun, fourrure rousse aux poignets et au bord de la manche.

| Clip | Durée | Boucle | Contenu |
|---|---|---|---|
| `Pose_Repos` | 2 images (0.04 s) | non | flottement, doigts recourbés sans toucher (orbe à droite, artefact à gauche) |
| `Pose_OrbePaume` | 2 images | non | main plus ouverte, doigts écartés (orbe au-dessus de la paume) |
| `Pose_Pistolet` | 2 images | non | index et majeur tendus, annulaire et auriculaire à demi repliés (barillet flottant) |
| `Pose_PriseAnneau` | 2 images | non | doigts en crochet au-dessus de l'anneau flottant (lanterne) |
| `Boucle_Repos`, `Boucle_OrbePaume`, `Boucle_Pistolet`, `Boucle_PriseAnneau` | **3.5 s** | **oui** | la pose du même nom + respiration (bascule de 1.4° de l'avant-bras au coude, soit ≈ 7 mm à la main, et torsion de 0.8°) + doigts qui ondulent (±5°, deux fois par boucle, décalés d'un doigt à l'autre) ; à jouer en état de repos à la place de la pose fixe |
| `Pose_Attraper` | 0.29 s (3 clés) | non | inchangé |
| autres `Pose_*` | 2 images | non | inchangées |

Événements : aucun (les déplacements de la main entière restent dans FirstPersonView).

### Goule (`SK_Goule`, armature `Goule`, 26 os + 10 os d'aide)

**Gréement refait (2026-11-16)** : base = chaleur sur le maillage d'origine (bouche ouverte, mâchoire et hanches nettes),
copie étanche en voxels (`poids_lisses.py`) là où la chaleur échoue et sous les genoux ; 4 os au plus par sommet.
**Os d'aide** `Bras_Aide`, `AvantBras_Aide`, `Main_Aide`, `Jambe_Aide`, `Pied_Aide` (.L/.R : épaules, coudes, poignets,
genoux, chevilles) : même position que l'os, même parent, ils copient la **moitié** de sa rotation (contrainte Copy Rotation
0.5 dans le .blend, cuite dans les clips à l'export) ; les sommets du pli passent sur eux, chaque côté du pli ne tourne que de
la moitié de l'angle : moins d'écrasement. **Rien à piloter côté Unity** (ils sont dans les clips) ; une animation faite
dans Blender (vidéos de référence) les entraîne d'elle-même. Pic de distorsion des triangles autour des articulations
(98e centile de |log2 aire/aire au repos|), avant → après : épaules (bras levés) 1.67/1.78 → 1.30/1.55, genoux 0.79–1.53 →
0.70–1.18, coude 0.69 → 0.64, mâchoire et hanches inchangées. Planche `rendus/Lot2/Goule_Greement_avant_apres.jpg`.

**Os correcteurs du creux des genoux (branche `art-fix`)** `Jambe_Pli.L/.R` : enfants de `Jambe_Aide`, même repos. Ils
prennent la part de l'aide sur le côté comprimé du genou ; un pilote (driver) les pousse vers l'extérieur du pli au-delà de
**101.5°** entre la cuisse et la jambe (`PLI_SEUIL`, `PLI_GAIN` 0.11 m/rad, `PLI_LARGEUR` 0.02 m dans `sk_goule.py`), cuit
dans les clips à l'export. Seul `Goule_Mort` dépasse ce seuil (106.2° ; `Escalade` 101.3° au plus) : **les autres clips
sont identiques au sommet près** (3 LOD). `Goule_Mort`, image extrême : genou gauche 2.17 → **0.88**, droit 1.26 → 0.99 ;
pic sur tout le clip 2.17 → 1.94 (avant la PR #8 : 2.04). Rien à piloter côté Unity ; 28 os déformants + 10 aides + 2
correcteurs. Planche `rendus/Lot2/Goule_Genou_Mort_avant_apres.jpg` (carte de déformation, courbes image par image).

**Modèle à utiliser :** `SK_Goule` (gréé) à la place de `SM_Goule` (statique, gardé pour les aperçus), en (0, 0, 0) sous
« Corps (visuel) », rotation nulle (avant +Z). Le FBX contient l'armature `Goule` et `SM_Goule_LOD0` (22 000 triangles),
`_LOD1` (11 000), `_LOD2` (5 000) en `SkinnedMeshRenderer` (mêmes 26 os), avec un `LODGroup` à la racine. Import réglé :
*Generic*, *Bake Axis Conversion*, boucles. Ajouter un `Animator` (contrôleur côté `main`) ; pas de déplacement de la racine
(le code déplace la goule), sauf `Goule_Mort` qui couche le corps sur le flanc.

**Échelle de 1.8 m** (le jeu agrandit la goule de 1.1 à ≈ 1.8 m, × 1.64) : vérifiée (planches ci-dessous, à 1.8 m) ; la
texture de 2048 px reste nette de près (`Goule_texture_gros_plans.jpg`) ; proportions inchangées (allongée, bras jusqu'au sol).
**Seuils de LOD conseillés à 1.8 m** (hauteur d'écran, champ de vision de 70°) : **LOD1 sous 0.25 (≈ 5 m), LOD2 sous 0.10
(≈ 13 m), disparition sous 0.012 (≈ 105 m)** ; le `LODGroup` mesure l'objet mis à l'échelle, ces seuils s'entendent donc
avec l'échelle du jeu (ils remplacent ceux conseillés à 1.1 m au § 4 ter).

**Squelette :** `Racine` (au sol) → `Bassin` → `Dos_1` → `Dos_2` → `Poitrine` → `Cou` → `Tete` → `Machoire` ;
`Epaule.L/R` → `Bras` → `AvantBras` → `Main` → `Doigts` ; `Bassin` → `Cuisse.L/R` → `Jambe` → `Pied` → `Orteils`
(`.L` = gauche du personnage). Métarig Rigify (types des chaînes posés) : dans le `.blend`, *Rigify > Generate* donne un
rig de contrôle pour retoucher les animations à la main ; les os exportés restent ceux de ce squelette.

| Clip (`Goule|…`) | Durée | Boucle | Contenu | À caler sur le jeu |
|---|---|---|---|---|
| `Goule_Repos` | 2.0 s | oui | respiration, tête qui oscille, mâchoire et doigts qui bougent | — |
| `Goule_Course` | 0.6 s | oui | galop sur 4 pattes (avant et arrière en opposition, dos qui ondule) | vitesse de lecture ∝ vitesse de course (0.6 s à la vitesse de chasse) |
| `Goule_Annonce` | **0.3 s** | non | s'accroupit, gueule qui s'ouvre (dernière image tenue) | annonce du bond (0.3 s), en même temps que la lueur des yeux |
| `Goule_Bond` | 0.4 s | non | détente : bras tendus en avant, jambes en arrière (dernière image tenue en vol) | départ du bond ; garder la dernière image jusqu'à l'atterrissage |
| `Goule_Reception` | 0.33 s | non | choc à 0.12 s (accroupie), retour au repos | atterrissage |
| `Goule_Escalade` | 0.8 s | oui | reptation en diagonale, dos qui ondule (le code bascule le visuel de 80° sur le mur) | sur un mur |
| `Goule_Coup` | 0.47 s | non | armé du bras droit (0 → 0.15 s), frappe (**impact à 0.24 s**), retour | dégâts au contact à 0.24 s |
| `Goule_Etourdie` | 1.2 s | oui | tête qui roule, corps qui tangue, mâchoire pendante | étourdissement |
| `Goule_Mort` | 1.0 s | non | jambes qui cèdent (0.3 s), chute sur le flanc (dernière image tenue) | mort, avant la disparition |

Planches : `docs/art/rendus/Lot2/Goule_<Animation>.jpg` (5 images réparties sur chaque clip, à 1.8 m).

### Traqueur (`SK_Traqueur`, armature `Traqueur`, 32 os) — v3, 2026-11-17 (branche `art-traqueur`, méthode D partie A)

Diagnostic : `rendus/Traqueur/Traqueur_Diagnostic.jpg` ; résultat : `rendus/Traqueur/Traqueur_v3_Retopo_Greement.jpg`.
Scripts : `Source~/Ennemis/scripts/traqueur_v3/` (étapes 1 à 5). **Ce qui suit remplace la section v2 plus bas.**

- **Maillage hybride** : corps **retopologisé en quads** (QuadriFlow, 97 % de sommets de valence 4, **boucles continues aux
  épaules, coudes, hanches et genoux**) ; **mains, tête et pieds d'origine** (Tripo retouché), coutures dans des zones rigides
  (milieu de l'avant-bras, cou, tibia), normales reprises de la surface d'origine autour des coutures. **38 364 triangles** +
  mèches 1 344 (budget 40 à 60 k). Volume positif (faces vers l'extérieur), mèches comprises.
- **Matériaux du corps (3)** : `M_Traqueur` (corps, **nouvelle texture `T_Traqueur_Corps_BaseColor`**, UV de la
  retopologie, plus d'émission), **`M_Traqueur_Tete` (nouveau)** : tête, pieds et la bande d'avant-bras d'origine avant le
  poignet (texture d'origine `T_Traqueur_BaseColor`, **émission des pupilles** `T_Traqueur_Emission`), `M_Traqueur_Mains` :
  **arrêté au poignet** (demande D-070 : plus de bande pâle qui s'étire), griffes émissives inchangées. Ordre dans Unity :
  `M_Traqueur`, `M_Traqueur_Tete`, `M_Traqueur_Mains`. **À vérifier côté `main`** : le rouge des pupilles s'écrit
  désormais sur `M_Traqueur_Tete` (un bloc de propriétés sans index les touche tous : sans effet sur `M_Traqueur`, qui n'a plus
  d'émission).
- **Liaison debout, bras écartés** (pose du modèle) : **la pose de repos n'est plus voûtée** ; la voûte est dans chaque clip
  (tous les os sont clés à chaque image). **32 os, plus d'os d'aide** (`*_Aide` retirés) : **refaire les prefabs et la vitrine
  qui référencent l'ancien squelette** (un rendu gréé dont les os ne correspondent plus n'est pas dessiné). Os **recalés au
  centre du volume** (coudes, poignets, doigts, pouce, tête, mâchoire, pied, orteils), roll 0, axe Y le long du membre,
  aucune échelle ni rotation figée à l'export.
- **Poids** : os le plus proche puis diffusion le long des arêtes (40 passes), **masqués par région** : un sommet du bras
  n'est tiré que par la chaîne de son bras ; **0 sommet tiré par un os à plus de 2.5 m** (avant : 1 079) ; 4 os au plus.
- **Allongement par l'échelle (D-070)** : à chaque articulation, mélange **du côté du parent seulement** ; `Bras` ne pèse rien
  derrière l'épaule ; la main ne porte aucun poids de l'avant-bras. Testé comme le code (bras tendu, `Bras` ×k en Y,
  l'avant-bras hérite, `Main` à l'échelle inverse) : à **×3.8**, main ×1.18 au plus, **8 triangles retournés**, surface
  continue ; seule la bande au-dessus de l'épaule s'étire fortement (inévitable : l'échelle part de l'épaule).
- **Clips bornés** : mains levées : épaule **104°** (≤ 110°), clavicule et torse prennent le reste, **torsion 0°** (avant
  150° et 88°) ; revers : **70°, torsion 0°** (avant 85° et 61°) ; bras **visés** (rotation la plus courte) ; prise : doigts
  qui se ferment vers la paume (bout des doigts → poignet 4.08 → 3.14 m) ; **marche et passage bas : pieds verrouillés par IK**
  (2 os), au sol 61 % du temps, **vitesse au sol constante : 3.71 m/s pour `Traqueur_Marche`, 2.29 m/s pour
  `Traqueur_PassageBas`** à la vitesse de lecture 1 (lecture = vitesse de poursuite / ces valeurs : plus de glisse).
- **Mesures** (repos, image 1) : épaule **11.6 m** de haut, **2.6 m** du centre, **3.56 m devant le pivot** ; sommet **13.6 m**
  (repos), **8.5 m** (passage bas), 20 m mains levées ; genou 3.8 m ; bout des doigts 4.4 m. Mêmes noms de clips et durées.
- **Tête refaite (2026-11-17, branche `art-traqueur-tete`)** : la tête livrée avait des trous (visage, yeux, bouche) et ses
  pupilles ne s'affichaient plus. **Cause** : la pièce « tête » d'origine était bornée à 2.6 m autour de la coupe du cou ;
  museau, yeux et mâchoire (2.6 à 2.9 m, mâchoire pendante sous le cou) manquaient et des fragments de la retopologie restaient
  à leur place. **Correctif** (`traqueur_v3/tete_fix.py`) :
  - tête reprise de la source par le même plan de coupe, **sans borne**, mâchoire comprise (1 844 faces), avec sa jupe au cou ;
  - **calotte crânienne** fermée sous les mèches (le crâne d'origine était la masse de cheveux retirée) : plaquée juste derrière le
    visage, elle n'apparaît qu'aux trous ;
  - poids repris du maillage précédent ; normales d'avant sur le reste du corps.
  **Pupilles** :
  - masque `T_Traqueur_Emission` agrandi en **disques de 0.4 m** autour de chaque œil, peints sur toutes les faces qui passent
    dans la sphère : visibles de face, d'en bas et de profil ;
  - 54 faces sur le masque (6 avant), aucune ailleurs ;
  - émission inchangée dans `M_Traqueur_Tete` : (4, 0.25, 0.15), *à travers le brouillard* 0.8.

  **Contrôle dans Unity** (pose de repos, brouillard linéaire 6–45 m des niveaux, depuis 1.7 m) : luminance max près de la
  tête **0.50 à 60 m** en 320 × 180 (seuil de `StalkerVisibilityTests` : 0.06), 0.61 à 30 m, 0.65 à 10 m. Planche
  `rendus/Traqueur/Traqueur_v3_Tete_corrigee.jpg`.

  **Bilan** : **38 878 triangles** (+514) + mèches 1 344 ; mêmes 32 os, mêmes clips, mêmes matériaux. **Côté `main`** :
  décocher `eyeGlints` (sphères de repli) dans `Stalker_Default` et relancer le test ; le rouge reste réglable par
  `_EmissionColor` de `M_Traqueur_Tete`.
- **Partie C** (clips d'après les vidéos Seedance) : liste des vidéos dans `docs/art/TRAQUEUR-REFERENCES.md`.

#### Ancienne version (v2, 2026-11-16) — remplacée


**Gréement refait (2026-11-16)** : poids « chaleur » sur une copie étanche en voxels, **masqués par régions** (un bras ne
tire jamais le torse, ni la mâchoire le crâne), poids doux par distance là où la chaleur échoue, lissés (12 passes) ; une
première fois debout pour voûter le modèle sans bosses, puis recalculés sur le modèle voûté avec les **os d'aide** (mêmes
noms et même principe que la goule). **Épaules** : géométrie lissée (Taubin, sans rétrécir) autour de chaque épaule après
la mise en voûte. Pic de distorsion, avant → après : épaules bras levés 1.01/1.80 → 0.02/0.36, revers 0.51 → 0.10, genoux
du passage bas 0.61/1.05 → 0.50/0.58, doigts 1.18/0.80 → 1.15/0.66. **Pouce (branche `art-fix`)** : poids recalculés le
long de l'axe du pouce (rampe main → pouce large, rampe `Pouce_1` → `Pouce_2` ; `POUCE_*` dans `sk_traqueur.py`), fondus
dans les poids d'origine, sommets tenus par les doigts gardés : `Traqueur_Prise` pouce 1.51 → **0.65** (avant la PR #8 :
0.88), base du pouce 1.03 → 0.80, doigts inchangés (1.15/0.66), poignet 0.76 → 0.41 (Prise), 0.31 → 0.30 (main plantée).
Pas de nouvel os. Planche `rendus/Lot2/Traqueur_Pouce_avant_apres.jpg`.
Mesures après : épaule 11.75 m, yeux 12.4 m, sommet 13.96 m (tient dans 14 m). Planche
`rendus/Lot2/Traqueur_Greement_avant_apres.jpg`.

**Modèle à utiliser :** `SK_Traqueur` remplace `SM_Traqueur_Corps` et les pièces `SM_Traqueur_Bras`, `_Main_D`, `_Main_G`
(gardées pour l'aperçu, plus à jour). En (0, 0, 0) sous « Corps (se baisse) », rotation nulle (avant +Z). Deux
`SkinnedMeshRenderer` : `SK_Traqueur` (24 804 triangles ; `M_Traqueur` sur le corps, `M_Traqueur_Mains` sur les bras et
les mains) et `SK_Traqueur_Cheveux` (7 mèches, 1 344 triangles, `M_Traqueur_Cheveux`). Import réglé : *Generic*,
*Bake Axis Conversion*, boucles.

**Géant voûté** (pose de repos = pose voûtée) : ≈ 20 m debout ; épaule **11.7 m**, yeux **12.3 m**, sommet du crâne
**13.9 m** (tient dans 14 m), genou 3.8 m, bout des doigts 4.0 m (au genou). Avec des yeux à 13 m, le crâne dépasserait
14 m (2 m de crâne au-dessus des yeux) : j'ai gardé 14 m comme limite. Mains : 4.4 m du poignet au bout des doigts.
Écart avec le code : l'épaule de `Stalker.cs` est à 12 m de haut et `width` × 0.4 = 2.8 m du centre ; celle du modèle
est à 11.7 m et **2.6 m** du centre, **4 m devant le pivot** (dos voûté).

**Os que le code oriente** (après l'`Animator`, dans `LateUpdate`) : `Epaule.R/L` (clavicule), `Bras.R/L` (épaule),
`AvantBras.R/L` (coude), `Main.R/L` (poignet) ; la main droite est celle de la prise. Doigts : `Doigts_1`, `Doigts_2`,
`Pouce_1`, `Pouce_2` (les quatre doigts groupés). `Machoire` (déboîtée au repos : ouverte et décalée).

| Clip (`Traqueur|…`) | Durée | Boucle | Contenu | À caler sur le jeu |
|---|---|---|---|---|
| `Traqueur_Repos` | 3.0 s | oui | respiration, tête et bras qui oscillent | — |
| `Traqueur_Marche` | 2.0 s | oui | marche voûtée lourde (sans déplacement de la racine) | vitesse de lecture ∝ vitesse de poursuite |
| `Traqueur_Prise` | **0.5 s** | non | bras droit tendu vers l'avant (0 → 0.32 s), **griffes qui se referment de 0.32 à 0.5 s** | annonce de la prise (0.5 s) ; le code oriente ensuite `Bras.R` → `Main.R` vers la cible |
| `Traqueur_MainsLevees` | **0.2 s** | non | deux mains au-dessus de la tête (dernière image tenue) | mains levées (0.2 s) |
| `Traqueur_MainPlantee` | 2.0 s | non | main droite frappée au sol (**impact à 0.2 s**), **plantée de 0.2 à 1.7 s (1.5 s)**, relevée de 1.7 à 2.0 s | main plantée (1.5 s) |
| `Traqueur_Revers` | **0.2 s** | non | revers du bras gauche, de travers à l'extérieur | revers (0.2 s) |
| `Traqueur_PassageBas` | 2.4 s | oui | marche accroupie, sommet ≈ **8.6 m** (passages de 9 m) | passage baissé |
| `Traqueur_Machoire` | 2.4 s | oui | **mâchoire seule** (balancement de côté, ouverture) | sur un calque additionnel avec un masque limité à `Machoire` |

Lueurs : pupilles par `_EmissionColor` de `M_Traqueur` (rouge par défaut) ; griffes par `_EmissionColor` de
`M_Traqueur_Mains` (repos (0.35, 0.32, 0.25), prise (3.5, 0.49, 0.21)). Planches : `rendus/Lot2/Traqueur_<Animation>.jpg`,
`Traqueur_texture_gros_plans.jpg`.

### Armes du koala et du hibou (lot E, 2026-11-21, branche `art-armes`)

Planche : `docs/art/rendus/Armes/Vitrine_Armes.jpg` (vue FPS, caméra 60°, bras gauche à la pose de repos, scène
`Assets/Art/Scenes/Vitrine_Armes.unity`). Constructeur : `Assets/Art/Source~/Armes/scripts/ArmesBuilder.cs`.

| Objet | Prefab | Mesures, pivot, repères | Matériaux et réglages |
|---|---|---|---|
| Épée en os du koala | `Assets/Art/Prefabs/FPS/Artefact_Epee_Koala.prefab` (modèle `SM_Epee_Koala.fbx`) | **0.62 m** de long, 0.113 m de large (garde), 0.053 m d'épaisseur ; **8 000 triangles** ; **pivot au milieu de la poignée** ; lame le long de **+Z** (comme l'axe de tir du barillet), plat de la lame vers +Y ; repères `FX_Epee_Garde` (0, 0, 0.084) et `FX_Epee_Pointe` (0, 0, 0.523) ; se pose sur **`Socket_Artefact`** du bras gauche, rotation nulle | `M_Epee_Koala` (`ItHunts/Toon`) : texture `T_Epee_Koala_BaseColor` (2048), **runes émissives vertes** (`T_Epee_Koala_Emission` = tons les plus clairs de la texture, ~2 % des pixels ; `_EmissionColor` (0.56, 3.5, 1.2) HDR), rim vert ; **lueur discrète** : lumière ponctuelle `Lueur_Verte` (0.35, portée 0.9 m, sans ombres), à couper si le budget de lumières l'exige ; `T_Epee_Koala_Normal` livrée (le toon ne la lit pas encore) |
| Livre du hibou | `Assets/Art/Prefabs/FPS/Artefact_Livre_Hibou.prefab` (modèle `SM_Livre_Hibou.fbx`) | ouvert : 0.23 × 0.18 m (× 0.6 en main = 0.14 m), **812 triangles** ; pivot au milieu du dos ; les pages regardent **−Z** (le joueur) ; enfants `SM_Livre_Hibou_Pages_Libres` (3 pages soulevées) et `SM_Livre_Hibou_Volutes` (3 rubans) ; repère `FX_Livre` (départ des liens de drain et de soin, lot D) ; **flotte** au-dessus de `Socket_Artefact` : `ArtefactFlottant` (repos (0.01, 0.13, 0.09), rotation (−15, 8, 0), échelle 0.6, flottement 1.2 cm à 0.35 Hz, balancement 4° à 0.23 Hz) | shader **`ItHunts/Spectre`** (translucide, bords en 2 paliers nets, lignes de texte qui défilent, bruit qui monte, **pages qui frémissent** : couleur de sommet R = poids, 0 au dos, 1 au bord ; `_Fremit` amplitude et fréquence) : `M_Livre_Couverture`, `M_Livre_Pages`, `M_Livre_Volutes` ; lumière `Lueur_Bleue` (0.5, portée 1.2 m) |
### Kit des nouveaux modules (lot H, 2026-11-21, branche `art-kit-modules`)

D'après les 4 références de l'utilisateur (`docs/art/references-modules/` : prison à étages, caserne, citerne inondée, puits
circulaire), à l'échelle du jeu (traqueur de 14 m, couloirs de 14 m, joueur de 1.8 m). Modélisé par script dans Blender
(`Assets/Art/Source~/Modules/scripts/sm_modules.py`), sans génération. **UV en échelle monde** (même projection et mêmes
tuiles que le kit gothique) ; matériaux du niveau 1 : pierre → `M_Pierre_Monde`, dalles → `M_Dalles_Monde` (UV monde dans le
shader), bois `M_Bois_Brut`, fer `M_FerRouille`, corde `M_Corde`, chiffon `M_Chiffon_Brule`. Modèles :
`Assets/Art/Models/Modules/SM_Mod_*.fbx` ; planche `docs/art/rendus/Modules/Planche_Kit_Modules.jpg`. Sans collider : à ajouter
dans les prefabs du jeu (boîtes simples). Repères Unity : +Y en haut, +Z vers l'avant.

| Pièce (`SM_Mod_…`) | Taille X × Y × Z (m) | Pivot | Triangles | Matériaux | Notes |
|---|---|---|---|---|---|
| `EscalierPierre_4m` | 3 × 4 × 6 | sol, centre de l'emprise ; monte vers +Z | 160 | pierre | 16 marches de 0.25 × 0.375 m, plein dessous |
| `EscalierBois_4m` | 2 × 4 (+0.19 sous le sol) × 5.2 | sol, centre ; monte vers +Z | 288 | bois | ouvert, limons, rampe côté +X |
| `Echelle_6m` | 0.67 × 6 × 0.06 | pied, au milieu | 328 | bois | barreaux tous les 0.3 m |
| `Passerelle_8m` | 2.5 × 1.46 × 8.1 | dessus du plancher (0), centre | 576 | bois | garde-corps de 1.1 m des deux côtés ; poutres sous le plancher (−0.36 m) |
| `PasserellePilotis_8m` | 2 × 3 × 8 | dessus du plancher (0), centre | 480 | bois | pieux jusqu'à −3 m (fond de citerne), sans garde-corps |
| `PontCordes_12m` | 1.8 × 2.4 × 12.2 | milieu de la ligne des appuis (dessus des planches aux bouts) | 1728 | bois, corde | flèche de 0.8 m, poteaux de 1.4 m ; cordes en aplat (M_Corde) |
| `Balcon_6m` | 6 × 2.7 × 2.5 | ras du mur, dessus de la dalle (0) ; avance vers +Z (le mur est en −Z) | 180 | dalles, pierre, bois | 3 consoles, garde-corps de bois |
| `PorteCellule` | 1.2 × 2.4 × 0.18 | bas de la charnière (côté −X) | 208 | fer | tourne autour de Y |
| `Cellule_4m` | 5 × 4.1 × 4.7 | sol, milieu de la façade (façade vers +Z, cellule vers −Z) | 430 | pierre, dalles, fer | repère `Porte_Charniere` (−0.6, 0, 0) pour `SM_Mod_PorteCellule` |
| `GrandePorte_4x10` | 10.4 × 12 × 1.9 | sol, milieu de l'ouverture | 344 | pierre | ouverture 4 × 10 m : **le traqueur (14 m) doit se faufiler** (porte_etroite) |
| `GrandePorte_6x10` | 12.4 × 12 × 1.9 | idem | 344 | pierre | ouverture 6 × 10 m : se faufile (porte_etroite) |
| `GrandePorte_6x12` | 12.4 × 14 × 1.9 | idem | 344 | pierre | ouverture 6 × 12 m : se baisse (porte_etroite) |
| `GrandePorte_8x14` | 14.4 × 16 × 1.9 | idem | 344 | pierre | ouverture 8 × 14 m : passe voûté |
| `GrandePorte_8x16` | 14.4 × 18 × 1.9 | idem | 344 | pierre | ouverture 8 × 16 m : passe debout |
| `ArcheBrisee_8m` | 9.5 × 14 × 3.5 | sol, milieu entre les piliers | 252 | pierre | piliers de 1.2 m, 9 m de haut, 8 m d'axe en axe ; départs d'arc, blocs tombés |
| `TuyauGros` | 1.4 × 2.1 × 3 | ras du mur, sur l'axe (le mur est en −Z) | 300 | fer | Ø 1.2 m, coude vers le bas ; repère `FX_Bouche` pour l'eau qui tombe |
| `RoueEau_8m` | 3.6 × 8.2 × 8.2 | centre de l'axe (tourne autour de X) | 1252 | bois, fer | 16 aubes ; matériaux sans UV monde (elle tourne) |
| `SolInonde_8m` | 8 × 0.8 × 8 | sol, centre | 208 | dalles, pierre | affaissé jusqu'à −0.5 m au centre ; poser l'eau (`M_Eau_Toon`) vers −0.2 m |
| `EscalierSpirale_45` | 5.3 × 4 × 8.5 | **centre du puits**, au pied du segment | 318 | pierre, bois | 45° de tour, rayon 9.5 à 12 m, monte de 2.5 m ; 8 segments = un tour de 20 m (tourner de 45° et monter de 2.5 m) |
| `Coursive_45` | 5.3 × 2.4 × 8.5 | **centre du puits**, dessus de la dalle | 306 | dalles, pierre, bois | anneau de 2.5 m de large, garde-corps côté vide ; 8 segments = un anneau complet |
| `ChaineGrimpe_12m` | 0.8 × 12.6 × 0.8 | **en haut** (point d'ancrage) | 1932 | fer | maillons de 0.5 m, fil de Ø 0.12 m |
| `ChaineTendue_16m` | 0.8 × 1.7 × 16.2 | milieu de la ligne des ancrages | 2824 | fer | flèche de 1.2 m |
| `PlateformeBond_6m` | 3 × 6 × 3 | sol, sous le centre ; dessus à 6 m | 34 | pierre, dalles | pilier de 2 m, dessus de 3 × 3 m |
| `Rebord_6m` | 6 × 1.4 × 1.2 | ras du mur, dessus du rebord (0) ; avance vers +Z | 60 | pierre | 1.2 m de profondeur, 3 consoles |
| `MurGrimpe_8x10` | 8 × 10 × 1.05 | sol, milieu de la face grimpable (face vers +Z) | 1408 | pierre, bois | prises saillantes de 0.25 à 0.45 m en quinconce, 4 racines |
| `Galerie_8m` | 8 × 3.3 × 3 | ras du mur, dessus de la dalle (0) ; avance vers +Z | 184 | dalles, pierre | parapet crénelé de 1.1 m côté vide, 4 consoles |
| `LitsSuperposes` | 2 × 1.9 × 0.9 | sol, centre | 160 | bois, chiffon | couchages à 0.4 et 1.4 m |
| `Ratelier` | 2.1 × 2.4 × 0.4 | sol, centre | 192 | bois, fer | 6 lances |
| `Table` | 2.4 × 0.8 × 2 | sol, centre | 132 | bois | plateau à 0.8 m, deux bancs |
| `Tonneau` | 0.74 × 1 × 0.72 | sol, centre | 252 | bois, fer | Ø 0.7 m |

**Déjà au catalogue (réutilisés, pas refaits)** : cages suspendues (`SM_Cage`), treuil (`SM_Treuil`), petite chaîne
(`SM_Chaine`), tuyau (`SM_Tuyau`), colonne brisée (`SM_ColonneBrisee`), échafaudage (`SM_Echafaudage`), grille et herse.

#### Éléments de passage (traqueur et joueur)

| Pièce | Type | Points d'accroche (repères vides du FBX, `Accroche_*`) | Usage |
|---|---|---|---|
| `SM_Mod_ChaineGrimpe_12m` | `chaine_grimpe` | `Accroche_00m` (ancrage) puis `Accroche_03m`, `_06m`, `_09m`, `_12m` (bas) le long de la chaîne | maillons assez gros pour le traqueur (main de ~1.5 m) ; le joueur s'y accroche avec la traction |
| `SM_Mod_ChaineTendue_16m` | `chaine_tendue` | `Accroche_A`, `Accroche_Milieu` (point bas, −1.2 m), `Accroche_B` | on s'y suspend et on avance main sur main ; le traqueur la franchit d'un bond |
| `SM_Mod_PlateformeBond_6m` | `plateforme_bond` | `Accroche_Dessus` (centre du dessus, 6 m) | espacer de 6 à 10 m pour les bonds du traqueur, 3 à 5 m pour le joueur |
| `SM_Mod_Rebord_6m` | `rebord` | `Accroche_Bord` (milieu de l'arête avant) | accroche le long de toute l'arête avant (x de −3 à 3 m) |
| `SM_Mod_MurGrimpe_8x10` | `mur_grimpe` | `Accroche_Pied` et `Accroche_Sommet` (au milieu, 0.5 m devant la face) | toute la face avant est grimpable ; prises visibles |
| `SM_Mod_Galerie_8m` | `rebord` | `Accroche_Bord` (haut du parapet côté vide) | corniche haute : on s'accroche au parapet |
| `SM_Mod_Echelle_6m` | `chaine_grimpe` | `Accroche_Bas`, `Accroche_Haut` | échelle pour le joueur ; le traqueur l'ignore (trop étroite) |
| `SM_Mod_PontCordes_12m` | `rebord` | `Accroche_Bout_A`, `Accroche_Bout_B` | les cordes latérales se saisissent sur toute la longueur |
| `SM_Mod_GrandePorte_4x10` | `porte_etroite` | milieu de l'ouverture au sol (pivot) | le traqueur passe à quatre pattes (ouverture < sa hauteur voûtée) |
| `SM_Mod_GrandePorte_6x10` | `porte_etroite` | idem | idem |
| `SM_Mod_GrandePorte_6x12` | `porte_etroite` | idem | passe voûté, tête baissée |

#### Textures et images qui manquent (à fournir par l'utilisateur)

- **Pierre brute de paroi grimpable** (tuile 4 × 3 m, joints irréguliers, prises marquées) : le mur grimpable utilise la pierre
  du couloir ; une tuile dédiée rendrait la face lisible comme « grimpable » à pleine vitesse.
- **Lierre et racines** (feuillage en découpe alpha, tuile 1 × 2 m) : les racines du mur sont des tubes de bois sans feuilles.
- **Planches de bois usées** (tuile 2 × 1 m, planches en long avec clous) pour les passerelles et le pont : `T_Bois_Brut` est un
  bois générique.
- **Corde tressée** (tuile 0.25 × 1 m) : la corde est un aplat (`M_Corde`).
- **Mousse et dépôts d'eau** (tuile ou masque) pour les pieds de piliers et les sols inondés de la citerne.
- **Bois de la roue à eau** mouillé et sombre (variante de `T_Bois_Brut`).

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

## Nuit 4, lot 4 : props en 3D (2026-11-23, branche `art-props-3d`)

Huit props encore en primitives greybox deviennent de vrais modèles. Chaîne : image concept dans la DA (Higgsfield,
Nano Banana 2.1, 2K) → **Tripo H3.1** image vers 3D (texture, sans PBR) → nettoyage par script dans Blender 5.0
(`Assets/Art/Source~/Props/scripts/nettoyer_prop.py`, réglages relevés dans `docs/art/props/sources/reglages_props.json`,
un rapport `rapport_<Nom>.json` par prop). Sources (images, GLB, journal des dépenses : 88 crédits) dans
`docs/art/props/sources/`. Contrôle : `docs/art/rendus/Nuit4/Lot4/Planche_Props.jpg` (FBX réimporté, mannequin de 1.8 m,
coffres couvercle ouvert à 70°).

Conventions communes : FBX dans `Assets/Art/Models/Props/SM_Prop_<Nom>.fbx` (réglages d'export de `kitlib.export` :
**+Y en haut, +Z en avant dans Unity**, échelle 1, 1 unité = 1 m), pivot **au sol, au centre de l'emprise** ; une texture
de base `Assets/Art/Textures/Props/T_Prop_<Nom>_BaseColor.png` par prop (couleurs peintes par Tripo : un peu d'ombre
douce est restée dans la texture, à surveiller avec le shader toon) ; un matériau par prop `M_Prop_<Nom>` (à refaire
dans Unity avec `ItHunts/Toon`). Aucun collider dans les FBX.

**Deuxième passe (branche `art-props-3d-b`)** : ce tableau est à jour (triangles LOD0 / LOD1, textures 1024, poutre refaite,
coffres creusés) ; les nouveaux props sont au § « Deuxième passe » plus bas. Chaque FBX contient maintenant
`SM_Prop_<Nom>_LOD0` et `SM_Prop_<Nom>_LOD1` (à 50 %) ; `Props4.Construit` en fait un `LODGroup` sur la racine du prefab.

| Prop | Fichier | Dimensions l × p × h (m) | Pivot | Triangles LOD0 / LOD1 | Texture | Remplace (greybox) | Réglage dans le prefab |
|---|---|---|---|---|---|---|---|
| Tonneau | `SM_Prop_Tonneau.fbx` | 0.6 × 0.6 × 0.9 | sol, centre | 3 667 / 1 833 | 1024 | `Prop_Tonneau_Attrapable` (0.6 × 0.9 × 0.6) et `Prop_Tonneau` (0.9 × 1.1 × 0.9) | attrapable : échelle 1, pivot de la greybox au centre → modèle à (0, −0.45, 0) si la greybox est centrée ; fixe : échelle (1.5, 1.22, 1.5) |
| Chaise | `SM_Prop_Chaise.fbx` | 0.45 × 0.45 × 1.0 | sol, centre | 1 959 / 979 (décimée de 3 511) | 1024 | `Prop_Chaise_Attrapable` (0.55 × 1 × 0.55) | échelle 1 : tient dans la boîte de collision de la greybox (garder sa boîte, son Rigidbody, `Grabbable` et la lueur) |
| Poutre tombée (**refaite**, v02) | `SM_Prop_Poutre.fbx` | 3.97 × 0.58 × 0.50 (ferrure comprise ; bout cassé et éclaté à une extrémité) | sol, centre | 2 952 / 1 476 | 1024 | `Prop_Poutre` (6 × 0.5 × 0.5) quand elle est posée au sol | longueur sur X ; **échelle uniforme 1** (section ≈ celle de la greybox, 4 m au lieu de 6 : la greybox n'est pas utilisée dans le niveau, 0 instance). La poutre suspendue à crochet reste `SM_Poutre` (§ 2) |
| Sarcophage | `SM_Prop_Sarcophage.fbx` | 2.2 × 0.9 × 0.8 | sol, centre | 5 879 / 2 939 (décimé de 6 513) | 1024 | `Prop_Sarcophage` (1.2 × 1 × 2.5, long sur Z) | longueur sur X : rotation (0, 90, 0) ; échelle (1.14, 1.25, 1.33) pour remplir la boîte, ou échelle 1 |
| Râtelier d'armes | `SM_Prop_Ratelier.fbx` | 1.6 × 0.5 × 1.8 | sol, centre | 5 670 / 2 834 | 1024 | `Prop_Ratelier` (2.6 × 2.1 × 0.5) | échelle (1.625, 1.17, 1) ; armes soudées au râtelier (un seul objet) |
| Coffre de récompense | `SM_Prop_Coffre.fbx` + vide **`Couvercle`** (enfants `Couvercle_LOD0`, `Couvercle_LOD1`) | 0.9 × 0.55 × 0.6 (corps 0.38 de haut) | corps : sol, centre ; `Couvercle` : **axe de la charnière arrière**, (0, 0.381, −0.262) dans le repère du coffre | 4 963 / 2 616 (couvercle 1466 / 748 ; fond creusé compris) | 1024 + `M_Prop_Coffre_Interieur` (rebord, parois et fond intérieurs, sombre) | coffre greybox « Economy_Chest » de `TestArenaSceneBuilder.BuildChest` (corps 1.4 × 0.8 × 0.9, couvercle 1.5 × 0.2 × 1, collider 1.4 × 0.9 × 0.9) | échelle 1.5 pour garder l'encombrement de la greybox (1.35 × 0.83 × 0.9) ; brancher `Chest.lid` sur l'enfant `Couvercle`. Ouverture : rotation locale **X ≈ −70°** (le bord avant monte et part vers l'arrière) |
| Coffre de salle verrouillée | `SM_Prop_CoffreVerrou.fbx` + vide **`Couvercle`** (enfants `Couvercle_LOD0`, `Couvercle_LOD1`) | 0.9 × 0.55 × 0.6 (corps 0.42 de haut) | corps : sol, centre ; `Couvercle` : axe de la charnière, (0, 0.42, −0.274) | 5 937 / 3 021 (couvercle 1934 ; décimé de 6 491, fond creusé compris) | 1024 + `M_Prop_CoffreVerrou_Interieur` | coffre de la salle verrouillée (`G_SalleVerrouillee`, `LockedRoomSpot`) | même règle ; le cadenas et sa chaîne sont sur le corps (ils restent en place à l'ouverture) |
| Statue gothique | `SM_Prop_Statue.fbx` | 0.76 × 0.75 × 2.6 (échelle uniforme réglée sur la hauteur) | sol, centre | 5 880 / 2 940 (décimée de 6 646) | 1024 | `Prop_Statue` (1.4 × 3.6 × 1.4) | échelle uniforme 1.385 pour la hauteur de 3.6 m (socle 1.05 m de côté) |

Remarques :

- **Couvercle des coffres.** Aujourd'hui `Chest` cache les renderers du couvercle pour le joueur qui l'a ouvert ; avec le
  pivot sur la charnière on peut animer l'ouverture (rotation X de 0 à −70° du vide `Couvercle`, qui porte les deux LOD
  du couvercle, par caméra comme le masquage actuel). **Deuxième passe** : le corps est **creusé d'un fond intérieur de
  0.25 m** (rebord de 5 cm, parois et fond en `*_Interieur`), fond à 0.13 m (coffre) ou 0.17 m (coffre verrouillé) du sol ;
  l'ouverture du couvercle est refermée par une face plane (rectangle de deux triangles et rebord triangulé en Delaunay
  contraint, plus de remplissage en éventail). Le moraillon n'est pas coupé : sa plaque haute et sa charnière sont sur le
  couvercle, la plaque de serrure sur le corps, la découpe passe à leur jointure. Une lueur dorée de butin (ART-HANDOFF :
  coffres en doré / orangé) se pose bien dans le creux.
- **Déformations dues à la mise aux cotes** (rapport : `echelle_appliquee`, `etirement_max`) : la poutre v01 étirée de
  × 3.1 est remplacée (v02, étirement × 1.3 au plus) ; restent de la première passe le sarcophage et le râtelier (rapport
  × 1.73 entre leurs axes : gisant plus long et plus plat que sur l'image) et le coffre verrouillé (× 1.42), gardés tels
  quels pour ne pas changer leurs cotes déjà intégrées ; les autres restent sous × 1.25.
- **Pièces de kit à la place de primitives** (aucun nouveau modèle) : `Prop_MurEffondre` → `SM_Cave_MurBrise_*` ;
  `Prop_Gravats` → `SM_Cave_Eboulis_*` ; caisses (`Prop_Caisse`, `Prop_Caisse_Attrapable`, `Prop_PileDeCaisses`) → modèle de
  caisse existant `SM_Caisse` (§ 4 bis).
- **Fait dans Unity (lot 4, branche `art-props-3d`)** avec `Assets/Art/Source~/Props/scripts/Props4.cs` (`Props4.Construit`) :
  - **Textures éclaircies** pour la lecture en jeu (gamma 0,8 puis gain 1,08, script `eclaircir_props.py`) ; les ombres peintes par Tripo s'ouvrent, les teintes restent.
  - **Matériaux toon** `Assets/Art/Materials/Props/M_Prop_<Nom>.mat`, copiés de `M_Caisse` (`ItHunts/Toon`, UV du modèle). S'y ajoutent `M_Prop_Coffre_Interieur` et `M_Prop_CoffreVerrou_Interieur` : **toon copiés de `M_Caisse`** eux aussi, sans texture, couleur de base très sombre (0.035, 0.028, 0.024), donc éclairés comme le reste (rebord, parois et fond intérieurs).
  - **Lueur dorée des coffres** (« tout ce qui est interactif brille ») : rim chaud du shader toon, HDR (1, 0.62, 0.25) × 1,6, largeur 0,22, intensité 0,65. Pas de masque d'émission : l'atlas de Tripo mêle le laiton et le bois clair.
  - **Prefabs** `Assets/Art/Prefabs/Props/P_Prop_<Nom>.prefab` : modèle, matériaux et colliders simples. Une boîte pour la plupart ; boîte de socle et capsule pour la statue ; **une seule boîte** pour le râtelier (montants, barre et armes, 60 % de la profondeur) ; pour chaque coffre, une boîte sur la racine à la taille du coffre fermé : **elle ne couvre pas le couvercle ouvert** (aucun collider sur `Couvercle`, la boîte ne suit pas l'ouverture). Pas de Rigidbody, de `Grabbable` ni de lueur : c'est main qui les ajoute en remplaçant le maillage des greybox, avec les réglages du tableau.
  - **Captures en jeu** (actes I et II, seed 4242), greybox remplacée sur place avec ces réglages : `docs/art/rendus/Nuit4/Lot4/Planche_Jeu_Acte1.jpg`, `Planche_Jeu_Acte2.jpg` et les images de `Jeu/`. Les tailles mesurées sont dans `Jeu/points_Acte*.txt`.
  - **Écarts relevés.** Dans l'acte II, un `Prop_Ratelier` greybox mesure 0,6 × 1,4 × 0,48 m, et non 2,6 × 2,1 × 0,5 : l'échelle fixe du tableau le grossit. Pour main : régler l'échelle d'après la boîte de la greybox de chaque instance (hauteur et emprise), pas une échelle fixe par type. Même remarque pour `Prop_Tonneau` de l'acte I (groupe de 3,1 × 0,95 × 1,2 m).
  - **Éléments de main sur les captures.** Les étiquettes « Coffre / 30 âmes » (police de main) et la lueur violette des attrapables sont du ressort de main ; les étiquettes sont masquées sur les captures.

### Deuxième passe (2026-11-23, branche `art-props-3d-b`)

Neuf props de plus (par ordre d'usage dans le niveau) et la poutre refaite ; LOD1 ajoutés aux huit props de la première
passe (tableau ci-dessus mis à jour), textures toutes en 1024, coffres creusés. Même chaîne : concept Nano Banana 2.1 en
2K (deux concepts du lot 4 en référence de style), Tripo H3.1, `nettoyer_prop.py` ; la paille est modélisée par script
(`paille.py`, texture Nano Banana 2.1). Dépense : voir `docs/art/props/sources/JOURNAL-DEPENSES.md`.

Nouveautés de la chaîne :

- **Étirement non uniforme limité à × 1,3** (rapport entre la plus grande et la plus petite échelle des axes) : quand Tripo
  donne d'autres proportions que la greybox, le modèle s'en rapproche sans dépasser cette limite (la hauteur compte double ;
  pour la poutre, la longueur). Écart aux cotes visées : colonne « Dimensions » contre « Remplace ».
- **LOD1 à 50 %** (`SM_Prop_<Nom>_LOD1`, décimation par fusion d'arêtes), sauf la paille. `Props4.Lods` crée un seul
  `LODGroup` sur la racine du prefab : LOD0 jusqu'à 25 % de hauteur d'écran, LOD1 jusqu'à 1,5 %, puis rien.
- **Contrôles** : faces superposées corrigées à la source (`faces_superposees.nettoie`, copie du module des kits), puis
  contrôle de chaque maillage exporté (sommets isolés, arêtes nulles, faces dégénérées, faces superposées : **0 partout**,
  `docs/art/rendus/Nuit4/Lot4/controle_<Nom>.json`). Les arêtes non manifold restantes (0 à 41 par prop) viennent des
  maillages de Tripo (pièces soudées en T) : sans effet sur le rendu ni sur les colliders (boîtes).
- **Rendus** : `docs/art/rendus/Nuit4/Lot4/Planche_Props_2.jpg` (FBX réimportés, silhouette humaine de 1.80 m, éclairage
  sombre et chaud, vignette du LOD1) et les images de `Props2/` ; cotes mesurées sur les FBX réimportés dans
  `Props2/mesures_fbx.json`.

| Prop | Fichier | Dimensions l × p × h (m) | Pivot | Triangles LOD0 / LOD1 | Étirement | Remplace (greybox, l × h × p) | Réglage dans le prefab (échelles **uniformes** seulement) |
|---|---|---|---|---|---|---|---|
| Lanterne | `SM_Prop_Lanterne.fbx` | 0.39 × 0.40 × 0.80 | sol, centre | 1 470 / 734 | × 1.00 | `Prop_Lanterne` (0.5 × 0.8 × 0.5), posée ou suspendue | échelle **1** ; aucun collider ; vide **`Feu`** à (0, 0.288, 0) (lumière ponctuelle chaude, ombres coupées : les vitres sont opaques) et **`Accroche`** à (0, 0.8, 0) en haut de l'anneau : suspendue, placer `Accroche` au point d'attache (modèle à −0.8 m sous lui). Émission des vitres et de la bougie : `T_Prop_Lanterne_Emission` (masque), couleur HDR (1, 0.55, 0.2) × 2.2 |
| Étagère | `SM_Prop_Etagere.fbx` | 2.52 × 0.81 × 3.03 | sol, centre | 5 389 / 2 693 | × 1.30 | `Prop_Etagere` (2.6 × 3.2 × 0.7) | échelle uniforme **1** (ou 1.05 pour toucher 3.2 m) ; une boîte ; 11 cm plus profonde que la greybox : la reculer de 5 cm contre le mur ; pots et livres soudés |
| Coffre décoratif | `SM_Prop_CoffreDeco.fbx` | 1.20 × 0.80 × 0.80 | sol, centre | 2 939 / 1 469 | × 1.16 | `Prop_CoffreDeco` (1.2 × 0.8 × 0.8) | échelle **1** ; une boîte ; fermé, bois très sombre, ferrures noires, couvercle plat : **sans rim doré** ni lueur (il ne doit pas passer pour un coffre de récompense) |
| Table | `SM_Prop_Table.fbx` | 2.60 × 1.20 × 0.95 | sol, centre | 2 940 / 1 470 | × 1.11 | `Prop_Table` (2.6 × 0.95 × 1.2) | échelle **1** ; une boîte (ou plateau + deux tréteaux si on veut glisser dessous) |
| Banc | `SM_Prop_Banc.fbx` | 2.48 × 0.60 × 0.56 | sol, centre | 1 958 / 978 | × 1.30 | `Prop_Banc` (2.6 × 0.55 × 0.6) | échelle **1** ; une boîte |
| Paille (litière) | `SM_Prop_Paille.fbx` | 2.00 × 1.93 × 0.27 | sol, centre | 1 080 (sans LOD1) | × 1 (modélisée) | `Prop_Paille` (2 × 0.3 × 2) | échelle **1** ; **aucun collider**, pas d'ombre portée utile ; nappe à 3 mm du sol (pas de z-fighting) ; sans LOD1 |
| Armoire | `SM_Prop_Armoire.fbx` | 1.60 × 0.85 × 2.62 | sol, centre | 4 209 / 2 103 | × 1.30 | `Prop_Armoire` (1.6 × 2.7 × 0.8) | échelle uniforme **1** (ou 1.03 pour 2.7 m) ; une boîte ; portes fermées (un seul maillage) |
| Lit de cachot | `SM_Prop_Lit.fbx` | 1.30 × 2.24 × 0.71 | sol, centre | 3 920 / 1 960 | × 1.30 | `Prop_Lit` (1.3 × 0.7 × 2.3, long sur Z) | échelle **1** ; longueur sur Z (avant = pied du lit) ; une boîte |
| Chaudron | `SM_Prop_Chaudron.fbx` | 1.30 × 1.30 × 1.00 | sol, centre | 3 430 / 1 714 | × 1.00 | `Prop_Chaudron` (1.3 × 1 × 1.3) | échelle **1** ; une boîte (ou capsule) ; rempli d'un liquide sombre (pas de creux) |
| Poutre tombée (v02) | `SM_Prop_Poutre.fbx` | 3.97 × 0.58 × 0.50 | sol, centre | 2 952 / 1 476 | × 1.30 | `Prop_Poutre` (6 × 0.5 × 0.5), posée au sol | échelle **1** (voir le tableau du lot 4) ; une boîte |

Dans Unity (session art, avant que main remplace les greybox) : **relancer `Props4.Construit`** (copier `Props4.cs` dans un dossier Editor) :
les FBX de la première passe ont changé de hiérarchie (`SM_Prop_<Nom>` est devenu `SM_Prop_<Nom>_LOD0` + `_LOD1` ; le
`Couvercle` des coffres est un vide sur la charnière qui porte `Couvercle_LOD0` et `Couvercle_LOD1`), les prefabs
`P_Prop_*` sont reconstruits au même chemin (mêmes GUID) et les neuf nouveaux sont créés. `Chest.lid` se branche sur le
vide `Couvercle`. Unity crée les `.meta` des nouveaux FBX et textures à l'import.

### Intégration Unity, deuxième passe (lot 4, relecture) — **ce tableau et ces règles font foi**

Les échelles du premier tableau (dont les échelles non uniformes) et la planche `Planche_Props.jpg` (rendu Blender de la
première passe, poutre v01) sont **historiques** ; la référence est `Planche_Props_2.jpg` (Blender) et surtout les captures
en jeu `Planche_Jeu_Acte1.jpg`, `Planche_Jeu_Acte2.jpg`, `Planche_Jeu_Acte3.jpg`.

- **Prefabs** (`Props4.Construit`, `Assets/Art/Source~/Props/scripts/Props4.cs`) : 17 `P_Prop_<Nom>` (mêmes chemins, mêmes GUID
  pour les 8 premiers) + 3 **variantes assemblées** sans génération : `P_Prop_Tonneaux_Couches` (4 tonneaux couchés en rang,
  2,7 × 0,63 × 1,6 m), `P_Prop_Table_Banquet` (2 tables bout à bout, 6 × 1,09 × 1,38 m), `P_Prop_Banc_Eglise` (2 bancs, 4 ×
  0,46 × 0,49 m). Matériaux toon, **LODGroup** (LOD0 jusqu'à 25 % de l'écran, LOD1 jusqu'à 1,5 %), colliders simples (aucun pour
  la lanterne et la paille ; la paille n'a pas de LOD1), lumière chaude sur le vide `Feu` de la lanterne (1,5 ; 6 m) et vitres
  émissives.
- **Lueur des coffres** (`M_Prop_Coffre`, `M_Prop_CoffreVerrou`) : émission chaude modulée par la texture peinte (HDR (1 ; 0,58 ;
  0,18) × 1,1 et × 0,9), le laiton et le bois clair brillent : le coffre se lit dans le noir à distance de jeu, même sans la
  lueur de main. Le coffre décoratif ne brille pas (ce n'est pas un objet interactif).
- **Attrapables** (tonneau, chaise) : la lueur reste celle de main (`EmissionGlow.Set(renderer, couleur, wholeModel: true)`,
  qui marche sur ItHunts/Toon) ; les captures « après » la posent sur le modèle (couleur de prise à portée), comme en jeu.
  Échelle 1 : le modèle tient dans la boîte physique de la greybox.
- **Règle de pose par instance (pour main)** : pour chaque greybox, choisir parmi le modèle et ses variantes (tonneau → rang
  couché ; table → banquet ; banc → banc d'église) celui dont les proportions sont les plus proches, tourner de 90° si l'axe
  long diffère, échelle **uniforme** = moyenne géométrique des rapports d'axes (coffres ≤ × 1,6 ; attrapables × 1), posé au
  centre de la boîte, au sol. C'est la fonction `Props4.Ajuste` (écart de forme = somme des |log| des rapports restants ;
  au-delà de 0,9 l'instance n'a pas de modèle adapté).
- **Couverture mesurée** (seed 4242, `Jeu/points_Acte*.txt`) : actes I et III, toutes les instances ont un modèle ou une
  variante adaptés ; acte II, 15 instances restent sans modèle : `Prop_Armoire` en plaque de 3,9 × 0,7 × 3,9 m (armoire couchée :
  utiliser la pièce de kit `Chateau_Armoire_Tombee`) et `Prop_Ratelier` en petit support de 0,6 × 1,4 × 0,5 m (garder la
  greybox ou poser un `P_Prop_Tonneau` / caisse ; un petit présentoir est à modéliser plus tard).
- **Lanterne et lit** : aucune instance dans les trois actes à la seed 4242 (ils existent dans le catalogue de main) ; rendus de
  contrôle dans `Planche_Props_2.jpg`.
- **Étirements résiduels** de la première passe (rapports `rapport_*.json`) : sarcophage et râtelier × 1,73, coffre verrouillé
  × 1,42 (gisant un peu aplati) ; les nouveaux props restent sous × 1,3. À reprendre dans `nettoyer_prop.py` si l'utilisateur
  le demande (sans nouvelle génération).
- **Densité de texels** : textures en 1024 ; ≈ 400 px/m pour l'étagère (2,5 m) et la table, ≈ 280 px/m pour la statue à 3,6 m
  (sous la référence de 512 px/m du lot 2 ; acceptable pour des props vus de 3 à 20 m, à passer en 2048 si l'on veut des gros
  plans).
- **Journal Unity** : `docs/art/rendus/Nuit4/Lot4/journal_unity.txt` (aucune erreur ni aucun avertissement venant des assets du
  lot 4).

## Salles d'exemple des trois actes (lot 5, nuit du 2026-11-22, PR art-salles-exemple)

Onze salles entièrement habillées et éclairées, assemblées avec les kits des lots 2 à 4, prêtes à servir de modèles. Prefabs dans `Assets/Art/Levels/Exemples/<Acte>/<Nom>.prefab` (pièces des kits avec collisions, blocs de sol et de plafond, lumières) ; scènes de contrôle `Assets/Art/Scenes/Exemples_<Acte>.unity` (éclairage de l'acte) ; captures dans `docs/art/rendus/Exemples/<Acte>/`. Les plans sont générés par `Assets/Art/Source~/Kits/scripts/salles.py` (texte lisible dans `Assets/Art/Source~/Kits/plans/`), assemblés par `SalleBuilder.cs`. **Pour `main` :** y ajouter le gameplay (points d'apparition, vagues, coffres, portes de module, NavMesh) ; les repères `Accroche_*` des pièces marquent les points de traction et les rebords. Coordonnées dans le repère du prefab (m), +Z vers le nord.

### Chateau_GrandHall (Château, 32 × 24 m, voûtes à 19–24 m)

- **Identité :** Grand hall de réception abandonné : nef à piliers, trois rayons de lune qui tombent des hautes fenêtres nord sur le marbre, tables renversées d'un banquet interrompu.
- **Fonction :** Salle d'arène de l'acte II (vagues) et hub vers les galeries.
- **Entrées et sorties :** Arcade sud-ouest (6 × 10 m, x = -12) ; galerie est à 8 m (sortie haute vers les étages).
- **Routes de parcours :** Course sur le tapis central → saut sur la table renversée → traction sur un pilier (accroches à 12 m) → galerie est à 8 m ; ou grand escalier du bas-côté sud (monte vers l'est jusqu'à la galerie). Bunny hop dans la nef de 32 m.
- **Points de grab et de traction :** Piliers (`Accroche_Traction` à 12 m), balustrade de la galerie, chandeliers (lustres que le jeu peut faire tomber).
- **Moment de tension :** Les rayons de lune sont les seuls endroits éclairés : on y voit le traqueur entrer par l'arcade avant qu'il ne replonge dans le noir des bas-côtés.
- **Captures :** `docs/art/rendus/Exemples/Chateau/Chateau_GrandHall_*.jpg`

### Chateau_SalleBibliotheque (Château, 24 × 16 m, 12 m de haut, galerie à 6 m)

- **Identité :** Bibliothèque silencieuse : rayonnages du sol au plafond, galerie de lecture, livres jetés au sol, un lutrin encore ouvert.
- **Fonction :** Salle de transition verticale : on y monte vers les étages ; petite vague dans les allées.
- **Entrées et sorties :** Porte basse sud (arcade réduite, 4 × 8.5 m) ; sortie haute par la galerie nord sous la fenêtre (6 m).
- **Routes de parcours :** Wall jump entre deux rayonnages qui se font face (coins nord-est et nord-ouest, 5.5 m d'écart) jusqu'à la galerie ; course sur la galerie en U (48 m) ; descente en glissade sur la table.
- **Points de grab et de traction :** Lustre (centre, 12 m), balustrades de la galerie.
- **Moment de tension :** Allées étroites entre les rayonnages : on n'y voit pas venir ce qui tourne le coin ; le lustre grince au-dessus de la seule zone éclairée.
- **Captures :** `docs/art/rendus/Exemples/Chateau/Chateau_SalleBibliotheque_*.jpg`

### Chateau_Chapelle (Château, 22 × 30 m, 20 m sous les fenêtres)

- **Identité :** Chapelle profanée : rosace au fond, deux rayons de lune croisés sur les bancs, cierges encore allumés sur l'autel.
- **Fonction :** Salle de tension avant le boss de l'acte II, ou salle à coffre (sur l'autel).
- **Entrées et sorties :** Arcade ouest-sud (porte de 6 × 10 m) ; sortie derrière l'autel (à percer dans le mur nord par le jeu).
- **Routes de parcours :** Bunny hop dans l'allée centrale (24 m) ; bancs = couverts bas et marches de saut ; colonnes de 12 m en quinconce pour la traction ; rosace = repère visible depuis l'entrée.
- **Points de grab et de traction :** Colonnes (sommet à 12 m), rebord de l'autel, tentures (décor).
- **Moment de tension :** Bancs alignés comme des rangées de silhouettes ; seul l'autel est éclairé, on le voit de loin et on sait qu'il faut y aller.
- **Captures :** `docs/art/rendus/Exemples/Chateau/Chateau_Chapelle_*.jpg`

### Chateau_SalleDeBal (Château, 36 × 24 m, 20 m sous plafond, galeries à 8 m)

- **Identité :** Salle de bal figée au milieu de la fête : piste vide en damier sous quatre rayons de lune, tables et chaises renversées tout autour, estrade des musiciens abandonnée.
- **Fonction :** Arène principale de l'acte II (vagues) : piste centrale dégagée, bords encombrés.
- **Entrées et sorties :** Arcade sud (x = -4) ; galeries est et ouest à 8 m (sorties hautes).
- **Routes de parcours :** Bunny hop en rond sur la piste (cercle de 20 m) ; traction vers les lustres (pivot à 19 m, couronne à 12 m) puis lâcher vers une galerie ; estrade = tremplin.
- **Points de grab et de traction :** Lustres (3), balustrades des galeries, estrade.
- **Moment de tension :** Les rayons de lune découpent la piste en bandes claires : ce qui la traverse est visible une seconde, puis disparaît.
- **Captures :** `docs/art/rendus/Exemples/Chateau/Chateau_SalleDeBal_*.jpg`

### Cave_Cachots (Caves, 40 × 20 m, voûte à 9 m)

- **Identité :** Cachots du château : douze cellules alignées, deux forcées, une partie de dés interrompue à la table des gardes.
- **Fonction :** Couloir de tension : goules dans les cellules ouvertes, embuscades latérales.
- **Entrées et sorties :** Bout ouest de la galerie (6 m de large) ; bout est par la salle des gardes.
- **Routes de parcours :** Course droite de 40 m (bunny hop) ; saut sur la table des gardes ; les grilles servent de murs de wall jump (5 m) pour monter sur le linteau des cellules et courir dessus.
- **Points de grab et de traction :** Linteaux de pierre des cellules (5.6 m), brasero (repère lumineux).
- **Moment de tension :** Les deux cellules ouvertes : on passe devant le noir de chacune sans savoir si elle est vide.
- **Captures :** `docs/art/rendus/Exemples/Caves/Cave_Cachots_*.jpg`

### Cave_SalleDeGarde (Caves, 28 × 20 m, voûte à 10 m)

- **Identité :** Poste de garde à moitié effondré : la roche a percé la maçonnerie, le repas des gardes est resté sur la table, la réserve est derrière le mur écroulé.
- **Fonction :** Salle de combat courte (goules) et salle à coffre (dans la réserve).
- **Entrées et sorties :** Arche basse sud-ouest ; brèches des murs brisés au sud (2 à 4 m de haut, on saute par-dessus).
- **Routes de parcours :** Saut par-dessus les murs brisés (crêtes de 4 à 8 m : wall jump sur les tronçons hauts) ; table et bancs comme marches ; contournement de la réserve par le mur à moitié écroulé.
- **Points de grab et de traction :** Crêtes des murs brisés, brasero (repère).
- **Moment de tension :** La réserve n'est visible qu'en passant le mur écroulé : coffre ou goule, on ne le sait qu'au dernier moment.
- **Captures :** `docs/art/rendus/Exemples/Caves/Cave_SalleDeGarde_*.jpg`

### Cave_Citerne (Caves, 54 × 44 m, voûte à 19 m)

- **Identité :** Ancienne citerne devenue caverne : un canal noir la coupe en deux, un pont de pierre l'enjambe, une passerelle pourrie franchit un gouffre.
- **Fonction :** Salle de parcours de l'acte I : le sol est coupé par l'eau et le vide, il faut choisir sa route.
- **Entrées et sorties :** Brèche sud-est (paroi manquante, 12 m) ; sortie haute à l'ouest par les rebords (8.5 m).
- **Routes de parcours :** Pont de pierre (bunny hop par-dessus le canal) ; passerelle de bois (deux planches manquent : saut) ; plateformes de roche (dessus à 4 m et 6 m) puis rebords à 4.5 et 8.5 m contre la paroi ouest (wall jump) ; pilier de roche = traction pour couper le canal.
- **Points de grab et de traction :** Pilier de roche (Accroche_Traction à 9 m), rebords (Accroche_Rebord), parapets du pont.
- **Moment de tension :** Le gouffre sous la passerelle : on entend l'eau tomber, la planche manquante se voit au dernier moment.
- **Captures :** `docs/art/rendus/Exemples/Caves/Cave_Citerne_*.jpg`

### Cave_CavernePuits (Caves, Ø 44 m, voûte à 18 m)

- **Identité :** Caverne ronde autour d'un vieux puits, réserve pillée dans une alcôve, et au nord la grande porte de bois qui ferme l'acte.
- **Fonction :** Salle de fin de l'acte I (sortie) et hub des galeries.
- **Entrées et sorties :** Arche haute sud (8 × 11 m, le traqueur passe) ; arche basse ouest (joueur seul) ; porte de sortie nord (12 × 13 m).
- **Routes de parcours :** Tour complet de la caverne en course (140 m) ; plateforme de roche → corniche de bois (6 m) → saut vers l'arche en ruine ; le puits est un obstacle central (on le contourne en glissade).
- **Points de grab et de traction :** Traverse du puits (3.8 m), arche en ruine, corniche de bois.
- **Moment de tension :** La porte de sortie est en vue dès l'entrée, éclairée par deux candélabres : il faut traverser toute la caverne pour l'atteindre.
- **Captures :** `docs/art/rendus/Exemples/Caves/Cave_CavernePuits_*.jpg`

### Jardin_AlleeDesStatues (Jardin, 80 × 64 m, ciel ouvert)

- **Identité :** Jardin à la française retourné à la sauvagerie : allée de statues voilées sous la lune, fontaine croupie, cimetière du domaine.
- **Fonction :** Première salle de l'acte III : traversée ouverte, vagues d'ennemis dans les parcelles.
- **Entrées et sorties :** Grande porte du château (sud, 9 × 13 m) ; sortie vers l'arène par la brèche de la falaise au nord (x = 0), où se raccorde l'escalier de descente de l'arène.
- **Routes de parcours :** Course sur l'allée de gravier (60 m, bunny hop) ; wall jump entre les deux gros arbres du sud-ouest (5 à 6 m d'écart) puis traction sur une statue ; toit du kiosque par traction ; serre traversée en glissade.
- **Points de grab et de traction :** Statues (Accroche_Traction), arbres (Accroche_Traction à mi-tronc), toit du kiosque (Accroche_Traction).
- **Moment de tension :** La brume cache le sol et les goules qui rampent ; les statues voilées ressemblent à des silhouettes qui attendent : on ne sait pas laquelle va bouger.
- **Captures :** `docs/art/rendus/Exemples/Jardin/Jardin_AlleeDesStatues_*.jpg`

### Jardin_Labyrinthe (Jardin, 48 × 48 m, haies de 2.6 m)

- **Identité :** Labyrinthe de haies devenu sauvage, kiosque au centre, statues voilées aux détours, brume jusqu'aux genoux.
- **Fonction :** Salle de traque : le traqueur y entre aussi (les haies ne l'arrêtent pas visuellement mais le cachent) ; objectif au centre.
- **Entrées et sorties :** Portail sud (x = -3) ; sortie nord (x = 3).
- **Routes de parcours :** Couloirs de 6 m (le chemin le plus court fait environ 70 m) ; double saut sur une haie (2.6 m < 2.9 m) pour couper ; toit du kiosque par traction : vue sur tout le labyrinthe ; arbres morts aux angles pour remonter.
- **Points de grab et de traction :** Kiosque (Accroche_Traction à 5.2 m), arbres des angles, statues.
- **Moment de tension :** On n'entend que la brume et ses propres pas ; au détour d'une haie, une statue voilée, ou autre chose.
- **Captures :** `docs/art/rendus/Exemples/Jardin/Jardin_Labyrinthe_*.jpg`

### Arene_Boss (Jardin, 64 × 64 m, 8 m sous le jardin)

- **Identité :** Cuvette de falaises au pied du domaine, ancien théâtre de verdure : piliers brisés, deux statues géantes, la lune juste au-dessus.
- **Fonction :** Arène du boss : terrain dégagé avec relief, accroches de traction, couverts.
- **Entrées et sorties :** Escalier de descente au sud (12 m de large, depuis le jardin) ; aucune autre sortie avant la mort du boss.
- **Routes de parcours :** Boucle complète autour des buttes (bunny hop sur 60 m) ; traction de pilier en pilier (16 à 20 m d'écart) ; statues géantes (accroche à 9 m) ; glissade sur les pentes des buttes.
- **Points de grab et de traction :** 6 piliers brisés (Accroche_Traction à 8 m), 2 statues géantes (Accroche_Traction à 9 m), l'arbre mort du coin sud-est.
- **Moment de tension :** On descend l'escalier vers une arène vide sous la lune ; le boss entre par le haut des falaises.
- **Captures :** `docs/art/rendus/Exemples/Jardin/Arene_Boss_*.jpg`

## Nuit 4, lot 3 : kits pour un level design en hauteur (2026-11-23, branche `art-kits-3`)

Ce lot ajoute 68 pièces et en modifie 3, dans les kits des trois actes (`Assets/Art/Models/KitCaves|KitChateau|KitJardin/`). Elles sont modélisées par script dans Blender, sans IA, et la méthode est la même que pour les kits existants : UV monde, peinture de sommets, matériaux de la bibliothèque et du château.

- **Catalogue** (emprise, triangles, matériaux) : docs/art/KITS.md, section « Nuit 4, lot 3 ».
- **Planche** : `docs/art/rendus/Nuit4/Lot3/Planche_Kits_Lot3.jpg`. Il y a aussi une image par pièce : vue 3/4, mannequin de 1.8 m, gabarit rouge du traqueur pour les pièces qui le concernent, parties mobiles entrouvertes pour contrôler les pivots.
- **Sources** (`Assets/Art/Source~/Kits/scripts/`) :
  - `kit_hauteur.py` : module commun, importé par `kit_caves.py`, `kit_chateau.py` et `kit_jardin.py` ;
  - `rendu_kits.py` : rendus de contrôle ;
  - `planche_kits.py` : planche ;
  - `kit_doc_lot3.py` : catalogue et manifeste.
- **Régénérer une pièce** : `blender -b --factory-startup -P kit_x.py -- SM_…`.

**Axes.** Les positions ci-dessous sont en **axes Unity**. La pièce regarde vers **+Z**, Y est en haut. Une cote Blender devient : x Unity = −x Blender, y Unity = z Blender, z Unity = −y Blender. Le côté « G » d'une porte est à gauche vu de face, donc à **+X** dans Unity.

**Enfants et repères exportés** :

| Nom | Rôle | Dans Unity |
|---|---|---|
| `Col_Rampe` | rampe lisse sous les nez de marche (escaliers) : pente de 33.7° (27.8° au jardin), 34.4° au plus dans les colimaçons (mesurée sur le FBX), sous la limite de 45° du contrôleur | **collision seule** : KitBuilder retire le rendu et met un MeshCollider (matériau `M_Collision_Invisible`) |
| `Col_Escalier` | volume de collision de l'escalier (relecture du lot 3) : joues des volées, dessus des paliers 6 mm au-dessus des paliers visibles, dos, noyau, limon et sous-face du colimaçon, murs d'échiffre et socles du jardin, éboulis des caves ; 1 cm hors des faces visibles | **collision seule**, comme `Col_Rampe`. KitBuilder ne met plus de collider au rendu d'une pièce qui a des `Col_*` : tout ce qui arrête le joueur est dans `Col_Rampe` et `Col_Escalier` (les garde-corps gardent le leur) |
| `Garde_Corps` | garde-corps, balustres, cordes, mains courantes, parapets de pont | collision des joueurs ; calque « garde » à poser par main (D-078 A2 : la main et le regard du traqueur le traversent) |
| `Deco_SansCollision` | corde et seau des puits | aucun collider (KitBuilder) : rien où s'accrocher au-dessus de la margelle |
| `Deco_Eau` | surface de l'eau des puits (relecture du lot 3) | aucun collider : le joueur traverse l'eau et descend jusqu'au fond (−2.4 m), qui reste solide |
| `Battant_*`, `Vantail`, `Serrure`, `Herse`, `Trappe_*` | parties mobiles, pivot à l'axe | jamais statiques (KitBuilder) ; KitBuilder range `Serrure` sous `Vantail` |
| `Accroche_Bord`, `Accroche_Rebord_n` | milieu de l'arête avant d'un dessus plat | rebords de hissage, `StalkerPassage` « rebord » s'il y a lieu |
| `Accroche_Pied`, `Accroche_Sommet` | pied et sommet d'une face grimpable | `StalkerPassage` « mur_grimpe » (16 m au plus), `ClimbLink` des goules |
| `Accroche_PorteEtroite` | milieu de l'ouverture, au sol | `StalkerPassage` « porte_etroite » |
| `Accroche_Traction`, `Accroche_Bas`, `Accroche_Haut` | points de traction ; pied et haut d'échelle | |
| `Appui_A`, `Appui_B` | bouts du tablier des passerelles | à poser sur les deux planchers |
| `Apparition_Traqueur`, `Porte_Traqueur` | point d'apparition du traqueur ; milieu de sa sortie | `StalkerSpawn` ou repaire |
| `Passage_Entree`, `Passage_Sortie` | bouts des passages dans la roche | |
| `RayonLune` | milieu du vitrage des fenêtres | y mettre `Chateau_RayonLune` en enfant, transform local nul : le rayon descend à 55° vers la salle (+Z) |
| `Feu_n` | flamme d'une bougie | feu et lumière ajoutés par KitBuilder |
| `Feu_Torche` | flamme des torches du château (relecture du lot 3) | feu de torche à l'échelle 1 et lumière de torche (1.35 / 15.4 m, ombres douces) ; KitBuilder lit le numéro avec `int.TryParse` (« Feu_Torche » compte comme `Feu_1`) |

### Pièces communes (une version par acte : `SM_Cave_…`, `SM_Chateau_…`, `SM_Jardin_…`)

| Pièce | Emprise (L × l × h) | Pivot | Usage et mesures |
|---|---|---|---|
| `Escalier_Droit_4m` (jardin : `_3m`) | marches de 3 m de large (château 4 m, jardin 3.2 m) ; emprise 4.24 m aux caves (éboulis côté paroi), 4.45 m au jardin (murs d'échiffre) ; 7.5 à 8.3 m de long | sol, milieu du pied de la 1re marche ; monte vers **−Z** | 20 marches de 20 × 30 cm (jardin : 16 de 18.75 × 38 cm), palier de 1.5 m en haut. **Caves** : bloc maçonné, corde d'un côté, l'autre côté contre une paroi. **Château** : pierre claire, tapis central, balustrades et boules. **Jardin** : murs d'échiffre pleins et socles à vase (plus de touffes dans les vases). Collisions : `Col_Rampe` et `Col_Escalier`. |
| `Escalier_Tournant_4m` | 7.5 × 6 m (château 8.5 × 7, jardin 9.5 × 7.8) | pied de la 1re volée ; volée 1 vers −Z, volée 2 vers **−X** | deux volées de 2 m, palier carré à 2 m, palier de 1.5 m en haut ; garde-corps sur tous les côtés vides |
| `Escalier_Colimacon` | Ø 4 m (caves 3.6 m), 6.4 m + noyau | **axe du noyau**, au sol | 32 marches de 20 cm, 16 par tour (3 m libres sous la marche du tour suivant), sens anti-horaire vu de dessus, départ côté −X. Sous-face hélicoïdale ; palier de 100° à 6.4 m au-dessus des premières marches. Rampe hélicoïdale qui commence à 0.3 m du noyau. |
| `Mezzanine_8x6` | 8 × 6 m, dessus à **5 m** | sol, au ras du mur, au milieu ; avance vers +Z | double saut et hissage (5.3 m) ou escalier, échelle ; garde-corps devant et côté +X ; côté −X ouvert pour une arrivée ; 6 m de profondeur : **le traqueur ne s'y pose pas** |
| `Coursive_8m` | 8 × 2.5 m | **dessus**, au ras du mur | consoles ou contrefiches ; garde-corps côté vide ; `Accroche_Bord` (0, 0, 2.5) |
| `Passerelle_10m` | 10 × 2.4 m | **dessus**, au centre | entre deux planchers ; garde-corps des deux côtés ; pour que le traqueur passe dessous, la poser avec le **dessous à 10 m** ou plus (§ 5 ter) |
| `PorteHaute` | mur de 8 × 10 × 1 m | sol, milieu de la face avant | porte de joueur de 2.4 × 3.6 m, **seuil à 5 m**, rebord de 3 × 1.2 m devant (`Accroche_Bord` à (0, 5, 1.2)) ; derrière, un étage haut |
| `PorteEtroite` | mur de 12 × 10 × 1.5 m ; jardin : mur de 8 × 9 × 1.2 m (comme le mur d'enceinte), emprise 8.2 × 1.8 × 9.3 m avec le chaperon et l'encadrement | sol, milieu de la face avant | ouverture de **4.4 m** qui contient le gabarit de **4 × 5.25 m** du traqueur baissé (`narrowDoorWidth` 4, `narrowDoorHeight` 5.2 plus 5 cm de marge) : le traqueur s'y faufile, le joueur passe à l'aise. Plein cintre aux caves et au jardin, arc brisé au château. |
| `Echelle_6m` | 0.6–0.9 × 7 m | pied, dos au mur (z = 0) ; échelle à 0.22 m devant | barreaux tous les 30 cm, montants qui dépassent d'1 m (château : échelons de fer scellés) ; `chaine_grimpe` du joueur ; le traqueur l'ignore |
| `MurRebords_10m` | 6 × 10 m | sol, milieu de la face | 4 rebords plats de 2.2 × 1 m, dessus à 2.2 / 4.4 / 6.6 / 8.8 m, en quinconce (x ±1.4) ; on monte de l'un à l'autre (hissage) ; avec un mur en face à 3–6 m, wall jumps |
| `MurEscalade_12m` | 8 × 12 m | sol, milieu de la face | prises plates de 0.6 à 1.1 m de large, de 0.7 à 1 m de saillie, tous les 2 m en quinconce (même grille qu'avant), arête du haut nette. **Relecture du lot 3** : aux caves et au château, les prises font partie de l'appareil : boutisses de moellon à dessous chanfreiné et abouts de poutre aux caves, pierres d'attente à fort chanfrein dessous au château, arêtes usées, pierres voisines de la même assise en saillie de 3 cm, joints creux de 3.5 cm (1 038 et 1 388 triangles). Au jardin : roche et racines. |
| `PorteVerrouillee` | mur de 6 × 6.6 × 1 m | sol, milieu de la face | salle payante (pièce de 12 × 12 m, porte de 3 × 3 m, § 0) ; voir « Portes » ; pas de seuil (celui des goules est posé par le jeu). **Relecture du lot 3** : chambranle de porte mouluré d'un seul tenant (piédroits et linteau, deux ressauts, socles), clef au sceau d'âme, plus de linteau à corniche ; rainure cachée dans le mur (y Unity de −0.37 à −0.63, x ±1.68, jusqu'à 6.45 m) où montent le vantail et la serrure ; coulisses de fer dans le tableau. |
| `Tremie_8m` (caves, château) | dalle de 8 × 8 m (0.6 à 1 m) | **dessus**, au centre | trou de 4 × 4 m aux bords plats et nets ; au château, bordure de marbre et balustrade sur trois côtés, côté +Z ouvert |

### Pièces propres à un acte

| Pièce | Emprise | Pivot | Usage |
|---|---|---|---|
| `SM_Cave_Puits` (**refait**) | margelle Ø 3.2 m, 0.9 m de haut ; montants à ±1.95 m | axe, au sol | **Margelle lisse** : paroi droite, dessus arrondi en deux quarts de rond de 12 cm, ni marche ni arête vive. Les montants sont posés au sol, hors de la margelle. **Eau** (`M_Eau_Toon`) à −1.5 m dans l'enfant **`Deco_Eau`** (sans collider : on la traverse), **fond** à −2.4 m (éboulis, solide) : on en ressort d'un saut et d'un hissage (3.3 m). Corde et seau sans collision. **Percer le sol de la salle sur Ø 3.2 m sous le puits.** |
| `SM_Jardin_Puits` | idem, arceau de fer forgé à 4 m | axe, au sol | même margelle, même eau (`Deco_Eau`), même fond ; plus de touffe au pied ; **percer le sol sur Ø 3.2 m** |
| `SM_Cave_Cheminee_16m` | 13.5 × 7 × 16.6 m | sol, milieu de l'entrée | deux parois de roche parallèles à **3.5 m** et un fond (wall jumps en zigzag), peu bossues ; `Accroche_Pied` et `Accroche_Sommet` (0, 0 et 16, −2) |
| `SM_Cave_Balcon_Bois` | 3.2 × 1.8 m | dessus, au ras du mur | point de vue, `Accroche_Traction` (0, 1.1, 1.8) |
| `SM_Chateau_Fenetre_Lancette_Simple` / `_Double` / `_Triple` | panneaux de 4 × 12, 8 × 12 et 8 × 20 m, 1 m d'épaisseur (comme les murs du kit) | sol, milieu de la face intérieure | baies en arc brisé de 1.6 × 6.6, 4 × 7.3 et 5.6 × 14.3 m ; ébrasement intérieur de 0.45 m ; remplage (meneaux, arcs, oculus, rose à 6 rayons) ; **vitrail au plomb** (relecture du lot 3) : petits losanges sertis, en majorité bleu-gris sombre, bordure ponctuée de rouge et d'ambre, un médaillon rond par lancette (couronne ambre, quartiers rouges, cœur ambre), rose en secteurs rouge et ambre ; mêmes trois matériaux. `RayonLune` à (0, 6.3 / 6.15 / 10.65, −0.72). Elles se raccordent aux `SM_Chateau_Mur_8x12` et `_8x20`. |
| `SM_Chateau_Chandelier_Petit` / `_Moyen` / `_Grand` | Ø 2.0, 3.3 et 4.6 m | **point d'accroche au plafond** ; tout pend vers −Y | Ø ≈ 8.3 cm × (L + l) de la salle : pièce de 12 × 12 m, salle de 24 × 16 m, hall de 32 × 24 m. Le bas est à **2.4, 5 et 8 m** sous le plafond. Bas du lustre à 3.5 m du sol au moins, donc plafond de **5.9 / 8.5 / 11.5 m** au moins (plafonds conseillés : 12 m pour une salle, 20 m pour le hall). 8, 18 et 32 bougies. |
| `SM_Chateau_Lustre_Cristal` | Ø 2.4 m, bas à 3 m sous le plafond | point d'accroche | trois étages de bras dorés, 24 bougies, gouttes de cristal en **`M_Kit_Cristal`** (relecture du lot 3 : elles étaient en `M_Kit_Vitrail`, presque noir) ; plafond de 6.5 m au moins. Décor : le lustre attrapable du jeu reste `Prop_Lustre`. |
| `SM_Chateau_Applique_2Bougies` / `_Miroir` | 0.75 × 0.66 m et 0.8 × 1.26 m | point de scellement, dos au mur | à poser à 2–2.5 m ; flammes à 0.36–0.4 m devant le mur |
| `SM_Chateau_Candelabre_Pied` | Ø 0.84 × 2.3 m | sol | 7 bougies à 2.2–2.3 m |
| `SM_Chateau_Torche_Murale` (**nouvelle**) | 0.16 × 0.41 × 0.76 m | point de scellement, **dos du support contre le plan Z = 0** (la torche avance vers +Z) | platine de fer forgé en losange, console en S et contrefiche, deux bagues (dorée en haut), manche de bois, tête de tissu goudronné ; à poser à 2.2–2.6 m ; `Feu_Torche` à (0, 0.56, 0.34) ; 393 triangles |
| `SM_Chateau_Torchere` (**nouvelle**) | 0.67 × 0.59 × 1.88 m | sol, axe | trépied à volutes, fût à nœuds dorés, coupe et cage de fer, fagot de tissu goudronné ; `Feu_Torche` à 1.92 m ; 832 triangles |
| `SM_Jardin_Falaise_Rebords_16m` | 16 × 16 m | sol, milieu du front | 4 rebords plats de 4 × 3.2 m tous les **3.5 m** (3.5 / 7 / 10.5 / 14 m), herbe au sommet (16.3 m) : passage d'un niveau de la forêt à l'autre |
| `SM_Jardin_Falaise_Passage` | massif de 30 × 17 × 18 m | sol, milieu de l'entrée ; la gorge file vers −Z | gorge de **10 m** de large et 16 m de long sous une arche naturelle (dessous vers 10.5 m) : **le traqueur passe**, baissé |
| `SM_Jardin_Falaise_Fente` | 18 × 13 × 14 m | idem | fente de **2.6 m** de large et 12 m de long : joueurs seuls ; tout point à moins de 15 m d'une ouverture (pas d'abri, D-035) ; bloc coincé vers 6 m ; parois peu bossues (wall jumps) |
| `SM_Jardin_Terrasse_8m` / `_Acces_8m` | 8 × 6 m, dessus à **3 m** | sol, milieu du pied du mur | mur de soutènement à contreforts, dessus dallé, balustrade. La version à accès a une ouverture de 3.6 m entre deux piliers à vases, pour `SM_Jardin_Escalier_Droit_3m` posé devant (son palier arrive au bord). |
| `SM_Jardin_Pont_Pierre_12m` | 12 × 3.6 m | **milieu, au niveau des bouts du tablier** | dos d'âne (+1.2 m, 17° au plus, sans marche), arche de 8 m sur un lit à −2.5 m, parapets de 0.8 m |
| `SM_Jardin_Pont_Bois_10m` | 10 × 2.4 m | dessus, au centre | rondins, planches, main courante de branches |
| `SM_Jardin_MurEnceinte_Tour` | Ø 6 m (7.2 avec le fruit), 14.4 m | axe, au sol | tour d'angle entre deux `MurEnceinte_8m` ; plateforme crénelée à 13.2 m (`Accroche_Traction`) |
| `SM_Jardin_MurEnceinte_Breche_8m` | 8 × 1.2 × 9 m | sol, milieu de la face | brèche de 3 m dont le fond est à **1.4 m**, éboulis en rampe des deux côtés : on passe d'un saut, le traqueur l'enjambe |
| `SM_Jardin_MurEnceinte_CheminRonde_8m` | 8 × 2.4 × 9 m | sol, milieu de la face côté jardin | allée dallée à 9 m, parapet crénelé côté extérieur (−Z) |

### Cellules d'apparition du traqueur

Le traqueur fait 7 × 14 m et se baisse jusqu'à 9 m. Toutes les cellules le contiennent debout. Le repère `Apparition_Traqueur` est au centre de son sol.

| Pièce | Emprise | Volume intérieur | Sortie | Remarques |
|---|---|---|---|---|
| `SM_Cave_CelluleTraqueur_Herse` | façade de 18 × 21 × 2 m, bloc de 15 × 15.4 m | 12 × 12 m, berceau de 18 m | arc de 10 × 11 m, **herse** | Chaînes et anneaux au fond. Sol de la cellule fourni (dalles à 0). Il faut 21 m de hauteur ou un adossement à la paroi. Rainure cachée dans la façade (relecture du lot 3) : la herse y monte. |
| `SM_Chateau_CelluleTraqueur` | façade de 18 × **22.5** × 2 m (relecture du lot 3 : la herse levée et ses fleurons restent cachés), pierre claire | berceau brisé de 19.5 m | arc brisé de 10 × 12 m, herse de fer forgé en losanges | corniche, double archivolte |
| `SM_Cave_CelluleTraqueur_Barricade` | 24.5 × 18 × 21 m | caverne de 13 × 13 m, plafond à 15 m au moins au-dessus de lui | bouche de 10 × 11 m (il sort baissé), **barricade de rondins** à deux battants | roche, éboulis |
| `SM_Cave_CelluleTraqueur_Fosse` | 15 × 15 m, 15 m de profondeur | fosse de 12 × 12 m | **deux trappes de grilles** au ras du sol | Mur du fond à prises (`Accroche_Pied` et `Accroche_Sommet`, `mur_grimpe` de 15 m : il en sort en grimpant). Margelle plate. **À poser dans un trou de 15 × 15 m du sol de la salle.** Les grilles fermées se marchent. |
| `SM_Jardin_CelluleTraqueur_Voliere` | Ø 16 m (17 avec le portail), 21.7 m | Ø 16 m, dôme à 20 m | portail de 8.5 × 11 m à deux vantaux | volière de fer en ruine sur socle, barreaux tous les 0.5 m (on voit le monstre dedans) ; plus de touffes de lierre ; 4 cm de jeu au gond des vantaux |

### Portes et parties mobiles (axes Unity, angles signés dans Unity)

Les deux portes se distinguent au premier coup d'œil :

| | Porte de sortie d'acte | Porte verrouillée (salle payante) |
|---|---|---|
| Forme | grande arche de 8 × 10 m dans une façade de 12 × 14 m | ouverture carrée de 3 × 3 m dans un chambranle de porte mouluré d'un seul tenant (sans corniche) |
| Matériau | bois épais cloué : chêne aux caves et au jardin, boiserie au château | fer riveté (vantail en guillotine) dans un chambranle de pierre |
| Symbole | sceau de lune en fer (doré au château) à cheval sur les deux battants : anneau, croissant à gauche, étoile à droite, huit rayons | sceau d'âme : **flamme cyan à trois langues** (`M_Kit_Sceau_Ame`, couleur des âmes) sur un disque, répétée sur la clef du chambranle |
| Ouverture | deux battants pivotent | la serrure tourne, puis le vantail monte |

| Pièce | Partie | Pivot (Unity, m) | Mouvement prévu |
|---|---|---|---|
| `SM_Cave_PorteSortie`, `SM_Chateau_PorteSortie`, `SM_Jardin_PorteSortie` | `Battant_G` | (+4, 0, **−1.11**) | rotation autour de **Y** de **−90°** : s'ouvre vers −Z, du côté de la sortie, à l'opposé de la face visible ; 1.6 s, en douceur. **Relecture du lot 3** : les battants sont posés contre la face arrière de l'arche (gonds 1 cm derrière, z = −1.11) ; l'archivolte arrière est retirée : à aucun moment ils ne traversent les piédroits |
| | `Battant_D` | (−4, 0, −1.11) | **+90°** autour de Y |
| `SM_Chateau_GrandeEntree` | `Battant_G` / `Battant_D` | (±4.5, 0, **−1.51**) | idem (−90° / +90°) ; contre la face arrière du portail (relecture du lot 3) ; elle porte aussi le sceau de lune |
| `SM_*_PorteVerrouillee` | `Serrure` (rangée sous `Vantail` par KitBuilder) | (0, 1.5, −0.35) | 1. quart de tour de **+90°** autour de **Z** (`Quaternion.Euler(0, 0, 90)`, sens antihoraire vu de face), 0.5 s ; le contrôle couvre aussi −90° (les deux sens sont libres) |
| | `Vantail` | (0, 0.02, −0.5), bas du vantail | 2. translation de **+3.1 m en Y**, 1.2 s : il rentre dans la rainure du mur, au-dessus du linteau (la serrure monte avec lui) ; l'ouverture est libre |
| `SM_Cave_CelluleTraqueur_Herse`, `SM_Chateau_CelluleTraqueur` | `Herse` | (0, 0, −1), bas de la herse | translation de **+9.2 m en Y**, 2.5 s, par à-coups : le bas s'arrête à 9.2 m (le traqueur sort baissé) et le haut monte dans une rainure cachée de la façade (21 m aux caves, 22.5 m au château) |
| `SM_Cave_CelluleTraqueur_Barricade` | `Battant_G` / `Battant_D` | (+5, 0, 0.9) / (−5, 0, 0.9) | **+100°** / **−100°** autour de Y : vers la salle (+Z), enfoncés par le traqueur, 0.6 s |
| `SM_Cave_CelluleTraqueur_Fosse` | `Trappe_G` / `Trappe_D` | (+5.98, 0, 0) / (−5.98, 0, 0), charnière au bord de la fosse | **−110°** / **+110°** autour de **Z** : les grilles se relèvent, 1.5 s |
| `SM_Jardin_CelluleTraqueur_Voliere` | `Battant_G` / `Battant_D` | (+4.24, 0, 7.13) / (−4.24, 0, 7.13) | **+100°** / **−100°** autour de Y, vers le jardin (+Z) |

Convention Unity des angles ci-dessus : `Quaternion.Euler(0, θ, 0)` pour Y et `Quaternion.Euler(0, 0, θ)` pour Z, appliqués à la rotation locale de la partie (fermée = identité). Les rendus de contrôle montrent ces parties à mi-course. **Contrôle géométrique** (relecture du lot 3, `controle_lot3.py`) : à 0, 50 et 100 % de la course, intersection par arbres BVH entre chaque partie mobile et la maçonnerie de sa pièce : **0 intersection pour les 22 parties mobiles** (`docs/art/rendus/Nuit4/Lot3/controle_parties_mobiles.json`). Avant la relecture, toutes échouaient sauf les trappes de la fosse.

### Lumières

- **Repères `Feu_n`** (chandeliers, lustre, appliques, candélabre) : KitBuilder pose `VFX_Torche_Feu` à l'échelle 0.045 sur chaque bougie, et une seule lumière ponctuelle par pièce, sur `Feu_1`.
  - Chandeliers et lustre : la lumière est recentrée sur l'axe, 0.3 m sous les bougies ; 1.6 / 18 m, et 2.4 / 26 m pour `Chandelier_Grand`.
  - Appliques et candélabre : 0.6 / 5 m.
- **Fenêtres** : un `Chateau_RayonLune` par repère `RayonLune`.
- **Château** : deux torches ajoutées à la relecture du lot 3, `SM_Chateau_Torche_Murale` et `SM_Chateau_Torchere`, repère `Feu_Torche` : feu et lumière de torche (comme `SM_Cave_Torche_Murale`).

### Ce qui reste à faire dans Unity

Ni Unity ni les `.meta` n'ont été touchés par ce lot.

1. Lancer `KitBuilder.Construit` pour `KitCaves`, `KitChateau` et `KitJardin` avec la version à jour de `Assets/Art/Source~/Kits/scripts/KitBuilder.cs`, qui fait déjà :
   - les 4 nouveaux matériaux (`M_Kit_Vitrail_Rouge`, `M_Kit_Vitrail_Ambre`, `M_Kit_Sceau_Ame`, `M_Collision_Invisible`) ;
   - `Col_*` en collision seule, `Deco_*` sans collider ;
   - les parties mobiles non statiques et `Serrure` sous `Vantail` ;
   - les lustres traités comme sources de lumière ;
   - Read/Write coché, comme au lot 2.

   Ensuite, vérifier le rapport `[mat?]` et faire générer les `.meta` des 68 nouveaux FBX.
2. **Collisions** :
   - régler le pas du contrôleur pour qu'il monte les marches par la `Col_Rampe` ;
   - mettre `Garde_Corps` sur le calque « garde » ;
   - pour la fosse, percer le sol de la salle sur 15 × 15 m ;
   - poser les `StalkerPassage` et `ClimbLink` sur les repères `Accroche_*`.
3. **Animations des portes** : scripts d'ouverture selon le tableau ci-dessus, et sons (grincement, chaîne de herse, déclic du sceau). La porte verrouillée garde sa lueur et son texte « Salle verrouillée / 50 âmes ».
4. **Scènes vitrines et captures dans l'éclairage du jeu** : `KitBuilder.Vitrine` / `Capture`.

### Relecture du lot 3 : corrections (branche `art-kits-3-b`)

Corrections faites dans Blender seulement (Unity n'a pas été ouvert, aucun `.meta` touché, aucun crédit dépensé). Planche avant / après : `docs/art/rendus/Nuit4/Lot3/Planche_Corrections.jpg`, images dans `docs/art/rendus/Nuit4/Lot3/Corrections/`.

- **Touffes de lierre** : toutes les touffes polyédriques en `M_Kit_Feuillage` sont retirées des pièces du jardin (et les touffes d'herbe de la falaise à rebords). Elles sont remplacées, là où c'est utile, par **deux cartes de lierre plates** plaquées au mur (bandes fines à bords de feuilles, 3 cm devant la paroi). Pièces anciennes du jardin : `SM_Jardin_Haie_6m`, `SM_Jardin_Massif` et `SM_Jardin_Foret_Lisiere_20m` sont refaites en blocs et rideaux de feuillage bosselés, sans boule. Noms et pivots inchangés.
- **Puits** : eau dans l'enfant `Deco_Eau` (sans collider) ; **percer le sol de la salle sur Ø 3.2 m sous le puits**.
- **Torches du château** : `SM_Chateau_Torche_Murale` et `SM_Chateau_Torchere`, repère `Feu_Torche`.
- **Murs d'escalade** (caves, château) : prises intégrées à l'appareil, même grille, mêmes repères.
- **Porte verrouillée** : chambranle de porte d'un seul tenant, flamme à trois langues, rainure pour le vantail ; mêmes enfants, noms, pivots et cotes d'ouverture.
- **Lustre en cristal** : gouttes en `M_Kit_Cristal`.
- **Vitraux** : losanges, médaillons, rose ; mêmes trois matériaux.
- **Collisions des escaliers** : enfant `Col_Escalier` dans les 9 escaliers (voir le tableau des enfants).
- **Parties mobiles** : portes de sortie et grande entrée (battants contre la face arrière, **pivots déplacés** : z Unity −1.11 et −1.51), cellules à herse (rainure ; façade du château à 22.5 m), barricade (roche dégagée de la course), volière (socles des poteaux et jeu au gond).

À faire dans Unity :
1. Réimporter les FBX de la liste du rapport et relancer `KitBuilder.Construit` pour les trois kits, avec `KitBuilder.cs` à jour. Il crée `M_Kit_Cristal` et lit `Feu_Torche`. Il faut aussi la règle « pas de collider au rendu d'une pièce qui a des `Col_*` ».
2. Faire générer les `.meta` des deux nouveaux FBX.
3. Les prefabs des portes de sortie changent de pivot : si une animation ou une scène a déjà été posée, la reprendre.

### Fait dans Unity (lot 3, relecture) — remplace « Reste à faire côté Unity » ci-dessus

- **Prefabs** reconstruits par `KitBuilder.Construit` pour les trois kits (`.meta` générés et commités).
  - Les objets `Col_*` sont des collisions seules (rendu retiré) ; les `Deco_*` (eau des puits, etc.) n'en ont pas.
  - **Une pièce qui a des `Col_*` (les 9 escaliers) n'a plus de collider sur son maillage visuel** (les nez de marche doublaient la rampe) ; `Col_Rampe` + `Col_Escalier` couvrent les marches, paliers, joues, noyaux et murs d'échiffre ; les `Garde_Corps` gardent leur collider.
  - Parties mobiles non statiques ; torchère et candélabre sur pied avec une boîte de collision.
- **Animation d'ouverture** : composant `ItHunts.Art.VFX.OuverturePorte` (assembly `ItHunts.Art.VFX`, déjà référencée par `ItHunts.Runtime`) posé par KitBuilder sur les 13 portes et cellules.
  - Genres : `Battants` (portes de sortie, −90° / +90° en Y, 1,8 s) ; `Guillotine` (porte verrouillée : serrure +90° en Z sur 30 % de la course, puis vantail +3,1 m, rebond de fin, 2,2 s) ; `Herse` (+9,2 m, 2,6 s) ; `Trappes` (fosse, −110° / +110° en Z) ; `Vantaux` (barricade et volière, ±100° en Y, 0,9 s).
  - API : `Ouvre()`, `Ferme()`, `Pose(0..1)` (aperçu, chargement), `EstOuverte`, `ouvrirAuDemarrage`. Les colliders des parties mobiles suivent.
  - Contrôle de course (Blender) : 0 intersection à 0, 50 et 100 % (`controle_parties_mobiles.json`).
- **Matériaux** : `M_Kit_Vitrail*` en verre vieilli aux teintes sourdes (le toon accentue les écarts de teinte : valeurs proches, saturation de la lumière 0,4) ; `M_Kit_Cristal` (gouttes du lustre) ; `M_Kit_Sceau_Ame` ; `M_Collision_Invisible` (jamais rendu).
- **Ciel** : `M_Kit_Ciel` utilise la **lune peinte du menu principal** (`Assets/Art/Textures/Ciel/T_Ciel_Lune.png`, extraite de `Titre_Fond.png` par `Assets/Art/Source~/Kits/scripts/lune_menu.py`) ; **halo et dégradé continus** (plus de paliers, shader `ItHunts/Ciel`). Le ciel de l'acte III en jeu (captures `docs/art/rendus/Nuit4/Jeu/Acte3/*_lot3.jpg`) ne passe pas par `Jardin_Ciel` : son halo en paliers vient du ciel de la scène de main. **Pour main :** utiliser le prefab `Jardin_Ciel` (ou `M_Kit_Ciel`) comme ciel des actes.
- **Grosses formes noires ellipsoïdales** :
  - celles des kits (touffes de lierre et d'herbe en boule, buissons) sont **supprimées** (cartes de lierre plates, blocs de feuillage) ;
  - celles de l'acte III en jeu sont les **arbres de secours de main** (`ActLevel.Dressing.Tree` : tronc cylindre + 3 sphères de feuillage) utilisés quand `Act_3.asset` ne contient pas les pièces `Jardin_ArbreMort_Grand`, `_Penche`, `_Tordu` dans sa liste `pieces`. **Pour main :** ajouter ces trois prefabs à `Act_3.asset > pieces` ; les sphères disparaissent.
- **Lumières** : les torches murales, appliques, lustres et chandeliers portent leur feu et leur lumière ponctuelle (KitBuilder). En jeu, plusieurs pièces lumineuses dans une même salle s'ajoutent aux torches de main : à doser dans le niveau (limite de 4 lumières par objet du `PC_RPAsset`, rendu Forward+).
- **Captures en jeu** : pièces posées dans des salles générées des actes II et I (seed 4242), fenêtre et appliques au mur, lustre au plafond, portes à 0 / 50 / 100 % d'ouverture : `docs/art/rendus/Nuit4/Lot3/Planche_Jeu_Lot3.jpg` et `Jeu/` (outil `Assets/Art/Source~/Kits/scripts/CapturesLot3.cs`). Vitrines des trois kits : `Planche_Unity_KitCaves.jpg`, `_KitChateau.jpg`, `_KitJardin.jpg`.
- **Journal Unity** : 0 erreur et 0 avertissement à l'import des FBX et à la construction des prefabs des trois kits (seules erreurs de la console : `MagnetismAudio.OnDestroy`, côté main).
