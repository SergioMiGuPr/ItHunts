# Recherche : faire de bonnes cartes de poursuite et de mouvement

Objet : règles concrètes pour dessiner à la main les cartes des trois actes (caves, château gothique, forêt et
jardin entre falaises), avec un traqueur intuable de 14 m qui poursuit en permanence, des goules en vagues
cachées, des coffres sur ancres et un marchand sur 2-3 emplacements. Les chiffres viennent des sources en fin de
document ; quand ils sont **déduits** (calcul à partir de nos réglages), c'est indiqué.

Repères chiffrés du jeu utilisés partout (voir `features/boss-traqueur/design.md`) : course du joueur 7.5 m/s,
bunny hop 18 à 26 m/s, traction plafonnée à 18 m/s ; traqueur 9 m/s en Chasse, 11 en Enragé (jusqu'à 18 par
paliers), pointe à 16 m/s pendant 3 s quand il voit le joueur ; il se baisse sous 9 m (×0.6) et ne passe pas les
portes des pièces (2 à 4 m).

---

## 1. Poursuite et évasion avec un poursuivant invincible

Ce qu'en disent les meilleurs jeux. Dans **Dead by Daylight**, le tueur va un peu plus vite que le survivant
(4.6 m/s contre 4.0, soit 115 %) : en ligne droite, le survivant perd toujours ; il survit grâce aux **tuiles**
(murs en L et en T avec fenêtre, « jungle gyms » avec une fenêtre et une palette sur des côtés opposés), qui
forcent le tueur à faire le tour pendant que le survivant coupe. Une poursuite démarre à 12 m et se termine au-delà
de 18 m. La force d'une boucle vient surtout de la **fenêtre** (le raccourci réservé au fuyard), plus que de la
palette ; une boucle trop forte devient « infinie », et les patchs ont dû espacer ou affaiblir des tuiles.
**Alien: Isolation** sépare un « directeur » qui sait toujours où est le joueur d'un alien qui doit le trouver
lui-même ; une jauge de menace monte avec la proximité et la ligne de vue, et quand elle sature, le directeur
renvoie l'alien dans les conduits pour laisser respirer. Le niveau est fait de boucles et de conduits que l'alien
utilise aussi. **Resident Evil 2** (Mr. X) mise sur le son (pas lourds, portes claquées) et sur la maîtrise du
plan : connaître les boucles du commissariat est la récompense. **Pac-Man** donne le modèle de fond : couloirs
en boucle, tunnels latéraux, et des phases « dispersion » de quelques secondes où les fantômes lâchent la
poursuite, de plus en plus courtes avec la difficulté.

Règles :

