# Améliorations : implementation

**Updated:** 2026-11-13 · [Design](design.md) · [Testing](testing.md)

## Approach

- **Données :** `UpgradeDefinition` (nom, description d'une ligne, icône, couleur, effet `UpgradeKind`, valeur, prix) ;
  `UpgradePool` ; assets dans `Assets/_Project/Settings/Upgrades/` et `UpgradePool_Default` (créés par le constructeur
  s'ils manquent, sinon gardés tels quels).
- **Effets :** `PlayerUpgrades` (ajouté au joueur à sa première amélioration) cumule les effets ; copies propres au joueur de
  `MovementSettings` et `MagnetismOrbSettings` (`PlayerMotor.UseSettings`) et du barillet (`ArtifactHolder.Equip`) ;
  `PlayerHealth.SetMaxHealthBonus` ; dégâts des objets lancés par `ImpactDamager.OwnerDamageMultiplier` (coup direct, objet
  repoussé, zone) ; dégâts de l'onde par `RepulsionWave.RepelDamageFor` (jamais mortels d'un coup pour un volant) ; collecte
  par `SoulSystem` (rayon, vitesse).

## Files

| File | Role |
|---|---|
| `Assets/_Project/Scripts/Runtime/Settings/UpgradeDefinition.cs`, `UpgradePool.cs` | données |
| `Assets/_Project/Scripts/Runtime/Economy/PlayerUpgrades.cs` | effets par joueur |
| `Assets/_Project/Scripts/Runtime/Player/PlayerMotor.cs`, `PlayerHealth.cs` | réglages propres, PV max, soin |
| `Assets/_Project/Tests/EditMode/EconomyTests.cs` | valeurs, tirage |

## Build steps

1. **8 premières améliorations** — fait (2026-11-13, D-043), playtest en attente.
