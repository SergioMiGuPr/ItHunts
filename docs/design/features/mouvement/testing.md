# Mouvement : testing

**Updated:** 2026-11-11 · [Design](design.md) · [Implementation](implementation.md)

## Acceptance criteria

Étape 1 (course et saut). Les tests tournent sur un sol factice : ils prouvent la simulation,
pas le CharacterController.

- [x] La course atteint 7.5 m/s en ≤ 9 ticks (0.15 s) depuis l'arrêt. *(`Assets/_Project/Tests/EditMode/MovementTests.cs` › Course_AtteintLaVitesseMaxEnMoinsDe015s)*
- [x] La friction arrête un joueur lancé à 7.5 m/s en ≤ 24 ticks (0.4 s). *(MovementTests › Friction_ArretCompletEnMoinsDe04s)*
- [x] Un saut complet monte à 1.3 m ± 1 % et atteint son sommet en 0.36 s ± 1 tick. *(MovementTests › Saut_HauteurEtTempsAuSommet)*
- [x] Sans auto-bunny hop, un appui maintenu ne fait sauter qu'une fois, même en retombant au sol. *(MovementTests › Saut_SansAutoBhopUnSeulSautParAppui)*
- [x] Avec un seul saut autorisé, pas de second saut via le coyote time après un saut. *(MovementTests › Saut_PasDeDoubleSautParLeCoyoteTime)*
- [x] Relâcher pendant la montée multiplie la vitesse verticale par 0.5 ; relâcher en descente ne change rien. *(MovementTests › Saut_RelacherPendantLaMonteeCoupeLaVitesse, Saut_RelacherEnDescenteNeChangeRien)*
- [x] Coyote (1 saut, sans auto-bunny hop) : saut accepté 6 ticks après le bord, refusé à 7. *(MovementTests › Coyote_SautAccepte6TicksApresLeBord, Coyote_SautRefuse7TicksApresLeBord ; vérifié qu'il échoue avec coyoteTime = 0.12 s)*
- [x] Buffer (1 saut, sans auto-bunny hop) : un appui 7 ticks avant l'atterrissage fait sauter à l'atterrissage, 8 ticks non. *(MovementTests › Buffer_Appui7TicksAvantAtterrissageSaute, Buffer_Appui8TicksAvantAtterrissageNeSautePas)*
- [x] En l'air, garder la direction de la vitesse n'ajoute rien ; strafer à 90° ajoute de la vitesse. *(MovementTests › Air_TenirLaDirectionDeLaVitesseNAjouteRien, Air_StraferA90DegresAjouteDeLaVitesse ; formule seule, pas un vrai strafe)*
- [x] Les réglages par défaut donnent une impulsion de 7.21 m/s, 6 ticks de coyote, 7 de buffer, 2 sauts, auto-bunny hop activé, contrôle aérien 0.15 et plafond de 50 m/s. *(MovementTests › Parametres_ValeursDuDesign ; vérifié qu'il échoue avec jumpHeight = 1.4 m)*
- [x] Contrôle aérien à 1 : avec W seul, la vitesse s'aligne sur la caméra en un tick, sans perte. *(MovementTests › ControleAerien_A1LaVitesseSuitLaCameraInstantanement)*
- [x] Contrôle aérien à 0 : la direction reste celle de Source. *(MovementTests › ControleAerien_A0RestePurSource)*
- [x] Contrôle aérien par défaut (0.15) : virage partiel au premier tick, aligné à moins de 2° en 0.5 s, sans jamais perdre de vitesse. *(MovementTests › ControleAerien_ParDefautTourneProgressivementSansPerdreDeVitesse)*
- [x] En strafant, le contrôle aérien ne s'applique pas et le strafe ajoute de la vitesse. *(MovementTests › ControleAerien_IgnoreEnStrafantEtLeGainSourceReste)*
- [x] Auto-bunny hop : maintenir le saut fait au moins 3 sauts en 3 s ; désactivé, un seul. *(MovementTests › AutoBhop_MaintenirResauteAChaqueAtterrissage, AutoBhop_Desactive_UnSeulSaut)*
- [x] Maintenir le saut ne déclenche pas de saut en l'air. *(MovementTests › AutoBhop_MaintenirNeDeclenchePasDeSautEnLAir)*
- [x] En auto-bunny hop, une vitesse de 30 m/s est conservée sur 3 s (aucune friction, pas de plafond bas). *(MovementTests › AutoBhop_GardeUneGrandeVitesseSansFriction)*
- [x] Sauts multiples : 2 par défaut (le 3e est refusé), 3 si `maxJumps` = 3. *(MovementTests › SautsMultiples_DeuxParDefaut_LeTroisiemeEstRefuse, SautsMultiples_ReglableATrois)*
- [x] Le saut en l'air garde la vitesse horizontale et annule la chute. *(MovementTests › SautsMultiples_LeSautEnLAirGardeLaVitesseHorizontale)*
- [x] Les sauts se rechargent en retouchant le sol. *(MovementTests › SautsMultiples_RechargesAuSol)*
- [x] Tomber d'un bord sans sauter laisse seulement le saut en l'air. *(MovementTests › SautsMultiples_TomberDUnBordLaisseLeSeulSautEnLAir)*
- [x] La vitesse horizontale est plafonnée à 50 m/s. *(MovementTests › Securite_VitesseHorizontalePlafonnee)*
- [x] Les sauts se rechargent au contact d'un mur (un saut en l'air redevient disponible après un double saut). *(MovementTests › Mur_LeContactRechargeLesSauts)*
- [x] Wall slide : en poussant contre un mur en l'air, la chute plafonne à 3 m/s. *(MovementTests › WallSlide_LimiteLaChuteA3 ; et Play Mode 2026-11-11 contre le mur de 20 m : −3.00 m/s, état WallSlide)*
- [x] Wall slide : la vitesse le long du mur (12 m/s) est conservée, rien n'entre dans le mur. *(MovementTests › WallSlide_ConserveLaVitesseLeLongDuMur)*
- [x] Sans pousser vers le mur, pas de wall slide : chute normale. *(MovementTests › WallSlide_SansPousserOnTombeNormalement)*
- [x] Wall jump : 7.5 m/s loin du mur, élan le long du mur conservé, sans consommer de saut. *(MovementTests › WallJump_ImpulsionLoinDuMurEnGardantLElan ; Play Mode : impulsion jusqu'au mur opposé à 5 m)*
- [x] Pas de contrôle en l'air pendant les 0.15 s qui suivent un wall jump, puis il revient. *(MovementTests › WallJump_VerrouDeDirectionPendant015s)*
- [x] Wall-coyote : wall jump accepté 6 ticks après avoir quitté le mur, refusé à 7. *(MovementTests › WallJump_WallCoyoteAccepte6Ticks, WallJump_WallCoyoteRefuse7Ticks)*
- [x] Pas deux wall jumps de suite sur le même mur ; un autre mur, puis le premier, sont acceptés. *(MovementTests › WallJump_PasDeuxFoisDeSuiteLeMemeMur)*
- [x] Dans une cheminée de 3 m, quatre wall jumps alternés font monter de plus de 2 m. *(MovementTests › WallJump_OnMonteDansUneChemineeDe3m ; murs factices)*
- [x] Wall jump : 1.8 m de hauteur. *(MovementTests › WallJump_MonteDe18m ; Play Mode 2026-11-12 contre le mur de 200 m : 1.80 m)*
- [x] Wall jump : direction entre la normale et l'avant selon `wallJumpForwardBias` (0, 0.5, 1). *(MovementTests › WallJump_DirectionMelangeNormaleEtCamera)*
- [x] Wall jump caméra face au mur : on s'éloigne du mur et on avance le long de la trajectoire. *(MovementTests › WallJump_CameraFaceAuMur_PasVersLeMur)*
- [x] Wall jump : vitesse horizontale ≥ vitesse d'avant + 1 m/s, quelle que soit la caméra. *(MovementTests › WallJump_VitesseConserveePlusBonus ; Play Mode : 15.00 → 20.98 m/s)*
- [x] Wall slide sans perte, caméra à 20° vers le mur, W seul. *(MovementTests › WallSlide_SansPerteAvecLaCameraVersLeMur ; Play Mode 2026-11-12 : 15.00 → 15.00 m/s en 1 s, contre 13.78 avant)*
- [x] Wall slide actif sans pousser quand on longe le mur à 10 m/s, sans perte. *(MovementTests › WallSlide_SActiveSansPousserQuandOnLongeLeMur ; Play Mode : W seul caméra le long du mur, 15.00 → 15.00 m/s, contre 7.50 avant)*
- [x] Tolérance d'angle de l'entrée : 60° accroche, 85° non. *(MovementTests › WallSlide_ToleranceDAngleDeLEntree)*
- [x] Wall run : à 15 m/s le long du mur, chute ≤ 0.5 m/s ; à l'arrêt, 3 m/s. *(MovementTests › WallRun_ChuteRalentieAHauteVitesse)*
- [x] Le mur ne recharge pas les charges de mouvement (sauf option). *(MovementTests › Mur_NeRechargePasLesChargesParDefaut)*
- [x] Charges : 3 au départ ; saut au sol gratuit, saut en l'air 1 charge ; rien sans charge ; recharge 2 s au sol, 4 s en l'air (+1 s le 2026-11-13, dash et saut en l'air ralentis tous les deux). *(MovementTests › Charges_*)*
- [x] Dash : 14 m/s vers l'avant sans entrée, dans la direction de l'entrée sinon ; une vitesse plus haute est redirigée sans perte ; annule la chute ; 0.25 s entre deux dashs ; en l'air : saut + deux dashs puis plus rien. *(MovementTests › Dash_*, Charges_EnLAirSautPuisDeuxDashs_PuisPlusRien ; Play Mode : dash au sol 14.00 m/s, 3 → 2 charges)*
- [x] Dash : 11 ticks (0.18 s), 6.5 m, sortie à 12 m/s depuis l'arrêt. *(MovementTests › Dash_DureDistanceEtVitesseDeSortie_AuSol ; Play Mode 2026-11-12 : 35.5 m/s pendant, 12.0 m/s à 0.2 s, 10.0 m en 0.5 s ; avant : 14 → 7.5 m/s, 4.2 m)*
- [x] Dash en l'air : altitude constante pendant le dash. *(MovementTests › Dash_EnLAirNiGraviteNiChutePendantLeDash ; Play Mode : chute de 1.7 m en 0.5 s contre 4.0 m avant)*
- [x] Pas de chute brutale à la sortie au sol (≥ 12 m/s 0.1 s après). *(MovementTests › Dash_PasDeChuteBrutaleALaSortieAuSol)*
- [x] 8 directions : direction à moins de 0.5° de l'entrée, 6.5 m. *(MovementTests › Dash_HuitDirections(0..7) ; Play Mode : 8/8 identiques, 0° d'écart)*
- [x] En l'air à 20 m/s : plus rapide pendant le dash, puis 20 m/s gardés et redirigés. *(MovementTests › Dash_EnLAirAHauteVitesse_PlusRapidePendantPuisVitesseDAvant ; Play Mode : 20 → 35.5 m/s, contre 20 → 20 avant)*
- [x] Enchaînements : un appui pendant un dash part dès la fin du délai ; 3 charges, le 4e appui est refusé. *(MovementTests › Dash_EnchainementsMemorisesEtCharges ; Play Mode : 3/3 avec des appuis espacés de 0.15 s ou plus, 2/3 à 0.05-0.10 s car un seul appui reste en attente ; avant : 1/3 à 0.05 s, 2/3 à 0.10-0.15 s)*
- [x] Un saut pendant le dash le termine avec la vitesse de sortie. *(MovementTests › Dash_UnSautTermineLeDash)*
- [x] Gain du wall jump plafonné à 20 m/s ; au-delà, vitesse conservée. *(MovementTests › WallJump_GainPlafonneA20(19, 25))*
- [x] Profil progressif : pic sous 32 m/s, baisse à chaque tick, pas de marche à la sortie ; 6.5 m. *(MovementTests › Dash_ProfilProgressif_PicPlusBasEtSansCassure ; Play Mode 2026-11-12 : 30.3 → 14.0 m/s par pas d'≈ 1 m/s, puis 12.0 ; 6.50 m)*
- [x] Profil constant réglable. *(MovementTests › Dash_ProfilConstantReglable)*
- [x] Dash au sol : +1 m/s plafonné à 20, sortie ≥ 12 m/s. *(MovementTests › Dash_AuSolGagneUnMetreParSeconde(10, 15, 19.5, 22) ; Play Mode : 10 → 12, 15 → 16, 19.5 → 20, 22 → 22)*
- [x] Dash en l'air dans 4 directions : vitesse exactement conservée et redirigée, sans chute pendant le dash. *(MovementTests › Dash_EnLAirAucunGainEtRedirection(0, 2, 4, 6) ; Play Mode : 15.00 m/s dans les 4 directions, pic transitoire 27.6 m/s, 0.00 m de chute)*
- [x] Dash-jump réussi pendant le dash ou juste après : +3 m/s ; plafonné à 20. *(MovementTests › DashJump_ReussiPendantLeDash, DashJump_ReussiJusteApresLeDash, DashJump_PlafonneA20 ; Play Mode : 15 → 18, 19 → 20, 22 → 22)*
- [x] Dash-jump raté hors fenêtre : pas de +3, retard affiché. *(MovementTests › DashJump_RateHorsFenetre ; Play Mode : « raté +117 ms »)*
- [x] Pas de dash-jump en l'air. *(MovementTests › DashJump_PasEnLAir)*
- [ ] Le HUD des charges s'affiche sous le viseur. *(pas vérifié à l'écran : pas de capture d'écran par la CLI ; composant présent sur le prefab et sur l'instance de la scène)*
- [x] Le ressenti des murs et du dash. *(playtest de l'utilisateur, 2026-11-12 : « le mouvement de base est validé »)*
- [x] Le bonus de glissade est plafonné par le plafond commun (+2 jusqu'à 20 m/s, rien au-delà). *(MovementTests › Glissade_BonusPlafonneParLePlafondCommun(17, 19, 25))*
- [x] Entrer en glissade à 7.5 m/s donne +2 m/s et baisse la capsule à 0.9 m. *(MovementTests › Glissade_EntreeDonneLeBonusEtBaisseLaCapsule)*
- [x] Sous 6 m/s, la touche accroupit ; la marche accroupie plafonne à 3.5 m/s. *(MovementTests › Glissade_SousLaVitesseMinimaleOnSAccroupit)*
- [x] Le bonus de glissade ne revient qu'après 0.8 s. *(MovementTests › Glissade_LeBonusARechargeDe08s)*
- [x] À plat, depuis la course, la glissade dure plus d'1 s (≈ 6.45 m/s après 1 s) puis s'arrête sous 4 m/s, accroupi si la touche reste tenue. *(MovementTests › Glissade_APlatRalentitLentementPuisSArrete)*
- [x] En descente (30°), la glissade accélère ; en montée, elle freine. *(MovementTests › Glissade_EnDescenteAccelere, Glissade_EnMonteeFreine ; normale de pente simulée, géométrie plate)*
- [x] Relâcher la touche relève le joueur. *(MovementTests › Glissade_RelacherRelevesLeJoueur)*
- [x] Entrer en glissade à 20 m/s garde toute la vitesse, plus le bonus. *(MovementTests › Glissade_EntreeA20ConserveToutLElanPlusLeBonus)*
- [x] Atterrir à 20 m/s en tenant la glissade garde toute la vitesse, plus le bonus. *(MovementTests › Glissade_AtterrirA20DepuisUnBunnyHopConserveLElan)*
- [x] Une glissade lancée à 20 m/s dure entre 5 et 7 s sur le plat. *(MovementTests › Glissade_A20DurePlusieursSecondesAPlat)*
- [x] La décélération est de 3 m/s² à 20 comme à 10 m/s. *(MovementTests › Glissade_DecelerationConstanteQuelleQueSoitLaVitesse)*
- [x] Depuis 7.5 m/s, on traverse en glissade un passage bas de 10 m, touche tenue ou relâchée dessous, sans descendre sous 5 m/s. *(MovementTests › Glissade_TraverseUnPassageBasDe10mDepuis7v5(True/False) ; plafond factice)*
- [x] Sous un plafond bas, on reste en bas et on ne peut pas sauter ; on se relève en sortant. *(MovementTests › Glissade_SousUnPlafondBasOnResteBasEtOnNeSautePas ; plafond factice)*
- [x] Sauter depuis une glissade garde toute la vitesse et relève le joueur. *(MovementTests › Glissade_SauterGardeLaVitesseEtRelève)*
- [x] Atterrir en tenant la touche, à 8 m/s, enchaîne sur une glissade. *(MovementTests › Glissade_AtterrirEnMaintenantLaToucheGlisse)*
- [x] Saut et glissade tenus ensemble à l'atterrissage : le saut gagne. *(MovementTests › Glissade_SautEtGlissadeMaintenusALAtterrissage_LeSautGagne)*
- [ ] Le vrai passage bas de 1.1 m se franchit en glissade et bloque le relevé. *(pas prouvé avec le CharacterController : playtest)*
- [x] Dans `Test_Movement`, le joueur apparaît au sol (état Sol, 60 Hz), la caméra est à 1.6 m, FOV 70°, Cinemachine actif, sans erreur en console. *(Play Mode lancé par la CLI le 2026-11-11, état lu par `script eval`)*
- [x] Un appui de saut injecté dans `PlayerInputReader` fait sauter puis retomber le vrai CharacterController. *(Play Mode 2026-11-11 : saut au tick 2516, retour au sol ; hauteur non mesurée en jeu)*
- [ ] Les touches et la manette pilotent bien le joueur. *(pas prouvé : l'éditeur ignore le clavier simulé quand la fenêtre Game n'a pas le focus ; playtest de l'utilisateur)*
- [ ] Le ressenti de la course et du saut est bon au clavier-souris et à la manette. *(playtest de l'utilisateur)*

## Playtest — étape 1 (course et saut)

**Préparation.** Ouvrir `Assets/Scenes/Test_Movement.unity`, lancer Play, **cliquer dans la
fenêtre Game** (elle capture la souris ; Échap la libère, un clic la reprend). F3 (clavier) ou
Select (manette) affiche ou masque le debug. Pour régler en direct, sélectionner
`Assets/_Project/Settings/Movement_Default` ou `Camera_Default` : les changements restent après
le Play Mode. Départ face au nord : blocs de hauteur à gauche (x = −20), parcours de trous à
droite (x = 10, escalier), rampes plus loin à droite (x = 20 à 35).

**Clavier-souris** (ZQSD sur AZERTY, ou les flèches ; Espace pour sauter)
1. Avancer 3 s : vitesse horizontale stable à **7.50 m/s**. Relâcher : arrêt net en moins d'une
   demi-seconde, sans glisser.
2. Diagonale (avant + côté) : toujours **7.50 m/s**, pas plus.
3. Souris : faire un demi-tour. Trop rapide ou trop lent ? (`mouseSensitivity`, essayer 0.05 à 0.2.)
4. Regarder tout en haut et tout en bas : la vue s'arrête à ±89°, sans se retourner.
5. Blocs : 0.5, 1 et 1.3 m se montent en sautant (1.3 m de justesse) ; 1.5 et 2 m sont impossibles.
6. Hauteur variable : tape brève sur Espace = petit saut, maintien = saut complet. L'écart te va ?
7. Coyote : courir sur une plateforme de 2 m et sauter un poil après le bord : le saut part
   quand même (compteur « Coyote x/6 » dans le debug).
8. Buffer : appuyer juste avant d'atterrir : le saut repart dès le contact. Maintenir Espace en
   continu : **un seul** saut, pas de rebond automatique.
9. Trous avec élan : 2 à 4 m faciles, 5 m limite, 6 et 7 m impossibles pour l'instant (normal :
   glissade et bunny hop viendront).
10. Rampes : 15°, 30° et 45° se montent en marchant, 50° bloque. En descendant sans sauter, l'état
    reste « Sol » (pas d'alternance Sol / Air).
11. Escalier (marches de 0.25 m) : se monte sans sauter.
12. Murs d'enceinte : foncer dedans = arrêt sans tremblement ; les longer en biais = on glisse le long.
13. Avant-goût du bunny hop : sauter en avançant puis, en l'air, lâcher l'avant, tenir un côté et
    tourner la souris du même côté : la vitesse horizontale doit monter un peu.

**Manette** (stick gauche, stick droit, A / Croix pour sauter)
14. Toucher la manette : le debug passe à « Contrôle : Manette » ; toucher la souris : il revient
    à « Clavier-souris ».
15. Stick gauche à fond : **7.50 m/s** ; à mi-course : environ 3.75 m/s.
16. Stick droit à fond : un tour complet en ≈ 1.3 s. Corrections fines au centre ? Dérive au
    repos ? (`gamepadYawSpeed`, `gamepadPitchSpeed`, `gamepadResponseExponent`.)
17. Refaire les points 5 à 9 à la manette.

**À me décrire :** pour chaque point, OK ou pas, avec les chiffres du debug quand ça cloche ;
puis ton ressenti : la course est-elle trop lente (le boss chassera à 9 m/s) ? Le saut est-il
trop flottant ou trop lourd ? Les sensibilités te conviennent-elles ?

## Playtest — corrections après l'étape 1

Le debug affiche maintenant « Sauts restants », « Auto-bhop » et « Contrôle aérien ».

1. **Contrôle aérien :** prendre de l'élan, sauter, puis en l'air tenir **W seul** et tourner la
   caméra de 90° : la trajectoire suit la caméra en environ un quart de seconde, et la vitesse
   horizontale du debug ne baisse pas. Essayer `airControl` à 0 (comme avant), 0.15, 0.3 et 1.
2. **Strafe intact :** en l'air, tenir Q ou D (sans W) en tournant la souris du même côté : la
   vitesse monte toujours.
3. **Auto-bunny hop :** maintenir Espace : on resaute à chaque atterrissage, et la vitesse ne
   baisse pas entre deux sauts. Enchaîner strafe + souris pour accélérer au-delà de 7.5 m/s.
4. **Double saut :** au sommet d'un saut, réappuyer : second saut, sans perte de vitesse
   horizontale. Un troisième appui ne fait rien ; « Sauts restants » repasse à 2/2 à
   l'atterrissage. Avec `maxJumps` = 3 : trois sauts.
5. **Trous :** les trous de 6 et 7 m doivent maintenant passer avec le double saut ou le bunny hop.

## Playtest — glissade

Glissade : Ctrl gauche ou C (B / Rond à la manette), **maintenue**. Le debug affiche l'état
(Glissade, Sol accroupi) et la recharge du bonus.

1. **Plat :** courir puis glisser : +2 m/s (≈ 9.5 m/s), la vue descend ; la glissade dure
   environ 1.8 s puis tu restes accroupi tant que tu tiens la touche. Relâcher te relève.
1 bis. **Depuis un bunny hop** (≈ 20 m/s) : lâcher le saut, tenir la glissade avant
   d'atterrir : la vitesse du debug ne baisse pas à l'entrée (+2 si le bonus est prêt), puis
   descend doucement (3 m/s par seconde) ; la glissade dure plusieurs secondes.
2. **Bonus :** deux glissades rapprochées : la seconde n'a pas le bonus (« recharge » dans le debug).
3. **Passage bas** (couloir à gauche, x = −32) : y entrer en glissade depuis la course, même
   en relâchant la touche dessous : tu traverses en glissant (jamais sous 5 m/s), sans pouvoir
   sauter, et tu te relèves en sortant.
4. **Longue rampe** (devant, plateforme de 6 m) : monter, puis redescendre en glissade : la
   vitesse monte (≈ 9.5 m/s en haut, ≈ 14 m/s en bas). Glisser en montée freine.
5. **Enchaînements :** glisse puis saute : la vitesse est gardée. Tiens la glissade en l'air :
   tu glisses dès l'atterrissage. Tiens saut + glissade : le bunny hop continue.
6. **Accroupi :** à l'arrêt, tenir la touche : marche lente (3.5 m/s).
7. Refaire 1, 4 et 5 à la manette.

## Scène de test agrandie — vérifications

- [x] Le sol fait 400 × 400 m ; toutes les zones existent (base, zone ouverte, ligne droite, pentes, grands murs, écarts, verticalité). *(script `check_scene.cs` par `script eval`, 2026-11-11)*
- [x] La zone ouverte (198 × 218 m, de 0.1 à 25 m de haut) ne contient aucun obstacle. *(même script : 0 collider)*
- [x] Sommets des pentes à 26, 30.8 et 30 m ; mur seul de 200 m × 15 m ; vue à 1 000 m. *(même script)*
- [x] Le joueur démarre au sol en (0, 0, −10), sans erreur en console. *(Play Mode par la CLI)*
- [x] Les 6 points de téléportation (F4 départ, F5 à F9 zones) posent le joueur au sol au bon endroit. *(Play Mode par la CLI, 2026-11-11 : `PlayerMotor.Teleport` appelé sur chaque point ; touches non simulables sans le focus de la fenêtre Game)*
- [ ] Les touches F4 à F9 téléportent bien. *(playtest)*

**Playtest de la scène**
1. Bunny hop sur la ligne droite de 380 m (à l'est) : vitesse de pointe atteinte, et la ligne
   est-elle assez longue ?
2. Zone ouverte : bunny hop libre et virages en contrôle aérien ; repères visibles de loin ?
3. Pentes de 10°, 20° et 30° : monter, puis redescendre en glissade (vitesse en bas ?).
4. Grands murs : longer les murs de 20 m et le mur de 200 m (pas encore de wall slide : c'est
   l'étape suivante) ; vérifier que l'échelle convient.
5. Écarts de 8 à 22 m : lesquels passent en bunny hop + double saut ?
6. Verticalité : monter de 2 m en 2 m avec le double saut jusqu'à 16 m.
7. Lisibilité générale : la lumière et les couleurs suffisent-elles à 100 m et plus ?

## Playtest — dash v3 et dash-jump (checklist courte)

1. **Dash à l'arrêt :** poussée forte qui s'amortit en douceur (≈ 30 m/s → 12 m/s sur 0.3 s),
   sans marche brutale à la fin.
2. **Dash au sol en course :** la vitesse de sortie vaut la vitesse d'avant + 1 m/s (debug
   « Dernier dash »), au moins 12 m/s.
3. **Dash-jump :** sauter pendant le dash au sol ou juste après (0.15 s) : debug « Dash-jump :
   RÉUSSI », +3 m/s. Trop tard : « raté, trop tard de N ms ». La fenêtre te semble-t-elle juste ?
4. **Dash en l'air :** dans toutes les directions, ta vitesse ressort identique, juste redirigée.
5. **Plafond :** au-dessus de 20 m/s, ni le dash ni le dash-jump ni le wall jump n'ajoutent de
   vitesse ; elle est conservée.
6. **HUD :** les 3 segments sous le viseur se vident et se rechargent.

## History

- 2026-11-11 : tests de l'étape 1.
- 2026-11-11 : tests du contrôle aérien, de l'auto-bunny hop, des sauts multiples et du plafond.
- 2026-11-11 : tests de la glissade et de l'accroupi.
- 2026-11-11 : tests de conservation de l'élan, de durée et du passage bas (D-013).
- 2026-11-11 : vérifications de la scène agrandie.
- 2026-11-11 : tests du wall slide et du wall jump.
- 2026-11-12 : murs v2, charges et dash.
- 2026-11-12 : dash v2, plafond du wall jump, HUD.
- 2026-11-12 : dash v3, dash-jump, plafond commun.
