import os, time
import bpy, bmesh
import numpy as np
from mathutils import Vector
OUT = r"C:\Users\PC\AppData\Local\Temp\claude\fa13b7ae4f76688a\5c2bc682-360e-47d5-a52c-698998cd4727\scratchpad\traq2"
VOX = float(os.environ.get("VOX", "0.045"))
TARGET = int(os.environ.get("TARGET", "11000"))
# fichier ouvert par la ligne de commande (blender -b stage1.blend)
src = bpy.data.objects["Traq_Source"]
sc = bpy.context.scene
t0 = time.time()
me = src.data.copy(); ob = bpy.data.objects.new("Traq_Retopo", me); sc.collection.objects.link(ob)
for o in bpy.context.view_layer.objects: o.select_set(False)
bpy.context.view_layer.objects.active = ob; ob.select_set(True)
# trous bouchés (calotte des cheveux retirés, orteils) : le volume doit être fermé pour les voxels
bm = bmesh.new(); bm.from_mesh(ob.data)
n_fill = 0
for _ in range(3):
    left = {e for e in bm.edges if e.is_boundary}
    while left:
        e = left.pop(); loop = [e]; v = e.verts[1]; start = e.verts[0]; ok = True
        while v is not start:
            nxt = [f for f in v.link_edges if f in left]
            if not nxt:
                ok = False; break
            f = nxt[0]; left.discard(f); loop.append(f); v = f.other_vert(v)
        if ok and len(loop) >= 3:
            try:
                r_ = bmesh.ops.contextual_create(bm, geom=loop)
                n_fill += len(r_["faces"])
            except Exception as ex:
                pass
print("faces de bouchage", n_fill)
bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 4])
bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
print("bords restants", sum(1 for e in bm.edges if e.is_boundary))
bm.to_mesh(ob.data); bm.free()
m = ob.modifiers.new("Vox", "REMESH"); m.mode = "VOXEL"; m.voxel_size = VOX; m.adaptivity = 0.0; m.use_smooth_shade = True
dg = bpy.context.evaluated_depsgraph_get()
me_v = bpy.data.meshes.new_from_object(ob.evaluated_get(dg))
ob.modifiers.clear(); old = ob.data; ob.data = me_v; bpy.data.meshes.remove(old)
bmv = bmesh.new(); bmv.from_mesh(ob.data)
print("voxel : bords %d, non manifold %d" % (sum(1 for e in bmv.edges if e.is_boundary), sum(1 for e in bmv.edges if not e.is_manifold)))
# coques : on garde la plus grande (les autres sont des cavités internes nées des auto-intersections)
bmv.verts.ensure_lookup_table()
seen = {}; comps = []
for v in bmv.verts:
    if v in seen: continue
    st = [v]; seen[v] = len(comps); members = [v]
    while st:
        x = st.pop()
        for e in x.link_edges:
            w = e.other_vert(x)
            if w not in seen:
                seen[w] = len(comps); st.append(w); members.append(w)
    comps.append(members)
comps.sort(key=len, reverse=True)
print("coques", len(comps), [len(c) for c in comps[:8]])
drop = [v for c in comps[1:] for v in c]
bmesh.ops.delete(bmv, geom=drop, context="VERTS")
# arêtes quasi nulles (QuadriFlow les refuse : < 0.1 mm) : sommets fusionnés à moins de 3 mm, faces dégénérées dissoutes
print("arêtes < 0.2 mm avant lissage %d" % sum(1 for e in bmv.edges if e.calc_length() < 2e-4))
for _ in range(3):
    bmesh.ops.smooth_vert(bmv, verts=bmv.verts, factor=0.5, use_axis_x=True, use_axis_y=True, use_axis_z=True)
