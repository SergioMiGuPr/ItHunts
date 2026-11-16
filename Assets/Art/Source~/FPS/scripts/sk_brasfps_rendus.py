# -*- coding: utf-8 -*-
"""Rendus des bras FPS (Blender, sans export) : vue FPS de chaque personnage (caméra du jeu à 60°, mains à leur pose de
repos de FpsView, orbe greybox sur Socket_Orbe), une image par pose (main droite), les 4 bras côte à côte.
Écrit des PNG et un JSON dans docs/art/rendus/FPS/_bras_tmp/ ; brasfps_legendes.py fait les JPEG et les planches.

Lancer dans Blender :
    p = r"<dépôt>/Assets/Art/Source~/FPS/scripts/sk_brasfps_rendus.py"
    exec(compile(open(p, encoding="utf-8").read(), p, "exec"), {"__file__": p, "__name__": "__main__"})
"""
import sys, os, math, json, importlib.util
sys.dont_write_bytecode = True
HERE = os.path.dirname(os.path.abspath(__file__))
spec = importlib.util.spec_from_file_location("sk_brasfps_mod", os.path.join(HERE, "sk_brasfps.py"))
S = importlib.util.module_from_spec(spec)
S.__dict__["__file__"] = os.path.join(HERE, "sk_brasfps.py")
spec.loader.exec_module(S)
F, C = S.F, S.C
import bpy
from mathutils import Vector, Matrix, Euler

TMP = os.path.join(F.RENDER_DIR, "_bras_tmp")
MMAP = Matrix(((-1, 0, 0, 0), (0, 0, -1, 0), (0, 1, 0, 0), (0, 0, 0, 1)))     # Unity -> Blender
SFLIP = Matrix.Scale(-1, 4, (1, 0, 0))                                        # Unity -> axes du fichier (X inversé)
REST = {"D": ((0.21, -0.22, 0.47), (-15, -10, 0)), "G": ((-0.23, -0.25, 0.45), (-10, 12, 0))}   # FpsView, repère caméra
WRIST = 0.27                                                                  # poignet : z = 0.27 depuis le pivot (coude)


def unity_rot(e):
    """Quaternion.Euler(x, y, z) d'Unity : Z, puis X, puis Y."""
    x, y, z = (math.radians(a) for a in e)
    return (Matrix.Rotation(y, 4, "Y") @ Matrix.Rotation(x, 4, "X") @ Matrix.Rotation(z, 4, "Z"))


def place_fps(arm_ob, side):
    p, e = REST[side]
    Mu = Matrix.Translation(p) @ unity_rot(e) @ Matrix.Translation((0, 0, -WRIST))
    arm_ob.matrix_world = MMAP @ Mu @ SFLIP


def setup_render(sc, res=(1280, 720), bg=(0.06, 0.06, 0.07)):
    sc.render.engine = "BLENDER_WORKBENCH"
    sh = sc.display.shading
    sh.light = "STUDIO"; sh.color_type = "MATERIAL"
    sh.show_object_outline = True; sh.object_outline_color = (0.02, 0.02, 0.02)
    sh.show_cavity = True; sh.cavity_type = "BOTH"; sh.cavity_ridge_factor = sh.cavity_valley_factor = 1.0
    sh.show_specular_highlight = False
    sh.background_type = "WORLD"
    if sc.world is None:
        sc.world = bpy.data.worlds.new("Fond_Bras")
    sc.world.color = bg
    sc.view_settings.view_transform = "Standard"
    sc.render.resolution_x, sc.render.resolution_y = res
    sc.render.image_settings.file_format = "PNG"
    sc.display.render_aa = "8"


def camera(sc, loc, look_at, lens=None, fov_v=None):
    cd = bpy.data.cameras.new("_CamBras")
    cam = bpy.data.objects.new("_CamBras", cd)
    sc.collection.objects.link(cam); sc.camera = cam
    cd.clip_start = 0.005
    if fov_v:
        cd.sensor_fit = "VERTICAL"; cd.angle = math.radians(fov_v)
    if lens:
        cd.lens = lens
    d = (Vector(look_at) - Vector(loc)).normalized()
    cam.location = loc
    cam.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()      # haut de l'image = +Z Blender (+Y Unity)
    return cam


