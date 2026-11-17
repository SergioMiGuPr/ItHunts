"""Correctif de la tête v3 (2026-11-17) : la pièce « tête » d'origine (stage3) était bornée à 2.6 m autour de la coupe du cou,
or le museau, les yeux et la bouche sont à 2.6–2.9 m : ces faces manquaient (trous) et des fragments de la retopologie
(voxels, matériau du corps) restaient à leur place. Ici :
1. la pièce de la tête est retirée du maillage final (îlots entièrement côté tête), avec les fragments de la retopologie ;
2. elle est refaite depuis Traq_Source (stage3.blend) par le même plan de coupe, **sans borne de rayon**, avec sa jupe ;
   la calotte crânienne (sous les mèches) ferme l'arrière du crâne, plaquée derrière le visage (n'apparaît qu'aux trous) ;
3. poids repris du maillage précédent (point le plus proche), normales : celles d'avant sur le reste du corps, celles de
   la surface d'origine autour de la couture ;
4. pupilles : le masque d'émission (T_Traqueur_Emission) est agrandi en disques de PUPIL_R autour du centre de chaque œil,
   peints sur toutes les faces de la tête qui passent dans la sphère (lisibles de face et d'en bas) ;
5. export FBX et SK_Traqueur.blend (mêmes objets, os et clips).
À lancer une seule fois, sur le SK_Traqueur.blend de la PR #13 (le fichier est réécrit).
Lancer : blb.sh SK_Traqueur.blend tete_fix.py"""
import sys, os, json, math
sys.path.insert(0, r"C:\Users\PC\ItHunts\ItHunts-art\Assets\Art\Source~\Ennemis\scripts")
for m in ("sk_traqueur", "sm_traqueur", "peinture", "ennemis_common", "poids_lisses"):
    sys.modules.pop(m, None)
import sk_traqueur as K
import numpy as np
import bpy, bmesh
from mathutils import Vector
from mathutils.bvhtree import BVHTree

OUT = r"C:\Users\PC\AppData\Local\Temp\claude\fa13b7ae4f76688a\5c2bc682-360e-47d5-a52c-698998cd4727\scratchpad\traq2"
ROOT = r"C:\Users\PC\ItHunts\ItHunts-art"
TEXD = os.path.join(ROOT, "Assets", "Art", "Textures", "Ennemis")
EXPORT = os.environ.get("EXPORT", "0") == "1"
PUPIL_R = float(os.environ.get("PUPIL_R", "0.2"))          # rayon du disque émissif (m) : 0.4 m de diamètre
sc = bpy.context.scene; vl = bpy.context.view_layer
for o in list(bpy.data.objects):
    if o.name not in sc.objects:
        sc.collection.objects.link(o)
ob = bpy.data.objects["SK_Traqueur"]; arm = bpy.data.objects["Traqueur"]; hair = bpy.data.objects["SK_Traqueur_Cheveux"]
me = ob.data
with bpy.data.libraries.load(os.path.join(OUT, "stage3.blend")) as (a, b):
    b.objects = ["Traq_Source", "SK_Traqueur"]
src, s3 = b.objects
src.name = "_src"; s3.name = "_s3"
B = {n: (Vector(h), Vector(t)) for n, h, t, p, c in json.load(open(os.path.join(OUT, "stage1.json")))["table"]}
h, t = B["Cou"]; N = (t - h).normalized(); P = h + (t - h) * 0.75
Pn, Nn = np.array(P[:]), np.array(N[:])
REP = []
def log(s):
    print(s); REP.append(s)

# --- 0. contrôle : la liaison du maillage final est celle de stage3
co = np.zeros(len(me.vertices) * 3); me.vertices.foreach_get("co", co); co = co.reshape(-1, 3)
c3 = np.zeros(len(s3.data.vertices) * 3); s3.data.vertices.foreach_get("co", c3); c3 = c3.reshape(-1, 3)
assert co.shape == c3.shape, (co.shape, c3.shape)
log("écart liaison finale / stage3 : %.2e m" % np.abs(co - c3).max())
M = np.array(src.matrix_world); assert np.allclose(M, np.eye(4)), M

