"""Étape 4 : poids par régions (liaison debout, bras écartés), normales des coutures, mèches, clips bornés sur la base
voûtée (marche et passage bas : pieds verrouillés par IK à 2 os), mesures et test d'étirement ×3.8 par l'échelle."""
import sys, os, json, math, time
sys.path.insert(0, r"C:\Users\PC\ItHunts\ItHunts-art\Assets\Art\Source~\Ennemis\scripts")
for m in ("sk_traqueur", "sm_traqueur", "peinture", "ennemis_common", "poids_lisses"):
    sys.modules.pop(m, None)
import sk_traqueur as K
import poids_lisses as PL
import numpy as np
import bpy, bmesh
from mathutils import Vector, Quaternion, Matrix, kdtree
from mathutils.bvhtree import BVHTree
OUT = r"C:\Users\PC\AppData\Local\Temp\claude\fa13b7ae4f76688a\5c2bc682-360e-47d5-a52c-698998cd4727\scratchpad\traq2"
sc = bpy.context.scene; vl = bpy.context.view_layer
ob = bpy.data.objects["SK_Traqueur"]; arm = bpy.data.objects["Traqueur"]; src = bpy.data.objects["Traq_Source"]
FPS = K.FPS
REP = []
def log(s):
    print(s); REP.append(s)
t0 = time.time()

# ------------------------------------------------------------------ 1. poids
for md in list(ob.modifiers): ob.modifiers.remove(md)
ob.vertex_groups.clear(); ob.parent = None
for pb in arm.pose.bones:
    pb.rotation_mode = "QUATERNION"; pb.rotation_quaternion = Quaternion(); pb.location = (0, 0, 0); pb.scale = (1, 1, 1)
vl.update()
with bpy.data.libraries.load(os.path.join(OUT, "stage2.blend")) as (src_lib, dst_lib):
    dst_lib.objects = ["Traq_Retopo"]
prox = dst_lib.objects[0]; prox.name = "_proxy_chaleur"; sc.collection.objects.link(prox)
prox.vertex_groups.clear()
for md in list(prox.modifiers): prox.modifiers.remove(md)
sys.path.insert(0, OUT)
sys.modules.pop("refit", None)
import refit as RF
outside = RF.refit(arm, prox)
log("os recalés sur le maillage ; encore en partie dehors : %s" % (", ".join(outside) or "aucun"))
names = [b.name for b in arm.data.bones if b.name != "Racine"]
idx = {n: i for i, n in enumerate(names)}
co = np.array([v.co[:] for v in ob.data.vertices])
segs = {n: (np.array(arm.data.bones[n].head_local[:]), np.array(arm.data.bones[n].tail_local[:])) for n in names}
D = np.stack([PL.seg_dist(co, *segs[n]) for n in names], 1)
nearest = np.array(names)[np.argmin(D, 1)]
ARM = {s: ["Bras." + s, "AvantBras." + s, "Main." + s, "Doigts_1." + s, "Doigts_2." + s, "Pouce_1." + s, "Pouce_2." + s] for s in "LR"}
LEG = {s: ["Cuisse." + s, "Jambe." + s, "Pied." + s, "Orteils." + s] for s in "LR"}
TORSO = ["Bassin", "Dos_1", "Dos_2", "Poitrine", "Cou", "Epaule.L", "Epaule.R"]
HEAD = ["Tete", "Machoire"]
def along(n, P):
    a, b = segs[n]; ab = b - a
    return ((P - a) @ ab) / float(ab @ ab)
