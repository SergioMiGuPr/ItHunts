# Mouvement : implementation

**Updated:** 2026-11-11 · [Design](design.md)

## Approach

**Plan, no code yet.** Les chiffres vivent dans [design.md](design.md) ; ce plan dit où ils
vont dans le code et d'où ils viennent.

Trois couches séparées, pour que l'écran partagé et la prédiction réseau arrivent sans réécriture :

1. **Entrées** (`Update`, par image) : un `PlayerInputReader` lit les actions du `PlayerInput`
   de *son* joueur et remplit une structure `MovementInput` : déplacement (Vector2), yaw et pitch
   de la vue, saut maintenu, glissade maintenue, et **les appuis mémorisés avec leur numéro de
   tick**. Un appui n'est jamais lu avec `WasPressedThisFrame` dans `FixedUpdate` : il serait
   manqué ou lu deux fois.
2. **Simulation** (`FixedUpdate`, 60 Hz) : `MovementMotor`, une classe C# **pure** (sans
   MonoBehaviour) : `Step(in MovementInput, ref MovementState, MovementSettings, ICollisionProbe)`.
   `MovementState` est une structure de données simple (position, vitesse, état, compteurs en
   ticks), donc copiable : c'est ce qu'il faudra renvoyer et rejouer en réseau.
3. **Présentation** (`Update` / `LateUpdate`) : la caméra suit une position **interpolée**
   entre les deux derniers ticks (le CharacterController n'a pas d'interpolation intégrée) ; la
   rotation de la vue s'applique à chaque image, sans lissage.

**Collisions :** `CharacterController` (capsule, pentes, marches et glissement le long des murs
intégrés), derrière l'interface `ICollisionProbe` qui ajoute un test de sol (SphereCast) et de mur
(CapsuleCast). *Alternative rejetée pour M1 :* un Rigidbody cinématique avec notre propre boucle
« cast and slide ». Plus de contrôle, mais une semaine de travail sur les collisions avant de
pouvoir sentir quoi que ce soit. L'interface permet de basculer plus tard si le
CharacterController bloque (surf, rollback réseau).

**Machine à états :** un `enum MovementMode { Sol, Air, Glissade, WallSlide }` et une table de
transitions évaluée une fois par tick, dans `MovementMotor`. Le WallJump est une impulsion dans
l'état Air, pas un état à part. Pas de classes d'état : l'état reste une donnée simple,
copiable et sérialisable pour le réseau.

## Code map (prévu)

| Fichier | Rôle |
|---|---|
| `Assets/_Project/Scripts/Runtime/ItHunts.Runtime.asmdef` | assembly du jeu |
| `.../Input/PlayerControls.inputactions` | carte « Player » : Move, Look, Jump, Slide, OrbPrimary, OrbSecondary, SwapOrb ; schémas Clavier-souris et Manette |
| `.../Input/PlayerInputReader.cs` | `PlayerInput` → `MovementInput` (appuis mémorisés par tick) |
| `.../Movement/MovementInput.cs`, `MovementState.cs` | structures de données |
| `.../Movement/MovementMotor.cs` | simulation pure : sol, air (façon Source), saut, puis glissade et murs |
| `.../Movement/MovementMath.cs` | fonctions statiques testables : accélération, friction, accélération en l'air, impulsion de saut |
| `.../Movement/PlayerMotor.cs` | MonoBehaviour : FixedUpdate → `Step`, `CharacterController.Move`, interpolation |
| `.../Movement/CharacterControllerProbe.cs` | `ICollisionProbe` : sol, murs, pente |
| `.../Camera/PlayerLook.cs` | yaw/pitch, FOV selon la vitesse ; pilote une `CinemachineCamera` |
| `.../Settings/MovementSettings.cs`, `CameraSettings.cs` | ScriptableObjects (`CreateAssetMenu`) |
| `.../Debug/MovementDebugOverlay.cs` | affichage de debug (F3 / bouton Select) |
| `Assets/_Project/Prefabs/Player.prefab` | CharacterController + PlayerInput + PlayerMotor + tête + caméra |
| `Assets/_Project/Settings/Movement_Default.asset`, `Camera_Default.asset` | réglages |
| `Assets/Scenes/Test_Movement.unity` | banc d'essai |
| `Assets/_Project/Tests/EditMode/ItHunts.Tests.EditMode.asmdef` + `MovementMathTests.cs`, `MovementMotorTests.cs` | tests |

