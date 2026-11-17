"""Étape 3 : montage hybride.
- Traq_Retopo (quads, boucles aux articulations) garde torse, bras jusqu'au milieu de l'avant-bras, jambes jusqu'au-dessus
  de la cheville, cou ; UV neuves, texture du corps cuite depuis l'original (T_Traqueur_Corps_BaseColor, 4096).
- Traq_Source (Tripo d'origine retouché) fournit mains, tête et pieds, avec leurs UV et textures d'origine.
- Coupes par le même plan des deux côtés, dans des zones rigides (avant-bras, cou, tibia) ; jupe rentrée sous la couture."""
import os, json, time, math
import bpy, bmesh
import numpy as np
from mathutils import Vector
OUT = r"C:\Users\PC\AppData\Local\Temp\claude\fa13b7ae4f76688a\5c2bc682-360e-47d5-a52c-698998cd4727\scratchpad\traq2"
TEXD = r"C:\Users\PC\ItHunts\ItHunts-art\Assets\Art\Textures\Ennemis"
sc = bpy.context.scene; vl = bpy.context.view_layer
src = bpy.data.objects["Traq_Source"]; ret = bpy.data.objects["Traq_Retopo"]
src.hide_render = False
B = {n: (Vector(h), Vector(t)) for n, h, t, p, c in json.load(open(os.path.join(OUT, "stage1.json")))["table"]}
t0 = time.time()


def cuts():
    """(nom, point, normale vers la pièce d'origine, rayon d'action)."""
    out = []
    for s in ("L", "R"):
        h, t = B["Main." + s]; d = (t - h).normalized()
        out.append(("main_" + s, h - d * 0.6, d, 3.5))
        h, t = B["Jambe." + s]; d = (t - h).normalized()            # vers le bas
        ph, pt = B["Pied." + s]
        out.append(("pied_" + s, ph - d * 0.55, d, 3.0))
    h, t = B["Cou"]; d = (t - h).normalized()
    out.append(("tete", h + (t - h) * 0.75, d, 2.6))
    return out


CUTS = cuts()


def side_mask(co, keep_piece):
    """Par sommet : True s'il appartient à une pièce d'origine (au-delà d'un plan, dans son rayon)."""
    m = np.zeros(len(co), bool)
    for nm, p, n, r in CUTS:
        P = np.array(p[:]); N = np.array(n[:])
        mm = (((co - P) @ N) > 0) & (np.linalg.norm(co - P, axis=1) < r)
        if nm == "tete":
            mm &= co[:, 2] > P[2] - 0.4
        m |= mm
    return m


def bisect_all(ob):
    bm = bmesh.new(); bm.from_mesh(ob.data)
    for nm, p, n, r in CUTS:
        geom = [f for f in bm.faces if (f.calc_center_median() - p).length < r]
        g = list({x for f in geom for x in list(f.verts) + list(f.edges)}) + geom
        bmesh.ops.bisect_plane(bm, geom=g, plane_co=p, plane_no=n, dist=1e-5)
    bm.to_mesh(ob.data); bm.free()


def keep_faces(ob, piece):
    """piece=True : ne garde que les pièces d'origine ; False : garde le reste."""
    bm = bmesh.new(); bm.from_mesh(ob.data)
    cen = np.array([f.calc_center_median()[:] for f in bm.faces])
    m = side_mask(cen, True)
    kill = [f for f, k in zip(bm.faces, m) if k != piece]
    bmesh.ops.delete(bm, geom=kill, context="FACES")
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context="VERTS")
    bm.to_mesh(ob.data); bm.free()


# --- 1. retopologie : coupée, UV neuves, cuisson de la texture du corps depuis l'original
bisect_all(ret); keep_faces(ret, False)
for o in vl.objects: o.select_set(False)
vl.objects.active = ret; ret.select_set(True)
bpy.ops.object.mode_set(mode="EDIT"); bpy.ops.mesh.select_all(action="SELECT")
bpy.ops.uv.smart_project(angle_limit=math.radians(60), island_margin=0.003, area_weight=0.0)
bpy.ops.object.mode_set(mode="OBJECT")
for p in ret.data.polygons: p.use_smooth = True
print("corps retopo : %d faces, %.0f s" % (len(ret.data.polygons), time.time() - t0))
sc.render.engine = "CYCLES"; sc.cycles.device = "CPU"; sc.cycles.samples = 1
img_src = bpy.data.images.load(os.path.join(TEXD, "T_Traqueur_BaseColor.png"), check_existing=True)
for m in src.data.materials:                      # la source émet sa couleur
    m.use_nodes = True; nt = m.node_tree
    for n in list(nt.nodes): nt.nodes.remove(n)
    tex = nt.nodes.new("ShaderNodeTexImage"); tex.image = img_src
    em = nt.nodes.new("ShaderNodeEmission"); out = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(tex.outputs["Color"], em.inputs["Color"]); nt.links.new(em.outputs[0], out.inputs[0])