1. **Le poursuivant est plus rapide en ligne droite, le joueur gagne par la géométrie.** Rapport de vitesse visé :
   1.1 à 1.2 (DbD : 1.15 ; ici 9 / 7.5 = 1.2 en course, mais le bunny hop et la traction inversent le rapport). Une
   ligne droite sans obstacle de plus de **40 m** (déduit : à 1.5 m/s d'écart, un avantage de 15 m fond en 10 s
   s'il court sans enchaîner) est une ligne de mort pour un joueur qui ne maîtrise pas le bunny hop.
2. **Chaque boucle doit donner un raccourci au joueur seulement.** Le joueur passe par une porte de 2 à 4 m, un
   passage bas (< 9 m), une fenêtre, un saut ou une traction ; le traqueur doit faire le tour. Viser un détour du
   traqueur **au moins 1.5 fois** plus long que le chemin du joueur pour compenser son écart de vitesse.
3. **Longueur de boucle : 25 à 60 m de périmètre** (déduit, équivalent des tuiles de DbD à notre échelle et à notre
   vitesse) : assez court pour que le joueur garde le traqueur en vue ou l'entende, assez long pour qu'il ne
   revienne pas sur lui. Prévoir dans chaque carte une ou deux « grandes boucles » de 100 m et plus qui relient
   plusieurs salles.
4. **Les boucles s'usent.** Comme les palettes de DbD qui se cassent : une boucle ne doit pas tenir indéfiniment.
   Ici, l'usure vient de l'Enragé et de ses paliers, des goules qui bloquent, ou d'un raccourci à usage unique
   (caisse qui bouche, passerelle qui tombe). Chaque carte : **≥ 1 raccourci consommable par zone**.
5. **Chaînage de tuiles : 8 à 15 m entre deux refuges.** Le joueur qui lâche une boucle doit voir la suivante.
   DbD a dû espacer les tuiles quand elles s'enchaînaient trop ; à l'inverse, plus de 25 m de vide sans rien = mort
   certaine.
6. **Des respirations.** Alien: Isolation retire le monstre quand la menace sature ; Pac-Man alterne poursuite et
   dispersion. Ici le traqueur ne lâche pas, mais la carte peut offrir des **zones de répit** où il est ralenti
   (plafond bas, ×0.6) : **1 zone de répit tous les 60 à 90 s de course**, jamais une salle sûre totale.
7. **Annoncer le poursuivant au son et à la lumière** (Mr. X) : le joueur doit savoir de quel côté il arrive
   avant de le voir. Portes qui tremblent, pas lourds, lumière qui vacille à son passage.

**Application ici :** dessiner d'abord chaque carte comme un graphe de boucles (cercles) avant les salles ; marquer
en rouge les passages trop petits pour le traqueur (portes, plafonds < 9 m) et vérifier que chaque boucle en
contient au moins un. Le chronomètre et l'Enragé tiennent le rôle de l'usure.

---

## 2. Mouvement et verticalité

Ce qu'en disent les meilleurs jeux. **Titanfall 2** a appris aux joueurs à faire confiance à la course murale par
des passages imposés tôt dans la campagne, et certaines cartes sont construites pour enchaîner les courses
murales. **Mirror's Edge** colore en rouge les objets utiles (la « vision du coureur ») sur un monde désaturé ;
la critique note que le rouge montre le chemin le plus direct, pas toujours le plus rapide, et que tout arrêt pour
chercher la suite casse le flux. **Dishonored** offre plusieurs entrées et sorties à chaque lieu clé et oppose
haut et bas ; Arkane parle de « guider et attirer » plutôt que d'imposer. **Doom Eternal** fait des arènes des
temps forts séparés par des passages de traversée (escalade, barres de suspension, dash). **Deadlock** met des
tyroliennes au-dessus de chaque voie et des toits accessibles par cordes et bouches d'air : la hauteur donne des
routes, mais coûte du temps pour rejoindre une autre voie.

Règles :

1. **Mesurer d'abord le contrôleur, puis la carte.** Noter les vraies valeurs : saut maximal, distance d'un saut
   en course, d'un wall jump, d'une traction, d'un bunny hop. Construire une salle étalon (« gym ») avec ces
   distances avant toute carte. Écarts « confortables » = **70 à 80 % du maximum** ; écarts « experts » = 90 à 100 %.
2. **Rythme des points d'accroche : un point d'appui toutes les 1 à 2 secondes de déplacement** (déduit de
   l'enchaînement des courses murales de Titanfall 2 et du rythme d'Ultrakill) : à 18 m/s, une surface magnétique
   ou un mur de wall jump tous les **15 à 30 m** sur une route haute.
3. **La route haute doit être plus rapide ou plus sûre, jamais seulement plus jolie.** Elle permet d'éviter les
   goules, de couper une boucle ou d'atteindre un coffre. En échange, elle coûte de la maîtrise ou une charge. Le
   traqueur qui grimpe et bondit doit mettre **plus de temps** à y monter que le joueur (au moins 2 s de montée).
4. **Toujours une descente rapide.** Chaque route haute a une sortie vers le bas **tous les 30 à 50 m** (chute sans
   dégâts, glissade, rampe) : on ne coince pas le joueur en hauteur.
5. **Golden path lisible, raccourcis découverts.** Une route principale large (3 m et plus) et éclairée ; les
   variantes rapides demandent une capacité (traction, wall jump). Le joueur doit **voir** le raccourci depuis la
   route principale au moins une fois (Dishonored, Bloodborne).
6. **Une couleur pour le mouvement.** Comme le rouge de Mirror's Edge : toutes les surfaces magnétiques et murs
   utiles brillent d'une même teinte, partout dans le jeu, contre un décor désaturé.
7. **Enseigner avant d'exiger.** Titanfall 2 force la course murale tôt : l'acte I doit contenir un passage
   obligatoire pour chaque capacité (traction, wall jump, glissade sous un obstacle) avant de la demander sous la
   pression du traqueur.

**Application ici :** créer `Gym_Mesures` (scène d'étalonnage, hors cartes) avec écarts de 4, 6, 8, 10, 12 m,
hauteurs de 2, 3, 5 m, murs de wall jump et surfaces de traction à distance maximale. Chaque route haute de carte
reprend ces écarts.

---

## 3. Structure : boucles, raccourcis, hubs, repères, blockout

Ce qu'en disent les meilleurs jeux. **Bloodborne** et **Dark Souls** relient leurs zones par des raccourcis qui
reviennent sur un point connu (une grille qui se rouvre près de la clinique) et laissent voir tôt les lieux qu'on
atteindra plus tard : le joueur apprend par les repères, sans carte. **Dead Cells** pose d'abord l'entrée et la
sortie, puis les salles spéciales (trésor, marchand), puis remplit avec les salles de combat. **Valve** donne des
mesures de départ pour le blockout (porte standard 48 × 108 unités, couloir 64 × 128, environ 1.2 × 2.7 m et
1.6 × 3.3 m), à grossir pour un jeu rapide. Les guides de level design (Unity, Game Developer) insistent sur
la lumière comme guide, les lignes directrices (rampes, tuyaux, bordures éclairées) et un « objet héros » par
espace.

Règles :

1. **Zéro impasse.** Chaque salle a **≥ 2 sorties** ; les salles de plus de 400 m² en ont **≥ 3**, dont au moins une
   en hauteur. Une alcôve à coffre est permise si elle fait moins de 6 m de profondeur (le joueur ressort avant que
   le traqueur ferme).
2. **Un hub par carte**, visible depuis plusieurs routes, où passent au moins 3 boucles. C'est le meilleur
   emplacement du marchand ou de la porte de sortie.
3. **Repères : 1 grand repère visible de presque partout** (tour, arbre géant, cristal, vitrail) **et 1 repère local
   par salle** (couleur de lumière, objet unique). Dans le noir, un repère est une **lumière**.
4. **Lignes de vue : 20 à 40 m** dans les salles de poursuite, pour voir le traqueur arriver sans voir toute la
   carte. Couper les lignes de plus de 60 m (60 m est sa portée de détection).
5. **Échelle pour la vitesse.** Couloirs de course **4 m de large minimum** (glissade et bunny hop), salles de
   poursuite **≥ 20 × 20 m**, plafonds **≥ 6 m** quand le mouvement vertical est attendu ; les passages de 2 à 4 m
   servent à semer le traqueur, pas à se déplacer.
6. **Blockout d'abord, art ensuite.** Formes grises, une couleur par fonction (boucle, refuge, route haute, ancre de
   coffre), et un passage chronométré : temps de l'entrée à la sortie par le golden path, et par la route experte.
7. **Lumière = chemin.** Lumière chaude sur la route principale et la sortie, froide ou nulle dans les culs de
   salle ; un point lumineux fort visible depuis chaque entrée de salle.

**Application ici :** chaque carte se dessine d'abord en graphe (hub, boucles, sortie, ancres) dans
`docs/design/level/`, puis en blockout avec le kit de modules de la DA ; on mesure les temps de traversée au bot de
playtest.

---

## 4. Composition « image de magasin » dans le noir

Ce qu'en disent les meilleurs jeux. Bloodborne doit beaucoup de son atmosphère à l'échelle écrasante de Yharnam :
silhouettes de flèches gothiques en contre-jour sur un ciel plus clair, brume, lanternes rares. Dark Souls et
Hollow Knight procèdent par plans : premier plan sombre, plan de jeu éclairé, arrière-plan brumeux clair. Arcane
construit ses images sur un contraste chaud / froid et des sources colorées dans le cadre. Darkest Dungeon
limite la lumière à un cercle de torche. Point commun : **l'œil va vers le point le plus clair et le plus
saturé**, et un guide d'éclairage pour les niveaux conseille d'aller aux extrêmes (grand, clair, coloré) pour
qu'un point focal ressorte.

Règles :

1. **Un point focal par vue**, le plus lumineux de l'image, qui coïncide avec un objectif (sortie, coffre, route
   haute). Pas deux sources de même force dans le même cadre.