# --- 1. sauvegardes : normales par position, poids
me.calc_loop_triangles() if hasattr(me, "calc_loop_triangles") else None
cn = np.zeros(len(me.loops) * 3); me.corner_normals.foreach_get("vector", cn) if hasattr(me, "corner_normals") else None
cn = cn.reshape(-1, 3); lv = np.zeros(len(me.loops), int); me.loops.foreach_get("vertex_index", lv)
vn_old = np.zeros((len(me.vertices), 3)); np.add.at(vn_old, lv, cn)
vn_old /= np.maximum(np.linalg.norm(vn_old, axis=1, keepdims=True), 1e-9)
key = lambda p: (round(p[0], 4), round(p[1], 4), round(p[2], 4))
NORM = {key(p): n for p, n in zip(co, vn_old)}
old = ob.copy(); old.data = me.copy(); old.name = "_old"; sc.collection.objects.link(old)
GN = [g.name for g in ob.vertex_groups]

# --- 3. nouvelle pièce depuis la source, même plan, sans borne de rayon
pm = src.data.copy(); pm.name = "_tete"
bm = bmesh.new(); bm.from_mesh(pm)
bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.002)   # coutures de la source Tripo
geom = list(bm.verts) + list(bm.edges) + list(bm.faces)
bmesh.ops.bisect_plane(bm, geom=geom, plane_co=P, plane_no=N, dist=1e-5)
cen = np.array([f.calc_center_median()[:] for f in bm.faces])
side = (cen - Pn) @ Nn; d = np.linalg.norm(cen - Pn, axis=1)
keep = (side > 0) & (d < 6.0)          # mâchoire comprise (elle pend sous le cou)
log("source côté tête : %d faces (dont %d sous l'ancien seuil de hauteur : mâchoire)" % (keep.sum(), (keep & (cen[:, 2] <= Pn[2] - 0.4)).sum()))
bmesh.ops.delete(bm, geom=[f for f, k in zip(bm.faces, keep) if not k], context="FACES")
bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context="VERTS")
# on ne garde que la plus grande partie connexe (la tête) : pas de miettes détachées
bm.faces.ensure_lookup_table()
comp = {}; lab = 0
for f0 in bm.faces:
    if f0 in comp:
        continue
    stack = [f0]; comp[f0] = lab
    while stack:
        f = stack.pop()
        for e in f.edges:
            for g in e.link_faces:
                if g not in comp:
                    comp[g] = lab; stack.append(g)
    lab += 1
sizes = np.bincount([comp[f] for f in bm.faces])
log("parties connexes de la tête : %s" % sorted(sizes.tolist(), reverse=True)[:8])
big = int(np.argmax(sizes))
bmesh.ops.delete(bm, geom=[f for f in bm.faces if comp[f] != big], context="FACES")
bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context="VERTS")
CAP = []
def boundary_loops(bm, skip):
    """Boucles de bord orientées comme leurs faces (a→b dans la face), hors arêtes `skip`."""
    nxt = {}
    for e in bm.edges:
        if not e.is_boundary or e in skip: continue
        f = e.link_faces[0]
        for l in f.loops:
            if l.edge is e:
                nxt[l.vert] = (l.link_loop_next.vert, e)
    loops = []; used = set()
    for v0 in list(nxt):
        if v0 in used: continue
        L = []; v = v0
        while v in nxt and v not in used:
            used.add(v); L.append(v); v = nxt[v][0]
        if len(L) > 2: loops.append(L)
    return loops