allowed = np.zeros((len(co), len(names)), bool)
region = np.full(len(co), "torse", dtype=object)
for s in "LR":
    m = np.isin(nearest, ARM[s] + ["Epaule." + s]) & (np.abs(co[:, 0]) > 2.2)     # bras (épaule comprise)
    ta = along("Bras." + s, co)
    shoulder = m & (ta < 0.22)
    region[m] = "bras" + s
    for n in ARM[s]: allowed[m, idx[n]] = True
    allowed[shoulder, idx["Epaule." + s]] = True; allowed[shoulder, idx["Poitrine"]] = True
    m = np.isin(nearest, LEG[s])
    tl = along("Cuisse." + s, co)
    hip = m & (tl < 0.18)
    region[m] = "jambe" + s
    for n in LEG[s]: allowed[m, idx[n]] = True
    allowed[hip, idx["Bassin"]] = True
mh = np.isin(nearest, HEAD) | ((nearest == "Cou") & (co[:, 2] > 17.3))
region[mh] = "tete"
allowed[mh, idx["Tete"]] = True; allowed[mh, idx["Cou"]] = True
jaw = mh & (co[:, 1] > 1.91) & (co[:, 2] < 18.04)
allowed[jaw, idx["Machoire"]] = True
mt = region == "torse"
for n in TORSO: allowed[mt, idx[n]] = True
for s in "LR":                       # haut du torse : l'épaule suit un peu le bras ; bassin : un peu la cuisse
    allowed[mt & (np.linalg.norm(co - segs["Bras." + s][0], axis=1) < 1.3), idx["Bras." + s]] = True
    allowed[mt & (np.linalg.norm(co - segs["Cuisse." + s][0], axis=1) < 1.4), idx["Cuisse." + s]] = True
log("régions : " + ", ".join("%s %d" % (r, int((region == r).sum())) for r in sorted(set(region))))
# Mélange seulement du côté du parent à chaque articulation (étirement par l'échelle : un enfant qui n'hérite pas de
# l'échelle, comme la main, ne doit porter aucun poids de son parent étiré) ; Bras seulement au-delà de l'épaule.
for s_ in "LR":
    behind = along("Bras." + s_, co) < 0.02
    allowed[behind, idx["Bras." + s_]] = False
    allowed[behind & (region == "bras" + s_), idx["Epaule." + s_]] = True
    past_wrist = along("AvantBras." + s_, co) > 1.0
    allowed[past_wrist & (region == "bras" + s_), idx["AvantBras." + s_]] = False
    for par, kid in (("Main", "Doigts_1"), ("Doigts_1", "Doigts_2"), ("Main", "Pouce_1"), ("Pouce_1", "Pouce_2")):
        pass
# 1) chaque sommet sur l'os permis le plus proche ; 2) diffusion le long des arêtes (transitions aux articulations)
Dm = np.where(allowed, D, np.inf)
W = np.zeros((len(co), len(names)), np.float32)
W[np.arange(len(co)), np.argmin(Dm, 1)] = 1.0
edges_ = np.array([e.vertices[:] for e in ob.data.edges], np.int64)
IT = int(os.environ.get("DIFF", "40"))
W = PL.smooth(W, edges_, IT, 0.5, allowed)
# coutures : les deux côtés (pièces séparées) reçoivent la moyenne de leurs poids
kd = kdtree.KDTree(len(co))
for i, c in enumerate(co): kd.insert(c, i)
kd.balance()
bmq = bmesh.new(); bmq.from_mesh(ob.data)
bverts = [v.index for v in bmq.verts if any(e.is_boundary for e in v.link_edges)]
bmq.free()
W2 = W.copy(); nseam = 0
for i in bverts:
    near = [j for (_, j, d) in kd.find_range(co[i], 0.12) if j != i]
    if near:
        W2[i] = (W[i] + W[near].mean(0)) * 0.5; nseam += 1
W = W2 / np.maximum(W2.sum(1, keepdims=True), 1e-9)
W = PL.prune(W)
PL.write_groups(ob, arm, names, W)
log("poids par diffusion : os le plus proche puis %d passes le long des arêtes ; %d sommets de couture moyennés" % (IT, nseam))
# contrôle : aucun sommet du bras (au-delà de l'épaule) tiré par un os hors du bras
for s in "LR":
    m = (region == "bras" + s) & (along("Bras." + s, co) >= 0.22)
    out = [n for n in names if n not in ARM[s]]
    log("bras %s : %d sommets, poids hors du bras max %.4f" % (s, int(m.sum()), float(W[m][:, [idx[n] for n in out]].sum(1).max()) if m.any() else 0))
