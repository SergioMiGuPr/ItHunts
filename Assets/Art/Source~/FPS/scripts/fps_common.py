# -*- coding: utf-8 -*-
"""Outils communs aux objets de la vue FPS (lanterne, orbe). S'appuie sur it_common (Niveau1).

Repère : coordonnées Blender (Z en haut, avant vers -Y). L'export FBX de it_common (avant -Z, haut Y,
transformations appliquées) donne dans Unity : +Y en haut, +Z vers l'avant, rotation nulle, échelle 1.
"""
import sys, os, math
sys.dont_write_bytecode = True
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "..", "Niveau1", "scripts"))
sys.modules.pop("it_common", None)
import it_common as C
import bpy
import numpy as np
from mathutils import Vector, Matrix

ROOT = C.ROOT
MODELS_DIR = os.path.join(ROOT, "Assets", "Art", "Models", "FPS")
SOURCE_DIR = os.path.join(ROOT, "Assets", "Art", "Source~", "FPS")
RENDER_DIR = os.path.join(ROOT, "docs", "art", "rendus", "FPS")


def srgb(hexa):
    """'#RRGGBB' (sRGB) -> couleur linéaire (entrée Base Color)."""
    out = []
    for i in (1, 3, 5):
        c = int(hexa[i:i + 2], 16) / 255
        out.append(c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4)
    return tuple(out)


def flat_material(name, hexa, alpha=1.0):
    """Couleur de base peinte en aplat : ni métal ni brillance. alpha < 1 : indication de translucidité
    (le shader du jeu décide ; ici seulement pour l'aperçu)."""
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    for n in list(nt.nodes):
        nt.nodes.remove(n)
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    nt.links.new(bsdf.outputs[0], out.inputs[0])
    col = srgb(hexa)
    bsdf.inputs["Base Color"].default_value = (*col, 1.0)
    for key, val in (("Metallic", 0.0), ("Roughness", 1.0), ("Specular IOR Level", 0.0), ("Alpha", alpha)):
        if key in bsdf.inputs:
            bsdf.inputs[key].default_value = val
    mat.diffuse_color = (*col, alpha)
    mat.metallic, mat.roughness = 0.0, 1.0
    return mat


def empty(name, loc, parent, size=0.01, sc=None):
    """Repère vide (flèches) ; sans rotation : dans Unity, son +Z est l'avant de l'objet."""
    other = bpy.data.objects.get(name)
    if other is not None:                    # même nom dans une autre scène de la session : on le libère,
        other.name = name + "_autre"         # sinon Blender suffixe (.001) et le FBX porte le mauvais nom
    ob = bpy.data.objects.new(name, None)
    ob.name = name
    ob.empty_display_type = "ARROWS"
    ob.empty_display_size = size
    (sc or bpy.context.scene).collection.objects.link(ob)
    bpy.context.view_layer.update()          # matrice du parent à jour avant le rattachement
    ob.parent = parent
    ob.matrix_parent_inverse.identity()
    ob.matrix_world = Matrix.Translation(Vector(loc))
    return ob


def attach(child, parent):
    """Rend child enfant de parent sans le déplacer (pivot conservé)."""
    bpy.context.view_layer.update()          # matrices à jour (sinon l'emplacement est compté deux fois)
    mw = child.matrix_world.copy()
    child.parent = parent
    child.matrix_parent_inverse.identity()
    child.matrix_world = mw


def verify_empties(path):
    """Réimport brut (axes du fichier = axes Unity avant le passage main gauche) : position et axes
    des repères vides. Unity inverse X ; +Y et +Z gardent leur sens."""
    prev = bpy.context.window.scene
    sc = bpy.data.scenes.new("_VerifVides")
    bpy.context.window.scene = sc
    bpy.ops.import_scene.fbx(filepath=path, use_manual_orientation=True, axis_forward="Y", axis_up="Z",
                             use_image_search=False)
    lines = []
    for ob in sc.objects:
        if ob.type == "EMPTY":
            m = ob.matrix_world
            z = m.col[2].xyz.normalized()
            y = m.col[1].xyz.normalized()
            p = m.translation
            lines.append("  repère %s : position Unity (%.4f, %.4f, %.4f) m, +Z local -> (%.2f, %.2f, %.2f), +Y local -> (%.2f, %.2f, %.2f)"
                         % (ob.name, -p.x, p.y, p.z, -z.x, z.y, z.z, -y.x, y.y, y.z))
    for ob in list(sc.objects):
        data = ob.data
        bpy.data.objects.remove(ob, do_unlink=True)
        if isinstance(data, bpy.types.Mesh) and data.users == 0:
            bpy.data.meshes.remove(data)
    bpy.data.scenes.remove(sc)
    for m in list(bpy.data.materials):
        if m.users == 0 and "." in m.name:
            bpy.data.materials.remove(m)
    bpy.context.window.scene = prev
    return "\n".join(lines)


