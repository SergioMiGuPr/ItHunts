# -*- coding: utf-8 -*-
"""Clips du traqueur v4 (Blender 5.0, 30 images/s), relevés sur les vidéos de docs/art/references-anim/.

Chaque clip est décrit par des poses (directions des os dans le plan de profil, voir anim_traqueur.vers_monde) clés toutes
les 2 images, calculées par des fonctions du temps calées sur la vidéo (instants relevés image par image : voir le tableau
de chaque clip). Pieds et mains en appui : IK analytique sur une position monde fixe (glissement nul). Le modèle n'est
jamais modifié (anim_traqueur.verifier_action, empreinte).
"""
import bpy, math
from mathutils import Vector, Quaternion
import anim_traqueur as A

V = Vector
W = A.vers_monde
TAU = 2 * math.pi


def ss(a, b, x):
    t = max(0.0, min(1.0, (x - a) / (b - a)))
    return t * t * (3 - 2 * t)


def lerp(a, b, t):
    return a + (b - a) * t


# ------------------------------------------------------------------ pose voûtée de base (vidéo traqueur_ref_repos, 0.00 s)
def pose_voutee(b=0.0, sway=0.0, bras=(0.0, 0.0), avbras=(0.0, 0.0), doigts=(0.0, 0.0), tete_lat=0.0, genou=0.0):
    """b : respiration 0..1 ; sway : balancement avant/arrière du buste ; bras/avbras/doigts : (G, D) écarts vers l'avant."""
    d = {"Bassin": W(0.05 - 0.05 * sway, 1), "Colonne_1": W(0.15 + 0.04 * sway, 1), "Colonne_2": W(0.25 + 0.05 * sway, 1),
         "Colonne_3": W(0.45 - 0.08 * b + 0.06 * sway, 1), "Colonne_4": W(0.85 - 0.14 * b, 0.8 + 0.12 * b),
         "Cou_1": W(1, 0.15 + 0.05 * b), "Cou_2": W(1, -0.25, tete_lat * 0.5, 1), "Tete": W(0.75, -0.6 - 0.04 * b, tete_lat, 1)}
    for i, (s, k) in enumerate((("G", 1), ("D", -1))):
        d["Clavicule_" + s] = W(0.35, 0.05 + 0.07 * b, 1, k)
        d["Bras_" + s] = W(0.1 + bras[i], -1, 0.12, k)
        d["AvantBras_" + s] = W(0.17 + avbras[i], -1, 0.04, k)
        d["Main_" + s] = W(0.05 + avbras[i] * 1.2, -1, 0, k)
        d["Doigts_" + s] = W(-0.15 + doigts[i], -1, 0, k)
        d["Cuisse_" + s] = W(0.25 + genou, -1, 0.05, k)
        d["Jambe_" + s] = W(-0.3 - genou, -1, 0.03, k)
    return d


def appliquer(d, a=None, pieds=None, machoire=0.0, vent=V((0, 0, 0))):
    """Pose complète : directions, pieds verrouillés (dict côté -> (cheville, dir pied, dir orteils)), mâchoire, cheveux."""
    a = a or A.arm()
    A.poser(d, a)
    if pieds:
        for s, (ch, dp, do) in pieds.items():
            A.ik_jambe(s, ch, V((0, -1, 0.25)), dp, do, a)
    pm = a.pose.bones["Machoire"]
    pm.rotation_quaternion = Quaternion(V((1, 0, 0)), math.radians(machoire))
    bpy.context.view_layer.update()
    A.cheveux_gravite(a, 1.0, vent)