far = 0
for j, n in enumerate(names):
    w = W[:, j] > 0.05
    far += int((w & (D[:, j] > 2.5)).sum())
log("sommets tirés (> 5 %%) par un os à plus de 2.5 m : %d (avant : 1079)" % far)

# ------------------------------------------------------------------ 2. normales des coutures : reprises de la surface d'origine
me = ob.data
dg = vl.depsgraph if hasattr(vl, "depsgraph") else bpy.context.evaluated_depsgraph_get()
bvh = BVHTree.FromObject(src, bpy.context.evaluated_depsgraph_get())
bm = bmesh.new(); bm.from_mesh(me); bm.verts.ensure_lookup_table()
seam = set()
for e in bm.edges:
    if e.is_boundary or (len(e.link_faces) == 2 and e.link_faces[0].material_index != e.link_faces[1].material_index):
        for v in e.verts: seam.add(v.index)
# anneau de 2 voisins autour des coutures
ring = set(seam)
for _ in range(2):
    ring |= {w.index for i in list(ring) for e in bm.verts[i].link_edges for w in e.verts}
bm.free()
me.calc_loop_triangles()
vn = np.array([v.normal[:] for v in me.vertices])
for i in ring:
    loc, nrm, fi, dist = bvh.find_nearest(me.vertices[i].co)
    if loc is not None and dist < 0.3:
        # normale lissée de la source au point : moyenne pondérée des normales de sommets du triangle
        vn[i] = np.array(nrm[:])
vn /= np.linalg.norm(vn, axis=1, keepdims=True)
me.normals_split_custom_set_from_vertices([tuple(x) for x in vn])
log("normales reprises de la source sur %d sommets autour des coutures" % len(ring))

# ------------------------------------------------------------------ 3. base voûtée, sol
def reset():
    for pb in arm.pose.bones:
        pb.rotation_mode = "QUATERNION"; pb.rotation_quaternion = Quaternion(); pb.location = (0, 0, 0); pb.scale = (1, 1, 1)
reset()
K.pose_hunch(arm, K.HUNCH)
vl.update()
BASE = {pb.name: pb.rotation_quaternion.copy() for pb in arm.pose.bones}
RH = {pb.name: pb.matrix.to_quaternion() for pb in arm.pose.bones}
dgr = bpy.context.evaluated_depsgraph_get()
mev = ob.evaluated_get(dgr).to_mesh(); zmin = min(v.co.z for v in mev.vertices); ob.evaluated_get(dgr).to_mesh_clear()
GROUND = -zmin
log("base voûtée : pieds ramenés au sol de %.2f m" % GROUND)
rb = arm.pose.bones["Racine"]; RR = rb.bone.matrix_local.to_quaternion()

def apply_clip(spec, root=(0, 0, 0)):
    for pb in arm.pose.bones:
        pb.rotation_quaternion = BASE[pb.name].copy(); pb.location = (0, 0, 0); pb.scale = (1, 1, 1)
    for name, rots in spec.items():
        pb = arm.pose.bones[name]
        q = Quaternion()
        for ax, ang in rots:
            q = Quaternion(K.AXES[ax], math.radians(ang)) @ q
        R = RH[name]
        pb.rotation_quaternion = BASE[name] @ (R.inverted() @ q @ R)
    rb.location = RR.inverted() @ (Vector((-root[0], -root[1], root[2] + GROUND)))
    vl.update()