2. **Trois plans de valeur** : premier plan presque noir (cadre, arche, branche), plan de jeu à valeur moyenne,
   fond **plus clair que le premier plan** (brume éclairée, ciel, fenêtre). Le joueur et le traqueur se lisent en
   silhouette sur le fond.
3. **Contre-jour pour le traqueur.** Placer les grandes lumières derrière les zones où il arrive : sa silhouette de
   14 m découpée sur la brume est l'image de magasin du jeu.
4. **Chaud / froid** : lumière chaude (orange, bougies, feu) = sûr, chemin, marchand ; froide (bleu, vert pâle) =
   danger, profondeur. Respecter le code des couleurs de l'orbe (bleu, orange, violet, vert) pour ne pas les noyer.
5. **Brume en couches**, densité croissante avec la distance, pour séparer les plans et cacher la fin des grandes
   salles (et la limite de la carte).
6. **Lignes directrices** : rampes, rangées de colonnes, bords éclairés et racines conduisent l'œil vers le point
   focal ; les lignes verticales (colonnes, falaises) disent « en haut, il y a une route ».

**Application ici :** pour chaque carte, choisir 3 « vues de magasin » (une par zone) dès le blockout : position de
caméra, point focal, contre-jour. La DA les éclaire en priorité ; elles servent aux captures Steam.