def repos(fin=96):
    """Traqueur_Repos : boucle de 3.2 s (96 images) = un cycle de respiration de la vidéo (montée du buste 0 → 1.8 s, descente
    jusqu'à 3.2 s, relevé sur le centre de la silhouette) ; balancement léger de la tête, bras qui pendent et se balancent,
    doigts qui frémissent, mâchoire. Pieds fixes."""
    a = A.arm()
    act = A.nouvelle_action("Traqueur_Repos", fin)
    A.remise_a_zero(a)
    # pose de référence : pieds au sol
    appliquer(pose_voutee(), a)
    z = min(A.point("Contact_Pied_" + s, "head").z for s in "GD")
    A.deplacer_racine((0, 0, -z))
    pieds = {s: (A.point("Pied_" + s, "head"), A.direction("Pied_" + s), A.direction("Orteils_" + s)) for s in "GD"}
    r0 = a.pose.bones["Racine"].location.copy()
    for f in range(0, fin + 1, 2):
        ph = TAU * f / fin
        phw = ph + 0.25 * math.sin(ph)                         # montée plus longue que la descente
        b = 0.5 - 0.5 * math.cos(phw)
        sway = 0.5 * math.sin(ph + 0.4)
        bras = (0.05 * math.sin(ph + 0.9), 0.05 * math.sin(ph + 0.9 + math.pi * 0.8))
        avb = (0.05 * math.sin(ph + 0.3), 0.05 * math.sin(ph + 0.3 + math.pi * 0.8))
        dg = (0.12 * math.sin(3 * ph + 1.0) + 0.07 * math.sin(5 * ph + 2.0), 0.12 * math.sin(4 * ph + 0.3) + 0.06 * math.sin(7 * ph + 1.0))
        tl = 0.07 * math.sin(ph + 1.2)
        A.remise_a_zero(a)
        a.pose.bones["Racine"].location = r0 + V((0, 0, -0.05 * b))   # genoux qui fléchissent un peu à l'inspiration
        bpy.context.view_layer.update()
        appliquer(pose_voutee(b, sway, bras, avb, dg, tl), a, pieds, machoire=3 + 3 * b + 1.5 * math.sin(2 * ph),
                  vent=V((tl * 0.5, 0, 0)))
        A.cle(f, a)
    A.lisser_quaternions(act)
    A.interpolation(act)
    A.boucler(act)
    return act


# ------------------------------------------------------------------ démarche générique (appuis fixes, IK, racine qui avance)
def _mj(s):
    """Courbe à jerk minimal 0 -> 1."""
    s = max(0.0, min(1.0, s))
    return s * s * s * (10 - 15 * s + 6 * s * s)


class Membre:
    """Un appui : côté, type ('main' / 'pied'), instant de pose (fraction du cycle), durée en l'air (fraction), décalage
    avant au milieu de l'appui (m, par rapport à l'épaule / la hanche projetée), écart latéral (m), hauteur de l'arc (m)."""
    def __init__(self, cote, typ, pose, vol, avance, lat, haut):
        self.cote, self.typ, self.pose, self.vol, self.avance, self.lat, self.haut = cote, typ, pose, vol, avance, lat, haut


def _etat(m, ph):
    """Renvoie ('appui', fraction de l'appui) ou ('vol', fraction du vol) pour la phase ph (0..1) du cycle."""
    lev = (m.pose - m.vol) % 1.0
    x = (ph - lev) % 1.0
    if x < m.vol:
        return "vol", x / m.vol
    return "appui", (x - m.vol) / (1 - m.vol)


