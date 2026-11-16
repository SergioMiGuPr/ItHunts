# -*- coding: utf-8 -*-
"""Planche des poses de l'orbe (référence pour les animations codées sur main ; rien n'est exporté).

Une image par état de docs/art/MATERIAUX.md (« Orbe : couleur du personnage, animations par action ») : repos,
traction, répulsion, objet tenu, lancer, refus, recharge, charge rechargée. Vue depuis l'œil du joueur (derrière,
au-dessus, à gauche de l'orbe), couleur du caméléon (orange). Le mouvement est montré par une surimpression de
3 instants (les positions passées en transparence) ; lueurs : halo ajouté au rendu.

Jauge des 6 tablettes : traction = 02 et 03 (en haut), répulsion = 04 (à gauche), mouvement = 01, 06, 05 (à droite
et en bas). Tablette allumée = charge prête.

Lancer dans Blender (après sm_orbe.py, ou seul) :
    p = r"<dépôt>/Assets/Art/Source~/FPS/scripts/sm_orbe_poses.py"
    exec(compile(open(p, encoding="utf-8").read(), p, "exec"), {"__file__": p, "__name__": "__main__"})
"""
import sys, os, math, importlib.util
sys.dont_write_bytecode = True
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.modules.pop("fps_common", None)
import fps_common as F
C = F.C
import bpy
import numpy as np
from mathutils import Vector, Matrix, Quaternion

spec = importlib.util.spec_from_file_location("sm_orbe_mod", os.path.join(HERE, "sm_orbe.py"))
O = importlib.util.module_from_spec(spec)
O.__dict__["__file__"] = os.path.join(HERE, "sm_orbe.py")
spec.loader.exec_module(O)

ORANGE = "#FF7A1A"          # caméléon
ORANGE_FLASH = "#FFD08A"    # éclat (lancer, charge rechargée)
ORANGE_DIM = "#8A3E0E"      # énergie en recharge, tablette qui clignote (mi-intensité)
GLYPHE_ETEINT = "#3A342E"
FOND = (0.006, 0.0055, 0.008)   # linéaire : ≈ #141317 à l'écran (fond sombre du jeu)
JAUGE = {"traction": (2, 3), "repulsion": (4,), "mouvement": (1, 6, 5)}
RES = 900


def U(x, y, z):
    return Vector((-x, -z, y))


def setup():
    sc = C.new_scene("Orbe_Poses")
    objs = O.build(O.PARAMS)
    bpy.context.view_layer.update()
    sc.render.engine = "BLENDER_WORKBENCH"
    sh = sc.display.shading
    sh.light = "STUDIO"; sh.color_type = "TEXTURE"
    sh.show_object_outline = True; sh.object_outline_color = (0.02, 0.02, 0.02)
    sh.show_cavity = True; sh.cavity_type = "BOTH"
    sh.cavity_ridge_factor = sh.cavity_valley_factor = 1.2
    sh.show_specular_highlight = False
    sh.background_type = "WORLD"
    if sc.world is None:
        sc.world = bpy.data.worlds.new("Fond_Orbe")
    sc.world.color = FOND
    sc.view_settings.view_transform = "Standard"
    sc.render.resolution_x = sc.render.resolution_y = RES
    sc.render.image_settings.file_format = "PNG"
    sc.display.render_aa = "8"
    cam_data = bpy.data.cameras.new("_PosesCam")
    cam = bpy.data.objects.new("_PosesCam", cam_data)
    sc.collection.objects.link(cam); sc.camera = cam
    cam_data.lens = 50; cam_data.clip_start = 0.01
    d = Vector((0.4, 1, 0.5)).normalized()           # œil du joueur : derrière (+Y Blender = -Z Unity), au-dessus, à gauche
    cam.location = d * 0.6
    cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
    parts = {o.name.split(".")[0]: o for o in objs}      # sans suffixe (.001) si la scène SM_Orbe est ouverte
    rest = {n: o.matrix_world.copy() for n, o in parts.items()}
    return sc, parts, rest


