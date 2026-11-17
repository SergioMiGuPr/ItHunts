"""Index de matériau par îlots : le corps retopologisé (plus grande île) = 0 ; pièces d'origine : mains = 1, tête et pieds = 2
(classées par le plan de coupe dont elles dépassent, comme stage3.py)."""
import os, json, bpy, bmesh, collections
import numpy as np
from mathutils import Vector
OUT = r"C:\Users\PC\AppData\Local\Temp\claude\fa13b7ae4f76688a\5c2bc682-360e-47d5-a52c-698998cd4727\scratchpad\traq2"
ob = bpy.data.objects["SK_Traqueur"]; me = ob.data
B = {n: (Vector(h), Vector(t)) for n, h, t, p, c in json.load(open(os.path.join(OUT, "stage1.json")))["table"]}
cuts = []
for s in ("L", "R"):
    h, t = B["Main." + s]; d = (t - h).normalized(); cuts.append(("main", h - d * 0.6, d))
    h, t = B["Jambe." + s]; d = (t - h).normalized(); ph, pt = B["Pied." + s]; cuts.append(("pied", ph - d * 0.55, d))
h, t = B["Cou"]; d = (t - h).normalized(); cuts.append(("tete", h + (t - h) * 0.75, d))
bm = bmesh.new(); bm.from_mesh(me); bm.faces.ensure_lookup_table()
comp = [-1] * len(bm.faces); nc = 0
for f in bm.faces:
    if comp[f.index] >= 0: continue
    st = [f]; comp[f.index] = nc
    while st:
        x = st.pop()
        for e in x.edges:
            for g in e.link_faces:
                if comp[g.index] < 0: comp[g.index] = nc; st.append(g)
    nc += 1
cen = np.array([f.calc_center_median()[:] for f in bm.faces]); bm.free()
comp = np.array(comp); sizes = collections.Counter(comp.tolist()); body = max(sizes, key=sizes.get)
mi = np.zeros(len(cen), int)
for c in range(nc):
    if c == body: continue
    P = cen[comp == c].mean(0)
    best = max(cuts, key=lambda k: float(np.dot(P - np.array(k[1][:]), np.array(k[2][:]))) - 0.05 * float(np.linalg.norm(P - np.array(k[1][:]))))
    if best[0] != "main":
        mi[comp == c] = 2; continue
    # main : M_Traqueur_Mains (clair) seulement après le poignet (tête de l'os Main recalé) ; avant, matériau 2
    s_ = "L" if P[0] < 0 else "R"
    mb = bpy.data.objects["Traqueur"].data.bones["Main." + s_]
    W0 = np.array(mb.head_local[:]); dW = np.array((mb.tail_local - mb.head_local).normalized()[:])
    sel = comp == c
    mi[sel] = np.where(((cen[sel] - W0) @ dW) > 0.0, 1, 2)
me.polygons.foreach_set("material_index", mi.astype(np.int32)); me.update()
print("îlots %d ; faces par matériau %s" % (nc, collections.Counter(mi.tolist())))
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "stage4.blend"))