def set_pose(arm_ob, name, frame=None):
    act = bpy.data.actions[name]
    arm_ob.animation_data.action = act
    if hasattr(arm_ob.animation_data, "action_slot") and act.slots:
        arm_ob.animation_data.action_slot = act.slots[0]
    f = frame if frame is not None else S.FRAMES.get(name, (1,))[-1]
    bpy.context.scene.frame_set(f)
    bpy.context.view_layer.update()


def render(sc, path):
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True, scene=sc.name)


def orb_marker(sc, loc):
    me = bpy.data.meshes.new("_orbe")
    import bmesh
    bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=16, v_segments=10, radius=0.04); bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new("_orbe", me)
    sc.collection.objects.link(ob)
    me.materials.append(F.flat_material("_Rendu_Orbe", "#FF7A1A"))
    ob.location = loc
    return ob


def run(persos=None):
    os.makedirs(TMP, exist_ok=True)
    legend = dict(fps=[], poses=[], bras=[])
    for perso in (persos or S.PERSOS):
        # ---- bras droit (ses actions sont remplacées par celles du gauche quand on le construit : on le rend d'abord)
        sc, arm, mesh, socks, acts, tris = S.build_side(perso, "D")
        # ---- poses : main droite, vue de 3/4 avant (côté pouce, au-dessus)
        setup_render(sc, res=(720, 720))
        arm.matrix_world = MMAP @ SFLIP                       # repère de la main = monde (vue)
        wrist = MMAP @ Vector((0, 0, WRIST))
        tgt = MMAP @ Vector((0, 0, WRIST + 0.075))
        cam = camera(sc, tgt + (MMAP.to_3x3() @ Vector((-0.2, 0.24, 0.12))), tgt, lens=50)
        for name in S.POSES:
            set_pose(arm, name)
            fn = os.path.join(TMP, "Pose_%s_%s.png" % (perso, name))
            render(sc, fn)
            legend["poses"].append(dict(perso=perso, pose=name, png=os.path.basename(fn)))
        name = "Pose_Attraper"
        if True:
            for i, f in enumerate(S.FRAMES[name]):
                set_pose(arm, name, f)
                fn = os.path.join(TMP, "Pose_%s_%s_cle%d.png" % (perso, name, i + 1))
                render(sc, fn)
        # ---- bras entier, de profil 3/4 (repos)
        set_pose(arm, "Pose_Repos")
        bpy.data.objects.remove(cam)
        cam = camera(sc, MMAP @ Vector((-0.42, 0.22, 0.05)), MMAP @ Vector((0, 0, 0.21)), lens=50)
        fn = os.path.join(TMP, "Bras_%s.png" % perso)
        render(sc, fn)
        legend["bras"].append(dict(perso=perso, png=os.path.basename(fn), tris=tris))
        bpy.data.objects.remove(cam)
        # ---- vue FPS : les deux bras dans la scène du droit, à leur pose de repos de FpsView
        arm.animation_data.action = None
        scg, armg, meshg, socksg, actsg, trisg = S.build_side(perso, "G")
        for o in (armg, meshg) + tuple(socksg):
            if o.name not in sc.collection.objects:
                sc.collection.objects.link(o)
        place_fps(arm, "D"); place_fps(armg, "G")
        bpy.context.window.scene = sc
        S.apply_pose(arm, S.POSES["Pose_Repos"][0], mirror=False)      # droite : pose posée sur les os (sans action)
        set_pose(armg, "Pose_Repos")                                   # gauche : son action (miroir)
        bpy.context.view_layer.update()
        orb = orb_marker(sc, next(s for s in socks if s.name.startswith("Socket_Orbe")).matrix_world.translation)
        setup_render(sc, res=(1280, 720))
        cam = camera(sc, Vector((0, 0, 0)), Vector((0, -1, 0)), fov_v=60)
        fn = os.path.join(TMP, "FPS_%s.png" % perso)
        render(sc, fn)
        legend["fps"].append(dict(perso=perso, png=os.path.basename(fn), tris_d=tris, tris_g=trisg))
        bpy.data.objects.remove(cam); bpy.data.objects.remove(orb)
        print("rendus :", perso)
    with open(os.path.join(TMP, "legendes.json"), "w", encoding="utf-8") as f:
        json.dump(legend, f, ensure_ascii=False, indent=1)
    S.clear_actions()


if __name__ == "__main__":
    run()
