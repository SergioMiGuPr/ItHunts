# Coop locale : implementation

**Updated:** 2026-11-14 · [Design](design.md) · [Testing](testing.md)

## Approach

- **`SplitScreen`** (`Runtime/Player/SplitScreen.cs`, un par scène de niveau, `SplitScreen.Current`) :
  - `Join(device)` instancie le prefab du joueur avec `PlayerInput.Instantiate(..., pairWithDevice)` et verrouille le joueur 1
    sur son appareil (`LockFirstPlayer`, `neverAutoSwitchControlSchemes`) ;
  - `Layout()` règle la découpe (`ViewRect`), le canal Cinemachine de chaque joueur (Default, puis Channel01), le masque du
    cerveau de sa caméra et le facteur de FOV (`FovScaleFor`, `PlayerLook.FovScale`). Le rig de vue est détaché du joueur au
    démarrage (`PlayerLook.ViewRig`) : ses caméras virtuelles se cherchent dans le rig, pas dans le joueur ;
  - `BeforeCamera` (`RenderPipelineManager.beginCameraRendering`) affiche les mains et l'orbe du seul joueur de la caméra, et
    la capsule des autres ;
  - `LateUpdate` place les capsules (couchées à terre). Pour un spectateur, il coupe le cerveau Cinemachine et recopie la
    caméra de l'allié ;
  - `OnGUI` dessine les bandeaux « À terre » et « Spectateur » et la barre de réanimation, dans la moitié du joueur ;
  - un seul `AudioListener` (joueur 1). L'affichage de debug (`MovementDebugOverlay`, IMGUI plein écran) est coupé pour le
    joueur 2.
- **`PlayerHealth`** :
  - `Downed`, `Spectating`, `DownedAt`, `DownedLeft`, `TickDowned(now)`, `OthersStanding()`, `ReviveFromDown(hp)` ;
  - dans `Raise`, une mort devient « à terre » s'il reste un allié debout ;
  - `Dead` reste vrai à terre, donc les cibles des ennemis, les vagues et la fin de partie n'ont pas changé ;
  - `Restore` (zone suivante) remet tout à zéro ;
  - événements `CoopLife` à chaque changement.
- **`CoopRevive`** (sur le joueur) : `Step(dt)` cherche l'allié à terre le plus proche dans `reviveRange`. Le maintien vient
  de `PlayerInputReader.InteractHeld`, ou de `HeldOverride` dans les tests. Expose `Target`, `Progress` et `Revives`.
- **`PlayerMotor`** : entrée neutre quand le joueur est à terre ou mort.
- **`TelekinesisView`** : lueurs des objets recalculées avant la caméra de chaque joueur.
- **Menu** : `MainMenu` gère Start sur une manette et l'entrée « Joueur 2 » ; `RunSession.CoopDevice` garde la manette d'une
  scène à l'autre ; `GreyboxMenu.LastNavPad` sert à savoir quelle manette a appuyé.
- **Scènes** : `SplitScreen` est posé par `TestArenaSceneBuilder.FinishScene` (Test_Arena, bases du niveau 1). Level_01 l'a
  reçu par la recopie du 2026-11-14.
- **Mesure** : `LevelPerfProbe -ithunts-perf-split` fait rejoindre un 2e joueur (`LevelPerfProbe.SpawnSecondPlayer`).

## Réseau (étape 11)

- « À terre », la réanimation et le passage spectateur sont décidés par l'autorité, avec l'état de santé.
- La capsule et les bandeaux restent locaux. `SplitScreen` reste propre au local.

## Files

| File | Role |
|---|---|
| `Assets/_Project/Scripts/Runtime/Player/SplitScreen.cs` | rejoindre, découpe, canaux, FOV, corps, spectateur, bandeaux |
| `Assets/_Project/Scripts/Runtime/Player/CoopRevive.cs` | réanimation par maintien |
| `Assets/_Project/Scripts/Runtime/Player/PlayerHealth.cs` | à terre, spectateur, fin de partie |
| `Assets/_Project/Scripts/Runtime/Player/PlayerLook.cs` | `ViewRig`, `FovScale` |
| `Assets/_Project/Scripts/Runtime/Orbs/TelekinesisView.cs` | lueurs par caméra |
| `Assets/_Project/Scripts/Runtime/Settings/CoopSettings.cs` | réglages (`Coop_Default`) |
| `Assets/_Project/Scripts/Runtime/Level/MainMenu.cs` | rejoindre au menu |
| `Assets/_Project/Tests/EditMode/CoopTests.cs` | tests |