on_plane = lambda e: all(abs((v.co - P).dot(N)) < 1e-3 for v in e.verts)
# éclats : faces pendantes (au moins 2 arêtes de bord hors couture), retirées en quelques passes
for _ in range(6):
    sh = [f for f in bm.faces if sum(1 for e in f.edges if e.is_boundary and not on_plane(e)) >= 2]
    if not sh: break
    bmesh.ops.delete(bm, geom=sh, context="FACES")
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context="VERTS")
    CAP.append(len(sh))
log("éclats retirés par passe : %s" % CAP)
skip = {e for e in bm.edges if e.is_boundary and on_plane(e)}
LP = boundary_loops(bm, skip)
for L in sorted(LP, key=len, reverse=True)[:6]:
    c = np.array([v.co[:] for v in L])
    log("  boucle %d sommets, centre %s, étendue %s" % (len(L), c.mean(0).round(2), (c.max(0) - c.min(0)).round(2)))
# petits trous (moins de 12 arêtes) : bouchés
small = [e for L in LP if len(L) < 12 for e in bm.edges if e.is_boundary and e.verts[0] in set(L) and e.verts[1] in set(L)]
if small:
    r_ = bmesh.ops.holes_fill(bm, edges=small, sides=12); log("petits trous bouchés : %d faces" % len(r_["faces"]))
# calotte : ellipsoïde (axes du monde, symétrique en x) ajusté par moindres carrés aux bords de l'ouverture du crâne
BV = np.array([v.co[:] for e in bm.edges if e.is_boundary and e not in skip for v in e.verts])
BV = BV[(BV - Pn) @ Nn > 0.3]
YF = float(os.environ.get("CAP_YF", "2.2")); ZB = float(os.environ.get("CAP_ZB", "17.6"))
BV = BV[(BV[:, 1] < YF) & (BV[:, 2] > ZB)]                 # couronne : en arrière du visage, au-dessus des oreilles
log("couronne : %d sommets, x %s, y %s, z %s" % (len(BV), BV[:, 0].min().round(2), (BV[:, 1].min().round(2), BV[:, 1].max().round(2)),
    (BV[:, 2].min().round(2), BV[:, 2].max().round(2))))
# calotte : du dos du masque du visage à l'arrière de la couronne ; chaque sommet est ensuite ramené juste derrière la
# surface d'origine quand un rayon partant du centre la touche avant lui (la calotte n'apparaît qu'aux trous)
lo, hi = BV.min(0), BV.max(0)
YB = lo[1] - 0.05; YFR = float(os.environ.get("CAP_YFRONT", "2.9"))
CEN = np.array([0.0, (YB + YFR) / 2, (lo[2] + hi[2]) / 2 + float(os.environ.get("CAP_DZ", "-0.1"))])
RAD = np.array([np.abs(BV[:, 0]).max() + 0.02, (YFR - YB) / 2, (hi[2] - lo[2]) / 2 + float(os.environ.get("CAP_MZ", "0.3"))])
log("calotte : centre %s, demi-axes %s (ajustée sur %d sommets de bord)" % (CEN.round(2), RAD.round(2), len(BV)))
RAD_C = RAD * float(os.environ.get("CAP_K", "0.97"))
ret_ = bmesh.ops.create_uvsphere(bm, u_segments=32, v_segments=18, radius=1.0)
cv = ret_["verts"]
for v in cv:
    v.co = Vector((CEN + np.array(v.co[:]) * RAD_C).tolist())
pvb = BVHTree.FromBMesh(bm)   # contient aussi la sphère : on filtre ses faces par index
nsrc = len([f for f in bm.faces]) - len({f for v in cv for f in v.link_faces})
bm.faces.ensure_lookup_table()
capset = {f.index for v in cv for f in v.link_faces}
pulled = 0
Cv = Vector(CEN.tolist())
for v in cv:
    d_ = v.co - Cv; L_ = d_.length; dn = d_ / L_
    o_ = Cv.copy(); best = None
    for _ in range(8):                                   # premier impact hors calotte le long du rayon
        hit_ = pvb.ray_cast(o_, dn, L_ + 0.05 - (o_ - Cv).length)
        if hit_[0] is None: break
        if hit_[2] not in capset:
            best = (hit_[0] - Cv).length; break
        o_ = hit_[0] + dn * 1e-4
    if best is not None and best - 0.03 < L_:
        v.co = Cv + dn * max(best - 0.03, 0.05); pulled += 1
