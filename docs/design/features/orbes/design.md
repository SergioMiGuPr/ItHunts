# Orbes

**Status:** designed · **Updated:** 2026-11-12 · [GDD](../../GDD.md)

Deux orbes, un seul actif, échange instantané. Le magnétisme déplace le joueur (et bouscule les
ennemis) ; la télékinésie transforme l'arène en munitions. De base, les orbes ne font pas de
dégâts directs ; des améliorations peuvent en ajouter, en plus de l'environnement (D-008). Elles
collectent aussi les âmes autour d'elles. Piliers : « l'arène est
l'arme », « le mouvement est la survie ».

## Règles

1. **Échange :** une touche (molette au clavier-souris, un bouton à la manette), sans recharge.
   Échanger lâche l'objet tenu.
2. **États de chaque orbe :** Prête → EnUsage → Recharge → Prête. Coût (D-015) : le
   **magnétisme** consomme **1 charge de mouvement** par attraction ou répulsion (barre partagée
   avec le saut en l'air et le dash, voir [mouvement](../mouvement/design.md)) ; la
   **télékinésie** consomme sa propre **endurance**. Sans ressource, l'usage est refusé (son et HUD).
3. **Attraction (magnétisme, mode 1) :** un rayon sur une surface magnétique (qui brille) à portée
   donne une **impulsion unique** vers le point visé, ajoutée à la vitesse actuelle. Surfaces
   seulement, jamais les ennemis.
4. **Répulsion (magnétisme, mode 2) :** même visée ; **impulsion opposée** au point visé
   (rocket jump). Au point visé, une **onde** bouscule les ennemis dans son rayon. Un ennemi qui
   percute un mur au-dessus d'une vitesse d'impact prend des dégâts proportionnels. **Sans effet
   sur le boss.**
5. **Attraper (télékinésie, mode 1) :** un rayon attrape un objet, un petit ennemi (sous la masse
   maximale) ou un projectile ennemi. La goule n'est attrapable qu'**étourdie**. L'objet tenu
   flotte devant le joueur.
6. **Lancer (télékinésie, mode 2) :** l'objet part dans la direction de visée ; ses dégâts
   dépendent de sa masse et de sa vitesse d'impact. Un projectile ennemi renvoyé blesse son tireur.
7. **Âmes :** chaque orbe aspire automatiquement les âmes dans un rayon autour d'elle.
8. **Dégâts par amélioration :** une amélioration peut donner des dégâts directs à une orbe
   (par exemple l'onde de répulsion) ou un bonus aux objets lancés. Ces dégâts s'ajoutent à
   ceux de l'environnement, sans les remplacer.

## Valeurs

ScriptableObjects `MagnetismOrbSettings`, `TelekinesisOrbSettings`, `StaminaSettings` (endurance de
la télékinésie). Les charges de mouvement sont dans `MovementSettings`. Valeurs de départ [ASSUMPTION].

| Nom | Valeur | Unité | À observer en playtest |
|---|---|---|---|
| Endurance max (télékinésie) | 100 | pts | |
| Régénération / délai avant régénération | 25 / 0.8 | pts/s / s | rythme d'usage |
| Portée du magnétisme | 25 | m | |
| Impulsion d'attraction | 16 | m/s | atteindre une plateforme à 6 m de haut |
| Impulsion de répulsion | 14 | m/s | hauteur du rocket jump |
| Coût attraction / répulsion | 1 / 1 | charge de mouvement | partagé avec saut en l'air et dash |
| Recharge magnétisme | 0.5 | s | abus et bugs |
| Rayon de l'onde de répulsion / poussée | 4 / 12 | m / m/s | |
| Vitesse d'impact mini contre un mur / dégâts | 6 / 5 | m/s / PV par m/s | |
| Portée de la télékinésie | 15 | m | |
| Masse maximale attrapable | 40 | kg | quels ennemis sont « petits » |
| Vitesse de lancer | 30 | m/s | |
| Coût attraper / lancer | 10 / 15 | pts | |
| Recharge télékinésie | 0.3 | s | |
| Durée d'étourdissement de la goule | 1.5 | s | fenêtre du combo |
| Rayon de collecte des âmes | 6 | m | |

## Interactions

| Fonctionnalité | Lien |
|---|---|
| [Mouvement](../mouvement/design.md) | les impulsions s'ajoutent à la vitesse ; l'état Magnétisme n'annule pas l'élan |
| [Boss traqueur](../boss-traqueur/design.md) | aucun effet sur lui |

## Cas particuliers

- Attraction pendant une glissade : la glissade se termine, la vitesse est conservée.
- Objet tenu qui touche le joueur : pas de collision avec son porteur (pas de « vol sur caisse »).
- Mort ou échange en tenant un objet : l'objet tombe avec sa vitesse actuelle.

## Questions ouvertes

- [ASSUMPTION] L'objet tenu ne bloque pas les projectiles (pas de bouclier).

## History

- 2026-11-11 : designed.
- 2026-11-11 : dégâts directs possibles via les améliorations (D-008).
- 2026-11-12 : le magnétisme consomme les charges de mouvement ; l'endurance ne sert plus qu'à la
  télékinésie (D-015).
