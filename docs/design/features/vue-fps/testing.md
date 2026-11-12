# Vue FPS et HUD : testing

**Updated:** 2026-11-12 · [Design](design.md) · [Implementation](implementation.md)

## Acceptance criteria

- [x] L'intensité du mouvement des mains agit sur le balancement, le balancier et la plongée (50 % : moitié, 0 % : rien), pas sur les gestes ni les poses. *(`Assets/_Project/Tests/EditMode/FpsViewTests.cs` › Intensite_AgitSurBalancementBalancierPlongee, Intensite_SansEffetSurLesGestesNiLesPoses)*
- [x] Balancier : rien à l'arrêt ni en l'air, 1.8 pas/s à 7.5 m/s, borné au-delà. *(FpsViewTests › Balancier_RienAlArretNiEnLAir_PlusRapideEnCourant)*
- [x] HUD discret : pleine et inutilisée → 40 % après 1.5 s ; usage, recharge, refus → 100 % tout de suite ; une charge qui revient clignote une fois. *(FpsViewTests › HudDiscret_PleineEtInutiliseeSEstompe_UsageEtRefusRemontentToutDeSuite, HudDiscret_UneChargeQuiRevientClignoteUneFois)*
- [x] Plafond du bonus de vitesse au lancer : 20 m/s. *(FpsViewTests › Lancer_BonusDeVitessePlafonne)*
- [x] Immunité de 2 s après la fin d'un étourdissement. *(FpsViewTests › Etourdissement_ImmunitéDe2sApresLaFin)*
- [x] Couches `FirstPersonP1` à `P4` (8 à 11) ; réglages par défaut. *(FpsViewTests › Couches_DesMains_UneParJoueur, Reglages_ParDefaut)*
- [x] Option 2 en place : pas de caméra des mains dans le prefab (une seule caméra par joueur), orbe de 0.08 m dans la main droite, HUD uGUI branché, HUD provisoire retiré ; passes des mains (profondeur effacée, mains à 60°, HUD) dans `PC_Renderer` et `Mobile_Renderer`, couches des mains exclues des passes normales. *(FpsViewTests › Prefab_MainsParPasseDeRendu_OrbeDansLaPaumeDroite_HudUgui, Renderers_PassesDesMains_ProfondeurEffaceeMainsA60_HUD)*
- [x] En Play Mode (option 1, même géométrie qu'en option 2) : mains à 60° ; **couverture de l'écran par les mains au repos : 4.8 %** (objectif < 12 %), viseur et anneaux dégagés ; FOV de vitesse 80.8° : 4.7 % (même taille) ; collé à un mur (yeux à 0.9 m) : mains entières ; HUD discret : 40 % au repos après 2.2 s, 100 % juste après un dash ; geste de répulsion à l'image même où l'état simulé change. *(Play Mode, 2026-11-12 ; captures : repos, course, glissade, vitesse, mur, objet tenu, traction en tenant un objet, répulsion)*
- [x] Deux joueurs côte à côte : P2 sur `FirstPersonP2` ; chaque joueur ne voit que ses mains ; chaque HUD centré dans sa moitié. *(Play Mode, 2026-11-12, capture)*
- [x] Option 2, même rendu que l'option 1 : mêmes tailles, mains entières collées à un mur, HUD par-dessus. *(Play Mode, 2026-11-12, captures)*
- [x] **Coût par image, mesuré dans un build de développement Windows** (1920 × 1080, RTX 3050, i5-10400F ; médianes sur 30 s par état, mains et HUD allumés / éteints en blocs de 5 s ; `PerfProbe`). Règle : surcoût < 0.5 ms à 1 joueur, < 1.5 ms à 2 joueurs. *(build, 2026-11-12)*

  | Surcoût | Fil principal | Fil de rendu | GPU | Total |
  |---|---|---|---|---|
  | Option 1 (caméra superposée), 1 joueur | +1.04 ms | +0.56 ms | +0.56 ms | +1.60 ms ✗ |
  | Option 1, 2 joueurs | +6.27 ms | +1.15 ms | +0.92 ms | +7.19 ms ✗ |
  | **Option 2 (passe de rendu), 1 joueur** | **+0.21 ms** | **+0.11 ms** | **+0.06 ms** | **+0.26 ms ✓** |
  | **Option 2, 2 joueurs** | **+0.44 ms** | **+0.16 ms** | **+0.19 ms** | **+0.63 ms ✓** |

  (Total : le plus lourd des deux fils CPU, plus le GPU.) Décision : option 2.
- [x] Ressenti : mains, gestes et HUD validés. *(playtest de l'utilisateur, 2026-11-12)*
- [x] **Barres de PV, chiffres de dégâts et durabilité collés à leur cible en mouvement.** Cause : `RectTransformUtility.ScreenPointToLocalPointInRectangle` visait le plan du canvas de l'image précédente. Corrigé : position calculée directement (écart au centre de la zone de la caméra ÷ échelle du canvas). Écart moyen avant → après : arrêt 0 → 0 px ; saut 168 px (max 978) → 0 ; bunny hop 141 px (max 46 960) → 0 ; traction 162 px (max 707) → 0 ; dash 556 px (max 5 518) → 0. *(Play Mode, 2026-11-12)*
- [x] **Occlusion ambiante (SSAO) coupée pour les mains** : la passe qui efface la profondeur coupe aussi le mot-clé de la SSAO pour la suite de l'image (la SSAO, calculée sur le décor, assombrissait les mains). Collé à un mur, au pied du mur, SSAO allumée / éteinte : pixels des mains assombris de plus de 4/255 **21.9 % → 0.1 %**, assombrissement moyen **2.87 → −0.01** /255 ; le décor la reçoit toujours (1.06 /255, inchangé). *(Play Mode, 2026-11-12, captures avant / après ; `ArtifactTests` › Reglages_PrefabEtRendu)*
- [ ] Ressenti : dégâts contre un mur, objets repoussés, rendu des mains par la passe de rendu. *(playtest)*

## Playtest — vue FPS, HUD, orbe (checklist F12)

F12 : zone Télékinésie. F3 : pages du debug (la page 3 montre l'intensité, le geste en cours et l'opacité du HUD).

1. **Mains :** deux mains en bas de l'écran, l'orbe dans la paume droite ; rien ne cache le viseur ni ses anneaux.
2. **Mouvement :** balancement quand tu tournes, balancier en courant (rien en l'air), petite plongée à l'atterrissage ; en glissade, mains plus basses et inclinées.
3. **Gestes :** bras tendu en traction, poussée à la répulsion, main qui se tend pour attraper, main relevée qui tient, lancer.
4. **Mur et vitesse :** collé à un mur, mains entières ; à pleine vitesse (FOV large), les mains gardent leur taille.
5. **HUD discret :** jauges pleines estompées après 1.5 s ; un dash, une traction, une répulsion ou un refus les ramènent tout de suite ; une charge qui revient clignote une fois.
6. **HUD :** chiffres de dégâts, barres de PV, carrés de durabilité de l'objet tenu ; temps de recharge de la répulsion.
7. **Confort :** `FirstPersonView › Hand Motion Intensity` à 0 : plus de balancement ni de balancier, les gestes restent.
8. **Ajouts :** lancer en courant à plus de 20 m/s : bonus plafonné (debug page 3) ; un factice étourdi ne peut pas l'être de nouveau pendant 2 s après la fin.
9. **Barres de PV en mouvement :** saut, bunny hop, traction, dash en regardant un factice : la barre reste collée au-dessus de lui.
10. **Répulsion, appui :** clic droit face à des caisses ou des factices : onde depuis la main droite ; jusqu'à 14 m, 30° de chaque côté, ≈ 22 m/s de près ; rien derrière un mur.
11. **Lustre :** sous le portique, à côté du factice, vise le lustre : il tombe sur le factice.
12. **Combo :** repousse un factice contre un mur (étourdi), attrape-le avec F, lance-le : dégâts pour les deux. Gardé 3 s : il se débat puis se libère ; 2 s sans reprise possible.
13. **Garde-fous :** F sur un factice non étourdi ne fait rien ; la traction refuse toujours les factices.
14. **Répulsion, maintien :** clic droit maintenu : anneaux continus, temps restant près du viseur (0.8 s), poussée sans dépasser 25 m/s, pas de nouvelle propulsion ; la recharge de 5 s part de la fin.
15. **Coupures :** pendant le maintien, F, clic gauche ou A coupent la répulsion.
16. **Mur :** un factice plaqué contre un mur est étourdi une seule fois à l'impact, puis rien tant que tu le plaques.
17. **Dégâts contre un mur (D-031) :** repousse un factice contre un mur de près : chiffre de dégâts (40 au plus), une seule fois par impact ; pendant ses 2 s d'immunité, un nouvel impact ne compte pas.
18. **Objet repoussé (D-031) :** repousse une caisse sur un factice : petit chiffre de dégâts (moitié d'un lancer), pas de cercle de zone ; un carré de durabilité en moins seulement si elle touche le factice ; une caisse lente (sous 6 m/s) ou repoussée il y a plus de 1 s ne blesse plus.
19. **Rendu des mains (passe de rendu) :** mêmes mains qu'avant, entières contre un mur, même taille à pleine vitesse.
20. **Occlusion ambiante :** collé à un mur, regard vers son pied : plus de taches sombres sur la paume ni sur l'orbe.

## History

- 2026-11-12 : vue FPS et HUD (D-028).
- 2026-11-12 : barres de PV collées à leur cible ; répulsion en cône (D-029) ; prise des ennemis étourdis (D-030).
- 2026-11-12 : répulsion façon aimant ; dégâts contre un mur et objets repoussés (D-031) ; mesure dans un build, option 2.
- 2026-11-12 : occlusion ambiante coupée pour les mains ; barillet dans la main gauche (D-032).
