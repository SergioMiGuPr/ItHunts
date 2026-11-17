# Images à générer : lots B (fonds de menu) et C (personnages)

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

### B2 — Raccord menu → campement · `ui_raccord_v01.png` · 16:9 · **entrée préparée**
Images jointes : **Image 1** = `entree_raccord.png` (que je prépare : moitié droite de `ui_menu_principal_fond_v01`, bande grise
au centre, moitié gauche de B1).

> Image 1 shows two night forest scenes side by side with a flat grey band between them. Repaint only the grey band so that the two scenes join into one continuous panorama: continue the cobblestone path from the left scene into the bare earth of the campsite on the right, continue the dead trees, roots, rocks, ruins and fog naturally, keep the horizon line, the sky gradient and the lighting continuous (cold moonlight on the left, warm firelight starting to appear toward the right). Do not change anything outside the grey band. Same size as Image 1. [STYLE]

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

### B7 — Château isolé, fenêtres allumées · `ui_chateau_allume_v01.png` · 16:9
Image jointe : **Image 1** = `ui_menu_principal_fond_v01.png`.

> Isolate the castle of Image 1 with everything that belongs to it: the spires, the main building with its crimson glowing windows, the arched bridges, the rock cliff underneath and the waterfalls, at the same size, same design and same position in the frame. Remove the moon, the sky, the clouds, the trees, the ruins, the fog and the ground, and replace them with a pure flat green background (#00FF00). The bottom of the cliff ends with a clean, slightly irregular rocky edge (no fog). Crisp outlines against the green, no glow spilling onto the green. [STYLE]

### B8 — Même château, fenêtres éteintes · `ui_chateau_eteint_v01.png` · 16:9
Image jointe : **Image 1** = `ui_chateau_allume_v01.png` (B7).

> Same image as Image 1, pixel for pixel, but every window is dark: replace each glowing crimson window with a dark unlit window (deep blue-black glass with faint cold moonlight reflections) and remove the crimson glow around them. Change nothing else: same castle, same outlines, same green background. [STYLE]

### B9 et B10 — Arbres lointains et ruines (moitiés gauche et droite) · `ui_arbres_lointains_g_v01.png`, `_d_v01.png` · 16:9 · **entrées préparées**
Image jointe : **Image 1** = `panorama_gauche.png` (puis `panorama_droite.png`), que je prépare après B2.

> From Image 1, keep only the middle-distance layer: the distant dead trees, the ruined stone arches and pillars and the low hills behind the path and behind the campsite, at the same size and position. Remove the sky, the moon, the clouds, the castle and its cliff, the fog, the big trees in the foreground, the path, the ground, the rocks, the roots and the camp objects, and replace all of them with a pure flat green background (#00FF00). Where a removed foreground object was hiding the middle layer, paint the missing trees and ruins so the layer is complete. Crisp outlines against the green, no fog, no glow spilling onto the green. [STYLE]

### B11 et B12 — Arbres proches (moitiés) · `ui_arbres_proches_g_v01.png`, `_d_v01.png` · 16:9 · **entrées préparées**
Image jointe : **Image 1** = `panorama_gauche.png` (puis `panorama_droite.png`).

> From Image 1, keep only the large trees closest to the viewer (the big twisted trunks with their branches that frame the scene, for example the huge tree on the right edge and the tree above the tent), complete with their roots where they meet the ground, at the same size and position. Replace everything else with a pure flat green background (#00FF00). Where another object was hiding part of a kept tree, paint the missing part. Crisp outlines against the green, no fog, no glow on the green. [STYLE]

### B13 et B14 — Sol et premier plan (moitiés) · `ui_sol_g_v01.png`, `_d_v01.png` · 16:9 · **entrées préparées**
Image jointe : **Image 1** = `panorama_gauche.png` (puis `panorama_droite.png`).

> From Image 1, keep only the ground and everything standing on it in the near distance: the cobblestone path and its puddles, the bare earth of the campsite, the fire ring with its embers, the rocks, the roots, the gravestones, the canvas canopy with its hanging lantern, the banner, the barrels and the crates, at the same size and position. Remove the sky, the moon, the clouds, the castle, the distant trees and ruins, the fog and the big framing trees, and replace them with a pure flat green background (#00FF00) above the ground line. Where a removed tree was hiding the ground or a camp object, paint the missing part. Crisp outlines against the green, no fog, no glow on the green. [STYLE]

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
| B | 16 (B1 à B16) + B17 si besoin | B2 et B9 à B14 sur entrées que je prépare |
| C | 12 (C1 à C12) + C-L si besoin | commencer par le test du renard (C1 pas nécessaire pour le test : j'utilise le renard de `ui_campement_v01`) |
