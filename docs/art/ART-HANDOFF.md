# ItHunts : dossier de mesures et de livraison pour l'artiste

**Mis à jour :** 2026-11-12 · Tiré des réglages réels du jeu (`Assets/_Project/Settings/*.asset`) et de la scène
de test `Test_Movement`. Quand un réglage change, ce document est mis à jour par le côté code (branche `main`).

Ce dossier dit **à quelle taille** construire le monde et les objets, **dans quel style**, et **comment les
livrer**. Les mesures viennent du contrôleur du joueur : un décor qui les ignore casse le mouvement, qui est le
cœur du jeu.

## 1. Organisation du travail

- **Dépôt :** même dépôt GitHub (`SergioMiGuPr/ItHunts`), branche **`art`**. Fusion dans `main` **par pull
  request** uniquement.
- **Ton périmètre :** `Assets/Art/` (modèles, textures, matériaux, shaders, scènes de présentation) et
  `docs/art/`, plus ta ligne dans `docs/assets/MANIFEST.md` pour chaque asset.
- **Hors périmètre :** `Assets/_Project/` (code, réglages, prefabs du jeu) et `Assets/Scenes/Test_Movement.unity`.
  Tu travailles dans **tes propres scènes** (`Assets/Art/Scenes/`).
- **Réglages partagés** (`ProjectSettings/`, `Packages/`, renderer URP dans `Assets/Settings/`) : n'y touche pas
  directement. Décris le changement voulu (par exemple : ajouter une passe de contours au renderer URP) dans la
  PR ou dans un ticket ; il est fait sur `main`.
- **⚠ Réglages partagés changés sur `main` (2026-11-12, commit `ba228b1`, étape 5) :** couches `FirstPersonP1` à
  `P4` (8 à 11) dans `ProjectSettings/TagManager.asset` ; dans `PC_Renderer` et `Mobile_Renderer`, ces couches sont
  exclues des passes normales et trois passes « ItHunts Mains » dessinent les mains et le HUD (voir § mains). **Mets
  ta branche `art` à jour avant de continuer** (`git merge main` ou `git rebase main`) et ne garde pas une ancienne
  version de ces fichiers en résolvant un conflit. N'utilise pas les couches 8 à 11 pour tes objets.
