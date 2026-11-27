# Fin de la nuit 7 (2026-11-27)

## À faire sur `main` (urgent)

Les rochers du jardin sont corrigés dans leurs FBX (base enfoncée de 8 cm), mais `main` compense encore l'ancien défaut. **Depuis la
fusion de la PR #64, ils sont donc enterrés en jeu** :
- `Assets/_Project/Scripts/Runtime/Level/Acts/ActLevel.Dressing.cs` (~l. 528) : `sink` = 0.28 / 0.73 / 0.6 m → **0** ;
- `Assets/_Project/Settings/Ensembles/Acte3.txt` (l. 161-163, 171, 176, 181, 225) : y de −0,4 / −0,8 / −0,7 m pour les rochers → **0**.

Détail : `docs/art/INTEGRATION-PROPS.md`, section « Nuit 7, lot 2 ».

## Branches et PR

| Lot | Branche | État |
|---|---|---|
| 1. Artefacts de l'écran de choix | `art-artefacts-choix` (`a0e977c`) | poussée, **fusionnée par main : PR #63** |
| 2. Sous-bois du jardin, rochers | `art-sousbois` (`eb4022d`) | poussée, **fusionnée par main : PR #64** |
| 3. Revue des cartes en boucle | — | **pas déclenchée** : aucune nouvelle mention « À revoir après correction » sur `main` de 1 h à 8 h |
| Fin | `art-fin-7` (ce fichier) | poussée ; **PR à ouvrir à la main** (`gh` absent) : https://github.com/SergioMiGuPr/ItHunts/pull/new/art-fin-7 |

## Lot 1 : artefacts de l'écran de choix

Les quatre artefacts (lanterne de feu, épée en os, barillet, livre à lien) sont illustrés dans un même style, un même cadrage
(centrés en x, même ligne de base, marges au filet et aux vignettes vérifiées par le script) et une même taille, avec chacun une
animation lente et discrète. Les traînées tournantes sont retirées. La lanterne a été refaite par la même chaîne que les trois
autres, en gardant son dessin : l'ancienne découpe était floue, son fer en partie transparent, et des restes de traînées y étaient
incrustés. Le barillet aussi : c'était un contour doré translucide, hors du style de la lanterne et de l'objet en jeu.

