# Âmes et économie

**Status:** implemented · **Updated:** 2026-11-13 · [Implementation](implementation.md) · [Testing](testing.md) · [GDD](../../GDD.md) · D-043

## Résumé

Les ennemis tués lâchent des éclats d'âme que l'orbe ramasse toute seule autour d'elle. Les âmes paient les coffres (un choix
d'amélioration parmi 3), le marchand (améliorations et soin) et la porte de la salle verrouillée. En coop, les âmes sont gagnées en commun et dépensées individuellement.

## Règles

1. **Éclats :** à la mort d'un ennemi, sa valeur en éclats (goule 3, volant 2) tombe sur place ; **×2 en Enragé** (D-009),
   au moment où l'ennemi meurt. Le traqueur ne donne jamais d'âmes.
2. **Morts sans joueur :** des âmes seulement si un joueur y est pour quelque chose dans les **5 dernières secondes** : coup
   (balle), objet lancé ou repoussé, ennemi lancé ou attrapé, onde de répulsion, éclatement d'un volant lancé, lustre décroché.
   Une goule tombée seule, ou tuée par une autre cause, ne donne rien.
3. **Collecte :** une orbe attire les éclats à moins de **8 m**, à **25 m/s** ; ramassé au contact. Un éclat attiré suit
   cette orbe jusqu'au bout (il retombe si son joueur meurt).
4. **Durée :** un éclat au sol disparaît au bout de **30 s** (perdu) ; il **clignote pendant les 5 dernières secondes**.
5. **Coop (D-007 révisée) : âmes gagnées en commun, dépensées individuellement.** Un éclat est **commun** (visible par tous)
   et **n'importe quelle orbe le ramasse** (la plus proche dans son rayon). Quand une orbe ramasse un éclat, **chaque joueur
   vivant reçoit 1 âme dans sa propre réserve** (un joueur mort à ce moment ne reçoit rien). Chacun dépense sa réserve.
   **Réseau (étape 11) :** création, collecte et gain sur l'autorité.
6. **Interaction :** **E au clavier, Y à la manette** : coffre, marchand, porte de la salle verrouillée, orbe de choix. L'objet
   visé (dans un petit cône, à portée) s'affiche sous le viseur (« E : ouvrir le coffre (30 âmes) »). Quand un objet est visé, Y
   interagit au lieu de recharger le barillet ; sinon, Y recharge.
7. **Refus :** pas assez d'âmes : le compteur et l'aide passent en **rouge** un instant, son de refus ; rien n'est retiré.
8. **Coffre (30 âmes), par joueur :** il s'ouvre **seulement avec E / Y** (s'il y a assez d'âmes ; l'ouverture par un objet
   lancé ou une répulsion a été retirée au playtest). **Chaque joueur peut ouvrir chaque coffre une fois**, en payant son prix,
   avec **son propre choix parmi 3**. Un coffre ouvert par un joueur reste ouvrable pour les autres et **montre à chacun son
   état** : couvercle ouvert et « Coffre / ouvert » pour qui l'a ouvert, couvercle fermé et son prix pour les autres. **3 orbes** flottent au-dessus, chacune avec le nom et l'icône de son amélioration (texte en
   monde, lisible à 10 m), tirées avec la seed de la run. **Sans menu ni pause** : on prend la sienne avec **F** ou en la regardant
   avec **E** ; les deux autres disparaissent. Sans choix, elles restent **30 s**. Les orbes d'un joueur ne sont visibles et
   utilisables que par lui.
9. **Marchand :** placé **au hasard** parmi ses emplacements avec la seed de la run. Ses offres sont des orbes flottantes avec
   leur prix : 3 améliorations tirées avec la seed et un **soin** (+40 PV pour 35 âmes) ; achat avec E / Y ou F ; une offre
   achetée disparaît. **Étape 8 :** un marchand en plus dans la zone de départ sûre des zones 2 et 3.
10. **Hausse du prix à chaque achat :** un réglage en % par catégorie (coffres, marchand, portes), compté **par joueur** et
    composé (30 âmes à +20 % : 30, 36, 43…) ; un refus ou un coffre gratuit ne compte pas. **0 % pour l'instant** (prix fixes).
11. **Salle verrouillée (50 âmes) :** une porte payante (E / Y), fenêtre à barreaux ; derrière, un coffre gratuit.
12. **Remise à zéro de la zone** (test) : éclats effacés, coffres refermés, porte reverrouillée, marchand replacé ; les âmes et les
    améliorations des joueurs restent (la vraie mort, qui termine la run, vient à l'étape 8).

## Valeurs

`Economy_Default` (`EconomySettings`), validées le 2026-11-13.

| Nom | Valeur | Unité | À observer en playtest |
|---|---|---|---|
| Âmes : goule, volant, traqueur | 3, 2, 0 | | |
| Rayon de collecte ; aspiration (n'importe quelle orbe) | 8 ; 25 | m ; m/s | ramasser sans détour |
| Durée au sol ; clignotement | 30 ; 5 | s | |
| Enragé | ×2 | | |
| Mort sans joueur : délai | 5 | s | |
| Coffre ; salle verrouillée | 30 ; 50 | âmes | |
| Marchand : améliorations ; soin | 40 à 80 ; 35 (+40 PV) | âmes | |
| Orbes de choix : nombre, durée | 3, 30 s | | |
| Interaction : portée (coffre, porte) ; orbes ; tolérance | 4 ; 8 ; 12° | m ; m ; | |
| Hausse du prix à chaque achat : coffres, marchand, portes | 0, 0, 0 | % | |

## Interactions

| Fonctionnalité | Lien |
|---|---|
| [Améliorations](../ameliorations/design.md) | ce que les coffres et le marchand proposent |
| [Vagues](../vagues/design.md) | ennemis à tuer ; ×2 en Enragé |
| [Orbes](../orbes/design.md) | collecte autour de l'orbe |
| [Artefacts](../artefacts/design.md) | Y partagé avec la recharge du barillet (selon le contexte) |

## Questions ouvertes

- Une part des âmes en méta-progression à la mort (étape 8).
- Prix selon la zone (zones 2 à 4).

## History

- 2026-11-13 : designed et construit (étape 7, D-043).
- 2026-11-13 : playtest validé ; coffre seulement avec E / Y ; hausse du prix à chaque achat (0 %).
- 2026-11-13 : âmes gagnées en commun, dépensées individuellement (D-007 révisée) ; coffres par joueur.

- 2026-11-13 : le marchand placé au hasard est retiré ; marchand de l'artefact dans la salle de départ (D-050, [run et niveaux](../run-et-niveaux/design.md)).