**Règles multijoueur respectées :** aucun singleton ; tout l'état sur le prefab ; `PlayerInput`
(plus tard `PlayerInputManager` pour l'écran partagé, chaque joueur avec sa caméra et son canal
de sortie Cinemachine) ; simulation à pas fixe à partir d'une structure d'entrée.

## Data and config

- **Réglages :** `MovementSettings` contient tous les chiffres de [design.md](design.md) ;
  `CameraSettings` ceux de la section Caméra. Modifiables en Play Mode, et **les modifications
  restent dans l'asset** en quittant le Play Mode : c'est voulu pour le réglage, mais pense à
  dupliquer l'asset avant une grosse séance d'essais.
- **Pas fixe :** `Time.fixedDeltaTime` = 1/60 s (Unity est à 50 Hz par défaut) ; les durées
  (coyote, buffer, wall-coyote, verrou) sont converties en ticks avec `round(durée × 60)`.
- **Persistance :** aucune.

## D'où viennent les valeurs de départ

| Réglage (design.md) | Valeur | Source |
|---|---|---|
| Pas fixe | 60 Hz | skill `physics-tuning` (60 Hz de référence) ; Unity est à 50 Hz par défaut, on monte pour un jeu rapide |
| Coyote time | 0.10 s (6 ticks) | skill `platformer` (départ 0.1 s) ; **Celeste** 0.1 s |
| Buffer de saut | 0.12 s (7 ticks) | skill `platformer` (0.1-0.15 s) |
| Coupe au relâchement | 0.5 × | skill `platformer` (0.4-0.6) |
| Hauteur 1.3 m, gravité 20 m/s² | sommet en 0.36 s | skill `platformer` (temps au sommet de départ 0.35 s ; Celeste ≈ 0.37 s) ; gravité proche de **Quake** (800 unités/s² ≈ 20 m/s² avec 1 unité ≈ 1 pouce) |
| Gravité en chute | 1.5 × | skill `platformer` (1.3-2.5, départ 1.8) ; on prend le bas de la plage pour ne pas casser le rythme du bunny hop |
| Intégration du saut | demi-pas de gravité avant et après le déplacement | skill `platformer` (la hauteur visée est atteinte exactement) |
| Vitesse de course | 7.5 m/s | notre choix, proche de **Quake** (320 unités/s ≈ 8 m/s) ; doit rester sous le boss (9 m/s) |
| Accélération au sol | 10 | **Source / Half-Life 2** (`sv_accelerate` 10) ; vitesse max atteinte en ≈ 0.1 s, dans la plage du skill `platformer` (0.05-0.1 s) |
| Friction au sol | 6 | **Quake / HL2** (`sv_friction` 4) et **CS:GO** (5.2) ; un peu plus pour un arrêt net |
| Vitesse d'arrêt | 2.5 m/s | **Quake / Source** (`sv_stopspeed` 100 unités/s) : sous cette vitesse, la friction freine comme si on allait à 2.5 m/s, pour s'arrêter vraiment |
| Accélération en l'air / plafond | 10 / 1 m/s | **Source** (`sv_airaccelerate` 10, plafond ≈ 30 unités/s ≈ 0.6-0.8 m/s) : c'est ce qui crée le gain de vitesse en strafe et le bunny hop |
| Bunny hop | aucune friction sur le tick du saut | mécanique de **Quake / Source** |
| Glissade (bonus, friction, pente) | 2 m/s, 0.5, g·sin θ | notre choix, dans l'esprit de **Titanfall 2** (glissade qui garde l'élan) |
| Wall jump : hauteur | 0.8 × saut | skill `platformer` (≈ 0.8 × saut au sol) |
| Wall jump : impulsion | 7.5 m/s (= course) | skill `platformer` (≈ 1.25 × course en 2D) ; réduit car en 3D la vitesse le long du mur est conservée en plus |
| Wall-coyote / verrou de direction | 0.10 / 0.15 s | skill `platformer` (0.08-0.12 s / 0.15 s ; Celeste 0.16 s) |
| Chute max en wall slide | 3 m/s | notre choix (le skill propose ≈ 0.2 × la chute max, soit 7 m/s, trop rapide pour « glisser ») |
| Pitch | ±89° | convention FPS ; le skill `camera-systems` conseille ±80°, mais il faut viser le sol sous soi pour la répulsion |
| FOV selon la vitesse | +12° de 8 à 20 m/s | fiche `parkour.md` (« speed-shows ») ; valeurs à nous |
| Courbe du stick | exposant 2 | skill `input-systems` (courbe de réponse) |

