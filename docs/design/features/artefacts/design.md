# Artefacts

**Status:** designed · **Updated:** 2026-11-12 · [GDD](../../GDD.md)

Un artefact est une arme ou un pouvoir de combat porté en **main gauche**, en plus des orbes (main
droite). Il donne une façon de se battre quand l'arène ne fournit plus rien à lancer, et rend chaque run
différente. Cohérent avec D-008 : de base, les orbes ne font pas de dégâts directs ; les artefacts et
les améliorations en ajoutent (D-022).

## Règles

1. **Tirage :** au début de chaque run, chaque joueur reçoit **1 artefact tiré au hasard** (seed de la
   run).
2. **Coop :** deux joueurs ne peuvent pas avoir le même artefact (tirage sans remise).
3. **Emplacement :** un seul artefact à la fois, en main gauche. Système générique : chaque artefact
   définit son tir, ses munitions et son rechargement dans ses propres réglages (un ScriptableObject par
   artefact).
4. **Premier artefact, le barillet :** la main gauche se transforme en **barillet de 6 munitions** qui
   tire des projectiles.

## Valeurs

À fixer avec les questions ouvertes ; ScriptableObject `ArtifactSettings` (commun) et un asset par
artefact (`Barrel_Default`).

| Nom | Valeur | Unité | À observer en playtest |
|---|---|---|---|
| Munitions du barillet | 6 | balles | |
| Dégâts, cadence, vitesse des projectiles | à définir | | temps pour tuer une goule |
| Rechargement | à définir | s | |

## Interactions

| Fonctionnalité | Lien |
|---|---|
| [Orbes](../orbes/design.md) | main droite ; clic gauche et clic droit sont déjà pris |
| Améliorations | pourront modifier un artefact (dégâts, munitions…) |
| Coop en ligne | tirage sans doublon entre les joueurs ; projectiles à synchroniser |

## Questions ouvertes

- **Touche de tir de la main gauche** (clic gauche et clic droit sont pris par les orbes) : clic
  molette ou bouton latéral de la souris, une touche du clavier, ou un modificateur ; à la manette,
  RB est libre (LB = dash).
- **Rechargement du barillet :** automatique quand il est vide, sur une touche, ou les deux.
- **Dégâts et cadence de départ** du barillet (et vitesse ou gravité des projectiles).

## History

- 2026-11-12 : designed (D-022).
