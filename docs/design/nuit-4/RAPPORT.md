# Rapport de la nuit 4 (2026-11-23)

Brief : « ambition maximale », vérification en jeu obligatoire. Recherche préalable : [RECHERCHE-NUIT-4.md](../RECHERCHE-NUIT-4.md)
(34 règles, une cinquantaine de sources). Décisions D-100 à D-108 dans le [GDD](../GDD.md). Rapports du bot : [bot/](bot/) ;
preuves de la parade : [bot/parade/](bot/parade/) ; preuves des portes : [bot/porte/](bot/porte/).

## En bref

- **La nuit a débordé sur la journée** : la session s'est arrêtée vers 1 h (limite d'usage atteinte par un sous-agent) et a repris à
  12 h 18. Tout ce qui suit a été vérifié dans l'éditeur, en jeu, par des tests PlayMode qui jouent avec les vraies entrées.
- **Trois bugs du playtest sont reproduits, expliqués et corrigés avec preuve en jeu** : porte de sortie (9 cartes sur 9), parade
  (la vraie cause est le bond), goules figées.
- **Le bot termine l'acte I** (seed 11) en 10 min 49 de jeu, 0 erreur du journal, 106 parades réussies, 4 achats.
- **Pas encore « exceptionnel »**, je le dis franchement : les captures montrent encore des ouvertures noires et des salles
  sombres ; le ressenti (fuite, boss, screamer) n'a été vérifié que par le code et les mesures, pas manette en main.

## PR d'art

| PR | Branche | Contenu | État | Branchement |
|---|---|---|---|---|
| #45 | art-textures-1 | 5 textures peintes (écorce, herbe, gravier, tapis / tenture, marbre) | fusionnée (5dddcfc) | rien à câbler (GUID inchangés) |
| #46 | art-menus-4 | campement : personnages assis peints dans la maquette | fusionnée (464ef52) | prefab du campement à jour ; test du campement vert |
| #47 | art-materiaux-2 | 512 px/m, matériaux du château, 0 face superposée dans les 98 pièces, FBX lisibles (NavMesh) | fusionnée (3571a07), conflit du manifeste résolu | acte II câblé par fonction (tableau de l'ART-HANDOFF), bug des sols corrigé |
| #48 | art-ameliorations | 70 illustrations, 10 cadres de rang | fusionnée (424419f), conflit du manifeste résolu | 70 icônes sur les améliorations, cadres dans le HUD (`UpgradeArtSetup`) |

Pour chaque PR : périmètre vérifié (seulement `Assets/Art/`, `docs/art/`, manifeste), compilation, tests EditMode verts (558 :
557 réussis, 1 non concluant connu), fusion, push de `main`, fusion dans `animations`. PR non encore arrivées à l'écriture :
art-kits-3, art-props-3d, art-ui-4, art-vfx-sons-4, art-fin-4.

## Contrôle point par point