---

## 5. Cartes faites à la main qui changent à chaque run

Ce qu'en disent les meilleurs jeux. **Hades** a essayé de randomiser obstacles et pièges dans les salles et a
trouvé qu'il coûtait plus cher de rendre une salle modulaire que d'en faire une autre : la variété vient surtout
des rencontres ; les salles se retournent pour que l'entrée corresponde à la sortie précédente, et les petites
salles viennent en début de biome, les grandes plus tard. **Dead Cells** assemble des tuiles faites à la main
selon un graphe par biome (entrée, sortie, salles spéciales, puis remplissage), le nombre d'ennemis dépendant de
la longueur des tuiles de combat. **Risk of Rain 2** a des variantes de cartes tirées à l'entrée et un budget de
« crédits » pour placer coffres et autels, mis à l'échelle du nombre de joueurs (×1 / ×1.5 / ×2 / ×2.5 de 1 à
4). **Hunt: Showdown** a des points d'intérêt fixes, répartis à peu près régulièrement, et tire au hasard le
repaire du boss et les indices parmi des emplacements possibles.

Règles :

1. **Variété par le contenu, pas par la géométrie** (leçon de Hades) : la géométrie est fixe et soignée ; ce qui
   varie, ce sont les ennemis, les coffres, le marchand, l'état de quelques passages.
2. **Ancres en surnombre : 2 à 3 fois plus d'ancres que d'objets tirés.** Par exemple 12 ancres de coffre pour 4 à 6
   coffres, 3 emplacements de marchand pour 1.
3. **Budget de crédits** (Risk of Rain 2) : chaque carte a un budget d'interactifs et un budget d'ennemis, mis à
   l'échelle du nombre de joueurs ; les objets chers (coffre doré) coûtent plus de crédits.
4. **Règles d'espacement au tirage** (Hunt) : deux coffres à **≥ 25 m** l'un de l'autre, le marchand à **≥ 40 m** de
   l'entrée, aucun coffre à moins de 15 m du point d'apparition du traqueur.
