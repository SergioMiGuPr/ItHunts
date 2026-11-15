# ItHunts : dossier de mesures et de livraison pour l'artiste

**Mis à jour :** 2026-11-15 · Tiré des réglages réels du jeu (`Assets/_Project/Settings/*.asset`), des constructeurs de
scène et des scènes `Test_Movement`, `Test_Arena` et `Level_01`. **C'est la référence unique des mesures pour la DA** : quand un
réglage qui y figure change, ce document est mis à jour par le côté code (branche `main`).

> **Ce qui a changé le 2026-11-15** (après le playtest des étapes 8 et 9) :
> - **Nouveau § 0** : toutes les dimensions de ce qui a un modèle, dans un seul tableau (joueur, traqueur, goule, volant,
>   caisses, lustres, coffres, marchands, portes, couloirs et salles).
> - **Nouveaux § 0 bis, 0 ter et 0 quater** : masses des objets et des ennemis attrapables, timings que les animations doivent
>   respecter, couleur de l'orbe par action (D-024) et codes couleur du jeu (rouge = annonce d'attaque et refus).
> - **Volant : envergure 2.3 m** (et non ≈ 2.4 m).
> - **Plus aucune porte « refuge »** (D-035) : aucune porte ni aucun abri où le traqueur ne peut pas passer la main.
> - **Textes en monde** (marchand, sortie, coffres, orbes de choix) : maintenant **cachés par le décor** (test de profondeur), lignes
>   de 0.28 m, toujours face à l'écran ; ne pas compter sur eux à travers un mur.
> - **Catalogue des props** : `Prop_TorcheEteinte` (flamme sans lumière, une torche sur deux dans les bases régénérées).
> - Goules : elles grimpent aussi les flancs des plateformes et les murs proches d'un joueur hors d'atteinte, puis **bondissent
>   depuis le mur** (même annonce de 0.3 s) : garder les murs et flancs de plateforme **pleins**, sans trou ni rebord qui coupe.

## 0. Dimensions de référence (tout ce qui a un modèle)

Un modèle doit tenir dans ces volumes (± 10 %), sauf mention contraire. Pivot et axes : § 9.

