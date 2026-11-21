using UnityEngine;

namespace ItHunts.Audio
{
    /// <summary>
    /// Gain au mixage d'une source (en dB, au-delà du volume 1 d'une AudioSource) : le crépitement du feu du campement est livré
    /// très faible (−46 dB RMS) mais avec des crêtes fortes (−5.7 dB : les claquements). +28 dB le placent à 5 dB environ sous la
    /// musique ; un limiteur (réduction immédiate, relâche réglable) garde les claquements sous le plafond sans les écrêter,
    /// puis un écrêtage doux de sécurité. Réglable en Play Mode (MenuSkin_Default : fireGainDb).
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioGain : MonoBehaviour
    {
        [Range(0f, 40f)] public float gainDb = 28f;
        [Tooltip("Plafond des crêtes après le gain (part de la pleine échelle).")]
        [Range(0.3f, 1f)] public float ceiling = 0.85f;
        [Tooltip("Relâche du limiteur (s).")]
        [Range(0.005f, 0.5f)] public float release = 0.06f;

        private volatile float _linear = 1f, _ceiling = 0.85f, _releaseCoef = 0.9997f;
        private float _env;
        private int _rate = 48000;

        public static float Linear(float db) => Mathf.Pow(10f, db / 20f);

        /// <summary>Écrêtage doux : linéaire jusqu'à ±0.8, puis courbe continue (pente 1 au raccord) qui ne dépasse jamais ±1.</summary>
        public static float SoftClip(float x)
        {
            float a = Mathf.Abs(x);
            if (a <= 0.8f)
                return x;
            return Mathf.Sign(x) * (0.8f + 0.2f * (float)System.Math.Tanh((a - 0.8f) / 0.2f));
        }

        /// <summary>
        /// Un bloc d'échantillons (pur, testable) : gain, limiteur sur l'enveloppe des crêtes (attaque immédiate, relâche
        /// exponentielle), écrêtage doux. Renvoie l'enveloppe pour le bloc suivant.
        /// </summary>
        public static float Process(float[] data, float gain, float ceiling, float releaseCoef, float env)
        {
            for (int i = 0; i < data.Length; i++)
            {
                float x = data[i] * gain;
                float a = Mathf.Abs(x);
                env = a > env ? a : env * releaseCoef;
                float reduce = env > ceiling ? ceiling / env : 1f;
                data[i] = SoftClip(x * reduce);
            }

            return env;
        }

        public static float ReleaseCoef(float seconds, int rate) => Mathf.Exp(-1f / Mathf.Max(1f, seconds * rate));

        private void Awake() => _rate = AudioSettings.outputSampleRate;

        private void Update()
        {
            _linear = Linear(gainDb);
            _ceiling = ceiling;
            _releaseCoef = ReleaseCoef(release, _rate);
        }

        private void OnAudioFilterRead(float[] data, int channels) => _env = Process(data, _linear, _ceiling, _releaseCoef, _env);
    }
}
