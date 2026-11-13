# Volant

**Status:** in progress · **Updated:** 2026-11-13 · [Implementation](implementation.md) · [Testing](testing.md) · [GDD](../../GDD.md)

Petit ennemi volant de la zone 1 (BRIEF, D-033, D-040), assez léger pour être attrapé et lancé : il harcèle le joueur en
piqué, et devient un projectile qui éclate. Il fait de chaque grande salle une arme, même sans caisses. Bloc 6 de l'étape 6.

## Règles

1. **Poursuite :** il sait toujours où sont les joueurs ; **8 volants en chasse** au plus, les autres attendent (ils planent
   sur place) et prennent le relais (le plus proche) ; un volant en attente se réveille par **l'aggro** des goules (joueur
   vu à 20 m, ou à 8 m sans le voir), jusqu'à **12 en chasse au total** ; réveillé ainsi, il se rendort si le joueur reste à
   plus de 50 m pendant 5 s, sauf si une place se libère.
2. **Ronde :** il tourne au-dessus du joueur, **8 m de haut, 8 m de rayon, 6 m/s** (sous un plafond bas : 1.5 m sous le
   plafond) ; quand un mur le sépare du joueur, il suit le chemin des goules (NavMesh) à hauteur de vol.
3. **Piqué :** recharge prête et joueur en vue à moins de 16 m : **cri et lueur rouge (0.4 s)**, puis il fonce à **14 m/s**
   vers le joueur en **corrigeant sa visée** pendant le piqué (là où est le joueur, sans avance ; virage de **80°/s** au plus) ;
   il abandonne quand le joueur passe derrière lui : un dash tôt est rattrapé, un dash tard l'esquive ; touché : **8 PV et une poussée légère, jamais de ralentissement** ; il remonte en ronde
   (**recharge de 3 s**).
4. **Attrapable à tout moment** (F), sans étourdissement, **comme une caisse de 8 kg** ; tenu, il se débat et **se libère au
   bout de 2 s** (il tremble avant) ; libéré ou posé, il **remonte en ronde avec 3 s de recharge** avant de pouvoir piquer.
5. **Lancé, il éclate** au premier impact : **zone de 3 m, 30 dégâts au centre, le quart au bord** (linéaire), **jamais à
   travers un mur**, même cercle au sol que les objets ; **tir ami appliqué** (au nom du joueur qui l'a lancé) ; en plus des
   dégâts d'impact habituels ; **il meurt**.
6. **Repoussé** (répulsion) : il **tombe au sol, étourdi 1.5 s**, facile à attraper ; s'il percute un mur en tombant, il prend
   les dégâts contre un mur habituels (D-031).
7. **Fragile :** 25 PV, et **il meurt au premier impact de projectile**, quels que soient les dégâts : balle (barillet compris),
   objet lancé ou repoussé, ennemi lancé, dégâts contre un mur (réglage `dieOnProjectileHit`, activé). Pas par une zone (éclatement
   d'un autre volant, zone d'un objet), ni par l'onde de répulsion seule (il tombe étourdi). **Zone de touche sur le modèle** :
   sphère du corps et une boîte par aile (déclencheurs `Hitbox` qui suivent le battement : les balles et les projectiles les
   touchent, la physique ne change pas). Le traqueur l'ignore.
8. **Placement :** **4 par grande salle**, aucun dans les couloirs (réglable : `ArenaLayout_Test.flyersPerHall`).

## Valeurs

`Flyer_Default` (`FlyerSettings`).

| Nom | Valeur | Unité | À observer en playtest |
|---|---|---|---|
| PV / masse | 25 / 8 | PV / kg | |
| Ronde : vitesse, hauteur, rayon ; trajet | 6, 8, 8 ; 9 | m/s, m, m ; m/s | |
| Piqué : portée, annonce, vitesse, durée max, recharge | 16 m, 0.4 s, 14 m/s, 1.5 s, 3 s | | lisible, esquivable |
| Piqué : virage, avance sur le joueur | 80 °/s, 0 | | dash tard : esquive ; course : 32 % touchés |
| Meurt au premier impact de projectile | oui | | |
| Piqué : dégâts, poussée | 8 PV, 3 m/s | | |
| Tenu : libération | 2 | s | assez pour viser et lancer |
| Éclatement : rayon, centre, bord, vitesse minimale | 3 m, 30, ×0.25, 4 m/s | | |
| Étourdi après une répulsion | 1.5 | s | |
| En chasse : plafond, total avec l'aggro | 8, 12 | | |
| Aggro : vue, proximité, rendormi | 20 m, 8 m, 50 m pendant 5 s | | |
| Taille, zone de touche | sphère de 0.4 m (corps 0.6 × 0.45 × 0.8 m) ; ailes 0.9 × 0.5 m (zone 0.9 × 0.1 × 0.55 m), envergure 2.3 m | | |

## Interactions

| Fonctionnalité | Lien |
|---|---|
| [Orbes](../orbes/design.md) | attrapable à tout moment (aide à la visée comprise) ; lancé, il éclate ; repoussé, il tombe étourdi |
| [Goule](../goule/design.md) | même aggro ; l'éclatement blesse les goules autour |
| [Artefacts](../artefacts/design.md) | barillet : une balle le tue |
| [Boss traqueur](../boss-traqueur/design.md) | il l'ignore |

## Questions ouvertes

- Piqué (réglé le 2026-11-13) : virage de 80°/s au lieu de 90 (90 : 38 % de touches en course), sans avance (0.3 : 68 %).
  En traction (ligne droite), il touche 100 % du temps sans esquive, en bunny hop 0 % : à juger au playtest.
- Répulsion contre un mur : tué 3 fois sur 5 dans la mesure (les deux autres fois, pas de dégâts contre le mur relevés ; cause
  non vérifiée, sans doute un impact sous 6 m/s).
- Nombre et répartition par vagues (bloc 7).

## History

- 2026-11-12 : designed (D-033).
- 2026-11-13 : construit (bloc 6, D-040).
- 2026-11-13 : tué au premier impact de projectile, zone de touche des ailes, piqué à visée corrigée (80°/s).
