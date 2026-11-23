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
> - **Textes en monde** (marchand, sortie, coffres, orbes de choix, salle verrouillée, « à terre ») : **cachés par le décor**, avec
>   une règle unique : un texte n'est affiché que si **aucun décor (collider solide sans Rigidbody) n'est entre la caméra et lui**,
>   en plus du test de profondeur. Lignes de 0.28 m (sortie 0.5 m), toujours face à l'écran, **toujours à l'échelle du monde**
>   (même posés sur une porte étirée). Sur une porte (sortie, salle verrouillée), **un texte devant chaque face**, à 0.12 m : un
>   modèle de porte doit donc avoir une **collision pleine** (sinon le texte de l'autre face se voit à travers).
> - **Animations (PR #6, branche `animations`, 2026-11-16)** : goule et traqueur gréés branchés (contrôleurs dans
>   `Assets/_Project/Animation/`, un état par clip, fondus par le code) ; épaule du traqueur lue sur l'os `Bras.R` : **3.55 m devant le
>   pivot** dans la pose de repos animée (4 m annoncés) ; bras du traqueur étirés par le code au-delà de leur longueur quand la main va
>   à 15 m. Bras FPS : cadrage de `reference_bras_fps.png` appliqué ; avec la lanterne (échelle 0.6) et le barillet, **17 à 26 % de
>   l'écran** est couvert (objectif du jeu : 15 %) : à voir avec l'utilisateur (objets plus petits ou mains plus écartées).
> - **Retours du lot (branche `retours-lot`, 2026-11-16)** :
>   - **Arc de couloir (`SM_Arc_Couloir`)** : le plafond des couloirs est **plat** (15 m), l'arc est un **anneau en ogive** de
>     14 × 11 m posé sur des piédroits de 4 m : seule la clé touchait le plafond, il restait 3 à 6 m de vide de chaque côté entre
>     l'extrados et le plafond. En attendant, le jeu comble ces écoinçons par un mur de pierre (`M_Pierre_Monde`, 1.4 m d'épaisseur,
>     enfoncé de 0.3 m dans l'anneau). **À demander :** une variante **`SM_Arc_Couloir_Ecoincons`** : même anneau (intrados et
>     profil inchangés, 1.6 m de profondeur), avec des **écoinçons pleins** : contour extérieur **rectangulaire de 14 m de large
>     sur 11 m de haut** (le haut plat affleure le plafond, les côtés affleurent les murs), écoinçons en retrait de 0.1 m sur
>     chaque face pour que l'anneau reste en saillie, pivot inchangé (au sol, au centre), matériau des murs (`M_Pierre_Monde`, UV
>     monde) ; le jeu l'élargit comme l'arc actuel (échelle x = largeur / 14).
>   - **Goule à 1.8 m** (au lieu de 1.1 m) : le jeu agrandit `SM_Goule` × 1.64 (réglage `bodyHeight` de `Ghoul_Default`) ; rien à
>     refaire, mais les **seuils de LOD de l'import (0.25 / 0.125 / 0.01)** sont remplacés par le jeu (0.33 / 0.13 / 0.016 : les
>     distances de bascule conseillées par la DA pour 1.1 m) ; une version livrée directement à 1.8 m est possible plus tard.
>   - **Lueurs** : yeux de la goule et pupilles du traqueur en **rouge sombre** (sous le seuil du bloom, réglable) ; griffes du
>     traqueur (`M_Traqueur_Mains`, posé par le jeu sur les bras et les mains) en **ivoire**, faibles au repos, plus vives pendant
>     la prise.
>   - **Bras FPS** : les poses de la PR #5 sont branchées. **Repos = `Pose_OrbePaume`** (paumes vers le haut, des deux
>     côtés), traction, répulsion, attraper (3 clés), tenir un objet, lancer. `Pose_Pistolet` et `Pose_PriseAnneau` ne
>     servent plus : l'orbe et l'artefact **flottent** au-dessus de la paume (`Socket_Orbe`, `Socket_Artefact` + hauteur de
>     flottement réglée par le jeu), le barillet n'a plus de canon. **Pour les animations à venir :** les livrer dans les mêmes
>     FBX, en clips nommés **`Anim_<nom>`** (ex. **`Anim_Repos`** pour le repos animé, en boucle) ; le menu *ItHunts > Vue
>     FPS : bras de la DA* les cuit à 30 images/s et le jeu joue `Anim_Repos` à la place de `Pose_OrbePaume` dès qu'il existe.
>     Une nouvelle pose fixe garde le nom `Pose_*` (deux clés).
> - **Intégration du lot de la PR #4 (branche `integration-lot`, 2026-11-16)** : kit `SM_Kit_*` (UV monde) dans toute la carte, props
>   calés sur `INTEGRATION-PROPS.md` (torche +0.545 m, lumière sur `FX_Flamme`), `SM_Caisse` sur toutes les caisses attrapables,
>   échafaudage praticable (plateaux dessus à 3 et 6 m, épaissis de 0.07 à 0.3 m vers le bas pour le NavMesh), goule `SM_Goule`,
>   traqueur en pièces (**épaule du code : 1.8 m du centre, 11.1 m de haut ; main = zone qui tue 4.9 × 1.5 × 4.5 m**), bras FPS
>   du renard et du caméléon, flamme de la lanterne à la couleur du personnage, calque de rendu 1 nommé « FPS ».
>   **Écarts à corriger côté DA :** (1) **bras FPS : les clips `Pose_*` sont vides à l'import, sauf `Pose_Attraper`** (une seule
>   image par action : l'exporteur FBX ne la garde pas) : mettre au moins deux clés par pose ; en attendant, le jeu ferme les
>   doigts lui-même ; (2) **caisse : la rune (masque d'émission) n'est que sur la face avant** : invisible des autres côtés, le
>   signal « caisse à portée » se lit moins bien qu'avec la greybox (tout le cube) ; rune sur chaque face, ou dire si la face
>   avant suffit ; (3) **droits commerciaux « à confirmer »** pour la goule, le traqueur, la caisse, le kit et les textures
>   retouchées (forfaits Tripo, Higgsfield) : à régler avant toute publication ; (4) mains et bras du traqueur peu lisibles dans
>   l'ambiance « sombre » (aucune émission) : un liseré ou une lueur de bord ? (5) `SM_Goule` à 22 000 triangles, jusqu'à 80
>   goules en jeu : un niveau de détail réduit aiderait la performance.
> - **Intégration du rendu et du lot de modélisation (PR #3, branche `rendu-integration`, 2026-11-16)** : contours plein écran et
>   pré-passe sans les mains (`PC_Renderer`), ambiance intermédiaire dans toutes les scènes, lanterne du joueur, `SM_LanterneFeu` en main
>   (`FX_Flamme`, `FX_Jet`, verre allumé par `_EmissionColor`), props et arène remplacés par les modèles (liste dans `PROGRESS.md`).
>   **Demandes à la DA :** (1) `IsMatchingLightLayer` dans `ItHunts_ToonForwardPass` (sinon la lanterne du joueur éclaire encore les
>   artefacts en toon) ; (2) *Read/Write* sur `Assets/Art/Models/Niveau1` (fusion du décor) ; (3) **`INTEGRATION-PROPS.md` absent de la
>   PR #3** : pivots, boîtes de collision et repères repris du manifeste ; torche à +0.545 m (consigne), l'alignement sur la greybox
>   donnait +0.330 m ; (4) normales lissées dans les FBX de vue FPS pour la coque du contour (arêtes vives du barillet).
> - **`.meta` des assets art (règle du § 1, mise à jour)** : la session art génère et commite les `.meta` de ses nouveaux assets ; la
>   session principale seulement pour des assets jamais ouverts dans Unity. Ancienne règle : les `.meta` de `Assets/Art/` sont générés et commités par la
>   session principale (Unity), juste après la fusion d'une PR art. La branche `art` fait un **pull de `main` avant chaque
>   nouveau lot** ; ne jamais générer de `.meta` sur `art` sans avoir tiré `main` (détail au § 1).
> - **Demande à la DA : « Read/Write » activé à l'import des modèles du décor** (`Assets/Art/Models/Niveau1/*.fbx`, case *Read/Write*
>   de l'onglet Model). La carte aléatoire fusionne les pièces du kit par salle et par matériau (performance) ; Unity ne peut pas
>   fusionner un maillage non lisible : ces pièces restent aujourd'hui des objets séparés (≈ 500 par carte : arcs, colonnes, portes,
>   poutres, ponts). C'est un réglage de vos `.meta`, donc à faire sur `art`.
> - **Personnages et orbe (D-064 à D-066, `carte-suite`)** : chaque personnage a son artefact et sa couleur — **renard rouge
>   (lanterne de feu), caméléon orange (barillet), koala vert, hibou bleu** (artefacts à concevoir). Avec l'**orbe v2**, l'orbe
>   gardera **la couleur du personnage en permanence** ; les actions se liront au **mouvement des anneaux et des runes** ; les
>   **6 tablettes** servent de jauge (**2 traction, 1 répulsion, 3 mouvement**). Le code couleur par action (§ 0 quater) reste
>   jusqu'à l'intégration de l'orbe v2. **Lanterne de feu** : mesures au § 5 bis (greybox en place, modèle attendu par PR).
> - **Hauteurs abaissées (`carte-suite`, charte « caves à l'échelle humaine »)** : salles simples 18 m, à étages 22 m, d'égout 16 m,
>   carrefours 16 m, couloirs 15 m (§ 0) ; les arcs de couloir portent la voûte (sommet au plafond, deux piédroits de 1 × 1.6 m).
> - **Carte aléatoire (`carte-aleatoire`)** : nouvelle section « Kit de construction » : toute la structure en pièces `Kit_*` (4 m),
>   à remplacer par les vrais modèles ; liste des pièces par priorité.
> - **Barillet (`barillet-modele`)** : `SM_Barillet` branché dans la vue FPS (§ 5 bis) ; le canon reste greybox, à modéliser.
> - **Catalogue des props** : `Prop_TorcheEteinte` (flamme sans lumière, une torche sur deux dans les bases régénérées).
> - Goules : elles grimpent aussi les flancs des plateformes et les murs proches d'un joueur hors d'atteinte, puis **bondissent
>   depuis le mur** (même annonce de 0.3 s) : garder les murs et flancs de plateforme **pleins**, sans trou ni rebord qui coupe.

## 0. Dimensions de référence (tout ce qui a un modèle)

Un modèle doit tenir dans ces volumes (± 10 %), sauf mention contraire. Pivot et axes : § 9.

| Objet | Dimensions | Où c'est réglé | Détails |
|---|---|---|---|
| **Joueur** | capsule **1.8 × 0.8 m** (rayon 0.4) ; yeux à 1.6 m ; accroupi 0.9 m, yeux à 0.75 m | `Movement_Default` | § 2 ; en écran partagé, l'autre joueur est vu comme une capsule (provisoire, à remplacer par un corps) |
| **Traqueur** (boss) | capsule **7 × 14 m** (Ø 7, 14 de haut) ; **se baisse jusqu'à 9 m** ; épaule à 12 m ; **portée de main 15 m** depuis l'épaule (et 15 m dans une pièce depuis l'ouverture) | `Stalker_Default` | mains : boîtes de 4 × 2.2 × 4.5 m, bras de 1.5 m d'épaisseur (§ 5 ter) |
| **Goule** | capsule **Ø 1.1 × 1.8 m** (modèle 1.1 m mis à l'échelle × 1.64 par le jeu, `bodyHeight`) | `Ghoul_Default` | basse, rampe, grimpe aux murs (visuel basculé de 80°) |
| **Volant** | corps 0.6 × 0.45 × 0.8 m (sphère de collision de 0.4 m de rayon) ; ailes de 0.9 × 0.5 m ; **envergure 2.3 m** | `Flyer_Default`, constructeur | zone de touche : corps + une boîte de 0.9 × 0.1 × 0.55 m par aile |
| **Caisses attrapables** | cubes de **0.5 / 0.7 / 0.8 / 0.9 / 1.1 m** ; caisse trop lourde 1.3 m | scènes, props | § 5 et catalogue |
| **Tonneau attrapable** | Ø 0.6 × 0.9 m | `Prop_Tonneau_Attrapable` | |
| **Lustre** (salles) | 1.6 × 0.3 × 1.6 m, suspendu vers 7.6 m (au plus ≈ 15 m) | scènes | se décroche à l'onde (cône de 14 m) |
| **Lustre géant** (arène du boss) | **12 × 1.5 × 12 m**, accroché à **24 m** | `BossArenaBuilder` | piège : tombe sur le traqueur |
| **Coffre** | caisse **1.4 × 0.8 × 0.9 m** + couvercle **1.5 × 0.2 × 1 m** séparé | `Economy_Default`, scènes | orbes de choix de 0.55 m à 2.2 m au-dessus (§ 5 quater) |
| **Marchand** (au hasard, retiré à l'étape 8) / **marchand de l'artefact** (salle de départ) | silhouette **0.9 × 2.2 m** + enseigne **1.6 × 0.4 m**, centrée à **5.5 m** de haut, 1.6 m devant lui (au-dessus de la rangée d'offres, dans leur plan) | scènes | 4 offres à 1.6 m de haut, 1.6 m devant lui ; l'enseigne ne doit cacher aucune étiquette d'offre |
| **Grande porte de sortie** | **10 × 10 m** | `Level01Builder` | escalier de 20 marches de 0.4 × 1.6 m, 10 m de large, palier de 12 m |
| **Portes des pièces** (annexes, salles verrouillées) | **3 × 3 m** dans un mur de 1 m ; fenêtre 3 × 1.5 m | constructeurs | sous la porte, le jeu ajoute un seuil pour les goules : ne pas y mettre de marche ni d'objet |
| **Couloirs** | **14 m de large** ; couloirs plats : **voûte gothique** du kit de la PR #8 (murs jusqu'à la naissance à 8 m, voûte en lancette, clé à 20.5 m, **dessus à 21.2 m**, arcs-doubleaux sur colonnes engagées à chaque travée et contre chaque bout, 19.8 m libres) ; travées de 8 m ajustées à la longueur du couloir (pièces étirées en z, UV monde) ; murs de travée : arcade aveugle (renfoncement de 1.2 m, fond et sol posés par le jeu), niche ou mur plein ; porte des salles inchangée (14 × 15 m, arc d'entrée), pignon de pierre au-dessus jusqu'à 21.6 m. Couloirs en pente : 16 m de large, plafond plat à 15 m ; tronçons de 40 m au plus | `MapLayout_Niveau1` (`gothicCorridors`, `gothicArcadeShare` 0.25, `gothicNicheShare` 0.35) | voie centrale de 8 m, décor dans une bande de 3 m le long des murs ; torches à 4 m sur les trumeaux, à 2.2 m de la colonne (une travée sur deux, en quinconce), jamais sur une arcade ; la collision de la voûte et des arcs est traversée par le traqueur (comme les anciens arcs) |
| **Salles** | **46 à 66 m** de côté (modules de 48, 56, 64 m) ; plafond par type : **salle simple 18 m, salle à étages 22 m, salle d'égout 16 m** ; carrefours **20 m**, plafond **16 m** ; niveau haut à **+16 m** | `MapLayout_Niveau1` | voies dégagées en croix de 11 m entre les ouvertures |
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

> **Fait (D-066, 2026-11-17) :** l'orbe v2 (`SM_Orbe`) est branchée en vue FPS : énergie et glyphes à la **couleur du
> personnage** (`CharacterDefinition.color`) en permanence ; anneaux qui tournent (plus vite en traction), tablettes = jauge des
> charges (`OrbModelView`, réglages `OrbModel_Default`). Le tableau ci-dessous reste vrai pour le **trait, l'ancre, les marques et
> les signaux** (plus pour l'orbe elle-même). Attention aux couleurs réservées (rouge = annonce d'attaque et refus).

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
- **`.meta` des assets art (règle mise à jour le 2026-11-15) :** la branche `art` travaille maintenant **dans Unity**
  (worktree `ItHunts-art`). Elle génère elle-même les `.meta` de ses **nouveaux** assets de `Assets/Art/` (shaders,
  matériaux, scènes…) et les commite dans sa PR avec les fichiers. Les captures de `docs/art/` n'ont pas de `.meta`.
  Pour les assets **déjà sur `main`**, la branche `art` ne génère rien : elle fait un pull de `main` avant chaque lot.
  Seule exception : un réglage d'import voulu (par exemple le remappage des matériaux d'un FBX), listé exactement
  dans la PR. **La session principale n'ajoute des `.meta` que pour des assets art jamais ouverts dans Unity.** (Les
  `.meta` des PR #1 et #2 sont sur `main` depuis le commit `255d6ff`.)

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
| Course | **8.5 m/s** (D-072 ; élan gardé au sol jusqu’à 20 m/s en tenant « avancer ») | |
| Vitesse max des gains (glissade, saut, dash, répulsion) | **20 m/s** | ; bunny hop mesuré jusqu'à **26 m/s** |
| Saut | **1.3 m** | rebord atteint au sol : ≤ 1.3 m |
| Double saut (2e saut en l'air) | **+1.6 m**, soit **≈ 2.9 m** au total | rebord atteint en sautant dessus : ≤ 2.9 m |
| Hissage automatique (D-074) | dessus du rebord jusqu'à **0.6 m au-dessus de la tête** : **3.7 m** avec un saut, **≈ 5.3 m** avec le double saut ; 0.3 s | un mur ne bloque vraiment qu'à **5.5 m+** ; dessus d'au moins **0.6 m** de profondeur et **1.8 m** de vide au-dessus pour qu'on s'y hisse |
| Saut en course | **≈ 5.5 m** (8.5 m/s) | trou franchissable par tous |
| Saut + double saut en course | **≈ 10 m** | |
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
- **Barillet (premier artefact, main gauche, D-032) : modèle `SM_Barillet` branché (2026-11-15, branche `barillet-modele`).**
  Flotte au-dessus de la paume gauche, axe de tir (+Z) vers l'avant de la vue. Repère `Barillet (vue)` en **(0, 0.11, 0.05)**
  dans le repère de la main gauche, rotation (0, 0, 0), **échelle 0.5 (= 0.05 m de diamètre, la largeur de la paume ;
  2026-11-17, il couvrait un tiers de l'écran à l'échelle 1)** : taille, décalage et flottement réglables dans
  `Barrel_Default` (« Vue FPS »). Le modèle entier tourne de **−60° autour de son Z local à chaque tir** (sens horaire vu du joueur), en
  0.08 s (courbe réglable, `Barrel_Default`), et finit toujours sur un multiple de 60°. Mesures (m) :

  | Pièce | Taille | Position (repère du barillet) |
  |---|---|---|
  | `SM_Barillet` (modèle livré, pivot au centre) | 0.100 × 0.100 × 0.111 | centre (0, 0, 0) |
  | 6 chambres `SM_Barillet_Chambre_01` à `_06` | maillage propre à chacune | 01 en haut (0, 0.0307), puis tous les 60° |
  | Canon (**greybox, à modéliser**) | cylindre Ø 0.022 × 0.10 | centre (0, 0.0307, 0.1055), dans l'axe de la chambre du haut |
  | Bouche du canon (départ de la traînée) | — | (0, 0.0307, 0.1565) |

  **Les munitions se lisent sur les chambres** : chargée = **émission** (blanc chaud provisoire (1, 0.86, 0.66) × 1.4 depuis le 2026-11-17, × 3 éblouissait,
  au-delà du seuil de bloom, réglable dans `Barrel_Default`, pas le violet de la télékinésie) ; vide = **couleur de base
  seule #1A120C** ; barillet inactif (objet tenu) : émission × 0.2. La chambre du haut est la prochaine à tirer ; au
  rechargement, les chambres se rallument une à une. Matériaux (provisoires, la liste sera validée avec la DA) : le corps
  utilise `Assets/_Project/Art/Barillet/M_Barillet_Laiton` (texture `T_Barillet_Laiton_BaseColor` seulement, ni métal ni
  brillance), les runes gardent le matériau du FBX, les chambres une copie de `M_Barillet_Chambre` avec l'émission activée
  (une couleur par chambre via MaterialPropertyBlock). **Reste à livrer :** le canon (point d'attache `Barrel_Muzzle` au
  bout). Recul géré par le code (la main recule de 3.5 cm et se relève de 8° à chaque tir).
- **Lanterne de feu (renard, D-065), greybox à remplacer :** tenue dans la paume gauche comme le barillet, centre à
  **(0, 0.075, 0.05)** dans le repère de la main gauche (réglable dans `FpsView_Default` : `lanternInHand`, rotation,
  échelle). Mesures (m) :

  | Pièce | Taille | Position (repère de la lanterne) |
  |---|---|---|
  | Socle et chapeau | 0.07 × 0.012 × 0.07 | y = −0.045 et +0.045 |
  | Toit | 0.04 × 0.012 × 0.04 | y = 0.056 |
  | 4 montants | 0.007 × 0.08 × 0.007 | aux coins (±0.03, ±0.03) |
  | Anse | cylindre Ø 0.006 × 0.044 | y = 0.068 |
  | **Flamme** (émission réglée par le code) | 0.032 × 0.045 × 0.032 | centre (0, −0.005, 0) |
  | Sortie du jet (point d'attache) | — | (0, 0.03, 0.05) |

  **Le jet part de la lanterne** (le cône du code part de 0.45 m sous les yeux, 0.22 m à gauche, 0.35 m devant) : le point
  `FX_Jet` du modèle doit être à cet endroit, en haut de la lanterne. Recharge **2.5 s** (et non 3 s).
  **États à lire sur le modèle :** flamme **allumée** (prête, émission orangée HDR (4, 1.7, 0.4)), **vive** pendant le jet
  (× 1.4), **éteinte** à la fin du jet puis **se rallume en 3 s** (taille et émission), **atténuée** (× 0.25) quand l'orbe tient
  un objet. La cage doit laisser voir la flamme (côtés ouverts ou vitres claires). Le **jet** (5 m, cône de 40°, 0.6 s) est
  fait de bouffées de flammes par le code (sphères additives, jaune → rouge) : un effet de particules dessiné à la main
  pourra les remplacer. Recul géré par le code (main poussée de 3 cm, tremblement).
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
| Goule | capsule Ø 1.1 × 1.8 m de haut (retours du lot : à la taille du joueur ; `SM_Goule` de 1.1 m agrandi × 1.64 par le jeu ; seuils de LOD réglés par le jeu, `lodTransitions`), 50 kg | basse, rampe ; yeux lumineux ; posture accroupie avant le bond (le code écrase le visuel à 60 % en hauteur) ; **grimpe aux murs** : le code bascule le visuel de 80° (plaquée contre la paroi) |
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
| Marchand | silhouette de **0.9 × 2.2 m** + enseigne **1.6 × 0.4 m** centrée à **5.5 m**, 1.6 m devant lui, au-dessus du milieu de la rangée d'offres (1.6 m au-dessus du plus haut texte) | robe **violette** (émission), enseigne violette lumineuse ; 4 orbes d'offre à 1.6 m de haut (une sur deux 0.6 m plus haut), **1.6 m devant lui**, espacées de 2.4 m, avec leur prix |
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

## Kit de construction de la carte aléatoire (2026-11-15, branche `carte-aleatoire`)

La carte du niveau est maintenant **tirée au hasard** à chaque partie à partir de **modules** (salles, carrefours) et de couloirs,
sur une **grille de 120 m**. Toute la structure (murs, sols, plafonds, piliers, arcs, portes, ponts, poutres, escaliers) est faite
d'**instances des prefabs du kit** (`Assets/_Project/Prefabs/Kit/Kit_*.prefab`, liste dans `Settings/Kit_Niveau1.asset`) :
**remplacer le contenu d'un prefab `Kit_*` par le vrai modèle met à jour toutes les salles, tous les couloirs et tous les modules
d'un coup.** Les pièces du kit sont **visuelles** : la collision est une boîte fusionnée posée par le code (ne pas mettre de
collider dans le modèle). Le batching statique est fait au chargement.

**Compatibilité avec un pas de 4 m :** 120 m = 30 pièces de 4 m ; salles de 48, 56 et 64 m, carrefours de 20 m, plafonds de 36 et
24 m, couloirs en pente de 16 m, pièces annexes de 12 m : **multiples de 4**. Ne le sont pas : couloirs plats de **14 m** de large
(3.5 pièces), **19 m** de haut (4.75), pièces annexes de **6 m** de haut (1.5), repaire de **26 m** de profondeur (6.5), et la
longueur d'un couloir entre un carrefour (20 m) et une salle (82 m, 102 m…). Le code pave avec des pièces entières et **comble le
reste par une seule pièce raccourcie au bord** (pièce mise à l'échelle) : prévoir des modèles qui supportent un raccourcissement,
ou livrer les demi-pièces de la liste ci-dessous.

**Pièces dont les modules et la carte ont besoin, par ordre de priorité** (nombre posé sur une carte de 49 cases : environ 60 000
pièces au total) :

| Priorité | Prefab | Cotes et pivot | Usage | État |
|---|---|---|---|---|
| 1 | `Kit_Mur4x4` | **4 × 4 m, 1 m d'épaisseur** ; pivot au pied, au milieu (x −2..2, y 0..4, z −0.5..0.5) | murs des salles, des couloirs, du repaire, des annexes ; côtés des étages pleins | greybox |
| 2 | `Kit_Sol4` | **4 × 4 m, 1 m d'épaisseur** ; pivot au milieu du dessus (y −1..0) | sols, plafonds, paliers, dessus des étages pleins | greybox |
| 3 | `Kit_Escalier` | marche de **4 m de large, 0.25 m de haut, 1 m de profondeur** ; pivot au milieu du bas de la contremarche (x −2..2, y 0..0.25, z 0..1) ; **étirée** en hauteur (marche pleine jusqu'au sol) et en profondeur selon la pente | toutes les pentes de liaison et les escaliers des modules : la collision est une pente lisse, les marches sont visuelles et restent sous la pente (les pieds ne s'y enfoncent pas) | greybox |
| 4 | `Kit_Arc` | `SM_Arc_Couloir` (14 × 11 × 1.6 m) | entrée de chaque couloir et tous les 20 m dans les couloirs plats | **livré** |
| 5 | `Kit_Pilier` | `SM_Colonne` (1.4 × 8 × 1.4 m) | colonnes des modules | **livré** |
| 6 | `Kit_Pont` | passerelle de **4 m de long, 3 m de large, tablier de 1 m** ; pivot au milieu du dessus ; étirée en largeur | ponts des modules et des couloirs (dessous à 10 m) | greybox |
| 7 | `Kit_Poutre` | `SM_Poutre` (6 × 0.5 × 0.5 m) | poutres des couloirs et des carrefours | **livré** |
| 8 | `Kit_PortePiece` | `SM_Porte_Piece` (4.4 × 5 × 1 m, ouverture 3 × 3 m) | porte des pièces annexes et des salles verrouillées | **livré** |
| 9 | `Kit_GrandePorte` | `SM_GrandePorte` (cadre 13 × 14 × 2 m) | sortie du niveau | **livré** |
| 10 | `Kit_PontEgout4` / `Kit_PontEgout14` | `SM_Pont_Egout_4m` / `_14m` | ponts des salles à canal | **livré** |
| 11 (à faire) | `Kit_Mur2x4`, `Kit_Mur4x3` | 2 × 4 m et 4 × 3 m, 1 m d'épaisseur | demi-pièces des couloirs de 14 m × 19 m, des annexes de 6 m, des longueurs non multiples de 4 | à créer |

Les pièces de **parcours** (poutres et plateformes non magnétiques, plateformes qui s'effondrent) restent hors du kit : ce sont des
objets de jeu (matériau rayé, D-051).

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

## Rendu : demandes à la session art et inventaire des props (2026-11-17)

Contexte : la lumière d'ambiance horreur est en place côté `main` (`HorrorLook`, réglages `HorrorLook_Default`) : étalonnage
par ambiance, halos des torches, torches qui vacillent, cône de la lanterne qui projette des ombres (3 lumières ombrées au plus,
les plus proches des joueurs), bloom, vignettage, grain. Captures : `docs/design/features/rendu/captures/ambiance_horreur/`.
Pour que ces flaques de lumière révèlent la matière, il manque des choses **dans le shader toon** (`Assets/Art/Shaders`, périmètre
de la DA, rien n'a été modifié) :

| Demande | Pourquoi | Piste |
|---|---|---|
| **Cartes normales** (`_BumpMap`, `_BumpScale`, mot-clé `_NORMALMAP`) dans `ItHunts_ToonForwardPass` et la passe DepthNormals | une torche à 4 m sur un mur et le cône de la lanterne arrivent **rasants** : avec une normale peinte, les joints et le relief des pierres sortent du noir ; aujourd'hui le mur est plat sous la lumière | normale en espace tangent (`TransformTangentToWorld`), utilisée pour `ndl` des lumières additionnelles et du contour ; textures `T_*_Normal` pour `M_Pierre`, `M_Dalles`, `M_Voute_Briques`, kit gothique |
| **Sol mouillé brillant** | des reflets des torches au sol donnent la profondeur d'une cave humide et guident l'œil le long des couloirs | reflet spéculaire **en aplat** (une bande, dans le style) piloté par un masque de flaques (`_WetMask`, ou le canal alpha de `T_Dalles_BaseColor`) et `_WetSmoothness` ; sur `M_Dalles`, `M_Egout` |
| **Ombres des lumières additionnelles** | le cône de la lanterne et les torches proches projettent des ombres | **déjà reçues** (`_ADDITIONAL_LIGHT_SHADOWS`, `light.shadowAttenuation`) : à garder ; vérifier que les modèles du décor ont bien leur passe ShadowCaster et que l'ombre en bande ne crénèle pas de près (biais) |
| **Tourbillon de l'énergie de l'orbe** | l'énergie de `SM_Orbe` est une sphère unie : sa rotation ne se voit pas | shader de vortex (UV sphériques réservées par `MATERIAUX.md`) : bandes qui défilent, couleur par `_EmissionColor` (le jeu l'écrit par bloc de propriétés) |
| **Corps du traqueur dans le noir** | `T_Traqueur_BaseColor` est très sombre : de loin, dans le brouillard des ambiances sombres, on ne voit que ses yeux | côté jeu : lumière d'appoint qui n'éclaire que lui (calque de rendu « Traqueur ») ; côté DA, au choix : texture un peu plus claire, ou un rim léger sur `M_Traqueur` |

**Inventaire des props du catalogue (prefabs `Assets/_Project/Prefabs/Props`) :**

- **Importés (modèle de la DA)** : `Prop_Caisse_Attrapable` (`SM_Caisse`), `Prop_Cage`, `Prop_Chaine`, `Prop_Colonne`,
  `Prop_ColonneBrisee`, `Prop_Echafaudage`, `Prop_Grille`, `Prop_Tapisserie`, `Prop_Torche` et `Prop_TorcheEteinte` (`SM_Torche`),
  `Prop_Tuyau` ; le kit de la carte (murs, sols, escalier, pont, poutre, pilier, porte de pièce, grande porte, arcs, kit gothique)
  et l'arène du boss (herse, leviers, treuil, plateformes) sont aussi au modèle.
- **Manquants (encore greybox)**, par nombre utilisé dans le niveau 1 : `Prop_Tonneau` (212), `Prop_Tonneau_Attrapable` (131),
  `Prop_Etagere` (117), `Prop_Caisse` (100, fixe), `Prop_CoffreDeco` (85), `Prop_Table` (82), `Prop_Banc` (79),
  `Prop_PileDeCaisses` (78), `Prop_Paille` (77), `Prop_Gravats` (72), `Prop_Statue` (69), `Prop_Chaise_Attrapable` (63),
  `Prop_Sarcophage` (52), `Prop_Ratelier` (40), `Prop_Armoire` (30), `Prop_Lit` (23), `Prop_Chaudron` (14), `Prop_MurEffondre` (11),
  `Prop_Autel` (4), `Prop_Poutre` (0) ; **`Prop_Lanterne` (258, une lumière : son halo est déjà dessiné par le jeu)**.
- **Hors catalogue, encore greybox** : `Coffre` (coffre à âmes), `Marchand_Artefact`, `Volant` (ennemi).

## Interface : intégration du lot A (PR #10, 2026-11-17)

- **HUD habillé branché** (`PlayerHud` + `PlayerHud.Skin.cs`, réglages `HudSkin_Default` relié par `Hud_Default`) : barre de vie
  (fond, traînée des dégâts récents, remplissage rouge sang, cadre, « PV/max » en EB Garamond SemiBold), cartouche des âmes (flamme,
  nombre), mini-carte ronde (fond, masque, cadre, icônes : coffre, coffre ouvert, bougie = marchand, flamme = salle verrouillée,
  porte finale verrouillée / ouverte, pastilles des alliés par personnage, tête de mort, flèche du joueur), messages et titres en
  Grenze Gotisch. Mesures d'UI-HANDOFF (coins, k, canal, rond). `MiniCarte_Coffre_Rare` n'est pas utilisée : il n'y a pas encore de
  coffre rare en jeu.
- **Différence avec UI-HANDOFF § 0 :** le canevas du HUD est **Screen Space - Overlay** (plus Screen Space - Camera) pour que
  l'étalonnage de l'Enragé ne le teinte pas ; sa racine épouse le rectangle de la caméra du joueur et l'échelle suit la hauteur
  de sa vue (référence 1080) : les tailles en pixels 1080p de la DA restent valables, écran partagé compris.
- **Atlas converti en V2 sur `main`** (demande de l'utilisateur) : *Sprite Packer* sur *Sprite Atlas V2 - Enabled*,
  `Assets/Art/UI/Atlas_HUD.spriteatlas` remplacé par `Atlas_HUD.spriteatlasv2` (mêmes dossiers `HUD` et `MiniCarte`, mêmes réglages :
  marge 4 px, ni rotation ni découpe serrée, 4096, compression haute qualité). **La branche `art-ui` doit faire un pull de `main`
  avant de toucher à l'atlas.**

## Bras FPS v2 : intégration (PR #11, 2026-11-17)

- Poses recuites (`FpsArmsSetup`, 20 os, 14 poses + 4 boucles) ; **orbe et artefact accrochés à `Socket_Flottement`** (ils suivent
  la main animée, plus le flottement de la vue) ; `Pose_MainOuverte` = **répulsion** (geste et maintenue), `Pose_Griffe` =
  **traction** (noms réglables dans `FpsView_Default`). Planche des 4 personnages et 14 poses :
  `docs/design/features/vue-fps/captures/bras_v2/`.
- **Orientation des bras (corrigée par la PR #14, 2026-11-17)** : les 8 FBX `SK_BrasFPS_*` sont à l'endroit (volume signé positif,
  1 339 à 1 686 cm³). Le contournement côté `main` (contour inversé pour les maillages retournés) est retiré : le contour des mains
  (`ItHunts/ContourMains`, coque qui cache les faces avant) suppose des **normales vers l'extérieur**. Le test
  `FpsViewTests.BrasFps_FacesVersLExterieur` vérifie chaque livraison (un bras retourné serait noir). Captures après la PR #14 :
  `docs/design/features/vue-fps/captures/bras_pr14/`.

## Bras du traqueur : allongement par l'échelle des os (D-070, 2026-11-17)

- **Ce que fait le code** : au-delà de la longueur du bras (environ 3.95 m de l'épaule au poignet), le bras se tend vers la cible,
  puis **l'os `Bras.L/.R` reçoit une échelle le long de son axe** (l'axe vers `AvantBras`, **Y** sur le modèle actuel) ; l'avant-bras,
  aligné, hérite de l'allongement ; **`Main` reçoit l'échelle inverse** et garde sa taille. Mesuré : **×2.3 à 5 m, ×2.4 à 10 m,
  ×3.2 à 15 m** (jusqu'à ×3.8 dans le pire cas). Portée (15 m) et zone qui tue inchangées. Captures avant/après :
  `docs/design/features/boss-traqueur/captures/bras_echelle/`.
- **Pour le nouveau maillage et le gréement** : garder la chaîne `Bras` → `AvantBras` → `Main` (mêmes noms, `.L`/`.R`), l'axe de
  chaque os **le long du membre** (l'os suivant posé sur un seul axe local), **pas d'échelle ni de rotation figée** sur ces os à
  l'export, et le coude droit possible (le code tend le bras avant d'allonger). **La texture s'étire avec le bras** (×3 à 4 en
  longueur) : préférer sur le bras et l'avant-bras un motif qui supporte l'étirement (veines, peau tendue) et **arrêter
  `M_Traqueur_Mains` au poignet** : aujourd'hui sa zone claire couvre le bas de l'avant-bras et devient une longue bande pâle
  quand le bras s'allonge.


### Traqueur v3 (PR #13) : intégration et retours (2026-11-17)

- **Intégré sur `main` (branche `animations`)** : prefabs et scènes reconstruits (4 scènes de jeu, `Test_Vitrine`), 3 matériaux du
  corps lus dans l'ordre `M_Traqueur`, `M_Traqueur_Tete`, `M_Traqueur_Mains` ; la lueur des pupilles ne va qu'aux matériaux qui ont un
  masque d'émission (`M_Traqueur` v3 n'en a plus : le corps reste éteint). Lecture des clips : vitesse au sol ÷ 3.71 (`Traqueur_Marche`)
  ou ÷ 2.29 (`Traqueur_PassageBas`), réglables dans `Stalker_Default` (`walkClipSpeed`, `crouchClipSpeed`) ; pied posé mesuré à
  0.48 m/s en poursuite (5.3 m/s avant). Allongement D-070 : ×1.8 à ×3.0 de 5 à 15 m, main à l'échelle 1. Sommet : 13.9 m en marche,
  8.5 m baissé (poutres à 10 m). Mesures et captures : `docs/design/features/boss-traqueur/captures/v3/`.
- **Tête v3 corrigée (PR #16, 2026-11-18)** : tête reprise sans borne, pupilles en disques de 0.4 m (hauteurs différentes, voulu).
  Repli `eyeGlints` **décoché** dans `Stalker_Default` (le code reste, réactivable). `StalkerVisibilityTests` passe sans lui :
  luminance 0.56 à 10 m, 0.34 à 30 m, 0.14 à 60 m dans le brouillard (seuil 0.06). **Attention à l'ordre des matériaux** : le FBX
  de la PR #16 range la tête en dernier (`M_Traqueur`, `M_Traqueur_Mains`, `M_Traqueur_Tete`) ; les instances des scènes gardaient
  l'ancien ordre (la tête prenait le matériau des mains) : traqueurs des scènes reconstruits, et `SkinnedBonesTests` vérifie
  désormais que chaque rendu a les matériaux de son FBX dans le même ordre. Capture : `tete_v3_pr16.png`.

## Menus : intégration de la PR #18 (art-menus, 2026-11-18)

Les cinq prefabs (`Menu_Titre`, `Menu_Campement`, `Menu_Choix`, `Menu_Pause`, `Menu_Parametres`) sont branchés tels quels sur la
logique du jeu, par l'assembly `ItHunts.Menus` (`Assets/_Project/Scripts/Menus/`). Les composants d'aperçu (`TitreApercu`,
`CampementApercu`, `ChoixPersoApercu`, `OptionDefilante`, `TeinteGroupe`, `SelectionAuSurvol`) sont **coupés** dans le jeu (ils
lisaient eux-mêmes le clavier, la manette ou l'EventSystem) ; leurs méthodes sont appelées (`Applique`, `Occupe`, `Libere`, `Pret`,
`Choisit`) et les teintes du § 2.1 de l'UI-HANDOFF sont posées par le code. Garder ces noms d'objets et ces méthodes, ou prévenir.
Réglages : `MenuSkin_Default` (prefabs, musique 0.55, feu à +28 dB puis limiteur, feu absent au titre). « Réduire les
animations » pilote `MenuFige`. Champ `ArtifactDefinition.description` ajouté. Captures : `docs/design/features/menus/captures/maquettes/`.

Retours à la DA :
- **`Menus_Cadre.png`** : la colonne de droite garde des pixels bleutés semi-transparents (ciel de la maquette ; par exemple
  (36, 57, 89) à alpha 182 sur la dernière colonne) : une bande bleue verticale se voit à droite du cadre de la pause et des
  paramètres.
- **Place 2 (pierre) vide** (J1 seul) : ni `+` ni lanterne éteinte livrés (seulement pour les places 3 et 4) ; la pierre reste nue.
- Pause : la maquette a 4 boutons ; le jeu en a jusqu'à 6 (Quitter l'écran partagé, outils de développement) : le 4e cartouche est
  reproduit au même pas et le cadre allongé (9 tranches). Si un autre pas est voulu, le donner.
- Manifeste : les 5 prefabs et les 4 shaders `ItHunts/UI/*` n'ont pas de ligne.
## Sons du jeu (lot B, 2026-11-21, branche `art-audio`)

20 sons générés par l'utilisateur sur Higgsfield (effets : Mirelo ; musiques : ElevenLabs Music v2.5), sources MP3 dans
`docs/art/audio/jeu/`, préparés par `Assets/Art/Source~/Audio/scripts/sons_jeu.py` (silences retirés, effets à −1 dBFS de
crête, boucles et musiques raccordées par fondu enchaîné, musiques au niveau RMS de la musique du menu). Ils remplacent les
sons synthétisés provisoires (`ProceduralSfx`) ; **le branchement se fait sur `main`** (les noms de méthode ci-dessous sont
ceux de `ProceduralSfx` et des vues qui les jouent).

| Son (`Assets/Art/Audio/…`) | Moment du jeu | Remplace | Import |
|---|---|---|---|
| `Traqueur/Sfx_Traqueur_Pas` | chaque pas du traqueur (`StalkerAudio`, cadence selon sa vitesse, hauteur 0,9 à 1,05) ; le fichier contient une suite de pas : jouer un extrait par pas ou à vitesse lente | `StalkerStep` | mono, décompressé |
| `Traqueur/Sfx_Traqueur_Souffle_Boucle` | souffle continu (volume plus fort en Chasse et Enragé) | `StalkerBreath` | mono, boucle, compressé en mémoire |
| `Traqueur/Sfx_Traqueur_Grognement` | grondement (`LastRoarTime`), passage en Alerte ou Recherche | `StalkerRoar` | mono, décompressé |
| `Traqueur/Sfx_Traqueur_Cri_Enrage_01`, `_02` | passage en mode Enragé (fin du timer), une variante au hasard | (nouveau) | mono, décompressé |
| `Traqueur/Sfx_Traqueur_Cri_Prise` | le traqueur attrape un joueur (mort en un coup) | (nouveau) | mono, décompressé |
| `Traqueur/Sfx_Traqueur_Cri_Apparition_01` (4,42 s), `_02` (5,0 s) | **apparition** du traqueur (sortie de sa cellule, entrée dans la zone) : rugissement qui monte, attaque en moins de 0,5 s ; une variante au hasard ; son 3D portant loin (80–120 m) | (nouveau, nuit 4) | mono, décompressé, **normalize décoché** (niveau calé par script) |
| `Traqueur/Sfx_Traqueur_Cri_Vu_01` (2,57 s), `_02` (2,22 s) | **« je t'ai vu »** : le traqueur repère un joueur (passage en Chasse) ; courte aspiration puis hurlement ; une variante au hasard, pas plus d'une fois toutes les 8 s | (nouveau, nuit 4) | mono, décompressé, **normalize décoché** |
| `Goule/Sfx_Goule_Alerte` | la goule annonce son bond (`GhoulMode.Annonce`) | `GhoulHiss` | mono, décompressé |
| `Goule/Sfx_Goule_Attaque_01`, `_02` | coup ou bond d'une goule, une variante au hasard | (nouveau) | mono, décompressé |
| `Goule/Sfx_Goule_Mort` | mort d'une goule | `Kill` (goule) | mono, décompressé |
| `Volant/Sfx_Volant_Cri` | le volant annonce son piqué (`FlyerMode.Annonce`) | `FlyerScreech` | mono, décompressé |
| `Volant/Sfx_Volant_Ailes_Boucle` | vol du volant (boucle 3D, volume selon la distance) | (nouveau) | mono, boucle, compressé en mémoire |
| `Volant/Sfx_Volant_Mort` | mort (éclatement) d'un volant | `Break` (volant) | mono, décompressé |
| `Armes/Sfx_Barillet_Tir_01`, `_02` | tir du barillet, une variante au hasard (alterner) | `BarrelShot` | mono, décompressé |
| `Armes/Sfx_Barillet_Recharge` | rechargement du barillet (début ; le clic de fin peut rester procédural) | `BarrelReloadStart` | mono, décompressé |
| `Armes/Sfx_Lanterne_Jet_Debut` | début du jet de feu de la lanterne | `LanternJet` (attaque) | mono, décompressé |
| `Armes/Sfx_Lanterne_Jet_Boucle` | jet de feu maintenu (boucle tant que le jet dure, s'arrête avec un fondu court) | `LanternJet` (corps) | mono, boucle, compressé en mémoire |
| `Musique/Musique_Jeu_Exploration_Boucle` (145,5 s) | exploration du niveau, hors chasse | `TensionDrone` (Alerte, Recherche) | stéréo, streaming |
| `Musique/Musique_Jeu_Enrage_Boucle` (87,5 s) | traqueur en Chasse puis Enragé : fondu croisé depuis l'exploration | `TensionDrone` (Chasse, Enragé) | stéréo, streaming |

Réglages d'import : effets courts **Decompress On Load** (aucune latence au tir), boucles d'effets **Compressed In Memory**,
musiques **Streaming** (chargement en arrière-plan) ; Vorbis 0,7, fréquence d'origine (44,1 kHz) ; mono forcé pour tous les
effets (sons 3D et armes en main), musiques en stéréo. Durées, silences retirés et raccords : `sons_jeu.json`.

## Nouveaux modules et passages du traqueur (main, 2026-11-21, D-078)

Échelle de jeu des 4 modules (64 × 64 m) et des passages du traqueur, pour remplacer les blocs par le kit (PR #25) :

| Élément | Mesure | Remarque |
|---|---|---|
| Plateforme où le traqueur se pose (coursive, mezzanine, anneau) | **10 m de profondeur au moins** | il fait 7 m de large ; plus étroit : joueurs seulement |
| Coursive basse de la prison | 14 m de profondeur, dessus à 5.5 m | la 2e (11 m) fait 4 m : joueurs seulement |
| Mezzanine de la caserne | 10 m, dessus à 6 m | |
| Anneau bas du puits | 10 m, dessus à 11 m ; anneau haut 3 m à 17 m | plafond du puits 30 m |
| Porte étroite | **4 × 5.2 m au moins** (mesure de la DA sur `Traqueur_PorteEtroite`) : il se faufile (vitesse × 0.45, baissé à 5.2 m) ; les portes des modules font 8 × 10 m | `narrowDoorWidth` 4, `narrowDoorHeight` 5.2 (D-080) ; plus petite : seul son bras passe |
| Bond | 22 m au plus, 6 m de dénivelé | arc de 5 m au-dessus de la ligne |
| Rebord / mur grimpable / chaîne | 8 / 16 / 16 m de haut au plus | |
| Saut depuis une chaîne | 18 m au plus | |
| Garde-corps, cordes, échelles, chaînes | collision pour les joueurs, traversée par la main et le regard du traqueur (A2, matin du 2026-11-21) | une collision pleine cachait le joueur à la main du traqueur |
| Cages suspendues du puits | dessus à 16 m au plus | au-dessus, cachette |

## Traqueur v4 : modèle verrouillé, mesures et portée (2026-11-21, branche `art-traqueur-v4`)

- **Modèle** : `Assets/Art/Models/Ennemis/SK_Traqueur_v4.fbx` (armature `Traqueur`, 68 os, **57 978 triangles** : corps 38 000,
  tête 15 827 avec bouche et dents, cheveux 4 152 ; 6 formes sur la tête). Matériaux `M_Traqueur_v4_Corps`, `_Tete`, `_Orbite`,
  `_Dents`, `_Cheveux` (toon). Vitrine : `Assets/Art/Scenes/Vitrine_Traqueur_v4.unity`. Planche : `docs/art/rendus/Traqueur/v4/planche_v4b.png`.
- **14 m** debout (sommet du crâne ; cheveux 14.2 m), pieds à 0, Z Unity en avant. **Une seule échelle uniforme** sur tout le
  modèle (aucune proportion changée) ; tête Tripo à × 1.05 (cou de la tête = cou du corps).
- **Verrou** : maillage, poids et formes ne changent plus (`docs/art/rendus/Traqueur/v4/EMPREINTE.md`). Les animations ne
  tournent que les os (plus la position de `Racine` et de `Bassin`) ; **aucune échelle d'os** : le code ne doit plus étirer les
  bras de cette version (D-070 concerne l'ancien traqueur).
- **Os utiles au code** : `Contact_Main_G/D` (sous les griffes), `Contact_Pied_G/D` (sous la plante, à z = 0), cibles `IK_Main_*`,
  `IK_Pied_*`, pôles `Pole_Coude_*`, `Pole_Genou_*` (non déformants) ; `Machoire` ; chaînes `Cheveux_1_*` à `Cheveux_7_*` (4 os)
  pour `BoneSpringChain` (simuler `_2` à `_4` ; collisions conseillées dans `NOTES.md`).

| Posture | Portée réelle |
|---|---|
| Debout, bras tendu | épaule à **11.1 m** (1.37 m de l'axe) ; bras tendu **8.3 m** ; portée horizontale **9.6 m** depuis l'axe ; vers le haut **19.4 m** |
| Voûté (repos des vidéos) | hauteur **11.7 m** ; épaules à 10.5 m ; portée devant **≈ 9.2 m** à hauteur d'épaule ; griffes ballantes à 2.3 m du sol |
| À quatre pattes | dos **≈ 8.3 m**, épaules 7.6 à 8 m ; portée devant ≈ 3.3 m (épaules hautes) à **≈ 7 m** (aplati) ; estimation |
| Se faufiler | **plus petite porte ≈ 4 × 6 m** (largeur × hauteur), estimation à confirmer avec `Traqueur_PorteEtroite` |

Les valeurs du § 0 (capsule Ø 7 × 14 m, épaule à 12 m, portée de main 15 m) décrivent l'ancien traqueur étiré par le code ;
avec la v4 sans étirement, la portée réelle de la main est celle du tableau ci-dessus (à reporter dans `Stalker_Default` par `main`).

## Clips du traqueur v4 (PR `art-traqueur-anim-*`, 2026-11-21)

FBX : `Assets/Art/Animations/Traqueur_v4/<clip>.fbx`, une armature seule par fichier, en rig **générique**.
- L'avatar est copié de `SK_Traqueur_v4`, et le nœud du root motion est `Traqueur/Racine` (il est réglé sur le modèle et sur chaque clip).
- 30 i/s, une clé par image, sans compression.
- Le clip prend le nom `Traqueur_<Action>`.
- Avancée vers +Z du modèle.

Les clés ne portent que sur les rotations, plus la position de `Racine` et de `Bassin` : aucune échelle d'os n'est modifiée.

Contrôles, vidéos côte à côte et planches : `docs/art/rendus/Traqueur/v4/anim/` (`ANIMATIONS.md`).

| Clip | Durée | Boucle | Root motion | Instants clés | Vitesse réelle du cycle |
|---|---|---|---|---|---|
| `Traqueur_Repos` | 3.2 s | oui | non | inspiration au plus haut à 1.8 s | 0 |
| `Traqueur_Marche_A` | 5.5 s | oui | **oui** (7.0 m par cycle) | appuis : main D 0.55 s, pied G 2.42 s, main G 3.36 s, pied D 5.0 s | **1.27 m/s** |
| `Traqueur_Marche_B` | 5.5 s | oui | **oui** (7.0 m par cycle) | appuis : main G 0.55 s, pied D 2.42 s, main D 3.36 s, pied G 5.0 s (les côtés exacts de la vidéo ; `_A` en est le miroir) | **1.27 m/s** |
| `Traqueur_CourseEnragee_A` | 2.6 s | oui | **oui** | appuis main D 0 / 1.3 s, pied G 0.2 / 1.5 s, main G 0.65 / 1.95 s, pied D 0.85 / 2.15 s ; **frappes au mur** (murs à 7 m de l'axe) : main G 0.25 s, main D 0.9 s et 2.2 s | **11 m/s** |
| `Traqueur_CourseEnragee_B` | 3.9 s | oui | **oui**, avec un écart latéral de 2.2 m qui revient à 0 | mêmes appuis que A ; main D plaquée au mur droit 0.9 s, frappe droite 2.2 s, main G au mur gauche 2.9 s | **11 m/s** |
| `Traqueur_CourseEnragee_C` | 2.0 s | oui | **oui** | sur deux jambes : pied G 0 / 1.0 s, pied D 0.5 / 1.5 s ; frappes : main D 0.6 s, main G 1.6 s | **9 m/s** |
| `Traqueur_Prise` | 3.5 s | non | oui (0.1 m) | à genoux ; bras droit tendu à **0.70 s** ; **prise (poing) 2.37 → 2.6 s** | 0 |
| `Traqueur_Bond` | 6.0 s | non | **oui**, 19.4 m vers l'avant, +5.4 m vers le haut (sur un bloc) | décollage **2.05 s** ; mains sur le bloc **4.6 s**, pieds **4.85 s** | — |
| `Traqueur_BondPrise` | 5.0 s | non | **oui**, 8.4 m vers l'avant | décollage **1.05 s** ; **poing 2.55 → 2.72 s** ; mains au sol **3.0 s**, pieds **3.15 s** | — |
| `Traqueur_PriseRebord` | 6.0 s | non | oui (0.2 m) ; à jouer au sommet d'un rebord de 6 m | bras tendu vers le bas **1.75 → 3.1 s** ; **poing 3.25 s** ; retour du poing 4.4 → 5.2 s | 0 |
| `Traqueur_Reception` | 5.0 s | non | **oui** (chute : +6 m vers l'avant, −8.5 m) | mains au sol **0.35 s**, pieds **0.5 s**, le plus bas 0.85 s ; **sortie possible dès 1.1 s** | — |
| `Traqueur_Escalade` | 4.5 s | non | **oui** (+9.5 m vers l'avant, +7.5 m) ; bloc de 7.5 m | main D en haut **0.25 s**, main G **0.6 s** ; détente **2.55 s** ; pieds sur le bloc **3.25 s** | — |
| `Traqueur_ChaineGrimpe` | 5.0 s | non | **oui** (+4.8 m de montée) | saut **0.35 s**, mains sur la chaîne **0.55 s** ; montée **1.1 → 2.9 s** en 2 cycles de 0.9 s (+2.4 m chacun ; cette partie boucle) | montée 2.7 m/s |
| `Traqueur_ChaineSaut` | 6.0 s | non | **oui** (+15 m vers l'avant, jusqu'à un bloc de 3.3 m) | balancement arrière 2.0 → 2.8 s, avant 2.8 → 3.4 s ; **lâcher 3.4 s** ; mains sur le bloc **4.2 s**, pieds **4.35 s** | — |
| `Traqueur_ChainePrise` | 6.0 s | non | non (pendu, accroche fixe) | bras droit tendu vers le bas **0.75 → 3.5 s** ; **poing 3.75 s** ; main sur la chaîne **5.0 s** | 0 |
| `Traqueur_PorteEtroite` | 7.0 s | non | **oui** (7 m vers l'avant) | corps abaissé **2.5 → 3.5 s** (hauteur 5.2 m au droit de la porte) ; tête dans la porte vers 3 s ; jambes passées vers 7 s | **1.0 m/s** |

Clips sans vidéo, **non faits** : `Traqueur_MurGrimpe` (pas de vidéo « grimper un mur ») et `Traqueur_Visage_Cri` / `_Grognement` / `_Machoire` (pas de vidéo du visage) ; les 6 formes du visage peuvent être animées par le code en attendant.
Passage d'une porte : avec `Traqueur_PorteEtroite`, le corps tient dans **4 m de large et 5.2 m de haut** ; les membres s'écartent jusqu'à 3.6 m de l'axe près du sol.

Pour jouer la marche à une autre vitesse sans glissement, il faut multiplier la vitesse de lecture par (vitesse voulue / 1.27). Sinon, utiliser le root motion.

## Traqueur v4 dans le jeu (main, matin du 2026-11-21, D-080, D-081)

| Élément | Valeur | Remarque |
|---|---|---|
| Modèle | `SK_Traqueur_v4` tel quel (aucune échelle d'os, aucune déformation) | le jeu s'adapte au modèle |
| Portée de la prise | bras 8.3 m + **fente du corps 6.5 m** (0.4 s, finie à l'impact) | clip de prise : l'impact doit tomber à la fin de la fente (donner l'instant d'impact) |
| Épaule (pose voûtée) | 10.5 m de haut, 1.37 m de côté, 0.9 m devant | sert à la portée et aux cachettes |
| Par une porte de pièce | 10.3 m (bras + 2 m d'épaule dans la porte de 3 m) | pièces annexes du niveau aléatoire : 12 × 8 m |
| Plus petite porte | 4 × 5.2 m (il se faufile, mesure sur le clip) | `Traqueur_PorteEtroite` |
| Baissé (ponts, poutres) | 8.5 m | `Traqueur_PassageBas` |
| Cheveux | ressort maison, os `Cheveux_N_2` à `_4`, obstacles conseillés (NOTES.md) | réglages `Stalker_Default` > boneChains |
| Clip manquant | mouvement provisoire procédural (dos voûté, bras ballants, cycle de marche) | remplacé par chaque clip `Traqueur_<Action>` livré |

## Menu des touches (main, matin du 2026-11-21, A5, D-083)

Paramètres > Touches : panneau des menus (`Carte_Legende_Cadre`), marque de focus (`Carte_Rose`), cartouche de ligne
(`Menus_Cartouche_M`), polices des menus. **Provisoire, à fournir :** icônes des touches (clavier, souris, boutons de manette
Xbox / PlayStation), onglets « Clavier / souris » et « Manette » (état choisi et non choisi), bouton « Réinitialiser » ; la page
Paramètres a une 7e ligne « Touches » (copie d'une ligne existante) et « Retour » descend d'un cran.


## Eau dans les niveaux : retours du jeu (main, 2026-11-21, point 10 du playtest)

Vérifié côté jeu : **toutes** les surfaces d'eau du niveau aléatoire utilisent `M_Eau_Toon` (`ItHunts/EauToon`, 45 surfaces sur
une carte avec la citerne) ; le shader avance avec `_Time` en jeu (et dans le build : même shader, profil `PC_RPAsset` avec textures
de profondeur et opaque). Mesures (deux images à 5 s d'écart) : eau de test `Test_Eau` 16 % de pixels qui changent, bassin d'un
niveau 35 %. Le playtest la trouve pourtant « fixe, comme une texture ». **Demandes à la DA** (le jeu ne touche pas au matériau) :

1. **Chutes d'eau figées** : l'eau qui tombe des tuyaux de la citerne inondée est un plan vertical avec le même shader ; les uv
   sont en xz monde, donc **rien ne coule vers le bas** (0 % de pixels qui changent en 5 s, `docs/design/level/captures/eau_chute_figee.png`).
   Il faudrait un mode « chute » (uv selon y, défilement vertical rapide, écume au pied) ou un matériau dédié.
2. **Animation trop lente pour un niveau sombre** : courant `_Courant` 0.35 m/s sur une tuile de 6 m, ondulation `_Ondulation` 0.04 ;
   à distance et dans le noir, le mouvement ne se lit pas (`eau_bassin_5s_ecart.png`). Proposition : courant ≈ 0.8 m/s, ondulation
   ≈ 0.08, écume qui bouge plus (`_EcumeBruit`), reflets des torches plus présents ; à valider sur `Test_Eau` puis en jeu.

## Sons de la nuit du 2026-11-22 (lot 6, branche `art-sons-2`)

51 sons générés sur Higgsfield (effets et ambiances : Mirelo ; musiques : ElevenLabs Music v2.5 ; 52 générations, une refaite), pour 57.6 crédits sur
le plafond de 60 ; sources MP3 dans `docs/art/audio/nuit3/`, préparés par `Assets/Art/Source~/Audio/scripts/sons_nuit3.py`
(silences retirés, queues de bruit coupées à −40 dB sous la crête, effets à −1 dBFS, sons d'interface de 0.45 s au plus,
boucles raccordées par fondu enchaîné, battement de cœur calé sur trois battements exacts de 0.838 s, ambiances à −26 dB
RMS sous la musique, musiques au niveau de la musique du menu). Avec le lot B, **ils remplacent tous les sons synthétisés
provisoires de `ProceduralSfx`** (colonne « Remplace »). Le branchement se fait sur `main`.

| Son (`Assets/Art/Audio/…`) | Moment du jeu | Remplace | Import |
|---|---|---|---|
| `Musique/Musique_Chateau_Boucle` (57.515 s) | exploration de l'acte II (étages du château) | TensionDrone (Alerte, Recherche) dans l'acte II | stéréo, streaming |
| `Musique/Musique_Jardin_Boucle` (57.5 s) | exploration de l'acte III (jardin) | TensionDrone dans l'acte III | stéréo, streaming |
| `Musique/Musique_Boss_Boucle` (57.362 s) | combat contre le boss (arène de l'acte III) | (nouveau) | stéréo, streaming |
| `Ambiance/Amb_Cave_Boucle` (18.0 s) | ambiance de fond de l'acte I (gouttes, vent dans les galeries), sous la musique | (nouveau) | stéréo, streaming, boucle |
| `Ambiance/Amb_Chateau_Boucle` (17.994 s) | ambiance de fond de l'acte II (vent dans les fenêtres, craquements) | (nouveau) | stéréo, streaming, boucle |
| `Ambiance/Amb_Jardin_Boucle` (18.0 s) | ambiance de fond de l'acte III (vent dans les arbres, corbeaux lointains) | (nouveau) | stéréo, streaming, boucle |
| `Monde/Sfx_Porte_Ouverture` (2.5 s) | une porte s'ouvre (porte de module, de pièce, de sortie) | (nouveau) | mono, décompressé |
| `Monde/Sfx_Porte_Verrouillee` (1.5 s) | le joueur essaie une porte verrouillée | Refusal (porte) | mono, décompressé |
| `Interface/Sfx_Menu_Interaction` (0.45 s) | ouverture d'un menu d'interaction (porte à prix, marchand, coffre) | (nouveau) | mono, décompressé |
| `Interface/Sfx_Achat` (1.2 s) | achat confirmé (âmes dépensées) | (nouveau) | mono, décompressé |
| `Orbe/Sfx_Ame_Collectee` (1.0 s) | une âme est aspirée par l'orbe (une variante de hauteur 0.9 à 1.1 par âme) | SoulPickup | mono, décompressé |
| `Orbe/Sfx_Attraper` (1.0 s) | télékinésie : objet attrapé | (nouveau) | mono, décompressé |
| `Orbe/Sfx_Relacher` (0.8 s) | télékinésie : objet relâché | (nouveau) | mono, décompressé |
| `Orbe/Sfx_Lancer` (1.0 s) | télékinésie : objet lancé | (nouveau) | mono, décompressé |
| `Impacts/Sfx_Impact_Leger` (0.282 s) | petit objet lancé ou balle qui touche un mur | ImpactHit (léger) | mono, décompressé |
| `Impacts/Sfx_Impact_Lourd` (1.5 s) | caisse lancée qui touche (ImpactDamage), chute d'un gros objet | ImpactHit (lourd) | mono, décompressé |
| `Joueur/Sfx_Dash` (1.0 s) | dash | (nouveau) | mono, décompressé |
| `Joueur/Sfx_Saut` (0.6 s) | saut (et double saut, hauteur 1.1) | (nouveau) | mono, décompressé |
| `Joueur/Sfx_Atterrissage` (0.8 s) | atterrissage (volume selon la vitesse de chute) | (nouveau) | mono, décompressé |
| `Joueur/Sfx_Glissade_Boucle` (1.7 s) | glissade (boucle tant qu'elle dure) | (nouveau) | mono, boucle, compressé en mémoire |
| `Joueur/Sfx_WallJump` (0.8 s) | wall jump | (nouveau) | mono, décompressé |
| `Joueur/Sfx_Hissage` (1.0 s) | hissage automatique sur un rebord | (nouveau) | mono, décompressé |
| `Combat/Sfx_Parade_Reussie` (0.997 s) | parade réussie | Parry | mono, décompressé |
| `Combat/Sfx_Parade_Ratee` (0.99 s) | parade ratée | (nouveau) | mono, décompressé |
| `Combat/Sfx_Etourdissement` (1.5 s) | ennemi étourdi | Stun | mono, décompressé |
| `Combat/Sfx_Headshot_Critique` (0.573 s) | tir critique à la tête | (nouveau) | mono, décompressé |
| `Combat/Sfx_Balle_Paralysante` (0.779 s) | balle paralysante qui touche (cible figée) | (nouveau) | mono, décompressé |
| `Combat/Sfx_Explosion_Kill` (1.8 s) | explosion de zone sur un kill | Kill (explosion) | mono, décompressé |
| `Joueur/Sfx_Chute_Vide` (2.498 s) | chute dans le vide | (nouveau) | mono, décompressé |
| `Joueur/Sfx_Plongeon_Eau` (1.8 s) | plongeon dans l'eau | (nouveau) | mono, décompressé |
| `Traqueur/Sfx_Coeur_Boucle` (2.514 s) | cœur qui bat quand le traqueur est proche : 3 paliers (volume 0.3 / 0.6 / 1 et vitesse 1 / 1.25 / 1.5 à 60 / 40 / 20 m) | (nouveau) | mono, boucle, compressé en mémoire |
| `Traqueur/Sfx_Traqueur_Arrivee_Porte` (3.0 s) | le traqueur arrive par une porte (enfonce l'ouverture) | (nouveau) | mono, décompressé, 3D |
| `Boss/Sfx_Boss_Cri_Phase` (3.0 s) | boss : changement de phase | (nouveau) | mono, décompressé, 3D |
| `Boss/Sfx_Boss_Attaque_Griffe` (1.5 s) | boss : attaque de griffe | (nouveau) | mono, décompressé, 3D |
| `Boss/Sfx_Boss_Attaque_Morsure` (1.5 s) | boss : attaque de morsure (bond) | (nouveau) | mono, décompressé, 3D |
| `Boss/Sfx_Boss_Blessure` (1.5 s) | boss : blessure (point faible touché) | (nouveau) | mono, décompressé, 3D |
| `Boss/Sfx_Boss_Mort` (4.0 s) | boss : mort | (nouveau) | mono, décompressé, 3D |
| `Interface/Sfx_UI_Survol` (0.45 s) | interface : survol d'un bouton | (nouveau) | mono, décompressé |
| `Interface/Sfx_UI_Clic` (0.45 s) | interface : clic, validation | (nouveau) | mono, décompressé |
| `Interface/Sfx_UI_Retour` (0.45 s) | interface : retour, fermeture | (nouveau) | mono, décompressé |
| `Goule/Sfx_Goule_Mort_Organique` (0.8 s) | mort d'une goule (son organique court, **à la place de l'ancien son de mort**) | Kill (goule) et Sfx_Goule_Mort | mono, décompressé, 3D |
| `Orbe/Sfx_Traction_Debut` (0.991 s) | magnétisme : la traction s'accroche | TractionStart | mono, décompressé |
| `Orbe/Sfx_Traction_Boucle` (1.7 s) | magnétisme : traction maintenue (boucle, hauteur selon la vitesse) | TractionHum | mono, boucle, compressé en mémoire |
| `Orbe/Sfx_Traction_Fin` (0.8 s) | magnétisme : fin de la traction | TractionEnd | mono, décompressé |
| `Orbe/Sfx_Repulsion` (1.195 s) | magnétisme : répulsion | Repel | mono, décompressé |
| `Interface/Sfx_Refus` (0.45 s) | action refusée (pas de charge, recharge en cours) | Refusal | mono, décompressé |
| `Interface/Sfx_Pret` (0.45 s) | charge ou recharge à nouveau prête | Ready | mono, décompressé |
| `Impacts/Sfx_Caisse_Brisee` (1.5 s) | une caisse se brise | Break (caisse) | mono, décompressé |
| `Armes/Sfx_Lanterne_Rallumage` (1.2 s) | la lanterne se rallume | LanternRelight | mono, décompressé |
| `Armes/Sfx_Barillet_Recharge_Fin` (0.266 s) | fin du rechargement du barillet (clic) | BarrelReloadDone | mono, décompressé |
| `Monde/Sfx_Vague_Cor` (2.5 s) | une nouvelle vague arrive | WaveHorn | mono, décompressé |

Le son de mort « aigu » des goules n'est plus utilisé : `Sfx_Goule_Mort_Organique` (court, organique) le remplace. Ambiances : une par acte, en boucle sous la musique, volume 0.6 ; musiques : fondu croisé de 2 s avec l'exploration.

## Actes de la nuit 3 dans le jeu (main, 2026-11-22, D-092 à D-099)

Mesures à respecter pour les prochains kits et salles d'exemple (le générateur `ActPlan` / `ActLevel` les lit dans
`Assets/_Project/Settings/Actes/Act_1..3`) :

| Élément | Mesure | Réglage | Remarques |
|---|---|---|---|
| Portes entre salles des actes | **12 m de large**, 9 m de haut (10 m dans l'acte II) | `doorWidth`, `doorHeight` | aucun piédroit dans l'ouverture (le traqueur fait 7 m de large) ; les piédroits du kit se posent **à l'extérieur** de l'ouverture |
| Plafonds | acte I 16 à 22 m ; acte II 22 à 30 m (lustres) ; acte III ciel ouvert | `ceiling` | les rayons de lune de l'acte II tombent des hautes fenêtres du kit |
| Marches entre étages | 3 m par étage, rampes lisses pour la collision, escalier du kit pour la vue | `floorStep` | |
| Surface des salles | 700 à 2 300 m² (acte I), 800 à 2 600 m² (acte II), 1 200 à 3 400 m² (acte III) | `roomArea` | goules : 0.22 / 0.24 / 0.20 par 100 m² |
| Allées | 10 m libres dans les salles sans bassin ni fosse, anneau de 10 m autour des bassins et des fosses | `ActPlan.PlanInterior` | ne pas poser de mobilier à collisions dans ces bandes |
| Mobilier, battants de porte, candélabres | **enjambés par le traqueur** (`StalkerPassesThrough`) | | un objet décoratif ne doit jamais fermer un passage |
| Porte de sortie (menu de la DA) | menu d'interaction à **9 m**, point de visée à 1.8 m du sol ; salle verrouillée : 8 m | `ExitDoor`, `LockedDoor` | plus de texte 3D ni de lueur rouge / orange : `Panneau_Porte` affiche nom et prix en âmes |
| Prix de la sortie | 200 âmes + 50 par acte (acte II : 250) | `Zone_Acte*.exitPrice` | |
| Arène du boss | cuvette de **46 m de rayon**, falaises de 12 m (non magnétiques au-dessus), palier en haut de l'escalier à 8.5 m, 6 piliers brisés (dessus plat à 10 m), statue (socle à 7.5 m) | `BossArena` | les points faibles du boss se visent depuis ces hauteurs |
| Fin de run | `Panneau_FinDeRun` : **13 lignes** (le jeu reproduit la 7e ligne de la maquette et resserre le pas) | `Night3ArtUi.FitRows` | une maquette à 13 lignes éviterait la police réduite |

Sons : `Sfx_Impact_Lourd` joue pour un objet de 15 kg ou plus (caisse), `Sfx_Boss_Attaque_Morsure` à la réception du bond du
boss. Les 51 sons de la nuit sont tous branchés.