| Objet | Dimensions | Où c'est réglé | Détails |
|---|---|---|---|
| **Joueur** | capsule **1.8 × 0.8 m** (rayon 0.4) ; yeux à 1.6 m ; accroupi 0.9 m, yeux à 0.75 m | `Movement_Default` | § 2 ; en écran partagé, l'autre joueur est vu comme une capsule (provisoire, à remplacer par un corps) |
| **Traqueur** (boss) | capsule **7 × 14 m** (Ø 7, 14 de haut) ; **se baisse jusqu'à 9 m** ; épaule à 12 m ; **portée de main 15 m** depuis l'épaule (et 15 m dans une pièce depuis l'ouverture) | `Stalker_Default` | mains : boîtes de 4 × 2.2 × 4.5 m, bras de 1.5 m d'épaisseur (§ 5 ter) |
| **Goule** | capsule **Ø 0.9 × 1.1 m** | `Ghoul_Default` | basse, rampe, grimpe aux murs (visuel basculé de 80°) |
| **Volant** | corps 0.6 × 0.45 × 0.8 m (sphère de collision de 0.4 m de rayon) ; ailes de 0.9 × 0.5 m ; **envergure 2.3 m** | `Flyer_Default`, constructeur | zone de touche : corps + une boîte de 0.9 × 0.1 × 0.55 m par aile |
| **Caisses attrapables** | cubes de **0.5 / 0.7 / 0.8 / 0.9 / 1.1 m** ; caisse trop lourde 1.3 m | scènes, props | § 5 et catalogue |
| **Tonneau attrapable** | Ø 0.6 × 0.9 m | `Prop_Tonneau_Attrapable` | |
| **Lustre** (salles) | 1.6 × 0.3 × 1.6 m, suspendu vers 7.6 m (au plus ≈ 15 m) | scènes | se décroche à l'onde (cône de 14 m) |
| **Lustre géant** (arène du boss) | **12 × 1.5 × 12 m**, accroché à **24 m** | `BossArenaBuilder` | piège : tombe sur le traqueur |
| **Coffre** | caisse **1.4 × 0.8 × 0.9 m** + couvercle **1.5 × 0.2 × 1 m** séparé | `Economy_Default`, scènes | orbes de choix de 0.55 m à 2.2 m au-dessus (§ 5 quater) |
| **Marchand** (au hasard, retiré à l'étape 8) / **marchand de l'artefact** (salle de départ) | silhouette **0.9 × 2.2 m** + enseigne **1.6 × 0.4 m** à 2.9 m | scènes | 4 offres à 1.6 m de haut, 1.6 m devant lui |
| **Grande porte de sortie** | **10 × 10 m** | `Level01Builder` | escalier de 20 marches de 0.4 × 1.6 m, 10 m de large, palier de 12 m |
| **Portes des pièces** (annexes, salles verrouillées) | **3 × 3 m** dans un mur de 1 m ; fenêtre 3 × 1.5 m | constructeurs | sous la porte, le jeu ajoute un seuil pour les goules : ne pas y mettre de marche ni d'objet |
| **Couloirs** | **14 m de large, 19 m de haut** (16 m de large en pente) ; tronçons de 40 m au plus | `LevelLayoutSettings` | voie centrale de 8 m, décor dans une bande de 3 m le long des murs |
| **Salles** | **46 à 66 m** de côté (modules de 48, 56, 64 m), plafond **36 m** ; carrefours **20 m**, plafond 24 m ; niveau haut à **+16 m** | `LevelLayoutSettings` | voies dégagées en croix de 11 m entre les ouvertures |
| **Pièces annexes** | **12 × 12 m, 6 m de haut** | constructeurs | profondeur ≤ 15 m depuis une ouverture |
| **Passages en hauteur** | ouverture **3 × 3 m à 4.5 m** du sol, conduit 8 × 3.5 × 3 m | `LevelParkour` | murs autour non magnétiques |
| **Arène du boss** | **80 × 80 m, 30 m** de haut ; fosse de 18 m de large, 8 m de profondeur | `BossArenaBuilder` | leviers : plaque 1.2 × 0.8 × 1.2 m sur un socle de 1.2 m |

## 0 bis. Masses (objets et ennemis attrapables)

La télékinésie attrape jusqu'à **40 kg** (au-delà : lueur rouge terne et refus). La masse doit **se lire à l'œil**.

| Objet ou ennemi | Masse | Attrapable |
|---|---|---|
| Petite caisse (0.5 m) | 5 kg | oui |
| Chaise attrapable | 8 kg | oui |
| Caisse moyenne (0.7 m) / caisse attrapable (0.8 m) | 15 kg | oui |
| Tonneau attrapable | 20 kg | oui |
| Caisse (0.9 m) | 25 kg | oui |
| Lustre (salles) | 25 kg | oui (il tombe) |
| Grosse caisse (1.1 m) | 40 kg | oui (le maximum) |
| Caisse trop lourde (1.3 m) | 60 kg | **non** (lueur rouge terne) |
| **Volant** | **8 kg** | **oui, à tout moment** ; se libère en **2 s** |
| **Goule** | **50 kg** | **seulement étourdie** (la masse de 40 kg ne s'applique pas aux ennemis étourdis) ; se libère en **3 s** |
| Ennemi factice (greybox) | 80 kg | étourdi seulement |
| Traqueur | — | **jamais** (intuable, insensible à l'orbe) |

## 0 ter. Timings que les animations doivent respecter

Le jeu décide du moment des coups ; l'animation doit montrer l'annonce **pendant** ce temps, pas avant ni après.

| Action | Durée | Ce que l'animation montre |
|---|---|---|
| **Bond de la goule** (au sol, depuis une corniche **ou depuis un mur**) | annonce **0.3 s** puis bond | posture accroupie (le code écrase le visuel à 60 %), lueur rouge ; le bond s'esquive par un dash pendant le vol |
| **Prise du traqueur** (main abattue) | annonce **0.5 s** ; la visée se recale **0.15 s avant** la descente | bras levé, ombre et lueur rouge au sol à la taille de la main |
| **Mains levées au-dessus de la tête** (traqueur) | annonce **0.2 s**, puis 1 s | deux mains au-dessus de la tête |
| **Revers** (traqueur, balayage de la main) | annonce **0.2 s**, balayage 0.25 s | |
| **Main plantée** (traqueur, après une prise ratée) | **1.5 s** au sol | la main reste au sol, il tourne plus lentement |
| **Piqué du volant** | annonce **0.4 s** | cri, lueur rouge, ailes repliées en piqué |
| **Étourdissement** (goule, volant) | **1.5 s** | sonné ; attrapable pendant ce temps |
| **Libération d'un ennemi tenu** | goule **3 s** (elle se débat les **0.5 s** d'avant : clignote et tremble) ; volant **2 s** | elle se débat |

## 0 quater. Couleurs : orbe par action (D-024) et codes du jeu

| Couleur | Sens |
|---|---|
| **Bleu** | traction (orbe, trait, ancre) ; c'est elle qui l'emporte sur l'objet tenu |
| **Orange** | répulsion ; aussi les pièges, cibles et leviers du combat de boss |
| **Violet** | objet visé ou tenu (télékinésie) ; marchands |
| **Vert** | lancer (un éclat bref, qui l'emporte sur le reste) ; levier activé, sortie ouverte |
| **Rouge** | **annonce d'attaque** (bond, prise, piqué, mains levées) **et refus** (trop lourd, surface non magnétique, porte verrouillée) |
| **Cyan** | âmes |
| **Doré / orangé** | coffres |

Ces couleurs saturées sont **réservées au jeu** : à éviter en grandes surfaces dans le décor (§ 7).


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
| Wall jump | **1.8 m** de haut, poussée de 7.5 m/s (**6.4 m/s** effectifs : −15 % sur les murs, gains arrêtés à **18 m/s**, D-044) | murs de wall slide : **≥ 6 m** de haut, longs |
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
| Volant (D-040) | sphère de **0.4 m** de rayon (corps 0.6 × 0.45 × 0.8 m, **ailes de 0.9 m** chacune, **envergure 2.3 m**), 8 kg ; **zone de touche** : sphère du corps + une boîte de 0.9 × 0.1 × 0.55 m par aile, qui suit le battement (garder les ailes à ces dimensions, ou prévenir) | vole en ronde **8 m au-dessus du joueur** (rayon 8 m) ; attrapable à tout moment comme une caisse ; ailes animées par le code (battement, repliées en piqué) ; yeux lumineux, **lueur rouge** à l'annonce du piqué ; éclate (zone de 3 m) quand il est lancé |

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
  15 m depuis l'ouverture d'une pièce). Pas de recoin sous un objet bas, pas de pièce plus profonde que 15 m. **Plus aucune
  porte « refuge »** : aucune porte, trappe ou grille ne doit fermer un endroit où il ne passe pas la main.
- **Volumes de salle (D-045, D-047) :** chaque salle de vos scènes de niveau reçoit un objet vide avec le composant
  `RoomVolume` (une boîte du sol au plafond, type Salle) ; chaque couloir, un volume de type Couloir par **tronçon de 40 m au
  plus**. Aucun ennemi n'apparaît dans la salle ou le tronçon d'un joueur (ni dans les tronçons voisins), et la minimap s'en
  sert.
- **Murs grimpables des goules :** faces avant des corniches et murs sous les ponts des couloirs (composant
  `ClimbLink`) ; les garder dégagés.
- **Lisibilité :** la goule et le traqueur doivent rester visibles dans le noir (yeux, silhouette) ; les volumes se
  lisent à pleine vitesse (éclairage en grille, panneaux bleus de traction, murs de course plus clairs) ; la sortie est
  verte.

## 5 quater. Âmes et économie (étape 7, D-043)

| Élément | Mesure | Lueur et lisibilité |
|---|---|---|
| Éclat d'âme | sphère de **0.25 m**, flotte un peu au sol, file vers l'orbe (25 m/s) | **cyan très lumineux** (HDR ≈ 0.4 / 2.6 / 3.2, bloom) ; clignote ses 5 dernières secondes ; **commun** : visible par tous les joueurs |
| Coffre | caisse de **1.4 × 0.8 × 0.9 m** (collision sur tout le volume) + couvercle **1.5 × 0.2 × 1 m** (caché, **pour chaque joueur**, quand il l'a ouvert : un modèle avec couvercle séparé) | liseré **orangé** (émission), couvercle **doré lumineux** (HDR ≈ 3 / 2 / 0.5) ; texte « Coffre / 30 âmes » à 1.7 m, « Coffre / ouvert » pour qui l'a ouvert |
| Orbes de choix (coffre) | sphères de **0.55 m**, à **2.2 m** au-dessus du coffre (celle du milieu 0.6 m plus haut), espacées de **2.4 m** ; flottent (±0.12 m) et tournent | couleur de l'amélioration (HDR) ; **icône puis nom, deux lignes de 0.28 m**, toujours face à l'écran, **lisibles à 10 m** (vérifié par une capture) ; **visibles seulement par le joueur qui a ouvert le coffre** (deux joueurs : deux jeux d'orbes au même endroit) |
| Marchand | silhouette de **0.9 × 2.2 m** + enseigne **1.6 × 0.4 m** à 2.9 m | robe **violette** (émission), enseigne violette lumineuse ; 4 orbes d'offre à 1.6 m de haut (une sur deux 0.6 m plus haut), **1.6 m devant lui**, espacées de 2.4 m, avec leur prix |
| Porte de la salle verrouillée | **3 × 3 m** (l'ouverture de la pièce), barreaux sur la fenêtre (3 × 1.5 m) | bois sombre à **lueur orangée** ; texte « Salle verrouillée / 50 âmes » |

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
  | Rouge | annonce d'attaque, refus, objet trop lourd |

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

## Étape 8 : niveau 1, arène, menus (D-048 à D-053)

- **Niveau 1 (`Level_01`, copie retravaillée ; `Level_01_Base` régénérée par le constructeur) :**
  - **Grille :** 7 × 7 salles à 120 m d'écart, soit 800 × 785 m. Salles de 46 à 66 m de côté, carrefours de 20 m ;
    plafonds de 36 m (salles) et 24 m (carrefours) ; niveau haut à +16 m.
  - **Couloirs :** 14 × 19 m (16 m en pente). Ponts et poutres : dessous à 10 m.
  - **Corniches :** 11 m, pleines jusqu'au sol.
  - **Canaux d'égout :** 6 m de large, 4 m de profondeur, eau à 1.2 m sous le sol. Grand pont de 14 m, petit pont de 4 m.
  - **Muret des coffres :** 3 m de haut.
  - **Plateformes non magnétiques :** 4 × 4 m, de 2.5 à 6.5 m, écarts de 3 m ; matériau à rayures sans lueur, à garder
    lisible à la DA (ne jamais les faire briller comme les cibles de traction).
- **Sortie :**
  - **Grande porte :** 10 × 10 m, rouge verrouillée, orange payable.
  - **Escaliers :** 20 marches de 0.4 × 1.6 m, 10 m de large.
  - **Palier :** 12 m.
- **Arène du combat de boss :**
  - **Dimensions :** 80 × 80 m, 30 m de haut.
  - **Herse :** piliers de 28 m, passage de 10 m, grille de 10 × 12 m ; poche ouest fermée jusqu'au plafond.
  - **Fosse :** 18 m de large, 8 m de profondeur, sur toute la profondeur de l'arène ; tablier du pont de 12 m.
  - **Lustre géant :** 12 × 12 m, à 24 m, avec un treuil sur le mur nord.
  - **Lisibilité :** cibles de 1.4 m qui brillent (orange), zones de pièges en orange au sol.
- **Salle de départ :** marchand de l'artefact (silhouette violette de 2.2 m, enseigne) ; 3 orbes d'offre par joueur.
- **Menus et minimap :** greybox IMGUI (`MainMenu`, écran de fin, minimap en haut à gauche), à refaire à l'étape 10 ;
  aucun menu ne met le jeu en pause.

## Catalogue des props (après le playtest de l'étape 8) : modèles à produire

Chaque objet de décor est un prefab greybox **`Assets/_Project/Prefabs/Props/Prop_<Nom>.prefab`**, fait de formes simples, avec
une seule boîte de collision sur la racine (collisions simplifiées) et le batching statique pour le décor fixe. Pour le
remplacer : ouvrir le prefab, remplacer les formes par le modèle (même pivot : **au sol, au centre**, +z vers l'avant), garder la
boîte de collision (ajuster sa taille) et **ajouter l'étiquette (label) « DA »** au prefab, pour que le constructeur ne le
recrée plus. Toutes les scènes suivent. Les props attrapables gardent leurs composants (Rigidbody, Grabbable) et leur lueur.
Nombre utilisé : niveau 1 (modules, couloirs, annexes) après génération du 2026-11-14.

| Prefab | Catégorie | Dimensions (m, l × h × p) | Nombre utilisé | Rôle |
|---|---|---|---|---|
| `Prop_Tonneau` | Tonneau | 0.9 × 1.1 × 0.9 | 212 | fixe |
| `Prop_Tonneau_Attrapable` | Tonneau | 0.6 × 0.9 × 0.6 | 131 | attrapable (20 kg) |
| `Prop_Caisse` | Caisse | 1.3 × 1.3 × 1.3 | 100 | fixe |
| `Prop_Caisse_Attrapable` | Caisse | 0.8 × 0.8 × 0.8 | 279 | attrapable (15 kg) |
| `Prop_PileDeCaisses` | Caisse | 2.6 × 2.6 × 2.6 | 78 | fixe |
| `Prop_Etagere` | Étagère | 2.6 × 3.2 × 0.7 | 117 | fixe |
| `Prop_Table` | Table | 2.6 × 0.95 × 1.2 | 82 | fixe |
| `Prop_Chaise_Attrapable` | Chaise | 0.55 × 1 × 0.55 | 63 | attrapable (8 kg) |
| `Prop_Banc` | Banc | 2.6 × 0.55 × 0.6 | 79 | fixe |
| `Prop_Lit` | Lit | 1.3 × 0.7 × 2.3 | 23 | fixe |
| `Prop_CoffreDeco` | Coffre décoratif | 1.2 × 0.8 × 0.8 | 85 | fixe |
| `Prop_Armoire` | Armoire | 1.6 × 2.7 × 0.8 | 30 | fixe |
| `Prop_Ratelier` | Râtelier d'armes | 2.6 × 2.1 × 0.5 | 40 | fixe |
| `Prop_Statue` | Statue | 1.4 × 3.6 × 1.4 | 69 | fixe |
| `Prop_Colonne` | Pilier / colonne | 1.4 × 8 × 1.4 | 358 | fixe |
| `Prop_ColonneBrisee` | Colonne brisée | 1.4 × 2.6 × 1.4 | 47 | fixe |
| `Prop_Chaine` | Chaîne | 0.2 × 4 × 0.2 | 247 | décor sans collision |
| `Prop_Cage` | Cage | 2.2 × 3 × 2.2 | 50 | fixe |
| `Prop_Lanterne` | Lanterne | 0.5 × 0.8 × 0.5 | 258 | lumière, sans collision |
| `Prop_Torche` | Torche murale | 0.3 × 1 × 0.5 | 1231 | lumière, sans collision |
| `Prop_TorcheEteinte` | Torche murale (flamme sans lumière) | 0.3 × 1 × 0.5 | une torche sur deux dans les bases régénérées (étape 9) | décor sans collision ; même modèle que la torche, sans lumière |
| `Prop_Gravats` | Tas de gravats | 3.2 × 1.2 × 3 | 72 | fixe |
| `Prop_MurEffondre` | Mur effondré | 5 × 3 × 1.2 | 11 | fixe |
| `Prop_Echafaudage` | Échafaudage | 4 × 6 × 2 | 24 | fixe (on y monte) |
| `Prop_Poutre` | Poutre | 6 × 0.5 × 0.5 | 0 | fixe |
| `Prop_Grille` | Grille | 3 × 3 × 0.2 | 46 | fixe |
| `Prop_Tapisserie` | Tapisserie | 2.6 × 4 × 0.08 | 29 | décor sans collision |
| `Prop_Autel` | Autel | 2.6 × 1.2 × 1.3 | 4 | fixe |
| `Prop_Sarcophage` | Sarcophage | 1.2 × 1 × 2.5 | 52 | fixe |
| `Prop_Paille` | Paille (litière) | 2 × 0.3 × 2 | 77 | décor sans collision |
| `Prop_Chaudron` | Chaudron | 1.3 × 1 × 1.3 | 14 | fixe |
| `Prop_Tuyau` | Tuyau d'égout | 0.8 × 0.8 × 6 | 28 | fixe |

- **Modules de salle** (`Assets/_Project/Prefabs/Modules/Module_*.prefab`) : 13 salles (48, 56, 64 m) et 2 carrefours (20 m),
  assemblés par le générateur ; ils utilisent ces props. Mesures à respecter dans les salles : voies dégagées en croix de 11 m
  entre les ouvertures (rien sous 10 m), étages pleins jusqu'au sol, ponts dont le dessous est à 10 m.
- **Couloirs :** décor dans une bande de 3 m le long des murs, voie centrale de 8 m ; torches tous les 12 m à 4 m de haut ;
  colonnes engagées tous les 18 m.
- **Pièces annexes** (12 × 12 m, porte de 3 × 3 m) : chambre, cellule, réserve, cuisine, armurerie, chapelle, crypte.
- **Passages en hauteur :** ouverture de 3 × 3 m à 4.5 m du sol, conduit de 8 × 3.5 × 3 m ; murs autour non magnétiques (rayés).
- **Galeries de parcours :** 20 m de large le long d'un couloir, vide de 10 m, paliers de 6 × 5 m, poutres de 0.6 m,
  plateformes de 2.4 × 2.4 m qui tombent ; tout est non magnétique (ne jamais faire briller ces surfaces).

- **Arène du combat de boss (après le playtest de l'étape 8) :** galeries pleines à 6 m (5 m de profondeur) le long des murs nord et
  sud, tours de 11 m (6 × 6 m), galerie de 6 m dans la poche est ; herse à x = −18 (poche de 22 m) ; 6 leviers (plaque de
  1.2 × 0.8 × 1.2 m sur un socle de 1.2 m, orange puis vert) ; 5 plateformes de 2.4 m qui tombent au-dessus de la fosse.
