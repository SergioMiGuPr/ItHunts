# Volant : implementation

**Updated:** 2026-11-13 · [Design](design.md) · [Testing](testing.md)

## Approach

- **Corps :** racine au centre, Rigidbody cinématique, `SphereCollider` (0.4 m) ; `Grabbable` ennemi avec
  `grabbableAnytime` (attrapable sans étourdissement) et `holdDurationOverride` = 2 s (lu par `TelekinesisOrb`,
  `TelekinesisHolder`, `TelekinesisView` via `Grabbable.HoldDuration`) ; `ImpactDamager` (dégâts d'impact habituels) ;
  `EnemyBody` (recul, gravité, étourdissement ; `wallDamageWhileStunned` : dégâts contre un mur même déjà étourdi, une
  fois par recul ; `EndFlight` pour reprendre le vol) ; `Damageable` (25 PV).
- **Cerveau :** `Flyer` (FixedUpdate) : modes `FlyerMode` (Attente, Ronde, Annonce, Pique, Remontee, Tenu, Lance, AuSol) ;
  plafond et aggro comme les goules (à vol d'oiseau) ; vol : ligne droite si rien ne cache le point, sinon coins du chemin
  des goules (`EnemyLocomotion`) à hauteur de vol ; déplacement par balayage de sphère qui glisse contre le décor.
- **Éclatement :** `Flyer.BurstAt` (zone `EnemyMath.BurstDamage`, ligne de vue `ImpactDamager.InSight`, coups publiés par
  `ImpactDamager.ApplyDirect`, tir ami `FriendlyFire` + `PlayerHealth.TryHurt`, cercle `AreaEvents.RaiseArea`), puis mort.
- **Retour :** ailes qui battent, corps incliné en piqué, lueur rouge et cri (`ProceduralSfx.FlyerScreech`) à l'annonce,
  éclats et son à l'éclatement ; page 3 du debug (en vie, en chasse, en piqué, le plus proche).
- **Scène :** `TestArenaSceneBuilder.BuildFlyer`, 4 par grande salle à 12 m du sol ; remise à zéro par `ZoneState`.

**Réseau (étape 11) :** cerveau, piqué et éclatement sur l'autorité.

## Files

| File | Role |
|---|---|
| `Assets/_Project/Scripts/Runtime/Enemies/Flyer.cs` | cerveau, vol, piqué, éclatement |
| `Assets/_Project/Scripts/Runtime/Settings/FlyerSettings.cs` | réglages (`Flyer_Default`) |
| `Assets/_Project/Scripts/Runtime/Orbs/Grabbable.cs`, `TelekinesisOrb.cs`, `TelekinesisHolder.cs` | prise à tout moment, libération au bout de 2 s |
| `Assets/_Project/Scripts/Runtime/Combat/EnemyBody.cs` | chute étourdie, dégâts contre un mur, reprise du vol |
| `Assets/_Project/Scripts/Runtime/Combat/Hitbox.cs` | zone de touche des ailes (déclencheur) : balles et projectiles |
| `Assets/_Project/Scripts/Runtime/Combat/Damageable.cs`, `ImpactDamager.cs`, `Artifacts/BarrelShooter.cs` | mort au premier impact de projectile (`ProjectileDamage`, jamais pour une zone) |
| `Assets/_Project/Tests/EditMode/EnemyTests.cs`, `CombatTests.cs` | valeurs, éclatement, prise à tout moment, scène |

## Build steps

1. **Volant** — fait (2026-11-13, D-040), playtest en attente.
2. **Corrections sans playtest** (2026-11-13) — mort au premier impact de projectile, zone de touche des ailes, piqué à visée
   corrigée (80°/s, sans avance) ; playtest en attente.
