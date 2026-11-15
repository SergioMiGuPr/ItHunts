# Vagues : testing

**Updated:** 2026-11-15 · [Design](design.md) · [Implementation](implementation.md)

## Acceptance criteria

Après le playtest des étapes 8 et 9 (D-060, D-061) :

- [x] Valeurs : fournées 3 → 10 goules (14 en Enragé), 1 → 3 volants (4), fenêtre 8 s, pas de 0.2 s, salles récentes 60 s. *(`WaveTests.cs` › Asset_ValeursValidees)*
- [x] Fournée au début du pic, bornée par le plafond de population. *(WaveTests › Vague_FourneeAuDebutDuPic_SousLePlafond)*
- [x] Jamais dans une salle visitée depuis 60 s ni dans la salle de départ. *(WaveTests › SalleRecente_60s_EtSalleDeDepart_JamaisDApparition)*
- [x] Chasseurs par paliers 1, 2, 4, 6, 8, Enragé 10. *(WaveTests › Chasseurs_ParPaliers_1Puis2_4_6_8_Enrage10)*
- [x] Habitants 1 à 3 par grande salle. *(WaveTests › Habitants_1a3ParGrandeSalle_SelonLaTaille)*
- [x] Aggro sans limite, semé à 50 m pendant 5 s, chasseur jamais semé, relâché quand son joueur tombe. *(`CoopTests.cs` › Aggro_SansLimite_SemeA50mPendant5s_ChasseurJamaisSeme)*
- [x] Un chasseur par joueur, le plus proche par le chemin, relais quand il meurt, aucun pendant le combat de boss. *(CoopTests › Chasseurs_ParJoueur_RelaisAuPlusProcheParLeChemin)*
- [x] En Play Mode (Level_01_Base, temps ×3, joueur qui change de salle toutes les 8 s, 240 s de zone) : **60 apparitions, toutes dans les 7.1 s après le début du pic** ; 0 hors de la fenêtre, 0 dans une salle visitée depuis moins de 60 s, 0 dans la salle de départ. *(Play Mode, 2026-11-15)*
- [x] En Play Mode, chasseurs au fil de la zone (timer sauté à 50, 170, 350, 540, 720, 940 s) : solo **1, 2, 4, 6, 8, 10** ; à 2 joueurs **1/1, 2/2, 4/4, 6/6, 8/8, 10/10** ; en aggro : 0 à 8 selon le moment. Relais : 4 chasseurs tués, 4 nouveaux en moins de 1 s, au contact du joueur 15 s plus tard. Habitants : 89 gardés, 74 retirés. *(Play Mode, 2026-11-15)*

Avant (D-041, en partie remplacé) :

