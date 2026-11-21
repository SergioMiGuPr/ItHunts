# Effets visuels peints (lot D, 2026-11-21, branche `art-vfx`)

Remplacent les particules qui jurent avec la DA : **formes dessinées** (planches d'animation image par image), **aplats de 3
tons + trait d'encre**, **dissolution par morceaux** au lieu d'un fondu. Aucune particule réaliste, aucune texture photo.
Planche : `docs/art/rendus/VFX/Planche_VFX.jpg` (captures Unity sans bloom) ; textures : `docs/art/rendus/VFX/Textures_VFX.png`.

## Comment c'est fait

- **Textures** (`Assets/Art/VFX/Textures/`, générées par `Assets/Art/Source~/VFX/scripts/vfx_textures.py`, sans IA) : elles
  codent la **forme**, pas la couleur. R = ton (0 encre, 1/3 sombre, 2/3 moyen, 1 cœur), G = bruit de dissolution, A =
  couverture. Données linéaires (sRGB décoché), BC7, mipmaps.
- **Shader** `ItHunts/VFX/Peint` (`Assets/Art/VFX/Shaders/`) : rampe en aplats (`_Encre`, `_Ton1`, `_Ton2`, `_Ton3` en HDR),
  `_Intensite` (les tons clairs passent au-delà de 1 pour le bloom, l'encre reste noire), `_EncreForce`, `_Defilement` (uv
  par seconde), `_Coupe` ; mélange alpha ou additif (`_Additif`). **La couleur de sommet teinte la rampe et son alpha dissout la
  forme** (une particule « meurt » en se défaisant par morceaux). Brouillard URP respecté.
- **Matériaux** (`Assets/Art/VFX/Materiaux/`) : un par famille de couleurs (feu, feu de la lanterne, âme, fumée de goule, de
  volant, poussière, parade, liens…).
- **Réglages exposés** : `VfxPeint` à la racine de chaque prefab : `teinte`, `intensite`, `echelle`, `vitesse`,
  `detruireALaFin` (effets ponctuels) ; appliqués par bloc de propriétés (les matériaux partagés ne changent pas). `Joue()` et
  `Arrete()`. Les liens ont `LienVfx` (source, cible, creux, ondulation, largeur, sens et vitesse du flux, `Ouvre` / `Ferme`),
  la trace `TraceVfx` (`Tire(a, b)`, durée 0,14 s, largeur 4,5 cm). Les modules des systèmes de particules restent réglables
  dans le prefab.

## Prefabs (`Assets/Art/VFX/Prefabs/`) et où les brancher sur `main`

| Prefab | Effet | Type | Remplace / se branche sur |
|---|---|---|---|
| `VFX_Torche_Feu` | flammes dessinées en boucle (7/s, 0,32 à 0,46 m) + braises | boucle | flammes des torches murales et des feux (`KitSet` torche, `AmbianceLight.Kind.Torche`) |
| `VFX_Lanterne_Jet` | cône de flammes rouge-orangé le long de +Z (9 à 12 m/s, flammes qui gonflent × 4) + étincelles | boucle tant que le jet dure | jet de la lanterne (`FireLanternView`), poser à `FX_Jet` ; `Arrete()` à la fin du jet |
| `VFX_Etincelles` | gerbe de 12 étincelles losanges étirées, gravité | ponctuel | impacts (objets lancés, balles sur un mur) |
| `VFX_Barillet_Tir` | éclat de bouche (2 × 2 images, 0,08 s) + 5 étincelles | ponctuel | tir du barillet (`BarrelView`), à la bouche du canon |
| `VFX_Barillet_Trace` | bande effilée qui se défait de l'arrière vers l'avant (0,14 s) | `TraceVfx.Tire(bouche, impact)` | `BarrelView.tracer` (le `LineRenderer` actuel) |
| `VFX_Ame` | petite flamme gris-bleu en boucle + volutes | boucle | âmes au sol et aspirées (`SoulSystem`) |
| `VFX_Mort_Goule` | 4 bouffées d'encre violacées (1,1 à 1,6 m) + débris rouge sombre | ponctuel | mort d'une goule |
| `VFX_Mort_Volant` | 3 bouffées bleutées + plumes d'encre | ponctuel | mort (éclatement) d'un volant (`Flyer`) |
| `VFX_Etourdissement` | 4 étoiles jaunes qui tournent (rayon 0,35 m) | boucle, repère local | au-dessus de la tête d'un ennemi étourdi (`AreaEffects` : durée de l'étourdissement) |
| `VFX_Parade_Eclair` | éclat blanc-bleu + anneau qui s'ouvre (× 4 en 0,28 s) + étincelles | ponctuel | contre réussi (parade, nouvelle mécanique) |
| `VFX_Lien_Drain` | bande ondulée rouge sang, le flux va **de l'ennemi vers le hibou** | `LienVfx` (source = livre, cible = ennemi) | livre du hibou : drain |
| `VFX_Lien_Soin` | bande ondulée bleu-vert, le flux va **du hibou vers l'allié** | `LienVfx` (source = livre, cible = allié) | livre du hibou : soin |
| `VFX_Poussiere_Atterrissage` | anneau de 12 bouffées de poussière (rayon 2,4 m, 1,8 à 3 m) qui s'écartent + gravats | ponctuel, au sol | atterrissage d'un bond du traqueur (échelle 14 m ; `echelle` pour les autres créatures) |

