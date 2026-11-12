# Boss traqueur : testing

**Updated:** 2026-11-12 · [Design](design.md) · [Implementation](implementation.md)

## Acceptance criteria

- [x] Le coup du traqueur tue en un coup, à 1, 100, 1 000 et 1 000 000 PV. *(`Assets/_Project/Tests/EditMode/StalkerTests.cs` › Traqueur_TueEnUnCoup_QuelsQueSoientLesPVDuJoueur)*
- [x] La prise à la main tue quels que soient les PV (1, 100, 1 000 000), seulement si la capsule du joueur touche la main (boîte orientée) : à côté ou au-dessus (double saut), il survit. *(StalkerTests › PriseALaMain_TueQuelsQueSoientLesPV_SeulementSousLaMain ; `EnemyTests.cs` › MainDuTraqueur_CapsuleContreBoiteOrientee)*
- [x] Valeurs du géant : 14 × 7 m, main à 15 m, annonce 0.5 s, recharge 2.5 s, vue et sons à 60 m, tremblement à 25 m, suivi désactivé, dégagement de 4.5 m ; vitesses 3 / 9 / 11, pointe 16 / 18 (3 s, recharge 6 s). *(EnemyTests › Reglages_ValeursDeDepart)*
- [x] Vitesse : sans suivi, sa base ; avec suivi à 0.85, 85 % du joueur ; jamais au-dessus de 18 (bunny hop 18 à 26). *(EnemyTests › VitesseDuTraqueur_Base_Suivi_PointePlafonneeSousLeBunnyHop)*
- [x] Pointe de 3 s seulement quand il voit, recharge de 6 s ; prédiction horizontale ; cône de vue. *(EnemyTests › Pointe_3s_PuisRecharge6s_SeulementQuandIlVoit, Prediction_OuSeraLeJoueur_HorizontalSeulement, Vue_ConeEtPortee)*
- [x] Tremblement de la caméra : seulement à moins de 25 m, plus fort près, désactivable. *(EnemyTests › TremblementDeCamera_SeulementPres)*
- [x] Intuable : aucun dégât (objet lancé, mur, direct) ; les balles l'atteignent sans le blesser. *(EnemyTests › Traqueur_Intuable ; ArtifactTests › Tir_InstantaneDepuisLesYeux_Portee60_MurBloque_TraqueurInsensible_TireurIgnore)*
- [x] NavMesh : par une petite porte de 1.2 m, la goule passe, le traqueur fait le tour. *(EnemyTests › NavMesh_PetitePorte_LaGoulePasse_LeTraqueurFaitLeTour)*
- [x] Zone : grâce 15 s, timer 4 min, enragé à 0. *(EnemyTests › Zone_GraceTimerEnrage)*
- [x] Scène Test_Arena (labyrinthe, D-035) : NavMesh, zone, sortie, graphe, traqueur intuable, au moins 76 goules, 6 pièces, 20 murs grimpables et plus, au moins 75 objets ; plus de grilles ni de petites portes. *(EnemyTests › Scene_TestArena_Labyrinthe)*
- [x] Règle de Pac-Man : jamais la branche d'arrivée (500 cas au hasard), demi-tour seulement dans un cul-de-sac ou s'il est permis, joueur derrière lui : il fait le tour ; chaque couloir du labyrinthe dans une boucle sauf le repaire. *(EnemyTests › PacMan_JamaisDeDemiTour_SaufCulDeSacOuPermis, Labyrinthe_ChaqueCouloirDansUneBoucle, Labyrinthe_DeLaScene_ToutesLesBoucles)*
- [x] Valeurs D-035 : 9 m baissé, ×0.6, 15 m dans une pièce, 5 s entre deux cibles, visée corrigée 0.15 s, demi-tour permis en Enragé. *(EnemyTests › Reglages_ValeursDeDepart)*
- [x] En Play Mode (labyrinthe, 2026-11-12) : **NavMesh** en 55 ms, 26 murs grimpables ; les 10 intersections reliées pour le traqueur et pour les goules (corrigé en chemin : l'allège sous l'ouverture d'une salle haute était « non praticable » et coupait les pentes). *(Play Mode)*
- [x] En Play Mode, **portée de la main** : **7 893 points où l'on tient debout sur 7 893** à portée depuis un point relié à son repaire (dans les pièces 175/175, sous un pont ou une poutre 661/661, collés à un mur 1 157/1 157) ; il se tient au plus à 17 m à plat du joueur. Corrigé en chemin : essais au sol sous les corniches, deuxième anneau d'essais (un coin de corniche au-dessus d'une rampe). *(Play Mode)*
- [x] En Play Mode, **passages bas** : il passe sous les 9 ponts et poutres, baissé à 9.3–9.5 m, 1.2 à 1.75 s baissé, **0.5 s perdue sous une poutre, 0.67 à 0.70 s sous un pont** ; aucun demi-tour. *(Play Mode)*
- [x] En Play Mode, **poursuite** (il part 38–40 m derrière ; goules coupées) : ligne droite SO → SE (210 m) : **course rattrapée en 6.9 s**, bunny hop 18 m/s et traction (18 m/s) pas rattrapés ; **boucle SO → S → C → O** : course 13.5 s, **bunny hop 35.1 s**, traction 23.3 s (il suit la boucle par derrière, il rattrape aux virages) ; **Enragé** : traction 12.5 s (ligne) et 8.0 s (boucle), traction + bunny hop pas rattrapé en ligne droite, **26.3 s dans la boucle (il fait demi-tour et prend le joueur à revers)**. **0 demi-tour hors cul-de-sac et hors Enragé** sur toutes les mesures. *(Play Mode)*
- [x] Captures : labyrinthe vu de dessus, traqueur baissé sous le pont de la salle centrale (9.5 m), bras passé par la porte de la pièce SO (joueur tué à l'intérieur). *(Play Mode, 2026-11-12)*
- [x] En Play Mode, **NavMesh** construit au lancement en 252 ms (goule : voxels de 0.1 m, traqueur : 0.6 m) ; le traqueur rejoint chaque arène et chaque repaire depuis le sien (B → A 300 m, B → C 198 m, repaire A 341 m, repaire C 208 m), passe par ses raccourcis (110 m) et par le couloir sinueux ; il n'a pas de NavMesh dans les tunnels bas ; la goule passe les petites portes et pas les grilles. *(Play Mode, 2026-11-12)*
- [x] En Play Mode, **portée de la main** : toutes les surfaces où le joueur peut se tenir dans les trois arènes (grille de 2 m, 6 547 points : sol, fosses, rampes, corniches, plateformes jusqu'à 25 m, dessus des murs de course) sont à moins de 15 m de son épaule depuis un point de son NavMesh ; le plus loin : 14.4 m (A), 14.3 m (B), 14.8 m (C) ; **aucun abri dans les arènes** (aucun point sans 4.5 m de dégagement au-dessus). Corrigé en chemin : dessous des plateformes basses et des corniches, fonds de fosse à 6 m, panneaux horizontaux à 34 m, recoins sous les rampes. *(Play Mode, 2026-11-12)*
- [x] En Play Mode, **esquive de la prise** (20 essais avec un dash à un moment tiré entre 0.1 et 0.4 s après le début de l'annonce, 5 sans réaction) : **à l'arrêt 20/20** esquivées (sans réaction 0/5) ; **en course 20/20** (0/5) ; **en bunny hop à 18 m/s 20/20** (0/5) ; **sur la plateforme de 25 m 20/20** (0/5). *(Play Mode, 2026-11-12)*
- [x] En Play Mode, **poursuite** dans chaque arène (il part 30 m derrière, il voit le joueur, fuite le long de la plus longue ligne droite) — suivi à 0 et à 0.85 (mêmes résultats, sauf indiqué) : **A, course : attrapé à la main en 9.2 s** (9.6 s au contact avec 0.85) ; **A, bunny hop 18 m/s et traction : pas attrapé** (fin de ligne) ; **B et C, course : pas attrapé** (la ligne passe dans un trou de 8 m entre un bloc et un mur, ou dans le couloir sinueux : il contourne, perd le joueur de vue et cherche) ; **B et C, bunny hop et traction : pas attrapé**. Il ne dépasse jamais 16 m/s. *(Play Mode, 2026-11-12)*
- [x] En Play Mode, **coût** (éditeur) avec les 30 goules vivantes en chasse autour du joueur et le traqueur : goules 0.22 ms par pas fixe (13 ms par seconde), traqueur 0.04 ms par pas fixe ; fil principal 3.9 → 4.5 ms par image. *(Play Mode, 2026-11-12)*
- [x] Zone des mains levées : au-dessus de lui, en haut du cylindre, traction vers lui (prédite), redescente prédite ; pas au sol à côté, pas trop loin, pas en s'éloignant. *(EnemyTests › MainsLevees_ZoneEtTrajectoirePredite)*
- [x] Valeurs D-037 : 10 m, 12 m au-dessus de la tête, 0.2 s, 1 s, 1.5 s. *(EnemyTests › Reglages_ValeursDeDepart)*
- [x] En Play Mode, **le modèle entier tue** : 2 239 sommets du modèle sur 2 239 dans une zone mortelle (corps, bras, mains), bras au repos et bras levés. *(Play Mode, 2026-11-12)*
- [x] En Play Mode, **passages par-dessus lui** (couloir S – SE, plafond 19 m ; 20 essais par cas, départ et visée tirés au hasard) : **0/20** pour chaque cas — immobile et en poursuite × saut + double saut, dash-jump, traction (point visé 3 à 12 m au-delà de lui, au plafond), saut depuis un mur (lancé à 10–14 m de haut contre le mur, 14–18 m/s) ; **traction haute dans la salle SE** (plafond 36 m, point visé à 35.6 m) : **0/20** immobile, **0/20** en poursuite. Morts : par la main de la prise pour les sauts au sol, par les mains levées pour les sauts de mur et la plupart des tractions. Le saut depuis un mur est approché par une vitesse de départ (pas un vrai wall run scripté). *(Play Mode, 2026-11-12)*
- [x] Enragé : paliers 11 / 13 / 15 / 16.5 / 18 m/s à 0 / 10 / 20 / 25 / 30 s, montée en 1 s, maximum 18 m/s pointe comprise. *(EnemyTests › Enrage_PaliersSurLaMemeCible_PlafondA18, Reglages_ValeursDeDepart)*
- [x] En Play Mode, **Enragé** (il part 38–40 m derrière) : ligne droite de 210 m : course rattrapée en **3.9 s**, traction **16.6 s** (palier 2), traction + bunny hop et bunny hop **pas rattrapés** (la ligne finit en 11 à 12 s) ; boucle : course **4.5 s**, traction **15.6 s** (palier 2), traction + bunny hop **23.7 s** (palier 3), bunny hop **31.4 s** (palier 5, 18 m/s). Jamais plus de 18 m/s. *(Play Mode, 2026-11-12)*
- [x] En Play Mode, **passage à côté de lui** dans une grande salle (salles temporaires de 40 et 80 m, il est au centre en Chasse, le joueur le contourne par un côté jusqu'au bord opposé ; 20 essais par cas) : sans réaction, **0/120** ; avec un dash 0.38–0.48 s après le début de chaque annonce de prise : salle de 40 m **0/20** en course, bunny hop et traction ; salle de 80 m **3/20 en course, 1/20 en bunny hop, 0/20 en traction**. Morts : par la main (pendant la descente de la prise) ou le corps. Joueur scripté simple (une ligne droite le long du mur). *(Play Mode, 2026-11-12)*
- [x] Main plantée après une prise ratée (1.5 s, rotation ×0.4), pas après une prise réussie (grabHold), recharge 2.5 s. *(EnemyTests › MainPlantee_ApresUnePriseRatee_PasApresUnePriseReussie)*
- [x] En Play Mode, **passage à côté avec la main plantée** (le joueur provoque une prise à 9 m devant et 7 m de côté, l'esquive par un dash de côté 0.38–0.48 s après le début de l'annonce, puis passe le long du mur ; 20 essais par cas) : **salle de 80 m** : course 0/20 (tuée par le bras ou la main plantés), **bunny hop 20/20, traction 20/20** ; **salle de 40 m** : course 0/20 (rattrapée au corps), **bunny hop 3/20, traction 20/20** ; **couloir de 14 m** : **0/60** (course, bunny hop, traction). *(Play Mode, 2026-11-12)*
- [ ] Ressenti : taille, tension de la traque, lisibilité et équité de la prise, abris, tremblement. *(playtest)*

## Playtest — labyrinthe (D-035 à D-037)

Ouvrir `Assets/Scenes/Test_Arena.unity`, Play. F3 trois fois : page 3 (traqueur : « Poursuite » droit sur toi ou
labyrinthe vers telle intersection, d'où il vient, demi-tours, baissé ; « Main » ; résumé des goules). Plan : départ
au sud-ouest (SO), sortie verte au nord-est, en haut (+16 m) ; repaire à l'est. Réglages : `Stalker_Default`,
`Ghoul_Default`, `Magnetism_Default` ; dimensions : `ArenaLayout_Test`, puis *ItHunts > Reconstruire la scène Test_Arena
(labyrinthe)*.

1. **Labyrinthe :** parcours-le (grandes salles, carrefours, pentes vers le niveau haut, la descente de 15° du carrefour
   N vers la salle centrale en glissade, lignes droites de 145 à 255 m). Assez de place ? Lisible ?
2. **Pac-Man :** après 15 s de grâce, il sort de son repaire et vient vers toi. Passe derrière lui dans un couloir : il
   ne fait pas demi-tour, il va jusqu'à l'intersection suivante et fait le tour. Dans un couloir, tu ne peux pas le
   dépasser : sème-le par une boucle (par exemple SO → S → C → O).
3. **Se baisser :** sous les ponts (salles, couloirs S – C et C – E) et les poutres, il s'écrase à 9.5 m et ralentit.
4. **Pièces :** entre dans une petite pièce (porte de 3 m) : il s'arrête devant la porte ou la fenêtre et passe le bras
   (zone rouge à l'intérieur) ; aucun coin n'est sûr.
5. **Prise :** zone rouge 0.5 s, puis la main ; elle corrige sa visée 0.15 s avant de tomber. Plus dure à esquiver ?
6. **Bords et hauteurs :** colle-toi à un mur extérieur, monte sur une corniche (11 m), un pont, une salle haute : il
   t'atteint quand même.
7. **Traction à 18 m/s (D-036) :** elle monte vite à 18 m/s et s'arrête après 2.5 s (≈ 43 m) ; lancé plus vite (bunny
   hop), elle garde ta vitesse. Assez pour fuir ? Combinée au bunny hop, elle sème le traqueur en ligne droite.
8. **Goules :** voir [goule/testing.md](../goule/testing.md) (encerclement, murs, bond d'en haut).
9. **Par-dessus lui (D-037) :** essaie de lui passer au-dessus (double saut, traction, saut de mur, traction haute en salle) :
   il lève les deux mains (0.2 s) et elles te suivent pendant 1 s ; toucher son corps, ses bras ou ses mains tue. Reste-t-il
   un moyen ? Lisible ?
10. **Enragé qui accélère (D-038)** : reste longtemps sur la même fuite : il accélère toutes les 10, 10, 5 et 5 s (page 3 :
    temps sur la cible, palier ; HUD qui pulse plus fort, grondement). Combien de temps tiens-tu en bunny hop ?
11. **Main plantée (D-039)** : dans une grande salle, laisse-le tenter une prise, esquive (dash de côté juste avant que la main
    tombe) : sa main reste plantée 1.5 s (poussière, lueur qui s'éteint, page 3 : « Main plantée : OUI ») ; passe à côté de lui
    pendant ce temps (bunny hop ou traction). Salle de 80 m : faisable ? Salle de 40 m : trop facile en traction ? Couloir :
    impossible ? Le geste des mains levées reste là si tu passes par-dessus.
12. **Enragé** (timer 4 min) : il peut faire demi-tour ; la boucle ne suffit plus. Mort : remise à zéro de la zone.

## History

- 2026-11-11 : designed.
- 2026-11-12 : construit (étape 6, blocs 3 et 4, D-033) ; mesures de poursuite.
- 2026-11-12 : géant de 14 m, prise à la main, nouvelle Test_Arena à l'échelle (D-034) ; mesures de portée, d'esquive, de poursuite et de coût.
- 2026-11-12 : labyrinthe et poursuite façon Pac-Man (D-035), traction à 18 m/s (D-036) ; mesures de portée, de passages bas, de poursuite ; captures.
- 2026-11-12 : mains levées, contact mortel du corps entier (D-037) ; mesures de passage par-dessus et de couverture du modèle.
- 2026-11-12 : Enragé qui accélère (D-038) ; passage à côté mesuré.
- 2026-11-12 : main plantée (D-039), passage à côté mesuré.
