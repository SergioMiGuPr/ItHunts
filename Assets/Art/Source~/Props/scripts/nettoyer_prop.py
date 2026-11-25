# -*- coding: utf-8 -*-
"""Nettoyage d'un prop généré (Higgsfield → Tripo H3.1, GLB texturé) pour Unity (nuit 4, lot 4 et sa deuxième passe).

Usage (Blender en ligne de commande) :
    blender -b --factory-startup -P nettoyer_prop.py -- <Nom>            # un prop de la table PROPS
    blender -b --factory-startup -P nettoyer_prop.py -- <Nom> --inspecter # vues orthographiques du GLB brut

Étapes : import du GLB (docs/art/props/sources/<glb>) → un seul maillage → soudure des sommets → suppression des faces
dégénérées et des parties flottantes (îlots minuscules) → rotation pour mettre l'avant vers -Y → mise à l'échelle sur
les cotes réelles (1 unité = 1 m ; deuxième passe : étirement non uniforme limité à × 1,3, la hauteur d'abord) → pivot
au sol, au centre de l'emprise → décimation au budget → (coffres) découpe du couvercle : le corps est creusé d'un fond
intérieur (paroi et fond sombres, matériau « _Interieur »), les ouvertures sont refermées par des faces planes
triangulées proprement (Delaunay contraint, plus d'éventail) → texture de base en PNG 1024
(Assets/Art/Textures/Props/T_Prop_<Nom>_BaseColor.png) → LOD1 à 50 % → faces superposées corrigées
(faces_superposees.nettoie) → contrôle du maillage → FBX (réglages de kitlib.export : Unity +Y en haut, +Z en avant)
dans Assets/Art/Models/Props/SM_Prop_<Nom>.fbx. Un rapport JSON est écrit à côté du GLB, un contrôle dans
docs/art/rendus/Nuit4/Lot4/controle_<Nom>.json.

Hiérarchie du FBX : SM_Prop_<Nom>_LOD0 et SM_Prop_<Nom>_LOD1 (maillages, origine = pivot du prop) ; coffres : en plus un
vide « Couvercle » sur l'axe de la charnière, avec Couvercle_LOD0 et Couvercle_LOD1 ; lanterne : vides « Feu » (flamme)
et « Accroche » (haut de l'anneau).

Conventions : Blender Z en haut, avant vers -Y, charnière des coffres côté +Y.
"""
import json, math, os, sys
import bpy, bmesh
from mathutils import Vector, Matrix
from mathutils.geometry import delaunay_2d_cdt

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "..", "Kits", "scripts"))
sys.path.insert(0, HERE)
import kitlib  # noqa: E402  (réglages d'export FBX communs)
import faces_superposees  # noqa: E402  (copie du module des kits : faces coplanaires qui se recouvrent)

ROOT = kitlib.ROOT
SRC = os.path.join(ROOT, "docs", "art", "props", "sources")
OUT_FBX = os.path.join(ROOT, "Assets", "Art", "Models", "Props")
OUT_TEX = os.path.join(ROOT, "Assets", "Art", "Textures", "Props")
OUT_CTRL = os.path.join(ROOT, "docs", "art", "rendus", "Nuit4", "Lot4")

