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


# Lot 2 des retours (2026-11-22, branche `art-effets-2`)

Même style que le lot D : formes peintes en aplats et encre, dissolution par morceaux, shader `ItHunts/VFX/Peint`. Planche :
`docs/art/rendus/VFX/Planche_VFX_Lot2.jpg` (captures Unity en vue à la première personne, sans bloom). Constructeur :
`VfxBuilder.ConstruitLot2` (ne reconstruit pas les 13 prefabs du lot D) ; captures `VfxBuilder.CaptureFps`. Nouvelle texture
`T_VFX_Arc_2x2` (arc de lame qui se déploie puis se défait, `vfx_textures.py`, sans IA).

## Prefabs et où les brancher sur `main`

| Prefab | Effet | Type | Se branche sur |
|---|---|---|---|
| `VFX_Traction` | fumée et éclats bleus qui **filent de l'orbe au point visé** pendant toute la traction (spirale légère, petits près de l'orbe) | `FluxVfx` : `source` = transform de l'orbe, `Regle(point)` à chaque image, `Ouvre()` au début de la traction, `Ferme()` à la fin (les particules parties finissent leur course) | **remplace le trait** `MagnetismOrbView.line` (désactiver le `LineRenderer`) ; l'ancre et l'éclat bleus restent possibles |
| `VFX_Repulsion` | onde courte orange (0,2 s, ~1 m) dans le plan de la surface + 6 éclats | ponctuel ; poser au point visé, rotation `LookRotation(normale)` (+Z = normale) | `MagnetismOrbView` à la répulsion (à la place ou en plus de `impactMarker`) |
| `VFX_Epee_Taille_H` | coup 1 : taille horizontale, arc dessiné qui se déploie en 0,17 s | ponctuel ; poser à la caméra (position et rotation), l'arc est à 0,9 m devant, 0,28 m sous le regard | `BoneSword`, coup d'index 0 |
| `VFX_Epee_Taille_D` | coup 2 : taille diagonale (même arc, incliné de 35°) | idem | coup d'index 1 |
| `VFX_Epee_Estoc` | coup 3 et fente : faisceau de traits vers l'avant + anneau à 1,6 m | idem | coup d'index 2 et fente (dash, glissade) |
| `VFX_Epee_Impact` | éclat d'impact **vert** + anneau + étincelles | ponctuel, au point touché | coup qui touche (`BoneSword`, chaque ennemi touché) |
| `VFX_Vitesse_Bords` | traits pâles qui filent vers les bords de l'écran | continu ; **enfant de la caméra** (position et rotation nulles) ; `VitesseVfx.Regle(vitesseHorizontale)` à chaque image | `PlayerMotor` (vitesse horizontale) ; rien sous `seuilBas` (9 m/s), maximum à `seuilHaut` (18 m/s) ; `force` pour l'accessibilité (0 = coupé) |
| `VFX_Impact_Leger` | éclat clair + 7 étincelles + 2 bouffées | ponctuel | balle du barillet sur un mur, petit objet lancé |
| `VFX_Impact_Lourd` | grand éclat + 12 étincelles + 5 bouffées + gravats | ponctuel | caisse lancée qui touche (`ImpactDamage`), chute d'un gros objet |
| `VFX_Poussiere_Joueur` | anneau de 9 bouffées au sol (rayon 0,3 m) + graviers | ponctuel, aux pieds | atterrissage du joueur ; `VfxPeint.echelle` selon la vitesse de chute (0,6 petit saut, 1 chute normale, 1,5 grande chute) |

Réglages exposés : `VfxPeint` (teinte, intensité, échelle, vitesse, destruction à la fin) sur chaque prefab ; `FluxVfx` (vitesse
16 m/s, densité 9 particules par mètre, spirale, taille près de l'orbe, débit maximal) ; `VitesseVfx` (seuils, débit,
étirement, lissage, force). Les systèmes de particules restent réglables dans les prefabs.

**Réseau** : tout est cosmétique et local ; la traction d'un autre joueur se dessine avec sa propre orbe et son point visé
(déjà répliqués pour le mouvement).

## Coût mesuré (tous les effets, `VfxBuilder.Mesure`)