log("calotte : %d sommets ramenés derrière la surface d'origine" % pulled)
capf = list({f for v in cv for f in v.link_faces})
# sous la couture du cou : inutile
low = [f for f in capf if (f.calc_center_median() - P).dot(N) < 0.05]
bmesh.ops.delete(bm, geom=low, context="FACES")
bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context="VERTS")
cvs = {v for v in cv if v.is_valid}
uvL = bm.loops.layers.uv.active
# UV constante : celle du sommet de bord le plus haut à l'arrière (peau du crâne), pas d'étirement de texture
rim = [v for e in bm.edges if e.is_boundary and e not in skip for v in e.verts if v not in cvs and ((v.co - P).dot(N)) > 0.3]
bc = bpy.data.images.load(os.path.join(TEXD, "T_Traqueur_BaseColor.png"), check_existing=True)
bw, bh = bc.size; bpx = np.array(bc.pixels[:], dtype=np.float32).reshape(bh, bw, 4)[..., :3]
cand = [l[uvL].uv.copy() for v in rim for l in v.link_loops]
cols = np.array([bpx[min(int(u.y % 1 * bh), bh - 1), min(int(u.x % 1 * bw), bw - 1)] for u in cand])
med = np.median(cols, 0); uv0 = cand[int(np.argmin(np.linalg.norm(cols - med, axis=1)))]
log("calotte : couleur médiane de la peau autour de la couronne %s" % med.round(3))
ncap = 0
for f in bm.faces:
    if all(v in cvs for v in f.verts):
        ncap += 1
        for l in f.loops: l[uvL].uv = uv0
log("calotte : %d faces, UV %s" % (ncap, tuple(round(x, 3) for x in uv0)))
bm.faces.ensure_lookup_table()
bnd = [e for e in bm.edges if e.is_boundary]
ring = [e for e in bnd if all(abs((v.co - P).dot(N)) < 1e-3 for v in e.verts)]
other = [e for e in bnd if e not in set(ring)]
log("bords de la pièce : %d au plan du cou, %d ailleurs (trous de la source)" % (len(ring), len(other)))
# jupe sous la couture du cou (comme stage3)
ext = bmesh.ops.extrude_edge_only(bm, edges=ring)
for v in [g for g in ext["geom"] if isinstance(g, bmesh.types.BMVert)]:
    radial = (v.co - P) - N * (v.co - P).dot(N)
    v.co = v.co - N * 0.06 - radial.normalized() * 0.04
bm.to_mesh(pm); bm.free()
pm.uv_layers.active.name = me.uv_layers[0].name
for l in list(pm.uv_layers)[1:]:
    pm.uv_layers.remove(l)
for a in list(pm.attributes):
    if a.name not in me.attributes and not a.name.startswith(".") and a.name not in ("position",) and a.domain != "CORNER":
        try: pm.attributes.remove(a)
        except Exception: pass
pm.materials.clear()
for m in me.materials:
    pm.materials.append(m)
for p in pm.polygons:                      # après materials.clear() (qui remet les index à 0)
    p.material_index = 2; p.use_smooth = True
piece = bpy.data.objects.new("_tete", pm); sc.collection.objects.link(piece)
# orientation : faces vers l'extérieur du crâne (contrôle)
pc = np.array([p.center[:] for p in pm.polygons]); pn = np.array([p.normal[:] for p in pm.polygons])
ctr = pc.mean(0); out_frac = float(((pn * (pc - ctr)).sum(1) > 0).mean())
log("pièce : %d faces, %d triangles, %.0f %% des faces tournées vers l'extérieur" % (
    len(pm.polygons), sum(len(p.vertices) - 2 for p in pm.polygons), 100 * out_frac))

