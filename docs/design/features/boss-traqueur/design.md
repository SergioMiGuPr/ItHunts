# Boss traqueur

**Status:** in progress · **Updated:** 2026-11-12 · [Implementation](implementation.md) · [Testing](testing.md) · [GDD](../../GDD.md)

Un seul monstre, le même toute la run : un **géant de 14 m** qui poursuit le joueur sans relâche dans le labyrinthe,
façon Pac-Man, l'attrape à la main et le tue d'un coup ; il ne peut être ni blessé, ni ralenti, ni traversé avant la zone finale. Il est plus rapide que la
course de base : seul un mouvement maîtrisé lui échappe. On l'entend et on le sent (le sol tremble) avant de le voir.
Piliers : « le mouvement est la survie », « risque contre butin » (D-002). Décisions D-002, D-009, D-033, D-034, D-035, D-037, D-038, D-039.

## Règles

1. **États (D-035) :** Repaire (temps de grâce au début de la zone) → **Chasse** : poursuite continue, il sait toujours où
   est le joueur visé (plus de vue ni d'ouïe ; Rôde, Alerte et Recherche n'existent plus) → **Enragé** après le timer.
2. **Règle de Pac-Man (D-035) :** dans un couloir, il **ne fait jamais demi-tour**. À une intersection (salle ou carrefour),
   il prend la branche qui le rapproche le plus du joueur, en distance par les couloirs et **sans repasser par
   l'intersection où il est** ; **jamais celle d'où il vient**, sauf dans un **cul-de-sac** (son repaire). **Enragé :
   demi-tour permis** (réglage `allowUTurnWhenEnraged`, coché). Quand le joueur est **proche (35 m) et devant lui**
   (et que le chemin part vers l'avant), il fonce droit dessus. Il remplit la largeur d'un couloir : **on ne le dépasse
   pas, seule une boucle le sème.**
3. **Taille (D-034) et passages bas (D-035) :** **14 m de haut, 7 m de large** ; il ne grimpe pas. Il passe partout où
   il y a **9 m** de haut (`crouchMinHeight`) : sous un pont, une poutre ou un passage plus bas que lui, **il se baisse**
   (le corps s'écrase, vitesse ×0.6). Seules les **portes des pièces** (2 à 4 m) sont trop petites.
4. **Prise à la main (D-034) :** sa main atteint **tout point à moins de 15 m de son épaule** (épaule à 12 m ; il pivote
   le buste vers la cible), **y compris en hauteur** : aucune plateforme d'une arène n'est hors de portée quand il est
   dessous ou à côté. Quand il voit (ou sent) le joueur à portée : **le bras se lève et une ombre et une lueur rouge
   marquent la zone visée pendant 0.5 s**, puis **la main s'abat sur le point prédit** (où sera le joueur à l'impact).
   La zone de prise est **la main elle-même** (boîte de 4 × 2.2 × 4.5 m du modèle, orientée) : le joueur qu'elle recouvre
   meurt, **quels que soient ses PV**. **Recharge de 2.5 s.** **Visée corrigée 0.15 s avant la descente** (D-035, `grabReaim`) : il vise de nouveau où sera
   le joueur. Un dash, un saut ou une traction au bon moment l'esquivent
   (un double saut passe au-dessus). Pendant une prise, il ralentit (30 %).
4 bis. **Main plantée (D-039) :** après une **prise ratée** (la main ne touche personne), sa main **reste plantée au sol
   1.5 s**, bien visible (nuage de poussière, lueur rouge qui s'éteint) : pas de nouvelle prise, rotation ralentie (×0.4), pas
   de pointe, mais **son corps continue vers le joueur à sa vitesse normale** (piste b) ; **revers de la main gauche** (piste a) :
   0.5 s après le début de la main plantée, un joueur à **7 m** ou moins du bord de son corps (jusqu'à 12 m de haut) déclenche une
   fois une annonce de **0.2 s** (zone rouge au sol, `Zone du revers`), puis un coup mortel de 0.25 s dans toute la zone ; le corps
   et les mains tuent toujours au contact ; le geste des mains au-dessus de la tête reste possible.
   C'est la fenêtre pour **le contourner dans une grande salle** (provoquer une prise, l'esquiver, passer à côté). Après une
   prise réussie, recharge de 2.5 s comme avant.
5. **Contact (D-037) :** son corps est solide pour le joueur et pour les objets ; **tout contact avec son corps, ses bras ou
   ses mains tue**, quels que soient les PV (le modèle entier est couvert par ces zones, bras au repos comme levés).
5 bis. **Mains au-dessus de la tête (D-037) :** quand un joueur est au-dessus de lui ou **va y passer** (trajectoire
   prédite sur 0.6 s, en ligne droite et en chute ; cylindre de **10 m** de rayon, de 7 m au-dessus de ses pieds jusqu'à
   **12 m au-dessus de sa tête**), il **lève les deux mains** : annonce **0.2 s**, puis **zone mortelle pendant 1 s** : ses
   mains suivent le joueur (45 m/s, à portée des épaules) ; recharge **1.5 s**, séparée de celle de la prise ; la prise
   reste possible pendant le geste (la main droite y retourne). On ne passe plus par-dessus lui.
6. **Pièces et bords (D-035) : plus d'abri.** Devant une pièce où il ne passe pas, il **s'arrête à l'ouverture la plus
   proche** (porte ou fenêtre) et **passe le bras** : sa main va jusqu'à **15 m à l'intérieur** depuis l'ouverture
   (`roomReach`) ; aucune pièce ne dépasse cette profondeur. Ailleurs, la portée se calcule **depuis là où il peut vraiment
   se tenir** (plusieurs essais autour du joueur, à sa hauteur et au sol dessous) ; collé à un mur ou au bord de la
   carte, il **se penche** (+2.5 m, `edgeLean`). La main a besoin de 4.5 m de dégagement au-dessus du point visé (les
   plafonds bas des pièces : elle y entre par l'ouverture).
6 bis. **Enragé qui accélère (D-038) :** tant qu'il reste sur la même cible, sa vitesse de base monte par paliers :
   **0 s → 11 m/s, 10 s → 13, 20 s → 15, 25 s → 16.5, 30 s → 18** (maximum), en 1 s à chaque palier ; la pointe ne dépasse
   jamais 18 m/s ; chaque palier s'annonce (grondement, « TRAQUEUR ENRAGÉ » qui pulse plus fort et s'illumine). Tout repart de
   zéro s'il change de cible (coop, au plus toutes les 5 s) ou si la cible meurt.
7. **Vitesse (D-033) :** base Rôde / Chasse / Enragé à **3 / 9 / 11 m/s** ; il **vise l'endroit où sera le joueur**
   (prédiction, 0.8 s) ; quand il le voit (ligne de vue, 60 m), **pointe à 16 m/s (18 enragé) pendant 3 s**, recharge de 6 s ; **suivi de la
   vitesse du joueur désactivé par défaut** (0 ; 0.85 à essayer : au moins 85 % de la vitesse du joueur, jamais au-delà
   de sa pointe) ; il ralentit dans les virages serrés. Il **ne bride jamais le mouvement du joueur** (D-012).
8. **Intuable :** ni dégâts (objets lancés, chutes, murs, zone, balles), ni répulsion, ni étourdissement, ni prise.
9. **Annonce :** pas lourds audibles à **60 m** (atténuation 3D), qui **font trembler la caméra à moins de 25 m**
   (réglage de confort du joueur pour le couper), souffle, grondement quand il repère le joueur, nappe de tension qui
   monte en Alerte et en Chasse (sons provisoires).
10. **Coop (D-035) :** il vise le joueur **le plus proche par le chemin** et change de cible **au plus une fois toutes les
    5 s** (`retargetInterval`), tout de suite si sa cible meurt.
11. **Phase enragée :** tant qu'elle dure, les âmes gagnées sont multipliées (D-009). HUD : coin « TRAQUEUR ENRAGÉ ».
12. **Zone finale :** il devient tuable, seulement par l'environnement (lustres, piliers, pièges, points faibles
    accessibles au magnétisme) et les dégâts ajoutés par les améliorations. Les règles détaillées iront dans « Combat
    final », à concevoir.

## Valeurs

ScriptableObject `Stalker_Default` (`StalkerSettings`) ; zone : `Zone_Test` ; arènes : `ArenaLayout_Test`. Repères :
course 7.5 m/s, plafond des gains 20 m/s, bunny hop 18 à 26 m/s, traction 18 m/s (D-036).

| Nom | Valeur | Unité | À observer en playtest |
|---|---|---|---|
| Taille (hauteur × largeur), hauteur de l'épaule | 14 × 7, 12 | m | un vrai géant |
| Portée de la main (depuis l'épaule) | 15 | m | ≈ 10 m au sol, tout le dessus d'une plateforme de 25 m quand il est dessous |
| Annonce de la prise, descente, main au sol, recharge | 0.5, 0.12, 0.35, 2.5 | s | lisible et esquivable |
| Main (zone de prise) | 4 × 2.2 × 4.5 | m | |
| Mains levées : rayon, hauteur (de / au-dessus de la tête), prédiction | 10, 7 / 12, 0.6 | m, m, s | |
| Mains levées : annonce, zone mortelle, recharge, vitesse des mains, écart | 0.2, 1, 1.5, 45, 3 | s, s, s, m/s, m | quasi impossible à passer |
| Enragé : paliers (temps sur la cible → vitesse de base), montée, maximum | 0 → 11, 10 → 13, 20 → 15, 25 → 16.5, 30 → 18 ; 1 s ; 18 | s → m/s | la fuite en Enragé a une fin |
| Épaisseur des bras | 1.5 | m | mortels au contact |
| Main plantée après une prise ratée : durée, rotation, vitesse du corps | 1.5, ×0.4, ×1 | s | fenêtre pour le contourner (grande salle seulement) |
| Revers (main plantée) : portée depuis le bord du corps, hauteur, délai, annonce, coup | 7, 12, 0.5, 0.2, 0.25 | m, m, s, s, s | salle de 40 m fermée, 80 m ouverte |
| Dégagement de la main au-dessus de la cible | 4.5 | m | |
| Visée corrigée avant la descente | 0.15 | s | |
| Portée dans une pièce (depuis l'ouverture) ; penché au bord | 15 ; +2.5 | m | |
| Hauteur minimale de passage ; vitesse baissé | 9 ; ×0.6 | m | temps perdu sous un pont : 0.5 à 0.7 s |
| Foncer droit : distance, angle | 35, 200 | m, ° | |
| Arrivée à une intersection | 9 | m | |
| Demi-tour permis en Enragé | oui | | |
| Changement de cible en coop | 5 | s | |
| Contact du corps | 0.3 | m (entre les surfaces) | |
| Vitesse Chasse / Enragé (base) | 9 / 11 | m/s | > course (7.5) |
| Pointe (normale / enragée), durée, recharge | 16 / 18, 3, 6 | m/s, s, s | |
| Suivi de la vitesse du joueur | 0 (essayer 0.85) | | |
| Prédiction | 0.8 | s | coupe les trajectoires |
| Accélération / vitesse de rotation | 12 / 180 | m/s² / °/s | virages serrés = moyen de lui échapper |
| Largeur minimale d'un passage pour lui | 7 (14 dans les couloirs du labyrinthe) | m | |
| Portes des pièces (trop petites pour lui) | 2 à 4 (3 en test) | m | |
| Vue (pour la pointe seulement) : portée / angle / hauteur des yeux | 60 / 110 / 12.5 | m / ° / m | |
| Sons : portée ; tremblement de la caméra | 60 ; à moins de 25 m | m | confort |
| Temps de grâce au début de la zone | 60 (test : 15) | s | |
| Timer de zone avant Enragé | 20 min (test : 4) | | |
| Multiplicateur d'âmes en phase enragée | 2 | × | [ASSUMPTION] ; vaut-il le risque ? |

## Cas particuliers

- Joueur dans une pièce : il va devant l'ouverture la plus proche et passe le bras ; plus d'abri nulle part (mesuré :
  7 893 points sur 7 893 à portée, dont 175 dans les pièces).
- Joueur derrière lui dans un couloir : il continue jusqu'à l'intersection suivante, puis fait le tour.
- Coop : le plus proche par le chemin, au plus un changement toutes les 5 s ; les joueurs « à terre » sont ignorés
  [ASSUMPTION].
- Sortie de zone pendant une poursuite : la transition se fait, le boss ne suit pas.
- Mort du joueur : toute la zone est remise à zéro, traqueur compris (repaire, grâce, timer).

## Questions ouvertes

- **Passer à côté de lui** (D-037, D-039) : réglé le 2026-11-13 par les pistes (b) puis (a). Mesures (20 essais) : (b) seule :
  40 m toujours 20/20 en traction ; (b) + revers de 15 m : 80 m 0/20 (trop fort, même avec 0.5 s de délai) ; portée 6 m : 40 m
  11/20 en traction ; **7 m (retenu) : 40 m 0/20, 80 m 20/20 en bunny hop et en traction**, couloir 0/60 ; 8 m : 80 m traction
  17/20 ; 9 m : 3/20. À juger au playtest : le revers est-il lisible (zone rouge 0.2 s) ? La salle de 40 m doit-elle rester
  fermée en bunny hop aussi (0/20) ?
- Déroulé exact du combat final (phases, points faibles).
- [ASSUMPTION] Temps de grâce de 60 s au début de chaque zone.
- Prise à la main : la visée corrigée (0.15 s) suffit-elle à la rendre moins facile ? À juger au playtest.
- Durée max d'une traction (2.5 s) : à 18 m/s, une traction couvre au plus 43 m ; faut-il l'allonger ? (voir
  [orbes](../orbes/design.md)).

## History

- 2026-11-11 : designed.
- 2026-11-11 : timer réglable (4 min en test), multiplicateur d'âmes en phase enragée, cible en
  coop, mort par l'environnement en zone finale (D-008 à D-010).
- 2026-11-12 : vitesse (base, pointe, suivi plafonné sous le bunny hop, prédiction), raccourcis réservés, construit
  (étape 6, bloc 3, D-033).
- 2026-11-12 : géant de 14 m qui attrape à la main, abris étroits et bas, tremblement de caméra (D-034).
- 2026-11-12 : poursuite façon Pac-Man dans un labyrinthe, se baisse sous les ponts, bras par les ouvertures des pièces,
  plus d'abris, visée corrigée, cible la plus proche par le chemin en coop (D-035).
- 2026-11-12 : mains levées au-dessus de la tête, contact mortel du corps, des bras et des mains (D-037).
- 2026-11-12 : Enragé qui accélère sur la même cible (D-038) ; mesure du passage à côté (D-037).
- 2026-11-12 : main plantée après une prise ratée (D-039).

- 2026-11-13 : combat de boss de fin de niveau à trois pièges (D-049) ; sonné (`Stagger`) et replacé (`WarpTo`) seulement par les pièges ([run et niveaux](../run-et-niveaux/design.md)).