Même méthode que le lot D (chaque effet seul, 60 images/s, processeur de l'éditeur, indicatif). `VFX_Traction` : 68
particules au plus pour un trajet de 8 m (débit proportionnel à la longueur, plafonné à 300/s par système). `VFX_Vitesse_Bords`
n'émet rien à l'arrêt (0 particule mesurée) : au maximum ~30 traits.

| Effet | Systèmes / lignes | Matériaux | Particules max | Durée simulée (s) | Simulation (µs par image, moyenne) | Textures (Kio) |
|---|---|---|---|---|---|---|
| `VFX_Ame` | 2 / 0 | 1 | 8 | 2,2 | 14,5 | 683 |
| `VFX_Barillet_Tir` | 2 / 0 | 2 | 6 | 2,0 | 3,9 | 183 |
| `VFX_Barillet_Trace` | 0 / 1 | 1 | ligne (≤ 64 points) | 0,0 | 0,0 | 22 |
| `VFX_Epee_Estoc` | 2 / 0 | 2 | 13 | 2,0 | 2,3 | 55 |
| `VFX_Epee_Impact` | 3 / 0 | 3 | 10 | 2,0 | 2,7 | 226 |
| `VFX_Epee_Taille_D` | 1 / 0 | 1 | 1 | 2,0 | 1,1 | 683 |
| `VFX_Epee_Taille_H` | 1 / 0 | 1 | 1 | 2,0 | 1,2 | 683 |
| `VFX_Etincelles` | 1 / 0 | 1 | 12 | 2,0 | 2,5 | 11 |
| `VFX_Etourdissement` | 1 / 0 | 1 | 4 | 2,0 | 3,6 | 43 |
| `VFX_Impact_Leger` | 3 / 0 | 3 | 10 | 2,0 | 2,8 | 866 |
| `VFX_Impact_Lourd` | 4 / 0 | 4 | 24 | 2,0 | 9,9 | 866 |
| `VFX_Lanterne_Jet` | 2 / 0 | 2 | 27 | 2,0 | 5,3 | 695 |
| `VFX_Lien_Drain` | 0 / 1 | 1 | ligne (≤ 64 points) | 0,0 | 0,0 | 43 |
| `VFX_Lien_Soin` | 0 / 1 | 1 | ligne (≤ 64 points) | 0,0 | 0,0 | 43 |
| `VFX_Mort_Goule` | 2 / 0 | 2 | 14 | 2,0 | 3,7 | 695 |
| `VFX_Mort_Volant` | 2 / 0 | 1 | 11 | 2,1 | 2,1 | 683 |
| `VFX_Parade_Eclair` | 3 / 0 | 2 | 12 | 2,0 | 2,8 | 215 |
| `VFX_Poussiere_Atterrissage` | 2 / 0 | 2 | 22 | 2,7 | 2,4 | 695 |
| `VFX_Poussiere_Joueur` | 2 / 0 | 2 | 14 | 2,0 | 2,3 | 695 |
| `VFX_Repulsion` | 2 / 0 | 2 | 7 | 2,0 | 2,3 | 55 |
| `VFX_Torche_Feu` | 2 / 0 | 2 | 8 | 2,1 | 3,8 | 695 |
| `VFX_Traction` | 2 / 0 | 2 | 68 | 2,0 | 7,1 | 695 |
| `VFX_Vitesse_Bords` | 1 / 0 | 1 | 0 | 2,0 | 1,0 | 11 |


# Lot 8 de la nuit du 2026-11-22 (branche `art-vfx-3`)

Même style que les lots D et 2 : formes peintes en aplats et à l'encre, shader `ItHunts/VFX/Peint`, dissolution par
morceaux.

**Fichiers :**
- planche `docs/art/rendus/VFX/Planche_VFX_Lot3.jpg`, une capture par effet dans `docs/art/rendus/VFX/Lot3/` (captures Unity sans bloom) ;
- constructeur `VfxBuilder.ConstruitLot3` ;
- nouvelles textures (`vfx3_textures.py`, sans IA) : `T_VFX_Eclair_2x2` (quatre éclairs ramifiés) et `T_VFX_Fissure` (étoile de fissures au sol) ;
- nouveau script `LumiereFlash` : flash ou pulsation d'une lumière ponctuelle, purement cosmétique.

| Prefab | Effet | Type | Se branche sur |
|---|---|---|---|
| `VFX_Critique` | Éclat or et rouge, anneau, 16 étincelles, flash or (4 / 6 m, 0.18 s). | ponctuel, au point touché | tir critique à la tête, avec `HUD_Critique` (lot 7) et `Sfx_Headshot_Critique` (lot 6) |
| `VFX_Balle_Paralysante` | Six éclairs cyan qui jaillissent, anneau, étincelles, flash cyan. | ponctuel, au point touché | impact de la balle paralysante |
| `VFX_Paralysie` | Boucle autour d'une cible figée : arcs électriques sur une capsule de 0.9 × 1.8 m, grains, lumière cyan qui pulse (3.5 Hz). | continu ; enfant de l'ennemi, à mettre à l'échelle de sa taille ; détruire à la fin de la paralysie | état paralysé |
| `VFX_Explosion_Kill` | Explosion de zone d'environ 4 m : éclat cramoisi de 3.2 m, onde au sol jusqu'à 8.5 m, étoile de fissures, fumée, débris, flash orange (6 / 12 m). | ponctuel, au sol | explosion de zone sur un kill |
| `VFX_Parade_Eclair_Lisible` | **Remplace `VFX_Parade_Eclair`** : étoile blanche de 1.5 m qui gonfle puis se rétracte, 5 éclairs, double anneau, étincelles, flash blanc bleuté (5 / 8 m). | ponctuel, au point de contact | parade réussie |
| `VFX_Poussiere_Lune` | Poussière blanc-gris en suspension, 260 grains, volume de 4 × 12 × 4 m. | continu, à poser dans un rayon de lune | identique à celle de `Chateau_RayonLune` (lot 3), pour d'autres puits de lumière |
| `VFX_Brume_Sol` | Brume au sol : nappes de 6 à 12 m qui dérivent sur 30 × 30 m, sous 1.2 m. | continu | jardin et arène (même matériau que `Jardin_Brume`, lot 4) |
| `VFX_Boss_Griffe` | Trois lacérations rouges de 5.5 m, débris. | ponctuel ; tourner dans le plan du coup | attaque de griffe du boss, avec `Sfx_Boss_Attaque_Griffe` |
| `VFX_Boss_Morsure` | Deux mâchoires qui se referment, éclat sombre. | ponctuel, devant la gueule | morsure, avec `Sfx_Boss_Attaque_Morsure` |
| `VFX_Boss_Impact_Sol` | Fissures de 9 m, onde, poussière, gravats. | ponctuel, au sol | coup au sol, atterrissage d'un bond du boss |
| `VFX_Boss_Blessure` | Sang noir qui gicle (gerbe en cône), gouttes qui retombent, fumée. | ponctuel ; +Z dans le sens de la blessure | boss blessé, avec `Sfx_Boss_Blessure` |
| `VFX_Boss_PointFaible` | **Boucle** : cœur ambré qui pulse (étoile de 1.7 m), anneau qui s'en échappe, braises, lumière orange qui pulse (2.6 / 7 m, 1.25 Hz). | continu ; enfant de l'os du point faible | point faible exposé ; le couper quand il est fermé |

**Coût :**
- les effets ponctuels utilisent au plus une trentaine de particules ;
- la paralysie et le point faible en gardent moins de 40 ;
- poussière et brume : 260 et 160 particules au plus, sans collision ni ombre ;
- les lumières des flashs sont sans ombre.

**Réseau :** tout est cosmétique et local.

## Nuit 4, lot 7 : marquage du boss, annonces des volants, apparitions, screamer de mort

Les captures sont faites en jeu, dans l'arène de `Acte_4_Boss` : éclairage du jeu, lanterne et mains du joueur.

- **Boss** : sur de **vraies attaques**, déclenchées dans le jeu. Les marques de main sont remplacées par les miennes, à la même place et au même instant.
- **Simulations** : les « après » du volant tenu et des apparitions, car le code de main n'est pas encore branché.

Planches dans `docs/art/rendus/Nuit4/Lot7/` :
- `Planche_Boss_<Prise|Charge|Balayage|Debris>.jpg` et `Planche_Boss_DePres.jpg` ;
- `Planche_Volant.jpg` et `Planche_Apparition.jpg` ;
- `Planche_Screamer.jpg` et `Planche_Screamer_Adouci.jpg` ;
- `Planche_Cris_Traqueur.png`.

Outil de capture : `Assets/Art/Source~/VFX/scripts/CapturesLot7.cs` (mode d'emploi en tête du fichier).

### Marquage au sol des attaques du boss (`MarquageBoss`, shader `ItHunts/VFX/MarquageSol`)

Il remplace les marques orange de `FinalBoss.Mark` (cylindre ou cube en Lit transparent additif). Ce que le joueur lit :
- **Bord net** rouge sang éteint, de **12 cm** quelle que soit la taille de la marque (au moins 1,5 px à l'écran).
- **Remplissage** rouge sang sombre et translucide. La zone à venir assombrit le sol ; la zone déjà « remplie » est un peu plus chaude.
- **Front** en dégradé doux de 1,2 m, et non un second bord.
- **Montée** d'intensité sur les 40 derniers pour cent.
- **Éclair** de 0,1 s à l'impact : le bord devient chaud et le remplissage rougeoie à peine. Puis la marque disparaît en 0,18 s.

Une seule couleur pour tout le boss : le type d'attaque se lit à la forme.

| Attaque (`FinalBoss`) | Aujourd'hui | Appel à la place (durée = `TelegraphLeft`) |
|---|---|---|
| Prise, Cri, Débris, Éruptions | `Disc(at, r)` | `MarquageBoss.Cercle(at, r, duree)` (disque plein, y compris pour le Cri : son centre n'est pas sûr) |
| Bond | `Disc(tp, leapRadius)` | `MarquageBoss.Cercle(tp, leapRadius, duree).Prolonge(0.9f)` : l'onde de choc tombe après les 0,9 s de vol, la marque reste pleine jusque-là |
| Charge | `Lane(from, dir, longueur, 7)` | `MarquageBoss.Couloir(from, dir, longueur, largeur, duree)`, puis `Annule()` quand la course s'arrête réellement (distance atteinte ou bord de l'arène à `ArenaRadius - 6.5`) |
| Balayage | `Cone(...)` (5 bandes) | `var m = MarquageBoss.Cone(from, dir, sweepRange, 45f, duree);` puis `m.Oriente(transform.forward)` à chaque image de l'annonce (le boss pivote vers la cible et frappe selon son orientation finale) |
| Invocation | `Disc(m, 2f)` | `MarquageBoss.Apparition(m, 2f, duree)` (ce n'est pas un coup : cercle plein pulsé, même matériau que les apparitions des vagues) |

**Écarts entre marques et dégâts, à régler côté main** :
- **Charge :** les dégâts touchent tout joueur à moins de 5 m du boss (couloir réel de 10 m de large), alors que la marque fait 7 m. Il faut aligner l'un sur l'autre : marque de 10 m, ou rayon de dégâts de 3,5 m.
- **Bond et Débris sur un sol en relief :** la marque est plate, à `ArenaCenter.y` + 3 cm. Sur la butte ou l'escalier, elle s'enterre ou flotte, alors que `Shockwave` touche jusqu'à 4 m plus haut. **Demande prioritaire à main :** un décalque URP (renderer feature « Decal » sur le renderer, réglage partagé) permettra de projeter le même shader sur le relief.

- **Matériau.** `Assets/Art/VFX/Resources/M_VFX_Marquage_Boss.mat`, chargé par `Resources.Load`, donc présent dans le build sans autre réglage.
  - C'est la seule source des valeurs : régler ce matériau change le jeu.
  - `MarquageBoss.Configure(mat)` permet d'en passer un autre.
  - Les valeurs propres à chaque marque passent par un `MaterialPropertyBlock` : aucune allocation de matériau.
- **Accessibilité.** `MarquageBoss.ReduireEffets = GameSettings.ReduceMotion` réduit l'éclair de moitié. Ce réglage est commun à tous les joueurs : une marque au sol est vue par tous. En coop, prendre le réglage le plus prudent.
- **Performance.** Le `MaterialPropertyBlock` sort ces marques du SRP Batcher. C'est sans effet notable : une à quinze marques à la fois.
- **Aperçu.** `Pose(t)` montre l'état à l'instant `t`, pour les captures et l'éditeur.

### Marque d'apparition des vagues (`WaveDirector.telegraphMaterial`)

Mettre `Assets/Art/VFX/Resources/M_VFX_Apparition.mat` dans `WaveDirector.telegraphMaterial` :
- Il remplace `Wave_Spawn_Telegraph` (Unlit orange HDR 3), créé aujourd'hui par `ActSceneBuilder`, `RandomLevelBuilder` et `TestArenaSceneBuilder`.
- Même shader que le marquage du boss : cercle plein, bord de 6 cm, pulsation à 1,2 Hz.
- Le shader lit la forme dans l'espace de l'objet. Il marche donc tel quel sur le cylindre aplati de `ShowMarker` (1,6 × 0,01 × 1,6) : seul le dessus est dessiné (le dessous et les flancs sont écartés par la normale), donc pas de double disque.
- Pour un point de volant, `ShowMarker` pose la marque 0,3 m sous le point d'apparition, parfois en l'air. Une marque « au sol » flotte alors dans le vide. **Proposition pour main :** projeter le point au sol (raycast vers le bas) pour la marque.

Il n'y a plus de disque orange plein.

### Volants : annonce du piqué et volant tenu (`ContourDiscret`, shader `ItHunts/VFX/ContourCoque`)

Plus de boule : un **contour collé à la silhouette** (coque inversée : le maillage est gonflé de 2 cm et ses faces avant sont écartées). Le contour garde **au moins 3 px à l'écran** (2 px pour la prise) : il reste lisible à 12 et 20 m (voir `Planche_Volant.jpg`).

- **Composant.** `ContourDiscret` va sur la racine de `Volant.prefab`, avec deux matériaux :
  - `annonce = M_VFX_Volant_Annonce` : encre rouge sang (0.62, 0.08, 0.06), opacité 1, pulsation 2,2 Hz ;
  - `prise = M_VFX_Prise_Contour` : ton d'os neutre (0.36, 0.33, 0.29), opacité 0,75, fixe.

  Au premier usage, il crée des copies des rendus en enfants. Il ignore le rendu « Annonce du piqué ».
- **`Flyer.UpdateVisual`.** Remplacer `telegraph.enabled = Mode == Annonce` par `contour.Montre(Mode == FlyerMode.Annonce && !Dead ? contour.annonce : tenu ? contour.prise : null)`. Laisser la sphère « Annonce du piqué » éteinte, ou la retirer du prefab.
- **`TelekinesisView`, ennemi tenu.**
  - Supprimer `SetGlow(grabColor * glowHeld)` : plus de lueur.
  - C'est `Flyer` qui montre `prise` tant qu'il est tenu.
  - Le clignotement « il va se libérer » alterne `prise` et `null`, au même rythme qu'aujourd'hui.
- **Modèle définitif.** Le futur modèle du volant gardera le même composant. Pour un `SkinnedMeshRenderer`, le matériau de coque est ajouté en dernier : il ne cerne que le dernier sous-maillage, il faut donc un modèle en un sous-maillage. Les primitives de la greybox ont des normales séparées : la coque s'ouvre un peu aux arêtes des cubes. Un modèle lissé n'aura pas ce défaut.
- **Reste du violet, à trancher par l'utilisateur.** Le trait violet de l'orbe vers l'objet tenu (`TelekinesisView`, `line`) suit la règle de game design « violet = objet tenu » (couleur de l'orbe). La consigne de la DA dit « ni violet quand attrapé » pour le volant lui-même. Le volant n'a plus de violet ; le trait de l'orbe reste tel quel tant que l'utilisateur n'a pas tranché.

### Screamer de la mort par le traqueur (`ScreamerMort`)

Effet d'écran fait par le post-traitement URP : un `Volume` à priorité 200, avec un profil créé en code.
- Le **poids du volume suit une courbe** : de 0 à 1 en 0,07 s, l'effet arrive en fondu sur l'étalonnage du jeu (`HorrorLook`).
- Ensuite, il **remplace** les paramètres qu'il force : vignette, aberration, grain, distorsion, saturation, contraste, exposition et filtre. Les autres réglages de `HorrorLook` (bloom, virage des couleurs) restent actifs.

**Version forte (0,9 s)**, dans l'ordre :
1. Image désaturée, puis teintée de rouge sang. On ne teinte pas directement en rouge : un rouge sur ciel bleu virerait au violet.
2. **Sursaut** de 0,15 s : exposition +0,55 et contraste.
3. **Distorsion** en barillet (−0,45) qui relâche, avec un pic à 17,5 %.
4. **Aberration chromatique** (0,55, limitée pour éviter une frange bleue), **grain** fort et **vignette** sang serrée (0,78, arrondie).
5. L'image s'assombrit (−1,8 IL) vers l'écran de fin.

**Version adoucie (1,2 s)** : ni sursaut, ni rougeur vive, ni distorsion, ni flash. L'aberration et le grain sont au quart, la vignette est douce.
- Elle est pilotée par `GameSettings.ReduceMotion` (« Réduire les animations »).
- Si main préfère un réglage dédié « Réduire les effets », il est à ajouter côté main.

**Utilisation (main)** :
- Un objet `Volume` + `ScreamerMort` par joueur.
- À la mort par le traqueur : `Joue(GameSettings.ReduceMotion)`, en même temps que `Sfx_Traqueur_Cri_Prise`.
- L'état final reste affiché jusqu'à `Arrete()` : il enchaîne avec l'écran de fin. À la relance, appeler `Arrete()`.
- Un plan où le traqueur remplit l'écran (caméra tournée vers sa tête, comme sur les captures) rend l'effet bien plus fort. C'est à décider avec main.

**Écran partagé** : deux options.
- `global = true`, avec une couche de volume par joueur (`volumeLayerMask` de sa caméra). Il faut alors 2 à 4 couches dans `ProjectSettings/TagManager` : **demande à main**.
- `global = false`, avec un collider de volume autour de la caméra du joueur.

**Aperçu** :
- `apercu` (de 0 à 1) et `apercuReduit` montrent l'effet dans l'inspecteur, **en Play Mode seulement**. Ils sont remis à 0 dans un build.
- `Pose(k, reduit)` sert aux captures.

### Contrôle du lot 7

- **Journal Unity** (après réimport des shaders, des matériaux et des sons, compilation et captures en Play Mode dans `Acte_4_Boss`) : 0 erreur et 0 avertissement sur ces assets. Les seules erreurs de la console sont `Destroying assets is not permitted` dans `ItHunts.Audio.MagnetismAudio.OnDestroy` (`Assets/_Project`, côté main).
- **Read/Write des maillages de l'arène** : 6 pièces de `KitJardin` utilisées par l'arène du boss (`SM_Arene_Butte`, `_Couvert`, `_EscalierDescente`, `_PilierBrise`, `SM_Jardin_Falaise_16m`, `SM_Jardin_Statue_Geante`) donnaient l'erreur « Source mesh … does not allow read access » (NavMesh en build). Elles sont passées à Read/Write. La PR du lot 2 (#47) rend lisibles toutes les pièces des kits : les deux changements sont identiques.
- **Éclairage des captures.** Ce sont celles du jeu, sans lumière ajoutée : l'arène du boss est très sombre par choix de DA (ciel de nuit, braseros), et les marques sont faites pour s'y lire. Le volant est la greybox de main (pas de modèle d'ennemi demandé à la DA cette nuit).

## Nuit 5, lot 4 : plus de rouge

Le marquage du boss décrit plus haut garde ses formes, son front, sa montée et son éclair, mais **plus aucun rouge** : flaque
d'ombre translucide (le sol s'assombrit), fin trait d'encre fumée (opacité 0,55), ton d'os pâle à l'impact. Même matériau,
rien à changer côté main. Le contour d'annonce du piqué des volants passe à l'encre fumée ; le contour de prise est un fin
trait d'os. Nouveaux : indices sombres `M_VFX_Indice_Cercle` / `_Rect` (goule qui bondit, main et revers du traqueur),
`VFX_Trait_Prise`, liens du livre lancés à chaque coup (`LienVfx.Lance`), apparitions `VFX_Apparition_Trou` / `_Grille` /
`_Ombre`. Où les brancher, valeurs de réglage et sons de l'épée : `docs/art/ART-HANDOFF.md`, section « Nuit 5, lot 4 ».
Captures : `docs/art/rendus/Nuit5/Lot4/`.
