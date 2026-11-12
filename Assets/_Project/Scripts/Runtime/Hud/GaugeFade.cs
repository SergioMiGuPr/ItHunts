using UnityEngine;

namespace ItHunts.Hud
{
    /// <summary>
    /// HUD discret pour une jauge (logique pure, testable) : une jauge pleine et inutilisée s'estompe à idleAlpha après
    /// idleDelay ; toute charge utilisée, tout refus, toute recharge en cours la ramène tout de suite à 100 % ; une
    /// charge qui revient la fait clignoter une fois.
    /// </summary>
    public sealed class GaugeFade
    {
        private float _alpha = 1f;
        private float _fullSince = float.NegativeInfinity;
        private float _blinkUntil = float.NegativeInfinity;
        private int _lastWhole = -1;

        public float Alpha => _alpha;

        /// <summary>Vrai pendant le clignotement d'une charge revenue.</summary>
        public bool Blinking(float time) => time < _blinkUntil;

        /// <param name="value">Valeur de la jauge (charges, fractionnaires pendant la recharge).</param>
        /// <param name="max">Valeur pleine.</param>
        /// <param name="alert">Événement qui doit la montrer (refus, usage sans changement de valeur).</param>
        public float Update(float value, float max, bool alert, float time, float dt, float idleAlpha, float idleDelay,
            float fadeSpeed, float blinkDuration)
        {
            int whole = Mathf.FloorToInt(value + 1e-4f);
            if (_lastWhole >= 0 && whole > _lastWhole)
                _blinkUntil = time + blinkDuration;                     // une charge revient
            bool used = _lastWhole >= 0 && whole < _lastWhole;
            _lastWhole = whole;

            bool full = value >= max - 1e-4f;
            if (!full || used || alert)
            {
                _alpha = 1f;                                             // utilisée, en recharge ou refusée : tout de suite
                _fullSince = float.PositiveInfinity;
                return _alpha;
            }

            if (float.IsPositiveInfinity(_fullSince))
                _fullSince = time;                                       // vient de redevenir pleine
            if (float.IsNegativeInfinity(_fullSince))
                _fullSince = time;
            if (time - _fullSince >= idleDelay)
                _alpha = Mathf.MoveTowards(_alpha, idleAlpha, fadeSpeed * dt);
            else
                _alpha = 1f;
            return _alpha;
        }
    }
}
