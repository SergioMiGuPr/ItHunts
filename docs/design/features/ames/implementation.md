# Âmes et économie : implementation

**Updated:** 2026-11-13 · [Design](design.md) · [Testing](testing.md)

## Approach

- **Calculs purs :** `SoulMath` (part prise dans les 5 s, âmes à la mort ×2 en Enragé, destinataires en coop, prix, tirage
  avec la seed, emplacement du marchand, clignotement) : testés en EditMode.
- **État de la run :** `RunState` (objet de la scène) tient, par joueur, les âmes et les améliorations prises ; `TrySpend`
  refuse (événement `Refused` : HUD en rouge, son) sans rien retirer ; `DrawUpgrades` tire avec la seed et un numéro de tirage.
- **Éclats :** `SoulSystem` (objet de la scène, marqueur « ItHunts.Souls ») écoute `Damageable.Died` ; un ennemi qui a une
  valeur et un dernier attaquant (`Damageable.NoteAttacker`, posé par les balles, les objets lancés ou repoussés et leur zone,
  la répulsion, la prise et le lancer d'un ennemi, l'éclatement d'un volant, un lustre décroché) récent donne ses éclats à
  chaque joueur vivant à moins de 40 m. Éclats en données (pas d'objets) : chute, attraction par l'orbe du propriétaire,
  durée de vie ; dessinés par `Graphics.RenderMeshInstanced` pour la caméra de leur joueur seulement
  (`RenderPipelineManager.beginCameraRendering`).
- **Interaction :** `PlayerInteractor` (ajouté au joueur par `PlayerInputReader`) choisit l'objet visé parmi `Interactables`
  (`IInteractable` : coffre, `OfferOrb`, porte) ; action `Interact` (E / Y) ; F sur une orbe la prend au lieu de la
  télékinésie ; Y visant un objet n'envoie pas la recharge du barillet.
- **Coffre :** `Chest` (collider sur la racine) : seulement E / Y ; ouvert : 3 `OfferOrb` (réservées à celui qui l'a ouvert, 30 s).
- **Prix :** `Prices` et `RunState.TryBuy` : prix de base monté de x % (composé) par achat payant du joueur dans la catégorie
  (`PriceKind` : coffre, marchand, porte ; `EconomySettings.chestPriceIncrease`, `merchantPriceIncrease`, `doorPriceIncrease`).
- **Marchand :** `Merchant` (emplacements, offres `OfferOrb` avec prix, soin) ; **porte :** `LockedDoor`.
- **Textes en monde :** `WorldLabel` (TextMesh, police intégrée, face à la caméra principale).
- **Remise à zéro de la zone :** éclats effacés, coffres refermés, porte reverrouillée, marchand replacé.

**Réseau (étape 11) :** création, attribution et collecte des éclats, paiements et choix sur l'autorité (âmes individuelles :
personne ne vole celles d'un autre).

## Files

| File | Role |
|---|---|
| `Assets/_Project/Scripts/Runtime/Settings/EconomySettings.cs` | réglages (`Economy_Default`) |
| `Assets/_Project/Scripts/Runtime/Economy/SoulMath.cs` | calculs purs |
| `Assets/_Project/Scripts/Runtime/Economy/Prices.cs` | prix d'un achat pour un joueur (hausse à chaque achat) |
| `Assets/_Project/Scripts/Runtime/Economy/SoulSystem.cs` | éclats, collecte, rendu par joueur |
| `Assets/_Project/Scripts/Runtime/Economy/Interactables.cs`, `PlayerInteractor.cs` | interaction E / Y |
| `Assets/_Project/Scripts/Runtime/Economy/Chest.cs`, `OfferOrb.cs`, `Merchant.cs`, `LockedDoor.cs`, `WorldLabel.cs` | coffres, offres, marchand, porte, textes |
| `Assets/_Project/Scripts/Runtime/Artifacts/RunState.cs` | âmes et améliorations par joueur |
| `Assets/_Project/Scripts/Runtime/Combat/Damageable.cs` | `Died`, `NoteAttacker` |
| `Assets/_Project/Scripts/Runtime/Hud/PlayerHud.cs` | compteur d'âmes, refus en rouge, aide |
| `Assets/_Project/Input/PlayerControls.inputactions`, `Scripts/Runtime/Input/PlayerInputReader.cs` | action `Interact` (E / Y) |
| `Assets/_Project/Scripts/Editor/TestArenaSceneBuilder.cs` | coffres, marchand, salle verrouillée, système d'âmes |
| `Assets/_Project/Tests/EditMode/EconomyTests.cs` | tests |

## Build steps

1. **Âmes, coffres, marchand, salle verrouillée** — fait et validé en playtest (2026-11-13, D-043) ; coffre seulement avec E / Y, hausse du prix à chaque achat.
