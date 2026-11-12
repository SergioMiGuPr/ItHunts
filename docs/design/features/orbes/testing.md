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

Retour visuel et sonore (partie 2) :

- [x] Anneau bleu seulement si une traction est possible (surface à portée, charge entière, pas déjà en traction) ; anneau orange seulement si la répulsion l'est. *(`Assets/_Project/Tests/EditMode/MagnetismFeedbackTests.cs` › AnneauBleu_TractionPossible, AnneauOrange_RepulsionPossible)*
- [x] Refus : le groupe du mode refusé clignote en rouge 0.3 s, l'autre non ; branché sur un vrai refus de l'orbe. *(MagnetismFeedbackTests › Refus_ClignoteEnRouge03s_SeulementLeModeRefuse, Refus_ReelDeLOrbe_FaitClignoterLeGroupe ; capture : groupe bleu en rouge)*
- [x] Sons synthétisés non vides, durées attendues, sans saturation ; bourdonnement bouclé sans claquement ; en jeu, il joue pendant la traction (hauteur 1.64 à 28 m/s). *(MagnetismFeedbackTests › Sons_NonVidesEtDansLesBornes, Bourdonnement_BoucleSansClaquement, Sons_DeviennentDesAudioClips ; Play Mode 2026-11-12)*
- [x] Bloom à seuil 1 dans la scène de test, post-process activé sur la caméra du joueur, réglages du retour assignés. *(MagnetismFeedbackTests › Bloom_ConfigureDansLaSceneDeTest, PrefabJoueur_PostProcessEtRetourBranches)*
- [x] Lueur de l'orbe et du trait, ancre visible à 45 m, éclat orange, anneau orange au viseur. *(captures de la vue Game, 2026-11-12)*
- [x] Ressenti : lisibilité à pleine vitesse, sons supportables. *(playtest de l'utilisateur, 2026-11-12 : « c'est bon » ; sons provisoires)*

Télékinésie et échange (étape 4, partie 1) :

- [x] Échange : la molette ou Y bascule, 1 et 2 choisissent ; seule l'orbe active reçoit les boutons. *(`Assets/_Project/Tests/EditMode/TelekinesisTests.cs` › Echange_BasculeEtChoixDirect, TelekinesieActive_LeMagnetismeNeRecoitPasLesBoutons, MagnetismeActif_LaTelekinesieNeRecoitPasLesBoutons)*
- [x] Échanger arrête la traction sans perte d'élan et lâche l'objet tenu. *(TelekinesisTests › Echange_ArreteLaTraction_SansPerte, Echange_LacheLObjetTenu ; Play Mode : commande « lâcher », gravité rétablie)*
- [x] Attraper coûte 10 et l'objet reste tenu sans maintenir ; refus : trop lourd, rien, hors de portée, pas d'endurance. *(TelekinesisTests › Attraper_Coute10_ObjetTenuSansMaintenir, Attraper_Refus_TropLourd_Rien_PasDEndurance, Attraper_HorsDePortee_Refuse ; Play Mode : caisse de 60 kg refusée)*
- [x] Poser (nouveau clic gauche) est gratuit ; lancer coûte 15, refusé sans endurance (l'objet reste tenu) ou sans rien en main. *(TelekinesisTests › Poser_NouveauClicGauche, Lancer_Coute15_EtRelacheLObjet, Lancer_SansEndurance_GardeLObjet, Lancer_RienEnMain_Refuse)*
- [x] Le lancer ajoute la vitesse du joueur. *(TelekinesisTests › Lancer_VitesseDuJoueurAjoutee ; Play Mode : 30.0 m/s à l'arrêt, 43.5 m/s lancé à 15 m/s)*
- [x] Recharge de 0.3 s après un lancer, avec mémorisation de l'appui ; endurance : rien pendant 0.8 s, puis +25/s, plafond 100. *(TelekinesisTests › Recharge_ApresUnLancer, Endurance_RegenereApresLeDelai)*
- [x] Tenir : l'objet rejoint le point de maintien (écart 0.00 m) et le suit en course à 15 m/s (écart max 0.22 m) ; il ne touche pas le porteur ; contre un mur, il bute sans traverser ; attrapé à 12 m, il n'est pas lâché en chemin ; le lustre accroché tombe une fois posé. *(Play Mode, 2026-11-12)*
- [x] Retour : objet tenu violet, trait violet, orbe violette, anneau vert, barre d'endurance, nom de l'orbe active. *(capture de la vue Game, 2026-11-12)*
- [x] Avec de vraies entrées (touche 2 puis clic gauche simulés dans l'Input System) : l'appui est reçu, l'objet passe en « tenu », sans gravité, vole à 40 m/s et rejoint le point de maintien : à 7 m, écart 0.18 m après 0.1 s puis 0.00 m ; à 55 m, 51.2 → 3.2 m en 1.2 s, 0.00 m à 1.6 s. *(Play Mode, 2026-11-12 ; avant correction, la caisse recevait 37.5 m/s sans bouger : simulation physique restée en mode Script après une mesure)*
- [x] Avec de vrais corps (scène isolée) : un objet attrapé à 10 m et à 55 m vole jusqu'au point de maintien sans dépasser la vitesse d'approche, puis y reste. *(`Assets/_Project/Tests/EditMode/TelekinesisPhysicsTests.cs` › Physique_ObjetA10m_VientEtResteAuPointDeMaintien, Physique_ObjetA55m_VientEtResteAuPointDeMaintien)*
- [ ] Ressenti : tenir, lancer, échanger à pleine vitesse. *(playtest)*

## Playtest — télékinésie et échange, partie 1 (checklist courte)

F12 zone Télékinésie. Molette ou Y : échanger ; 1 / 2 : choisir. Télékinésie active : clic gauche / RT
attrape (puis pose), clic droit / LT lance.

0. **Commandes :** le debug (F3) écrit ce que fait chaque bouton avec l'orbe active et le dernier appui
   reçu.
1. **Repérer :** les objets attrapables brillent faiblement de loin, plus fort à moins de 60 m, vivement
   quand tu les vises (anneau violet) ; la caisse de 60 kg brille en rouge terne et est refusée.
2. **Tenir :** attrape une caisse, de près puis de loin (elle vole jusqu'à toi à 40 m/s), cours, saute,
   tracte-toi : elle suit sans te gêner ? Cache-t-elle
   trop la vue (distance et hauteur réglables dans `Telekinesis_Default`) ?
3. **Lancer :** à l'arrêt puis en pleine course (plus fort) ; vise le mur de la grande salle.
4. **Poser et lustre :** un nouveau clic gauche pose l'objet ; attrape le lustre accroché au portique.
5. **Endurance :** barre violette (10 pour attraper, 15 pour lancer, remonte après 0.8 s) ; refus en
   rouge.
6. **Échange :** en tenant un objet, change d'orbe : il tombe ; en pleine traction, change d'orbe : tu
   gardes ton élan.

## History

- 2026-11-12 : magnétisme, partie 1.
- 2026-11-12 : v2 après playtest (D-021).
- 2026-11-12 : v3 : charges d'attraction, répulsion unique, marquage, HUD.
- 2026-11-12 : partie 2, retour visuel et sonore.
- 2026-11-12 : télékinésie et échange, partie 1.
