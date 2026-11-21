# UI-HANDOFF : habillage de l'interface

Ce que `main` doit savoir pour brancher l'interface habillée : sprites, bordures, tailles, ancrages, couleurs, polices,
états et réglages des animations. Tenu par la piste DA (branches `art-ui`, `art-menus`). Les sprites sont **visuels seulement** :
tout le texte (nombres, noms, boutons, descriptions) est du **texte Unity**, sauf le logo IT HUNTS et les libellés peints
des boutons fixes des maquettes (Prêt, Valider, Modifier).

Maquettes et pièces d'origine (images générées par IA, Higgsfield, Nano Banana Pro) : `docs/art/ui/maquettes/`.
Elles servent de matière à découper ; depuis le 2026-11-18, les écrans de menu **sont** ces maquettes (§ 2).

## 0. Conventions

| Règle | Valeur |
|---|---|
| Canvas | `Screen Space - Camera` (comme `PlayerHud`), `CanvasScaler` **Scale With Screen Size**, référence **1920 × 1080**, **Match = 1 (hauteur)**, Reference Pixels Per Unit **100** |
| Sprites | **double définition** : **Pixels Per Unit 200**. *Set Native Size* donne la taille prévue en 1080p (pixels du fichier / 2) ; net jusqu'en 2160p |
| Réduire un élément à bordures | multiplier la taille par *k* **et** régler `Image.pixelsPerUnitMultiplier = 1 / k`, sinon les bordures (emblème, pointe) se déforment |
| Écran partagé | chaque joueur a son canvas ; en 960 × 1080 l'échelle reste 1 (Match hauteur), seule la largeur disponible baisse : ancrer aux coins, pas au centre |
| Import | Sprite (Single), Mesh **Full Rect** (obligatoire pour Sliced / Tiled), pas de mipmaps, Bilinear, Clamp, compression **High Quality** |
| Atlas | `Assets/Art/UI/Atlas_HUD.spriteatlas` (HUD + mini-carte, marge 4 px, sans rotation ni découpe serrée) |
| Noms | `Groupe_Element[_Partie]` : `BarreVie_Cadre_Embleme`, `MiniCarte_Coffre_Rare`… |

**Réglage à faire sur `main` (ProjectSettings, hors périmètre art)** : *Project Settings > Editor > Sprite Packer > Mode* est
sur **Disabled** (`m_SpritePackerMode: 0`), donc l'atlas n'est pas empaqueté. Le passer sur **Sprite Atlas V2 - Enabled**
(Unity propose alors de convertir `Atlas_HUD.spriteatlas` en `.spriteatlasv2` : accepter). Sans ce réglage tout s'affiche
quand même, avec un appel de rendu par texture.

### Couleurs

| Rôle | Hex | Note |
|---|---|---|
| Âmes (flamme, nombre) | `#E0FAFD` cœur, `#7EC2DB` milieu, `#647A8D` bord ; texte `#D6E0EB` | gris-bleu clair spectral, plus pâle et plus gris que le hibou |
| Joueur renard (rouge) | `#DD373F` | pastille, bord de vignette, lanterne |
| Joueur koala (vert) | `#55C851` | |
| Joueur caméléon (orange) | `#DEA13F` | |
| Joueur hibou (bleu) | `#5FB9ED` | |
| Métal des cadres | `#96849C` moyen, `#D8C6CD` reflets | |
| Fond des cartouches | `#0E0B12`, opacité 86 % | |
| Vie (remplissage) | `#C41E26` haut, `#8A0A14` milieu, `#3E040A` bas (médiane `#7D0912`) | rouge sang |
| Vie perdue (traînée) | `#ECD6CE` → `#967E7C` | se teinte dans `Image.color` si besoin |
| Violet | réservé aux objets interactifs (lueur violette en jeu) et au marchand | le coffre normal de la mini-carte reste violet (décision) |

### Polices (validées, licence OFL)

| Usage | Police | Fichiers (`Assets/Art/UI/Polices/`) |
|---|---|---|
| Texte (menus, nombres, descriptions) | **EB Garamond** | `EBGaramond/EBGaramond-Regular.ttf`, `-Italic`, `-SemiBold` (nombres du HUD, boutons), `-Bold` |
| Titres gothiques (écrans, ACTE I / II / III) | **Grenze Gotisch** | `GrenzeGotisch/GrenzeGotisch-Regular.ttf`, `-SemiBold`, `-Bold` |

Fichiers statiques des dépôts d'origine (`octaviopardo/EBGaramond12`, `Omnibus-Type/Grenze-Gotisch`), licence **SIL Open
Font License 1.1** jointe dans chaque dossier (`OFL.txt`), sans nom de police réservé : intégration dans le jeu et vente du
jeu autorisées, la police seule ne se revend pas, la licence doit accompagner les fichiers. Importées comme `Font` Unity
(texte uGUI). Pour TextMesh Pro : *Window > TextMeshPro > Font Asset Creator* à partir de ces `.ttf` (SDF, 4096, jeu de
caractères latin étendu pour les accents), à faire sur `main` avec l'import des TMP Essentials.

