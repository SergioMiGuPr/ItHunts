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

- [x] Playtest orbe unique : tout est bon, sauf la prise à distance en mouvement → aide à la visée (D-026). *(playtest de l'utilisateur, 2026-11-12)*

Aide à la visée de la prise (D-026) :

- [x] Cône : 8° jusqu'à 5 m, 3° à 60 m, linéaire entre les deux ; coupé si `aimAssist` est décoché. *(`Assets/_Project/Tests/EditMode/TelekinesisTests.cs` › Aide_ConeSelonLaDistance, Aide_ReglagesParDefaut)*
- [x] Sélection dans le cône et pas au-delà ; le plus proche du viseur, pas du joueur ; jamais à travers un mur ; un objet visible au-dessus d'un muret visé est pris ; ni derrière ni loin sur le côté ; objets trop lourds seulement au rayon exact. *(`Assets/_Project/Tests/EditMode/TelekinesisPhysicsTests.cs` › Aide_SelectionneUnObjetDansLeCone_PasAuDela, Aide_LePlusProcheDuViseur_PasLePlusProcheDuJoueur, Aide_JamaisATraversUnMur, Aide_ObjetVisibleACoteDUnMurViseDevant, Aide_PasDerriereNiLoinSurLeCote, Aide_PasPourLesObjetsTropLourds_MaisLeRayonExactLesMontre)*
- [x] Mesuré en Play Mode (caisse de 25 kg, vraies touches simulées), écart toléré entre le viseur et le centre de la caisse, avec / sans aide : **10 m : 10.3° (1.8 m) / 2.6° (0.45 m)** ; **30 m : 6.6° (3.5 m) / 0.8° (0.4 m)** ; **59 m : 3.5° (3.6 m) / 0.4° (0.4 m)**. Prise réelle avec F à 80 % de l'écart toléré : réussie aux trois distances. **En bunny hop à 20 m/s**, visée qui oscille de ±3° : caisse sélectionnée **100 % / 71 %** du temps à 10 m, **93 % / 29 %** à 30 m, **86 % / 17 %** à 59 m ; F pressé avec 2.5° d'erreur : pris aux trois distances avec l'aide, seulement à 10 m sans. *(Play Mode, 2026-11-12)*

Dégâts, partie 2 (D-025) :

- [x] Formule : 25 kg à 30 m/s = 55, à 45 m/s = 92.5 ; 5 kg à 30 m/s = 11 ; rien au seuil de 8 m/s ; paliers 25 et 60. *(`Assets/_Project/Tests/EditMode/CombatTests.cs` › Degats_MasseFoisVitesseAuDessusDuSeuil, Degats_Paliers, Reglages_ParDefaut)*
- [x] Chute : seuil de 3 m/s si l'impact est surtout vertical et l'objet au-dessus (25 kg tombée de 4 m ≈ 14.6). *(CombatTests › Chute_SeuilPlusBas, Chute_ReconnueSeulementSiVerticaleEtObjetAuDessus)*
- [x] PV : dégâts pris = PV restants, mort une seule fois, plus de dégâts mort, réapparition après 3 s ; un même objet ne blesse la même cible qu'une fois toutes les 0.3 s ; chaque coup est publié. *(CombatTests › PV_DegatsMortUneFoisPuisReapparition, Intervalle_UnCoupParObjetEtParCible, Coup_RetireLesPV_PublieLeCoup_RespecteLIntervalle)*
- [x] Tir tendu : sans gravité après un lancer, gravité ensuite ; tout de suite après une pose. *(CombatTests › TirTendu_SansGraviteApresUnLancer_PasApresUnePose)*
- [x] Le magnétisme ne s'accroche jamais à un ennemi. *(CombatTests › Magnetisme_NeSAccrochePasAUnEnnemi ; Play Mode : traction visée sur un factice refusée, « rien à portée »)*
- [x] Scène : 4 factices (dont 1 mobile), retour des dégâts, `ImpactDamager` sur chaque objet attrapable ; chiffres de dégâts sur le prefab joueur ; sons non vides. *(CombatTests › Scene_EnnemisFacticesEtObjetsQuiBlessent, Sons_ImpactEtMort_NonVidesSansSaturation)*
- [x] Vrais lancers en Play Mode (F puis clic gauche simulés) sur un factice fixe : 25 kg à 10 m et 20 m → **55 dégâts à 30.0 m/s** ; 5 kg à 10 m et 20 m → **11** ; 25 kg lancée en course (11 m/s) → **79.6 à 39.8 m/s** ; à 29 m → **42.5 à 25 m/s** (le tir tendu s'arrête à 15 m, la caisse retombe) ; caisse de 25 kg tombée de 4 m → **13.8 à 8.5 m/s** (chute) ; mort puis réapparition entre 2.5 et 3.2 s ; factice mobile à 6 m/s. Avant le tir tendu : à 20 m, la caisse touchait le sol d'abord (24.8 m/s) et la caisse de 5 kg ratait à 10 m. *(Play Mode, 2026-11-12)*
- [ ] Ressenti : aide à la visée, dégâts, retour des coups. *(playtest)*

- [x] Playtest aide à la visée et partie 2 : dégâts bons, gravité des objets à 20 m/s² ; à corriger : lancer de travers en mouvement → D-027. *(playtest de l'utilisateur, 2026-11-12)*

Après le playtest de la partie 2 (D-027) :

- [x] Lancer : la direction est toujours celle du viseur ; seule la part de la vitesse du joueur dans cette direction s'ajoute, jamais en négatif. *(`Assets/_Project/Tests/EditMode/TelekinesisTests.cs` › Lancer_DirectionToujoursCelleDuViseur_SeuleLaPartUtileDeLaVitesseSAjoute)*
- [x] Écart entre le point sous le viseur au clic et le point d'impact sur un mur, caisse de 25 kg, en Play Mode (vraies touches simulées). **Avant** (vitesse du joueur ajoutée en entier, gravité 9.81) / **après** (part utile seulement, rayon depuis les yeux du début du tick, gravité 20) : *(Play Mode, 2026-11-12)*

  | Situation | 10 m | 20 m | 30 m |
  |---|---|---|---|
  | À l'arrêt | 0.06 → 0.06 m | 0.05 → 0.10 m | 0.89 → 1.82 m (vertical : retombée après le tir tendu) |
  | Course de côté (7.5 m/s) | **2.01 → 0.07 m** | **4.50 → 0.10 m** | **7.06 → 1.82 m** (latéral 7.00 → 0.00) |
  | Bunny hop (20 m/s, vers la cible) | 0.06 → 0.03 m | 0.20 → 0.01 m | 2.46 → 0.01 m (lancer à 50 m/s) |
  | Traction (20 m/s, en diagonale) | **5.70 → 0.13 m** | **8.66 → 0.06 m** | **10.30 → 0.15 m** |

  Écart latéral restant : au plus 0.11 m. Étape intermédiaire (part utile seulement, rayon depuis les yeux après le mouvement) : 0.13 m de côté et 0.2 à 0.4 m en traction, soit un tick de déplacement.
- [x] Gravité des objets à 20 m/s². *(`Assets/_Project/Tests/EditMode/CombatTests.cs` › Lancer_RayonDuClicDepuisLesYeuxDuDebutDuTick_ReglagesD027)*
- [x] Zone : rayon 1.3 / 2.5 / 3.4 m (5 / 25 / 40 kg), 4 m imposé ; 60 % au centre, 25 % de ça au bord, rien au-delà. *(CombatTests › Zone_RayonSelonLaMasse_DegatsDegressifs)*
- [x] Durabilité : 1 / 2 / 3 / 3 / 5 lancers (5 / 15 / 20 / 25 / 40 kg), imposée par objet ; la petite caisse casse au premier lancer avec sa zone (l'ennemi proche touché, pas le lointain) ; 25 kg casse au 3e ; un rebond, une chute non lancée ou un lancer sous le seuil ne comptent pas ; le lustre casse en tombant, zone de 4 m. *(CombatTests › Durabilite_SelonLaMasse, Durabilite_PetiteCaisseCasseeAuPremierLancer_AvecZone, Durabilite_CaisseDe25kg_TroisLancers_LeTroisiemeLaCasse, Durabilite_UnLancerNeCompteQuUneFois_EtPasLesChocsNonLances, Lustre_CasseAuPremierImpactMemeTombe_ZoneDe4m)*
- [x] Onde : 16 m/s (15 kg ou ennemi de 80 kg au centre), 20.8 (5 kg), 5.6 (60 kg), moitié au bord, rien au-delà de 4 m, jamais vers le bas ; elle repousse objets et ennemis, décroche le lustre sans le pousser, épargne l'objet tenu. *(CombatTests › Onde_VitesseSelonMasseEtDistance, Onde_RepousseObjetsEtEnnemis_DecrocheLeLustre_PasLObjetTenu)*
- [x] Étourdissement : repoussé à 12 m/s contre un mur, étourdi (sans le traverser), prêt pour la prise ; à 4 m/s, non. *(CombatTests › Etourdi_SiLEnnemiPercuteUnMurAssezVite, PasEtourdi_SiLEnnemiArriveTropLentement)*
- [x] Visée du magnétisme sur un ennemi : signalée ; pas de traction (ni d'anneau bleu), la répulsion part. *(CombatTests › Magnetisme_VisseeSurUnEnnemi_Signalee, Magnetisme_PasDeTractionVersUnEnnemi_MaisLaRepulsionOui)*
- [x] En Play Mode (2026-11-12) : caisse de 25 kg sur un factice → **55** direct et **20.6 de zone** sur le voisin à 1.6 m ; durabilité 3/3 → 2/3 → 1/3 → cassée au 3e lancer ; caisse de 5 kg cassée au premier, revenue après 4 s ; onde sur le factice mobile → **12.3 m/s**, projeté contre le mur, **étourdi 1.5 s** ; factice fixe repoussé contre le mur à **10.3 m/s** → étourdi, libéré après 1.5 s ; lustre décroché par l'onde → tombe droit, **30 dégâts de chute** à 15 m/s sur le factice dessous, casse, zone de 4 m. Corrigé pendant ces mesures : durabilité lue avant la masse (1 lancer pour tout), lustre poussé de côté par l'onde, lustre trop bas (le factice dessous était dans l'onde).
- [x] Ressenti : visée du lancer en mouvement, répulsion (onde) et étourdissement contre un mur validés. *(playtest de l'utilisateur, 2026-11-12)*

Onde en cône (D-029) et prise des ennemis étourdis (D-030) :

- [x] Cône : 16 m/s tout près, ×0.75 à 4 m, ×0.5 à 8 m, rien au-delà ; même force au bord (30°), rien au-delà de 30°. *(`Assets/_Project/Tests/EditMode/CombatTests.cs` › Onde_VitesseSelonMasseDistanceEtAngle)*
- [x] Dans le cône : caisse et factice repoussés ; derrière le joueur, sur le côté (59°), au-delà de 8 m, derrière un mur, ou tenu : rien. *(CombatTests › Onde_ConeDepuisLeJoueur_DedansDehorsDerriereUnMur)*
- [x] Lustre décroché en visant vers le haut par en dessous (≈ 6.3 m), sans poussée ; traqueur jamais repoussé ni étourdi. *(CombatTests › Onde_LustreDecrocheEnVisantVersLeHaut_TraqueurExclu)*
- [x] Prise : un factice non étourdi est refusé ; étourdi, il est pris (80 kg, sans limite de masse), aussi par l'aide à la visée ; le traqueur jamais ; déjà tenu, personne d'autre ne le prend. *(CombatTests › Prise_SeulUnEnnemiEtourdi_JamaisLeTraqueur)*
- [x] Ennemi tenu : libéré seul à 3 s ; lancé comme un objet. *(`Assets/_Project/Tests/EditMode/TelekinesisTests.cs` › EnnemiTenu_SansLimiteDeMasse_SeLibereSeulAuBoutDe3s, EnnemiTenu_LanceCommeUnObjet)*
- [x] En Play Mode : vitesses du cône, caisse de 15 kg / factice de 80 kg : **2 m : 14.0 / 14.0 m/s (bord 14.1 / 14.2)** ; **5 m : 11.0 / 11.0 (bord 11.1 / 11.1)** ; **7.9 m : 8.1 / 8.1 (bord 8.3 / 8.3)** ; vrai clic droit : factice à 4 m repoussé à 11.8 m/s, joueur propulsé à 13.8 m/s ; lustre visé par en dessous à 5.9 m : décroché, 30 dégâts sur le factice dessous ; prise refusée sans étourdissement, acceptée étourdi ; factice lancé sur un autre à 11 m : 100 → 0 pour les deux (176 dégâts calculés) ; libération seule à 2.99 s, puis immunisé (ni ré-étourdi, ni repris) ; lancé contre un mur à 30 m/s : 100 → 0. Corrigé pendant ces mesures : un factice au repos (devenu attrapable) blessait celui qui le percutait. *(Play Mode, 2026-11-12)*
- [x] Playtest du cône de 8 m : pas assez fort ni assez loin → répulsion façon aimant (D-029 mise à jour). *(playtest de l'utilisateur, 2026-11-12)*

Répulsion façon aimant (D-029 mise à jour) :

- [x] Onde de choc : 22 m/s jusqu'à 2 m, ×0.8 à 8 m, ×0.6 à 14 m, rien au-delà ; même force au bord du cône ; multiplicateurs de masse. *(`Assets/_Project/Tests/EditMode/CombatTests.cs` › Onde_VitesseSelonMasseDistanceEtAngle ; `Assets/_Project/Tests/EditMode/RepulsionHoldTests.cs` › Reglages_RepulsionMaintenue)*
- [x] Propulsion seulement à l'appui : même vitesse du joueur, tick par tick, avec et sans maintien ; aucune nouvelle répulsion pendant le maintien. *(RepulsionHoldTests › Propulsion_SeulementALAppui_IdentiqueAvecEtSansMaintien)*
- [x] Maintien : fin au relâchement (recharge de 5 s à partir de la fin), fin à 0.8 s bouton tenu, coupé par une prise, un lancer ou une traction. *(RepulsionHoldTests › Maintien_SArreteAuRelachement_RechargeDepuisLaFin, Maintien_SArreteA08s, Maintien_CoupeParUneAutreActionDeLOrbe)*
- [x] Poussée continue plafonnée à 25 m/s dans l'axe (sans gravité : 25.0 au plus, atteints). *(RepulsionHoldTests › Poussee_ContinueNeDepassePas25ms)*
- [x] Contre un mur : plaqué en continu, ni étourdi ni blessé ; projeté par l'onde, étourdi une seule fois à l'impact. *(RepulsionHoldTests › Poussee_PlaqueContreUnMur_NiDegatsNiEtourdissementContinus, Poussee_EtourdiUneSeuleFoisALImpact_PuisImmunite)*
- [x] Traqueur exclu, à l'appui et en continu. *(RepulsionHoldTests › Poussee_TraqueurExclu)*
- [x] En Play Mode, vrais clics droits, **avant** (cône de 8 m, 16 m/s, sans maintien) → **après** ; vitesse juste après l'appui et distance parcourue avant l'arrêt : *(Play Mode, 2026-11-12)*

  | Cible, distance | Appui avant | Appui après | Maintien 0.8 s après |
  |---|---|---|---|
  | Caisse 15 kg, 2 m | 13.1 m/s, 7.1 m | 22.8 m/s, 22.1 m | 22.1 → max 26.0 m/s, 22.8 m |
  | Caisse 15 kg, 7 m | 8.4 m/s, 2.3 m | 18.0 m/s, 12.3 m | 18.9 → 22.5 m/s, 23.5 m |
  | Caisse 15 kg, 14 m | rien | 13.1 m/s, 6.3 m | 13.1 m/s, 6.3 m (sort aussitôt du cône) |
  | Factice 80 kg, 2 m | 13.4 m/s, 10.2 m | 21.9 m/s, 27.0 m | 22.8 → 25.8 m/s, 33.9 m |
  | Factice 80 kg, 7 m | 8.6 m/s, 4.3 m | 18.3 m/s, 19.0 m | 19.1 → 19.5 m/s, 20.8 m |
  | Factice 80 kg, 14 m | rien | 13.3 m/s, 10.0 m | 13.3 m/s, 10.0 m |

  Propulsion du joueur : 13.3 à 14.0 m/s, la même avec un appui et avec un maintien. Plafond : la poussée continue s'arrête à 25 m/s dans son axe ; les 0.4 à 1.0 m/s de plus mesurés viennent de la gravité (axe tourné vers le bas, sur une cible au sol). Capture : anneaux continus dans le cône, orbe orange, paume tendue, « 0.5 s » et barre de maintien sous les jauges. Corrigé pendant ces mesures : pour une cible traversée par l'axe du cône, la distance était celle de son centre (une caisse à 13.8 m comptait 14.2 m) ; c'est maintenant sa surface avant.
- [x] Ressenti : répulsion façon aimant (appui et maintien), coupures, mur et lustre validés. *(playtest de l'utilisateur, 2026-11-12)*

Dégâts contre un mur et objets repoussés (D-031) :

- [x] Contre un mur : 5 PV par m/s au-delà de 6 m/s, 40 au plus ; une fois par impact, pas pendant l'immunité, traqueur exclu ; projeté par l'onde : dégâts une seule fois à l'impact, rien en continu. *(`Assets/_Project/Tests/EditMode/RepulsionHoldTests.cs` › Mur_5PVParMsAuDelaDe6_Plafond40, Mur_UnSeulDegatParImpact_PasPendantLImmunite_TraqueurExclu, Poussee_EtourdiUneSeuleFoisALImpact_PuisImmunite)*
- [x] Objet repoussé : 15 kg à 20 m/s sur un factice → 9 PV (×0.5), sans zone, un lancer de durabilité en moins (pas contre un mur) ; inoffensif sous 6 m/s, après 1 s, et pour un joueur (aucun joueur n'est une cible). *(RepulsionHoldTests › Repousse_BlesseCommeUnLancerFois05_SansZone_DurabiliteSurEnnemiSeulement, Repousse_InoffensifSous6ms_EtApres1s_EtPourLeJoueur)*
- [x] En Play Mode (vrais clics droits, 2026-11-12) : factice repoussé contre le mur de la grande salle, à 1.5 m devant le mur, joueur à **2 m : impact 22.3 m/s → 40 PV** (plafond) ; **7 m : 18.2 m/s → 40 PV** ; **13.8 m : 12.9 m/s → 34.7 PV**. Caisse de 15 kg repoussée sur un factice placé à 15.5 m (hors du cône) : **à 2 m, arrivée à 10.9 m/s → 2.2 PV** ; **à 7 m, 11.1 m/s → 2.3 PV** (elle ralentit en glissant au sol) ; un lancer de durabilité perdu à chaque fois. Lustre visé par en dessous à 5.9 m : décroché, 30 PV sur le factice dessous, cassé. *(Play Mode, 2026-11-12)*
- [ ] Ressenti : dégâts contre un mur, objets repoussés. *(playtest)*

## Playtest — lancer, zone, durabilité, onde (checklist courte)

Commandes : A traction, clic droit répulsion (onde), F attraper / relâcher, clic gauche lancer. F12 : zone
Télékinésie (3 factices fixes, 1 mobile, lustre sous le portique de 8 m).

1. **Lancer en mouvement :** en course de côté, en bunny hop et en traction, la caisse part pile où vise le viseur à
   10 et 20 m (à 30 m, elle retombe un peu après 15 m). En courant vers la cible, elle frappe plus fort.
2. **Zone :** une caisse sur un factice blesse aussi un factice voisin (chiffre plus petit) ; cercle orange au sol ;
   rien à travers un mur.
3. **Durabilité :** en tenant une caisse, des carrés au-dessus d'elle (5 kg : 1, 15 kg : 2, 25 kg : 3, 40 kg : 5) ;
   un carré s'éteint à chaque lancer ; au dernier, elle se casse (éclat, débris, son) et revient 4 s plus tard.
4. **Lustre :** vise-le avec la répulsion : il se décroche et tombe sur le factice dessous (≈ 30 dégâts + zone de
   4 m), et se casse. Lancé, il se casse aussi au premier impact.
5. **Onde :** répulsion au sol près d'un factice ou de caisses : ils sont repoussés (les lourds moins), cercle bref ;
   tu es toujours propulsé.
6. **Étourdissement :** repousse un factice contre un mur (le mobile passe devant le mur de la grande salle) :
   étoiles, lueur jaune, son ; il ne bouge plus pendant 1.5 s ; le mobile s'arrête puis rejoint son trajet.
7. **Garde-fous :** la traction refuse toujours un factice (pas d'anneau bleu) ; tes objets ne te blessent jamais.
8. **Gravité à 20 m/s² :** les objets tombent plus vite qu'avant. Est-ce que ça te convient ?

## History

- 2026-11-12 : magnétisme, partie 1.
- 2026-11-12 : v2 après playtest (D-021).
- 2026-11-12 : v3 : charges d'attraction, répulsion unique, marquage, HUD.
- 2026-11-12 : partie 2, retour visuel et sonore.
- 2026-11-12 : télékinésie et échange, partie 1.
- 2026-11-12 : une seule orbe (D-024) : actions simultanées, couleur selon l'action, sans endurance, attraction en 4 s ; objet tenu sans avance à grande vitesse.
- 2026-11-12 : aide à la visée de la prise (D-026) ; dégâts d'impact, tir tendu, chutes, ennemis factices (D-025).
- 2026-11-12 : D-027 : lancer vers le viseur (mesures avant / après), gravité 20, zone, durabilité, onde, étourdissement.
