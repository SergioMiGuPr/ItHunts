# -*- coding: utf-8 -*-
"""SM_Barillet : barillet de l'artefact de la main gauche (vue FPS), d'après fiche3d_barillet_v01.

Repère : le script travaille en coordonnées Unity (X droite, Y haut, Z avant = direction du tir),
converties en Blender par U() : Unity (x, y, z) -> Blender (-x, -z, y). L'export FBX (avant -Z,
haut Y) redonne exactement le repère Unity. Pivot : centre du barillet.

Géométrie :
- corps de révolution (rayon R, longueur L), bord avant chanfreiné, axe central percé ;
- 6 chambres borgnes percées depuis la face avant ; pour chacune, un objet séparé
  SM_Barillet_Chambre_01 à _06 (matériau M_Barillet_Chambre) qui réunit l'insert plat au fond de
  la chambre et une pastille en léger relief sur la face arrière (vue par le joueur) : le code
  allume ou éteint une chambre d'un coup. Pivot : sur l'axe de la chambre, au centre du barillet ;
- 6 encoches arrondies sur le bord avant et 6 cannelures depuis l'arrière, entre les chambres ;
- une bague (acier) qui dépasse à l'avant et à l'arrière ;
- une rune gravée en creux sur le dessus (au-dessus de la chambre 01), allongée selon l'axe,
  matériau M_Barillet_Rune.

Numérotation des chambres : 01 en haut, puis dans l'ordre où elles arrivent en haut quand le
barillet tourne de 60° dans le sens horaire vu du joueur (de l'arrière), soit une rotation de
-60° autour de Z local en Unity à chaque tir : 02 est en haut à gauche, 06 en haut à droite.

Texture : atlas T_Barillet_Laiton_BaseColor.png tiré de la fiche (vue de face projetée sur la face
avant, bague et chambres ; laiton de la vue de côté sur le flanc et l'arrière ; cannelure peinte
dans les cannelures et les encoches).

Lancer dans Blender :
    p = r"<dépôt>/Assets/Art/Source~/FPS/scripts/sm_barillet.py"
    exec(compile(open(p, encoding="utf-8").read(), p, "exec"), {"__file__": p, "__name__": "__main__"})
"""
import sys, os, math
sys.dont_write_bytecode = True
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "..", "Niveau1", "scripts"))
sys.modules.pop("it_common", None)
import it_common as C
import bpy, bmesh
import numpy as np
from mathutils import Vector

ROOT = C.ROOT
MODELS_DIR = os.path.join(ROOT, "Assets", "Art", "Models", "FPS")
TEX_DIR = os.path.join(ROOT, "Assets", "Art", "Textures", "FPS")
SOURCE_DIR = os.path.join(ROOT, "Assets", "Art", "Source~", "FPS")
RENDER_DIR = os.path.join(ROOT, "docs", "art", "rendus", "FPS")

PARAMS = dict(
    R=0.05, L=0.10, chanfrein=0.0015, segs=36,
    chambre=dict(r_centre=0.0307, r=0.0112, profondeur=0.04, segs=14),
    insert=dict(retrait=0.0004, segs=14, pastille_r=0.0085, pastille_relief=0.0008),
    axe=dict(r_trou=0.0063, r_bague=0.0125, saillie=0.0055, segs=12),
    encoche=dict(r=0.008, profondeur=0.014, segs=12),
    cannelure=dict(r=0.0065, enfoncement=0.004, longueur=0.045, segs=12),
    rune=dict(longueur=0.052, largeur=0.024, trait=0.0028, profondeur=0.001, z=-0.011, subdiv=2),
    couleur_rune=(0.86, 0.70, 0.34), couleur_chambre=(0.78, 0.58, 0.20),
    # fiche (pixels de l'image d'origine 2752 x 1536)
    fiche="fiche3d_barillet_v01.png",
    face_centre=(533, 0), face_rayon=374,      # centre y mesuré au lancement
    flanc=(int(740 * 1.376 + 140 * 1.376), int(270 * 1.376 + 135 * 1.376),
           int(740 * 1.376 + 320 * 1.376), int(270 * 1.376 + 290 * 1.376)),
    cannelure_crop=(int(740 * 1.376 + 330 * 1.376), int(270 * 1.376 + 160 * 1.376),
                    int(740 * 1.376 + 530 * 1.376), int(270 * 1.376 + 272 * 1.376)),
    texture="T_Barillet_Laiton_BaseColor.png",
)

