using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Enemies
{
    /// <summary>Ce que le visage du traqueur doit montrer à cette image (données seulement).</summary>
    public struct StalkerFaceInput
    {
        public bool HasTarget, Sees, Enraged, Moving;
        public Vector3 LookAt;
        public float Now, LastCry, LastGrowl;
    }

    /// <summary>
    /// Visage du traqueur v4 (C4, matin du 2026-11-21) : couche séparée, ajoutée par-dessus l'animation après l'Animator. Regard
    /// vers le joueur repéré (cou et tête, angles limités), mâchoire qui pend et oscille (forme MachoirePendante), cri (forme Cri,
    /// yeux écarquillés) au passage en Enragé et juste avant une prise, grognement (lèvres retroussées) à proximité, yeux plissés en
    /// chasse, clignements au hasard, bave en particules. Les formes sont celles de la DA (6 formes de SK_Traqueur_v4_Tete) : aucune
    /// échelle d'os, aucune déformation hors de ces formes. Réglages dans StalkerSettings (« Visage »).
    /// </summary>
    public sealed class StalkerFace
    {
        private readonly Transform _self, _head, _neck;
        private readonly SkinnedMeshRenderer _skin;
        private readonly int _cri, _jaw, _lips, _squint, _wide, _blink;
        private readonly Vector3 _faceLocal;
        private readonly ParticleSystem _drool;
        private float _look, _blinkUntil = -1f, _nextBlink, _t;
        private float _wCri, _wJaw, _wLips, _wSquint, _wWide;

        public bool Valid => _skin != null && _head != null;
        /// <summary>Poids courants (0 à 100) : tests et debug.</summary>
        public float Cry => _wCri;
        public float Jaw => _wJaw;
        public float Lips => _wLips;
        public bool Blinking => _blinkUntil > _t;
        public float LookWeight => _look;
        public ParticleSystem Drool => _drool;

        public StalkerFace(Transform[] bones, Transform stalker, SkinnedMeshRenderer head, StalkerSettings s)
        {
            _self = stalker;
            _skin = head;
            _head = System.Array.Find(bones, t => t.name == "Tete");
            _neck = System.Array.Find(bones, t => t.name == "Cou_2");
            if (_skin == null || _skin.sharedMesh == null || _head == null)
                return;
            Mesh m = _skin.sharedMesh;
            _cri = m.GetBlendShapeIndex("Cri");
            _jaw = m.GetBlendShapeIndex("MachoirePendante");
            _lips = m.GetBlendShapeIndex("LevresRetroussees");
            _squint = m.GetBlendShapeIndex("YeuxPlisses");
            _wide = m.GetBlendShapeIndex("YeuxEcarquilles");
            _blink = m.GetBlendShapeIndex("Clignement");
            _faceLocal = _head.InverseTransformDirection(stalker.forward);    // modèle tourné vers +Z au repos
            _nextBlink = Random.Range(s.faceBlinkInterval.x, s.faceBlinkInterval.y);
            if (s.faceDroolRate > 0f)
                _drool = CreateDrool(System.Array.Find(bones, t => t.name == "Machoire") ?? _head, s);
        }

        private static ParticleSystem CreateDrool(Transform jaw, StalkerSettings s)
        {
            var go = new GameObject("Bave");
            go.transform.SetParent(jaw, false);
            go.transform.localPosition = Vector3.zero;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 0.3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.gravityModifier = 0.6f;
            main.startColor = new Color(0.85f, 0.88f, 0.78f, 0.75f);
            main.maxParticles = 64;
            ParticleSystem.EmissionModule em = ps.emission;
            em.rateOverTime = s.faceDroolRate;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh != null)
                r.sharedMaterial = new Material(sh) { name = "Bave du traqueur (jeu)", color = new Color(0.85f, 0.88f, 0.78f, 0.75f) };
            ps.Play();
            return ps;
        }

        /// <summary>Poids d'une forme pour un événement (cri, grognement) : montée, tenue, descente (0 à 1).</summary>
        public static float Envelope(float since, float duration, float rise = 0.12f, float fall = 0.35f)
        {
            if (since < 0f || since > duration)
                return 0f;
            if (since < rise)
                return since / rise;
            return since > duration - fall ? Mathf.Clamp01((duration - since) / fall) : 1f;
        }

        public void Update(in StalkerFaceInput i, StalkerSettings s, float dt)
        {
            if (!Valid || s == null)
                return;
            _t = i.Now;

            // Regard : cou (40 %) et tête (60 %) vers le joueur repéré, angles limités.
            _look = Mathf.MoveTowards(_look, i.HasTarget && i.Sees ? 1f : 0f, dt * s.faceLookSpeed);
            if (_look > 0.001f)
            {
                Vector3 from = _head.TransformDirection(_faceLocal);
                Vector3 to = i.LookAt - _head.position;
                Vector3 local = _self.InverseTransformDirection(to);
                float yaw = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -s.faceLookYaw, s.faceLookYaw);
                float pitch = Mathf.Clamp(-Mathf.Atan2(local.y, new Vector2(local.x, local.z).magnitude) * Mathf.Rad2Deg, -s.faceLookPitch, s.faceLookPitch);
                Vector3 want = _self.rotation * (Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward);
                Quaternion delta = Quaternion.FromToRotation(from, want);
                if (_neck != null)
                    _neck.rotation = Quaternion.Slerp(Quaternion.identity, delta, 0.4f * _look) * _neck.rotation;
                from = _head.TransformDirection(_faceLocal);
                _head.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(from, want), 0.6f * _look) * _head.rotation;
            }

            // Formes : cri (événement), grognement (événement), mâchoire qui pend, yeux selon l'état, clignements.
            float cry = Envelope(i.Now - i.LastCry, s.faceCryDuration);
            float growl = Envelope(i.Now - i.LastGrowl, s.faceGrowlDuration, 0.2f, 0.5f);
            float sway = Mathf.Sin(i.Now * 1.3f) * 0.6f + Mathf.Sin(i.Now * 2.9f + 1f) * 0.4f;
            float jaw = Mathf.Clamp(s.faceJawHang + s.faceJawSway * sway + (i.Moving ? 8f : 0f), 0f, 100f) * (1f - cry);
            float rate = dt * 10f;
            _wCri = Mathf.MoveTowards(_wCri, cry * 100f, rate * 100f);
            _wJaw = Mathf.MoveTowards(_wJaw, jaw, rate * 60f);
            _wLips = Mathf.MoveTowards(_wLips, Mathf.Max(growl * 100f, i.HasTarget && i.Sees ? 25f : 0f), rate * 60f);
            _wSquint = Mathf.MoveTowards(_wSquint, i.Sees && !i.Enraged && cry < 0.1f ? 55f : 0f, rate * 40f);
            _wWide = Mathf.MoveTowards(_wWide, Mathf.Max(cry, i.Enraged ? 0.6f : 0f) * 100f, rate * 60f);
            if (i.Now >= _nextBlink)
            {
                _blinkUntil = i.Now + 0.14f;
                _nextBlink = i.Now + Random.Range(s.faceBlinkInterval.x, s.faceBlinkInterval.y);
            }

            Set(_cri, _wCri);
            Set(_jaw, _wJaw);
            Set(_lips, _wLips);
            Set(_squint, _wSquint);
            Set(_wide, _wWide);
            Set(_blink, Blinking && cry < 0.5f ? 100f : 0f);
            if (_drool != null)
            {
                ParticleSystem.EmissionModule em = _drool.emission;
                em.rateOverTime = s.faceDroolRate * (1f + 2f * cry + (i.Moving ? 0.5f : 0f));
            }
        }

        private void Set(int index, float weight)
        {
            if (index >= 0)
                _skin.SetBlendShapeWeight(index, weight);
        }
    }
}
