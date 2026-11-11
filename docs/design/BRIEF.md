# Brief — roguelite FPS de mouvement à deux orbes

> Document vivant. Claude le complète avec toi au fil du projet.

## L'idée en une phrase

Un roguelite à la première personne où l'on traverse des arènes sombres en enchaînant glissades,
wall jumps et bunny hops, armé de deux orbes magiques (magnétisme et télékinésie), en survivant à
des vagues d'ennemis jusqu'au boss qui ouvre la zone suivante.

## Ce qui est décidé

| Sujet | Décision |
|---|---|
| Moteur | Unity 6.3 LTS, URP, C# |
| Vue | Première personne ; on voit seulement les mains et l'orbe équipé |
| Genre | Roguelite : runs rejouables, améliorations via coffres et marchands |
| Structure | Vagues d'ennemis, puis un boss qui fait changer de zone |
| Niveaux | Pas de couloirs linéaires : arènes et décors faits pour bien se déplacer, et qui récompensent le bon mouvement (référence : Deadlock) |
| Décors | Caves et châteaux, à l'échelle humaine, assez larges pour bouger vite |
| Monnaie | Âmes des monstres tués, collectées automatiquement par les orbes dans un rayon autour d'elles. Sert aux marchands, à ouvrir des salles et des coffres |
| Ambiance | Très sombre, beaucoup de lumières dans les pièces ; ce qui est interactif brille (objets à attraper, lustres, caisses…) |
| Style visuel | Mélange Borderlands (cel-shading, contours) et Arcane (rendu peint, lumière dramatique) |
| Joueurs | Solo d'abord ; écran partagé local à 2 pour les tests ; coop en ligne de 1 à 4 plus tard |

## Mouvement

| Capacité | Statut | Notes |
|---|---|---|
| Course | prévu | |
| Saut (coyote, buffer, hauteur variable) | prévu | |
| Glissade avec élan | prévu | |
| Wall slide avec élan | prévu | |
| Wall jump | prévu | |
| Bunny hop / contrôle en l'air | prévu | enchaîner les sauts sans perdre de vitesse |
| Endurance | prévu | consommée par les orbes, peut-être par la glissade |
| Recharge courte entre deux utilisations | prévu | légère, pour garder le rythme |

## Les deux orbes (un seul actif, échange rapide)

| Orbe | Mode 1 | Mode 2 | Couleurs |
|---|---|---|---|
| Magnétisme | S'attirer vers un mur ou le sol visé | S'en repousser | 2 couleurs, une par mode |
| Télékinésie | Attraper un objet | Lancer l'objet tenu | 2 couleurs, une par mode |

## Questions ouvertes

**Orbes**
1. Échange d'orbe : une touche, la molette, ou les deux ? L'échange a-t-il une recharge ?
2. Attraction : le joueur est-il tiré jusqu'au point (comme un grappin), ou reçoit-il une
   impulsion dans sa direction ? Peut-on viser un ennemi ?
3. Répulsion : sert-elle aussi à se propulser en l'air (comme un rocket jump) ? À repousser des
   ennemis ?
4. Télékinésie : peut-on attraper des ennemis ? Des projectiles ennemis ? Quel poids maximum ?
5. Les orbes font-elles des dégâts directement, ou seulement à travers les objets lancés et
   l'environnement ?
6. Peut-on trouver d'autres orbes ou objets équipables pendant la run, en plus des deux de base ?

**Roguelite**
7. Types d'améliorations : statistiques (+vitesse, +endurance), modifications des orbes (orbe qui
   explose, attraction en chaîne…), objets passifs ?
8. Méta-progression : garde-t-on quelque chose entre deux runs (âmes, déblocages) ou rien ?
9. Les âmes sont-elles perdues à la mort ?
10. Durée visée d'une run, nombre de zones, nombre de salles par zone.
11. Salles : choisies au hasard dans une liste faite à la main, ou générées ?

**Combat**
12. Types d'ennemis de la première zone et du premier boss.
13. Points de vie du joueur, soins, mort : relance immédiate ?

**Coop**
14. En coop : âmes partagées ou individuelles ? Améliorations partagées ? Réanimation ?
15. La difficulté s'adapte-t-elle au nombre de joueurs ?

**Contrôles et plateforme**
16. Clavier-souris et manette ? PC uniquement ?

## Références

- Mouvement et arènes : Deadlock.
- Style : Borderlands, Arcane.
- Autres jeux qui t'inspirent : …