def set_color(name, hexa):
    m = bpy.data.materials[name]
    col = F.srgb(hexa)
    m.diffuse_color = (*col, 1.0)
    bsdf = next((n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED"), None)
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (*col, 1.0)


def gauge(lit, blink=(), flash=()):
    """lit : tablettes allumées (numéros 1 à 6) ; blink : à mi-intensité (clignote) ; flash : éclat."""
    for i in range(1, 7):
        c = GLYPHE_ETEINT
        if i in lit:
            c = ORANGE
        if i in blink:
            c = ORANGE_DIM
        if i in flash:
            c = ORANGE_FLASH
        set_color("M_Orbe_Tablette_%02d" % i, c)


def ring_axis(k):
    A = O.PARAMS["anneaux"][k - 1]
    return U(*A["axe"]).normalized(), U(*A["normale"]).normalized()


def pose(parts, rest, rings=None, ring_scale=1.0, align=None, crown_r=None, crown_rot=0.0, tilt=0.0, whole=None):
    """rings : {k: angle° autour de l'axe de l'anneau} ; align : normale commune (Blender) pour les 3 anneaux, avec un
    angle de rotation autour d'elle ({'n': v, 'spin': °}) ; crown_r : rayon de la couronne ; crown_rot : rotation de la
    couronne autour de l'axe de visée (°) ; tilt : inclinaison des tablettes vers le centre (°) ; whole : rotation de
    toute l'orbe autour de l'axe de visée (°)."""
    view_axis = Vector((0, -1, 0))                       # +Z Unity (vers l'avant, l'axe de visée)
    W = Matrix.Rotation(math.radians(whole or 0.0), 4, view_axis)
    for k in (1, 2, 3):
        o = parts["SM_Orbe_Anneau_%d" % k]
        ax, n = ring_axis(k)
        if align is not None:
            q = n.rotation_difference(align["n"])
            R = Matrix.Rotation(math.radians(align.get("spin", 0.0) + 40 * k), 4, align["n"]) @ q.to_matrix().to_4x4()
        else:
            R = Matrix.Rotation(math.radians((rings or {}).get(k, 0.0)), 4, ax)
        o.matrix_world = W @ R @ Matrix.Scale(ring_scale, 4)
    T = O.PARAMS["tablette"]
    for i in range(6):
        o = parts["SM_Orbe_Tablette_%02d" % (i + 1)]
        a = math.radians(T["angles"][i])
        c0 = U(T["rayon"] * math.cos(a), T["rayon"] * math.sin(a), 0)
        radial = c0.normalized()
        r = crown_r if crown_r is not None else T["rayon"]
        M = Matrix.Translation(radial * (r - T["rayon"]))
        if tilt:
            tang = U(-math.sin(a), math.cos(a), 0).normalized()
            M = M @ Matrix.Translation(c0) @ Matrix.Rotation(math.radians(tilt), 4, tang) @ Matrix.Translation(-c0)
        o.matrix_world = W @ Matrix.Rotation(math.radians(crown_rot), 4, view_axis) @ M
    e = parts["SM_Orbe_Energie"]
    e.matrix_world = W
    bpy.context.view_layer.update()


def render(sc, path):
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True, scene=sc.name)
    img = bpy.data.images.load(path, check_existing=False)
    px = np.array(img.pixels[:], dtype=np.float32).reshape(RES, RES, 4)[::-1, :, :3]
    bpy.data.images.remove(img)
    os.remove(path)
    return px


def glow(px, strength=1.0):
    """Halo : les pixels proches de l'orange (glyphes, énergie) sont flous et ajoutés (effet de bloom)."""
    o = F.srgb(ORANGE)
    target = np.array([c ** (1 / 2.2) for c in o], dtype=np.float32)
    lum = px.max(axis=2)
    mask = ((px[..., 0] > 0.55) & (px[..., 0] > px[..., 2] * 1.8) & (lum > 0.5)).astype(np.float32)
    k = 18
    f = np.fft.fft2(mask)
    fy = np.fft.fftfreq(RES)[:, None]; fx = np.fft.fftfreq(RES)[None, :]
    blur = np.real(np.fft.ifft2(f * np.exp(-2 * math.pi ** 2 * (fx ** 2 + fy ** 2) * k ** 2)))
    blur = np.clip(blur * 2.2 * strength, 0, 1)[..., None]
    return np.clip(px + blur * np.array([1.0, 0.45, 0.1], dtype=np.float32) * 0.8, 0, 1)


def onion(frames, weights):
    out = np.zeros_like(frames[0])
    for f, w in zip(frames, weights):
        out += f * w
    return out / sum(weights)


STATES = []


def state(nom, titre, texte):
    def deco(fn):
        STATES.append((nom, titre, texte, fn))
        return fn
    return deco


ALL = (1, 2, 3, 4, 5, 6)


@state("01_Repos", "Repos", "rotation lente et désynchronisée (20, 30, 45°/s) ; jauge pleine : 2 traction, 1 répulsion, 3 mouvement")
def s_repos(sc, parts, rest, tmp):
    gauge(ALL); set_color("M_Orbe_Energie", ORANGE)
    fr = []
    for t in (0.0, 0.25, 0.5):
        pose(parts, rest, rings={1: 20 * t, 2: -30 * t, 3: 45 * t})
        fr.append(render(sc, tmp))
    return glow(onion(fr, (1, 1, 2)), 0.8)


@state("02_Traction", "Traction", "les 3 anneaux se calent dans le plan face à la visée et tournent ensemble à 720°/s ; couronne à 90°/s ; une charge de traction consommée (03 éteinte)")
def s_traction(sc, parts, rest, tmp):
    gauge((1, 2, 4, 5, 6)); set_color("M_Orbe_Energie", ORANGE_FLASH)
    n = Vector((0, -1, 0))
    fr = []
    for i, spin in enumerate((0, 12, 24)):
        pose(parts, rest, align={"n": n, "spin": spin}, crown_rot=1.5 * i)
        fr.append(render(sc, tmp))
    return glow(onion(fr, (1, 1, 2)), 1.3)


@state("03_Repulsion", "Répulsion", "les anneaux s'écartent d'un coup (×1.35 en 0.06 s, basculés de 25°), tablettes projetées à 0.17 m ; charge de répulsion consommée (04 éteinte)")
def s_repulsion(sc, parts, rest, tmp):
    gauge((1, 2, 3, 5, 6)); set_color("M_Orbe_Energie", ORANGE_FLASH)
    fr = []
    for s, r, a in ((1.0, 0.13, 0), (1.18, 0.15, 12), (1.35, 0.17, 25)):
        pose(parts, rest, rings={1: a, 2: a, 3: a}, ring_scale=s, crown_r=r)
        fr.append(render(sc, tmp))
    return glow(onion(fr, (0.6, 0.8, 2)), 1.4)


@state("04_ObjetTenu", "Objet tenu", "anneaux resserrés autour de l'énergie (×0.8), gyroscope serré, rotation régulière ; couronne resserrée à 0.10 m, tablettes inclinées vers le centre")
def s_tenu(sc, parts, rest, tmp):
    gauge(ALL); set_color("M_Orbe_Energie", ORANGE)
    fr = []
    for t in (0.0, 0.1):
        pose(parts, rest, rings={1: 90 + 180 * t, 2: 30 - 180 * t, 3: -30 + 180 * t}, ring_scale=0.8, crown_r=0.10, tilt=-30)
        fr.append(render(sc, tmp))
    return glow(onion(fr, (1, 2)), 0.9)


@state("05_Lancer", "Lancer", "coup d'arrêt puis les anneaux pivotent de 90° en 0.08 s vers l'avant ; éclat bref des glyphes (0.12 s, ×3)")
def s_lancer(sc, parts, rest, tmp):
    gauge((), flash=ALL); set_color("M_Orbe_Energie", ORANGE_FLASH)
    fr = []
    for a in (0, 45, 90):
        pose(parts, rest, rings={1: a, 2: a * 0.6, 3: a * 0.3})
        fr.append(render(sc, tmp))
    return glow(onion(fr, (0.6, 0.8, 2)), 1.8)


@state("06_Refus", "Refus", "secousse sèche (±8°, 3 allers-retours en 0.15 s) ; plus de charge de traction : 02 et 03 clignotent")
def s_refus(sc, parts, rest, tmp):
    gauge((4, 1, 5, 6), blink=(2, 3)); set_color("M_Orbe_Energie", ORANGE)
    fr = []
    for w in (-8, 8, 0):
        pose(parts, rest, whole=w)
        fr.append(render(sc, tmp))
    return glow(onion(fr, (1, 1, 1.5)), 0.8)


@state("07_Recharge", "Recharge", "après un lancer : anneaux ralentis (×0.5), glyphes éteints jusqu'à la fin de la recharge, énergie atténuée")
def s_recharge(sc, parts, rest, tmp):
    gauge(()); set_color("M_Orbe_Energie", ORANGE_DIM)
    fr = []
    for t in (0.0, 0.25):
        pose(parts, rest, rings={1: 10 * t, 2: -15 * t, 3: 22 * t})
        fr.append(render(sc, tmp))
    return glow(onion(fr, (1, 2)), 0.5)


@state("08_ChargeRechargee", "Charge rechargée", "la tablette de la charge revenue s'allume par un éclat (0.1 s, ×2) puis reste allumée (ici 03, traction)")
def s_rechargee(sc, parts, rest, tmp):
    gauge((1, 2, 4, 5, 6), flash=(3,)); set_color("M_Orbe_Energie", ORANGE)
    pose(parts, rest)
    return glow(render(sc, tmp), 1.2)


def save_png(px, path):
    """Image composée (sans légende) en PNG ; la légende et le JPEG sont faits par orbe_poses_legendes.py (PIL)."""
    h, w = px.shape[:2]
    im = bpy.data.images.new("_pose", w, h, alpha=False)
    a = np.ones((h, w, 4), dtype=np.float32)
    a[..., :3] = np.clip(px[::-1], 0, 1)
    im.pixels.foreach_set(a.ravel())
    im.filepath_raw = path; im.file_format = "PNG"; im.save()
    bpy.data.images.remove(im)


def run():
    import json
    sc, parts, rest = setup()
    tmpdir = os.path.join(F.RENDER_DIR, "_poses_tmp")
    os.makedirs(tmpdir, exist_ok=True)
    tmp = os.path.join(tmpdir, "_frame.png")
    legend = []
    for nom, titre, texte, fn in STATES:
        px = fn(sc, parts, rest, tmp)
        save_png(px, os.path.join(tmpdir, "Orbe_Poses_%s.png" % nom))
        legend.append(dict(nom=nom, titre=titre, texte=texte))
        print("pose :", nom)
    with open(os.path.join(tmpdir, "legendes.json"), "w", encoding="utf-8") as f:
        json.dump(legend, f, ensure_ascii=False, indent=1)
    # couleurs des matériaux remises comme dans le FBX
    set_color("M_Orbe_Energie", O.PARAMS["couleurs"]["energie"])
    for i in range(1, 7):
        set_color("M_Orbe_Tablette_%02d" % i, O.PARAMS["couleurs"]["glyphe"])
    print("ensuite : python orbe_poses_legendes.py (légendes, JPEG, planche)")


if __name__ == "__main__":
    run()
