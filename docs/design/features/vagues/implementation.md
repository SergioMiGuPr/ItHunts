# Vagues : implementation

**Updated:** 2026-11-13 · [Design](design.md) · [Testing](testing.md)

## Approach

- **Calculs purs :** `WaveMath` (tendance interpolée, niveau Enragé, pics et accalmies, nombre voulu, intervalle, plafonds
  mis à l'échelle, distance, champ de vision, règles de reconstitution) : testés en EditMode.
- **Directeur :** `WaveDirector` (objet de la scène, FixedUpdate, marqueur de profilage « ItHunts.Waves ») : lit le temps de
  la zone (`ZoneState.Elapsed`), calcule le voulu par type, compte les présents, la dette (tués à reconstituer, bornée à ce
  qui manque), lance les apparitions normales (horloge par type) et la reconstitution (une toutes les 4 s). Une apparition :
  membre libre de la réserve + point choisi (`ChoosePoint` : type, distance, pas utilisé depuis 2 s, mélange par la seed de la
  run, 24 essais de visibilité au plus) ; lueur d'annonce (cylindres réutilisés) ; à la fin, `Ghoul.Spawn` / `Flyer.Spawn`
  (nouvelle place de départ, PV pleins, en attente). Les plafonds de chasse passent par `Ghoul.HuntingScale` et
  `Flyer.HuntingScale`. Un pic lance `PeakStarted` (HUD) et le cor (`ProceduralSfx.WaveHorn`).
- **Visibilité :** hors du champ de vision (demi-champ horizontal pour 16:9 + 10°), ou rayon des yeux au point coupé par le
  décor statique, pour chaque joueur vivant.
- **Accalmie :** `Ghoul.PackShare` / `Flyer.PackShare` (posés par le directeur) réduisent le plafond sans l'aggro ; `Pack` renvoie
  les chasseurs en trop (les plus loin) en attente (`Retreat` : recul de `lullRetreatDistance`, aggro désarmée tant que le joueur
  n'est pas sorti de la zone d'aggro) ; au pic, `Pack` les reprend.
- **Coop :** `WaveMath.PlayerMultiplier`, `Shares`, `NextPlayer`, `DistanceOkFor` ; le directeur note pour qui chaque ennemi est
  apparu (`OwnedBy`). Choix de cible : `TargetKeeper` (goule et volant), `EnemyMath.ShouldSwitchTarget`, `Damageable.NoteAttacker`
  (balle, objet ou ennemi lancé, prise).
- **Points :** `SpawnPoint` (type goule et / ou volant, gizmo dans l'éditeur), liste statique des points actifs.
- **Remise à zéro de la zone :** `ZoneState.ResetZone` appelle `WaveDirector.ResetAll` (toute la réserve rentre, la
  population de départ est reposée au pas suivant, seed de la run + numéro de remise à zéro).
- **Scène :** le constructeur pose 108 points (8 goules par grande salle, 3 par couloir ; volants : 4 par grande salle à 12 m,
  1 par couloir à 5 m sous le plafond ; `ArenaLayout_Test`), la réserve (80 goules, 20 volants inactifs sous le niveau) et le
  directeur.

**Réseau (étape 11) :** directeur et apparitions sur l'autorité.

## Files

| File | Role |
|---|---|
| `Assets/_Project/Scripts/Runtime/Settings/WaveSettings.cs` | réglages (`Waves_Default`) |
| `Assets/_Project/Scripts/Runtime/Enemies/WaveMath.cs` | calculs purs |
| `Assets/_Project/Scripts/Runtime/Enemies/WaveDirector.cs` | directeur des vagues |
| `Assets/_Project/Scripts/Runtime/Enemies/SpawnPoint.cs` | points d'apparition |
| `Assets/_Project/Scripts/Runtime/Enemies/Ghoul.cs`, `Flyer.cs`, `Combat/EnemyBody.cs` | `Spawn`, `HuntingScale`, `SetHome` |
| `Assets/_Project/Scripts/Runtime/Hud/PlayerHud.cs` | « VAGUE n » à chaque pic |
| `Assets/_Project/Scripts/Runtime/Debugging/MovementDebugOverlay.cs` | ligne « Vagues » (page 3) |
| `Assets/_Project/Scripts/Editor/TestArenaSceneBuilder.cs` | points, réserve, directeur |
| `Assets/_Project/Scripts/Runtime/Enemies/TargetKeeper.cs` | choix de cible en coop |
| `Assets/_Project/Tests/EditMode/WaveTests.cs`, `CoopWaveTests.cs`, `EnemyTests.cs` | calculs, coop, accalmie, cible, asset, scène |

## Build steps

1. **Vagues** — fait (2026-11-13, D-041), validé en playtest.
2. **Accalmie, coop, choix de cible** — fait (2026-11-13), playtest en attente.
