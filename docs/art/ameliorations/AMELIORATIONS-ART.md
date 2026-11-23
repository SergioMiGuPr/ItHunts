# Illustrations des améliorations (nuit 4, lot 5)

70 illustrations, une par amélioration de `docs/design/AMELIORATIONS.md`, plus 10 cadres de rang : 5 rangs × (carré pour les améliorations, losange pour les passifs).

## Fabrication

- **Génération** : Higgsfield → Nano Banana 2.1, en 4K, par grilles de 2 × 2 icônes (18 grilles + 2 planches de cadres + 2 grilles refaites après relecture = **22 images, 66 crédits**). L'image de référence de style est un recadrage de la maquette `ui_choix_perso_v01` de l'utilisateur.
- **Prompts** : formule commune dans `Assets/Art/Source~/UI/scripts/ameliorations/liste_icones.py` (sujet de chaque icône), plus le gabarit ci-dessous. Chaque icône a un sujet unique et central, des contours encrés, des aplats, un fond sombre fumé et une seule couleur d'accent, pour rester lisible à 64 px.
- **Découpe** : `decoupe_icones.py` découpe les quarts rognés et supprime les gouttières claires. `cadres.py` extrait les cadres (alpha hors du blanc pur, pointes voisines masquées).
- **Refaites après la relecture indépendante** (doublons à 64 px ou sujet faux) : G13 Sillage, G15 Onde de choc, K4 Onde de taille, G23 Charge en plus, K-P1 Peau épaisse, H1 Lien multiple, G17 Charge de répulsion, R3 Mèche vive (grilles 20 et 21).
- **Refaite une fois** : C8 « Langue de caméléon », que la grille 12 dessinait comme un serpent. La version retenue est celle de la grille 17. La version de la grille 12 reste dans les sources et n'est pas utilisée.

## Gabarit de prompt (grilles)

> A 2x2 grid of four separate square game ability icons, exactly 4 equal square cells separated by thin solid black gutters, no frames, no borders, no text, no letters, no numbers. Each icon: one single bold central subject filling 70% of its cell, hand-painted dark fantasy illustration, thick black ink outlines, flat cel-shaded color planes with painted texture, dramatic rim light, deep dark smoky background vignette (near black, slightly warm), strong silhouette readable at 64 pixels, limited muted palette (bone, rusted iron, old gold, dried blood red) with one glowing accent color per icon. Same painting style as the reference image (gothic grimdark storybook painting). Top-left cell: … Top-right cell: … Bottom-left cell: … Bottom-right cell: …

Cadres : « Game UI asset sheet on a perfectly flat pure white background: a single row of five separate square [ou diamond-shaped] ornate gothic icon frames… 1) aged bronze-copper, 2) tarnished silver, 3) old gold, 4) pale icy diamond blue crystal inlays on silver, 5) mythic violet-magenta crystal inlays on dark iron with tiny violet glow… »

## Utilisation (HUD, marchand, écran de fin)

- **Icône** : `Assets/Art/UI/Ameliorations/Icones/ICO_<id>.png`, 512 px, sprite. Pour les passifs, le `-` de l'identifiant devient `_` (ex. `ICO_R_P1`).
- **Cadre** : `Assets/Art/UI/Ameliorations/Cadres/Cadre_<Rang>.png` pour une amélioration, `Cadre_Passif_<Rang>.png` pour un passif. Rangs : `Bronze`, `Argent`, `Or`, `Diamant`, `Mythique`.
- **Cadres** : même emprise pour les 5 rangs (496 px dans 512, la rangée du HUD ne saute pas) ; bords décontaminés (plus de liseré blanc) ; **repère de forme du rang** pour les daltoniens : 1 à 5 gemmes serties sous le cadre (Bronze 1 … Mythique 5).
- **Masque des passifs** : `Cadres/Masque_Passif_Losange.png` (losange à 62 %) à poser en `Mask` sur l'icône d'un passif.
- **Composition** : le cadre a la taille de la case. L'icône est centrée **à 80 %** de la case sous un cadre carré. Sous un cadre de passif, elle est centrée **à 62 %** et découpée en losange (masque losange, sommets aux milieux des côtés).
- **Tailles conseillées** : HUD 56 à 64 px (rangée au-dessus de la barre de vie, le passif en premier), écran de fin 72 px, marchand 128 à 160 px.
- **Planches de contrôle** : `planche_64.png` (les 70 icônes à 64 px) et `planche_cadres.png` (rangs à 128 et à 64 px).

## Correspondance

