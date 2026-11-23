# Améliorations

**Status:** implemented (rangs, nuit 4) · **Updated:** 2026-11-23 · [Implementation](implementation.md) · [Testing](testing.md) · [GDD](../../GDD.md) · D-043, D-100

## Résumé

70 améliorations à **5 rangs** (Bronze, Argent, Or, Diamant, Mythique), gagnées dans les coffres (un choix parmi 3) ou achetées au
marchand. La liste de référence, avec l'effet de chaque rang et les synergies, est [AMELIORATIONS.md](../../AMELIORATIONS.md) ;
les assets en sont générés par `docs/design/outils/gen_upgrades.py`.

## Règles

1. **Une amélioration = un asset** (`UpgradeDefinition`) : code du document (G1, R-P1, C3…), nom, description d'une ligne, icône
   (sprite de la DA, sinon le symbole), personnage (Aucun : générique), passif, une valeur par rang pour 1 à 3 paramètres et une
   ligne de texte par rang.
2. **Tables** : 30 génériques dans `UpgradePool_Default` ; 10 par personnage (2 passifs + 8 de l'artefact) dans
   `UpgradePool_Renard`, `_Cameleon`, `_Koala`, `_Hibou`, réservées à leur porteur (aussi en coop).
3. **Emplacements** : 10 par joueur ; **un seul passif** (en prendre un autre remplace le premier, rang perdu). Une amélioration
   possédée **monte de rang** au lieu de se cumuler ; passé 10, seules les montées de rang sont proposées.
4. **Marchand** : 1 amélioration nouvelle + 1 montée de rang d'une amélioration possédée non Mythique + le soin ; rien à monter :
   2 nouvelles ; emplacements pleins : 2 montées. **Prix par rang** visé : 60 (nouvelle, Bronze), 90 (Argent), 140 (Or),
   220 (Diamant), 350 (Mythique) ; hausse par achat **+10 % du prix de base**, plafonnée à ×2 (`EconomySettings`).
5. **Coffre** : 3 choix dont au moins 1 nouvelle ; une montée de rang possible dès l'acte II (dès l'acte I avec l'Œil du
   collectionneur, rang Or).
6. **Rangs** : Bronze = valeur de base ; chaque rang augmente la valeur ; l'Or ajoute souvent un petit effet ; le Mythique un
   effet nouveau (en gras dans le document).
7. **Traqueur** : aucun effet ne le blesse, ne l'étourdit, ne le repousse ni ne le ralentit (hors de l'arène du boss) ; seules les
   améliorations qui le disent agissent sur sa détection (Souffle court, Peau changeante, Vision nocturne).
8. **États lus par les synergies** (`Damageable`) : étourdi, paralysé, en feu, saignant, lié.
9. **HUD** : rangée d'icônes au-dessus de la barre de vie, en bas à gauche, cadre à la couleur du rang, passif en premier, 10 cases.
10. **Écran de fin** : noms avec rang, 3 par ligne, plusieurs lignes « Améliorations (1/n) » au besoin (plus de débordement).

## Interactions

| Fonctionnalité | Lien |
|---|---|
| [Âmes et économie](../ames/design.md) | coffres, marchand, prix par rang |
| [Mouvement](../mouvement/design.md), [Orbes](../orbes/design.md), [Artefacts](../artefacts/design.md) | systèmes modifiés |
| [Vagues](../vagues/design.md) | le scaling des goules lit la puissance (améliorations + rangs) |

## Questions ouvertes

- Équilibre des 70 au rang Mythique : à régler en playtest (les valeurs du document sont des valeurs de départ).
- Icônes et cadres de rang : à fournir par la DA (70 icônes, 5 cadres).

## History

- 2026-11-13 : 8 premières améliorations (D-043).
- 2026-11-22 : 8 du caméléon (D-097).
- 2026-11-23 : 70 améliorations à rangs, emplacements, passif, marchand à montée de rang (D-100).
