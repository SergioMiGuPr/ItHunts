# Run et niveaux : implementation

**Updated:** 2026-11-13 · [Design](design.md) · [Testing](testing.md)

## Approach

- **Run :**
  - `RunDefinition` (ScriptableObject `Run_Default`) : niveaux, difficultés, personnages.
  - `RunSession` (statique, vit d'une scène à l'autre ; état de la run, pas d'un joueur) : `Begin`, `NextLevelScene`, `End`,
    `Load`. Il expose aussi les multiplicateurs, qui valent 1 sans run lancée.
  - Les multiplicateurs sont appliqués dans `Damageable` (PV, sauf le traqueur), `PlayerHealth.TakeEnemyDamage` (dégâts),
    `EnemyLocomotion.SpeedScale` (goules, volants) et `WaveDirector` (nombre).
- **Zone :**
  - `ZoneState` porte `Started` (sortie de la salle de départ, volume `RoomKind.Depart`), `ExitUnlocked`, `LevelWon`,
    `RunOver`, `BossFightActive` et `ExitHandler` (le combat de boss prend la main à la sortie).
  - La touche Page haut avance le timer de 60 s (debug).
  - `WaveDirector` ne tourne pas avant `Started` ni pendant le combat.
- **Porte de sortie :** `ExitDoor` (`IInteractable`), texte par caméra, matériau verrouillé ou payable.
- **Fin :**
  - `RunEndScreen` (IMGUI, liste `GreyboxMenu` : souris, clavier, manette) ; `PlayerLook.MenuOpen` libère le curseur.
  - `CoopLife` : événements de réanimation et de spectateur, pour l'étape 9.
- **Menus :** `MainMenu` (scène `MainMenu`, première du build), construite par *ItHunts > Reconstruire le menu principal*.
- **Marchand :** `ArtifactMerchant`. Offres par joueur (`OfferOrb` réservée, avec `ViewerOnly`), tirage seed × joueur. Le pool
  est `UpgradePool_Barillet` ; les effets passent par `PlayerUpgrades`, sur la copie du `BarrelSettings`.
- **Non magnétique :**
  - `NonMagneticSurface` est lu par `PhysicsAimProbe` (`AimHit.NonMagnetic`).
  - `MagnetismOrb` refuse la traction (`MagnetRefusal.NonMagnetique`).
  - `MagnetismFeedback.CanAttract` en tient compte.
- **Constructeurs :**
  - `TestArenaSceneBuilder` est partiel. `BeginScene` et `FinishScene` sont communs (sortie, arène, graphe, NavMesh, zone,
    run, traqueur, vagues, joueur, minimap).
  - `Level01Builder` (plan `LevelLayoutSettings`, grille, retrait de couloirs avec degré ≥ 2 et carte d'un seul tenant,
    intérieurs, canaux `CanalFloor` et `DeepWater`, coffres `ChestCorner`, `ParkourCourse`).
  - `BossArenaBuilder` (arène, `BuildTrap`).
- **Combat :**
  - `BossFight` : début, entrée des joueurs, `Stalker.WarpTo` et `Stalker.Stagger`, compte des pièges, `LevelWon`.
  - `BossTrap` : cible `Damageable` d'un PV avec `TrapTarget` (sans âmes), zone, pièce qui tombe ou disparaît, réarmement.
- **Minimap :**
  - `MapDiscovery` : découvertes communes, `DiscoverAt`, `IsKnownPoint`, remise à zéro avec la zone.
  - `MinimapHud` : IMGUI dans le `pixelRect` de la caméra de chaque joueur.

## Réseau (étape 11)

Sur l'hôte : début du timer, porte payée, fin de run et changement de niveau, découvertes, pièges, achats au marchand.
`RunSession` est local : l'hôte enverra la difficulté et le niveau.