# Nom : glb, cotes X × Y × Z en m [largeur, profondeur, hauteur], rotation Z en degrés pour amener l'avant vers -Y,
# budget de triangles du LOD0, taille de texture, hauteur relative de la découpe du couvercle (coffres).
# garder_texture : la texture déjà éclaircie du dépôt est réutilisée (réduite à 1024), au lieu d'être réextraite du GLB.
# limite : étirement non uniforme maximal (rapport entre la plus grande et la plus petite échelle des trois axes).
# Première passe : on garde les échelles d'origine (pivots et cotes déjà intégrés) ; deuxième passe : limite 1,3.
PROPS = {
    "Tonneau":      dict(glb="tripo_tonneau_v01.glb",      dims=(0.6, 0.6, 0.9),  rot_z=0, budget=4000, tex=1024, garder_texture=True),
    "Chaise":       dict(glb="tripo_chaise_v01.glb",       dims=(0.45, 0.45, 1.0), rot_z=0, budget=2000, tex=1024, garder_texture=True),
    "Poutre":       dict(glb="tripo_poutre_v02.glb",       dims=(4.0, 0.3, 0.3),  rot_z=0, budget=3000, tex=1024, limite=1.3),
    "Sarcophage":   dict(glb="tripo_sarcophage_v01.glb",   dims=(2.2, 0.9, 0.8),  rot_z=0, budget=6000, tex=1024, garder_texture=True),
    "Ratelier":     dict(glb="tripo_ratelier_v01.glb",     dims=(1.6, 0.5, 1.8),  rot_z=0, budget=6000, tex=1024, garder_texture=True),
    "Coffre":       dict(glb="tripo_coffre_v01.glb",       dims=(0.9, 0.55, 0.6), rot_z=0, budget=5000, tex=1024, garder_texture=True,
                         couvercle=0.55, creux=0.25),
    "CoffreVerrou": dict(glb="tripo_coffreverrou_v01.glb", dims=(0.9, 0.55, 0.6), rot_z=0, budget=6000, tex=1024, garder_texture=True,
                         couvercle=0.55, creux=0.25),
    "Statue":       dict(glb="tripo_statue_v01.glb",       dims=(1.0, 1.0, 2.6),  rot_z=0, budget=6000, tex=1024, garder_texture=True),
    # deuxième passe (nuit 4, lot 4 b)
    "Lanterne":     dict(glb="tripo_lanterne_v01.glb",     dims=(0.5, 0.5, 0.8),  rot_z=0, budget=1500, tex=1024, limite=1.3,
                         feu=0.36),
    "Etagere":      dict(glb="tripo_etagere_v01.glb",      dims=(2.6, 0.7, 3.2),  rot_z=0, budget=5500, tex=1024, limite=1.3),
    "CoffreDeco":   dict(glb="tripo_coffredeco_v01.glb",   dims=(1.2, 0.8, 0.8),  rot_z=0, budget=3000, tex=1024, limite=1.3),
    "Table":        dict(glb="tripo_table_v01.glb",        dims=(2.6, 1.2, 0.95), rot_z=0, budget=3000, tex=1024, limite=1.3),
    "Banc":         dict(glb="tripo_banc_v01.glb",         dims=(2.6, 0.6, 0.55), rot_z=0, budget=2000, tex=1024, limite=1.3),
    "Armoire":      dict(glb="tripo_armoire_v01.glb",      dims=(1.6, 0.8, 2.7),  rot_z=0, budget=4500, tex=1024, limite=1.3),
    "Lit":          dict(glb="tripo_lit_v01.glb",          dims=(1.3, 2.3, 0.7),  rot_z=0, budget=4000, tex=1024, limite=1.3),
    "Chaudron":     dict(glb="tripo_chaudron_v01.glb",     dims=(1.3, 1.3, 1.0),  rot_z=0, budget=3500, tex=1024, limite=1.3),
    # nuit 6, lot 1 (ensembles)
    "AngePleureur": dict(glb="tripo_angepleureur_v01.glb", dims=(1.6, 1.6, 2.4),  rot_z=0, budget=7000, tex=2048, limite=1.3),
    "Fauteuil":     dict(glb="tripo_fauteuil_v01.glb",     dims=(0.9, 0.9, 1.75), rot_z=0, budget=5000, tex=1024, limite=1.3),
}
INSPECT = os.environ.get("PROPS_INSPECT", os.path.join(SRC, "inspect"))  # vues de contrôle (hors dépôt de préférence)
OVERRIDES = os.path.join(SRC, "reglages_props.json")  # rot_z / couvercle / dims relevés à l'inspection (prioritaires)


def args():
    a = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    return a[0], "--inspecter" in a


def importer(path):
    kitlib.reset()
    bpy.ops.import_scene.gltf(filepath=path)
    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    bpy.ops.object.select_all(action="DESELECT")
    for o in meshes:
        o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    if len(meshes) > 1:
        bpy.ops.object.join()
    ob = bpy.context.view_layer.objects.active
    # détacher des parents (nœuds vides du glTF) en gardant la transformation, puis l'appliquer
    mw = ob.matrix_world.copy()
    ob.parent = None
    ob.matrix_world = mw
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    for o in list(bpy.context.scene.objects):
        if o is not ob:
            bpy.data.objects.remove(o, do_unlink=True)
    ob.name = "Prop"
    return ob


def bbox(ob):
    vs = [v.co for v in ob.data.vertices]
    mn = Vector((min(v.x for v in vs), min(v.y for v in vs), min(v.z for v in vs)))
    mx = Vector((max(v.x for v in vs), max(v.y for v in vs), max(v.z for v in vs)))
    return mn, mx