5. **Variantes d'état** : 2 ou 3 passages par carte qui peuvent être ouverts ou fermés selon la graine (grille,
   éboulis, pont), pour que la meilleure boucle ne soit pas toujours la même. Toujours vérifier que chaque
   variante garde zéro impasse.
6. **2 à 3 cartes par acte, plus des variantes** : avec 3 cartes et 2 variantes d'état, 6 configurations par acte.
   Tirer avec une graine pour pouvoir rejouer une run (et la donner au bot de test).

**Application ici :** le système d'ancres existant (coffres, marchand) suit ces règles ; ajouter un validateur
d'éditeur qui tire 100 graines par carte et vérifie espacements, budgets et accessibilité.

---

## 6. Navigation d'un grand poursuivant et des goules qui grimpent

Ce qu'on sait. Dans Unity (AI Navigation), un **NavMeshLink** relie deux morceaux de NavMesh du même type d'agent ;
il a une largeur, un sens (bidirectionnel ou non), un coût et un type de zone (les liens générés sont de type
« Jump »). Les rapports de bugs montrent les pièges : agents qui tentent de passer dans un trou trop étroit
quand le rayon de cuisson ne correspond pas au rayon de l'agent ; liens larges qui font osciller l'agent ; liens
qui se déclenchent à 2.25 fois le rayon de l'agent (le saut part de trop loin) ; et avec la traversée
automatique coupée, c'est au code de mener l'agent au bout du lien et de signaler la fin. Alien: Isolation montre
une autre voie : le monstre a ses propres routes (conduits) et un directeur qui le renvoie vers le joueur.

Règles :

1. **Un type d'agent par taille** : traqueur (rayon de 2 à 3 m, hauteur 14 m) et goule (0.5 m), chacun avec son
   NavMesh cuit à son rayon. Les portes de 2 à 4 m n'existent alors pas pour le traqueur : c'est voulu.
2. **Liens de saut et d'escalade posés à la main**, largeur 0 ou faible (≤ 2 m), à sens unique quand c'est une
   chute ; traversée par le code (arc de saut, escalade animée), avec une durée connue (1 à 2 s) qui donne au
   joueur son avance.
3. **Couloirs du traqueur ≥ 2 fois son rayon + 2 m** : entre arbres, colonnes et falaises, laisser des allées d'au
   moins 6 à 8 m, ou marquer franchement les zones qu'il contourne. Rien entre les deux : c'est là qu'il se coince.
4. **Filet anti-blocage** : s'il n'a pas progressé de 2 m en 2 s, il saute vers le point de NavMesh le plus proche
   du joueur ou passe par un lien de secours ; en dernier recours, il disparaît hors champ et réapparaît
   derrière (2 téléportations seulement dans tout Alien: Isolation : à garder rare et invisible).
5. **Le saut du traqueur est un choix de design** : chaque lien de saut ouvre une route de poursuite. En poser
   **moins** sur les routes hautes du joueur que sur le sol, pour que la hauteur reste un refuge temporaire.
6. **Goules : murs escaladables marqués** (un type de zone « Climb » au coût élevé) pour qu'elles prennent le sol
   en priorité et grimpent seulement quand c'est plus court ; elles ne doivent pas suivre partout le joueur en
   hauteur.
7. **Tester avec un bot** : faire parcourir au traqueur un tour complet de chaque boucle et toutes les paires de
   salles ; un chemin partiel (`PathPartial`) est une erreur de carte.

**Application ici :** ajouter au bot de playtest un test « traqueur sur toutes les paires d'ancres », et un rapport
des liens de saut par carte (nombre, longueur, sens).

---

## Liste de contrôle pour juger une carte

1. Chaque salle a ≥ 2 sorties ; les salles de plus de 400 m² en ont ≥ 3, dont une en hauteur.
2. Aucune impasse de plus de 6 m de profondeur, dans toutes les variantes d'état.
3. Chaque boucle mesure 25 à 60 m de périmètre et contient au moins un passage interdit au traqueur (porte ≤ 4 m ou
   plafond < 9 m).
