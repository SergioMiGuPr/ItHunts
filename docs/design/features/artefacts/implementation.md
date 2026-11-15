# Artefacts : implementation

**Updated:** 2026-11-12 · [Design](design.md) · [Testing](testing.md)

## Approach

- **Tirage :** `ArtifactDraw.Sequence(seed, taille du pool, joueurs)` (pur) enchaîne des tours complets du pool
  mélangés (Fisher-Yates, générateur SplitMix32 maison : même résultat sur toutes les plateformes). `RunState` (objet
  « État de la run » de la scène, pas le joueur) tient la seed (au hasard au lancement, ou fixée) et le pool ;
  `ArtifactFor(index du joueur)`.
- **Porte-artefact :** `ArtifactHolder` sur le prefab du joueur ; au démarrage, il demande son artefact au `RunState`
  de la scène (sinon l'artefact par défaut). `PlayerMotor` l'appelle à chaque tick **après la télékinésie**, avec la
  même `MovementInput` et deux drapeaux : l'orbe tenait-elle un objet au début du tick, en tient-elle un maintenant.
- **Barillet :** `Barrel.Step` (pur) sur `BarrelState` (copiable, ticks) : appuis mémorisés, cadence, rechargement,
  inactif quand l'orbe tient un objet. Sur un tir, `BarrelShooter.Fire` lance le rayon depuis les yeux au début du tick
  (comme le lancer, D-027) : premier collider touché hors tireur et déclencheurs ; dégâts par `Damageable.ApplyDamage`
  et `DamageEvents` (chiffres, éclat, son) ; `BarrelEvents.Shot`.
- **Entrées :** action `Reload` (R / Y) dans `PlayerControls`, `MovementInput.ReloadPressedTick` ; le clic gauche
  reste `ThrowPressedTick` (lancer ou tir).
- **Vue :** `BarrelView` (chambres par `MaterialPropertyBlock`, rotation d'un cran par tir, traînée `LineRenderer` dont
  le départ suit le canon à chaque image, couleur HDR par le matériau ; éclat ; sons `ProceduralSfx`) ;
  `FirstPersonView` : recul et pose inactive de la main gauche, `ViewToWorld` (point des mains vu à 60° → monde).
  HUD : `PlayerHud.UpdateAmmo` (segments, HUD discret), PV du joueur.
- **Tir ami :** `FriendlyFire.Allows` (pur) ; `PlayerHealth` sur le joueur (pas un `Damageable` : les ennemis, la
  répulsion et la zone ne le visent pas) ; `Grabbable.LastOwnerId` (posé à la prise par `TelekinesisHolder`, à la
  poussée et au décrochage par `RepulsionWave`). Unity n'envoie pas `OnCollisionEnter` à un objet qui percute un
  `CharacterController` : `ImpactDamager.FixedUpdate` teste le contact des objets dangereux avec les autres joueurs
  (12 cm), vitesse d'impact = vitesse au pas précédent ; `ImpactDamager.HurtPlayer`, zone dans `ApplyArea`.
- Marqueurs de profilage : `ItHunts.Artifact` (simulation et tir), `ItHunts.BarrelView`.

**Réseau (étape 11) :** `BarrelState` est copiable et calculé à pas fixe depuis l'entrée (prédiction possible) ; le
rayon sera rejoué par le serveur avec compensation de latence ; la seed suffit à reproduire le tirage. La détection
maison du contact objet-joueur (`ImpactDamager.FixedUpdate`) et les blessures du tir ami devront tourner **côté
autorité réseau** (là où la physique des objets fait autorité), jamais chez chaque client.

## Personnages et lanterne de feu (D-064, D-065)

- `CharacterDefinition` (un asset par personnage : nom, couleur, artefact, jouable) et `CharacterRoster`
  (`Settings/Personnages/Characters_Default`, ordre du menu, personnage par défaut). Référencé par `Run_Default` (menu) et par
  l'état de la run (`RunState.characters`) de chaque scène.
- `CharacterPick` (logique pure) : choix gardés valides, jamais deux fois le même personnage, défilement du menu.
  `RunSession.Characters` garde le choix de chaque joueur d'une scène à l'autre ; `RunState.CharacterFor` /
  `ArtifactFor(playerIndex)` en déduisent le personnage et l'artefact ; sans liste de personnages, l'ancien tirage (D-032).
- `FireLanternSettings` (`FireLantern_Default`) ; `FireLantern` (état `LanternState` et tick, logique pure : jet, recharge,
  clic mémorisé, objet tenu) ; `FireLanternJet.Sweep` (cibles du cône à chaque tick du jet, ligne de vue contre le décor
  seulement, une touche par cible et par jet) ; `ArtifactHolder` fait tourner l'artefact du personnage.
- `FireLanternView` (lanterne en main, flamme, bouffées, sons) ; `FirstPersonView` (main gauche) ; `PlayerHud` (un segment) ;
  debug (F12 : état de la lanterne, cibles du dernier jet, personnage) ; `ArtifactMerchant` (améliorations du barillet
  seulement pour le barillet).
- Éditeur : *ItHunts > Personnages : créer les assets, poser la lanterne, brancher les scènes* (`CharacterSetup`), aussi appelé
  par les constructeurs du prefab Player et des scènes.
- Réseau (étape 11) : l'état de la lanterne est une structure copiable, le cône est à rejouer par l'autorité (comme le tir
  instantané du barillet).

## Files

| File | Role |
|---|---|
| `Assets/_Project/Scripts/Runtime/Settings/ArtifactDefinition.cs`, `ArtifactPool.cs`, `BarrelSettings.cs` | artefact, pool, réglages du barillet |
| `Assets/_Project/Scripts/Runtime/Settings/CoopSettings.cs`, `PlayerHealthSettings.cs` | tir ami, PV du joueur |
| `Assets/_Project/Scripts/Runtime/Artifacts/ArtifactDraw.cs` | tirage par la seed (pur) |
| `Assets/_Project/Scripts/Runtime/Artifacts/RunState.cs` | état de la run : seed, artefact de chaque joueur |
| `Assets/_Project/Scripts/Runtime/Artifacts/ArtifactHolder.cs` | emplacement d'artefact du joueur |
| `Assets/_Project/Scripts/Runtime/Artifacts/Barrel.cs` | logique du barillet (pure) |
| `Assets/_Project/Scripts/Runtime/Artifacts/BarrelShooter.cs` | tir instantané |
| `Assets/_Project/Scripts/Runtime/Artifacts/BarrelView.cs` | retour visuel et sonore |
| `Assets/_Project/Scripts/Runtime/Combat/FriendlyFire.cs`, `Player/PlayerHealth.cs` | tir ami, PV du joueur |
| `Assets/_Project/Tests/EditMode/ArtifactTests.cs` | 12 tests |
| `Assets/_Project/Tests/EditMode/StalkerTests.cs` | traqueur : tue en un coup quels que soient les PV (préparé, ignoré jusqu'à l'étape 6) |

Réglages : `Barrel_Default`, `ArtifactPool_Default`, `Coop_Default`, `PlayerHealth_Default`. Scène : « État de la
run », pas de tir (cibles à 10, 30 et 60 m au sud de la zone Télékinésie).

## Build steps

1. **Système générique, barillet, tir ami** — fait et validé en playtest (2026-11-12, D-032).
