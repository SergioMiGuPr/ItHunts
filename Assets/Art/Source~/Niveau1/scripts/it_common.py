# -*- coding: utf-8 -*-
"""Outils communs aux pièces de décor du niveau 1 (It Hunts).

Utilisé par les scripts sm_*.py de ce dossier. À lancer dans Blender (5.0) :
    exec(open(r"<chemin>/sm_arc_couloir.py", encoding="utf-8").read())

Conventions (Blender) : Z en haut, l'avant de la pièce regarde vers -Y (vue de face de Blender).
À l'export FBX (avant -Z, haut Y, transformation appliquée), cela donne dans Unity : +Y en haut,
+Z vers l'avant, rotation nulle, échelle 1.
"""
import bpy, bmesh, math, os
import numpy as np
from mathutils import Vector

# --------------------------------------------------------------------------- chemins
HERE = os.path.dirname(os.path.abspath(__file__)) if "__file__" in globals() else None
ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "..", "..", ".."))
TEX_DIR = os.path.join(ROOT, "Assets", "Art", "Textures", "Niveau1")
MODELS_DIR = os.path.join(ROOT, "Assets", "Art", "Models", "Niveau1")
SOURCE_DIR = os.path.join(ROOT, "Assets", "Art", "Source~", "Niveau1")
RENDER_DIR = os.path.join(ROOT, "docs", "art", "rendus", "Niveau1")
FICHES_DIR = os.path.join(ROOT, "docs", "art", "fiches")
RENDER_FMT = "PNG"          # les scripts du lot de props passent en "JPEG" (qualité 90)
RENDER_DIST_ADD = 2.0       # marge fixe de la caméra (m) ; 0 pour les petits props

# Textures tuilables : (fichier, largeur de tuile en m, hauteur de tuile en m)
TILES = {
    "pierre": ("texture_mur_pierre_v01_tuile.png", 4.0, 3.0),
    "egout": ("texture_mur_egout_v01_tuile.png", 2.0, 2.0),
    "dalles": ("texture_sol_dalles_v01_tuile.png", 4.0, 4.0),
    "fer": ("texture_fer_rouille_v01_tuile.png", 1.0, 1.0),       # 256 px (256 px/m) depuis la retouche du 2026-11-15
    "voute": ("texture_voute_briques_v01_tuile.png", 3.0, 3.0),   # 16 assises : briques de 0.44 × 0.19 m, 341 px/m
    "eau": ("texture_eau_egout_v01_tuile.png", 4.0, 4.0),         # 256 px/m ; écume en quinconce (retouche)
}
MAT_NAMES = {"pierre": "M_Pierre", "egout": "M_Egout", "dalles": "M_Dalles", "fer": "M_FerRouille",
             "voute": "M_Voute_Briques", "eau": "M_Eau_Egout"}


# --------------------------------------------------------------------------- matériaux
def make_material(name, image_path):
    """Matériau simple : couleur de base seulement, ni brillance ni métal."""
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    for n in list(nt.nodes):
        nt.nodes.remove(n)
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    tex = nt.nodes.new("ShaderNodeTexImage")
    out.location, bsdf.location, tex.location = (300, 0), (0, 0), (-400, 0)
    img = None
    for im in bpy.data.images:
        if im.filepath and os.path.normcase(bpy.path.abspath(im.filepath)) == os.path.normcase(image_path):
            img = im
            im.reload()
    if img is None:
        img = bpy.data.images.load(image_path)
    tex.image = img
    nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    nt.links.new(bsdf.outputs[0], out.inputs[0])
    for key, val in (("Metallic", 0.0), ("Roughness", 1.0), ("Specular IOR Level", 0.0)):
        if key in bsdf.inputs:
            bsdf.inputs[key].default_value = val
    mat.metallic, mat.roughness = 0.0, 1.0
    return mat


def tile_material(key):
    return make_material(MAT_NAMES[key], os.path.join(TEX_DIR, TILES[key][0]))


# --------------------------------------------------------------------------- UV
def world_uv(p, n, tw, th, voff=0.0):
    """Projection cubique en coordonnées monde (même densité partout)."""
    ax, ay, az = abs(n.x), abs(n.y), abs(n.z)
    if az >= ax and az >= ay:            # dessus / dessous
        return (p.x / tw, (p.y if n.z > 0 else -p.y) / th)
    if ax >= ay:                         # faces latérales ±X
        return ((p.y if n.x > 0 else -p.y) / tw, p.z / th + voff)
    return ((-p.x if n.y > 0 else p.x) / tw, p.z / th + voff)   # faces ±Y