# --- 2. retrait de l'ancienne tête (îlots côté tête) et des fragments de la retopologie
bm = bmesh.new(); bm.from_mesh(me); bm.faces.ensure_lookup_table()
seen = set(); kill = []; n_isl = 0
for f0 in bm.faces:
    if f0.index in seen:
        continue
    isl = []; stack = [f0]; seen.add(f0.index)
    while stack:
        f = stack.pop(); isl.append(f)
        for e in f.edges:
            for g in e.link_faces:
                if g.index not in seen:
                    seen.add(g.index); stack.append(g)
    c = np.array([f.calc_center_median()[:] for f in isl])
    side = (c - Pn) @ Nn
    if side.min() > -0.15 and c[:, 2].min() > Pn[2] - 1.0 and np.linalg.norm(c - Pn, axis=1).max() < 6.0:
        kill += isl; n_isl += 1
pb = BVHTree.FromPolygons([v.co[:] for v in pm.vertices], [p.vertices[:] for p in pm.polygons])
isl_set = set(kill); dbl = []
for f in bm.faces:
    if f in isl_set: continue
    c = f.calc_center_median()
    if (c - P).dot(N) > 0 and pb.find_nearest(c)[3] < 0.12:
        dbl.append(f)
log("faces du corps qui doublent la tête (mâchoire en voxels) : %d (matériaux %s)" % (len(dbl), np.bincount([f.material_index for f in dbl], minlength=3).tolist()))
kill += dbl
log("ancienne tête : %d îlots, %d faces retirées (matériaux %s)" % (n_isl, len(kill),
    np.bincount([f.material_index for f in kill], minlength=3).tolist()))
bmesh.ops.delete(bm, geom=kill, context="FACES")
bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context="VERTS")
bm.to_mesh(me); bm.free()

# --- 4. poids : point le plus proche du maillage précédent
dg = bpy.context.evaluated_depsgraph_get()
om = old.data; bvh = BVHTree.FromPolygons([v.co[:] for v in om.vertices], [p.vertices[:] for p in om.polygons])
W = np.zeros((len(om.vertices), len(GN)))
for v in om.vertices:
    for g in v.groups:
        W[v.index, g.group] = g.weight
for n in GN:
    piece.vertex_groups.new(name=n)
for v in pm.vertices:
    loc, nrm, fi, dist = bvh.find_nearest(v.co)
    vids = om.polygons[fi].vertices
    pts = np.array([om.vertices[i].co[:] for i in vids]); wd = 1.0 / (np.linalg.norm(pts - np.array(loc[:]), axis=1) + 1e-4)
    w = (W[list(vids)] * wd[:, None]).sum(0) / wd.sum()
    top = np.argsort(-w)[:4]; w4 = np.zeros_like(w); w4[top] = w[top]; w4 /= max(w4.sum(), 1e-9)
    for j in top:
        if w4[j] > 1e-4:
            piece.vertex_groups[GN[j]].add([v.index], float(w4[j]), "REPLACE")

# --- 5. réunion et normales
for o in vl.objects: o.select_set(False)
ob.select_set(True); piece.select_set(True); vl.objects.active = ob
with bpy.context.temp_override(active_object=ob, selected_editable_objects=[ob, piece], selected_objects=[ob, piece]):
    bpy.ops.object.join()
me = ob.data
nco = np.zeros(len(me.vertices) * 3); me.vertices.foreach_get("co", nco); nco = nco.reshape(-1, 3)
vn = np.array([v.normal[:] for v in me.vertices])
bvs = BVHTree.FromPolygons([v.co[:] for v in src.data.vertices], [p.vertices[:] for p in src.data.polygons])
bm = bmesh.new(); bm.from_mesh(me); bm.verts.ensure_lookup_table()
seam = set()
for e in bm.edges:
    if e.is_boundary or (len(e.link_faces) == 2 and e.link_faces[0].material_index != e.link_faces[1].material_index):
        seam |= {v.index for v in e.verts}
