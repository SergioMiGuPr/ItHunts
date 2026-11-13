# Âmes et économie : implementation

**Updated:** 2026-11-13 · [Design](design.md) · [Testing](testing.md)

## Approach

- **Calculs purs :** `SoulMath` (part prise dans les 5 s, âmes à la mort ×2 en Enragé, destinataires en coop, prix, tirage
  avec la seed, emplacement du marchand, clignotement) : testés en EditMode.
- **État de la run :** `RunState` (objet de la scène) tient, par joueur, les âmes et les améliorations prises ; `TrySpend`
  refuse (événement `Refused` : HUD en rouge, son) sans rien retirer ; `DrawUpgrades` tire avec la seed et un numéro de tirage.
- **Éclats :** `SoulSystem` (objet de la scène, marqueur « ItHunts.Souls ») écoute `Damageable.Died` ; un ennemi qui a une
  valeur et un dernier attaquant (`Damageable.NoteAttacker`, posé par les balles, les objets lancés ou repoussés et leur zone,
  la répulsion, la prise et le lancer d'un ennemi, l'éclatement d'un volant, un lustre décroché) récent fait tomber ses éclats,
  **communs**. Éclats en données (pas d'objets) : chute, attraction par l'orbe la plus proche dans son rayon (elle le garde
  jusqu'au bout), collecte (`RunState.AddSouls` pour **chaque joueur vivant**), durée de vie ; dessinés par
  `Graphics.RenderMeshInstanced` pour chaque caméra (`RenderPipelineManager.beginCameraRendering`). `Step` est public (tests).
- **Interaction :** `PlayerInteractor` (ajouté au joueur par `PlayerInputReader`) choisit l'objet visé parmi `Interactables`
  (`IInteractable` : coffre, `OfferOrb`, porte) ; action `Interact` (E / Y) ; F sur une orbe la prend au lieu de la
  télékinésie ; Y visant un objet n'envoie pas la recharge du barillet.
- **Coffre :** `Chest` (collider sur la racine) : seulement E / Y, **une ouverture par joueur** (dictionnaire joueur →
  ouverture : ses 3 `OfferOrb`, réservées et visibles par lui seul via `ViewerOnly`, 30 s) ; couvercle et texte par caméra
  (`WorldLabel.TextFor`, rendu du couvercle coupé pour la caméra de qui l'a ouvert).
- **Prix :** `Prices` et `RunState.TryBuy` : prix de base monté de x % (composé) par achat payant du joueur dans la catégorie
  (`PriceKind` : coffre, marchand, porte ; `EconomySettings.chestPriceIncrease`, `merchantPriceIncrease`, `doorPriceIncrease`).
- **Marchand :** `Merchant` (emplacements, offres `OfferOrb` avec prix, soin) ; **porte :** `LockedDoor`.
- **Par caméra :** `WorldLabel` (TextMesh, police intégrée) se tourne vers chaque caméra et peut changer de texte et de
  visibilité selon son joueur (`Interactables.PlayerOfCamera`) ; `ViewerOnly` réserve le rendu d'un objet à un joueur.
- **Remise à zéro de la zone :** éclats effacés, coffres refermés, porte reverrouillée, marchand replacé.

**Réseau (étape 11) :** création et collecte des éclats, gain pour chaque joueur, paiements, ouvertures de coffre et choix
sur l'autorité ; les orbes de choix n'existent que chez leur joueur.

## Files

| File | Role |
|---|---|
| `Assets/_Project/Scripts/Runtime/Settings/EconomySettings.cs` | réglages (`Economy_Default`) |
| `Assets/_Project/Scripts/Runtime/Economy/SoulMath.cs` | calculs purs |
| `Assets/_Project/Scripts/Runtime/Economy/Prices.cs` | prix d'un achat pour un joueur (hausse à chaque achat) |
| `Assets/_Project/Scripts/Runtime/Economy/SoulSystem.cs` | éclats communs, collecte, gain pour chaque joueur vivant |
| `Assets/_Project/Scripts/Runtime/Economy/Interactables.cs`, `PlayerInteractor.cs` | interaction E / Y |
| `Assets/_Project/Scripts/Runtime/Economy/Chest.cs`, `OfferOrb.cs`, `Merchant.cs`, `LockedDoor.cs`, `WorldLabel.cs`, `ViewerOnly.cs` | coffres par joueur, offres, marchand, porte, textes et rendu par caméra |
| `Assets/_Project/Scripts/Runtime/Artifacts/RunState.cs` | âmes et améliorations par joueur |
| `Assets/_Project/Scripts/Runtime/Combat/Damageable.cs` | `Died`, `NoteAttacker` |
| `Assets/_Project/Scripts/Runtime/Hud/PlayerHud.cs` | compteur d'âmes, refus en rouge, aide |
| `Assets/_Project/Input/PlayerControls.inputactions`, `Scripts/Runtime/Input/PlayerInputReader.cs` | action `Interact` (E / Y) |
| `Assets/_Project/Scripts/Editor/TestArenaSceneBuilder.cs` | coffres, marchand, salle verrouillée, système d'âmes |
| `Assets/_Project/Tests/EditMode/EconomyTests.cs` | tests |

## Build steps

1. **Âmes, coffres, marchand, salle verrouillée** — fait et validé en playtest (2026-11-13, D-043) ; coffre seulement avec E / Y, hausse du prix à chaque achat.
2. **Après le playtest (partie 2)** — fait (2026-11-13), playtest en attente : âmes gagnées en commun (D-007 révisée), coffres par joueur.
