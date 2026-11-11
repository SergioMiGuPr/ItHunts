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
- [ ] Les sauts se rechargent au contact d'un mur. *(pas encore : branché avec la détection des murs)*
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

## History

- 2026-11-11 : tests de l'étape 1.
- 2026-11-11 : tests du contrôle aérien, de l'auto-bunny hop, des sauts multiples et du plafond.
- 2026-11-11 : tests de la glissade et de l'accroupi.
- 2026-11-11 : tests de conservation de l'élan, de durée et du passage bas (D-013).
