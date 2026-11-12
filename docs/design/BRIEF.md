# Brief — roguelite FPS de mouvement à deux orbes

> Document vivant. Claude le complète avec toi au fil du projet. Mis à jour le 2026-11-11 avec
> les réponses aux questions ouvertes. Le GDD qui en découle : [GDD.md](GDD.md).

## L'idée en une phrase

Un roguelite à la première personne où l'on farme des zones sombres en enchaînant glissades,
wall jumps et bunny hops, armé de deux orbes magiques (magnétisme et télékinésie), traqué par un
monstre géant et intuable qu'on ne peut que fuir, jusqu'à la zone finale où on peut enfin le tuer.

## Ce qui est décidé

| Sujet | Décision |
|---|---|
| Moteur | Unity 6.3 LTS, URP, C# |
| Vue | Première personne ; on voit seulement les mains et l'orbe équipé |
| Genre | Roguelite : runs rejouables, améliorations via coffres et marchands |
| Structure | 4 zones : **cave du château**, **couloirs et salles du château**, **forêt face au château**, puis une **grande zone finale** (combat contre le boss). Dans chacune des 3 premières zones, environ **20 min de farm** avec un timer. On peut sortir plus tôt (moins de temps pour farmer). Une fois le timer écoulé, le boss devient enragé en continu : sortir vivant devient très dur |
| Zones | Arènes faites main, tirées au hasard (avec une seed) et reliées par des couloirs et des portes ; exploration libre ; vagues dans les arènes ; coffres, marchands et salles verrouillées dispersés ; sortie visible |
| Boss | **Un seul et même monstre** toute la run, de la taille des couloirs et des salles, qui passe difficilement les portes. **Il tue en un coup**, il est **intuable** et rien ne le ralentit dans les 3 premières zones. On ne peut pas le traverser : il faut le fuir. Il **rôde en permanence**, peut aller partout, et contourne les petites portes par un autre accès. Il est **plus rapide que la course de base** : seul le bon mouvement (glissade, wall jump, bunny hop, élan, orbes) lui échappe. Ambiance sonore de menace. Il n'est tuable que dans la zone finale |
| Niveaux | Pas de couloirs linéaires : arènes et décors faits pour bien se déplacer, et qui récompensent le bon mouvement (référence : Deadlock) |
| Décors | Caves, château, forêt, à l'échelle humaine, assez larges pour bouger vite |
| Monnaie | Âmes des monstres tués, collectées automatiquement par les orbes dans un rayon autour d'elles. Sert aux marchands, à ouvrir des salles et des coffres |
| Ambiance | Très sombre, beaucoup de lumières dans les pièces ; ce qui est interactif brille (objets à attraper, lustres, caisses…) |
| Style visuel | Mélange Borderlands (cel-shading, contours) et Arcane (rendu peint, lumière dramatique) |
| Joueurs | Solo d'abord ; écran partagé local à 2 pour les tests ; coop en ligne de 1 à 4 plus tard |
| Contrôles | PC, clavier-souris et manette dès le départ |

## Mouvement

| Capacité | Statut | Notes |
|---|---|---|
| Course | prévu | |
| Saut (coyote, buffer, hauteur variable) | prévu | |
| Glissade avec élan | prévu | **en pente descendante, on prend de la vitesse** ; ne consomme pas d'endurance |
| Wall slide avec élan | prévu | |
| Wall jump | prévu | |
| Bunny hop / contrôle en l'air | prévu | enchaîner les sauts sans perdre de vitesse |
| Endurance | prévu | consommée par les orbes seulement |
| Recharge courte entre deux utilisations | prévu | légère, pour garder le rythme |

## Les deux orbes (un seul actif, échange instantané)

| Orbe | Mode 1 | Mode 2 | Couleurs |
|---|---|---|---|
| Magnétisme | Se tracter en ligne droite vers n'importe quelle surface visée (bouton maintenu, D-021) | S'en repousser (propulsion) ; l'onde bouscule les ennemis, qui prennent des dégâts s'ils percutent un mur | 2 couleurs, une par mode |
| Télékinésie | Attraper un objet, un petit ennemi ou un projectile | Lancer ce qu'on tient | 2 couleurs, une par mode |