`Burning` (brûlure par la lanterne) peut prendre `VFX_Torche_Feu` à `echelle` 0,6 au lieu de ses particules créées par code.

## Coût mesuré

Mesure dans l'éditeur (`VfxBuilder.Mesure`) : chaque effet seul, simulé à 60 images/s sur sa durée ; temps de simulation
moyen par image (processeur de l'éditeur, indicatif) ; mémoire des textures (BC7 + mipmaps, partagées entre effets : la planche
de flammes sert aux torches, à la lanterne et aux âmes).

| Effet | Systèmes / lignes | Matériaux | Particules max | Durée simulée (s) | Simulation (µs par image, moyenne) | Textures (Kio) |
|---|---|---|---|---|---|---|
| `VFX_Ame` | 2 / 0 | 1 | 8 | 2,2 | 3,2 | 683 |
| `VFX_Barillet_Tir` | 2 / 0 | 2 | 6 | 2,0 | 1,8 | 183 |
| `VFX_Barillet_Trace` | 0 / 1 | 1 | ligne (≤ 64 points) | 0,0 | 0,0 | 22 |
| `VFX_Etincelles` | 1 / 0 | 1 | 12 | 2,0 | 1,0 | 11 |
| `VFX_Etourdissement` | 1 / 0 | 1 | 4 | 2,0 | 1,5 | 43 |
| `VFX_Lanterne_Jet` | 2 / 0 | 2 | 26 | 2,0 | 4,3 | 695 |
| `VFX_Lien_Drain` | 0 / 1 | 1 | ligne (≤ 64 points) | 0,0 | 0,0 | 43 |
| `VFX_Lien_Soin` | 0 / 1 | 1 | ligne (≤ 64 points) | 0,0 | 0,0 | 43 |
| `VFX_Mort_Goule` | 2 / 0 | 2 | 14 | 2,0 | 1,9 | 695 |
| `VFX_Mort_Volant` | 2 / 0 | 1 | 11 | 2,1 | 2,0 | 683 |
| `VFX_Parade_Eclair` | 3 / 0 | 2 | 12 | 2,0 | 2,6 | 215 |
| `VFX_Poussiere_Atterrissage` | 2 / 0 | 2 | 22 | 2,7 | 2,2 | 695 |
| `VFX_Torche_Feu` | 2 / 0 | 2 | 9 | 2,1 | 2,9 | 695 |

Lecture : **une passe de rendu par système ou ligne** (pas de regroupement entre systèmes de particules) ; aucun effet ne dépasse
27 particules ; les textures partagées tiennent en ~2,2 Mio en tout (flamme, fumée, poussière 683 Kio chacune ; les autres moins
de 100 Kio). Surcouche (overdraw) : les flammes et la poussière sont les plus grandes à l'écran ; la couverture est nette (pas
de bords semi-transparents larges), ce qui limite le coût du mélange. Si les torches sont nombreuses : baisser `debit` des
flammes (7/s) ou ne garder qu'une torche sur deux (déjà fait par `RandomLevel.ThinTorches`).