Les nombres du HUD gagnent à avoir un contour sombre (`Outline` 1,5 px `#0A050A` à 90 %).

## 1. Lot A : HUD (fait)

Scène d'essai : `Assets/Art/Scenes/Test_UI_HUD.unity` (HUD complet, valeurs factices ; planche des icônes à 48 et 24 px en
bas à droite). Planche : `docs/art/rendus/UI/LotA_HUD_avant_apres.jpg`.

Les tailles « 1080p » sont la taille native (*Set Native Size*). Bordures en pixels du sprite (gauche, bas, droite, haut).

### 1.1 Barre de vie (`Assets/Art/UI/HUD/`)

| Sprite | Pixels | 1080p natif | Bordures | Usage |
|---|---|---|---|---|
| `BarreVie_Cadre` | 862 × 262 | 431 × 131 | 245, 0, 106, 0 | cadre entier : `Image` **Sliced** (centre étiré) ; la longueur se règle par la largeur |
| `BarreVie_Fond` | 862 × 262 | 431 × 131 | 245, 0, 106, 0 | fond sombre de l'emblème et du canal, même rect que le cadre, **dessous** |
| `BarreVie_Cadre_Embleme` / `_Milieu` / `_Bout` | 245 / 128 / 106 × 262 | 122,5 / 64 / 53 × 131 | — | même cadre en 3 parties : le milieu est **raccordable** (`Image` Tiled, ou répété) |
| `BarreVie_Fond_Embleme` / `_Milieu` / `_Bout` | idem | idem | — | fond en 3 parties, alignées pixel pour pixel sur le cadre |
| `BarreVie_Remplissage` | 610 × 46 | 305 × 23 | 23, 0, 28, 0 | rouge sang ; `Image` **Filled**, Horizontal, origine Left, `fillAmount = PV / PVmax` |
| `BarreVie_Remplissage_Retard` | 610 × 46 | 305 × 23 | 23, 0, 28, 0 | traînée claire des dégâts récents, sous le remplissage, rattrape la valeur en ~0,4 s |

**Canal** (zone du remplissage) dans le cadre natif : à **100 px du bord gauche**, **26 px du bord droit**, **54 px du haut**,
**23 px de haut** (à multiplier par *k*). Le remplissage s'ancre en étirement horizontal sur ce canal, il suit donc la longueur
de la barre. Ordre : `Fond` → `Remplissage_Retard` → `Remplissage` → `Cadre` → texte « 45/100 » (texte Unity, à 14 px du début
du canal, 17 px en 1080p). Dans `Test_UI_HUD` : *k* = 0,9 (118 px de haut), 440 px de long, coin bas gauche à (32, 32).

### 1.2 Cartouche des âmes (`Assets/Art/UI/HUD/`)

| Sprite | Pixels | 1080p natif | Bordures | Usage |
|---|---|---|---|---|
| `Ames_Cadre` | 607 × 204 | 303,5 × 102 | 190, 0, 93, 0 | cadre entier, `Image` Sliced |
| `Ames_Fond` | 607 × 204 | 303,5 × 102 | 190, 0, 93, 0 | fond sombre du rond et du cartouche, dessous |
| `Ames_Cadre_*`, `Ames_Fond_*` (Embleme / Milieu / Bout) | 190 / 96 / 93 × 204 | 95 / 48 / 46,5 × 102 | — | version 3 parties (milieu raccordable) |
| `Ames_Flamme` | 153 × 256 | 76,5 × 128 | — | flamme des âmes ; dans le rond : hauteur ≈ 0,8 × diamètre du rond, `preserveAspect` |

Rond de l'icône : centre à **(58, 51) px** du coin haut gauche (natif), diamètre intérieur **51 px**. Zone du nombre : commence à
**86 px** du bord gauche, centrée verticalement (hauteur utile 46 px). Dans `Test_UI_HUD` : *k* = 0,75 (76 px de haut),
250 px de long, coin haut gauche à (32, −32), nombre en 30 px.

### 1.3 Mini-carte (`Assets/Art/UI/MiniCarte/`)

| Sprite | Pixels | 1080p natif | Usage |
|---|---|---|---|
| `MiniCarte_Cadre` | 562 × 590 | 281 × 295 | anneau gothique, **au-dessus** de tout |
| `MiniCarte_Masque` | 562 × 590 | 281 × 295 | disque blanc : `Image` + `Mask` (`Show Mask Graphic` décoché) ; la carte et les icônes sont ses enfants |
| `MiniCarte_Fond` | 562 × 590 | 281 × 295 | disque sombre (82 %) légèrement vignetté, sous la carte |

Les trois ont le même rect : les empiler en étirement. Centre de la zone visible : (140, 143) px du coin haut gauche (natif),
**rayon 96 px** (bord intérieur de l'anneau ; les ornements débordent jusqu'à 75 px du centre). Dans `Test_UI_HUD` :
*k* = 0,85, coin haut droit à (−32, −32).

**Icônes** (96 × 96 px, natif 48 ; pastilles 48 × 48 px, natif 24) : trait d'encre net, sans lueur ni fond. Lisibles à **24
et 48 px** sur fond sombre (vérifié dans la scène, 1920 × 1080 et 960 × 1080).