def bbox_monde(objs):
    pts = [o.matrix_world @ v.co for o in objs if o.type == "MESH" for v in o.data.vertices]
    mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return mn, mx


def tris(ob):
    return sum(len(p.vertices) - 2 for p in ob.data.polygons)


def nettoyer(ob, rapport):
    bm = bmesh.new()
    bm.from_mesh(ob.data)
    n0 = len(bm.verts)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    rapport["sommets_soudes"] = n0 - len(bm.verts)
    # faces dégénérées (aire nulle) et arêtes de longueur nulle
    mn, mx = Vector((1e9,) * 3), Vector((-1e9,) * 3)
    for v in bm.verts:
        mn = Vector(map(min, mn, v.co)); mx = Vector(map(max, mx, v.co))
    diag = (mx - mn).length
    bmesh.ops.dissolve_degenerate(bm, edges=bm.edges, dist=diag * 1e-6)
    deg = [f for f in bm.faces if f.calc_area() < (diag * 1e-6) ** 2]
    bmesh.ops.delete(bm, geom=deg, context="FACES")
    rapport["faces_degenerees"] = len(deg)
    loose = [v for v in bm.verts if not v.link_faces]
    bmesh.ops.delete(bm, geom=loose, context="VERTS")
    # îlots : on supprime les parties flottantes minuscules (débris de reconstruction)
    bm.verts.ensure_lookup_table()
    seen, islands = set(), []
    for v in bm.verts:
        if v.index in seen:
            continue
        stack, isl = [v], []
        seen.add(v.index)
        while stack:
            c = stack.pop()
            isl.append(c)
            for e in c.link_edges:
                o = e.other_vert(c)
                if o.index not in seen:
                    seen.add(o.index)
                    stack.append(o)
        islands.append(isl)
    retires = 0
    for isl in islands:
        a = Vector((min(v.co.x for v in isl), min(v.co.y for v in isl), min(v.co.z for v in isl)))
        b = Vector((max(v.co.x for v in isl), max(v.co.y for v in isl), max(v.co.z for v in isl)))
        if (b - a).length < diag * 0.015 and len(isl) < 60:
            bmesh.ops.delete(bm, geom=isl, context="VERTS")
            retires += 1
    rapport["ilots"] = len(islands)
    rapport["ilots_flottants_retires"] = retires
    bm.to_mesh(ob.data)
    bm.free()
    ob.data.update()


def assainir(ob):
    """Après une décimation : sommets confondus, arêtes nulles, faces d'aire nulle et sommets isolés retirés."""
    bm = bmesh.new()
    bm.from_mesh(ob.data)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-6)
    bmesh.ops.dissolve_degenerate(bm, edges=bm.edges, dist=1e-6)
    deg = [f for f in bm.faces if f.calc_area() < 1e-10]
    if deg:
        bmesh.ops.delete(bm, geom=deg, context="FACES")
    loose = [v for v in bm.verts if not v.link_faces]
    if loose:
        bmesh.ops.delete(bm, geom=loose, context="VERTS")
    bmesh.ops.triangulate(bm, faces=bm.faces[:])
    bm.to_mesh(ob.data)
    bm.free()
    ob.data.update()


def aligner(ob):
    """Tourne le maillage autour de Z pour que son emprise au sol soit un rectangle d'aire minimale (Tripo pose
    parfois l'objet de biais, comme sur l'image 3/4). Renvoie l'angle appliqué en degrés (entre -45 et 45)."""
    pts = [(v.co.x, v.co.y) for v in ob.data.vertices]
    best, ang = None, 0.0
    for i in range(-90, 91):
        a = math.radians(i * 0.5)
        c, s = math.cos(a), math.sin(a)
        xs = [x * c - y * s for x, y in pts]
        ys = [x * s + y * c for x, y in pts]
        aire = (max(xs) - min(xs)) * (max(ys) - min(ys))
        if best is None or aire < best:
            best, ang = aire, i * 0.5
    ob.data.transform(Matrix.Rotation(math.radians(ang), 4, "Z"))
    ob.data.update()
    return ang


