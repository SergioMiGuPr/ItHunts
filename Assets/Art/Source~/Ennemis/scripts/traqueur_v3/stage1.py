import sys, os, math
sys.path.insert(0, r"C:\Users\PC\ItHunts\ItHunts-art\Assets\Art\Source~\Ennemis\scripts")
for m in ("sk_traqueur", "sm_traqueur", "peinture", "ennemis_common", "poids_lisses"):
    sys.modules.pop(m, None)
import sk_traqueur as K
import numpy as np
import bpy, bmesh
from mathutils import Matrix, Vector
OUT = r"C:\Users\PC\AppData\Local\Temp\claude\fa13b7ae4f76688a\5c2bc682-360e-47d5-a52c-698998cd4727\scratchpad\traq2"
T, E, C, P = K.T, K.E, K.C, K.P
sc = C.new_scene("Traq2"); sc.render.fps = K.FPS
ob = E.import_glb(P["glb"])
bm = bmesh.new(); bm.from_mesh(ob.data)
bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-7)
bm.to_mesh(ob.data); bm.free()
px = E.source_pixels(ob, P["texture"])
print("cheveux", K.remove_hair(ob, px))
print(K.remake_toes(ob))
p_base, info, info_g, zones = K.repaint(ob, px)
print("texture", p_base)
K.set_new_uv(ob)
me = ob.data
V = T.verts(me)
eye_idx = int(np.argmin(np.linalg.norm(V - np.array((0.037, 0.012, 0.243)), axis=1)))
wa = K.arm_weights(V)
J = K.shorten_arms(ob)
M = Matrix.Rotation(math.pi / 2, 4, "Z") @ Matrix.Scale(K.S20, 4) @ Matrix.Translation((-K.X0, 0, -K.Z0))
me.transform(M)
mat = C.make_material("M_Traqueur", p_base)
mat_m = C.make_material("M_Traqueur_Mains", p_base)
me.materials.clear(); me.materials.append(mat); me.materials.append(mat_m)
for p in me.polygons:
    if np.mean(wa[list(p.vertices)]) > 0.5:
        p.material_index = 1
ob.name = me.name = "Traq_Source"
table = K.bone_table(J)
arm_ob = K.make_armature(table)
np.save(os.path.join(OUT, "Vraw.npy"), V)
np.save(os.path.join(OUT, "wa.npy"), wa)
import json
json.dump({"eye_idx": eye_idx, "p_base": p_base, "table": [(n, list(h), list(t), p, c) for n, h, t, p, c in table]},
          open(os.path.join(OUT, "stage1.json"), "w"))
print("tris", E.tri_count(ob), "verts", len(me.vertices), "bounds", [round(x, 2) for x in np.array([v.co[:] for v in me.vertices]).min(0)], [round(x, 2) for x in np.array([v.co[:] for v in me.vertices]).max(0)])
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "stage1.blend"))
# rendus de contrôle
sc.render.engine = "BLENDER_WORKBENCH"
sh = sc.display.shading; sh.light = "STUDIO"; sh.color_type = "TEXTURE"
sc.render.resolution_x, sc.render.resolution_y = 900, 900; sc.render.image_settings.file_format = "JPEG"
cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); sc.collection.objects.link(cam); sc.camera = cam
cam.data.type = "ORTHO"; cam.data.ortho_scale = 22
for nm, loc in (("face", (0, 40, 10)), ("profil", (40, 0, 10))):
    cam.location = loc; cam.rotation_euler = (Vector((0, 0, 10)) - Vector(loc)).to_track_quat("-Z", "Y").to_euler()
    sc.render.filepath = os.path.join(OUT, "s1_%s.jpg" % nm); bpy.ops.render.render(write_still=True)
