"""Exporte les triangles d'UV des faces du corps (matériau 0) de SK_Traqueur, pour le comblement de la texture."""
import os, bpy, numpy as np
OUT = r"C:\Users\PC\AppData\Local\Temp\claude\fa13b7ae4f76688a\5c2bc682-360e-47d5-a52c-698998cd4727\scratchpad\traq2"
me = bpy.data.objects["SK_Traqueur"].data
uv = me.uv_layers.active.data
me.calc_loop_triangles()
T = [[uv[l].uv[:] for l in t.loops] for t in me.loop_triangles if me.polygons[t.polygon_index].material_index == 0]
np.save(os.path.join(OUT, "uv_corps.npy"), np.array(T, np.float32))
print(len(T))