def limiter(s, k, poids=(1.0, 1.0, 2.0)):
    """Échelles par axe ramenées dans une fenêtre [a, k·a] : l'étirement non uniforme ne dépasse pas k. a minimise
    l'écart pondéré (en log) aux cotes visées ; par défaut la hauteur compte double (cote prioritaire)."""
    if max(s) / min(s) <= k:
        return s
    best = None
    lo, hi = min(s) / k, max(s)
    for i in range(401):
        a = lo * (hi / lo) ** (i / 400)
        c = Vector([min(max(x, a), a * k) for x in s])
        err = sum(w * math.log(c[j] / s[j]) ** 2 for j, w in enumerate(poids))
        if best is None or err < best[0]:
            best = (err, c)
    return best[1]


def placer(ob, rot_z, dims, uniforme=False, limite=None, poids=(1.0, 1.0, 2.0)):
    """uniforme : une seule échelle, réglée sur la hauteur (statue : on ne l'élargit pas pour remplir l'emprise).
    limite : étirement non uniforme maximal (voir limiter)."""
    ob.data.transform(Matrix.Rotation(math.radians(rot_z), 4, "Z"))
    mn, mx = bbox(ob)
    size = mx - mn
    s = Vector((dims[0] / size.x, dims[1] / size.y, dims[2] / size.z))
    if uniforme:
        s = Vector((s.z, s.z, s.z))
    elif limite:
        s = limiter(s, limite, poids)
    c = Vector(((mn.x + mx.x) / 2, (mn.y + mx.y) / 2, mn.z))
    ob.data.transform(Matrix.Translation(-c))
    ob.data.transform(Matrix.Diagonal((s.x, s.y, s.z, 1.0)))
    ob.data.update()
    return s


def decimer(ob, budget, rapport=None):
    t = tris(ob)
    if rapport is not None:
        rapport["triangles_tripo"] = t
    if t <= budget:
        return
    m = ob.modifiers.new("Dec", "DECIMATE")
    m.ratio = budget / t * 0.98
    m.use_collapse_triangulate = True
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.modifier_apply(modifier=m.name)
    assainir(ob)


def trianguler(ob):
    bm = bmesh.new()
    bm.from_mesh(ob.data)
    bmesh.ops.triangulate(bm, faces=bm.faces)
    bm.to_mesh(ob.data)
    bm.free()


def texture(ob, nom, taille, garder):
    """Texture de base en PNG (taille × taille) et matériau M_Prop_<Nom> (une texture, sans PBR).
    garder : la texture du dépôt (déjà éclaircie) est réutilisée et réduite, sinon elle est extraite du GLB."""
    os.makedirs(OUT_TEX, exist_ok=True)
    path = os.path.join(OUT_TEX, "T_Prop_%s_BaseColor.png" % nom)
    src = None
    if garder and os.path.exists(path):
        img = bpy.data.images.load(path)
        src = list(img.size[:])
        if img.size[0] != taille:
            img.scale(taille, taille)
            img.filepath_raw = path
            img.file_format = "PNG"
            img.save()
    else:
        img = None
        for m in ob.data.materials:
            if m and m.use_nodes:
                for n in m.node_tree.nodes:
                    if n.type == "TEX_IMAGE" and n.image:
                        img = n.image
                        break
            if img:
                break
        src = list(img.size[:]) if img else None
        if img:
            img = img.copy()
            if img.size[0] != taille:
                img.scale(taille, taille)
            img.filepath_raw = path
            img.file_format = "PNG"
            img.save()
            img.filepath = path
    mat = bpy.data.materials.new("M_Prop_%s" % nom)
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
    bsdf.inputs["Roughness"].default_value = 1.0
    if os.path.exists(path):
        tex = nt.nodes.new("ShaderNodeTexImage")
        tex.image = bpy.data.images.load(path, check_existing=True)
        nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    ob.data.materials.clear()
    ob.data.materials.append(mat)
    return src, path


# ------------------------------------------------------------------ coffres : couvercle, ouvertures et fond creusé
def _boucles(bm, zc):
    """Boucles de bord (arêtes libres) à la hauteur de coupe, en listes ordonnées de sommets."""
    bnd = [e for e in bm.edges if e.is_boundary and all(abs(v.co.z - zc) < 1e-4 for v in e.verts)]
    adj = {}
    for e in bnd:
        a, b = e.verts
        adj.setdefault(a, []).append(b)
        adj.setdefault(b, []).append(a)
    vus, boucles = set(), []
    for v0 in adj:
        if v0 in vus:
            continue
        boucle, prec, cour = [v0], None, v0
        vus.add(v0)
        while True:
            suiv = [w for w in adj[cour] if w is not prec and w not in vus]
            if not suiv:
                break
            prec, cour = cour, suiv[0]
            boucle.append(cour)
            vus.add(cour)
        if len(boucle) >= 3:
            boucles.append(boucle)
    return boucles


