# -*- coding: utf-8 -*-
"""Sons de l'épée du koala (nuit 5, lot 4) : retouche après relecture.

- vides : crête recalée à 45 ms (coupe du début, fondu d'entrée de 3 ms ; 1,5 ms pour les impacts) pour une synchro égale ;
- Vide_02 : passe-bas doux à 5 kHz (souffle trop aigu par rapport aux deux autres) ;
- tous : décalage continu retiré, passe-haut 70 Hz, fondu de fin de 10 ms, crête à −1 dBFS.
    python -I epee_nuit5.py   (travaille sur Assets/Art/Audio/Armes/Sfx_Epee_*.wav, en place)
"""
import os, sys, wave
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "..", "..", ".."))
D = os.path.join(ROOT, "Assets", "Art", "Audio", "Armes")


def lit(p):
    with wave.open(p, "rb") as w:
        sr, n, ch, sw = w.getframerate(), w.getnframes(), w.getnchannels(), w.getsampwidth()
        x = np.frombuffer(w.readframes(n), dtype=np.int16).astype(np.float64) / 32768.0
    if ch > 1:
        x = x.reshape(-1, ch).mean(axis=1)
    return x, sr


def ecrit(p, x, sr):
    y = np.clip(np.round(x * 32767.0), -32768, 32767).astype(np.int16)
    with wave.open(p, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(sr); w.writeframes(y.tobytes())


def filtre(x, sr, haut=None, bas=None):
    """Filtrage doux dans le domaine fréquentiel (pentes en cosinus sur une octave)."""
    n = len(x); N = 1 << int(np.ceil(np.log2(n * 2)))
    X = np.fft.rfft(np.concatenate([x, np.zeros(N - n)]))
    f = np.fft.rfftfreq(N, 1.0 / sr)
    g = np.ones_like(f)
    if haut:
        t = np.clip(np.log2(np.maximum(f, 1e-9) / haut) + 0.5, 0, 1)
        g *= 0.5 - 0.5 * np.cos(np.pi * t)
    if bas:
        t = np.clip(np.log2(np.maximum(f, 1e-9) / bas) + 0.5, 0, 1)
        g *= 0.5 + 0.5 * np.cos(np.pi * t)
    return np.fft.irfft(X * g)[:n]


def traite(nom):
    p = os.path.join(D, "Sfx_Epee_%s.wav" % nom)
    x, sr = lit(p)
    x = x - x.mean()
    x = filtre(x, sr, haut=70.0, bas=5000.0 if nom == "Vide_02" else None)
    if nom.startswith("Vide"):
        env = np.convolve(np.abs(x), np.ones(int(sr * 0.005)) / int(sr * 0.005), mode="same")
        pic = int(np.argmax(env))
        debut = max(0, pic - int(sr * 0.045))
        x = x[debut:]
    fi = int(sr * (0.003 if nom.startswith("Vide") else 0.0015))      # fondu d'entrée : pas de clic au départ
    x[:fi] *= np.linspace(0, 1, fi)
    ff = int(sr * 0.010)
    x[-ff:] *= np.linspace(1, 0, ff) ** 2
    x = x - np.linspace(x[0], x[-1], len(x)) * 0
    x *= 10 ** (-1 / 20) / max(np.abs(x).max(), 1e-9)
    ecrit(p, x, sr)
    env = np.convolve(np.abs(x), np.ones(int(sr * 0.005)) / int(sr * 0.005), mode="same")
    X = np.abs(np.fft.rfft(x)); f = np.fft.rfftfreq(len(x), 1.0 / sr)
    c = float((X * f).sum() / max(X.sum(), 1e-9))
    fin = 20 * np.log10(max(np.abs(x[-int(sr * 0.002):]).max(), 1e-9))
    return "%s : %.3f s, crête à %d ms, centre spectral %.0f Hz, fin %.0f dB, moyenne %.5f" % (nom, len(x) / sr, int(np.argmax(env) * 1000 / sr), c, fin, x.mean())


if __name__ == "__main__":
    sys.stdout = open(sys.stdout.fileno(), "w", encoding="utf-8", closefd=False)
    for n in ("Vide_01", "Vide_02", "Vide_03", "Impact_01", "Impact_02", "Impact_03"):
        print(traite(n))