apply_clip({})
P0 = {pb.name: (arm.matrix_world @ pb.head).copy() for pb in arm.pose.bones}
measure = {"épaule": P0["Bras.R"].z, "genou": P0["Jambe.L"].z, "bout des doigts": (arm.matrix_world @ arm.pose.bones["Doigts_2.L"].tail).z,
           "sommet du crâne": (arm.matrix_world @ arm.pose.bones["Tete"].tail).z}
log("repos voûté : " + ", ".join("%s %.2f m" % kv for kv in measure.items()))

# ------------------------------------------------------------------ 4. marche : pieds verrouillés (IK analytique à 2 os)
def ik_leg(s, target, fwd=Vector((0, 1, 0))):
    c = arm.pose.bones["Cuisse." + s]; j = arm.pose.bones["Jambe." + s]
    H = arm.matrix_world @ c.head
    l1 = (c.tail - c.head).length; l2 = (j.tail - j.head).length
    d = target - H; dist = min(d.length, (l1 + l2) * 0.999); dn = d.normalized()
    cosA = (l1 * l1 + dist * dist - l2 * l2) / (2 * l1 * dist)
    A = math.acos(max(-1, min(1, cosA)))
    side = dn.cross(fwd).normalized()                       # axe du genou
    knee_dir = Quaternion(side, -A) @ dn                    # genou vers l'avant
    if knee_dir.dot(fwd) < (Quaternion(side, A) @ dn).dot(fwd):
        knee_dir = Quaternion(side, A) @ dn
    Kp = H + knee_dir * l1
    K.aim(arm, "Cuisse." + s, Kp - H)
    K.aim(arm, "Jambe." + s, (H + dn * dist) - (arm.matrix_world @ arm.pose.bones["Jambe." + s].head))
    for bone, key in (("Pied", "pied"), ("Orteils", "orteils")):
        t = K.HUNCH[key]; sx = -1 if s == "L" else 1
        K.aim(arm, bone + "." + s, (t[0] * sx, t[1], t[2]))

FOOT0 = {s: P0["Pied." + s].copy() for s in "LR"}           # cheville au sol, pose de base

def walk_frame(t, T_, L, duty, lift, deep=0.0):
    ph = 2 * math.pi * t / T_
    s = {}
    for side, o in (("L", 0.0), ("R", math.pi)):
        p = ph + o
        add_ = s.setdefault
        s.setdefault("Bras." + side, []).append(("x", 10 * math.sin(p) - 10 * deep))
        s.setdefault("AvantBras." + side, []).append(("x", -6 - 5 * max(0.0, math.sin(p))))
    s.setdefault("Bassin", []).extend([("y", 3 * math.sin(ph)), ("z", 4 * math.sin(ph)), ("x", 25 * deep)])
    s.setdefault("Dos_1", []).append(("x", 12 * deep)); s.setdefault("Dos_2", []).append(("x", 8 * deep))
    s.setdefault("Poitrine", []).append(("z", -4 * math.sin(ph))); s.setdefault("Cou", []).extend([("z", 3 * math.sin(ph)), ("x", -18 * deep)])
    s.setdefault("Tete", []).append(("x", -14 * deep)); s.setdefault("Machoire", []).append(("z", 3 * math.sin(ph)))
    bob = 0.18 * math.cos(2 * ph) - 0.1 - 4.2 * deep
    apply_clip(s, (0, 0, bob))
    feet = {}
    for side, o in (("L", 0.0), ("R", 0.5)):
        u = (t / T_ + o) % 1.0
        F = FOOT0[side].copy()
        if u < duty:
            k = u / duty; F.y += L / 2 - L * k
        else:
            k = (u - duty) / (1 - duty); kk = K.ss(k)
            F.y += -L / 2 + L * kk; F.z += lift * math.sin(math.pi * k)
        ik_leg(side, F)
        feet[side] = F
    return feet

def a_marche_ik(t):
    return walk_frame(t, 2.0, 4.6, 0.62, 0.9)