## Scène `Test_Movement` (greybox, formes simples)

- Sol de 60 × 60 m avec des repères tous les 5 m (pour lire les distances de saut).
- **Sauts :** blocs de 0.5 / 1 / 1.3 / 1.5 / 2 m ; trous de 2 à 7 m.
- **Glissade :** couloir de 50 m, avec un passage de 1 m de haut (seulement en glissade) ;
  rampes descendantes de 15°, 30° et 45°.
- **Murs :** une cheminée (deux murs à 4 m l'un de l'autre, 12 m de haut) ; des murs isolés pour
  les wall jumps en chaîne.
- **Bunny hop :** piste droite de 100 m avec des portiques de chronométrage (temps et vitesse
  affichés au passage).
- Plus tard : surfaces magnétiques, objets à lancer, ennemis factices, porte de 1.2 m.

## Affichage de debug (F3 / bouton Select)

Vitesse horizontale et verticale (m/s), vitesse max de la dernière seconde, état
(`Sol` / `Air` / `Glissade` / `WallSlide`), au sol oui/non, angle de pente, compteurs coyote,
buffer et wall-coyote (en ticks), gain de vitesse du dernier saut, normale du mur. Plus tard :
endurance et recharges des orbes. Dans la vue Scène, des gizmos dessinent le vecteur vitesse et
la normale du mur.

## Étapes de construction

Chacune se termine par une compilation propre, des tests verts et une checklist de playtest.

1. **Course et saut** (point 4 du prompt de lancement) : entrées, prefab joueur, caméra et FOV,
   états Sol et Air, accélération en l'air façon Source, coyote, buffer, hauteur variable,
   gravité de chute, debug, scène de test (zone sauts).
2. Bunny hop (aucune friction sur le tick du saut) + piste chronométrée.
3. Glissade, glissade en pente, passage bas.
4. Wall slide et wall jump, cheminée.

## Tests EditMode prévus (étape 1)

- La vitesse au sol atteint 7.5 m/s en ≤ 0.15 s (9 ticks) et la friction l'arrête en ≤ 0.4 s
  (simulation à la main : 0.35 s).
- Hauteur de saut simulée à 60 Hz = 1.3 m ± 1 % ; sommet en 0.36 s ± 1 tick (simulation :
  1.2996 m en 0.367 s).
- Coyote : un saut au tick 6 après le bord passe, au tick 7 il est refusé.
- Buffer : un appui 7 ticks avant l'atterrissage saute, 8 ticks avant non.
- Relâcher pendant la montée coupe la vitesse verticale de moitié ; relâcher en descente ne fait rien.
- En l'air, maintenir la direction de la vitesse n'ajoute rien au-delà du plafond ; strafer à 90°
  en tournant ajoute de la vitesse.
- Un seul saut par appui (pas de double saut via le coyote).

## Risques pour le réseau (à garder en tête)

- Le bunny hop dépend d'un tick précis : les entrées sont horodatées en ticks dès maintenant.
- `CharacterController.Move` peut être rejoué plusieurs fois dans une image (prédiction), à
  condition d'appeler `Physics.SyncTransforms` ; à vérifier quand on arrivera au réseau.

## History

- 2026-11-11 : plan écrit.