# --------------------------------------------------------------------------- constructeur
class Builder:
    """Construit un maillage face par face.

    Chaque face reçoit une normale attendue : l'ordre des sommets est retourné si besoin, pour que
    toutes les normales pointent vers l'extérieur. Les sommets sont soudés à l'intérieur d'une
    même « coque » (shell) seulement, pour que les blocs voisins restent séparés.
    """

    def __init__(self):
        self.bm = bmesh.new()
        self.uv = self.bm.loops.layers.uv.new("UVMap")
        self.mode = self.bm.faces.layers.int.new("uvmode")    # 0 = UV donnés, 1 = projection monde
        self.mats = []            # clés de matériau (index = material_index)
        self.uvparams = {}        # clé -> (tw, th, voff)
        self.shell = 0
        self.vmap = {}
        self.bevel_edges = set()

    def mat_index(self, key):
        if key not in self.mats:
            self.mats.append(key)
        return self.mats.index(key)

    def set_uv_params(self, key, tw, th, voff=0.0):
        self.uvparams[key] = (tw, th, voff)

    def new_shell(self):
        self.shell += 1
        return self.shell

    def v(self, p):
        k = (self.shell, round(p[0], 5), round(p[1], 5), round(p[2], 5))
        vert = self.vmap.get(k)
        if vert is None:
            vert = self.bm.verts.new((p[0], p[1], p[2]))
            self.vmap[k] = vert
        return vert

    def face(self, pts, mat, normal=None, uvs=None):
        pts = [Vector(p) for p in pts]
        # supprime les doublons consécutifs (faces dégénérées aux pointes)
        clean, cuv = [], []
        for i, p in enumerate(pts):
            if not clean or (p - clean[-1]).length > 1e-6:
                clean.append(p)
                cuv.append(uvs[i] if uvs else None)
        if len(clean) > 2 and (clean[0] - clean[-1]).length < 1e-6:
            clean.pop(); cuv.pop()
        if len(clean) < 3:
            return None
        pts = clean
        uvs = cuv if uvs else None
        if normal is not None:
            nrm = Vector((0, 0, 0))
            for i in range(len(pts)):                      # normale de Newell
                a, b = pts[i], pts[(i + 1) % len(pts)]
                nrm += Vector(((a.y - b.y) * (a.z + b.z), (a.z - b.z) * (a.x + b.x), (a.x - b.x) * (a.y + b.y)))
            if nrm.dot(Vector(normal)) < 0:
                pts = pts[::-1]
                if uvs:
                    uvs = uvs[::-1]
        verts = [self.v(p) for p in pts]
        if len(set(verts)) < len(verts):
            return None
        try:
            f = self.bm.faces.new(verts)
        except ValueError:
            return None
        f.material_index = self.mat_index(mat)
        if uvs:
            for loop, uv in zip(f.loops, uvs):
                loop[self.uv].uv = uv
            f[self.mode] = 0
        else:
            f[self.mode] = 1
        return f

    # ---- primitives
    def box(self, mn, mx, mat, skip=(), bevel=0.0, shell=True):
        """Boîte alignée sur les axes. skip : faces à omettre parmi '-x','+x','-y','+y','-z','+z'."""
        if shell:
            self.new_shell()
        x0, y0, z0 = mn
        x1, y1, z1 = mx
        F = {
            "-x": ([(x0, y0, z0), (x0, y1, z0), (x0, y1, z1), (x0, y0, z1)], (-1, 0, 0)),
            "+x": ([(x1, y0, z0), (x1, y1, z0), (x1, y1, z1), (x1, y0, z1)], (1, 0, 0)),
            "-y": ([(x0, y0, z0), (x1, y0, z0), (x1, y0, z1), (x0, y0, z1)], (0, -1, 0)),
            "+y": ([(x0, y1, z0), (x1, y1, z0), (x1, y1, z1), (x0, y1, z1)], (0, 1, 0)),
            "-z": ([(x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0)], (0, 0, -1)),
            "+z": ([(x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)], (0, 0, 1)),
        }
        faces = [self.face(p, mat, n) for k, (p, n) in F.items() if k not in skip]
        if bevel > 0:
            self._mark_bevel(faces, bevel)
        return faces

    def prism_y(self, poly, y0, y1, mat, skip_front=False, skip_back=False, bevel=0.0, shell=True,
                side_mat=None, uv_front=None, uv_back=None, uv_side=None):
        """Extrude un polygone (x, z) le long de Y, de y0 (face avant, normale -Y) à y1 (arrière)."""
        if shell:
            self.new_shell()
        poly = [(float(x), float(z)) for x, z in poly]
        area = sum(poly[i][0] * poly[(i + 1) % len(poly)][1] - poly[(i + 1) % len(poly)][0] * poly[i][1]
                   for i in range(len(poly)))
        if area < 0:
            poly = poly[::-1]
        faces = []
        if not skip_front:
            pts = [(x, y0, z) for x, z in poly]
            faces.append(self.face(pts, mat, (0, -1, 0), uv_front(pts) if uv_front else None))
        if not skip_back:
            pts = [(x, y1, z) for x, z in poly]
            faces.append(self.face(pts, mat, (0, 1, 0), uv_back(pts) if uv_back else None))
        n = len(poly)
        for i in range(n):
            (ax, az), (bx, bz) = poly[i], poly[(i + 1) % n]
            nrm = (bz - az, 0, -(bx - ax))     # polygone CCW vu de face : normale extérieure
            pts = [(ax, y0, az), (bx, y0, bz), (bx, y1, bz), (ax, y1, az)]
            faces.append(self.face(pts, side_mat or mat, nrm, uv_side(pts) if uv_side else None))
        if bevel > 0:
            self._mark_bevel(faces, bevel)
        return faces

    def prism_z(self, poly, z0, z1, mat, skip_bottom=True, skip_top=False, shell=True, bevel=0.0):
        """Extrude un polygone (x, y) le long de Z."""
        if shell:
            self.new_shell()
        poly = [(float(x), float(y)) for x, y in poly]
        area = sum(poly[i][0] * poly[(i + 1) % len(poly)][1] - poly[(i + 1) % len(poly)][0] * poly[i][1]
                   for i in range(len(poly)))
        if area < 0:
            poly = poly[::-1]
        faces = []
        if not skip_bottom:
            faces.append(self.face([(x, y, z0) for x, y in poly], mat, (0, 0, -1)))
        if not skip_top:
            faces.append(self.face([(x, y, z1) for x, y in poly], mat, (0, 0, 1)))
        n = len(poly)
        for i in range(n):
            (ax, ay), (bx, by) = poly[i], poly[(i + 1) % n]
            faces.append(self.face([(ax, ay, z0), (bx, by, z0), (bx, by, z1), (ax, ay, z1)], mat, (by - ay, -(bx - ax), 0)))
        if bevel > 0:
            self._mark_bevel(faces, bevel)
        return faces

    def sweep(self, rings, mat, closed_profile=True, caps=(True, True), uv_rings=None, shell=True,
              cap_normals=None):
        """Relie des anneaux de sommets (même nombre de points). Normales orientées vers l'extérieur
        du centre de chaque anneau. uv_rings : mêmes dimensions que rings, en (u, v)."""
        if shell:
            self.new_shell()
        faces = []
        m = len(rings[0])
        cent = [sum((Vector(p) for p in r), Vector()) / m for r in rings]
        for i in range(len(rings) - 1):
            ra, rb = rings[i], rings[i + 1]
            rng = range(m) if closed_profile else range(m - 1)
            for k in rng:
                k2 = (k + 1) % m
                pts = [ra[k], ra[k2], rb[k2], rb[k]]
                mid = sum((Vector(p) for p in pts), Vector()) / 4
                c = (cent[i] + cent[i + 1]) / 2
                uvs = None
                if uv_rings:
                    ua, ub = uv_rings[i], uv_rings[i + 1]
                    uvs = [ua[k], ua[k2], ub[k2], ub[k]]
                faces.append(self.face(pts, mat, mid - c, uvs))
        if caps[0]:
            d = Vector(rings[0][0]) - Vector(rings[1][0]) if cap_normals is None else Vector(cap_normals[0])
            faces.append(self.face(rings[0], mat, d))
        if caps[1]:
            d = Vector(rings[-1][0]) - Vector(rings[-2][0]) if cap_normals is None else Vector(cap_normals[1])
            faces.append(self.face(rings[-1], mat, d))
        return faces

    def _mark_bevel(self, faces, width):
        for f in faces:
            if f is None:
                continue
            for e in f.edges:
                self.bevel_edges.add((e, width))

    # ---- finalisation
    def finish(self, name, materials, origin=(0, 0, 0), sharp_angle=35.0, collection=None):
        """materials : dict clé -> bpy Material. Crée l'objet, origine = pivot (coordonnées monde)."""
        bm = self.bm
        bm.verts.ensure_lookup_table()
        # chanfreins (arêtes des blocs) : groupés par largeur
        by_w = {}
        for e, w in self.bevel_edges:
            if e.is_valid:
                by_w.setdefault(w, set()).add(e)
        for w, edges in by_w.items():
            edges = [e for e in edges if e.is_valid and len(e.link_faces) == 2]
            if edges:
                bmesh.ops.bevel(bm, geom=edges, offset=w, offset_type="OFFSET", segments=1, profile=0.5,
                                affect="EDGES", clamp_overlap=True)
        # UV en projection monde pour les faces sans UV donnés. Les normales doivent être calculées AVANT : une face
        # neuve a une normale nulle, et toutes recevaient la projection du dessus (corrigé le 2026-11-15)
        bm.normal_update()
        for f in bm.faces:
            if f[self.mode] == 1:
                key = self.mats[f.material_index]
                tw, th, voff = self.uvparams.get(key, (1.0, 1.0, 0.0))
                n = f.normal
                for loop in f.loops:
                    loop[self.uv].uv = world_uv(loop.vert.co, n, tw, th, voff)
        bm.normal_update()
        # lissage : faces lisses, arêtes vives au-delà de sharp_angle
        lim = math.radians(sharp_angle)
        for f in bm.faces:
            f.smooth = True
        for e in bm.edges:
            if len(e.link_faces) != 2 or e.calc_face_angle(math.pi) > lim:
                e.smooth = False
        ox, oy, oz = origin
        for vt in bm.verts:
            vt.co.x -= ox; vt.co.y -= oy; vt.co.z -= oz
        me = bpy.data.meshes.get(name)
        if me is not None and me.users == 0:
            bpy.data.meshes.remove(me)
        me = bpy.data.meshes.new(name)
        bm.to_mesh(me)
        bm.free()
        if "uvmode" in me.attributes:
            me.attributes.remove(me.attributes["uvmode"])
        for key in self.mats:
            me.materials.append(materials[key])
        ob = bpy.data.objects.new(name, me)
        (collection or bpy.context.scene.collection).objects.link(ob)
        ob.location = origin
        return ob


