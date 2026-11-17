"""Étape 5 : mèches (construites dans la pose voûtée, ramenées à la liaison), matériaux, export FBX et .blend, mesures."""
import sys, os, math, shutil, json
sys.path.insert(0, r"C:\Users\PC\ItHunts\ItHunts-art\Assets\Art\Source~\Ennemis\scripts")
for m in ("sk_traqueur", "sm_traqueur", "peinture", "ennemis_common", "poids_lisses"):
    sys.modules.pop(m, None)
import sk_traqueur as K
import numpy as np
import bpy, bmesh
from mathutils import Vector, Matrix, Quaternion
OUT = r"C:\Users\PC\AppData\Local\Temp\claude\fa13b7ae4f76688a\5c2bc682-360e-47d5-a52c-698998cd4727\scratchpad\traq2"
ROOT = r"C:\Users\PC\ItHunts\ItHunts-art"
TEXD = os.path.join(ROOT, "Assets", "Art", "Textures", "Ennemis")
C = K.C
sc = bpy.context.scene; vl = bpy.context.view_layer
ob = bpy.data.objects["SK_Traqueur"]; arm = bpy.data.objects["Traqueur"]
for o in list(sc.objects):
    if o.type == "MESH" and o not in (ob,):
        bpy.data.objects.remove(o, do_unlink=True)
corps = os.path.join(TEXD, "T_Traqueur_Corps_BaseColor.png")
shutil.copyfile(os.path.join(OUT, "T_Traqueur_Corps_BaseColor_comble.png"), corps)
# --- mèches : pose voûtée de base (Repos, image 1)
arm.animation_data.action = bpy.data.actions["Traqueur_Repos"]; sc.frame_set(1); vl.update()
tb = arm.pose.bones["Tete"]
Mpose = arm.matrix_world @ tb.matrix                     # repère posé de la tête
Mrest = arm.matrix_world @ tb.bone.matrix_local
head, tail = Mpose.translation, Mpose @ Vector((0, tb.bone.length, 0))
rng = np.random.default_rng(3)
# crâne réel : sommets tenus par la tête (poids > 0.8), dans la pose voûtée
gi = ob.vertex_groups["Tete"].index
dgh = bpy.context.evaluated_depsgraph_get(); mev = ob.evaluated_get(dgh).to_mesh()
HP = np.array([mev.vertices[v.index].co[:] for v in ob.data.vertices if any(g.group == gi and g.weight > 0.8 for g in v.groups)])
ob.evaluated_get(dgh).to_mesh_clear()
c = Vector(np.median(HP, 0)); R = float(np.percentile(np.linalg.norm(HP - np.array(c[:]), axis=1), 60))
c = c + Vector((0, -0.25 * R, 0.35 * R))                     # vers le haut et l'arrière du crâne
print("crâne : centre %s, rayon %.2f m, %d sommets" % (tuple(round(x, 2) for x in c), R, len(HP)))
side, back, up = Vector((1, 0, 0)), Vector((0, -1, 0)), Vector((0, 0, 1))
me = bpy.data.meshes.new("SK_Traqueur_Cheveux"); bm = bmesh.new()
for k in range(7):
    a = math.pi * (0.22 + 0.56 * k / 6)
    root = c + (side * math.cos(a) * 0.9 + back * math.sin(a) * 1.0 + up * 0.15) * R + back * R * 0.25
    L = 4.6 + rng.uniform(-0.8, 1.0); pts = [root]
    # vers l'arrière, par-dessus les épaules (la tête du géant voûté avance loin devant le torse)
    dirv = (side * math.cos(a) * 0.35 + back * 1.0 + up * 0.35).normalized(); step = L / 8
    for j in range(8):
        dirv = (dirv * 0.82 - up * 0.18 + back * 0.05 + Vector(rng.normal(0, 0.04, 3))).normalized(); pts.append(pts[-1] + dirv * step)
    w0 = 0.42 + rng.uniform(-0.08, 0.12)
    for j in range(8):
        K.capsule(bm, pts[j], pts[j + 1], w0 * (1 - j / 8) ** 0.7 + 0.04, w0 * (1 - (j + 1) / 8) ** 0.7 + 0.04, n=6, rings=1, up=up)
bm.to_mesh(me); bm.free()
me.transform(Mrest @ Mpose.inverted())                   # ramenées à la pose de liaison (suivent la tête)
me.flip_normals()                                        # les capsules sortent retournées : faces vers l'extérieur
hair = bpy.data.objects.new("SK_Traqueur_Cheveux", me); sc.collection.objects.link(hair)
g = hair.vertex_groups.new(name="Tete"); g.add(list(range(len(me.vertices))), 1.0, "REPLACE")
hair.parent = arm; md = hair.modifiers.new("Armature", "ARMATURE"); md.object = arm
for p in me.polygons: p.use_smooth = True
# --- matériaux (noms remappés dans Unity vers les .mat du même nom)
mats = [C.make_material("M_Traqueur", corps),
        C.make_material("M_Traqueur_Mains", os.path.join(TEXD, "T_Traqueur_BaseColor.png")),
        C.make_material("M_Traqueur_Tete", os.path.join(TEXD, "T_Traqueur_BaseColor.png"))]
MI = np.zeros(len(ob.data.polygons), np.int32); ob.data.polygons.foreach_get("material_index", MI)
ob.data.materials.clear()
for m in mats: ob.data.materials.append(m)
ob.data.polygons.foreach_set("material_index", MI); ob.data.update()
print("faces par matériau", np.bincount(MI))
mh = bpy.data.materials.get("M_Traqueur_Cheveux") or bpy.data.materials.new("M_Traqueur_Cheveux")
mh.diffuse_color = (0.02, 0.018, 0.022, 1); me.materials.append(mh)
# --- mesures utiles à main (pose de Repos, image 1, avant +Y dans le .blend = +Z dans Unity)
vl.update()
R_ = {}
for s in "LR":
    R_["épaule " + s] = tuple(round(x, 2) for x in (arm.matrix_world @ arm.pose.bones["Bras." + s].head))
    R_["longueur bras+avant-bras " + s] = round(arm.data.bones["Bras." + s].length + arm.data.bones["AvantBras." + s].length, 2)
dg = bpy.context.evaluated_depsgraph_get(); mm = ob.evaluated_get(dg).to_mesh()
R_["sommet (repos)"] = round(max(v.co.z for v in mm.vertices), 2); ob.evaluated_get(dg).to_mesh_clear()
R_["triangles corps"] = C.tri_count(ob); R_["triangles mèches"] = C.tri_count(hair); R_["os"] = len(arm.data.bones)
print(json.dumps(R_, ensure_ascii=False))
json.dump(R_, open(os.path.join(OUT, "mesures_finales.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
# --- export
arm.animation_data.action = bpy.data.actions["Traqueur_Repos"]; sc.frame_set(1)
K.export([arm, ob, hair], os.path.join(ROOT, "Assets", "Art", "Models", "Ennemis", "SK_Traqueur.fbx"))
acts = [a for a in bpy.data.actions if a.name.startswith("Traqueur_")]
blocks = {sc} | set(sc.objects) | {o.data for o in sc.objects if o.data is not None} | set(acts)
sc.use_fake_user = True
bpy.data.libraries.write(os.path.join(ROOT, "Assets", "Art", "Source~", "Ennemis", "SK_Traqueur.blend"), blocks,
                         path_remap="RELATIVE_ALL", fake_user=True, compress=True)
print("exporté :", len(acts), "clips")