def _aire(p):
    return 0.5 * sum(p[i][0] * p[(i + 1) % len(p)][1] - p[(i + 1) % len(p)][0] * p[i][1] for i in range(len(p)))


def _dedans(pt, poly):
    x, y = pt
    r = False
    for i in range(len(poly)):
        (x1, y1), (x2, y2) = poly[i], poly[(i + 1) % len(poly)]
        if (y1 > y) != (y2 > y) and x < x1 + (y - y1) * (x2 - x1) / (y2 - y1):
            r = not r
    return r


def _coupe(a, b, c, d):
    def o(p, q, r):
        return (q[0] - p[0]) * (r[1] - p[1]) - (q[1] - p[1]) * (r[0] - p[0])
    return o(a, b, c) * o(a, b, d) < 0 and o(c, d, a) * o(c, d, b) < 0


def _rectangle(poly):
    """Plus grand rectangle aligné (bbox rétrécie pas à pas) entièrement dans le polygone, à 1.5 cm au moins du bord."""
    x0, x1 = min(p[0] for p in poly), max(p[0] for p in poly)
    y0, y1 = min(p[1] for p in poly), max(p[1] for p in poly)
    m = 0.03
    while m < min(x1 - x0, y1 - y0) / 2 - 0.02:
        r = [(x0 + m, y0 + m), (x1 - m, y0 + m), (x1 - m, y1 - m), (x0 + m, y1 - m)]
        ok = all(_dedans(c, poly) for c in r)
        ok = ok and not any(r[0][0] - 0.015 < p[0] < r[1][0] + 0.015 and r[0][1] - 0.015 < p[1] < r[2][1] + 0.015 for p in poly)
        ok = ok and not any(_coupe(poly[i], poly[(i + 1) % len(poly)], r[j], r[(j + 1) % 4])
                            for i in range(len(poly)) for j in range(4))
        if ok:
            return r, m
        m += 0.005
    return None, None


def _uv(bm, faces):
    uv = bm.loops.layers.uv.active
    if uv:
        for f in faces:
            for l in f.loops:
                l[uv].uv = (l.vert.co.x * 0.5 + 0.5, l.vert.co.y * 0.5 + 0.5)


def _face(bm, vs, haut, idx):
    """Face (ou rien si elle existe déjà) orientée vers +Z (haut=True) ou -Z."""
    try:
        f = bm.faces.new(vs)
    except ValueError:
        return None
    f.normal_update()
    if (f.normal.z < 0) == haut:
        f.normal_flip()
    f.material_index = idx
    return f


def refermer(o, zc, idx, haut, creux):
    """Referme les ouvertures de la coupe (hauteur zc) par des faces planes triangulées en Delaunay contraint.
    Boucle principale : un rebord (épaisseur de paroi) autour d'un rectangle intérieur ; si creux > 0, le rectangle
    descend de creux (parois et fond, « fond intérieur ») ; sinon il est fermé par deux triangles. Renvoie un résumé."""
    bm = bmesh.new()
    bm.from_mesh(o.data)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-6)
    boucles = _boucles(bm, zc)
    boucles.sort(key=lambda b: -abs(_aire([(v.co.x, v.co.y) for v in b])))
    res = dict(boucles=len(boucles), paroi_m=None, fond_m=None, faces_ajoutees=0)
    nouvelles = []
    for k, boucle in enumerate(boucles):
        poly = [(v.co.x, v.co.y) for v in boucle]
        if _aire(poly) < 0:
            boucle, poly = boucle[::-1], poly[::-1]
        rect, marge = _rectangle(poly) if k == 0 else (None, None)
        pts = [Vector(p) for p in poly] + ([Vector(c) for c in rect] if rect else [])
        n = len(poly)
        aretes = [(n + j, n + (j + 1) % 4) for j in range(4)] if rect else []
        out = delaunay_2d_cdt(pts, aretes, [list(range(n))], 1, 1e-6, True)
        overts, _, ofaces, orig, _, _ = out
        bv = []
        for i, co in enumerate(overts):
            src = [j for j in orig[i] if j < n]
            if src:
                bv.append(boucle[src[0]])
            else:
                bv.append(bm.verts.new((co.x, co.y, zc)))
        coins = {}
        if rect:
            for i in range(len(overts)):
                for j in orig[i]:
                    if j >= n:
                        coins[j - n] = bv[i]
        for f in ofaces:
            c = sum((overts[i] for i in f), Vector((0, 0))) / len(f)
            dans_rect = rect and rect[0][0] < c.x < rect[1][0] and rect[0][1] < c.y < rect[2][1]
            if dans_rect and creux > 0:
                continue
            nf = _face(bm, [bv[i] for i in f], haut, idx)
            if nf:
                nouvelles.append(nf)
        if rect and creux > 0 and len(coins) == 4:
            bas = [bm.verts.new((coins[j].co.x, coins[j].co.y, zc - creux)) for j in range(4)]
            centre = Vector(((rect[0][0] + rect[1][0]) / 2, (rect[0][1] + rect[2][1]) / 2, zc - creux / 2))
            for j in range(4):
                a, b = j, (j + 1) % 4
                try:
                    f = bm.faces.new([coins[a], coins[b], bas[b], bas[a]])
                except ValueError:
                    continue
                f.normal_update()
                if f.normal.dot(centre - f.calc_center_median()) < 0:   # parois tournées vers l'intérieur du coffre
                    f.normal_flip()
                f.material_index = idx
                nouvelles.append(f)
            fond = _face(bm, bas, True, idx)
            if fond:
                nouvelles.append(fond)
            res["paroi_m"] = round(marge, 3)
            res["fond_m"] = round(creux, 3)
        elif rect:
            res["paroi_m"] = round(marge, 3)
    _uv(bm, nouvelles)
    res["faces_ajoutees"] = len(nouvelles)
    bmesh.ops.triangulate(bm, faces=[f for f in nouvelles if f.is_valid])
    bm.to_mesh(o.data)
    bm.free()
    o.data.update()
    return res