def render(sc, objs, basename, views=("face", "cote", "34"), res=1024, suffix="", pad=1.2):
    """Rendus Workbench : lumière plate, couleurs des matériaux, contour, fond gris."""
    os.makedirs(RENDER_DIR, exist_ok=True)
    sc.render.engine = "BLENDER_WORKBENCH"
    sh = sc.display.shading
    sh.light = "FLAT"; sh.color_type = "TEXTURE"
    sh.show_object_outline = True; sh.object_outline_color = (0.05, 0.05, 0.05)
    sh.show_cavity = True; sh.cavity_type = "BOTH"
    sh.cavity_ridge_factor = sh.cavity_valley_factor = 1.5
    sh.curvature_ridge_factor = sh.curvature_valley_factor = 1.5
    sc.display.matcap_ssao_distance = 0.01
    if sc.world is None:
        sc.world = bpy.data.worlds.new("Fond_Gris")
    sc.world.color = (0.32, 0.32, 0.32)
    sc.view_settings.view_transform = "Standard"
    sc.render.resolution_x = sc.render.resolution_y = res
    sc.render.image_settings.file_format = "JPEG"
    sc.render.image_settings.quality = 90
    sc.display.render_aa = "8"
    mn = Vector((1e9, 1e9, 1e9)); mx = -mn
    for ob in objs:
        if ob.type != "MESH":
            continue
        for c in ob.bound_box:
            p = ob.matrix_world @ Vector(c)
            mn = Vector((min(mn.x, p.x), min(mn.y, p.y), min(mn.z, p.z)))
            mx = Vector((max(mx.x, p.x), max(mx.y, p.y), max(mx.z, p.z)))
    ctr, size = (mn + mx) / 2, mx - mn
    cam_data = bpy.data.cameras.new("_RenduCam")
    cam = bpy.data.objects.new("_RenduCam", cam_data)
    sc.collection.objects.link(cam); sc.camera = cam
    cam_data.clip_start, cam_data.clip_end = 0.001, 50
    dirs = {"face": Vector((0, -1, 0)), "cote": Vector((1, 0, 0)), "dessus": Vector((0, -0.0001, 1)),
            "dos": Vector((0, 1, 0)), "34": Vector((0.7, -1, 0.55)).normalized(),
            "oeil": Vector((0.4, 1, 0.5)).normalized()}   # œil du joueur : derrière (+Y Blender = -Z Unity), au-dessus à gauche
    out = []
    for v in views:
        d = dirs[v]
        if v in ("34", "oeil"):
            cam_data.type = "PERSP"; cam_data.lens = 50
            dist = size.length * 1.45
        else:
            cam_data.type = "ORTHO"
            w = {"face": max(size.x, size.z), "dos": max(size.x, size.z), "cote": max(size.y, size.z),
                 "dessus": max(size.x, size.y)}[v]
            cam_data.ortho_scale = w * pad
            dist = size.length * 2 + 0.5
        cam.location = ctr + d * dist
        cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
        path = os.path.join(RENDER_DIR, "%s_%s%s.jpg" % (basename, v, suffix))
        sc.render.filepath = path
        bpy.ops.render.render(write_still=True, scene=sc.name)
        out.append(path)
    bpy.data.objects.remove(cam); bpy.data.cameras.remove(cam_data)
    return out


def run(name, build, params, views=("face", "cote", "34")):
    sc = C.new_scene(name)
    objs = build(params)
    bpy.context.view_layer.update()
    fbx = os.path.join(MODELS_DIR, name + ".fbx")
    C.export_fbx(objs, fbx)
    C.save_blend(sc, os.path.join(SOURCE_DIR, name + ".blend"))
    res = C.verify_fbx(fbx)
    emp = verify_empties(fbx)
    bpy.context.window.scene = sc
    renders = render(sc, objs, name, views)
    print(C.report(res))
    print(emp)
    print("Rendus :", [os.path.basename(p) for p in renders])
    return res


