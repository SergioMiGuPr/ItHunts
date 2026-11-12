# Orbes : implementation

**Updated:** 2026-11-12 · [Design](design.md) · [Testing](testing.md)

## Approach

Magnétisme, partie 1 (cœur, v2 après playtest, D-021) et orbe visible simple construits ; le retour
visuel et sonore complet est la partie 2. Les chiffres vivent dans [design.md](design.md).

- **Ordre d'un tick :** `MagnetismOrb.Step` **avant** `MovementMotor.Step`, même entrée. L'orbe démarre
  ou arrête la traction et applique la répulsion tout de suite (pas d'un tick de retard).
- **Traction :** état `MovementMode.Traction` du moteur. Le point, l'accélération et la vitesse de
  traction sont copiés dans `MovementState` au départ (`TractionPoint`, `TractionAcceleration`,
  `TractionSpeed`). Dans cet état, le moteur saute sol, murs, sauts, gravité, friction et chute max ;
  la vitesse vient de `MovementMath.TractionVelocity`. Le moteur arrête lui-même la traction sur un dash
  ou un obstacle (moins de la moitié du déplacement voulu) ; l'orbe le voit au tick suivant et démarre
  sa recharge. L'orbe l'arrête sur relâchement, arrivée, ligne de vue coupée, durée max, saut ou
  répulsion (`TractionEnd`).
- **Charges :** `MagnetismState.AttractCharges` (fractionnaires, rechargées à chaque tick de
  `dt / attractChargeRechargeTime`, plafonnées à `attractCharges`) ; la répulsion a une charge unique :
  prête quand `repelCooldown` s'est écoulé depuis `LastRepelTick`.
- **Répulsion :** `MovementMath.RepelVelocity` (chute annulée si l'impulsion monte, vitesse horizontale
  jamais en baisse, gain par `CappedGain`).
- **Dash :** une traction ou une répulsion pendant un dash appelle `MovementMotor.EndDash` (vitesse de
  sortie), puis s'applique.
- **État :** `MagnetismState` (structure copiable) : états par mode, recharges, appuis consommés,
  visée du tick, dernière traction et dernière répulsion, dernier refus.
- **Entrées (orbe unique, D-024) :** `MovementInput.Pitch`, `AttractPressedTick`, `AttractHeld`,
  `RepelPressedTick`, `GrabPressedTick`, `ThrowPressedTick` (`PlayerInputReader`, actions `Attract`
  (`<Keyboard>/q` = A en AZERTY, RB), `Repel` (clic droit, LT), `Grab` (F, X), `Throw` (clic gauche, RT)).
  Les touches physiques de l'Input System suivent la disposition QWERTY : `q` est la touche marquée A sur un
  AZERTY. Le magnétisme et la télékinésie reçoivent la même entrée chaque tick, chacun lit ses champs.
- **Visée :** `IAimProbe` ; en jeu `PhysicsAimProbe` (Raycast dans `Scene`, la scène physique par défaut ;
  ignore les triggers, le joueur lui-même et `IgnoreBody`, l'objet tenu). Toute surface répond.
- **Couleur de l'orbe :** `OrbLook.Resolve` (pure, testée) choisit l'action affichée : lancer > répulsion
  (éclats de `orbFlashDuration`) > traction > objet tenu > recharge > prête ; `MagnetismOrbView` colore la
  sphère (couleurs du magnétisme et de la télékinésie).
- **Vue :** `MagnetismOrbView` sur la racine du joueur : sphère sous la caméra, en bas à droite
  (matériau URP Unlit, couleur par `MaterialPropertyBlock`), `LineRenderer` vers le point pendant la
  traction, et deux disques plats pour la répulsion : la marque de visée (`MagnetismState.RepelInRange`,
  `AimPoint`, `AimNormal`) et la marque d'impact (`LastRepelPoint`, `LastRepelNormal`), placés dans le
  monde à chaque image.
- **HUD :** `MovementHud` (IMGUI) : 2e rangée sous les charges de mouvement, charges d'attraction en bleu
  et répulsion en orange avec le temps restant (vérifié par capture de la vue Game).
- **Debug :** `MovementDebugOverlay` (F3) : états et recharges, visée, traction en cours, dernière
  traction (vitesses de départ, max et fin, durée, raison), dernière répulsion, refus. F10 zone
  Magnétisme, F11 grande salle.

**Réseau :** l'orbe et la traction sont des fonctions pures des entrées et de l'état ; seul le rayon
dépend de la scène.

## Files

| File | Role |
|---|---|
| `Assets/_Project/Scripts/Runtime/Orbs/MagnetismOrb.cs` | simulation de l'orbe (départ et fin de traction, répulsion) |
| `Assets/_Project/Scripts/Runtime/Orbs/MagnetismState.cs` | état, statuts, raisons de refus |
| `Assets/_Project/Scripts/Runtime/Orbs/MagnetismOrbView.cs` | orbe visible et trait de traction |
| `Assets/_Project/Scripts/Runtime/Orbs/IAimProbe.cs`, `PhysicsAimProbe.cs` | rayon de visée |
| `Assets/_Project/Scripts/Runtime/Movement/MovementMotor.cs` | état Traction |
| `Assets/_Project/Scripts/Runtime/Settings/MagnetismOrbSettings.cs` | réglages (`Assets/_Project/Settings/Magnetism_Default.asset`) |
| `Assets/_Project/Tests/EditMode/MagnetismTests.cs` | 32 tests (sol et visée factices) |
| `Assets/_Project/Scripts/Runtime/Hud/MovementHud.cs` | charges de mouvement, d'attraction et répulsion ; anneaux du viseur ; refus |
| `Assets/_Project/Scripts/Runtime/Orbs/MagnetismFeedback.cs` | règles du viseur et du clignotement |
| `Assets/_Project/Scripts/Runtime/Audio/ProceduralSfx.cs`, `MagnetismAudio.cs` | sons synthétisés et leur déclenchement |
| `Assets/_Project/Scripts/Runtime/Settings/MagnetismFeedbackSettings.cs` | réglages du retour |
| `Assets/_Project/Tests/EditMode/MagnetismFeedbackTests.cs` | 14 tests (viseur, refus, sons, bloom, prefab) |
| `Assets/_Project/Scripts/Runtime/Orbs/OrbLook.cs` | couleur de l'orbe unique selon l'action (D-024) |
| `Assets/_Project/Scripts/Runtime/Orbs/TelekinesisOrb.cs`, `TelekinesisState.cs` | logique de la télékinésie |
| `Assets/_Project/Scripts/Runtime/Orbs/TelekinesisHolder.cs` | côté physique, rayon de visée |
| `Assets/_Project/Scripts/Runtime/Orbs/Grabbable.cs`, `TelekinesisView.cs` | objets attrapables, retour visuel |
| `Assets/_Project/Scripts/Runtime/Settings/TelekinesisOrbSettings.cs`, `TelekinesisFeedbackSettings.cs` | réglages |
| `Assets/_Project/Tests/EditMode/TelekinesisTests.cs` | 19 tests (actions simultanées, attraper, relâcher, lancer, recharge, sans endurance, couleur de l'orbe, commandes, réglages) |
| `Assets/_Project/Tests/EditMode/TelekinesisPhysicsTests.cs` | 7 tests avec de vrais corps dans une scène isolée (l'objet vient au point de maintien ; il y reste en traction à 30 et 40 m/s et en bunny hop ; la visée ignore l'objet tenu) |

## Build steps

1. **Magnétisme, cœur** — fait (2026-11-12) ; v2 après playtest (D-021) : traction continue, toute
   surface, sans charges de mouvement, aucune perte d'élan, orbe visible simple ; v3 : charges
   d'attraction, répulsion unique, marquage et HUD ; validé en playtest.
2. **Magnétisme, retour visuel et sonore** — fait et validé (2026-11-12) ; sons provisoires, remplacés à
   l'étape direction artistique (mêmes événements) :
   - réglages `MagnetismFeedbackSettings` (`Assets/_Project/Settings/MagnetismFeedback_Default.asset`) ;
   - lueur : post-process activé sur la caméra du joueur, Volume global avec bloom à seuil 1 dans
     `Test_Movement` (`Assets/_Project/Settings/PostProcess_TestMovement.asset`) ; couleurs HDR ;
   - `MagnetismOrbView` : ancre et éclat de traction, éclat de répulsion, marques à taille constante à
     l'écran au-delà de 8 m ;
   - `MovementHud` : anneaux du viseur (textures générées), clignotement rouge du groupe refusé ;
     règles dans `MagnetismFeedback` (testables) ;
   - `ProceduralSfx` (sons synthétisés) et `MagnetismAudio` (détection des changements d'état à chaque
     tick, bourdonnement en boucle dont `pitch` suit la vitesse).
3. **Télékinésie et échange des orbes, partie 1** — fait (2026-11-12, D-023) ; l'échange et l'endurance ont été
   retirés après le playtest (étape 3 bis) :
   - `OrbSelection` (état copiable `OrbSelectionState`) : bascule et choix direct ; la seule orbe active
     reçoit les boutons (`OrbSelection.WithoutOrbButtons`) ; l'échange arrête la traction
     (`MagnetismOrb.StopTraction`, `TractionEnd.Echange`) et lâche l'objet tenu ;
   - `TelekinesisOrb` (logique pure, état `TelekinesisState`) : endurance, coûts, recharge après un lancer,
     refus ; renvoie une commande (attraper, lancer, poser, lâcher) ;
   - `TelekinesisHolder` (côté physique) : applique la commande aux `Rigidbody`, fait suivre le point de
     maintien en vitesse (vitesse du joueur + correction plafonnée par la vitesse d'approche), ignore la
     collision avec le porteur,
     lancer vers le point visé par le viseur ; `PhysicsGrabProbe` : rayon de visée ;
   - `Grabbable` : objet attrapable (masse du Rigidbody, accroché ou non, lueur par émission) ;
   - `TelekinesisView` : lueur des objets, orbe violette ou verte, trait ; HUD : barre d'endurance,
     orbe active, anneaux violet et vert ; debug F3 ; zone Télékinésie (F12).
   - Ordre d'un tick dans `PlayerMotor` (partie 1) : échange → magnétisme → télékinésie → mouvement → côté physique.
   - `PlayerMotor` avertit au lancement si la physique n'est pas en `SimulationMode.FixedUpdate` (sinon les
     objets ne bougent pas).
3 bis. **Une seule orbe (D-024)** — fait (2026-11-12), playtest en attente :
   - `OrbSelection`, `StaminaSettings` et `Stamina_Default.asset` supprimés ; `TractionEnd.Echange` aussi ;
   - actions `Grab` et `Throw` ajoutées, `SwapOrb`, `SwapOrbScroll`, `SelectOrb1`, `SelectOrb2` supprimées ;
     `Attract` passe sur A (AZERTY) / RB ;
   - `TelekinesisOrb.Step` sans endurance : F attrape ou pose, clic gauche lance (sans objet : consommé sans
     effet ni refus, réservé à l'artefact) ; refus : rien d'attrapable, trop lourd, recharge ;
   - `OrbLook` et `MagnetismOrbView` : couleur de l'orbe ; `TelekinesisView` ne touche plus l'orbe ;
   - `MovementHud` : plus de barre d'endurance ni de nom d'orbe ; troisième anneau (violet ou vert, rouge au
     refus) ; debug F3 : commandes du schéma actif et derniers appuis des quatre actions ;
   - `PhysicsAimProbe.IgnoreBody` : la visée du magnétisme ignore l'objet tenu ;
   - **objet tenu à grande vitesse :** `TelekinesisHolder.Drive` reçoit le déplacement réel des yeux pendant le
     tick (`PlayerMotor`, `_lastEye`) et corrige l'écart d'avant ce déplacement. Avant, il ajoutait la vitesse
     du joueur à une correction qui la comptait déjà : l'objet prenait un tick d'avance (0.22 m mesurés à
     15 m/s en partie 1, 0.67 m à 40 m/s). Mesuré en Play Mode avec le vrai joueur : 0.000 m en traction à
     30 m/s, 0.05 m en bunny hop à 20 m/s en virage de 30°/s ;
   - attraction : 1 charge toutes les 4 s (`Magnetism_Default.asset`).
   - Ordre d'un tick dans `PlayerMotor` : magnétisme → télékinésie → mouvement → côté physique.
4. **Télékinésie, partie 2** — à faire : dégâts des objets lancés, ennemis factices, éclats et sons.