# --------------------------------------------------------------------------- courbes d'arcs
def pointed_arch_centers(half_span, rise):
    """Arc brisé dont les centres sont sur la ligne de naissance (tangent à la verticale aux pieds).
    Renvoie a (distance du centre à l'axe, côté opposé) et R. Exige rise >= half_span."""
    a = (rise * rise - half_span * half_span) / (2 * half_span)
    return a, half_span + a


def tudor_arch(half_span, r1, phi, apex_h):
    """Arc brisé à quatre centres (surbaissé), tangent à la verticale aux naissances.
    Côté droit, naissance en (half_span, 0). Petit arc de rayon r1 sur l'angle phi, puis grand arc
    tangent jusqu'à la pointe (0, apex_h). Renvoie (C1, r1, phi, C2, r2)."""
    C1 = Vector((half_span - r1, 0.0))
    u = Vector((math.cos(phi), math.sin(phi)))
    T = C1 + r1 * u
    A = Vector((0.0, apex_h))
    d = A - T
    r2 = -d.length_squared / (2 * d.dot(u))
    C2 = T - r2 * u
    return C1, r1, phi, C2, r2


def tudor_point(arch, s, off=0.0):
    """Point de l'arc (côté droit) à l'abscisse curviligne normalisée s in [0, 1], décalé de off vers
    l'extérieur (extrados) ; renvoie (point 2D, normale extérieure 2D)."""
    C1, r1, phi, C2, r2 = arch
    # angle final sur le grand arc : la pointe est en x = 0 pour le décalage donné
    R2 = r2 + off
    th_end = math.acos(max(-1.0, min(1.0, -C2.x / R2)))
    L1 = (r1 + off) * phi
    L2 = R2 * (th_end - phi)
    t = s * (L1 + L2)
    if t <= L1:
        th = t / (r1 + off)
        u = Vector((math.cos(th), math.sin(th)))
        return C1 + (r1 + off) * u, u
    th = phi + (t - L1) / R2
    u = Vector((math.cos(th), math.sin(th)))
    return C2 + R2 * u, u