# --------------------------------------------------------------------------- textures peintes (tuiles)
TEX_DIR = os.path.join(ROOT, "Assets", "Art", "Textures", "FPS")
TILE_M = 0.05            # une tuile de 256 px couvre 0.05 m : 5 120 px/m, comme T_Barillet_Laiton (512 px / 0.1 m)

# palettes relevées sur les fiches (médianes et quantiles, sans les traits d'encre)
PAINT = {
    "T_LanterneFeu_Fer_BaseColor": dict(ramp=["#2C2523", "#3B3230", "#4A403C", "#5A4E48"], rouille="#5A3426",
                                        rouille_part=0.07, trainees=(1.5, 22.0), seed=11),
    "T_LanterneFeu_Cuivre_BaseColor": dict(ramp=["#6E4335", "#8B594A", "#A86E5C", "#C8876A"], rouille="#4A3A33",
                                           rouille_part=0.08, trainees=(22.0, 1.5), seed=23),
    "T_Orbe_Pierre_BaseColor": dict(ramp=["#3A3837", "#4A4643", "#5A5550", "#706A63"], fissures=0.035,
                                    trainees=(4.0, 9.0), seed=37),
    "T_Orbe_Fer_BaseColor": dict(ramp=["#1E1C1B", "#2B2928", "#363331", "#45413E"], rouille="#3F2A22",
                                 rouille_part=0.06, trainees=(1.5, 22.0), seed=41),
}


def _hex01(h):
    return np.array([int(h[i:i + 2], 16) / 255 for i in (1, 3, 5)], dtype=np.float32)


def _noise(n, sx, sy, rng):
    """Bruit gaussien périodique (filtrage dans l'espace de Fourier) : la tuile se raccorde sans couture."""
    w = rng.standard_normal((n, n))
    fy = np.fft.fftfreq(n)[:, None]
    fx = np.fft.fftfreq(n)[None, :]
    k = np.exp(-2 * math.pi ** 2 * ((fx * sx) ** 2 + (fy * sy) ** 2))
    out = np.real(np.fft.ifft2(np.fft.fft2(w) * k))
    return (out - out.mean()) / (out.std() + 1e-8)


def painted_tile(name, spec, n=256, out_dir=None):
    """Tuile peinte « à la main » : grandes taches, aplats en paliers (rendu peint), traînées de pinceau
    dans une direction, taches de rouille ou fissures. Valeurs sRGB."""
    rng = np.random.default_rng(spec["seed"])
    blot = _noise(n, 28, 28, rng)
    mid = _noise(n, 7, 7, rng)
    sx, sy = spec["trainees"]
    streak = _noise(n, sx, sy, rng)
    t = 0.5 + 0.17 * blot + 0.08 * mid + 0.12 * streak
    steps = 4
    tq = np.floor(np.clip(t, 0, 0.999) * steps) / (steps - 1)
    t = np.clip(0.55 * tq + 0.45 * t, 0, 1)                       # paliers adoucis : aplats peints
    ramp = [_hex01(h) for h in spec["ramp"]]
    x = t * (len(ramp) - 1)
    i0 = np.clip(np.floor(x).astype(int), 0, len(ramp) - 2)
    fr = (x - i0)[..., None]
    R = np.stack(ramp)
    col = R[i0] * (1 - fr) + R[i0 + 1] * fr
    if "rouille" in spec:
        rust = _noise(n, 12, 12, rng)
        thr = np.quantile(rust, 1 - spec["rouille_part"])
        m = np.clip((rust - thr) * 3.0, 0, 1)[..., None] * 0.45
        col = col * (1 - m) + _hex01(spec["rouille"]) * m
    if "fissures" in spec:
        cr = _noise(n, 10, 10, rng)
        m = np.clip(1 - np.abs(cr) / spec["fissures"], 0, 1)[..., None]
        col = col * (1 - 0.45 * m)
        chips = _noise(n, 3, 3, rng)
        col = col + (chips > 2.3)[..., None] * 0.06               # quelques éclats clairs
    px = np.ones((n, n, 4), dtype=np.float32)
    px[..., :3] = np.clip(col, 0, 1)
    out_dir = out_dir or TEX_DIR
    os.makedirs(out_dir, exist_ok=True)
    path = os.path.join(out_dir, name + ".png")
    C.save_pixels(px, path)
    return path


def tex_material(mat_name, tex_name):
    """Matériau à texture peinte (couleur de base seule) ; crée la tuile si besoin."""
    path = painted_tile(tex_name, PAINT[tex_name])     # déterministe (graine fixe) : régénérée à l'identique
    return C.make_material(mat_name, path)