4. Le détour du traqueur sur chaque boucle est ≥ 1.5 fois le chemin du joueur.
5. Aucune ligne droite dégagée de plus de 40 m sans refuge ou obstacle.
6. Un refuge (boucle, route haute, passage bas) tous les 8 à 15 m sur les routes de fuite ; jamais plus de 25 m de
   vide.
7. Au moins une grande boucle de plus de 100 m qui relie plusieurs salles.
8. Une zone de répit (ralentissement du traqueur) tous les 60 à 90 s de course.
9. Au moins un raccourci consommable par zone.
10. Chaque route haute a une descente rapide tous les 30 à 50 m.
11. Points d'accroche toutes les 1 à 2 s de déplacement sur les routes hautes (15 à 30 m à 18 m/s).
12. Écarts de saut et de traction tirés de la salle d'étalonnage (70 à 80 % du maximum en route normale).
13. Le traqueur met au moins 2 s de plus que le joueur pour atteindre une route haute.
14. Un hub visible depuis plusieurs routes, traversé par ≥ 3 boucles.
15. Un grand repère visible depuis la plupart des salles et un repère lumineux propre à chaque salle.
16. Lignes de vue de 20 à 40 m dans les salles de poursuite, aucune au-delà de 60 m.
17. Couloirs de course ≥ 4 m de large, plafonds ≥ 6 m là où l'on attend du mouvement vertical.
18. Allées du traqueur ≥ 6 à 8 m entre arbres, colonnes et rochers ; aucune zone « entre deux » où il se coince.
19. Tous les liens de saut et d'escalade sont posés à la main et listés ; aucun chemin partiel du traqueur entre
    deux ancres (test au bot).
20. 2 à 3 fois plus d'ancres que d'objets tirés ; espacement des coffres ≥ 25 m, marchand ≥ 40 m de l'entrée.
21. Un point focal lumineux visible depuis chaque entrée de salle ; la sortie et la route principale en lumière
    chaude.
22. Trois vues « de magasin » choisies, avec contre-jour sur l'arrivée du traqueur et brume en couches.
23. Chaque capacité exigée sous la pression a été enseignée sans danger plus tôt dans l'acte.
24. Temps de traversée mesurés (golden path et route experte) et notés dans le document de la carte.

---

## Sources

