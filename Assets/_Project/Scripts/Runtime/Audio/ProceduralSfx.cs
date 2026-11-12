using UnityEngine;

namespace ItHunts.Audio
{
    /// <summary>
    /// Sons provisoires synthétisés par le code (aucun fichier audio) : échantillons mono à 44.1 kHz entre −1 et 1.
    /// Les fonctions renvoient des tableaux (testables) ; ToClip en fait un AudioClip. À remplacer par de vrais sons
    /// plus tard.
    /// </summary>
    public static class ProceduralSfx
    {
        public const int SampleRate = 44100;

        /// <summary>Début de traction : « zip » montant (balayage 300 → 1100 Hz, harmonique douce).</summary>
        public static float[] TractionStart() => Sweep(0.18f, 300f, 1100f, attack: 0.01f, harmonic: 0.3f);

        /// <summary>Fin de traction : clic sec (bruit très court et petit tic aigu).</summary>
        public static float[] TractionEnd()
        {
            float[] s = new float[Samples(0.05f)];
            var rng = new System.Random(7);
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Exp(-t * 120f);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                s[i] = env * (0.5f * noise + 0.5f * Mathf.Sin(2f * Mathf.PI * 2000f * t));
            }

            return Normalize(s, 0.8f);
        }

        /// <summary>Répulsion : coup sourd (90 → 40 Hz) et souffle (bruit filtré qui s'éteint).</summary>
        public static float[] Repel()
        {
            float duration = 0.4f;
            float[] s = new float[Samples(duration)];
            var rng = new System.Random(11);
            float phase = 0f, low = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float f = Mathf.Lerp(90f, 40f, t / duration);
                phase += 2f * Mathf.PI * f / SampleRate;
                float thump = Mathf.Sin(phase) * Mathf.Exp(-t * 9f);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                low += (noise - low) * 0.08f;                         // filtre passe-bas simple : souffle
                float whoosh = low * Mathf.Exp(-t * 7f) * Mathf.Min(1f, t / 0.02f);
                s[i] = thump + 2.5f * whoosh;
            }

            return Normalize(s, 0.9f);
        }

        /// <summary>Refus : deux bips graves (220 Hz).</summary>
        public static float[] Refusal()
        {
            float beep = 0.07f, gap = 0.05f;
            float[] s = new float[Samples(beep * 2f + gap)];
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float local = t < beep ? t : t - beep - gap;
                if (local < 0f || local >= beep)
                    continue;
                float env = Mathf.Min(1f, local / 0.005f) * Mathf.Min(1f, (beep - local) / 0.01f);
                float x = Mathf.Sin(2f * Mathf.PI * 220f * t);
                s[i] = env * (0.7f * x + 0.3f * Mathf.Sign(x));            // un peu « carré » : plus sec
            }

            return Normalize(s, 0.7f);
        }

        /// <summary>Charge revenue : petit tintement (1320 Hz et son octave).</summary>
        public static float[] Ready()
        {
            float[] s = new float[Samples(0.25f)];
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Min(1f, t / 0.003f) * Mathf.Exp(-t * 14f);
                s[i] = env * (Mathf.Sin(2f * Mathf.PI * 1320f * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * 2640f * t));
            }

            return Normalize(s, 0.6f);
        }

        /// <summary>
        /// Bourdonnement de traction en boucle : 1 s à 110 Hz (nombre entier de périodes, boucle sans claquement),
        /// dent de scie douce (5 harmoniques). La hauteur se règle avec AudioSource.pitch.
        /// </summary>
        public static float[] TractionHum()
        {
            float[] s = new float[SampleRate];
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float x = 0f;
                for (int k = 1; k <= 5; k++)
                    x += Mathf.Sin(2f * Mathf.PI * 110f * k * t) / k;
                x += 0.15f * Mathf.Sin(2f * Mathf.PI * 3f * t) * Mathf.Sin(2f * Mathf.PI * 220f * t);   // léger battement
                s[i] = x;
            }

            return Normalize(s, 0.5f);
        }

        public static AudioClip ToClip(string name, float[] samples)
        {
            var clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static int Samples(float seconds) => Mathf.CeilToInt(seconds * SampleRate);

        private static float[] Sweep(float duration, float from, float to, float attack, float harmonic)
        {
            float[] s = new float[Samples(duration)];
            float phase = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float u = t / duration;
                float f = Mathf.Lerp(from, to, u * u);
                phase += 2f * Mathf.PI * f / SampleRate;
                float env = Mathf.Min(1f, t / attack) * (1f - u) * (1f - u);
                s[i] = env * (Mathf.Sin(phase) + harmonic * Mathf.Sin(2f * phase));
            }

            return Normalize(s, 0.8f);
        }

        /// <summary>Ramène le pic à <paramref name="peak"/>.</summary>
        private static float[] Normalize(float[] s, float peak)
        {
            float max = 0f;
            foreach (float v in s)
                max = Mathf.Max(max, Mathf.Abs(v));
            if (max > 1e-6f)
                for (int i = 0; i < s.Length; i++)
                    s[i] *= peak / max;
            return s;
        }
    }
}