| Sprite | Sens | Taille conseillée (1080p) |
|---|---|---|
| `MiniCarte_Fleche` | joueur local, pointe vers le haut au repos : tourner selon le cap | 24 |
| `MiniCarte_Joueur_Rouge` / `_Vert` / `_Orange` / `_Bleu` | alliés (renard, koala, caméléon, hibou) | 14 à 18 |
| `MiniCarte_Coffre_Normal` / `_Rare` / `_Ouvert` | coffres | 24 |
| `MiniCarte_Porte_Verrouillee` / `_Ouverte` | porte finale | 24 |
| `MiniCarte_Allie_Mort` | allié mort (tête de mort) | 24 |
| `MiniCarte_Bougie` | **marchand** | 20 à 24 |
| `MiniCarte_Flamme` | **tas d'âmes / salle d'âmes** (gris-bleu des âmes, plus violet) | 24 |

Non découpées (décision) : cage, orbe fêlée, losange.

### 1.4 Décisions du lot A (2026-11-17)

- Le coffre normal reste violet (les objets interactifs ont une lueur violette en jeu).
- L'icône flamme passe en gris-bleu des âmes ; bougie = marchand, flamme = tas d'âmes / salle d'âmes.

## 2. Menus : titre, campement, choix du personnage, pause, paramètres (fait, 2026-11-18, branche `art-menus`)

