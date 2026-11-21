# Outils pour un rendu professionnel (piste DA, 2026-11-21)

Liste de ce qui aiderait la production, **rien n'est installé**. Gratuits d'abord, puis payants. Les prix sont des ordres de
grandeur relevés de mémoire (novembre 2026) : **à vérifier** sur la page de l'éditeur avant tout achat, ainsi que la licence
(usage commercial, nombre de postes).

## Gratuits

| Outil | Pour | Pourquoi chez nous |
|---|---|---|
| **Rigify** (fourni avec Blender) | gréement | squelettes de quadrupède et de bipède avec IK prêts ; base pour le traqueur (quatre pattes et voûté) et la goule |
| **Blender, courbes de cheveux + Geometry Nodes** (fourni) | cheveux, mèches | mèches en rubans avec pointes effilées, convertibles en maillage pour le jeu (cheveux du traqueur) |
| **Instant Meshes** (open source) | retopologie automatique en quads | alléger les modèles Tripo (traqueur, épée) avant le gréement |
| **QRemeshify** (module Blender, open source) | retopologie en quads dans Blender | même usage, sans quitter Blender |
| **Blender, cuisson de normal maps** (fourni, Cycles) | détail du maillage source sur la version jeu | version allégée du traqueur avec le détail cuit |
| **Krita** (open source) | peinture de textures et de flipbooks | textures peintes à la main, planches d'effets image par image |
| **ArmorPaint** (code ouvert ; binaires officiels à quelques euros) | peinture 3D sur les modèles | retoucher les textures Tripo (aplats, traits d'encre) directement sur le modèle |
| **Unity Recorder** (paquet Unity) | captures et vidéos des rendus | planches de contrôle et vidéos de validation plus faciles |
| **Shader Graph / VFX Graph** (paquets URP) | effets | prototyper des variantes des shaders peints |
| **Mixamo** (gratuit, compte Adobe) | animations humanoïdes de base | références et premiers jets pour des créatures bipèdes ; inutile pour le traqueur à quatre pattes |
| **Materialize** (open source) | cartes de normales et de relief depuis une image | relief léger pour les tuiles peintes (pierre, bois) |

## Payants

| Outil | Prix indicatif | Pour | Pourquoi chez nous |
|---|---|---|---|
| **Auto-Rig Pro** (module Blender) | ~40 $ | gréement et export vers Unity | quadrupèdes, IK propres, export FBX fiable vers Unity ; ferait gagner beaucoup de temps sur le traqueur |
| **Quad Remesher** (module Blender) | ~60 $ (indépendant) à ~110 $ | retopologie en quads de qualité | meilleure que Instant Meshes sur les créatures organiques |
| **Hair Tool** (module Blender) | ~60 à 80 $ | mèches stylisées en rubans | cheveux en mèches plates, profils effilés, export maillage |
| **Cascadeur** | gratuit limité ; Pro ~20 $/mois | animation assistée par la physique | courses et bonds du traqueur crédibles (poids, appuis) |
| **EmberGen** (JangaFX) | ~20 $/mois ou licence ~300 $ | simulations de feu et de fumée en flipbooks | à styliser ensuite (posterisation, encre) pour les feux et les fumées |
| **Substance 3D Painter** (Adobe) | ~20 $/mois | peinture et cuisson de textures | standard du métier ; ArmorPaint couvre déjà l'essentiel |
| **Marmoset Toolbag** | ~200 à 300 $ | cuisson de normal maps, rendus de présentation | cuissons propres pour les versions allégées, planches de présentation |

## Recommandation

Commencer par les gratuits : **Rigify** et **QRemeshify** (traqueur), **courbes de cheveux** de Blender, **Krita** pour les
flipbooks. Le seul achat qui ferait gagner nettement du temps : **Auto-Rig Pro** (gréement du traqueur et export Unity).