- **Unity 6000.3.28f1** exactement (même version que `main`), URP. Les gros fichiers (FBX, GLB, PNG, PSD, blend…)
  passent par **Git LFS** (déjà configuré dans `.gitattributes`). Le quota LFS gratuit de GitHub est petit (de
  l'ordre de 1 Go de stockage) : pas de fichiers de travail inutiles dans le dépôt.
- Ne renomme ni ne déplace un fichier sans son `.meta` (Unity y garde l'identifiant de l'asset).

## 2. Le joueur

| Mesure | Valeur | Ce que ça implique |
|---|---|---|
| Hauteur de la capsule | **1.8 m** | passage debout : prévoir **≥ 2.2 m** sous plafond (saut compris : 3 m+) |
| Rayon de la capsule | **0.4 m** (0.8 m de large) | couloir praticable ≥ 1.2 m ; **confortable à pleine vitesse : ≥ 3 m** |
| Hauteur des yeux | **1.6 m** | cadre des vues, hauteur des rambardes (≤ 1.1 m pour ne pas boucher la vue) |
| Glissade / accroupi : capsule | **0.9 m**, yeux à **0.75 m** | passage bas glissable : **≥ 1.0 m** (testé à 1.1 m) |
| Marche franchie sans sauter | **0.3 m** | marches d'escalier ≤ 0.3 m ; au-delà, c'est un saut |
| Pente praticable | **≤ 45°** | au-delà, on glisse ; une glissade en pente accélère |

Le joueur ne voit que **ses mains et l'orbe** (vue première personne). L'orbe est en bas à droite de la vue.

## 3. Mouvement : hauteurs et distances

Valeurs théoriques calculées sur les réglages (gravité 20 m/s², chute ×1.5), à l'arrêt du contrôle aérien ; en
jeu, un bon joueur va plus loin.

| Capacité | Mesure | Repère pour le décor |
|---|---|---|
| Course | **7.5 m/s** | |
| Vitesse max des gains (glissade, saut, dash, répulsion) | **20 m/s** | ; bunny hop mesuré jusqu'à **26 m/s** |
| Saut | **1.3 m** | rebord atteint au sol : ≤ 1.3 m |
| Double saut (2e saut en l'air) | **+1.6 m**, soit **≈ 2.9 m** au total | rebord atteignable sans aide : ≤ 2.9 m ; un mur de **3 m+** bloque vraiment |
| Saut en course | **≈ 4.9 m** | trou franchissable par tous |
| Saut + double saut en course | **≈ 9 m** | |
| Dash (au sol ou en l'air) | **6.5 m** en 0.3 s | s'ajoute aux sauts |
| Bunny hop à 20 m/s | **≈ 13 m** par saut | les arènes doivent être **longues** (lignes de 50 m+) |
| Wall jump | **1.8 m** de haut, poussée de 7.5 m/s | murs de wall slide : **≥ 6 m** de haut, longs |
| Écarts entre murs testés (wall jump d'un mur à l'autre) | **3 m** (cheminée de 30 m), **6 m**, **10 m** | 3 m : on monte en zigzag ; 10 m : limite confortable |

**Terrain d'essai déjà testé** (scène `Test_Movement`, greybox) :

- blocs de 0.5, 1, 1.3, 1.5 et 2 m ; rampes de 15°, 30°, 45° et 50° (trop raide) ; longues pentes de 10° à 30° ;
- trous de 2 à 7 m (plateformes de 2 m) ; écarts de 8 à 22 m (plateformes de 4 m) ; escalier de plateformes de
  2 m en 2 m jusqu'à 16 m ;
- couloir de glissade de 4 m de large et 50 m de long, avec passage bas à 1.1 m ;
- paires de murs de 20 m de haut à 6 m et 10 m d'écart ; mur seul de 200 m × 15 m ; cheminée de 3 m × 30 m ;
- grande salle de 50 × 50 m et 45 m de haut, porte de 8 m.

## 4. L'orbe : portées

L'orbe a deux pouvoirs, utilisables en même temps.

| Pouvoir | Portée | Repère pour le décor |
|---|---|---|
| **Traction** (le joueur est tiré en ligne droite vers un point visé, **toute surface**) | **60 m**, jusqu'à 18 m/s (D-036), 2.5 s au plus (≈ 43 m) | salles de **30 à 50 m** de haut possibles ; points d'accroche visibles en hauteur |
| **Répulsion** (rocket jump depuis une surface) | **40 m** ; saut de **5 m** (7.6 m avec un saut juste avant) | |
| **Onde de répulsion** (repousse objets et ennemis devant le joueur ; maintenue, elle pousse en continu 0.8 s) | cône de **14 m**, ouverture **60°**, jusqu'à 22 m/s | les lustres doivent être à **moins de 14 m** des yeux du dessous ; prévoir de la place derrière les ennemis (ils glissent jusqu'à 27–34 m) |
| **Télékinésie** (attraper puis lancer un objet) | **60 m** ; aide à la visée : cône de 8° de près, 3° à 60 m | **chaque arène fournit des objets à lancer**, visibles de loin |
| Objet tenu | flotte à **2.5 m** devant les yeux, **0.35 m** sous la visée | objets tenus ≤ **1.3 m** pour ne pas boucher la vue |
| Lancer | **30 m/s** + la vitesse du joueur ; **droit sur ≈ 15 m** (tir tendu), puis l'objet retombe | lignes de tir dégagées de 15 à 20 m dans les arènes |
| Ennemis factices (greybox) | capsule de **1.8 × 0.8 m** | gabarit provisoire, en attendant la goule |

## 5. Objets attrapables (props interactifs)

Masse **maximale attrapable : 40 kg** (au-delà, l'objet brille en rouge terne et refuse). Tailles actuelles de la
greybox, à garder comme gabarit :

| Objet | Taille | Masse |
|---|---|---|
| Petite caisse | cube de 0.5 m | 5 kg |
| Caisse moyenne | cube de 0.7 m | 15 kg |
| Caisse | cube de 0.9 m | 25 kg |
| Grosse caisse | cube de 1.1 m | 40 kg (max) |
| Caisse trop lourde | cube de 1.3 m | 60 kg |
| Tonneau | Ø 0.6 m × 0.9 m | 20 kg |
| Lustre (suspendu ; tombe quand on l'attrape ou qu'une onde de répulsion le décroche ; se casse au premier impact) | 1.6 × 0.3 × 1.6 m, suspendu à ≈ 7.6 m | 25 kg |

- **Silhouette proche d'une forme simple** (boîte, cylindre, capsule) : la collision est une forme simple ajoutée
  dans Unity. Pour une forme complexe, fournis en plus un **maillage de collision convexe** très bas
  (`<Asset>_Collider`, < 100 triangles).
- La masse se lit à l'œil : plus gros et plus « plein » = plus lourd. Les objets trop lourds doivent avoir l'air
  massifs (métal, pierre).
- Le jeu fait **briller** ces objets (émission pilotée par le code, voir § 7).
- Les objets **se cassent** après 1 à 5 lancers selon leur masse (le lustre au premier choc) : prévoir à terme des
  morceaux cassés (pour l'instant, débris en cubes greybox). Ils tombent à **20 m/s²**, comme le joueur.
- Un lustre doit pouvoir être visé **par en dessous à moins de 14 m** des yeux (l'onde en cône le décroche) : en
  dessous de ≈ 15 m de haut.
- Les ennemis **étourdis** peuvent être attrapés et lancés : prévoir des créatures dont la silhouette reste lisible
  quand elles flottent devant le joueur (factice actuel : capsule de 1.8 × 0.8 m, 80 kg).

## 5 bis. Mains et HUD (greybox, étape 5)

Les mains greybox sont faites pour être **remplacées telles quelles** : garde les mêmes volumes (± 10 %), le même
point d'ancrage et les mêmes axes, et le jeu n'a rien à retoucher.

- **Rendu des mains :** champ de vision **fixe de 60°** (indépendant du FOV de vitesse, 70 → 82°), dessinées par une
  passe du renderer URP après le monde, sur une profondeur effacée (elles passent devant tout). Plan proche de la
  caméra du joueur : **0.05 m** (rien des mains plus près). Les mains sont sur une couche par joueur
  (`FirstPersonP1` à `P4`, mise par le code).
- **Contour cel-shading des mains :** un contour en post-process qui lit la profondeur ne les verra pas (elles n'y
  sont pas) ; prévoir un contour par le matériau (coque inversée) ou une passe dédiée à la couche des mains.
- **Occlusion ambiante :** les mains ne la reçoivent pas (elle est calculée sur le décor, derrière elles) ; leur
  ombrage doit venir du matériau (ombres en aplats peintes, cel-shading), pas de la SSAO.
- **Repère d'une main :** origine au **poignet**, doigts vers **+z**, dos de la main vers **+y**. Mesures (m) :

  | Pièce | Taille | Position (repère de la main) |
  |---|---|---|
  | Avant-bras | cylindre Ø 0.06 × 0.26 | de z −0.27 à −0.01 |
  | Paume | 0.085 × 0.03 × 0.09 | centre z 0.045 |
  | Articulation des doigts | — | z 0.09 (les doigts tournent autour de x : 0° ouverts, 25° au repos, 40° fermés) |
  | Doigts | 0.08 × 0.025 × 0.07 | centre z 0.035 après l'articulation |
  | Pouce | 0.022 × 0.022 × 0.055 | x ∓0.05 (côté intérieur), z 0.05, tourné de 30° |

- **Position de repos (repère caméra, m et °) :** main droite (0.21, −0.22, 0.47), rotation (−15, −10, 0) ; main
  gauche (−0.23, −0.25, 0.45), rotation (−10, 12, 0). Au repos, les mains couvrent **4.8 %** de l'écran, et rien ne
  doit entrer dans un cercle de **48 px** (à 1080) autour du viseur.
- **Orbe :** sphère de **Ø 0.08 m**, centre en (0, 0.06, 0.045) dans le repère de la main droite (au-dessus de la
  paume). Prévoir un point d'attache nommé `Orb_Socket` à cet endroit ; l'orbe reste un objet séparé (le jeu change sa
  couleur, HDR, bloom).
- **Barillet (premier artefact, main gauche, D-032) :** tenu dans la paume gauche, pointé dans le sens des doigts
  (+z de la main). Repère `Barillet (vue)` en (0, 0.055, 0.05) dans le repère de la main gauche. Mesures (m) :

  | Pièce | Taille | Position (repère du barillet) |
  |---|---|---|
  | Corps (tourne d'un cran de 60° à chaque tir, autour de z) | cylindre Ø 0.075 × 0.07 | centre (0, 0, 0) |
  | 6 chambres (face arrière, vers la caméra) | Ø 0.016, au rayon 0.024 | z −0.037 ; chambre 1 en haut, puis tous les 60° |
  | Canon | cylindre Ø 0.022 × 0.10 | centre (0, 0.022, 0.085) |
  | Bouche du canon (départ de la traînée) | — | (0, 0.022, 0.136) |

  **Les munitions se lisent sur les chambres** : une chambre chargée s'allume (couleur HDR orangée, bloom), une vide
  est noire, et elles sont grisées quand l'orbe tient un objet (barillet inactif). Prévoir dans le modèle : 6
  chambres **séparées** (le jeu change leur couleur une à une), le corps qui tourne en pièce séparée, un point
  d'attache `Barrel_Muzzle` au bout du canon, pivot du corps sur son axe. Budget proposé : 3 000 à 5 000 triangles,
  texture 1024. Recul géré par le code (la main recule de 3.5 cm et se relève de 8° à chaque tir).
- **Livraison des mains :** `SK_Mains` (bras et mains, squelette avec poignet et doigts), pivot au poignet de chaque
  main, mêmes axes ; budget 15 000 à 20 000 triangles, texture 2048. Les gestes sont faits par le code (positions et
  rotations du poignet, ouverture des doigts) : des animations faites à la main pourront venir plus tard.
- **HUD :** résolution de référence **1920 × 1080**, échelle 1, marge de sécurité **5 %** ; autour du viseur :
  segments de **22 × 6 px** (écart 4 px, rangées à 6 px, 26 px sous le viseur), anneaux de rayon **8 / 13 / 18 px**,
  épaisseur 2 px ; coins réservés de **240 × 60 px** (âmes en haut à gauche, timer en haut à droite, traqueur enragé
  sous le timer) ; munitions du barillet : **6 segments de 8 × 9 px** sous les jauges de l'orbe ; PV du joueur (tir
  ami, provisoire) : barre de **220 × 10 px** en bas à gauche, cachée quand ils sont pleins. Couleurs réservées : § 7.

## 5 ter. Ennemis, passages et labyrinthe (étape 6, D-033 à D-035)

Mesures greybox (le jeu s'appuie dessus : collisions, NavMesh, portée des coups). Un modèle doit tenir dans ces
volumes (± 10 %), pivot **aux pieds**, avant vers **+z**.

| Créature | Volume de collision | Notes |
|---|---|---|
| Goule | capsule Ø 0.9 × 1.1 m de haut, 50 kg | basse, rampe ; yeux lumineux ; posture accroupie avant le bond (le code écrase le visuel à 60 % en hauteur) ; **grimpe aux murs** : le code bascule le visuel de 80° (plaquée contre la paroi) |
| **Traqueur (géant)** | capsule **Ø 7 × 14 m** ; **baissé : 9 m** | intuable ; yeux rouges vers 13 m ; **épaule à 12 m** ; il ne grimpe pas ; **se baisse** sous les ponts (le code écrase le corps jusqu'à 9 m : prévoir une pose accroupie) |
| Mains du traqueur (deux) | **boîtes de 4 × 2.2 × 4.5 m** (largeur × épaisseur × longueur) ; bras de **1.5 m** d'épaisseur | elles tuent au contact, comme le corps et les bras (D-037) : garder ces volumes ; bras de l'épaule à la main (le code l'étire), **portée 15 m depuis l'épaule** ; pose **deux mains levées au-dessus de la tête** (geste contre ceux qui passent par-dessus) |
| Volant (à venir) | ≈ 0.5 m d'envergure, 8 kg | attrapable comme une caisse |

**Prévoir dans le modèle du traqueur :** un bras droit articulé épaule → coude → poignet (le code pose la main : repos le
long du corps, levée au-dessus de la tête pendant l'annonce, abattue sur la cible), une main en pièce séparée
(`Stalker_Hand`, pivot au centre de la paume), et un repère `Stalker_Shoulder_R`. Annonce visible de loin : bras levé,
**ombre et lueur rouge au sol** à la taille de la main (couleur réservée au danger).

**Labyrinthe (Test_Arena, `ArenaLayout_Test`, D-035) :**

| Élément | Mesure | Pour qui |
|---|---|---|
| Carte | ≈ **260 × 260 m**, grille de 3 × 3 intersections à **100 m** ; deux niveaux (sol, **+16 m**) | le mouvement et le géant |
| Grandes salles | **50 à 64 m** de côté, plafond **36 m** au-dessus de leur sol | |
| Carrefours | **20 m** de côté, plafond 24 m | |
| Couloirs | **14 m de large, 19 m de haut** (12–16 × 18–20) ; tous dans une boucle, aucun cul-de-sac (sauf le repaire) | lui et le joueur |
| Couloirs en pente entre niveaux | **16 m de large** : pente de **8 m** (14 à 16° sur 55 à 65 m, pleine) et **voie au sol de 8 m** qui finit au pied d'un **mur de 16 m** (les goules le grimpent) | glissade ; goules |
| Lignes droites | **145 à 255 m** | élan |
| **Hauteur minimale de passage** | **9 m** (il se baisse en dessous de 14 m) ; ponts et poutres : **dessous à 10 m**, tablier de 1 m | il passe dessous baissé |
| Ponts, corniches | **6 m de large** au plus (moins que lui : il n'y monte pas) ; corniches à **11 m**, pleines jusqu'au sol | |
| **Pièces fermées** | **12 × 12 m, 6 m de haut**, porte de **3 × 3 m** (2 à 4 m) et une fenêtre ; **profondeur au plus 15 m depuis une ouverture** | le joueur et les goules ; lui passe le bras |
| Repaire | cul-de-sac de 26 × 20 m, 20 m de haut | |
| Murs de course | au moins 10 m de long ; 9 à 12 m de haut | |

- **Plus d'abris (D-035) :** tout point où l'on tient debout doit rester à portée de sa main (15 m depuis l'épaule, ou
  15 m depuis l'ouverture d'une pièce). Pas de recoin sous un objet bas, pas de pièce plus profonde que 15 m.
- **Murs grimpables des goules :** faces avant des corniches et murs sous les ponts des couloirs (composant
  `ClimbLink`) ; les garder dégagés.
- **Lisibilité :** la goule et le traqueur doivent rester visibles dans le noir (yeux, silhouette) ; les volumes se
  lisent à pleine vitesse (éclairage en grille, panneaux bleus de traction, murs de course plus clairs) ; la sortie est
  verte.

## 6. Budgets proposés (à valider ensemble)

Triangles par asset (LOD0) et textures. Rendu peint à la main : on mise sur la texture de couleur (base color) et
les contours, peu sur les normal maps.

| Catégorie | Triangles (LOD0) | Textures | LOD |
|---|---|---|---|
| Mains + orbe (vue FPS) | 15 000 – 20 000 | 2048 | non |
| Petits props attrapables (caisses, pots) | 500 – 2 000 | 512 – 1024 | non |
| Props moyens (tonneau, lustre, statue) | 2 000 – 6 000 | 1024 | LOD1 à 50 % |
| Pièces de décor modulaires (murs, arches, piliers) | 500 – 5 000 | trim sheets 1024 – 2048 partagées | LOD1 si > 3 000 |
| Goule (ennemi au sol) | 15 000 – 25 000 | 2048 | LOD1, LOD2 |
| Volant | 8 000 – 15 000 | 1024 – 2048 | LOD1 |
| Boss traqueur (géant) | 40 000 – 60 000 | 2 × 2048 ou 4096 | LOD1, LOD2 |
| Scène entière visible | ≈ 1 – 2 millions | | |

Densité de texels visée : **≈ 256 – 512 px/m** pour le décor et les props (style peint, détails lisibles à
pleine vitesse plutôt que fins).

## 7. Style

Mélange de deux techniques de rendu : **bande dessinée encrée** et **peinture dramatique**. Dans les prompts et
le guide de style, **décris les traits**, ne cite pas de jeu ou de série.

- **Contours** épais et sombres autour des silhouettes et des grandes arêtes (passe de contours dans le renderer).
- **Cel-shading** : ombres en **2 ou 3 aplats** nets, pas de dégradé doux.
- **Textures peintes à la main** : coups de pinceau visibles, hachures dans les creux, couleurs un peu
  saturées ; pas de photo-texture.
- **Lumière dramatique et colorée** : décor **très sombre** (caves et châteaux), nombreuses sources de lumière
  chaudes (torches, braseros), contre-jours colorés.
- **Échelle humaine mais large** : salles et couloirs assez larges pour traverser à 20 m/s (§ 3).
- **Tout ce qui est interactif brille.** Les couleurs saturées suivantes sont **réservées au jeu** ; évite-les en
  grandes surfaces dans le décor :

  | Couleur | Sens |
  |---|---|
  | Bleu | traction (orbe, trait, ancre) |
  | Orange | répulsion |
  | Violet | télékinésie : objet visé ou tenu |
  | Vert | objet lancé |
  | Rouge | refus, objet trop lourd |

  **Technique :** le code fixe la couleur d'émission (`_EmissionColor`) des objets attrapables. Leur matériau doit
  avoir l'**émission activée** et une **carte d'émission** (masque) qui dit *où* ils brillent (arêtes, runes,
  cerclages), la couleur vient du jeu.

Images d'inspiration : `docs/design/references-visuelles/`. Le guide de style détaillé (`docs/art/STYLE-GUIDE.md`)
est la première livraison attendue.

## 8. Pipeline de création (connecteur Higgsfield)

Chaque génération coûte des crédits : **annonce le nombre de modèles avant de lancer**. Les noms des modèles et
leurs options sont à vérifier dans le connecteur au moment de s'en servir.

1. **Image concept** dans le style (§ 7), vue de face ou trois-quarts, fond neutre, une seule pièce par image.
2. **Image → 3D :**
   - props et décor : **Tripo P2** (image vers 3D, export GLB ou FBX), **nombre de polygones limité** au budget du
     § 6, **auto_size** activé ;
   - personnages et créatures : **Tripo H3.1** en **multivues** (face, profil, dos) pour une forme cohérente.
3. **Squelette et animations des créatures :** **Meshy** (rigging et animation).
4. **Nettoyage dans Blender** : échelle (1 unité = 1 m), pivot, orientation, réduction des polygones, UV propres
   (pas de chevauchement pour les textures uniques), transformations appliquées, retouche des textures peintes.
5. **Import Unity** dans `Assets/Art/Models/…`, contrôle dans une scène de présentation, à côté d'un mannequin de
   1.8 m et d'une caisse de 0.9 m.
6. **Une ligne par asset dans `docs/assets/MANIFEST.md`** : source, outil et modèle utilisés, licence, échelle
   vérifiée, pivot, triangles, droits vérifiés.

**Droits d'usage commercial, à vérifier avant d'intégrer quoi que ce soit au jeu :**

- les conditions du forfait Higgsfield utilisé, et celles de chaque modèle appelé (Tripo, Meshy) : usage
  commercial autorisé, propriété des sorties, obligation de mention ;
- certains forfaits gratuits imposent une licence ouverte avec attribution, ou interdisent l'usage commercial ;
- **Steam** demande de déclarer le contenu généré par IA : garde la trace de ce qui est généré (colonne « Source /
  outil » du manifeste) ;
- note dans le manifeste la date et le forfait au moment de la génération.

## 9. Règles de livraison

- **Formats :** FBX (préféré pour les personnages animés) ou GLB ; textures **PNG** (TGA ou EXR si besoin) ;
  fichiers source (`.blend`, `.psd`) dans `Assets/Art/Source/` seulement s'ils sont utiles à d'autres.
- **Échelle :** 1 unité = 1 m, facteur d'échelle 1 à l'import, transformations appliquées.
- **Axes :** dans Unity, Y vers le haut et l'avant de l'objet vers +Z, rotation (0, 0, 0) à l'import. Depuis
  Blender : exporter avec *Apply Transform* (FBX) ou en glTF, puis **vérifier dans Unity** (rotation nulle, objet
  debout, face vers +Z) ; corriger dans Blender plutôt que dans Unity.
- **Pivot :**
  - props posés au sol et décor : **au centre de la base** ;
  - objets suspendus (lustre) : **au point d'accroche** (en haut) ;
  - personnages et créatures : **entre les pieds**, au sol ;
  - pièces modulaires : à un coin, sur une grille de **1 m** (ou 0.5 m pour les petites pièces).
- **Nommage :**
  - `SM_` maillage statique, `SK_` maillage animé, `M_` matériau, `A_` animation (`A_Goule_Course`) ;
  - textures `T_<Asset>_<Type>` avec `BaseColor`, `Normal`, `Emission` (masque de lueur), `Mask` ;
  - LOD : `_LOD0`, `_LOD1`… ; collision : `<Asset>_Collider` ;
  - pas d'espaces ni d'accents dans les noms de fichiers.
- **Dossiers :** `Assets/Art/Models/<Catégorie>/<Asset>/` (catégories : `Props`, `Decor`, `Creatures`, `FPS`),
  `Assets/Art/Materials/`, `Assets/Art/Shaders/`, `Assets/Art/Scenes/`.
- **Vérifier avant la PR :** échelle à côté du mannequin, pivot, orientation, triangles dans le budget, matériaux
  URP (pas de shader manquant, rose), ligne du manifeste remplie.
