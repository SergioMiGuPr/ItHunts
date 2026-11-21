# Fin du travail de nuit (piste DA, 2026-11-21)

Signal pour la session `main` : **toutes les PR de la nuit sont ouvertes.** Le lot G (traqueur v4) suit, mais il n'a
volontairement **ni commit ni PR** (l'utilisateur valide d'abord la planche) : rien d'autre n'arrivera de la piste DA cette nuit.

| Lot | PR | Branche | État (au moment d'écrire ce fichier) |
|---|---|---|---|
| A. Corrections des menus | [#19](https://github.com/SergioMiGuPr/ItHunts/pull/19) | `art-menus-2` | fusionnée |
| B. Audio du jeu (20 sons) | [#20](https://github.com/SergioMiGuPr/ItHunts/pull/20) | `art-audio` | fusionnée |
| C. Interface en jeu | [#21](https://github.com/SergioMiGuPr/ItHunts/pull/21) | `art-ui-jeu` | fusionnée |
| D. Effets visuels peints | [#22](https://github.com/SergioMiGuPr/ItHunts/pull/22) | `art-vfx` | fusionnée |
| E. Armes du koala et du hibou | [#23](https://github.com/SergioMiGuPr/ItHunts/pull/23) | `art-armes` | fusionnée (12 crédits Higgsfield dépensés sur 60) |
| F. Eau toon | [#24](https://github.com/SergioMiGuPr/ItHunts/pull/24) | `art-eau` | fusionnée |
| H. Kit des nouveaux modules | [#25](https://github.com/SergioMiGuPr/ItHunts/pull/25) | `art-kit-modules` | ouverte |
| I. Outils et fin | celle-ci | `art-outils` | ouverte |
| G. Traqueur v4 | aucune (consigne) | `art-traqueur-v4`, sans commit | en cours après ce fichier ; planche dans `docs/art/rendus/Traqueur/v4/` |

Aucun lot bloqué. À intégrer côté `main` (voir chaque PR) : brancher les sons (ART-HANDOFF, « Sons du jeu »), l'interface en jeu
(UI-HANDOFF § 7), les effets (`docs/art/VFX-HANDOFF.md`), les armes (INTEGRATION-PROPS), l'eau (`docs/art/EAU.md`) et le kit des
modules (INTEGRATION-PROPS, colliders à ajouter).