def demarche(nom, T, S, membres, posture, hanche_z, n_cycles=1, roulis_pied=35.0, vent_cheveux=0.2, machoire=None):
    """T : durée du cycle (images) ; S : longueur de foulée (m, avance du corps par cycle) ; posture(ph) -> dict de directions
    (buste, tête, clavicules ; les membres sont posés par IK) ; hanche_z(ph) -> hauteur de Racine (décalage, m)."""
    a = A.arm()
    fin = T * n_cycles
    act = A.nouvelle_action(nom, fin)
    A.remise_a_zero(a)
    B = a.data.bones
    L = lambda n: (B[n].tail_local - B[n].head_local).length
    # vecteurs de repos du pied : cheville, bout du pied (articulation des orteils), pointe des orteils
    pied_rest = {}
    for s in "GD":
        ch = B["Pied_" + s].head_local.copy(); bo = B["Pied_" + s].tail_local.copy(); po = B["Orteils_" + s].tail_local.copy()
        pied_rest[s] = (ch - bo, (po - bo).normalized(), (bo - ch).normalized(), po.z)
    # positions de référence (posture du début, racine à 0) : épaules et hanches projetées
    A.poser(posture(0.0), a); A.deplacer_racine((0, 0, hanche_z(0.0)))
    base_y = {}
    for s in "GD":
        base_y[("main", s)] = A.point("Bras_" + s, "head").y
        base_y[("pied", s)] = A.point("Cuisse_" + s, "head").y
    v = S / T                                     # m par image
    # mains sans décalage imposé (avance None) : posées à 96 % de l'allonge du bras, pour un bras presque tendu
    for m in membres:
        if m.typ == "main" and m.avance is None:
            k = 1 if m.cote == "G" else -1
            dm = W(0.55, -1, 0.1, k); dd = W(1, -0.25, 0.05, k)
            poignet_z = 0.18 - (dd * L("Doigts_" + m.cote)).z - (dm * L("Main_" + m.cote)).z
            h = A.point("Bras_" + m.cote, "head").z - poignet_z
            R = 0.96 * (L("Bras_" + m.cote) + L("AvantBras_" + m.cote))
            xp = math.sqrt(max(R * R - h * h, 0.0))
            D = v * (1 - m.vol) * T
            m.avance = xp - D / 2
            print("main", m.cote, "épaule", round(h, 2), "pose à", round(xp, 2), "m devant, avance", round(m.avance, 2))
    A.remise_a_zero(a)
    def racine_y(f):
        return -v * f                             # avant = -Y
    # position monde d'un appui pour le pas n (milieu de l'appui à l'instant tm)
    def appui_pos(m, k):
        """Point de contact du k-ième appui de ce membre (k entier), au sol."""
        # milieu de l'appui k : pose + (1 - vol)/2 du cycle, au cycle k
        tm = (m.pose + (1 - m.vol) / 2 + k) * T
        y = racine_y(tm) + base_y[(m.typ, m.cote)] - m.avance
        x = (1 if m.cote == "G" else -1) * m.lat
        return V((x, y, 0.0))
    for f in range(0, fin + 1):
        ph = (f % T) / T
        cyc = f // T
        A.remise_a_zero(a)
        rz = hanche_z(ph)
        a.pose.bones["Racine"].location = V((0, 0, 0))
        A.deplacer_racine((0, racine_y(f), rz))
        A.poser(posture(ph), a)
        for m in membres:
            et, u = _etat(m, ph)
            # index de l'appui courant / suivant
            lev = (m.pose - m.vol) % 1.0
            k_cur = math.floor(f / T - m.pose) + (1 if ((f / T - m.pose) % 1.0) >= 0 else 0)
            # appui en cours : celui dont la pose est la dernière passée
            k_pose = math.floor(f / T - m.pose)          # dernier instant de pose passé : k_pose (pose au temps (k+pose)T)
            if et == "appui":
                P = appui_pos(m, k_pose)
                roll = 0.0
                ua = u
                if m.typ == "pied" and ua > 0.8:
                    roll = roulis_pied * _mj((ua - 0.8) / 0.2)
                _poser_membre(a, m, P, roll, "appui", 0.0, pied_rest)
            else:
                P0 = appui_pos(m, k_pose); P1 = appui_pos(m, k_pose + 1)
                s_ = _mj(u)
                P = P0.lerp(P1, s_) + V((0, 0, m.haut * math.sin(math.pi * min(1.0, u * 1.1))))
                roll = roulis_pied * (1 - _mj(u * 1.6)) if m.typ == "pied" else 0.0
                _poser_membre(a, m, P, roll, "vol", u, pied_rest)
        A.cheveux_gravite(a, 1.0, V((0, vent_cheveux, 0)))
        if machoire:
            a.pose.bones["Machoire"].rotation_quaternion = Quaternion(V((1, 0, 0)), math.radians(machoire(ph)))
        A.cle(f, a)
    A.lisser_quaternions(act)
    A.interpolation(act)
    return act


def _poser_membre(a, m, P, roll, etat, u, pied_rest):
    s = m.cote
    k = 1 if s == "G" else -1
    if m.typ == "pied":
        rel_ch, dir_orteils, dir_pied, z_po = pied_rest[s]
        # P = point d'appui du bout du pied (articulation des orteils) au sol ; roulis autour de cet axe (lève le talon)
        boule = V((P.x, P.y, P.z + 0.12))
        R = Quaternion(V((1, 0, 0)), math.radians(roll))
        cheville = boule + R @ rel_ch
        dp = (R @ dir_pied).normalized()
        do = dir_orteils if etat == "appui" else (Quaternion(V((1, 0, 0)), math.radians(20.0 * math.sin(math.pi * u))) @ dir_orteils)
        A.ik_jambe(s, cheville, V((0.15 * k, -1, 0.4)), dp, do, a)
    else:
        # P = pointe des griffes au sol ; main posée à plat, doigts allongés vers l'avant
        if etat == "appui":
            dm = W(0.55, -1, 0.1, k); dd = W(1, -0.25, 0.05, k)
        else:
            # en l'air : la main quitte le sol, pend doigts recourbés, puis s'ouvre et s'allonge avant la pose
            pa_m, pa_d = W(0.55, -1, 0.1, k), W(1, -0.25, 0.05, k)          # main posée
            cu_m, cu_d = W(0.15, -1, 0.1, k), W(-0.5, -1, 0.05, k)          # main pendante, doigts recourbés
            if u < 0.3:
                o = _mj(u / 0.3); dm = pa_m.lerp(cu_m, o).normalized(); dd = pa_d.lerp(cu_d, o).normalized()
            else:
                o = _mj((u - 0.45) / 0.55); dm = cu_m.lerp(pa_m, o).normalized(); dd = cu_d.lerp(pa_d, o).normalized()
        Lm = (a.data.bones["Main_" + s].tail_local - a.data.bones["Main_" + s].head_local).length
        Ld = (a.data.bones["Doigts_" + s].tail_local - a.data.bones["Doigts_" + s].head_local).length
        poignet = P + V((0, 0, 0.18)) - dd * Ld - dm * Lm
        A.ik_bras(s, poignet, V((0.5 * k, 1, 0.2)), dm, dd, a)


