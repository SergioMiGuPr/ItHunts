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
- [x] Recharge : 1 charge toutes les 3 s, une par une, plafonnée à 3 ; séparées des charges de mouvement. *(MagnetismTests › Attraction_UneChargeToutesLes3s_UneParUne, Attraction_ChargesSepareesDesChargesDeMouvement)*
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

## Playtest — magnétisme v3 (checklist courte)

F10 zone Magnétisme, F11 grande salle. HUD sous le viseur : 1re rangée charges de mouvement,
2e rangée charges d'attraction (bleu) et répulsion (orange).

1. **Charges d'attraction :** 3 tractions de suite, la 4e est refusée ; une charge revient toutes les
   3 s (HUD bleu). Le rythme te semble-t-il juste ?
2. **Répulsion :** un seul usage, puis 5 s de recharge (HUD orange avec le temps restant) ; un second
   appui pendant la recharge ne fait rien.
3. **Portée et marquage :** le disque orange apparaît au viseur sur toute surface à moins de 40 m (terne
   en recharge) ; une marque s'étend au point d'impact quand tu repousses.
4. **Ensemble :** grande salle, enchaîne traction, relâchement, saut en l'air, répulsion : l'ensemble
   est-il encore fort sans être abusé ?

## History

- 2026-11-12 : magnétisme, partie 1.
- 2026-11-12 : v2 après playtest (D-021).
- 2026-11-12 : v3 : charges d'attraction, répulsion unique, marquage, HUD.
