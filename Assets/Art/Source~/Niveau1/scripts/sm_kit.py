# -*- coding: utf-8 -*-
"""Pièces du kit de la carte aléatoire (INTEGRATION-PROPS.md § 1, ART-HANDOFF « Kit de construction ») :
SM_Kit_Mur4x4, SM_Kit_Sol4, SM_Kit_Escalier, SM_Kit_Pont, SM_Kit_Mur2x4, SM_Kit_Mur4x3.

Préparées pour les UV en coordonnées monde du shader (RENDU.md § 5 ter) : volumes nets à faces planes, normales vers
l'extérieur, arêtes vives (normales séparées par face), aucune géométrie qui doive se raccorder d'une pièce à l'autre.
Jusqu'à ≈ 60 000 pièces jointives, étirées ou coupées par le code : un chanfrein, un socle ou des joints modelés
feraient une couture tous les 4 m ; le dessin des pierres vient de la texture projetée en monde. UV fournies quand même
(projection monde de Blender, mêmes tuiles que le shader) pour l'aperçu et un repli sans le mot-clé _WORLD_UV.

Cotes et pivots (repère Unity, ART-HANDOFF) :
- Mur 4 × 4 × 1 (2 × 4, 4 × 3) : pivot au pied, au milieu (x −2..2, y 0..4, z −0.5..0.5) ; M_Pierre ;
- Sol 4 × 1 × 4 : pivot au milieu du dessus (y −1..0) ; dessus M_Dalles, flancs et dessous M_Pierre ;
- Escalier : marche de 4 × 0.25 × 1, pivot au milieu du bas de la contremarche (x −2..2, y 0..0.25, z 0..1), étirée
  par le code en hauteur et en profondeur (la collision reste une pente lisse) ; giron M_Dalles, reste M_Pierre ;
- Pont : passerelle de 3 (x, étirée) × 1 × 4 (z), pivot au milieu du dessus ; dessus M_Dalles, reste M_Pierre.

Lancer dans Blender :
    p = r"<dépôt>/Assets/Art/Source~/Niveau1/scripts/sm_kit.py"
    exec(compile(open(p, encoding="utf-8").read(), p, "exec"), {"__file__": p, "__name__": "__main__"})
"""
import sys, os
sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.modules.pop("n1_props", None)
import n1_props as N
C, F = N.C, N.F

# (nom, boîte en coordonnées Unity : (xmin, ymin, zmin), (xmax, ymax, zmax), matériau du dessus, matériau du reste)
PIECES = [
    ("SM_Kit_Mur4x4", (-2.0, 0.0, -0.5), (2.0, 4.0, 0.5), "pierre", "pierre"),
    ("SM_Kit_Sol4", (-2.0, -1.0, -2.0), (2.0, 0.0, 2.0), "dalles", "pierre"),
    ("SM_Kit_Escalier", (-2.0, 0.0, 0.0), (2.0, 0.25, 1.0), "dalles", "pierre"),
    ("SM_Kit_Pont", (-1.5, -1.0, -2.0), (1.5, 0.0, 2.0), "dalles", "pierre"),
    ("SM_Kit_Mur2x4", (-1.0, 0.0, -0.5), (1.0, 4.0, 0.5), "pierre", "pierre"),
    ("SM_Kit_Mur4x3", (-2.0, 0.0, -0.5), (2.0, 3.0, 0.5), "pierre", "pierre"),
]


def U2B(x, y, z):
    """Unity -> Blender (avant -Z / haut Y à l'export) : (x, y, z) -> (-x, -z, y)."""
    return (-x, -z, y)


def make(name, mn, mx, top, rest):
    def build(P):
        mats = N.materials()
        B = N.builder()
        a, b = U2B(*mn), U2B(*mx)
        lo = tuple(min(a[i], b[i]) for i in range(3))
        hi = tuple(max(a[i], b[i]) for i in range(3))
        faces = B.box(lo, hi, rest)
        if top != rest:
            for f in faces:
                if f is not None:
                    f.normal_update()
                if f is not None and f.normal.z > 0.9:
                    f.material_index = B.mat_index(top)
        ob = B.finish(name, mats, sharp_angle=30.0)
        return [ob]
    return build


if __name__ == "__main__":
    for name, mn, mx, top, rest in PIECES:
        N.run(name, make(name, mn, mx, top, rest), {}, views=("face", "34"))