def posture_marche(ph, lat=1.0):
    w = TAU * ph
    sw = 0.07 * math.sin(w + 0.6) * lat
    return {"Bassin": W(1, 0.12), "Colonne_1": W(1, 0.2, sw * 0.5, 1), "Colonne_2": W(1, 0.02, sw, 1),
            "Colonne_3": W(1, -0.25, sw, 1), "Colonne_4": W(1, -0.5, sw * 0.5, 1),
            "Cou_1": W(1, -0.1), "Cou_2": W(1, -0.25 + 0.04 * math.sin(2 * w), -sw, 1),
            "Tete": W(1, 0.05 + 0.05 * math.sin(2 * w + 0.8), -sw * 1.5, 1),
            "Clavicule_G": W(0.35 + 0.25 * math.sin(w + 2.2), 0.0, 1, 1), "Clavicule_D": W(0.35 + 0.25 * math.sin(w + 2.2 + math.pi), 0.0, 1, -1)}


def marche_membres():
    return [Membre("D", "main", 0.10, 0.25, None, 1.7, 1.6), Membre("G", "pied", 0.44, 0.15, 0.0, 1.15, 0.9),
            Membre("G", "main", 0.61, 0.25, None, 1.7, 1.6), Membre("D", "pied", 0.91, 0.15, 0.0, 1.15, 0.9)]


def marche_a(T=165, S=7.0):
    """Traqueur_Marche_A : marche lourde à quatre pattes de la vidéo traqueur_ref_marche (cycle relevé sur les appuis au sol,
    caméra fixe) : main droite posée à 0.10 du cycle, jambe gauche à 0.44, main gauche à 0.61, jambe droite à 0.91 ;
    mains en l'air 25 % du cycle (bras qui va loin devant), pieds 15 % ; dos à l'horizontale avec la bosse, tête basse."""
    act = demarche("Traqueur_Marche_A", T, S, marche_membres(), posture_marche, lambda ph: -1.5 + 0.12 * math.cos(2 * TAU * ph),
                   machoire=lambda ph: 4 + 2 * math.sin(TAU * ph))
    return act


def marche_b(T=165, S=7.0):
    """Traqueur_Marche_B : la même marche en miroir (la main gauche ouvre le cycle, comme dans la seconde moitié de la vidéo),
    balancement du buste et de la tête inversé, tête un peu plus haute : variante pour casser la répétition."""
    mem = [Membre("G", "main", 0.10, 0.25, None, 1.7, 1.7), Membre("D", "pied", 0.44, 0.15, 0.0, 1.15, 0.9),
           Membre("D", "main", 0.61, 0.25, None, 1.7, 1.7), Membre("G", "pied", 0.91, 0.15, 0.0, 1.15, 0.9)]
    def post(ph):
        d = posture_marche(ph, lat=-1.0)
        d["Clavicule_G"], d["Clavicule_D"] = W(0.35 + 0.25 * math.sin(TAU * ph + 2.2 + math.pi), 0.0, 1, 1), W(0.35 + 0.25 * math.sin(TAU * ph + 2.2), 0.0, 1, -1)
        d["Tete"] = W(1, 0.15 + 0.06 * math.sin(2 * TAU * ph + 1.6), 0.08 * math.sin(TAU * ph), 1)
        return d
    return demarche("Traqueur_Marche_B", T, S, mem, post, lambda ph: -1.45 + 0.12 * math.cos(2 * TAU * ph + 0.5),
                    machoire=lambda ph: 6 + 3 * math.sin(TAU * ph + 1.0))