def a_passage_ik(t):
    return walk_frame(t, 2.4, 3.4, 0.62, 0.6, deep=K.PASSAGE)

# ------------------------------------------------------------------ 5. clips bornés
def spec_add(s, b, ax, a): s.setdefault(b, []).append((ax, a))
def both(s, n, ax, a, mirror=1): spec_add(s, n + ".L", ax, a); spec_add(s, n + ".R", ax, a * (mirror if ax in ("y", "z") else 1))
def lerp_spec(a, b, u):
    out = {}
    for bone in set(a) | set(b):
        ra, rb_ = {}, {}
        for ax, v in a.get(bone, []): ra[ax] = ra.get(ax, 0) + v
        for ax, v in b.get(bone, []): rb_[ax] = rb_.get(ax, 0) + v
        out[bone] = [(ax, ra.get(ax, 0) * (1 - u) + rb_.get(ax, 0) * u) for ax in ("y", "z", "x")]
    return out
def blend(pa, pb, u):
    return lerp_spec(pa[0], pb[0], u), tuple(pa[1][i] * (1 - u) + pb[1][i] * u for i in range(3))
ss = K.ss

def a_repos(t):
    ph = 2 * math.pi * t / 3.0; s = {}
    spec_add(s, "Poitrine", "x", 2.0 * math.sin(ph)); spec_add(s, "Dos_2", "x", 1.0 * math.sin(ph + 0.3))
    spec_add(s, "Cou", "z", 6 * math.sin(ph)); spec_add(s, "Tete", "y", 3 * math.sin(ph + 1.0))
    both(s, "Bras", "x", 2.5 * math.sin(ph + 0.6)); both(s, "Doigts_1", "x", 5 * math.sin(2 * ph))
    spec_add(s, "Machoire", "z", 3 * math.sin(ph))
    return s, (0, 0, 0.08 * math.sin(ph))

CURL = float(os.environ.get("CURL", "1"))          # signe de la fermeture des doigts (vérifié par la mesure)
def reach_spec(close):
    s = {}
    spec_add(s, "Poitrine", "z", 10); spec_add(s, "Poitrine", "x", 6); spec_add(s, "Machoire", "x", 10)
    spec_add(s, "Doigts_1.R", "x", CURL * (-12 + 62 * close)); spec_add(s, "Doigts_2.R", "x", CURL * (-8 + 50 * close))
    spec_add(s, "Pouce_1.R", "x", CURL * 35 * close); spec_add(s, "Pouce_2.R", "x", CURL * 28 * close)
    return s
def a_prise(t):
    u = ss(t / 0.32)
    if t < 0.32:
        s, r = blend(({}, (0, 0, 0)), (reach_spec(0.0), (0, 0, -0.2)), u)
    else:
        s, r = blend((reach_spec(0.0), (0, 0, -0.2)), (reach_spec(1.0), (0, 0, -0.2)), ss((t - 0.32) / 0.18))
    s["_aim"] = (K.REACH, u)
    return s, r

def a_mains_levees(t):
    # bras visés (rotation la plus courte : pas de torsion), clavicule et torse prennent leur part ; épaule ≤ 110°
    u = ss(t / 0.2)
    up = {}
    both(up, "Epaule", "x", -22); both(up, "Main", "x", -10); both(up, "Doigts_1", "x", -15); both(up, "Doigts_2", "x", -10)
    spec_add(up, "Poitrine", "x", -14); spec_add(up, "Dos_2", "x", -8); spec_add(up, "Cou", "x", -8); spec_add(up, "Machoire", "x", 22)
    s_, r_ = blend(({}, (0, 0, 0)), (up, (0, 0, 0.3)), u)
    s_["_aim"] = ({"Bras.L": (-0.45, 0.30, 1.0), "Bras.R": (0.45, 0.30, 1.0),
                   "AvantBras.L": (-0.25, 0.45, 1.0), "AvantBras.R": (0.25, 0.45, 1.0)}, u)
    return s_, r_

