# -*- coding: utf-8 -*-
"""Sons du jeu (lot B, 2026-11-21) : les 20 MP3 de docs/art/audio/jeu/ (générés par l'utilisateur sur Higgsfield)
préparés pour Unity, en WAV 16 bits dans Assets/Art/Audio/{Traqueur,Goule,Volant,Armes,Musique}/ :
- silences de tête et de queue retirés (seuil -50 dBFS sur l'enveloppe, 10 ms gardés) ;
- effets : crête normalisée à -1 dBFS ; fondus de 4 ms (début) et 25 ms (fin) contre les clics des sons ponctuels ;
- boucles (_boucle) et musiques : fondu enchaîné à puissance constante de la fin vers le début (raccord sans trou ni
  clic), puis normalisation ; musiques au même niveau RMS que la boucle du menu (-14,2 dB), crête limitée à -1 dBFS.
Rapport : sons_jeu.json (durées, silences retirés, gain, raccord).

Usage : python sons_jeu.py   (ffmpeg dans le PATH)
"""
import json, os, subprocess
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../../../../.."))
SRC = os.path.join(ROOT, "docs/art/audio/jeu")
OUT = os.path.join(ROOT, "Assets/Art/Audio")
SEUIL_DB = -50.0
CRETE_DB = -1.0
RMS_MENU_DB = -14.2      # Musique_Menu_Boucle (boucles_audio.json)
SR = 44100

# source -> (dossier, nom Unity, type) ; type : un (son ponctuel), boucle, musique
SONS = {
    "sfx_traqueur_cri_enrage_01": ("Traqueur", "Sfx_Traqueur_Cri_Enrage_01", "un"),
    "sfx_traqueur_cri_enrage_02": ("Traqueur", "Sfx_Traqueur_Cri_Enrage_02", "un"),
    "sfx_traqueur_cri_prise": ("Traqueur", "Sfx_Traqueur_Cri_Prise", "un"),
    "sfx_traqueur_grognement": ("Traqueur", "Sfx_Traqueur_Grognement", "un"),
    "sfx_traqueur_souffle_boucle": ("Traqueur", "Sfx_Traqueur_Souffle_Boucle", "boucle"),
    "sfx_traqueur_pas": ("Traqueur", "Sfx_Traqueur_Pas", "un"),
    "sfx_goule_alerte": ("Goule", "Sfx_Goule_Alerte", "un"),
    "sfx_goule_attaque_01": ("Goule", "Sfx_Goule_Attaque_01", "un"),
    "sfx_goule_attaque_02": ("Goule", "Sfx_Goule_Attaque_02", "un"),
    "sfx_goule_mort": ("Goule", "Sfx_Goule_Mort", "un"),
    "sfx_volant_cri": ("Volant", "Sfx_Volant_Cri", "un"),
    "sfx_volant_ailes_boucle": ("Volant", "Sfx_Volant_Ailes_Boucle", "boucle"),
    "sfx_volant_mort": ("Volant", "Sfx_Volant_Mort", "un"),
    "sfx_barillet_tir_01": ("Armes", "Sfx_Barillet_Tir_01", "un"),
    "sfx_barillet_tir_02": ("Armes", "Sfx_Barillet_Tir_02", "un"),
    "sfx_barillet_recharge": ("Armes", "Sfx_Barillet_Recharge", "un"),
    "sfx_lanterne_jet_debut": ("Armes", "Sfx_Lanterne_Jet_Debut", "un"),
    "sfx_lanterne_jet_boucle": ("Armes", "Sfx_Lanterne_Jet_Boucle", "boucle"),
    "musique_jeu_exploration": ("Musique", "Musique_Jeu_Exploration_Boucle", "musique"),
    "musique_jeu_enrage": ("Musique", "Musique_Jeu_Enrage_Boucle", "musique"),
}


