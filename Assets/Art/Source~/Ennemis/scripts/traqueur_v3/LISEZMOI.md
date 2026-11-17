# Traqueur v3 (méthode D, partie A)

Scripts de travail, dans l'ordre (`blb.sh fichier.blend script.py` : Blender 5.0 en arrière-plan sur un .blend ; les
fichiers intermédiaires vont dans le dossier de travail `OUT` en tête de chaque script, à adapter) :

1. `stage1.py` : pipeline d'origine (`sk_traqueur.py`) jusqu'au géant debout, sans voûte → `stage1.blend`, table des os.
2. `stage2.py` : trous bouchés, voxels (0.045 m), plus grande coque, lissage léger (QuadriFlow refuse les arêtes < 0.1 mm),
   QuadriFlow (`TARGET=22000`), projection plafonnée (0.15 m) sur la surface d'origine → `stage2.blend`.
3. `stage3.py` : montage hybride (corps retopologisé ; mains, tête, pieds d'origine), cuisson de la texture du corps.
4. `stage4.py` (+ `refit.py`) : os recalés, poids par régions (diffusion), normales des coutures, clips bornés, mesures,
   test d'étirement. `fixmat.py` : matériaux par îlots, `M_Traqueur_Mains` arrêté au poignet.
5. `uvdump.py` puis `inpaint.py` (Python) : comblement de la texture du corps ; `stage5.py` : mèches, matériaux, export FBX
   et `SK_Traqueur.blend`. `planche.py` : planche. `TraqUnity.cs`, `TraqShot.cs` : import, contrôle et captures dans Unity.

Le solveur de chaleur de Blender échoue sur ce squelette (tous les maillages, toutes les échelles) : poids par diffusion.
