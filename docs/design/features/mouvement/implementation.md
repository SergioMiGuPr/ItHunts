# Mouvement : implementation

**Updated:** 2026-11-11 · [Design](design.md) · [Testing](testing.md)

## Approach

Étapes 1 (course, saut, contrôle aérien, auto-bunny hop, sauts multiples) et 3 (glissade,
accroupi) construites ; les murs sont à faire (voir « Étapes de construction »). Les chiffres vivent dans [design.md](design.md).

Trois couches séparées, pour que l'écran partagé et la prédiction réseau arrivent sans réécriture :

1. **Entrées** (`Update`, par image) : `PlayerInputReader` lit les actions du `PlayerInput` de
   *son* joueur et horodate chaque appui de saut avec le numéro du prochain tick
   (`PlayerMotor.NextTick`). Un appui n'est jamais lu avec `WasPressedThisFrame` en `FixedUpdate`.
2. **Simulation** (`FixedUpdate`, 60 Hz) : `MovementMotor.Step(ref MovementState, in MovementInput,
   MovementSettings, dt, ICollisionProbe)`, classe statique pure. `MovementState` et
   `MovementInput` sont des structures de données simples, copiables : c'est ce qu'il faudra
   envoyer, renvoyer et rejouer en réseau.
3. **Présentation** (`Update`) : `PlayerLook` place le pivot de vue (`ViewRig`, détaché du joueur
   au lancement) sur la position **interpolée** entre les deux derniers ticks, applique la rotation
   à chaque image sans lissage et ajuste le FOV de la `CinemachineCamera`.

**Collisions :** `CharacterController` derrière `ICollisionProbe` (`CharacterControllerProbe`) :
sol par SphereCast sous les pieds, puis repli sur le dernier contact par-dessous pour les bords
de marche ; recollage au sol en descente jusqu'à `groundSnapDistance`. *Alternative rejetée pour
M1 :* Rigidbody cinématique avec notre propre « cast and slide » (plus de travail avant de pouvoir
sentir quoi que ce soit) ; l'interface permet de basculer plus tard.

**Machine à états :** `enum MovementMode { Ground, Air, Slide, WallSlide }` évalué une fois par tick, plus
un drapeau `Crouched` (capsule basse). Murs : `ICollisionProbe.ProbeWall` (CapsuleCast
horizontal vers la direction voulue, la vitesse et le dernier mur, au-dessus de la hauteur de
marche), état de mur dans `MovementState` (`LastWallTick`, `LastWallNormal`, `LastWallId`,
`LastWallJumpId`, `SteerLockUntilTick`). Le mur est cherché vers l'entrée, la vitesse, le mur
précédent et les deux côtés de la trajectoire ; les collisions latérales retirent seulement la
part de vitesse qui entre dans l'obstacle (normale reçue par `OnControllerColliderHit`).
Charges de mouvement et dash : `MovementState.Charges`, `LastDashTick` ; formules du wall jump et de
la projection le long du mur dans `MovementMath` (`WallJumpVelocity`, `ProjectAlongWall`). La hauteur de capsule est
pilotée par la simulation (`ICollisionProbe.SetHeight`, `HasHeadroom` par SphereCast vers le haut). Le bunny hop de base est déjà là, par construction : le tick
du saut n'applique pas de friction.

## Code map

| Fichier | Rôle |
|---|---|
| `Assets/_Project/Scripts/Runtime/ItHunts.Runtime.asmdef` | assembly du jeu (Input System, Cinemachine) |
| `Assets/_Project/Input/PlayerControls.inputactions` | carte « Player » : Move, Look, LookGamepad, Jump, Slide, Dash, ToggleDebug, Pause ; schémas `KeyboardMouse` et `Gamepad` |
| `.../Runtime/Input/PlayerInputReader.cs` | `PlayerInput` → valeurs par image, appuis horodatés en ticks |
| `.../Runtime/Movement/MovementInput.cs`, `MovementState.cs` | structures de données (état en ticks) |
| `.../Runtime/Movement/MovementMath.cs` | friction, accélération au sol et en l'air (Source), contrôle aérien, accélération en pente, plafond |
| `.../Runtime/Movement/MovementMotor.cs` | simulation d'un tick : sol, accroupi, glissade, saut (coyote, buffer, coupe, sauts multiples, auto-bunny hop), contrôle aérien, gravité en demi-pas |
| `.../Runtime/Movement/ICollisionProbe.cs`, `CharacterControllerProbe.cs` | collisions |
| `.../Runtime/Player/PlayerMotor.cs` | FixedUpdate → `Step`, interpolation, retour au départ sous y = −30 m |
| `.../Runtime/Player/PlayerLook.cs` | sensibilités souris et manette, pitch, FOV, verrouillage du curseur |
| `.../Runtime/Settings/MovementSettings.cs`, `CameraSettings.cs` | ScriptableObjects de réglage |
| `.../Runtime/Debugging/MovementDebugOverlay.cs` | debug F3 / Select |
| `.../Runtime/Debugging/DebugTeleportPoints.cs`, `DebugTeleporter.cs` | téléportation de debug : F4 départ, F5 à F9 zones (points dans la scène ; clavier lu directement, joueur clavier-souris seulement) |
| `.../Scripts/Editor/TestMovementSceneBuilder.cs` | menu *ItHunts > Reconstruire…* : pas fixe 60 Hz, réglages, prefab, scène |
| `Assets/_Project/Prefabs/Player.prefab` | CharacterController, PlayerInput, scripts, ViewRig (caméra + CinemachineCamera) |
| `Assets/_Project/Settings/Movement_Default.asset`, `Camera_Default.asset` | réglages en cours |
| `Assets/Scenes/Test_Movement.unity` | banc d'essai (généré par le builder, modifiable ensuite) |
| `Assets/_Project/Tests/EditMode/MovementTests.cs` | 109 tests (sol factice avec pente, plafond et murs, sans physique Unity) |
| `.../Runtime/Hud/MovementHud.cs` | HUD greybox (IMGUI) : point de visée et segments des charges, dans la zone de la caméra du joueur |

