# Avancement

> Mémoire du projet entre les sessions. Claude la met à jour à la fin de chaque étape.

## Étape en cours

**Matin du 2026-11-21 (en cours, branche `animations`) :** corrections A1 à A6, traqueur v4 (modèle B, animations C) dès les PR
d'art `art-traqueur-v4`, `art-traqueur-anim-N`, `art-fin-2`.
- **A1. Matériaux d'art jamais modifiés.** Cause : `CampementApercu.Eclaire` (DA) ne copie le matériau qu'en Play Mode ; en mode
  édition (test `Campement_PlacesSuiventLeSalon`), il écrivait dans `M_Campement_Assis_N.mat`, sauvegardé ensuite.
  `MainMenuArt.OwnCampMaterials` donne une copie à chaque place dès la construction (édition et jeu). Autres scripts vérifiés :
  eau, effets (`VfxPeint` : MaterialPropertyBlock ; `LienVfx` : instance), HUD, marchand : instances. Test : matériaux de la DA
  intacts et non sales. Vérifié : `git status` propre après 30 s de Play Mode du menu et `SaveAssets`.
- **A6. Garde-fous** : `.claude/settings.json` refuse `git reset --hard`, `git clean`, `git checkout -- .` (aussi en fin de
  commande composée) et `git restore .`.
- **A4. Parade sur sa touche (D-079).** Alt gauche / R3 ; E = interagir seulement ; dash manette revenu sur LB. Plus de partage
  E dans `PlayerInputReader`. Test des touches mis à jour (seul Y reste partagé : recharger ou interagir).
- **A3. Salles fermées et hissage.** Mesuré (sonde de plafond sur 7 × 7 points du sol de chaque salle) : Test_Arena, Level_01 et
  le niveau aléatoire (seeds 11, 33, 55) : **toutes** les salles (pièces verrouillées, annexes, départ, sortie, arène du boss,
  modules) ont un plafond au-dessus de 100 % de leur sol ; rien à corriger. Test `LevelClosureTests` (Salle_Verrouillee,
  Piece_Annexe, Sortie_Arene) pour que ça reste vrai.
