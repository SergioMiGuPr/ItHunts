# Vue FPS et HUD : implementation

**Updated:** 2026-11-12 · [Design](design.md) · [Testing](testing.md)

## Approach

- **Rendu des mains (option 2, D-028) :** `FirstPersonView` (sur le joueur) prend la couche `FirstPersonP{index + 1}`
  (couches 8 à 11, créées par le constructeur dans `ProjectSettings/TagManager.asset`), y met les mains, et la caméra
  du joueur ne voit que cette couche parmi les quatre. Le constructeur (`EnsureFirstPersonRendering`) règle chaque
  `UniversalRendererData` (`PC_Renderer`, `Mobile_Renderer`) : les passes normales ignorent les quatre couches ; après
  les transparents, `FirstPersonDepthClearFeature` (passe Render Graph qui efface la profondeur, caméras de jeu
  seulement), puis « Render Objects » des mains (file opaque, champ de vision `viewmodelFov`), puis « Render Objects »
  du HUD (file transparente). Changer `viewmodelFov` demande de relancer le constructeur. La passe qui efface la
  profondeur coupe aussi le mot-clé `_SCREEN_SPACE_OCCLUSION` pour la suite de l'image (réglage
  `handsReceiveAmbientOcclusion`, non par défaut ; la passe SSAO le rallume à l'image suivante). Le constructeur
  réutilise les passes existantes (mêmes identifiants) et ne touche jamais aux renderers des packages. Une caméra superposée
  (option 1) reste possible si `overlayCamera` est assignée.
- **Mesure dans un build :** `PerfProbe` (objet de la scène de test, inactif sans l'argument `-ithunts-perf`) :
  1 joueur puis 2 en écran partagé, blocs de 5 s allumés / éteints, médianes `FrameTimingManager` (fil principal, fil
  de rendu, GPU), fichier de résultats, puis quitte. Build de développement (`Builds/`, ignoré par git), avec
  `enableFrameTimingStats` et `runInBackground` mis le temps du build.
- **Mouvement des mains :** `ViewmodelMotion` (pur) : balancement, balancier, plongée (avec l'intensité), recul avec la
  vitesse, courbes des gestes (sans l'intensité). `FirstPersonView.LateUpdate` lit `PlayerMotor` (état simulé, orbe,
  télékinésie) et `PlayerLook` ; un geste démarre quand `LastRepelTick` ou `Telekinesis.LastActionTick` change.
- **Traits :** `FirstPersonView.LineStart` convertit la position de l'orbe dessinée à 60° en un point du monde vu au
  même endroit par la caméra du joueur (rapport des tangentes des demi-FOV) ; `MagnetismOrbView` et `TelekinesisView`
  s'en servent.
- **HUD :** `PlayerHud` construit au démarrage un canvas uGUI « Screen Space - Camera » sur la caméra des mains (devant
  les mains, sur la couche du joueur), avec `CanvasScaler` (1920 × 1080, hauteur) ; textes en police intégrée
  `LegacyRuntime.ttf`, anneaux en sprites générés. `GaugeFade` (pur) : HUD discret. Les coins réservés sont des
  `RectTransform` vides (`SoulsCorner`, `TimerCorner`, `StalkerCorner`).
- **Debug :** `MovementDebugOverlay` en trois pages (F3 / Select).
- **Ajouts :** `TelekinesisOrbSettings.throwSpeedBonusCap` (20 m/s) ; `TrainingDummySettings.stunImmunity` (2 s),
  `AreaEffects.CanStun` / `StunTimes`, `EnemyBody.StunImmune`.
- Marqueurs de profilage : `ItHunts.FirstPersonView`, `ItHunts.PlayerHud`.

**Réseau :** affichage seulement, lu dans l'état simulé ; rien à synchroniser.

## Files

| File | Role |
|---|---|
| `Assets/_Project/Scripts/Runtime/Player/FirstPersonView.cs` | caméra des mains, couches, mouvement, gestes, départ des traits |
| `Assets/_Project/Scripts/Runtime/Player/ViewmodelMotion.cs` | calculs purs du mouvement des mains |
| `Assets/_Project/Scripts/Runtime/Player/FirstPersonDepthClearFeature.cs` | passe URP qui efface la profondeur avant les mains |
| `Assets/_Project/Scripts/Runtime/Debugging/PerfProbe.cs` | mesure de performance dans un build |
| `Assets/_Project/Scripts/Runtime/Hud/PlayerHud.cs` | HUD uGUI d'un joueur |
| `Assets/_Project/Scripts/Runtime/Hud/GaugeFade.cs` | HUD discret (pur) |
| `Assets/_Project/Scripts/Runtime/Settings/FpsViewSettings.cs`, `HudSettings.cs` | réglages (`FpsView_Default`, `Hud_Default`) |
| `Assets/_Project/Scripts/Runtime/Debugging/MovementDebugOverlay.cs` | debug en trois pages |
| `Assets/_Project/Scripts/Editor/TestMovementSceneBuilder.cs` | couches, mains greybox, caméra des mains, HUD dans le prefab |
| `Assets/_Project/Tests/EditMode/FpsViewTests.cs` | 10 tests (intensité, gestes, balancier, HUD discret, plafond du lancer, immunité, couches, prefab, réglages) |

Supprimés : `Hud/MovementHud.cs` et `Combat/DamageNumbersHud.cs` (HUD provisoires en IMGUI).

## Build steps

1. **Vue FPS et HUD** — fait et validé en playtest (2026-11-12, D-028).
2. **Option 2 après mesure dans un build** — fait (2026-11-12) : passe de rendu, coût sous les seuils.