- Dead by Daylight, tuiles de labyrinthe : https://deadbydaylight.wiki.gg/wiki/Maze_Tiles
- Dead by Daylight, vitesses et règles de poursuite : https://deadbydaylight.wiki.gg/wiki/Movement_Speed
- Dead by Daylight, boucles (guide) : https://progameguides.com/dead-by-daylight/how-to-loop-all-map-tiles-in-dead-by-daylight/
- Dead by Daylight, débat sur l'espacement des palettes : https://forums.bhvr.com/dead-by-daylight/discussion/comment/4009774
- Alien: Isolation, IA et directeur : https://www.gamedeveloper.com/design/the-perfect-organism-the-ai-of-alien-isolation
- Alien: Isolation, retour sur l'IA : https://www.gamedeveloper.com/design/revisiting-the-ai-of-alien-isolation
- Building Fear in Alien: Isolation (GDC 2015) : https://gdcvault.com/play/1021852/Building-Fear-in-Alien
- Resident Evil 2, Mr. X : https://medium.com/super-jump/the-horrific-appeal-of-mr-x-af88c622faed
- Resident Evil 2, réalisateur sur l'IA et les salles sûres : https://www.pcgamer.com/resident-evil-2s-director-talks-mr-xs-ai-scary-footsteps-and-the-dmx-mod
- Resident Evil 2, examen du design : https://www.gamedeveloper.com/design/a-design-examination-on-resident-evil-2-remake
- The Pac-Man Dossier : https://www.gamedeveloper.com/design/the-pac-man-dossier
- Titanfall 2 et l'enchaînement des courses murales : https://gamingbolt.com/titanfall-2-will-have-maps-that-will-heavily-focus-on-chaining-wall-runs
- Making of Titanfall 2 : https://www.gamesradar.com/the-making-of-titanfall-2-how-building-blocks-buddy-robots-and-half-life-led-to-one-of-the-freshest-fps-in-years/
- Mirror's Edge Catalyst, vision du coureur : https://ea.com/news/runners-vision-in-mirrors-edge-catalyst
- Mirror's Edge, critique du flux : https://worldofleveldesign.com/categories/level_design_tutorials/one-mistake-in-mirrors-edge.php
- Dishonored, choix du joueur (GDC 2013) : https://www.pcgamer.com/dishonored-arkane-player-improvisation
- Dishonored 2, leçons de level design (GDC 2017) : https://80.lv/articles/gdc-2017-level-design-lessons-from-dishonored-2
- Dishonored 2, Clockwork Mansion : https://www.gamedeveloper.com/design/level-design-deep-dive-i-dishonored-2-s-i-clockwork-mansion
- Doom Eternal, plateformes et traversée : https://www.gamespot.com/articles/why-doom-eternal-incorporates-puzzle-platforming/1100-6473287/
- Doom (2016), « push forward combat » : https://gdconf.com/news/speaker-qa-kurt-loudy-jake-campbell-break-combat-philosophy-doom
- Deadlock, la carte : https://pcgamer.com/games/moba/why-i-love-deadlocks-map-which-has-brought-me-back-to-the-world-of-mobas-long-after-i-swore-id-quit
- Deadlock, refonte de la carte : https://pcgamer.com/games/moba/deadlock-gets-a-massive-map-overhaul-that-shrinks-its-map-from-four-lanes-to-three-this-has-a-large-range-of-accompanying-map-wide-changes
- Valve, dimensions de référence : https://developer.valvesoftware.com/wiki/Dimensions_(Half-Life_2_and_Counter-Strike:_Source)
- Bloodborne, level design de Yharnam : https://www.cheatcc.com/articles/how-yharnam-s-streets-teach-you-to-play-bloodborne/
- Bloodborne, échelle et atmosphère : https://gamesradar.com/games/action-rpg/10-years-on-bloodborne-remains-an-unmatchable-feat-of-atmosphere-thanks-to-the-mind-boggling-oppressive-scale-of-yharnam
- Guider le joueur (Unity) : https://unity.com/blog/level-design-basics
- Composition en level design : https://gamedeveloper.com/design/composition-in-level-design
- Guide pratique de level design à la première personne : https://gamedeveloper.com/design/practical-guide-on-first-person-level-design
- Hades, level design moins aléatoire qu'il n'y paraît : https://kotaku.com/hades-level-design-is-less-random-than-it-seems-1845254545
- Hades, fil d'Ed Gorinstein : https://threadreaderapp.com/thread/1311777010239193088
- Dead Cells, approche hybride : https://deepnight.net/tutorial/the-level-design-of-dead-cells-a-hybrid-approach/
- Dead Cells reproduit dans Unity (Edgar) : https://ondrejnepozitek.github.io/Edgar-Unity/docs/examples/dead-cells
- Risk of Rain 2, crédits de carte (Conduit Canyon) : https://riskofrain2.wiki.gg/wiki/Conduit_Canyon
- Risk of Rain 2, variantes (Titanic Plains) : https://riskofrain2.wiki.gg/wiki/Titanic_Plains
- Hunt: Showdown, cartes et compounds : https://huntshowdown.fandom.com/wiki/Maps
- Hunt: Showdown, changements des compounds : https://huntshowdown.com/news/stillwater-and-lawson-details-of-the-compound-changes-coming-with-up
- Unity AI Navigation, NavMeshLink : https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/NavMeshLink.html
- Unity, détection des liens liée au rayon : https://issuetracker.unity3d.com/issues/offmeshlink-detection-distance-should-be-adjustable-independent-from-agent-radius
- Unity, échec d'escalade sur un lien large : https://issuetracker.unity3d.com/issues/navmeshlink-agent-fails-to-climb-onto-non-point-to-point-navmeshlink
- Unity, agents bloqués sur NavMeshLink : https://discussions.unity.com/t/agents-get-stuck-on-navmeshlink/682073