- **A2. Collisions du kit des modules (PR #25).** Chaque pièce posée a des collisions simples (`ModuleLibraryNight.KitColliders`,
  cotes d'INTEGRATION-PROPS) en 4 types : *solide* ; *garde* (garde-corps, cordes, échelle, chaînes : bloque les joueurs, pas la
  main ni le regard du traqueur, hors NavMesh) ; *traverse* (solide pour les joueurs, le traqueur passe : piliers d'arche, rampe
  de l'escalier en spirale, lits) ; *décor* (solide, jamais un sol). **Pièces corrigées :** grande porte 4×10 / 6×10 (jambages,
  linteau ; échelle non uniforme : ouverture = porte étroite 8 × 10), cellule (murs, toit), échelle, mur grimpable, chaîne à
  grimper et chaîne tendue (capsules), pont de cordes (cordes ; tablier = pont du module, posé à 11 m), rebord, lits superposés,
  balcon (dalle à 6 m au ras de la mezzanine, garde-corps ; déplacé hors de la voie à x 10), râtelier, passerelle sur pilotis
  (plancher au ras du sol, sur sa plateforme), arche brisée (piliers ; rive sud tournée le long de la voie), gros tuyau, roue à
  eau, escalier en spirale (rampe lisse en 3 segments, 0 → 2.5 m), coursive en arc (dalle au ras de l'anneau à 11 m).
  Modules reconstruits et **validés** (0 cachette, entrées reliées). Validation alignée sur le jeu : le NavMesh de contrôle du
  traqueur ignore aussi ce qu'il traverse (`ReachCheck.BuildStalkerNavMesh`).
  **Liens du traqueur (seed 11) : 43 → 67, 0 rejeté.** Trois défauts de la nuit corrigés au passage : caserne (lits de la
  mezzanine : plus de NavMesh du traqueur, rebord et bond jamais reliés ; il enjambe les lits) ; prison (côté +z, le pont de
  planches à 11 m passe au-dessus de la 1re coursive : mur grimpable déplacé côté −z) ; puits (escalades : bouts recalés au-delà
  de 12 m à plat, limite passée à 16 m ; saut depuis la chaîne : chute de 11 m permise, jusqu'à la hauteur de chaîne).
  **5 seeds** : 0 cachette dans les nouveaux modules. **À noter (pas causé par A2, présent avant) :** ReachCheck compte ~200 à
  400 points « hors de portée » dans les couloirs du niveau aléatoire (210 / 274 / 238 / 418 / 236), la nuit en notait 4 à 9 :
  à revoir (mesure ou couloirs gothiques).

**Nuit du 2026-11-21 (fusionnée dans `main`, 4cf017f) :** un commit par point sur `animations`, PR d'art #19 à #26 intégrées au
fil de l'eau, puis `animations` fusionnée dans `main` et poussée. Build de développement : `Builds/Nuit_2026-11-21/ItHunts.exe`
(357 Mo, lancé 35 s sans exception). Tests EditMode : 476 + 1 non concluant attendu. Réglages modifiés par le build remis (non
commités). **À valider par l'utilisateur :** dash manette déplacé sur L3 (LB = parade) et E partagé (interagir / parer), D-075 ;
traqueur v4 non touché (D-070).
- **1a. Course 7.5 → 8.5 m/s (D-072).** `Movement_Default.runSpeed`, `ParkourCaps.Default`, ART-HANDOFF. Vitesses des ennemis
  inchangées. Écarts (ennemi − joueur à 8.5) : goule 10 (+1.5, bond 14 : +5.5) ; traqueur Chasse 9 (+0.5), pointe 16 (+7.5) ;
  Enragé 11 → 18 par paliers (+2.5 → +9.5), pointe enragée 18 (+9.5) ; plafond du traqueur 18 m/s.
- **1b. Élan gardé au sol (D-073).** `MovementMotor.GroundMomentum` (pas fixe, depuis `MovementInput`) : gardé en tenant avancer dans
  un cône de 45°, plafond 20 m/s au sol, perte progressive (lâché, hors cône, recul, virage > 60°, mur) ; réglages dans
  `Movement_Default` (« Élan gardé au sol ») ; debug : ligne « Élan au sol » (vitesse et raison). 12 tests (`GroundMomentumTests`).
- **2. Hissage automatique (D-074).** `MovementMode.Mantle`, `ICollisionProbe.ProbeLedge` (mur devant, dessus marchable, profondeur, place
  debout et pour monter) ; réglages « Hissage » de `Movement_Default` ; 8 tests (`MantleTests`). Zone d'essai derrière le départ dans
  `Test_Movement` (x −18 → 18, z −26) et la vitrine (z −18) : rebords de 1.5 à 4.2 m, étroit, bloqué (`MantleCourseBuilder`, ajoutée
  aux scènes sans les reconstruire). Détection vérifiée sur la vraie géométrie (CharacterController) : 3.7 m oui depuis 1.3 m,
  4.2 m non, étroit et bloqué non. **Conséquence niveaux :** un mur ne bloque qu'à 5.5 m+ (ART-HANDOFF).
- **PR #19 (`art-menus-2`) :** périmètre, GUID, compilation, 397 tests : fusionnée dans `main` (a89aa2e), poussée, `main` dans
  `animations` ; intégrée (ac9652a) : place 2 vide (+, lanterne éteinte, clic), personnages éclairés par `CampementApercu.Occupe`.
  Unity bloqué après le changement de branche (fil principal à l'arrêt, sans boîte de dialogue) : redémarré.
- **3. Parade (D-075).** `ParryMath` / `ParryState` (pur), `PlayerParry` (prefab joueur, `Parry_Default`), goule : coup au contact
  (à l'impact du clip) et bond passent par `PlayerParry.TryParry` ; `EnemyBody.StunFor` ; éclat et tintement provisoires
  (`DamageFeedback`, `ProceduralSfx.Parry`), icône provisoire du HUD. Touches : E (interagir sur un objet, sinon parer), LB ;
  **dash manette déplacé sur L3** (à confirmer). Vérifié en Play Mode (Test_Arena) : appui, impact tardif refusé, réussi,
  goule étourdie 1.5 s, recharge, traqueur insensible. 6 tests (`ParryTests`).
- **PR #20 (`art-audio`), #21 (`art-ui-jeu`), #22 (`art-vfx`) :** périmètre, GUID, compilation, 397 tests sur `main` ; conflits du
  manifeste (lignes ajoutées des deux côtés, toutes gardées) ; fusionnées, poussées, `main` dans `animations`. **Leçon :** après un
  changement de branche, forcer `AssetDatabase.Refresh(ForceSynchronousImport)` + recompilation, sinon Unity teste l'ancien code.
- **4. Interface en jeu (D-076).** Assembly `ItHunts.HudArt` : `PlayerHudArt` (réticule, charges, parade de la DA, canevas
  1920 × 1080 sur la caméra du HUD), `ShopPanelArt` (Panneau_Marchand / Panneau_Coffre dans la vue du joueur), `HudArtInstall`
  (`PlayerHud.Built`, `ShopMenu.Opened`). Logique : `ShopMenu` (Runtime/Economy), `ArtifactMerchant` et `Merchant` interactifs,
  coffre qui ouvre son choix, `OfferOrb.MenuOnly`. HUD : plus d'anneaux ni de jauges de traction et de répulsion, plus de recharge
  de tir. Vérifié en Play Mode (Test_Arena) : achat, refus (prix rouge), offres renouvelées, coffre, fermeture et commandes rendues.
  Captures : `docs/design/features/interface-jeu/captures/`. Icônes des améliorations : à fournir par la DA (masquées).
- **PR #23 (`art-armes`), #24 (`art-eau`) :** vérifiées, fusionnées dans `main`, poussées, `main` dans `animations`.
- **7. Eau de la DA.** `WaterSetup` (éditeur) : `M_Eau_Toon` (ItHunts/EauToon) sur toutes les surfaces d'eau (Level_01 et
  Level_01_Base : 7 canaux ; générateur aléatoire : matériau et éclaboussure du `RandomLevel`), constructeurs branchés ;
  `WaterSplash` (Runtime) : éclaboussure peinte à la surface quand un joueur, un ennemi ou un objet tombe dans l'eau (canal,
  fosse), taille 1.2 par mètre (0.4 à 8). Profondeur et texture opaque déjà actives dans `PC_RPAsset`. Capture :
  `docs/design/features/eau/canal_level01.jpg`. Remarque DA : `M_Campement_Assis_2.mat` est modifié sur disque pendant le
  Play Mode du menu (contraste, luminosité) : remis tel quel, à voir côté `CampementApercu`.
- **6. Sons de la DA (PR #20).** `GameAudioSettings` (`Resources/GameAudio_Default`, 20 sons), `GameAudio` (variante au hasard,
  hauteur ±6 %, son 3D ponctuel ; repli `ProceduralSfx` si un son manque). Traqueur : pas (boucle selon la vitesse, tremblement à
  chaque pas comme avant), souffle, grognement, cri d'enragé au hasard, cri à la prise ; goule : alerte, 2 attaques au hasard,
  mort ; volant : cri, ailes en boucle 3D, mort ; barillet : 2 tirs en alternance, recharge ; lanterne : début puis boucle
  pendant le jet (fondu de fin). Musique en jeu : exploration en boucle, fondu croisé (2.5 s) vers la musique Enragé, retour.
  Volumes : l'écouteur porte volume général × effets ; les musiques (menu, jeu) multiplient par `GameSettings.MusicGain`.
- **PR #25 (`art-kit-modules`), #26 (`art-outils`, `NUIT-FIN.md`) :** vérifiées, fusionnées, poussées, `main` dans `animations`.
  Toutes les PR de la nuit sont intégrées (#19 à #26) ; pas de PR du traqueur v4 (consigne).
- **8. Effets peints de la DA (PR #22).** `GameVfxSettings` (`Resources/GameVfx_Default`, 13 effets), `GameVfx.Spawn` ; Runtime
  référence `ItHunts.Art.VFX`. Torches (`Prop_Torche` : `VFX_Torche_Feu` à la place de la flamme), jet de la lanterne (à la place
  des bouffées), barillet (éclat de bouche, trace peinte, étincelles à l'impact), âmes (`VFX_Ame` sur les 32 plus proches, réserve
  réutilisée), morts de goule et de volant, étoiles d'étourdissement, éclair de parade, brûlure (flamme à 0.6). Liens du hibou :
  point 5 ; poussière d'atterrissage du traqueur : point 9. **Coût mesuré** (Play Mode, Level_01_Aleatoire, 235 torches,
  470 systèmes, ~1 700 particules) : `ParticleSystem.Update` 0.77 ms par image, fil principal 15.1 → 13.2 ms torches éteintes
  (éditeur). À surveiller en build ; leviers : `debit` des flammes, une torche sur deux (`RandomLevel.ThinTorches`).
- **5. Koala et hibou (D-077).** `BoneSwordSettings` / `BoneSword` (combo, vitesse, fente, recul), `SpiritBookSettings` /
  `SpiritBook` (drain, soin, plafonds, recharges), branchés dans `ArtifactHolder` ; `MovementInput.ThrowHeld` ; vue FPS
  `ArtifactModelView` (modèles de la DA `Artefact_Epee_Koala`, `Artefact_Livre_Hibou` ; coup animé par le code ; liens peints) ;
  `FirstPersonView.LeftHand`. Koala et hibou jouables (`playable`), descriptions sur l'écran de choix, pool des artefacts.
  Vérifié en Play Mode : coup du 3e (16 PV) sur une goule, épée et livre visibles en main, lien de drain accroché à 8 m.
  10 tests (`KoalaOwlArtifactTests`). Captures : `docs/design/features/artefacts/captures/`. Main gauche sans pose de prise
  (mains gelées).
- **10. Ressort des chaînes d'os (préparé, inactif).** `BoneSpringChain` (Verlet à pas fixe : raideur vers la pose animée,
  amortissement, gravité, inertie de la racine, longueur gardée, collisions sphère et capsule, pose d'origine remise avant
  l'Animator) ; réglages `BoneSpringSettings` dans `Stalker_Default` (`boneChainsEnabled` à faux : rien d'activé, modèle v4 et
  D-070 non touchés). Chaîne factice de 8 os qui se balance dans la vitrine (x 8, z −8, `BoneChainDemo`). 5 tests.
- **11. Outils.** `docs/design/OUTILS.md` : packages installés, gratuits officiels utiles plus tard (Animation Rigging,
  ProBuilder, Splines, Recorder…), payants avec prix indicatifs (Final IK, Magica Cloth 2…). **Rien installé, rien acheté** :
  AI Navigation (déjà là) couvre les liens du traqueur.
- **9. Nouveaux modules et passages du traqueur (D-078).** `ModuleLibraryNight` : prison à étages, caserne, citerne inondée,
  puits circulaire (4 modules **validés** : 0 cachette, NavMesh relié entre toutes les entrées), kit de la DA (PR #25) posé en
  habillage par ses bornes (sans collision). `StalkerPassage` + `StalkerPassageMath` (permis, durée, arc, coût, prise pendant le
  bond, récupération), liens dans `ZoneNavMesh` (bouts recalés à 4 m sur le sol du traqueur), traversée dans `Stalker.Locomote`
  (chasse et ronde). `ChestSpot.difficulty` (0 à 2), tirage pondéré, coffre rare sur ancre difficile (`rareChestOnHardSpot` 0.35).
  **5 seeds** (11, 22, 33, 44, 55) : 51 salles dont 12 à 17 des nouveaux types, 0 échec de plan, 47 à 77 liens du traqueur,
  **0 cachette dans les nouveaux modules** (4 à 9 points ailleurs : arène du boss et fosses de parcours, comme avant),
  construction 5.2 à 6.1 s + NavMesh 1.5 s. **Temps de trajet** (seed tirée en jeu) : bond 2.6 s en moyenne (28 liens : 8 aussi
  atteignables à pied en 2.6 s, 20 inaccessibles sans le lien), saut de chaîne 2.5 s (12, tous inaccessibles à pied), mur 2.6 s
  (10), chaîne 3.2 s (4) ; télégraphe et récupération compris : le joueur qui bouge bien a le temps de s'échapper.
  Enregistrements : `docs/design/features/run-et-niveaux/captures/modules_nuit/` (bond sur une coursive de la prison, chaîne
  du puits ; mp4 et planches). 6 tests (`StalkerPassageTests`).
  Captures vue de dessus et en perspective des 4 modules (même dossier). **Pas fait :** couloirs de service plus étroits (les
  couloirs restent les gothiques) ; poussière d'atterrissage peinte branchée mais pas vue en capture.

**Retours du 2026-11-18, second lot (`animations`, pas fusionnée dans `main`) :** un commit par point.
- **Règles de l'utilisateur :** plus aucune correction des mains FPS ni des animations avant ses photos et vidéos de référence ;
  menus : pas de logo écrit avec une police ni de boutons dessinés en remplacement, seulement les sprites de la session art.
- **Lumières qui sautent, vitrine (ccd8dec) :** la lanterne ponctuelle (enfant du corps simulé à 60/s) avançait par à-coups
  (0 puis 12.5 cm une image sur deux à 7.5 m/s) ; elle suit maintenant la position interpolée du corps, comme la caméra (écart
  12.5 → 0.0 cm). Le cône suivait déjà le corps (d973e78). Prefab commun : toutes les scènes. Vu de l'extérieur : aucun saut.
- **Accès à la vitrine (b7ca04b) :** lien « Vitrine (test) (F2) » sur l'écran titre (éditeur et builds de développement), F2 lance la
  vitrine, Playtest passe sur F3. `docs/design/features/menus/captures/vitrine_acces/`.
- **PR #17 (`art-ui-c`, lot T) :** méthode abandonnée, ne pas fusionner (la session art la ferme).
- **PR #18 (`art-menus`) :** vérifiée (périmètre, aucun GUID réutilisé, compilation, 397 tests), fusionnée dans `main` (fdf2b7f) puis
  sa correction des pieds des personnages assis (6cb410f), poussée, `main` fusionnée dans `animations`. `Campement_Fond.png.meta`
  (PPU 200 → 100, sans compression) : seul `MenuSkin_Default` s'en servait (taille posée par le code), sans effet ailleurs.
- **Description courte des artefacts (3522879)** : `ArtifactDefinition.description`, lue par l'écran de choix.
- **Feu du campement (3a36d50)** : `AudioGain` (+28 dB, limiteur, écrêtage doux), environ 5 dB sous la musique (0.55).
- **Menus de la DA intégrés (99dd767)** : assembly `ItHunts.Menus` (MainMenuArt, PauseMenuArt) ; logique gardée (brume, J1, Start,
  Prêt, doublons, ACTE, solo direct) ; choix plein écran (un à la fois) ; `GameSettings` (6 lignes de la DA, menu et pause) ;
  options d'image et cadrage des mains en « Options d'image (dév.) » dans la pause ; chat masqué ; musique continue, feu au
  campement. Captures et contrôle : `docs/design/features/menus/captures/maquettes/` (`CONTROLE.txt`) ; bord clair absent du build.
- **Retours à la DA** (ART-HANDOFF, « Menus : intégration de la PR #18 ») : bande bleue à droite de `Menus_Cadre.png` ; place 2
  vide sans `+` ni lanterne éteinte ; manifeste sans les prefabs et shaders UI.
- **En attente :** le prompt mouvement (course 8.5 m/s, élan gardé au sol) n'est pas dans cette session : à redemander.
- **Timer :** à revoir plus tard, rien changé.
- **Tests :** EditMode 427 + 1 non concluant attendu.

**Retours des menus du 2026-11-18 (`animations`) :** 4145cf6 (écran titre), 10c747f (campement et transition). Captures avant/après en
1920 × 1080 et 0,66x : `docs/design/features/menus/captures/retours_2026-11-18/`. Titre : deux styles en attendant le logo
(`MenuSkin_Default.titleStyle` : Grenze Gotisch SemiBold avec contour par défaut, ou EB Garamond Bold espacé), à choisir. Liseré clair :
absent de l'image rendue (mesuré), sans doute la vue Game de l'éditeur en zoom fractionnaire ; à confirmer dans un build.
- **Attendu de la DA pour remplacer les morceaux provisoires :** scène composée de l'écran titre (ciel, lune, nuages raccordables ou
  déjà posés, château détouré proprement, brume) et du campement (fond sans foyer à effet si l'effet doit rester code, ou foyer animé
  livré) ; logo IT HUNTS ; personnages 2D du campement (lot C, assis, de face et de dos pour les places du fond) et ancrages des places ;
  écran de choix du personnage (lot D) ; boutons (normal, survol, pressé, désactivé) et cadre du menu pause ; vraie musique.
- **Tests :** EditMode 419 + 1 non concluant attendu.

 (2026-11-18, `animations`, pas fusionnée dans `main`) :** un commit par point.
- **PR #15 (art-ui-b) :** vérifiée (périmètre, `.meta`, manifeste, compilation, 397 tests), fusionnée dans `main` (f223c21), poussée,
  `main` fusionnée dans `animations`.
- **Carte ouverte (9a1e62b) :** cadre, parchemin, salles à l'encre, brume dévoilée par les salles et le pinceau (par joueur, partagé
  en coop), vignette, titre, rose, légende ; écran partagé. `docs/design/features/vue-fps/captures/hud_carte/`.
- **Menus (b154457 titre, 6f92e58 campement, 77e6c98 choix, 02827cb pause) :** uGUI avec les pièces de la DA, polices EB Garamond et
  Grenze Gotisch, navigation souris, clavier et manette (focus visible), lisibles en 1920 × 1080, 0,66x et écran partagé. Logique du
  campement testée (`CampLobbyTests`). Captures : `docs/design/features/menus/captures/`. Réglages : `MenuSkin_Default`.
- **Pièces à demander à la session art (habillage provisoire en attendant) :** logo IT HUNTS (titre en Grenze Gotisch pour l'instant) ;
  boutons (normal, survol, pressé, désactivé ; texte et barre pour l'instant) ; cadre du menu pause et des panneaux (le panneau de
  la légende de la carte sert) ; lot D (écran de choix : cadre, vignettes, grande pose animée, panneau d'artefact, Valider / Prêt) ;
  lot C (personnages 2D animés du campement ; silhouettes pour l'instant) ; prefab `Fond_Panorama` avec les calques restants (arbres
  proches, sol, premier plan) et la jonction B2 entre le titre et le campement (deux toiles séparées pour l'instant) ; ancrages des 4
  places autour du feu (réglés à l'œil dans `MenuSkin_Default`) ; nuages raccordables (bandes coupées d'un côté : elles dérivent au
  lieu de défiler) ; vraie musique d'ambiance (nappe synthétisée).
- **Tests :** EditMode 419 + 1 non concluant attendu.

 + PR #13 et #14 (`animations`, pas fusionnée dans `main`) :** un commit par point.
- **PR #14 (bras FPS à l'endroit) et #13 (traqueur v3) :** vérifiées (périmètre, compilation, 397 tests), fusionnées dans `main`
  (03af276) et poussées, `main` fusionnée dans `animations`. Intégration #14 (c4e9d2a) : contournement des maillages retournés retiré,
  test du volume signé des 8 bras. Intégration #13 (519f19f) : traqueurs des 4 scènes et vitrine reconstruits, 3 matériaux du corps,
  lecture des clips = vitesse ÷ 3.71 / 2.29 (pied posé 5.3 → 0.48 m/s), D-070 vérifié (×1.8 à ×3.0, main à l'échelle 1), sommet 13.9 m
  en marche, 8.5 m baissé ; **yeux de repli** (2 sphères sur l'os `Tete`, `eyeGlints`) car les pupilles du modèle v3 ne s'affichent
  pas (trous du visage) : retour DA dans ART-HANDOFF ; `StalkerVisibilityTests` vert.
- **Lumières qui sautent (d973e78) :** banc d'essai (même trajet scripté, 8 essais, GIF + caméra fixe) : cause = le cône de la lanterne
  collé à la caméra (halo en retard puis recalé d'un coup après une rotation) ; il suit maintenant le corps (lissage réglable). Seuls
  sauts restants vus de l'extérieur : lampes qui grésillent (effet voulu, `buzzingLampShare`). `rendu/captures/lumieres_qui_sautent/`.
- **Orbe (299c1b1, ajout à D-069) :** immobile au repos, couronne face à la caméra, ne tourne que pendant une action, plus vive.
- **Mains (ce11b28) :** pose « tenir » au repos (doigts à demi refermés vers `Pose_Poing`, réglable par main).
- **HUD (5a59d49) :** âmes centrées, timer 56 sans objectif, échelle par la largeur sous 1100 px, carte ouverte uGUI par joueur
  (action `Map` : Tab, croix haut), brouillard par joueur et partagé en coop, icônes de la DA, `Coffre_Rare` (coffres des parcours).
- **Crash à la fermeture des builds (d1d1128) :** D3D12Core.dll, 0xc0000005 à chaque fermeture des builds récents. Bissection par builds :
  le stencil écrit par les passes « ItHunts Mains » du renderer. Stencil éteint dans `PC_Renderer`, allumé seulement pendant l'Enragé,
  éteint avant de quitter (`wantsToQuit`). Vérifié : 3/3 fermetures propres hors Enragé, 3/3 pendant l'Enragé.
- **Écran partagé, 6 rondes alternées (e2ec285) :** médiane 23.5 → 18.8 ms, 95e centile 45.0 → 30.6 ms, B devant dans les 6 rondes.
- **Coffre rare (6e90cf3, D-071) :** coffres des parcours, meilleur butin réglable dans `Economy_Default` (table dédiée, +1 choix, ×1.5).
- **PR #16 (tête du traqueur) :** fusionnée dans `main` (271beee), intégrée : `eyeGlints` décoché, scènes reconstruites (ordre des
  matériaux du nouveau FBX), test des matériaux ; `StalkerVisibilityTests` vert sans le contournement.
- **Tests :** EditMode 415 + 1 non concluant attendu ; PlayMode 1/1.
- **Points ouverts :** PR #15 (art-ui-b) puis les menus : attendre le retour de playtest de l'utilisateur.

**Retours de playtest du 2026-11-17, second lot (`animations`, poussée pour la session art, pas fusionnée dans `main`) :** un
commit par point.
- **Tablettes de l'orbe (350108f, D-069) :** une tablette par charge de traction et de répulsion (2 + 1 au départ), une tablette qui
  apparaît quand une amélioration augmente le maximum, rune éteinte quand la charge est utilisée, lueur à la couleur du personnage.
- **Lumières qui scintillent en mouvement (cde6ece) :** jusqu'à 678 lumières dans le champ pour 256 gardées par Forward+ ; lumières du
  décor éteintes au-delà du brouillard (hystérésis), fondu et hystérésis des ombres, atlas 4096, vacillement plus doux. Builds A/B
  (3 rondes) : solo médiane 12.7 → 10.8 ms, 95e centile 20.8 → 16.5 ms ; écran partagé médiane 18.8 → 17.3 ms, 95e centile
  28.9 → 32.5 ms (dispersé : 26.8 à 35.4). Détail : `docs/design/features/rendu/captures/scintillement/MESURES.txt`.
- **Enragé (042e56b) :** le monde seul vire au rouge, 35 % de sa couleur gardée ; vue FPS (stencil) et HUD (Overlay) non étalonnés.
- **PR #10 art-ui, #11 art-mains, #12 art-orbe :** vérifiées, fusionnées dans `main` (ef79230, acb5d8c, 206f3dc) et poussées, puis
  intégrées : HUD habillé (d60d273, atlas V2), bras v2 et `Socket_Flottement` (6096721), tablette seule et tourbillon (c30937a).
  Test des bras mis à 20 os (92e9a65).
- **Bras du traqueur (f628473, D-070) :** allongement par l'échelle de l'os du bras au lieu d'os glissés (×2.3 à 5 m, ×3.2 à 15 m),
  portée et prise inchangées ; captures `docs/design/features/boss-traqueur/captures/bras_echelle/`.
- **Tests :** EditMode 405 réussis + 1 non concluant attendu ; PlayMode 1/1. `PlayFromMainMenu` réactivé après les tests.
- **Points ouverts :**
  - PR `art-surfaces` pas encore arrivée (à intégrer : ce que le shader demande, captures avant/après).
  - DA : FBX des bras v2 retournés (contournement en place) ; traqueur : arrêter `M_Traqueur_Mains` au poignet (bande pâle quand le
    bras s'allonge) ; notes dans `docs/art/ART-HANDOFF.md`.
  - Icône `Coffre_Rare` de la minicarte pas encore utilisée.
  - Les builds de développement B plantent à la fermeture (après la mesure, `PlayerConnection::Cleanup`) : à surveiller.

**Retours de playtest du 2026-11-17, premier lot :** un commit par point.
- **Orbe (b16d61b) :** le « disque gris » était la sphère greybox de 8 cm (matériau unlit) ; `SM_Orbe` branchée (`OrbModelView`,
  `OrbModel_Default` : taille d'une pomme, couleur du personnage, anneaux, énergie qui pulse, tablettes = jauge des charges).
- **Barillet (4ec0015) :** échelle 0.5 (largeur de la paume), chambres × 1.4 au lieu de × 3 ; taille, décalage et flottement dans
  `Barrel_Default` (« Vue FPS »).
- **Traqueur invisible (f95490f) :** dans `Test_Vitrine`, 32 os pour 42 dans le maillage (pas dessiné) : vitrine reconstruite ; dans les
  niveaux, corps noir dans le noir et le brouillard (fin à 35-38 m en sombre et horreur) : lumière d'appoint sur le calque de rendu
  « Traqueur », pupilles 1.5 ; anciennes mains en boîtes plus en double. Tests : os de tous les rendus gréés (EditMode), traqueur
  visible à 10, 30, 60 m (**premier test PlayMode**, `Assets/_Project/Tests/PlayMode`, lancé en asynchrone).
- **Vitrine, zone « Traqueur » (ba826c4) :** couloir gothique de 150 m construit par le code de la carte, galerie haute, 2 poutres,
  ronde du traqueur (`PatrolPoints`) et poursuite (F1).
- **Lumière d'ambiance horreur (9d2b30f) :** `HorrorLook` (`HorrorLook_Default`, chaque élément activable) : étalonnage par ambiance,
  rouge sang presque monochrome en Enragé (12 s), halos (un système de particules), vacillement, cône de la lanterne ombré, 3 lumières
  ombrées au plus, bloom / vignettage / grain / aberration (menu pause). Coût CPU 0.16 ms dans l'éditeur.
- **ART-HANDOFF (1fc1b6d) :** demandes au shader toon (normales, sol mouillé, vortex de l'orbe) et inventaire des props (21 greybox).
- **Tests :** EditMode 403 réussis + 1 non concluant attendu ; PlayMode 1/1 (visibilité du traqueur).
- **Points ouverts :**
  - Traqueur au-delà de la fin du brouillard (35-38 m en sombre) : seuls ses yeux se voient ; options à trancher (silhouette pâle
    qui traverse le brouillard, brouillard plus loin, ou garder « on l'entend avant de le voir »).
  - En Enragé, tout vire au rouge : le rouge du danger ne se distingue plus que par sa luminosité (15 % de couleur gardée).

**PR #9 (`art-fix`) — 2026-11-17 :** pouce du traqueur repondéré, os correcteurs `Jambe_Pli.L/.R` du creux des genoux de la goule
(pilotés dans les clips). Vérifiée (périmètre, manifeste, compilation, 397 tests + 1 non concluant attendu), fusionnée dans `main`
(984efff) et poussée, puis `main` fusionnée dans `animations`. En jeu : 9 clips de la goule et 8 du traqueur branchés et lus sans
erreur, impact de `Goule_Coup` toujours à 0.24 s ; `Jambe_Pli` ne bouge que dans `Goule_Mort` et `Goule_Etourdie` (0.9 cm).

**PR #8 et voûte gothique (`animations`) — 2026-11-16, non poussée, à tester.** PR #8 vérifiée :
- périmètre `Assets/Art/`, `docs/art/` et manifeste ; `.meta` des 6 FBX présents (les sources sont dans `Source~`) ;
- compilation sans erreur, 398 tests ;
- fusionnée dans `main` (commit de fusion 32ed865, branche `art` gardée) et poussée ; `main` fusionnée dans `animations`.

Un commit par point :
- **Bras :** poses recuites (repos des doigts fléchi), écart avec les 14 clips 74.7° → 0.0° sur les 8 bras ; koala et hibou
  branchés sur leurs bras de la DA ; captures `docs/design/features/vue-fps/captures/mains_pr8/`.
- **Lumière (shader de la PR) :** plus de cercles nets au plafond.
  - Lanterne du renard ramenée au rouge sang (0.72 ; 0.40 ; 0.33) : le ×1.35 de `_LightSaturation` la mettait dans la saturation
    des yeux du traqueur.
  - Captures dans les 3 ambiances : `captures/lumiere_pr8/`.
- **Couloirs gothiques** (couloirs plats de 14 m, `gothicCorridors`) :
  - voûte en lancette, arcs-doubleaux sur colonnes engagées, travées de 8 m (arcade aveugle, niche ou mur plein) ;
  - torches sur les trumeaux à 4 m ; pignon au-dessus des portes ; plafond des galeries entré dans la voûte ;
  - 5 seeds : 0 cachette, NavMesh complet (goule et traqueur), aucun trou, aucun module traversé par la voûte ;
  - draw calls +27 % (solo) et +35 % (écran partagé), temps d'image inchangé ; mesures dans
    `docs/design/features/run-et-niveaux/captures/couloir_gothique/`.
- **Référence :** `docs/design/references-visuelles/reference_couloir_gothique.png`.
- **Plantage noté :** un plantage du pilote GPU (D3D12, « device removed ») pendant une capture ; il ne s'est pas reproduit après
  relance.
- **Points ouverts :**
  - **Avant tout build public :** confirmer les droits d'usage commercial des contenus Higgsfield (références, textures) selon
    le forfait.
  - Arcades des couloirs : elles sont aveugles (1.2 m de profondeur) ; si on veut les ouvrir sur des pièces annexes, il faudra
    une décision de level design.
  - Genou gauche de Goule_Escalade (pic 1,94 à l'image 13), laissé tel quel ; à revoir seulement s'il se voit, ou avec les
    animations d'après vidéo.
  - ~~Draw calls des couloirs gothiques (+27 à 35 %)~~ : fusionnés par couloir le 2026-11-17 (éditeur, 6 poses : 4 855 → 3 182,
    moins que le plafond plat à 4 258).
  - ~~Physique un peu plus chère avec la voûte (33 468 colliders contre 14 572)~~ : voûte, arcs et coins des portes en un
    MeshCollider statique par couloir le 2026-11-17 (16 839 colliders ; Physics.Simulate 0.534 → 0.468 ms en A/B dans l'éditeur ;
    NavMesh, cachettes, fuites et traction inchangés ; détail dans `captures/couloir_gothique/MESURES.txt`). Restent en boîtes :
    murs de travée (6 077, dans le NavMesh) et colonnes.
  - Goule, coup au contact : à l'instant des dégâts (0.24 s), le clip `Goule_Coup` en est à 0.21–0.22 s (fondu de 0.06 s) ; un
    bond peut couper le geste (priorité de l'état), les dégâts tombent quand même. Constaté au contrôle de la PR #9, inchangé par
    elle ; à revoir seulement si ça se voit.
- **Performance, mesure comparable (2026-11-17)** (`docs/design/features/run-et-niveaux/mesure_perf_ab_2026-11-17.txt`) :
  - A/B en alternance dans la même session, 3 rondes : A = 02df035 (référence 9.9 ms), B = HEAD ; médiane / 95e centile :
    - solo : A 11.6 / 16.9 ms, B 11.9 / 15.9 ms ;
    - écran partagé : A 19.5 / 28.4 ms, B 19.2 / 26.8 ms.
  - Pas de régression : l'écart avec 9.9 ms vient de l'état de la machine (le même exe a varié de 14.4 à 25.4 ms d'une session à
    l'autre).
  - Règle de mesure : toujours comparer deux builds en alternance dans la même session.
  - Builds de mesure non suivis : `Builds/Perf_A` et `Builds/Perf_B`.

**Animations (`animations`, depuis `main` après fusion de `retours-lot` et de la PR #6) — 2026-11-16, non poussée, à tester.**
- **Goule animée** (`SK_Goule`, `Assets/_Project/Animation/Goule.controller`) : un clip par état (repos, course selon la vitesse,
  annonce calée sur `leapTelegraph`, bond, réception, escalade, coup avec dégâts à l'impact 0.24 s, étourdie ou tenue, mort visible
  1 s avant de disparaître) ; LOD 0.25 / 0.10 / 0.012 (DA, 1.8 m) ; elle se baisse sous un plafond bas (`minCrouch`).
- **Passages bas :** agent des goules 0.45 × 1.2 m, porte des salles verrouillées hors de leur NavMesh, pas d'arc devant les portes
  annexes ; 20 seeds : la goule atteint les 3 425 volumes que le joueur atteint à pied.
- **Traqueur animé** (`SK_Traqueur`, `Traqueur.controller`, calque `Mâchoire`) : IK épaule-coude-poignet vers les zones de main du
  code, bras étiré au-delà de sa longueur (portée 15 m) ; épaule lue sur l'os `Bras.R` (2.6 m de côté, 11.66 m, 3.55 m devant) ;
  lueurs par matériau (pupilles, griffes) ; 0 cachette sur 20 seeds ; esquive 10/10 sans réaction, 0/20 en courant de côté.
- **Bras FPS :** boucles `Boucle_*` au repos, poses d'action paume vers le haut ; cadrage de la référence
  (`docs/design/references-visuelles/reference_bras_fps.png`, valeurs de la DA) : viseur dégagé pour tous les gestes, **couverture
  17 à 26 %** (au-dessus de 15 % : à trancher) ; orbe à `orbInHand` ; lumière d'appoint sur le calque « FPS » (`fillLight*`).
- **Arcs :** `SM_Arc_Couloir_Ecoincons` (les blocs provisoires ne restent que dans les couloirs en pente, cachés derrière) ; 0 vide
  sur 1 460 arcs (5 seeds).
- **Eau :** fosses de parcours avec eau (retour au seul contact), eau plus claire ; 120 vides sur 120 avec de l'eau (5 seeds).
- **Vitrine :** enclos fermé (murs de 16 m, porte de 4 × 3 m), socles sous les goules et traqueurs animés, scène sous le même GUID.
- **Build Windows release 0.1.0** : `Builds/ItHunts_0.1.0_Windows.zip` (non suivi par git), LISEZ-MOI.txt.
- **En attente :** PR #7 (bras peints des 4 personnages), puis fusion de `main` dans `animations`.

**Retours du lot (`retours-lot`, depuis `main` après fusion de `correctifs-rendu`, `integration-lot` et de la PR #5) — 2026-11-16,
non poussée, à tester en playtest.** PR #5 vérifiée (périmètre, `.meta`, import sans erreur, 392 tests) et fusionnée (commit de
fusion, branche `art` gardée), `main` poussé ; branches locales `correctifs-rendu` et `integration-lot` supprimées. Un commit par point :
- **1. Ambiance « sombre » :** ambiance 0.022, brouillard 6 → 38 m ; colonne du traqueur et âmes nettes à 50 m, caisse à 30 m, annonce
  du bond (disque) à 20 m.
- **2. Arcs :** cause = forme (anneau en ogive sous un plafond plat, 3 à 6 m de vide) ; écoinçons de pierre posés par le jeu
  (`KitBuild.Spandrels`, collision traversée par le traqueur) ; demande d'un arc à écoinçons pleins à la DA (ART-HANDOFF).
- **3. Lueurs :** yeux de la goule et pupilles du traqueur en rouge sombre réglable (`eyeGlowRest/Announce`, `pupilGlowIntensity`) ;
  griffes ivoire (`M_Traqueur_Mains` posé sur les bras et les mains, `clawGlowRest` 0.15 → `clawGlowGrab` 0.7) ; 3 LOD de la goule.
- **4. Goule à 1.8 m :** `bodyHeight` (modèle × 1.64, capsule 0.55 × 1.8 m, disque) ; LOD réglés par le jeu (0.33 / 0.13 / 0.016) ;
  agent NavMesh 0.45 × 1.5 m (à 1.9 m, des couloirs en pente se fermaient) ; masse inchangée (50 kg).
- **5. Cachettes :** tours des Étages en anneau et angles des Galeries hautes au plafond, ouverture des passages côté couloir à 4 m,
  ReachCheck aligné sur le jeu (arcs traversés) ; **0 cachette sur 20 seeds**.
- **6. Vue FPS :** mains plus hautes (avant-bras visibles), paumes vers le haut (`Pose_OrbePaume`), orbe et artefact qui flottent
  (`floatHeight/Amplitude/Period/Spin/Wobble`), barillet sans canon (tir depuis la chambre active), poses de la PR #5 branchées,
  clips `Anim_*` prévus (cuits par le menu des bras, `Anim_Repos` remplace le repos) ; couverture 9.1 %, viseur dégagé.
- **7. Torches :** écartées des arcs, piliers, colonnes et encadrements (`torchClearance` 1.2 m, glissent jusqu'à 4 m, sinon retirées).
- **8. Parcours :** rien (level design module par module).
- **9. Vitrine `Test_Vitrine` :** menu principal (« Playtest : vitrine ») et menu pause des scènes de test ; constructeur *ItHunts >
  Reconstruire la scène Test_Vitrine* ; panneau F1 (personnage, gestes, ambiance, figé, poursuite).
- **Aussi :** Échap / Start ouvrent la pause partout ; Play part de MainMenu (*ItHunts > Play démarre depuis MainMenu*).
- **Piège noté :** ne jamais modifier une scène ouverte sur le disque (boîte « scène modifiée » qui bloque l'éditeur) : ouvrir une
  scène vide d'abord.

**Intégration du lot art de la PR #4 (`integration-lot` = `correctifs-rendu` + `main` après fusion de la PR #4) — 2026-11-16,
non poussée, à tester en playtest.** PR #4 vérifiée (périmètre `Assets/Art/`, `docs/art/`, manifeste ; 111 assets avec `.meta` ;
import sans erreur ; 386 tests), fusionnée dans `main` (commit de fusion, branche `art` gardée) et poussée. Un commit par bloc.
- **1. Kit (`23528f6`) :** `SM_Kit_Mur4x4`, `Mur2x4`, `Mur4x3`, `Sol4`, `Escalier`, `Pont` (M_Pierre_Monde, M_Dalles_Monde) ;
  escalier en marches répétées (0.25 m, pleines jusqu'au sol), collision = pente lisse ; 18 modules revalidés ; 5 seeds :
  0 pièce hors fusion, faces confondues ≈ 1.5 m² par carte, raccords sans couture (captures `kit_*`).
- **2. Props (`fe075d9`) :** décalages de la fiche (torche +0.545 m, lumière sur `FX_Flamme`), `M_Torche`, `SM_Caisse` (prefab et
  207 caisses des scènes, lueur par le masque de la rune), échafaudage praticable (20/20 plateaux sur le NavMesh des goules, surface
  minimale du NavMesh 1 m²) ; props de la DA étiquetés « DA ».
- **3. Goule (`231bd23`) :** `SM_Goule` (prefab, constructeur, 978 goules des scènes) ; annonce : yeux et gueule par le masque ;
  sur un mur, bornes à 0.39 m du mur.
- **4. Traqueur (`835d27c`) :** pièces de la DA ; épaule `shoulderSide` 1.8 m, `shoulderHeight` 11.1 m ; main 4.9 × 1.5 × 4.5 m ;
  pupilles par le masque. Esquive : 10/10 prises sans réaction, 0/20 en courant de côté après 0.15 s (comme avant). Couverture :
  salle haute remplie corrigée (tours d'angle jusqu'au plafond) ; **4 cachettes sur 20 cartes** (coins des tours de 11 m des
  étages en anneau, galeries hautes) : à trancher (voir points ouverts).
- **5. Vue FPS (`af633d3`) :** bras du renard et du caméléon (`FpsArms`, poses cuites `FpsArmPoses`) ; seuls les clips
  `Pose_Attraper` ont des courbes : repli sur la fermeture des doigts des mains greybox ; flamme de la lanterne à la couleur
  du personnage, éteinte pendant la recharge ; calque « FPS » ; couverture 6.5 % (renard), 8.1 % (caméléon).
- **6. Performance (`02df035`) :** solo 11.2 → 9.9 ms médiane (5 % les plus lents 15.0 → 12.8), écran partagé 18.8 → 16.8 ms
  (23.7 → 21.7) avec une torche sur deux (`torchShare` 0.5) et le plafond des pas physiques (0.05 s) ; GPU Resident Drawer sans gain
  (754 draw calls, fil principal identique), laissé coupé. Objectifs non atteints : fil principal (rendu URP 5.4 ms, FixedUpdate
  2.6 ms). Unity du projet art ouvert pendant la mesure.
- **Texturé :** murs, sols, escaliers, ponts, arcs, colonnes, portes, poutres et ponts d'égout du kit ; torches, cage, grille,
  tapisserie, chaîne, tuyau, échafaudage, colonnes ; caisses attrapables ; arène du boss (herse, leviers, treuil, plateformes) ;
  goule ; traqueur ; bras FPS (renard, caméléon) ; lanterne de feu, barillet.
- **Encore greybox :** props Armoire, Autel, Banc, Caisse (fixe), Chaise attrapable, Chaudron, CoffreDeco, Etagere, Gravats,
  Lanterne, Lit, MurEffondre, Paille, PileDeCaisses, Poutre (prop), Ratelier, Sarcophage, Statue, Table, Tonneau (+ attrapable) ;
  coffres, marchands, lustres ; volant ; orbe (v2 à intégrer) ; bras du koala et du hibou (personnages pas encore jouables) ;
  pièces non magnétiques du parcours ; murs de l'arène du boss.
- **Points ouverts :** écarts transmis à la DA (ART-HANDOFF) : clips des bras vides, rune de la caisse sur une seule face, droits
  « à confirmer », mains du traqueur peu lisibles en « sombre », LOD de la goule ; **cachettes du traqueur** (options : reach 15 m
  depuis 2.5 m du centre, tours et galeries d'angle jusqu'au plafond, ou portée 15.5 m) ; performance (fil principal).
- **Piège noté :** changer de branche avec une scène ouverte bloque l'éditeur (boîte « scène modifiée ») : ouvrir une scène vide
  avant.

**Correctifs du rendu (`correctifs-rendu`, depuis `main` après la fusion de `rendu-integration`) — 2026-11-16, non poussée, à
tester par l'utilisateur dans `integration-lot`.** Un commit par point.
- **Phase enragée (`a2a3175`) :** ambiance « horreur » en fondu de 2 s au passage en Enragé (`AmbianceSettings.enraged`,
  `enragedTransition`), retour à la remise à zéro ; 4 signaux lisibles à 50 m en horreur.
- **1. Scintillement (`775d5fa`) :** couloirs et repaire coupés à la face extérieure du mur de la salle (les embrasures
  doublaient murs, sols et plafonds), pont d'égout +2 cm, cadres et toits des annexes, passages en hauteur, galeries, seuils,
  étagère, lit, gradins de la salle haute remplie (revalidée). Détecteur de faces confondues (triangles coplanaires de même
  orientation qui se recouvrent, visibles) : 5 seeds, 1 034 → 43 à 53 cas par carte (≈ 2 000 → 16 m², même matériau uni).
  Props de la DA étiquetés « DA » (`PropLibrary.BuildAll` les écrasait).
- **2. Arcs (`a223b70`) :** collision de l'anneau (boîtes sur sa ligne médiane relevée sur le maillage) pour les joueurs et les
  objets, le traqueur passe au travers (`StalkerPassesThrough`) ; piédroits = `SM_Colonne` réduite (1.05 × hauteur × 1.6 m)
  avec collision. 5 seeds : 1 459 arcs conformes.
- **3. Galeries (`6163965`) :** eau à 16 m sous le couloir (`MapLayoutSettings.galleryWaterDepth`), retour au palier
  seulement au contact de l'eau (avant : dès 1.5 m) ; 20 seeds conformes (0 traction, 0 cachette).
- **4. Lueur après la mort (`65accfd`) :** goule (yeux, gueule, corps, disque), volant et colonne du traqueur éteints à la mort,
  à la remise à zéro et au recyclage.
- **5. Distance de lecture (`ba7009c`) :** textes en monde lisibles jusqu'à 20 m, invisibles à 35 m, jamais au-delà de
  0.8 × la fin du brouillard ; yeux des goules et des volants sous le seuil du bloom hors annonce.
- **6. Ambiance « sombre » (`14882c5`) :** par défaut (0.03, brouillard 6 → 45 m) ; horreur passée à 5 → 35 m pour rester
  la plus sombre ; colonne lisible à 50 m, annonce du bond à 30 m.
- **7. Références des modules (`29d135b`) :** `docs/design/level/references-modules.md` à remplir ; placement inchangé.
- **8. Lanterne (`5c733c2`) :** 15 m, 25°, brûlure 4 s à 12 PV/s (ticks 0.25 s, relancée sans cumul), « Braises » +2 s ;
  1 goule ou 3 en 5 s (2 jets) à 5 et 15 m ; barillet 1 s seul, 5.5 s pour 3.
- **Pièges notés :** `BuildRandomLevelScene` refait `Sortie_Arene` en greybox (relancer *Décor : poser les modèles livrés*)
  et invalide les modules ; `PropLibrary.BuildAll` ne garde que les props étiquetés « DA ».

**Intégration du rendu et du lot de modélisation (`rendu-integration`, depuis `main` après fusion de la PR #3) — 2026-11-16, à
valider en Play Mode, non poussée.** Un commit par bloc.
- **Bloc 0 (`87f0bc4` sur `main`, poussé) :** PR #3 fusionnée (commit de fusion, branche `art` gardée) : 193 fichiers dans
  `Assets/Art/`, 108 dans `docs/art/`, `MANIFEST.md` ; chaque asset a son `.meta` ; réimport sans erreur ni avertissement.
- **Bloc 1 (`6019565`, `ddbd56e`) :** contours plein écran (`M_Contours`, clair dans le noir), pré-passe sans les couches des mains
  (captures avant/après : `docs/design/features/vue-fps/captures/rendu_mains_mur_avant_apres_prepasse.png`), ambiance intermédiaire
  partagée (`Ambiance_Intermediaire` : 0.055, brouillard 10 → 85 m ; `Ambiance_Horreur` prête, non activée), passe transparente des
  mains, contour par coque inversée (`ItHunts/ContourMains`). **GPU Resident Drawer essayé puis coupé** (37 ms par image en solo
  avec, 9,0 ms sans) ; variantes BRG gardées.
- **Bloc 2 (`32eff09`) :** lanterne du joueur (`PlayerLantern_Default` : 0.6, 6 m, sans ombres, à la hanche, couleur du personnage,
  une par joueur, exclue des mains par les calques de rendu) ; `SM_LanterneFeu` en main (`FX_Flamme`, `FX_Jet`, verre allumé par
  `_EmissionColor`, éteint pendant la recharge) ; test EditMode du barillet en toon.
- **Bloc 3 (`de92ab4`) :** `DecorModelInstaller` : props et arène du boss remplacés par les modèles (herse, 6 leviers ±35° et lueur,
  treuil, 5 plateformes qui tombent). `INTEGRATION-PROPS.md` absent de la PR : repères repris du manifeste (torche +0.545 m).
  Herse : bas de la grille à 16 m ouverte (traqueur 14 m), même passage de 10 m. Collisions du jeu gardées (échafaudage : boîte pleine).
  - **Texturé :** kit Arc, Pilier, PortePiece, GrandePorte, Poutre, PontEgout4/14 ; props Torche, TorcheEteinte, Cage, Grille,
    Tapisserie, Chaine, Tuyau, Echafaudage, ColonneBrisee, Colonne ; arène (herse, leviers, treuil, plateformes) ; barillet, lanterne.
  - **Encore greybox :** Kit_Mur4x4, Kit_Sol4, Kit_Escalier, Kit_Pont ; props Armoire, Autel, Banc, Caisse (+ attrapable),
    Chaise attrapable, Chaudron, CoffreDeco, Etagere, Gravats, Lanterne, Lit, MurEffondre, Paille, PileDeCaisses, Poutre, Ratelier,
    Sarcophage, Statue, Table, Tonneau (+ attrapable) ; coffres, marchands, grand lustre ; goule, volant, traqueur ; mains ; orbe
    (`SM_Orbe` livré, pas intégré) ; pièces non magnétiques du parcours.
- **Bloc 4 (`226605d`) :** signaux lisibles à 50 m (planches 10/30/50 m, arrêt et course : `docs/design/features/vue-fps/captures/signaux/`) :
  colonne rouge de la zone de prise (12 m, 4 m, 0.3), goule qui luit pendant l'annonce (yeux, gueule, corps), lueur à portée des
  caisses 1.0, halo des âmes (1.4 m, 0.35).
- **Bloc 5 :** D-067 (rendu) au GDD, règle 8 de `CLAUDE.md` alignée sur ART-HANDOFF § 1, demandes à la DA dans ART-HANDOFF.
- **Performance finale** (build de développement, seed 104729, 1920 × 1080, pic de vague actif) : solo **13,8 ms** médiane (5 % les plus
  lents 26,9 ; GPU 3,8 ; 7 235 draw calls), écran partagé **16,4 ms** (22,6 ; GPU 5,3 ; 8 372). Avant le bloc 1 : 22,2 ms (mesure
  peut-être perturbée) ; après le bloc 1 : 9,0 ms (13,1) et 15,7 ms (22,7). **Objectifs non atteints** (< 10 ms et < 16,6 ms, 5 % compris) :
  CPU, pas GPU ; cause principale les modèles de torche (4 matériaux × ≈ 1 645 torches) et les maillages de la DA non fusionnables.
- **Points ouverts :**
  - textures à refaire (`docs/art/MATERIAUX.md`) ;
  - questions à la DA : rouge du renard contre rouge du danger (colonne, goule), `IsMatchingLightLayer` dans le shader toon (la
    lanterne éclaire encore les artefacts), *Read/Write* sur `Assets/Art/Models/Niveau1`, `INTEGRATION-PROPS.md` manquant,
    normales lissées des FBX de vue FPS ;
  - réglage « horreur » pour la phase enragée du traqueur (à décider) ;
  - orbe v2 et bras FPS à intégrer plus tard (modèles, couleur du personnage, animations des anneaux, jauge en tablettes, poses des mains) ;
  - performance : fusion des modèles (Read/Write), moins de torches ou de lumières, regrouper les matériaux de la torche, réessayer le
    GPU Resident Drawer après la fusion, plafonner les pas fixes.

**Suite de la carte (`carte-suite`) — 2026-11-15, playtestée** (hauteurs, chargement, fluidité, menu validés ; arcs disparus, coop
bloquée, zones de parcours trop rares → corrigés ci-dessous), **fusionnée dans `main`**.
- **Après le playtest (2026-11-15) :**
  - **Arcs** : la fusion du décor ne peut pas combiner un maillage importé sans *Read/Write* (modèles de la DA) ; ces pièces
    (≈ 500 par carte : arcs, colonnes, portes, poutres, ponts) restent séparées au lieu de disparaître ; arcs aussi dans les couloirs
    en pente. 5 seeds : 1 465 arcs visibles, sur deux piédroits, sommet à 0.3 m au plus du plafond. Demande à la DA (ART-HANDOFF) :
    *Read/Write* sur `Assets/Art/Models/Niveau1` pour qu'ils soient fusionnés aussi.
  - **Coop** : au menu, le joueur 2 choisit avec sa manette (◀ ▶, A valide, B change) et la partie ne part qu'après sa
    validation ; en jeu, menu de choix dans sa moitié d'écran (`CharacterSelectMenu`, personnages pris grisés). Cause du blocage :
    le Start d'une nouvelle manette faisait basculer le joueur 1 (clavier) sur elle (changement automatique de l'Input System).
  - **Zones de parcours** : 24 par carte (6 salles de parcours entièrement non magnétiques, 18 galeries ouvertes sur le couloir),
    au moins 2 par tiers, 8 coffres de parcours ; parcours exigeants (`ParkourCourse` : poutre étroite, marches qui tombent, deux
    murs plus longs qu'un double saut, vide à dash) vérifiés sans traction. 20/20 seeds : 3 045 sauts faisables, 0 surface
    magnétique sur 45 695 rayons dans les salles de parcours, 0 cachette.
  - **Lanterne** : 32 PV, recharge 2.5 s, cône depuis la lanterne : goule 3.1 s (barillet 1 s), groupe de 3 3.1 s (5.4 s),
    goule qui rampe à 1 m touchée. **Améliorations** au marchand (portée, cône, recharge, brûlure) ; le marchand vend celles de
    l'artefact du joueur. 385 tests EditMode (384 + 1 non concluant voulu).
- **Point 1 (`26a7b85`) — arcs au sol :** l'arc du kit (11 m) était posé au niveau du sol dans des couloirs de 19 m (portique isolé),
  et les arcs de porte entraient de 1,8 m dans les couloirs en pente. `KitBuild.Arch` : sommet sous le plafond, sur deux piédroits ;
  arcs de porte dans l'épaisseur du mur. 5 seeds : 0 pièce mal posée.
- **Point 2 (`a2ee72f`) — hauteurs :** plafond par type de module (`ModuleShape` : simple 18 m, étages 22 m, égout 16 m, réglable par
  module avec `ceilingOverride`), croisements 16 m, couloirs 15 m (minimum 15). Avant : salles 36, croisements 24, couloirs 19.
  18/18 modules validés ; traqueur accroupi 1,3 % du trajet avant et après (3,9 s sur ≈ 440 s).
- **Point 3 (`400aea6`) — galeries et passages en hauteur :** `PlanBypass` (6 galeries, 10 passages, minimum 6 zones et une dans chaque
  tiers départ → sortie) ; passage fermé s'il est bloqué par un module. 20/20 seeds conformes, 16 à 19 zones par carte.
- **Point 4 (`0785cf2`) — écran de chargement :** `LoadingScreen` (titre, étape, barre) ; construction par tranches de 50 ms, systèmes
  de jeu sous une racine inactive activée à la fin. 6,7 à 7,5 s dans l'éditeur, 5,1 s en build.
- **Point 5 — performance :** fusion du décor par groupe (salle, couloir…) et par matériau au chargement (`RandomLevel.Batch`), à la
  place du batching statique : ≈ 53 600 pièces → 529 renderers. Build de développement, seed 104729, 1920 × 1080, timer à 13:40 :
  solo **19,2 → 8,0 ms** médiane (95 % 14,8 ; GPU 2,9 ; 2 555 draw calls au lieu de 4 940) ; écran partagé **13,4 ms** médiane
  (95 % 16,1 ; GPU 4,1). Reste : `FinishFrameRendering` 4,1 ms (8,1 à deux), 1 718 lumières, ≈ 4 600 renderers hors kit.
- **Décisions D-064 à D-066 :** un artefact par personnage, choisi avant la run, jamais deux fois le même en coop (renard rouge :
  lanterne de feu ; caméléon orange : barillet ; koala vert et hibou bleu : à concevoir) ; variété par les améliorations et la
  carte ; orbe à la couleur du personnage avec l'orbe v2 (anneaux et runes pour les actions, 6 tablettes : 2 traction,
  1 répulsion, 3 mouvement), code couleur actuel gardé d'ici là. GDD, fiches artefacts et orbes, ART-HANDOFF.
- **Lanterne de feu et choix du personnage :** menu (joueur 1 et joueur 2, cartes des 4 personnages, koala et hibou grisés) ;
  `CharacterDefinition` / `CharacterRoster` / `CharacterPick` ; `RunSession.Characters` ; lanterne (`FireLantern`,
  `FireLanternJet`, `FireLanternView`, `FireLantern_Default` : 5 m, 40°, 0.6 s, 25 PV, 3 s) en greybox dans la main gauche ;
  HUD, debug ; marchand : soin seulement pour le renard. Outil *ItHunts > Personnages : …* (assets, prefab, scènes).
  **Mesuré :** goule seule 7.2 s (3 jets) contre 1.0 s au barillet ; groupe de 3 : 7.2 s contre 5.4 s ; un jet dans un couloir :
  8/8 d'une meute (visée au groupe), 4/5 d'une file, 2/6 d'un groupe étalé sur 14 m. 377 tests EditMode (376 + 1 non concluant
  voulu). **À trancher en playtest :** équilibre de la lanterne contre une goule seule ; teintes des personnages face aux
  couleurs réservées (rouge = annonce d'attaque).

**Carte aléatoire — 2026-11-15, validée en playtest le 2026-11-15 et fusionnée dans `main`** (branche `carte-aleatoire` supprimée ;
réserve : ça rame un peu, mesuré avec Blender et deux projets Unity ouverts — mesure de performance dans `carte-suite`). Fiche : `docs/design/features/run-et-niveaux/` (design « Carte aléatoire », testing « Carte aléatoire »).
- **Bloc 1 (`ae95e8c`) :** format des modules (empreinte 1 × 1, 1 × 2, 2 × 2 ; entrées `ModuleDoor` ; contraintes ; résultat de
  validation), kit (`KitSet`, `KitBuild`), catalogue `ModuleCatalog_Niveau1`, outil *ItHunts > Modules : valider…* (format, voies du
  traqueur, cachettes `ReachCheck`, NavMesh entre les entrées, pièces hors kit, contenu).
- **Bloc 2 (`d4732dc`) :** `MapPlanner` (déterministe, seed de la run × numéro du niveau ; boucles sans pont ; sortie à 6 salles au
  moins tirée au hasard ; repaire au bord ; pentes ≤ 20° ; niveau haut seulement pour les modules qui le déclarent) ; `RandomLevel`
  (construction avant tous les systèmes) ; scène `Level_01_Aleatoire` (niveau 1 de la run) ; `Level_01` = carte fixe de référence
  (menu) ; prefabs du kit avec les modèles livrés (colonne, arc, porte de pièce, grande porte, poutre, ponts d'égout).
- **Bloc 3 :** les 15 modules en pièces du kit (structure), entrées aux ouvertures actuelles, escaliers des modules en collision lisse
  avec marches visuelles, 3 modèles vides à dupliquer (1 × 1, 1 × 2, 2 × 2, poids 0) ; 18/18 validés (corrigés : gravats dans une voie,
  statues sur les tours, poche d'angle de l'habillage) ; ART-HANDOFF : section « Kit de construction » (pièces par priorité, pas de 4 m).
- **Mesuré :** 20/20 cartes conformes (et 5/5 avec grandes salles), 0 couloir hors boucle, départ–sortie 6 à 10 salles, pente max
  14°, 0 cachette sur 244 357 points (hors zone de sortie et fosses de parcours) ; tirage 2 à 6 ms, construction 3.4 s (5 s au plus),
  NavMesh 1.7 s (2.8 s au plus) dans l'éditeur ; escaliers : même vitesse qu'une pente nue (avant : bloqué au pied en montée).
- **Limites connues :** pas d'écran de chargement (≈ 5 s figé) ; galeries de parcours le long des couloirs et passages en hauteur
  (Level_01) pas encore dans la carte aléatoire ; aucun vrai module de grande salle (seulement les modèles) ; ≈ 65 000 pièces du kit
  par carte (à réduire avec les demi-pièces ou des pièces plus grandes) ; `Level_01` reprend les modules convertis (prefabs liés).
- **Unity, à retenir :** régénérer une carte plusieurs fois dans la même session sans libérer les maillages combinés a fait planter
  l'éditeur (corrigé : `Resources.UnloadUnusedAssets` à chaque régénération).

**Modèle du barillet (demande de la session art) — 2026-11-15, validé en playtest (barillet, chambres, rotation, rechargement,
lisibilité) et fusionné dans `main`** (branche `barillet-modele` supprimée).
- `SM_Barillet` (PR #2) remplace le barillet greybox en main gauche (`BarrelModelInstaller`, menu *ItHunts > Barillet :
  installer le modèle…* ; le prefab Player est modifié en place, le constructeur l'utilise aussi). Logique de tir inchangée.
- Chambre chargée = émission (blanc chaud (1, 0.86, 0.66) × 3) ; vide = couleur de base #1A120C ; inactif (objet tenu) ×0.2 ;
  un matériau partagé (copie de `M_Barillet_Chambre`, émission activée) et un MaterialPropertyBlock par chambre.
- −60° autour du Z local par tir, 0.08 s, courbe réglable (`BarrelSpin`) ; rechargement : les chambres se rallument une à une.
- État visuel dérivé du compteur de munitions et de tirs du joueur (`BarrelModel`), sans état à part (coop en ligne).
- Laiton : `Assets/_Project/Art/Barillet/M_Barillet_Laiton.mat` (couleur de base seule) posé sur l'instance ; le FBX n'est pas
  modifié. **Canon encore greybox** (aligné sur la chambre du haut).
- Réglages : `Barrel_Default` (émission, intensité, part inactive, couleur de base, durée et courbe de rotation),
  `FpsView_Default` (position (0, 0.065, 0.05), rotation, échelle 1 du barillet).
- Mesuré en Play Mode (Test_Movement, tirs réels) : chambres conformes après 1, 3 et 6 tirs et après le rechargement ;
  rechargement ······ → 1····· → … → 123456 ; 30 tirs rapides avec une rotation allongée à 0.4 s (29 tirs pendant
  l'animation) : angle final 0°, écart 0° à un multiple de 60° ; à 20 m/s (FOV du monde 81.4°), mains dessinées à 60°,
  barillet à 0.385 m devant la caméra au plus près (plan proche 0.01 m).
- Tests : 7 nouveaux (`ModeleBarillet_*`), 363 au total, 362 passent, 1 non concluant à dessein.

**Corrections après le playtest des étapes 8 et 9 — terminées le 2026-11-15, fusionnées dans `main`** (branche `correctifs-8-9`,
supprimée après la fusion). Playtest du 2026-11-15 : vagues, ennemis endormis, chasseurs, pression au début de zone **bons** ;
goules en hauteur **bonnes** (plus de goule coincée ni de téléportation visible) ; aide à la visée à la manette pas encore testée.
Commits (un par point) :
- **Point 9 (`4b04c24`) :** sortie affichée sur la minimap dès le début de la zone (OK pour les tests, voir les notes plus bas).
- **Point 8 (`cdbfbae`) :** « Playtest : combat de boss » au menu principal ; **Page bas** en jeu (éditeur et builds de dev).
- **Point 6 (`ecc186a`), puis point 1 du 2026-11-15 (`fb667eb`) : textes en monde.**
  - Cause des textes encore vus à travers les murs : les textes de la sortie et de la salle verrouillée étaient enfants de la
    porte, une boîte étirée (10 × 10 m pour la sortie) ; ils en héritaient l'échelle, devenaient géants et déformés et dépassaient
    des murs en se tournant vers la caméra.
  - **Règle unique** (`WorldLabel`, `PlayerSight`) : un texte n'est affiché que si aucun décor (collider solide sans Rigidbody,
    ni joueur ni ennemi) n'est entre la caméra et lui, en plus du test de profondeur ; toujours à l'échelle du monde ; sur une
    porte, un texte devant chaque face.
  - Mesuré dans Level_01, rendu réel en 1080p (moitié de largeur en écran partagé), texte allumé / éteint : coffre, offre du
    marchand, orbe de choix, salle verrouillée, sortie et « à terre » : **5/5 caché** derrière un mur ou une porte (0 pixel) et
    **5/5 lisible à 10 m** (lignes de 19 à 37 px), en solo et pour chaque joueur en écran partagé ; textes réservés à l'autre
    joueur : jamais dessinés (10/10 et 10/10).
- **Point 2 du 2026-11-15 (`cdd478b`) :** téléportation de déblocage d'une goule seulement hors de la vue de tous les joueurs
  (elle et son point d'arrivée) ; sinon elle attend. Mesuré sur une zone complète (×4) : 17 téléportations, **0 vue**, 480 essais
  remis à plus tard.
- **Point 3 du 2026-11-15 (`6992282`) :** enseigne du marchand au-dessus de la rangée d'offres (centre à 5.5 m, dans leur plan).
  Mesuré au pixel sur 50 points de vue : texte touché par l'enseigne dans 27 vues avant, 3 après (à 2–3 m et 60°, au milieu des
  orbes).
- **Point 4 du 2026-11-15 (`58ffcf1`) :** règle des `.meta` des assets art dans l'encadré d'ART-HANDOFF.
- **Points 4, 5, 7, 10 et 11 du 2026-11-14** (`4bee9e8` D-060, `9930373` D-061, `859e6da` D-062, `f9d975a` D-063, `c7c374e`) :
  apparitions au début des vagues, aggro et chasseurs, goules qui grimpent, aide à la visée, ART-HANDOFF.
- **Tests :** 356 EditMode, 355 passent, 1 non concluant à dessein ; aucun test PlayMode.
- **Unity, à retenir :** après une compilation lancée par la CLI, l'éditeur au second plan ne recharge pas toujours le code :
  `EditorUtility.RequestScriptReload()`. Avec l'éditeur art ouvert (`ItHunts-art`, port 7801), **toujours passer
  `--project-path`** : sans lui, la CLI peut viser l'éditeur art. En Play Mode piloté, activer `set_run_in_background`.

Validé en playtest le 2026-11-15 (choix pris seul) : taille des vagues (3 à 10 goules, 1 à 3 volants, plus en Enragé, en 8 s au
plus) ; paliers des chasseurs (0, 15, 35, 55, 75 % du timer, volants inclus) ; escalade (30 m, mur à 12 m au plus, bond jusqu'à
12 m, abandon 10 s après 2 échecs en 20 s) ; aide à la visée (×0.5, 8°/s, cône de 6°, 45 m ; ressenti à tester plus tard).

**Notes pour plus tard (ne pas coder maintenant) :**
- **Combat de boss : à repenser.** Le traqueur s'y comporte comme dans tout le niveau, ce qui ne convient pas à une seule salle :
  beaucoup trop dur et pas spécialement fun. L'utilisateur réfléchit à ses **attaques propres à l'arène** et aux **moyens de le
  battre**. Le combat actuel (pièges, leviers) reste la base provisoire.
- **Minimap :** la sortie est visible dès le départ (bon pour les tests). Quand le niveau sera jouable, elle devra être
  **découverte comme les coffres**, et sa position sera **tirée au hasard** avec la carte aléatoire.

**Étapes 8 et 9 terminées pour le niveau 1 : validées en playtest le 2026-11-14 et fusionnées dans `main`.** Le playtest a
validé l'ensemble, et la coop en écran partagé marche bien. Les corrections demandées après ce playtest se font sur la branche
`correctifs-8-9` (voir plus bas).

**Étape 9 — écran partagé local à 2 joueurs, terminée et validée en playtest (2026-11-14).** Fiche :
`docs/design/features/coop-locale/` (design, implémentation, checklist). D-059.
Commits :
- `c34f97f` : écran partagé, à terre, réanimation, spectateur ;
- `1b04ff3` : lueurs par caméra ;
- avant la fusion : `5a7228c` (F3 éteint par défaut), `24fdaaf` (invulnérabilité de debug remise à zéro automatiquement),
  `4a8622b` (menu pause par joueur, « Quitter l'écran partagé » pour le joueur 2).
- **Rejoindre :** Start sur une manette, au menu ou en jeu ; clavier-souris + manette ou deux manettes ; joueur 1 verrouillé
  sur son appareil.
- **Écran :** gauche / droite par défaut, haut / bas en réglage. Chaque moitié a :
  - son HUD, ses mains, la minimap commune ;
  - son FOV adapté (×1.1 ou ×0.75) ;
  - les lueurs de ce que le joueur vise ;
  - la capsule de l'autre joueur.
- **Correction trouvée en vérifiant :** les deux caméras virtuelles étaient sur le même canal, parce que le rig de vue est
  détaché du joueur. La vue du joueur 1 suivait donc celle du joueur 2.
- **À terre (D-007) :**
  - relevé en 3 s à 2 m avec E / Y, retour à 50 PV ;
  - spectateur après 30 s, vue de l'allié, retour à la zone suivante ;
  - tous à terre ou morts : écran de fin ;
  - le traqueur ignore les joueurs à terre, mais son coup met à terre.
- **Menu pause** (Échap / Start), un par joueur, dans sa moitié, avec son seul appareil ; le jeu continue :
  - entrées : Reprendre, Quitter l'écran partagé (joueur 2 seulement), Menu principal ;
  - si le joueur 2 quitte alors que le joueur 1 est à terre, le joueur 1 meurt (personne pour le relever).
- **Tests :** 10 tests `CoopTests` ; 341 tests passent au total, 1 non concluant à dessein.
- **Performance de l'écran partagé : objectif de 16.6 ms non atteint.**
  - build : 17 à 18 ms au pic, contre 13.4 ms en solo ;
  - éditeur : 39 ms (30 ms en solo) ;
  - détail dans la fiche « testing ». Une torche sur deux n'éclaire plus dans les bases régénérées, mais pas dans Level_01.
- **Level_01 :** recopiée plusieurs fois le 2026-11-14 (corrections des plaques, puis `SplitScreen`). **Ne plus la recopier**
  (règle de l'utilisateur). Pour avoir la réduction des torches, régénérer la base et recopier soi-même.

Validé en playtest le 2026-11-14 (choix pris seul à l'étape 9) :
- FOV ×1.1 en gauche / droite, ×0.75 en haut / bas ;
- Start pour rejoindre ;
- capsules pour les corps ;
- son seulement pour le joueur 1 (**provisoire**) ;
- spectateur avec les mains de l'allié ;
- une torche sur deux sans lumière.

À valider : étiquette « À terre, maintiens E / Y » à 1.3 m au-dessus du joueur couché ; menu pause (pas de pause du jeu).

Optimisations et nettoyage, par priorité (liste seulement, rien de codé) :
1. **FixedUpdate de 1 600 scripts (4 à 6 ms) :**
   - couper les `Grabbable` et `ImpactDamager` au repos (réveil au contact, à la prise ou dans le rayon d'un joueur) ;
   - mettre en veille les ennemis loin de tout joueur.
2. **Lumières (environ 3 ms en écran partagé) :**
   - passer le renderer en Forward+ (culling des lumières par tuiles) ;
   - ou cuire l'éclairage des torches fixes ;
   - ou limiter les lumières temps réel à un rayon autour des joueurs.

   Demande de réglage partagé : à faire sur `main`.
3. **Occlusion culling** des salles (portes, couloirs), à cuire par niveau.
4. **Distance de vue :** far clip de la caméra et de l'objectif Cinemachine à environ 250 m (−1 ms mesuré).
5. ~~Affichage de debug (F3) éteint par défaut~~ : fait (`5a7228c`).
6. ~~`DebugInvulnerable` remis à zéro automatiquement~~ : fait (`24fdaaf`).
7. **Combat de boss** trop court pour un joueur rapide (estimé 35 à 45 s) : leviers plus éloignés, ou un 3e levier.
8. **Test_Arena :** remplacer les voies au sol à côté des pentes par des pentes sur toute la largeur, comme au niveau 1.
9. **Nettoyage :**
   - regrouper les `beginCameraRendering` (WorldLabel, ViewerOnly, Chest, SplitScreen, TelekinesisView) en un seul
     répartiteur par caméra ;
   - GUID de `Level_01.unity.meta` changé à chaque recopie (remis à l'ancien).

**Après le playtest de l'étape 7 (partie 2)** — fait et **validé en playtest** le 2026-11-13 ; bloc 0 (tronçons de couloir,
D-047) ajouté ensuite, commité et poussé sur `main` :
- **Tronçons de couloir (D-047) :** volumes de type Couloir, 40 m au plus (`corridorSegmentLength`), 24 dans Test_Arena ; pas
  d'apparition dans le tronçon d'un joueur ni les voisins. Mesuré (joueur au milieu d'un tronçon, 90 s, distance minimale à 0) :
  **28 + 27 apparitions sans tronçons, 0 + 0 avec**.
- **Traction : 2 charges** au lieu de 3 (réglable), 6 s par charge, 2.5 s, 18 m/s inchangés (D-036 mise à jour).
- **Murs (D-044) :** impulsion et bonus du wall jump, et gain du strafe le long du mur, **−15 %** (`wallGainScale` 0.85) et
  arrêtés à **18 m/s** (`wallGainCap`) au lieu de 20 ; l'élan acquis est gardé, rien ne freine un joueur plus rapide. Mesuré
  (couloir à deux murs, 30 wall jumps) : vitesse max **20.0 → 18.0 m/s** ; lancé à 22 m/s : 22 m/s gardés.
- **Âmes en coop (D-007 et D-043 révisées) :** gagnées en commun, dépensées individuellement : éclats communs, n'importe quelle
  orbe ramasse, chaque joueur vivant reçoit 1 âme par éclat ; plus d'éclats par joueur (réglage `shareRadius` retiré).
- **Coffres par joueur :** une ouverture par joueur, en payant, avec son propre choix parmi 3 (orbes vues par lui seul,
  `ViewerOnly`) ; couvercle et texte par caméra (« Coffre / ouvert » pour qui l'a ouvert).
- **Salles (D-045) :** `RoomVolume` (16 dans Test_Arena : salles, carrefours, repaire, pièces fermées ; à poser à la main au
  level design) ; jamais d'apparition dans la salle d'un joueur (au choix du point et à la fin de l'annonce). Mesuré : joueur au
  centre de la salle C, 90 s : **69 apparitions sur 184 dans sa salle sans volumes, 0 sur 146 avec**.
- 316 tests (1 non concluant à dessein). Checklist : `docs/design/features/ames/testing.md` (« après le playtest (partie 2) »).
- **Réseau (étape 11) :** collecte et gain des âmes, ouvertures de coffre et choix sur l'autorité ; les orbes de choix
  n'existent que chez leur joueur.

**Étape 8 — corrections après le playtest (partie A), 2026-11-14, validées en playtest et fusionnées dans `main`.** Fait,
un commit par point :
- **Point 5, soin (`b58cced`) :** 4e offre fixe du marchand de départ, +40 PV pour 35 âmes.
- **Point 1, population (D-054, `f662ae1`) :**
  - 2 à 5 goules par grande salle, 1 à 2 volants dans les salles hautes ; rien ne bouge avant la première sortie ;
  - vagues × surface / 36 300 m², borné 0.5 à 2.5 (niveau 1 : ×2.5) ;
  - mesuré sur 15 min : 0 grande salle vide sur 40, 164 à 344 ennemis vivants.
- **Points 2, 2 bis, 3, 4 (D-055 à D-057, `cd05cca`, puis correction `a3d3590`) :**
  - 31 props greybox (`Prop_*`, catalogue dans ART-HANDOFF) ;
  - 13 modules de salle et 2 de carrefour (prefabs, ajoutables sans code : voir la fiche run et niveaux) ;
  - une vingtaine de pièces annexes meublées, décor des couloirs, environ 3 900 props ;
  - 9 passages en hauteur (1 refermé, module devant) ; 6 galeries de parcours et 3 fosses ; pentes sur toute la largeur.
  - Mesuré :
    - 0 point hors de portée hors parcours (sur 48 323) ;
    - 0 dépassement sur 240 essais hors parcours ;
    - traversée de galerie réussie dans 89 cas sur 120, sans esquive.
- **Points 6 et 7 (D-058, `1d401c4`) :**
  - combat : 2 leviers par piège, pression × 0.7 par piège ;
  - arène : sur plusieurs hauteurs, avec décor, poche de la herse à 22 m ;
  - mesuré : 64.6 s à 7.5 m/s ; plus vite, la herse échoue en simulation (estimé 35 à 45 s) ;
  - Level_01 recopiée depuis la base (dernière fois le 2026-11-14, pour la correction des plaques).
- **Point 8, performance (`5ccbf2a`) :**
  - build de développement au pic, 1920 × 1080 : 18.7 → **11.4 ms** en médiane, 95e centile 16.1 ms ;
  - optimisé : minimap en texture, props fusionnés ;
  - reste : FixedUpdate de 1 600 scripts (3.2 ms), culling (1.4 ms).

Validé en playtest le 2026-11-14 (choix pris seul, partie A) : pièces fermées le long des couloirs ; salle de départ sans
module ; voie centrale des couloirs à 8 m ; leviers à portée de main du traqueur. Affichage de debug (F3) éteint par défaut
(`5a7228c`).

Encore à valider (partie A) :
- **Galeries :** paliers et poutres à portée de sa main ; Test_Arena garde sa voie au sol à côté des pentes.
- **Combat de boss :**
  - probablement trop court pour un joueur rapide ;
  - herse : il faut appâter le traqueur près de l'arche ;
  - le soin compte comme un achat au marchand (+25 %).

**Étape 8 — blocs 1 à 6 codés le 2026-11-13, branche locale `etape-8` (un commit par bloc, pas de push), playtest en
attente ; l'utilisateur fusionne après son playtest.** Fiche : `docs/design/features/run-et-niveaux/` (design, checklist).

Fait :
- **Bloc 1 (D-048), `2891dcb` :**
  - `Run_Default` : niveaux et difficultés, Facile seule jouable ; `RunSession` ; multiplicateurs des ennemis.
  - Salle de départ et timer à la première sortie ; traqueur 180 s, porte 600 s puis 200 âmes, Enragé 900 s.
  - Écran de fin ; PV 150 ; `CoopLife` (événements de l'étape 9) ; Page haut : +60 s au timer.
  - Mesuré dans Test_Arena : rien dans la salle de départ en 45 s ; événements à 3, 10 et 15 min ; victoire et mort.
- **Bloc 2 (D-050), `7d7ce81` :**
  - Marchand de l'artefact par joueur : 3 offres, +25 % par achat ; 5 améliorations du barillet provisoires.
  - Marchand placé au hasard retiré ; menu principal greybox (`MainMenu`, première scène du build).
- **Bloc 3 (D-051), `fb63959` :** `NonMagneticSurface`, refus en rouge, matériau à rayures ; mur d'essai dans la salle de
  départ de Test_Arena.
- **Bloc 4 (D-052), `e678c43` :**
  - Niveau 1 : `Level_01_Base` régénérée, `Level_01` jamais écrasée (recopiée une fois au bloc 6, sans retouche de
    l'utilisateur).
  - 800 × 785 m, 41 salles et 8 carrefours, 7 canaux, 14 coffres, 6 zones de parcours, 6 pièces, 204 volumes, 760 points
    d'apparition.
- **Bloc 5 (D-053), `ee1c5a3` :** minimap et brouillard de guerre (Tab : carte entière).
- **Bloc 6 (D-049), `810a353` :**
  - Arène de 80 × 80 m et trois pièges (herse, pont, lustre), dans Test_Arena et le niveau 1.
  - Mesuré (joueur simulé) : gagné en 21.5 s à 13.5 m/s et 20.8 s à 18 m/s, tué à 7.5 m/s.
- 324 tests EditMode (1 non concluant à dessein).

À valider (choix pris seul) :
- Aucune vague avant la sortie de la salle de départ ; porte payée une fois pour tous, sans hausse ; vagues arrêtées pendant
  le combat de boss.
- Marchand sans soin, nouveau tirage après chaque achat ; multiplicateurs de Moyenne et de Difficile.
- **Arène :**
  - poches de la herse et du pont : le traqueur atteint sinon le joueur par-dessus des murs de 10 m ;
  - un piège réussi rouvre le passage après 4 s ;
  - combat court (environ 21 s) ; sonné 4 s.
- Niveau 1 encore très régulier (grille) ; zones de parcours approximatives ; la carte fait 800 m, donc 3 min de course
  directe jusqu'à la sortie.

À faire ensuite : playtest de l'étape 8, puis fusion ; réanimation et spectateur avec l'écran partagé (étape 9).

**Vision de l'étape 8 (D-046)**, telle que donnée par l'utilisateur le 2026-11-13 :
- **Run :** une seule run du début à la fin : niveau 1 (cave du château), niveau 2, niveau 3, combat final (D-008 : tué
  seulement par l'environnement). **Difficultés :** Facile = niveau 1 (fin après son combat de boss) ; Moyenne = niveaux 1 et 2
  (ennemis plus forts) ; Difficile = niveaux 1, 2, 3, fuite devant le boss, combat final (encore plus forts). Stats des ennemis
  par difficulté dans un asset. **Seule Facile est jouable pour l'instant.**
- **Niveau :** salle de départ (tous les joueurs y apparaissent ; ni ennemi ni traqueur n'y apparaît ou n'y entre tant qu'aucun
  joueur n'en est sorti ; redevient normale à la première sortie, qui démarre le timer). Un **marchand dans chaque salle de
  départ** : améliorations de l'artefact (choix parmi 3 tirées au hasard, prix qui monte à chaque achat) ; les **coffres**
  donnent les améliorations de stats ; le marchand placé au hasard (D-043) est **supprimé**. Traqueur lâché à **3 min**.
  **Sortie** : grande porte verrouillée, déverrouillée à **10 min**, puis payée en âmes (prix à équilibrer), vers des escaliers
  qui montent au niveau suivant. **Timer de 15 min** (D-042) depuis la sortie de la salle de départ, puis Enragé (×2 âmes,
  D-009) : entre 10 et 15 min on part tranquille, après c'est le risque. **Combat de boss** contre le traqueur à la fin de
  chaque niveau, sans vraiment le tuer mais qu'il faut gagner, puis début du niveau suivant (forme : 2 ou 3 options à
  proposer). **PV du joueur** : 100 provisoires, à fixer.
- **Level design du niveau 1** (greybox, assets à la DA) : carte entière beaucoup plus grande que Test_Arena, salles plus
  travaillées, pas vide mais de la place pour bouger ; coffres dans le décor, derrière des obstacles ; **mini-zones de
  parcours sur surfaces non magnétiques** (la traction ne s'y accroche pas ; seuls endroits où l'on contourne le traqueur ;
  ailleurs on ne peut pas, ex. pentes avec du vide sur le côté) → réglage « surface non magnétique » par surface ; hauteurs
  permises si remplies de tous les côtés (objets, décor, ponts entre salles) ; ambiance cave de château, peut-être égouts et
  eau ; **minimap avec brouillard de guerre** (salles découvertes en les visitant ; coffres, marchand, salle verrouillée,
  sortie, salle de départ affichés une fois découverts ; en coop, alliés visibles et découvertes partagées ; version greybox).
- **Menus (greybox) :** menu principal (personnage : 1 seul ; difficulté : Facile seule ; Lancer le niveau 1 ; accès direct à
  Test_Movement et Test_Arena) ; inviter un ami, multijoueur, paramètres, touches : présents mais grisés.
- **Mort et fin de partie :** écran de mort, réanimation par un allié (D-007 : non réanimé = retour à la zone suivante),
  spectateur d'un allié vivant, écran de fin si le dernier joueur vivant meurt ; en solo : mort = écran de fin, puis menu.
- **Réponses de l'utilisateur (2026-11-13) :** combat (b) pièges ; égouts (a) canaux en décor, 20 PV ; carte 800 × 800 m
  (environ 50 salles, réglable) ; PV 150 ; porte 200 âmes ; 4 à 6 améliorations du barillet provisoires.

**Notes pour plus tard (ne pas coder) :**
- **Équilibrage complet** quand le niveau 1 entier sera jouable avec la DA : nombre d'ennemis, revenus en âmes (environ 280 en
  4 min aujourd'hui) contre prix des coffres, du marchand et de la porte, timer.
- **Améliorations :** les 8 actuelles sont provisoires et seront entièrement revues ; le système générique reste.
- **Étape 10 (DA), à refaire selon la direction artistique :** retour visuel et sonore des âmes, présentation des améliorations,
  menus du coffre et du marchand avec descriptions détaillées, minimap, HUD, menu principal. Menus inspirés de Ravenswatch et de
  Dead by Daylight pour la structure et l'ergonomie, dans le style du jeu (cel-shading, contours encrés, rendu peint, lumière
  dramatique). **Contrainte : aucun menu ne met le jeu en pause** (coop en ligne).
- **Niveaux 2, 3 et zone finale :** structure prête, contenu plus tard.

**Étape 7 terminée** (2026-11-13) : âmes et économie (D-043), **validée en playtest** (collecte, refus, marchand, coffres,
choix des améliorations ; salle verrouillée non testée, la mesure suffit). Après le playtest : coffre seulement avec E / Y ;
hausse du prix à chaque achat (%, par joueur, coffres / marchand / portes, 0 % pour l'instant). Commit et push sur `main`.
- Éclats d'âme (goule 3, volant 2, traqueur 0 ; ×2 en Enragé à la mort ; seulement si un joueur y est pour quelque chose dans
  les 5 s ; ramassés par l'orbe à 8 m, 25 m/s ; 30 s au sol, clignotent les 5 dernières) ; compteur dans le HUD, refus en rouge.
- Interaction **E / Y** (Y : interagir si un objet est visé, sinon recharger le barillet) ; coffres (30 âmes) à **3 orbes de
  choix en monde** (F ou E) ; marchand placé avec la seed (3 améliorations + soin) ; salle verrouillée (pièce NE, 50 âmes, coffre
  gratuit) ; 8 premières améliorations (assets, description d'une ligne).
- Mesuré : zone de 4 min, course 280 âmes ramassées, **209 perdues (43 %)**, bunny hop 314, **50 perdues (14 %)**, premier
  coffre payable vers 60 s ; collecte à 18 m/s (bunny hop 20/20, traction 12–14/19) ; coût 0.06 ms.
- **Unity :** la caméra d'un joueur n'est ni sous son prefab ni marquée MainCamera (Cinemachine, écran partagé) : la retrouver par
  `PlayerInput.camera` (`Interactables.PlayerOfCamera`, `CameraOf`).

**Étape 6 terminée** (2026-11-13) : ennemis, traqueur, volant, vagues, accalmie et coop des vagues, validés en playtest (le
choix de cible en coop et l'accalmie seront revus au prochain playtest, checklist F12 de `vagues/testing.md`). Commit et push
sur `main`.

**4 — Télékinésie, une seule orbe, dégâts** (D-023 à D-027) — terminée.

1. **Cœur et orbe unique** — **fait et validé en playtest** (2026-11-12), commit `5a8d454`. Retour : la prise à
   distance en mouvement était trop dure → aide à la visée.
2. **Aide à la visée de la prise (D-026)** — fait le 2026-11-12, **playtest en attente** : cône de 8° jusqu'à 5 m,
   3° à 60 m, le plus proche du viseur, jamais à travers un mur ; l'objet sélectionné pulse. Mesuré : écart toléré
   1.8 m à 10 m, 3.5 m à 30 m, 3.6 m à 60 m (0.4 m sans aide) ; en bunny hop à 20 m/s, caisse sélectionnée 86 à
   100 % du temps (17 à 71 % sans).
3. **Partie 2, dégâts (D-025)** — fait le 2026-11-12, **playtest en attente** (avec l'aide à la visée) : dégâts =
   masse × (vitesse d'impact − 8 m/s) × 0.1 pour tout impact sur un ennemi, seuil de 3 m/s pour une chute, tir
   tendu (sans gravité 0.5 s), 3 factices fixes et 1 mobile (100 PV, réapparition 3 s), chiffres de dégâts, barre de
   PV, éclats, sons provisoires ; jamais de dégâts aux joueurs ; pas de magnétisme sur un ennemi. Mesuré en Play
   Mode : 55 dégâts (25 kg à 10 et 20 m), 79.6 en course, 13.8 pour une chute de 4 m. 203 tests (1 non concluant à
   dessein : collision vérifiée en Play Mode). Checklist unique : `docs/design/features/orbes/testing.md`.
   **Playtest du 2026-11-12 :** dégâts bons ; gravité des objets à 20 m/s² ; lancer de travers en mouvement → D-027.
4. **Après ce playtest (D-027)** — fait et **validé en playtest** le 2026-11-12 (visée du lancer en mouvement,
   répulsion et étourdissement contre un mur) : lancer toujours vers le
   viseur (cause : toute la vitesse du joueur s'ajoutait ; écart en traction 10.3 m → 0.15 m à 30 m), gravité des
   objets à 20 m/s², dégâts de zone, durabilité selon la masse (1 / 2 / 3 / 5 lancers), lustre fragile (zone 4 m),
   répulsion en onde (4 m) qui repousse objets et ennemis, étourdissement 1.5 s contre un mur à 6 m/s ou plus.
   215 tests (1 non concluant à dessein). Checklist : `docs/design/features/orbes/testing.md`.
5. **Étape 4 terminée** (commit `44c2a2e`, poussé sur `main`).

**5 — Vue FPS et HUD, répulsion façon aimant, prise des ennemis étourdis, dégâts contre un mur** (D-028 à D-031) —
**fait et validé en playtest** (2026-11-12) ; commit et push sur `main`.
- Mains greybox, orbe dans la paume droite, mouvement et gestes ; HUD uGUI par joueur (barres de PV corrigées en
  mouvement : 168 px d'écart en saut → 0) ; debug F3 en trois pages.
- **Rendu des mains : option 2** (D-028), décidée après un build de développement : passe « Render Objects » à 60°
  après effacement de la profondeur, sans caméra en plus. Surcoût mesuré : 1 joueur **+0.26 ms** (option 1 : +1.60),
  2 joueurs **+0.63 ms** (option 1 : +7.19).
- Répulsion façon aimant (D-029) : cône de 14 m, onde de choc à l'appui, poussée continue au maintien (0.8 s).
- Prise des ennemis étourdis (D-030), dégâts contre un mur d'un ennemi repoussé (5 PV/m/s au-delà de 6, 40 au plus)
  et objets repoussés qui blessent (×0.5, sans zone) (D-031).
- 243 tests (1 non concluant à dessein). Commits `ba228b1` et `ce89ec8`, poussés sur `main`.
- Après coup : `0b23a0d` (poussé) rétablit trois réglages URP modifiés par le build sans le vouloir
  (préfiltres de shaders de `PC_RPAsset`, liste de `UniversalRenderPipelineGlobalSettings`, deux références mortes de
  `DefaultVolumeProfile`) et prévient la branche `art` dans ART-HANDOFF ; occlusion ambiante coupée pour les mains
  (taches sombres 21.9 % → 0.1 % des pixels des mains), validée en playtest.

**5 bis — Artefacts** (D-032) — **fait et validé en playtest** (2026-11-12) ; commit et push sur `main`.
- Système générique : un asset par artefact, pool `ArtifactPool_Default`, `RunState` (seed de la run) dans la scène,
  tirage sans doublon tant que le pool suffit, puis nouveau tour (validé).
- Barillet (`Barrel_Default`) : clic gauche / RT sans objet, tir instantané depuis les yeux (60 m), 6 coups, 12 dégâts,
  0.25 s, rechargement 1.2 s automatique ou R / Y ; inactif et rechargement annulé quand l'orbe tient un objet ; tir et
  lancer jamais au même tick ; munitions sur les chambres du barillet et sous le viseur ; traqueur insensible.
- Tir ami (`Coop_Default`, désactivé) : balles, objets lancés et repoussés ; jamais soi-même ; PV du joueur
  provisoires (`PlayerHealth_Default`, 100, réapparition). Découvert : Unity n'envoie pas `OnCollisionEnter` à un
  objet qui percute un `CharacterController` → contact testé à la main (`ImpactDamager.FixedUpdate`).
- Mesuré en Play Mode : temps pour tuer 2.95 s (9 balles), cadence 0.250 s, 100 % de touches à 10 / 30 / 60 m à l'arrêt
  et en bunny hop à 18 m/s (écart vu / balle 10 cm en moyenne), coût 0.015 ms par image. Pas de tir au sud de la zone
  Télékinésie (cibles à 10, 30 et 60 m). 255 tests (1 non concluant à dessein). Checklist :
  `docs/design/features/artefacts/testing.md`.

**6 — Ennemis, vagues et traqueur** (D-033 à D-041) — **terminée** (2026-11-13), ordre de l'utilisateur (D-011 d'abord) :
1. **Base commune et coups des ennemis** — fait (2026-11-12) : `EnemySettings` (base des factices, de la goule et du
   traqueur), `EnemyLocomotion` (NavMesh, pas fixe, sans NavMeshAgent), `EnemySenses`, `NoiseEvents`,
   `PlayerHealth.TakeEnemyDamage` / `Kill`, poussée sans ralentissement.
2. **Goule au sol** — fait : 10 m/s, bond annoncé 0.3 s, contact 15 PV/s ; tuée avec l'environnement en Play Mode.
3. **Traqueur complet** — fait : états, vue et ouïe, prédiction 0.8 s, pointe 16 / 18 m/s, suivi 85 % plafonné, NavMesh
   large construit au lancement (`ZoneNavMesh`), raccourcis réservés (`StalkerShortcut`), coup mortel, intuable, sons
   provisoires, timer et coin « enragé ». Mesuré : course rattrapée en 3.1 s ; bunny hop 18 / 26, traction, wall jump
   + glissade : jamais ; glissade + saut qui ralentit : 12 s.
4. **Prototype M3 dans Test_Arena** — fait. **Playtest du 2026-11-12 : arène beaucoup trop petite, pas assez
   d'ennemis, traqueur pas assez géant** → D-034 :
   - **Traqueur géant** (14 × 7 m) qui **attrape à la main** (15 m depuis l'épaule, annonce 0.5 s, recharge 2.5 s, mort
     quels que soient les PV), contact mortel, abris = passages étroits et bas seulement (4.5 m de dégagement), il
     contourne pour couper la sortie, perception de proximité, vue et sons à 60 m, tremblement de caméra à moins de
     25 m (confort), suivi de vitesse à 0.
   - **Nouvelle Test_Arena** : arènes de 100 / 90 / 90 m (plafonds 35 / 40 / 32 m), couloirs de 12 × 18 m (pente, sinueux,
     raccourcis à grilles) et tunnels de 4 × 4 m à petites portes, 3 repaires, 10 goules et 21 objets par arène.
     Dimensions réglables : `Assets/_Project/Settings/Editor/ArenaLayout_Test.asset`.
   - Mesuré : portée de la main sur 6 547 points, aucun hors de portée (14.8 m au plus), aucun abri dans les arènes ;
     dash qui esquive 20/20 à l'arrêt, en course, en bunny hop, sur la plateforme de 25 m (sans réaction : mort 5/5) ;
     lignes droites de 104 à 127 m (6.8 s au-dessus de 18 m/s dans A) ; 30 goules en chasse : 0.22 ms par pas fixe.
   - **Playtest en attente** (rien de commité). Checklist : `docs/design/features/boss-traqueur/testing.md`. 271 tests
     (1 non concluant à dessein).
   - **Playtest D-034 → D-035 et D-036** (« tu n'es en sécurité nulle part ») : fait le 2026-11-12, **playtest en
     attente, rien de commité** :
     - **Goules** : savent toujours où sont les joueurs (le plus proche par le chemin), encerclement (6 m), plus de poste ;
       **murs** (bloc 5 avancé) : liens `ClimbLink`, mode Mur, bond depuis un mur, une corniche ou un pont.
     - **Traqueur façon Pac-Man** : poursuite continue après la grâce, jamais de demi-tour (sauf cul-de-sac, ou Enragé),
       droit dessus quand le joueur est proche et devant, se baisse jusqu'à 9 m (×0.6), passe le bras par les ouvertures
       des pièces (15 m), portée depuis là où il tient (penché au bord), visée corrigée 0.15 s, cible la plus proche par
       le chemin toutes les 5 s au plus. `MazeGraph`, `ReachRoom`, `NotWalkable`, `ClimbLink`.
     - **Test_Arena en labyrinthe** (≈ 260 × 260 m, 9 intersections + repaire, 12 couloirs de 14 × 19 m en boucles, niveau
       haut à +16 m, pentes de 14 à 16°, ponts et poutres à 10 m, 6 pièces, 76 goules, 86 objets) ; tunnels et grilles
       supprimés. Captures : vue de dessus, traqueur baissé sous un pont, bras par une porte.
     - **Traction plafonnée à 18 m/s** sans jamais freiner (D-036).
     - Mesuré : 7 893/7 893 points à portée ; 9 passages bas franchis (0.5 à 0.7 s perdues) ; course rattrapée en 6.9 s
       (ligne) et 13.5 s (boucle), bunny hop et traction pas rattrapés en ligne droite, bunny hop rattrapé en 35 s dans
       la boucle ; Enragé : traction 8 à 12.5 s, traction + bunny hop 26 s dans la boucle (demi-tour) ; 0 demi-tour
       interdit ; goules : pont 6–8 s, salle haute 9.9 s, bond d'en haut esquivé 20/20 par un dash pendant le vol (0/20
       pendant l'annonce) ; 76 goules : 0.55 ms par pas fixe. 277 tests (1 non concluant à dessein).
     - Checklist : `docs/design/features/boss-traqueur/testing.md` (et goule, orbes).
   - **Playtest D-035 → D-037 et D-036 mise à jour** (2026-11-12), **playtest en attente, rien de commité** :
     - **Traqueur, mains levées** au-dessus de la tête (cylindre 10 m, jusqu'à 12 m au-dessus de la tête, trajectoire
       prédite 0.6 s ; annonce 0.2 s, zone mortelle 1 s, recharge 1.5 s, séparée de la prise) ; **corps, bras et mains
       mortels au contact** (modèle couvert à 100 %). Mesuré : **0 passage sur 200 essais**.
     - **Traction** : recharge d'une charge à 6 s (durée 2.5 s et plafond 18 m/s gardés).
     - **Goules** : murs grimpables partout (corniches tous les 5 m, ponts) et **murs de 16 m** au bout des voies au sol des
       couloirs en pente (16 m : pente 8 m + voie 8 m) ; salle haute en 6.8 s par le mur contre 9.7 s par la pente ;
       **25 en chasse au plus**, relais par la plus proche par le chemin (0.31 s) ; coût 0.34 ms par pas fixe.
     - 278 tests (1 non concluant à dessein). Checklist : `docs/design/features/boss-traqueur/testing.md`.
     - **Playtest du 2026-11-12 : traqueur beaucoup mieux, le reste bon** → D-038 : Enragé qui accélère sur la même cible
       (11 → 18 m/s en 30 s, paliers annoncés), aggro des goules (20 m en vue, 8 m, total 40, rendormie à 50 m / 5 s), murs
       grimpables automatiques (191 liens, 73 ms) ; passage à côté du traqueur mesuré quasi impossible (0/60 en 40 m, 4/60 en
       80 m) → trois pistes proposées (question ouverte dans boss-traqueur/design.md). 279 tests (1 non concluant à dessein).
     - **Playtest du 2026-11-12 : D-038 validé** (Enragé qui accélère, aggro, escalade automatique, mains levées, Pac-Man,
       pièces, traction à 6 s). Passage à côté : option (c) → **main plantée** (D-039) : 1.5 s après une prise ratée, sans
       nouvelle prise, rotation ×0.4, sans pointe. Mesuré : salle de 80 m 20/20 en bunny hop et en traction, salle de 40 m 3/20
       en bunny hop mais 20/20 en traction (plus facile que voulu, pistes dans boss-traqueur/design.md), couloir 0/60. Mur de
       16 m : 6.2 s contre 9.8 s par la pente. 280 tests (1 non concluant à dessein). **Main plantée : playtest en attente.**
     - Commit et push sur `main` : étape 6, blocs 1 à 5 (D-033 à D-039).
5. Goule sur les murs (fait et validé, D-035, D-037, D-038).
6. **Volant** (D-040) — fait le 2026-11-13, **commité sur `main` sans playtest complet (accord de l'utilisateur)** : poursuite (8 en chasse,
   aggro jusqu'à 12), ronde 8 m, piqué annoncé 0.4 s à 14 m/s (8 PV), attrapable à tout moment et libéré au bout de 2 s,
   éclatement de 3 m (30 → 7.5) quand il est lancé, chute étourdie après une répulsion, 4 par grande salle. Mesuré : prise
   en vol 120/120, 3 goules : 66 dégâts (combo répulsion → prise → lancer : 72 en 1.72 s), piqué esquivé 20/20, coût 0.18 ms.
   Main plantée passée à ×0.6 : la salle de 40 m reste à 20/20 en traction (×0.8 et ×1 aussi) → pistes proposées.
   282 tests (1 non concluant à dessein). Checklist : `docs/design/features/volant/testing.md`.
   - **Corrections du 2026-11-13, sans playtest** (vérifiées au prochain : checklist F12 de `volant/testing.md`) :
     - **Volant tué au premier impact de projectile** (`dieOnProjectileHit`) : balle, objet lancé ou repoussé, ennemi lancé, mur ;
       pas par une zone ni par l'onde seule. **Zone de touche** : ailes en déclencheurs `Hitbox` (les balles touchent désormais les
       déclencheurs qui ont un `Hitbox`, et eux seuls). Mesuré : 200/200 points du modèle, 0/120 à côté ; une balle le tue à 10, 30
       et 60 m (bunny hop 60/60).
     - **Piqué à visée corrigée** : 80°/s (90 demandé : 38 % en course), sans avance (`diveLead` 0 ; 0.3 : 68 %) ; course 32/100,
       bunny hop 0/50, traction 50/50 ; dash tard 0/20 touché, dash pendant l'annonce ou tôt 20/20 touché.
     - **Dash** : recharge d'une charge +1 s (sol 1 → 2 s, air 3 → 4 s, D-015 mise à jour), saut en l'air ralenti aussi.
     - **Main plantée** (D-039) : piste (b) (corps à vitesse normale, rotation ×0.4) insuffisante (40 m traction 20/20), puis piste
       (a) : **revers de la main gauche** (7 m du bord du corps, délai 0.5 s, annonce 0.2 s, coup 0.25 s). Mesuré : 80 m 20/20 en
       bunny hop et en traction, 40 m 0/20, couloir 0/60.
     - 286 tests (1 non concluant à dessein).
   - **Ajustements du 2026-11-13 (sans commit, playtest en attente)** : volant repoussé, **tout choc contre un mur ou le sol pendant
     1.5 s le tue** (`pushedContactKillTime`), mesuré 20/20 contre un mur et 20/20 contre le sol (les 2 échecs sur 5 venaient du
     script : répulsion en recharge) ; piqué en traction : 50/50 touché sans réaction, **0/50 avec un dash tard**.
7. **Vagues** (D-041) — fait et **validé en playtest** le 2026-11-13 (volant, dash, main plantée, piqué, vagues : difficulté
   gardée pour les tests) : tendance en part du timer
   (goules 10 → 60, 80 en Enragé ; volants 2 → 16, 20), pics de 20 s annoncés (cor, « VAGUE n ») et accalmies de 15 s à 60 %,
   apparitions 3 → 1 s (0.5 s en Enragé) sur 108 points (`SpawnPoint`, posables à la main), hors de vue, 25 à 80 m, lueur 0.6 s ;
   tués reconstitués 1 toutes les 4 s ; plafonds de chasse selon la tendance ; réserve de 80 goules et 20 volants, plus aucun ennemi
   posé à la main. Mesuré : points 108/108 valides, aucune apparition vue ni sous 25 m, coût au pic **0.78 ms** par pas fixe
   (80 goules, 20 volants). 294 tests (1 non concluant à dessein). Checklist : `docs/design/features/vagues/testing.md`.
   **Étape 7 (âmes) : multiplicateur ×2 en Enragé (D-009).**
   - **Après ce playtest (D-041 mise à jour)** : **accalmie** : plafond de chasse (sans l'aggro) à 60 %, les ennemis en trop (les
     plus loin) repassent en attente, reculent d'environ 10 m et ne poursuivent plus, sauf si le joueur repasse dans leur zone
     d'aggro ; au pic suivant, ils reprennent (mesuré : 15 → 12 en chasse, 22 au pic suivant, personne ne disparaît). **Coop** :
     × (1 + 0.5 par joueur vivant en plus), une part par joueur, apparitions à tour de rôle, à 25 m au moins de chacun (mesuré :
     25.0 m au plus près à 2 joueurs ensemble, 27.2 m séparés de 150 m ; 4 joueurs : 9 à 11 apparus pour chacun). **Choix de
     cible** (goule, volant) : il garde sa cible ; change si l'autre est plus près de 15 m ou si la cible est à plus de 60 m, 3 s
     au moins après le dernier changement ; immédiat si la cible meurt, ou si un autre joueur le blesse ou le lance (mesuré :
     1 changement en 60 s à égale distance, dû à un joueur repoussé, 0 avec un joueur qui tourne autour). 299 tests (1 non
     concluant à dessein). **À revoir au prochain playtest** : accalmie, choix de cible (F12).

**Notes pour plus tard (ne pas coder maintenant) :**
- **Étape 8, marchand :** un marchand en plus dans la zone de départ sûre des zones 2 et 3 (D-043).
- **Étape 8 (D-042 proposée, ne pas coder avant) :**
  - zone de départ sûre : aucun ennemi tant que les joueurs n'en sont pas sortis ;
  - le timer ne démarre que quand le premier joueur sort de la zone de départ ;
  - timer d'une vraie zone : 15 min au lieu de 20 (D-003 et D-010 mises à jour) ;
  - le traqueur sort de son repaire au bout de 3 min (au lieu de 60 s de grâce) ;
  - la vraie carte sera plus grande que Test_Arena ;
  - en test, Test_Arena garde ses réglages actuels (4 min, grâce de 15 s).
- **Level design :** Test_Arena reste un banc d'essai ; points d'apparition à poser à la main (trous, grilles, nids). Réserve
  des vagues (80 / 20) trop petite pour la coop à 4 (×2.5) : à agrandir à l'étape 9 ou 11.

**Note pour plus tard (lustre) :** le lustre doit pouvoir tomber **à distance** — traction sèche, objet
lancé dessus, ou chaîne visée — pour s'en servir contre les ennemis et contre le boss final (combat
final par l'environnement, D-008). Aujourd'hui, il ne se décroche qu'en l'attrapant.

L'étape 3 (orbe de magnétisme) est **terminée et validée en playtest** (2026-11-12).

## Feuille de route

Ordre de CLAUDE.md, une étape validée en playtest avant la suivante :
3. Magnétisme (fait) → 4. Télékinésie, une seule orbe, dégâts (fait) → 5. Vue FPS et HUD (fait) →
**5 bis. Artefacts** (fait, D-032) → **6. Ennemis et vagues, avec le traqueur** (fait, D-033 à D-041) → **7. Âmes et
économie** (fait, D-043) →
**8. Run complète** (faite pour le niveau 1, fusionnée le 2026-11-14) → **9. Écran partagé** (fait, fusionné le 2026-11-14) → 10. Direction artistique (dont les **vrais sons**, voir
ci-dessous) → 11. Coop en ligne → 12. Build.

**Piste parallèle : direction artistique (étape 10)**, menée par une deuxième personne depuis le 2026-11-12 :
branche `art` du même dépôt, uniquement `Assets/Art/` et `docs/art/` (plus sa ligne du manifeste), ses propres
scènes, fusion par pull request (règles dans CLAUDE.md, « Piste DA en parallèle »). Pipeline : Higgsfield (image
concept → Tripo P2 ou Tripo H3.1 multivues → Meshy pour le squelette et l'animation) → Blender → Unity ; droits
commerciaux vérifiés et notés au manifeste. Mesures, budgets, style et livraison : `docs/art/ART-HANDOFF.md` (à
tenir à jour quand un réglage qui y figure change). Première livraison attendue : le guide de style.

**Sons provisoires :** les sons actuels sont synthétisés par le code (`ProceduralSfx`). Ils seront
remplacés par de vrais sons à l'étape 10 (skill `audio-design`), avec la même liste d'événements :
début de traction, bourdonnement de traction (hauteur selon la vitesse), fin de traction,
répulsion, refus, charge revenue / répulsion prête.

## Fait

- 2026-11-12 — **Orbe de magnétisme (étape 3), terminée et validée en playtest.**
  - Partie 1, cœur (v3) : traction linéaire continue (clic gauche / RT maintenu, 60 m, 80 m/s² jusqu'à
    30 m/s, 2.5 s max) sur 3 charges d'attraction (1 toutes les 3 s), répulsion instantanée (clic droit
    / LT) à charge unique (40 m, 5 s), toute surface, aucune charge de mouvement, aucune perte d'élan,
    traction hors plafond commun ; HUD des charges ; zone Magnétisme (F10) et grande salle de 45 m (F11).
  - Partie 2, retour visuel et sonore : bloom à seuil 1, lueur de l'orbe, du trait, de l'ancre et des
    éclats ; viseur à deux anneaux ; refus en rouge dans le HUD ; sons synthétisés provisoires ;
    réglages dans `MagnetismFeedback_Default`.
  - 155 tests ; mesures en Play Mode ; captures de la vue Game. Détail : `docs/design/features/orbes/`.
  - Trois itérations du cœur : impulsion sur surfaces magnétiques (D-020) → traction continue sur toute
    surface, sans charges de mouvement (D-021) → 3 charges d'attraction et répulsion unique (D-021 mise
    à jour). Artefacts documentés (D-022).

- 2026-11-11 — Vérification de l'environnement (étape 0) :
  - Structure OK : Assets/, Packages/, ProjectSettings/ ; kit à la racine (29 skills,
    settings.json, .mcp.json, docs/, LICENSES/) ; aucun .meta orphelin du kit.
  - Disque C: = SSD NVMe (WD Blue SN570).
  - Unity mis à jour de 6000.3.1f1 vers **6000.3.28f1** (correctif sécurité Brotli
    CVE-2026-111497, corrigé à partir de 6000.3.27f2). Ouverture sans erreur.
  - Packages : URP 17.3.0 (actif), Input System 1.20.1 (seul actif), **Cinemachine 3.1.7**
    (ajouté), Test Framework 1.6.0, Pipeline 1.0.0, AI Navigation 2.0.16. Compilation OK.
  - Visible Meta Files + Force Text. CLI Unity 1.0.0-beta.13 (beta.17 disponible).
  - Git 2.56 + LFS 3.8.1 ; dépôt sur `main`, LFS activé, .gitignore et .gitattributes ;
    remote `origin` = https://github.com/SergioMiGuPr/ItHunts.git ; premier commit
    « Projet Unity + kit Claude Code ».

- 2026-11-11 — Brief (étape 1) : 16 questions ouvertes répondues, `docs/design/BRIEF.md` mis à
  jour ; `docs/design/GDD.md` écrit, avec les notes de conception mouvement, orbes et boss
  traqueur dans `docs/design/features/` (vérificateur game-docs : 0 erreur). GDD 0.2 validé
  par l'utilisateur (D-008 à D-011).

- 2026-11-11 → 2026-11-12 — **Mouvement de base (étape 2), terminé.** Détail :
  `docs/design/features/mouvement/` (design, implementation, testing).
  - Simulation pure à 60 Hz (`MovementMotor`), CharacterController, `PlayerInput` (clavier-souris
    et manette), caméra Cinemachine 3 avec FOV selon la vitesse, réglages dans `MovementSettings`
    et `CameraSettings`, debug F3 / Select, HUD des charges sous le viseur, téléportation de debug
    F4 (départ) et F5-F9 (zones).
  - Course, saut (coyote, buffer, hauteur variable), contrôle aérien façon CPMA limité à 100°,
    auto-bunny hop, 2 sauts (D-012, D-014).
  - Glissade qui conserve l'élan, accroupi, passage bas (D-013).
  - Wall slide sans perte (détection latérale, effet wall run) et wall jump façon Deadlock (D-016).
  - Charges de mouvement partagées (saut en l'air, dash, puis magnétisme) et dash progressif avec
    dash-jump (D-015, D-017, D-018) ; plafond commun des gains à 20 m/s, bonus de glissade inclus
    (D-019).
  - Scène `Test_Movement` de 400 × 400 m (zone ouverte, ligne droite de 380 m, pentes, grands murs,
    cheminée, écarts, verticalité), générée par *ItHunts > Reconstruire…*.
  - 109 tests EditMode ; mesures en Play Mode avec le vrai CharacterController à chaque correction.

## Décisions

Le détail est dans `docs/design/GDD.md` §Decisions (D-001 à D-023).

- Moteur : Unity 6.3 LTS (6000.3.28f1), URP.
- Vue première personne, deux orbes (magnétisme, télékinésie), roguelite.
- 4 zones (cave, château, forêt, finale) ; 20 min de farm par zone avec un timer, sortie
  anticipée possible.
- Boss traqueur unique : rôde, tue en un coup, intuable avant la zone finale, plus rapide que la
  course.
- Mouvement façon Deadlock : la vitesse se gagne et se garde ; la limite vient des sauts et des
  charges ; un seul plafond pour tous les gains (20 m/s).
- Dash et saut en l'air partagent les charges de mouvement ; le magnétisme a ses propres charges
  (3 charges d'attraction, répulsion unique, D-021) ; la télékinésie n'a pas d'endurance (D-024).
- **Une seule orbe** (D-024) : traction, répulsion, attraper / relâcher et lancer en même temps, chacun sur
  sa touche ; couleur de l'orbe selon l'action.
- Artefacts : 1 par joueur et par run, tiré au hasard, sans doublon en coop, en main gauche ;
  premier : le barillet (D-022).
- Architecture prête pour l'écran partagé et la coop en ligne (voir CLAUDE.md).

## Valeurs de réglage validées en playtest

Étape 6 validée le 2026-11-13 (assets `Ghoul_Default`, `Flyer_Default`, `Stalker_Default`, `Waves_Default`, `Zone_Test`) :

- **Goule :** 10 m/s, bond annoncé 0.3 s, contact 15 PV/s, 25 en chasse (40 avec l'aggro : 20 m en vue, 8 m), murs grimpables.
- **Volant :** 25 PV, tué au premier impact de projectile, choc mortel 1.5 s après une répulsion, ronde 8 × 8 m, piqué annoncé
  0.4 s à 14 m/s (8 PV, virage 80°/s, sans avance), attrapable à tout moment (libéré en 2 s), éclatement 3 m (30 → 7.5).
- **Traqueur :** prise à la main (15 m, annonce 0.5 s, recharge 2.5 s), mains levées, Enragé qui accélère (11 → 18 m/s en 30 s),
  main plantée 1.5 s (rotation ×0.4, corps à vitesse normale, revers de 7 m après 0.5 s, annonce 0.2 s).
- **Charges de mouvement :** recharge 2 s au sol, 4 s en l'air.
- **Vagues :** goules 10 / 20 / 35 / 50 / 60 (Enragé 80), volants 2 / 4 / 8 / 12 / 16 (Enragé 20), pics de 20 s, accalmies de
  15 s à 60 %, 3 → 1 s (0.5 s en Enragé), 25 à 80 m, annonce 0.6 s, reconstitution 1 toutes les 4 s.

Mouvement de base validé le 2026-11-12 avec les valeurs de `Assets/_Project/Settings/Movement_Default.asset`
et `Camera_Default.asset` (la note de conception en fait foi) :

- **Sol :** course 8.5 m/s (D-072), accélération 10, friction 6, vitesse d'arrêt 2.5 m/s.
- **Saut :** 1.3 m, gravité 20 m/s², chute ×1.5, coupe 0.5, coyote 0.10 s, buffer 0.12 s (« un
  peu flottant », accepté). Saut en l'air 1.6 m ; 2 sauts ; auto-bunny hop activé.
- **Air :** accélération 10, plafond 1 m/s, contrôle aérien 0.15 (jusqu'à 100°).
- **Glissade :** dès 6 m/s, bonus +2 m/s (recharge 0.8 s), décélération 3 m/s², 5 m/s minimum sous
  un obstacle, sortie sous 4 m/s ; accroupi 3.5 m/s, capsule 0.9 m.
- **Murs :** wall slide (chute 3 m/s, 0.5 m/s en wall run de 8 à 15 m/s, tolérance 75°, dès 3 m/s
  le long du mur) ; wall jump 1.8 m, impulsion 7.5 m/s, direction 0.5, bonus +1 m/s, wall-coyote
  0.1 s, verrou 0.15 s ; le mur recharge les sauts, pas les charges.
- **Charges et dash :** 3 charges, recharge 1 s au sol et 3 s en l'air (**2 s et 4 s depuis le 2026-11-13**, D-015 mise à jour,
  à revoir au playtest) ; dash 6.5 m en 0.3 s
  (profil progressif), sortie ≥ 12 m/s, +1 m/s au sol, aucun gain en l'air, dash-jump +3 m/s
  (fenêtre 0.15 s), délai 0.3 s, mémorisation 0.3 s, 0.2 s sans friction après.
- **Plafonds :** gains 20 m/s ; sécurité 50 m/s. Pic mesuré en bunny hop : 26 m/s.
- **Caméra :** FOV 70° (+12° de 8 à 20 m/s), souris 0.1°/point, manette 270 / 180 °/s (courbe 2).

Magnétisme validé le 2026-11-12 (v3) avec `Assets/_Project/Settings/Magnetism_Default.asset` :

- **Traction (attraction) :** portée 60 m, 80 m/s² jusqu'à 30 m/s (**18 m/s depuis D-036**), 2.5 s max, arrivée à 1 m ;
  3 charges d'attraction, 1 toutes les 3 s (passé à **4 s** par D-024, à valider en playtest) ; jamais
  de perte, hors plafond commun.
- **Répulsion :** 14 m/s, portée 40 m, charge unique rechargée en 5 s ; gain dans le plafond commun.
- **Commun :** toute surface ; aucune charge de mouvement ; mémorisation d'un appui 0.1 s.

Télékinésie et orbe unique validées le 2026-11-12 (D-024 à D-027), `Telekinesis_Default.asset`,
`Magnetism_Default.asset`, `Damage_Default.asset`, `TrainingDummy_Default.asset` :

- **Visée du lancer :** toujours vers le point sous le viseur au clic (rayon depuis les yeux au début du tick) ;
  vitesse de lancer 30 m/s, plus la part de la vitesse du joueur dans la direction du lancer (jamais négative).
  Mesuré : écart latéral au plus 0.11 m à 10, 20 et 30 m, à l'arrêt, de côté, en bunny hop et en traction.
- **Tir tendu :** sans gravité pendant 0.5 s (≈ 15 m) ou jusqu'au premier choc ; gravité des objets 20 m/s².
- **Onde de répulsion :** rayon 4 m ; 16 m/s pour un objet de 15 kg ou un ennemi de 80 kg, facteur de masse de 0.35
  à 1.3, 50 % au bord, montée 0.25 ; un objet accroché est décroché et tombe droit.
- **Étourdissement contre un mur :** à partir de 6 m/s d'impact, 1.5 s ; mesuré : 10.3 et 12.3 m/s → étourdi.
- Aussi en place (non revus un à un en playtest) : aide à la visée de la prise (8° → 3° à 60 m), dégâts d'impact
  (masse × (vitesse − 8) × 0.1, seuil de chute 3 m/s), zone (1 m + 0.06 m/kg, 60 % / 25 %), durabilité
  (1 / 2 / 3 / 5 lancers), lustre fragile (zone 4 m).

## Problèmes connus / à faire

- **Équilibrage ouvert après l'étape 6 (difficulté actuelle gardée pour les tests, 2026-11-13) :**
  - piqué du volant : 100 % de touches sur un joueur en traction sans réaction, 0 % en bunny hop ;
  - survie mesurée (traqueur coupé) : en course, mort en 6 à 28 s ; en bunny hop, souvent vivant après 60 s ;
  - population de départ faible (peu de points cachés à 25–80 m du départ) : à revoir avec la zone de départ (D-042) ;
  - coop à 4 : nombre voulu au-delà de la réserve ;
  - revers du traqueur (7 m) : salle de 40 m fermée même en bunny hop.

- **Équilibrage ouvert, à traiter quand les goules et les volants auront leurs PV (pas avant) :**
  - dégâts d'un ennemi lancé : 176 pour 80 kg à 30 m/s (un factice de 100 PV meurt, sa cible aussi) ;
  - plafond du bonus de vitesse au lancer (20 m/s) ;
  - force de la répulsion (22 m/s, 35 m/s², 14 m) ;
  - barillet (12 dégâts, 0.25 s, 1.2 s) face aux goules et aux volants.
- **Validés au playtest du 2026-11-12 :** dégâts contre un mur, objets repoussés, rendu des mains, occlusion ambiante,
  barillet (recharge, prise pendant une recharge, précision).
- **Tir ami :** réglage `Coop_Default.friendlyFire` (désactivé) en place (D-032) ; les PV du joueur sont provisoires
  (réapparition au départ, la vraie mort vient à l'étape 8). À tester pour de vrai à l'étape 9 (écran partagé).
- **Coop, traqueur :** il vise le joueur le plus proche par le chemin, au plus un changement toutes les 5 s (D-035 ; prêt,
  testé en solo seulement). Mort : la zone
  se remet à zéro quand tous les joueurs sont morts (réanimation : étape 9 / 11).
- **Traqueur, prise à la main :** visée corrigée 0.15 s avant la descente (D-035) ; encore trop facile ? À juger au playtest.
- **Traction (D-036) :** à 18 m/s, la durée max de 2.5 s limite une traction à ≈ 43 m (55 m avant) ; durée gardée par
  l'utilisateur (2026-11-12), recharge d'une charge passée à 6 s.
- **Goules (D-035) :** bond d'en haut esquivable seulement pendant le vol ; salles hautes atteintes par les pentes (pas
  de mur direct du sol vers +16 m : ajoutés par D-037) ; 76 goules, 25 en chasse au plus (D-037).
- **Réserve des vagues :** les ennemis inactifs de la réserve n'ont jamais eu d'`Awake` ; `ZoneState.ResetZone` les ranime
  quand même (`EnemyBody.ResetToHome` s'initialise maintenant s'il le faut). Dans les mesures, un joueur « immortel » par
  `hp.Restore()` à chaque image meurt quand même sous 70 goules : monter ses PV max le temps de la mesure.
- **Unity CLI :** `editor.playmode start` peut répondre sans démarrer le Play Mode ; un script lancé alors tourne en mode édition
  et modifie la scène (la reconstruire). Toujours attendre « playing True » avec `ping.cs`. Une recompilation peut aussi se faire
  au début d'un Play Mode (« recompiler et continuer ») : relancer le Play Mode après.
- **Mesures du volant :** dans une salle de mesure temporaire (140 × 140 m, loin du labyrinthe) ; les coins de la salle SE supposés
  au départ étaient hors du sol (le joueur tombait). Un script qui désactive des objets (traqueur, volants) les rend introuvables pour
  le script suivant : chercher avec `FindObjectsInactive.Include` ou relancer le Play Mode.
- **Mesures dans le labyrinthe :** un traqueur qui démarre au milieu d'une intersection prend son dos comme couloir
  d'arrivée (corrigé) ; la traction a une portée de 60 m (viser plus près dans les scripts).
- **`ProjectSettings/NavMeshAreas.asset` :** chaque Play de Test_Arena avance `m_LastAgentTypeID` (types d'agent créés
  à l'exécution par `ZoneNavMesh`) ; sans effet : ne pas commiter ce seul changement (le rétablir avec `git checkout`).
- **Unity (étape 6) :** les types d'agent de NavMesh créés à l'exécution ont des identifiants **négatifs** (ne pas
  tester `< 0`) ; un `NavMeshPath` ne se crée pas dans un initialiseur de champ d'un MonoBehaviour ; dans un
  constructeur de scène, `EditorSceneManager.NewScene` décharge les assets chargés avant (les recharger après) ; dans
  les scripts de mesure, viser après une téléportation seulement une image plus tard (la caméra suit en LateUpdate).
- **Étape 8, point ouvert : PV du joueur, mort et réapparition.** Aujourd'hui : 100 PV provisoires (validés pour les
  tests), réapparition au point de départ après 1 s (`PlayerHealth_Default`). À décider à l'étape 8 : PV définitifs,
  soins rares, écran de bilan et relance (BRIEF), réanimation en coop. Le test du traqueur qui tue en un coup est prêt
  (`StalkerTests`, ignoré jusqu'à l'étape 6).
- **Coop en ligne (étape 11) :** la détection maison du contact objet-joueur (`ImpactDamager.FixedUpdate`) et le tir
  ami devront être calculés côté autorité réseau.
- **Constructeur de la scène :** il ne touche plus aux renderers des packages (il modifiait celui d'URP, en lecture
  seule) et réutilise les passes des mains existantes (sinon chaque reconstruction changeait les identifiants dans
  `PC_Renderer` et `Mobile_Renderer`, partagés avec la branche `art`).
- **Unity :** un `GlobalKeyword.Create` dans un initialiseur statique d'une passe de rendu fait planter l'éditeur au
  rechargement du code (« not allowed during serialization ») ; le créer dans `Create()`. Après ce plantage, URP
  restait cassé (« Blitter is already initialized ») jusqu'à la correction du code et un redémarrage.
- **Tests :** un `CharacterController` créé dans une scène de prévisualisation reste dans la physique de la scène
  principale (les rayons du test ne le touchent pas) ; ajouter une capsule ordinaire au joueur simulé.
- **Mesures à 2 joueurs :** le `PlayerInput` d'un 2e joueur instancié à la main lit la même souris : couper son
  `PlayerInputReader` pendant une mesure.

- **Menu d'options** : l'intensité du mouvement des mains n'a pas encore d'écran (inspecteur du joueur, PlayerPrefs).
- Mesures en Play Mode par la CLI : une boucle d'attente trop courte laisse un script tourner ; attendre le `#fin`
  du journal avant d'arrêter le Play Mode. Un 2e joueur temporaire : couper son `AudioListener` (sinon avertissement
  à chaque image) ; sa caméra virtuelle n'est pas trouvée par `GetComponentInChildren` (P2 sans suivi, assez pour
  mesurer).

- **Point ouvert, à rééquilibrer après des runs complètes (pas avant) :** plafond du bonus de vitesse au lancer
  (20 m/s par défaut, `TelekinesisOrbSettings.throwSpeedBonusCap`, ajouté à l'étape 5).
- **À revérifier au prochain playtest :**
  - gravité des objets à 20 m/s² (en place, D-027) ;
  - exclusion du traqueur de la répulsion, de l'étourdissement, des dégâts de zone, des balles et de la prise : **le
    traqueur n'existe pas encore** (étape 6) ; tous ces systèmes respectent déjà `Damageable.ImmuneToOrbs`
    (`RepulsionWave`, `EnemyBody`, `ImpactDamager.ApplyArea`, `BarrelShooter`, prise) : le traqueur devra l'avoir ;
  - immunité de 2 s après un étourdissement : faite à l'étape 5 (`TrainingDummySettings.stunImmunity`).

- **Gravité :** objets à 20 m/s² comme le joueur (D-027). Unity n'enregistre pas `Physics.gravity` changée par
  la CLI : modifier `ProjectSettings/DynamicsManager.asset` et la valeur en mémoire.
- **Proposé :** tir tendu de 1 s au lieu de 0.5 s (avec la gravité à 20, l'objet retombe de 1.8 m à 30 m).
- **Objet tenu qui percute un ennemi** : il blesse (« tout impact »), effet bélier en traction : à juger en playtest.
- **Mesures en Play Mode :** choisir les points de départ sur le sol de la scène (au sud de la zone Télékinésie,
  au-delà de z = −40, il n'y a plus de sol). Les messages de collision (`OnCollisionEnter`) ne partent pas en mode
  édition : les dégâts réels se vérifient en Play Mode.
- **Ne pas recompiler pendant le Play Mode** d'une mesure (la recompilation attend et la CLI expire) : arrêter le
  Play Mode d'abord (`unity command editor.playmode stop`).

- **Écran partagé (étape 9) :** la lueur des objets attrapables est maintenant calculée par caméra (réglé le 2026-11-14).
- **Réseau (étape 11) :** les objets lancés sont des corps dynamiques locaux ; le serveur devra faire
  autorité dessus (voir CLAUDE.md).

- **Sons provisoires** (magnétisme) : synthétisés par le code, à remplacer à l'étape 10 (skill
  `audio-design`) en gardant les mêmes événements (début de traction, bourdonnement de traction (hauteur selon la vitesse), fin de traction, répulsion, refus, charge revenue / répulsion prête).

- **À traiter avec le traqueur :** un bon joueur va presque 3 × plus vite que lui (26 m/s contre
  9-11 m/s) ; pistes notées dans `docs/design/features/boss-traqueur/design.md`.
- Le HUD (IMGUI, provisoire) est vérifiable par `unity command capture game_view` (capture PNG de la vue
  Game) ; le vrai HUD viendra à l'étape 5.
- Serveur MCP Blender : échec de connexion. Pas bloquant avant l'étape DA (10).
- Editor.log : deux « Pipe is broken » du runner ILPP à la réouverture, sans effet
  (bruit connu lors des recompilations).
- Le vérificateur `check_unity_project.py` signale 4 GUID introuvables dans
  `Assets/Settings/DefaultVolumeProfile.asset` (modèle URP) : faux positif, Unity charge les
  19 composants sans manque.
- Touches clavier de l'Input System = positions physiques QWERTY : `<Keyboard>/q` est la touche marquée A
  sur l'AZERTY de l'utilisateur, `<Keyboard>/w` son Z (déplacements ZQSD).
- Tests EditMode : pas de scène additive (« untitled scene unsaved ») ; utiliser une scène de prévisualisation
  (`EditorSceneManager.NewPreviewScene`) et `PhysicsAimProbe.Scene`. Commandes CLI : `unity command test run
  --mode EditMode`, `unity command editor.playmode start` / `stop`, `unity command script eval --file`.
- En Play Mode dans l'éditeur, le clavier n'est lu que si la fenêtre Game a le focus (réglage
  Input System par défaut) : le clavier simulé par la CLI ne passe pas. Les vérifications en Play
  Mode passent donc par la simulation avancée tick par tick avec des entrées injectées.
- Mesures en Play Mode par la CLI : tous les ticks s'enchaînent dans une seule image ; il faut un
  `Physics.Simulate` entre deux ticks (mode de simulation `Script` le temps de la mesure), sinon la
  capsule retombe sur une copie figée d'elle-même à son point de départ.
  **Toujours remettre `Physics.simulationMode = FixedUpdate` dans un `finally`** : un script de mesure
  qui a planté l'avait laissé en `Script`, et les objets de la télékinésie ne bougeaient plus pendant
  le playtest (2026-11-12). `PlayerMotor` avertit maintenant au lancement si ce n'est pas `FixedUpdate`.
- Vraies entrées en Play Mode par la CLI : `InputSystem.settings.backgroundBehavior = IgnoreFocus` et
  `editorInputBehaviorInPlayMode = AllDeviceInputAlwaysGoesToGameView`, puis `InputSystem.QueueStateEvent`
  (souris, clavier), appui et relâchement sur deux images ; journal par coroutine lu dans un second eval.
- Pas fixe passé à 60 Hz (`ProjectSettings/TimeManager.asset`).
- Unity est réglé sur « recompiler et continuer à jouer » : une recompilation pendant le Play
  Mode provoque des NullReferenceException (état du joueur perdu). Proposé : « recompiler à la
  fin du Play Mode » (Preferences > General > Script Changes While Playing). Claude attend la fin
  du Play Mode avant de compiler.
