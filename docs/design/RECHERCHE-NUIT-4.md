# Recherche nuit 4 : poursuite, verticalité, économie des améliorations, scaling, boss lisibles, mort

Recherche en ligne du 22-23/11/2026, complément de `RECHERCHE-NUIT-3.md` (qui couvrait déjà : rythme des salles et
lumière, coyote/tampon/wall jump, empilement et proc coefficient RoR2, raretés de coffre, écran de fin, durées d'annonce
des boss, jauge de menace d'Alien Isolation, rayon de terreur DbD, parade et headshot). Rien n'est répété ici.

Convention : les chiffres **sourcés** sont attribués au jeu ou à l'article ; *(reco)* marque une valeur proposée pour
ItHunts, déduite des sources et des réglages actuels (`Movement_Default` : course 8,5 m/s, gains jusqu'à 20 m/s, saut 1,3 m,
hissage 3,7 / 5,3 m, dash 6,5 m ; `Stalker_Default` : poursuite **9 m/s**, enragé **11 m/s**, pointes 16 / 18 m/s ; traqueur
14 m, épaule à 12 m, main à 15 m ; `Economy_Default` : coffre 30 âmes, porte 50, soin 35, marchand +25 par achat ; salles de
46 à 66 m). Les sources communautaires (wikis, forums) sont signalées. Rien ne tranche une question ouverte du BRIEF.

---

## 1. Level design des cartes à poursuite et évasion

### Ce que font les meilleurs

1. **Dead by Daylight : la carte est une grille de tuiles de 16 m.** Les tuiles font 16 × 16, 16 × 32 ou 32 × 32 m ; la
   plus petite mesure fiable du jeu est la section de mur de bordure de 8 m. Chaque tuile « maze » contient une fenêtre
   et une palette **toujours sur des côtés opposés** (jungle gym), ou un mur en T + un mur en L avec fenêtre chacun, ou
   quatre murs parallèles (4-lane) ; chaque tuile peut aussi porter un casier, un crochet, un coffre (wiki DbD).
2. **Le poursuivant est à peine plus rapide : ×1,10 ou ×1,15.** Survivant 4,0 m/s, tueur 4,4 ou 4,6 m/s ; coup de
   fouet (lunge) 6,9 m/s contre un sprint post-blessure de 6,6 m/s pendant 1,8 s. En ligne droite le tueur gagne, mais
   lentement : c'est l'obstacle qui fait la poursuite, pas la course (wiki DbD, Movement Speeds).
3. **Les ressources de fuite sont comptées et espacées.** Palettes à **14, 16, 18 ou 20 m** minimum les unes des autres sur
   des emplacements prédéfinis ; étourdissement de 2 s si le tueur est dans la zone quand la palette est à 50 % ;
   2,34 s pour la casser. Une zone sans palette devient une « dead zone » : la mort assurée (wiki DbD, forums BHVR).
4. **Les fenêtres se ferment après 3 passages.** Vault rapide 0,5 s (course droite ≥ 2,5 m), moyen 0,9 s, lent 1,5 s ;
   tueur 1,7 s ; **3 vaults de la même fenêtre pendant une poursuite = bloquée 30 s** pour ce survivant seulement.
   Le jeu force à changer de boucle (wiki DbD, Windows).
5. **Les tuiles fortes sont séparées par des tuiles faibles.** Forums DbD : une carte trop grande affaiblit les tueurs
   lents ; des tuiles fortes contiguës deviennent injouables ; les tuiles « droites » ont une route centrale ouverte (BHVR).
6. **Pac-Man : aucun cul-de-sac, et le poursuivant est plus lent.** Le labyrinthe final est « entièrement tressé »
   (zéro impasse ; le croquis d'Iwatani en avait, elles ont été supprimées) ; murs d'au moins 2 cases d'épaisseur. Au
   niveau 1 Pac-Man va à 80 % et les fantômes à **75 %** ; les fantômes tombent à **40 %** dans les tunnels ; Pac-Man
   perd 1 frame par point mangé (collecter ralentit). Rythme imposé : scatter 7 s, chase 20 s, scatter 7 s, chase 20 s,
   scatter 5 s, puis chase. Deux zones où les fantômes ne peuvent pas tourner vers le haut (Pac-Man Dossier, TCRF).
7. **Alien Isolation : des boucles de toutes tailles, avec vue dans les deux sens.** La designer Catherine Woolley décrit
   des couloirs reliés par des conduits, des passages souterrains et d'autres couloirs ; « certaines boucles sont grandes et
   dangereuses, certaines ont des impasses, certaines sont très petites pour les situations délicates » ; les routes en
   boucle donnent des lignes de vue au joueur **et** à l'alien (PC Gamer, making of).
8. **Stealth et horreur : les plans d'architecte produisent des impasses, il faut les casser.** Un designer d'infiltration
   : « en stealth on veut de belles boucles pour circuler » ; Outlast 2 guide la fuite par la **lumière** (lune sur le
   sentier, lanterne), par la **densité d'obstacles** (le joueur évite le fouillis et file vers le dégagé) et rythme la
   poursuite en « beats » (laisser croire qu'on s'en sort, puis ralentir : trébuchement, passage qui s'effondre) ;
   une poursuite trop longue désensibilise (Game Developer).
9. **Génération cyclique plutôt qu'arborescente (Unexplored).** Partir d'un **cycle** à deux arcs entre départ et but,
   déclinés en court/long, sûr/dangereux, verrou/clé ; cycles imbriqués (« quelques-uns » par donjon) ; les générateurs
   classiques font des arbres pleins d'impasses (Game Developer). Lethal Company (DunGen) est l'exemple inverse : surtout
   des branches et des impasses ; les moddeurs y créent des boucles en forçant les tuiles sur une grille.

Sources :
- Distances (DbD wiki) : https://deadbydaylight.wiki.gg/wiki/Distances
- Maze Tiles (DbD wiki) : https://deadbydaylight.wiki.gg/wiki/Maze_Tiles
- Movement Speeds (DbD wiki) : https://deadbydaylight.wiki.gg/wiki/Movement_Speeds
- Pallets (DbD wiki) : https://deadbydaylight.wiki.gg/wiki/Pallets
- The making of Alien: Isolation (PC Gamer) : https://www.pcgamer.com/the-making-of-alien-isolation
- Windows (DbD wiki) : https://deadbydaylight.wiki.gg/wiki/Windows
- The Pac-Man Dossier : https://www.gamedeveloper.com/design/the-pac-man-dossier
- Prerelease: Pac-Man (TCRF, croquis d'Iwatani) : https://tcrf.net/Prerelease:Pac-Man_(Arcade)
- From pen and paper to blockouts (stealth) : https://gamedeveloper.com/design/from-pen-and-paper-to-blockouts-how-level-designers-craft-iconic-stealthy-encounters
- The art of the chase: Outlast 2 : https://gamedeveloper.com/audio/the-art-of-the-chase-level-design-and-player-orientation-in-i-outlast-2-i-
- Unexplored's secret: cyclic dungeon generation : https://gamedeveloper.com/design/unexplored-s-secret-cyclic-dungeon-generation-

### À appliquer dans ItHunts

1. **Rapport de vitesse de poursuite ≈ 1,05 à 1,15 sur la course nue, perdant face au mouvement.** DbD : ×1,15 ; Pac-Man :
   ×0,94. Aujourd'hui : traqueur 9 m/s contre course 8,5 m/s (**×1,06**) et 11 m/s enragé (×1,29). Garder 9 m/s en
   poursuite normale ; en enragé, viser **×1,2 max** sur la course (≈ 10,2 m/s) *(reco)*, les pointes (16 / 18 m/s) restant
   le « lunge » : courtes et annoncées. Un joueur à 13 m par saut de bunny hop (20 m/s) doit toujours semer un traqueur
   non enragé en ligne droite ; le danger doit venir des coins, pas de la vitesse.
2. **Zéro impasse dans les salles de combat ; impasses autorisées seulement dans les pièces annexes (12 × 12 m) et
   signalées.** Comme Pac-Man : le générateur vérifie que chaque salle et chaque carrefour a **≥ 2 sorties praticables**
   (porte, passage en hauteur 3 × 3 m à 4,5 m, conduit) et qu'aucune pièce annexe ne dépasse 15 m de profondeur.
   Cible *(reco)* : ≥ 95 % des nœuds du graphe de l'acte dans un cycle ; les impasses restantes portent une lueur rouge
   terne au seuil (code couleur « refus » de l'ART-HANDOFF).
3. **Une « tuile de boucle » par salle, dimensionnée sur le traqueur.** Obstacle central infranchissable pour lui
   (bloc, escalier fermé) de **8 à 12 m de côté**, boucle de **35 à 50 m** *(reco)* : à 9 m/s il met 4 à 5,5 s par tour, le
   joueur à 8,5 m/s perd 0,2 à 0,3 m/s en course nue et **gagne** dès qu'il glisse ou saute. L'obstacle coupe la ligne de
   vue (≥ 4 m de haut) et casse la portée de main (15 m) : pas de boucle autour d'un simple pilier de 2 m.
4. **Ressources de fuite comptées et espacées comme les palettes.** Par salle : **1 à 2 passages étroits ou bas** (le seul
   abri face au traqueur, D-035) espacés de **≥ 16 m**, jamais 0 dans une salle de combat, jamais plus de 2 ; un passage
   bas utilisé **3 fois dans la même poursuite se ferme 30 s** (herse, éboulis lumineux) *(reco, analogue au blocage
   d'Entité)* pour forcer à changer de boucle.
5. **Boucles à deux arcs typés (Unexplored).** Chaque cycle du générateur est étiqueté : arc A court et exposé (couloir
   droit où le traqueur circule), arc B long et abrité (passages en hauteur, conduits 8 × 3,5 × 3 m, pièces annexes
   traversantes) ; le vérificateur contrôle que l'arc B est inaccessible au traqueur sur **≥ 40 % de sa longueur** *(reco)*.
6. **Guider la fuite par la lumière et le vide, pas par des flèches.** La sortie de la boucle la plus sûre est la plus
   éclairée (règle de la nuit 3) ; le sol de l'arc rapide est **dégagé** (pas de caisses sur la route de bunny hop, les
   caisses sont sur les bords) ; les zones encombrées sont les zones où l'on *ne doit pas* fuir, et c'est lisible à
   pleine vitesse.

---

## 2. Verticalité et boucles dans les arènes de mouvement FPS

### Ce que font les meilleurs

1. **Des métriques écrites, un « zoo de métriques » et des sauts conçus plus courts que le maximum.** Level Design Book
   : joueur Unity 1,0 × 1,8 m, yeux 1,5 à 1,7 m, couloir mini 2 m (« au moins le double de la largeur du joueur, et c'est
   inconfortable »), porte 1,25 × 2,5 m, marche 0,1 à 0,15 m ; les portes modernes sont plus étroites que les couloirs ;
   palier tous les 12 à 16 marches ; pente 30-35°. Conseils : texture damier pour juger les distances, un niveau de test
   avec portes, couloirs, escaliers et plateformes à toutes les hauteurs, un outil de prévisualisation du saut dans
   l'éditeur (Psychonauts 2) ; « construire à l'échelle du joueur, pas à l'échelle réelle ».
2. **Quake, le modèle des arènes rapides.** Joueur 32 × 32 × 56 u, yeux 46 u, course 320 u/s, saut 43 u de haut et 244 u de
   long, marche 18 u ; couloir normal **≥ 128 u** (4 largeurs de joueur), plafond normal ≥ 128 u, haut ≥ 256 u ; petite
   pièce 256 u, moyenne 512, grande 1024 ; **monstres : prévoir au moins le double de leur taille** (un Ogre de 64 × 88
   demande 128 de large et 176 de haut) ; blockout sur grille de 64 ou 32 puis 16 ou 8 ; « les sauts spéciaux ne vont
   jamais sur le chemin critique, seulement vers des raccourcis ou des secrets » ; concevoir pour des sauts plus courts
   que le maximum (Level Design Book, Quake ; Jokey's guide : 225 u de saut, 42,5 de haut, 275 de chute sûre).
3. **Source / GoldSrc : les repères des cartes compétitives.** Porte 48 × 108 u, couloir 64 u, mur 128 u ; en pouces
   « mais pas vraiment » : seule la cohérence interne compte (World of Level Design).
4. **Titanfall : le mur est une route, pas un décor.** Wall run limité à **1,75 s** (3,5 s avec le kit), vitesse ≈ double
   du sprint ; les cartes multijoueur ont des voies larges et des plafonds hauts pour les titans, et des toits et couloirs
   tortueux pour les pilotes ; des marquages subtils signalent les murs courables (wiki Titanfall, essais de level design).
5. **Doom Eternal : la verticalité est faite d'outils, pas de hauteur.** Barres de singe + double saut dès les premières
   arènes, jump pads « utilisés sans retenue », escalade sur surfaces texturées ; Hugo Martin refuse les « petits coins où
   le joueur démonte les démons sans qu'ils puissent l'atteindre » ; les secrets restent « à portée de main » du chemin
   critique (PlayStation Blog, GamesRadar, Game Developer).
6. **Deadlock et Ultrakill : la verticalité se paie en temps de rotation, et l'élan doit traverser les techniques.**
   Deadlock est passé de 4 à 3 voies (février 2025) : « plus de verticalité = plus de façons de bouger mais plus de temps
   pour répondre ailleurs », avec un mode « Explorer la carte » pour apprendre les routes (PC Gamer). Ultrakill : le saut
   depuis un dash garde la vitesse du dash, enchaîné en glissade puis « slide-hop » ; puits et structures qu'on escalade
   entièrement (TV Tropes, Game Developer).

Sources :
- Metrics (The Level Design Book) : https://book.leveldesignbook.com/process/blockout/metrics
- Hammer / Source player scale (World of Level Design) : https://www.worldofleveldesign.com/categories/sourcesdk-authoringtools/hammer-source-player-scale-world-dimensions.php
- Quake metrics (The Level Design Book) : https://book.leveldesignbook.com/process/blockout/metrics/quake
- Wall Running (Titanfall wiki) : https://titanfall.wiki.gg/wiki/Wall_Running
- How Doom Eternal aims to be a bigger, better sequel (PS Blog) : https://blog.playstation.com/?p=220711
- Deadlock map overhaul 4 → 3 lanes (PC Gamer) : https://pcgamer.com/games/moba/deadlock-gets-a-massive-map-overhaul-that-shrinks-its-map-from-four-lanes-to-three-this-has-a-large-range-of-accompanying-map-wide-changes

### À appliquer dans ItHunts

1. **Une fiche de métriques figée dans `ART-HANDOFF.md` § 2-3, et un « zoo » dans `Test_Movement`.** Les valeurs existent
   (capsule 1,8 × 0,8 m, saut 1,3 m, hissage 3,7 / 5,3 m, bunny hop 13 m par saut) ; ajouter les **métriques de
   conception** (plus courtes que le maximum, règle Quake) : trou franchissable garanti **4,5 m** (max 5,5), rebord
   atteignable garanti **3,2 m** avec un saut et **4,8 m** avec double saut (max 3,7 / 5,3), écart de wall jump garanti
   **≤ 8 m** (testé jusqu'à 10) *(reco)*. Tout module du kit est vérifié contre ces valeurs, pas contre les maxima.
2. **Règle du « double de la taille du monstre » appliquée au traqueur et aux goules.** Quake demande 2× la taille pour
   qu'un monstre circule. Traqueur Ø 7 × 14 m (baissé 9 m) : les volumes où il doit passer font **≥ 14 m de large et
   ≥ 18 m sous plafond** (les salles simples à 18 m sont au seuil, les couloirs à 15 m l'obligent à se baisser : c'est
   voulu, mais à noter dans le kit comme « couloir lent pour lui ») ; goule Ø 1,1 m : ouvertures de goule ≥ 2,2 m.
3. **Couloirs ≥ 4 largeurs de joueur, portes plus étroites que les couloirs.** Quake 128 u pour 32 de joueur : couloir
   normal **≥ 3,2 m** (ART-HANDOFF dit déjà « confortable à pleine vitesse ≥ 3 m »), couloir de bunny hop **≥ 5 m** et
   **≥ 50 m de long** ; porte de pièce 3 × 3 m dans un couloir d'au moins 4 m, jamais une porte aussi large que son
   couloir (lisibilité du seuil).
4. **Étages à hauteur de hissage, et jamais de « coin sûr ».** Niveau haut à +16 m aujourd'hui (atteignable seulement par
   traction) ; ajouter une **mezzanine intermédiaire à +4,5 ou +5 m** (hissage avec double saut) dans chaque salle à
   étages ; règle Doom Eternal : aucune plateforme où le joueur est hors de portée de **toutes** les menaces plus de 5 s
   *(reco)* : les goules grimpent (7 m/s), les volants piquent, la main du traqueur monte à 12 + 15 m.
5. **Le mur est une route lisible.** Comme Titanfall : les murs de wall slide (≥ 6 m de haut) portent un marquage discret
   constant (bande de teinte du magnétisme ou lueur basse) et sont **séparés de 3 à 8 m** des murs qui leur font face ;
   un mur courable isolé sans mur en face est un mur de glissade descendante, pas de zigzag.
6. **Props à l'échelle obligatoires dans chaque module de greybox** : 1 porte 3 × 3 m, 1 caisse 0,9 m, 1 goule factice
   1,8 m, 1 gabarit plat de traqueur 7 × 14 m, 1 damier de 1 m au sol. Un module sans ces repères n'est pas validé.

---

## 3. Roguelites : améliorations, rangs, raretés et économie

### Ce que font les meilleurs

1. **Risk of Rain 2 : prix et récompenses indexés sur une même variable.** Coût d'un coffre `= base × coeff^1,25` (petit
   coffre base 25 $ en solo première étape ; valeurs affichées 30 / 50 pour petit / grand) ; or par kill `= 2 × coeff ×
   valeur du monstre` ; `coeff = (1 + 0,3·(joueurs−1) + minutes × 0,0506 × difficulté × joueurs^0,2) × 1,15^étapes`.
   Les prix ne changent **qu'au changement d'étape** ; en Rainstorm solo, après 20 min et 2 étapes : coeff ≈ 3,9, un petit
   coffre coûte ≈ 5,5× son prix initial (wiki RoR2, Difficulty ; guide Nerdlodge).
2. **Hades : deux axes séparés, rareté et niveau, à rendement décroissant.** Rareté Commun / Rare / Épique / Héroïque
   avec multiplicateurs communautaires ≈ **×1 / ×1,5 / ×2 / ×2,5** (variable selon le bienfait) ; le Pom of Power monte
   d'un niveau : exemple Passion Dash 20 → 36 → 48 (le 2e Pom donne moins que le 1er) ; Pom à **100 oboles** chez Charon
   (300 au Styx) ; Légendaires, Duos, Hermès et Chaos **ne se pomment pas** (Fextralife, gameplay.tips : chiffres
   communautaires, formule non publiée).
3. **Dead Cells : prix par niveau d'objet, plus par profondeur.** La 1.9 a supprimé la mise à l'échelle de l'or par biome
   (« bien plus facile de régler l'or généré par niveau ») ; **relance de boutique +40 % du prix normal à chaque fois**,
   jusqu'à 5 relances ; nourriture mineure 2 000, majeure 4 000, recharge de fiole 20 000, prix identiques partout ;
   **3 emplacements de mutation**, réinitialisation à 1 000 or, doublée à chaque fois, plafond 8 000 (wiki Dead Cells,
   patch notes 1.9).
4. **Megabonk (2025) : 85 objets, prix de coffre qui montent à chaque ouverture, choix passables.** 22 communs / 21 rares /
   20 épiques / 22 légendaires ; le prix du coffre augmente à **chaque coffre ouvert** (l'objet Credit Card ajoute +10 %
   par coffre contre un bonus) ; la Clé donne 9,1 % de coffre gratuit, **10 clés = 50 %** (hyperbolique) ; on peut
   **passer** un choix et **bannir** un objet ; PV des boss de ~8 000 à 30 000+ sur un même palier selon le moment, et
   500 000 au palier 2 : la pression vient du temps, pas des chiffres de départ (bloxodes, Rogueranker, guides tiers).
5. **Ravenswatch : un talent par niveau, 58 objets magiques en 5 raretés, pas de slot d'objet.** 1 talent choisi à chaque
   montée de niveau parmi **2** (les joueurs demandent 3 au rang 8), plus de 200 talents ; objets 10 / 10 / 10 / 14 / 14
   (commun, rare, épique, légendaire, maudit), bonus de collection à **5 / 4 / 3 exemplaires** ; puits utilisables 3 fois
   ; légendaires via quêtes, maudits via autels de boss ; 3 cycles jour/nuit par chapitre ; le rang du héros (2 à 5)
   débloque des talents pour les runs suivantes (bloxodes, kosgames ; la refonte « Kintaro » peut avoir changé ces chiffres).

Sources :
- Difficulty (RoR2 wiki, formules) : https://riskofrain2.wiki.gg/wiki/Difficulty
- Pom of Power (Fextralife) : https://hades.wiki.fextralife.com/Pom+of+Power
- Boons (Fextralife, multiplicateurs) : https://hades.wiki.fextralife.com/Boons
- Pom Power: how Poms and boon levels work : https://gameplay.tips/guides/hades-pom-power-how-poms-and-boon-levels-work.html
- Shops (Dead Cells wiki) : https://deadcells.wiki.gg/wiki/Shops
- Update of Plenty 1.9 (Dead Cells) : https://dead-cells.com/patchnotes/19
- Megabonk items (bloxodes) : https://bloxodes.com/wiki/megabonk/items
- Megabonk Key (Rogueranker) : https://rogueranker.com/megabonk-key/
- Ravenswatch magical objects (bloxodes) : https://bloxodes.com/wiki/ravenswatch/magical-objects
- Ravenswatch all heroes guide (kosgames) : https://kosgames.com/ravenswatch-all-heroes-guide-upgrade-unlock-and-effects-40258/

### À appliquer dans ItHunts

1. **Indexer prix et gains d'âmes sur un seul `coeff` d'acte, pas sur des hausses fixes** (aujourd'hui `chestPriceIncrease
   0`, `merchantPriceIncrease 25`). *(reco)* : `coeff = (1 + 0,3·(joueurs−1) + minutes × 0,05) × 1,15^actes` ; prix `= base
   × coeff^1,25` recalculé **au changement d'acte seulement** (lisible, comme RoR2) ; âmes par goule `= 2 × coeff ×
   valeur`. Après 20 min : coeff ≈ 2,0 → coffre 30 → ≈ 70 âmes à l'acte 2, 110 à l'acte 3 ; les gains doublent aussi.
   À régler avec le ×2 Enragé existant.
2. **Budget d'améliorations par run : 9 à 12, soit 3 à 4 par acte.** Hades donne ~10-14 bienfaits par run, Dead Cells 3
   mutations + 2 armes + 2 compétences, Ravenswatch ~10-12 talents. Cible *(reco)* : **1 coffre + 1 marchand garanti par
   acte**, 1 à 2 coffres optionnels derrière une salle verrouillée (50 âmes) ou un détour risqué ; 8 améliorations
   existent aujourd'hui : le catalogue doit passer à **24 minimum** (3 choix × 8 tirages sans doublon) avant la fin de
   l'étape 8.
3. **Deux axes : rareté (×1 / ×1,5 / ×2) et rang (Pom).** Garder les multiplicateurs de la nuit 3 pour la rareté ; ajouter
   un **« Pom »** vendu au marchand (**100 âmes ≈ 3 coffres**, 1 par visite) qui monte d'un rang une amélioration possédée
   avec rendement décroissant : rang 2 = +80 % de la valeur de base, rang 3 = +60 %, rang 4 = +40 %, plafond rang 5
   *(reco)*. Les améliorations « comportement » (ricochet, paralysie) et les duos **ne se pomment pas**.
4. **Un seul passif « signature » par run, les autres s'empilent.** Comme les 3 mutations de Dead Cells et l'artefact unique
   (D-064) : **1 emplacement « signature »** (effet qui change la règle : la traction laisse une traînée, la caisse revient)
   en plus de l'artefact ; remplacer la signature coûte **1 000 âmes puis ×2** plafonné à 8 000 (Dead Cells) : on
   change rarement, mais c'est possible. Les améliorations chiffrées n'ont pas de limite d'emplacements.
5. **Relance payante en pourcentage, passer et bannir gratuits.** Relance des 3 choix d'un coffre : **+40 % du prix du
   coffre** par relance (Dead Cells) et non ×1,5 cumulé ; **passer** un choix (rien) et **bannir** une amélioration pour la
   run (Megabonk) gratuits : ces deux boutons évitent que le joueur se sente volé par un tirage faible.
6. **Les chances de coffre gratuit et tout « % » restent hyperboliques.** Megabonk : 1 clé 9,1 %, 10 clés 50 % → c'est
   `p = n/(n+10)`. L'amélioration « aspiration » ou une future « chance de coffre gratuit » suit la formule de la nuit 3
   (`1 − 1/(1 + k·n)`), jamais une addition.

---

## 4. Scaling des ennemis

### Ce que font les meilleurs

1. **Risk of Rain 2 : une seule variable, exponentielle par étape, linéaire dans le temps.** `coeff` ci-dessus ; niveau des
   ennemis `= 1 + (coeff − facteurJoueurs)/0,33` ; par niveau **+30 % de PV et +20 % de dégâts** ; boss spéciaux PV
   `× (1 + coeff/2,5) × joueursVivants`, dégâts `× (1 + coeff/30)` ; Drizzle = temps à 50 %, Monsoon = temps à 150 % ;
   crédits de spawn du directeur linéaires en `coeff`, élites à **6×** le coût (Blazing, Overloading, Glacial) ou **36×**
   (Malachite, Celestine) ; un groupe trop bon marché (sous 36× ou 216× le coût de base) est rejeté : le jeu préfère peu
   d'ennemis chers à une marée d'ennemis faibles (wiki RoR2, Difficulty).
2. **Vampire Survivors : une vague cachée par minute, un quota et un plafond.** Chaque vague (1/min) fixe un **minimum
   d'ennemis vivants** et un **intervalle de spawn** ; sous le minimum, le jeu remplit jusqu'au quota ; au-delà de **300
   vivants** plus aucun spawn périodique ; à chaque boucle de niveau : **PV +100 %, fréquence et quantité +50 %, dégâts
   +25 %** ; la malédiction (Curse) ne s'applique qu'**à la minute suivante** (wiki VS).
3. **Megabonk : la pression vient du chrono, le « Difficulty » est une seconde manette.** PV et dégâts montent avec la
   durée de la run et le palier ; la stat Difficulty, choisie par le joueur, fait **spawner plus et plus vite** mais
   rapporte plus d'XP et d'or ; les paliers sautent (boss ×40 d'un palier à l'autre) plutôt que de glisser (guides tiers,
   chiffres non officiels).
4. **Left 4 Dead : le Director mesure une « intensité » et impose du repos.** Slides GDC 2009 de Mike Booth : l'intensité
   d'un survivant **monte** quand il est blessé (proportionnel aux dégâts), incapacité, tiré d'un rebord, ou engagé par
   des infectés ; elle **décroît avec le temps** sans engagement ; le Director suit le **max des 4**. Cycle : **Build Up**
   (population pleine jusqu'au seuil de pic) → **Sustain Peak** (**3 à 5 s** après le pic) → **Peak Fade** (population
   minimale jusqu'à ce que l'intensité ressorte de la zone de pic, « pour que le combat en cours finisse sans manger le
   repos ») → **Relax** (**30 à 45 s**, ou jusqu'à ce que l'équipe ait assez avancé vers l'abri) → Build Up. Relax = pas de
   hordes ni de spéciaux. Hordes toutes les **90 à 180 s** (Normal), jamais en Relax, créées **derrière** les survivants,
   hors de vue ; taille de horde croissante après un spawn ; boss (Tank, Witch, rien) tirés **sans répétition** tous les
   N mètres et **hors pacing** (PDF Valve GDC 2009 ; wiki Valve : `SustainPeakMinTime`, `RelaxMinInterval`, `MobSpawnMinTime`).

Sources :
- Difficulty (RoR2 wiki) : https://riskofrain2.wiki.gg/wiki/Difficulty
- Stages (Vampire Survivors wiki, vagues et plafond) : https://vampire-survivors.fandom.com/wiki/Stages
- The AI Systems of Left 4 Dead (Mike Booth, GDC 2009, PDF) : https://cdn.fastly.steamstatic.com/apps/valve/2009/ai_systems_of_l4d_mike_booth.pdf
- Director Scripts (Valve wiki) : https://developer.valvesoftware.com/wiki/Left_4_Dead_2/Scripting/Director_Scripts

### À appliquer dans ItHunts

1. **Même `coeff` pour les ennemis que pour l'économie (§ 3).** `Run_Default` a déjà 3 difficultés (PV ×1 / 1,4 / 1,8,
   dégâts ×1 / 1,25 / 1,5, vitesse ×1 / 1,1 / 1,2) : les garder comme **multiplicateurs de départ**, et ajouter le
   niveau `= 1 + (coeff − 1)/0,33` avec **+20 % PV et +10 % dégâts par niveau** *(reco, moins que RoR2 : on tue avec le
   décor, les PV de goule doivent rester « 1 caisse de 25 kg »)*. Jamais de vitesse scalée par le temps : la goule à
   10 m/s est déjà au-dessus de la course.
2. **Vagues « quota + intervalle », plafond dur.** La fournée de début de pic (D-060) devient `minimumVivants` et
   `intervalle` par salle et par acte : acte 1 **6 goules / 2 volants**, acte 2 **8 / 3**, acte 3 **10 / 4**, remplis dès
   qu'on passe sous le quota, **plafond 16 ennemis vivants par joueur** *(reco)* ; comme VS, tout changement de quota ne
   s'applique qu'au **pic suivant**, jamais au milieu d'un combat.
3. **Un directeur d'intensité par joueur, avec le cycle de L4D.** Intensité 0-100 : +1 par PV perdu, +30 si à terre,
   +15/s avec le traqueur à moins de 13 m, décroissance **−10/s** sans ennemi engagé *(reco)*. Cycle : Build Up jusqu'à 80
   → Sustain Peak **4 s** → Peak Fade → Relax **30 à 45 s** sans aucun spawn (les goules restantes finissent le combat).
   Le traqueur **n'est pas** soumis au cycle (comme le Tank), hors le détour de 8 à 12 s de la nuit 3.
4. **Spawn derrière et hors de vue, jamais devant.** Comme le Director : les goules apparaissent dans des salles **non
   visibles** et de préférence **derrière** le joueur par rapport à sa direction de déplacement (déjà : hors de vue,
   pas de salle visitée depuis 60 s) ; distance **25 à 50 m** *(reco)* pour qu'elles arrivent à 10 m/s en 2,5 à 5 s,
   c'est-à-dire pendant la fenêtre où le joueur attrape une caisse.
5. **Élites rares et chères, pas des marées.** Si des goules d'élite arrivent (étape 8+), elles coûtent **6× une goule** au
   budget de la vague et une vague ne peut pas en contenir plus d'une sous l'acte 3 ; une vague qui ne peut payer
   qu'une goule est annulée (règle RoR2 du groupe « trop bon marché »), ce qui garantit des combats denses ou rien.
6. **Boucle d'acte après le timer (Enragé) = boucle de niveau VS.** Au passage en Enragé : **PV +100 %, quantité +50 %,
   dégâts +25 %** pour les goules, âmes ×2 (déjà) ; une seule marche, annoncée, pas une rampe continue : le joueur sait
   exactement ce qu'il paie pour rester.

---

## 5. Combats de boss exigeants et lisibles

### Ce que font les meilleurs

1. **FromSoftware : le temps de réaction fixe le plancher ; la parade prend 6-8 frames à sortir.** Réaction visuelle
   180-200 ms, auditive 140-160 ms (revue Clemson citée par un mémoire) ; Elden Ring 1.17 : bouclier moyen 8 frames de
   démarrage + 12 actives, boucler 6 + 12, épée courbe 6 + 8 ; les joueurs distinguent le **télégraphe** (« esquive
   maintenant ») du **retard excessif** (« trop tôt est puni »). Sekiro : attaques « périlleuses » **imparables**,
   annoncées par un kanji rouge, **4 types** (estoc, balayage, saisie, foudre) à réponse unique ; critiques : le kanji
   cache le corps des petits boss et apparaît parfois **après** le début de l'élan (Steam guide, forums, Fextralife).
2. **Hades I lisible, Hades II critiqué : la règle est « le marquage = la hitbox ».** Hades I : mêlée autour du boss,
   distance vers l'extérieur, AoE rares (Thésée) ; Mégère « peint des cercles au sol qui explosent ». Hades II : onde de
   Cerbère qui touche **derrière lui** alors que le signal est frontal, écrasement de Polyphème en demi-cercle de 180°
   (esquive latérale inutile), lignes de visée qui tournent **plus vite** que l'attaque, cercle masqué par la poussière
   et visible ~1 s avant, « imparable » signalé seulement après l'échec, 5 à 6 animations d'AoE superposées (nex-3.com ;
   joueurs demandant une option « zone de frappe surlignée », Steam).
3. **FFXIV : le vocabulaire des marquages au sol et le « snapshot ».** Catégories : cercle, donut, cône, ligne, flaque,
   mine, AoE à tête chercheuse, regroupement (stack), dispersion (spread), proximité, flare, regard (gaze), lumières
   rouge/verte ; **les dégâts sont figés au moment où la barre d'incantation finit ou où le cercle disparaît**, pas au
   moment de l'animation : le joueur réagit à la disparition du marquage (consolegameswiki).
4. **Code couleur par réponse attendue.** Saros (Housemarque) : balles **bleues absorbables**, **rouges parables** ; Returnal :
   « bullet ballet », attaques organiques en spores, « engagement envers la simplicité » ; Furi : halo **rose** = onde à
   esquiver vers l'ennemi ou boule parable, hologrammes **rouges** à esquiver ; phases à barre bleue (distance) et rouge
   (duel au contact) (Creative Bloq, PS Blog, guides Furi).
5. **Une table d'attaques par boss, une seule forme principale par attaque.** Consigner forme, famille de couleur, durée
   d'annonce en ms et réponse attendue ; **réduire les effets secondaires pendant l'élan** ; « si le joueur esquive le
   signal visible mais prend un second coup non annoncé, le contrat est rompu » (bugnet.io).

Sources :
- Parry frame data 1.17.1 (Steam guide) : https://steamcommunity.com/sharedfiles/filedetails/?id=3360984277
- Combat / perilous attacks (Sekiro Fextralife) : https://sekiroshadowsdietwice.wiki.fextralife.com/Combat
- Hades II has a legibility problem : https://nex-3.com/blog/hades-ii-has-a-legibility-problem/
- Common mechanics and markers (FFXIV) : https://ffxiv.consolegameswiki.com/wiki/Common_mechanics_and_markers
- Snapshot (FFXIV) : https://ffxiv.consolegameswiki.com/wiki/Snapshot
- Modern games have a readability problem, Saros (Creative Bloq) : https://www.creativebloq.com/3d/video-game-design/modern-games-have-a-readability-problem-saros-shows-how-to-fix-it
- Returnal: the making of the Hyperion fight (PS Blog) : https://blog.playstation.com/?p=349605
- Furi general tips (couleurs) : https://www.gamepressure.com/furi/general-tips/zf9033
- How to design enemy attack telegraphs : https://bugnet.io/blog/how-to-design-enemy-attack-telegraphs

### À appliquer dans ItHunts

1. **Marquage au sol = hitbox exacte, et le coup tombe quand le marquage disparaît.** Toute attaque de zone du boss
   (onde, écrasement, charge) projette au sol **la forme réelle** du volume de dégâts (décal URP, pas un cercle
   générique) ; la zone **ne suit plus** le boss dans les derniers **300 ms** *(reco)* ; les dégâts sont appliqués à la
   frame où le décal s'éteint (snapshot FFXIV) et jamais après, même si l'animation continue.
2. **Plancher d'annonce 450 ms (nuit 3) justifié : 200 ms de réaction + 250 ms d'action.** Réaction visuelle 180-200 ms
   + dash 300 ms ou saut : l'attaque la plus rapide reste **≥ 450 ms**, et toute attaque **mortelle en un coup** (prise)
   garde ≥ 1 000 ms. Vérification automatique en test EditMode : chaque `AttackDefinition` a `telegraphMs ≥ 450` et
   `oneHitKill ⇒ telegraphMs ≥ 1000`.
3. **Trois couleurs, trois réponses, jamais plus.** Code déjà dans l'ART-HANDOFF : **rouge = attaque à esquiver**,
   **vert = parable / ouverture**, et ajouter **bleu = aimantable** (surface magnétique sur le corps du boss, nuit 3) ;
   aucune attaque ne mélange deux couleurs ; une attaque **imparable** est rouge dès son élan, pas après l'échec (leçon
   Sekiro et Hades II). Comme le kanji de Sekiro, le signal est **sur la partie qui frappe**, jamais au-dessus de la
   tête où il cacherait le corps.
4. **Une seule forme par attaque, et un panel de 4 à 6 attaques, pas 12.** Table d'attaques du boss (forme, couleur, ms,
   réponse) dans `docs/design/features/boss-traqueur/` ; pendant l'élan, **couper les particules secondaires** et la
   poussière à moins de 3 m du marquage (Polyphème) ; au plus **2 marquages au sol actifs en même temps** *(reco)*.
5. **Les lignes de visée tournent au plus aussi vite que l'attaque.** Pour la charge et le balayage : vitesse de rotation
   du marquage **≤ vitesse de rotation réelle** du boss (faceLookSpeed 3 aujourd'hui), figée dans les derniers 300 ms ;
   une attaque frontale ne touche **jamais** derrière le boss ; un demi-cercle fait 120° maximum pour qu'une esquive
   latérale de dash (6,5 m) sorte toujours.
6. **Son d'annonce unique et plus tôt que l'image.** Réaction auditive 140-160 ms contre 180-200 visuelle : le son part
   **50 ms avant** le décal, 1 son par attaque, en 3D ; en Enragé les annonces × 0,8 **ne raccourcissent pas** le son.

---

## 6. Screamer et mise en scène de la mort

### Ce que font les meilleurs

1. **Dead by Daylight : la mise à mort (mori) dure 9 à 14 s, à la troisième personne, unique par tueur.** Chiffres de
   joueurs (Nurse ≈ 14 s, Wraith ≈ 9 s) ; les joueurs la comparent aux slashers et font du « photobombing » : c'est un
   spectacle partagé, pensé pour un public (forums BHVR, mein-mmo).
2. **Alien Isolation : mort en un coup, vue à la première personne, plusieurs variantes.** « Être attrapé, c'est être tiré
   dans une représentation à la première personne de sa mort » ; au moins deux animations (saisie puis morsure ; l'alien
   grimpe sur la victime) ; la mort est « la récompense » du monstre qui s'approche en silence (PC Gamer, Destructoid).
3. **Phasmophobia et Lethal Company : pas de spectacle, un rôle après la mort.** Phasmophobia : « l'animation de mort
   n'offre aucune expérience graphique particulière », le mort voit en brouillard, reste sur la carte, peut lancer des
   objets, perd son matériel (wiki). Lethal Company : « un jeu où l'on rit de la mort » (Zeekerss) ; le Bracken brise la
   nuque quand on ne regarde pas ; **mode arachnophobie** : les araignées deviennent le mot « SPIDER » (PC Gamer, Automaton).
4. **Accessibilité : réduire sans supprimer.** Alan Wake 2 : réglage « horror flashes » **Low / Normal** (visuel et audio),
   pas de suppression totale ; Dead Space (remake) : **avertissements de contenu** avant les moments durs ; Little
   Nightmares III : secousse caméra et vibration désactivables ; DbD : **Heartbeat Visual Support** (cœur dessiné sur la
   poitrine quand le rayon de terreur est audible), désactivé par défaut, « aucun avantage, il ne montre que ce qu'on
   entendrait », modes daltoniens, FOV du dernier survivant désactivable (PC Gamer, Bandai Namco, PCGamesN).

Sources :
- Mori durations (forums BHVR) : https://forums.bhvr.com/dead-by-daylight/discussion/comment/1056258
- Alien: Isolation preview, first-person death (PC Gamer) : https://www.pcgamer.com/alien-isolation-preview-hands-on-with-creative-assemblys-ambitious-sci-fi-horror/2
- Death (Phasmophobia wiki) : https://phasmophobia.fandom.com/wiki/Death
- Lethal Company humour (PC Gamer) : https://www.pcgamer.com/lethal-companys-co-op-horror-comedy-is-like-phasmophobia-but-immediately-more-chaotic-and-deadly
- Arachnophobia mode (Automaton, jp) : https://automaton-media.com/articles/newsjp/20231212-275785/
- Alan Wake 2 anti-jumpscare toggle (PC Gamer) : https://www.pcgamer.com/alan-wake-2s-latest-patch-could-be-a-sign-of-more-anti-jumpscare-toggles-in-horror-games-to-comeand-im-all-for-it
- Little Nightmares III accessibility : https://en.bandainamcoent.eu/little-nightmares/news/new-and-improved-accessibility-features-little-nightmares-iii
- DbD Visual Terror Radius (PCGamesN) : https://www.pcgamesn.com/dead-by-daylight/terror-radius-update-bhvr-horror-game

### À appliquer dans ItHunts

1. **Mort par le traqueur : 2,5 à 4 s à la première personne, puis l'écran de fin.** Plus court qu'un mori (9-14 s, pensé
   pour un public), dans l'esprit d'Alien (vue FPS, une frappe) : **0,3 s** de flash et de coupure du son (le screamer :
   visage à 1 m, son < 80 Hz + cri), **2 à 3 s** de prise animée, fondu noir, écran de fin **< 10 s** après la mort (nuit 3).
   **Passable** d'un bouton après 1 s *(reco)*. En coop, le joueur « à terre » (D-059) n'a pas de screamer.
2. **Deux à trois variantes selon la situation, jamais une seule.** Comme Alien : saisie frontale (main qui arrive),
   saisie par-dessus (joueur qui passe au-dessus, geste « deux mains levées » de l'ART-HANDOFF), écrasement (joueur au
   sol sous la main). Le choix dépend du vecteur main → joueur, pas du hasard, pour rester cohérent avec ce qu'on a vu.
3. **Un réglage « Intensité des frayeurs » à deux niveaux, pas un interrupteur, annoncé au premier lancement.** **Normal /
   Réduite** (Alan Wake 2) : Réduite = pas de visage plein écran, flash remplacé par un fondu de 0,5 s, cri à −12 dB,
   secousse ×0,3 ; durée et message identiques pour garder le rythme. Au premier lancement, une ligne d'avertissement
   (Dead Space) : « apparitions soudaines et sons forts ; réglable dans Options > Accessibilité », avec le lien direct.
4. **Rayon de terreur visible en option.** Comme DbD : option « Battement visuel » désactivée par défaut, qui dessine le
   cœur (les 3 couches 40 / 27 / 13 m de la nuit 3) dans le coin « traqueur » du HUD **uniquement quand le son serait
   audible**, sans avantage ; plus les options déjà citées (secousse, vibration, FOV de vitesse) dans le même onglet.
5. **La mort apprend quelque chose.** Pendant le fondu : afficher la **distance à la sortie** ou au passage bas le plus
   proche (« abri à 9 m derrière vous »), une ligne seulement ; c'est l'équivalent du « cause de la mort » de la nuit 3,
   mais spatial : le joueur comprend quelle boucle il aurait dû prendre.

---

## Récapitulatif : règle → point du brief concerné

| # | Règle (chiffrée) | Point du brief |
|---|---|---|
| 1.1 | Traqueur ×1,06 sur la course (9 m/s) ; enragé ≤ ×1,2 (≈ 10,2 m/s) ; pointes 16/18 m/s courtes et annoncées | traqueur |
| 1.2 | ≥ 2 sorties par salle et carrefour ; ≥ 95 % des nœuds dans un cycle ; impasses seulement en pièces annexes, lueur rouge au seuil | level design, portes |
| 1.3 | 1 obstacle central de 8 à 12 m par salle, boucle de 35 à 50 m, ≥ 4 m de haut, coupe la ligne de vue et la main (15 m) | level design, traqueur |
| 1.4 | 1 à 2 passages étroits ou bas par salle de combat, espacés ≥ 16 m ; fermeture 30 s après 3 passages dans la même poursuite | level design, traqueur |
| 1.5 | Cycles à deux arcs typés (court exposé / long abrité, ≥ 40 % hors de portée du traqueur), contrôlés par le vérificateur | level design, minimap |
| 1.6 | Route de fuite dégagée et éclairée ; caisses sur les bords ; fouillis = « pas par là » | level design, améliorations (objets) |
| 2.1 | Métriques de conception < maxima : trou 4,5 m, rebord 3,2 / 4,8 m, écart de wall jump ≤ 8 m ; zoo dans Test_Movement | level design, bugs |
| 2.2 | Volumes du traqueur ≥ 14 m de large, ≥ 18 m sous plafond ; ouvertures de goule ≥ 2,2 m | level design, traqueur, ennemis |
| 2.3 | Couloir ≥ 3,2 m, couloir de bunny hop ≥ 5 × 50 m ; porte toujours plus étroite que son couloir | level design, portes |
| 2.4 | Mezzanine à +4,5 / +5 m dans chaque salle à étages ; aucun coin hors d'atteinte de toutes les menaces > 5 s | level design, ennemis |
| 2.5 | Murs de wall slide marqués, face à face à 3-8 m ; mur isolé = glissade descendante | level design |
| 2.6 | Props à l'échelle obligatoires dans chaque module de greybox (porte 3 × 3, caisse 0,9, goule 1,8, gabarit 7 × 14, damier) | level design |
| 3.1 | `coeff = (1 + 0,3·(j−1) + min × 0,05) × 1,15^actes` ; prix = base × coeff^1,25 au changement d'acte ; âmes = 2 × coeff × valeur | améliorations (économie), portes |
| 3.2 | 9 à 12 améliorations par run ; 1 coffre + 1 marchand garantis par acte ; catalogue ≥ 24 | améliorations |
| 3.3 | Rareté ×1 / 1,5 / 2 + « Pom » à 100 âmes, +80 / +60 / +40 % décroissants, rang 5 max ; comportements et duos non pommables | améliorations |
| 3.4 | 1 emplacement « signature » en plus de l'artefact ; changement 1 000 âmes ×2, plafond 8 000 | améliorations |
| 3.5 | Relance +40 % du prix par relance ; passer et bannir gratuits | améliorations, bugs (ressenti de tirage) |
| 3.6 | Toute chance = hyperbolique `n/(n+10)` ou `1 − 1/(1 + k·n)` | améliorations |
| 4.1 | Niveau ennemi `= 1 + (coeff − 1)/0,33` ; +20 % PV, +10 % dégâts par niveau ; vitesse jamais scalée par le temps | ennemis |
| 4.2 | Vagues quota + intervalle (6/2, 8/3, 10/4 par acte), plafond 16 vivants par joueur, changements au pic suivant seulement | ennemis |
| 4.3 | Directeur d'intensité 0-100 : Build Up → Peak 4 s → Fade → Relax 30-45 s sans spawn ; traqueur hors cycle | ennemis, traqueur |
| 4.4 | Spawn hors de vue, derrière, à 25-50 m | ennemis, level design |
| 4.5 | Élite = 6× le budget, max 1 par vague sous l'acte 3 ; vague « trop bon marché » annulée | ennemis |
| 4.6 | Enragé = +100 % PV, +50 % quantité, +25 % dégâts, âmes ×2, une seule marche annoncée | ennemis, traqueur |
| 5.1 | Marquage au sol = hitbox exacte, figé 300 ms avant, dégâts à l'extinction du marquage | boss |
| 5.2 | Test EditMode : `telegraphMs ≥ 450`, mortel ⇒ ≥ 1 000 | boss, bugs |
| 5.3 | Rouge = esquiver, vert = parable, bleu = aimantable ; imparable rouge dès l'élan ; signal sur le membre qui frappe | boss, traqueur |
| 5.4 | 4 à 6 attaques, une forme chacune, ≤ 2 marquages actifs, particules coupées pendant l'élan | boss |
| 5.5 | Rotation du marquage ≤ rotation du boss ; frontal ne touche pas derrière ; demi-cercle ≤ 120° | boss |
| 5.6 | Son d'annonce 50 ms avant l'image, 1 par attaque, non raccourci en Enragé | boss, traqueur |
| 6.1 | Mort FPS 2,5 à 4 s (flash 0,3 s + prise 2-3 s), passable après 1 s, écran de fin < 10 s ; pas de screamer « à terre » en coop | traqueur, bugs |
| 6.2 | 2-3 variantes de prise choisies par le vecteur main → joueur | traqueur |
| 6.3 | Option « Intensité des frayeurs » Normal / Réduite (visage, flash, −12 dB, secousse ×0,3), même durée ; avertissement au premier lancement | traqueur, minimap (HUD) |
| 6.4 | « Battement visuel » optionnel dans le coin traqueur du HUD, sans avantage | minimap (HUD), traqueur |
| 6.5 | Au fondu de mort : distance à l'abri ou à la sortie la plus proche, une ligne | traqueur, minimap, portes |
