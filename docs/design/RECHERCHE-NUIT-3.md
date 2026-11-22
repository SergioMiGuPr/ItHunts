# Recherche nuit 3 : level design, plateforme, améliorations, fin de run, boss, poursuite, parade

Recherche en ligne du 22/11/2026. Convention : les chiffres **sourcés** sont attribués au jeu ou à l'article ; les
chiffres marqués *(reco)* sont des valeurs proposées pour ItHunts, déduites des sources et des réglages actuels du
projet (`Movement_Default.asset` : coyote 0,10 s, tampon de saut 0,12 s, wall coyote 0,10 s, wall jump 1,8 m de haut et
7,5 m/s de poussée ; parade : fenêtre 0,18 s, arrêt sur image 0,08 s). Plusieurs sources sont secondaires (wikis,
presse) : elles sont signalées quand c'est le cas. Rien ici ne tranche une question ouverte du BRIEF : ce sont des
pistes à valider.

---

## 1. Level design professionnel

1. **Chaque salle a une fonction de combat ET une identité visuelle unique.** Doom 2016 reposait sur « couloir →
   arène → couloir », critiqué ensuite par Hugo Martin lui-même (« skate parks and arena fights ») ; Doom Eternal a
   rendu les espaces de transition utiles (« incidental combat », traversée en « puzzles d'adresse »).
   → application : dans le générateur, chaque module porte une étiquette de **fonction** (arène, couloir de fuite,
   cachette, salle de récompense, salle de traversée) et une **identité** (un objet-repère unique : orgue, puits,
   statue). Interdire deux modules de même fonction à la suite ; un couloir de liaison doit contenir au moins
   1 élément de mouvement (mur à wall jump, rampe de glissade ou point magnétique).

2. **Le rythme se règle comme un morceau de musique : pas d'arène après arène.** Hugo Martin (Doom Eternal) mesure le
   rythme « comme un morceau » et prévient que le joueur sature si les arènes s'enchaînent.
   → application : règle de génération **tension → relâche** : après une salle à vague, au moins 1 salle « calme »
   (récompense, marchand ou cachette) avant la suivante ; ratio cible par acte *(reco)* : 50 % combat, 30 % traversée
   ou fuite, 20 % repos.

3. **La lumière hiérarchise les sorties.** Robert Yang (GDC 2018, « How to light a level ») : utiliser des lumières
   clés (ponctuelles, spots) qui contrastent avec l'environnement sur la route probable, éclairer les sorties **par
   ordre d'importance** (l'entrée principale plus que la sortie discrète), garder un code constant (« si un type
   d'objet est éclairé d'une façon, toujours l'éclairer ainsi »). Naughty Dog guide avec des accents jaunes et rouges
   et des « weenies » (repères visibles de loin).
   → application : code lumière figé pour tout le jeu *(reco)* : porte de sortie payante = lumière chaude la plus forte
   de la salle (×2 l'intensité des autres sources), passages secondaires ×0,5, cachettes sans lumière directe ;
   surfaces magnétiques toujours de la même teinte (bleu de la traction) ; à ajouter à `ART-HANDOFF.md`.

4. **Une lumière de contour perpendiculaire au chemin critique détache les silhouettes.** Yang : en combat, les
   lumières clés se perdent ; une lumière de contour placée à peu près perpendiculairement au chemin critique fait
   ressortir les formes. Attention aux albédos trop sombres, qui rendent les salles bien plus noires que prévu
   (viser le haut des tons moyens).
   → application : chaque arène reçoit 1 lumière de contour latérale par axe de circulation principal, pour que
   goules et volants restent lisibles sur fond noir ; demander à la DA un albédo minimum (pas de mur sous ~25 %
   de luminance) plutôt que d'assombrir les textures.

5. **Lieux plausibles et histoire lisible sur place.** Harvey Smith (Arkane) : « on regarde chaque lieu pour voir s'il
   est plausible. A-t-il une histoire ? » ; le joueur doit pouvoir « se tenir à un endroit, regarder autour et
   reconstituer ce qui s'est passé ». Karnaca s'appuie sur des oppositions haut/bas, propre/sale, vide/peuplé.
   → application : chaque module reçoit 1 « scène muette » de 3 à 5 props cohérents (ex. : table renversée + traces
   de griffes + cage brisée = passage du traqueur). Le traqueur fait 14 m : ses traces (portes arrachées à 5,2 m,
   griffures en hauteur) racontent qu'il est passé là et préviennent de son gabarit.

6. **Jardin ouvert : guider par des bords naturels et des triangles, pas par des murs invisibles.** Breath of the Wild
   (CEDEC 2017) : les « triangles » (collines, rochers) cachent ce qui est derrière et créent la curiosité ; le monde
   s'organise en repères près des nœuds et en bords qui séparent les zones (Kevin Lynch, urbanisme).
   → application pour l'acte 3 : bordure faite de **haies taillées hautes de 6 m ou plus**, de fossés d'eau noire et de
   grilles de château ; à l'intérieur, des masses en triangle (bosquets, mausolées) tous les 15 à 25 m *(reco)* qui
   cassent la vue sur le traqueur (indispensable pour qu'une poursuite reste possible en espace ouvert) ; un repère
   très haut (tour, arbre mort illuminé) visible de partout pour s'orienter.

7. **Routes cachées, découvertes au deuxième passage.** Neon White (Ben Esposito) : chaque niveau est pensé pour être
   rejoué ; des raccourcis faits à la main, idéalement invisibles au premier passage ; les meilleurs sont nés de
   l'assemblage de rencontres conçues séparément (exactement comme un générateur de modules).
   → application : chaque module de l'atelier doit avoir **1 route lente évidente + 1 route rapide** (wall jumps,
   traction) que le vérificateur contrôle (atteignable avec les métriques réelles) ; la route rapide est la meilleure
   façon de semer le traqueur.

8. **Verticalité au service du mouvement.** Doom Eternal : le dash et l'escalade ont « ouvert massivement le level
   design » et forcé des niveaux plus grands. Titanfall 2 a construit sa campagne en « action blocks », petits
   prototypes de mouvement assemblés ensuite.
   → application : chaque arène a au moins 2 niveaux de hauteur (sol + balcon à 4 à 5,5 m, la hauteur de hissage
   actuelle) ; le traqueur (14 m) ne doit pas pouvoir couvrir les balcons : ils donnent un répit de quelques
   secondes, pas une cachette définitive.

Sources :
- Embracing Push Forward Combat in DOOM (GDC 2018) : https://gdcvault.com/play/1024940/Embracing-Push-Forward-Combat-in
- Hell Razer: The Making of Doom Eternal (Shacknews) : https://shacknews.com/article/116635/hell-razer-the-making-of-doom-eternal?page=6
- Why Doom Eternal Incorporates Puzzle Platforming (GameSpot) : https://www.gamespot.com/articles/why-doom-eternal-incorporates-puzzle-platforming/1100-6473287/
- GDC 2018: How to light a level (Robert Yang) : https://www.blog.radiator.debacle.us/2018/03/gdc-2018-how-to-light-level-slides-and.html
- Level design lighting in The Last of Us (80.lv) : https://80.lv/articles/see-how-level-design-lighting-helped-make-the-last-of-us-more-immersive
- Dishonored 2 director Harvey Smith on creating compelling worlds : https://gamedeveloper.com/design/-i-dishonored-2-i-director-harvey-smith-on-creating-compelling-worlds
- Space, place and the art of designing deep levels : https://www.gamedeveloper.com/design/space-place-and-the-art-of-designing-deep-levels
- BotW triangles (GamingBolt) : https://gamingbolt.com/the-legend-of-zelda-breath-of-the-wilds-ingenious-world-design-owes-itself-to-triangles/amp
- Open world level design (Radiator) : https://www.blog.radiator.debacle.us/2017/10/open-world-level-design-spatial.html
- Neon White and designing for player creativity : https://www.gamedeveloper.com/business/-neon-white-and-designing-for-player-creativity
- GDC18: Designing Titanfall levels with action blocks (80.lv) : https://80.lv/articles/gdc18-designing-titanfall-levels-with-action-blocks

---

## 2. Plateforme 3D et wall jumps agréables

1. **Coyote time et tampon de saut sont la base, invisibles mais obligatoires.** Celeste (Maddy Thorson) : saut
   possible « un court instant » après avoir quitté le bord, et saut pressé juste avant l'atterrissage exécuté à la
   frame d'atterrissage. Les sources communautaires donnent ~5 frames (~83 ms) de coyote à 60 i/s pour Celeste.
   → application : les valeurs actuelles (coyote 0,10 s, tampon 0,12 s) sont dans la bonne fourchette et plus
   généreuses que Celeste, ce qui convient à une vue première personne (on ne voit pas ses pieds). Ne pas descendre
   sous 0,10 s ; ajouter un **tampon d'atterrissage pour la glissade** de même durée (0,12 s) pour que
   « saut + glissade » enchaîne sans frame perdue en bunny hop.

2. **Fenêtre de wall jump élargie par la distance, pas seulement par le temps.** Celeste accepte le wall jump à
   2 pixels du mur (un quart de tuile) et le super wall jump à ~5 pixels.
   → application : tolérance de détection du mur *(reco)* : **0,35 m** de portée (environ un tiers de la largeur du
   joueur) pour le wall jump, sans exiger le contact ; garder `wallCoyoteTime` 0,10 s ; vérifier que la tolérance
   d'entrée (`wallSlideInputTolerance` 75°) ne rejette pas un joueur qui regarde le long du mur.

3. **Apex de saut flottant si le bouton est tenu.** Celeste : gravité divisée par 2 au sommet si le saut est tenu.
   → application : multiplicateur de gravité 0,5 quand |vitesse verticale| < 1,5 m/s et saut tenu *(reco)* ; donne
   le temps de viser un point magnétique ou un mur au sommet, sans changer la hauteur max.

4. **Correction de coin : ne pas punir un saut raté de 10 cm.** Celeste décale le personnage autour d'un coin quand
   la tête cogne, et le pousse sur le rebord quand un dash frôle le bord.
   → application : **hissage automatique** si le haut du collider est à moins de 0,3 m sous le rebord en montée, et
   décalage latéral de 0,2 m max quand la tête accroche un coin de plafond *(reco)*.

5. **Distance entre murs pour l'enchaînement.** Titanfall 2 : le joueur ne faisait pas confiance au wall run ; la
   solution fut le level design (sections d'apprentissage imposées) et un bonus d'élan au saut depuis le mur ; le wall
   run durait 1,75 s dans Titanfall 1 ; Titanfall 2 n'autorise à reprendre un mur qu'en dessous du point de prise
   précédent (« double wall kick »).
   → application : avec une poussée de 7,5 m/s et ~0,55 s de vol pour 1,8 m de hauteur, un couloir de wall jumps en
   zigzag doit faire **3 à 4,5 m de large** *(reco)* (plus étroit : on rebondit avant d'avoir le temps de tourner la
   tête ; plus large : on n'atteint pas l'autre mur sans élan). Le premier module de chaque acte contient un couloir
   de wall jumps sans danger, pour apprendre.

6. **Plateformes larges, pièges rares.** Les jeux de vitesse (Neon White, Ghostrunner) acceptent l'imprécision parce
   que le saut raté coûte quelques secondes, pas la run.
   → application : plateformes de traversée de **2 m minimum** de côté (≈ 3 largeurs de joueur), rebords de
   réception ≥ 1,2 m *(reco)* ; aucun vide mortel dans les caves et le château : une chute ramène plus bas dans la
   salle (le danger, c'est le traqueur qui rattrape, pas la mort par chute).

7. **Aide magnétique : aimanter le point visé, pas la trajectoire.** Principe commun aux jeux de mouvement en vue
   première personne : l'aide se fait sur la cible (rayon de visée élargi), jamais en prenant le contrôle du joueur.
   → application : la visée de la traction utilise un **sphere cast de 0,4 m** de rayon *(reco)* après un raycast
   raté, sur les surfaces magnétiques seulement ; le point retenu est le plus proche du centre de l'écran.

Sources :
- Celeste & Forgiveness (Maddy Thorson) : https://maddymakesgames.com/articles/celeste_and_forgiveness/
- Celeste coyote time (Game Rant) : https://gamerant.com/celeste-coyote-time-mechanic-platforming-impact-hidden-mechanics/
- Wall Running (Titanfall wiki) : https://titanfall.wiki.gg/wiki/Wall_Running
- Titanfall 2 speedrun glossary : https://tf2sr.github.io/TF2SR-Wiki/docs/knowledge-base/glossary/TITANFALL%202%20Speedrun%20Glossary
- Designer interview: getting Titanfall's controls just right : https://www.gamedeveloper.com/design/designer-interview-getting-i-titanfall-i-s-controls-just-right
- What Mario learned from Mickey Mouse, part 2: architecture and metrics : https://gamedeveloper.com/design/what-mario-learned-from-mickey-mouse---part-2-architecture-and-metrics
- Wall Jump (Super Mario Wiki) : https://www.mariowiki.com/Wall_Jump

---

## 3. Roguelites et améliorations

1. **Trois façons d'empiler, choisies par objet.** Risk of Rain 2 : empilement **linéaire** (+X par exemplaire, ex.
   N'kuhana's Opinion +100 % de dégâts par exemplaire), **hyperbolique** pour les chances (ne dépasse jamais 100 % :
   10 Old Guillotine = exécution à 56,5 % de vie), et exponentiel, rare.
   → application : dans les ScriptableObjects d'amélioration, un champ `StackMode` (Linéaire, Hyperbolique,
   Unique) ; toute **chance** (critique en chaîne, paralysie, ricochet) est hyperbolique :
   `p = 1 − 1/(1 + k·n)` avec k = 0,1 à 0,15 *(reco)*.

2. **Coefficient de déclenchement pour éviter les chaînes infinies.** RoR2 : la chance d'un effet « au toucher » est
   multipliée par le coefficient de proc de l'attaque ; un effet déclenché par un autre effet a un coefficient plus
   faible (chaînes freinées).
   → application : chaque source de dégâts porte un `procCoefficient` : caisse lancée 1,0, balle de barillet 0,7,
   ricochet 0,5, explosion sur kill 0,3, éclair en chaîne 0,2 *(reco)* ; profondeur de chaîne max 3.

3. **Rareté en multiplicateur, et des taux de coffre chiffrés.** Megabonk : chaque rareté multiplie la valeur de base
   (×1,2 peu commun, ×1,4 rare, ×1,6 épique, ×2 légendaire). RoR2 : petit coffre 79,2 % commun, 19,8 % peu commun,
   0,99 % légendaire ; grand coffre (2× prix) 80 % peu commun, 20 % légendaire.
   → application : une seule définition par amélioration + table de multiplicateurs de rareté (ci-dessus) ; coffre
   normal 80/19/1, coffre de la salle verrouillée 0/80/20 et 2× prix en âmes.

4. **Des améliorations qui changent le comportement, pas seulement les chiffres.** Hades : les dieux donnent des
   bienfaits liés à une action (attaque, spécial, dash) en 4 raretés colorées (blanc, bleu, violet, rouge) ; les
   bienfaits « duo » combinent deux dieux. Roboquest : chaque classe a ~12 atouts à 3 niveaux et des synergies propres.
   → application : rattacher chaque amélioration à **une action de l'orbe** (traction, répulsion, prise, lancer,
   artefact) ; 1 amélioration sur 4 au minimum modifie un comportement (la caisse ricoche, la répulsion paralyse, la
   traction laisse une traînée qui brûle) ; prévoir 3 à 5 « duos » qui n'apparaissent que si les deux améliorations
   parentes sont possédées.

5. **Catalogue d'effets d'arme, avec valeurs de départ.** Familles courantes dans Gunfire Reborn, Roboquest, RoR2 :
   perforation, ricochet, critique en chaîne, explosion sur kill, paralysie, rechargement sur kill.
   → application *(reco)* : perforation +1 cible (linéaire) ; ricochet 1 rebond à 60 % des dégâts, rayon 8 m ;
   critique en chaîne 15 % (hyperbolique), saute sur 2 cibles à 50 % ; explosion sur kill rayon 3 m, 40 % des PV max de
   la victime ; paralysie 10 % (hyperbolique), 1,5 s (la durée d'étourdissement actuelle) avec immunité 2 s ;
   rechargement sur kill : rend 1 charge de traction (pas plus d'1 par seconde). Le traqueur ignore tout
   (`ImmuneToOrbs`).

6. **L'ordre des choix fait la variété.** Hades et Dead Cells : le joueur choisit 1 parmi 3 ; la run se construit
   autour des premiers choix ; Supergiant déplace plutôt qu'il supprime les bienfaits trop forts.
   → application : offrir **3 choix** par récompense, dont au moins 1 de la même action qu'une amélioration déjà
   possédée (pousser la synergie) et 1 d'une action vierge (ouvrir une piste) ; relance payante en âmes (coût ×1,5
   à chaque relance).

7. **Méta-progression qui élargit, pas qui renforce.** Megabonk : 78 à 85 objets, dont 35 débloqués au départ ; les
   autres se débloquent par des défis (« survivre 60 s à la vague finale »). Roboquest : améliorations de camp entre
   les runs.
   → application : démarrer avec ~40 % du catalogue ; chaque déblocage lié à un exploit lisible (« tuer 10 goules
   avec une seule caisse », « finir un acte sans être touché ») affiché à l'écran de fin de run.

Sources :
- N'kuhana's Opinion (RoR2 wiki) : https://riskofrain2.wiki.gg/wiki/N%27kuhana%27s_Opinion
- Luck (RoR2 wiki) : https://riskofrain2.fandom.com/wiki/Luck
- Large Chest (RoR2 wiki) : https://riskofrain2.wiki.gg/wiki/Large_Chest
- ProcPatcher (Thunderstore) : https://thunderstore.io/c/riskofrain2/p/RiskOfBrainrot/ProcPatcher/
- Megabonk weapon and tome upgrades guide : https://gurugamer.com/pc-console/megabonk-complete-weapon-and-tome-upgrades-guide-25658
- How to unlock all items in Megabonk : https://progameguides.com/megabonk/how-to-unlock-all-items-in-megabonk/
- Hades boons (Fextralife) : https://hades.wiki.fextralife.com/Boons
- Hades 2 lead on boons (PC Gamer) : https://www.pcgamer.com/games/roguelike/hades-2-lead-says-they-didnt-nerf-the-games-most-popular-boons-but-rather-found-new-homes-for-them-during-early-access-development
- Roboquest: all Engineer perks : https://kosgames.com/?p=15038
- Roboquest (Wikipedia) : https://en.wikipedia.org/wiki/Roboquest

---

## 4. Écran de stats de fin de run

1. **Montrer d'un coup d'œil pourquoi la run est gagnée ou perdue.** Discussion r/roguelites (RoR2 cité comme
   référence d'écran de fin « propre ») : le joueur veut voir ce qu'il a bien et mal fait ; stats demandées : dégâts
   totaux, déclenchements par objet.
   → application : en haut, en gros : **cause de la mort** (« Le traqueur, acte 2, salle 7 ») et distance parcourue
   dans la run (actes et salles franchis, sous forme de frise).

2. **Les stats qui racontent le style de jeu.** Les écrans de RoR2 et Vampire Survivors détaillent les dégâts par
   objet ou par arme.
   → application : tableau **dégâts par source** (caisses, barillet, répulsion, chaque amélioration), trié du plus
   fort au plus faible, avec une barre ; c'est ce qui fait dire « la prochaine fois je prends plus de ricochet ».

3. **Les stats de mouvement, signature de ce jeu.** (Pas de source directe : déduction des piliers du projet.)
   → application : vitesse max atteinte, plus long bunny hop (nombre de sauts), temps passé au-dessus de 15 m/s,
   distance au plus près du traqueur sans mourir (« frôlé à 2,1 m »), nombre de fois semé.

4. **Économie et choix.** Hades et Slay the Spire conservent l'historique des runs (bienfaits, cartes, chemin).
   → application : âmes gagnées / dépensées, liste des améliorations prises dans l'ordre (icônes), porte de sortie
   payée à quel prix ; historique des 20 dernières runs consultable depuis le hub.

5. **Un objectif concret pour la prochaine run.** Les guides de design relient « encore une run » à des runs de 20 à
   40 min et à un redémarrage rapide après la défaite ; chaque échec doit apprendre quelque chose.
   → application : afficher 1 à 3 **déblocages proches** (« 7/10 goules tuées par une seule caisse ») et un bouton
   « Relancer » sélectionné par défaut ; de la mort à la nouvelle run en **moins de 10 s** *(reco)* ; cible de durée
   d'une run complète : 25 à 40 min.

6. **Records personnels mis en évidence.** Les jeux à speedrun (Hades : chrono interne qui exclut les pauses)
   rendent le temps comparable.
   → application : chrono de run hors pauses et menus ; chaque stat qui bat un record est marquée « Nouveau
   record » (couleur dorée) ; ne montrer que 8 à 10 stats par défaut, le reste dans un onglet « Détails ».

Sources :
- What game has the best end of run screen? (r/roguelites) : https://redlib.belloworld.it/r/roguelites/comments/1ephz8t/what_game_has_the_best_end_of_run_screen
- Building a roguelike: procedural progression (StraySpark) : https://www.strayspark.studio/blog/building-roguelike-ue5-procedural-progression
- Why we can't stop playing these roguelites : https://www.feedme.design/why-we-cant-stop-playing-these-roguelites
- 20 best one more run games (G2A) : https://www.g2a.com/news/features/20-best-one-more-run-games-endless-replayability/
- Hades speedrun rules (in-game timer) : https://speedrun.com/hades/guide/qz4uh

---

## 5. Combats de boss en arène ouverte

1. **Chaque attaque a une annonce mesurée en ms, consignée dans un tableau.** Les guides de design de boss
   recommandent une table de chaque attaque (durée d'annonce en ms + réponse attendue) ; l'injustice vient
   d'annonces **incohérentes** plus que de valeurs fausses ; la durée de l'annonce règle la difficulté (Cuphead,
   GMTK). Un document de design cite 0,4 à 0,6 s pour une attaque légère, plus pour les attaques dangereuses.
   → application *(reco)* pour le traqueur blessable : coup de patte 600 ms d'annonce, charge 900 ms, onde au sol
   1 200 ms, attaque qui tue en un coup ≥ 1 000 ms ; en phase 3, toutes les annonces × 0,8, jamais sous 450 ms.

2. **Annonce multicanale : animation + son + effet, et le son seul doit suffire.** Article « Enemy attacks and
   telegraphing » : combiner animation, son, voix, effets visuels ; le joueur doit savoir que le coup arrive avant
   de devoir l'éviter, et l'annonce doit montrer exactement ce qui va se passer. Doom Eternal : le Marauder devient
   vulnérable quand ses yeux flashent en vert avec un son dédié.
   → application : chaque attaque du traqueur a 1 son d'annonce unique et localisé en 3D (il est souvent hors champ
   vu sa taille) + 1 lueur sur la partie qui frappe ; un code couleur global : **lueur verte = ouverture / parable**,
   rouge = à esquiver.

3. **Les attaques retardées : avec parcimonie.** Elden Ring a fait des attaques retardées un mème ; les joueurs
   saluent les boss aux combos courts avec une seule attaque retardée importante et de grands trous pour riposter.
   → application : au plus **1 attaque à retard** dans le panel du traqueur, et seulement à partir de la phase 2 ;
   après chaque combo, une fenêtre de riposte ≥ 1,5 s *(reco)*.

4. **Points faibles en hauteur, révélés par la lumière.** Shadow of the Colossus : chaque colosse a au moins 1 point
   faible signalé par un sceau lumineux ; on s'agrippe à la fourrure et aux rebords ; l'endurance limite le temps
   accroché ; l'arène et le colosse étaient retouchés ensemble.
   → application : le traqueur (14 m) porte **3 points faibles lumineux** (nuque, dos, épaule) atteignables seulement
   par traction (plafond 18 m/s) vers des **surfaces magnétiques sur son corps** ou des piliers de l'arène ;
   accroché, le joueur a 3 s *(reco)* avant d'être éjecté (équivalent de l'endurance) ; c'est le mouvement qui ouvre
   les dégâts, pas le tir au sol.

5. **Deux ou trois phases, chacune introduit une idée.** Hades : boss final en 3 phases ; les phases suivantes
   raccourcissent les annonces pour les joueurs qui maîtrisent la base.
   → application : phase 1 (100 à 66 %) : les attaques de base de la poursuite ; phase 2 (66 à 33 %) : invocation de
   goules et volants (au plus 6 ennemis à la fois) + attaque retardée ; phase 3 (sous 33 %) : l'arène change (piliers
   qui s'effondrent, sol qui brûle) et les annonces × 0,8. Transition = 2 à 3 s d'invulnérabilité annoncée, jamais
   un coup pendant la transition.

6. **Les invocations servent le joueur.** Doom (« push forward combat ») : les ennemis faibles sont la ressource du
   joueur (vie, munitions).
   → application : les goules invoquées lâchent des âmes et peuvent être **attrapées et lancées** sur un point faible
   (dégâts ×1,5 *(reco)*) ; les volants sont des points d'accroche mobiles pour la traction.

7. **Ouvertures après une erreur du boss.** Monster Hunter : viser une blessure visible augmente les dégâts ; casser
   une partie affaiblit le monstre (Stories 3 : partie non cassée à temps = souffle de feu).
   → application : quand le traqueur rate une charge contre un pilier, il est **sonné 3 s** (seul moment où les
   points faibles bas sont accessibles au sol) ; chaque point faible détruit retire une attaque de son panel.

Sources :
- Enemy attacks and telegraphing (Game Developer) : https://gamedeveloper.com/design/enemy-attacks-and-telegraphing
- Boss phases: readable, learnable, beatable : https://molbv.github.io/blog/2025-07-08-boss-phases.html
- Salt & Steel boss design document : https://salt-and-steel.pages.dev/docs/15/boss-design
- How Cuphead's bosses try to kill you (GMTK, transcription) : https://amara.org/subtitles/TbmQ6Ac3Z6pb/en/1/download/How%20Cupheads%20Bosses%20Try%20to%20Kill%20You%20%20Game%20Makers%20Toolkit.en.srt
- Elden Ring's delayed attacks become a meme (Automaton) : https://automaton-media.com/en/news/20220316-10577/
- Doom Eternal Marauder guide (GameSpot) : https://gamespot.com/articles/doom-eternal-marauder-boss-guide-how-to-kill-the-g/1100-6475129/
- The making of the colossi (Glitterberri) : https://glitterberri.com/extended-developer-interview/making-of-the-colossi/
- Shadow of the Colossus (Wikipedia) : https://en.wikipedia.com/wiki/Shadow_of_the_Colossus
- Monster Hunter Wilds interview (Inverse) : https://www.inverse.com/gaming/monster-hunter-wilds-interview-capcom-tokuda
- Hades writer Greg Kasavin (Inlander) : https://inlander.com/culture/hades-writer-greg-kasavin-on-how-he-made-video-game-deaths-drive-a-feel-good-story-22725237

---

## 6. Peur et poursuite

1. **Une jauge de menace qui oblige le poursuivant à reculer.** Alien Isolation : un « directeur » suit la jauge de
   menace, qui monte quand l'alien est proche, visible ou capable d'atteindre vite le joueur (plus lentement s'il est
   dans une autre pièce) ; au pic prolongé, l'alien part « en coulisses » (conduits), de préférence près de là où le
   joueur va ensuite. La tension doit varier : une peur constante fait abandonner.
   → application : jauge de menace par joueur (0 à 100) ; au-delà de 80 pendant 20 s *(reco)*, le traqueur perd la
   piste et fait un détour de 8 à 12 s ; il ne s'arrête jamais (D-035 : il poursuit en permanence), mais sa
   **vitesse effective** baisse pendant le détour. À valider avec le design actuel du traqueur.

2. **Le monstre ne triche pas, ou presque.** Alien Isolation : le directeur sait où est le joueur, l'alien non ; le
   directeur ne fait que lui « indiquer » une zone ; 2 téléportations seulement en 12 à 18 h, pour des cinématiques ;
   des zones que l'alien ne fouille jamais pour éviter les détections injustes ; l'ordre de fouille est volontairement
   imparfait, ce qui ressemble à du doute.
   → application : le traqueur connaît toujours la **salle** du joueur (poursuite façon Pac-Man), mais pas sa position
   exacte quand le joueur est caché ; ne jamais le faire apparaître hors champ à moins de 30 m du joueur.

3. **Un rayon de terreur en 3 couches sonores.** Dead by Daylight : rayon de terreur de 32 m pour un tueur à 4,6 m/s
   (24 m à 4,4 m/s), découpé en **3 tiers** (loin, proche, très proche), avec une 4e couche « musique de poursuite »
   qui remplace la 3e pendant une poursuite ; le battement de cœur accélère et grossit avec la proximité.
   → application : rayon de terreur du traqueur = **40 m** *(reco, il est plus rapide et plus grand)* en 3 couches :
   40 à 27 m battement lent (60 bpm), 27 à 13 m moyen (90 bpm), sous 13 m rapide (130 bpm) + musique de poursuite
   dès qu'il a la ligne de vue.

4. **Les pas, signature du poursuivant.** Resident Evil 2 (Mr X) : de vrais pas humains n'avaient pas assez
   d'impact, l'équipe a superposé plusieurs sons ; ses pas lourds et ses portes claquées créent la peur ; il ne court
   pas, il semble sûr de rattraper le joueur ; on progresse pendant qu'il rôde.
   → application : pas du traqueur en 3 couches (impact grave < 80 Hz, craquement de pierre, cliquetis) audibles à
   travers les murs jusqu'à 40 m, avec une **légère secousse caméra** sous 15 m ; portes arrachées = son marquant.

5. **Ne pas trop montrer, et ne pas récompenser la peur.** Frictional (Amnesia) : des monstres trop présents avec des
   récompenses de jeu transforment la peur en excitation ; le Water Lurker marche parce qu'on l'associe à l'eau plus
   qu'on ne le voit ; les monstres s'approchent de la cachette puis repartent.
   → application : le traqueur est souvent **entendu ou aperçu en silhouette** (lumière de contour derrière lui), plus
   rarement vu en entier ; quand le joueur est en cachette, il passe à 5 à 8 m et repart (pas de jeu du chat et de la
   souris injuste).

6. **Le rythme : pics, puis répit pour progresser.** Alien Isolation et RE2 : alternance tension/relâche ; les pics
   sont suivis de pauses qui laissent avancer.
   → application : le timer d'acte et la rage finale sont le pic programmé ; entre-temps, garantir au moins 1 fenêtre
   de 15 à 20 s *(reco)* par salle à vague où le traqueur est à plus de 27 m (couche « loin »).

Sources :
- The perfect organism: the AI of Alien: Isolation (Game Developer) : https://www.gamedeveloper.com/design/the-perfect-organism-the-ai-of-alien-isolation
- Revisiting the AI of Alien: Isolation : https://www.gamedeveloper.com/design/revisiting-the-ai-of-alien-isolation
- Terror Radius (Dead by Daylight wiki) : https://deadbydaylight.wiki.gg/wiki/Terror_Radius
- Resident Evil 2 director talks Mr. X's AI and footsteps (PC Gamer) : https://www.pcgamer.com/resident-evil-2s-director-talks-mr-xs-ai-scary-footsteps-and-the-dmx-mod
- How Capcom created Resident Evil 2's scariest element : https://wegotthiscovered.com/gaming/xs-footsteps-created-resident-evil-2/
- How to design an alpha antagonist (Super Jump) : https://medium.com/super-jump/the-horrific-appeal-of-mr-x-af88c622faed
- 9 years, 9 lessons on horror (Frictional Games) : https://frictionalgames.blogspot.com/2019/10/9-years-9-lessons-on-horror.html?m=1
- GDC Europe: Amnesia's Grip on immersion : https://gamedeveloper.com/business/gdc-europe-i-amnesia-i-s-grip-delivers-terrifying-tales-of-immersion
- Water Lurker interview (USgamer) : https://www.usgamer.net/articles/monster-of-the-week-water-lurker-kaernk-amnesia-the-dark-descent-frictional-games-interview

---

## 7. Parade, retours et headshots

1. **Fenêtre de parade : 150 à 500 ms selon le jeu.** Sekiro : ~12 frames actives à 60 i/s (~200 ms), qui
   rétrécissent si on martèle le bouton (une source wiki dit 30 frames, chiffre douteux). Doom: The Dark Ages : ~500 ms
   par défaut, réglable jusqu'à 1 s dans les options. Un mod Ultrakill indique 10 frames par défaut (valeur de mod, pas
   du jeu de base).
   → application : la fenêtre actuelle de 0,18 s est au niveau de Sekiro, exigeante pour un FPS de mouvement ;
   proposer **0,25 s** par défaut et l'exposer dans les options d'accessibilité (0,18 à 0,5 s) ; pénalité de martelage :
   une nouvelle tentative dans les 0,4 s divise la fenêtre par 2.

2. **Pas de parade trop bonne, ni trop tôt.** Hakita (Ultrakill) : une parade tentée trop tôt ne doit pas compter, sinon
   « c'est juste un contre » ; Safford : « la pire parade est celle qui est trop bonne » ; la parade doit rester une
   option parmi d'autres (esquive, mouvement). Nine Sols : une parade légèrement imprécise interrompt quand même mais
   laisse des dégâts temporaires.
   → application : parade réservée aux attaques **marquées en vert** (projectiles de volants, griffe de goule), jamais
   au traqueur ; parade « limite » (dans les 80 ms après la fenêtre) *(reco)* : bloque 50 % des dégâts sans
   récompense.

3. **Arrêt sur image : tout le jeu, son compris.** Ultrakill : sur une parade réussie, tout le jeu, son et musique
   compris, se fige un instant ; les projectiles attendent quelques frames après le contact avant d'infliger les
   dégâts, ce qui laisse une marge pour parer. Repères de hitstop (blog) : coups lourds et parades 8 à 12 frames
   (130 à 200 ms), coups de grâce 12 à 20 frames, au-delà de 20 frames ça ressemble à un bug.
   → application : `parryHitStop` actuel 0,08 s → **0,12 s** et couper aussi l'audio pendant l'arrêt ; ajouter
   **3 frames (50 ms) de grâce** entre contact d'un projectile et dégâts *(reco)*.

4. **Récompenser la parade par des ressources, pas par des chiffres.** Steel Carnelian : la parade recharge munitions,
   boost et multiplicateur, et renvoie les projectiles. Doom: The Dark Ages : la parade étourdit l'ennemi et prépare une
   contre-attaque ; les projectiles verts sont renvoyables.
   → application : parade réussie = +1 charge de traction ou de répulsion, projectile renvoyé vers la cible visée
   (aide à la visée 10°), ennemi étourdi 1,5 s (valeur `stunDuration` actuelle).

5. **Headshot : son distinct + marqueur de couleur.** Halo 5, Overwatch et Titanfall 2 passent le marqueur d'impact en
   rouge sur un critique ; les guides FPS recommandent un son de headshot plus percutant et un son final de
   confirmation de kill ; un ingénieur son de Call of Duty : le joueur veut une confirmation immédiate que ses tirs
   touchent ; PC Gamer rappelle que la réaction de la cible doit porter l'essentiel du retour.
   → application : critique (tête de goule, œil de volant) = **×2 dégâts** *(reco)*, marqueur d'impact jaune vif plus
   gros de 50 %, son « tink » aigu superposé au son d'impact, petite projection de la tête (réaction physique),
   chiffre de dégâts optionnel ; kill = son de confirmation distinct, plus grave.

6. **Doser l'intensité du retour selon l'importance.** Hi-Fi Rush et Doom Eternal synchronisent les retours forts
   avec les actions rares (exécutions, parades) ; les petits coups restent discrets (principe général « juice »,
   Vlambeer 2013).
   → application : échelle de retours à 3 niveaux *(reco)* : toucher simple (marqueur, pas de hitstop), critique
   (marqueur + son + hitstop 30 ms), parade ou kill multiple (hitstop 120 ms + secousse 0,25 + flash). Ne jamais
   superposer deux hitstops : prendre le plus long.

Sources :
- Deflection (Sekiro Fextralife wiki) : https://sekiroshadowsdietwice.wiki.fextralife.com/Deflection
- Nine Sols vs Sekiro (Switchblade Gaming) : https://www.switchbladegaming.com/action-rpg/vs-sekiro-2/
- What goes into a good parry system? (Game Developer) : https://www.gamedeveloper.com/design/what-goes-into-a-good-parry-system-
- KnuckleBlasterParry changelog (mod Ultrakill) : https://thunderstore.io/c/ultrakill/p/DestinedEnd/KnuckleBlasterParry/changelog
- Doom: The Dark Ages difficulty settings (Method) : https://www.method.gg/doom-the-dark-ages/doom-the-dark-ages-all-difficulty-settings-explained
- Doom: The Dark Ages review (ESPN) : https://www.espn.com.au/gaming/story/_/id/45067066/doom-dark-ages-review
- Hit stop frame trick : https://gamejuice.pages.dev/articles/hit-stop-frame-trick
- Hitstop / hitfreeze / hitlag (critpoints) : https://critpoints.net/2017/05/17/hitstophitfreezehitlaghitpausehitshit/
- Sakurai on hitstop (Source Gaming) : https://sourcegaming.info/2015/11/11/thoughts-on-hitstop-sakurais-famitsu-column-vol-490-1/
- The reticle, unsung hero of modern shooters (Twinfinite) : https://twinfinite.net/2016/12/reticle-unsung-hero-modern-shooters/
- Hit markers are an FPS crutch (PC Gamer) : https://www.pcgamer.com/games/fps/hit-markers-are-an-fps-crutch/
- The origin of Call of Duty's most-heard sound (PC Gamer) : https://www.pcgamer.com/the-origin-of-call-of-dutys-most-heard-sound
- What makes an FPS feel good? (StraySpark) : https://www.strayspark.studio/blog/fps-game-design-fundamentals-ue5

---

## Synthèse : 15 décisions à appliquer cette nuit

Toutes sont des propositions réglables dans les ScriptableObjects ; celles qui touchent une question ouverte du BRIEF
(traqueur blessable, rayon de terreur, méta-progression) sont à présenter à l'utilisateur avant d'être figées.

1. Générateur : jamais 2 salles à vague de suite, au moins 1 salle calme entre les deux (cible 50 % combat / 30 % traversée / 20 % repos).
2. Chaque module : 1 route lente évidente + 1 route rapide (wall jump ou traction) contrôlée par le vérificateur de l'atelier.
3. Code lumière : porte de sortie = source la plus forte de la salle (×2), passages secondaires ×0,5, cachettes sans lumière directe.
4. Jardin (acte 3) : bords en haies ≥ 6 m, fossés et grilles ; masse qui coupe la vue tous les 15 à 25 m ; 1 repère haut visible partout.
5. Tampon de glissade à l'atterrissage : 0,12 s (comme le tampon de saut) ; coyote et tampon actuels conservés (0,10 / 0,12 s).
6. Portée de détection du mur pour le wall jump : 0,35 m sans contact exigé ; couloirs de wall jumps de 3 à 4,5 m de large.
7. Gravité ×0,5 à l'apex (|vy| < 1,5 m/s) si le saut est tenu.
8. Aide de visée de la traction : sphere cast de 0,4 m de rayon sur les surfaces magnétiques après un raycast raté.
9. Améliorations : champ `StackMode` ; toute chance est hyperbolique `1 − 1/(1 + 0,12·n)`.
10. `procCoefficient` par source (caisse 1,0 ; balle 0,7 ; ricochet 0,5 ; explosion 0,3 ; éclair 0,2), chaîne max 3.
11. Coffres : 80/19/1 (commun/rare/légendaire) ; salle verrouillée 0/80/20 à 2× le prix ; multiplicateurs de rareté ×1 / ×1,2 / ×1,4 / ×1,6 / ×2.
12. Écran de fin : cause de la mort + frise des salles, dégâts par source, 4 stats de mouvement, 1 à 3 déblocages proches, relance en moins de 10 s.
13. Rayon de terreur du traqueur : 40 m en 3 couches sonores (40/27/13 m, battement 60/90/130 bpm) + musique de poursuite à la ligne de vue.
14. Parade : fenêtre 0,25 s par défaut (réglable de 0,18 à 0,5 s), hitstop 0,12 s avec audio coupé, 50 ms de grâce avant les dégâts d'un projectile.
15. Headshot ×2 dégâts, marqueur jaune +50 % et son aigu dédié ; boss final : annonces de 600 à 1 200 ms (×0,8 en phase 3, jamais sous 450 ms) et 3 points faibles atteignables par traction.