print("après lissage : arêtes < 0.2 mm %d, plus courte %.5f m" % (sum(1 for e in bmv.edges if e.calc_length() < 2e-4), min(e.calc_length() for e in bmv.edges)))
# sommets non manifold (pincements entre deux nappes) : faces autour supprimées puis trous refermés, deux passes
for it in range(4):
    bad = [v for v in bmv.verts if not v.is_manifold]
    print("passe %d : sommets non manifold %d, arêtes non manifold %d" % (it, len(bad), sum(1 for e in bmv.edges if not e.is_manifold)))
    if not bad:
        break
    fs = list({f for v in bad for f in v.link_faces})
    bmesh.ops.delete(bmv, geom=fs, context="FACES")
    bmesh.ops.delete(bmv, geom=[v for v in bmv.verts if not v.link_faces], context="VERTS")
    left = {e for e in bmv.edges if e.is_boundary}
    while left:
        e = left.pop(); loop = [e]; v = e.verts[1]; start = e.verts[0]; ok = True
        while v is not start:
            nxt = [f for f in v.link_edges if f in left]
            if not nxt:
                ok = False; break
            f = nxt[0]; left.discard(f); loop.append(f); v = f.other_vert(v)
        if ok and len(loop) >= 3:
            try:
                bmesh.ops.contextual_create(bmv, geom=loop)
            except Exception:
                pass
    bmesh.ops.triangulate(bmv, faces=[f for f in bmv.faces if len(f.verts) > 4])
bmesh.ops.recalc_face_normals(bmv, faces=bmv.faces)
incons = 0
for e in bmv.edges:
    if len(e.link_loops) == 2:
        a, b = e.link_loops
        if a.vert is b.vert:
            incons += 1
print("arêtes à orientation incohérente", incons, "faces dégénérées", sum(1 for f in bmv.faces if f.calc_area() < 1e-10))
bmv.to_mesh(ob.data); bmv.free(); ob.data.update()
print("voxel : %d faces, %.0f s" % (len(ob.data.polygons), time.time() - t0))
with bpy.context.temp_override(object=ob, active_object=ob, selected_objects=[ob], selected_editable_objects=[ob]):
    r = bpy.ops.object.quadriflow_remesh(target_faces=TARGET, use_mesh_symmetry=(os.environ.get("SYM","0")=="1"), use_preserve_sharp=False,
                                         use_preserve_boundary=False, smooth_normals=False, mode="FACES", seed=3)
print("quadriflow", r, "%d faces, %.0f s" % (len(ob.data.polygons), time.time() - t0))
# projection sur la surface d'origine
# projection sur la surface d'origine, plafonnée (au-delà, le point le plus proche est sur une autre nappe : bouche, cavité)
from mathutils.bvhtree import BVHTree
SNAP = float(os.environ.get("SNAP", "0.15"))
dg = bpy.context.evaluated_depsgraph_get()
bvh = BVHTree.FromObject(src, dg)
moved = kept = 0
for v in ob.data.vertices:
    loc, nrm, idx, dist = bvh.find_nearest(v.co)
    if loc is not None and dist < SNAP:
        v.co = loc; moved += 1
    else:
        kept += 1
print("projection : %d sommets posés sur la surface d'origine, %d gardés (plus loin que %.2f m)" % (moved, kept, SNAP))
me = ob.data
nq = sum(1 for p in me.polygons if len(p.vertices) == 4)
val = np.zeros(len(me.vertices), int)
for e in me.edges:
    for v in e.vertices: val[v] += 1
L = sorted(((me.vertices[e.vertices[0]].co - me.vertices[e.vertices[1]].co).length, e.index) for e in me.edges)
print("arêtes : médiane %.3f m, max %s" % (L[len(L)//2][0], [round(x[0], 2) for x in L[-6:]]))
print("faces %d (quads %d), sommets %d, valence 4 : %.0f %%, triangles %d" % (len(me.polygons), nq, len(me.vertices), 100 * (val == 4).mean(), sum(len(p.vertices) - 2 for p in me.polygons)))
src.hide_render = True
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "stage2.blend"))
