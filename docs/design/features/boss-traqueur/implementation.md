# Boss traqueur : implementation

**Updated:** 2026-11-12 · [Design](design.md) · [Testing](testing.md)

## Approach

- **Corps :** objet racine aux pieds, Rigidbody cinématique, capsule solide de 2.5 × 3.5 m ; `Damageable` insensible
  (`immuneToOrbs` : `ApplyDamage` ne retire rien, la répulsion, l'étourdissement, la zone, les balles et la prise
  l'ignorent déjà) ; ni `Grabbable` ni `EnemyBody`. Pas de barre de PV.
- **Cerveau (D-035) :** `Stalker` (FixedUpdate, pas fixe) : Repaire pendant la grâce, puis Chasse / Enragé ; cible = le
  joueur le plus proche par le chemin (`EnemyLocomotion.Probe`), revue au plus toutes les 5 s. Décision toutes les
  0.2 s : `PursuitGoal` (devant l'ouverture d'une `ReachRoom` si le joueur y est, sinon le point prédit ramené sur son
  NavMesh) ; **droit dessus** si le joueur est proche, devant, et que le chemin d'essai part vers l'avant ; sinon
  **règle de Pac-Man** avec `MazeGraph` (intersections en boîtes, couloirs, distances par Floyd-Warshall, et sans
  l'intersection de départ) : `TrackNodes` note l'intersection d'arrivée en y entrant, `ChooseNext` l'exclut sauf
  cul-de-sac ; dans un couloir, il garde l'intersection devant lui. `UTurns` compte les demi-tours (mesure, doit
  rester à 0). Pointe : quand il voit le joueur (`EnemySenses.Sees`).
- **Se baisser :** `UpdateCrouch` (sphère lancée vers le haut sur place et 6.5 m devant) ; le corps (`body`) et la
  capsule s'écrasent jusqu'à `crouchMinHeight`, vitesse × `crouchSpeedFactor`. NavMesh du traqueur construit avec la
  hauteur baissée (9 m).
- **Portée :** `CanReach` (pièce : ouverture à moins de `roomReach` et point devant elle ; ailleurs : 34 essais sur son
  NavMesh autour du joueur, à sa hauteur et 11 m plus bas, épaule tournée vers lui, `handReach` + `edgeLean` s'il ne
  peut pas se placer dessous, ligne libre pour le bras, dégagement au-dessus).
- **NavMesh :** `ZoneNavMesh` (objet de la scène) le construit au lancement à partir des colliders fixes (`NavMeshBuilder`) :
  un type d'agent humain (rayon 0.45, voxels de 0.2 m dans le labyrinthe) et un pour le traqueur (rayon 3.5, hauteur 9) ;
  les pièces marquées `NotWalkable` (plafonds, toits, murs) sont des obstacles jamais praticables ; liens `ClimbLink` pour
  les goules seulement ; `StalkerShortcut` gardé mais plus utilisé ; identifiants de type d'agent négatifs
  (`EnemyLocomotion.NoAgent` = `int.MinValue`).
- **Mains levées et contact (D-037) :** `Stalker.UpdateOverhead` (Aucune → Annonce → Active → Retour ; déclenché par
  `InOverheadZone` → `EnemyMath.WillPassOver`), bras et main gauches (`leftArm`, `leftHand`) posés par le code comme le droit
  (`PoseLimb`) ; `TouchesLimb` (capsule du joueur contre les boîtes des deux mains et des deux bras) chaque pas fixe ;
  compteurs `OverheadAttempts`, `OverheadKills`, `LimbKills`.
- **Enragé (D-038) :** `EnemyMath.EnragedRampSpeed` (paliers, montée linéaire) avec le temps depuis la dernière nouvelle cible
  (`_targetSince`, remis à zéro dans `ChooseTarget`) et l'entrée en Enragé ; `EnemyMath.StalkerSpeed` plafonnée à `maxSpeed` ;
  `EnragedTier`, `LastTierTime` → grondement (`StalkerAudio`) et pulsation du HUD (`PlayerHud`) ; page 3 du debug.
- **Main plantée (D-039) :** `ResolveGrab` dit si la main a attrapé quelqu'un ; `EnemyMath.GrabHoldTime` donne le temps au sol
  (plantedDuration si ratée, grabHold sinon) ; `Planted` coupe la pointe, ralentit la rotation (`plantedTurnFactor`), bloque une
  nouvelle prise ; repère « Poussière » et « Lueur » qui s'éteint sous la main.
- **Coup et prise à la main (D-034) :** `StalkerStrike.Hit` → `PlayerHealth.Kill` (quels que soient les PV, sans tir
  ami) ; prise : `Stalker.UpdateGrab` (Annonce 0.5 s, visée corrigée 0.15 s avant la fin → Descente 0.12 s → AuSol 0.35 s → Retour ;
  par une ouverture, la main entre à l'horizontale), point visé
  `EnemyMath.Predict` posé sur la surface dessous, portée depuis `ReachShoulder` (épaule tournée vers la cible), abri si
  `OverheadClear` échoue (4.5 m de dégagement), résolution `StalkerStrike.Grab` → `EnemyMath.CapsuleIntersectsBox` avec la
  BoxCollider de la main du modèle. Bras posé par le code (`PoseArm`). Perception de proximité dans la boucle de vue.
- **Tremblement :** `StalkerAudio.Footstep` → `CameraShake` du joueur (décalage de la caméra virtuelle, réglage de confort
  enregistré par joueur).
- **Zone :** `ZoneState` (timer, grâce, sortie, remise à zéro à la mort, `PlayerHealth.ZoneHandlesDeath`), `ZoneExit`
  (déclencheur), HUD : timer et « TRAQUEUR ENRAGÉ » dans les coins réservés, message central.
- **Sons :** `StalkerAudio` (pas, souffle, grondement, nappe de tension), `ProceduralSfx`.
- **Debug :** page 3 (état, vitesse, pointe et recharge, point prédit) et repère rouge du point prédit.

**Réseau (étape 11) :** décisions, chemin et coup mortel sur l'autorité ; la mort en un coup est sensible à la latence
(GDD) : le serveur tranche.

## Files

| File | Role |
|---|---|
| `Assets/_Project/Scripts/Runtime/Enemies/Stalker.cs` | cerveau et déplacement |
| `Assets/_Project/Scripts/Runtime/Enemies/StalkerAudio.cs` | annonce sonore |
| `Assets/_Project/Scripts/Runtime/Enemies/StalkerStrike.cs` | coup mortel |
| `Assets/_Project/Scripts/Runtime/Enemies/MazeGraph.cs` | graphe du labyrinthe, règle de Pac-Man |
| `Assets/_Project/Scripts/Runtime/Enemies/ReachRoom.cs` | pièce où il passe le bras (ouvertures) |
| `Assets/_Project/Scripts/Runtime/Enemies/NotWalkable.cs`, `ClimbLink.cs` | décor jamais praticable, murs des goules |
| `Assets/_Project/Scripts/Runtime/Enemies/StalkerShortcut.cs` | raccourci réservé (plus utilisé, D-035) |
| `Assets/_Project/Scripts/Runtime/Enemies/EnemyMath.cs`, `EnemySenses.cs`, `EnemyLocomotion.cs`, `NoiseEvents.cs` | calculs, perception, déplacement, bruits (communs) |
| `Assets/_Project/Scripts/Runtime/Enemies/ZoneNavMesh.cs`, `ZoneState.cs`, `ZoneExit.cs` | NavMesh, zone, sortie |
| `Assets/_Project/Scripts/Runtime/Settings/StalkerSettings.cs`, `ZoneSettings.cs`, `EnemySettings.cs` | réglages |
| `Assets/_Project/Scripts/Editor/TestArenaSceneBuilder.cs`, `ArenaLayoutSettings.cs` | scène Test_Arena et ses dimensions (`ArenaLayout_Test`) |
| `Assets/_Project/Scripts/Runtime/Player/CameraShake.cs` | tremblement de caméra (confort) |
| `Assets/_Project/Tests/EditMode/EnemyTests.cs`, `StalkerTests.cs` | tests |

## Build steps

1. **Traqueur complet et prototype M3** — fait (2026-11-12, D-033, étape 6 blocs 3 et 4) ; playtest : arène trop petite.
2. **Géant qui attrape à la main, Test_Arena à l'échelle** — fait (2026-11-12, D-034), playtest en attente.
3. **Poursuite façon Pac-Man, labyrinthe** — fait (2026-11-12, D-035), playtest en attente.
4. **Mains levées, contact mortel du corps entier** — fait (2026-11-12, D-037), playtest en attente.
5. **Enragé qui accélère** — fait (2026-11-12, D-038), playtest en attente.
6. **Main plantée** — fait (2026-11-12, D-039), playtest en attente.