def plant_spec():
    s = {}
    spec_add(s, "Bras.R", "x", -40); spec_add(s, "Bras.R", "y", 8); spec_add(s, "AvantBras.R", "x", 20); spec_add(s, "Main.R", "x", -30)
    spec_add(s, "Doigts_1.R", "x", -20); spec_add(s, "Doigts_2.R", "x", -10)
    spec_add(s, "Poitrine", "x", 14); spec_add(s, "Dos_2", "x", 8); spec_add(s, "Poitrine", "z", 8); spec_add(s, "Cou", "x", -12)
    both(s, "Cuisse", "x", -12); both(s, "Jambe", "x", 18); spec_add(s, "Machoire", "x", 16)
    return s, (0, 0, -0.9)
def a_main_plantee(t):
    if t < 0.2:
        return blend(({}, (0, 0, 0)), plant_spec(), ss(t / 0.2))
    if t < 1.7:
        s, r = plant_spec(); spec_add(s, "Poitrine", "x", 1.5 * math.sin(2 * math.pi * (t - 0.2) / 1.5)); return s, r
    return blend(plant_spec(), ({}, (0, 0, 0)), ss((t - 1.7) / 0.3))

def a_revers(t):
    # bras gauche visé : du travers du corps (devant, vers la droite) à l'extérieur (gauche) ; le torse tourne avec (±18°)
    u = ss(t / 0.2)
    a = {}; spec_add(a, "Poitrine", "z", -18); spec_add(a, "Dos_2", "z", -8)
    b = {}; spec_add(b, "Poitrine", "z", 18); spec_add(b, "Dos_2", "z", 8); spec_add(b, "Machoire", "x", 14); both(b, "Doigts_1", "x", -15)
    s_, r_ = blend((a, (0, 0, 0)), (b, (0, 0, 0)), u)
    da = Vector((0.55, 0.75, -0.35)).normalized(); db = Vector((-0.95, 0.25, -0.15)).normalized()
    fa = Vector((0.65, 0.70, -0.30)).normalized(); fb = Vector((-0.90, 0.40, -0.20)).normalized()
    d = da.slerp(db, u) if hasattr(da, "slerp") else (da * (1 - u) + db * u).normalized()
    f = (fa * (1 - u) + fb * u).normalized()
    s_["_aim"] = ({"Bras.L": tuple(d), "AvantBras.L": tuple(f)}, 1.0)
    return s_, r_

def a_machoire(t):
    ph = 2 * math.pi * t / 2.4; s = {}
    spec_add(s, "Machoire", "z", 12 * math.sin(ph)); spec_add(s, "Machoire", "x", 9 + 8 * math.sin(2 * ph + 0.5))
    return s, (0, 0, 0)

ANIMS = [("Traqueur_Repos", a_repos, 3.0, True, None, False), ("Traqueur_Marche", a_marche_ik, 2.0, True, None, True),
         ("Traqueur_Prise", a_prise, 0.5, False, None, False), ("Traqueur_MainsLevees", a_mains_levees, 0.2, False, None, False),
         ("Traqueur_MainPlantee", a_main_plantee, 2.0, False, None, False), ("Traqueur_Revers", a_revers, 0.2, False, None, False),
         ("Traqueur_PassageBas", a_passage_ik, 2.4, True, None, True), ("Traqueur_Machoire", a_machoire, 2.4, True, {"Machoire"}, False)]

def pose_frame(fn, t, walk):
    if walk:
        fn(t); return
    spec, root = fn(t)
    aim_t = spec.pop("_aim", None)
    apply_clip(spec, root)
    if aim_t:
        targets, u = aim_t
        for bn, d in targets.items():
            pb = arm.pose.bones[bn]
            cur = (pb.matrix.to_3x3() @ Vector((0, 1, 0))).normalized()
            K.aim(arm, bn, cur.lerp(Vector(d).normalized(), u))