Remplace le lot B (fonds en calques), le lot T (scènes régénérées, PR #17 fermée) et les lots C et D prévus : **chaque écran
est la maquette de l'utilisateur, pixel pour pixel**. Rien n'est redessiné, régénéré ou remplacé par une police : tout est
**découpé** des images de l'utilisateur par **différence entre deux images alignées** (recalage affine robuste au
sous-pixel, puis alpha tiré de l'écart et couleur démêlée sur le fond), et les images retouchées ne remplissent que les zones
qui changent, par masque. Seul le texte variable (libellés, noms des joueurs, textes d'artefact, ACTE) est du texte Unity
(EB Garamond), posé aux emplacements et dans le style mesurés sur la maquette.

Scène d'aperçu : `Assets/Art/Scenes/Test_UI_Menus.unity` (F1 titre, F2 campement, F3 choix, Échap pause puis Paramètres,
F5 gèle ou relance les effets ; campement : touches 1 à 4 pour changer l'occupant d'une place, R pour « prêt »). Prefabs :
`Assets/Art/UI/Menus/Prefabs/Menu_Titre`, `Menu_Campement`, `Menu_Choix`, `Menu_Pause`, `Menu_Parametres`. Planches et
mesures : `docs/art/rendus/UI/Menus/` (`Controle_Titre.jpg`, `Controle_Campement.jpg`, `Controle_Choix.jpg`,
`Controle_mesures.json`, `Menus_Etats.jpg`, `Menus_Boutons_Etats.jpg`).

Scripts (tout se refait d'un coup) : `Assets/Art/Source~/UI/scripts/menus/` — `reg.py` (recalage, différences, détourage),
`titre.py`, `campement.py`, `assis.py`, `choix.py`, `cadres.py`, `textes.py` (mesure des textes peints), `export.py`
(`construction.txt`), `MenusBuilder.cs` (lancé par la CLI : `Importe`, `Construit`, `Capture`), `controle.py`,
`planche_etats.py`, `boucles_audio.py`.

### 2.1 Conventions propres aux menus

| Règle | Valeur |
|---|---|
| Repère | chaque écran a une racine `MenuEcran` de la **taille de la maquette** (1672 × 941, 1670 × 941 ou 1671 × 941) ; tous les enfants sont posés en **pixels de la maquette** depuis le coin haut gauche ; la racine est mise à l'échelle pour **couvrir** le canvas (1920 × 1080 : × 1,1477) |
| Canvas | `Screen Space - Camera`, `CanvasScaler` 1920 × 1080, Match 1 (hauteur), comme le HUD |
| Sprites des menus | **PPU 100**, Full Rect, sans mipmaps, Clamp, Bilinear, **non compressés** (le contrôle au pixel le demande ; passer en BC7 plus tard si la mémoire compte) ; masques et poids en **données linéaires** (sRGB décoché) |
| Espace couleur | le projet est en **linéaire** : les couleurs démêlées (alpha) et les lumières ajoutées sont calculées en linéaire, sinon un halo sombre apparaît autour des pièces semi-transparentes |
| Gel des effets | variable globale `_ItHuntsMenuFige` (composant `MenuFige`, ou `MenuFige.Regle(true)`) : effets au repos, le rendu est la maquette ; à brancher aussi sur l'option « Réduire les animations » |
| Boutons | shader `ItHunts/UI/Bouton` (la teinte du sommet est lue × 2, en gamma : gris 0,5 = pixels d'origine) ; `Button` en **Color Tint** : normal `(0.5, 0.5, 0.5)`, survol et sélection `(0.60, 0.575, 0.55)` (+20 %, un peu chaud), pressé `(0.40, 0.38, 0.36)` (−20 %), désactivé `(0.30, 0.29, 0.31, 0.85)` ; boutons longs en 5 morceaux : `TeinteGroupe` (mêmes valeurs) |

### 2.2 Shaders (`Assets/Art/Shaders/UI/`, include commun `ItHunts_UICommun.cginc`)

| Shader | Mélange | Rôle et réglages |
|---|---|---|
| `ItHunts/UI/Modulation` | multiplication × 2 (`DstColor SrcColor`), 0,5 = inchangé | fait bouger la lumière **déjà peinte** d'après un canal de masque (`_Canal`) : Vacillement (lanternes, fenêtres, lueur du feu), Pulsation (halo de lune), Scintillement (reflets des flaques) ; `_Amplitude` (fraction), `_Vitesse`, `_Echelle` (désynchronise les sources), `_Chaleur` |
| `ItHunts/UI/Additif` | `One One` | Texture (curseur, soulignement, lueurs des lanternes au sol, x vacillement) ; Brume (masque × bruit fractal qui dérive, `_Derive` en px/s, `_Seuil`) ; Flammes (mes flammes en additif, ondulation qui grandit au-dessus de `_Base`, scintillement) ; Etincelles (procédurales, sans texture) |
| `ItHunts/UI/Deformation` | prémultiplié | respiration (étirement autour de `_Pivot`, `_Respiration` px au sommet, `_Rythme`), cape (onde horizontale, `_Cape` px, sous `_CapeHaut`), poids par texture (`_POIDS_TEXTURE` : R respiration, G cape, B phase) ou profil vertical, flottement (`_Flottement` px), orbite des traînées (`_Orbite` tours/s dans une ellipse `_Centre`) ; **éclairage de scène** des personnages assis (`_Eclairage` = 1, 2026-11-21) : `_Luminosite`, `_Saturation`, `_Contraste` (exposant sur la luminance : noirs profonds), ombres froides (`_OmbreCouleur` bleu-violet, `_OmbreForce`, `_OmbreSeuil`), côté du feu chaud et côté opposé froid (`_FeuUV` en uv du sprite, `_ChaudCouleur`, `_FroidCouleur`, `_Transition`), liseré orange sur les bords tournés vers le feu (`_LisereCouleur`, `_LisereForce`, `_LisereLargeur` px), palpitation avec le feu (`_Palpite`, `_PalpiteVitesse`), lueur de la lanterne sur les jambes (`_LanterneUV` xy + rayon, `_LanterneCouleur`, `_LanterneForce`) |
| `ItHunts/UI/Bouton` | prémultiplié | voir 2.1 |

Matériaux réglés : `Assets/Art/UI/Menus/Materiaux/` (un par effet). Tout est réglable en Play Mode dans le matériau.

### 2.3 Écran titre (`Assets/Art/UI/Menus/Titre/`)

| Sprite | Rect (px maquette) | Rôle |
|---|---|---|
| `Titre_Fond` | 0, 0, 1672, 941 | **maquette `ui_menu_principal_v01` telle quelle** (logo et 4 personnages compris) ; zone du menu (x 16–414, y 398–742, bord fondu 22 px) remplie par `ui_menu_sans_texte_v01` recalée (écart au raccord 1,3/255 en moyenne) ; un reste de lueur violette du générateur à l'emplacement de la flamme a été retiré (fréquences moyennes remplacées par le fond flou, grain gardé) |
| `Titre_Personnages` + `_Poids` | 696, 548, 654, 244 | recadrage opaque des 4 personnages (dans `Titre_Fond`) déformé sur place (respiration 1,6 px, cape 1,3 px, une phase par personnage, pieds fixes à y 776) |
| `Titre_Effets_Masques` | plein écran | R fenêtres du château (palpitent), G reflets des flaques (scintillent), B lanternes des 4 personnages (vacillent), A halo de la lune (pulse ; disque centre 1079, 119, rayon 52) |
| `Titre_Brume_Masque` | plein écran (mi-définition) | où la brume dérive (zones déjà brumeuses : bleutées, peu détaillées) ; deux nappes additives (lente 11 px/s, proche 24 px/s) |
| `Titre_Curseur`, `Titre_Soulignement` | 62, 420 / 96, 508 | **lumière ajoutée** = maquette − fond sans texte (en linéaire) ; suivent l'élément sélectionné |

Menu (`TitreApercu`) : Jouer / Paramètres / Quitter, lignes de base y **495**, **604**, **694**. Sélectionné = style de « Jouer »
sur la maquette : EB Garamond Regular **57**, blanc `#FFFFFF`, bord gauche x 143,6, étirement horizontal **1,113** (le texte
peint est plus large que l'EB Garamond) ; les autres = style de « Settings » : **49**, `#B4B3B8`, x 127, étirement **1,170**.
Curseur à (−81,6 ; −75) et soulignement à (−47,6 ; +13) du bord gauche du rect du texte et de sa ligne de base.

### 2.4 Campement (`Assets/Art/UI/Menus/Campement/`)

| Sprite | Rôle |
|---|---|
| `Campement_Fond` | maquette `ui_campement_v01` ; dans les zones qui changent (personnages, interface, lanternes des places 3 et 4, feu, halos colorés des lanternes de la maquette) : `ui_campement_vide_sans_lanternes_v01` recalé, raccordé par un **champ de gain lisse** estimé hors des zones ; sous les flammes : `ui_campement_braises_v01` |
| `Campement_Flammes` | **passe1 − braises** en lumière ajoutée (linéaire) ; ondulation 5 px au sommet, scintillement 14 % ; les bûches des deux images n'étant pas disposées pareil, le rendu vaut le plus clair des deux (flammes de passe1 sur les braises) |
| `Campement_Effets_Masques` | R fenêtres du château, G lanterne de la tente, B lueur du feu au sol et sur les sièges (palpite ±14 %) ; étincelles procédurales au-dessus du foyer |
| `Campement_Lanterne_Banc`, `_Rocher` | **braises − vide** : lanternes éteintes, affichées seulement quand les places 2, 3 et 4 sont vides (place 2 : la lanterne du banc à 0,95, pied en 918, 596) |
| `Campement_Assis_<Perso>` + `_Lanterne` | les 4 `ui_assis_buche_<perso>` détourés du gris (caisse retirée dans son contour : faces claires même teintées, traits d'encre qui ne touchent pas le personnage), lanterne séparée et posée au pied de la place ; échelle choisie **par place** pour que le haut de capuche tombe sur celui de la maquette et le bas des pieds sur le sol devant le siège, au-dessus du cartouche (place 1 : y 657, bottes du renard de la maquette ; place 2 : y 640 ; places 3 et 4 : 640 et 650, extrapolés) ; jamais plus grande que l'échelle des épaules (0,22 à 0,23 aux places 1 et 2) ; détourage propre (2026-11-21 : couleur des bords plus clairs que l'intérieur remplacée par celle de l'intérieur, l'encre reste ; alpha resserré) ; valeurs pré-réglées sur celles du renard de la maquette et liseré froid de la lune en haut, peints ; **lumière de la scène dans le shader** (2.2) : valeur, saturation et contraste par personnage (`CampementApercu.persos` : renard 1,15 / 0,78 / 1,3, koala 1,05 / 0,8 / 1,1, caméléon 1,35 / 0,72 / 1,25, hibou 1,3 / 0,5 / 1,3), côté feu chaud et clair, côté opposé sombre et froid, liseré orange discret côté feu qui palpite avec lui (à gauche aux places 3 et 4), lueur de la lanterne sur les jambes ; **ombre de contact** douce sous le siège et les pieds (`Ombre`, noir 62 %, étendue des pieds mesurée) ; respiration 1,3 px, cape 0,9 px, lueur additive au sol à la couleur du personnage |
| `Campement_Cartouche_Nom` | cartouche du hibou **vidé de son texte peint** (modèle séparable ligne + colonne de la plaque, grain repris sur la même ligne) ; sert aux 4 places (celui du renard porte la lueur rouge de sa lanterne) ; nom en EB Garamond Regular **23**, `#F7E1B3`, centré, ligne de base mesurée ; décalé à gauche quand la coche est affichée |
| `Campement_Coche`, `_Modifier`, `_Plus_3`, `_Plus_4`, `_Pret`, `_Retour`, `_Parametres`, `_Amis` | **maquette − passe1**, aux positions exactes ; boutons à 4 états (2.1) ; `Plus_2` (place 2 vide) = `_Plus_3` centré sur le cartouche de la place 2 |

Sélecteur **ACTE I / II / III** (haut gauche, cartouche long, EB Garamond SemiBold 25, `OptionDefilante`) et emplacement du
**chat** (bas gauche, cartouche long, invite en italique 16) : cartouches découpés, pas de nouveau dessin. Places 1 (bûche) et
2 (pierre) : rects mesurés ; **places 3 (banc) et 4 (rocher) : extrapolées** (échelle × 0,85, assise plus haute), à valider.
Apparition d'un joueur : fondu et léger grandissement (0,6 s), la lueur s'allume plus fort puis se pose (`CampementApercu`).
**J1 seul** : la place 2 (pierre) est vide comme les places 3 et 4 : bouton + et lanterne éteinte (2026-11-21). Réglages de
l'éclairage : `CampementApercu` (`foyer`, `rayonLanterneCorps`, `teinteLanterne`, `ombre`) et matériaux `M_Campement_Assis_1` à `_4`
(en jeu, une copie par place). Contrôle : `docs/art/rendus/UI/Menus/Campement_Personnages.jpg`.

### 2.5 Choix du personnage (`Assets/Art/UI/Menus/Choix/`)

| Sprite | Rôle |
|---|---|
| `Choix_Fond` | maquette `ui_choix_perso_v01` (cadre, campement flouté derrière, bouton Valider) ; zones qui changent remplies par `ui_choix_fond_v01` : grand personnage et artefact (masque par différence), textes, rangée des skins et vignettes (rects) ; le générateur avait relevé les ombres (panneau 31/22/35 au lieu de 12/9/11) : **courbe de luminance** apprise sur le cadre et les zones vides, appliquée à toutes les images générées de l'écran, puis champ de gain lisse et grain fin ramené à celui de la maquette |
| `Choix_<Perso>` + `_Lueur` | `ui_choix_<perso>_v01` − fond, zone de gauche, reflet au sol compris (fondu sur 30 px au bord de la zone) ; respiration 1,6 px, cape 1,1 px ; la lanterne et son reflet vacillent (`_Lueur`) |
| `Choix_Artefact_Lanterne` (+ `_Trainees`) | **passe3 − fond** ; corps et traînées séparés ; flottent de 5 px, les traînées tournent (0,05 tour/s dans une ellipse aplatie à 0,45) |
| `Choix_Artefact_Barillet` (+ `_Trainees`) | **barillet − caméléon** (et différent du fond, pour ne pas garder la place de l'ancienne lanterne) |
| `Choix_Vignette_<Perso>_Discrete` / `_Illuminee` | découpées de `ui_choix_passe3_v01` (renard discret : `ui_choix_cameleon_v01`) ; version illuminée = vignette discrète + lueur du cadre illuminé du renard (passe3 − caméléon, hors portrait) **recolorée** : rouge, vert, orange ambré, bleu glacé ; seule la sélectionnée est illuminée |
| `Choix_Valider` | bouton de la maquette (contour mesuré), 4 états |

Textes (entre les deux filets) : nom du personnage à l'emplacement de « Passive — … » (EB Garamond SemiBold **22**,
`#F8E3BB`, ligne de base 481), nom de l'artefact à l'emplacement de « Artefact — … » (SemiBold 22, ligne de base 574),
description à l'emplacement de sa description (Italic **19**, `#B3A59A`, lignes de base 598 et 619). Les noms sont lus dans
`CharacterRoster` / `CharacterDefinition` ; la description dans un champ `description` de l'`ArtifactDefinition` **s'il existe**
(à ajouter sur `main`), sinon un texte d'aperçu résumé de `docs/design/features/artefacts/design.md`. Koala et hibou : « à
venir ». Pas de skins, pas de passif.

### 2.6 Pause et paramètres (`Assets/Art/UI/Menus/Commun/`)

| Sprite | Rôle |
|---|---|
| `Menus_Cadre` | cadre du choix recomposé en **9 tranches** (bordure 100 px) : les 4 coins de la maquette tels quels, tranches de 8 px prises contre chaque coin (raccord par construction), dedans du panneau ; le ciel bleu que les coins droits prenaient au-delà du métal (bande bleue verticale à droite) est retiré (2026-11-21) |
| `Menus_Cartouche_G`, `_M`, `_C`, `_D` | cartouche vidé en 4 morceaux (bouts, tranche raccordable, fleuron central) : boutons longs sans étirer le fleuron |
| `Menus_Lueur` | tache de lumière radiale (technique) pour les lueurs teintées |

Pause : voile noir 55 %, cadre 560 × 600, « Pause » (Regular 44), Reprendre / Paramètres / Retour au campement / Quitter le
jeu (Regular 27). Paramètres : cadre 820 × 760, 6 lignes `OptionDefilante` (libellé à gauche, « ‹ 80 % › » à droite, tout en
texte Unity, sans glissière dessinée) et Retour.

### 2.7 Audio (`Assets/Art/Audio/Menu/`)

`Musique_Menu_Boucle.wav` (117,5 s, 44,1 kHz) et `Sfx_Feu_Boucle.wav` (18,3 s, 48 kHz) : boucles préparées par
`boucles_audio.py` (silence d'encodeur et silences de tête et de queue retirés, **fondu enchaîné à puissance constante** de
2,5 s et 1,5 s de la fin vers le début). Import : musique en **Streaming**, Vorbis 0,7 ; feu **Compressed In Memory**, Vorbis
0,6. `AudioSource.loop = true` (le réglage de boucle est sur la source, pas sur le clip). Le feu est très faible (−46 dB RMS) :
monter son volume ou le gain du mixeur.

### 2.8 Ce que `main` doit faire

- Brancher la logique (lancer la partie, salon, choix réseau, réglages réels) sur les prefabs ; les composants `*Apercu`,
  `OptionDefilante`, `TeinteGroupe`, `SelectionAuSurvol`, `MenuEcran`, `MenuFige` (assembly `ItHunts.Art.Menus`, qui lit
  `ItHunts.Runtime`) peuvent être gardés ou remplacés.
- Ajouter un champ `description` (texte court) à `ArtifactDefinition` (lu automatiquement par l'écran de choix).
- Brancher « Réduire les animations » sur `MenuFige.Regle`.

### 2.9 Limites connues

- Les images générées sont **repeintes partout** (grain refait) : la différence demande un seuil, un nettoyage et des bords
  adoucis ; l'écart restant est mesuré dans les planches de contrôle.
- Campement : les personnages assis ont des jambes plus longues que ceux de la maquette (assis sur une caisse haute) ; pour garder les pieds au-dessus du cartouche, ils sont environ 17 % plus petits qu'à largeur d'épaules égale (2026-11-19) ; places 3 et 4 extrapolées.
- Choix : les vignettes de `passe3` ne sont pas celles de la maquette (hibou déverrouillé, autre peinture), comme prévu.

## 2 bis. Carte ouverte (Tab) (fait, 2026-11-17)

Même famille que les cadres du HUD : les bords reprennent le double rail de la barre de vie, les coins son étoile
d'emblème. Sprites dans `Assets/Art/UI/Carte/` (PPU 200, Full Rect, sans mipmaps), script `Source~/UI/scripts/carte.py`,
planche `docs/art/rendus/UI/Carte_montage.jpg` (montage 1920 × 1080 avec un contenu de carte d'exemple).

| Sprite | Pixels | Bordures | Usage |
|---|---|---|---|
| `Carte_Cadre` | 1024 × 1024 | 260 (les 4) | `Image` **Sliced**, étirée en **plein écran** (ancrage étiré) ; bande sombre (86 %) du bord de l'écran au rail intérieur, centre transparent ; bordure de **130 px en 1080p** |
| `Carte_Fond` | 1024 × 1024 | — | parchemin sombre **raccordable** : `Image` **Tiled** (ou `RawImage` à UV répétées, wrap Repeat), sous la carte dessinée par le jeu |
| `Carte_Vignette` | 512 × 512 | — | noir à alpha, bords assombris : `Image` étirée sur la zone de carte, au-dessus du fond et de la carte |
| `Carte_Brume` | 512 × 512 | — | **zone non découverte** : brume sombre gris-bleu raccordable (alpha 0.82 à 1), au-dessus de la carte, wrap Repeat |
| `Carte_Brume_Pinceau` | 256 × 256 | — | pinceau blanc à bord doux et irrégulier : à peindre dans une `RenderTexture` (masque) là où le joueur est passé ; la brume se multiplie par (1 − masque) |
| `Carte_Legende_Cadre` | 384 × 384 | 64 (les 4) | panneau de légende `Image` **Sliced** ; dedans, les icônes de la mini-carte (40 px) et leur nom (texte Unity, EB Garamond SemiBold 26) |
| `Carte_Rose` | 256 × 256 | — | rose des vents (étoile de l'emblème), 128 px en 1080p, coin haut gauche ; tourner avec le nord de la carte |

Ordre : `Carte_Fond` → contenu de la carte (salles et couloirs tracés à l'encre claire `#CDBEA5`, icônes) → `Carte_Brume`
(masquée) → `Carte_Vignette` → `Carte_Cadre` → `Carte_Legende_Cadre` + icônes + textes → `Carte_Rose` ; titre « Carte » en
Grenze Gotisch dans la bande du haut. Écran partagé : un canvas par joueur, le cadre s'étire avec la largeur disponible.

## 3. Lots C et D : faits dans le § 2 (personnages des menus, écran de choix)

## 5. Décisions de l'utilisateur (2026-11-17)

- Âmes : gris-bleu clair spectral (référence `ui_flamme_ames_v01`) ; violet réservé aux attrapables et au marchand.
- Difficulté **ACTE I / ACTE II / ACTE III** en haut à gauche du campement ; chat en bas à gauche (place prévue, masqué
  tant que le jeu en ligne n'existe pas).
- Personnages des menus en 2D peinte avec de légers mouvements.
- Choix du personnage : doublons permis ; c'est le bouton Prêt/Jouer qui se désactive (logique côté `main`).
- « Settings » devient « Paramètres » (texte Unity).
- Polices : EB Garamond (texte) et Grenze Gotisch (titres).
- Lots B et C : liste d'images validée, personnages en 3:4 ; images déposées dans `docs/art/ui/generees/` ; test du renard animé seulement quand les images du lot C sont arrivées.

## 6. Décisions de l'utilisateur (2026-11-18)

- **Menus identiques aux maquettes** : rien n'est recréé, redessiné, régénéré ou remplacé par une police ; tout est découpé
  par différence entre deux images alignées (alpha tiré de la différence, couleur de l'image la plus complète) ; seul le texte
  variable est du texte Unity. Si une différence échoue ou qu'un cartouche ne se vide pas proprement : demander une retouche
  à l'utilisateur au lieu de redessiner.
- **La base de chaque écran est la maquette** ; les images retouchées ne remplissent que les zones qui changent, par masque.
- Lot T et reconstruction en calques générés abandonnés (PR #17 fermée).
- Plus aucune correction des mains FPS ni d'aucune animation (mains, traqueur, goule) avant les photos et vidéos de référence.

## 7. Interface en jeu : réticule, charges, parade, marchand, coffre (fait, 2026-11-21, branche `art-ui-jeu`)

Tout est **découpé ou recomposé** dans les maquettes de l'utilisateur (aucun nouveau dessin) ; textes en EB Garamond.
Scène d'aperçu : `Assets/Art/Scenes/Test_UI_Jeu.unity` (1, 2, 3 : réticule ; espace : dépense une charge ; P : parade ;
O : contre réussi ; M : marchand ; K : coffre ; Échap : fermer). Planche : `docs/art/rendus/UI/Jeu/Planche_UI_Jeu.jpg`.
Constructeur : `Assets/Art/Source~/UI/scripts/UIJeuBuilder.cs` ; découpe : `ui_jeu.py`. Scripts (assemblée
`ItHunts.Art.UIJeu`) : `Assets/Art/UI/Jeu/Scripts/`. Tailles en unités du canvas 1920 × 1080 (*match height*).

### 7.1 Pièces (`Assets/Art/UI/Jeu/`, PPU 100, non compressées)

| Sprite | Pixels | Source |
|---|---|---|
| `Reticule_Etoile` | 136 × 136 | étoile à 4 pointes du cadre de la mini-carte (pointe nord de `MiniCarte_Cadre`, trait d'encre compris), isolée par un losange |
| `Charge_Pleine` | 59 × 59 | orbe bleue de `ui_icones_minicarte_v01` (trait d'encre 4 px) |
| `Charge_Vide` | 68 × 68 | orbe fêlée grise de `ui_icones_minicarte_v01` |
| `Parade_Icone` | 132 × 132 | losange cerclé de `ui_icones_minicarte_v01` |

Repris tels quels : `Menus_Cadre` et `Menus_Cartouche_*` (menus), `Ames_Cadre`, `Ames_Fond`, `Ames_Flamme` (HUD),
`Menus_Lueur` (lueurs), icônes de la mini-carte (aperçu des cartes seulement).

### 7.2 Prefabs (`Assets/Art/UI/Jeu/Prefabs/`) et API

| Prefab | Ancrage, position, taille | États | API (composant) |
|---|---|---|---|
| `HUD_Reticule` | centre de l'écran ; étoile **30 px**, lueur 48 px | **Neutre** : étoile blanc argent, lueur à peine visible ; **Attrapable** : lueur violette (télékinésie), étoile × 1,22 ; **Ennemi** : lueur rouge, étoile × 1,12 **tournée de 45°** (lisible sans la couleur) ; transition lissée (14/s), lueur qui pulse (±18 %, 1,6 Hz) | `ReticuleJeu.SetEtat(EtatReticule)` |
| `HUD_Charges` | bas centre, **132 px** au-dessus du bord ; 3 orbes de 24 px espacées de 34 px | **pleine** : orbe bleue + lueur bleue 35 % ; **vide** : orbe fêlée ; **en recharge** : orbe fêlée, l'orbe bleue monte du bas (remplissage vertical) à 55 % ; une charge qui revient brille (0,35 s) | `ChargesJeu.Regle(i, 0..1)` ou `RegleTout(pleines, recharge01)` |
| `HUD_Parade` | bas centre, à droite des charges (x +96, y 152) ; 36 px | **prête** : icône pleine (brille une fois en revenant) ; **en recharge** : icône à 45 %, voile sombre radial qui recule ; **contre réussi** : éclat blanc-bleu (0,6 s) | `ParadeJeu.Regle(0..1)`, `ParadeJeu.Contre()` |
| `Panneau_Marchand` | centre ; cadre 9 tranches **760 × 720**, voile noir 55 % plein écran | titre (Regular 44, `#F7E1B3`), solde d'âmes (haut droit, flamme + SemiBold 26), **4 cartes** de 600 × 82 (x 80, y 128 + 94 × i depuis le haut), zone de description (Italic 22), bouton **Fermer** (cartouche long 220 × 56, y 612) | `PanneauBoutique.Remplit(offres, âmes)`, évènements `Achat(int)` et `Ferme` |
| `Panneau_Coffre` | idem, **3 cartes**, prix 0 (choix gratuit : flamme et prix masqués) | idem | idem |

**Carte** (`CarteAmelioration`) : cartouche des âmes en 9 tranches (`Ames_Fond` puis `Ames_Cadre`, échelle 82/204) ; icône dans
le rond (centre à 116, 102 px du sprite × échelle, 86 % du diamètre intérieur) ; nom en Regular 27 à partir de 182 px du sprite ;
prix en SemiBold 27 aligné à droite, flamme des âmes à 112 px du bord droit. **Survol ou sélection (manette)** : lueur bleu
pâle derrière la carte, la carte grandit de 2,5 %, la description s'affiche dans le panneau. **Indisponible** (prix > âmes) :
cadre, fond, icône et flamme assombris (`#6B6670`), nom gris, **prix en rouge**, l'achat est refusé (l'évènement `Achat`
n'est pas émis) ; la carte reste sélectionnable pour lire sa description. Bouton Fermer : 4 états (`TeinteGroupe`, 2.1).

### 7.3 Ce que `main` doit faire

- Remplacer le point du viseur et les segments des charges de `PlayerHud` par ces prefabs (ou leurs pièces), et brancher
  `SetEtat` (cible attrapable ou ennemi sous le viseur) et `Regle` (charges, parade) ; garder le cercle libre de 48 px
  autour du viseur (ART-HANDOFF § 5 bis : l'étoile et sa lueur y tiennent).
- Ouvrir les panneaux depuis le marchand et le coffre (`Remplit` avec les offres tirées et les âmes du joueur), écouter
  `Achat` et `Ferme` ; les icônes des améliorations restent à fournir (l'aperçu prend des icônes de la mini-carte).
- En écran partagé (960 × 1080), les tailles en unités du canvas restent lisibles (*match height*).
