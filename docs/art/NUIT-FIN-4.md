# Fin de la nuit du 2026-11-23 (piste art, nuit 4)

Ce fichier signale la fin de la nuit à `main`. Il contient :
- la liste des PR et leur état ;
- le contrôle du brief, point par point, avec une preuve pour chaque point ;
- les crédits dépensés ;
- ce qui reste ouvert.

Chaque lot est passé dans une branche partie de `main` à jour, sauf les deux branches empilées signalées. Chaque lot a eu au moins une relecture indépendante, faite par un sous-agent qui n'avait pas vu le travail, et ses points ont été corrigés avant la PR. Toutes les captures sont prises en jeu (Play Mode, éclairage du jeu, lanterne du joueur, seed 4242), sauf les vitrines des kits, signalées comme telles.

## PR de la nuit

| PR | Branche | Lot | État à l'écriture |
|---|---|---|---|
| [#45](https://github.com/SergioMiGuPr/ItHunts/pull/45) | `art-textures-1` | 0 : textures peintes fournies (raccord sans couture, tuiles 2 × 2 m) | fusionnée |
| [#46](https://github.com/SergioMiGuPr/ItHunts/pull/46) | `art-menus-4` | 1 : campement, personnages assis peints dans la maquette | fusionnée |
| [#47](https://github.com/SergioMiGuPr/ItHunts/pull/47) | `art-materiaux-2` | 2 : matériaux et échelle (512 px/m), matériaux du château, 0 face superposée dans les kits | fusionnée |
| [#48](https://github.com/SergioMiGuPr/ItHunts/pull/48) | `art-ameliorations` | 5 : 70 illustrations des améliorations, 10 cadres de rang | fusionnée |
| [#49](https://github.com/SergioMiGuPr/ItHunts/pull/49) | `art-ui-4` | 6 : écran de fin, marchand à rangs, porte de sortie scellée, carte unifiée | fusionnée |
| [#50](https://github.com/SergioMiGuPr/ItHunts/pull/50) | `art-vfx-sons-4` | 7 : marquage du boss, contour des volants, apparitions, screamer, cris du traqueur | fusionnée |
| [#51](https://github.com/SergioMiGuPr/ItHunts/pull/51) | `art-props-3d` | 4 : 17 props en 3D, 3 variantes, coffres lumineux, LOD | ouverte |
| [#52](https://github.com/SergioMiGuPr/ItHunts/pull/52) | `art-kits-3` | 3 : kits verticaux, portes à ouvrir, torches, ciel | ouverte ; empilée sur #47, déjà fusionnée |
| cette PR | `art-fin-4` | fin : ce fichier | ouverte |

## Crédits (plafond de la nuit : 360)

| Poste | Plafond | Dépensé | Preuve |
|---|---|---|---|
| Illustrations des améliorations (lot 5) | 140 | **66** | devis dans la PR #48 ; `docs/art/ameliorations/AMELIORATIONS-ART.md` |
| Modèles 3D des props (lot 4) | 200 | **191** (88 + 103) | `docs/art/props/sources/JOURNAL-DEPENSES.md` ; PR #51 |
| Cris du traqueur (lot 7) | 20 | **8,5** (8 essais Mirelo) | PR #50 ; manifeste (lignes `Sfx_Traqueur_Cri_*`) |
| **Total** | **360** | **265,5** | solde Higgsfield : 949,72 au début de la nuit, 684,22 à la fin (`balance`) |

Aucun achat de plugin. Aucun modèle 3D d'ennemi ni de marchand. Les mains FPS et le modèle du traqueur (`EMPREINTE.md`) n'ont pas été touchés.

## Contrôle du brief, point par point

Un point sans preuve n'est pas compté comme fait. Les preuves sont un commit, une capture ou une mesure.

| Point du brief | État | Preuve |
|---|---|---|
| Recherche courte (texels, châteaux gothiques, fenêtres, marquages de boss) | fait | `docs/art/RECHERCHE-NUIT-4.md` (#45) |
| **Lot 0** : textures sans couture, tuiles cohérentes | fait | #45 ; planches `docs/art/rendus/Nuit4/Lot0/` ; `raccord.py` (coupure minimale) |
| **Lot 1** : campement, personnages assis, éclairage, ordre de dessin | fait | #46 (fusionnée) ; `docs/art/rendus/Nuit4/Lot1/LOT1-CAMPEMENT.md`, planches places, avant/après, zoom × 3 |
| **Lot 2** : densité 512 px/m, matériaux du château, faces superposées | fait | #47 ; ART-HANDOFF « Densité de texels » ; `faces_apres_*.json` (0 recouvrement sur 98 pièces) |
| **Lot 2** : bug des sols des fonctions (code de main) | signalé | ART-HANDOFF (note sur `ActLevel.Geometry`) |
| **Lot 3** : escaliers droits, tournants, en colimaçon, mezzanines, galeries, balcons, coursives, passerelles | fait | #52 ; `Planche_Unity_KitCaves/Chateau/Jardin.jpg`, `Planche_Jeu_Lot3.jpg` |
| **Lot 3** : portes à la taille du joueur et portes étroites du traqueur | fait | #52 ; gabarit de 4 × 5,25 m dans INTEGRATION-PROPS |
| **Lot 3** : puits, trémies, échelles, plateformes de wall jump, murs escaladables | fait | #52 ; puits avec `Deco_Eau` et fond solide à −2,4 m ; murs d'escalade à prises intégrées (1 038 et 1 388 triangles) |
| **Lot 3** : fenêtres gothiques à vitraux sombres avec lune, chandeliers, lustres, appliques, torches au château | fait | #52 ; `Torche_Murale`, `Torchere`, vitraux au plomb aux teintes sourdes, `M_Kit_Cristal` |
| **Lot 3** : falaises, terrasses, escaliers de jardin, ponts, murs d'enceinte | fait | #52 ; `Planche_Unity_KitJardin.jpg` |
| **Lot 3** : lune du menu principal pour le ciel, plus de halo en paliers | fait pour le ciel des kits ; à brancher par main pour l'acte III | `T_Ciel_Lune.png`, shader `ItHunts/Ciel` continu ; le ciel actuel de l'acte III ne passe pas par `Jardin_Ciel` (demande à main dans #52) |
| **Lot 3** : supprimer les grosses formes noires ellipsoïdales | fait dans les kits ; demande à main pour l'acte III | lierre et buissons en boule retirés (#52) ; les sphères restantes en jeu sont les arbres de secours de `ActLevel.Dressing.Tree` : ajouter `Jardin_ArbreMort_*` à `Act_3.asset > pieces` |
| **Lot 3** : porte verrouillée et porte de sortie distinctes, avec animation d'ouverture | fait | #52 ; composant `OuverturePorte` (13 portes) ; captures 0 / 50 / 100 % ; course contrôlée (`controle_parties_mobiles.json`, 0 intersection) |
| **Lot 3** : plus de cellules pour le traqueur, notées dans INTEGRATION-PROPS | fait | 5 cellules (#52) |
| **Lot 4** : props greybox remplacés par de vrais modèles (Tripo, ≤ 200 crédits) | fait, sauf variantes rares de l'acte II | #51 ; 17 modèles et 3 variantes ; `Planche_Jeu_Acte1/2/3.jpg` ; couverture mesurée dans `Jeu/points_Acte*.txt` ; 191 crédits |
| **Lot 5** : 70 illustrations (≤ 140 crédits) et 5 cadres de rang | fait | #48 ; `docs/art/ameliorations/planche_64.png`, `planche_cadres.png` ; 66 crédits |
| **Lot 6** : écran de fin en grille, minimap à un seul design avec icônes goule et traqueur | fait | #49 ; `docs/art/rendus/Nuit4/Lot6/`, `Cartes_Unifiees.jpg` |
| **Lot 6** : panneau de porte de sortie verrouillée (temps et explication), marchand (nouvelle et montée), UI-HANDOFF | fait | #49 ; UI-HANDOFF § 9 |
| **Lot 7** : marquage du boss sombre et sobre (bord net, remplissage translucide, montée avant l'impact) | fait | #50 ; `Planche_Boss_*.jpg` sur de vraies attaques |
| **Lot 7** : volants sans boule rouge ni violet (au plus un contour discret) | fait | #50 ; `Planche_Volant.jpg` (3,2 m, 12 m, 20 m), `Planche_Apparition.jpg` |
| **Lot 7** : screamer de mort, et version adoucie | fait | #50 ; `Planche_Screamer.jpg`, `Planche_Screamer_Adouci.jpg` |
| **Lot 7** : cris du traqueur, apparition et « je t'ai vu » (≤ 20 crédits), listés dans ART-HANDOFF | fait | #50 ; 4 fichiers WAV ; tableau des sons d'ART-HANDOFF ; 8,5 crédits |
| Une branche par lot, commits avec leurs `.meta`, push et PR, jamais de fusion ni de push forcé | fait | PR #45 à #52 ; aucune fusion faite par la piste art |
| Une ligne au manifeste par asset (source, droits, IA oui/non) | fait | `docs/assets/MANIFEST.md` (lignes des lots 0 à 7) |
| Devis avant chaque lot, noté dans la PR | fait | descriptions des PR #48, #50 et #51 (lots payants) ; 0 crédit pour les autres |
| Relecture indépendante de chaque lot, corrigée avant la PR | fait | lots 0, 1 (2 passes), 2, 5, 6, 7 (2 passes), 4 (2 passes) et 3 ; points listés dans chaque PR |
| Journal Unity : 0 erreur, aucune erreur de maillage sur nos assets | fait | `docs/art/rendus/Nuit4/Lot4/journal_unity.txt` ; section « Contrôle » des PR #50, #51 et #52. Seules restent les erreurs `MagnetismAudio.OnDestroy` (code de main) |
| Unity et Blender fermés en fin de nuit | fait | voir le rapport final |

## Pour main (récapitulatif)

- **Fusion** : #51 et #52 ; #52 contient aussi les commits du lot 2, déjà fusionnés.
- **Erreur à corriger côté main** : `ItHunts.Audio.MagnetismAudio.OnDestroy` (`Destroy` sur un asset).
- **Branchements à faire**, documentés dans chaque PR :
  - `FinalBoss` → `MarquageBoss` ;
  - `WaveDirector.telegraphMaterial` → `M_VFX_Apparition` ;
  - `ContourDiscret` sur `Volant.prefab` ;
  - `ScreamerMort` à la mort par le traqueur ;
  - cris d'apparition et « je t'ai vu » ;
  - props selon la règle de pose (`Props4.Ajuste`) ;
  - `Jardin_ArbreMort_*` dans `Act_3.asset > pieces` ;
  - `Jardin_Ciel` comme ciel des actes ;
  - `OuverturePorte.Ouvre()` sur les portes.
- **Demandes de réglages partagés** :
  - un décalque URP pour les marquages sur sol en relief ;
  - des couches de volume par joueur pour le screamer en écran partagé ;
  - doser les lumières des pièces lumineuses (4 lumières par objet dans `PC_RPAsset`).

## Ce qui reste ouvert

- **Droits commerciaux** des contenus générés (Higgsfield : Nano Banana 2.1, Tripo H3.1, Mirelo) : marqués « à confirmer par l'utilisateur » dans le manifeste.
- **Couleur du trait de l'orbe vers un ennemi tenu**, violet selon la règle « violet = objet tenu » : à trancher par l'utilisateur.
- **Lot 4** :
  - variantes rares de l'acte II sans modèle adapté : armoires couchées, petits supports de râtelier, deux poutres ;
  - étirements résiduels de la première passe : sarcophage et râtelier × 1,73, coffre verrouillé × 1,42 ;
  - densité de texels de 280 à 400 px/m sur les plus grands props.
- **Captures en jeu du lot 3** : salles des actes I et II seulement ; à la seed 4242, l'acte III n'a pas de salle aux dimensions retenues (fond à 6–22 m). Beaucoup de vues restent très sombres, à l'image de l'éclairage du jeu.
- **Fichiers locaux hors périmètre**, non commités, laissés tels quels : `.mcp.json`, `ProjectSettings/EditorSettings.asset`, `ProjectSettings/NavMeshAreas.asset`, `Assets/Settings/Mobile_RPAsset.asset`. Dossiers vides `C:\docs\…` laissés par un sous-agent : à supprimer à la main.
