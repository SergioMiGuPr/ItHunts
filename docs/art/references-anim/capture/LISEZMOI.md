# Vidéos prêtes pour une capture vidéo → 3D (DeepMotion, Rokoko Vision)

Une version par vidéo de `docs/art/references-anim/` : `<nom>_capture.mp4`. Rien n'a été envoyé à un service en ligne.

Traitement appliqué (`ffmpeg`) :
- 30 images/s constantes ;
- 1280 px de large ;
- marge grise de 15 % de chaque côté et de 7 % en haut et en bas : le corps ne touche plus les bords, ce qui aide le suivi ;
- contraste + 8 % ;
- pas de son.

Les vidéos sont déjà « une action par vidéo », sans coupe ni changement de plan.

## Ce que ces outils savent faire

DeepMotion Animate 3D et Rokoko Vision suivent un **squelette humain bipède**. Ils ne savent pas suivre une créature à quatre pattes : les mains au sol sont lues comme des pieds, ou perdues. Ils donnent :
- la pose du buste, des bras et des jambes en 3D, image par image ;
- un export FBX ou BVH, à recibler sur l'armature `Traqueur` (68 os) dans Blender, en gardant le verrou du modèle : rotations seules.

## Les vidéos qui en profiteraient le plus (par ordre)

| Vidéo | Pourquoi |
|---|---|
| `traqueur_ref_prise` | debout, un bras qui part droit devant : le suivi humain marche bien ; la torsion du buste et de l'épaule serait relevée exactement |
| `traqueur_ref_revers` | debout, balayage du bras avec torsion du buste : difficile à relever à la main de profil |
| `traqueur_ref_mains_levees` | debout, deux bras au-dessus de la tête |
| `traqueur_ref_main_plantee` | flexion du dos et des genoux, une main au sol : la profondeur (3D) aiderait |
| `traqueur_ref_repos` | respiration et balancement très fins : le suivi donnerait les petites amplitudes réelles |
| `traqueur_ref_porte_etroite`, `traqueur_ref_escalade`, `traqueur_ref_prise_rebord` | poses complexes ; le suivi serait partiel (bras au-dessus de la tête, corps plié) |
| `traqueur_ref_marche`, `_course_enragee*`, `_passage_bas`, `_bond*`, `_reception`, `_chaine_*` | à quatre pattes ou en l'air : **peu utile** avec ces outils ; les relevés à la main restent la référence |

`traqueur_ref_course_subjective` est filmée en vue subjective : elle est inutilisable pour une capture.