def couvercle(ob, frac, nom, creux, rapport, piv=None):
    """Coupe le maillage à la hauteur frac × H : la partie haute devient le couvercle, dont l'origine est sur la
    charnière (bord arrière +Y, au niveau de la coupe). Corps : fond creusé de creux m ; couvercle : face plane."""
    mn, mx = bbox(ob)
    zc = piv.z if piv is not None else mn.z + (mx.z - mn.z) * frac
    interieur = bpy.data.materials.new("M_Prop_%s_Interieur" % nom)
    interieur.use_nodes = True
    next(n for n in interieur.node_tree.nodes if n.type == "BSDF_PRINCIPLED").inputs["Base Color"].default_value = (0.02, 0.015, 0.012, 1)
    ob.data.materials.append(interieur)
    idx_int = len(ob.data.materials) - 1

    bm = bmesh.new()
    bm.from_mesh(ob.data)
    geom = bm.verts[:] + bm.edges[:] + bm.faces[:]
    res = bmesh.ops.bisect_plane(bm, geom=geom, dist=1e-6, plane_co=(0, 0, zc), plane_no=(0, 0, 1))
    cut_edges = [e for e in res["geom_cut"] if isinstance(e, bmesh.types.BMEdge)]
    bmesh.ops.split_edges(bm, edges=cut_edges)
    bm.to_mesh(ob.data)
    bm.free()

    bpy.ops.object.select_all(action="DESELECT")
    bpy.context.view_layer.objects.active = ob
    ob.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="DESELECT")
    bpy.ops.object.mode_set(mode="OBJECT")
    for p in ob.data.polygons:
        p.select = p.center.z > zc
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.separate(type="SELECTED")
    bpy.ops.object.mode_set(mode="OBJECT")
    lid = next(o for o in bpy.context.selected_objects if o is not ob)

    r_corps = refermer(ob, zc, idx_int, True, creux)
    r_couv = refermer(lid, zc, idx_int, False, 0.0)
    # origine du couvercle : charnière arrière (y max) au niveau de la coupe
    if piv is None:
        _, lmx = bbox(lid)
        piv = Vector((0.0, lmx.y, zc))
    lid.data.transform(Matrix.Translation(-piv))
    lid.location = piv
    rapport["couvercle"] = dict(hauteur_coupe=round(zc, 3), charniere=[round(c, 3) for c in piv],
                                triangles=tris(lid), ouverture_corps=r_corps, ouverture_couvercle=r_couv)
    return lid, piv