def tudor_lengths(arch, off=0.0):
    C1, r1, phi, C2, r2 = arch
    R2 = r2 + off
    th_end = math.acos(max(-1.0, min(1.0, -C2.x / R2)))
    return (r1 + off) * phi, R2 * (th_end - phi), th_end


def tudor_top(arch, x, off=0.0):
    """Hauteur (au-dessus de la naissance) de la courbe décalée à l'abscisse x (0 <= x <= demi-portée+off)."""
    C1, r1, phi, C2, r2 = arch
    x = abs(x)
    R1 = r1 + off
    # point de tangence (décalé)
    tx = C1.x + R1 * math.cos(phi)
    if x >= tx:
        dx = x - C1.x
        return math.sqrt(max(0.0, R1 * R1 - dx * dx))
    R2 = r2 + off
    dx = x - C2.x
    return C2.y + math.sqrt(max(0.0, R2 * R2 - dx * dx))


# --------------------------------------------------------------------------- polygones 2D
def offset_polygon(poly, d):
    """Décale un polygone simple (CCW) vers l'intérieur de d (intersections des côtés décalés)."""
    n = len(poly)
    area = sum(poly[i][0] * poly[(i + 1) % n][1] - poly[(i + 1) % n][0] * poly[i][1] for i in range(n))
    sgn = 1.0 if area > 0 else -1.0
    lines = []
    for i in range(n):
        a, b = Vector(poly[i]), Vector(poly[(i + 1) % n])
        t = (b - a).normalized()
        nin = Vector((-t.y, t.x)) * sgn        # normale intérieure
        lines.append((a + nin * d, t))
    out = []
    for i in range(n):
        p1, t1 = lines[i - 1]
        p2, t2 = lines[i]
        den = t1.x * t2.y - t1.y * t2.x
        if abs(den) < 1e-9:
            out.append(tuple(p2))
            continue
        s = ((p2.x - p1.x) * t2.y - (p2.y - p1.y) * t2.x) / den
        out.append(tuple(p1 + t1 * s))
    return out


# --------------------------------------------------------------------------- images (fiches)
def load_pixels(path):
    img = bpy.data.images.load(path, check_existing=False)
    w, h = img.size
    px = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)
    bpy.data.images.remove(img)
    return px[::-1]        # ligne 0 = haut de l'image, comme dans un éditeur d'images