| Demande | État | Preuve |
|---|---|---|
| Épée en os et livre à lien, style, cadrage et taille de la lanterne | fait (Nano Banana 2.1 d'après les fiches, référence de style : la lanterne ; détourage Higgsfield) | `docs/art/rendus/UI/Menus/Artefacts/Planche_Artefacts.jpg`, `Choix_Koala.jpg`, `Choix_Hibou.jpg` |
| Vérifier le barillet du caméléon | refait (même chaîne) | `Choix_Cameleon.jpg` |
| Lanterne : lueur qui respire et braises | fait | `Anim_Lanterne.gif` |
| Barillet : reflets | fait (éclat de métal poli toutes les 5,5 s, rune qui respire) | `Anim_Barillet.gif` |
| Épée : runes vertes qui pulsent | fait (de presque éteintes à vives, 4,5 s) | `Anim_Epee.gif` |
| Livre : pages qui frémissent, volutes bleues | fait | `Anim_Livre.gif` |
| Animation pas exceptionnelle → retirée | aucune retirée : les deux faibles de la première version (runes, volutes) ont été refaites jusqu'à être lisibles, vérifiées par le relecteur | relectures ci-dessous |
| Branchement dans UI-HANDOFF.md | fait | `docs/art/ui/UI-HANDOFF.md` § 11 (et § 2.5) |
| Captures en jeu, éclairage du jeu | vrai menu principal en Play Mode, instants fixés (`_ItHuntsMenuTemps`) | `docs/art/rendus/UI/Menus/Artefacts/` |
| 0 erreur au journal | 0 erreur, 0 avertissement au dernier passage | — |
| Tests | EditMode : 592 réussis, 1 non concluant (prévu), 0 échec | — |
| Une ligne au manifeste par asset, `.meta` | 13 sprites, shaders, matériaux, sources | `docs/assets/MANIFEST.md` |
| Relecture indépendante | 2 passes : 14 constats, puis 3 restants et 1 nouveau, tous corrigés (`0d6d2b8`, `a0e977c`) | — |

## Lot 2 : sous-bois du jardin, rochers

| Demande | État | Preuve |
|---|---|---|
| `Jardin_SousBois_B` en sous-bois texturé, lisible de nuit, varié | refait en cartes peintes de l'atlas de végétation (fougères, chêne, ronces, herbes, champignons pâles, souche, pierres), teinte variée carte par carte | `docs/art/rendus/Nuit7/Lot2/Pieces/`, `Planche_Avant_Apres.jpg` |
| Vérifier les autres pièces du sous-bois | `Jardin_SousBois_A`, les deux lisières, le grand arbre et la lisière de 20 m avaient le même défaut (aplats dentelés sombres) : refaits | idem, `Carte/` (acte III, graines 22 et 11) |
| Rochers `Jardin_Rocher_*` qui flottent de 0,3 à 0,7 m | corrigés (0,28 / 0,73 / 0,60 m) : chaque bloc posé, base enfoncée de 8 cm ; boîte du petit rocher recalée | `Pieces/Jardin_Rocher_*` ; **voir « À faire sur main »** |
| Aucune face superposée | 0 sur les 9 pièces (`faces_superposees`) | — |
| Collisions sur tout ce qui est solide | bois, souches et pierres : maillage ; rochers : maillage ou boîte ; feuillage traversable sans collision ; lisières : `Col_Lisiere` inchangé | `SousBoisBuilder.cs` (rapport) |
| 0 erreur au journal | 0 erreur, 0 avertissement | — |
| Manifeste | lignes des 9 pièces et du matériau `M_Kit_SousBois` (colonne IA : atlas généré) | `docs/assets/MANIFEST.md` |
| Relecture indépendante | 2 passes : 14 constats, puis 5 partiels et 5 nouveaux, tous traités (`e3df5d7`, `eb4022d`) ; restent assumés : pied des rideaux sombres de la lisière de 20 m, étirement non uniforme des lisières posées en mur par `ActLevel.Kits` | `INTEGRATION-PROPS.md` |

À vérifier côté `main` : emprise plus grande des sous-bois dans les ensembles qui les posent (`Jardin_Serre_B`, `KiosqueEns_B`,
`Clairiere_A/B`, `FontaineBancs_B`) ; images par seconde avec les lisières en cartes (+36 % de triangles par lisière de 12 m,
feuillage sans ombre portée et statique).

## Lot 3 : revue des cartes

Pas déclenchée : l'en-tête de `REVUE-CARTES.md` sur `main` est resté celui de la seconde revue (pas de nouvelle mention
« À revoir après correction » après `4500b0c`). `main` a été vérifié toutes les 10 à 15 minutes de 1 h à 8 h (`3f7662f` à la fin) :
journal dans `docs/art/rendus/Nuit7/veille_main.txt`. **État final de la revue** : celui de la seconde revue (8 défauts à corriger
par `main`, en tête les plafonds noirs des actes I et II). Le point marqué DA (`Ens_Buis_Boule`, triangles noirs) a été corrigé par
`art-vegetaux` (PR #62, fusionnée cette nuit).

## Crédits

**16,00 crédits** sur 40 (solde Higgsfield 644,19 → 628,19) : 8 images Nano Banana 2.1 en 1K (4 demandes de 2 variantes : épée,
livre, barillet, lanterne ; 1,5 crédit l'image) et 4 détourages (1 crédit chacun). Aucun achat. Lot 2 : aucun crédit (atlas
existant). Mains FPS et modèle du traqueur non modifiés ; `.mcp.json`, `Mobile_RPAsset.asset` et `EditorSettings.asset` non
touchés, aucun fichier local remis à l'état du dépôt (`ProjectSettings/NavMeshAreas.asset`, modifié par Unity, laissé tel quel,
non commité).

## Outils de la nuit

- `Assets/Art/Source~/UI/scripts/menus/artefacts_choix.py` et `MenusBuilder.ArtefactsChoix` (artefacts) ;
- `Assets/Art/Source~/Kits/scripts/SousBoisBuilder.cs`, classe `Cartes` de `kit_nuit5.py` (cartes de végétation) ;
- `CapturesLot3N5.cs` : point de pose libre.