# ------------------------------------------------------------------ LOD, contrôles
def lod1(ob, nom):
    """Copie décimée à 50 % (LOD1)."""
    c = ob.copy()
    c.data = ob.data.copy()
    c.name = c.data.name = nom
    bpy.context.scene.collection.objects.link(c)
    m = c.modifiers.new("Dec", "DECIMATE")
    m.ratio = 0.5
    m.use_collapse_triangulate = True
    bpy.context.view_layer.objects.active = c
    bpy.ops.object.modifier_apply(modifier=m.name)
    assainir(c)
    return c


def controle(o):
    """Contrôle d'un maillage : sommets isolés, arêtes nulles, faces dégénérées, arêtes non manifold, faces superposées."""
    me = o.data
    isoles = sum(1 for v in me.vertices if True) - len({i for p in me.polygons for i in p.vertices})
    nulles = sum(1 for e in me.edges if (me.vertices[e.vertices[0]].co - me.vertices[e.vertices[1]].co).length < 1e-6)
    bm = bmesh.new()
    bm.from_mesh(me)
    non_manifold = sum(1 for e in bm.edges if not e.is_manifold and not e.is_boundary)
    bord = sum(1 for e in bm.edges if e.is_boundary)
    bm.free()
    sup = faces_superposees.analyse(o, False)
    return dict(triangles=tris(o), sommets_isoles=isoles, aretes_nulles=nulles, faces_degenerees=sup["degenerees"],
                superposees=sup["superposees"], superposees_partielles=sup["partielles"],
                aretes_non_manifold=non_manifold, aretes_de_bord=bord)


def inspecter(ob, nom):
    """Vues orthographiques (dessus, face -Y, côté +X) du GLB brut pour relever rot_z et la coupe du couvercle."""
    mn, mx = bbox(ob)
    c = (mn + mx) / 2
    r = (mx - mn).length
    scn = bpy.context.scene
    try:
        scn.render.engine = "BLENDER_EEVEE_NEXT"
    except TypeError:
        scn.render.engine = "BLENDER_EEVEE"
    scn.render.resolution_x = scn.render.resolution_y = 512
    w = bpy.data.worlds.new("W"); w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.5, 0.5, 0.5, 1)
    w.node_tree.nodes["Background"].inputs[1].default_value = 1.0
    scn.world = w
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    scn.collection.objects.link(cam)
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = r * 1.05
    scn.camera = cam
    out = []
    for vue, d in (("dessus", Vector((0, 0, 1))), ("face", Vector((0, -1, 0))), ("cote", Vector((1, 0, 0)))):
        cam.location = c + d * r * 2
        cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
        p = os.path.join(INSPECT, "%s_%s.png" % (nom, vue))
        os.makedirs(os.path.dirname(p), exist_ok=True)
        scn.render.filepath = p
        bpy.ops.render.render(write_still=True)
        out.append(p)
    print("INSPECT", nom, "taille", [round(x, 3) for x in (mx - mn)], "tris", tris(ob), out)


def vide(nom, loc, parent=None):
    e = bpy.data.objects.new(nom, None)
    e.empty_display_size = 0.05
    bpy.context.scene.collection.objects.link(e)
    e.location = loc
    if parent:
        e.parent = parent
    return e