nring = set(i for i in seam if ((nco[i] - Pn) @ Nn) > -0.5 and np.linalg.norm(nco[i] - Pn) < 6)
for _ in range(2):
    nring |= {w.index for i in list(nring) for e in bm.verts[i].link_edges for w in e.verts}
bm.free()
kept = 0
for i, p in enumerate(nco):
    k = key(p)
    if i in nring:
        loc, nrm, fi, dist = bvs.find_nearest(Vector(p))
        if loc is not None and dist < 0.3:
            vn[i] = nrm[:]
    elif k in NORM:
        vn[i] = NORM[k]; kept += 1
vn /= np.linalg.norm(vn, axis=1, keepdims=True)
me.normals_split_custom_set_from_vertices([tuple(x) for x in vn])
log("normales : %d sommets repris d'avant, %d autour de la couture du cou depuis la source" % (kept, len(nring)))

# --- 6. contrôles de la tête
gi = ob.vertex_groups["Tete"].index
bm = bmesh.new(); bm.from_mesh(me); dl = bm.verts.layers.deform.active
headf = [f for f in bm.faces if f.material_index == 2 and ((f.calc_center_median() - P).dot(N)) > 0]
hb = {e for f in headf for e in f.edges if e.is_boundary}
bb = [e for e in bm.edges if e.is_boundary and all(f.material_index == 0 for f in e.link_faces)
      and all(((v.co - P).dot(N)) > -0.3 and (v.co - P).length < 6 for v in e.verts)]
log("bords ouverts du corps près du cou : %d" % len(bb))
log("tête : %d faces, %d arêtes de bord côté tête (hors jupe)" % (len(headf), sum(1 for e in hb
    if all(((v.co - P).dot(N)) > 0.0 for v in e.verts))))
bm.free()

# --- 7. pupilles : disques émissifs dans T_Traqueur_Emission
img = bpy.data.images.load(os.path.join(TEXD, "T_Traqueur_Emission.png"), check_existing=True)
w_, h_ = img.size; px = np.array(img.pixels[:], dtype=np.float32).reshape(h_, w_, 4)
lum = px[..., :3].max(2)
uvl = me.uv_layers[0].data
mi = np.zeros(len(me.polygons), int); me.polygons.foreach_get("material_index", mi)
hit = []
for p in me.polygons:
    if mi[p.index] != 2: continue
    if any(lum[min(int(uvl[li].uv[1] % 1 * h_), h_ - 1), min(int(uvl[li].uv[0] % 1 * w_), w_ - 1)] > 0.3 for li in p.loop_indices):
        hit.append(np.array(p.center[:]))
hit = np.array(hit)
log("faces de la tête sur le masque des pupilles (avant peinture) : %d" % len(hit))
eyes = []
for sgn in (1, -1):
    hs = hit[hit[:, 0] * sgn > 0]
    eyes.append(hs.mean(0)); log("œil %s : centre %s (%d faces)" % ("G" if sgn > 0 else "D", hs.mean(0).round(3), len(hs)))