def decode(path, ch):
    raw = subprocess.run(["ffmpeg", "-v", "error", "-i", path, "-f", "f32le", "-ac", str(ch), "-ar", str(SR), "-"],
                         capture_output=True, check=True).stdout
    return np.frombuffer(raw, np.float32).reshape(-1, ch).astype(np.float64)


def encode(path, a):
    pcm = (np.clip(a, -1, 1) * 32767).round().astype("<i2").tobytes()
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-f", "s16le", "-ac", str(a.shape[1]), "-ar", str(SR), "-i", "-", path],
                   input=pcm, check=True)


def db(x):
    return float(20 * np.log10(x + 1e-12))


def rms(a):
    return float(np.sqrt((a ** 2).mean()))


def boucle(a, xf):
    X = int(xf * SR)
    t = np.linspace(0, np.pi / 2, X)[:, None]
    n = len(a)
    out = a[:n - X].copy()
    out[:X] = a[:X] * np.sin(t) + a[n - X:] * np.cos(t)
    return out


def main():
    rep = {}
    for src, (dossier, nom, typ) in SONS.items():
        ch = 2 if typ == "musique" else 1
        a = decode(os.path.join(SRC, src + ".mp3"), ch)
        dur0 = len(a) / SR
        env = np.abs(a).max(1)
        nz = np.nonzero(env > 10 ** (SEUIL_DB / 20))[0]
        pad = int(0.010 * SR)
        i0, i1 = max(nz[0] - pad, 0), min(nz[-1] + 1 + pad, len(a))
        a = a[i0:i1]
        info = {"source": "docs/art/audio/jeu/%s.mp3" % src, "duree_source_s": round(dur0, 3),
                "silence_retire_s": [round(i0 / SR, 3), round(dur0 - i1 / SR, 3)]}
        if typ == "un":
            f_in, f_out = int(0.004 * SR), int(0.025 * SR)
            a[:f_in] *= np.linspace(0, 1, f_in)[:, None]
            a[-f_out:] *= np.linspace(1, 0, f_out)[:, None]
            g = 10 ** (CRETE_DB / 20) / np.abs(a).max()
            a = a * g
        else:
            xf = 2.5 if typ == "musique" else min(1.0, 0.15 * len(a) / SR)
            a = boucle(a, xf)
            info["fondu_raccord_s"] = round(xf, 3)
            if typ == "musique":
                g = 10 ** (RMS_MENU_DB / 20) / rms(a)
                pk = np.abs(a).max() * g
                if pk > 10 ** (CRETE_DB / 20):
                    # limiteur doux : compression des crêtes au-dessus de -6 dBFS (rare)
                    a = a * g
                    s = 10 ** (-6 / 20); lim = 10 ** (CRETE_DB / 20)
                    m = np.abs(a) > s
                    a[m] = np.sign(a[m]) * (s + (lim - s) * np.tanh((np.abs(a[m]) - s) / (lim - s)))
                    g = 1.0
                a = a * g
            else:
                g = 10 ** (CRETE_DB / 20) / np.abs(a).max()
                a = a * g
            info["saut_raccord"] = round(float(np.abs(a[-1] - a[0]).max()), 5)
        os.makedirs(os.path.join(OUT, dossier), exist_ok=True)
        encode(os.path.join(OUT, dossier, nom + ".wav"), a)
        info.update({"fichier": "Assets/Art/Audio/%s/%s.wav" % (dossier, nom), "type": typ, "canaux": ch,
                     "duree_s": round(len(a) / SR, 3), "crete_dbfs": round(db(np.abs(a).max()), 2), "rms_db": round(db(rms(a)), 1)})
        rep[nom] = info
        print(nom, info["duree_s"], "s, crête", info["crete_dbfs"], "dB, RMS", info["rms_db"], "dB", info.get("saut_raccord", ""))
    with open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "sons_jeu.json"), "w", encoding="utf-8") as f:
        json.dump(rep, f, indent=1, ensure_ascii=False)


if __name__ == "__main__":
    main()