# régions de l'atlas (u0, v0, u1, v1)
REG_FACE = (0.0, 0.5, 0.5, 1.0)
REG_FLANC = (0.5, 0.5, 1.0, 1.0)
REG_CANN = (0.0, 0.0, 1.0, 0.5)


def U(x, y, z):
    return Vector((-x, -z, y))


# --------------------------------------------------------------------------- texture
def build_texture(P):
    px = C.load_pixels(os.path.join(C.FICHES_DIR, P["fiche"]))
    g = px[..., :3].mean(-1)
    cx = P["face_centre"][0]
    col = g[:, cx]
    dark = [y for y in range(200, 1400) if col[y] < 0.06]
    # contour du disque : premier et dernier trait noir assez loin du centre
    top = dark[0]
    cy = top + P["face_rayon"] + 6          # le bas du disque est caché par la main
    rr = P["face_rayon"]
    print("fiche : disque de face, centre (%d, %d), haut %d" % (cx, cy, top))
    atlas = np.zeros((1024, 1024, 4), dtype=np.float32)
    # face avant : carré de côté 2 * rayon peint
    atlas[0:512, 0:512] = C.crop_resize(px, cx - rr, cy - rr, cx + rr, cy + rr, 512, 512)
    # flanc : bande de laiton, 4 tuiles autour, une sur deux retournée (raccords continus)
    x0, y0, x1, y1 = P["flanc"]
    tile = C.crop_resize(px, x0, y0, x1, y1, 512, 128)
    for k in range(4):
        atlas[k * 128:(k + 1) * 128, 512:1024] = tile if k % 2 == 0 else tile[::-1]
    x0, y0, x1, y1 = P["cannelure_crop"]
    atlas[512:1024, 0:1024] = C.crop_resize(px, x0, y0, x1, y1, 1024, 512)
    os.makedirs(TEX_DIR, exist_ok=True)
    path = os.path.join(TEX_DIR, P["texture"])
    C.save_pixels(atlas, path)
    return path, (cx, cy)


def in_reg(reg, s, t):
    u0, v0, u1, v1 = reg
    return (u0 + (u1 - u0) * s, v0 + (v1 - v0) * t)