- [x] Valeurs validées dans `Waves_Default` (tendance en part du timer, Enragé 80 / 20, pic 20 s, accalmie 15 s à 60 %, 3 → 1 s, 0.5 s en Enragé, 25 à 80 m, annonce 0.6 s, reconstitution 4 s, réserve 80 / 20). *(`Assets/_Project/Tests/EditMode/WaveTests.cs` › Asset_ValeursValidees)*
- [x] Tendance interpolée, identique avec un timer de 4 ou 20 min ; Enragé au-dessus. *(WaveTests › Tendance_InterpoleeEnPartDuTimer_EnrageAuDessus)*
- [x] Accalmie de 15 s au début, pics de 20 s numérotés, 60 % en accalmie. *(WaveTests › PicsEtAccalmies_CommenceParUneAccalmie_60PourCent)*
- [x] Intervalle 3 s → 1 s, 0.5 s en Enragé. *(WaveTests › Rythme_3sA1s_PuisDemiSecondeEnEnrage)*
- [x] Distance 25 à 80 m de tous les joueurs ; champ de vision. *(WaveTests › Distance_25a80m_DeTousLesJoueurs)*
- [x] Tués reconstitués un par un (16 s pour 5), jamais par les apparitions normales ; dette bornée. *(WaveTests › TuesReconstituesLentement_JamaisDUnCoup)*
- [x] Plafonds de chasse selon la tendance (25 → 4 au début, 33 en Enragé). *(WaveTests › PlafondsDeChasse_SuiventLaTendance)*
- [x] Scène : réserve de 80 goules et 20 volants, directeur, points d'apparition (salles, couloirs, en hauteur), plus aucun ennemi posé à la main. *(`EnemyTests.cs` › Scene_TestArena_Labyrinthe)*
- [x] En Play Mode : **108 points**, goules 76/76 sur le NavMesh et hors du décor, volants 32/32 hors du décor. *(Play Mode, 2026-11-13)*
- [x] En Play Mode, **traversée de 300 s** en course (joueur immortel, timer de 240 s) : présents / voulus relevés toutes les 10 s (voir History) ; 109 apparitions, **aucune sous 25 m ni vue par le joueur**, 6 entre 80 et 84 m (le joueur bouge pendant les 0.6 s d'annonce), 12 échecs sans point libre (réessayés au pas suivant). *(Play Mode, 2026-11-13)*
- [x] En Play Mode, **reconstitution** : 10 goules tuées d'un coup en début de pic : la dette baisse d'une toutes les 4 s (10 → 5 en 20 s) ; les apparitions normales suivent en plus la tendance qui monte. *(Play Mode, 2026-11-13)*
- [x] En Play Mode, **coût au pic** (Enragé, 80 goules dont 44 en chasse, 20 volants dont 15 en chasse) : goules 0.36 ms, volants 0.28 ms, vagues 0.14 ms, **0.78 ms par pas fixe** (éditeur ; seuil 1.5 ms). *(Play Mode, 2026-11-13)*
- [x] En Play Mode, **survie** d'un joueur scripté qui suit la boucle sans esquiver (traqueur coupé, 60 s au plus), départ au début d'un pic de chaque palier : course : mort en 6.5 s (0 %), 19.9 s (25 %), 27.8 s (50 %), 20.0 s (75 %), 15.0 s (Enragé) ; bunny hop : vivant après 60 s à 0, 50 et 75 %, mort en 45.7 s (25 %) et 40.3 s (Enragé). Limite : la population de départ posée d'un coup n'atteint que 0 à 2 ennemis (peu de points cachés à 25–80 m du départ, un point ne resert qu'après 2 s) : la mesure part d'une zone presque vide qui se remplit au rythme du palier. *(Play Mode, 2026-11-13)*
- [x] Choc mortel après une répulsion (volant) : 1.5 s, quelle que soit la vitesse ; goule jamais. *(CombatTests › Volant_RepousseChocMortelPendant15s_GouleJamais)*
- [x] Coop : ×1.5 / ×2.5, parts, tour de rôle, point à 25 m de chacun et 80 m du joueur choisi. *(`CoopWaveTests.cs` › Coop_Multiplicateur_Parts_TourDeRole, Coop_Point_25mDeChacun_80mDuJoueurChoisi)*
- [x] Accalmie : plafond à 60 %, recul de 10 m. *(CoopWaveTests › Accalmie_PlafondDeChasse60PourCent_ReculDe10m)*
- [x] Choix de cible : marge 15 m, abandon 60 m, délai 3 s ; pas d'oscillation à égale distance ; immédiat si blessé par l'autre joueur ou si la cible meurt. *(CoopWaveTests › Cible_Marge15m_Abandon60m_Delai3s, Cible_PasDOscillation_ExceptionsImmediates)*
- [x] En Play Mode, **accalmie** (un joueur qui court, pic 5 puis accalmie puis pic 6) : en chasse 15 à la fin du pic, **12 pendant l'accalmie** (3 goules et 2 volants renvoyés en recul ; présents 15 → 25, personne ne disparaît), **22 au pic suivant**. *(Play Mode, 2026-11-13)*
- [x] En Play Mode, **coop** (joueurs simulés, immobiles, 30 à 45 s depuis 105 s) : 2 ensemble : voulu 38 goules et 9 volants (×1.5), 17 et 16 apparus pour chacun, distance minimale au moment d'apparaître **25.0 m** ; séparés de 150 m : 20 et 17 apparus, à moins de 40 m de chacun 8.5 et 7.0 en moyenne, distance minimale **27.2 m** ; 4 dans 4 salles (×2.5, voulu 64 et 15) : 9 à 11 apparus pour chacun. Aucune apparition sous 25 m d'un joueur. *(Play Mode, 2026-11-13)*
- [x] En Play Mode, **choix de cible** (une goule, 2 joueurs à 20 m de part et d'autre, 60 s) : immobiles, **1 changement** (le joueur 1 avait été repoussé à 39 m, l'autre à 23 m : changement voulu) ; un joueur qui tourne autour d'elle (rayon 20 m, un tour en 12 s) : **0 changement**. Aucune oscillation. *(Play Mode, 2026-11-13)*
- [ ] Ressenti : montée de la pression, lisibilité des pics, accalmies, apparitions jamais « dans le dos ». *(playtest)*

- [x] Salles (D-045) : même salle qu'un joueur interdite, couloir permis, salle tournée, alcôve dans une salle. *(`RoomVolumeTests.cs` › JamaisDansLaSalleDUnJoueur_CouloirPermis, SalleTourneeEtSalleDansUneSalle)*
- [x] Tronçons de couloir (D-047) : son tronçon et les voisins interdits, deux tronçons plus loin permis, salle au bout permise. *(`RoomVolumeTests.cs` › Couloir_TronconDuJoueurEtVoisinsInterdits_PlusLoinPermis)*
- [ ] Playtest : reste immobile au milieu d'un long couloir pendant un pic : aucun ennemi n'apparaît à moins d'un tronçon (environ 40 à 80 m) ; ils arrivent des deux bouts.
- [x] En Play Mode (Test_Arena, 16 volumes de salle ; joueur immortel immobile au centre de la salle C, temps de zone 5 min, ennemis tués toutes les 8 s, traqueur coupé, 90 s) : **sans volumes, 69 apparitions sur 184 dans sa salle ; avec, 0 sur 146** (il y est resté tout le temps). Dans un autre essai où les goules l'avaient poussé dans un couloir, 14 apparitions ont eu lieu dans la salle C pendant son absence ; à chaque apparition, aucun joueur n'était dans la salle. *(Play Mode, 2026-11-13)*

## Checklist F12 — vagues (Test_Arena)

Page 3 du debug (F3) : ligne « Vagues » (pic ou accalmie et temps restant, goules et volants présents / voulus, à
reconstituer, prochaine apparition, échecs).

1. **Début :** quelques goules et un volant, loin de toi ; accalmie de 15 s, puis **cor grave et « VAGUE 1 »** en haut de
   l'écran.
2. **Pics et accalmies :** 20 s de pic (des ennemis arrivent), 15 s d'accalmie (il n'en arrive plus). La respiration se
   sent-elle ?
3. **Apparitions :** lueur orange au sol 0.6 s, puis l'ennemi ; jamais à moins de 25 m, jamais sous tes yeux. Regarde
   derrière toi après un virage : en vois-tu apparaître ?
4. **Montée :** 10 goules au départ, 60 à la fin du timer, 80 en Enragé (volants 2 → 16 → 20). Trop, pas assez ?
5. **Tuer achète du répit :** tue un groupe : ils reviennent un par un (un toutes les 4 s au plus).
6. **Enragé :** plus d'ennemis, apparitions plus rapides. Reste jouable ?
7. **Accalmie (nouveau) :** à la fin d'un pic, une partie des ennemis (les plus loin) s'arrêtent, reculent d'environ 10 m et ne
   te poursuivent plus ; repasse près d'eux (aggro) : ils reviennent ; au pic suivant, tous reprennent. Respire-t-on ?
8. **Choix de cible (coop, à l'écran partagé plus tard) :** une goule garde sa cible ; elle change si l'autre joueur est bien
   plus près (15 m) ou si sa cible est loin (60 m), et se retourne tout de suite contre celui qui la blesse ou la lance. En solo :
   rien à voir, sauf qu'aucune goule ne « hésite ».

## History

- 2026-11-13 : construit et mesuré (bloc 7, D-041). Courbe mesurée (temps : goules présentes / voulues, volants) : 0 s 1/6,
  0/1 ; 20 s (pic 1) 7/13, 3/3 ; 60 s (pic 2) 17/20, 4/4 ; 100 s (pic 3) 28/30, 7/7 ; 130 s (pic 4) 35/38, 9/9 ; 200 s (pic 6)
  51/53, 13/13 ; 240 s (Enragé) 60/80, 16/20 ; 270 s 80/80, 20/20. Joueur qui ne tue rien : pendant les accalmies, les présents
  restent au-dessus du voulu (rien n'est retiré).
