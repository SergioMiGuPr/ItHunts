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

6. `tete_fix.py` (correctif du 2026-11-17, à lancer **une seule fois** sur le `SK_Traqueur.blend` de la PR #13, avec
   `stage1.json` et `stage3.blend` dans `OUT`) : tête refaite depuis la source sans borne de rayon, calotte, pupilles agrandies
   dans `T_Traqueur_Emission` ; `EXPORT=1` écrit le FBX, le .blend et la texture. `TeteShot.cs` : contrôle dans Unity (trous,
   lueur des pupilles à 10, 30 et 60 m dans le brouillard des niveaux).

Le solveur de chaleur de Blender échoue sur ce squelette (tous les maillages, toutes les échelles) : poids par diffusion.