# couleur peinte : celle déjà utilisée par le masque (médiane des texels allumés)
on = px[lum > 0.3][:, :3]; col = np.median(on, 0) if len(on) else np.array([1, 1, 1]); col = col / max(col.max(), 1e-6)
log("couleur du masque : %s" % col.round(3))
me.calc_loop_triangles()
painted = 0
for tri in me.loop_triangles:
    if mi[tri.polygon_index] != 2: continue
    V = np.array([me.vertices[i].co[:] for i in tri.vertices])
    if min(np.linalg.norm(V.mean(0) - e) for e in eyes) > PUPIL_R + 0.5: continue
    UV = np.array([uvl[l].uv[:] for l in tri.loops]) * [w_, h_]
    x0, y0 = np.floor(UV.min(0)).astype(int); x1, y1 = np.ceil(UV.max(0)).astype(int)
    xs, ys = np.meshgrid(np.arange(x0, x1 + 1), np.arange(y0, y1 + 1)); Q = np.stack([xs.ravel() + 0.5, ys.ravel() + 0.5], 1)
    T_ = np.array([[UV[1, 0] - UV[0, 0], UV[2, 0] - UV[0, 0]], [UV[1, 1] - UV[0, 1], UV[2, 1] - UV[0, 1]]])
    if abs(np.linalg.det(T_)) < 1e-9: continue
    l12 = np.linalg.solve(T_, (Q - UV[0]).T).T; l0 = 1 - l12.sum(1)
    inside = (l0 > -0.02) & (l12[:, 0] > -0.02) & (l12[:, 1] > -0.02)
    if not inside.any(): continue
    Bc = np.stack([l0, l12[:, 0], l12[:, 1]], 1)[inside]; Pw = Bc @ V; q = Q[inside].astype(int)
    for e in eyes:
        dd = np.linalg.norm(Pw - e, axis=1)
        a_ = np.clip((PUPIL_R - dd) / (0.25 * PUPIL_R) + 1.0, 0, 1)        # bord adouci sur un quart du rayon
        m = a_ > 0
        if not m.any(): continue
        yy = np.clip(q[m, 1], 0, h_ - 1); xx = np.clip(q[m, 0], 0, w_ - 1)
        cur = px[yy, xx, :3]; new = np.maximum(cur, col[None, :] * a_[m, None])
        px[yy, xx, :3] = new; painted += int(m.sum())
log("texels peints : %d (rayon %.2f m)" % (painted, PUPIL_R))
# contrôle : faces sur le masque après peinture, et leur éloignement aux yeux
lum2 = px[..., :3].max(2); far = 0; nh = 0
for p in me.polygons:
    if mi[p.index] == 0: continue
    if any(lum2[min(int(uvl[li].uv[1] % 1 * h_), h_ - 1), min(int(uvl[li].uv[0] % 1 * w_), w_ - 1)] > 0.3 for li in p.loop_indices):
        nh += 1; c = np.array(p.center[:])
        if min(np.linalg.norm(c - e) for e in eyes) > PUPIL_R + 0.3: far += 1
log("faces sur le masque après peinture : %d, dont %d loin des yeux (UV partagées)" % (nh, far))
img.pixels.foreach_set(px.ravel())
img.filepath_raw = os.path.join(OUT if not EXPORT else TEXD, "T_Traqueur_Emission.png"); img.file_format = "PNG"; img.save()

# --- 8. ménage, export
for n in ("_old", "_tete", "_src", "_s3"):
    o = bpy.data.objects.get(n)
    if o: bpy.data.objects.remove(o, do_unlink=True)
ob.data.update()
log("total : %d triangles corps, %d os" % (K.C.tri_count(ob), len(arm.data.bones)))
json.dump({"rapport": REP, "yeux": [e.tolist() for e in eyes]}, open(os.path.join(OUT, "tete_fix.json"), "w", encoding="utf-8"),
          ensure_ascii=False, indent=1)
if EXPORT:
    arm.animation_data.action = bpy.data.actions["Traqueur_Repos"]; sc.frame_set(1)
    K.export([arm, ob, hair], os.path.join(ROOT, "Assets", "Art", "Models", "Ennemis", "SK_Traqueur.fbx"))
    acts = [a for a in bpy.data.actions if a.name.startswith("Traqueur_")]
    blocks = {sc} | set(sc.objects) | {o.data for o in sc.objects if o.data is not None} | set(acts)
    sc.use_fake_user = True
    bpy.data.libraries.write(os.path.join(ROOT, "Assets", "Art", "Source~", "Ennemis", "SK_Traqueur.blend"), blocks,
                             path_remap="RELATIVE_ALL", fake_user=True, compress=True)
    log("exporté : %d clips" % len(acts))
else:
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "tete_fix.blend"))
