# Orbes : testing

**Updated:** 2026-11-12 · [Design](design.md) · [Implementation](implementation.md)

## Acceptance criteria

Magnétisme v2 (D-021). Les tests tournent sur un sol et une visée factices ; les mesures en Play Mode
utilisent le vrai CharacterController et la vraie physique (entrées injectées, 2026-11-12).

- [x] La traction accélère à 80 m/s² jusqu'à 30 m/s (23 ticks), sans dépasser. *(`Assets/_Project/Tests/EditMode/MagnetismTests.cs` › Traction_AccelereJusquALaVitesseDeTraction ; Play Mode : 30 m/s en 0.38 s)*
- [x] Ligne droite, sans gravité : les yeux restent sur la droite départ → point. *(MagnetismTests › Traction_LigneDroiteSansGravite)*
- [x] Plus vite que 30 m/s : vitesse gardée et redirigée, sans plafond des gains. *(MagnetismTests › Traction_PlusVite_VitesseGardeeEtRedirigee_SansPlafond ; Play Mode : lancée à 40.00 m/s → 40.00 au 1er tick et au relâchement, horizontale 37.63 → 37.63 après)*
- [x] Vers le haut, la chute n'est pas comptée. *(MagnetismTests › Traction_VersLeHaut_LaChuteNestPasComptee)*
- [x] Au relâchement, vitesse et direction gardées (seule la gravité reprend). *(MagnetismTests › Traction_Relacher_GardeVitesseEtDirection)*
- [x] Fin de traction : arrivée, durée max, ligne de vue coupée, obstacle, saut (le saut part le même tick), dash, répulsion. *(MagnetismTests › Traction_SArreteAuPoint, Traction_DureeMax, Traction_LigneDeVueCoupee, Traction_BloqueeParUnObstacle, Traction_UnSautLArrete_EtLeSautPart, Traction_UnDashLArrete, Repulsion_PendantLaTraction_LArrete)*
- [x] Portée 60 m : sol → toit de la grande salle (43.4 m) en 1.60 s, fin « arrivée » à 43.2 m. *(Play Mode)* ; au-delà de 60 m, refus. *(MagnetismTests › Traction_RienAPortee)*
- [x] 1 traction = 1 charge d'attraction, sans autre délai ; sans charge, refus. *(MagnetismTests › Attraction_UneChargeParTraction_SansDelaiEntreDeux, Attraction_SansCharge_Refusee ; Play Mode : 3.00 → 2.03 → 1.07 → 0.11, 4e traction refusée)*
- [x] Recharge : 1 charge toutes les 4 s (D-024 ; 3 s avant), une par une, plafonnée à 3 ; séparées des charges de mouvement. *(MagnetismTests › Attraction_UneChargeToutesLes4s_UneParUne, Attraction_ChargesSepareesDesChargesDeMouvement ; TelekinesisTests › Reglages_AttractionUneChargeToutesLes4s)*
- [x] Aucune charge de mouvement dépensée. *(MagnetismTests › AucuneChargeDeMouvementDepensee ; Play Mode : 3.0 → 3.0)*
- [x] Répulsion : rocket jump 14 m/s ; vitesse horizontale jamais en baisse ; gain plafonné à 20 m/s. *(MagnetismTests › Repulsion_SurLeSol_RocketJump, Repulsion_SolSous25ms_VitesseHorizontaleIntacte, Repulsion_MurDevant_PasDePerte, Repulsion_MurDerriere_GainPlafonneA20, Repulsion_MurDerriere_Au-dela_DuPlafond_Conservee ; Play Mode : 25 → 25, 10 → 20, 15 → 15, 25 → 25)*
- [x] Répulsion vers le haut : la chute est annulée. *(MagnetismTests › Repulsion_VersLeHaut_AnnuleLaChute)*
- [x] Répulsion à charge unique : refusée 1 s après, de nouveau prête à 5 s (appui mémorisé) ; pas de cumul après une longue attente. *(MagnetismTests › Repulsion_UneSeuleCharge_Recharge5s, Repulsion_NeSAccumulePas ; Play Mode : seconde répulsion 1 s après refusée)*
- [x] Portée de la répulsion 40 m, et marquage de la visée seulement à portée. *(MagnetismTests › Repulsion_Portee40m_A38mAcceptee, Repulsion_Portee40m_A45mRefusee ; Play Mode : 38.0 m appliquée, 44.0 m refusée)*
- [x] HUD : 2e rangée, charges d'attraction en bleu, répulsion en orange avec le temps restant ; marque de visée au viseur. *(capture de la vue Game, 2026-11-12)*
- [x] Dash puis traction : vitesse de sortie du dash gardée, pas le pic. *(MagnetismTests › Dash_PuisTraction_GardeLaSortieDuDashPasLePic ; Play Mode : pic 30.3, sortie 12.0 → traction 13.30 m/s)*
- [x] Glissade : la traction ne fait rien perdre ; une répulsion horizontale laisse la glissade continuer. *(MagnetismTests › Glissade_TractionSansPerte, Glissade_RepulsionHorizontale_LaGlissadeContinue ; Play Mode : 13.90 → 15.23 m/s)*
- [x] États de l'orbe : Prête → EnUsage → Recharge → Prête, pour chaque mode. *(MagnetismTests › Etats_PreteEnUsageRecharge)*
- [x] Orbe visible (couleurs, recharge, trait), ressenti de la traction, charges et répulsion. *(playtest de l'utilisateur, 2026-11-12 : « tout est bon »)*

Retour visuel et sonore (partie 2) :

- [x] Anneau bleu seulement si une traction est possible (surface à portée, charge entière, pas déjà en traction) ; anneau orange seulement si la répulsion l'est. *(`Assets/_Project/Tests/EditMode/MagnetismFeedbackTests.cs` › AnneauBleu_TractionPossible, AnneauOrange_RepulsionPossible)*
- [x] Refus : le groupe du mode refusé clignote en rouge 0.3 s, l'autre non ; branché sur un vrai refus de l'orbe. *(MagnetismFeedbackTests › Refus_ClignoteEnRouge03s_SeulementLeModeRefuse, Refus_ReelDeLOrbe_FaitClignoterLeGroupe ; capture : groupe bleu en rouge)*
- [x] Sons synthétisés non vides, durées attendues, sans saturation ; bourdonnement bouclé sans claquement ; en jeu, il joue pendant la traction (hauteur 1.64 à 28 m/s). *(MagnetismFeedbackTests › Sons_NonVidesEtDansLesBornes, Bourdonnement_BoucleSansClaquement, Sons_DeviennentDesAudioClips ; Play Mode 2026-11-12)*
- [x] Bloom à seuil 1 dans la scène de test, post-process activé sur la caméra du joueur, réglages du retour assignés. *(MagnetismFeedbackTests › Bloom_ConfigureDansLaSceneDeTest, PrefabJoueur_PostProcessEtRetourBranches)*
- [x] Lueur de l'orbe et du trait, ancre visible à 45 m, éclat orange, anneau orange au viseur. *(captures de la vue Game, 2026-11-12)*
- [x] Ressenti : lisibilité à pleine vitesse, sons supportables. *(playtest de l'utilisateur, 2026-11-12 : « c'est bon » ; sons provisoires)*

Télékinésie et échange (étape 4, partie 1, D-023) : l'échange et l'endurance ont été retirés par D-024 ;
les critères ci-dessous qui en dépendaient sont remplacés par ceux de l'orbe unique.

- [x] Tenir : l'objet rejoint le point de maintien (écart 0.00 m) ; il ne touche pas le porteur ; contre un mur, il bute sans traverser ; attrapé à 12 m, il n'est pas lâché en chemin ; le lustre accroché tombe une fois posé. *(Play Mode, 2026-11-12)*
- [x] Avec de vrais corps (scène isolée) : un objet attrapé à 10 m et à 55 m vole jusqu'au point de maintien sans dépasser la vitesse d'approche, puis y reste. *(`Assets/_Project/Tests/EditMode/TelekinesisPhysicsTests.cs` › Physique_ObjetA10m_VientEtResteAuPointDeMaintien, Physique_ObjetA55m_VientEtResteAuPointDeMaintien)*
- [x] Le lancer ajoute la vitesse du joueur. *(TelekinesisTests › Lancer_VitesseDuJoueurAjoutee ; Play Mode : 30.0 m/s à l'arrêt, 43.5 m/s lancé à 15 m/s)*
- [x] Playtest partie 1 (utilisateur, 2026-11-12) : retour de design → une seule orbe (D-024). Dégâts selon la vitesse non testés, faute d'ennemis (partie 2).

Une seule orbe (D-024) :

- [x] Les quatre actions marchent en même temps : traction en tenant un objet, puis lancer pendant la traction (la traction continue) ; répulsion en tenant un objet (il reste tenu). *(`Assets/_Project/Tests/EditMode/TelekinesisTests.cs` › OrbeUnique_TractionEnTenantUnObjet_PuisLancerEnArrivant, OrbeUnique_RepulsionEnTenantUnObjet_LObjetResteTenu)*
- [x] Chaque touche a son action : F n'attire pas, A n'attrape pas ; le clic gauche sans objet ne fait rien (ni attraper, ni refus, ni magnétisme). *(TelekinesisTests › OrbeUnique_ChaqueToucheSonAction, ClicGauche_SansObjet_NeFaitRien)*
- [x] Commandes : `Attract` = A (AZERTY, `<Keyboard>/q`) et RB, `Repel` = clic droit et LT, `Grab` = F et X, `Throw` = clic gauche et RT ; actions d'échange supprimées ; aucune touche partagée entre deux actions. *(TelekinesisTests › Commandes_OrbeUnique_ClavierAzertyEtManette)*
- [x] F attrape (objet tenu sans maintenir), F de nouveau le pose ; refus : trop lourd, rien, hors de portée. *(TelekinesisTests › Attraper_F_ObjetTenuSansMaintenir, Relacher_F_PoseLObjet, Attraper_Refus_TropLourd_Rien, Attraper_HorsDePortee_Refuse)*
- [x] Clic gauche lance dans la direction visée. *(TelekinesisTests › Lancer_ClicGauche_RelacheLObjetDansLaVisee)*
- [x] Pas d'endurance : 12 cycles attraper-lancer de suite sans refus ; seul reste le délai de 0.3 s après un lancer (appui mémorisé). *(TelekinesisTests › PasDEndurance_AttraperEtLancerEnChaine, Recharge_ApresUnLancer)*
- [x] Couleur de l'orbe : prête → violet (tenu) → bleu (traction, l'emporte sur l'objet tenu) → vert (lancer, éclat) → bleu ; orange (répulsion, éclat) puis violet ; terne sans charge d'attraction, mais l'objet tenu passe avant. *(TelekinesisTests › Couleur_PreteTractionTenuLancer, Couleur_RepulsionEclatSurLObjetTenu, Couleur_RechargeSansChargeDAttraction)*
- [x] La visée du magnétisme ignore l'objet tenu, pas les autres objets. *(TelekinesisPhysicsTests › Physique_ViseeDuMagnetisme_IgnoreLObjetTenu)*
- [x] L'objet tenu reste devant le joueur à grande vitesse. Avec de vrais corps et des trajectoires synthétiques : écart 0.000 m en traction de 0 à 30 m/s et à 40 m/s, 0.15 m en bunny hop à 20 et 26 m/s en virage de 90°/s (retard dû à la rotation de la vue). *(TelekinesisPhysicsTests › Physique_ObjetTenu_TractionDe0a30ms_ResteDevant, Physique_ObjetTenu_TractionA40ms_ResteDevant, Physique_ObjetTenu_BunnyHopA20ms_ResteDevant, Physique_ObjetTenu_BunnyHopA26ms_ResteDevant)* Avec le vrai joueur en Play Mode (touches F, A, Z et espace simulées dans l'Input System) : caisse de 25 kg tenue, **traction à 30 m/s : 0.000 m** (0.55 m juste après une rotation brusque de la vue de 30°, 0.07 m 0.2 s plus tard) ; **bunny hop à 20 m/s en virage de 30°/s : 0.05 m**, sauts et atterrissages compris ; contre un mur, la caisse bute (2 à 3 m d'écart, voulu). Avant correction, l'objet prenait un tick d'avance (0.47 m à 40 m/s). *(Play Mode, 2026-11-12)*
- [ ] Ressenti : combos traction + objet, lisibilité de la couleur de l'orbe et des anneaux, commandes. *(playtest)*

## Playtest — une seule orbe (checklist courte)

Commandes : **A** maintenu = traction ; **clic droit** = répulsion ; **F** = attraper / relâcher ; **clic
gauche** = lancer. Manette : RB, LT, X, RT. F3 : debug (commandes et derniers appuis). F12 : zone
Télékinésie ; F11 : grande salle.

1. **Commandes :** chaque touche fait son action, et seulement elle ; la molette, Y et les touches 1 et 2 ne
   font plus rien ; le clic gauche sans objet ne fait rien.
2. **Combo :** attrape une caisse (F), tracte-toi vers le haut du mur de la grande salle (A), lance la caisse en
   arrivant (clic gauche) : la caisse reste devant toi pendant la traction, sans trembler ni prendre d'avance ?
3. **Bunny hop et course :** même chose en bunny hop et en dash, en tournant : la caisse suit bien ?
4. **Couleur de l'orbe :** bleu en traction (même en tenant un objet), orange à la répulsion, violet en tenant,
   éclair vert au lancer. Lisible à pleine vitesse ?
5. **Viseur :** anneaux bleu et orange, et un troisième à l'extérieur (violet : attrapable, vert : lancer
   possible, rouge : refus, par exemple sur la caisse de 60 kg).
6. **Plus d'endurance :** enchaîne attraper et lancer sans limite, à part le court délai après un lancer ; plus
   de barre violette.
7. **Attraction :** les charges bleues reviennent en 4 s chacune : trop lent, trop rapide ?

## History

- 2026-11-12 : magnétisme, partie 1.
- 2026-11-12 : v2 après playtest (D-021).
- 2026-11-12 : v3 : charges d'attraction, répulsion unique, marquage, HUD.
- 2026-11-12 : partie 2, retour visuel et sonore.
- 2026-11-12 : télékinésie et échange, partie 1.
- 2026-11-12 : une seule orbe (D-024) : actions simultanées, couleur selon l'action, sans endurance, attraction en 4 s ; objet tenu sans avance à grande vitesse.