if arm.animation_data is None: arm.animation_data_create()
acts = []
for name, fn, dur, loop, only, walk in ANIMS:
    old = bpy.data.actions.get(name)
    if old: bpy.data.actions.remove(old)
    act = bpy.data.actions.new(name); arm.animation_data.action = act; act.use_fake_user = True
    n = max(2, int(round(dur * FPS)))
    for f in range(0, n + 1):
        pose_frame(fn, f / FPS if not loop or f < n else 0.0, walk)
        for pb in arm.pose.bones:
            if only and pb.name not in only: continue
            pb.keyframe_insert("rotation_quaternion", frame=1 + f); pb.keyframe_insert("location", frame=1 + f)
    acts.append(act)
log("clips : %d (%.0f s)" % (len(acts), time.time() - t0))

# ------------------------------------------------------------------ 6. mesures : angles, pieds, doigts
def bone_dirs():
    return {pb.name: (pb.matrix.to_3x3() @ Vector((0, 1, 0))).normalized() for pb in arm.pose.bones}
apply_clip({}); vl.update()
def rel_q(pb):
    q = pb.matrix.to_quaternion()
    return (pb.parent.matrix.to_quaternion().inverted() @ q) if pb.parent else q
QB = {pb.name: rel_q(pb) for pb in arm.pose.bones}
def twist(qa, qb, axis):
    d = qa.inverted() @ qb
    # torsion = rotation autour de l'axe de l'os (décomposition swing-twist)
    v = Vector((d.x, d.y, d.z)); a = Vector((0, 1, 0))
    p = a * v.dot(a); tw = Quaternion((d.w, p.x, p.y, p.z)); tw.normalize()
    return abs(math.degrees(2 * math.acos(min(1, abs(tw.w)))))
