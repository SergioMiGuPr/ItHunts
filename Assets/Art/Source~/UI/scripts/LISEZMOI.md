# Scripts de découpe de l'UI (lot A : HUD)

Lancés depuis un dossier de travail contenant `uilib.py` (sorties dans `out/`), maquettes lues dans `docs/art/ui/maquettes/`.

1. `python icones.py <ui_icones_minicarte_v01.png> out '<seuils JSON>'` : détourage des icônes de mini-carte
   (seuils utilisés : `{"Porte_Verrouillee":26,"Joueur_Rouge":110,"Joueur_Vert":110,"Joueur_Orange":110,"Joueur_Bleu":110,"Flamme":115,"Coffre_Normal":75,"Coffre_Rare":80}` ;
   crâne et bougie : seuil sur la saturation, dans le script).
2. `python export_icones.py` : icônes en 96 px (pastilles joueurs 48 px), porte verrouillée éclaircie.
3. `python cadres.py` : barre de vie, cartouche des âmes, mini-carte, flamme ; écrit `out/mesures.json`.
4. Copier `out/HUD/*` et `out/MiniCarte/*` dans `Assets/Art/UI/`, puis dans Unity :
   `ImportUI.Run` (import, bordures, atlas) et `SceneHUD.Run` (scène `Test_UI_HUD`, captures).
5. `python planche_A.py` : planche `docs/art/rendus/UI/LotA_HUD_avant_apres.jpg`.

## Lot B (fonds de menu)

- `lotb_decoupes.py` : découpes des images générées (ciel, lune et halo, nuages, brume, feu, sièges, campement, château et masque des fenêtres) et entrée de B2 (`docs/art/ui/entrees/entree_raccord.png`). `python lotb_decoupes.py [étape …]`.
- `planche_B.py` : planche de contrôle `docs/art/rendus/UI/LotB_decoupes.jpg`.
