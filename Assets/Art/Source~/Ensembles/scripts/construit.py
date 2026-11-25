# -*- coding: utf-8 -*-
"""blender -b --factory-startup -P construit.py -- [noms…]   exporte les pièces d'ensemble (toutes par défaut)."""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import ens_lib
import importlib
for m in ("pieces_commun", "pieces_chateau", "pieces_jardin"):
    if os.path.exists(os.path.join(ens_lib.HERE, m + ".py")):
        importlib.import_module(m)
a = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
if a and a[0].startswith("@"):
    a = [n for n, f in ens_lib.PIECES.items() if f.module == a[0][1:]]
ens_lib.exporte(set(a) or None)
