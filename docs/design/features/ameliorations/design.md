# Améliorations

**Status:** implemented · **Updated:** 2026-11-13 · [Implementation](implementation.md) · [Testing](testing.md) · [GDD](../../GDD.md) · D-043

## Résumé

Des améliorations individuelles, gagnées dans les coffres (un choix parmi 3) ou achetées au marchand, qui modifient l'orbe, le
mouvement, les stats et l'artefact du joueur pour la run.

## Règles

1. **Une amélioration = un asset** (`UpgradeDefinition`) : nom, **description d'une ligne**, icône (greybox : symbole court et
   couleur de l'orbe), effet, valeur, prix chez le marchand. Le pool (`UpgradePool_Default`) liste celles qu'on peut tirer.
2. **Tirage avec la seed de la run :** chaque coffre et le marchand tirent des améliorations **différentes** dans le pool ; même
   seed, mêmes tirages (rejouer une run).
3. **Individuelles (D-007) :** elles ne touchent que le joueur qui les prend ; elles **se cumulent** si on reprend la même.
4. **Les réglages partagés ne changent pas** : le joueur reçoit sa propre copie des réglages modifiés (mouvement, magnétisme,
   barillet). Un réglage modifié en Play Mode sur l'asset partagé ne le touche plus après sa première amélioration.

## Les 8 premières (validées)

| Amélioration | Effet | Icône | Prix |
|---|---|---|---|
| Charge en plus | +1 charge de mouvement (dash, saut en l'air) | +1 | 70 |
| Traction rapide | une charge de traction revient 1 s plus vite (6 → 5 s) | -1s | 60 |
| Grande orbe | rayon de collecte des âmes +50 % (8 → 12 m) | R+ | 40 |
| Projectiles lourds | objets lancés ou repoussés : +25 % de dégâts (zone comprise) | x1.25 | 70 |
| Onde blessante | la répulsion blesse les ennemis touchés : 10 PV (D-008 ; jamais le traqueur) | 10 | 60 |
| Cœur solide | +25 PV max, et 25 PV tout de suite | PV+ | 50 |
| Barillet garni | +2 balles au barillet (6 → 8) | +2 | 50 |
| Aspiration | les âmes viennent à l'orbe 50 % plus vite (25 → 37.5 m/s) | >> | 40 |

## Interactions

| Fonctionnalité | Lien |
|---|---|
| [Âmes et économie](../ames/design.md) | coffres, marchand |
| [Mouvement](../mouvement/design.md), [Orbes](../orbes/design.md), [Artefacts](../artefacts/design.md) | systèmes modifiés |

## Questions ouvertes

- Rareté, améliorations qui se combinent, reliques passives (BRIEF).
- Valeur de l'« Onde blessante » (10 PV, proposée sans valeur demandée).

## History

- 2026-11-13 : designed et construit (étape 7, D-043).