def color_material(name, rgb):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = next(n for n in mat.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    bsdf.inputs["Base Color"].default_value = (*rgb, 1.0)
    for key, val in (("Metallic", 0.0), ("Roughness", 1.0), ("Specular IOR Level", 0.0)):
        if key in bsdf.inputs:
            bsdf.inputs[key].default_value = val
    mat.diffuse_color = (*rgb, 1.0)
    mat.metallic, mat.roughness = 0.0, 1.0
    return mat


# --------------------------------------------------------------------------- géométrie
def revolve(bm, prof, segs, cx=0.0, cy=0.0, closed=True, mat=0, phase=0.0):
    """Révolution autour d'un axe parallèle à Z (Unity) d'un profil [(r, z)] ; r = 0 -> un seul sommet."""
    rings = []
    for (r, z) in prof:
        if r == 0:
            v = bm.verts.new(U(cx, cy, z))
            rings.append([v] * segs)
        else:
            rings.append([bm.verts.new(U(cx + r * math.sin(phase + 2 * math.pi * k / segs),
                                         cy + r * math.cos(phase + 2 * math.pi * k / segs), z))
                          for k in range(segs)])
    n = len(prof) if closed else len(prof) - 1
    faces = []
    for i in range(n):
        a, b = rings[i], rings[(i + 1) % len(prof)]
        for k in range(segs):
            k1 = (k + 1) % segs
            q = [a[k], a[k1], b[k1], b[k]]
            uniq = []
            for v in q:
                if v not in uniq:
                    uniq.append(v)
            if len(uniq) >= 3:
                f = bm.faces.new(uniq)
                f.material_index = mat
                faces.append(f)
    return faces


def capsule(bm, cx, cy, r, z_plat, z_rond, segs, mat):
    """Cylindre d'axe Z (Unity) fermé à plat en z_plat et arrondi (demi-sphère) vers z_rond."""
    sgn = 1 if z_rond > z_plat else -1
    zc = z_rond - sgn * r
    prof = [(0, z_plat), (r, z_plat)]
    steps = 3
    for j in range(steps + 1):
        a = math.pi / 2 * j / steps
        prof.append((r * math.cos(a), zc + sgn * r * math.sin(a)) if j < steps else (0, zc + sgn * r))
    # profil de r=0 à r=0 : fermé naturellement (dernier point sur l'axe)
    faces = revolve(bm, prof, segs, cx, cy, closed=False, mat=mat)
    return faces


def rune_strokes(bm, P, mat):
    R = P["R"]
    q = P["rune"]
    hl, hw, w, d = q["longueur"] / 2, q["largeur"] / 2, q["trait"], q["profondeur"]
    zc = q["z"]
    # deux lignes brisées : (-1,-1)->(0,1)->(1,-1) et (-1,1)->(0,-1)->(1,1), en (s axial, t arc)
    segs = []
    for sgn in (1, -1):
        pts = [(-hl, -sgn * hw), (0.0, sgn * hw), (hl, -sgn * hw)]
        segs += [(pts[0], pts[1]), (pts[1], pts[2])]
    r_in, r_out = R - d, R + 0.002
    for (s0, t0), (s1, t1) in segs:
        ds, dt = s1 - s0, t1 - t0
        ln = math.hypot(ds, dt)
        es, et = ds / ln, dt / ln
        ns, nt = -et, es
        # prolonge chaque trait de la demi-largeur pour fermer les angles
        a = (s0 - es * w / 2, t0 - et * w / 2)
        b = (s1 + es * w / 2, t1 + et * w / 2)
        n = q["subdiv"]
        grid = []
        for i in range(n + 1):
            s = a[0] + (b[0] - a[0]) * i / n
            t = a[1] + (b[1] - a[1]) * i / n
            sec = []
            for (side, rad) in ((-1, r_in), (1, r_in), (1, r_out), (-1, r_out)):
                ss, tt = s + side * ns * w / 2, t + side * nt * w / 2
                al = tt / R
                sec.append(bm.verts.new(U(rad * math.sin(al), rad * math.cos(al), zc + ss)))
            grid.append(sec)
        for i in range(n):
            for k in range(4):
                f = bm.faces.new([grid[i][k], grid[i][(k + 1) % 4], grid[i + 1][(k + 1) % 4], grid[i + 1][k]])
                f.material_index = mat
        for sec in (grid[0], grid[-1]):
            f = bm.faces.new(sec)
            f.material_index = mat


def chamber_angle(k):
    """Angle (depuis +Y vers +X, Unity) de la chambre k (1..6)."""
    return -math.radians(60) * (k - 1)


def build(P):
    tex_path, _ = build_texture(P)
    R, L, ch = P["R"], P["L"], P["chanfrein"]
    A, Ch, E, Ca = P["axe"], P["chambre"], P["encoche"], P["cannelure"]
    tmp = [bpy.data.materials.get(n) or bpy.data.materials.new(n)
           for n in ("_t_corps", "_t_chambre", "_t_encoche", "_t_cannelure", "_t_rune")]

    # corps : révolution percée au centre, bord avant chanfreiné
    bm = bmesh.new()
    prof = [(0, -L / 2), (R, -L / 2), (R, L / 2 - ch), (R - ch, L / 2), (0, L / 2)]
    revolve(bm, prof, P["segs"], closed=False, mat=0)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new("_corps"); bm.to_mesh(me); bm.free()
    corps = bpy.data.objects.new("_corps", me)
    bpy.context.scene.collection.objects.link(corps)
    corps.data.materials.append(tmp[0])

    # outils de découpe
    bm = bmesh.new()
    revolve(bm, [(0, -L / 2 - 0.01), (A["r_trou"], -L / 2 - 0.01), (A["r_trou"], L / 2 + 0.01), (0, L / 2 + 0.01)],
            A["segs"], closed=False, mat=0)   # trou d'axe (matériau du corps)
    for k in range(1, 7):
        a = chamber_angle(k)
        cx, cy = Ch["r_centre"] * math.sin(a), Ch["r_centre"] * math.cos(a)
        zb = L / 2 - Ch["profondeur"]
        revolve(bm, [(0, zb), (Ch["r"], zb), (Ch["r"], L / 2 + 0.01), (0, L / 2 + 0.01)],
                Ch["segs"], cx, cy, closed=False, mat=1)
    for j in range(6):
        a = math.radians(30 + 60 * j)
        capsule(bm, R * math.sin(a), R * math.cos(a), E["r"], L / 2 + 0.01, L / 2 - E["profondeur"],
                E["segs"], 2)
        rc = R + Ca["r"] - Ca["enfoncement"]
        capsule(bm, rc * math.sin(a), rc * math.cos(a), Ca["r"], -L / 2 - 0.01, -L / 2 + Ca["longueur"],
                Ca["segs"], 3)
    rune_strokes(bm, P, 4)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-7)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new("_outils"); bm.to_mesh(me); bm.free()
    outils = bpy.data.objects.new("_outils", me)
    bpy.context.scene.collection.objects.link(outils)
    for m in tmp:
        outils.data.materials.append(m)

    mod = corps.modifiers.new("Decoupe", "BOOLEAN")
    mod.operation = "DIFFERENCE"
    mod.object = outils
    solvers = [i.identifier for i in bpy.types.BooleanModifier.bl_rna.properties["solver"].enum_items]
    mod.solver = "EXACT" if "EXACT" in solvers else solvers[0]
    if hasattr(mod, "use_self"):
        mod.use_self = True
    if hasattr(mod, "material_mode"):
        mod.material_mode = "TRANSFER"
    dg = bpy.context.evaluated_depsgraph_get()
    res = bpy.data.meshes.new_from_object(corps.evaluated_get(dg))
    names = [m.name if m else "" for m in res.materials]
    bpy.data.objects.remove(corps); bpy.data.objects.remove(outils)

    bm = bmesh.new(); bm.from_mesh(res); bpy.data.meshes.remove(res)
    bmesh.ops.dissolve_degenerate(bm, edges=bm.edges, dist=1e-6)
    # le booléen découpe les faces planes en morceaux : on refond les faces coplanaires (même matériau)
    bmesh.ops.dissolve_limit(bm, angle_limit=math.radians(0.5), use_dissolve_boundaries=False,
                             verts=bm.verts, edges=bm.edges, delimit={"MATERIAL"})
    kind = {}
    for f in bm.faces:
        kind[f] = names[f.material_index] if f.material_index < len(names) else "_t_corps"
    # fonds des chambres : cachés par les inserts, supprimés
    zb = L / 2 - Ch["profondeur"]
    fonds = [f for f in bm.faces if kind[f] == "_t_chambre" and f.normal.dot(U(0, 0, 1)) > 0.9
             and abs(-f.calc_center_median().y - zb) < 1e-5]
    for f in fonds:
        del kind[f]
    bmesh.ops.delete(bm, geom=fonds, context="FACES_ONLY")
    print("fonds de chambre supprimés :", len(fonds))

    # bague (acier) : anneaux avant et arrière, sans face cachée contre le corps
    for sgn in (1, -1):
        z0, z1 = sgn * L / 2, sgn * (L / 2 + A["saillie"])
        prof = [(A["r_trou"], z0), (A["r_bague"], z0), (A["r_bague"], z1), (A["r_trou"], z1), (A["r_trou"], z0)]
        # profil ouvert : extérieur, bout, intérieur (pas de face d'appui)
        faces = revolve(bm, prof[1:], A["segs"], closed=False, mat=0)
        for f in faces:
            kind[f] = "_bague"
    bm.normal_update()
    # orientation : normales vers l'extérieur du solide pour la bague (révolutions faites à la main)
    for f in bm.faces:
        if kind[f] == "_bague":
            c = f.calc_center_median()
            cu = Vector((-c.x, c.z, -c.y))            # Unity
            nu = Vector((-f.normal.x, f.normal.z, -f.normal.y))
            rad = math.hypot(cu.x, cu.y)
            if abs(nu.z) > 0.7:
                want = 1 if cu.z > 0 else -1
                if nu.z * want < 0:
                    f.normal_flip()
            else:
                out = Vector((cu.x, cu.y, 0)).normalized()
                inner = rad < (A["r_trou"] + A["r_bague"]) / 2
                d = nu.dot(out)
                if (inner and d > 0) or (not inner and d < 0):
                    f.normal_flip()
    bm.normal_update()

    # ---- UV
    uvl = bm.loops.layers.uv.new("UVMap")
    rr = P["face_rayon"]

    def face_uv(x, y, scale=1.0):
        # face avant vue de face : la droite de l'image est -X (Unity)
        s = 0.5 - x / R * 0.5
        t = 0.5 + y / R * 0.5
        return in_reg(REG_FACE, s, t)

    def flank_uv(z, ang):
        return in_reg(REG_FLANC, (z + L / 2) / L, min(1.0, max(0.0, 0.5 + ang / (2 * math.pi))))

    for f in bm.faces:
        k = kind[f]
        c = f.calc_center_median(); cu = Vector((-c.x, c.z, -c.y))
        nu = Vector((-f.normal.x, f.normal.z, -f.normal.y))
        pts = [Vector((-l.vert.co.x, l.vert.co.z, -l.vert.co.y)) for l in f.loops]
        uvs = []
        if k == "_t_rune":
            f.material_index = 1
        else:
            f.material_index = 0
        if k in ("_t_corps", "_t_rune"):
            rad_c = math.hypot(cu.x, cu.y)
            if abs(nu.z) < 0.5 and rad_c < A["r_trou"] + 0.0005:   # paroi du trou d'axe : sombre
                uvs = [face_uv(0.002 * math.sin(math.atan2(p.x, p.y)), 0.002 * math.cos(math.atan2(p.x, p.y)))
                       for p in pts]
            elif nu.z > 0.5:                                         # face avant + chanfrein
                uvs = [face_uv(p.x, p.y) for p in pts]
            elif nu.z < -0.5:                                        # arrière : laiton du flanc
                uvs = [in_reg(REG_FLANC, 0.5 + p.x / R * 0.45, 0.5 + p.y / R * 0.45) for p in pts]
            else:                                                    # flanc (raccord en bas, à 180°)
                a0 = math.atan2(cu.x, cu.y)
                for p in pts:
                    a = math.atan2(p.x, p.y)
                    while a - a0 > math.pi: a -= 2 * math.pi
                    while a - a0 < -math.pi: a += 2 * math.pi
                    uvs.append(flank_uv(p.z, a))
        elif k == "_t_chambre":
            # chambre la plus proche ; on projette sur le disque sombre peint de la fiche
            best = min(range(1, 7), key=lambda kk: (Vector((Ch["r_centre"] * math.sin(chamber_angle(kk)),
                                                              Ch["r_centre"] * math.cos(chamber_angle(kk)))) - cu.xy).length)
            a = chamber_angle(best)
            cc = Vector((Ch["r_centre"] * math.sin(a), Ch["r_centre"] * math.cos(a)))
            for p in pts:
                dv = p.xy - cc
                dfrac = min(1.0, max(0.0, (L / 2 - p.z) / Ch["profondeur"]))
                rho = Ch["r"] * (0.85 - 0.55 * dfrac)
                dirv = dv.normalized() if dv.length > 1e-6 else Vector((0, 1))
                q = cc + dirv * rho
                uvs.append(face_uv(q.x, q.y))
        elif k in ("_t_encoche", "_t_cannelure"):
            a = math.atan2(cu.x, cu.y)
            j = round((math.degrees(a) - 30) / 60)
            aj = math.radians(30 + 60 * j)
            radial = Vector((math.sin(aj), math.cos(aj)))
            tang = Vector((math.cos(aj), -math.sin(aj)))
            rcut = E["r"] if k == "_t_encoche" else Ca["r"]
            for p in pts:
                lat = p.xy.dot(tang) / rcut                 # -1..1 à travers le creux
                if k == "_t_cannelure":
                    u = 1.0 - (p.z + L / 2) / Ca["longueur"]                 # 0 au bout arrondi, 1 à l'arrière
                else:
                    u = 0.35 * (p.z - (L / 2 - E["profondeur"])) / E["profondeur"]
                u = min(1.0, max(0.0, u))
                uvs.append(in_reg(REG_CANN, u * 0.98 + 0.01, 0.5 + 0.42 * max(-1, min(1, lat))))
        elif k == "_bague":
            rad_c = math.hypot(cu.x, cu.y)
            for p in pts:
                ang = math.atan2(p.x, p.y)
                if abs(nu.z) > 0.7:                                   # anneau vu de face : peint
                    uvs.append(face_uv(p.x if nu.z > 0 else -p.x, p.y))
                elif rad_c < (A["r_trou"] + A["r_bague"]) / 2:        # intérieur : trou sombre
                    uvs.append(face_uv(0.002 * math.sin(ang), 0.002 * math.cos(ang)))
                else:                                                 # tour de la bague : acier
                    rho = 0.0095 + 0.0012 * (abs(p.z) - L / 2) / A["saillie"]
                    uvs.append(face_uv(rho * math.sin(ang), rho * math.cos(ang)))
        for l, uv in zip(f.loops, uvs):
            l[uvl].uv = uv

    # lissage : arêtes vives au-delà de 35°
    for f in bm.faces:
        f.smooth = True
    lim = math.radians(35)
    for e in bm.edges:
        if len(e.link_faces) != 2 or e.calc_face_angle(math.pi) > lim:
            e.smooth = False
    me = bpy.data.meshes.new("SM_Barillet")
    bm.to_mesh(me); bm.free()
    for m in tmp:
        bpy.data.materials.remove(m)
    laiton = C.make_material("M_Barillet_Laiton", tex_path)
    rune = color_material("M_Barillet_Rune", P["couleur_rune"])
    me.materials.append(laiton); me.materials.append(rune)
    body = bpy.data.objects.new("SM_Barillet", me)
    bpy.context.scene.collection.objects.link(body)

    # objets de chambre : insert au fond de la chambre + pastille sur la face arrière, un seul objet
    mch = color_material("M_Barillet_Chambre", P["couleur_chambre"])
    inserts = []
    I = P["insert"]
    n = I["segs"]
    for k in range(1, 7):
        a = chamber_angle(k)
        cx, cy = Ch["r_centre"] * math.sin(a), Ch["r_centre"] * math.cos(a)
        bm = bmesh.new()
        uvl2 = bm.loops.layers.uv.new("UVMap")

        def ring(r, z):
            return [bm.verts.new(U(r * math.sin(2 * math.pi * j / n), r * math.cos(2 * math.pi * j / n), z))
                    for j in range(n)]

        def disc(vs, toward_z, r):
            f = bm.faces.new(vs)
            f.normal_update()
            if f.normal.dot(U(0, 0, toward_z)) < 0:
                f.normal_flip()
            for l in f.loops:
                v = l.vert.co
                l[uvl2].uv = (0.5 - v.x / (2 * r), 0.5 + v.z / (2 * r))
            f.smooth = False
        # insert avant : disque au fond de la chambre, tourné vers l'ouverture (+Z)
        r = Ch["r"] + 0.0002        # couvre tout le fond (le fond du corps est supprimé)
        disc(ring(r, L / 2 - Ch["profondeur"] + I["retrait"]), 1, r)
        # pastille arrière : disque en léger relief, tourné vers le joueur (-Z)
        rp, hp = I["pastille_r"], I["pastille_relief"]
        bas, haut = ring(rp, -L / 2 + 0.0002), ring(rp, -L / 2 - hp)
        disc(haut, -1, rp)
        for j in range(n):
            j1 = (j + 1) % n
            f = bm.faces.new([bas[j], bas[j1], haut[j1], haut[j]])
            f.normal_update()
            mid = (bas[j].co + bas[j1].co) / 2
            # normale vers l'extérieur de la pastille (dans le plan XZ Blender)
            rad = Vector((mid.x, 0, mid.z)).normalized()
            if f.normal.dot(rad) < 0:
                f.normal_flip()
            for l, uv in zip(f.loops, ((j / n, 0), ((j + 1) / n, 0), ((j + 1) / n, 0.1), (j / n, 0.1))):
                l[uvl2].uv = uv
            f.smooth = True
        name = "SM_Barillet_Chambre_%02d" % k
        m2 = bpy.data.meshes.new(name); bm.to_mesh(m2); bm.free()
        m2.materials.append(mch)
        ob = bpy.data.objects.new(name, m2)
        bpy.context.scene.collection.objects.link(ob)
        ob.parent = body
        ob.matrix_parent_inverse.identity()
        ob.location = U(cx, cy, 0)          # pivot : sur l'axe de la chambre, au centre du barillet
        inserts.append(ob)
    print("corps : %d tris ; inserts : %d tris chacun" % (C.tri_count(body), C.tri_count(inserts[0])))
    return [body] + inserts


