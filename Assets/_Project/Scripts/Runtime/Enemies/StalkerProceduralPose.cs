using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Enemies
{
    /// <summary>
    /// Mouvement provisoire du traqueur v4 tant qu'un clip de la DA manque (C5, matin du 2026-11-21) : dos voûté (pose de la vidéo
    /// de repos), tête qui regarde devant, bras ballants, cycle de marche calé sur la vitesse au sol (pas de glisse visible à
    /// l'allure de la foulée), course enragée plus penchée, genoux pliés quand il se baisse, respiration au repos. Rotations seules
    /// (et la hauteur du bassin) : aucune échelle, aucune longueur d'os ne change (le modèle ne se déforme jamais). Remplacé état par
    /// état par les clips Traqueur_&lt;Action&gt; dès qu'ils sont livrés (Stalker.Play).
    /// </summary>
    public sealed class StalkerProceduralPose
    {
        private readonly Transform _root, _self;
        private readonly Transform _pelvis, _head;
        private readonly Transform[] _spine;
        private readonly Transform[] _arm = new Transform[2], _fore = new Transform[2], _thigh = new Transform[2], _shin = new Transform[2], _foot = new Transform[2];
        private readonly Transform[] _all;
        private readonly Quaternion[] _rest;
        private readonly Vector3 _pelvisRest;
        private readonly float _leg;
        private float _phase, _breath;

        /// <summary>Phase du cycle de marche (0 à 1) ; tests.</summary>
        public float Phase => Mathf.Repeat(_phase / (Mathf.PI * 2f), 1f);
        public bool Valid => _pelvis != null && _spine.Length > 0 && _arm[0] != null && _thigh[0] != null;

        public StalkerProceduralPose(Transform modelRoot, Transform stalker)
        {
            _root = modelRoot;
            _self = stalker;
            Transform[] bones = modelRoot.GetComponentsInChildren<Transform>(true);
            Transform B(string n) => System.Array.Find(bones, t => t.name == n);
            _pelvis = B("Bassin");
            _head = B("Tete");
            _spine = System.Array.FindAll(new[] { B("Colonne_1"), B("Colonne_2"), B("Colonne_3"), B("Colonne_4"), B("Cou_1"), B("Cou_2") }, t => t != null);
            string[] side = { "_D", "_G" };
            for (int i = 0; i < 2; i++)
            {
                _arm[i] = B("Bras" + side[i]);
                _fore[i] = B("AvantBras" + side[i]);
                _thigh[i] = B("Cuisse" + side[i]);
                _shin[i] = B("Jambe" + side[i]);
                _foot[i] = B("Pied" + side[i]);
            }

            var list = new System.Collections.Generic.List<Transform>(_spine) { _head };
            list.AddRange(_arm);
            list.AddRange(_fore);
            list.AddRange(_thigh);
            list.AddRange(_shin);
            list.AddRange(_foot);
            list.RemoveAll(t => t == null);
            _all = list.ToArray();
            _rest = new Quaternion[_all.Length];
            for (int i = 0; i < _all.Length; i++)
                _rest[i] = _all[i].localRotation;
            _pelvisRest = _pelvis != null ? _pelvis.localPosition : Vector3.zero;
            _leg = _thigh[0] != null && _foot[0] != null ? Vector3.Distance(_thigh[0].position, _foot[0].position) : 7f;
        }

        /// <summary>Pose de l'image : clip voulu (nom de l'état), vitesse au sol (m/s), part de hauteur (1 debout, moins : baissé).</summary>
        public void Apply(string clip, float groundSpeed, float crouch, StalkerSettings s, float dt)
        {
            if (!Valid || s == null)
                return;
            for (int i = 0; i < _all.Length; i++)
                _all[i].localRotation = _rest[i];
            _pelvis.localPosition = _pelvisRest;

            Vector3 right = _self.right, up = _self.up, fwd = _self.forward;
            float run = Mathf.InverseLerp(s.chaseSpeed * 0.9f, s.maxSpeed, groundSpeed);           // course enragée : plus penché
            float walk = Mathf.Clamp01(groundSpeed / Mathf.Max(0.1f, s.roamSpeed));
            float stride = Mathf.Max(1f, s.provisionalStride * Mathf.Lerp(1f, 1.5f, run));
            _phase += groundSpeed * dt / stride * Mathf.PI * 2f;                                   // un cycle = deux pas = une foulée
            _breath += dt * Mathf.PI * 2f / 3.2f;
            float sw = Mathf.Sin(_phase), cw = Mathf.Cos(_phase);

            // Bassin : baissé (passage bas, porte étroite) et léger rebond à chaque pas.
            float drop = s.provisionalKneeDrop + (1f - Mathf.Clamp01(crouch)) * s.height * 0.45f + walk * 0.18f * Mathf.Abs(sw);
            _pelvis.position -= up * drop;

            // Dos voûté (repos de la vidéo), plus en course et baissé ; respiration.
            float hunch = s.provisionalHunch + run * 18f + (1f - Mathf.Clamp01(crouch)) * 25f + Mathf.Sin(_breath) * 1.5f * (1f - walk);
            foreach (Transform b in _spine)
                b.rotation = Quaternion.AngleAxis(hunch / _spine.Length, right) * b.rotation;
            if (_head != null)
                _head.rotation = Quaternion.AngleAxis(-hunch * 0.75f + Mathf.Sin(_breath * 0.5f) * 2f, right) * _head.rotation;   // regarde devant

            // Jambes : hanche, genou ; plis du genou pour garder les pieds au sol quand le bassin descend.
            float bend = 2f * Mathf.Acos(Mathf.Clamp01((_leg - drop) / Mathf.Max(0.1f, _leg))) * Mathf.Rad2Deg;
            float amp = s.provisionalLegSwing * walk * Mathf.Lerp(1f, 1.4f, run);
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? 1f : -1f, ph = i == 0 ? sw : -sw, lift = Mathf.Max(0f, i == 0 ? cw : -cw);
                if (_thigh[i] != null)
                    _thigh[i].rotation = Quaternion.AngleAxis(-(ph * amp + bend * 0.5f), right) * _thigh[i].rotation;
                if (_shin[i] != null)
                    _shin[i].rotation = Quaternion.AngleAxis(bend + lift * amp * 1.2f, right) * _shin[i].rotation;

                // Bras ballants, balancés à l'opposé de la jambe, coude un peu plié vers l'avant.
                if (_arm[i] != null && _fore[i] != null)
                {
                    Vector3 cur = (_fore[i].position - _arm[i].position).normalized;
                    Vector3 hang = Quaternion.AngleAxis(-ph * s.provisionalArmSwing * walk, right) * (-up * 0.96f + fwd * 0.22f + right * side * 0.16f).normalized;
                    _arm[i].rotation = Quaternion.FromToRotation(cur, hang) * _arm[i].rotation;
                    Transform hand = _fore[i].childCount > 0 ? _fore[i].GetChild(0) : null;
                    if (hand != null)
                    {
                        Vector3 f0 = (hand.position - _fore[i].position).normalized;
                        Vector3 f1 = Quaternion.AngleAxis(-(18f + run * 20f), right) * hang;
                        _fore[i].rotation = Quaternion.FromToRotation(f0, f1) * _fore[i].rotation;
                    }
                }
            }
        }
    }
}
