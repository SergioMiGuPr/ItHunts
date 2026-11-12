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
- **Entrées :** `MovementInput.Pitch`, `AttractPressedTick`, `AttractHeld`, `RepelPressedTick`
  (`PlayerInputReader`, actions `Attract` et `Repel`).
- **Visée :** `IAimProbe` ; en jeu `PhysicsAimProbe` (Raycast, ignore les triggers et le joueur
  lui-même). Toute surface répond.
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
3. **Télékinésie et échange des orbes** — étape 4.
