# Artefacts : testing

**Updated:** 2026-11-12 · [Design](design.md) · [Implementation](implementation.md)

## Acceptance criteria

- [x] Tirage : même seed, même tirage ; aucun doublon tant que le pool suffit (200 seeds, 4 joueurs pour 4 artefacts, 2 à 4 joueurs pour 6) ; l'artefact d'un joueur ne dépend pas du nombre de joueurs ; chaque artefact sort en premier pour au moins une seed. *(`Assets/_Project/Tests/EditMode/ArtifactTests.cs` › Tirage_MemeSeedMemeResultat_SansDoublonTantQueLePoolSuffit)*
- [x] Pool trop petit : pool d'un seul artefact, 2 à 4 joueurs simulés ont tous le barillet ; pool de 2 pour 3 ou 4 joueurs, de 3 pour 4 : chacun au plus 2 fois ; pool vide : aucun artefact. *(ArtifactTests › Tirage_PoolTropPetit_DoublonsRepartisAuMieux_PoolVide, EtatDeLaRun_QuatreJoueursSimules_PoolDUnSeulArtefact)*
- [x] Barillet : 6 coups, 0.25 s entre deux tirs, rechargement automatique de 1.2 s après le 6e, 9 balles de 12 pour 100 PV en 2.95 s. *(ArtifactTests › Barillet_SixCoups_Cadence025_RechargementAuto12s_TempsPourTuer)*
- [x] R : seulement s'il manque une munition ; pas de tir pendant le rechargement ; clic un peu trop tôt mémorisé, trop tôt perdu. *(ArtifactTests › Barillet_RSeulementSIlManqueUneMunition_PasDeTirPendantLeRechargement, Barillet_ClicUnPeuTropTot_PartDesQuePossible_TropTot_Perdu)*
- [x] Objet tenu : le clic du lancer ne tire jamais (ni ce tick ni après) ; barillet inactif ; rechargement annulé, repris à zéro au relâchement ; réglage : il se termine. *(ArtifactTests › Barillet_ObjetTenu_Inactif_ClicDuLancerJamaisUnTir_RechargementAnnule)*
- [x] Tir instantané : exactement sous le viseur, portée 60 m, arrêté par un mur, le tireur ignoré, traqueur insensible. *(ArtifactTests › Tir_InstantaneDepuisLesYeux_Portee60_MurBloque_TraqueurInsensible_TireurIgnore)*
- [x] Tir ami : désactivé par défaut ; jamais soi-même ; un objet du monde ne blesse pas un joueur ; balles (12) ; objet lancé (18) et repoussé (×0.5 : 9, inoffensif sous 6 m/s) au nom du dernier joueur. *(ArtifactTests › TirAmi_JamaisSoiMeme_AlliesSeulementSiActive_MondeJamais, TirAmi_Balles, TirAmi_ObjetsLancesEtRepousses_AuNomDuDernierJoueur)*
- [x] Réglages par défaut, prefab (porte-artefact, barillet dans la main gauche, 6 chambres, PV du joueur, pas une cible du monde), occlusion ambiante coupée pour les mains. *(ArtifactTests › Reglages_PrefabEtRendu)*
- [x] En Play Mode, vrais clics, pas de tir : **à l'arrêt**, 2/2 touchés à 10, 30 et 60 m (59.7 m), écart vu / balle 0 cm ; **temps pour tuer** un factice de 100 PV : **2.950 s, 9 balles** (6 coups, recharge, 3 coups) ; **cadence réelle 0.250 s** (min et max), recharge mesurée 1.200 s ; **coût** : simulation 0.003 ms par image en moyenne (0.19 ms au plus, à l'image d'un tir), vue 0.012 ms (0.10 ms au plus). *(Play Mode, 2026-11-12 ; captures : barillet plein, traînée à 30 m, rechargement)*
- [x] En Play Mode, **bunny hop à 18 m/s** de côté, visée sur la cible à chaque image : **13/13 touchés à 10 m, 18/18 à 30 m, 12/12 à 60 m** (6 tirs écartés : cible au-delà de 60 m pendant la traversée) ; écart entre le rayon vu et la balle : **10 cm en moyenne, 20 cm au plus** (rayon des cibles : 40 cm). *(Play Mode, 2026-11-12, capture)*
- [x] En Play Mode, **2 joueurs** : P2 a lui aussi le barillet (pool d'un seul) ; balle sur P2 : 100 → 100 PV tir ami désactivé, 100 → 88 activé ; caisse de 15 kg lancée à 20 m/s : 0 puis 18 PV ; caisse de 25 kg repoussée par une vraie répulsion : 6.8 PV (×0.5) ; P1 jamais blessé. Objet tenu : barillet inactif (grisé), le clic lance l'objet sans tirer, le clic suivant tire. *(Play Mode, 2026-11-12, capture de l'objet tenu)*
- [x] Ressenti : barillet, rechargement, prise pendant un rechargement, précision. *(playtest de l'utilisateur, 2026-11-12)*
- [ ] Traqueur : tue en un coup quels que soient les PV du joueur (test préparé, ignoré tant que le traqueur n'existe pas). *(étape 6)*

## Playtest — barillet (checklist F12)

F12 : zone Télékinésie ; le **pas de tir** est juste au sud (marque au sol en (−95, −34), cibles à 10, 30 et 60 m vers
l'est). F3 trois fois : page 3, ligne « Artefact » (coups, rechargement, inactif, seed) et « Dernier tir ».

1. **Barillet :** dans la main gauche, 6 chambres allumées sur la face arrière ; rien ne cache le viseur.
2. **Tir :** clic gauche / RT sans objet : traînée dorée brève du canon jusqu'au point touché, éclat, chiffre « 12 »
   sur la cible, recul de la main, le barillet tourne d'un cran, une chambre s'éteint, un segment sous le viseur aussi.
3. **Cadence :** clique aussi vite que possible : un tir toutes les 0.25 s, pas plus ; un clic juste trop tôt part
   quand même.
4. **Rechargement automatique :** après le 6e tir, les chambres se rallument une à une (1.2 s), sons de rechargement ;
   pas de tir pendant ce temps.
5. **R / Y :** après 2 ou 3 tirs, R recharge ; barillet plein : R ne fait rien.
6. **Temps pour tuer :** une cible de 100 PV tombe en 9 balles (≈ 3 s avec le rechargement).
7. **Portée :** la cible à 60 m est touchée depuis la marque ; un pas en arrière, elle ne l'est plus.
8. **En mouvement :** en courant, en bunny hop, en traction : la balle part sous le viseur.
9. **Avec l'orbe :** attrape une caisse (F) : barillet grisé et abaissé, munitions grises ; clic gauche : la caisse
   part, **pas de tir** ; clic suivant : tir. Prends une caisse pendant un rechargement : il s'annule, et reprend au
   relâchement si le barillet est vide.
10. **Traqueur :** pas encore là (étape 6) ; il ignorera les balles.
11. **Tir ami (avec un 2e joueur, étape 9) :** dans `Coop_Default`, `Friendly Fire` décoché : rien ne blesse l'autre
    joueur ; coché : balles, caisses lancées et repoussées le blessent (barre de PV en bas à gauche) ; jamais
    soi-même.
12. **Seed :** chaque lancement affiche « Run : seed … » dans la console ; dans « État de la run », décocher
    `Random Seed` pour rejouer la même.

## History

- 2026-11-12 : designed (D-022).
- 2026-11-12 : système générique, barillet, tir ami ; mesures en Play Mode (D-032).
