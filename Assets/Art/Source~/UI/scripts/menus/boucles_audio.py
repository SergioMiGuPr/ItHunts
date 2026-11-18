# -*- coding: utf-8 -*-
"""Boucles audio du menu : silences de tête et de queue retirés (MP3 : 12 ms de silence d'encodeur ; feu : 0,1 s de
chaque côté), puis fondu enchaîné à puissance constante de la fin vers le début, pour que la lecture en boucle n'ait ni
trou ni clic. Sorties WAV 16 bits (Unity les ré-encode à l'import) dans Assets/Art/Audio/Menu/.

Usage : python boucles_audio.py   (ffmpeg dans le PATH)
"""
import json, os, subprocess, sys
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../../../../../.."))
SRC = os.path.join(ROOT, "docs/art/audio/menu")
OUT = os.path.join(ROOT, "Assets/Art/Audio/Menu")
FICHIERS = {
    # source : (sortie, fréquence, fondu en s)
    "musique_menu_v01.mp3": ("Musique_Menu_Boucle.wav", 44100, 2.5),
    "sfx_feu_boucle_v01.wav": ("Sfx_Feu_Boucle.wav", 48000, 1.5),
}
SEUIL_SILENCE_DB = -50.0


def decode(path, sr):
    raw = subprocess.run(["ffmpeg", "-v", "error", "-i", path, "-f", "f32le", "-ac", "2", "-ar", str(sr), "-"],
                         capture_output=True, check=True).stdout
    return np.frombuffer(raw, np.float32).reshape(-1, 2).astype(np.float64)


def encode(path, a, sr):
    pcm = (np.clip(a, -1, 1) * 32767).round().astype("<i2").tobytes()
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-f", "s16le", "-ac", "2", "-ar", str(sr), "-i", "-", path],
                   input=pcm, check=True)


def rms_db(x):
    return float(20 * np.log10(np.sqrt((x ** 2).mean()) + 1e-12))


def main():
    os.makedirs(OUT, exist_ok=True)
    rep = {}
    for src, (dst, sr, xf) in FICHIERS.items():
        a = decode(os.path.join(SRC, src), sr)
        env = np.abs(a).max(1)
        nz = np.nonzero(env > 10 ** (SEUIL_SILENCE_DB / 20))[0]
        a = a[nz[0]:nz[-1] + 1]
        X = int(xf * sr)
        t = np.linspace(0, np.pi / 2, X)[:, None]
        fin, fout = np.sin(t), np.cos(t)
        n = len(a)
        out = a[:n - X].copy()
        out[:X] = a[:X] * fin + a[n - X:] * fout
        # contrôle du raccord : dernier échantillon de la boucle vs premier
        jump = float(np.abs(out[-1] - out[0]).max())
        slope = float(np.abs(np.diff(out[-2000:], axis=0)).mean())
        encode(os.path.join(OUT, dst), out, sr)
        rep[dst] = {"source": src, "duree_s": round(len(out) / sr, 3), "fondu_s": xf, "silence_retire_s":
                    [round(nz[0] / sr, 3), round((len(env) - 1 - nz[-1]) / sr, 3)], "saut_raccord": round(jump, 5),
                    "pente_moyenne": round(slope, 5), "rms_db": round(rms_db(out), 1),
                    "rms_fin_vs_debut_db": [round(rms_db(out[-sr // 2:]), 1), round(rms_db(out[:sr // 2]), 1)]}
    print(json.dumps(rep, indent=1, ensure_ascii=False))
    with open(os.path.join(OUT, "..", "..", "Source~", "UI", "scripts", "menus", "boucles_audio.json"), "w", encoding="utf-8") as f:
        json.dump(rep, f, indent=1, ensure_ascii=False)


if __name__ == "__main__":
    main()