def crop_resize(px, x0, y0, x1, y1, w, h, flip_x=False):
    """Recadre (coordonnées pixels, origine en haut à gauche) puis redimensionne (bilinéaire)."""
    sub = px[y0:y1, x0:x1]
    sh, sw = sub.shape[:2]
    ys = np.linspace(0, sh - 1, h)
    xs = np.linspace(0, sw - 1, w)
    y0i = np.floor(ys).astype(int); y1i = np.minimum(y0i + 1, sh - 1); fy = (ys - y0i)[:, None, None]
    x0i = np.floor(xs).astype(int); x1i = np.minimum(x0i + 1, sw - 1); fx = (xs - x0i)[None, :, None]
    a = sub[y0i][:, x0i]; b = sub[y0i][:, x1i]; c = sub[y1i][:, x0i]; d = sub[y1i][:, x1i]
    out = (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy
    if flip_x:
        out = out[:, ::-1]
    return out


def save_pixels(px, path):
    """px : tableau (h, w, 4), ligne 0 en haut. Enregistre un PNG RGB."""
    h, w = px.shape[:2]
    name = os.path.basename(path)
    img = bpy.data.images.get(name)
    if img is not None:
        bpy.data.images.remove(img)
    img = bpy.data.images.new(name, w, h, alpha=False)
    data = np.ascontiguousarray(px[::-1], dtype=np.float32)
    data[..., 3] = 1.0
    img.pixels.foreach_set(data.ravel())
    img.filepath_raw = path
    img.file_format = "PNG"
    img.save()
    bpy.data.images.remove(img)


# --------------------------------------------------------------------------- scène, export, vérif
def new_scene(name):
    old = bpy.data.scenes.get(name)
    if old is not None:
        for ob in list(old.objects):
            data = ob.data
            bpy.data.objects.remove(ob, do_unlink=True)
            if data is not None and getattr(data, "users", 1) == 0:
                if isinstance(data, bpy.types.Mesh):
                    bpy.data.meshes.remove(data)
                elif isinstance(data, bpy.types.Camera):
                    bpy.data.cameras.remove(data)
        bpy.data.scenes.remove(old)
    sc = bpy.data.scenes.new(name)
    sc.unit_settings.system = "METRIC"
    sc.unit_settings.scale_length = 1.0
    bpy.context.window.scene = sc
    return sc


def tri_count(ob):
    return sum(len(p.vertices) - 2 for p in ob.data.polygons)


def export_fbx(objects, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    for ob in bpy.context.view_layer.objects:
        ob.select_set(False)
    for ob in objects:
        ob.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={"MESH", "EMPTY"},
        apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL", global_scale=1.0,
        axis_forward="-Z", axis_up="Y", bake_space_transform=True,
        use_mesh_modifiers=True, mesh_smooth_type="OFF", use_triangles=True, use_tspace=False,
        add_leaf_bones=False, bake_anim=False, path_mode="RELATIVE", embed_textures=False,
        use_custom_props=False, use_metadata=True)


def save_blend(scene, path):
    """Écrit un .blend qui ne contient que la scène de la pièce (et ce qu'elle utilise)."""
    os.makedirs(os.path.dirname(path), exist_ok=True)
    blocks = {scene} | set(scene.objects) | {o.data for o in scene.objects if o.data is not None}
    scene.use_fake_user = True
    bpy.data.libraries.write(path, blocks, path_remap="RELATIVE_ALL", fake_user=True, compress=True)


def verify_fbx(path, front_probe=None):
    """Réimporte le FBX dans une scène vide, deux fois :
    - import Blender standard (Z en haut) : dimensions, pivots, triangles ;
    - import brut (axes du fichier, Y en haut = axes Unity avant le passage main gauche) :
      la hauteur doit être sur Y et l'avant (front_probe : nom d'un objet ou d'un matériau placé
      sur la face avant) vers +Z.
    """
    prev = bpy.context.window.scene
    res = {}
    for mode in ("blender", "brut"):
        sc = bpy.data.scenes.new("_VerifFBX")
        bpy.context.window.scene = sc
        kw = dict(filepath=path, use_custom_normals=True, use_image_search=False)
        if mode == "brut":
            kw.update(use_manual_orientation=True, axis_forward="Y", axis_up="Z")
        bpy.ops.import_scene.fbx(**kw)
        dg = bpy.context.evaluated_depsgraph_get()
        info = {}
        allmin = Vector((1e9, 1e9, 1e9)); allmax = -allmin
        for ob in sc.objects:
            if ob.type != "MESH":
                continue
            mw = ob.matrix_world
            pts = [mw @ v.co for v in ob.data.vertices]
            mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
            mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
            allmin = Vector((min(allmin.x, mn.x), min(allmin.y, mn.y), min(allmin.z, mn.z)))
            allmax = Vector((max(allmax.x, mx.x), max(allmax.y, mx.y), max(allmax.z, mx.z)))
            info[ob.name] = dict(
                parent=ob.parent.name if ob.parent else None,
                pivot=tuple(round(c, 3) for c in mw.translation),
                rot=tuple(round(math.degrees(c), 2) for c in ob.matrix_world.to_euler()),
                scale=tuple(round(c, 4) for c in mw.to_scale()),
                tris=sum(len(p.vertices) - 2 for p in ob.data.polygons),
                size=tuple(round(c, 3) for c in (mx - mn)),
                bmin=tuple(round(c, 3) for c in mn), bmax=tuple(round(c, 3) for c in mx),
                mats=[m.name for m in ob.data.materials if m],
            )
        res[mode] = dict(objects=info, size=tuple(round(c, 3) for c in (allmax - allmin)),
                         bmin=tuple(round(c, 3) for c in allmin), bmax=tuple(round(c, 3) for c in allmax))
        # nettoyage
        for ob in list(sc.objects):
            data = ob.data
            bpy.data.objects.remove(ob, do_unlink=True)
            if isinstance(data, bpy.types.Mesh) and data.users == 0:
                bpy.data.meshes.remove(data)
        bpy.data.scenes.remove(sc)
    # matériaux importés en double (M_Pierre.001...) : on les supprime
    for m in list(bpy.data.materials):
        if m.users == 0 and "." in m.name:
            bpy.data.materials.remove(m)
    for im in list(bpy.data.images):
        if im.users == 0:
            bpy.data.images.remove(im)
    bpy.context.window.scene = prev
    return res


def report(res, expected=None):
    b = res["blender"]; r = res["brut"]
    lines = ["Réimport Blender : taille hors tout X×Y×Z = %s, min %s, max %s" % (b["size"], b["bmin"], b["bmax"]),
             "Réimport brut (axes Unity, Y haut) : taille X×Y×Z = %s, min %s, max %s" % (r["size"], r["bmin"], r["bmax"])]
    for name, o in sorted(b["objects"].items()):
        lines.append("  %s : tris %d, pivot %s, rot %s, échelle %s, taille %s, parent %s, mats %s" % (
            name, o["tris"], o["pivot"], o["rot"], o["scale"], o["size"], o["parent"], o["mats"]))
    for name, o in sorted(r["objects"].items()):
        lines.append("  [brut] %s : pivot %s, rot %s, bmin %s, bmax %s" % (name, o["pivot"], o["rot"], o["bmin"], o["bmax"]))
    lines.append("Total triangles : %d" % sum(o["tris"] for o in b["objects"].values()))
    return "\n".join(lines)


# --------------------------------------------------------------------------- rendus
def render_views(scene, objects, basename, views=("face", "cote", "34"), res=1024):
    """Rendus Workbench, lumière plate, texture, contour, fond gris."""
    os.makedirs(RENDER_DIR, exist_ok=True)
    sc = scene
    for eng in ("BLENDER_WORKBENCH",):
        try:
            sc.render.engine = eng
        except TypeError as e:
            print("moteur :", e)
    sh = sc.display.shading
    sh.light = "FLAT"
    sh.color_type = "TEXTURE"
    sh.show_object_outline = True
    sh.object_outline_color = (0.05, 0.05, 0.05)
    sh.show_cavity = True               # assombrit les creux, éclaire les arêtes : lisibilité en lumière plate
    sh.cavity_type = "BOTH"
    sh.cavity_ridge_factor = 2.0
    sh.cavity_valley_factor = 2.0
    sh.curvature_ridge_factor = 2.0
    sh.curvature_valley_factor = 2.0
    sc.display.matcap_ssao_distance = 0.4
    sc.display.matcap_ssao_attenuation = 1.0
    sh.background_type = "WORLD" if hasattr(sh, "background_type") else sh.background_type
    if sc.world is None:
        sc.world = bpy.data.worlds.new("Fond_Gris")
    sc.world.color = (0.32, 0.32, 0.32)
    sc.view_settings.view_transform = "Standard"
    sc.render.resolution_x = res
    sc.render.resolution_y = res
    sc.render.film_transparent = False
    sc.render.image_settings.file_format = RENDER_FMT
    if RENDER_FMT == "JPEG":
        sc.render.image_settings.quality = 90
    sc.display.render_aa = "8"

    mn = Vector((1e9, 1e9, 1e9)); mx = -mn
    for ob in objects:
        for c in ob.bound_box:
            p = ob.matrix_world @ Vector(c)
            mn = Vector((min(mn.x, p.x), min(mn.y, p.y), min(mn.z, p.z)))
            mx = Vector((max(mx.x, p.x), max(mx.y, p.y), max(mx.z, p.z)))
    ctr = (mn + mx) / 2
    size = mx - mn
    cam_data = bpy.data.cameras.get("_RenduCam") or bpy.data.cameras.new("_RenduCam")
    cam = bpy.data.objects.get("_RenduCam")
    if cam is None:
        cam = bpy.data.objects.new("_RenduCam", cam_data)
    if cam.name not in sc.collection.objects:
        sc.collection.objects.link(cam)
    sc.camera = cam
    cam_data.clip_end = 1000
    out = []
    for v in views:
        if v == "face":
            d = Vector((0, -1, 0)); cam_data.type = "ORTHO"
            cam_data.ortho_scale = max(size.x, size.z) * 1.15
        elif v == "cote":
            d = Vector((1, 0, 0)); cam_data.type = "ORTHO"
            cam_data.ortho_scale = max(size.y, size.z) * 1.15
        elif v == "dos":
            d = Vector((0, 1, 0)); cam_data.type = "ORTHO"
            cam_data.ortho_scale = max(size.x, size.z) * 1.15
        elif v == "34dos":   # 3/4 arrière gauche, un peu en hauteur
            d = Vector((-0.75, 1, 0.5)).normalized(); cam_data.type = "PERSP"
            cam_data.lens = 35
        else:   # 3/4 avant droit, un peu en hauteur
            d = Vector((0.75, -1, 0.5)).normalized(); cam_data.type = "PERSP"
            cam_data.lens = 35
        dist = size.length * (1.05 if v.startswith("34") else 2.0) + RENDER_DIST_ADD
        cam.location = ctr + d * dist
        cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
        path = os.path.join(RENDER_DIR, "%s_%s.%s" % (basename, v, "jpg" if RENDER_FMT == "JPEG" else "png"))
        sc.render.filepath = path
        bpy.ops.render.render(write_still=True, scene=sc.name)
        out.append(path)
    bpy.data.objects.remove(cam, do_unlink=True)
    return out


def run_piece(scene_name, build, params, fbx_name, front_probe=None, render_objects=None, views=("face", "cote", "34")):
    """Construit, exporte, sauvegarde, vérifie et rend une pièce. build(params) renvoie la liste
    d'objets à exporter."""
    sc = new_scene(scene_name)
    objs = build(params)
    bpy.context.view_layer.update()
    fbx = os.path.join(MODELS_DIR, fbx_name + ".fbx")
    export_fbx(objs, fbx)
    save_blend(sc, os.path.join(SOURCE_DIR, fbx_name + ".blend"))
    res = verify_fbx(fbx, front_probe)
    bpy.context.window.scene = sc
    renders = render_views(sc, render_objects or objs, fbx_name, views)
    print(report(res))
    print("Rendus :", [os.path.basename(p) for p in renders])
    return res


# --------------------------------------------------------------------------- calage des pierres peintes
_STONE_CACHE = {}


def stone_joints(key="pierre", courses=4, voff=0.0104):
    """Analyse la texture de pierre : pour chaque assise, positions u des joints verticaux peints."""
    if key in _STONE_CACHE:
        return _STONE_CACHE[key]
    px = load_pixels(os.path.join(TEX_DIR, TILES[key][0]))
    h, w = px.shape[:2]
    lum = px[..., :3].mean(axis=2)
    res = []
    for c in range(courses):
        # assise c : v de voff + c/courses à voff + (c+1)/courses (v = 0 en bas de l'image)
        v0, v1 = voff + c / courses, voff + (c + 1) / courses
        r0 = int((1 - (v0 + 0.3 / courses)) * h)
        r1 = int((1 - (v1 - 0.3 / courses)) * h)
        band = lum[min(r0, r1):max(r0, r1)]
        dark = (band < np.percentile(lum, 12)).mean(axis=0)       # part de la hauteur où la colonne est sombre
        k = 7
        dk = np.convolve(np.concatenate([dark[-k:], dark, dark[:k]]), np.ones(k) / k, "same")[k:-k]
        # un joint : colonne sombre sur presque toute la hauteur (les coulures ne le sont qu'en partie)
        joints, i = [], 0
        while i < w:
            if dk[i] > 0.6:
                j = i
                while j < w and dk[j] > 0.6:
                    j += 1
                seg = dk[i:j]
                joints.append((i + int(np.argmax(seg))) / w)
                i = j
            else:
                i += 1
        # fusionne les joints trop proches (bord gauche/droit de la même ligne)
        merged = []
        for u in joints:
            if merged and u - merged[-1] < 0.03:
                continue
            merged.append(u)
        if len(merged) > 1 and merged[0] + 1 - merged[-1] < 0.03:
            merged.pop()
        joints = merged
        res.append(sorted(joints))
    _STONE_CACHE[key] = res
    return res


def stone_offset(u0, u1, v_mid, key="pierre", voff=0.0104, courses=4, seed=0):
    """Décalage du à ajouter à u pour que [u0, u1] tombe dans une seule pierre peinte. Parmi les
    pierres assez larges, le choix (et la position dans la pierre) dépend de seed, pour varier."""
    J = stone_joints(key, courses, voff)
    c = int(math.floor((v_mid - voff) * courses)) % courses
    joints = J[c]
    if not joints:
        return 0.0
    wu = u1 - u0
    js = joints + [joints[0] + 1.0]
    cands = []
    for a, b in zip(js[:-1], js[1:]):
        m = (b - a - wu) / 2 - 0.006               # marge de chaque côté (sans toucher la ligne du joint)
        cands.append((m, a, b))
    fits = [cd for cd in cands if cd[0] >= 0]
    rnd = (seed * 2654435761) % 4294967296 / 4294967296.0
    if fits:
        m, a, b = fits[int(rnd * len(fits)) % len(fits)]
        slack = m * (2 * ((rnd * 7.0) % 1.0) - 1)  # position libre dans la pierre
        return (a + b) / 2 + slack - (u0 + u1) / 2
    m, a, b = max(cands)
    return (a + b) / 2 - (u0 + u1) / 2


def fit_stones(B, faces, key="pierre", min_dot=0.9):
    """Donne aux faces verticales de pierre (un bloc) des UV monde décalées pour tomber sur une pierre peinte."""
    tw, th, voff = B.uvparams.get(key, (4.0, 3.0, 0.0))
    for f in faces:
        if f is None or B.mats[f.material_index] != key:
            continue
        f.normal_update()
        n = f.normal
        if abs(n.z) > 1 - min_dot:
            continue
        uvs = [world_uv(l.vert.co, n, tw, th, voff) for l in f.loops]
        us = [u for u, v in uvs]
        vs = [v for u, v in uvs]
        cen = f.calc_center_median()
        seed = int(abs(cen.x * 7.31 + cen.y * 3.17 + cen.z * 11.73) * 1000) + 17 * len(f.verts)
        du = stone_offset(min(us), max(us), (min(vs) + max(vs)) / 2, key, voff, seed=seed)
        for l, (u, v) in zip(f.loops, uvs):
            l[B.uv].uv = (u + du, v)
        f[B.mode] = 0


# --------------------------------------------------------------------------- tubes et révolutions
def tube(B, path, radius, sides, mat, closed=False, up=Vector((0, 0, 1)), caps=True, phase=0.0):
    """Tube à section polygonale (sides côtés) le long d'un chemin (liste de Vector)."""
    B.new_shell()
    n = len(path)
    rings = []
    prev_nrm = None
    for i in range(n):
        if closed:
            t = (path[(i + 1) % n] - path[i - 1]).normalized()
        else:
            t = (path[min(i + 1, n - 1)] - path[max(i - 1, 0)]).normalized()
        ref = up if abs(t.dot(up)) < 0.95 else Vector((1, 0, 0))
        nrm = t.cross(ref).normalized() if prev_nrm is None else (prev_nrm - t * prev_nrm.dot(t)).normalized()
        prev_nrm = nrm
        bi = t.cross(nrm).normalized()
        rings.append([path[i] + (nrm * math.cos(phase + 2 * math.pi * k / sides) + bi * math.sin(phase + 2 * math.pi * k / sides)) * radius
                      for k in range(sides)])
    segs = range(n) if closed else range(n - 1)
    for i in segs:
        j = (i + 1) % n
        c = (path[i] + path[j]) / 2
        for k in range(sides):
            q = [rings[i][k], rings[i][(k + 1) % sides], rings[j][(k + 1) % sides], rings[j][k]]
            B.face(q, mat, sum(q, Vector()) / 4 - c)
    if not closed and caps:
        B.face(rings[0], mat, path[0] - path[1])
        B.face(rings[-1], mat, path[-1] - path[-2])


def revolve(B, prof, segs, mat, cx=0.0, cy=0.0, uv=None, cap_top=False, cap_bottom=False, phase=0.0, shell=True):
    """Révolution autour de l'axe Z d'un profil [(r, z), ...] (de bas en haut).
    uv(k, i, r, z) -> (u, v) facultatif (k : indice angulaire 0..segs, i : indice du profil)."""
    if shell:
        B.new_shell()
    ang = [phase + 2 * math.pi * k / segs for k in range(segs + 1)]
    P = [[Vector((cx + r * math.cos(a), cy + r * math.sin(a), z)) for (r, z) in prof] for a in ang]
    for k in range(segs):
        for i in range(len(prof) - 1):
            q = [P[k][i], P[k + 1][i], P[k + 1][i + 1], P[k][i + 1]]
            am = (ang[k] + ang[k + 1]) / 2
            (r0, z0), (r1, z1) = prof[i], prof[i + 1]
            # normale extérieure du profil (r, z) parcouru vers le haut : (dz, -dr)
            nr, nz = (z1 - z0), -(r1 - r0)
            nrm = Vector((math.cos(am) * nr, math.sin(am) * nr, nz))
            uvs = [uv(k, i, r0, z0), uv(k + 1, i, r0, z0), uv(k + 1, i + 1, r1, z1), uv(k, i + 1, r1, z1)] if uv else None
            B.face(q, mat, nrm, uvs)
    if cap_top:
        B.face([P[k][-1] for k in range(segs)], mat, (0, 0, 1))
    if cap_bottom:
        B.face([P[k][0] for k in range(segs)], mat, (0, 0, -1))


def render_detail(names, basename):
    """Rendu rapproché (face et 3/4) de petits sous-objets, dans la scène de la pièce."""
    sc = bpy.context.window.scene
    objs = [o for o in sc.objects if o.name in names]
    return render_views(sc, objs, basename + "_detail", ("face", "34"), res=768)