tgt = bpy.data.materials.new("_cible"); tgt.use_nodes = True
img = bpy.data.images.new("T_Traqueur_Corps_BaseColor", 4096, 4096, alpha=False)
node = tgt.node_tree.nodes.new("ShaderNodeTexImage"); node.image = img; tgt.node_tree.nodes.active = node
ret.data.materials.clear(); ret.data.materials.append(tgt)
for o in vl.objects: o.select_set(False)
src.select_set(True); ret.select_set(True); vl.objects.active = ret
t = time.time()
bpy.ops.object.bake(type="EMIT", use_selected_to_active=True, cage_extrusion=0.08, max_ray_distance=0.3, margin=16)
img.filepath_raw = os.path.join(OUT, "T_Traqueur_Corps_BaseColor.png"); img.file_format = "PNG"; img.save()
print("texture du corps cuite en %.0f s" % (time.time() - t))

# --- 2. pièces d'origine : mains, tête, pieds (UV et textures d'origine)
pieces = src.copy(); pieces.data = src.data.copy(); pieces.name = "Traq_Pieces"; sc.collection.objects.link(pieces)
bisect_all(pieces); keep_faces(pieces, True)
# jupe : l'anneau de bord des pièces est prolongé de 6 cm sous la couture, rentré de 4 cm vers l'axe
bm = bmesh.new(); bm.from_mesh(pieces.data)
bnd = [e for e in bm.edges if e.is_boundary]
bv = {v for e in bnd for v in e.verts}
ext = bmesh.ops.extrude_edge_only(bm, edges=bnd)
newv = [g for g in ext["geom"] if isinstance(g, bmesh.types.BMVert)]
for v in newv:
    best = min(CUTS, key=lambda c: abs((v.co - c[1]).dot(c[2])) + 0.01 * (v.co - c[1]).length)
    nm, p, n, r = best
    radial = (v.co - p) - n * (v.co - p).dot(n)
    v.co = v.co - n * 0.06 - radial.normalized() * 0.04
bm.to_mesh(pieces.data); bm.free()
print("pièces d'origine : %d faces (jupe : %d sommets)" % (len(pieces.data.polygons), len(newv)))

# --- 3. réunion : matériaux 0 = corps (UV neuves), 1 = mains (d'origine), 2 = tête et pieds (d'origine)
m_corps = bpy.data.materials.new("M_Traqueur"); m_mains = bpy.data.materials.new("M_Traqueur_Mains"); m_tete = bpy.data.materials.new("M_Traqueur_Tete")
for o in (ret, pieces):
    pass
src_mi = [p.material_index for p in pieces.data.polygons]
ret.data.materials.clear(); ret.data.materials.append(m_corps); ret.data.materials.append(m_mains); ret.data.materials.append(m_tete)
for p in ret.data.polygons: p.material_index = 0
pieces.data.materials.clear(); pieces.data.materials.append(m_corps); pieces.data.materials.append(m_mains); pieces.data.materials.append(m_tete)
for p, mi in zip(pieces.data.polygons, src_mi):
    p.material_index = 1 if mi == 1 else 2
# même nom de couche d'UV pour la réunion
ret.data.uv_layers[0].name = "UVMap"; pieces.data.uv_layers.active.name = "UVMap"
for l in list(pieces.data.uv_layers)[1:]:
    pieces.data.uv_layers.remove(l)
for o in vl.objects: o.select_set(False)
ret.select_set(True); pieces.select_set(True); vl.objects.active = ret
with bpy.context.temp_override(active_object=ret, selected_editable_objects=[ret, pieces], selected_objects=[ret, pieces]):
    bpy.ops.object.join()
ob = ret; ob.name = "SK_Traqueur"; ob.data.name = "SK_Traqueur"
me = ob.data
print("réuni : %d faces, %d triangles ; matériaux %s" % (len(me.polygons), sum(len(p.vertices) - 2 for p in me.polygons),
      [sum(1 for p in me.polygons if p.material_index == i) for i in range(3)]))
src.hide_render = True; src.hide_set(True)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "stage3.blend"))
print("total %.0f s" % (time.time() - t0))
