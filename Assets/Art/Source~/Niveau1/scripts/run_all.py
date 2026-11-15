# -*- coding: utf-8 -*-
"""Régénère les six pièces du niveau 1 (modèles FBX, .blend, rendus). À lancer dans Blender :
    p = r"<dépôt>/Assets/Art/Source~/Niveau1/scripts/run_all.py"
    exec(compile(open(p, encoding="utf-8").read(), p, "exec"), {"__file__": p, "__name__": "__main__"})
"""
import os
HERE = os.path.dirname(os.path.abspath(__file__))
for name in ("sm_arc_couloir.py", "sm_pont_egout.py", "sm_poutre.py", "sm_porte_piece.py", "sm_colonne.py", "sm_grandeporte.py"):
    p = os.path.join(HERE, name)
    print("=" * 20, name)
    exec(compile(open(p, encoding="utf-8").read(), p, "exec"), {"__file__": p, "__name__": "__main__"})
