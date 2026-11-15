# Rendu : cel-shading, contours et brouillard

**Étape 1 du chantier rendu (branche `art`, 2026-11-15).** Shaders maison, sans package externe, dans
`Assets/Art/Shaders/`. Banc d'essai : `Assets/Art/Scenes/Test_Rendu.unity`. Captures : `docs/art/rendus/Rendu/`.
**Décisions provisoires (2026-11-15, le DA pourra ajuster l'ambiance) : réglage intermédiaire (§ 3), contours en mode
« clair dans le noir » (§ 2), lanterne du personnage à confirmer (§ 3 bis).** La scène de test est enregistrée dans
cet état.

La tension à résoudre : l'horreur veut du noir franc, la vitesse (18 à 22 m/s) veut une géométrie lisible. Les leviers
sont, dans l'ordre de leur effet : **l'ambiance de la scène** (plancher d'éclairage des faces non éclairées), **le
brouillard** (jusqu'où on voit), **le mode de contour** (arêtes lisibles dans le noir sans éclairer le décor).

## 1. Shader toon : `ItHunts/Toon`

Fichiers : `ItHunts_Toon.shader`, `ItHunts_ToonInput.hlsl` (propriétés), `ItHunts_ToonForwardPass.hlsl` (éclairage),
`ItHunts_ToonUtilityPasses.hlsl` (ombres, profondeur, normales).

**Calcul :** couleur de base × (teinte d'ombre + ambiance + lune en bandes + bougies en paliers) + rim, puis brouillard,
puis émission. Pas de reflet spéculaire ni de métal (règle de style). Compatible SRP Batcher, Forward et Forward+
(boucle de lumières en grappes d'URP 17.3), instanciation GPU.

**Compatibilité avec le code du jeu :** les noms d'URP Lit sont gardés (`_BaseMap`, `_BaseColor`, `_EmissionColor`,
mot-clé `_EMISSION`) : les lueurs des objets attrapables, les chambres du barillet et le retour de dégâts marchent sans
changer le code.

**Passes :** `ForwardToon` (UniversalForward), `ShadowCaster` (ombres portées, même biais qu'URP), `DepthOnly`,
`DepthNormals`. Cette dernière est indispensable : sans elle, les contours et la SSAO ne voient pas les normales.

### Valeurs réglables (matériau) et valeurs de départ

| Groupe | Propriété | Départ | Effet |
|---|---|---|---|
| Couleur | `_BaseMap`, `_BaseColor` | blanc | texture peinte et teinte |
| | `_AlphaClip`, `_Cutoff` | non, 0.5 | découpe alpha (grilles, chaînes) |
| Bandes (lune) | `_BandCount` | **3** | nombre de bandes, ombre comprise (2 à 4) |
| | `_BandThreshold` | **0.15** | position de la 1re transition (0 = sur la ligne d'ombre) |
| | `_BandSoftness` | **0.02** | largeur des transitions (0.001 = coupure franche) |
| | `_ShadowColor` | **(0.028, 0.034, 0.075)** bleu nuit | teinte de la bande d'ombre, jamais du noir pur |
| | `_AmbientInfluence` | **1** | part de l'ambiance de la scène (0 à 2) |
| | `_ReceiveShadows` | oui | ombres reçues (elles passent dans les bandes : ombres en aplats) |
| Bougies | `_PointSteps` | **3** | paliers d'atténuation des lumières ponctuelles (1 à 5) |
| | `_PointThreshold` | **0.35** | position du 1er palier : le bord de l'îlot est vers 93 % de la portée |
| | `_PointSoftness` | **0.015** | bord des paliers (net) |
| | `_PointFalloff` | **0.8** | courbe de la portée (< 1 : îlots plus larges et plus pleins) |
| | `_PointWrap` | **0.2** | éclaire un peu les faces de biais |
| Rim | `_Rim` | **désactivé** | liseré de silhouette |
| | `_RimColor` | (0.35, 0.45, 0.7) HDR | |
| | `_RimWidth`, `_RimSoftness` | 0.25, 0.02 | |
| | `_RimIntensity` | 0.5 | |
| | `_RimLitOnly` | 0 | 0 : partout ; 1 : seulement là où une lumière touche |
| Émission | `_Emission` (`_EMISSION`) | désactivé | |
| | `_EmissionColor` | noir, HDR | > 1 : passe le seuil du bloom (1) |
| | `_EmissionMap` | blanc | masque : où ça brille |
| | `_EmissionThroughFog` | **0** | 0 : s'efface dans le brouillard comme le décor ; 1 : le traverse (yeux du traqueur : 0.7) |
| Rendu | `_Cull` | Back | |

Les lumières ponctuelles utilisent leur **portée (Range)** pour l'îlot, pas l'atténuation physique en 1/d² : une bougie
dessine une tache nette de la taille de sa portée, en 3 paliers. Ombres des bougies : activées dans la scène de test
(ombres douces, coupées en aplats par le shader).

## 2. Contours : `ItHunts/Contours`

Shader plein écran pour la **Full Screen Pass Renderer Feature** d'URP (aucun code C#). Détection par croix de Roberts
sur la **profondeur** (relative à la distance) et les **normales** ; épaisseur en pixels d'une image 1080p, quelle que
soit la résolution. Le trait s'efface avec la distance **et avec le brouillard de la scène** (même formule que le décor).
Matériau : `Assets/Art/Materials/Rendu/M_Contours.mat`.

| Propriété | Départ | Effet |
|---|---|---|
| `_Mode` | **ClairDansLeNoir** (retenu) | `Noir` : trait de couleur partout ; `ClairDansLeNoir` : noir à la lumière, plus clair que le fond dans le noir |
| `_Thickness` | **1.5 px** | épaisseur à 1080p (0.5 à 4) |
| `_OutlineColor` | (0.01, 0.008, 0.012) | encre presque noire |
| `_OutlineOpacity` | 1 | |
| `_DepthThreshold` | **0.06** | écart de profondeur relatif qui fait un trait (silhouettes) |
| `_DepthSoftness` | 0.03 | |
| `_GrazingBoost` | **6** | relève le seuil aux angles rasants (sols vus de loin sans traits parasites) |
| `_NormalThreshold` | **0.3** | 1 − cos de l'angle entre deux faces (≈ 45°) : arêtes |
| `_NormalSoftness` | 0.1 | |
| `_FadeStart`, `_FadeEnd` | **40 → 100 m** (intermédiaire) | fondu avec la distance, réglé avec le brouillard (§ 3) |
| `_UseFog` | oui | le trait disparaît avec le décor dans le brouillard |
| `_LiftColor` | **(0.07, 0.075, 0.1)** | mode clair : couleur ajoutée au fond pour le trait |
| `_LiftThreshold` | **0.06** | mode clair : luminance sous laquelle le trait s'éclaircit |

**Comparaison des modes** (réglage intermédiaire, mêmes vues) : `Contours_noir_*.jpg`, `Contours_clair-dans-le-noir_*.jpg`,
`Contours_aucun_*.jpg`. En mode noir, le trait ne se voit qu'à la lumière (dans le noir, il est noir sur noir). En mode
**clair dans le noir**, l'arc, les colonnes, le mannequin et la silhouette du couloir se lisent dans les zones sans
lumière, sans éclairer le décor : c'est le meilleur levier pour la lisibilité à pleine vitesse sans renoncer au noir.
Défaut visible : les bords des ouvertures de la voûte font de longues lignes fines (vraies arêtes, à garder ou à
atténuer avec `_LiftColor`).

**Les mains** sont dessinées après le monde (passes « ItHunts Mains ») : la passe de contours, injectée avant les
transparents, ne les touche pas. Leur contour devra venir de leur matériau (coque inversée), comme prévu dans
`ART-HANDOFF.md` § 5 bis.

## 3. Les trois réglages

Réglages **de scène** (Lighting > Environment et Fog, intensités des lumières, fondu du contour) : le matériau toon ne
change pas d'un réglage à l'autre. Brouillard **linéaire**. La lune est une directionnelle bleue `(0.55, 0.66, 1)` qui
n'entre que par les ouvertures de la voûte ; les bougies sont des ponctuelles `(1, 0.36, 0.16)`.

| Réglage | Ambiance (Flat) | Brouillard : couleur | Début → fin | Lune | Bougies (intensité, portée) | Fondu du contour |
|---|---|---|---|---|---|---|
| **Horreur** | (0.012, 0.013, 0.020) | (0.004, 0.005, 0.009) | 6 → 55 m | 0.12 | 1.4, 9 m | 30 → 70 m |
| **Intermédiaire** | (0.055, 0.058, 0.080) | (0.012, 0.014, 0.024) | 10 → 85 m | 0.25 | 1.8, 11 m | 40 → 100 m |
| **Lisible** | (0.130, 0.130, 0.165) | (0.030, 0.034, 0.052) | 18 → 130 m | 0.45 | 2.2, 13 m | 60 → 140 m |

Pour comparaison, les niveaux actuels du jeu ont une ambiance de **(0.16, 0.16, 0.20)** sans brouillard : « lisible »
en est proche.

**Captures** (même vue par réglage, FOV 70°, 1920 × 1080, contour noir) : `Rendu_<réglage>_couloir.jpg`,
`Rendu_<réglage>_pont.jpg` (depuis le quai du canal, face à l'arche du pont), `Rendu_<réglage>_traqueur.jpg`
(traqueur à 69 m).

**Ce qu'on voit :**
- **Horreur :** hors des îlots de bougie et des taches de lune, le décor est noir ; la profondeur du couloir ne se lit
  plus au-delà de 20 m. Le traqueur n'est visible **que par ses yeux** (émission qui traverse le brouillard à 70 %) :
  très efficace pour la peur, mais le joueur ne lit ni la taille ni la distance du géant.
- **Intermédiaire :** murs et sol se devinent à 30–40 m, les volumes (arc, colonnes) se lisent en silhouette ;
  traqueur : yeux nets, corps à peine visible à 69 m.
- **Lisible :** tout le couloir se lit, le visage clair du traqueur se détache au bout du brouillard ; l'ambiance
  « cave » reste, mais la peur repose alors sur le son et le mouvement.
- **Traqueur (silhouette de 14 × 7 m, visage clair, yeux rouges à 12.4 m) :** ce qui se lit de loin, ce sont les yeux.
  Le visage clair ne sort du noir qu'en réglage lisible, ou à moins de ≈ 50 m en intermédiaire.

## 3 bis. Lanterne du personnage (à confirmer)

**But :** que le mur longé en wall run reste lisible en réglage intermédiaire, sans éclaircir le reste du décor.

**Essai :** une lumière ponctuelle ajoutée à l'instance du joueur **dans `Test_Rendu` seulement** (objet
`Lanterne (scène de test)`, enfant de `Player` ; le prefab joueur n'est pas modifié). Elle passe par les mêmes paliers
du shader toon que les bougies.

| Réglage | Valeur de départ | Notes |
|---|---|---|
| Couleur | **(0.62, 0.16, 0.07)** rouge sombre « renard » | proche des bougies, plus sombre |
| Intensité | **0.6** | 1.0 essayée : mur plus lisible, mais le sol et les mains rougissent nettement |
| Portée | **6 m** | bord de l'îlot vers 5.6 m (paliers du matériau) |
| Position | (0, 1.1, 0.3) dans le repère du joueur | à hauteur de hanche, un peu devant |
| Ombres | **aucune** | coût et ombre du joueur évités |

Réglables dans l'inspecteur de la lumière (intensité, portée, couleur) ; les paliers viennent du matériau toon
(`_PointSteps`, `_PointThreshold`, `_PointFalloff`).

**Captures** (`docs/art/rendus/Rendu/`) :
- vue fixe du couloir dans une zone sans bougie (z = 85) : `Lanterne_couloir_avec.jpg`, `Lanterne_couloir_sans.jpg`,
  `Lanterne_couloir_avec_intensite1.jpg` ;
- vue fixe contre le mur ouest (position de wall run) : `Lanterne_mur_avec.jpg`, `Lanterne_mur_sans.jpg`,
  `Lanterne_mur_avec_intensite1.jpg` ;
- en mouvement (Play Mode, FOV du jeu) : course à 18 m/s `Lanterne_course_18ms.jpg` ; wall run à 19.3 m/s
  `Lanterne_wallrun_18ms.jpg` (avec) et `Lanterne_wallrun_18ms_sans.jpg` (sans).

**Résultat :**
- **Wall run :** avec la lanterne, les pierres du mur longé se lisent nettement (aplats rouges et joints) ; sans elle,
  le mur est noir et seuls les contours clairs le dessinent.
- **Reste du décor :** l'îlot ne dépasse pas ≈ 5.5 m ; au-delà, rien ne change (comparer les vues avec et sans). Le
  sol sous le joueur prend une teinte rouge sombre, et les bougies et les taches de lune restent les seules zones
  vraiment éclairées.
- **Effets de bord :**
  - la lanterne éclaire aussi les mains et le barillet. Les mains sont encore en greybox, en matériau Lit, et prennent
    un liseré rouge vif : à revoir avec leurs vrais matériaux toon ;
  - en coop, chaque joueur porte sa lanterne, ce qui fait une lumière de plus par joueur.

**Coût GPU** (éditeur, vue de 1469 × 712, médianes de 3 blocs de 5 s alternés, zone sans bougie) :

| Configuration | Sans lanterne | Avec lanterne | Coût |
|---|---|---|---|
| Plein écran (1 lanterne) | 1.68 – 1.77 ms | 1.77 – 1.84 ms | **≈ +0.1 ms** |
| Écran partagé à 2 (2 lanternes) | 2.50 – 2.76 ms | 2.44 – 3.00 ms | **dans le bruit de mesure** (< 0.2 ms) |

Sans ombre, une lumière ponctuelle de plus ne coûte presque rien en Forward+. **Avec ombres**, elle ajouterait 6 rendus
d'ombre par lanterne : ne pas les activer.

## 3 ter. Lisibilité des signaux du jeu (réglage intermédiaire)

**Essai :** dans `Test_Rendu`, les cinq signaux à surveiller pour survivre sont posés côte à côte à z = 60
(groupe `Signaux du jeu (z 60)`), en deux versions au même endroit :
- **actuel** : matériaux du jeu, en lecture seule (`Assets/_Project/Art/Greybox/`). URP Unlit pour la zone du traqueur,
  le bond de goule et l'âme ; URP Lit avec émission pour la caisse et la surface magnétique. La caisse est un vrai
  `Grabbable` : sa lueur est pilotée par le jeu (`TelekinesisView`) ;
- **réglé** : copie sur `ItHunts/Toon` (`Assets/Art/Materials/Rendu/M_Signal_*_Regle.mat`), mêmes couleurs, avec
  **« à travers le brouillard » à 1**.

Vues prises en Play Mode, avec le prefab joueur (FOV du jeu, HUD et mains), sur une ligne décalée de 2.5 m pour que la
caisse ne soit pas prise par l'aide à la visée :
- à 10, 30 et 50 m ;
- à l'arrêt (FOV 70°) et en course à 18 m/s (FOV 80°, capture au passage de la distance) ;
- avec et sans lanterne.

**Une planche JPEG par signal** (`docs/art/rendus/Rendu/Signal_<signal>.jpg`) : 4 lignes (arrêt ou course, avec ou
sans lanterne) × 6 colonnes (actuel puis réglé, à 10, 30 et 50 m). Chaque vignette est un recadrage de 560 × 315 px
d'une image 1080p, **sans agrandissement** : le signal y a la taille qu'il aura à l'écran.

| Signal | 10 m | 30 m | 50 m | Ce qu'il faut régler | Illisible même après réglage ? |
|---|---|---|---|---|---|
| **Zone de prise du traqueur** (disque rouge de Ø 4 m au sol), `Signal_ZoneTraqueur.jpg` | très lisible | lisible : une bande rouge fine | **à peine** : une ligne de 3 à 4 px qui se confond avec le bond de goule | « à travers le brouillard » = 1 ; garder le rouge saturé : émission ≈ (2.5, 0.15, 0.1) au lieu de (4, 0.25, 0.15) (la version toon blanchit au cœur avec le bloom) | **Oui, à 50 m** : un disque au sol vu de biais devient une ligne, quel que soit le brouillard. Il faut un **élément vertical** : colonne de lumière rouge, lumière ponctuelle rouge sous la main, ou bord en relief. La main levée du traqueur reste le vrai signal de loin |
| **Bond de goule** (disque Ø 1.6 m + yeux), `Signal_BondGoule.jpg` | lisible | un trait orangé, petit | **illisible** : il se confond avec la zone du traqueur voisine | « à travers le brouillard » = 1 ; mettre la lueur **sur la goule** (corps ou yeux qui s'embrasent) et pas seulement au sol | **Oui, à 50 m** (même raison : disque au sol). Le bond se joue de près : à 30 m, il faut au moins la lueur sur le corps |
| **Objet attrapable** (caisse de 0.8 m), `Signal_Attrapable.jpg` | **sombre** : violet terne, lueur « à portée » ×0.35 (luminance 0.29–0.40) | **à peine** (0.17–0.19) | lisible **seulement quand l'aide à la visée le sélectionne** (lueur « visée » ×1.6) | ici, le brouillard n'est pas le problème (la version réglée n'aide pas : 0.15) ; c'est **l'intensité de la lueur**, qui reste sous le seuil du bloom. Côté jeu (`TelekinesisFeedback_Default`) : `glowInRange` 0.35 → **≈ 1.0**, `glowIdle` 0.08 → **≈ 0.3** ; côté art : masque d'émission sur les arêtes | **Oui, avec les valeurs actuelles**, à 30 m et plus. Le réglage dépend de la session principale (réglages de télékinésie) |
| **Surface magnétique** (panneau bleu de 2 × 3 m), `Signal_Magnetique.jpg` | très lisible | très lisible | lisible | « à travers le brouillard » = 0.7 à 1 : la version actuelle perd 30 % à 50 m (luminance 0.61 → 0.44), la version réglée reste à 0.60 | Non |
| **Éclat d'âme** (sphère de 0.25 m, cyan), `Signal_Ame.jpg` | lisible | un point de 4 à 5 px | **à peine** : un point de 2 à 3 px | « à travers le brouillard » = 1 ; **halo** (sprite de lueur d'environ 1 m, toujours face à la caméra) ou éclat plus gros. La version toon paraît plus pâle que l'Unlit (moins de halo cyan) : garder l'Unlit, ou monter la saturation | **Oui, à 50 m** (taille). Les âmes filent vers l'orbe dans un rayon court : à 50 m, ce n'est pas bloquant |

- **Lanterne :** aucun effet sur les signaux. Sa portée (6 m) n'atteint pas 10 m ; elle éclaire seulement le sol autour
  du joueur.
- **Course :** à 18 m/s, le FOV de 80° rend les signaux environ 15 % plus petits qu'à l'arrêt. Les conclusions ne
  changent pas.
- **À retenir :** le brouillard intermédiaire (10 → 85 m) n'éteint vraiment aucun signal HDR à 50 m. Ce qui limite,
  c'est **la taille à l'écran** (disques au sol, âme) et **l'intensité** des lueurs qui restent sous le seuil du bloom
  (caisse « à portée »). « À travers le brouillard » est utile pour la surface magnétique et pour garder la couleur des
  autres signaux au bout du brouillard.

## 4. Mesures

### En mouvement (Play Mode, prefab joueur du jeu, FOV du jeu)

Vitesse forcée par une **copie en mémoire** des réglages de mouvement (`PlayerMotor.UseSettings`), entrée injectée :
aucun réglage du jeu modifié ni écrit sur disque. Colonnes du mur ouest cachées pendant le wall run (Play Mode
seulement).

| Situation | Vitesse mesurée | FOV mesuré | Captures |
|---|---|---|---|
| Course | **18.0 m/s** | **80°** (70 + 12 × (18−8)/12) | `Mouvement_18ms_a.jpg`, `_b.jpg` |
| Course | **22.0 m/s** | **82°** (plafond) | `Mouvement_22ms_a.jpg`, `_b.jpg` |
| Wall run le long du mur ouest | **19.3 m/s** en WallSlide pendant ≈ 1.3 s | **81°** | `Mouvement_wallrun_18ms_a.jpg` (contre le mur), `_b.jpg` |

- À 18 et 22 m/s, les contours restent stables (pas de scintillement visible sur les captures ; à confirmer à l'œil en
  jeu) et le fondu par le brouillard évite les traits qui « nagent » au loin.
- **En wall run, le mur contre lequel on court est presque noir** en réglage intermédiaire (pas de lumière à moins de
  10 m) : on ne lit plus la surface utilisée. Argument pour le mode de contour clair dans le noir, ou pour une ambiance
  plus haute, ou pour des bougies régulières le long des murs de course.
- **Défaut trouvé : contours fantômes des mains.** La pré-passe profondeur + normales du renderer
  (`m_PrepassLayerMask` de `PC_Renderer`) inclut les couches `FirstPersonP1` à `P4` (8 à 11), alors que les passes
  opaques les excluent : les mains y sont dessinées au FOV du monde, et les contours les tracent, décalées, sur l'image
  (`Mouvement_22ms_b.jpg`). Corrigé en local en retirant ces couches de la pré-passe (`Mouvement_22ms_b_prepass-corrigee.jpg`).
  **La SSAO lit la même texture : elle a probablement le même défaut sur `main` aujourd'hui.**

### Performances (temps GPU par image)

Mesuré **dans l'éditeur** (Play Mode, vue Game de 1469 × 712, `FrameTimingManager`), médiane de 3 blocs de 5 s
alternés par configuration, joueur immobile au début du couloir (3 bougies à ombres, lune à ombres, SSAO, bloom).
L'écran partagé est **simulé** : caméra du joueur sur la moitié gauche, une deuxième caméra (mêmes réglages URP,
post-process) sur la moitié droite.

| Configuration | Sans contours | Avec contours | Coût des contours |
|---|---|---|---|
| Plein écran (1 joueur) | 1.67 – 1.71 ms | 1.71 – 1.72 ms (un bloc à 2.40) | **≈ +0.03 ms** |
| Écran partagé à 2 (simulé) | 2.27 – 2.60 ms | 2.42 – 2.65 ms | **≈ +0.1 ms** |

- À 1920 × 1080 (≈ 2 fois plus de pixels que la vue mesurée), le coût de la passe est estimé à **0.1 à 0.2 ms**.
- La passe lit 9 échantillons de profondeur, 5 de normales et 1 de couleur par pixel ; elle **n'ajoute pas de
  pré-passe** : la texture des normales est déjà produite pour la SSAO.
- Limites : mesure dans l'éditeur (bruit), pas dans un build ; le build demande ton accord (règle 1). À refaire dans
  le build de mesure de la session principale (`-ithunts-perf`) sur `Level_01`.

### Compilation et tests

- Shaders : 0 erreur, 0 avertissement (`ShaderUtil.GetShaderMessages`). Recompilation des scripts : 0 erreur,
  0 avertissement.
- Tests EditMode : **341 réussis, 1 non concluant à dessein** (`Physique_CaisseLanceeSurUnFactice_LeBlesse`, vérifié en
  Play Mode), comme sur `main`.

## 5. Pour la session principale

Réglages partagés à faire **sur `main`** (rien n'est commité sur `art` dans `Assets/Settings/`).

1. **`PC_Renderer` : ajouter une Full Screen Pass Renderer Feature** (et dans `Mobile_Renderer` si on s'en sert) :
   - nom : `ItHunts Contours` ;
   - Injection Point : **Before Rendering Transparents** (avant les passes « ItHunts Mains », événement 500) ;
   - Requirements : **Depth + Normal** ;
   - Fetch Color Buffer : **coché** ; Bind Depth-Stencil : décoché ;
   - Pass Material : `Assets/Art/Materials/Rendu/M_Contours.mat`, Pass Index 0.
2. **`PC_Renderer` : retirer les couches 8 à 11 de `Prepass Layer Mask`** (Filtering > Prepass) : passer de
   `Everything` (4294967295) à la même valeur que l'Opaque Layer Mask (**4294963455**). Corrige les contours fantômes des
   mains, et sans doute un défaut de SSAO déjà présent.
3. **Brouillard dans les vraies scènes** (Lighting > Environment > Other Settings, ou dans les constructeurs de scène) :
   `RenderSettings.fog = true`, mode **Linear**, couleur, début et fin du réglage retenu (§ 3), et **couleur de fond des
   caméras = couleur du brouillard**. Le fondu du contour (`_FadeStart`, `_FadeEnd` de `M_Contours`) suit le réglage.
4. **Ambiance** des scènes (`ambientMode = Flat`, `ambientLight`) : valeur du réglage retenu (aujourd'hui 0.16 / 0.16 /
   0.20 dans `ArenaLayout_*`).
5. **Lumières ponctuelles** (torches, lanternes) : le shader toon utilise leur **portée** pour dessiner l'îlot. Les
   intensités du jeu (6, portée 50) sont faites pour Lit : avec le toon, viser **1.5 à 2.5 d'intensité et 9 à 13 m de
   portée** pour une torche.
6. **Lanterne (si le DA la confirme)** : une `Light` ponctuelle enfant du prefab joueur (§ 3 bis : couleur
   (0.62, 0.16, 0.07), intensité 0.6, portée 6 m, en (0, 1.1, 0.3), **sans ombres**), une par joueur, réglage dans un
   ScriptableObject si on veut la régler en Play Mode. Rien n'est fait dans le prefab sur `art`.
7. Les matériaux du jeu ne changent pas tant que l'étape 2 (matériaux définitifs) n'est pas faite ; le shader toon peut
   remplacer URP Lit sur les matériaux greybox (mêmes noms de propriétés).

## 5 bis. Contour des mains et du barillet (proposition, rien n'est codé)

**Le problème :** les mains et le barillet sont dessinés par les passes « ItHunts Mains », après le monde, sur une
profondeur effacée et à 60° de FOV (D-028). La passe de contours plein écran lit la profondeur et les normales de la
scène : elle ne les voit pas. Le correctif de la pré-passe (§ 5, point 2) les retire de toute façon de ces textures.
Sans autre solution, les mains seraient le seul objet sans contour.

| Option | Principe | Coût estimé (non mesuré) | Rendu comparé au contour de la scène |
|---|---|---|---|
| **A. Coque inversée dans le matériau** (recommandée) | Une 2e passe dans `ItHunts/Toon` (étiquette dédiée, par exemple `LightMode = "ItHuntsOutline"`) : les sommets sont poussés le long de la normale **en espace écran** (épaisseur en pixels, comme la passe plein écran), faces avant coupées, couleur d'encre. Les passes « ItHunts Mains » la dessinent : il suffit d'ajouter cette étiquette à leur liste de *Shader Passes* (réglage partagé, sur `main`) | 1 draw de plus par pièce (≈ 10 draws pour mains + barillet), 15 à 25 k triangles de sommets en plus ; remplissage limité à un liseré : **< 0.05 ms** par joueur | Silhouette nette de la même épaisseur (1.5 px à 1080p) et de la même encre ; **pas de traits intérieurs** (plis des doigts, bord des chambres) : ils viennent de la texture peinte. Les arêtes vives (barillet) demandent des **normales lissées** cuites dans les sommets (couleur ou UV2, depuis Blender), sinon la coque se fend aux angles. Le mode « clair dans le noir » se fait en éclaircissant l'encre selon la lumière reçue |
| **B. Passe dédiée plein écran** | Une passe dessine les mains (couches 8 à 11) dans une texture profondeur + normales à part, puis le même shader `ItHunts/Contours` s'applique dessus et se compose sur l'image | Pré-passe des mains (petite) + une passe plein écran par caméra : **≈ 0.05 à 0.15 ms** par joueur ; demande une `ScriptableRendererFeature` en C# (Render Graph), à écrire et maintenir sur `main` | **Identique** à la scène, traits intérieurs compris (doigts, chambres). Plus de code, une texture plein écran de plus par caméra en écran partagé |
| C. Rim sombre dans le shader (fresnel) | Assombrir les faces de biais | Gratuit | Trait d'épaisseur irrégulière (épais sur les surfaces plates vues de biais, absent ailleurs) : ne ressemble pas à l'encre de la scène. **À écarter** |

**Recommandation :** A pour les mains et le barillet, parce qu'il n'y a ni code de jeu ni réglage du renderer à
changer, à part la liste des passes des mains. L'artiste fournit des normales lissées dans les FBX de vue FPS. On
passera à B seulement si le DA veut les traits intérieurs des doigts.

## 6. Proposition de décision pour le GDD : D-060

**État : réglage intermédiaire, contours clairs dans le noir, lanterne à confirmer.** À écrire dans le GDD par la session
principale après validation du DA.

> **D-060 — Rendu cel-shading et contours (DA, étape 10).** Le décor utilise un shader toon maison (`ItHunts/Toon`) :
> lumière principale en *N* bandes nettes (3 par défaut), ombre teintée bleu nuit, lumières ponctuelles en îlots à bord
> net (paliers sur la portée), ombres portées en aplats, émission HDR pour tout ce qui est interactif (bloom à seuil 1),
> ni métal ni brillance. Un contour plein écran (profondeur + normales, Full Screen Pass d'URP, avant les transparents)
> trace silhouettes et arêtes, en mode **clair dans le noir** (noir à la lumière, un peu plus clair que le fond dans
> le noir), et s'efface avec le brouillard linéaire de la scène. Réglage retenu : **intermédiaire** (ambiance 0.055 /
> 0.058 / 0.080, brouillard 10 → 85 m, lune 0.25, bougies 1.8 sur 11 m, § 3). Chaque joueur porte une **lanterne**
> rouge sombre sans ombre (0.6, portée 6 m) pour lire les murs qu'il longe (**à confirmer**). Coût mesuré : ≈ 0.1 ms
> par image pour les contours, ≈ 0.1 ms par lanterne. Les mains et le barillet gardent leur propre contour (matériau).
> La pré-passe profondeur + normales exclut les couches des mains.