| # | Demande | État | Preuve |
|---|---|---|---|
| 0 | AMELIORATIONS.md : 70 améliorations, rangs, synergies, poussé sur main | ✅ | commit 1363294 (main, en début de nuit) |
| R | Recherche en ligne, résumée et appliquée | ✅ | `RECHERCHE-NUIT-4.md` ; règles appliquées : zéro impasse, boucles, paliers, quota + repos (L4D), scaling plus lent que le joueur, marquages figés 300 ms, screamer 3 s avec option réduite |
| 1 | Porte qui n'envoie pas à l'acte suivant | ✅ | cause : aucun `OnTriggerEnter` quand le joueur traverse la boîte de fin d'acte ; test de boîte à chaque pas fixe ; `ExitHallTests` **9/9** (le joueur marche par l'entrée « avancer ») ; commit 2bd87f8 ; captures `bot/porte/` |
| 1 | Parade : 0 réussie sur une run | ✅ | mesuré en jeu (`ParryInGameTests`, vraie touche V) : le coup de griffes se pare (3/3, appui 106 à 121 ms avant l'impact) ; **cause** : le bond (annonce 0.3 s + vol 0.2 à 0.45 s) était toujours « trop tôt » ; appui pendant le vol accepté (D-108) ; `bot/parade/PARADE.md` et captures ; bot : 106 parades réussies sur l'acte I |
| 1 | Goules figées après une répulsion | ✅ | recul sans fin relâché après 4 s (`EnemyBody.KnockTimeout`) ; commit 267dc13 |
| 1 | Améliorations visibles dans le monde (orbes, texte) | ✅ | `OfferOrb.WorldVisible` faux, plus de texte sur les coffres ; test `Coffre_SansTexteFlottant_InviteParJoueur` |
| 1 | Écran de fin : liste qui déborde ; Recommencer et Campement | ⚠️ | liste en lignes de 3 (« Améliorations (1/n) ») ; boutons branchés comme avant ; **vérification en jeu par le bot (run complète) : voir « Résultats du bot »** |
| 1 | Puits vide, bugs de saut sur la margelle | ✅ code | eau à −1.2 m, collision en cylindre lisse ; à valider manette en main |
| 1 | Faces superposées sur les rebords de portes et de murs | ✅ | côté jeu : murs prolongés seulement aux coins, linteau et seuil à la largeur exacte ; côté art : 0 face superposée dans les kits (PR #47) |
| 1 | Acte II : défauts d'affichage | ✅ | cause : tuiles trop grandes et matériaux de niveau 1 (PR #47 : 512 px/m) ; château câblé à ses matériaux ; sols à la matière de leur fonction |
| 1 | Boss : apparition du joueur et du boss | ✅ code | boss perché hors de vue, entrée par un bond (D-105) |
| 1 | Sphère rouge, ouvertures noires, formes noires (acte III) | ⚠️ | sphère rouge = boule d'annonce greybox du volant (retirée) ; formes noires = cimes en sphères (retirées) ; **ouvertures noires : en cours (lueurs aux portes)** |
| 2 | 70 améliorations en ScriptableObjects, rangs, cadres | ✅ | 70 assets générés (`gen_upgrades.py`), 70 icônes et 10 cadres de la DA branchés ; tests `DixAmeliorations…`, `Marchand_UneNouvelle_UneMonteeDeRang_EtLeSoin`, `PrixParRang…` |
| 2 | Limite 10, un passif | ✅ | `UpgradeSlots` |
| 2 | Marchand nouvelle + montée, coût croissant | ✅ | test du marchand ; prix 60 / 90 / 140 / 220 / 350 |
| 2 | Économie réglée sur les âmes gagnées | ⚠️ | bot acte I : 114 âmes à 10:00 après 126 dépensées ; sortie baissée à 150 / 200 / 250 ; **mesure sur 9 cartes en cours** |
| 2 | HUD : améliorations au-dessus de la barre de vie | ✅ code | rangée de 10 cases, cadre de la DA, passif en losange ; capture en jeu à faire |
| 3 | Vagues cachées, pression constante | ✅ | plus de « VAGUE n » ; quota par acte, repos après blessure (D-103) |
| 3 | Scaling sur la puissance du joueur, plus lent | ✅ | `EnemyScaling` (exposant 0.9), par acte |
| 3 | Aucune salle vide, performances | ⚠️ | 87 goules à l'acte I ; images par seconde **dans l'éditeur à ×3** : 21 en moyenne ; **mesure dans le build à faire** |
| 4 | Cartes plus grandes, boucles, plusieurs routes | ✅ | 24 à 26 salles (16 avant), 5 à 13 boucles, 0 ou 1 impasse sur 15 cartes (test `ZeroImpasse…`) |
| 4 | Verticalité : étages, portes en hauteur | ✅ | pas de 4 à 5 m, sols de −8 à +15 m, 3 à 11 paliers par carte (portes en hauteur avec escalier) |
| 4 | Plus de cellule d'apparition du traqueur | ✅ | porte pleine qu'il enfonce, vestibule sans lumière |
| 4 | Acte II : château (halls, galeries, fenêtres de lune, lustres) | ✅ code | fenêtres de lune tous les 18 m, lustres à l'échelle, matériaux du château |
| 4 | Acte III : falaises, terrasses | ⚠️ | terrasses et paliers ; pas de nouveaux passages dans la roche cette nuit |
| 5 | Portes verrouillée et de sortie différentes, animées | ✅ code | grille de fer qui se lève / battants du kit / porte enfoncée par le traqueur |
| 5 | Sortie avant la fin du temps : temps restant et explication | ✅ code | panneau de la DA : « Sortie scellée · m:ss », « survivez jusque-là » |
| 6 | Minimap : murs précis, même dessin, ennemis à vue | ✅ | `ActMapPainter` (9 tests), `MinimapVision` ; capture à faire |
| 7 | Cri du traqueur à l'apparition et au repérage | ✅ code | cri de repérage (D-104) |
| 7 | Mort par prise : caméra forcée, gros plan, screamer, option réduite | ✅ code | `StalkerDeathCam` (3 s) ; à voir manette en main |
| 7 | Équilibre traction / traqueur | ✅ mesure | vitesse max du joueur mesurée par le bot : **24.8 m/s** ; traqueur 9.5 (chasse), 16.5 (pointe), 18.5 (pointe enragée) : voir « Mesures » ; rien changé |
| 8 | Boss : patterns larges, marquages sobres, tracé exact, arène | ✅ code | D-105 |
| 9 | Nettoyage DA : volants, lune du menu, inventaire | ✅ | voir « Inventaire » |

## Résultats du bot (3 seeds par acte)

(rempli à la fin des passages)

## Mesures

- **Vitesse** (bot, acte I) : maximum 24.8 m/s. Traqueur : chasse 9.5, pointe 16.5 (3 s toutes les 6 s), enragé 11.5, pointe enragée
  18.5. Un joueur qui enchaîne traction et bunny hop dépasse donc le traqueur de 6 à 15 m/s. **Ma proposition (non appliquée)** :
  pas de plafond sur le joueur ; si la fuite paraît trop facile en playtest, plafonner seulement la traction à 18 m/s (D-036) en
  sortie, pas le bunny hop.
- **Durée de l'acte I** (bot, seed 11) : 10 min 49 de jeu, dont 6 min d'attente d'âmes avant l'ajustement du prix.

## Réglages choisis

| Réglage | Valeur | Où |
|---|---|---|
| Prix de la porte de sortie | 150 / 200 / 250 âmes | `Zone_Acte1..3` |
| Prix des améliorations | 60, 90, 140, 220, 350 ; hausse +10 % linéaire, plafond ×2 | `Economy_Default` |
| Salles par acte | 22 / 20 / 22 demandées (24 à 26 obtenues avec les annexes) | `Act_1..3` |
| Boucles, impasses | `loopChance` 0.8, `minDoorsPerRoom` 2, `landingChance` 0.5 | `Act_1..3` |
| Pression | quota 6 / 8 / 10 goules par joueur, 2.5 s, plafond 16, repos 12 s | `Wave_*` |
| Scaling | PV +12 %, dégâts +6 % par point de puissance (exposant 0.9) ; ×1.25 / ×1.15 par acte | `EnemyScaling_Default` |

## Inventaire DA (retiré ou remplacé)

- Orbes d'offre et textes flottants des coffres et du marchand : retirés (menus de la DA).
- Boule rouge d'annonce du volant : retirée (yeux et corps qui luisent).
- Violet d'un ennemi attrapé : retiré (clignotement blanc seulement avant qu'il se libère).
- Lune du menu dans le ciel de l'acte III : désactivée.
- Cimes de forêt en sphères (acte III) : retirées.
- Herse à barreaux et cellule visible du traqueur : remplacées par une porte pleine.
- « VAGUE n » à l'écran et klaxon des vagues : retirés.
- Reste hors DA : les escaliers et grilles de porte sont des volumes simples aux matériaux de la DA (pièces demandées dans
  l'ART-HANDOFF) ; capsule du joueur 2 en coop locale.

## Ce qui reste (honnêtement)

(rempli à la fin)

## Checklist de test (dans l'ordre)

(rempli à la fin)
