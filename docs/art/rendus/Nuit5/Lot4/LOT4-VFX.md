# Nuit 5, lot 4 : effets (branche `art-vfx-5`)

Captures en jeu dans l'arène `Acte_4_Boss` (éclairage, post-process et lanterne du jeu, seed 4242, point de vue choisi
automatiquement là où le terrain est dégagé), outil `Assets/Art/Source~/VFX/scripts/CapturesLot4N5.cs`. Les images sont
dans `Jeu/` ; `Jeu/points.txt` donne la caméra de chacune. Comportement du lien testé en jeu : `test_lien.txt` (7 cas).

## Planches

| Planche | Contenu |
|---|---|
| `Planche_Marquages_Boss.jpg` | Prise, Balayage, Charge, Débris : **vraies attaques** du boss ; mêmes marques, valeurs rouges de la nuit 4 (avant) et indice sombre (après) ; vue du joueur et témoin à 7 m |
| `Planche_Marquages_Ennemis.jpg` | goule qui bondit (4 m et 11 m), volant avant le piqué, volant tenu, main et revers du traqueur : rouge de main (avant) et indices de la DA (après) |
| `Planche_Prise_Apparition.jpg` | objet tenu (violet de main / contour d'os et fil discret) ; marque d'apparition (rouge / sombre) |
| `Planche_Livre.jpg` | le hibou équipé, livre en main : drain sur une goule, soin sur un allié, coup sans cible (en vol, accroche ou bout, tenu ou retrait) |
| `Planche_Apparitions.jpg` | goule qui sort d'un trou, d'une grille, de l'ombre (0,25, 0,5, 0,95 s) |

## Ce qui change

- **Plus de rouge** : shader `ItHunts/VFX/MarquageSol` aux valeurs sombres (flaque d'ombre, trait d'encre fumée gris chaud à
  0,75 d'opacité, ton d'os à l'impact) ; boss et apparitions changent sans code. Indices `M_VFX_Indice_Cercle` / `_Rect` pour
  la goule et le traqueur ; contour d'encre fumée pour le volant avant le piqué ; contour d'os pour un volant ou un objet
  tenu ; `VFX_Trait_Prise`.
- **Lien du livre** : `LienVfx.Lance` à chaque appui, même sans cible (va au bout de la portée, se rétracte, se détruit) ;
  accroche visible (anneau de 0,85 m qui se referme, crochets, crochets qui tournent tant que le lien tient) ; drain rouge
  sombre, soin bleu-vert, intensité ≤ 1 (pas de bloom) ; bande effilée près du livre. `Regle` en positions monde, ignoré
  après un lancer sans cible ; rétraction seule si la cible disparaît ; aucun matériau instancié (bloc de propriétés).
- **Apparitions** : `VFX_Apparition_Trou`, `_Grille`, `_Ombre` (sans lumière ; terre, rouille, encre ; flaque d'ombre sans
  contour).
- **Épée du koala** : 3 vides et 3 impacts (`Assets/Art/Audio/Armes/Sfx_Epee_*`), crête des vides à 44 ms, fondus.

Branchements et valeurs pour main : `docs/art/ART-HANDOFF.md`, section « Nuit 5, lot 4 ».

## Relecture indépendante

Une relecture a rendu 21 points (6 bloquants). Corrigés : liens trop vifs (intensité ramenée à 1, tons sombres, anneau
réduit) ; captures du livre refaites avec le hibou équipé et le soin sur un allié ; halo orange de la lanterne du joueur
retiré des vues extérieures (et signalé à main) ; fuite de matériau de `LienVfx` ; consignes du livre réécrites pas à pas ;
indices rendus lisibles (bord à 0,75) ; doc du traqueur et fichiers exacts ; flaque sans anneau ; poussière en volutes ;
goule de l'apparition « ombre » activée sous la fumée (plus de mise à l'échelle) ; sons recalés et adoucis ; `Regle` après
un lancer sans cible, cible perdue, `Ferme` en vol, gradient mis en cache, éditeur ; durée et manifeste corrigés.

Seconde passe : 2 bloquants restants, corrigés. La salle était noyée de bleu par la lanterne du hibou (réglage de main) :
valeur désaturée proposée et utilisée dans les captures ; le livre spectral (matériaux de la DA) est baissé d'environ moitié.
Le lien partait des doigts : il part du centre du livre (doc de main corrigée). Corrigés aussi : anneau d'accroche en ton d'os
(visible sur le drain), trou d'ombre presque opaque sous la goule, `GameVfx.Spawn(…, life: 0f)`, `TargetPoint` à exposer,
portée améliorée du joueur, phrase sur la synchro des sons, rétraction sans `Regle` même si la cible vit, code mort.

## Limites

- Les « après » de la goule, du volant, du traqueur et de l'objet tenu sont posés par l'outil de capture (matériaux de la DA
  sur les objets de main) : le code de main n'est pas encore branché.
- Le jeu n'a pas de corps de joueur vu de l'extérieur : le soin s'accroche au point du corps d'un allié (repère vide).
- La grille des apparitions est une grille de démonstration (fer rouillé de la bibliothèque) : le jeu n'en a pas.
- Le sol de l'arène est rougi par la lanterne du joueur et ses fissures : ce n'est pas un marquage. Le boss (9 m) est coupé
  à la taille dans la vue du joueur de la Charge : c'est le cadrage réel à 13 m.
- Lisibilité des indices (trait gris chaud à 0,75) pour les attaques qui tuent en un coup (main et revers du traqueur) : à
  valider en jeu par l'utilisateur ; le bord se règle par `_OpaciteBord` et `_CouleurBord` du matériau.
- Droits d'usage commercial des sons générés (Higgsfield, Mirelo) : à confirmer par l'utilisateur (manifeste).