**Règles multijoueur respectées :** aucun singleton ; tout l'état sur le prefab ; caméra dans le
prefab et non taguée MainCamera (une par joueur) ; `PlayerInput` (puis `PlayerInputManager` pour
l'écran partagé) ; simulation à pas fixe à partir d'une structure d'entrée.

## Data and config

- **Config :** `MovementSettings` (`runSpeed`, `groundAcceleration`, `groundFriction`, `stopSpeed`,
  `airAcceleration`, `airWishSpeedCap`, `gravity`, `fallGravityMultiplier`, `maxFallSpeed`,
  `jumpHeight`, `jumpCutMultiplier`, `coyoteTime`, `jumpBuffer`, `airControl`, `maxJumps`,
  `airJumpHeight`, `autoBunnyHop`, glissade (`slideMinSpeed`, `slideBoost`, `slideBoostCooldown`,
  `slideDeceleration`, `slideMinSpeedUnderObstacle`, `slideSlopeAcceleration`, `slideExitSpeed`,
  `slideSteering`), accroupi
  (`crouchHeight`, `crouchEyeHeight`, `crouchSpeed`), murs (`wallSlideMaxFallSpeed`, `wallJumpKick`,
  `wallJumpHeight`, `wallCoyoteTime`, `wallJumpSteerLock`, `allowSameWallJump`,
  `wallRefillsJumps`), `maxHorizontalSpeed`, capsule, `slopeLimit`,
  `stepOffset`, `groundSnapDistance`) ; `CameraSettings` (`mouseSensitivity`, `invertMouseY`,
  `gamepadYawSpeed`, `gamepadPitchSpeed`, `gamepadResponseExponent`, `invertGamepadY`,
  `pitchLimit`, FOV). Modifiables en Play Mode ; **les modifications restent dans l'asset**.
- **Pas fixe :** `ProjectSettings/TimeManager.asset` = 1/60 s. Les durées sont converties en
  ticks par `MovementSettings.SecondsToTicks` (coyote 6, buffer 7).
- **Persistance :** aucune.

## Deviations from design

- Aucune sur les règles. Le joueur repose à 0.04 m au-dessus du sol (marge de collision
  `skinWidth` du CharacterController), invisible en jeu.

## D'où viennent les valeurs de départ

| Réglage (design.md) | Valeur | Source |
|---|---|---|
| Pas fixe | 60 Hz | skill `physics-tuning` (60 Hz de référence) ; Unity est à 50 Hz par défaut, on monte pour un jeu rapide |
| Coyote time | 0.10 s (6 ticks) | skill `platformer` (départ 0.1 s) ; **Celeste** 0.1 s |
| Buffer de saut | 0.12 s (7 ticks) | skill `platformer` (0.1-0.15 s) |
| Coupe au relâchement | 0.5 × | skill `platformer` (0.4-0.6) |
| Hauteur 1.3 m, gravité 20 m/s² | sommet en 0.36 s | skill `platformer` (temps au sommet de départ 0.35 s ; Celeste ≈ 0.37 s) ; gravité proche de **Quake** (800 unités/s² ≈ 20 m/s² avec 1 unité ≈ 1 pouce) |
| Gravité en chute | 1.5 × | skill `platformer` (1.3-2.5, départ 1.8) ; bas de la plage pour ne pas casser le rythme du bunny hop |
| Intégration du saut | demi-pas de gravité avant et après le déplacement | skill `platformer` (hauteur visée atteinte exactement) |
| Vitesse de course | 7.5 m/s | notre choix, proche de **Quake** (320 unités/s ≈ 8 m/s) ; sous le boss (9 m/s) |
| Accélération au sol | 10 | **Source / Half-Life 2** (`sv_accelerate` 10) ; vitesse max en 0.15 s |
| Friction au sol | 6 | **Quake / HL2** (`sv_friction` 4) et **CS:GO** (5.2) ; un peu plus pour un arrêt net |
| Vitesse d'arrêt | 2.5 m/s | **Quake / Source** (`sv_stopspeed` 100 unités/s) |
| Accélération en l'air / plafond | 10 / 1 m/s | **Source** (`sv_airaccelerate` 10, plafond ≈ 30 unités/s) : gain de vitesse en strafe |
| Bunny hop | aucune friction sur le tick du saut | mécanique de **Quake / Source** |
| Glissade (bonus, décélération, pente) | 2 m/s, 3 m/s², g·sin θ | notre choix, dans l'esprit de **Titanfall 2 / Apex / Deadlock** (élan conservé, décélération constante) ; 3 m/s² fait durer une glissade à 20 m/s environ 6 s |
| Wall jump : hauteur | 0.8 × saut | skill `platformer` |
| Wall jump : impulsion | 7.5 m/s (= course) | skill `platformer` (≈ 1.25 × course en 2D), réduit car la vitesse le long du mur est conservée |
| Wall-coyote / verrou de direction | 0.10 / 0.15 s | skill `platformer` (0.08-0.12 s / 0.15 s ; Celeste 0.16 s) |
| Chute max en wall slide | 3 m/s | notre choix (le skill propose ≈ 7 m/s, trop rapide pour « glisser ») |
| Pitch | ±89° | convention FPS ; `camera-systems` conseille ±80°, mais il faut viser le sol sous soi |
| FOV selon la vitesse | +12° de 8 à 20 m/s | fiche `parkour.md` (« speed-shows ») |
| Courbe du stick | exposant 2 | skill `input-systems` |

## Scène `Test_Movement` (greybox)

Téléportation de debug : **F4** départ, **F5** zone ouverte, **F6** ligne droite, **F7** pentes,
**F8** grands murs, **F9** écarts et verticalité.

Construite par `TestMovementSceneBuilder` (menu *ItHunts > Reconstruire…*) sur un sol de
**400 × 400 m** (x −200..200, z −40..360), grille tous les 10 m, enceinte de 12 m. Départ en
(0, 0, −10), face au nord (+z). Caméra : vue jusqu'à 1 000 m.

| Zone | Position | Contenu |
|---|---|---|
| Zone de base | x −40..40, z −20..60 | repères tous les 5 m ; blocs 0.5 à 2 m (x = −20) ; escalier et trous de 2 à 7 m (x = 10) ; rampes 15° à 50° (x = 20..35) ; couloir de glissade 50 m avec passage bas de 1.1 m (x = −32) ; rampe de 20° vers une plateforme de 6 m (x = 0) |
| Grande zone ouverte | x −100..100, z 80..300 | 200 × 220 m vides ; 4 tours repères orange de 30 m juste hors des coins |
| Ligne droite de bunny hop | x = 185, z −30..350 | 380 m × 10 m, bandes tous les 25 m, bande jaune tous les 100 m |
| Longues pentes | x = −180 / −160 / −140, depuis z = −20 | 10° sur 150 m (+26 m), 20° sur 90 m (+30.8 m), 30° sur 60 m (+30 m), plateforme de 20 m au sommet |
| Grands murs | x 117..163, z 90..290 | paires de murs de 20 m × 120 m espacées de 6 m et de 10 m ; mur seul de 200 m × 15 m (x = 162) ; cheminée de 3 m × 30 m (x = 130, z = 250) |
| Écarts larges | z = 315, x −99..26 | plateformes à 4 m (accès par rampe de 20°) séparées de 8, 10, 12, 15, 18 et 22 m |
| Verticalité | z = 342, x 20..83 | 8 plateformes de 2 à 16 m, +2 m à chaque fois, 4 m d'écart |

## Étapes de construction

1. **Course et saut** — fait et validé (2026-11-11), avec contrôle aérien, auto-bunny hop, sauts
   multiples (D-012).
2. Bunny hop : fait avec l'étape 1 (piste chronométrée pas encore construite).
3. **Glissade, glissade en pente, passage bas, accroupi** — fait et validé (2026-11-11, D-013).
4. **Wall slide et wall jump avec élan, recharge des sauts au mur** — fait et validé (v2 façon
   Deadlock, 2026-11-12, D-016).
5. **Charges de mouvement et dash** — fait et validé (2026-11-12, D-015, D-017, D-018, D-019).

## Risques pour le réseau

- Le bunny hop dépend d'un tick précis : les appuis sont déjà horodatés en ticks.
- `CharacterController.Move` devra être rejoué plusieurs fois dans une image (prédiction) :
  vérifier `Physics.SyncTransforms` à l'étape 11.

## History

- 2026-11-11 : plan écrit.
- 2026-11-11 : étape 1 (course et saut) implémentée.
- 2026-11-11 : contrôle aérien, auto-bunny hop, sauts multiples, plafond de sécurité.
- 2026-11-11 : glissade et accroupi.
- 2026-11-11 : décélération constante et vitesse minimale sous obstacle (D-013).
- 2026-11-11 : scène de test agrandie à 400 × 400 m.
- 2026-11-11 : wall slide et wall jump.
- 2026-11-12 : murs v2 (D-016), charges de mouvement et dash (D-015) ; correction : la téléportation
  oublie les contacts mémorisés (sinon un tick de friction au sol après une téléportation en l'air).
