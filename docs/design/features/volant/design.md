# Volant

**Status:** designed · **Updated:** 2026-11-12 · [GDD](../../GDD.md)

Petit ennemi volant de la zone 1 (BRIEF, D-033), assez léger pour être attrapé et lancé : il harcèle le joueur en
piqué, et devient un projectile. Il fait de l'arène une arme même quand il n'y a plus de caisses. Bloc 6 de l'étape 6.

## Règles

1. **Vol :** il tourne au-dessus du joueur, hors de portée de la course, à 6 m/s.
2. **Piqué :** il plonge sur le joueur à 14 m/s ; un piqué qui touche fait 8 PV.
3. **Attrapable à tout moment** (F, sans étourdissement), **comme une caisse de 8 kg** ; tenu, **il se débat et se libère
   au bout de 2 s** (réglable), en clignotant juste avant, comme un ennemi tenu (D-030).
4. **Lancé :** il fait les dégâts d'un objet lancé (masse 8 kg, vitesse d'impact), zone au premier impact.
5. **Fragile :** 25 PV ; 3 balles du barillet.

## Valeurs

`Flyer_Default` (à créer) ; valeurs de départ validées (D-033).

| Nom | Valeur | Unité | À observer en playtest |
|---|---|---|---|
| PV / masse | 25 / 8 | PV / kg | |
| Vitesse en vol / en piqué | 6 / 14 | m/s | |
| Coup en piqué | 8 | PV | |
| Tenu : libération | 2 | s | assez pour viser et lancer |

## Interactions

| Fonctionnalité | Lien |
|---|---|
| [Orbes](../orbes/design.md) | attrapable sans étourdissement ; lancé comme un objet de 8 kg |
| [Artefacts](../artefacts/design.md) | 3 balles |

## Questions ouvertes

- Hauteur de vol, fréquence des piqués, comportement en groupe (bloc 6).
- La répulsion le repousse-t-elle plus loin (léger) ou le fait-elle tomber ?

## History

- 2026-11-12 : designed (D-033).