for name, fn, dur, loop, only, walk in ANIMS:
    act = bpy.data.actions[name]; arm.animation_data.action = act
    n = int(round(dur * FPS)); worst = {}
    foot = {"L": [], "R": []}
    for f in range(1, n + 2):
        sc.frame_set(f); vl.update()
        for b in ("Bras.L", "Bras.R", "AvantBras.L", "AvantBras.R"):
            pb = arm.pose.bones[b]
            dq = QB[b].inverted() @ rel_q(pb)
            ang = math.degrees(dq.angle); tw = twist(QB[b], rel_q(pb), None)
            w = worst.get(b, (0, 0)); worst[b] = (max(w[0], ang), max(w[1], tw))
        for s in "LR":
            foot[s].append((arm.matrix_world @ arm.pose.bones["Pied." + s].head).copy())
    top = 0.0
    for f in range(1, n + 2, 3):
        sc.frame_set(f); dgt = bpy.context.evaluated_depsgraph_get(); mm = ob.evaluated_get(dgt).to_mesh()
        top = max(top, max(v.co.z for v in mm.vertices)); ob.evaluated_get(dgt).to_mesh_clear()
    txt = name + " (sommet %.1f m) : " % top + ", ".join("%s %.0f° (torsion %.0f°)" % (b, a, t) for b, (a, t) in worst.items() if a > 3)
    if walk:
        for s in "LR":
            pts = foot[s]; zg = min(p.z for p in pts)
            st = [i for i in range(len(pts) - 1) if pts[i].z < zg + 0.02 and pts[i + 1].z < zg + 0.02]
            sp = [(pts[i + 1] - pts[i]).length * FPS for i in st]
            txt += " ; pied %s au sol %d %% du temps, vitesse au sol %.2f à %.2f m/s" % (s, 100 * len(st) // (len(pts) - 1), min(sp), max(sp))
    log(txt)
# doigts de la prise : le bout des doigts doit se rapprocher de la paume en fermant
act = bpy.data.actions["Traqueur_Prise"]; arm.animation_data.action = act
def tipdist(f):
    sc.frame_set(f); vl.update()
    return ((arm.matrix_world @ arm.pose.bones["Doigts_2.R"].tail) - (arm.matrix_world @ arm.pose.bones["Main.R"].head)).length
d0, d1 = tipdist(10), tipdist(16)
log("prise : bout des doigts -> poignet %.2f m (ouverte) -> %.2f m (fermée) : %s" % (d0, d1, "fermeture dans le bon sens" if d1 < d0 else "SENS INVERSÉ"))

# ------------------------------------------------------------------ 7. étirement ×k du bras droit par l'échelle (fin de la prise)
sc.frame_set(16); vl.update()
PM = {pb.name: pb.matrix.copy() for pb in arm.pose.bones}
arm.animation_data.action = None
for pb in arm.pose.bones:
    pb.matrix = PM[pb.name]; vl.update()
dirB = (arm.pose.bones["Bras.R"].matrix.to_3x3() @ Vector((0, 1, 0))).normalized()
K.aim(arm, "AvantBras.R", dirB); K.aim(arm, "Main.R", dirB); vl.update()
def arm_edges():
    dg2 = bpy.context.evaluated_depsgraph_get(); m = ob.evaluated_get(dg2).to_mesh()
    P = np.array([v.co[:] for v in m.vertices]); ob.evaluated_get(dg2).to_mesh_clear(); return P
E = np.array([e.vertices[:] for e in ob.data.edges])
armR = np.isin(region, ["brasR"])
sel = armR[E[:, 0]] & armR[E[:, 1]]
ja, jf, jm = idx["Bras.R"], idx["AvantBras.R"], idx["Main.R"]
handw = W[:, [idx[n] for n in ("Main.R", "Doigts_1.R", "Doigts_2.R", "Pouce_1.R", "Pouce_2.R")]].sum(1)
hand_e = (handw[E[:, 0]] > 0.9) & (handw[E[:, 1]] > 0.9)
log("sommets de la main (poids main > 0.9) : %d" % int((handw > 0.9).sum()))
P1 = arm_edges(); L1 = np.linalg.norm(P1[E[:, 0]] - P1[E[:, 1]], axis=1)
F = np.array([p.vertices[:3] for p in ob.data.polygons])
def fnorm(P): n = np.cross(P[F[:, 1]] - P[F[:, 0]], P[F[:, 2]] - P[F[:, 0]]); return n / np.maximum(np.linalg.norm(n, axis=1, keepdims=True), 1e-12)
N1 = fnorm(P1)
mainb = arm.data.bones["Main.R"]
for k in (2.0, 3.0, 3.8):
    arm.pose.bones["Bras.R"].scale = (1, k, 1); arm.pose.bones["AvantBras.R"].scale = (1, 1, 1)
    mainb.inherit_scale = "NONE"
    vl.update()
    Pk = arm_edges(); Lk = np.linalg.norm(Pk[E[:, 0]] - Pk[E[:, 1]], axis=1)
    r = Lk / np.maximum(L1, 1e-6)
    flips = int(((fnorm(Pk) * N1).sum(1) < 0)[np.isin(region[F[:, 0]], ["brasR"])].sum())
    log("étirement ×%.1f : arêtes du bras : rapport max %.2f, %.1f %% au-delà de ×%.1f ; main : rapport max %.2f ; triangles retournés %d" % (
        k, r[sel].max(), 100 * (r[sel] > k * 1.15).mean(), k * 1.15, r[hand_e].max() if hand_e.any() else 0, flips))
    if k == 3.8:
        np.save(os.path.join(OUT, "etirement_k38.npy"), Pk)
arm.pose.bones["Bras.R"].scale = (1, 1, 1); mainb.inherit_scale = "FULL"; vl.update()
json.dump(REP, open(os.path.join(OUT, "stage4_rapport.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
arm.animation_data.action = acts[0]; sc.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "stage4.blend"))
log("fini en %.0f s" % (time.time() - t0))