def main():
    nom, insp = args()
    cfg = dict(PROPS[nom])
    if os.path.exists(OVERRIDES):
        with open(OVERRIDES, encoding="utf-8") as f:
            cfg.update(json.load(f).get(nom, {}))
    ob = importer(os.path.join(SRC, cfg["glb"]))
    ang = aligner(ob) if cfg.get("aligner", True) else 0.0
    if insp:
        print("ALIGNEMENT", ang)
        inspecter(ob, nom)
        return
    rapport = dict(nom=nom, glb=cfg["glb"], alignement_deg=ang, rot_z=cfg["rot_z"])
    nettoyer(ob, rapport)
    s = placer(ob, cfg["rot_z"], cfg["dims"], cfg.get("uniforme", False), cfg.get("limite"), tuple(cfg.get("poids", (1.0, 1.0, 2.0))))
    rapport["echelle_appliquee"] = [round(x, 4) for x in s]
    rapport["etirement_max"] = round(max(s) / min(s), 3)
    rapport["cotes_visees"] = list(cfg["dims"])
    decimer(ob, cfg["budget"] - (300 if cfg.get("couvercle") else 0), rapport)
    trianguler(ob)
    src, tex = texture(ob, nom, cfg["tex"], cfg.get("garder_texture", False))
    rapport["texture_tripo"] = src
    rapport["texture"] = os.path.relpath(tex, ROOT).replace("\\", "/")
    rapport["texture_taille"] = cfg["tex"]

    base = "SM_Prop_%s" % nom
    racines = []
    ob.name = ob.data.name = base + "_LOD0"
    l1 = lod1(ob, base + "_LOD1") if not cfg.get("sans_lod") else None
    if cfg.get("couvercle"):
        # le couvercle est coupé dans le LOD0 et dans le LOD1 (même hauteur, même charnière)
        lid0, piv = couvercle(ob, cfg["couvercle"], nom, cfg.get("creux", 0.0), rapport)
        lid1, _ = couvercle(l1, cfg["couvercle"], nom + "_L1", cfg.get("creux", 0.0), dict(), piv)
        interieur = ob.data.materials[len(ob.data.materials) - 1]     # un seul matériau intérieur pour les deux LOD
        for o in (l1, lid1):
            o.data.materials[len(o.data.materials) - 1] = interieur
        pivot = vide("Couvercle", piv)
        for lid, n in ((lid0, "Couvercle_LOD0"), (lid1, "Couvercle_LOD1")):
            lid.name = lid.data.name = n
            lid.location = (0, 0, 0)
            lid.parent = pivot
            lid.matrix_parent_inverse = Matrix.Identity(4)
        racines.append(pivot)
    racines = [ob] + ([l1] if l1 else []) + racines
    if cfg.get("feu"):
        mn, mx = bbox(ob)
        vide("Feu", (0.0, 0.0, round((mx.z - mn.z) * cfg["feu"], 3)))
        vide("Accroche", (0.0, 0.0, round(mx.z, 3)))
        racines += [bpy.data.objects["Feu"], bpy.data.objects["Accroche"]]
        rapport["feu"] = list(bpy.data.objects["Feu"].location)
        rapport["accroche"] = list(bpy.data.objects["Accroche"].location)
    bpy.context.view_layer.update()

    # faces superposées corrigées à la source, puis contrôle de chaque maillage
    maillages = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    ctrl = dict(nom=nom, objets={})
    for o in maillages:
        ctrl["objets"][o.name] = dict(avant=faces_superposees.nettoie(o))
        assainir(o)
        ctrl["objets"][o.name]["apres"] = controle(o)
    lod0 = [o for o in maillages if o.name.endswith("_LOD0")]
    lod1s = [o for o in maillages if o.name.endswith("_LOD1")]
    mn, mx = bbox_monde(lod0)
    rapport["dimensions"] = [round(x, 3) for x in (mx - mn)]
    rapport["pivot_bas"] = round(mn.z, 4)
    rapport["centre_emprise"] = [round((mn.x + mx.x) / 2, 4), round((mn.y + mx.y) / 2, 4)]
    rapport["triangles"] = sum(tris(o) for o in lod0)
    rapport["triangles_lod1"] = sum(tris(o) for o in lod1s)
    ctrl.update(dimensions=rapport["dimensions"], triangles_lod0=rapport["triangles"], triangles_lod1=rapport["triangles_lod1"])
    ctrl["ok"] = all(v["apres"]["sommets_isoles"] == 0 and v["apres"]["aretes_nulles"] == 0 and v["apres"]["faces_degenerees"] == 0
                     and v["apres"]["superposees"] == 0 and v["apres"]["superposees_partielles"] == 0 for v in ctrl["objets"].values())

    os.makedirs(OUT_FBX, exist_ok=True)
    fbx = os.path.join(OUT_FBX, "SM_Prop_%s.fbx" % nom)
    kitlib.export(racines, fbx)
    rapport["fbx"] = os.path.relpath(fbx, ROOT).replace("\\", "/")
    rapport["objets_fbx"] = sorted(o.name for o in bpy.context.scene.objects)
    with open(os.path.join(SRC, "rapport_%s.json" % nom), "w", encoding="utf-8") as f:
        json.dump(rapport, f, ensure_ascii=False, indent=1)
    os.makedirs(OUT_CTRL, exist_ok=True)
    with open(os.path.join(OUT_CTRL, "controle_%s.json" % nom), "w", encoding="utf-8") as f:
        json.dump(ctrl, f, ensure_ascii=False, indent=1)
    print("RAPPORT", json.dumps(rapport, ensure_ascii=False))
    print("CONTROLE", nom, "ok" if ctrl["ok"] else "A VOIR", json.dumps({k: v["apres"] for k, v in ctrl["objets"].items()}))


main()