## Réponses aux questions (2026-11-11)

**Orbes**
1. Échange : une touche (molette au clavier-souris, un bouton à la manette), **sans recharge**.
2. Attraction : **impulsion unique** vers le point visé, puis la physique normale reprend.
   **Surfaces seulement**, pas d'ennemis.
3. Répulsion : **propulsion** (rocket jump) **+ onde qui bouscule** les ennemis près du point
   visé, **+ dégâts** si l'ennemi percute un mur. Sans effet sur le boss.
4. Télékinésie : **objets et petits ennemis** (limite de poids réglable), pas les gros ni le
   boss. **Projectiles ennemis attrapables et renvoyables.**
5. Les orbes ne font **pas de dégâts directs** de base : seulement via les objets lancés, les
   ennemis projetés et le décor. Des améliorations peuvent en ajouter (voir les précisions
   plus bas).
6. **Pas d'autres orbes** : les deux orbes de base évoluent grâce aux améliorations.

**Roguelite**
7. Améliorations : **modifications des orbes, statistiques, modifications du mouvement, reliques
   passives** ; d'autres types possibles plus tard.
8. Méta-progression : **déblocages seulement** (nouvelles améliorations possibles, variantes,
   salles), aucun bonus chiffré permanent.
9. Âmes : **perdues à la mort**, mais une part est convertie en monnaie de déblocage.
10. Durée : environ 20 min par zone × 3, puis le combat final. On peut dépasser le timer (boss
    enragé) ou partir plus tôt.
11. Salles : **arènes faites main, tirées au hasard** et reliées entre elles (exploration libre).

**Combat**
12. Ennemis de la zone 1 :
    - une **goule** qui rampe très vite, bondit sur le joueur, grimpe aux murs et bondit depuis
      le mur. Elle est attrapable seulement **quand elle est étourdie** (touchée par un objet ou
      projetée contre un mur) ;
    - un **petit volant**, assez léger pour être attrapé et lancé.

    D'autres ennemis viendront plus tard. Le boss : voir « Ce qui est décidé ».
13. **PV fixes, soins rares** (fontaines, marchand, reliques). La mort termine la run : écran de
    bilan, puis relance en un bouton.

**Coop**
14. **Âmes et améliorations individuelles.** Un joueur tué peut être réanimé. Si personne ne le
    relève, il réapparaît à la zone suivante, à condition que l'équipe la passe.
15. Difficulté : **plus d'ennemis** avec plus de joueurs, **boss inchangé**.

**Contrôles et plateforme**
16. **PC, clavier-souris + manette** dès le départ.

## Précisions validées avec le GDD (2026-11-11)

1. **Dégâts :** de base, les orbes ne font aucun dégât direct. On tue les monstres, et le boss
   dans la zone finale, avec l'environnement : objets lancés, lustres, piliers, pièges, points
   faibles accessibles au magnétisme. Les **améliorations** trouvées pendant la run (coffres,
   marchands) peuvent ajouter des dégâts directs aux orbes ou aux objets lancés ; ces dégâts
   s'ajoutent à ceux de l'environnement. Le traqueur des zones 1 à 3 reste intuable.
2. **Rester après le timer :** pendant la phase enragée, un **multiplicateur d'âmes** s'applique.
3. **Coop :** le boss chasse le dernier joueur vu ou entendu (pour l'instant).
4. **La glissade ne consomme pas d'endurance.**
5. **Durée de zone réglable**, avec une valeur de test de **4 min** (cible : 20 min).
6. **Premier prototype de zone :** une seule zone, une goule et le traqueur.

## Références

- **Deadlock** : mouvement et arènes.
- **Titanfall 2** : wall run, conservation de l'élan.
- **Ultrakill** : mouvement rapide, style.
- **Dishonored** : pouvoirs de mobilité, usage de l'environnement.
- **Hades** : structure roguelite, améliorations.
- **Lethal Company** : traque, tension, sortir à temps.
- **Borderlands** et **Arcane** : style visuel.