| Id | Amélioration | Fichier | Source |
|---|---|---|---|
| G1 | Riposte | `ICO_G1.png` | grille_00 case 0 |
| G2 | Garde large | `ICO_G2.png` | grille_00 case 1 |
| G3 | Second souffle | `ICO_G3.png` | grille_00 case 2 |
| G4 | Rythme de fer | `ICO_G4.png` | grille_00 case 3 |
| G5 | Grande orbe | `ICO_G5.png` | grille_01 case 0 |
| G6 | Aspiration | `ICO_G6.png` | grille_01 case 1 |
| G7 | Âmes gourmandes | `ICO_G7.png` | grille_01 case 2 |
| G8 | Marchandage | `ICO_G8.png` | grille_01 case 3 |
| G9 | Orbe vorace | `ICO_G9.png` | grille_02 case 0 |
| G10 | Traction rapide | `ICO_G10.png` | grille_02 case 1 |
| G11 | Harpon | `ICO_G11.png` | grille_02 case 2 |
| G12 | Élan de traction | `ICO_G12.png` | grille_02 case 3 |
| G13 | Sillage | `ICO_G13.png` | grille_03 case 0 |
| G14 | Onde blessante | `ICO_G14.png` | grille_03 case 1 |
| G15 | Onde de choc | `ICO_G15.png` | grille_03 case 2 |
| G16 | Rebond | `ICO_G16.png` | grille_03 case 3 |
| G17 | Charge de répulsion | `ICO_G17.png` | grille_04 case 0 |
| G18 | Projectiles lourds | `ICO_G18.png` | grille_04 case 1 |
| G19 | Cœur solide | `ICO_G19.png` | grille_04 case 2 |
| G20 | Détonateur | `ICO_G20.png` | grille_04 case 3 |
| G21 | Briseur d'os | `ICO_G21.png` | grille_05 case 0 |
| G22 | Prédateur | `ICO_G22.png` | grille_05 case 1 |
| G23 | Charge en plus | `ICO_G23.png` | grille_05 case 2 |
| G24 | Semelles de vent | `ICO_G24.png` | grille_05 case 3 |
| G25 | Pas de chat | `ICO_G25.png` | grille_06 case 0 |
| G26 | Vif-argent | `ICO_G26.png` | grille_06 case 1 |
| G27 | Chute amortie | `ICO_G27.png` | grille_06 case 2 |
| G28 | Allumette | `ICO_G28.png` | grille_06 case 3 |
| G29 | Souffle court | `ICO_G29.png` | grille_07 case 0 |
| G30 | Œil du collectionneur | `ICO_G30.png` | grille_07 case 1 |
| R-P1 | Fourrure de braise | `ICO_R_P1.png` | grille_07 case 2 |
| R-P2 | Flair | `ICO_R_P2.png` | grille_07 case 3 |
| R1 | Long souffle | `ICO_R1.png` | grille_08 case 0 |
| R2 | Flamme large | `ICO_R2.png` | grille_08 case 1 |
| R3 | Mèche vive | `ICO_R3.png` | grille_08 case 2 |
| R4 | Braises | `ICO_R4.png` | grille_08 case 3 |
| R5 | Feu grégeois | `ICO_R5.png` | grille_09 case 0 |
| R6 | Combustion | `ICO_R6.png` | grille_09 case 1 |
| R7 | Souffle double | `ICO_R7.png` | grille_09 case 2 |
| R8 | Foulée ardente | `ICO_R8.png` | grille_09 case 3 |
| C-P1 | Peau changeante | `ICO_C_P1.png` | grille_10 case 0 |
| C-P2 | Œil fendu | `ICO_C_P2.png` | grille_10 case 1 |
| C1 | Balles paralysantes | `ICO_C1.png` | grille_10 case 2 |
| C2 | Cadavre explosif | `ICO_C2.png` | grille_10 case 3 |
| C3 | Balle perforante | `ICO_C3.png` | grille_11 case 0 |
| C4 | Ricochet | `ICO_C4.png` | grille_11 case 1 |
| C5 | Main leste | `ICO_C5.png` | grille_11 case 2 |
| C6 | Sixième chambre | `ICO_C6.png` | grille_11 case 3 |
| C7 | Éventail | `ICO_C7.png` | grille_12 case 0 |
| C8 | Langue de caméléon | `ICO_C8.png` | grille_17 case 2 |
| K-P1 | Peau épaisse | `ICO_K_P1.png` | grille_12 case 2 |
| K-P2 | Griffes grimpantes | `ICO_K_P2.png` | grille_12 case 3 |
| K1 | Lame longue | `ICO_K1.png` | grille_13 case 0 |
| K2 | Enchaînement | `ICO_K2.png` | grille_13 case 1 |
| K3 | Fente | `ICO_K3.png` | grille_13 case 2 |
| K4 | Onde de taille | `ICO_K4.png` | grille_13 case 3 |
| K5 | Os brisé | `ICO_K5.png` | grille_14 case 0 |
| K6 | Garde d'os | `ICO_K6.png` | grille_14 case 1 |
| K7 | Brise-garde | `ICO_K7.png` | grille_14 case 2 |
| K8 | Élan du bûcheron | `ICO_K8.png` | grille_14 case 3 |
| H-P1 | Vision nocturne | `ICO_H_P1.png` | grille_15 case 0 |
| H-P2 | Plumes | `ICO_H_P2.png` | grille_15 case 1 |
| H1 | Lien multiple | `ICO_H1.png` | grille_15 case 2 |
| H2 | Chaînage | `ICO_H2.png` | grille_15 case 3 |
| H3 | Lien vorace | `ICO_H3.png` | grille_16 case 0 |
| H4 | Sangsue | `ICO_H4.png` | grille_16 case 1 |
| H5 | Longue portée | `ICO_H5.png` | grille_16 case 2 |
| H6 | Lien d'ancrage | `ICO_H6.png` | grille_16 case 3 |
| H7 | Pages brûlantes | `ICO_H7.png` | grille_17 case 0 |
| H8 | Soin lié | `ICO_H8.png` | grille_17 case 1 |

La variante de « Âmes gourmandes » (grille 17) n'est pas retenue : elle reste dans `sources/` (hors du jeu).
