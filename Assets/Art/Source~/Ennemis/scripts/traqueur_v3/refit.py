"""Recalage des os sur le maillage (centroïdes de tranches) : coudes, poignets, articulations des doigts et du pouce,
tête, mâchoire, pied et orteils. Utilisé par stage4.py (refit(arm, ob_full) avant les poids)."""
import numpy as np
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree


def _inside_fn(ob):
    bvh = BVHTree.FromObject(ob, bpy.context.evaluated_depsgraph_get())
    def inside(p):
        n = 0; o = Vector(p); d = Vector((0.0123, 0.0071, 1.0)).normalized()
        for _ in range(60):
            hit, nrm, i, dist = bvh.ray_cast(o, d)
            if hit is None: break
            n += 1; o = hit + d * 1e-4
        return n % 2 == 1
    return inside


def slice_c(P, a, d, t, w):
    s = (P - a) @ d
    m = np.abs(s - t) < w
    if m.sum() < 6:
        m = np.abs(s - t) < w * 3
    return P[m].mean(0) if m.any() else a + d * t


def refit(arm, ob):
    co = np.array([v.co[:] for v in ob.data.vertices])
    vl = bpy.context.view_layer
    for o in vl.objects: o.select_set(False)
    arm.select_set(True); vl.objects.active = arm
    bpy.ops.object.mode_set(mode="EDIT")
    eb = arm.data.edit_bones
    V = lambda b, k: np.array(getattr(eb[b], k)[:])
    rep = []
    for s in "LR":
        sh, el, wr = V("Bras." + s, "head"), V("AvantBras." + s, "head"), V("Main." + s, "head")
        d = (wr - sh) / np.linalg.norm(wr - sh)
        # sommets du bras : proches de l'axe épaule-poignet (dans un cylindre), côté de ce bras
        rel = co - sh; t = rel @ d; r = np.linalg.norm(rel - np.outer(t, d), axis=1)
        L = np.linalg.norm(wr - sh)
        armv = co[(t > -0.2) & (t < L + 0.3) & (r < 1.6) & (np.sign(co[:, 0]) == np.sign(sh[0]))]
        E2 = slice_c(armv, sh, d, (el - sh) @ d, 0.18)
        W2 = slice_c(armv, sh, d, L, 0.18)
        eb["AvantBras." + s].head = Vector(E2); eb["Main." + s].head = Vector(W2)
        # main : de poignet aux jointures, puis doigts groupés
        k1, k2, tip = V("Main." + s, "tail"), V("Doigts_1." + s, "tail"), V("Doigts_2." + s, "tail")
        dh = (tip - W2) / np.linalg.norm(tip - W2)
        rel = co - W2; t = rel @ dh; r = np.linalg.norm(rel - np.outer(t, dh), axis=1)
        Lh = np.linalg.norm(tip - W2)
        hand = co[(t > -0.1) & (t < Lh + 0.4) & (r < 1.8) & (np.sign(co[:, 0]) == np.sign(sh[0]))]
        K1 = slice_c(hand, W2, dh, (k1 - W2) @ dh, 0.15)
        K2 = slice_c(hand, W2, dh, (k2 - W2) @ dh, 0.12)
        Tp = slice_c(hand, W2, dh, ((hand - W2) @ dh).max() - 0.25, 0.2)
        eb["Main." + s].tail = Vector(K1); eb["Doigts_1." + s].head = Vector(K1)
        eb["Doigts_1." + s].tail = Vector(K2); eb["Doigts_2." + s].head = Vector(K2); eb["Doigts_2." + s].tail = Vector(Tp)
        # pouce : sommets de la main plus proches de l'ancien pouce que des doigts
        p1h, p2t = V("Pouce_1." + s, "head"), V("Pouce_2." + s, "tail")
        dp = (p2t - p1h) / np.linalg.norm(p2t - p1h)
        rel = hand - p1h; t = rel @ dp; r = np.linalg.norm(rel - np.outer(t, dp), axis=1)
        th = hand[(t > -0.2) & (t < np.linalg.norm(p2t - p1h) + 0.5) & (r < 0.55)]
        if len(th) > 20:
            tt = (th - p1h) @ dp
            A = th[tt < np.percentile(tt, 20)].mean(0); Bt = th[tt > np.percentile(tt, 90)].mean(0)
            M = A + (Bt - A) * 0.55
            eb["Pouce_1." + s].head = Vector(A); eb["Pouce_1." + s].tail = Vector(M)
            eb["Pouce_2." + s].head = Vector(M); eb["Pouce_2." + s].tail = Vector(Bt)
        # pied et orteils : cheville au centre, orteils jusqu'au bout du pied
        ah, at = V("Pied." + s, "head"), V("Orteils." + s, "tail")
        foot = co[(np.abs(co[:, 0] - ah[0]) < 1.2) & (co[:, 2] < ah[2] + 0.4) & (co[:, 1] > ah[1] - 1.0)]
        if len(foot):
            ball_y = foot[:, 1].min() + (foot[:, 1].max() - foot[:, 1].min()) * 0.72
            ball = foot[np.abs(foot[:, 1] - ball_y) < 0.2].mean(0)
            toe = foot[foot[:, 1] > np.percentile(foot[:, 1], 93)].mean(0)
            ank = foot[np.abs(foot[:, 2] - ah[2]) < 0.2].mean(0)
            eb["Jambe." + s].tail = Vector(ank); eb["Pied." + s].head = Vector(ank)
            eb["Pied." + s].tail = Vector(ball); eb["Orteils." + s].head = Vector(ball); eb["Orteils." + s].tail = Vector(toe)
    # tête : du haut du cou au centre du crâne ; mâchoire : de la charnière au menton
    ct = V("Cou", "tail")
    head = co[(co[:, 2] > ct[2] - 0.3) & (np.abs(co[:, 0]) < 2.2)]
    neck = co[(np.abs(co[:, 2] - ct[2]) < 0.2) & (np.abs(co[:, 0]) < 1.5)]
    if len(neck):
        N = neck.mean(0); eb["Cou"].tail = Vector(N); eb["Tete"].head = Vector(N)
    core = head[np.abs(head[:, 0]) < 0.9]                         # sans les oreilles ni les cornes écartées
    zt = np.percentile(core[:, 2], 92)
    sl = core[np.abs(core[:, 2] - (ct[2] + (zt - ct[2]) * 0.7)) < 0.25]
    skull = sl.mean(0) if len(sl) else core.mean(0)
    eb["Tete"].tail = Vector(skull)
    jaw = head[(head[:, 1] > 1.91) & (head[:, 2] < 18.04)]
    if len(jaw) > 20:
        hinge = jaw[jaw[:, 1] < np.percentile(jaw[:, 1], 20)].mean(0)
        chin = jaw[jaw[:, 1] > np.percentile(jaw[:, 1], 85)].mean(0)
        eb["Machoire"].head = Vector(hinge); eb["Machoire"].tail = Vector(chin)
    for b in eb:
        b.roll = 0.0
    bpy.ops.object.mode_set(mode="OBJECT")
    inside = _inside_fn(ob)
    out = []
    for b in arm.data.bones:
        if b.name == "Racine": continue
        ins = [inside(b.head_local.lerp(b.tail_local, t)) for t in (0.1, 0.3, 0.5, 0.7, 0.9)]
        if not all(ins):
            out.append("%s %s" % (b.name, "".join("x" if i else "." for i in ins)))
    return out
