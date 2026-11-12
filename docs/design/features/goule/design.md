# Goule

**Status:** in progress · **Updated:** 2026-11-12 · [Implementation](implementation.md) · [Testing](testing.md) · [GDD](../../GDD.md)

Ennemi de base de la zone 1 (BRIEF, D-033) : une créature basse qui rampe très vite, bondit sur le joueur, grimpe aux
murs et bondit depuis le mur. Elle oblige à bouger et se tue surtout avec l'environnement : objets lancés, mur après
une répulsion, prise quand elle est étourdie, lustre. Elle sait toujours où sont les joueurs et grimpe aux murs (D-035).

## Règles

1. **Poursuite permanente (D-035) :** plus de vue ni de poste : elle va vers le joueur **le plus proche par le chemin**
   (revu chaque seconde), même s'il est derrière elle, et ne lâche jamais.
2. **Encerclement :** loin du joueur (plus de 10 m), chaque goule vise un point décalé autour de lui (rayon 6 m, angle
   propre à chacune) : elles arrivent de plusieurs côtés au lieu de faire la queue. Près de lui, droit dessus.
   Au sol, 10 m/s (la course à 7.5 m/s ne la sème pas ; le bunny hop ou la traction, si).
3. **Contact :** 15 PV, au plus une fois par seconde.
4. **Bond annoncé :** quand le joueur est en ligne de vue entre 2.5 et 6 m (au même niveau), ou **plus bas qu'elle** (sur
   un mur, une corniche, un pont : jusqu'à 6 m + la moitié de la hauteur à l'horizontale), et que le bond est prêt, elle s'arrête et prend une
   **posture visible 0.3 s** (elle s'accroupit, lueur rouge au sol, sifflement), puis bondit vers l'endroit où il sera
   (0.2 s d'avance), arc de 1 m, au plus 14 m/s. Touché : **15 PV et une légère poussée** (4 m/s), **jamais de
   ralentissement** du joueur (D-012). Recharge de 2.5 s.
5. **Étourdie, tenue, lancée :** comme le factice (D-027, D-030, D-031) : repoussée contre un mur à 6 m/s ou plus, elle
   est étourdie 1.5 s et prend 5 PV par m/s au-delà de 6 (40 au plus) ; étourdie, elle s'attrape (F) ; lancée, elle
   blesse ce qu'elle percute et se blesse elle-même. Elle reprend la chasse après.
6. **Mort :** elle disparaît ; la remise à zéro de la zone la fait revenir.
7. **Murs (D-035, D-037) :** elle **grimpe** (7 m/s, en biais : elle court sur le mur) tous les murs qui mènent quelque
   part (liens de NavMesh : faces des corniches de 11 m tous les 5 m, murs sous les ponts des couloirs, **murs de 16 m** au
   bout des voies au sol des couloirs en pente, qui mènent directement au niveau haut) pour rejoindre corniches et ponts,
   et **redescend d'un saut** (12 m/s). Depuis un mur, une corniche ou un pont, elle **bondit** sur le joueur plus bas :
   même annonce (0.3 s, lueur rouge, sifflement) ; le saut dure environ 1 s : **un dash pendant le vol l'esquive**, un
   dash pendant l'annonce non (elle vise après). Les salles hautes (+16 m) : par les pentes ou par les murs de 16 m.
8. **Plafond de goules en chasse (D-037) :** au plus **25** à la fois (`maxHunting`) ; les autres attendent à leur place ;
   quand une chasseuse meurt, la goule en attente la plus proche d'un joueur par le chemin prend le relais (moins de 0.5 s).
9. **Aggro (D-038) :** une goule en attente qui voit un joueur à moins de **20 m** (ligne de vue) ou qui est à moins de
   **8 m** de lui (même sans le voir) se met en chasse, en plus des 25, jusqu'à **40 en chasse au total**. Réveillée ainsi,
   elle se rendort là où elle est si le joueur reste à plus de **50 m pendant 5 s**, sauf si une place du plafond se libère
   (elle la prend).
10. **Murs grimpables automatiques (D-038) :** les liens d'escalade sont créés au lancement sur tous les murs verticaux qui
   mènent à un sol praticable (jusqu'à 20 m), sans placement à la main.

## Valeurs

`Ghoul_Default` (`GhoulSettings`) ; valeurs de départ validées (D-033).

| Nom | Valeur | Unité | À observer en playtest |
|---|---|---|---|
| PV / masse | 60 / 50 | PV / kg | une caisse de 25 kg lancée (55) + une balle ; 5 balles |
| Vitesse au sol, accélération, rotation | 10, 40, 720 | m/s, m/s², °/s | |
| Choix de la cible | le plus proche par le chemin, chaque seconde | | |
| Encerclement : rayon, droit dessus en dessous de | 6, 10 | m | |
| Murs : montée, descente | 7, 12 | m/s | |
| Goules en chasse au même moment | 25 | | les autres attendent |
| Aggro : vue, proximité, total, rendormie (distance, temps) | 20 m, 8 m, 40, 50 m pendant 5 s | | |
| Murs grimpables automatiques : hauteur min, max, écart | 2, 20, 5 | m | |
| Contact | 15 PV / 1 s, à 0.35 m | | |
| Bond : distance, annonce, vitesse, arc, recharge | 2.5–6 m, 0.3 s, 14 m/s, 1 m, 2.5 s | | lisible et esquivable |
| Bond : dégâts, poussée, avance de visée | 15 PV, 4 m/s, 0.2 s | | |
| Étourdissement, immunité | 1.5 s, 2 s | | |
| Taille | capsule 0.9 × 1.1 m | | passe les portes des pièces (3 m) |

## Interactions

| Fonctionnalité | Lien |
|---|---|
| [Orbes](../orbes/design.md) | répulsion, étourdissement, prise, lancer ; la traction refuse les ennemis |
| [Artefacts](../artefacts/design.md) | 12 dégâts par balle |
| [Boss traqueur](../boss-traqueur/design.md) | même zone ; la goule entre dans les pièces, pas lui |

## Questions ouvertes

- Nombre de goules par vague (bloc 7) et apparition.
- Bond d'en haut : esquivable seulement pendant le vol (un dash pendant l'annonce ne suffit pas) ; à juger au playtest.
- Plafond de 25 goules en chasse : bon chiffre ? (76 présentes dans Test_Arena.) Bloc 7 : nombre qui monte avec le temps.

## History

- 2026-11-12 : au sol (étape 6, bloc 2, D-033).
- 2026-11-12 : poursuite permanente, encerclement, murs et bond d'en haut (D-035).
- 2026-11-12 : murs de 16 m vers le niveau haut, murs grimpables partout, plafond de 25 goules en chasse (D-037).
- 2026-11-12 : aggro (plafond total 40), murs grimpables automatiques (D-038).
