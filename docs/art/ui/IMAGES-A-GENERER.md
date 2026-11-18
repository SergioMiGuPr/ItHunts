# Images à générer : lot T (scènes d'un seul tenant, priorité haute), lots B (fonds de menu) et C (personnages)

Outil : **Higgsfield, Nano Banana Pro**, **2K**. Format **16:9** pour le décor ; **3:4** conseillé pour un personnage seul
(un personnage en pied en 16:9 n'occupe qu'un tiers de l'image ; si tu tiens au 16:9, garde le même prompt).

- **Une image par prompt.** Les images jointes (« Image 1 », « Image 2 ») sont à charger comme références dans l'ordre indiqué.
- Nomme chaque résultat comme indiqué (`ui_…_v01.png`) et dépose-le dans `docs/art/ui/maquettes/`.
- Certaines images dépendent d'une autre : l'ordre est indiqué ; pour celles marquées **« entrée préparée »**, je te fournis
  l'image d'entrée quand la précédente est faite.
- Fonds unis : **vert pur `#00FF00`** pour le décor (aucun vert dans la palette du décor, détourage propre) ; **gris clair
  uni `#C8C8C8`** pour les personnages (le koala et le caméléon sont verts). Lueurs et brumes : **sur noir pur**, je les
  passe en mode additif.

Bloc de style commun, **à coller à la fin de chaque prompt** (noté `[STYLE]`) :

> Style: hand-painted dark fantasy illustration, thick dark ink outlines around every silhouette, cel-shaded shadows in two or three flat tones, visible brush strokes and hatching in the dark areas, deep blue-violet moonlit night palette with crimson glowing castle windows and warm orange firelight accents. Match exactly the painting style, line weight, palette, lighting and level of detail of the attached reference images. No text, no letters, no logo, no watermark, no signature, no user interface, no frame, no border.

## PRIORITÉ HAUTE (2026-11-18) : lot T, scènes peintes d'un seul tenant

**Pourquoi** : le playtest des menus intégrés montre que les morceaux du lot B ne vont pas ensemble : halo autour du château,
nuages en bandes régulières coupées par un trait noir, feu vectoriel plat sur un décor peint, bancs et rochers collés sans
ombre ni perspective. **Nouvelle règle** : chaque écran part d'**une seule illustration complète**. Les calques sont tirés de
**cette même image**, par des passes d'édition ; aucune pièce n'est générée à part. Ce lot **remplace** B1–B17 et E1–E9 pour
l'écran titre et le campement ; les sprites de `Assets/Art/UI/Menus/` seront remplacés par ses découpes.

**Réglages communs** :
- **Higgsfield, Nano Banana Pro**, résolution **4k**, format **16:9** : l'image sort vers 5504 × 3072 ; je la ramène à
  **3840 × 2160** à la découpe.
- **Dépôt** : `docs/art/ui/generees/`.
- **Passes d'édition** (marquées « édition de Tx ») :
  - charger l'image indiquée comme **seule** image jointe ;
  - une seule modification par passe (ta leçon) ;
  - garder le format et la résolution.
- **Fonds sans alpha** (le modèle n'en produit pas) :
  - **vert pur `#00FF00`** pour ce qui doit être détouré ;
  - **noir pur** pour les lueurs, le feu et le brouillard.
  Je fais l'alpha moi-même : clé de chroma avec retrait du vert sur les bords, ou alpha tiré de la lumière. Je vérifie
  ensuite chaque calque sur damier et sur le fond voisin, sans halo.

Bloc `[STYLE-T]`, **à coller à la fin de chaque prompt de création** (pas des passes d'édition) :

> Style: one single cohesive hand-painted dark fantasy illustration, thick dark ink outlines, cel-shaded shadows in two or three flat tones, visible brush strokes and hatching in the dark areas, deep blue-violet moonlit night palette, crimson glowing castle windows, warm orange firelight accents, strong atmospheric depth with value contrast between near and far planes, painterly volumetric fog. Everything painted in the same brush style and lighting, nothing looks pasted. No text, no letters, no logo, no watermark, no signature, no user interface, no frame, no border.

Fin commune des **passes d'édition** (notée `[GARDER]`) :

> Keep the exact same composition, framing, painting style, colors, lighting and every other element unchanged, pixel-aligned with the input image. Same aspect ratio and resolution.

### T1 — Écran titre, scène complète · `ui_titre_scene_v01.png` · 16:9 · 4k

> A wide cinematic night landscape illustration for a game title screen. On the right two thirds: a tall gothic castle with many sharp spires and crimson glowing windows, standing on a massive dark rock cliff, several thin waterfalls falling from the cliff into the valley, stone bridges with arches linking the towers. Behind the castle a large bright full moon, placed in the upper middle of the image, to the right of the empty left third, with a soft painted glow in the sky around it. A night sky in deep blue-violet with a few stars, two layers of painted clouds: distant thin clouds high behind the castle, and nearer, darker, larger clouds drifting in front of the lower part of the cliff. The bottom of the image is a sea of thick moonlit fog filling the valley, covering the foot of the cliff. The left third of the image is calm and almost empty: only dark sky, faint far clouds and fog, darker in value, with no castle, no trees and no detail, so that a title and a menu can be placed there. No characters, no foreground trees, no ground in the foreground. [STYLE-T]

**Passes de découpe**, chacune partant du résultat de la précédente (je recale chaque passe sur T1 au pixel avant de la
découper) :

**T1a — sans le brouillard** · `ui_titre_passe_a_v01.png` · édition de **T1**
> Same image: remove the sea of fog at the bottom, and paint what it was hiding: the full foot of the rock cliff, the end of the waterfalls and a dark valley floor. [GARDER]

**T1b — sans les nuages de devant** · `ui_titre_passe_b_v01.png` · édition de **T1a**
> Same image: remove the near dark clouds that drift in front of the lower part of the cliff, and paint the cliff, waterfalls and sky that were behind them. [GARDER]

**T1c — sans le château** · `ui_titre_passe_c_v01.png` · édition de **T1b**
> Same image: remove the castle, the rock cliff, the bridges and the waterfalls entirely, and paint the night sky, the distant clouds and the moon that were behind them. [GARDER]

**T1d — ciel et lune seuls** · `ui_titre_passe_d_v01.png` · édition de **T1c**
> Same image: remove the distant clouds, and paint the night sky and the moon glow that were behind them. [GARDER]

**Calques sur fond uni** (une substitution par passe, toujours à partir de l'image indiquée) :

| Calque | Fichier | Édition de | Prompt |
|---|---|---|---|
| Château (rocher, ponts, cascades) | `ui_titre_calque_chateau_v01.png` | **T1b** | Same image: replace the night sky, the moon and the clouds behind the castle with pure flat uniform green (#00FF00); keep the castle, the rock cliff, the bridges and the waterfalls exactly as they are, with clean hard edges and no glow on the green. [GARDER] |
| Nuages du fond | `ui_titre_calque_nuages_fond_v01.png` | **T1c** | Same image: replace the night sky and the moon with pure flat uniform green (#00FF00); keep the distant clouds exactly as they are, with their soft painted edges, no glow on the green. [GARDER] |
| Nuages de devant | `ui_titre_calque_nuages_devant_v01.png` | **T1a** | Same image: replace everything behind the near dark clouds (sky, moon, castle, cliff, waterfalls, distant clouds) with pure flat uniform green (#00FF00); keep the near clouds exactly as they are, no glow on the green. [GARDER] |
| Brouillard | `ui_titre_calque_brouillard_v01.png` | **T1** | Same image: replace everything that is not the sea of fog (sky, moon, clouds, castle, cliff, waterfalls) with pure flat black (#000000); keep the fog exactly as it is, with its soft edges and its moonlit highlights. [GARDER] |
| Fenêtres (masque) | — | — | par script : rouge des fenêtres dans le calque du château, comme pour `Fond_Chateau_Fenetres` |

**Plan de découpe et d'intégration** (fait par moi ; tout est dans `Assets/Art/UI/Menus/Titre/`, à 3840 × 2160, PPU 200) :

| Calque, de l'arrière vers l'avant | Source | Alpha | Animation |
|---|---|---|---|
| `Titre_Ciel` (ciel et lune) | T1d | opaque | halo de la lune qui pulse (additif tiré de T1d) |
| `Titre_Nuages_Fond` | T1c → calque des nuages du fond | clé de chroma, bords doux | défile, **raccordé gauche-droite** |
| `Titre_Chateau` | T1b → calque du château | clé de chroma, **sans halo** (bord resserré de 1 px, vert retiré des bords) | fenêtres qui palpitent (masque) |
| `Titre_Nuages_Devant` | T1a → calque des nuages de devant | clé de chroma, bords doux | défile plus vite, **raccordé** |
| `Titre_Brouillard` | calque du brouillard | additif, ou alpha tiré de la lumière | dérive lente, **raccordé** |

**Nuages raccordables** : je les rends tuilables à l'horizontale par script, avec un fondu croisé d'un tiers de leur largeur
sur une copie décalée d'une demi-largeur. Aucun bord coupé : les bandes gardent leur haut et leur bas peints, et le trait noir
vu au playtest disparaît. **Contrôle** : la pile de calques est recomposée et comparée à T1 (écart moyen et carte des écarts
sur la planche).

### T2 — Logo IT HUNTS, lisible · `ui_logo_a_v01.png`, `ui_logo_b_v01.png`, `ui_logo_c_v01.png` · 16:9 · 4k

Le « IT » en gothique de la maquette ne se lit pas. Il faut des **capitales lisibles** et des griffures **à côté** des lettres
(pas en travers). Pas d'image jointe. Fond **vert pur** : je fais l'alpha et je livre en PNG transparent, plus une lueur à
part en additif. Les propositions sortent d'une génération chacune ; tu peux relancer celle qui te plaît pour avoir des
variantes.

**A, pierre gravée** :
> The game title logo text "IT HUNTS" on one line, large clearly readable classic Roman capital letters with sharp wedge serifs, every letter easy to read, carved from cracked weathered grey-violet stone with a thick dark ink outline and cel-shaded bevels, three thin crimson claw scratches cutting across the stone surface between letters without hiding any letter shape, a faint dark red glow inside the scratches. Centered, filling 80% of the image width. Hand-painted dark fantasy game logo. Nothing else in the image. Pure flat uniform green background (#00FF00), no shadow, no glow on the background.

**B, fer forgé** :
> The game title logo text "IT HUNTS" on two lines, "IT" above "HUNTS", both words the same width, bold readable display capitals inspired by blackletter but with simple open letter shapes (the I and the T must be instantly recognizable), forged dark iron with mauve-grey highlights and a thick dark ink outline, small thorn-like spikes only on the outer serifs, a single crimson claw slash passing behind the letters. Centered, filling 70% of the image width. Hand-painted dark fantasy game logo, cel-shaded. Nothing else in the image. Pure flat uniform green background (#00FF00), no shadow, no glow on the background.

**C, os et sang** :
> The game title logo text "IT HUNTS" on one line, tall narrow readable serif capitals with slightly ragged hand-cut edges, bone-white letters with dark ink outline and cel-shaded grooves, dried crimson stains dripping from the bottom of a few letters, two small claw marks above the word, the letter shapes stay perfectly legible. Centered, filling 80% of the image width. Hand-painted dark fantasy game logo. Nothing else in the image. Pure flat uniform green background (#00FF00), no shadow, no glow on the background.

Si aucune ne convient : je peux composer le logo par script avec la police **Grenze Gotisch** (déjà sous licence OFL dans le
projet) et une texture de pierre peinte, sans génération.

### T3 — Campement, scène complète avec foyer et 4 places · `ui_campement_scene_v01.png` · 16:9 · 4k

Pas d'image jointe : on veut une scène neuve, cohérente d'un seul tenant. Le **foyer** est peint **avec des braises, sans
grandes flammes** (les flammes animées de T4 se posent dessus), mais la scène est déjà éclairée comme si le feu brûlait.

> A wide night campsite scene in a dark haunted forest clearing, seen from a slightly high three-quarter view, painted as one single illustration. In the middle of the image, slightly below center: a round stone fire pit with glowing orange embers and a few charred logs, but no tall flames, a small curl of smoke. Around the fire pit, four empty places to sit, painted as part of the ground with correct perspective, contact shadows and warm firelight on their sides facing the fire: two places in the foreground, one at the lower left (a fallen tree log lying on the ground) and one at the lower right (a flat low boulder), and two places in the background behind the fire, one at the upper left of the fire (a short wooden bench) and one at the upper right of the fire (a cut tree stump). All four seats have the same sitting height relative to the ground and are clearly separated from each other. Behind them: a small canvas tent with a hanging iron lantern, a torn banner on a pole, barrels and crates, old ruins, twisted dead trees framing the clearing, and through a gap in the trees, far away on the upper right, a gothic castle with crimson windows under a full moon. Warm orange firelight on the near ground and seats, cold blue moonlight on the background, light fog between the trees. No characters, no animals, no people. Leave the top left corner and the bottom left corner darker and calm (room for interface), and the bottom right corner clear of important details. [STYLE-T]

**Passes** (même principe que T1) :
- **T3a — fumée retirée** · `ui_campement_passe_a_v01.png` · édition de **T3** (seulement si la fumée gêne les flammes animées) :
  > Same image: remove the curl of smoke above the fire pit, keep the embers and the logs. [GARDER]
- **T3b — calque de premier plan** · `ui_campement_calque_avant_v01.png` · édition de **T3** (ou T3a). Il sert à repasser
  devant les personnages assis aux deux places de devant :
  > Same image: replace everything with pure flat uniform green (#00FF00) except the two foreground seats (the log at the lower left and the boulder at the lower right) and the ground details directly in front of them; keep those exactly as they are. [GARDER]

**Ancrages** (prefab `Assets/Art/UI/Menus/Campement/Campement_Scene.prefab`) :
- **enfants vides** : `Ancre_Foyer` (centre de la base des flammes), `Ancre_Place_1` à `_4` (milieu de chaque assise, là où
  se posent les hanches du personnage) ;
- **ordre des places** : 1 avant gauche, 2 avant droite, 3 fond gauche, 4 fond droite ; pour chaque place, une **échelle**
  (les places du fond sont plus petites) et un **ordre d'affichage** ;
- **unités** : coordonnées en pixels de l'image 3840 × 2160 et en unités du canvas 1920 × 1080 ;
- je les mesure sur l'image reçue et je les écris dans le prefab et dans `UI-HANDOFF.md`.

### T4 — Feu de camp animé · `ui_feu_planche_v01.png` · 16:9 · 4k · entrée préparée

Pour que le style colle au foyer : je te prépare **`entree_foyer.png`**, le foyer découpé de T3. Il sert de **seule image
jointe**, comme référence de style et de taille.

> A sprite sheet of a looping campfire flame animation, 12 frames arranged in a regular grid of 4 columns and 3 rows, every cell the same size and clearly separated, in each cell the same campfire flames seen from the same angle, rising from the same base point at the bottom center of the cell, each frame slightly different so that the 12 frames play as a smooth seamless loop (frame 12 leads back into frame 1). Hand-painted flames in the same painting style as the embers of the attached fire pit: thick dark ink outline on the flame shapes, two or three flat cel-shaded tones of deep red, orange and pale yellow, a few small sparks. Flames only: no logs, no stones, no ground, no smoke. The flames are about as wide as the fire pit in the attached image. Pure flat black background (#000000) in every cell, no glow spreading on the background. [STYLE-T]

**De mon côté** :
- découpe des 12 cases et recalage de la base de chaque flamme ;
- **alpha prémultiplié tiré de la lumière**, plus une couche additive pour la lueur ;
- contrôle de la boucle (écart entre l'image 12 et l'image 1) ;
- `Campement_Feu_Anim` : 12 images, 10 à 12 images/s, posée sur `Ancre_Foyer`.

**Si la planche est incohérente d'une case à l'autre**, plan de repli : une **vidéo** en boucle de 2 à 3 s du feu sur fond
noir, à partir de l'image d'entrée. J'en tire les images et je referme la boucle par fondu.

### T5 — Nappe de brouillard pour la transition titre → campement · `ui_transition_brouillard_v01.png` · 16:9 · 4k

> A full-frame texture of thick rolling night fog, painted, soft billowing volumes with a few denser darker swirls and lighter moonlit wisps, cold blue-violet and grey tones, even density over the whole image (no empty area, no vignette, no horizon, no light source), the left and right edges continuing the same fog so the texture can repeat horizontally. Hand-painted with visible brush strokes and soft cel-shaded tones, no ink outlines. Nothing else in the image. [STYLE-T]

**De mon côté** :
- texture rendue **raccordable** dans les deux sens par script ;
- une version opaque et une version en alpha tiré de la lumière ;
- la nappe traverse l'écran pendant que la caméra glisse : elle cache le titre, puis se retire sur le campement.

**Ordre conseillé** : T1, puis T1a → T1b → T1c → T1d, puis les quatre calques ; T2 (en parallèle) ; T3 → T3b ; je prépare
`entree_foyer.png` → T4 ; T5 quand tu veux.

## État au 2026-11-17 et leçon

Reçues (dans `docs/art/ui/generees/`) : B1 (`ui_campement_vide_v01` + `ui_campement_passe1_v01`), B3, B4, B5, B6, B15, B16 ;
découpées (`UI-HANDOFF.md` § 2). **B7/B8 abandonnées** : château découpé par script dans `ui_menu_principal_fond_v01`, masque
des fenêtres tiré de leur rouge. Les originaux **2K (2752 × 1536)** ont remplacé les premières versions (1280 × 714) le
2026-11-17 ; découpes refaites.

**Leçon (utilisateur)** : avec une image de référence, le modèle **modifie la scène** au lieu d'en extraire un élément ; un
objet seul marche **sans référence**, sur noir ou gris uni ; une retouche marche en **passes d'une seule suppression**. D'où
la méthode pour la suite : je découpe les calques moi-même ; tu ne génères que des **passes d'édition** (une suppression par
passe, en partant chaque fois du résultat précédent) qui repeignent ce qui était caché. Le calque retiré se retrouve par
différence entre deux passes (calage vérifié sur B1 : 0 px), en sachant que toute l'image est un peu repeinte à chaque fois.

## Inventaire : ce qui sort des maquettes, ce qui manque

| Calque / pièce | D'où il vient | Image à générer |
|---|---|---|
| Composition du menu principal (moitié gauche du panorama) | `ui_menu_principal_fond_v01` **fait foi** | — |
| Campement (moitié droite) | `ui_campement_v01`, sans personnages ni interface ni sièges | **B1** |
| Raccord entre les deux | — | **B2** (entrée préparée) |
| Ciel | — (le ciel des maquettes est caché par le château et les arbres) | **B3** |
| Lune | — (en partie cachée par le château) | **B4** |
| Nuages (2 couches) | — | **B5** |
| Brume (2 couches) | — | **B6** |
| Château + falaise, fenêtres allumées / éteintes (masque des fenêtres = différence des deux) | `ui_menu_principal_fond_v01` | **B7**, **B8** |
| Arbres lointains et ruines | panorama B1 + B2 | **B9**, **B10** (entrées préparées) |
| Arbres proches | panorama | **B11**, **B12** (entrées préparées) |
| Sol et premier plan (chemin, racines, tombes, camp : tente, bannière, tonneaux, caisses) | panorama | **B13**, **B14** (entrées préparées) |
| Feu de camp (flammes) | — | **B15** |
| 4 sièges à la même hauteur d'assise | `ui_campement_v01` | **B16** |
| Logo IT HUNTS | `ui_menu_principal_v01` : je le détoure ; **B17** seulement si le détourage laisse un halo | (B17) |
| Lanternes du camp, lueur du feu, palpitations | peintes dans les calques + lueurs additives faites dans Unity | — |
| Personnages assis (4), debout de face (4), de dos (4) | maquettes (apparence) | **C1** à **C12** |

**Panorama** : 2 écrans de large (5 504 × 1 536 px en 2K). Calques et parallaxe prévus (déplacement par rapport au premier
plan quand la caméra glisse du titre au campement) : ciel 10 %, lune 12 %, nuages 15 et 25 %, château 25 %, arbres lointains
55 %, brume 40 et 80 %, arbres proches 90 %, sol et premier plan 100 %. Le château reste donc visible des deux côtés (il glisse
d'un quart d'écran seulement). Les calques B9 à B14 sont faits en deux moitiés qui se recouvrent ; le raccord sera caché derrière
un tronc.

**Ordre conseillé** : B1 → (je prépare B2) → B2 → B3 à B8, B15, B16 (indépendantes) → (je prépare B9 à B14) → B9 à B14.
Personnages : le test du renard se fait sur le renard découpé de `ui_campement_v01` ; génère C1 à C12 **après** validation
de ce test (la méthode d'animation peut changer ce qu'on demande aux images).

## Lot B : décor

### B1 — Campement vide (moitié droite du panorama) · `ui_campement_vide_v01.png` · 16:9
Images jointes : **Image 1** = `ui_campement_v01.png`, **Image 2** = `ui_menu_principal_fond_v01.png`.

> Repaint Image 1 as an empty night campsite, same framing, same camera and same composition. Remove completely: the four characters, the two name plates and the "Modifier" plate, the two round plus buttons, the gear and people icons in the top right, the back arrow in the top left and the red "Prêt" button in the bottom right; paint the ground, rocks and background that were behind them. Also remove the four seats (the log on the left, the flat stone block, the long log bench, the big rock on the right) and the three lanterns standing on the ground near them; leave bare, flat, trodden earth where they were. Remove the castle, its cliff, its waterfalls and the moon in the upper right: replace them with night sky and low misty hills with distant dead trees, using the same sky colors and cloud shapes as Image 2. Keep the campfire ring of stones in the center, with charred logs and glowing embers only, no flames, but keep the warm orange firelight on the ground, the stones and the nearby objects. Keep the canvas canopy on its poles, the small lantern hanging from it, the tall banner, the barrels, the crates, the ruined stone arches and all the trees exactly where they are. Keep the top-left, bottom-left and bottom-right corners dark and calm (they will hold interface elements). [STYLE]

### B2 — Raccord menu → campement · `ui_raccord_v01.png` · 16:9 · **entrée prête**
Image jointe : **Image 1** = `docs/art/ui/entrees/entree_raccord.png` (2752 × 1536 : moitié droite de
`ui_menu_principal_fond_v01` | bande grise unie `#808080` de 560 px au centre | moitié gauche de B1, agrandie).
Une seule modification demandée (remplir la bande), conformément à la leçon.

> Edit Image 1: fill only the flat grey vertical band in the middle. Paint in it the missing part of the scene so that the left and right halves join into one continuous night landscape: continue the dead trees, the misty hills, the rocks and roots, the ground (the wet cobblestone path on the left becomes the bare trodden earth of the campsite on the right), keep the horizon line, the sky and the fog continuous, cold moonlight on the left turning into warm firelight toward the right. Do not change anything outside the grey band: same pixels, same framing, same size as Image 1. [STYLE]

Si le modèle retouche aussi les côtés : pas grave, je ne garde que la bande (fondu de 40 px de chaque côté).

### B3 — Ciel seul · `ui_ciel_v01.png` · 16:9
Image jointe : **Image 1** = `ui_menu_principal_fond_v01.png`.

> Paint only the night sky of Image 1, as a full-frame sky plate: deep blue-violet gradient, darker at the top, a soft cold blue glow near the bottom edge (the horizon), a few faint stars, very soft thin cloud texture painted into the sky itself. No moon, no castle, no trees, no ground, no mountains, no large clouds (they will be separate layers). The sky must be calm and even across the whole width so it can be stretched. [STYLE]

### B4 — Lune · `ui_lune_v01.png` · 16:9
Image jointe : **Image 1** = `ui_menu_principal_fond_v01.png`.

> A single full moon, same look as the moon in Image 1 (pale blue-white, soft painted craters, thin dark ink outline), centered, about one third of the image height, with a soft blue halo around it, on a pure flat black background (#000000). Nothing else in the image. [STYLE]

### B5 — Nuages · `ui_nuages_v01.png` · 16:9
Image jointe : **Image 1** = `ui_menu_principal_fond_v01.png`.

> Five separate long horizontal bands of night clouds stacked vertically with clear black gaps between them, each band spanning the full width, same cloud style as Image 1: soft painted clouds lit from above by cold moonlight, blue-grey and violet tones, bright rims on top, darker undersides, wispy edges. Pure flat black background (#000000) everywhere else. The clouds must not touch the top or bottom edge. [STYLE]

### B6 — Brume · `ui_brume_v01.png` · 16:9
Image jointe : **Image 1** = `ui_menu_principal_fond_v01.png`.

> Two separate horizontal bands of low ground fog, one in the upper half and one in the lower half, with a clear black gap between them, each spanning the full width and seamlessly tileable horizontally (the left and right edges must match), same fog as Image 1: soft cold blue-grey mist with light painted wisps, denser at the bottom of each band and fading upward. Pure flat black background (#000000) everywhere else. [STYLE]

### B7, B8 — Château isolé · **abandonnées** (2026-11-17)
Le modèle ne sait pas isoler un élément d'une scène sur fond vert (il ajoute une lueur verte et garde le décor). Château
découpé par script dans `ui_menu_principal_fond_v01` (`Fond_Chateau`), masque des fenêtres d'après leur rouge
(`Fond_Chateau_Fenetres`). Les branches qui passent devant partent avec la passe E1.

### B9 à B14 — remplacées par des passes d'édition (E1 à E9)
Je découpe moi-même les calques (fond lointain, arbres proches, sol et premier plan) dans le panorama ; ces passes ne servent
qu'à **repeindre ce qui est caché** derrière ce que j'enlève. **Une seule suppression par passe**, chaque passe part du
**résultat de la précédente** (Image 1), même format 16:9, 2K. Le fond final (dernière passe de chaque moitié) devient le
calque lointain ; chaque objet retiré se retrouve par différence entre deux passes successives.

Phrase commune à la fin de chaque prompt (`[GARDER]`) :
> Change nothing else: same framing, same size, same position, shape and colors of every other element, same lighting. [STYLE]

**Moitié gauche (menu)** : Image 1 de E1 = `ui_menu_principal_fond_v01.png`.

| Passe | Fichier | Prompt |
|---|---|---|
| E1 | `ui_menu_e1_v01.png` | Edit Image 1: remove only the big twisted dead tree on the right side of the image (its trunk, roots and all its branches, including the branches crossing in front of the castle and the moon). Paint what was behind it: the night sky and clouds, the castle and its bridge, the cliff, the waterfalls and the fog. [GARDER] |
| E2 | `ui_menu_e2_v01.png` | Edit Image 1: remove only the bare dead tree on the left side behind the stone arch, with all its branches. Paint the sky, the clouds, the misty hills and the ruins that were behind it. [GARDER] |
| E3 | `ui_menu_e3_v01.png` | Edit Image 1: remove only the ruined stone arch and the broken stone pillars on the left. Paint the misty hills, the fog and the ground that were behind them. [GARDER] |
| E4 | `ui_menu_e4_v01.png` | Edit Image 1: remove only the gravestones in the foreground. Paint the path, the rocks, the roots and the fog that were behind them. [GARDER] |

**Moitié droite (campement)** : Image 1 de E5 = `ui_campement_vide_v01.png` (B1).

| Passe | Fichier | Prompt |
|---|---|---|
| E5 | `ui_camp_e5_v01.png` | Edit Image 1: remove only the canvas canopy, its hanging lantern, its ropes and its wooden poles. Paint the tree trunk, the forest, the ruins and the ground that were behind them. [GARDER] |
| E6 | `ui_camp_e6_v01.png` | Edit Image 1: remove only the tattered banner and its pole. Paint the trees, the ruins and the fog that were behind it. [GARDER] |
| E7 | `ui_camp_e7_v01.png` | Edit Image 1: remove only the barrels and the wooden crates. Paint the tree roots and the ground that were behind them. [GARDER] |
| E8 | `ui_camp_e8_v01.png` | Edit Image 1: remove only the huge twisted tree on the left (trunk, roots and every branch). Paint the forest, the ruined arches, the sky and the ground that were behind it. [GARDER] |
| E9 | `ui_camp_e9_v01.png` | Edit Image 1: remove only the dead trees along the right edge. Paint the misty forest, the sky and the ground that were behind them. [GARDER] |

Après B2 : s'il contient un grand arbre au premier plan, une passe de plus du même modèle (« remove only the big tree in the
middle… »). Les objets du camp retirés (auvent, bannière, tonneaux, caisses) redeviennent des sprites découpés par différence ;
si l'un sort abîmé, on le demande seul sur gris uni `#C8C8C8`, sans image jointe (comme B16).

### B15 — Feu de camp · `ui_feu_v01.png` · 16:9
Image jointe : **Image 1** = `ui_campement_v01.png`.

> Only the flames of a campfire, same style as the fire in Image 1: tall painted flames in orange, yellow and a white-hot core, with dark ink outlines on the outer flame shapes, a few rising sparks, centered, filling about 60% of the image height, base of the flames flat and horizontal near the bottom. No logs, no stones, no ground, no smoke. Pure flat black background (#000000). [STYLE]

### B16 — Les 4 sièges, même hauteur d'assise · `ui_sieges_v01.png` · 16:9
Image jointe : **Image 1** = `ui_campement_v01.png`.

> The four campsite seats of Image 1, redrawn side by side with clear gaps between them, all seen from the same three-quarter front view and the same camera height as in Image 1: from left to right, a thick fallen log lying on its side, a squarish flat-topped stone block, a long log bench on two short wooden legs, a large rough rock with a flat top. The flat top of every seat is at exactly the same height in the image (the same seat height), aligned on one horizontal line; the bottoms also rest on one common ground line. Lit by cold moonlight from above and soft warm firelight from the front, so they can be mirrored. Pure flat green background (#00FF00), no ground, no shadows on the background, crisp outlines. [STYLE]

### B17 (seulement si besoin) — Logo isolé · `ui_logo_v01.png` · 16:9
Image jointe : **Image 1** = `ui_menu_principal_v01.png`.

> Isolate the "IT HUNTS" title logo of Image 1, exactly as it is (same letters, same cracked stone texture, same red claw slashes), centered, on a pure flat black background (#000000). Nothing else. Keep the exact lettering and shapes, do not redesign it. [STYLE]

## Lot C : personnages

Apparence de référence : renard et hibou dans `ui_campement_v01` ; renard, koala, caméléon, hibou dans `ui_choix_perso_v01`
(vignettes) ; les quatre de dos dans `ui_menu_principal_v01`. Mêmes proportions pour les quatre (petits personnages
anthropomorphes, tête ≈ ¼ de la hauteur, capes à capuche en lambeaux, lanterne de fer).

| Personnage | Description à insérer dans les prompts (`[PERSO]`) | Lanterne |
|---|---|---|
| Renard | a small anthropomorphic red fox with a long bushy tail with a white tip, glowing red eyes, wearing a hooded, tattered crimson cloak with dark stitched rune patterns, dark leather armor with straps and buckles, leather boots | red glass `#DD373F` |
| Koala | a small anthropomorphic grey koala with big round fluffy ears and a dark nose, wearing a hooded, tattered dark green cloak with thin gold stitched rune patterns, dark leather straps and boots | green glass `#55C851` |
| Caméléon | a small anthropomorphic green chameleon with a crest on its head, a big orange eye and a long tail curled in a spiral, wearing a hooded, tattered mustard-ochre cloak with dark stitched rune patterns, dark leather straps and boots | amber-orange glass `#DEA13F` |
| Hibou | a small anthropomorphic grey-brown owl with ear tufts and glowing ice-blue eyes, wearing a hood and a cloak made of layered feathers, dark leather straps, taloned feet | blue glass `#5FB9ED` |

**Lanterne** : tenue par son anneau, **pendue à l'écart du corps** (un vide visible entre la lanterne et la jambe ou la cape).
C'est ce qui permet de la faire osciller sans repeindre ce qu'il y a derrière. Si une image sort avec la lanterne collée au
corps, il faudra une passe d'édition : « Same image, remove the lantern and the hand's grip on it, paint the cloak and legs
that were behind it, keep the hand open and relaxed » (et la lanterne seule à part, image C-L ci-dessous).

**Pièces découpées** : avec l'animation par déformation (masque peint), aucune autre passe n'est nécessaire : la tête, la
queue et la cape bougent de quelques pixels, sans découvrir de zone cachée.

### C1 à C4 — Assis, trois-quarts vers la droite · `ui_<perso>_assis_v01.png` · 3:4
(`renard`, `koala`, `cameleon`, `hibou`.) Images jointes : **Image 1** = `ui_campement_v01.png`, **Image 2** =
`ui_choix_perso_v01.png`.

> Full-body character illustration of [PERSO], same character design as in the reference images. The character sits on a plain light-grey rectangular box used as a placeholder seat (simple flat-topped box, top at knee height, nothing on it), body turned three-quarters to the right, head turned to the right as if looking at a campfire, relaxed resting pose with forearms on the knees, cloak draped over the box. In the right hand it holds a small iron lantern by its top ring; the lantern hangs down in front of the box, clearly separated from the legs and the cloak by a visible gap. The lantern glass glows [LANTERNE], but no light halo on the background. Soft warm firelight from the right, cold moonlight from above. The whole character is visible including the tail and feet, centered, filling about 80% of the image height. Pure flat uniform light grey background (#C8C8C8), no ground, no shadow on the background. [STYLE]

### C5 à C8 — Debout de face (écran de choix) · `ui_<perso>_debout_v01.png` · 3:4
Images jointes : **Image 1** = `ui_choix_perso_v01.png`, **Image 2** = `ui_campement_v01.png`.

> Full-body character illustration of [PERSO], same character design as in the reference images, standing in a calm, confident pose facing the viewer with the body turned slightly three-quarters to the left, the tail visible, the right arm hanging down and holding a small iron lantern by its top ring, the lantern hanging clear of the body with a visible gap. The lantern glass glows [LANTERNE], no light halo on the background. Cold moonlight from above and a soft rim light. Feet near the bottom of the image, the character filling about 85% of the image height, centered. Pure flat uniform light grey background (#C8C8C8), no ground, no shadow on the background. [STYLE]

### C9 à C12 — De dos, debout (menu principal) · `ui_<perso>_dos_v01.png` · 3:4
Images jointes : **Image 1** = `ui_menu_principal_v01.png`.

> Full-body character illustration of [PERSO], seen from behind, standing still and looking toward a distant castle, hood up, same design and same back view as the matching character in Image 1. The right arm hangs slightly away from the body holding a small iron lantern by its top ring, the lantern clearly separated from the body. The lantern glass glows [LANTERNE], no light halo on the background. Cold moonlight from above, the cloak hem tattered. Feet near the bottom of the image, the character filling about 85% of the image height, centered. Pure flat uniform light grey background (#C8C8C8), no ground, no shadow on the background. [STYLE]

### C-L — Lanterne seule (seulement si une lanterne sort collée au corps) · `ui_lanterne_<perso>_v01.png` · 1:1

> A small iron lantern with a top ring, same design as in the reference image, glass glowing [LANTERNE], hanging straight down, centered, on a pure flat uniform light grey background (#C8C8C8), no light halo on the background. [STYLE]

## Récapitulatif

| Lot | Images | Notes |
|---|---|---|
| **T (priorité haute)** | T1 + 4 passes + 4 calques ; T2 (3 logos) ; T3 + 1 ou 2 passes ; T4 (planche du feu) ; T5 | remplace B et E pour l'écran titre et le campement |
| B | **remplacé par T** (titre, campement) ; était : B2 + passes E1 à E9 (+ B17 si besoin) | B1, B3–B6, B15, B16 reçues ; B7/B8 abandonnées ; B9–B14 remplacées par E1–E9 |
| C | 12 (C1 à C12) + C-L si besoin | commencer par le test du renard (C1 pas nécessaire pour le test : j'utilise le renard de `ui_campement_v01`) |
