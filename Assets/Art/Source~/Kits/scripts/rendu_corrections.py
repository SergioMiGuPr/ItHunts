# -*- coding: utf-8 -*-
"""Nuit 4, lot 3, relecture : rendus « avant » (FBX du commit 5fa0c32, extraits à part) et « après » (FBX actuels) des pièces
corrigées, de près quand la correction est petite, avec les mêmes réglages que rendu_kits.py.

    blender -b --factory-startup -P rendu_corrections.py -- <dossier des FBX avant> <dossier de sortie> [<nom> …]

Images : <sortie>/<nom>_avant.jpg et <nom>_apres.jpg (une seule image « après » pour les pièces nouvelles).
"""
import os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import rendu_kits as R

ROOT = R.ROOT
KIT = {"Cave": "KitCaves", "Chateau": "KitChateau", "Jardin": "KitJardin"}

# nom : (cible (x, y, z, rayon) en axes Blender ou None, surfaces de collision visibles)
PIECES = {
    "SM_Cave_PorteVerrouillee": ((0, 0, 2.7, 3.6), False),
    "SM_Chateau_PorteVerrouillee": ((0, 0, 2.7, 3.6), False),
    "SM_Jardin_PorteVerrouillee": ((0, 0, 3.0, 4.2), False),
    "SM_Cave_MurEscalade_12m": ((0, -0.3, 5.0, 4.6), False),
    "SM_Chateau_MurEscalade_12m": ((0, -0.3, 5.0, 4.6), False),
    "SM_Chateau_Fenetre_Lancette_Simple": ((0, 0.6, 6.3, 3.9), False),
    "SM_Chateau_Fenetre_Lancette_Double": ((0, 0.6, 6.4, 5.2), False),
    "SM_Chateau_Fenetre_Lancette_Triple": ((0, 0.6, 10.6, 8.4), False),
    "SM_Chateau_Lustre_Cristal": ((0, 0, -1.9, 1.7), False),
    "SM_Chateau_Torche_Murale": ((0, -0.25, 0.1, 0.75), False),
    "SM_Chateau_Torchere": ((0, 0, 1.15, 1.35), False),
    "SM_Cave_Puits": (None, False),
    "SM_Jardin_Puits": (None, False),
    "SM_Cave_PorteSortie": (None, False),
    "SM_Chateau_PorteSortie": (None, False),
    "SM_Jardin_PorteSortie": (None, False),
    "SM_Chateau_GrandeEntree": (None, False),
    "SM_Cave_CelluleTraqueur_Herse": (None, False),
    "SM_Chateau_CelluleTraqueur": (None, False),
    "SM_Cave_CelluleTraqueur_Barricade": (None, False),
    "SM_Jardin_CelluleTraqueur_Voliere": (None, False),
    "SM_Cave_Escalier_Droit_4m": (None, True),
    "SM_Cave_Escalier_Tournant_4m": (None, True),
    "SM_Cave_Escalier_Colimacon": (None, True),
    "SM_Chateau_Escalier_Droit_4m": (None, True),
    "SM_Chateau_Escalier_Tournant_4m": (None, True),
    "SM_Chateau_Escalier_Colimacon": (None, True),
    "SM_Jardin_Escalier_Droit_3m": (None, True),
    "SM_Jardin_Escalier_Tournant_4m": (None, True),
    "SM_Jardin_Escalier_Colimacon": (None, True),
    "SM_Jardin_Mezzanine_8x6": (None, False),
    "SM_Jardin_Coursive_8m": (None, False),
    "SM_Jardin_PorteHaute": (None, False),
    "SM_Jardin_PorteEtroite": (None, False),
    "SM_Jardin_MurRebords_10m": (None, False),
    "SM_Jardin_MurEscalade_12m": (None, False),
    "SM_Jardin_Falaise_Rebords_16m": (None, False),
    "SM_Jardin_Terrasse_8m": (None, False),
    "SM_Jardin_Terrasse_Acces_8m": (None, False),
    "SM_Jardin_MurEnceinte_Tour": (None, False),
    "SM_Jardin_MurEnceinte_Breche_8m": (None, False),
    "SM_Jardin_MurEnceinte_CheminRonde_8m": (None, False),
    "SM_Jardin_Haie_6m": (None, False),
    "SM_Jardin_Massif": (None, False),
    "SM_Jardin_Foret_Lisiere_20m": (None, False),
}

if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:]
    avant, out = os.path.abspath(argv[0]), os.path.abspath(argv[1])
    noms = argv[2:] or list(PIECES)
    os.makedirs(out, exist_ok=True)
    for n in noms:
        cible, col = PIECES[n]
        fa = os.path.join(avant, n + ".fbx")
        if os.path.exists(fa):
            R.rend(fa, out, sortie=n + "_avant", cible=cible, montrer_col=False)
        fb = os.path.join(ROOT, "Assets", "Art", "Models", KIT[n.split("_")[1]], n + ".fbx")
        R.rend(fb, out, sortie=n + "_apres", cible=cible, montrer_col=col)
