# Run et niveaux : testing

**Updated:** 2026-11-13 · [Design](design.md) · [Implementation](implementation.md)

## Acceptance criteria

- [x] Facile : niveau 1 puis victoire ; difficultés grisées refusées ; multiplicateurs à 1 sans run ; Moyenne : niveau suivant seulement s'il existe. *(`Assets/_Project/Tests/EditMode/RunFlowTests.cs` › Facile_NiveauUnPuisVictoire, DifficulteNonJouable_Refusee_SansRunMultiplicateursA1, Moyenne_NiveauSuivantSeulementSIlExiste_Multiplicateurs)*
- [x] Réglages : 180 / 600 / 900 s, porte 200 âmes, PV 150. *(RunFlowTests › Reglages_Etape8_TimersPorteEtPV ; `EnemyTests.cs` › Zone_GraceTimerEnrage)*
- [x] Tronçons de couloir : le sien et les voisins interdits. *(`RoomVolumeTests.cs` › Couloir_TronconDuJoueurEtVoisinsInterdits_PlusLoinPermis)*
- [x] Traction refusée sur une surface non magnétique, charge gardée. *(`MagnetismTests.cs` › Traction_SurfaceNonMagnetique_RefuseeEtChargeGardee)*
- [x] Minimap : découverte en visitant, points clés une fois découverts, découvertes communes. *(RoomVolumeTests › Minimap_DecouverteEnVisitant_PointsClesVisiblesUneFoisDecouverts)*
- [x] Scène du niveau 1 : volumes, porte, marchand d'artefact (plus de marchand au hasard), canaux, parcours, coffres, apparitions, départ, combat, minimap, Level_01 dans le build. *(RunFlowTests › Scene_Niveau1_Base_EtCopieRetravaillee)*
- [x] Piège raté sans combat, remise à zéro. *(RunFlowTests › Piege_RateSansCombat_SeReArme)*
- [x] En Play Mode (Test_Arena), **salle de départ**, 45 s : timer arrêté, 0 apparition, 0 ennemi, traqueur immobile ; sortie : timer démarré. **3 min** : traqueur lâché (10 m en 2 s). **10 min** : porte déverrouillée ; avec 150 âmes, refus ; avec 250, ouverte (50 restent). **15 min** : Enragé, goule 3 → 6 âmes. **Sortie** : écran de victoire. **Mort** : écran de fin, curseur libre. *(Play Mode, 2026-11-13)*
- [x] En Play Mode, **marchand de l'artefact** : 3 offres par joueur ; 3 achats de suite à 40, 75 (60 + 25 %), 94 âmes ; barillet modifié (portée 60 → 85 m, dégâts 12 → 19.2). *(Play Mode, 2026-11-13)*
- [x] En Play Mode, **niveau 1** : NavMesh en 0.9 s (deux NavMesh 0.45 s, 949 murs grimpables 0.46 s) ; départ → sortie 1 329 m ; main du traqueur : 0 point hors de portée sur 12 467 ; pic à 14 min : 25 goules et 15 volants, image 12.6 ms (éditeur). Canal : −20 PV, remontée sur la rive. *(Play Mode, 2026-11-13)*
- [x] En Play Mode, **combat de boss** (joueur simulé qui attire) : 13.5 m/s gagné en 21.5 s, 18 m/s en 20.8 s, 7.5 m/s tué au 3e piège. *(Play Mode, 2026-11-13)*
- [ ] Ressenti : salle de départ, timers, porte, marchand, menus, niveau 1, minimap, combat. *(playtest)*

## Checklist de playtest

1. **Menu** (`MainMenu`) : choisir Facile, Lancer ; Moyenne refusée ; Test_Movement et Test_Arena s'ouvrent ; les éléments
   grisés ne se choisissent pas ; tout se fait aussi à la manette.
2. **Salle de départ :** aucun ennemi tant qu'on reste dedans ; le timer démarre à la sortie (coin haut droite). Page haut :
   +60 s.
3. **Marchand :** 3 offres, prix qui monte, nouvelles offres après chaque achat ; l'effet se sent sur le barillet.
4. **Porte de sortie :** rouge avec le compte à rebours, orange à 10 min, 200 âmes, escaliers, palier.
5. **Combat :** herse, pont, lustre : la zone orange et la cible se lisent-elles à pleine vitesse ? Le traqueur se laisse-t-il
   attirer ? Combat trop court ?
6. **Niveau 1 :** traversée, canaux (−20 PV, retour sur la rive), coffres derrière les murets, zones de parcours, minimap
   (Tab), taille de la carte.
7. **Mort :** écran de fin, Menu principal ou Rejouer.
