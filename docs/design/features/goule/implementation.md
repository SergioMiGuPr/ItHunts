# Goule : implementation

**Updated:** 2026-11-12 · [Design](design.md) · [Testing](testing.md)

## Approach

- **Corps :** objet racine aux pieds, Rigidbody cinématique, `CapsuleCollider` (rayon 0.45, hauteur 1.1), `Grabbable`
  ennemi (prise étourdie), `ImpactDamager` (blesse et se blesse lancée), `EnemyBody` (recul, étourdissement, dégâts
  contre un mur, vol ; `returnHome` coupé), `Damageable` (60 PV, pas de réapparition seule). Réglages communs dans
  `EnemySettings` (base de `GhoulSettings`, `StalkerSettings`, `TrainingDummySettings`).
- **Cerveau :** `Ghoul` (FixedUpdate) : modes `GhoulMode` (Poste, Chasse, Annonce, Bond, Mur ; Retour gardé pour les vagues) ;
  cible = joueur le plus proche par le chemin (`EnemyLocomotion.Probe` si plusieurs), encerclement (angle d'or par goule) ; quand
  `EnemyBody.Busy` (repoussée, étourdie, tenue, en vol), elle ne fait rien et reprend ensuite.
- **Déplacement :** `EnemyLocomotion` sur le NavMesh humain de `ZoneNavMesh` ; murs : liens `ClimbLink` ajoutés au NavMesh
  (`NavMesh.AddLink`, goules seulement) ; quand le coin suivant du chemin est bien plus haut ou plus bas et tout près,
  mode Mur (ligne droite à `climbSpeed` / `dropSpeed`, corps plaqué), puis coin suivant ; bond : vitesse de départ
  `EnemyMath.LeapVelocityTo` (aussi vers un point plus bas), intégration avec gravité, balayage (`SweepTest`) contre les murs, retombée sur le NavMesh.
- **Coups :** `EnemyMath.CapsuleGap` (distance entre capsules) ; `PlayerHealth.TakeEnemyDamage` (pas de tir ami) ;
  poussée `PlayerMotor.AddVelocity` → `EnemyMath.PushWithoutSlowdown`.
- **Retour :** visuel qui s'accroupit pendant l'annonce, disque rouge au sol, sifflement (`ProceduralSfx.GhoulHiss`),
  yeux lumineux.

**Réseau (étape 11) :** cerveau et coups sur l'autorité ; la poussée du joueur passe par sa simulation.

## Files

| File | Role |
|---|---|
| `Assets/_Project/Scripts/Runtime/Enemies/Ghoul.cs` | cerveau, déplacement, murs, bond, coups |
| `Assets/_Project/Scripts/Runtime/Enemies/ClimbLink.cs` | mur grimpable posé à la main (facultatif depuis D-038) |
| `Assets/_Project/Scripts/Runtime/Enemies/ZoneNavMesh.cs` | NavMesh, murs grimpables trouvés automatiquement (`FindClimbs`, `TryTop`) |
| `Assets/_Project/Scripts/Runtime/Settings/GhoulSettings.cs` | réglages (`Ghoul_Default`) |
| `Assets/_Project/Scripts/Runtime/Settings/EnemySettings.cs` | réglages communs des ennemis |
| `Assets/_Project/Scripts/Runtime/Player/PlayerHealth.cs`, `PlayerMotor.cs` | coups des ennemis, poussée sans ralentissement |
| `Assets/_Project/Tests/EditMode/EnemyTests.cs` | calculs, coups, réglages |

## Build steps

1. **Au sol** — fait (2026-11-12, étape 6 bloc 2), playtest avec le prototype M3.
2. **Poursuite permanente, encerclement, murs** — fait (2026-11-12, D-035), playtest en attente.
3. **Plafond, aggro, murs automatiques** — fait (2026-11-12, D-037, D-038), playtest en attente.