# --------------------------------------------------------------------------- rendus
def render(sc, objs, basename, views, res=1024):
    os.makedirs(RENDER_DIR, exist_ok=True)
    old = C.RENDER_DIR
    C.RENDER_DIR = RENDER_DIR
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
    sc.render.image_settings.file_format = "PNG"
    sc.display.render_aa = "8"
    cam_data = bpy.data.cameras.new("_RenduCam")
    cam = bpy.data.objects.new("_RenduCam", cam_data)
    sc.collection.objects.link(cam); sc.camera = cam
    cam_data.clip_start, cam_data.clip_end = 0.001, 10
    out = []
    dirs = {   # directions Blender depuis le centre vers l'œil
        "face": U(0, 0, 1), "cote": U(1, 0, 0), "dessus": U(0, 1, 0.0001), "dos": U(0, 0, -1),
        "34": (U(0.6, 0.45, 1)).normalized(), "34dos": (U(0.65, 0.5, -1)).normalized(),
        "oeil": (U(0.3, 0.35, -1)).normalized(),   # œil du joueur : derrière, un peu au-dessus et à droite
    }
    for v in views:
        d = dirs[v]
        if v.startswith("34"):
            cam_data.type = "PERSP"; cam_data.lens = 50; dist = 0.3
        else:
            cam_data.type = "ORTHO"; cam_data.ortho_scale = 0.135; dist = 1.0
        cam.location = d * dist
        up = "Y"
        cam.rotation_euler = (-d).to_track_quat("-Z", up).to_euler()
        path = os.path.join(RENDER_DIR, "%s_%s.png" % (basename, v))
        sc.render.filepath = path
        bpy.ops.render.render(write_still=True, scene=sc.name)
        out.append(path)
    bpy.data.objects.remove(cam); bpy.data.cameras.remove(cam_data)
    C.RENDER_DIR = old
    return out


def run():
    sc = C.new_scene("SM_Barillet")
    objs = build(PARAMS)
    bpy.context.view_layer.update()
    fbx = os.path.join(MODELS_DIR, "SM_Barillet.fbx")
    C.export_fbx(objs, fbx)
    C.save_blend(sc, os.path.join(SOURCE_DIR, "SM_Barillet.blend"))
    res = C.verify_fbx(fbx)
    bpy.context.window.scene = sc
    renders = render(sc, objs, "SM_Barillet", ("face", "cote", "dessus", "dos", "34", "34dos", "oeil"))
    print(C.report(res))
    print("Rendus :", [os.path.basename(p) for p in renders])
    return res


if __name__ == "__main__":
    run()
