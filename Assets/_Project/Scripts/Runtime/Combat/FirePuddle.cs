using System.Collections.Generic;
using UnityEngine;

namespace ItHunts.Combat
{
    /// <summary>
    /// Flaque de feu au sol (renard : « Feu grégeois » R5 et « Foulée ardente » R8, D-100) : pendant duration s, tout ennemi vivant
    /// à moins de radius (à hauteur du sol) brûle (dps PV/s, relancé tant qu'il reste dedans), est ralenti (slow) et, à sa première
    /// entrée, étourdi (entryStun). Le traqueur n'est jamais touché (Burning refuse ImmuneToOrbs). Les joueurs ne brûlent pas
    /// (le feu du renard est le sien). Retour visuel : flamme peinte de la DA si elle existe, sinon une lumière orange.
    /// </summary>
    public sealed class FirePuddle : MonoBehaviour
    {
        private float _radius, _until, _dps, _slow, _entryStun, _nextCheck;
        private int _attacker, _maxStacks;
        private readonly HashSet<Damageable> _entered = new HashSet<Damageable>();

        public static readonly List<FirePuddle> All = new List<FirePuddle>();
        public float Radius => _radius;
        public float Remaining => Mathf.Max(0f, _until - Time.time);
        /// <summary>Ennemis touchés par cette flaque (mesures, tests).</summary>
        public int Burned { get; private set; }

        public static FirePuddle Spawn(Vector3 at, float radius, float duration, float dps, int attackerId, float slow = 0f, float entryStun = 0f,
            int maxStacks = 1)
        {
            if (radius <= 0f || duration <= 0f)
                return null;
            var go = new GameObject("Flaque de feu");
            go.transform.position = at;
            var p = go.AddComponent<FirePuddle>();
            p._radius = radius;
            p._until = Time.time + duration;
            p._dps = dps;
            p._slow = slow;
            p._entryStun = entryStun;
            p._attacker = attackerId;
            p._maxStacks = Mathf.Max(1, maxStacks);
            p.Visual();
            Destroy(go, duration + 0.1f);
            return p;
        }

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        private void Visual()
        {
            if (!Application.isPlaying)
                return;
            var vfx = GameVfx.Settings;
            GameObject fx = vfx != null && vfx.torchFire != null
                ? GameVfx.Spawn(vfx.torchFire, transform.position + Vector3.up * 0.1f, Quaternion.identity, Mathf.Max(0.5f, _radius * 0.8f), transform, 0f)
                : null;
            if (fx != null)
                return;
            var light = new GameObject("Lueur").AddComponent<Light>();
            light.transform.SetParent(transform, false);
            light.transform.localPosition = Vector3.up * 0.6f;
            light.type = LightType.Point;
            light.color = new Color(1f, 0.45f, 0.1f);
            light.intensity = 3f;
            light.range = _radius * 2.5f;
        }

        /// <summary>Une vérification (public pour les tests) : brûle, ralentit, étourdit à l'entrée.</summary>
        public void Check(float now)
        {
            foreach (Damageable d in UpgradeEffects.EnemiesAround(transform.position, _radius))
            {
                Vector3 c = d.Center;
                if (Mathf.Abs(c.y - transform.position.y) > 2.5f)
                    continue;
                if (Burning.Apply(d, _dps, 1f, _attacker, 0.25f, null, now, _maxStacks, _slow) == null)
                    continue;
                Burned++;
                if (_entryStun > 0f && _entered.Add(d) && d.Body != null)
                    d.Body.StunFor(_entryStun);
            }
        }

        private void FixedUpdate()
        {
            float now = Time.time;
            if (now >= _until)
            {
                enabled = false;
                return;
            }

            if (now < _nextCheck)
                return;
            _nextCheck = now + 0.2f;
            Check(now);
        }
    }
}
